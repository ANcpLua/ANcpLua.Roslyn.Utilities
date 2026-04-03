using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.AI;

/// <summary>
///     xUnit v3 collection definition for tests requiring a live BitNet server.
///     Apply <c>[Collection(BitNetTestGroup.Name)]</c> to test classes that inject <see cref="BitNetFixture" />.
/// </summary>
[CollectionDefinition(Name)]
public sealed class BitNetTestGroup : ICollectionFixture<BitNetFixture>
{
    /// <summary>
    ///     Collection name constant. Use in <c>[Collection(BitNetTestGroup.Name)]</c>.
    /// </summary>
    public const string Name = "BitNet";
}

/// <summary>
///     Marks a test class as requiring the BitNet LLM fixture.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class BitNetAttribute : Attribute;
