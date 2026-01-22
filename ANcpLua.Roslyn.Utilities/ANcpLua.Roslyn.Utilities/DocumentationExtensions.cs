// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for retrieving and processing documentation comments from Roslyn symbols.
/// </summary>
/// <remarks>
///     <para>
///         This class provides functionality to retrieve XML documentation comments from symbols,
///         with support for expanding <c>inheritdoc</c> elements to include inherited documentation.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Supports the <c>inheritdoc</c> element with optional <c>cref</c> and <c>path</c> attributes.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Automatically resolves documentation from base types, implemented interfaces, and overridden members.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Handles type parameter reference rewriting when inheriting documentation from generic types.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Prevents infinite recursion when processing circular <c>inheritdoc</c> references.
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="ISymbol.GetDocumentationCommentXml" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class DocumentationExtensions
{
    /// <summary>
    ///     Gets the documentation comment for a symbol, optionally expanding <c>inheritdoc</c> elements.
    /// </summary>
    /// <param name="symbol">The symbol to retrieve documentation for.</param>
    /// <param name="compilation">
    ///     The compilation containing the symbol. Used to resolve <c>cref</c> references in <c>inheritdoc</c> elements.
    /// </param>
    /// <param name="preferredCulture">
    ///     The preferred culture for localized documentation. Pass <c>null</c> to use the default culture.
    /// </param>
    /// <param name="expandIncludes">
    ///     <c>true</c> to expand <c>include</c> elements that reference external documentation files;
    ///     otherwise, <c>false</c>.
    /// </param>
    /// <param name="expandInheritdoc">
    ///     <c>true</c> to expand <c>inheritdoc</c> elements by resolving and inlining inherited documentation;
    ///     otherwise, <c>false</c>.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    ///     The XML documentation comment string for the symbol. Returns an empty string if no documentation is found.
    ///     When <paramref name="expandInheritdoc" /> is <c>true</c>, any <c>inheritdoc</c> elements are replaced
    ///     with the actual inherited documentation content.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         When <paramref name="expandInheritdoc" /> is enabled, the method automatically synthesizes
    ///         an <c>inheritdoc</c> element for symbols that are eligible but lack documentation:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Override members (methods, properties, events)</description>
    ///         </item>
    ///         <item>
    ///             <description>Explicit interface implementations</description>
    ///         </item>
    ///         <item>
    ///             <description>Implicit interface implementations</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         The method handles the following <c>inheritdoc</c> scenarios:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <c>&lt;inheritdoc/&gt;</c> - Inherits from the most relevant base member (override target,
    ///                 interface implementation, or base type).
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <c>&lt;inheritdoc cref="Member"/&gt;</c> - Inherits from the specified member.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <c>&lt;inheritdoc path="/summary"/&gt;</c> - Inherits only the specified XPath elements.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     // Get documentation with inheritdoc expansion
    ///     var docs = method.GetDocumentationComment(
    ///         compilation,
    ///         expandInheritdoc: true);
    ///     </code>
    /// </example>
    public static string GetDocumentationComment(
        this ISymbol symbol,
        Compilation compilation,
        CultureInfo? preferredCulture = null,
        bool expandIncludes = false,
        bool expandInheritdoc = false,
        CancellationToken cancellationToken = default) =>
        GetDocumentationComment(symbol, null, compilation, preferredCulture, expandIncludes,
            expandInheritdoc, cancellationToken);

    /// <summary>
    ///     Gets the plain text summary from a symbol's documentation, with whitespace normalized.
    /// </summary>
    public static string? GetSummaryText(this ISymbol symbol, Compilation compilation, CancellationToken ct = default)
    {
        var xml = symbol.GetDocumentationComment(compilation, expandInheritdoc: true, cancellationToken: ct);
        if (string.IsNullOrEmpty(xml)) return null;

        try
        {
            var element = XElement.Parse(xml);
            return element.Element("summary")?.Value.NormalizeWhitespace();
        }
        catch { return null; }
    }

    /// <summary>
    ///     Gets the plain text remarks from a symbol's documentation, with whitespace normalized.
    /// </summary>
    public static string? GetRemarksText(this ISymbol symbol, Compilation compilation, CancellationToken ct = default)
    {
        var xml = symbol.GetDocumentationComment(compilation, expandInheritdoc: true, cancellationToken: ct);
        if (string.IsNullOrEmpty(xml)) return null;

        try
        {
            var element = XElement.Parse(xml);
            return element.Element("remarks")?.Value.NormalizeWhitespace();
        }
        catch { return null; }
    }

    private static string GetDocumentationComment(
        ISymbol symbol,
        HashSet<ISymbol>? visitedSymbols,
        Compilation compilation,
        CultureInfo? preferredCulture,
        bool expandIncludes,
        bool expandInheritdoc,
        CancellationToken cancellationToken)
    {
        var xmlText = symbol.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);

        if (!expandInheritdoc)
            return xmlText ?? string.Empty;

        if (string.IsNullOrEmpty(xmlText))
        {
            if (IsEligibleForAutomaticInheritdoc(symbol))
                xmlText = "<doc><inheritdoc/></doc>";
            else
                return string.Empty;
        }

        try
        {
            var element = XElement.Parse(xmlText, LoadOptions.PreserveWhitespace);
            element.ReplaceNodes(RewriteMany(symbol, visitedSymbols, compilation, element.Nodes().ToArray(),
                cancellationToken));
            xmlText = element.ToString(SaveOptions.DisableFormatting);
        }
        catch (XmlException)
        {
            // Malformed documentation comments - not actionable
        }

        return xmlText ?? string.Empty;

        static bool IsEligibleForAutomaticInheritdoc(ISymbol symbol)
        {
            if (symbol.IsOverride)
                return true;

            if (symbol.ContainingType is null)
                return false;

            return symbol.Kind is SymbolKind.Method or SymbolKind.Property or SymbolKind.Event
                   && symbol.ExplicitOrImplicitInterfaceImplementations().Any();
        }
    }

    private static XNode[] RewriteInheritdocElements(
        ISymbol symbol,
        HashSet<ISymbol>? visitedSymbols,
        Compilation compilation,
        XNode node,
        CancellationToken cancellationToken)
    {
        if (node is XElement element && ElementNameIs(element, DocumentationXmlNames.InheritdocElementName))
        {
            var rewritten = RewriteInheritdocElement(symbol, visitedSymbols, compilation, element, cancellationToken);
            if (rewritten is not null)
                return rewritten;
        }

        if (node is not XContainer container)
            return [Copy(node, false)];

        var oldNodes = container.Nodes();
        container = Copy(container, false);

        var rewrittenNodes = RewriteMany(symbol, visitedSymbols, compilation, oldNodes.ToArray(), cancellationToken);
        container.ReplaceNodes(rewrittenNodes);

        return [container];
    }

    private static object[] RewriteMany(
        ISymbol symbol,
        HashSet<ISymbol>? visitedSymbols,
        Compilation compilation,
        IEnumerable<XNode> nodes,
        CancellationToken cancellationToken)
    {
        var result = new List<XNode>();
        foreach (var child in nodes)
            result.AddRange(RewriteInheritdocElements(symbol, visitedSymbols, compilation, child, cancellationToken));

        return [.. result];
    }

    private static XNode[]? RewriteInheritdocElement(
        ISymbol memberSymbol,
        HashSet<ISymbol>? visitedSymbols,
        Compilation compilation,
        XElement element,
        CancellationToken cancellationToken)
    {
        var crefAttribute = element.Attribute(XName.Get(DocumentationXmlNames.CrefAttributeName));
        var pathAttribute = element.Attribute(XName.Get(DocumentationXmlNames.PathAttributeName));

        var candidate = GetCandidateSymbol(memberSymbol);

        var symbol = crefAttribute switch
        {
            null when candidate is null => null,
            null => candidate,
            _ => DocumentationCommentId.GetFirstSymbolForDeclarationId(crefAttribute.Value, compilation)
        };

        if (symbol is null)
            return null;

        visitedSymbols ??= [];
        if (!visitedSymbols.Add(symbol))
            return null; // Prevent recursion

        try
        {
            var inheritedDocumentation = GetDocumentationComment(symbol, visitedSymbols, compilation,
                null, true, true, cancellationToken);

            if (string.IsNullOrEmpty(inheritedDocumentation))
                return [];

            var document = XDocument.Parse(inheritedDocumentation, LoadOptions.PreserveWhitespace);
            var xpathValue = pathAttribute?.Value is { Length: > 0 } path
                ? NormalizePath(path)
                : element.Parent is { } parent
                    ? BuildXPathForElement(parent)
                    : null;

            if (xpathValue is null)
                return [];

            RewriteTypeParameterReferences(document, symbol);

            return TrySelectNodes(document, xpathValue) ?? [];
        }
        catch (XmlException)
        {
            return [];
        }
        finally
        {
            visitedSymbols.Remove(symbol);
        }

        static ISymbol? GetCandidateSymbol(ISymbol memberSymbol)
        {
            if (memberSymbol.ExplicitInterfaceImplementations().Any())
                return memberSymbol.ExplicitInterfaceImplementations().First();

            if (memberSymbol.IsOverride)
                return memberSymbol.GetOverriddenMember();

            return memberSymbol switch
            {
                IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor } methodSymbol =>
                    memberSymbol.ContainingType.BaseType?.Constructors
                        .FirstOrDefault(c => IsSameSignature(methodSymbol, c)),
                IMethodSymbol methodSymbol => methodSymbol.ExplicitOrImplicitInterfaceImplementations()
                    .FirstOrDefault(),
                INamedTypeSymbol { TypeKind: TypeKind.Class } typeSymbol => typeSymbol.BaseType,
                INamedTypeSymbol { TypeKind: TypeKind.Interface } typeSymbol => typeSymbol.Interfaces.FirstOrDefault(),
                _ => memberSymbol.ExplicitOrImplicitInterfaceImplementations().FirstOrDefault()
            };
        }

        static bool IsSameSignature(IMethodSymbol left, IMethodSymbol right)
        {
            if (left.Parameters.Length != right.Parameters.Length || left.IsStatic != right.IsStatic)
                return false;

            if (!SymbolEqualityComparer.Default.Equals(left.ReturnType, right.ReturnType))
                return false;

            for (var i = 0; i < left.Parameters.Length; i++)
                if (!SymbolEqualityComparer.Default.Equals(left.Parameters[i].Type, right.Parameters[i].Type))
                    return false;

            return true;
        }

        static string NormalizePath(string path) => path.StartsWith("/", StringComparison.Ordinal) ? "/*" + path : path;

        static string BuildXPathForElement(XElement element)
        {
            if (ElementNameIs(element, "member") || ElementNameIs(element, "doc"))
                return "/*/node()[not(self::overloads)]";

            var path = "/node()[not(self::overloads)]";
            for (var current = element; current is not null; current = current.Parent)
            {
                var currentName = current.Name.ToString();
                if (ElementNameIs(current, "member") || ElementNameIs(current, "doc"))
                    currentName = "*";

                path = "/" + currentName + path;
            }

            return path;
        }
    }

    private static void RewriteTypeParameterReferences(XDocument document, ISymbol symbol)
    {
        var typeParameterRefs = document
            .Descendants(DocumentationXmlNames.TypeParameterReferenceElementName)
            .ToImmutableArray();

        foreach (var typeParameterRef in typeParameterRefs)
        {
            if (typeParameterRef.Attribute(DocumentationXmlNames.NameAttributeName) is not { } typeParamName)
                continue;

            var index = symbol.OriginalDefinition.GetAllTypeParameters()
                .IndexOf(p => p.Name == typeParamName.Value);

            if (index < 0)
                continue;

            var typeArgs = symbol.GetAllTypeArguments();
            if (index >= typeArgs.Length)
                continue;

            var docId = typeArgs[index].GetDocumentationCommentId();
            if (docId is null || docId.StartsWith("!", StringComparison.Ordinal))
                continue;

            var replacement = new XElement(DocumentationXmlNames.SeeElementName);
            replacement.SetAttributeValue(DocumentationXmlNames.CrefAttributeName, docId);
            typeParameterRef.ReplaceWith(replacement);
        }
    }

    private static TNode Copy<TNode>(TNode node, bool copyAttributeAnnotations) where TNode : XNode
    {
        XNode copy;

        if (node.NodeType is XmlNodeType.Document)
            copy = new XDocument((XDocument)(object)node);
        else
        {
            XContainer temp = new XElement("temp");
            temp.Add(node);
            copy = temp.LastNode ?? throw new InvalidOperationException("Failed to copy XML node - LastNode was null after Add");
            temp.RemoveNodes();
        }

        Debug.Assert(copy != node);
        Debug.Assert(copy.Parent is null);

        CopyAnnotations(node, copy);

        if (copyAttributeAnnotations && node.NodeType is XmlNodeType.Element)
        {
            var sourceElement = (XElement)(object)node;
            var targetElement = (XElement)copy;

            using var sourceAttributes = sourceElement.Attributes().GetEnumerator();
            using var targetAttributes = targetElement.Attributes().GetEnumerator();

            while (sourceAttributes.MoveNext() && targetAttributes.MoveNext())
            {
                Debug.Assert(sourceAttributes.Current.Name == targetAttributes.Current.Name);
                CopyAnnotations(sourceAttributes.Current, targetAttributes.Current);
            }
        }

        return (TNode)copy;
    }

    private static void CopyAnnotations(XObject source, XObject target)
    {
        foreach (var annotation in source.Annotations<object>())
            target.AddAnnotation(annotation);
    }

    private static XNode[]? TrySelectNodes(XNode node, string xpath)
    {
        try
        {
            var xpathResult = (IEnumerable)node.XPathEvaluate(xpath);
            return xpathResult?.Cast<XNode>().ToArray();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (XPathException)
        {
            return null;
        }
    }

    private static bool ElementNameIs(XElement element, string name) =>
        string.IsNullOrEmpty(element.Name.NamespaceName) &&
        DocumentationXmlNames.ElementEquals(element.Name.LocalName, name);
}

/// <summary>
///     Internal XML element and attribute names used for documentation comment processing.
/// </summary>
/// <remarks>
///     <para>
///         This file-scoped class contains constant strings for XML element and attribute names
///         commonly found in C# XML documentation comments. These constants are used when
///         parsing and rewriting documentation comments, particularly for <c>inheritdoc</c> expansion.
///     </para>
///     <list type="bullet">
///         <item>
///             <description><c>inheritdoc</c> - Inherits documentation from a base or interface member.</description>
///         </item>
///         <item>
///             <description><c>typeparamref</c> - References a type parameter by name.</description>
///         </item>
///         <item>
///             <description><c>see</c> - Creates a hyperlink reference to another type or member.</description>
///         </item>
///     </list>
/// </remarks>
file static class DocumentationXmlNames
{
    /// <summary>
    ///     The element name for <c>inheritdoc</c> elements.
    /// </summary>
    public const string InheritdocElementName = "inheritdoc";

    /// <summary>
    ///     The element name for <c>typeparamref</c> elements.
    /// </summary>
    public const string TypeParameterReferenceElementName = "typeparamref";

    /// <summary>
    ///     The element name for <c>see</c> elements.
    /// </summary>
    public const string SeeElementName = "see";

    /// <summary>
    ///     The attribute name for <c>cref</c> attributes.
    /// </summary>
    public const string CrefAttributeName = "cref";

    /// <summary>
    ///     The attribute name for <c>name</c> attributes.
    /// </summary>
    public const string NameAttributeName = "name";

    /// <summary>
    ///     The attribute name for <c>path</c> attributes.
    /// </summary>
    public const string PathAttributeName = "path";

    /// <summary>
    ///     Compares two element names for equality using case-insensitive comparison.
    /// </summary>
    /// <param name="name1">The first element name to compare.</param>
    /// <param name="name2">The second element name to compare.</param>
    /// <returns>
    ///     <c>true</c> if the element names are equal (ignoring case); otherwise, <c>false</c>.
    /// </returns>
    public static bool ElementEquals(string name1, string name2) => string.Equals(name1, name2, StringComparison.OrdinalIgnoreCase);
}
