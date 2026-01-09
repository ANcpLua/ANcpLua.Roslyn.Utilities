using System.Collections.Immutable;
using ANcpLua.Roslyn.Utilities.Models;
using ANcpLua.Roslyn.Utilities.Patterns;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
/// Declarative validation for symbols. Reads like intent, accumulates violations.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
sealed class SemanticGuard<T> where T : ISymbol
{
    private readonly T _symbol;
    private readonly List<DiagnosticInfo> _violations = [];

    private SemanticGuard(T symbol)
    {
        _symbol = symbol;
    }

    internal static SemanticGuard<T> Create(T symbol) => new(symbol);

    public T Symbol => _symbol;
    public bool IsValid => _violations.Count == 0;

    public EquatableArray<DiagnosticInfo> Violations =>
        _violations.Count == 0
            ? default
            : _violations.ToImmutableArray().AsEquatableArray();

    public SemanticGuard<T> MustMatch(SymbolPattern<T> pattern, DiagnosticInfo onFail)
    {
        if (!pattern.Matches(_symbol))
            _violations.Add(onFail);
        return this;
    }

    public SemanticGuard<T> MustMatch(SymbolPattern<T> pattern, Func<T, DiagnosticInfo> onFail)
    {
        if (!pattern.Matches(_symbol))
            _violations.Add(onFail(_symbol));
        return this;
    }

    public SemanticGuard<T> Must(Func<T, bool> predicate, DiagnosticInfo onFail)
    {
        if (!predicate(_symbol))
            _violations.Add(onFail);
        return this;
    }

    public SemanticGuard<T> Must(Func<T, bool> predicate, Func<T, DiagnosticInfo> onFail)
    {
        if (!predicate(_symbol))
            _violations.Add(onFail(_symbol));
        return this;
    }

    public SemanticGuard<T> MustNot(Func<T, bool> predicate, DiagnosticInfo onFail)
    {
        if (predicate(_symbol))
            _violations.Add(onFail);
        return this;
    }

    public SemanticGuard<T> MustBePublic(DiagnosticInfo onFail)
    {
        if (_symbol.DeclaredAccessibility != Accessibility.Public)
            _violations.Add(onFail);
        return this;
    }

    public SemanticGuard<T> MustBeStatic(DiagnosticInfo onFail)
    {
        if (!_symbol.IsStatic)
            _violations.Add(onFail);
        return this;
    }

    public SemanticGuard<T> MustNotBeStatic(DiagnosticInfo onFail)
    {
        if (_symbol.IsStatic)
            _violations.Add(onFail);
        return this;
    }

    public SemanticGuard<T> MustHaveAttribute(ITypeSymbol attributeType, DiagnosticInfo onFail)
    {
        if (!_symbol.HasAttribute(attributeType))
            _violations.Add(onFail);
        return this;
    }

    public SemanticGuard<T> MustHaveAttribute(string fullyQualifiedName, DiagnosticInfo onFail)
    {
        if (!_symbol.HasAttribute(fullyQualifiedName))
            _violations.Add(onFail);
        return this;
    }

    public SemanticGuard<T> MustNotHaveAttribute(ITypeSymbol attributeType, DiagnosticInfo onFail)
    {
        if (_symbol.HasAttribute(attributeType))
            _violations.Add(onFail);
        return this;
    }

    public SemanticGuard<T> MustBeVisibleOutsideAssembly(DiagnosticInfo onFail)
    {
        if (!_symbol.IsVisibleOutsideOfAssembly())
            _violations.Add(onFail);
        return this;
    }

    public DiagnosticFlow<T> ToFlow() =>
        IsValid
            ? DiagnosticFlow.Ok(_symbol)
            : DiagnosticFlow.Fail<T>(Violations);
}

/// <summary>
/// SemanticGuard extensions for specific symbol types.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class SemanticGuardExtensions
{
    // Method-specific guards
    public static SemanticGuard<IMethodSymbol> MustBeAsync(this SemanticGuard<IMethodSymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(m => m.IsAsync, onFail);

    public static SemanticGuard<IMethodSymbol> MustNotBeAsync(this SemanticGuard<IMethodSymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(m => !m.IsAsync, onFail);

    public static SemanticGuard<IMethodSymbol> MustReturnVoid(this SemanticGuard<IMethodSymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(m => m.ReturnsVoid, onFail);

    public static SemanticGuard<IMethodSymbol> MustNotReturnVoid(this SemanticGuard<IMethodSymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(m => !m.ReturnsVoid, onFail);

    public static SemanticGuard<IMethodSymbol> MustReturnTask(this SemanticGuard<IMethodSymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(m => m.ReturnType.IsTaskType(), onFail);

    public static SemanticGuard<IMethodSymbol> MustHaveNoParameters(this SemanticGuard<IMethodSymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(m => m.Parameters.IsEmpty, onFail);

    public static SemanticGuard<IMethodSymbol> MustHaveParameterCount(this SemanticGuard<IMethodSymbol> guard, int count, DiagnosticInfo onFail) =>
        guard.Must(m => m.Parameters.Length == count, onFail);

    public static SemanticGuard<IMethodSymbol> MustHaveCancellationToken(this SemanticGuard<IMethodSymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(m => HasCancellationToken(m), onFail);

    private static bool HasCancellationToken(IMethodSymbol method)
    {
        foreach (var p in method.Parameters)
        {
            if (p.Type.ToDisplayString() == "System.Threading.CancellationToken")
                return true;
        }
        return false;
    }

    public static SemanticGuard<IMethodSymbol> MustBeExtensionMethod(this SemanticGuard<IMethodSymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(m => m.IsExtensionMethod, onFail);

    public static SemanticGuard<IMethodSymbol> MustNotBeExtensionMethod(this SemanticGuard<IMethodSymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(m => !m.IsExtensionMethod, onFail);

    // Type-specific guards
    public static SemanticGuard<INamedTypeSymbol> MustHaveMultipleDeclarations(this SemanticGuard<INamedTypeSymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(t => t.DeclaringSyntaxReferences.Length > 1, onFail);

    public static SemanticGuard<INamedTypeSymbol> MustBeClass(this SemanticGuard<INamedTypeSymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(t => t.TypeKind == TypeKind.Class, onFail);

    public static SemanticGuard<INamedTypeSymbol> MustBeStruct(this SemanticGuard<INamedTypeSymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(t => t.TypeKind == TypeKind.Struct, onFail);

    public static SemanticGuard<INamedTypeSymbol> MustBeInterface(this SemanticGuard<INamedTypeSymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(t => t.TypeKind == TypeKind.Interface, onFail);

    public static SemanticGuard<INamedTypeSymbol> MustBeRecord(this SemanticGuard<INamedTypeSymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(t => t.IsRecord, onFail);

    public static SemanticGuard<INamedTypeSymbol> MustNotBeAbstract(this SemanticGuard<INamedTypeSymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(t => !t.IsAbstract, onFail);

    public static SemanticGuard<INamedTypeSymbol> MustNotBeGeneric(this SemanticGuard<INamedTypeSymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(t => !t.IsGenericType, onFail);

    public static SemanticGuard<INamedTypeSymbol> MustImplement(this SemanticGuard<INamedTypeSymbol> guard, ITypeSymbol interfaceType, DiagnosticInfo onFail) =>
        guard.Must(t => t.Implements(interfaceType), onFail);

    public static SemanticGuard<INamedTypeSymbol> MustInheritFrom(this SemanticGuard<INamedTypeSymbol> guard, ITypeSymbol baseType, DiagnosticInfo onFail) =>
        guard.Must(t => t.InheritsFrom(baseType), onFail);

    public static SemanticGuard<INamedTypeSymbol> MustBeDisposable(this SemanticGuard<INamedTypeSymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(t => IsDisposable(t), onFail);

    private static bool IsDisposable(INamedTypeSymbol type)
    {
        foreach (var i in type.AllInterfaces)
        {
            var name = i.ToDisplayString();
            if (name is "System.IDisposable" or "System.IAsyncDisposable")
                return true;
        }
        return false;
    }

    // Property-specific guards
    public static SemanticGuard<IPropertySymbol> MustHaveGetter(this SemanticGuard<IPropertySymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(p => p.GetMethod is not null, onFail);

    public static SemanticGuard<IPropertySymbol> MustHaveSetter(this SemanticGuard<IPropertySymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(p => p.SetMethod is not null, onFail);

    public static SemanticGuard<IPropertySymbol> MustBeReadOnly(this SemanticGuard<IPropertySymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(p => p.IsReadOnly, onFail);

    public static SemanticGuard<IPropertySymbol> MustBeRequired(this SemanticGuard<IPropertySymbol> guard, DiagnosticInfo onFail) =>
        guard.Must(p => p.IsRequired, onFail);
}

/// <summary>
/// Static entry point for SemanticGuard.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class SemanticGuard
{
    public static SemanticGuard<T> For<T>(T symbol) where T : ISymbol => SemanticGuard<T>.Create(symbol);

    public static SemanticGuard<IMethodSymbol> ForMethod(IMethodSymbol method) => SemanticGuard<IMethodSymbol>.Create(method);
    public static SemanticGuard<INamedTypeSymbol> ForType(INamedTypeSymbol type) => SemanticGuard<INamedTypeSymbol>.Create(type);
    public static SemanticGuard<IPropertySymbol> ForProperty(IPropertySymbol property) => SemanticGuard<IPropertySymbol>.Create(property);
    public static SemanticGuard<IFieldSymbol> ForField(IFieldSymbol field) => SemanticGuard<IFieldSymbol>.Create(field);
    public static SemanticGuard<IParameterSymbol> ForParameter(IParameterSymbol parameter) => SemanticGuard<IParameterSymbol>.Create(parameter);
}
