using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class ConvertExtensionsTests
{
    private enum SampleEnum : byte
    {
        Zero = 0,
        One = 1,
        Two = 2
    }

    [Fact]
    public void ToEnum_ConvertsUnderlyingIntegralPayloadFromTypedConstant()
    {
        var argument = GetTypedConstantForEnumArgument("SampleEnum.One");

        argument.ToEnum(SampleEnum.Two).Should().Be(SampleEnum.One);
        argument.ToEnum<SampleEnum>().Should().Be(SampleEnum.One);
    }

    [Fact]
    public void ToEnum_DefaultsWhenPayloadIsUnsupportedType()
    {
        var argument = GetObjectTypedConstant("\"invalid\"");

        argument.ToEnum(SampleEnum.Two).Should().Be(SampleEnum.Two);
        argument.ToEnum<SampleEnum>().Should().BeNull();
    }

    private static TypedConstant GetTypedConstantForEnumArgument(string argumentExpression)
    {
        const string source =
            """
            using System;

            public enum SampleEnum : byte
            {
                Zero = 0,
                One = 1,
                Two = 2
            }

            [AttributeUsage(AttributeTargets.Method)]
            public sealed class EnumArgumentAttribute : Attribute
            {
                public EnumArgumentAttribute(SampleEnum value) { }
            }

            public sealed class Subject
            {
                [EnumArgumentAttribute(ARG)]
                public void Method() { }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source.Replace("ARG", argumentExpression));
        var semanticModel = GetSemanticModel(tree);
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

        return semanticModel.GetDeclaredSymbol(method)!
            .GetAttributes()
            .Single(attribute => attribute.AttributeClass?.Name == "EnumArgumentAttribute")
            .ConstructorArguments[0];
    }

    private static TypedConstant GetObjectTypedConstant(string argumentExpression)
    {
        const string source =
            """
            using System;

            [AttributeUsage(AttributeTargets.Method)]
            public sealed class ObjectArgumentAttribute : Attribute
            {
                public ObjectArgumentAttribute(object value) { }
            }

            public sealed class Subject
            {
                [ObjectArgumentAttribute(ARG)]
                public void Method() { }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source.Replace("ARG", argumentExpression));
        var semanticModel = GetSemanticModel(tree);
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

        return semanticModel.GetDeclaredSymbol(method)!
            .GetAttributes()
            .Single(attribute => attribute.AttributeClass?.Name == "ObjectArgumentAttribute")
            .ConstructorArguments[0];
    }

    private static SemanticModel GetSemanticModel(SyntaxTree tree)
    {
        var compilation = CSharpCompilation.Create(
            "typed-constant-fixture",
            new[] { tree },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation.GetSemanticModel(tree);
    }
}
