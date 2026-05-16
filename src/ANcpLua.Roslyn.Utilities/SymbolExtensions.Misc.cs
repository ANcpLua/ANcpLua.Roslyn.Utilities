using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class SymbolExtensions
{
    private const string GeneratedFileSuffix = ".g.cs";

    /// <summary>
    ///     Checks if a symbol is declared in a top-level statement.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    ///     <c>true</c> if the symbol is declared in a compilation unit (top-level statements);
    ///     <c>false</c> if it has no declaring syntax references, is declared elsewhere,
    ///     or is in a generated file (ending with <c>.g.cs</c>).
    /// </returns>
    public static bool IsTopLevelStatement(this ISymbol symbol, CancellationToken cancellationToken)
    {
        if (symbol.DeclaringSyntaxReferences.Length is 0)
            return false;

        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
            if (!IsTopLevelOrGeneratedSyntax(syntaxReference, cancellationToken))
                return false;

        return true;
    }

    private static bool IsTopLevelOrGeneratedSyntax(SyntaxReference reference, CancellationToken cancellationToken)
    {
        var syntax = reference.GetSyntax(cancellationToken);

        // A non-compilation-unit syntax in a generator-emitted file is ignored
        // (this lets us treat tooling-generated partials as compatible with top-level).
        return syntax.IsKind(SyntaxKind.CompilationUnit)
               || syntax.SyntaxTree.FilePath.EndsWith(GeneratedFileSuffix, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Gets the type associated with a symbol, depending on the symbol kind.
    /// </summary>
    /// <param name="symbol">The symbol to get the type from.</param>
    /// <returns>
    ///     The associated <see cref="ITypeSymbol" /> for parameter/field/property/local/method symbols;
    ///     otherwise, <c>null</c>.
    /// </returns>
    public static ITypeSymbol? GetSymbolType(this ISymbol symbol)
    {
        return symbol switch
        {
            IParameterSymbol parameter => parameter.Type,
            IFieldSymbol field => field.Type,
            IPropertySymbol { GetMethod: not null } property => property.Type,
            ILocalSymbol local => local.Type,
            IMethodSymbol method => method.ReturnType,
            _ => null
        };
    }

    /// <summary>
    ///     Gets the containing namespace of a symbol as a string.
    /// </summary>
    /// <param name="symbol">The symbol to get the namespace from.</param>
    /// <returns>
    ///     The fully qualified namespace name, or an empty string if the symbol
    ///     is in the global namespace.
    /// </returns>
    public static string GetNamespaceName(this ISymbol symbol)
    {
        var ns = symbol.ContainingNamespace;
        return ns.IsGlobalNamespace ? string.Empty : ns.ToDisplayString();
    }

    /// <summary>
    ///     Gets the overridden member for a method, property, or event.
    /// </summary>
    /// <param name="symbol">The symbol to get the overridden member from.</param>
    /// <returns>
    ///     The overridden symbol for method/property/event; <c>null</c> otherwise.
    /// </returns>
    public static ISymbol? GetOverriddenMember(this ISymbol? symbol)
    {
        return symbol switch
        {
            IMethodSymbol method => method.OverriddenMethod,
            IPropertySymbol property => property.OverriddenProperty,
            IEventSymbol @event => @event.OverriddenEvent,
            _ => null
        };
    }
}
