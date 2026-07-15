using ANcpLua.Analyzers.AotReflection;
using AwesomeAssertions;
using Xunit;

namespace ANcpLua.Analyzers.AotReflection.Tests;

public sealed class ClassMetadataDispatchTests
{
    private static MethodMetadata Method(string name, Type parameterType, Func<object?[], object?> body) =>
        new()
        {
            Name = name,
            IsStatic = true,
            Parameters = [new ParameterMetadata { Type = parameterType }],
            Invoker = (_, args) => body(args),
        };

    private static ConstructorMetadata Ctor(Type parameterType, Func<object?[], object> body) =>
        new()
        {
            Parameters = [new ParameterMetadata { Type = parameterType }],
            Factory = body,
        };

    [Fact]
    public void InvokeMethod_SameArityOverloads_DispatchesByParameterType()
    {
        // Arrange — string overload FIRST so the old name+arity-only path would misdispatch an int arg.
        var metadata = new ClassMetadata
        {
            Methods =
            [
                Method("Value", typeof(string), args => $"string:{args[0]}"),
                Method("Value", typeof(int), args => $"int:{args[0]}"),
            ],
        };

        // Act & Assert
        metadata.InvokeMethod(null, "Value", 42).Should().Be("int:42");
        metadata.InvokeMethod(null, "Value", "hi").Should().Be("string:hi");
    }

    [Fact]
    public void InvokeMethod_UniqueArity_DispatchesByArity()
    {
        // Arrange
        var metadata = new ClassMetadata
        {
            Methods =
            [
                new MethodMetadata { Name = "Sum", IsStatic = true, Invoker = (_, _) => "zero" },
                new MethodMetadata
                {
                    Name = "Sum",
                    IsStatic = true,
                    Parameters = [new ParameterMetadata { Type = typeof(int) }, new ParameterMetadata { Type = typeof(int) }],
                    Invoker = (_, args) => $"two:{args[0]}+{args[1]}",
                },
            ],
        };

        // Act & Assert
        metadata.InvokeMethod(null, "Sum").Should().Be("zero");
        metadata.InvokeMethod(null, "Sum", 1, 2).Should().Be("two:1+2");
    }

    [Fact]
    public void InvokeMethod_NullArgument_MatchesReferenceParameter()
    {
        // Arrange
        var metadata = new ClassMetadata
        {
            Methods = [Method("Name", typeof(string), args => args[0] is null ? "null" : "value")],
        };

        // Act & Assert — null fits a reference-type parameter, so it dispatches rather than throwing.
        metadata.InvokeMethod(null, "Name", [null]).Should().Be("null");
    }

    [Fact]
    public void InvokeMethod_UnknownName_Throws()
    {
        // Arrange
        var metadata = new ClassMetadata { Methods = [Method("Known", typeof(int), _ => 0)] };

        // Act
        var act = () => metadata.InvokeMethod(null, "Unknown", 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateInstance_SameArityOverloads_DispatchesByParameterType()
    {
        // Arrange — string ctor FIRST so the old arity-only path would misdispatch an int arg.
        var metadata = new ClassMetadata
        {
            Constructors =
            [
                Ctor(typeof(string), args => $"from-string:{args[0]}"),
                Ctor(typeof(int), args => $"from-int:{args[0]}"),
            ],
        };

        // Act & Assert
        metadata.CreateInstance(7).Should().Be("from-int:7");
        metadata.CreateInstance("x").Should().Be("from-string:x");
    }

    [Fact]
    public void CreateInstance_NoMatchingArity_Throws()
    {
        // Arrange
        var metadata = new ClassMetadata { Constructors = [Ctor(typeof(int), _ => new object())] };

        // Act
        var act = () => metadata.CreateInstance(1, 2);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
