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
            [CSharpSyntaxTree.ParseText(NestedSource), CSharpSyntaxTree.ParseText(builder.ToString())],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var errors = compilation.GetDiagnostics().Where(d => d.Severity is DiagnosticSeverity.Error).ToArray();
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

    private static INamedTypeSymbol GetType(string source, string metadataName)
    {
        var compilation = CSharpCompilation.Create(
            "TypeDeclarationShapes",
            [CSharpSyntaxTree.ParseText(source)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation.GetTypeByMetadataName(metadataName)
               ?? throw new InvalidOperationException($"Expected test symbol '{metadataName}' to resolve.");
    }

    private static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n");
    }
}
