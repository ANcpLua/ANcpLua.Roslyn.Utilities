// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI.Workflows.Generators.Diagnostics;
using Microsoft.Agents.AI.Workflows.Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Agents.AI.Workflows.Generators.Analysis;

/// <summary>
/// Provides semantic analysis of executor route candidates.
/// </summary>
/// <remarks>
/// Analysis is split into two phases for efficiency with incremental generators:
/// <list type="number">
/// <item><see cref="AnalyzeHandlerMethod"/> - Called per method, extracts data and performs method-level validation only.</item>
/// <item><see cref="CombineHandlerMethodResults"/> - Groups methods by class and performs class-level validation once.</item>
/// </list>
/// This avoids redundant class validation when multiple handlers exist in the same class.
/// </remarks>
internal static class SemanticAnalyzer
{
    // Fully-qualified type names used for symbol comparison
    private const string ExecutorTypeName = "Microsoft.Agents.AI.Workflows.Executor";
    private const string ExecutorOfTTypeName = "Microsoft.Agents.AI.Workflows.Executor`1";
    private const string WorkflowContextTypeName = "Microsoft.Agents.AI.Workflows.IWorkflowContext";
    private const string CancellationTokenTypeName = "System.Threading.CancellationToken";
    private const string ValueTaskTypeName = "System.Threading.Tasks.ValueTask";
    private const string ValueTaskOfTTypeName = "System.Threading.Tasks.ValueTask`1";
    private const string SendsMessageAttributeName = "Microsoft.Agents.AI.Workflows.SendsMessageAttribute";
    private const string YieldsOutputAttributeName = "Microsoft.Agents.AI.Workflows.YieldsOutputAttribute";

    private readonly record struct KnownTypes(
        INamedTypeSymbol? Executor,
        INamedTypeSymbol? ExecutorOfT,
        INamedTypeSymbol? WorkflowContext,
        INamedTypeSymbol? CancellationToken,
        INamedTypeSymbol? ValueTask,
        INamedTypeSymbol? ValueTaskOfT);

    /// <summary>
    /// Analyzes a method with [MessageHandler] attribute found by ForAttributeWithMetadataName.
    /// Returns a MethodAnalysisResult containing both method info and class context.
    /// </summary>
    /// <remarks>
    /// This method only extracts raw data and performs method-level validation.
    /// Class-level validation is deferred to <see cref="CombineHandlerMethodResults"/> to avoid
    /// redundant validation when a class has multiple handler methods.
    /// </remarks>
    public static MethodAnalysisResult AnalyzeHandlerMethod(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        // The target should be a method
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return MethodAnalysisResult.Empty;
        }

        // Get the containing class
        INamedTypeSymbol? classSymbol = methodSymbol.ContainingType;
        if (classSymbol is null)
        {
            return MethodAnalysisResult.Empty;
        }

        // Get the method syntax for location info
        MethodDeclarationSyntax? methodSyntax = context.TargetNode as MethodDeclarationSyntax;
        KnownTypes knownTypes = ResolveKnownTypes(context.SemanticModel.Compilation);

        // Extract class-level info (raw facts, no validation here)
        string classKey = GetClassKey(classSymbol);
        bool isPartialClass = IsPartialClass(classSymbol, cancellationToken);
        bool derivesFromExecutor = DerivesFromExecutor(classSymbol, knownTypes);
        bool hasManualConfigureRoutes = HasConfigureRoutesDefined(classSymbol);

        // Extract class metadata
        string? @namespace = classSymbol.ContainingNamespace?.IsGlobalNamespace == true
            ? null
            : classSymbol.ContainingNamespace?.ToDisplayString();
        string className = classSymbol.Name;
        string? genericParameters = GetGenericParameters(classSymbol);
        bool isNested = classSymbol.ContainingType != null;
        string containingTypeChain = GetContainingTypeChain(classSymbol);
        bool baseHasConfigureRoutes = BaseHasConfigureRoutes(classSymbol, knownTypes);
        Microsoft.Agents.AI.Workflows.Generators.Models.EquatableArray<string> classSendTypes = GetClassLevelTypes(classSymbol, SendsMessageAttributeName);
        Microsoft.Agents.AI.Workflows.Generators.Models.EquatableArray<string> classYieldTypes = GetClassLevelTypes(classSymbol, YieldsOutputAttributeName);

        // Get class location for class-level diagnostics
        DiagnosticLocationInfo? classLocation = GetClassLocation(classSymbol, cancellationToken);

        // Analyze the handler method (method-level validation only)
        // Skip method analysis if class doesn't derive from Executor (class-level diagnostic will be reported later)
        var methodDiagnostics = ImmutableArray.CreateBuilder<Microsoft.Agents.AI.Workflows.Generators.Models.DiagnosticInfo>();
        HandlerInfo? handler = null;
        if (derivesFromExecutor)
        {
            handler = AnalyzeHandler(methodSymbol, methodSyntax, context.Attributes, methodDiagnostics, knownTypes);
        }

        return new MethodAnalysisResult(
            classKey, @namespace, className, genericParameters, isNested, containingTypeChain,
            baseHasConfigureRoutes, classSendTypes, classYieldTypes,
            isPartialClass, derivesFromExecutor, hasManualConfigureRoutes,
            classLocation,
            handler,
            Diagnostics: new Microsoft.Agents.AI.Workflows.Generators.Models.EquatableArray<Microsoft.Agents.AI.Workflows.Generators.Models.DiagnosticInfo>(methodDiagnostics.ToImmutable()));
    }

    /// <summary>
    /// Combines multiple MethodAnalysisResults for the same class into an AnalysisResult.
    /// Performs class-level validation once (instead of per-method) for efficiency.
    /// </summary>
    public static AnalysisResult CombineHandlerMethodResults(IEnumerable<MethodAnalysisResult> methodResults)
    {
        using IEnumerator<MethodAnalysisResult> enumerator = methodResults.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return AnalysisResult.Empty;
        }

        // All methods should have same class info - use first.
        MethodAnalysisResult first = enumerator.Current;
        Location classLocation = first.ClassLocation?.ToRoslynLocation() ?? Location.None;

        ImmutableArray<Diagnostic>.Builder allDiagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        ImmutableArray<HandlerInfo>.Builder handlers = ImmutableArray.CreateBuilder<HandlerInfo>();
        CollectMethodResult(first, allDiagnostics, handlers);

        while (enumerator.MoveNext())
        {
            CollectMethodResult(enumerator.Current, allDiagnostics, handlers);
        }

        // Class-level validation (done once, not per-method)
        if (!first.DerivesFromExecutor)
        {
            allDiagnostics.Add(Diagnostic.Create(
                DiagnosticDescriptors.NotAnExecutor,
                classLocation,
                first.ClassName,
                first.ClassName));
            return AnalysisResult.WithDiagnostics(allDiagnostics.ToImmutable());
        }

        if (!first.IsPartialClass)
        {
            allDiagnostics.Add(Diagnostic.Create(
                DiagnosticDescriptors.ClassMustBePartial,
                classLocation,
                first.ClassName));
            return AnalysisResult.WithDiagnostics(allDiagnostics.ToImmutable());
        }

        if (first.HasManualConfigureRoutes)
        {
            allDiagnostics.Add(Diagnostic.Create(
                DiagnosticDescriptors.ConfigureRoutesAlreadyDefined,
                classLocation,
                first.ClassName));
            return AnalysisResult.WithDiagnostics(allDiagnostics.ToImmutable());
        }

        if (handlers.Count == 0)
        {
            return AnalysisResult.WithDiagnostics(allDiagnostics.ToImmutable());
        }

        ExecutorInfo executorInfo = new(
            first.Namespace,
            first.ClassName,
            first.GenericParameters,
            first.IsNested,
            first.ContainingTypeChain,
            first.BaseHasConfigureRoutes,
            new Microsoft.Agents.AI.Workflows.Generators.Models.EquatableArray<HandlerInfo>(handlers.ToImmutable()),
            first.ClassSendTypes,
            first.ClassYieldTypes);

        if (allDiagnostics.Count > 0)
        {
            return AnalysisResult.WithInfoAndDiagnostics(executorInfo, allDiagnostics.ToImmutable());
        }

        return AnalysisResult.Success(executorInfo);
    }

    /// <summary>
    /// Analyzes a class with [SendsMessage] or [YieldsOutput] attribute found by ForAttributeWithMetadataName.
    /// Returns ClassProtocolInfo entries for each attribute instance (handles multiple attributes of same type).
    /// </summary>
    /// <param name="context">The generator attribute syntax context.</param>
    /// <param name="attributeKind">Whether this is a Send or Yield attribute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The analysis results for the class protocol attributes.</returns>
    public static ImmutableArray<ClassProtocolInfo> AnalyzeClassProtocolAttribute(
        GeneratorAttributeSyntaxContext context,
        ProtocolAttributeKind attributeKind,
        CancellationToken cancellationToken)
    {
        // The target should be a class
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return ImmutableArray<ClassProtocolInfo>.Empty;
        }

        KnownTypes knownTypes = ResolveKnownTypes(context.SemanticModel.Compilation);

        // Extract class-level info (same for all attributes)
        string classKey = GetClassKey(classSymbol);
        bool isPartialClass = IsPartialClass(classSymbol, cancellationToken);
        bool derivesFromExecutor = DerivesFromExecutor(classSymbol, knownTypes);
        bool hasManualConfigureRoutes = HasConfigureRoutesDefined(classSymbol);

        string? @namespace = classSymbol.ContainingNamespace?.IsGlobalNamespace == true
            ? null
            : classSymbol.ContainingNamespace?.ToDisplayString();
        string className = classSymbol.Name;
        string? genericParameters = GetGenericParameters(classSymbol);
        bool isNested = classSymbol.ContainingType != null;
        string containingTypeChain = GetContainingTypeChain(classSymbol);
        DiagnosticLocationInfo? classLocation = GetClassLocation(classSymbol, cancellationToken);

        // Extract a ClassProtocolInfo for each attribute instance
        ImmutableArray<ClassProtocolInfo>.Builder results = ImmutableArray.CreateBuilder<ClassProtocolInfo>();

        foreach (AttributeData attr in context.Attributes)
        {
            if (attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is INamedTypeSymbol typeSymbol)
            {
                string typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                results.Add(new ClassProtocolInfo(
                    classKey,
                    @namespace,
                    className,
                    genericParameters,
                    isNested,
                    containingTypeChain,
                    isPartialClass,
                    derivesFromExecutor,
                    hasManualConfigureRoutes,
                    classLocation,
                    typeName,
                    attributeKind));
            }
        }

        return results.ToImmutable();
    }

    /// <summary>
    /// Combines ClassProtocolInfo results into an AnalysisResult for classes that only have protocol attributes
    /// (no [MessageHandler] methods). This generates only ConfigureSentTypes/ConfigureYieldTypes overrides.
    /// </summary>
    /// <param name="protocolInfos">The protocol info entries for the class.</param>
    /// <returns>The combined analysis result.</returns>
    public static AnalysisResult CombineProtocolOnlyResults(IEnumerable<ClassProtocolInfo> protocolInfos)
    {
        using IEnumerator<ClassProtocolInfo> enumerator = protocolInfos.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return AnalysisResult.Empty;
        }

        // All entries should have same class info - use first.
        ClassProtocolInfo first = enumerator.Current;
        Location classLocation = first.ClassLocation?.ToRoslynLocation() ?? Location.None;

        // Class-level validation
        if (!first.DerivesFromExecutor)
        {
            return AnalysisResult.WithDiagnostics(ImmutableArray.Create(Diagnostic.Create(
                DiagnosticDescriptors.NotAnExecutor,
                classLocation,
                first.ClassName,
                first.ClassName)));
        }

        if (!first.IsPartialClass)
        {
            return AnalysisResult.WithDiagnostics(ImmutableArray.Create(Diagnostic.Create(
                DiagnosticDescriptors.ClassMustBePartial,
                classLocation,
                first.ClassName)));
        }

        HashSet<string> sendTypeSet = new(StringComparer.Ordinal);
        HashSet<string> yieldTypeSet = new(StringComparer.Ordinal);
        AddProtocolType(first, sendTypeSet, yieldTypeSet);

        while (enumerator.MoveNext())
        {
            AddProtocolType(enumerator.Current, sendTypeSet, yieldTypeSet);
        }

        // Create ExecutorInfo with no handlers but with protocol types
        ExecutorInfo executorInfo = new(
            first.Namespace,
            first.ClassName,
            first.GenericParameters,
            first.IsNested,
            first.ContainingTypeChain,
            BaseHasConfigureRoutes: false, // Not relevant for protocol-only
            Handlers: Microsoft.Agents.AI.Workflows.Generators.Models.EquatableArray<HandlerInfo>.Empty,
            ClassSendTypes: ToSortedEquatableArray(sendTypeSet),
            ClassYieldTypes: ToSortedEquatableArray(yieldTypeSet));

        return AnalysisResult.Success(executorInfo);
    }

    private static void CollectMethodResult(
        MethodAnalysisResult method,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        ImmutableArray<HandlerInfo>.Builder handlers)
    {
        foreach (Microsoft.Agents.AI.Workflows.Generators.Models.DiagnosticInfo diagnostic in method.Diagnostics)
        {
            diagnostics.Add(diagnostic.ToDiagnostic());
        }

        if (method.Handler is { } handler)
        {
            handlers.Add(handler);
        }
    }

    private static void AddProtocolType(
        ClassProtocolInfo protocol,
        HashSet<string> sendTypes,
        HashSet<string> yieldTypes)
    {
        if (protocol.AttributeKind == ProtocolAttributeKind.Send)
        {
            sendTypes.Add(protocol.TypeName);
        }
        else
        {
            yieldTypes.Add(protocol.TypeName);
        }
    }

    private static Microsoft.Agents.AI.Workflows.Generators.Models.EquatableArray<string> ToSortedEquatableArray(HashSet<string> values)
    {
        return new Microsoft.Agents.AI.Workflows.Generators.Models.EquatableArray<string>(ToSortedImmutableArray(values));
    }

    /// <summary>
    /// Gets the source location of the class identifier for diagnostic reporting.
    /// </summary>
    private static DiagnosticLocationInfo? GetClassLocation(INamedTypeSymbol classSymbol, CancellationToken cancellationToken)
    {
        foreach (SyntaxReference syntaxRef in classSymbol.DeclaringSyntaxReferences)
        {
            SyntaxNode syntax = syntaxRef.GetSyntax(cancellationToken);
            if (syntax is ClassDeclarationSyntax classDecl)
            {
                return DiagnosticLocationInfo.FromLocation(classDecl.Identifier.GetLocation());
            }
        }

        return null;
    }

    /// <summary>
    /// Returns a unique identifier for the class used to group methods by their containing type.
    /// </summary>
    private static string GetClassKey(INamedTypeSymbol classSymbol)
    {
        return classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    /// <summary>
    /// Checks if any declaration of the class has the 'partial' modifier.
    /// </summary>
    private static bool IsPartialClass(INamedTypeSymbol classSymbol, CancellationToken cancellationToken)
    {
        foreach (SyntaxReference syntaxRef in classSymbol.DeclaringSyntaxReferences)
        {
            SyntaxNode syntax = syntaxRef.GetSyntax(cancellationToken);
            if (syntax is ClassDeclarationSyntax classDecl &&
                classDecl.IsPartial())
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Walks the inheritance chain to check if the class derives from Executor or Executor&lt;T&gt;.
    /// </summary>
    private static bool DerivesFromExecutor(INamedTypeSymbol classSymbol, KnownTypes knownTypes)
    {
        if (knownTypes.Executor is not null || knownTypes.ExecutorOfT is not null)
        {
            INamedTypeSymbol? current = classSymbol.BaseType;
            while (current is not null)
            {
                if (MatchesTypeOrOriginalDefinition(current, knownTypes.Executor) ||
                    MatchesTypeOrOriginalDefinition(current, knownTypes.ExecutorOfT))
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }

        // Fallback if metadata resolution fails.
        INamedTypeSymbol? fallbackCurrent = classSymbol.BaseType;
        while (fallbackCurrent is not null)
        {
            string fullName = fallbackCurrent.OriginalDefinition.ToDisplayString();
            if (fullName == ExecutorTypeName || fullName.StartsWith(ExecutorTypeName + "<", StringComparison.Ordinal))
            {
                return true;
            }

            fallbackCurrent = fallbackCurrent.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Checks if this class directly defines ConfigureRoutes (not inherited).
    /// If so, we skip generation to avoid conflicting with user's manual implementation.
    /// </summary>
    private static bool HasConfigureRoutesDefined(INamedTypeSymbol classSymbol)
    {
        foreach (var member in classSymbol.GetMembers("ConfigureRoutes"))
        {
            if (member is IMethodSymbol method && !method.IsAbstract &&
                SymbolEqualityComparer.Default.Equals(method.ContainingType, classSymbol))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if any base class (between this class and Executor) defines ConfigureRoutes.
    /// If so, generated code should call base.ConfigureRoutes() to preserve inherited handlers.
    /// </summary>
    private static bool BaseHasConfigureRoutes(INamedTypeSymbol classSymbol, KnownTypes knownTypes)
    {
        INamedTypeSymbol? baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            // Stop at Executor - its ConfigureRoutes is abstract/empty
            if (MatchesTypeOrOriginalDefinition(baseType, knownTypes.Executor) ||
                MatchesTypeOrOriginalDefinition(baseType, knownTypes.ExecutorOfT))
            {
                return false;
            }

            foreach (var member in baseType.GetMembers("ConfigureRoutes"))
            {
                if (member is IMethodSymbol method && !method.IsAbstract)
                {
                    return true;
                }
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    private static KnownTypes ResolveKnownTypes(Compilation compilation) =>
        new(
            compilation.GetBestTypeByMetadataName(ExecutorTypeName),
            compilation.GetBestTypeByMetadataName(ExecutorOfTTypeName),
            compilation.GetBestTypeByMetadataName(WorkflowContextTypeName),
            compilation.GetBestTypeByMetadataName(CancellationTokenTypeName),
            compilation.GetBestTypeByMetadataName(ValueTaskTypeName),
            compilation.GetBestTypeByMetadataName(ValueTaskOfTTypeName));

    private static bool MatchesTypeOrOriginalDefinition(ITypeSymbol? symbol, ITypeSymbol? expected)
    {
        if (symbol is null || expected is null)
        {
            return false;
        }

        if (SymbolEqualityComparer.Default.Equals(symbol, expected))
        {
            return true;
        }

        return symbol is INamedTypeSymbol named &&
               SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, expected);
    }

    private static bool IsTypeMatch(ITypeSymbol type, ITypeSymbol? expected, string fallbackFullName) =>
        expected is not null
            ? MatchesTypeOrOriginalDefinition(type, expected)
            : type.ToDisplayString() == fallbackFullName;

    private static bool HasResolvedValueTaskSymbols(KnownTypes knownTypes) =>
        knownTypes.ValueTask is not null && knownTypes.ValueTaskOfT is not null;

    /// <summary>
    /// Validates a handler method's signature and extracts metadata.
    /// </summary>
    /// <remarks>
    /// Valid signatures:
    /// <list type="bullet">
    /// <item><c>void Handle(TMessage, IWorkflowContext, [CancellationToken])</c></item>
    /// <item><c>ValueTask HandleAsync(TMessage, IWorkflowContext, [CancellationToken])</c></item>
    /// <item><c>ValueTask&lt;TResult&gt; HandleAsync(TMessage, IWorkflowContext, [CancellationToken])</c></item>
    /// <item><c>TResult Handle(TMessage, IWorkflowContext, [CancellationToken])</c> (sync with result)</item>
    /// </list>
    /// </remarks>
    private static HandlerInfo? AnalyzeHandler(
        IMethodSymbol methodSymbol,
        MethodDeclarationSyntax? methodSyntax,
        ImmutableArray<AttributeData> messageHandlerAttributes,
        ImmutableArray<Microsoft.Agents.AI.Workflows.Generators.Models.DiagnosticInfo>.Builder diagnostics,
        KnownTypes knownTypes)
    {
        Location location = methodSyntax?.Identifier.GetLocation() ?? Location.None;

        // Check if static
        if (methodSymbol.IsStatic)
        {
            diagnostics.Add(Microsoft.Agents.AI.Workflows.Generators.Models.DiagnosticInfo.Create(DiagnosticDescriptors.HandlerCannotBeStatic, location, methodSymbol.Name));
            return null;
        }

        // Check parameter count
        if (methodSymbol.Parameters.Length < 2)
        {
            diagnostics.Add(Microsoft.Agents.AI.Workflows.Generators.Models.DiagnosticInfo.Create(DiagnosticDescriptors.InsufficientParameters, location, methodSymbol.Name));
            return null;
        }

        // Check second parameter is IWorkflowContext
        IParameterSymbol secondParam = methodSymbol.Parameters[1];
        if (!IsTypeMatch(secondParam.Type, knownTypes.WorkflowContext, WorkflowContextTypeName))
        {
            diagnostics.Add(Microsoft.Agents.AI.Workflows.Generators.Models.DiagnosticInfo.Create(DiagnosticDescriptors.MissingWorkflowContext, location, methodSymbol.Name));
            return null;
        }

        // Check for optional CancellationToken as third parameter
        bool hasCancellationToken = methodSymbol.Parameters.Length >= 3 &&
            IsTypeMatch(methodSymbol.Parameters[2].Type, knownTypes.CancellationToken, CancellationTokenTypeName);

        // Analyze return type
        ITypeSymbol returnType = methodSymbol.ReturnType;
        HandlerSignatureKind? signatureKind = GetSignatureKind(returnType, knownTypes);
        if (signatureKind == null)
        {
            diagnostics.Add(Microsoft.Agents.AI.Workflows.Generators.Models.DiagnosticInfo.Create(DiagnosticDescriptors.InvalidReturnType, location, methodSymbol.Name));
            return null;
        }

        // Get input type
        ITypeSymbol inputType = methodSymbol.Parameters[0].Type;
        string inputTypeName = inputType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Get output type
        string? outputTypeName = null;
        if (signatureKind == HandlerSignatureKind.ResultSync)
        {
            outputTypeName = returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }
        else if (signatureKind == HandlerSignatureKind.ResultAsync && returnType is INamedTypeSymbol namedReturn)
        {
            if (namedReturn.TypeArguments.Length == 1)
            {
                outputTypeName = namedReturn.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        // Get Yield and Send types from attribute
        (Microsoft.Agents.AI.Workflows.Generators.Models.EquatableArray<string> yieldTypes, Microsoft.Agents.AI.Workflows.Generators.Models.EquatableArray<string> sendTypes) = GetAttributeTypeArrays(messageHandlerAttributes);

        return new HandlerInfo(
            methodSymbol.Name,
            inputTypeName,
            outputTypeName,
            signatureKind.Value,
            hasCancellationToken,
            yieldTypes,
            sendTypes);
    }

    /// <summary>
    /// Determines the handler signature kind from the return type.
    /// </summary>
    /// <returns>The signature kind, or null if the return type is not supported (e.g., Task, Task&lt;T&gt;).</returns>
    private static HandlerSignatureKind? GetSignatureKind(ITypeSymbol returnType, KnownTypes knownTypes)
    {
        if (returnType.SpecialType == SpecialType.System_Void)
        {
            return HandlerSignatureKind.VoidSync;
        }

        if (MatchesTypeOrOriginalDefinition(returnType, knownTypes.ValueTask) ||
            (!HasResolvedValueTaskSymbols(knownTypes) && returnType.ToDisplayString() == ValueTaskTypeName))
        {
            return HandlerSignatureKind.VoidAsync;
        }

        if (returnType is INamedTypeSymbol namedType &&
            (MatchesTypeOrOriginalDefinition(namedType, knownTypes.ValueTaskOfT) ||
             (!HasResolvedValueTaskSymbols(knownTypes) &&
              namedType.OriginalDefinition.ToDisplayString() == "System.Threading.Tasks.ValueTask<TResult>")))
        {
            return HandlerSignatureKind.ResultAsync;
        }

        string returnTypeName = returnType.ToDisplayString();

        // Any non-void, non-Task type is treated as a synchronous result
        if (returnType.SpecialType != SpecialType.System_Void &&
            !returnTypeName.StartsWith("System.Threading.Tasks.Task", StringComparison.Ordinal) &&
            !returnTypeName.StartsWith("System.Threading.Tasks.ValueTask", StringComparison.Ordinal))
        {
            return HandlerSignatureKind.ResultSync;
        }

        // Task/Task<T> not supported - must use ValueTask
        return null;
    }

    /// <summary>
    /// Extracts Yield and Send type arrays from the [MessageHandler] attribute's named arguments.
    /// </summary>
    /// <example>
    /// [MessageHandler(Yield = new[] { typeof(OutputA), typeof(OutputB) }, Send = new[] { typeof(Request) })]
    /// </example>
    private static (Microsoft.Agents.AI.Workflows.Generators.Models.EquatableArray<string> YieldTypes, Microsoft.Agents.AI.Workflows.Generators.Models.EquatableArray<string> SendTypes) GetAttributeTypeArrays(
        ImmutableArray<AttributeData> messageHandlerAttributes)
    {
        var yieldTypes = ImmutableArray<string>.Empty;
        var sendTypes = ImmutableArray<string>.Empty;

        if (messageHandlerAttributes.IsDefaultOrEmpty)
        {
            return (new Microsoft.Agents.AI.Workflows.Generators.Models.EquatableArray<string>(yieldTypes), new Microsoft.Agents.AI.Workflows.Generators.Models.EquatableArray<string>(sendTypes));
        }

        foreach (AttributeData messageHandlerAttribute in messageHandlerAttributes)
        {
            foreach (KeyValuePair<string, TypedConstant> namedArgument in messageHandlerAttribute.NamedArguments)
            {
                if (namedArgument.Key.Equals("Yield", StringComparison.Ordinal) && !namedArgument.Value.IsNull)
                {
                    yieldTypes = ExtractTypeArray(namedArgument.Value);
                }
                else if (namedArgument.Key.Equals("Send", StringComparison.Ordinal) && !namedArgument.Value.IsNull)
                {
                    sendTypes = ExtractTypeArray(namedArgument.Value);
                }
            }
        }

        return (new Microsoft.Agents.AI.Workflows.Generators.Models.EquatableArray<string>(yieldTypes), new Microsoft.Agents.AI.Workflows.Generators.Models.EquatableArray<string>(sendTypes));
    }

    /// <summary>
    /// Converts a TypedConstant array (from attribute argument) to fully-qualified type name strings.
    /// </summary>
    /// <remarks>
    /// Results are sorted to ensure consistent ordering for incremental generator caching.
    /// </remarks>
    private static ImmutableArray<string> ExtractTypeArray(TypedConstant typedConstant)
    {
        if (typedConstant.Kind != TypedConstantKind.Array)
        {
            return ImmutableArray<string>.Empty;
        }

        HashSet<string> uniqueTypes = new(StringComparer.Ordinal);
        foreach (TypedConstant value in typedConstant.Values)
        {
            if (value.Value is INamedTypeSymbol typeSymbol)
            {
                uniqueTypes.Add(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }

        return ToSortedImmutableArray(uniqueTypes);
    }

    /// <summary>
    /// Collects types from [SendsMessage] or [YieldsOutput] attributes applied to the class.
    /// </summary>
    /// <remarks>
    /// Results are sorted to ensure consistent ordering for incremental generator caching,
    /// since GetAttributes() order is not guaranteed across partial class declarations.
    /// </remarks>
    /// <example>
    /// [SendsMessage(typeof(Request))]
    /// [YieldsOutput(typeof(Response))]
    /// public partial class MyExecutor : Executor { }
    /// </example>
    private static Microsoft.Agents.AI.Workflows.Generators.Models.EquatableArray<string> GetClassLevelTypes(INamedTypeSymbol classSymbol, string attributeName)
    {
        HashSet<string> uniqueTypes = new(StringComparer.Ordinal);

        foreach (AttributeData attr in classSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == attributeName &&
                attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is INamedTypeSymbol typeSymbol)
            {
                uniqueTypes.Add(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }

        return new Microsoft.Agents.AI.Workflows.Generators.Models.EquatableArray<string>(ToSortedImmutableArray(uniqueTypes));
    }

    /// <summary>
    /// Builds the chain of containing types for nested classes, outermost first.
    /// </summary>
    /// <example>
    /// For class Outer.Middle.Inner.MyExecutor, returns "Outer.Middle.Inner"
    /// </example>
    private static string GetContainingTypeChain(INamedTypeSymbol classSymbol)
    {
        List<string> chain = new();
        INamedTypeSymbol? current = classSymbol.ContainingType;

        while (current != null)
        {
            chain.Add(current.Name);
            current = current.ContainingType;
        }

        chain.Reverse();
        return string.Join(".", chain);
    }

    /// <summary>
    /// Returns the generic type parameter clause (e.g., "&lt;T, U&gt;") for generic classes, or null for non-generic.
    /// </summary>
    private static string? GetGenericParameters(INamedTypeSymbol classSymbol)
    {
        if (!classSymbol.IsGenericType)
        {
            return null;
        }

        string parameters = string.Join(", ", classSymbol.TypeParameters.Select(p => p.Name));
        return $"<{parameters}>";
    }

    private static ImmutableArray<string> ToSortedImmutableArray(HashSet<string> values)
    {
        if (values.Count == 0)
        {
            return ImmutableArray<string>.Empty;
        }

        string[] orderedValues = new string[values.Count];
        values.CopyTo(orderedValues);
        Array.Sort(orderedValues, StringComparer.Ordinal);
        return ImmutableArray.Create(orderedValues);
    }
}
