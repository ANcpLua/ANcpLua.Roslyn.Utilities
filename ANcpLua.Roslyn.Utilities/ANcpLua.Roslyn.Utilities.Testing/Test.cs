using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Provides a fluent API for testing Roslyn incremental generators.
/// </summary>
/// <typeparam name="TGenerator">
///     The type of the incremental generator to test.
///     Must implement <see cref="IIncrementalGenerator" /> and have a parameterless constructor.
/// </typeparam>
/// <remarks>
///     <para>
///         This static class serves as the primary entry point for generator testing.
///         It provides simplified methods that handle compilation setup, generator execution,
///         and result comparison automatically.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Automatically runs the generator twice to validate incremental caching behavior.</description>
///         </item>
///         <item>
///             <description>Returns a <see cref="GeneratorResult" /> with fluent assertion methods.</description>
///         </item>
///         <item>
///             <description>Supports both simple source-only tests and advanced configuration scenarios.</description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <para>Basic usage with source code:</para>
///     <code>
///     using var result = await Test&lt;MyGenerator&gt;.Run(@"
///         namespace Example;
///         [MyAttribute]
///         public partial class Foo { }
///     ");
///     result
///         .Produces("Foo.g.cs", expectedContent)
///         .IsCached()
///         .IsClean();
///     </code>
/// </example>
/// <example>
///     <para>Advanced usage with custom configuration:</para>
///     <code>
///     using var result = await Test&lt;MyGenerator&gt;.Run(engine => engine
///         .WithSource(source1)
///         .WithSource(source2)
///         .WithAdditionalText("config.json", configContent)
///         .WithLanguageVersion(LanguageVersion.CSharp12));
///     result
///         .Compiles()
///         .HasNoForbiddenTypes();
///     </code>
/// </example>
/// <seealso cref="GeneratorResult" />
/// <seealso cref="GeneratorTestEngine{TGenerator}" />
/// <seealso cref="TestConfiguration" />
public static class Test<TGenerator> where TGenerator : IIncrementalGenerator, new()
{
    /// <summary>
    ///     Runs the generator on the provided source code and returns an assertable result.
    /// </summary>
    /// <param name="source">
    ///     The C# source code to compile and run the generator against.
    ///     This source will be parsed using the language version specified in <see cref="TestConfiguration.LanguageVersion" />
    ///     .
    /// </param>
    /// <param name="cancellationToken">
    ///     An optional <see cref="CancellationToken" /> to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="GeneratorResult" /> containing the generated outputs, diagnostics,
    ///     and caching information for inspection and fluent assertions.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 The generator is executed twice with identical input to validate
    ///                 that incremental caching is working correctly.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Reference assemblies are resolved based on <see cref="TestConfiguration.ReferenceAssemblies" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 The returned <see cref="GeneratorResult" /> implements <see cref="IDisposable" />
    ///                 and calls <see cref="GeneratorResult.Verify" /> on disposal to ensure all assertions pass.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     using var result = await Test&lt;MyGenerator&gt;.Run(@"
    ///         [GenerateInterface]
    ///         public partial class MyService { }
    ///     ");
    ///     result.Produces("IMyService.g.cs").IsClean();
    ///     </code>
    /// </example>
    /// <seealso cref="Run(Action{GeneratorTestEngine{TGenerator}}, CancellationToken)" />
    /// <seealso cref="GeneratorResult" />
    public static async Task<GeneratorResult> Run(string source, CancellationToken cancellationToken = default)
    {
        var engine = new GeneratorTestEngine<TGenerator>().WithSource(source);
        var (firstRun, secondRun) = await engine.RunTwiceAsync(cancellationToken);
        return new GeneratorResult(firstRun, secondRun, source, typeof(TGenerator));
    }

    /// <summary>
    ///     Runs the generator with custom configuration and returns an assertable result.
    /// </summary>
    /// <param name="configure">
    ///     An action that configures the <see cref="GeneratorTestEngine{TGenerator}" />
    ///     with sources, references, additional texts, and other options.
    /// </param>
    /// <param name="cancellationToken">
    ///     An optional <see cref="CancellationToken" /> to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="GeneratorResult" /> containing the generated outputs, diagnostics,
    ///     and caching information for inspection and fluent assertions.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Use this overload when you need fine-grained control over the test setup,
    ///         such as adding multiple source files, additional texts, custom references,
    ///         or specific analyzer configuration options.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 The <see cref="GeneratorTestEngine{TGenerator}" /> supports fluent configuration
    ///                 with methods like <c>WithSource</c>, <c>WithReference</c>, and <c>WithAdditionalText</c>.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 The generator is executed twice to validate incremental caching behavior.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 The source property of the returned result will be <c>null</c> since multiple
    ///                 sources may be configured.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     using var result = await Test&lt;MyGenerator&gt;.Run(engine => engine
    ///         .WithSource(@"public partial class Foo { }")
    ///         .WithSource(@"public partial class Bar { }")
    ///         .WithAdditionalText("settings.json", "{\"enabled\": true}")
    ///         .WithReference(typeof(SomeExternalType).Assembly));
    /// 
    ///     result
    ///         .Produces("Foo.g.cs")
    ///         .Produces("Bar.g.cs")
    ///         .IsCached()
    ///         .HasNoForbiddenTypes();
    ///     </code>
    /// </example>
    /// <seealso cref="Run(string, CancellationToken)" />
    /// <seealso cref="GeneratorTestEngine{TGenerator}" />
    /// <seealso cref="GeneratorResult" />
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