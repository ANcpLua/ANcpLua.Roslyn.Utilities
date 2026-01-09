using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
sealed class PublicApiWalker : SymbolVisitor
{
    private readonly CancellationToken _cancellationToken;
    private readonly HashSet<INamedTypeSymbol> _types;
    private readonly HashSet<IMethodSymbol> _methods;
    private readonly HashSet<IPropertySymbol> _properties;
    private readonly HashSet<IFieldSymbol> _fields;
    private readonly HashSet<IEventSymbol> _events;

    private PublicApiWalker(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _types = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        _methods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        _properties = new HashSet<IPropertySymbol>(SymbolEqualityComparer.Default);
        _fields = new HashSet<IFieldSymbol>(SymbolEqualityComparer.Default);
        _events = new HashSet<IEventSymbol>(SymbolEqualityComparer.Default);
    }

    public ImmutableArray<INamedTypeSymbol> Types => _types.ToImmutableArray();
    public ImmutableArray<IMethodSymbol> Methods => _methods.ToImmutableArray();
    public ImmutableArray<IPropertySymbol> Properties => _properties.ToImmutableArray();
    public ImmutableArray<IFieldSymbol> Fields => _fields.ToImmutableArray();
    public ImmutableArray<IEventSymbol> Events => _events.ToImmutableArray();

    public static PublicApiWalker Walk(IAssemblySymbol assembly, CancellationToken cancellationToken = default)
    {
        var walker = new PublicApiWalker(cancellationToken);
        walker.Visit(assembly);
        return walker;
    }

    public static PublicApiWalker Walk(INamespaceSymbol ns, CancellationToken cancellationToken = default)
    {
        var walker = new PublicApiWalker(cancellationToken);
        walker.Visit(ns);
        return walker;
    }

    public override void VisitAssembly(IAssemblySymbol symbol)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        symbol.GlobalNamespace.Accept(this);
    }

    public override void VisitNamespace(INamespaceSymbol symbol)
    {
        foreach (var member in symbol.GetMembers())
        {
            _cancellationToken.ThrowIfCancellationRequested();
            member.Accept(this);
        }
    }

    public override void VisitNamedType(INamedTypeSymbol symbol)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (!symbol.IsVisibleOutsideOfAssembly() || !_types.Add(symbol))
            return;

        foreach (var nestedType in symbol.GetTypeMembers())
        {
            _cancellationToken.ThrowIfCancellationRequested();
            nestedType.Accept(this);
        }

        foreach (var member in symbol.GetMembers())
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (!member.IsVisibleOutsideOfAssembly())
                continue;

            switch (member)
            {
                case IMethodSymbol method when method.MethodKind is MethodKind.Ordinary or MethodKind.Constructor:
                    _methods.Add(method);
                    break;
                case IPropertySymbol property:
                    _properties.Add(property);
                    break;
                case IFieldSymbol field:
                    _fields.Add(field);
                    break;
                case IEventSymbol @event:
                    _events.Add(@event);
                    break;
            }
        }
    }
}

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class NamespaceExtensions
{
    public static IEnumerable<INamedTypeSymbol> GetAllTypes(this INamespaceSymbol ns)
    {
        foreach (var member in ns.GetMembers())
        {
            if (member is INamedTypeSymbol type)
            {
                yield return type;
                foreach (var nested in GetNestedTypes(type))
                    yield return nested;
            }
            else if (member is INamespaceSymbol nestedNs)
            {
                foreach (var nestedType in nestedNs.GetAllTypes())
                    yield return nestedType;
            }
        }
    }

    public static IEnumerable<INamespaceSymbol> GetAllNamespaces(this INamespaceSymbol ns)
    {
        yield return ns;
        foreach (var member in ns.GetMembers())
        {
            if (member is INamespaceSymbol nestedNs)
            {
                foreach (var descendant in nestedNs.GetAllNamespaces())
                    yield return descendant;
            }
        }
    }

    public static IEnumerable<INamedTypeSymbol> GetTypesRecursive(this IAssemblySymbol assembly) =>
        assembly.GlobalNamespace.GetAllTypes();

    public static IEnumerable<INamedTypeSymbol> GetPublicTypes(this IAssemblySymbol assembly) =>
        assembly.GlobalNamespace.GetAllTypes().Where(t => t.IsVisibleOutsideOfAssembly());

    public static IEnumerable<INamedTypeSymbol> GetPublicTypes(this INamespaceSymbol ns) =>
        ns.GetAllTypes().Where(t => t.IsVisibleOutsideOfAssembly());

    private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
    {
        foreach (var nested in type.GetTypeMembers())
        {
            yield return nested;
            foreach (var deepNested in GetNestedTypes(nested))
                yield return deepNested;
        }
    }
}
