using ANcpLua.Analyzers.DiscriminatedUnion.Models;

namespace ANcpLua.Analyzers.DiscriminatedUnion.Extraction;

internal static class UnionExtractor
{
    public static DiagnosticFlow<UnionModel> Extract(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.TargetSymbol is not INamedTypeSymbol root ||
            context.TargetNode is not RecordDeclarationSyntax rootSyntax)
            return DiagnosticFlow.Fail<UnionModel>(DiagnosticInfo.Create(
                DiagnosticDescriptors.RootMustBePartialRecord,
                context.TargetNode,
                context.TargetSymbol?.Name ?? "<unknown>"));

        if (!rootSyntax.Modifiers.Any(SyntaxKind.PartialKeyword) ||
            rootSyntax.Kind() is not SyntaxKind.RecordDeclaration)
            return DiagnosticFlow.Fail<UnionModel>(DiagnosticInfo.Create(
                DiagnosticDescriptors.RootMustBePartialRecord,
                rootSyntax,
                root.Name));

        if (rootSyntax.ParameterList is { Parameters.Count: > 0 })
            return DiagnosticFlow.Fail<UnionModel>(DiagnosticInfo.Create(
                DiagnosticDescriptors.RootMustNotHavePrimaryCtor,
                rootSyntax,
                root.Name));

        var nonRecordMembers = rootSyntax.Members
            .OfType<TypeDeclarationSyntax>()
            .Where(t => t is not RecordDeclarationSyntax recordMember || recordMember.Kind() is not SyntaxKind.RecordDeclaration)
            .ToArray();

        foreach (var bad in nonRecordMembers)
            return DiagnosticFlow.Fail<UnionModel>(DiagnosticInfo.Create(
                DiagnosticDescriptors.CaseMustBeNestedPartialRecord,
                bad,
                bad.Identifier.Text,
                root.Name));

        var caseSyntaxes = rootSyntax.Members
            .OfType<RecordDeclarationSyntax>()
            .Where(r => r.Kind() is SyntaxKind.RecordDeclaration)
            .ToArray();

        if (caseSyntaxes.Length == 0)
            return DiagnosticFlow.Fail<UnionModel>(DiagnosticInfo.Create(
                DiagnosticDescriptors.RootMustHaveCases,
                rootSyntax,
                root.Name));

        foreach (var caseSyntax in caseSyntaxes)
            if (!caseSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                return DiagnosticFlow.Fail<UnionModel>(DiagnosticInfo.Create(
                    DiagnosticDescriptors.CaseMustBeNestedPartialRecord,
                    caseSyntax,
                    caseSyntax.Identifier.Text,
                    root.Name));

        var cases = caseSyntaxes
            .Select(c => new UnionCase(c.Identifier.Text, ToCamelCase(c.Identifier.Text)))
            .ToArray()
            .ToEquatableArray();

        var ns = root.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : root.ContainingNamespace.ToDisplayString();

        var typeParamList = rootSyntax.TypeParameterList?.ToString() ?? string.Empty;

        return DiagnosticFlow.Ok(new UnionModel(
            ns,
            root.Name,
            typeParamList,
            root.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            cases));
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var first = char.ToLowerInvariant(name[0]);
        var camel = first + name.Substring(1);
        return SyntaxFacts.GetKeywordKind(camel) is SyntaxKind.None &&
               SyntaxFacts.GetContextualKeywordKind(camel) is SyntaxKind.None
            ? camel
            : "@" + camel;
    }
}
