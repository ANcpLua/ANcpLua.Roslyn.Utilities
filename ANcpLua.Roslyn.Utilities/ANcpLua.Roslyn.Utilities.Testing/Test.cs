using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     God API entry point for generator testing.
/// </summary>
/// <typeparam name="TGenerator">The generator type to test.</typeparam>
public static class Test<TGenerator> where TGenerator : IIncrementalGenerator, new()
{
    /// <summary>
    ///     Runs the generator on the provided source and returns an assertable result.
    /// </summary>
    /// <param name="source">The source code to compile.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A <see cref="GeneratorResult" /> for inspection and fluent assertions.</returns>
    public static async Task<GeneratorResult> Run(string source, CancellationToken cancellationToken = default)
    {
        var engine = new GeneratorTestEngine<TGenerator>().WithSource(source);
        var (firstRun, secondRun) = await engine.RunTwiceAsync(cancellationToken);
        return new GeneratorResult(firstRun, secondRun, source, typeof(TGenerator));
    }

    /// <summary>
    ///     Runs the generator with custom configuration.
    /// </summary>
    /// <param name="configure">Configuration action for the test engine.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A <see cref="GeneratorResult" /> for inspection and fluent assertions.</returns>
    public static async Task<GeneratorResult> Run(
        Action<GeneratorTestEngine<TGenerator>> configure,
        CancellationToken cancellationToken = default)
    {
        var engine = new GeneratorTestEngine<TGenerator>();
        configure(engine);
        var (firstRun, secondRun) = await engine.RunTwiceAsync(cancellationToken);
        return new GeneratorResult(firstRun, secondRun, null, typeof(TGenerator));
    }
}
