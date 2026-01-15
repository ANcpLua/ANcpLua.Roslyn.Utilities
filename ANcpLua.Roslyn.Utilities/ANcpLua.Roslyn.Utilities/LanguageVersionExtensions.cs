using Microsoft.CodeAnalysis.CSharp;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="LanguageVersion" /> that provide version comparison utilities.
/// </summary>
/// <remarks>
///     <para>
///         These extensions allow source generators and analyzers to determine the minimum C# language
///         version being used in a compilation, enabling feature-gated code generation.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Uses numeric comparison against internal language version values for forward compatibility.</description>
///         </item>
///         <item>
///             <description>Each method returns <c>true</c> if the version is at least the specified version or higher.</description>
///         </item>
///         <item>
///             <description>
///                 Works with both explicit versions and <see cref="LanguageVersion.Latest" /> /
///                 <see cref="LanguageVersion.Preview" />.
///             </description>
///         </item>
///     </list>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class LanguageVersionExtensions
{
    /// <summary>
    ///     Determines whether the specified language version is C# 10.0 or above.
    /// </summary>
    /// <param name="languageVersion">The language version to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="languageVersion" /> is C# 10.0 or a later version; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     C# 10.0 introduced features such as file-scoped namespaces, global using directives,
    ///     record structs, and extended property patterns.
    /// </remarks>
    /// <seealso cref="IsCSharp11OrAbove" />
    /// <seealso cref="IsCSharp12OrAbove" />
    public static bool IsCSharp10OrAbove(this LanguageVersion languageVersion)
    {
        return languageVersion >= (LanguageVersion)1000;
    }

    /// <summary>
    ///     Determines whether the specified language version is C# 11.0 or above.
    /// </summary>
    /// <param name="languageVersion">The language version to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="languageVersion" /> is C# 11.0 or a later version; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     C# 11.0 introduced features such as raw string literals, required members,
    ///     generic attributes, and file-scoped types.
    /// </remarks>
    /// <seealso cref="IsCSharp10OrAbove" />
    /// <seealso cref="IsCSharp12OrAbove" />
    public static bool IsCSharp11OrAbove(this LanguageVersion languageVersion)
    {
        return languageVersion >= (LanguageVersion)1100;
    }

    /// <summary>
    ///     Determines whether the specified language version is C# 12.0 or above.
    /// </summary>
    /// <param name="languageVersion">The language version to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="languageVersion" /> is C# 12.0 or a later version; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     C# 12.0 introduced features such as primary constructors for classes and structs,
    ///     collection expressions, and inline arrays.
    /// </remarks>
    /// <seealso cref="IsCSharp11OrAbove" />
    /// <seealso cref="IsCSharp13OrAbove" />
    public static bool IsCSharp12OrAbove(this LanguageVersion languageVersion)
    {
        return languageVersion >= (LanguageVersion)1200;
    }

    /// <summary>
    ///     Determines whether the specified language version is C# 13.0 or above.
    /// </summary>
    /// <param name="languageVersion">The language version to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="languageVersion" /> is C# 13.0 or a later version; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     C# 13.0 introduced features such as params collections, new lock object,
    ///     and new escape sequence for escape character.
    /// </remarks>
    /// <seealso cref="IsCSharp12OrAbove" />
    /// <seealso cref="IsCSharp14OrAbove" />
    public static bool IsCSharp13OrAbove(this LanguageVersion languageVersion)
    {
        return languageVersion >= (LanguageVersion)1300;
    }

    /// <summary>
    ///     Determines whether the specified language version is C# 14.0 or above.
    /// </summary>
    /// <param name="languageVersion">The language version to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="languageVersion" /> is C# 14.0 or a later version; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     C# 14.0 is associated with .NET 10 and includes new language features
    ///     targeting that runtime.
    /// </remarks>
    /// <seealso cref="IsCSharp13OrAbove" />
    public static bool IsCSharp14OrAbove(this LanguageVersion languageVersion)
    {
        return languageVersion >= (LanguageVersion)1400;
    }
}