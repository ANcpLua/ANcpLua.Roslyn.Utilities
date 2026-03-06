namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     Represents a generated file with a name and text content.
/// </summary>
/// <remarks>
///     <para>
///         This record struct is used to pair a hint name with the corresponding source text
///         for use with Roslyn incremental source generators.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Immutable value type with value-based equality semantics.</description>
///         </item>
///         <item>
///             <description>Use <see cref="Empty" /> to obtain a sentinel value representing no file.</description>
///         </item>
///         <item>
///             <description>Check <see cref="IsEmpty" /> before adding to source output to avoid empty files.</description>
///         </item>
///     </list>
/// </remarks>
/// <param name="Name">
///     The hint name for the generated file. This name is used by Roslyn to identify the generated source
///     and should typically include a file extension (e.g., <c>"MyType.g.cs"</c>).
/// </param>
/// <param name="Text">
///     The source text content of the generated file. This should contain valid C# source code
///     that will be added to the compilation.
/// </param>
/// <seealso cref="EquatableArray{T}" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    readonly record struct FileWithName(
        string Name,
        string Text)
{
    /// <summary>
    ///     Gets an empty file instance.
    /// </summary>
    /// <remarks>
    ///     This property returns a sentinel value where both <see cref="Name" /> and <see cref="Text" />
    ///     are <see cref="string.Empty" />. Use this when a generator pipeline needs to return
    ///     a non-null value but has no file to produce.
    /// </remarks>
    /// <returns>A <see cref="FileWithName" /> with empty name and text.</returns>
    public static FileWithName Empty => new(
        string.Empty,
        string.Empty);

    /// <summary>
    ///     Gets a value indicating whether this file is empty.
    /// </summary>
    /// <remarks>
    ///     A file is considered empty if either <see cref="Name" /> or <see cref="Text" /> is
    ///     <c>null</c> or an empty string. Check this property before adding the file to
    ///     source output to avoid generating empty or invalid source files.
    /// </remarks>
    /// <returns>
    ///     <see langword="true" /> if <see cref="Name" /> or <see cref="Text" /> is <c>null</c> or empty;
    ///     otherwise, <see langword="false" />.
    /// </returns>
    public bool IsEmpty => string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Text);
}