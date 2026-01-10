namespace ANcpLua.Roslyn.Utilities.Testing.MSBuild;

/// <summary>Common target framework monikers.</summary>
public static class Tfm
{
    public const string NetStandard20 = "netstandard2.0";
    public const string NetStandard21 = "netstandard2.1";
    public const string Net60 = "net6.0";
    public const string Net70 = "net7.0";
    public const string Net80 = "net8.0";
    public const string Net90 = "net9.0";
    public const string Net100 = "net10.0";
}

/// <summary>Common MSBuild property names.</summary>
public static class Prop
{
    public const string TargetFramework = "TargetFramework";
    public const string TargetFrameworks = "TargetFrameworks";
    public const string OutputType = "OutputType";
    public const string Nullable = "Nullable";
    public const string ImplicitUsings = "ImplicitUsings";
    public const string LangVersion = "LangVersion";
    public const string TreatWarningsAsErrors = "TreatWarningsAsErrors";
    public const string IsPackable = "IsPackable";
    public const string GenerateDocumentationFile = "GenerateDocumentationFile";
    public const string ManagePackageVersionsCentrally = "ManagePackageVersionsCentrally";
    public const string CentralPackageTransitivePinningEnabled = "CentralPackageTransitivePinningEnabled";
    public const string RootNamespace = "RootNamespace";
    public const string AssemblyName = "AssemblyName";
    public const string Version = "Version";
    public const string Authors = "Authors";
    public const string Company = "Company";
    public const string Product = "Product";
    public const string Description = "Description";
    public const string PackageId = "PackageId";
    public const string PackageTags = "PackageTags";
    public const string RepositoryUrl = "RepositoryUrl";
    public const string RepositoryType = "RepositoryType";
    public const string PublishRepositoryUrl = "PublishRepositoryUrl";
    public const string EmbedUntrackedSources = "EmbedUntrackedSources";
    public const string IncludeSymbols = "IncludeSymbols";
    public const string SymbolPackageFormat = "SymbolPackageFormat";
    public const string Deterministic = "Deterministic";
    public const string ContinuousIntegrationBuild = "ContinuousIntegrationBuild";
    public const string EnableNETAnalyzers = "EnableNETAnalyzers";
    public const string AnalysisLevel = "AnalysisLevel";
    public const string EnforceCodeStyleInBuild = "EnforceCodeStyleInBuild";
}

/// <summary>Common MSBuild property values.</summary>
public static class Val
{
    public const string Library = "Library";
    public const string Exe = "Exe";
    public const string WinExe = "WinExe";
    public const string True = "true";
    public const string False = "false";
    public const string Enable = "enable";
    public const string Disable = "disable";
    public const string Latest = "latest";
    public const string Preview = "preview";
    public const string All = "all";
    public const string None = "none";
    public const string Snupkg = "snupkg";
}

/// <summary>Common MSBuild item names.</summary>
public static class Item
{
    public const string PackageReference = "PackageReference";
    public const string PackageVersion = "PackageVersion";
    public const string ProjectReference = "ProjectReference";
    public const string Compile = "Compile";
    public const string Content = "Content";
    public const string None = "None";
    public const string EmbeddedResource = "EmbeddedResource";
    public const string InternalsVisibleTo = "InternalsVisibleTo";
    public const string Using = "Using";
    public const string Analyzer = "Analyzer";
}

/// <summary>Common MSBuild attribute names.</summary>
public static class Attr
{
    public const string Include = "Include";
    public const string Exclude = "Exclude";
    public const string Remove = "Remove";
    public const string Update = "Update";
    public const string Version = "Version";
    public const string VersionOverride = "VersionOverride";
    public const string PrivateAssets = "PrivateAssets";
    public const string IncludeAssets = "IncludeAssets";
    public const string ExcludeAssets = "ExcludeAssets";
    public const string Condition = "Condition";
    public const string Label = "Label";
}
