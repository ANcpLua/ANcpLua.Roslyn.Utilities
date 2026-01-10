using Meziantou.Framework;

namespace ANcpLua.Roslyn.Utilities.Testing.MSBuild;

/// <summary>
/// Locates the repository root directory for integration testing.
/// </summary>
public readonly record struct RepositoryRoot
{
    private RepositoryRoot(FullPath path)
    {
        FullPath = path;
    }

    /// <summary>Gets the full path to the repository root.</summary>
    public FullPath FullPath { get; }

    /// <summary>Gets a path relative to the repository root.</summary>
    public FullPath this[string relativePath] => FullPath / relativePath;

    /// <summary>
    /// Locates the repository root by searching for common marker files.
    /// </summary>
    /// <param name="markerFiles">Files that indicate the repository root (e.g., "*.sln", ".git").</param>
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

    /// <summary>Implicit conversion to FullPath.</summary>
    public static implicit operator FullPath(RepositoryRoot root)
    {
        return root.FullPath;
    }

    /// <summary>Implicit conversion to string.</summary>
    public static implicit operator string(RepositoryRoot root)
    {
        return root.FullPath;
    }
}
