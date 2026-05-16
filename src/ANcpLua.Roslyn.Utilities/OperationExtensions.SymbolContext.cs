using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharpParseOptions = Microsoft.CodeAnalysis.CSharp.CSharpParseOptions;
using IMethodSymbol = Microsoft.CodeAnalysis.IMethodSymbol;
using INamedTypeSymbol = Microsoft.CodeAnalysis.INamedTypeSymbol;
using IOperation = Microsoft.CodeAnalysis.IOperation;
using LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class OperationExtensions
{
    /// <summary>
    ///     Gets the method symbol that contains this operation.
    /// </summary>
    /// <param name="operation">The operation whose containing method to find.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    ///     The <see cref="IMethodSymbol" /> for the containing method, local function, accessor,
    ///     or lambda expression; or <c>null</c> if the operation is not within a method-like construct
    ///     or the semantic model is unavailable.
    /// </returns>
    /// <seealso cref="GetContainingType" />
    /// <seealso cref="GetContainingBlock" />
    public static IMethodSymbol? GetContainingMethod(this IOperation operation, CancellationToken cancellationToken)
    {
        var model = operation.SemanticModel;
        if (model is null)
            return null;

        foreach (var syntax in operation.Syntax.AncestorsAndSelf())
        {
            var method = syntax switch
            {
                MethodDeclarationSyntax m => model.GetDeclaredSymbol(m, cancellationToken),
                LocalFunctionStatementSyntax lf => model.GetDeclaredSymbol(lf, cancellationToken),
                AccessorDeclarationSyntax acc => model.GetDeclaredSymbol(acc, cancellationToken),
                LambdaExpressionSyntax lam => model.GetSymbolInfo(lam, cancellationToken).Symbol as IMethodSymbol,
                _ => null
            };

            if (method is not null)
                return method;
        }

        return null;
    }

    /// <summary>
    ///     Gets the named type symbol that contains this operation.
    /// </summary>
    /// <param name="operation">The operation whose containing type to find.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    ///     The <see cref="INamedTypeSymbol" /> for the containing type declaration,
    ///     or <c>null</c> if the operation is not within a type or the semantic model is unavailable.
    /// </returns>
    /// <seealso cref="GetContainingMethod" />
    public static INamedTypeSymbol? GetContainingType(this IOperation operation, CancellationToken cancellationToken)
    {
        if (operation.SemanticModel is null)
            return null;

        foreach (var syntax in operation.Syntax.AncestorsAndSelf())
            if (syntax is TypeDeclarationSyntax typeDecl)
                return operation.SemanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

        return null;
    }

    /// <summary>
    ///     Gets the C# language version of the source containing this operation.
    /// </summary>
    /// <param name="operation">The operation whose language version to determine.</param>
    /// <returns>
    ///     The <see cref="LanguageVersion" /> of the source file, or <see cref="LanguageVersion.Default" />
    ///     if the syntax tree options are not C# parse options.
    /// </returns>
    public static LanguageVersion GetCSharpLanguageVersion(this IOperation operation)
    {
        return operation.Syntax.SyntaxTree.Options is CSharpParseOptions options
            ? options.LanguageVersion
            : LanguageVersion.Default;
    }
}
