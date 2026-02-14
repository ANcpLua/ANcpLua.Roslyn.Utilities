using ANcpLua.Roslyn.Utilities.Models;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides declarative validation for Roslyn symbols using a fluent API.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="SemanticGuard{T}" /> enables building validation rules that read like intent
///         while accumulating violations for diagnostic reporting.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Chain multiple validation rules fluently</description>
///         </item>
///         <item>
///             <description>Accumulates all violations rather than short-circuiting</description>
///         </item>
///         <item>
///             <description>Converts to <see cref="DiagnosticFlow{T}" /> for railway-oriented programming</description>
///         </item>
///     </list>
/// </remarks>
/// <typeparam name="T">The type of symbol being validated. Must implement <see cref="ISymbol" />.</typeparam>
/// <example>
///     <code>
/// SemanticGuard.ForMethod(method)
///     .MustBeAsync(asyncRequired)
///     .MustReturnTask(taskRequired)
///     .MustHaveCancellationToken(ctRequired)
///     .ToFlow();
/// </code>
/// </example>
/// <seealso cref="SemanticGuard" />
/// <seealso cref="SemanticGuardExtensions" />
/// <seealso cref="DiagnosticFlow{T}" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class SemanticGuard<T> where T : ISymbol
{
    private readonly List<DiagnosticInfo> _violations = [];

    private SemanticGuard(T symbol) => Symbol = symbol;

    internal static SemanticGuard<T> Create(T symbol) => new(symbol);

    /// <summary>
    ///     Gets the symbol being validated.
    /// </summary>
    public T Symbol { get; }

    /// <summary>
    ///     Gets a value indicating whether all validation rules have passed.
    /// </summary>
    /// <returns><c>true</c> if no violations have been recorded; otherwise, <c>false</c>.</returns>
    public bool IsValid => _violations.Count is 0;

    /// <summary>
    ///     Gets the collection of diagnostic violations accumulated during validation.
    /// </summary>
    /// <returns>
    ///     An <see cref="EquatableArray{T}" /> containing all recorded violations,
    ///     or <c>default</c> if no violations occurred.
    /// </returns>
    public EquatableArray<DiagnosticInfo> Violations =>
        _violations.Count is 0
            ? default
            : _violations.ToImmutableArray().AsEquatableArray();

    /// <summary>
    ///     Validates that the symbol satisfies the specified predicate.
    /// </summary>
    /// <param name="predicate">A function that returns <c>true</c> if the symbol is valid.</param>
    /// <param name="onFail">The diagnostic to record if the predicate returns <c>false</c>.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public SemanticGuard<T> Must(Func<T, bool> predicate, DiagnosticInfo onFail)
    {
        if (!predicate(Symbol))
            _violations.Add(onFail);
        return this;
    }

    /// <summary>
    ///     Validates that the symbol satisfies the specified predicate, with a dynamic diagnostic.
    /// </summary>
    /// <param name="predicate">A function that returns <c>true</c> if the symbol is valid.</param>
    /// <param name="onFail">A function that creates the diagnostic based on the symbol if the predicate returns <c>false</c>.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public SemanticGuard<T> Must(Func<T, bool> predicate, Func<T, DiagnosticInfo> onFail)
    {
        if (!predicate(Symbol))
            _violations.Add(onFail(Symbol));
        return this;
    }

    /// <summary>
    ///     Validates that the symbol does not satisfy the specified predicate.
    /// </summary>
    /// <param name="predicate">A function that returns <c>true</c> for an invalid condition.</param>
    /// <param name="onFail">The diagnostic to record if the predicate returns <c>true</c>.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public SemanticGuard<T> MustNot(Func<T, bool> predicate, DiagnosticInfo onFail)
    {
        if (predicate(Symbol))
            _violations.Add(onFail);
        return this;
    }

    /// <summary>
    ///     Validates that the symbol has public accessibility.
    /// </summary>
    /// <param name="onFail">The diagnostic to record if the symbol is not public.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public SemanticGuard<T> MustBePublic(DiagnosticInfo onFail)
    {
        if (Symbol.DeclaredAccessibility != Accessibility.Public)
            _violations.Add(onFail);
        return this;
    }

    /// <summary>
    ///     Validates that the symbol is static.
    /// </summary>
    /// <param name="onFail">The diagnostic to record if the symbol is not static.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public SemanticGuard<T> MustBeStatic(DiagnosticInfo onFail)
    {
        if (!Symbol.IsStatic)
            _violations.Add(onFail);
        return this;
    }

    /// <summary>
    ///     Validates that the symbol is not static.
    /// </summary>
    /// <param name="onFail">The diagnostic to record if the symbol is static.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public SemanticGuard<T> MustNotBeStatic(DiagnosticInfo onFail)
    {
        if (Symbol.IsStatic)
            _violations.Add(onFail);
        return this;
    }

    /// <summary>
    ///     Validates that the symbol has the specified attribute.
    /// </summary>
    /// <param name="attributeType">The <see cref="ITypeSymbol" /> representing the attribute type.</param>
    /// <param name="onFail">The diagnostic to record if the attribute is not present.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public SemanticGuard<T> MustHaveAttribute(ITypeSymbol attributeType, DiagnosticInfo onFail)
    {
        if (!Symbol.HasAttribute(attributeType))
            _violations.Add(onFail);
        return this;
    }

    /// <summary>
    ///     Validates that the symbol has the specified attribute by its fully qualified name.
    /// </summary>
    /// <param name="fullyQualifiedName">The fully qualified metadata name of the attribute (e.g., "System.ObsoleteAttribute").</param>
    /// <param name="onFail">The diagnostic to record if the attribute is not present.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public SemanticGuard<T> MustHaveAttribute(string fullyQualifiedName, DiagnosticInfo onFail)
    {
        if (!Symbol.HasAttribute(fullyQualifiedName))
            _violations.Add(onFail);
        return this;
    }

    /// <summary>
    ///     Validates that the symbol does not have the specified attribute.
    /// </summary>
    /// <param name="attributeType">The <see cref="ITypeSymbol" /> representing the attribute type.</param>
    /// <param name="onFail">The diagnostic to record if the attribute is present.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public SemanticGuard<T> MustNotHaveAttribute(ITypeSymbol attributeType, DiagnosticInfo onFail)
    {
        if (Symbol.HasAttribute(attributeType))
            _violations.Add(onFail);
        return this;
    }

    /// <summary>
    ///     Validates that the symbol is visible outside the assembly.
    /// </summary>
    /// <param name="onFail">The diagnostic to record if the symbol is not externally visible.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    /// <seealso cref="SymbolExtensions.IsVisibleOutsideOfAssembly" />
    public SemanticGuard<T> MustBeVisibleOutsideAssembly(DiagnosticInfo onFail)
    {
        if (!Symbol.IsVisibleOutsideOfAssembly())
            _violations.Add(onFail);
        return this;
    }

    /// <summary>
    ///     Converts the validation result to a <see cref="DiagnosticFlow{T}" />.
    /// </summary>
    /// <returns>
    ///     A successful <see cref="DiagnosticFlow{T}" /> containing the symbol if <see cref="IsValid" /> is <c>true</c>;
    ///     otherwise, a failed flow containing all accumulated <see cref="Violations" />.
    /// </returns>
    /// <seealso cref="DiagnosticFlow{T}" />
    public DiagnosticFlow<T> ToFlow() =>
        IsValid
            ? DiagnosticFlow.Ok(Symbol)
            : DiagnosticFlow.Fail<T>(Violations);
}

/// <summary>
///     Provides extension methods for <see cref="SemanticGuard{T}" /> with symbol-type-specific validations.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>Method-specific guards for <see cref="IMethodSymbol" /></description>
///         </item>
///         <item>
///             <description>Type-specific guards for <see cref="INamedTypeSymbol" /></description>
///         </item>
///         <item>
///             <description>Property-specific guards for <see cref="IPropertySymbol" /></description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="SemanticGuard{T}" />
/// <seealso cref="SemanticGuard" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class SemanticGuardExtensions
{
    // Method-specific guards

    /// <summary>
    ///     Validates that the method is declared with the <c>async</c> modifier.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the method.</param>
    /// <param name="onFail">The diagnostic to record if the method is not async.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<IMethodSymbol> MustBeAsync(this SemanticGuard<IMethodSymbol> guard,
        DiagnosticInfo onFail)
    {
        return guard.Must(static m => m.IsAsync, onFail);
    }

    /// <summary>
    ///     Validates that the method is not declared with the <c>async</c> modifier.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the method.</param>
    /// <param name="onFail">The diagnostic to record if the method is async.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<IMethodSymbol> MustNotBeAsync(this SemanticGuard<IMethodSymbol> guard,
        DiagnosticInfo onFail)
    {
        return guard.Must(static m => !m.IsAsync, onFail);
    }

    /// <summary>
    ///     Validates that the method returns <c>void</c>.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the method.</param>
    /// <param name="onFail">The diagnostic to record if the method does not return void.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<IMethodSymbol> MustReturnVoid(this SemanticGuard<IMethodSymbol> guard,
        DiagnosticInfo onFail)
    {
        return guard.Must(static m => m.ReturnsVoid, onFail);
    }

    /// <summary>
    ///     Validates that the method does not return <c>void</c>.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the method.</param>
    /// <param name="onFail">The diagnostic to record if the method returns void.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<IMethodSymbol> MustNotReturnVoid(this SemanticGuard<IMethodSymbol> guard,
        DiagnosticInfo onFail)
    {
        return guard.Must(static m => !m.ReturnsVoid, onFail);
    }

    /// <summary>
    ///     Validates that the method returns a Task-like type.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the method.</param>
    /// <param name="onFail">The diagnostic to record if the method does not return a Task-like type.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    /// <seealso cref="TypeSymbolExtensions.IsTaskType" />
    public static SemanticGuard<IMethodSymbol> MustReturnTask(this SemanticGuard<IMethodSymbol> guard,
        DiagnosticInfo onFail)
    {
        return guard.Must(static m => m.ReturnType.IsTaskType(), onFail);
    }

    /// <summary>
    ///     Validates that the method has no parameters.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the method.</param>
    /// <param name="onFail">The diagnostic to record if the method has parameters.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<IMethodSymbol> MustHaveNoParameters(this SemanticGuard<IMethodSymbol> guard,
        DiagnosticInfo onFail)
    {
        return guard.Must(static m => m.Parameters.IsEmpty, onFail);
    }

    /// <summary>
    ///     Validates that the method has the specified number of parameters.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the method.</param>
    /// <param name="count">The required number of parameters.</param>
    /// <param name="onFail">The diagnostic to record if the parameter count does not match.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<IMethodSymbol> MustHaveParameterCount(this SemanticGuard<IMethodSymbol> guard,
        int count, DiagnosticInfo onFail)
    {
        return guard.Must(m => m.Parameters.Length == count, onFail);
    }

    /// <summary>
    ///     Validates that the method has a <see cref="System.Threading.CancellationToken" /> parameter.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the method.</param>
    /// <param name="onFail">The diagnostic to record if no CancellationToken parameter is present.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<IMethodSymbol> MustHaveCancellationToken(this SemanticGuard<IMethodSymbol> guard,
        DiagnosticInfo onFail) =>
        guard.Must(HasCancellationToken, onFail);

    private static bool HasCancellationToken(IMethodSymbol method)
    {
        foreach (var p in method.Parameters)
            if (p.Type.ToDisplayString() == "System.Threading.CancellationToken")
                return true;
        return false;
    }

    /// <summary>
    ///     Validates that the method is an extension method.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the method.</param>
    /// <param name="onFail">The diagnostic to record if the method is not an extension method.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<IMethodSymbol> MustBeExtensionMethod(this SemanticGuard<IMethodSymbol> guard,
        DiagnosticInfo onFail)
    {
        return guard.Must(static m => m.IsExtensionMethod, onFail);
    }

    /// <summary>
    ///     Validates that the method is not an extension method.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the method.</param>
    /// <param name="onFail">The diagnostic to record if the method is an extension method.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<IMethodSymbol> MustNotBeExtensionMethod(this SemanticGuard<IMethodSymbol> guard,
        DiagnosticInfo onFail)
    {
        return guard.Must(static m => !m.IsExtensionMethod, onFail);
    }

    // Type-specific guards

    /// <summary>
    ///     Validates that the type has multiple partial declarations.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the type.</param>
    /// <param name="onFail">The diagnostic to record if the type does not have multiple declarations.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<INamedTypeSymbol> MustHaveMultipleDeclarations(
        this SemanticGuard<INamedTypeSymbol> guard, DiagnosticInfo onFail)
    {
        return guard.Must(static t => t.DeclaringSyntaxReferences.Length > 1, onFail);
    }

    /// <summary>
    ///     Validates that the type is a class.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the type.</param>
    /// <param name="onFail">The diagnostic to record if the type is not a class.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<INamedTypeSymbol> MustBeClass(this SemanticGuard<INamedTypeSymbol> guard,
        DiagnosticInfo onFail)
    {
        return guard.Must(static t => t.TypeKind == TypeKind.Class, onFail);
    }

    /// <summary>
    ///     Validates that the type is a struct.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the type.</param>
    /// <param name="onFail">The diagnostic to record if the type is not a struct.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<INamedTypeSymbol> MustBeStruct(this SemanticGuard<INamedTypeSymbol> guard,
        DiagnosticInfo onFail)
    {
        return guard.Must(static t => t.TypeKind == TypeKind.Struct, onFail);
    }

    /// <summary>
    ///     Validates that the type is an interface.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the type.</param>
    /// <param name="onFail">The diagnostic to record if the type is not an interface.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<INamedTypeSymbol> MustBeInterface(this SemanticGuard<INamedTypeSymbol> guard,
        DiagnosticInfo onFail)
    {
        return guard.Must(static t => t.TypeKind == TypeKind.Interface, onFail);
    }

    /// <summary>
    ///     Validates that the type is a record.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the type.</param>
    /// <param name="onFail">The diagnostic to record if the type is not a record.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<INamedTypeSymbol> MustBeRecord(this SemanticGuard<INamedTypeSymbol> guard,
        DiagnosticInfo onFail)
    {
        return guard.Must(static t => t.IsRecord, onFail);
    }

    /// <summary>
    ///     Validates that the type is not abstract.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the type.</param>
    /// <param name="onFail">The diagnostic to record if the type is abstract.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<INamedTypeSymbol> MustNotBeAbstract(this SemanticGuard<INamedTypeSymbol> guard,
        DiagnosticInfo onFail)
    {
        return guard.Must(static t => !t.IsAbstract, onFail);
    }

    /// <summary>
    ///     Validates that the type is not generic.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the type.</param>
    /// <param name="onFail">The diagnostic to record if the type is generic.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<INamedTypeSymbol> MustNotBeGeneric(this SemanticGuard<INamedTypeSymbol> guard,
        DiagnosticInfo onFail)
    {
        return guard.Must(static t => !t.IsGenericType, onFail);
    }

    /// <summary>
    ///     Validates that the type implements the specified interface.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the type.</param>
    /// <param name="interfaceType">The <see cref="ITypeSymbol" /> representing the interface to check for.</param>
    /// <param name="onFail">The diagnostic to record if the type does not implement the interface.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    /// <seealso cref="TypeSymbolExtensions.Implements" />
    public static SemanticGuard<INamedTypeSymbol> MustImplement(this SemanticGuard<INamedTypeSymbol> guard,
        ITypeSymbol interfaceType, DiagnosticInfo onFail)
    {
        return guard.Must(t => t.Implements(interfaceType), onFail);
    }

    /// <summary>
    ///     Validates that the type inherits from the specified base type.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the type.</param>
    /// <param name="baseType">The <see cref="ITypeSymbol" /> representing the expected base type.</param>
    /// <param name="onFail">The diagnostic to record if the type does not inherit from the base type.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    /// <seealso cref="TypeSymbolExtensions.InheritsFrom" />
    public static SemanticGuard<INamedTypeSymbol> MustInheritFrom(this SemanticGuard<INamedTypeSymbol> guard,
        ITypeSymbol baseType, DiagnosticInfo onFail)
    {
        return guard.Must(t => t.InheritsFrom(baseType), onFail);
    }

    /// <summary>
    ///     Validates that the type implements <see cref="System.IDisposable" /> or <c>IAsyncDisposable</c>.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the type.</param>
    /// <param name="onFail">The diagnostic to record if the type is not disposable.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<INamedTypeSymbol> MustBeDisposable(this SemanticGuard<INamedTypeSymbol> guard,
        DiagnosticInfo onFail) =>
        guard.Must(IsDisposable, onFail);

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

    /// <summary>
    ///     Validates that the property has a getter.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the property.</param>
    /// <param name="onFail">The diagnostic to record if the property does not have a getter.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<IPropertySymbol> MustHaveGetter(this SemanticGuard<IPropertySymbol> guard,
        DiagnosticInfo onFail)
    {
        return guard.Must(static p => p.GetMethod is not null, onFail);
    }

    /// <summary>
    ///     Validates that the property has a setter.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the property.</param>
    /// <param name="onFail">The diagnostic to record if the property does not have a setter.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<IPropertySymbol> MustHaveSetter(this SemanticGuard<IPropertySymbol> guard,
        DiagnosticInfo onFail)
    {
        return guard.Must(static p => p.SetMethod is not null, onFail);
    }

    /// <summary>
    ///     Validates that the property is read-only (has no setter).
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the property.</param>
    /// <param name="onFail">The diagnostic to record if the property is not read-only.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<IPropertySymbol> MustBeReadOnly(this SemanticGuard<IPropertySymbol> guard,
        DiagnosticInfo onFail)
    {
        return guard.Must(static p => p.IsReadOnly, onFail);
    }

    /// <summary>
    ///     Validates that the property is marked with the <c>required</c> modifier.
    /// </summary>
    /// <param name="guard">The <see cref="SemanticGuard{T}" /> for the property.</param>
    /// <param name="onFail">The diagnostic to record if the property is not required.</param>
    /// <returns>The current <see cref="SemanticGuard{T}" /> instance for method chaining.</returns>
    public static SemanticGuard<IPropertySymbol> MustBeRequired(this SemanticGuard<IPropertySymbol> guard,
        DiagnosticInfo onFail)
    {
        return guard.Must(static p => p.IsRequired, onFail);
    }
}

/// <summary>
///     Provides static factory methods for creating <see cref="SemanticGuard{T}" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         Use this class as the entry point for declarative symbol validation.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// SemanticGuard.ForMethod(method)
///     .MustBeAsync(asyncRequired)
///     .MustReturnTask(taskRequired)
///     .ToFlow();
///
/// SemanticGuard.ForType(type)
///     .MustBeClass(classRequired)
///     .MustImplement(interfaceType, implRequired)
///     .ToFlow();
/// </code>
/// </example>
/// <seealso cref="SemanticGuard{T}" />
/// <seealso cref="SemanticGuardExtensions" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class SemanticGuard
{
    /// <summary>
    ///     Creates a new <see cref="SemanticGuard{T}" /> for the specified symbol.
    /// </summary>
    /// <typeparam name="T">The type of symbol to validate. Must implement <see cref="ISymbol" />.</typeparam>
    /// <param name="symbol">The symbol to validate.</param>
    /// <returns>A new <see cref="SemanticGuard{T}" /> instance for the specified symbol.</returns>
    public static SemanticGuard<T> For<T>(T symbol) where T : ISymbol => SemanticGuard<T>.Create(symbol);

    /// <summary>
    ///     Creates a new <see cref="SemanticGuard{T}" /> for the specified method symbol.
    /// </summary>
    /// <param name="method">The method symbol to validate.</param>
    /// <returns>A new <see cref="SemanticGuard{T}" /> instance for the method.</returns>
    /// <seealso cref="SemanticGuardExtensions.MustBeAsync" />
    /// <seealso cref="SemanticGuardExtensions.MustReturnTask" />
    /// <seealso cref="SemanticGuardExtensions.MustHaveCancellationToken" />
    public static SemanticGuard<IMethodSymbol> ForMethod(IMethodSymbol method) => SemanticGuard<IMethodSymbol>.Create(method);

    /// <summary>
    ///     Creates a new <see cref="SemanticGuard{T}" /> for the specified named type symbol.
    /// </summary>
    /// <param name="type">The type symbol to validate.</param>
    /// <returns>A new <see cref="SemanticGuard{T}" /> instance for the type.</returns>
    /// <seealso cref="SemanticGuardExtensions.MustBeClass" />
    /// <seealso cref="SemanticGuardExtensions.MustImplement" />
    /// <seealso cref="SemanticGuardExtensions.MustInheritFrom" />
    public static SemanticGuard<INamedTypeSymbol> ForType(INamedTypeSymbol type) => SemanticGuard<INamedTypeSymbol>.Create(type);

    /// <summary>
    ///     Creates a new <see cref="SemanticGuard{T}" /> for the specified property symbol.
    /// </summary>
    /// <param name="property">The property symbol to validate.</param>
    /// <returns>A new <see cref="SemanticGuard{T}" /> instance for the property.</returns>
    /// <seealso cref="SemanticGuardExtensions.MustHaveGetter" />
    /// <seealso cref="SemanticGuardExtensions.MustHaveSetter" />
    /// <seealso cref="SemanticGuardExtensions.MustBeReadOnly" />
    public static SemanticGuard<IPropertySymbol> ForProperty(IPropertySymbol property) => SemanticGuard<IPropertySymbol>.Create(property);

    /// <summary>
    ///     Creates a new <see cref="SemanticGuard{T}" /> for the specified field symbol.
    /// </summary>
    /// <param name="field">The field symbol to validate.</param>
    /// <returns>A new <see cref="SemanticGuard{T}" /> instance for the field.</returns>
    public static SemanticGuard<IFieldSymbol> ForField(IFieldSymbol field) => SemanticGuard<IFieldSymbol>.Create(field);

    /// <summary>
    ///     Creates a new <see cref="SemanticGuard{T}" /> for the specified parameter symbol.
    /// </summary>
    /// <param name="parameter">The parameter symbol to validate.</param>
    /// <returns>A new <see cref="SemanticGuard{T}" /> instance for the parameter.</returns>
    public static SemanticGuard<IParameterSymbol> ForParameter(IParameterSymbol parameter) => SemanticGuard<IParameterSymbol>.Create(parameter);
}
