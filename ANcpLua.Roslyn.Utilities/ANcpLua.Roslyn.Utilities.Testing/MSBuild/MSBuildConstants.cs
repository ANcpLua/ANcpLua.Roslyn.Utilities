namespace ANcpLua.Roslyn.Utilities.Testing.MSBuild;

/// <summary>
///     Provides constants for common Target Framework Monikers (TFMs).
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>Use these constants when configuring test compilations with specific target frameworks.</description>
///         </item>
///         <item>
///             <description>TFM values follow the standard .NET naming conventions.</description>
///         </item>
///         <item>
///             <description>Includes both .NET Standard and modern .NET versions.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="Prop.TargetFramework" />
/// <seealso cref="Prop.TargetFrameworks" />
public static class Tfm
{
    /// <summary>
    ///     The .NET Standard 2.0 target framework moniker.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Maximum compatibility with .NET Framework 4.6.1+ and .NET Core 2.0+.</description>
    ///         </item>
    ///         <item>
    ///             <description>Required for Roslyn analyzers and source generators.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public const string NetStandard20 = "netstandard2.0";

    /// <summary>
    ///     The .NET Standard 2.1 target framework moniker.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Compatible with .NET Core 3.0+ and .NET 5+.</description>
    ///         </item>
    ///         <item>
    ///             <description>Not compatible with .NET Framework.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public const string NetStandard21 = "netstandard2.1";

    /// <summary>
    ///     The .NET 6 target framework moniker.
    /// </summary>
    public const string Net60 = "net6.0";

    /// <summary>
    ///     The .NET 7 target framework moniker.
    /// </summary>
    public const string Net70 = "net7.0";

    /// <summary>
    ///     The .NET 8 target framework moniker.
    /// </summary>
    public const string Net80 = "net8.0";

    /// <summary>
    ///     The .NET 9 target framework moniker.
    /// </summary>
    public const string Net90 = "net9.0";

    /// <summary>
    ///     The .NET 10 target framework moniker (current LTS release).
    /// </summary>
    public const string Net100 = "net10.0";
}

/// <summary>
///     Provides constants for common MSBuild property names.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>
///                 Use these constants when reading or setting MSBuild properties via
///                 <c>AnalyzerConfigOptions</c>.
///             </description>
///         </item>
///         <item>
///             <description>Property names are case-sensitive in MSBuild.</description>
///         </item>
///         <item>
///             <description>Covers project configuration, packaging, source link, and code analysis properties.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="Val" />
/// <seealso cref="Item" />
public static class Prop
{
    /// <summary>
    ///     The <c>TargetFramework</c> property for single-targeted projects.
    /// </summary>
    /// <seealso cref="Tfm" />
    public const string TargetFramework = "TargetFramework";

    /// <summary>
    ///     The <c>TargetFrameworks</c> property for multi-targeted projects.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Values are semicolon-separated TFMs.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="Tfm" />
    public const string TargetFrameworks = "TargetFrameworks";

    /// <summary>
    ///     The <c>OutputType</c> property specifying the project output type.
    /// </summary>
    /// <seealso cref="Val.Library" />
    /// <seealso cref="Val.Exe" />
    /// <seealso cref="Val.WinExe" />
    public const string OutputType = "OutputType";

    /// <summary>
    ///     The <c>Nullable</c> property controlling nullable reference type annotations.
    /// </summary>
    /// <seealso cref="Val.Enable" />
    /// <seealso cref="Val.Disable" />
    public const string Nullable = "Nullable";

    /// <summary>
    ///     The <c>ImplicitUsings</c> property controlling implicit global using directives.
    /// </summary>
    /// <seealso cref="Val.Enable" />
    /// <seealso cref="Val.Disable" />
    public const string ImplicitUsings = "ImplicitUsings";

    /// <summary>
    ///     The <c>LangVersion</c> property specifying the C# language version.
    /// </summary>
    /// <seealso cref="Val.Latest" />
    /// <seealso cref="Val.Preview" />
    public const string LangVersion = "LangVersion";

    /// <summary>
    ///     The <c>TreatWarningsAsErrors</c> property controlling warning-to-error promotion.
    /// </summary>
    /// <seealso cref="Val.True" />
    /// <seealso cref="Val.False" />
    public const string TreatWarningsAsErrors = "TreatWarningsAsErrors";

    /// <summary>
    ///     The <c>IsPackable</c> property controlling whether the project produces a NuGet package.
    /// </summary>
    /// <seealso cref="Val.True" />
    /// <seealso cref="Val.False" />
    public const string IsPackable = "IsPackable";

    /// <summary>
    ///     The <c>GenerateDocumentationFile</c> property controlling XML documentation generation.
    /// </summary>
    /// <seealso cref="Val.True" />
    /// <seealso cref="Val.False" />
    public const string GenerateDocumentationFile = "GenerateDocumentationFile";

    /// <summary>
    ///     The <c>ManagePackageVersionsCentrally</c> property enabling Central Package Management (CPM).
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>When enabled, package versions are defined in Directory.Packages.props.</description>
    ///         </item>
    ///         <item>
    ///             <description>Project files use <see cref="Item.PackageReference" /> without Version attributes.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="CentralPackageTransitivePinningEnabled" />
    public const string ManagePackageVersionsCentrally = "ManagePackageVersionsCentrally";

    /// <summary>
    ///     The <c>CentralPackageTransitivePinningEnabled</c> property enabling transitive dependency pinning in CPM.
    /// </summary>
    /// <seealso cref="ManagePackageVersionsCentrally" />
    public const string CentralPackageTransitivePinningEnabled = "CentralPackageTransitivePinningEnabled";

    /// <summary>
    ///     The <c>RootNamespace</c> property specifying the root namespace for the project.
    /// </summary>
    public const string RootNamespace = "RootNamespace";

    /// <summary>
    ///     The <c>AssemblyName</c> property specifying the output assembly name.
    /// </summary>
    public const string AssemblyName = "AssemblyName";

    /// <summary>
    ///     The <c>Version</c> property specifying the package and assembly version.
    /// </summary>
    public const string Version = "Version";

    /// <summary>
    ///     The <c>Authors</c> property specifying package authors.
    /// </summary>
    public const string Authors = "Authors";

    /// <summary>
    ///     The <c>Company</c> property specifying the company name.
    /// </summary>
    public const string Company = "Company";

    /// <summary>
    ///     The <c>Product</c> property specifying the product name.
    /// </summary>
    public const string Product = "Product";

    /// <summary>
    ///     The <c>Description</c> property specifying the package description.
    /// </summary>
    public const string Description = "Description";

    /// <summary>
    ///     The <c>PackageId</c> property specifying the NuGet package identifier.
    /// </summary>
    public const string PackageId = "PackageId";

    /// <summary>
    ///     The <c>PackageTags</c> property specifying searchable package tags.
    /// </summary>
    public const string PackageTags = "PackageTags";

    /// <summary>
    ///     The <c>RepositoryUrl</c> property specifying the source repository URL.
    /// </summary>
    /// <seealso cref="RepositoryType" />
    /// <seealso cref="PublishRepositoryUrl" />
    public const string RepositoryUrl = "RepositoryUrl";

    /// <summary>
    ///     The <c>RepositoryType</c> property specifying the source control type (e.g., "git").
    /// </summary>
    /// <seealso cref="RepositoryUrl" />
    public const string RepositoryType = "RepositoryType";

    /// <summary>
    ///     The <c>PublishRepositoryUrl</c> property enabling repository URL publishing in the package.
    /// </summary>
    /// <seealso cref="RepositoryUrl" />
    public const string PublishRepositoryUrl = "PublishRepositoryUrl";

    /// <summary>
    ///     The <c>EmbedUntrackedSources</c> property enabling Source Link for untracked files.
    /// </summary>
    public const string EmbedUntrackedSources = "EmbedUntrackedSources";

    /// <summary>
    ///     The <c>IncludeSymbols</c> property enabling symbol package generation.
    /// </summary>
    /// <seealso cref="SymbolPackageFormat" />
    public const string IncludeSymbols = "IncludeSymbols";

    /// <summary>
    ///     The <c>SymbolPackageFormat</c> property specifying the symbol package format.
    /// </summary>
    /// <seealso cref="Val.Snupkg" />
    /// <seealso cref="IncludeSymbols" />
    public const string SymbolPackageFormat = "SymbolPackageFormat";

    /// <summary>
    ///     The <c>Deterministic</c> property enabling deterministic builds.
    /// </summary>
    /// <seealso cref="ContinuousIntegrationBuild" />
    public const string Deterministic = "Deterministic";

    /// <summary>
    ///     The <c>ContinuousIntegrationBuild</c> property indicating a CI build environment.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Enables Source Link path mapping for reproducible builds.</description>
    ///         </item>
    ///         <item>
    ///             <description>Should be set to true only in CI pipelines.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="Deterministic" />
    public const string ContinuousIntegrationBuild = "ContinuousIntegrationBuild";

    /// <summary>
    ///     The <c>EnableNETAnalyzers</c> property enabling built-in .NET code analyzers.
    /// </summary>
    /// <seealso cref="AnalysisLevel" />
    /// <seealso cref="EnforceCodeStyleInBuild" />
    // ReSharper disable once InconsistentNaming - Must match MSBuild property name exactly
    public const string EnableNETAnalyzers = "EnableNETAnalyzers";

    /// <summary>
    ///     The <c>AnalysisLevel</c> property specifying the code analysis rule severity level.
    /// </summary>
    /// <seealso cref="EnableNETAnalyzers" />
    public const string AnalysisLevel = "AnalysisLevel";

    /// <summary>
    ///     The <c>EnforceCodeStyleInBuild</c> property enabling code style enforcement during build.
    /// </summary>
    /// <seealso cref="EnableNETAnalyzers" />
    public const string EnforceCodeStyleInBuild = "EnforceCodeStyleInBuild";

    // ── ANcpLua.NET.Sdk polyfill/extension injection properties ──

    /// <summary>Injects Throw guard clause utilities (all TFMs).</summary>
    public const string InjectSharedThrow = "InjectSharedThrow";

    /// <summary>Injects StringOrdinalComparer extension.</summary>
    public const string InjectStringOrdinalComparer = "InjectStringOrdinalComparer";

    /// <summary>Injects <c>System.Threading.Lock</c> polyfill.</summary>
    public const string InjectLockPolyfill = "InjectLockPolyfill";

    /// <summary>Injects <c>TimeProvider</c> polyfill.</summary>
    public const string InjectTimeProviderPolyfill = "InjectTimeProviderPolyfill";

    /// <summary>Injects <c>Index</c>/<c>Range</c> struct polyfills.</summary>
    public const string InjectIndexRangeOnLegacy = "InjectIndexRangeOnLegacy";

    /// <summary>Injects <c>IsExternalInit</c> polyfill for records support.</summary>
    public const string InjectIsExternalInitOnLegacy = "InjectIsExternalInitOnLegacy";

    /// <summary>Injects <c>RequiredMemberAttribute</c> polyfill.</summary>
    public const string InjectRequiredMemberOnLegacy = "InjectRequiredMemberOnLegacy";

    /// <summary>Injects <c>CompilerFeatureRequiredAttribute</c> polyfill.</summary>
    public const string InjectCompilerFeatureRequiredOnLegacy = "InjectCompilerFeatureRequiredOnLegacy";

    /// <summary>Injects <c>CallerArgumentExpressionAttribute</c> polyfill.</summary>
    public const string InjectCallerAttributesOnLegacy = "InjectCallerAttributesOnLegacy";

    /// <summary>Injects <c>ParamCollectionAttribute</c> polyfill.</summary>
    public const string InjectParamCollectionOnLegacy = "InjectParamCollectionOnLegacy";

    /// <summary>Injects <c>UnreachableException</c> polyfill.</summary>
    public const string InjectUnreachableExceptionOnLegacy = "InjectUnreachableExceptionOnLegacy";

    /// <summary>Injects <c>StackTraceHiddenAttribute</c> polyfill.</summary>
    public const string InjectStackTraceHiddenOnLegacy = "InjectStackTraceHiddenOnLegacy";

    /// <summary>Injects nullability attributes (<c>AllowNull</c>, <c>MaybeNull</c>, <c>NotNull</c>, etc.).</summary>
    public const string InjectNullabilityAttributesOnLegacy = "InjectNullabilityAttributesOnLegacy";

    /// <summary>Injects AOT/Trim attributes (<c>DynamicallyAccessedMembers</c>, etc.).</summary>
    public const string InjectTrimAttributesOnLegacy = "InjectTrimAttributesOnLegacy";

    /// <summary>Injects <c>ExperimentalAttribute</c> polyfill.</summary>
    public const string InjectExperimentalAttributeOnLegacy = "InjectExperimentalAttributeOnLegacy";

    /// <summary>Injects diagnostic code analysis classes polyfill.</summary>
    public const string InjectDiagnosticClassesOnLegacy = "InjectDiagnosticClassesOnLegacy";

    /// <summary>Injects <c>String.Contains</c>/<c>String.Replace</c> with <c>StringComparison</c> overloads.</summary>
    public const string InjectStringExtensionsPolyfill = "InjectStringExtensionsPolyfill";
}

/// <summary>
///     Provides constants for common MSBuild property values.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>Use these constants when setting MSBuild property values.</description>
///         </item>
///         <item>
///             <description>Boolean values use lowercase strings ("true"/"false") per MSBuild conventions.</description>
///         </item>
///         <item>
///             <description>Includes output types, boolean toggles, and special values.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="Prop" />
public static class Val
{
    /// <summary>
    ///     The "Library" output type for class library projects.
    /// </summary>
    /// <seealso cref="Prop.OutputType" />
    public const string Library = "Library";

    /// <summary>
    ///     The "Exe" output type for console application projects.
    /// </summary>
    /// <seealso cref="Prop.OutputType" />
    public const string Exe = "Exe";

    /// <summary>
    ///     The "WinExe" output type for Windows GUI application projects.
    /// </summary>
    /// <seealso cref="Prop.OutputType" />
    public const string WinExe = "WinExe";

    /// <summary>
    ///     The "true" boolean value for MSBuild properties.
    /// </summary>
    public const string True = "true";

    /// <summary>
    ///     The "false" boolean value for MSBuild properties.
    /// </summary>
    public const string False = "false";

    /// <summary>
    ///     The "enable" value for feature toggle properties.
    /// </summary>
    /// <seealso cref="Prop.Nullable" />
    /// <seealso cref="Prop.ImplicitUsings" />
    public const string Enable = "enable";

    /// <summary>
    ///     The "disable" value for feature toggle properties.
    /// </summary>
    /// <seealso cref="Prop.Nullable" />
    /// <seealso cref="Prop.ImplicitUsings" />
    public const string Disable = "disable";

    /// <summary>
    ///     The "latest" value for the <see cref="Prop.LangVersion" /> property.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Uses the latest stable C# language version.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="Prop.LangVersion" />
    /// <seealso cref="Preview" />
    public const string Latest = "latest";

    /// <summary>
    ///     The "preview" value for the <see cref="Prop.LangVersion" /> property.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Enables preview C# language features.</description>
    ///         </item>
    ///         <item>
    ///             <description>Preview features may change before final release.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="Prop.LangVersion" />
    /// <seealso cref="Latest" />
    public const string Preview = "preview";

    /// <summary>
    ///     The "all" value for asset inclusion/exclusion properties.
    /// </summary>
    /// <seealso cref="Attr.PrivateAssets" />
    /// <seealso cref="Attr.IncludeAssets" />
    /// <seealso cref="Attr.ExcludeAssets" />
    public const string All = "all";

    /// <summary>
    ///     The "none" value for asset inclusion/exclusion properties.
    /// </summary>
    /// <seealso cref="Attr.PrivateAssets" />
    /// <seealso cref="Attr.IncludeAssets" />
    /// <seealso cref="Attr.ExcludeAssets" />
    public const string None = "none";

    /// <summary>
    ///     The "snupkg" value for the <see cref="Prop.SymbolPackageFormat" /> property.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Portable PDB symbol package format.</description>
    ///         </item>
    ///         <item>
    ///             <description>Recommended format for NuGet.org symbol server.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="Prop.SymbolPackageFormat" />
    /// <seealso cref="Prop.IncludeSymbols" />
    public const string Snupkg = "snupkg";
}

/// <summary>
///     Provides constants for common MSBuild item names.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>Use these constants when working with MSBuild item groups.</description>
///         </item>
///         <item>
///             <description>Item names are case-sensitive in MSBuild.</description>
///         </item>
///         <item>
///             <description>Covers package references, project references, and file items.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="Attr" />
/// <seealso cref="Prop" />
public static class Item
{
    /// <summary>
    ///     The <c>PackageReference</c> item for NuGet package dependencies.
    /// </summary>
    /// <seealso cref="Attr.Version" />
    /// <seealso cref="Attr.PrivateAssets" />
    public const string PackageReference = "PackageReference";

    /// <summary>
    ///     The <c>PackageVersion</c> item for Central Package Management version definitions.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Used in Directory.Packages.props files.</description>
    ///         </item>
    ///         <item>
    ///             <description>Requires <see cref="Prop.ManagePackageVersionsCentrally" /> to be enabled.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="Prop.ManagePackageVersionsCentrally" />
    public const string PackageVersion = "PackageVersion";

    /// <summary>
    ///     The <c>ProjectReference</c> item for project-to-project dependencies.
    /// </summary>
    public const string ProjectReference = "ProjectReference";

    /// <summary>
    ///     The <c>Compile</c> item for C# source files.
    /// </summary>
    public const string Compile = "Compile";

    /// <summary>
    ///     The <c>Content</c> item for content files included in the output.
    /// </summary>
    public const string Content = "Content";

    /// <summary>
    ///     The <c>None</c> item for files with no build action.
    /// </summary>
    public const string None = "None";

    /// <summary>
    ///     The <c>EmbeddedResource</c> item for embedded resource files.
    /// </summary>
    public const string EmbeddedResource = "EmbeddedResource";

    /// <summary>
    ///     The <c>InternalsVisibleTo</c> item for exposing internal types to friend assemblies.
    /// </summary>
    public const string InternalsVisibleTo = "InternalsVisibleTo";

    /// <summary>
    ///     The <c>Using</c> item for global using directives.
    /// </summary>
    public const string Using = "Using";

    /// <summary>
    ///     The <c>Analyzer</c> item for Roslyn analyzer references.
    /// </summary>
    public const string Analyzer = "Analyzer";
}

/// <summary>
///     Provides constants for common MSBuild attribute names.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>Use these constants when building or parsing MSBuild item metadata.</description>
///         </item>
///         <item>
///             <description>Attribute names are case-sensitive in MSBuild.</description>
///         </item>
///         <item>
///             <description>Covers item identity, versioning, asset control, and conditional attributes.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="Item" />
/// <seealso cref="Val" />
public static class Attr
{
    /// <summary>
    ///     The <c>Include</c> attribute specifying items to include.
    /// </summary>
    /// <seealso cref="Exclude" />
    /// <seealso cref="Remove" />
    /// <seealso cref="Update" />
    public const string Include = "Include";

    /// <summary>
    ///     The <c>Exclude</c> attribute specifying items to exclude from inclusion.
    /// </summary>
    /// <seealso cref="Include" />
    public const string Exclude = "Exclude";

    /// <summary>
    ///     The <c>Remove</c> attribute specifying items to remove from the item group.
    /// </summary>
    /// <seealso cref="Include" />
    public const string Remove = "Remove";

    /// <summary>
    ///     The <c>Update</c> attribute specifying items to modify without adding.
    /// </summary>
    /// <seealso cref="Include" />
    public const string Update = "Update";

    /// <summary>
    ///     The <c>Version</c> attribute specifying the package version.
    /// </summary>
    /// <seealso cref="Item.PackageReference" />
    /// <seealso cref="VersionOverride" />
    public const string Version = "Version";

    /// <summary>
    ///     The <c>VersionOverride</c> attribute for overriding CPM-defined versions.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Used with Central Package Management to override a specific package version.</description>
    ///         </item>
    ///         <item>
    ///             <description>Takes precedence over versions defined in Directory.Packages.props.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="Version" />
    /// <seealso cref="Prop.ManagePackageVersionsCentrally" />
    public const string VersionOverride = "VersionOverride";

    /// <summary>
    ///     The <c>PrivateAssets</c> attribute controlling asset transitivity.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Assets listed here are not consumed by dependent projects.</description>
    ///         </item>
    ///         <item>
    ///             <description>Common values: "all", "none", or specific asset types.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="Val.All" />
    /// <seealso cref="Val.None" />
    /// <seealso cref="IncludeAssets" />
    /// <seealso cref="ExcludeAssets" />
    public const string PrivateAssets = "PrivateAssets";

    /// <summary>
    ///     The <c>IncludeAssets</c> attribute specifying which assets to include.
    /// </summary>
    /// <seealso cref="ExcludeAssets" />
    /// <seealso cref="PrivateAssets" />
    public const string IncludeAssets = "IncludeAssets";

    /// <summary>
    ///     The <c>ExcludeAssets</c> attribute specifying which assets to exclude.
    /// </summary>
    /// <seealso cref="IncludeAssets" />
    /// <seealso cref="PrivateAssets" />
    public const string ExcludeAssets = "ExcludeAssets";

    /// <summary>
    ///     The <c>Condition</c> attribute for conditional MSBuild evaluation.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Supports MSBuild condition expressions (e.g., "'$(Configuration)'=='Debug'").</description>
    ///         </item>
    ///         <item>
    ///             <description>Can be applied to properties, items, and targets.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public const string Condition = "Condition";

    /// <summary>
    ///     The <c>Label</c> attribute for identifying MSBuild elements.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Used to tag PropertyGroups and ItemGroups for organizational purposes.</description>
    ///         </item>
    ///         <item>
    ///             <description>Has no effect on build behavior.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public const string Label = "Label";
}

/// <summary>
///     Provides factory methods for generating MSBuild XML property snippets.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>Generates well-formed XML elements for common MSBuild properties.</description>
///         </item>
///         <item>
///             <description>Useful for building test project files or Directory.Build.props content.</description>
///         </item>
///         <item>
///             <description>All methods return strings suitable for embedding in MSBuild XML files.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="Prop" />
/// <seealso cref="Val" />
public static class XmlSnippetBuilder
{
    /// <summary>
    ///     Generates a <c>&lt;TargetFramework&gt;</c> XML element.
    /// </summary>
    /// <param name="tfm">The target framework moniker value.</param>
    /// <returns>An XML element string like <c>&lt;TargetFramework&gt;net10.0&lt;/TargetFramework&gt;</c>.</returns>
    /// <seealso cref="Tfm" />
    public static string TargetFramework(string tfm) => $"<{Prop.TargetFramework}>{tfm}</{Prop.TargetFramework}>";

    /// <summary>
    ///     Generates a <c>&lt;LangVersion&gt;</c> XML element.
    /// </summary>
    /// <param name="version">The C# language version value.</param>
    /// <returns>An XML element string like <c>&lt;LangVersion&gt;14&lt;/LangVersion&gt;</c>.</returns>
    /// <seealso cref="Val.Latest" />
    /// <seealso cref="Val.Preview" />
    public static string LangVersion(string version) => $"<{Prop.LangVersion}>{version}</{Prop.LangVersion}>";

    /// <summary>
    ///     Generates an <c>&lt;OutputType&gt;</c> XML element.
    /// </summary>
    /// <param name="type">The output type value.</param>
    /// <returns>An XML element string like <c>&lt;OutputType&gt;Library&lt;/OutputType&gt;</c>.</returns>
    /// <seealso cref="Val.Library" />
    /// <seealso cref="Val.Exe" />
    public static string OutputType(string type) => $"<{Prop.OutputType}>{type}</{Prop.OutputType}>";

    /// <summary>
    ///     Generates an XML element for any MSBuild property.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <returns>An XML element string like <c>&lt;PropertyName&gt;value&lt;/PropertyName&gt;</c>.</returns>
    /// <seealso cref="Prop" />
    public static string Property(string name, string value) => $"<{name}>{value}</{name}>";
}
