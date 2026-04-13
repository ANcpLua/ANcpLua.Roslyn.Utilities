using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.BitNet;

/// <summary>
///     xUnit v3 collection definition for tests requiring a live BitNet server.
///     Apply <c>[Collection("BitNet")]</c> to test classes that inject <see cref="BitNetFixture" />.
/// </summary>
[CollectionDefinition("BitNet")]
public sealed class BitNetTestGroup : ICollectionFixture<BitNetFixture>;
