using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides extension methods for Roslyn syntax types.
/// </summary>
/// <remarks>
///     <para>
///         This class contains utility methods for working with C# syntax nodes, including
///         expression analysis, modifier checking, and location extraction.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Extract method and identifier names from expressions</description>
///         </item>
///         <item>
///             <description>Check for specific modifiers on member declarations</description>
///         </item>
///         <item>
///             <description>Detect partial types and primary constructor types</description>
///         </item>
///         <item>
///             <description>Retrieve name token locations for diagnostics</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="InvocationExpressionSyntax" />
/// <seealso cref="MemberDeclarationSyntax" />
/// <seealso cref="TypeDeclarationSyntax" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class SyntaxExtensions
{
    /// <summary>
    ///     Gets the method name from an invocation expression.
    /// </summary>
    /// <param name="invocation">The invocation expression to extract the method name from.</param>
    /// <returns>
    ///     The method name as a string, or <c>null</c> if the method name cannot be determined
    ///     from the expression syntax.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method handles both member access expressions (e.g., <c>obj.Method()</c>)
    ///         and simple identifier expressions (e.g., <c>Method()</c>).
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>For member access expressions, returns the name portion (e.g., "Method" from "obj.Method")</description>
    ///         </item>
    ///         <item>
    ///             <description>For identifier expressions, returns the identifier text directly</description>
    ///         </item>
    ///         <item>
    ///             <description>For other expression types, returns <c>null</c></description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="GetIdentifierName" />
    public static string? GetMethodName(this InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
    }

    /// <summary>
    ///     Gets the identifier name from an expression.
    /// </summary>
    /// <param name="expression">The expression to extract the identifier name from.</param>
    /// <returns>
    ///     The identifier name as a string, or <c>null</c> if the identifier cannot be determined
    ///     from the expression syntax.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method extracts identifier text from both simple identifier expressions
    ///         and member access expressions.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>For identifier expressions (e.g., <c>foo</c>), returns the identifier text</description>
    ///         </item>
    ///         <item>
    ///             <description>For member access expressions (e.g., <c>obj.Property</c>), returns the member name</description>
    ///         </item>
    ///         <item>
    ///             <description>For other expression types, returns <c>null</c></description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="GetMethodName" />
    public static string? GetIdentifierName(this ExpressionSyntax expression)
    {
        return expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => null
        };
    }

    /// <summary>
    ///     Checks if a member declaration has a specific modifier.
    /// </summary>
    /// <param name="member">The member declaration to check.</param>
    /// <param name="kind">The <see cref="SyntaxKind" /> of the modifier to look for.</param>
    /// <returns>
    ///     <c>true</c> if the member has the specified modifier; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Common modifier kinds include <see cref="SyntaxKind.PublicKeyword" />,
    ///         <see cref="SyntaxKind.PrivateKeyword" />, <see cref="SyntaxKind.StaticKeyword" />,
    ///         <see cref="SyntaxKind.AsyncKeyword" />, and <see cref="SyntaxKind.PartialKeyword" />.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Access modifiers: <see cref="SyntaxKind.PublicKeyword" />,
    ///                 <see cref="SyntaxKind.PrivateKeyword" />, <see cref="SyntaxKind.ProtectedKeyword" />,
    ///                 <see cref="SyntaxKind.InternalKeyword" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Member modifiers: <see cref="SyntaxKind.StaticKeyword" />,
    ///                 <see cref="SyntaxKind.ReadOnlyKeyword" />, <see cref="SyntaxKind.ConstKeyword" />,
    ///                 <see cref="SyntaxKind.VolatileKeyword" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Inheritance modifiers: <see cref="SyntaxKind.AbstractKeyword" />,
    ///                 <see cref="SyntaxKind.VirtualKeyword" />, <see cref="SyntaxKind.OverrideKeyword" />,
    ///                 <see cref="SyntaxKind.SealedKeyword" />, <see cref="SyntaxKind.NewKeyword" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Async/unsafe: <see cref="SyntaxKind.AsyncKeyword" />, <see cref="SyntaxKind.UnsafeKeyword" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Type modifiers: <see cref="SyntaxKind.PartialKeyword" />,
    ///                 <see cref="SyntaxKind.FileKeyword" />
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Check for async methods in an analyzer
    /// if (methodDeclaration.HasModifier(SyntaxKind.AsyncKeyword))
    /// {
    ///     // Analyze async-specific patterns
    /// }
    ///
    /// // Check for static members
    /// if (memberDeclaration.HasModifier(SyntaxKind.StaticKeyword))
    /// {
    ///     // Apply static member rules
    /// }
    ///
    /// // Check for virtual methods that could be overridden
    /// if (methodDeclaration.HasModifier(SyntaxKind.VirtualKeyword) ||
    ///     methodDeclaration.HasModifier(SyntaxKind.AbstractKeyword))
    /// {
    ///     // Method can be overridden in derived classes
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="IsPartial" />
    public static bool HasModifier(this MemberDeclarationSyntax member, SyntaxKind kind) => member.Modifiers.Any(kind);

    /// <summary>
    ///     Checks if a type declaration has the partial modifier.
    /// </summary>
    /// <param name="type">The type declaration to check.</param>
    /// <returns>
    ///     <c>true</c> if the type has the <c>partial</c> modifier; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Partial types are commonly required for source generators to augment existing types
    ///         with generated members.
    ///     </para>
    /// </remarks>
    /// <seealso cref="HasModifier" />
    /// <seealso cref="IsPrimaryConstructorType" />
    public static bool IsPartial(this TypeDeclarationSyntax type) => type.Modifiers.Any(SyntaxKind.PartialKeyword);

    /// <summary>
    ///     Checks if a type declaration is a primary constructor type.
    /// </summary>
    /// <param name="type">The type declaration to check.</param>
    /// <returns>
    ///     <c>true</c> if the type is a class, struct, or record with a primary constructor
    ///     (parameter list); otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Primary constructors were introduced in C# 12 for classes and structs,
    ///         and have been available for records since C# 9. A primary constructor is
    ///         declared by adding a parameter list directly after the type name.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Applies to <see cref="ClassDeclarationSyntax" />, <see cref="StructDeclarationSyntax" />, and
    ///                 <see cref="RecordDeclarationSyntax" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Returns <c>true</c> only if the type has a non-null
    ///                 <see cref="TypeDeclarationSyntax.ParameterList" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>Does not apply to interfaces or other type declarations</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="IsPartial" />
    public static bool IsPrimaryConstructorType(this TypeDeclarationSyntax type) =>
        type is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax
        && type.ParameterList is not null;

    /// <summary>
    ///     Gets the name token location for a type declaration.
    /// </summary>
    /// <param name="type">The type declaration to get the name location from.</param>
    /// <returns>
    ///     The <see cref="Location" /> of the type's identifier token.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is useful for reporting diagnostics that should point to the type name
    ///         rather than the entire type declaration.
    ///     </para>
    /// </remarks>
    /// <seealso cref="GetNameLocation(MethodDeclarationSyntax)" />
    public static Location GetNameLocation(this TypeDeclarationSyntax type) => type.Identifier.GetLocation();

    /// <summary>
    ///     Gets the name token location for a method declaration.
    /// </summary>
    /// <param name="method">The method declaration to get the name location from.</param>
    /// <returns>
    ///     The <see cref="Location" /> of the method's identifier token.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is useful for reporting diagnostics that should point to the method name
    ///         rather than the entire method declaration.
    ///     </para>
    /// </remarks>
    /// <seealso cref="GetNameLocation(TypeDeclarationSyntax)" />
    public static Location GetNameLocation(this MethodDeclarationSyntax method) => method.Identifier.GetLocation();

    // ========== Code Fix Helpers ==========

    /// <summary>
    ///     Creates an invocation expression for calling an extension method.
    /// </summary>
    /// <param name="receiver">The expression that will become the receiver (first argument to the extension method).</param>
    /// <param name="methodName">The name of the extension method to call.</param>
    /// <param name="arguments">The arguments to pass to the extension method (excluding the receiver).</param>
    /// <returns>
    ///     An <see cref="InvocationExpressionSyntax" /> representing <c>receiver.methodName(arguments)</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This helper is commonly used in code fix providers that replace verbose patterns
    ///         with cleaner extension method calls. The receiver's trivia is stripped to produce
    ///         clean output.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Transform: SymbolEqualityComparer.Default.Equals(a, b)
    /// // Into: a.IsEqualTo(b)
    /// var newInvocation = leftArg.CreateExtensionMethodCall("IsEqualTo", rightArg);
    /// </code>
    /// </example>
    /// <seealso cref="CreateExtensionMethodCall(ExpressionSyntax, string)" />
    public static InvocationExpressionSyntax CreateExtensionMethodCall(
        this ExpressionSyntax receiver,
        string methodName,
        params ExpressionSyntax[] arguments)
    {
        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            receiver.WithoutTrivia(),
            SyntaxFactory.IdentifierName(methodName));

        if (arguments.Length is 0)
            return SyntaxFactory.InvocationExpression(memberAccess);

        var argumentList = SyntaxFactory.ArgumentList(
            SyntaxFactory.SeparatedList(
                arguments.Select(static arg => SyntaxFactory.Argument(arg.WithoutTrivia()))));

        return SyntaxFactory.InvocationExpression(memberAccess, argumentList);
    }

    /// <summary>
    ///     Creates a parameterless invocation expression for calling an extension method.
    /// </summary>
    /// <param name="receiver">The expression that will become the receiver.</param>
    /// <param name="methodName">The name of the extension method to call.</param>
    /// <returns>
    ///     An <see cref="InvocationExpressionSyntax" /> representing <c>receiver.methodName()</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// // Transform: collection ?? Array.Empty&lt;T&gt;()
    /// // Into: collection.OrEmpty()
    /// var newInvocation = collection.CreateExtensionMethodCall("OrEmpty");
    /// </code>
    /// </example>
    /// <seealso cref="CreateExtensionMethodCall(ExpressionSyntax, string, ExpressionSyntax[])" />
    public static InvocationExpressionSyntax CreateExtensionMethodCall(
        this ExpressionSyntax receiver,
        string methodName) =>
        receiver.CreateExtensionMethodCall(methodName, []);

    /// <summary>
    ///     Adds the <c>static</c> modifier to an anonymous function (lambda or delegate).
    /// </summary>
    /// <param name="lambda">The anonymous function to add the modifier to.</param>
    /// <returns>
    ///     A new anonymous function with the <c>static</c> modifier added.
    ///     If the function type is not recognized, returns the original lambda unchanged.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method handles all types of anonymous functions:
    ///     </para>
    ///     <list type="bullet">
    ///         <item><description><see cref="SimpleLambdaExpressionSyntax" />: <c>x => x + 1</c></description></item>
    ///         <item><description><see cref="ParenthesizedLambdaExpressionSyntax" />: <c>(x, y) => x + y</c></description></item>
    ///         <item><description><see cref="AnonymousMethodExpressionSyntax" />: <c>delegate(int x) { return x; }</c></description></item>
    ///     </list>
    ///     <para>
    ///         The method preserves existing modifiers and adds <c>static</c> at the beginning.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // In a code fix provider:
    /// var newLambda = originalLambda.WithStaticModifier();
    /// // Transforms: x => x + 1
    /// // Into: static x => x + 1
    /// </code>
    /// </example>
    public static AnonymousFunctionExpressionSyntax WithStaticModifier(this AnonymousFunctionExpressionSyntax lambda)
    {
        var staticKeyword = SyntaxFactory.Token(SyntaxKind.StaticKeyword).WithTrailingTrivia(SyntaxFactory.Space);

        return lambda switch
        {
            SimpleLambdaExpressionSyntax simple => simple.AddModifiers(staticKeyword),
            ParenthesizedLambdaExpressionSyntax paren => paren.AddModifiers(staticKeyword),
            AnonymousMethodExpressionSyntax anon => anon.AddModifiers(staticKeyword),
            _ => lambda
        };
    }

    /// <summary>
    ///     Adds the <c>static</c> modifier to a local function.
    /// </summary>
    /// <param name="localFunction">The local function to add the modifier to.</param>
    /// <returns>
    ///     A new local function with the <c>static</c> modifier added.
    /// </returns>
    /// <example>
    ///     <code>
    /// // In a code fix provider:
    /// var newFunction = originalFunction.WithStaticModifier();
    /// // Transforms: int Add(int x, int y) => x + y;
    /// // Into: static int Add(int x, int y) => x + y;
    /// </code>
    /// </example>
    public static LocalFunctionStatementSyntax WithStaticModifier(this LocalFunctionStatementSyntax localFunction)
    {
        var staticKeyword = SyntaxFactory.Token(SyntaxKind.StaticKeyword).WithTrailingTrivia(SyntaxFactory.Space);
        return localFunction.AddModifiers(staticKeyword);
    }

    /// <summary>
    ///     Checks if an anonymous function has the <c>static</c> modifier.
    /// </summary>
    /// <param name="lambda">The anonymous function to check.</param>
    /// <returns>
    ///     <c>true</c> if the function has the <c>static</c> modifier; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsStatic(this AnonymousFunctionExpressionSyntax lambda) =>
        lambda.Modifiers.Any(SyntaxKind.StaticKeyword);

    /// <summary>
    ///     Checks if a local function has the <c>static</c> modifier.
    /// </summary>
    /// <param name="localFunction">The local function to check.</param>
    /// <returns>
    ///     <c>true</c> if the function has the <c>static</c> modifier; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsStatic(this LocalFunctionStatementSyntax localFunction) =>
        localFunction.Modifiers.Any(SyntaxKind.StaticKeyword);

    // ========== Code Generation Helpers ==========

    /// <summary>
    ///     Converts a type name string to its fully qualified <c>global::</c> form for use in generated code.
    /// </summary>
    /// <param name="typeName">The type name to qualify (e.g., <c>"string"</c>, <c>"Task&lt;Order&gt;"</c>, <c>"int[]"</c>).</param>
    /// <param name="typeParameterNames">
    ///     Optional list of type parameter names (e.g., <c>T</c>, <c>TResult</c>) that should be returned as-is
    ///     without a <c>global::</c> prefix. Pass <c>null</c> when there are no type parameters.
    /// </param>
    /// <returns>
    ///     The fully qualified type name. C# keyword aliases are mapped to their BCL equivalents
    ///     (e.g., <c>"string"</c> becomes <c>"global::System.String"</c>), <c>"void"</c> is returned as-is,
    ///     and all other types are prefixed with <c>global::</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method recursively handles composite type names:
    ///     </para>
    ///     <list type="bullet">
    ///         <item><description>Arrays: <c>"int[]"</c> becomes <c>"global::System.Int32[]"</c></description></item>
    ///         <item><description>Nullable reference types: <c>"Order?"</c> becomes <c>"global::Order?"</c></description></item>
    ///         <item><description>Generic types: <c>"Task&lt;string&gt;"</c> becomes <c>"global::System.Threading.Tasks.Task&lt;global::System.String&gt;"</c></description></item>
    ///         <item><description>Nested generics: <c>"Dictionary&lt;string, List&lt;int&gt;&gt;"</c> is handled correctly</description></item>
    ///         <item><description>Type parameters: <c>"T"</c> is returned as <c>"T"</c> when present in <paramref name="typeParameterNames" /></description></item>
    ///     </list>
    ///     <para>
    ///         <b>C# keyword mappings:</b> <c>string</c> -> <c>System.String</c>, <c>int</c> -> <c>System.Int32</c>,
    ///         <c>bool</c> -> <c>System.Boolean</c>, <c>object</c> -> <c>System.Object</c>, etc.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Simple types
    /// "string".ToGlobalTypeName()           // "global::System.String"
    /// "int".ToGlobalTypeName()              // "global::System.Int32"
    /// "void".ToGlobalTypeName()             // "void"
    /// "MyApp.Order".ToGlobalTypeName()      // "global::MyApp.Order"
    ///
    /// // Composite types
    /// "int[]".ToGlobalTypeName()            // "global::System.Int32[]"
    /// "Order?".ToGlobalTypeName()           // "global::Order?"
    /// "Task&lt;string&gt;".ToGlobalTypeName()     // "global::Task&lt;global::System.String&gt;"
    ///
    /// // With type parameters
    /// var typeParams = new[] { "T", "TResult" };
    /// "Task&lt;TResult&gt;".ToGlobalTypeName(typeParams)  // "global::Task&lt;TResult&gt;"
    /// "T".ToGlobalTypeName(typeParams)                    // "T"
    /// </code>
    /// </example>
    public static string ToGlobalTypeName(this string typeName, IReadOnlyList<string>? typeParameterNames = null)
    {
        if (string.IsNullOrEmpty(typeName))
            return typeName;

        // Check if this is a type parameter (T, TResult, etc.)
        if (typeParameterNames is not null)
        {
            for (var i = 0; i < typeParameterNames.Count; i++)
            {
                if (string.Equals(typeName, typeParameterNames[i], StringComparison.Ordinal))
                    return typeName;
            }
        }

        // Handle array types (e.g., "string[]", "int[][]")
        if (typeName.EndsWithOrdinal("[]"))
        {
            var elementType = typeName.Substring(0, typeName.Length - 2);
            return elementType.ToGlobalTypeName(typeParameterNames) + "[]";
        }

        // Handle nullable reference types (trailing ?)
        if (typeName.EndsWithOrdinal("?"))
        {
            var innerType = typeName.Substring(0, typeName.Length - 1);
            return innerType.ToGlobalTypeName(typeParameterNames) + "?";
        }

        // Handle generic types: Task<Order> or Dictionary<string, Order>
        var genericStart = typeName.IndexOf('<');
        if (genericStart > 0 && typeName.EndsWithOrdinal(">"))
        {
            var baseTypeName = typeName.Substring(0, genericStart);
            var argsContent = typeName.Substring(genericStart + 1, typeName.Length - genericStart - 2);

            var args = ParseGenericArguments(argsContent);
            var qualifiedArgs = args.Select(a => a.ToGlobalTypeName(typeParameterNames));

            return $"{baseTypeName.ToGlobalTypeName(typeParameterNames)}<{string.Join(", ", qualifiedArgs)}>";
        }

        // Map C# keyword aliases to their BCL names
        var mapped = MapPrimitiveKeyword(typeName);

        return mapped == "void" ? "void" : $"global::{mapped}";
    }

    /// <summary>
    ///     Maps a C# primitive keyword to its BCL type name.
    ///     Returns the original type name if it is not a primitive keyword.
    /// </summary>
    private static string MapPrimitiveKeyword(string typeName)
    {
        return typeName switch
        {
            "string" => "System.String",
            "int" => "System.Int32",
            "long" => "System.Int64",
            "short" => "System.Int16",
            "byte" => "System.Byte",
            "sbyte" => "System.SByte",
            "uint" => "System.UInt32",
            "ulong" => "System.UInt64",
            "ushort" => "System.UInt16",
            "float" => "System.Single",
            "double" => "System.Double",
            "decimal" => "System.Decimal",
            "bool" => "System.Boolean",
            "char" => "System.Char",
            "object" => "System.Object",
            "void" => "void",
            _ => typeName
        };
    }

    /// <summary>
    ///     Determines whether a type name is a C# primitive keyword alias.
    /// </summary>
    /// <param name="typeName">The type name to check.</param>
    /// <returns><c>true</c> if the type name is a C# primitive keyword; otherwise, <c>false</c>.</returns>
    public static bool IsPrimitiveKeyword(this string typeName)
    {
        return typeName is "string" or "bool" or "byte" or "sbyte" or "short" or "ushort"
            or "int" or "uint" or "long" or "ulong" or "float" or "double"
            or "decimal" or "char" or "object" or "void";
    }

    /// <summary>
    ///     Parses generic type arguments from a comma-separated string, correctly handling nested generics.
    /// </summary>
    /// <param name="argsContent">
    ///     The content between the outermost angle brackets (e.g., <c>"string, List&lt;int&gt;"</c>
    ///     from <c>"Dictionary&lt;string, List&lt;int&gt;&gt;"</c>).
    /// </param>
    /// <returns>A list of individual type argument strings, trimmed of whitespace.</returns>
    private static IEnumerable<string> ParseGenericArguments(string argsContent)
    {
        var args = new List<string>();
        var depth = 0;
        var start = 0;

        for (var i = 0; i < argsContent.Length; i++)
        {
            switch (argsContent[i])
            {
                case '<':
                    depth++;
                    break;
                case '>':
                    depth--;
                    break;
                case ',' when depth is 0:
                    args.Add(argsContent.Substring(start, i - start).Trim());
                    start = i + 1;
                    break;
            }
        }

        if (start < argsContent.Length)
            args.Add(argsContent.Substring(start).Trim());

        return args;
    }
}