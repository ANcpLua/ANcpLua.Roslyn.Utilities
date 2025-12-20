namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     Represents a generated file with a name and text content.
/// </summary>
/// <param name="Name">The hint name for the generated file.</param>
/// <param name="Text">The source text content.</param>
public readonly record struct FileWithName(
    string Name,
    string Text)
{
    /// <summary>
    ///     Gets an empty file instance.
    /// </summary>
    public static FileWithName Empty => new(
        string.Empty,
        string.Empty);

    /// <summary>
    ///     Gets a value indicating whether this file is empty.
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Text);
}