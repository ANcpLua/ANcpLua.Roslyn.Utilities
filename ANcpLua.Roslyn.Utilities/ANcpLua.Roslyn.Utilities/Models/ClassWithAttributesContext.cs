using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     Provides context information for a class declaration along with its applied attributes.
/// </summary>
/// <param name="SemanticModel">
///     The <see cref="Microsoft.CodeAnalysis.SemanticModel" /> that provides semantic information
///     about the class and its containing compilation.
/// </param>
/// <param name="Attributes">
///     An immutable array of <see cref="AttributeData" /> representing all attributes
///     applied to the class declaration.
/// </param>
/// <param name="ClassSyntax">
///     The <see cref="ClassDeclarationSyntax" /> node representing the class declaration
///     in the syntax tree.
/// </param>
/// <param name="ClassSymbol">
///     The <see cref="INamedTypeSymbol" /> representing the semantic symbol for the class.
/// </param>
/// <remarks>
///     <para>
///         This record struct aggregates syntax and semantic information about a class declaration,
///         making it convenient for source generator pipelines that need to inspect class attributes.
///     </para>
///     <para>
///         <b>WARNING:</b> This type contains Roslyn symbols that use reference equality.
///         Do NOT store this in incremental generator pipelines as it will break caching.
///     </para>
///     <para>
///         Use this only in the initial <c>Select</c>/<c>SelectMany</c> transform, then extract
///         the data you need into equatable types (strings, primitives, <see cref="LocationInfo" />, etc.).
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <see cref="SemanticModel" /> - Use for resolving symbols and type information
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="Attributes" /> - Contains all <see cref="AttributeData" /> applied to the class
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="ClassSyntax" /> - The syntax node for location and text span information
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="ClassSymbol" /> - The type symbol for semantic analysis
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="LocationInfo" />
/// <seealso cref="EquatableArray{T}" />
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
