// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for retrieving and processing documentation comments from Roslyn symbols.
/// </summary>
public static class DocumentationExtensions
{
    /// <summary>
    ///     Gets the documentation comment for a symbol, optionally expanding inheritdoc elements.
    /// </summary>
    public static string GetDocumentationComment(
        this ISymbol symbol,
        Compilation compilation,
        CultureInfo? preferredCulture = null,
        bool expandIncludes = false,
        bool expandInheritdoc = false,
        CancellationToken cancellationToken = default) =>
        GetDocumentationComment(symbol, visitedSymbols: null, compilation, preferredCulture, expandIncludes,
            expandInheritdoc, cancellationToken);

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
            return [Copy(node, copyAttributeAnnotations: false)];

        var oldNodes = container.Nodes();
        container = Copy(container, copyAttributeAnnotations: false);

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

        if (crefAttribute is null && candidate is null)
            return null;

        ISymbol symbol;
        if (crefAttribute is null)
        {
            symbol = candidate!; // candidate is guaranteed non-null here due to check above
        }
        else
        {
            var resolved = DocumentationCommentId.GetFirstSymbolForDeclarationId(crefAttribute.Value, compilation);
            if (resolved is null)
                return null;
            symbol = resolved;
        }

        visitedSymbols ??= [];
        if (!visitedSymbols.Add(symbol))
            return null; // Prevent recursion

        try
        {
            var inheritedDocumentation = GetDocumentationComment(symbol, visitedSymbols, compilation,
                preferredCulture: null, expandIncludes: true, expandInheritdoc: true, cancellationToken);

            if (string.IsNullOrEmpty(inheritedDocumentation))
                return [];

            var document = XDocument.Parse(inheritedDocumentation, LoadOptions.PreserveWhitespace);
            var xpathValue = string.IsNullOrEmpty(pathAttribute?.Value)
                ? BuildXPathForElement(element.Parent!)
                : NormalizePath(pathAttribute!.Value);

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
            {
                if (!SymbolEqualityComparer.Default.Equals(left.Parameters[i].Type, right.Parameters[i].Type))
                    return false;
            }

            return true;
        }

        static string NormalizePath(string path) =>
            path.StartsWith("/", StringComparison.Ordinal) ? "/*" + path : path;

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
        {
            copy = new XDocument((XDocument)(object)node);
        }
        else
        {
            XContainer temp = new XElement("temp");
            temp.Add(node);
            copy = temp.LastNode!;
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
file static class DocumentationXmlNames
{
    public const string InheritdocElementName = "inheritdoc";
    public const string TypeParameterReferenceElementName = "typeparamref";
    public const string SeeElementName = "see";
    public const string CrefAttributeName = "cref";
    public const string NameAttributeName = "name";
    public const string PathAttributeName = "path";

    public static bool ElementEquals(string name1, string name2) =>
        string.Equals(name1, name2, StringComparison.OrdinalIgnoreCase);
}
