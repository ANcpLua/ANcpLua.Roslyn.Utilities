using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Provides a fluent API for testing Roslyn incremental generators.
/// </summary>
public static class Test
{
    /// <summary>
    ///     Runs one or more generators with custom configuration and returns an assertable result.
    /// </summary>
    /// <param name="configure">An action that configures the test engine.</param>
    /// <param name="primaryGeneratorType">The primary generator type for reporting purposes.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A <see cref="GeneratorResult" />.</returns>
    internal static async Task<GeneratorResult> Run(
        Action<GeneratorTestEngine> configure,
        Type? primaryGeneratorType = null,
        CancellationToken cancellationToken = default)
    {
        var engine = new GeneratorTestEngine();
        configure(engine);
        var (firstRun, secondRun) = await engine.RunTwiceAsync(cancellationToken);
        return new GeneratorResult(firstRun, secondRun, null, primaryGeneratorType ?? typeof(GeneratorTestEngine));
    }
}

/// <summary>
///     Generic version of <see cref="Test" /> for a single generator.
/// </summary>
/// <typeparam name="TGenerator">The type of the incremental generator to test.</typeparam>
public static class Test<TGenerator> where TGenerator : IIncrementalGenerator, new()
{
    public static async Task<GeneratorResult> Run(string source, CancellationToken cancellationToken = default)
    {
        var engine = new GeneratorTestEngine<TGenerator>().WithSource(source);
        var (firstRun, secondRun) = await engine.RunTwiceAsync(cancellationToken);
        return new GeneratorResult(firstRun, secondRun, source, typeof(TGenerator));
    }

    internal static async Task<GeneratorResult> Run(
        Action<GeneratorTestEngine<TGenerator>> configure,
        CancellationToken cancellationToken = default)
    {
        var engine = new GeneratorTestEngine<TGenerator>();
        configure(engine);
        var (firstRun, secondRun) = await engine.RunTwiceAsync(cancellationToken);
        return new GeneratorResult(firstRun, secondRun, null, typeof(TGenerator));
    }
}
