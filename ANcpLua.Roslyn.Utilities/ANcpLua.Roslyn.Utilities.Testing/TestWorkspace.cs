using System.Runtime.CompilerServices;
using Meziantou.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     A temporary workspace for file-based generator testing.
///     Wraps <see cref="TemporaryDirectory" /> with generator-specific helpers.
/// </summary>
public sealed class TestWorkspace : IDisposable, IAsyncDisposable
{
    private readonly TemporaryDirectory _tempDir;

    private TestWorkspace(TemporaryDirectory tempDir) => _tempDir = tempDir;

    /// <summary>
    ///     Gets the root path of the workspace.
    /// </summary>
    public FullPath Root => _tempDir.FullPath;

    /// <summary>
    ///     Creates a new temporary test workspace.
    /// </summary>
    public static TestWorkspace Create() => new(TemporaryDirectory.Create());

    /// <summary>
    ///     Combines the workspace root with a relative path.
    /// </summary>
    public FullPath this[string relativePath] => _tempDir.GetFullPath(relativePath);

    /// <summary>
    ///     Writes a source file to the workspace.
    /// </summary>
    public FullPath WriteSource(string relativePath, string content)
    {
        var path = _tempDir.CreateTextFile(relativePath, content);
        return path;
    }

    /// <summary>
    ///     Writes multiple source files to the workspace.
    /// </summary>
    public TestWorkspace WithSources(params (string Path, string Content)[] files)
    {
        foreach (var (path, content) in files)
            WriteSource(path, content);
        return this;
    }

    /// <summary>
    ///     Reads a file from the workspace.
    /// </summary>
    public string ReadFile(string relativePath) =>
        File.ReadAllText(_tempDir.GetFullPath(relativePath));

    /// <summary>
    ///     Reads a file from the workspace asynchronously.
    /// </summary>
    public Task<string> ReadFileAsync(string relativePath, CancellationToken ct = default) =>
        File.ReadAllTextAsync(_tempDir.GetFullPath(relativePath), ct);

    /// <summary>
    ///     Checks if a file exists in the workspace.
    /// </summary>
    public bool FileExists(string relativePath) =>
        File.Exists(_tempDir.GetFullPath(relativePath));

    /// <summary>
    ///     Creates a directory in the workspace.
    /// </summary>
    public FullPath CreateDirectory(string relativePath) =>
        _tempDir.CreateDirectory(relativePath);

    /// <summary>
    ///     Gets all source files in the workspace.
    /// </summary>
    public IEnumerable<(FullPath Path, string Content)> GetSourceFiles()
    {
        foreach (var file in Directory.EnumerateFiles(Root, "*.cs", SearchOption.AllDirectories))
        {
            var path = FullPath.FromPath(file);
            yield return (path, File.ReadAllText(file));
        }
    }

    /// <summary>
    ///     Gets all source files in the workspace asynchronously.
    /// </summary>
    public async IAsyncEnumerable<(FullPath Path, string Content)> GetSourceFilesAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var file in Directory.EnumerateFiles(Root, "*.cs", SearchOption.AllDirectories))
        {
            var path = FullPath.FromPath(file);
            var content = await File.ReadAllTextAsync(file, ct);
            yield return (path, content);
        }
    }

    /// <summary>
    ///     Creates additional texts from files matching a pattern.
    /// </summary>
    public IEnumerable<AdditionalText> GetAdditionalTexts(string pattern = "*.*")
    {
        foreach (var file in Directory.EnumerateFiles(Root, pattern, SearchOption.AllDirectories))
        {
            if (!file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                yield return new FileAdditionalText(FullPath.FromPath(file));
        }
    }

    /// <summary>
    ///     Writes generated output to the workspace for snapshot comparison.
    /// </summary>
    public FullPath WriteGeneratedOutput(string hintName, string content) =>
        WriteSource($"Generated/{hintName}", content);

    /// <summary>
    ///     Compares a generated file with expected content.
    /// </summary>
    public bool VerifyGeneratedContent(string hintName, string expectedContent, bool normalizeNewlines = true)
    {
        var path = _tempDir.GetFullPath($"Generated/{hintName}");
        if (!File.Exists(path))
            return false;

        var actual = File.ReadAllText(path);
        if (normalizeNewlines)
        {
            actual = TextUtilities.NormalizeNewlines(actual);
            expectedContent = TextUtilities.NormalizeNewlines(expectedContent);
        }

        return string.Equals(actual, expectedContent, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public void Dispose() => _tempDir.Dispose();

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _tempDir.DisposeAsync();

    /// <summary>
    ///     Path operator for fluent workspace navigation.
    /// </summary>
    public static FullPath operator /(TestWorkspace workspace, string path) =>
        workspace._tempDir.GetFullPath(path);

    /// <summary>
    ///     Implicit conversion to <see cref="FullPath" />.
    /// </summary>
    public static implicit operator FullPath(TestWorkspace workspace) => workspace.Root;

    /// <summary>
    ///     Implicit conversion to string path.
    /// </summary>
    public static implicit operator string(TestWorkspace workspace) => workspace.Root;

    private sealed class FileAdditionalText : AdditionalText
    {
        private readonly FullPath _path;

        public FileAdditionalText(FullPath path) => _path = path;

        public override string Path => _path;

        public override SourceText? GetText(CancellationToken ct = default)
        {
            using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return SourceText.From(stream);
        }
    }
}
