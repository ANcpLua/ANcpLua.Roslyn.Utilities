using ANcpLua.Analyzers.ExtensibleEnumMirror.Models;

namespace ANcpLua.Analyzers.ExtensibleEnumMirror.Extraction;

internal static class MirrorExtractor
{
    public static DiagnosticFlow<MirrorModel> Extract(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.TargetSymbol is not INamedTypeSymbol marker ||
            context.TargetNode is not TypeDeclarationSyntax markerSyntax ||
            markerSyntax is not (ClassDeclarationSyntax or StructDeclarationSyntax))
            return DiagnosticFlow.Fail<MirrorModel>(DiagnosticInfo.Create(
                DiagnosticDescriptors.MarkerMustBePartial,
                context.TargetNode,
                context.TargetSymbol?.Name ?? "<unknown>"));

        if (!markerSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            return DiagnosticFlow.Fail<MirrorModel>(DiagnosticInfo.Create(
                DiagnosticDescriptors.MarkerMustBePartial,
                markerSyntax,
                marker.Name));

        var attribute = context.Attributes.Length > 0 ? context.Attributes[0] : null;
        if (attribute is null ||
            attribute.ConstructorArguments.IsEmpty ||
            attribute.ConstructorArguments[0].Value is not INamedTypeSymbol target)
            return DiagnosticFlow.Fail<MirrorModel>(DiagnosticInfo.Create(
                DiagnosticDescriptors.MissingTargetType,
                markerSyntax));

        if (!IsExtensibleEnumStruct(target))
            return DiagnosticFlow.Fail<MirrorModel>(DiagnosticInfo.Create(
                DiagnosticDescriptors.InvalidTarget,
                markerSyntax,
                target.ToDisplayString()));

        var knownMembers = target.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.IsStatic
                        && !p.IsWriteOnly
                        && p.GetMethod is not null
                        && p.DeclaredAccessibility == Accessibility.Public
                        && SymbolEqualityComparer.Default.Equals(p.Type, target))
            .Select(p => p.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray()
            .ToEquatableArray();

        var keyword = markerSyntax is StructDeclarationSyntax ? "struct" : "class";
        var ns = marker.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : marker.ContainingNamespace.ToDisplayString();

        return DiagnosticFlow.Ok(new MirrorModel(
            ns,
            marker.Name,
            keyword,
            target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            target.Name,
            knownMembers));
    }

    private static bool IsExtensibleEnumStruct(INamedTypeSymbol target)
    {
        if (!target.IsValueType) return false;
        if (target.IsGenericType) return false;
        if (!target.IsReadOnly) return false;

        foreach (var iface in target.AllInterfaces)
        {
            if (!iface.IsGenericType) continue;
            if (iface.Name != "IEquatable") continue;
            if (iface.ContainingNamespace?.ToDisplayString() != "System") continue;
            if (iface.TypeArguments.Length != 1) continue;
            if (SymbolEqualityComparer.Default.Equals(iface.TypeArguments[0], target))
                return true;
        }

        return false;
    }
}
