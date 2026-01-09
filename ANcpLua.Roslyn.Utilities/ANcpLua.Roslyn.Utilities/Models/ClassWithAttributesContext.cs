using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     Context for a class declaration with its attributes.
/// </summary>
/// <param name="SemanticModel">The semantic model.</param>
/// <param name="Attributes">The attributes on the class.</param>
/// <param name="ClassSyntax">The class declaration syntax.</param>
/// <param name="ClassSymbol">The named type symbol for the class.</param>
/// <remarks>
///     <para>
///         <b>WARNING:</b> This type contains Roslyn symbols that use reference equality.
///         Do NOT store this in incremental generator pipelines as it will break caching.
///     </para>
///     <para>
///         Use this only in the initial <c>Select</c>/<c>SelectMany</c> transform, then extract
///         the data you need into equatable types (strings, primitives, <see cref="LocationInfo" />, etc.).
///     </para>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
readonly record struct ClassWithAttributesContext(
    SemanticModel SemanticModel,
    ImmutableArray<AttributeData> Attributes,
    ClassDeclarationSyntax ClassSyntax,
    INamedTypeSymbol ClassSymbol);
