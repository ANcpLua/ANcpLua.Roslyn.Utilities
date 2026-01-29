using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Provides a fluent API for dynamic compilation in tests with pre-configured
///     references and Shouldly-style assertions.
/// </summary>
/// <remarks>
///     <para>
///         This class simplifies dynamic compilation testing by providing:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Fluent builder pattern for configuring compilations</description>
///         </item>
///         <item>
///             <description>Pre-configured references for System.Runtime and core types</description>
///         </item>
///         <item>
///             <description>Easy access to compiled assemblies and types</description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <para>
///         Basic compilation with assertions:
///     </para>
///     <code>
/// Compile.Source(code)
///     .WithCommonReferences()
///     .Build()
///     .ShouldSucceed();
/// </code>
///     <para>
///         Get assembly directly (throws on failure):
///     </para>
///     <code>
/// var assembly = Compile.Source(code).BuildOrThrow();
/// </code>
///     <para>
///         Create instance from compiled code:
///     </para>
///     <code>
/// var greeter = Compile.Source(code)
///     .Build()
///     .ShouldSucceed()
///     .CreateInstance&lt;IGreeter&gt;("Greeter");
/// </code>
/// </example>
/// <seealso cref="CompileResult" />
/// <seealso cref="CompileResultAssertions" />
public sealed class Compile
{
    private readonly List<SyntaxTree> _sources = [];
    private readonly HashSet<MetadataReference> _references = [];
    private string _assemblyName = "DynamicAssembly";
    private OutputKind _outputKind = OutputKind.DynamicallyLinkedLibrary;
    private CSharpParseOptions _parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
    private OptimizationLevel _optimization = OptimizationLevel.Debug;
    private bool _allowUnsafe;

    private Compile()
    {
        _references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        _references.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location));
    }

    /// <summary>
    ///     Creates a compiler with the specified source code.
    /// </summary>
    /// <param name="code">The C# source code to compile.</param>
    /// <returns>A new <see cref="Compile" /> instance configured with the source.</returns>
    public static Compile Source(string code) => new Compile().WithSource(code);

    /// <summary>
    ///     Creates an empty compiler for configuration.
    /// </summary>
    /// <returns>A new empty <see cref="Compile" /> instance.</returns>
    public static Compile Empty() => new();

    /// <summary>
    ///     Adds source code to the compilation.
    /// </summary>
    /// <param name="code">The C# source code to add.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public Compile WithSource(string code)
    {
        _sources.Add(CSharpSyntaxTree.ParseText(code, _parseOptions));
        return this;
    }

    /// <summary>
    ///     Adds multiple source files to the compilation.
    /// </summary>
    /// <param name="sources">The C# source code files to add.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public Compile WithSources(params string[] sources)
    {
        foreach (var code in sources)
            WithSource(code);
        return this;
    }

    /// <summary>
    ///     Adds a reference from a type's assembly.
    /// </summary>
    /// <typeparam name="T">A type whose assembly should be referenced.</typeparam>
    /// <returns>This instance for fluent chaining.</returns>
    public Compile WithReference<T>()
    {
        _references.Add(MetadataReference.CreateFromFile(typeof(T).Assembly.Location));
        return this;
    }

    /// <summary>
    ///     Adds a reference from an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to reference.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public Compile WithReference(Assembly assembly)
    {
        _references.Add(MetadataReference.CreateFromFile(assembly.Location));
        return this;
    }

    /// <summary>
    ///     Adds a reference from a file path.
    /// </summary>
    /// <param name="path">The path to the assembly file.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public Compile WithReference(string path)
    {
        _references.Add(MetadataReference.CreateFromFile(path));
        return this;
    }

    /// <summary>
    ///     Adds references from multiple types' assemblies.
    /// </summary>
    /// <param name="types">Types whose assemblies should be referenced.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public Compile WithReferences(params Type[] types)
    {
        foreach (var type in types)
            _references.Add(MetadataReference.CreateFromFile(type.Assembly.Location));
        return this;
    }

    /// <summary>
    ///     Adds common .NET references (Console, Linq, Collections).
    /// </summary>
    /// <returns>This instance for fluent chaining.</returns>
    public Compile WithCommonReferences()
    {
        _references.Add(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location));
        _references.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));
        _references.Add(MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location));
        return this;
    }

    /// <summary>
    ///     Sets the assembly name for the compilation output.
    /// </summary>
    /// <param name="name">The assembly name.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public Compile WithAssemblyName(string name)
    {
        _assemblyName = name;
        return this;
    }

    /// <summary>
    ///     Sets the output kind for the compilation.
    /// </summary>
    /// <param name="kind">The output kind.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public Compile WithOutputKind(OutputKind kind)
    {
        _outputKind = kind;
        return this;
    }

    /// <summary>
    ///     Configures the compilation as an executable.
    /// </summary>
    /// <returns>This instance for fluent chaining.</returns>
    public Compile AsExecutable()
    {
        _outputKind = OutputKind.ConsoleApplication;
        return this;
    }

    /// <summary>
    ///     Sets the C# language version for parsing.
    /// </summary>
    /// <param name="version">The language version.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public Compile WithLanguageVersion(LanguageVersion version)
    {
        _parseOptions = _parseOptions.WithLanguageVersion(version);
        var existingSources = _sources.Select(s => s.GetText().ToString()).ToList();
        _sources.Clear();
        foreach (var source in existingSources)
            _sources.Add(CSharpSyntaxTree.ParseText(source, _parseOptions));
        return this;
    }

    /// <summary>
    ///     Enables release mode optimization.
    /// </summary>
    /// <returns>This instance for fluent chaining.</returns>
    public Compile WithOptimization()
    {
        _optimization = OptimizationLevel.Release;
        return this;
    }

    /// <summary>
    ///     Allows unsafe code in the compilation.
    /// </summary>
    /// <returns>This instance for fluent chaining.</returns>
    public Compile WithUnsafe()
    {
        _allowUnsafe = true;
        return this;
    }

    /// <summary>
    ///     Builds the compilation and returns the result.
    /// </summary>
    /// <returns>A <see cref="CompileResult" /> containing the compilation output.</returns>
    public CompileResult Build()
    {
        var compilation = CSharpCompilation.Create(
            _assemblyName,
            _sources,
            _references,
            new CSharpCompilationOptions(_outputKind)
                .WithOptimizationLevel(_optimization)
                .WithAllowUnsafe(_allowUnsafe));

        using var ms = new MemoryStream();
        var emitResult = compilation.Emit(ms);

        Assembly? assembly = null;
        if (emitResult.Success)
        {
            ms.Seek(0, SeekOrigin.Begin);
            assembly = Assembly.Load(ms.ToArray());
        }

        return new CompileResult(emitResult, assembly, compilation);
    }

    /// <summary>
    ///     Builds and throws on failure. Returns the assembly.
    /// </summary>
    /// <returns>The compiled assembly.</returns>
    /// <exception cref="InvalidOperationException">Thrown when compilation fails.</exception>
    public Assembly BuildOrThrow() => Build().ShouldSucceed().Assembly!;
}

/// <summary>
///     Represents the result of a dynamic compilation with query methods for inspection.
/// </summary>
/// <remarks>
///     <para>
///         This class encapsulates the complete output of a compilation, including:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>The emit result with success/failure status</description>
///         </item>
///         <item>
///             <description>The loaded assembly if compilation succeeded</description>
///         </item>
///         <item>
///             <description>All diagnostics (errors, warnings, info)</description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <para>
///         Inspect compilation results:
///     </para>
///     <code>
/// var result = Compile.Source(code).Build();
/// if (result.Succeeded)
/// {
///     var type = result.GetType("MyClass");
///     var instance = result.CreateInstance("MyClass");
/// }
/// else
/// {
///     foreach (var error in result.Errors)
///         Console.WriteLine(error.GetMessage());
/// }
/// </code>
/// </example>
/// <seealso cref="Compile" />
/// <seealso cref="CompileResultAssertions" />
public sealed class CompileResult
{
    internal CompileResult(EmitResult emitResult, Assembly? assembly, CSharpCompilation compilation)
    {
        EmitResult = emitResult;
        Assembly = assembly;
        Compilation = compilation;
        Diagnostics = emitResult.Diagnostics;
        Errors = emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Warnings = emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToArray();
    }

    /// <summary>
    ///     Gets the raw emit result from the compilation.
    /// </summary>
    public EmitResult EmitResult { get; }

    /// <summary>
    ///     Gets the loaded assembly if compilation succeeded.
    /// </summary>
    public Assembly? Assembly { get; }

    /// <summary>
    ///     Gets the compilation object for advanced inspection.
    /// </summary>
    public CSharpCompilation Compilation { get; }

    /// <summary>
    ///     Gets all diagnostics produced by the compilation.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>
    ///     Gets error diagnostics only.
    /// </summary>
    public IReadOnlyList<Diagnostic> Errors { get; }

    /// <summary>
    ///     Gets warning diagnostics only.
    /// </summary>
    public IReadOnlyList<Diagnostic> Warnings { get; }

    /// <summary>
    ///     Gets a value indicating whether the compilation succeeded.
    /// </summary>
    public bool Succeeded => EmitResult.Success;

    /// <summary>
    ///     Gets a value indicating whether the compilation failed.
    /// </summary>
    public bool Failed => !EmitResult.Success;

    /// <summary>
    ///     Checks if the compilation has an error with the specified ID.
    /// </summary>
    /// <param name="errorId">The diagnostic ID to check for (e.g., "CS0246").</param>
    /// <returns><see langword="true" /> if an error exists; otherwise, <see langword="false" />.</returns>
    public bool HasError(string errorId) => Errors.Any(e => e.Id == errorId);

    /// <summary>
    ///     Checks if the compilation has a warning with the specified ID.
    /// </summary>
    /// <param name="warningId">The diagnostic ID to check for (e.g., "CS8618").</param>
    /// <returns><see langword="true" /> if a warning exists; otherwise, <see langword="false" />.</returns>
    public bool HasWarning(string warningId) => Warnings.Any(w => w.Id == warningId);

    /// <summary>
    ///     Checks if a type exists in the compiled assembly.
    /// </summary>
    /// <param name="typeName">The fully qualified type name.</param>
    /// <returns><see langword="true" /> if the type exists; otherwise, <see langword="false" />.</returns>
    public bool ContainsType(string typeName) => Assembly?.GetType(typeName) is not null;

    /// <summary>
    ///     Gets a type from the compiled assembly.
    /// </summary>
    /// <param name="name">The fully qualified type name.</param>
    /// <returns>The type, or <see langword="null" /> if not found.</returns>
    public Type? GetType(string name) => Assembly?.GetType(name);

    /// <summary>
    ///     Gets a type from the compiled assembly, throws if not found.
    /// </summary>
    /// <param name="name">The fully qualified type name.</param>
    /// <returns>The type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the type is not found.</exception>
    public Type GetRequiredType(string name) =>
        GetType(name) ?? throw new InvalidOperationException($"Type '{name}' not found in compiled assembly");

    /// <summary>
    ///     Creates an instance of a type from the compiled assembly.
    /// </summary>
    /// <param name="typeName">The fully qualified type name.</param>
    /// <returns>The created instance, or <see langword="null" /> if creation failed.</returns>
    public object? CreateInstance(string typeName) => Assembly?.CreateInstance(typeName);

    /// <summary>
    ///     Creates an instance of a type from the compiled assembly.
    /// </summary>
    /// <typeparam name="T">The expected type of the instance.</typeparam>
    /// <param name="typeName">The fully qualified type name.</param>
    /// <returns>The created instance, or <see langword="null" /> if creation failed.</returns>
    public T? CreateInstance<T>(string typeName) where T : class => CreateInstance(typeName) as T;

    /// <summary>
    ///     Creates an instance of a type, throws if null.
    /// </summary>
    /// <typeparam name="T">The expected type of the instance.</typeparam>
    /// <param name="typeName">The fully qualified type name.</param>
    /// <returns>The created instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when instance creation fails.</exception>
    public T CreateRequiredInstance<T>(string typeName) where T : class =>
        CreateInstance<T>(typeName) ?? throw new InvalidOperationException($"Failed to create instance of '{typeName}'");

    /// <summary>
    ///     Formats all diagnostics for display.
    /// </summary>
    /// <returns>A formatted string containing all diagnostics.</returns>
    public string FormatDiagnostics()
    {
        if (Diagnostics.Count == 0)
            return "(none)";

        var sb = new StringBuilder();
        foreach (var d in Diagnostics)
            sb.AppendLine($"  {d.Severity}: {d.Id} - {d.GetMessage()}");
        return sb.ToString();
    }
}

/// <summary>
///     Provides fluent assertion extension methods for <see cref="CompileResult" />.
/// </summary>
/// <remarks>
///     <para>
///         This class enables expressive, chainable assertions for validating compilation
///         results in tests. All assertion methods return the original <see cref="CompileResult" />
///         to support fluent chaining.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// Compile.Source(code)
///     .Build()
///     .ShouldSucceed()
///     .ShouldHaveNoWarnings()
///     .ShouldContainType("MyNamespace.MyClass");
/// </code>
/// </example>
/// <seealso cref="CompileResult" />
/// <seealso cref="Compile" />
public static class CompileResultAssertions
{
    /// <summary>
    ///     Asserts that the compilation succeeded.
    /// </summary>
    /// <param name="result">The compilation result to validate.</param>
    /// <param name="because">Optional custom failure message.</param>
    /// <returns>The same <see cref="CompileResult" /> instance for fluent chaining.</returns>
    public static CompileResult ShouldSucceed(this CompileResult result, string? because = null)
    {
        Assert.True(result.Succeeded,
            because ?? $"Compilation should succeed. Diagnostics:\n{result.FormatDiagnostics()}");
        return result;
    }

    /// <summary>
    ///     Asserts that the compilation failed.
    /// </summary>
    /// <param name="result">The compilation result to validate.</param>
    /// <param name="because">Optional custom failure message.</param>
    /// <returns>The same <see cref="CompileResult" /> instance for fluent chaining.</returns>
    public static CompileResult ShouldFail(this CompileResult result, string? because = null)
    {
        Assert.True(result.Failed,
            because ?? "Compilation should fail but succeeded.");
        return result;
    }

    /// <summary>
    ///     Asserts that the compilation has no warnings.
    /// </summary>
    /// <param name="result">The compilation result to validate.</param>
    /// <returns>The same <see cref="CompileResult" /> instance for fluent chaining.</returns>
    public static CompileResult ShouldHaveNoWarnings(this CompileResult result)
    {
        Assert.True(result.Warnings.Count == 0,
            $"Expected no warnings, got {result.Warnings.Count}. Diagnostics:\n{result.FormatDiagnostics()}");
        return result;
    }

    /// <summary>
    ///     Asserts that the compilation produced an error with the specified ID.
    /// </summary>
    /// <param name="result">The compilation result to validate.</param>
    /// <param name="errorId">The diagnostic ID to check for.</param>
    /// <returns>The same <see cref="CompileResult" /> instance for fluent chaining.</returns>
    public static CompileResult ShouldHaveError(this CompileResult result, string errorId)
    {
        Assert.True(result.HasError(errorId),
            $"Expected error {errorId}. Diagnostics:\n{result.FormatDiagnostics()}");
        return result;
    }

    /// <summary>
    ///     Asserts that the compilation produced a warning with the specified ID.
    /// </summary>
    /// <param name="result">The compilation result to validate.</param>
    /// <param name="warningId">The diagnostic ID to check for.</param>
    /// <returns>The same <see cref="CompileResult" /> instance for fluent chaining.</returns>
    public static CompileResult ShouldHaveWarning(this CompileResult result, string warningId)
    {
        Assert.True(result.HasWarning(warningId),
            $"Expected warning {warningId}. Diagnostics:\n{result.FormatDiagnostics()}");
        return result;
    }

    /// <summary>
    ///     Asserts that the compilation has at least N errors.
    /// </summary>
    /// <param name="result">The compilation result to validate.</param>
    /// <param name="count">The minimum number of errors expected.</param>
    /// <returns>The same <see cref="CompileResult" /> instance for fluent chaining.</returns>
    public static CompileResult ShouldHaveErrors(this CompileResult result, int count)
    {
        Assert.True(result.Errors.Count >= count,
            $"Expected at least {count} errors, got {result.Errors.Count}. Diagnostics:\n{result.FormatDiagnostics()}");
        return result;
    }

    /// <summary>
    ///     Asserts that a type exists in the compiled assembly.
    /// </summary>
    /// <param name="result">The compilation result to validate.</param>
    /// <param name="typeName">The fully qualified type name.</param>
    /// <returns>The same <see cref="CompileResult" /> instance for fluent chaining.</returns>
    public static CompileResult ShouldContainType(this CompileResult result, string typeName)
    {
        result.ShouldSucceed();
        Assert.True(result.ContainsType(typeName),
            $"Type '{typeName}' not found in compiled assembly.");
        return result;
    }
}
