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
public readonly record struct ClassWithAttributesContext(
    SemanticModel SemanticModel,
    ImmutableArray<AttributeData> Attributes,
    ClassDeclarationSyntax ClassSyntax,
    INamedTypeSymbol ClassSymbol);