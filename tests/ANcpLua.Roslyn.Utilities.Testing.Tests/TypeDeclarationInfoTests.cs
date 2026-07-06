using System;
using System.Linq;
using ANcpLua.Roslyn.Utilities;
using ANcpLua.Roslyn.Utilities.Models;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class TypeDeclarationInfoTests
{
    private const string NestedSource = """
namespace Deep
{
    public partial class Outer<T>
    {
        public partial struct Middle
        {
            public partial record struct Inner<U>
            {
            }
        }
    }
}
""";

    private const string ShapesSource = """
namespace Shapes
{
    public class PlainClass { }
    public partial interface IContract { }
    public record PlainRecord { }
    public readonly record struct ValueRecord { }
    public struct PlainStruct { }
    public enum PlainEnum { None }
}

public class GlobalType { }
""";

    [Fact]
    public void From_SimpleClass_CapturesDeclaration()
    {
        var info = TypeDeclarationInfo.From(GetType(ShapesSource, "Shapes.PlainClass"));

        info.Namespace.Should().Be("Shapes");
        info.Keyword.Should().Be("class");
        info.Name.Should().Be("PlainClass");
        info.GenericParameterClause.Should().BeNull();
        info.IsPartial.Should().BeFalse();
        info.IsNested.Should().BeFalse();
        info.ContainingTypes.IsEmpty.Should().BeTrue();
        info.DisplayName.Should().Be("PlainClass");
        info.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void From_NestedInGenericClassAndStruct_CapturesPerLevelChain()
    {
        var info = TypeDeclarationInfo.From(GetType(NestedSource, "Deep.Outer`1+Middle+Inner`1"));

        info.Namespace.Should().Be("Deep");
        info.Keyword.Should().Be("record struct");
        info.Name.Should().Be("Inner");
        info.GenericParameterClause.Should().Be("<U>");
        info.IsPartial.Should().BeTrue();
        info.IsNested.Should().BeTrue();
        info.DisplayName.Should().Be("Inner<U>");

        var chain = info.ContainingTypes.AsImmutableArray();
        chain.Length.Should().Be(2);
        chain[0].Should().Be(new ContainingTypeInfo("class", "Outer", "<T>", true));
        chain[1].Should().Be(new ContainingTypeInfo("struct", "Middle", null, true));
        chain[0].DisplayName.Should().Be("Outer<T>");
    }

    [Fact]
    public void From_GlobalNamespaceType_HasNullNamespace()
    {
        var info = TypeDeclarationInfo.From(GetType(ShapesSource, "GlobalType"));

        info.Namespace.Should().BeNull();
    }

    [Theory]
    [InlineData("Shapes.IContract", "interface")]
    [InlineData("Shapes.PlainRecord", "record")]
    [InlineData("Shapes.ValueRecord", "record struct")]
    [InlineData("Shapes.PlainStruct", "struct")]
    [InlineData("Shapes.PlainEnum", "enum")]
    public void From_CapturesDeclarationKeyword(string metadataName, string expectedKeyword)
    {
        var info = TypeDeclarationInfo.From(GetType(ShapesSource, metadataName));

        info.Keyword.Should().Be(expectedKeyword);
    }

    [Fact]
    public void IsFullyPartial_TrueWhenTargetAndAllContainersArePartial()
    {
        var info = TypeDeclarationInfo.From(GetType(NestedSource, "Deep.Outer`1+Middle+Inner`1"));

        info.IsFullyPartial.Should().BeTrue();
    }

    [Fact]
    public void IsFullyPartial_FalseWhenContainerIsNotPartial()
    {
        const string source = """
namespace Gap
{
    public class Outer
    {
        public partial class Inner { }
    }
}
""";

        var info = TypeDeclarationInfo.From(GetType(source, "Gap.Outer+Inner"));

        info.IsPartial.Should().BeTrue();
        info.IsFullyPartial.Should().BeFalse();
    }

    [Fact]
    public void BeginDeclaration_EmitsNestedPartialWrapper()
    {
        var info = TypeDeclarationInfo.From(GetType(NestedSource, "Deep.Outer`1+Middle+Inner`1"));

        var builder = new IndentedStringBuilder();
        using (info.BeginDeclaration(builder))
        {
            builder.AppendLine("public int Generated => 1;");
        }

        const string expected = """
namespace Deep
{
    partial class Outer<T>
    {
        partial struct Middle
        {
            partial record struct Inner<U>
            {
                public int Generated => 1;
            }
        }
    }
}

""";
        Normalize(builder.ToString()).Should().Be(Normalize(expected));
    }

    [Fact]
    public void BeginDeclaration_GlobalNamespacePartialType_OmitsNamespaceBlock()
    {
        const string source = "public partial class Standalone { }";
        var info = TypeDeclarationInfo.From(GetType(source, "Standalone"));

        var builder = new IndentedStringBuilder();
        using (info.BeginDeclaration(builder))
        {
        }

        const string expected = """
partial class Standalone
{
}

""";
        Normalize(builder.ToString()).Should().Be(Normalize(expected));
    }

    [Fact]
    public void BeginDeclaration_Output_CompilesTogetherWithOriginalSource()
    {
        var info = TypeDeclarationInfo.From(GetType(NestedSource, "Deep.Outer`1+Middle+Inner`1"));

        var builder = new IndentedStringBuilder();
        using (info.BeginDeclaration(builder))
        {
            builder.AppendLine("public int Generated => 1;");
        }

        var compilation = CSharpCompilation.Create(
            "PartialMerge",
            [
                CSharpSyntaxTree.ParseText(NestedSource, cancellationToken: TestContext.Current.CancellationToken),
                CSharpSyntaxTree.ParseText(builder.ToString(), cancellationToken: TestContext.Current.CancellationToken)
            ],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var errors = compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(d => d.Severity is DiagnosticSeverity.Error).ToArray();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void From_ProducesEqualValues_AcrossIdenticalCompilations()
    {
        var first = TypeDeclarationInfo.From(GetType(NestedSource, "Deep.Outer`1+Middle+Inner`1"));
        var second = TypeDeclarationInfo.From(GetType(NestedSource, "Deep.Outer`1+Middle+Inner`1"));

        second.Should().Be(first);
        second.GetHashCode().Should().Be(first.GetHashCode());
    }

    [Fact]
    public void Default_IsDefault()
    {
        default(TypeDeclarationInfo).IsDefault.Should().BeTrue();
    }

    [Fact]
    public void GetHintName_NestedGenericChain_EncodesEveryLevelWithArity()
    {
        var info = TypeDeclarationInfo.From(GetType(NestedSource, "Deep.Outer`1+Middle+Inner`1"));

        info.GetHintName().Should().Be("Deep.Outer(1)-Middle-Inner(1).g.cs");
    }

    [Fact]
    public void GetHintName_GlobalNamespaceType_OmitsNamespacePrefix()
    {
        var info = TypeDeclarationInfo.From(GetType("public class Standalone { }", "Standalone"));

        info.GetHintName().Should().Be("Standalone.g.cs");
    }

    [Fact]
    public void GetHintName_SimpleNamespacedType_UsesQualifiedName()
    {
        var info = TypeDeclarationInfo.From(GetType(ShapesSource, "Shapes.PlainClass"));

        info.GetHintName().Should().Be("Shapes.PlainClass.g.cs");
    }

    [Fact]
    public void GetHintName_ArityMarker_DisambiguatesGenericOverloads()
    {
        const string source = """
namespace Overloads
{
    public class Result { }
    public class Result<T> { }
    public class Result<T1, T2> { }
}
""";

        var plain = TypeDeclarationInfo.From(GetType(source, "Overloads.Result")).GetHintName();
        var one = TypeDeclarationInfo.From(GetType(source, "Overloads.Result`1")).GetHintName();
        var two = TypeDeclarationInfo.From(GetType(source, "Overloads.Result`2")).GetHintName();

        plain.Should().Be("Overloads.Result.g.cs");
        one.Should().Be("Overloads.Result(1).g.cs");
        two.Should().Be("Overloads.Result(2).g.cs");
        new[] { plain, one, two }.Distinct().Should().HaveCount(3);
    }

    [Fact]
    public void GetHintName_ArityMarker_CannotBeForgedByIdentifier()
    {
        // A type literally named Result_1 must not collide with Result<T>.
        const string source = """
namespace Forgery
{
    public class Result_1 { }
    public class Result<T> { }
}
""";

        var literal = TypeDeclarationInfo.From(GetType(source, "Forgery.Result_1")).GetHintName();
        var generic = TypeDeclarationInfo.From(GetType(source, "Forgery.Result`1")).GetHintName();

        literal.Should().Be("Forgery.Result_1.g.cs");
        generic.Should().Be("Forgery.Result(1).g.cs");
        literal.Should().NotBe(generic);
    }

    [Fact]
    public void GetHintName_NestingSeparator_DistinguishesNamespaceFromContainingType()
    {
        // namespace A { class B { class C } } must not collide with namespace A.B { class C }.
        const string nestedShape = """
namespace A
{
    public class B
    {
        public class C { }
    }
}
""";
        const string namespacedShape = """
namespace A.B
{
    public class C { }
}
""";

        var nested = TypeDeclarationInfo.From(GetType(nestedShape, "A.B+C")).GetHintName();
        var namespaced = TypeDeclarationInfo.From(GetType(namespacedShape, "A.B.C")).GetHintName();

        nested.Should().Be("A.B-C.g.cs");
        namespaced.Should().Be("A.B.C.g.cs");
        nested.Should().NotBe(namespaced);
    }

    [Fact]
    public void GetHintName_IsAcceptedByRoslynHintNameValidation()
    {
        var info = TypeDeclarationInfo.From(GetType(NestedSource, "Deep.Outer`1+Middle+Inner`1"));
        var hintName = info.GetHintName();

        var compilation = CSharpCompilation.Create(
            "HintNameProbe",
            [],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new AddSourceProbeGenerator(hintName).AsSourceGenerator());
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult().Results.Single();

        result.Exception.Should().BeNull();
        result.GeneratedSources.Length.Should().Be(1);
        result.GeneratedSources[0].HintName.Should().Be(hintName);
    }

    [Fact]
    public void GetFullyQualifiedMetadataName_RoundTripsThroughGetTypeByMetadataName()
    {
        var compilation = CreateCompilation(NestedSource);
        var original = compilation.GetTypeByMetadataName("Deep.Outer`1+Middle+Inner`1")
                       ?? throw new InvalidOperationException("Expected test symbol to resolve.");

        var info = TypeDeclarationInfo.From(original);
        var metadataName = info.GetFullyQualifiedMetadataName();

        metadataName.Should().Be("Deep.Outer`1+Middle+Inner`1");
        var resolved = compilation.GetTypeByMetadataName(metadataName);
        SymbolEqualityComparer.Default.Equals(resolved, original).Should().BeTrue();
    }

    [Theory]
    [InlineData(ShapesSource, "Shapes.PlainClass", "Shapes.PlainClass")]
    [InlineData("public class Standalone { }", "Standalone", "Standalone")]
    public void GetFullyQualifiedMetadataName_SimpleShapes(string source, string metadataName, string expected)
    {
        var info = TypeDeclarationInfo.From(GetType(source, metadataName));

        info.GetFullyQualifiedMetadataName().Should().Be(expected);

        var resolved = CreateCompilation(source).GetTypeByMetadataName(expected);
        resolved.Should().NotBeNull();
    }

    [Fact]
    public void GetFullyQualifiedName_NestedGenericChain_UsesGlobalAliasAndParameterNames()
    {
        var info = TypeDeclarationInfo.From(GetType(NestedSource, "Deep.Outer`1+Middle+Inner`1"));

        info.GetFullyQualifiedName().Should().Be("global::Deep.Outer<T>.Middle.Inner<U>");
    }

    [Fact]
    public void GetFullyQualifiedName_GlobalNamespaceType_KeepsGlobalAlias()
    {
        var info = TypeDeclarationInfo.From(GetType("public class Standalone { }", "Standalone"));

        info.GetFullyQualifiedName().Should().Be("global::Standalone");
    }

    [Fact]
    public void GetFullyQualifiedName_IsValidInsideGeneratedPartial()
    {
        var info = TypeDeclarationInfo.From(GetType(NestedSource, "Deep.Outer`1+Middle+Inner`1"));

        var builder = new IndentedStringBuilder();
        using (info.BeginDeclaration(builder))
        {
            builder.AppendLine($"public {info.GetFullyQualifiedName()} Self() => this;");
        }

        var compilation = CSharpCompilation.Create(
            "SelfReference",
            [
                CSharpSyntaxTree.ParseText(NestedSource, cancellationToken: TestContext.Current.CancellationToken),
                CSharpSyntaxTree.ParseText(builder.ToString(), cancellationToken: TestContext.Current.CancellationToken)
            ],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var errors = compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(d => d.Severity is DiagnosticSeverity.Error).ToArray();
        errors.Should().BeEmpty();
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create(
            "TypeDeclarationShapes",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private sealed class AddSourceProbeGenerator(string hintName) : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(hintName, "// probe"));
        }
    }

    private static INamedTypeSymbol GetType(string source, string metadataName)
    {
        return CreateCompilation(source).GetTypeByMetadataName(metadataName)
               ?? throw new InvalidOperationException($"Expected test symbol '{metadataName}' to resolve.");
    }

    private static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
