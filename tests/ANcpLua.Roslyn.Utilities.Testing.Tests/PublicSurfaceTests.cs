using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class PublicSurfaceTests
{
    // Consumers import ANcpLua.Roslyn.Utilities next to System.Linq and Roslyn's namespaces; an extension
    // method with the same name and parameter types as one of theirs turns every call into CS0121.
    // The .Sources-only async helpers compile for modern targets alone; this assembly links them (csproj).
    [Fact]
    public void ExtensionMethods_DoNotDuplicateBclOrRoslynSignatures()
    {
        var theirs = new[]
            {
                typeof(object), typeof(Enumerable), typeof(AsyncEnumerable), typeof(PriorityQueue<,>),
                typeof(ImmutableArray), typeof(SyntaxNode), typeof(CSharpCompilation)
            }
            .Select(static type => type.Assembly)
            .Distinct()
            .SelectMany(static assembly => ExtensionMethods(assembly.GetExportedTypes()))
            .Select(Signature)
            .ToHashSet(StringComparer.Ordinal);

        var ours = typeof(Guard).Assembly.GetExportedTypes().Concat(typeof(PublicSurfaceTests).Assembly.GetTypes()
            .Where(static type => type.Namespace == "ANcpLua.Roslyn.Utilities.Async"));

        var duplicates = ExtensionMethods(ours)
            .Where(method => theirs.Contains(Signature(method)))
            .Select(static method => $"{method.DeclaringType!.Name}.{Signature(method)}")
            .ToList();

        duplicates.Should().BeEmpty("each makes consumer calls ambiguous (CS0121): {0}", string.Join("; ", duplicates));
    }

    private static IEnumerable<MethodInfo> ExtensionMethods(IEnumerable<Type> types) =>
        types
            .Where(static type => type is { IsAbstract: true, IsSealed: true })
            .SelectMany(static type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(static method => method.IsDefined(typeof(ExtensionAttribute), false));

    private static string Signature(MethodInfo method) =>
        $"{method.Name}({string.Join(", ", method.GetParameters().Select(static p => Shape(p.ParameterType)))})";

    private static string Shape(Type type) => type switch
    {
        { IsGenericParameter: true } => (type.DeclaringMethod is null ? "T" : "M") + type.GenericParameterPosition,
        { HasElementType: true } => Shape(type.GetElementType()!) + (type.IsArray ? "[]" : type.IsByRef ? "&" : "*"),
        { IsGenericType: true } =>
            $"{type.GetGenericTypeDefinition().FullName}<{string.Join(", ", type.GetGenericArguments().Select(Shape))}>",
        _ => type.FullName ?? type.Name
    };
}
