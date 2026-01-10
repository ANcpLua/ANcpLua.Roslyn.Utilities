using Meziantou.Framework;

namespace ANcpLua.Roslyn.Utilities.Testing.MSBuild;

/// <summary>
/// Locates the repository root directory for integration testing scenarios.
/// </summary>
/// <remarks>
/// <para>
/// This struct provides a robust mechanism for finding the root directory of a source repository
/// by searching upward from the current working directory for common marker files.
/// </para>
/// <list type="bullet">
///     <item><description>Immutable value type with value-based equality semantics.</description></item>
///     <item><description>Supports glob patterns (e.g., <c>*.sln</c>) for flexible marker matching.</description></item>
///     <item><description>Provides implicit conversions to <see cref="FullPath"/> and <see cref="string"/> for convenience.</description></item>
///     <item><description>Throws <see cref="DirectoryNotFoundException"/> when the repository root cannot be found.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var root = RepositoryRoot.Locate();
/// var testDataPath = root["TestData/SampleProject"];
/// </code>
/// </example>
/// <seealso cref="FullPath"/>
public readonly record struct RepositoryRoot
{
    private RepositoryRoot(FullPath path)
    {
        FullPath = path;
    }

    /// <summary>
    /// Gets the full path to the repository root directory.
    /// </summary>
    /// <value>
    /// A <see cref="Meziantou.Framework.FullPath"/> representing the absolute path to the repository root.
    /// </value>
    public FullPath FullPath { get; }

    /// <summary>
    /// Gets a path relative to the repository root by combining the root path with a relative path segment.
    /// </summary>
    /// <param name="relativePath">The relative path to append to the repository root.</param>
    /// <returns>
    /// A <see cref="FullPath"/> representing the combined absolute path.
    /// </returns>
    /// <example>
    /// <code>
    /// var root = RepositoryRoot.Locate();
    /// FullPath srcPath = root["src/MyProject"];
    /// FullPath testFile = root["tests/Sample.cs"];
    /// </code>
    /// </example>
    public FullPath this[string relativePath] => FullPath / relativePath;

    /// <summary>
    /// Locates the repository root by searching upward from the current directory for marker files.
    /// </summary>
    /// <param name="markerFiles">
    /// Files or directories that indicate the repository root. Supports glob patterns (e.g., <c>*.sln</c>).
    /// When empty, defaults to <c>*.sln</c>, <c>*.slnx</c>, and <c>.git</c>.
    /// </param>
    /// <returns>
    /// A <see cref="RepositoryRoot"/> instance representing the located repository root directory.
    /// </returns>
    /// <exception cref="DirectoryNotFoundException">
    /// Thrown when no repository root can be found by traversing up from the current directory.
    /// </exception>
    /// <remarks>
    /// <list type="bullet">
    ///     <item><description>Searches upward from <see cref="FullPath.CurrentDirectory"/> toward the file system root.</description></item>
    ///     <item><description>Glob patterns (containing <c>*</c>) are matched against files in each directory.</description></item>
    ///     <item><description>Non-glob markers are checked as both files and directories.</description></item>
    ///     <item><description>The first directory containing any matching marker is returned.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Use default markers (*.sln, *.slnx, .git)
    /// var root = RepositoryRoot.Locate();
    ///
    /// // Use custom markers
    /// var root = RepositoryRoot.Locate("package.json", ".git");
    /// </code>
    /// </example>
    /// <seealso cref="FullPath"/>
    public static RepositoryRoot Locate(params string[] markerFiles)
    {
        if (markerFiles.Length == 0)
            markerFiles = ["*.sln", "*.slnx", ".git"];

        var directory = FullPath.CurrentDirectory();
        while (true)
        {
            foreach (var marker in markerFiles)
            {
                if (marker.Contains('*'))
                {
                    if (Directory.GetFiles(directory, marker).Length > 0)
                        return new RepositoryRoot(directory);
                }
                else
                {
                    var markerPath = directory / marker;
                    if (File.Exists(markerPath) || Directory.Exists(markerPath))
                        return new RepositoryRoot(directory);
                }
            }

            var parent = directory.Parent;
            if (parent == directory)
                throw new DirectoryNotFoundException("Repository root not found. Searched for: " + string.Join(", ", markerFiles));
            directory = parent;
        }
    }

    /// <summary>
    /// Implicitly converts a <see cref="RepositoryRoot"/> to a <see cref="FullPath"/>.
    /// </summary>
    /// <param name="root">The repository root to convert.</param>
    /// <returns>The <see cref="FullPath"/> of the repository root.</returns>
    public static implicit operator FullPath(RepositoryRoot root)
    {
        return root.FullPath;
    }

    /// <summary>
    /// Implicitly converts a <see cref="RepositoryRoot"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="root">The repository root to convert.</param>
    /// <returns>The string representation of the repository root path.</returns>
    public static implicit operator string(RepositoryRoot root)
    {
        return root.FullPath;
    }
}
