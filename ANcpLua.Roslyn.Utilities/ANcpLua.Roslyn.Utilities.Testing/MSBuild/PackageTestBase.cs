using Meziantou.Framework;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.MSBuild;

/// <summary>
///     Abstract base class for integration tests using custom NuGet packages or SDKs.
/// </summary>
/// <typeparam name="TFixture">The fixture type for managing test resources (must implement <see cref="IAsyncLifetime" />).</typeparam>
/// <remarks>
///     <para>
///         <see cref="PackageTestBase{TFixture}" /> provides a convenient foundation for integration tests
///         that need to work with isolated NuGet packages or custom SDKs. It integrates with test fixtures
///         for lifecycle management and provides builder patterns for quick project creation and building.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Generic over the fixture type, allowing flexibility in fixture implementation while maintaining
///                 a common test base.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Provides <see cref="CreateProjectBuilder" /> for easy project creation with fixture configuration.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Includes convenience methods (<see cref="QuickBuild" />, <see cref="BuildLibrary" />,
///                 <see cref="BuildExe" />) for common test scenarios.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Supports flexible target framework and property configuration for multi-targeting scenarios.
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
/// public class MyPackageTests : PackageTestBase&lt;NuGetPackageFixture&gt;
/// {
///     private readonly NuGetPackageFixture _fixture;
///
///     public MyPackageTests(NuGetPackageFixture fixture)
///     {
///         _fixture = fixture;
///     }
///
///     [Fact]
///     public async Task BuildSimpleProject()
///     {
///         var result = await QuickBuild("public class Foo { }");
///         result.ShouldSucceed();
///     }
///
///     [Fact]
///     public async Task BuildLibraryWithProperties()
///     {
///         var result = await BuildLibrary(
///             "public class Bar { }",
///             Tfm.Net80,
///             (Prop.Nullable, Val.Enable));
///
///         result.ShouldSucceed();
///     }
///
///     [Fact]
///     public async Task BuildExecutable()
///     {
///         var result = await BuildExe("Console.WriteLine(\"Hello\");");
///         result.ShouldSucceed();
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="PackageProjectBuilder" />
/// <seealso cref="NuGetPackageFixture" />
public abstract class PackageTestBase<TFixture>
    where TFixture : class
{
    /// <summary>
    ///     Gets the test fixture for managing package and SDK resources.
    /// </summary>
    /// <value>The fixture instance provided by the test framework.</value>
    protected TFixture Fixture { get; }

    /// <summary>
    ///     Creates a new <see cref="PackageTestBase{TFixture}" /> with the provided fixture.
    /// </summary>
    /// <param name="fixture">The fixture instance for test resource management.</param>
    /// <remarks>
    ///     The fixture is typically injected by xUnit's dependency injection system.
    /// </remarks>
    protected PackageTestBase(TFixture fixture)
    {
        Fixture = fixture;
    }

    /// <summary>
    ///     Creates a new <see cref="PackageProjectBuilder" /> with the fixture's package configuration.
    /// </summary>
    /// <param name="testOutputHelper">Optional test output helper for logging.</param>
    /// <param name="packageDirectory">The package directory path.</param>
    /// <param name="packageName">The package name.</param>
    /// <param name="packageVersion">The package version.</param>
    /// <param name="importStyle">The SDK import style. Defaults to <see cref="PackageImportStyle.SdkElement" />.</param>
    /// <returns>A new <see cref="PackageProjectBuilder" /> configured with the provided settings.</returns>
    /// <remarks>
    ///     This method is provided as a helper for test classes. Derived classes should override
    ///     or provide additional builder methods that map specific fixture properties to builder configuration.
    /// </remarks>
    protected PackageProjectBuilder CreateProjectBuilder(
        ITestOutputHelper? testOutputHelper,
        FullPath packageDirectory,
        string packageName,
        string packageVersion,
        PackageImportStyle importStyle = PackageImportStyle.SdkElement) =>
        new PackageProjectBuilder(testOutputHelper, packageDirectory, packageName, packageVersion, importStyle);

    /// <summary>
    ///     Quickly builds a project with minimal configuration for library testing.
    /// </summary>
    /// <param name="code">The C# source code to compile.</param>
    /// <param name="tfm">
    ///     The target framework moniker. Defaults to <see cref="Tfm.Net100" />.
    /// </param>
    /// <param name="extraProps">Optional additional MSBuild properties.</param>
    /// <returns>
    ///     A task representing the build operation, which returns a <see cref="BuildResult" /> with
    ///     the build output.
    /// </returns>
    /// <remarks>
    ///     This method is a convenience for common quick build scenarios. It creates a temporary project
    ///     with library output type, compiles the provided source code, and returns the result.
    ///     For more control, use <see cref="CreateProjectBuilder" /> directly.
    /// </remarks>
    /// <example>
    ///     <code>
    /// var result = await QuickBuild(
    ///     "public class Foo { }",
    ///     Tfm.Net80,
    ///     (Prop.Nullable, Val.Enable));
    /// result.ShouldSucceed();
    /// </code>
    /// </example>
    protected virtual Task<BuildResult> QuickBuild(
        string code,
        string tfm = Tfm.Net100,
        params (string Key, string Value)[] extraProps) =>
        throw new NotImplementedException();

    /// <summary>
    ///     Builds a library project with the provided source code.
    /// </summary>
    /// <param name="code">The C# source code to compile.</param>
    /// <param name="tfm">
    ///     The target framework moniker. Defaults to <see cref="Tfm.Net100" />.
    /// </param>
    /// <param name="extraProps">Optional additional MSBuild properties.</param>
    /// <returns>
    ///     A task representing the build operation, which returns a <see cref="BuildResult" /> with
    ///     the build output.
    /// </returns>
    /// <remarks>
    ///     This method is a convenience for building class libraries. It typically delegates to
    ///     <see cref="QuickBuild" /> or provides library-specific defaults. For more control,
    ///     use <see cref="CreateProjectBuilder" /> and configure the project manually.
    /// </remarks>
    /// <example>
    ///     <code>
    /// var result = await BuildLibrary("public class Bar { }");
    /// result.ShouldSucceed();
    /// </code>
    /// </example>
    protected virtual Task<BuildResult> BuildLibrary(
        string code,
        string tfm = Tfm.Net100,
        params (string Key, string Value)[] extraProps) =>
        throw new NotImplementedException();

    /// <summary>
    ///     Builds an executable project with the provided source code.
    /// </summary>
    /// <param name="code">The C# source code to compile.</param>
    /// <param name="tfm">
    ///     The target framework moniker. Defaults to <see cref="Tfm.Net100" />.
    /// </param>
    /// <param name="extraProps">Optional additional MSBuild properties.</param>
    /// <returns>
    ///     A task representing the build operation, which returns a <see cref="BuildResult" /> with
    ///     the build output.
    /// </returns>
    /// <remarks>
    ///     This method is a convenience for building console applications or other executables.
    ///     It configures the project with <see cref="Val.Exe" /> output type and compiles the provided
    ///     source code. For more control, use <see cref="CreateProjectBuilder" /> directly.
    /// </remarks>
    /// <example>
    ///     <code>
    /// var result = await BuildExe("Console.WriteLine(\"Hello\");");
    /// result.ShouldSucceed();
    /// </code>
    /// </example>
    protected virtual Task<BuildResult> BuildExe(
        string code,
        string tfm = Tfm.Net100,
        params (string Key, string Value)[] extraProps) =>
        throw new NotImplementedException();
}

/// <summary>
///     Abstract base class for integration tests using <see cref="NuGetPackageFixture" />.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="NuGetPackageTestBase" /> provides concrete implementations of convenience methods
///         for testing with <see cref="NuGetPackageFixture" />. It handles the boilerplate of fixture
///         configuration and package directory setup.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// public class MyPackageTests : NuGetPackageTestBase
/// {
///     private readonly NuGetPackageFixture _fixture;
///     private readonly ITestOutputHelper _output;
///
///     public MyPackageTests(NuGetPackageFixture fixture, ITestOutputHelper output)
///     {
///         _fixture = fixture;
///         _output = output;
///     }
///
///     [Fact]
///     public async Task BuildWithNuGet()
///     {
///         var result = await QuickBuild(
///             "public class Foo { }",
///             Tfm.Net80);
///         result.ShouldSucceed();
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="PackageProjectBuilder" />
/// <seealso cref="NuGetPackageFixture" />
public abstract class NuGetPackageTestBase : PackageTestBase<NuGetPackageFixture>
{
    /// <summary>
    ///     The test output helper for logging build output.
    /// </summary>
    protected ITestOutputHelper? TestOutputHelper { get; }

    /// <summary>
    ///     Creates a new <see cref="NuGetPackageTestBase" /> with fixture and optional output helper.
    /// </summary>
    /// <param name="fixture">The <see cref="NuGetPackageFixture" /> for test resource management.</param>
    /// <param name="testOutputHelper">Optional test output helper for logging build operations.</param>
    protected NuGetPackageTestBase(NuGetPackageFixture fixture, ITestOutputHelper? testOutputHelper = null)
        : base(fixture)
    {
        TestOutputHelper = testOutputHelper;
    }

    /// <summary>
    ///     Quickly builds a project with the fixture's package configuration.
    /// </summary>
    /// <param name="code">The C# source code to compile.</param>
    /// <param name="tfm">
    ///     The target framework moniker. Defaults to <see cref="Tfm.Net100" />.
    /// </param>
    /// <param name="extraProps">Optional additional MSBuild properties.</param>
    /// <returns>
    ///     A task representing the build operation, which returns a <see cref="BuildResult" /> with
    ///     the build output.
    /// </returns>
    protected override async Task<BuildResult> QuickBuild(
        string code,
        string tfm = Tfm.Net100,
        params (string Key, string Value)[] extraProps)
    {
        await using var project = CreateProjectBuilder(
            TestOutputHelper,
            Fixture.PackageDirectory,
            "TestPackage",
            Fixture.Version);

        return await project
            .WithTargetFramework(tfm)
            .WithOutputType(Val.Library)
            .WithProperties(extraProps)
            .AddSource("Code.cs", code)
            .BuildAsync();
    }

    /// <summary>
    ///     Builds a library project with the fixture's package configuration.
    /// </summary>
    /// <param name="code">The C# source code to compile.</param>
    /// <param name="tfm">
    ///     The target framework moniker. Defaults to <see cref="Tfm.Net100" />.
    /// </param>
    /// <param name="extraProps">Optional additional MSBuild properties.</param>
    /// <returns>
    ///     A task representing the build operation, which returns a <see cref="BuildResult" /> with
    ///     the build output.
    /// </returns>
    protected override Task<BuildResult> BuildLibrary(
        string code,
        string tfm = Tfm.Net100,
        params (string Key, string Value)[] extraProps) =>
        QuickBuild(code, tfm, extraProps);

    /// <summary>
    ///     Builds an executable project with the fixture's package configuration.
    /// </summary>
    /// <param name="code">The C# source code to compile.</param>
    /// <param name="tfm">
    ///     The target framework moniker. Defaults to <see cref="Tfm.Net100" />.
    /// </param>
    /// <param name="extraProps">Optional additional MSBuild properties.</param>
    /// <returns>
    ///     A task representing the build operation, which returns a <see cref="BuildResult" /> with
    ///     the build output.
    /// </returns>
    protected override async Task<BuildResult> BuildExe(
        string code,
        string tfm = Tfm.Net100,
        params (string Key, string Value)[] extraProps)
    {
        await using var project = CreateProjectBuilder(
            TestOutputHelper,
            Fixture.PackageDirectory,
            "TestPackage",
            Fixture.Version);

        return await project
            .WithTargetFramework(tfm)
            .WithOutputType(Val.Exe)
            .WithProperties(extraProps)
            .AddSource("Program.cs", code)
            .BuildAsync();
    }
}
