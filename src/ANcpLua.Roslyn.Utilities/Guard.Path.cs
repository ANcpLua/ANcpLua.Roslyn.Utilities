using System.IO;
using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class Guard
{
    private static readonly char[] s_invalidFileNameChars = Path.GetInvalidFileNameChars();

    private static readonly char[] s_invalidPathChars = Path
        .GetInvalidPathChars()
        .Concat(s_invalidFileNameChars.Except(['/', '\\', ':']))
        .Distinct()
        .ToArray();

    /// <summary>
    ///     Validates that a file exists at the specified path and returns the path.
    /// </summary>
    /// <param name="path">The file path to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is empty or the file does not exist.</exception>
    /// <example>
    ///     <code>
    /// public void LoadConfig(string path)
    /// {
    ///     var validPath = Guard.FileExists(path);
    ///     var content = File.ReadAllText(validPath);
    /// }
    /// </code>
    /// </example>
    public static string FileExists(
        [NotNull] string? path,
        [CallerArgumentExpression(nameof(path))]
        string? paramName = null)
    {
        NotNullOrEmpty(path, paramName);
#pragma warning disable RS1035 // File I/O is valid for non-analyzer callers (testing, CLI tools)
        return File.Exists(path)
#pragma warning restore RS1035
            ? path
            : throw new ArgumentException($"File not found: {path}", paramName);
    }

    /// <summary>
    ///     Validates that a directory exists at the specified path and returns the path.
    /// </summary>
    /// <param name="path">The directory path to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is empty or the directory does not exist.</exception>
    /// <example>
    ///     <code>
    /// public void ProcessDirectory(string path)
    /// {
    ///     var validPath = Guard.DirectoryExists(path);
    ///     foreach (var file in Directory.GetFiles(validPath)) { }
    /// }
    /// </code>
    /// </example>
    public static string DirectoryExists(
        [NotNull] string? path,
        [CallerArgumentExpression(nameof(path))]
        string? paramName = null)
    {
        NotNullOrEmpty(path, paramName);
#pragma warning disable RS1035 // File I/O is valid for non-analyzer callers (testing, CLI tools)
        return Directory.Exists(path)
#pragma warning restore RS1035
            ? path
            : throw new ArgumentException($"Directory not found: {path}", paramName);
    }

    /// <summary>
    ///     Validates that a string is a valid file name (contains no invalid characters) and returns it.
    /// </summary>
    /// <param name="value">The file name to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated file name.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is empty or contains invalid characters.</exception>
    /// <example>
    ///     <code>
    /// public void SaveFile(string fileName)
    /// {
    ///     var validName = Guard.ValidFileName(fileName);
    ///     File.WriteAllText(Path.Combine(_directory, validName), content);
    /// }
    /// </code>
    /// </example>
    public static string ValidFileName(
        [NotNull] string? value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        NotNullOrEmpty(value, paramName);

        foreach (var invalidChar in s_invalidFileNameChars)
            if (value.IndexOf(invalidChar) != -1)
                throw new ArgumentException($"Invalid character '{invalidChar}' in file name: {value}", paramName);

        return value;
    }

    /// <summary>
    ///     Validates that a string is a valid file name if not null.
    /// </summary>
    /// <param name="value">The file name to validate, or <c>null</c>.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated file name, or <c>null</c> if input was <c>null</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is empty or contains invalid characters.</exception>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ValidFileNameOrNull(
        string? value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        return value is null ? null : ValidFileName(value, paramName);
    }

    /// <summary>
    ///     Validates that a string is a valid directory/path name (contains no invalid characters) and returns it.
    /// </summary>
    /// <param name="value">The path to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is empty or contains invalid characters.</exception>
    public static string ValidPath(
        [NotNull] string? value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        NotNullOrEmpty(value, paramName);

        foreach (var invalidChar in s_invalidPathChars)
            if (value.IndexOf(invalidChar) != -1)
                throw new ArgumentException($"Invalid character '{invalidChar}' in path: {value}", paramName);

        return value;
    }

    /// <summary>
    ///     Validates that a string is a valid directory/path name if not null.
    /// </summary>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ValidPathOrNull(
        string? value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        return value is null ? null : ValidPath(value, paramName);
    }

    /// <summary>
    ///     Validates that a string is a valid file extension (no leading dot, no path separators) and returns it.
    /// </summary>
    /// <param name="value">The extension to validate (e.g., "txt", "json").</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated extension without a leading dot.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="value" /> is empty, starts with a dot, or contains path separators.
    /// </exception>
    /// <example>
    ///     <code>
    /// public void RegisterExtension(string extension)
    /// {
    ///     var valid = Guard.ValidExtension(extension);
    ///     _extensions.Add("." + valid);
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="NormalizedExtension" />
    public static string ValidExtension(
        [NotNull] string? value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        NotNullOrEmpty(value, paramName);

        if (value.StartsWith(".", StringComparison.Ordinal))
            throw new ArgumentException("Extension must not start with a period ('.').", paramName);

        if (value.Contains('\\', StringComparison.Ordinal) || value.Contains('/', StringComparison.Ordinal))
            throw new ArgumentException("Extension must not contain path separators.", paramName);

        return value;
    }

    /// <summary>
    ///     Validates and normalizes a file extension to include a leading dot.
    ///     Accepts both "txt" and ".txt" formats.
    /// </summary>
    /// <param name="value">The extension to normalize (e.g., "txt" or ".txt").</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The extension with a leading dot (e.g., ".txt").</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is empty or contains path separators.</exception>
    /// <example>
    ///     <code>
    /// var ext1 = Guard.NormalizedExtension("txt");   // returns ".txt"
    /// var ext2 = Guard.NormalizedExtension(".txt");  // returns ".txt"
    /// </code>
    /// </example>
    /// <seealso cref="ValidExtension" />
    public static string NormalizedExtension(
        [NotNull] string? value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        NotNullOrEmpty(value, paramName);

        if (value.Contains('\\', StringComparison.Ordinal) || value.Contains('/', StringComparison.Ordinal))
            throw new ArgumentException("Extension must not contain path separators.", paramName);

        return value.StartsWith(".", StringComparison.Ordinal) ? value : $".{value}";
    }
}
