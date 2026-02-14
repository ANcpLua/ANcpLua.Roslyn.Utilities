namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for string comparison operations with explicit comparison semantics.
/// </summary>
/// <remarks>
///     <para>
///         These extensions provide readable alternatives to using <see cref="StringComparison" />
///         parameters, making code more readable and less error-prone.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <b>Ordinal comparisons:</b> <see cref="EqualsOrdinal" />, <see cref="ContainsOrdinal" />
///                 for byte-by-byte comparison.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Case-insensitive:</b> <see cref="EqualsIgnoreCase" />, <see cref="ContainsIgnoreCase" />
///                 for culture-invariant case-insensitive comparison.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Prefix/suffix checks:</b> <see cref="StartsWithOrdinal" />, <see cref="EndsWithOrdinal" />,
///                 and their case-insensitive variants.
///             </description>
///         </item>
///     </list>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class StringComparisonExtensions
{
    // ========== Equality Comparisons ==========

    /// <summary>
    ///     Determines whether this string equals another using ordinal comparison.
    /// </summary>
    /// <param name="value">The first string to compare.</param>
    /// <param name="other">The string to compare to.</param>
    /// <returns>
    ///     <c>true</c> if the strings are equal using ordinal comparison;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Ordinal comparison compares strings byte-by-byte, which is the fastest comparison
    ///         method and should be used when comparing identifiers, paths, or technical strings.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// if (methodName.EqualsOrdinal("Dispose"))
    /// {
    ///     // Handle dispose method
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="EqualsIgnoreCase" />
    public static bool EqualsOrdinal(this string? value, string? other)
        => string.Equals(value, other, StringComparison.Ordinal);

    /// <summary>
    ///     Determines whether this string equals another using case-insensitive ordinal comparison.
    /// </summary>
    /// <param name="value">The first string to compare.</param>
    /// <param name="other">The string to compare to.</param>
    /// <returns>
    ///     <c>true</c> if the strings are equal ignoring case using ordinal comparison;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Uses <see cref="StringComparison.OrdinalIgnoreCase" /> which is culture-invariant
    ///         and appropriate for comparing technical strings where case doesn't matter.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// if (extension.EqualsIgnoreCase(".cs"))
    /// {
    ///     // It's a C# file
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="EqualsOrdinal" />
    public static bool EqualsIgnoreCase(this string? value, string? other)
        => string.Equals(value, other, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Determines whether this string equals any of the specified values using ordinal comparison.
    /// </summary>
    /// <param name="value">The string to compare.</param>
    /// <param name="values">The values to compare against.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="value" /> equals any of <paramref name="values" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// if (httpMethod.EqualsAnyOrdinal("GET", "POST", "PUT"))
    /// {
    ///     // Process allowed methods
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="EqualsAnyIgnoreCase" />
    public static bool EqualsAnyOrdinal(this string? value, params string[] values)
    {
        if (value is null)
            return false;

        foreach (var v in values)
            if (string.Equals(value, v, StringComparison.Ordinal))
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether this string equals any of the specified values using case-insensitive ordinal comparison.
    /// </summary>
    /// <param name="value">The string to compare.</param>
    /// <param name="values">The values to compare against.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="value" /> equals any of <paramref name="values" /> ignoring case;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// if (fileExtension.EqualsAnyIgnoreCase(".jpg", ".jpeg", ".png", ".gif"))
    /// {
    ///     // It's an image file
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="EqualsAnyOrdinal" />
    public static bool EqualsAnyIgnoreCase(this string? value, params string[] values)
    {
        if (value is null)
            return false;

        foreach (var v in values)
            if (string.Equals(value, v, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    // ========== Contains Comparisons ==========

    /// <summary>
    ///     Determines whether this string contains the specified substring using ordinal comparison.
    /// </summary>
    /// <param name="value">The string to search in.</param>
    /// <param name="substring">The substring to search for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="value" /> contains <paramref name="substring" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Returns <c>false</c> if either <paramref name="value" /> or <paramref name="substring" /> is <c>null</c>.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// if (path.ContainsOrdinal("/api/"))
    /// {
    ///     // It's an API route
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="ContainsIgnoreCase" />
    public static bool ContainsOrdinal(this string? value, string? substring)
    {
        if (value is null || substring is null)
            return false;

        return value.Contains(substring, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Determines whether this string contains the specified substring using case-insensitive ordinal comparison.
    /// </summary>
    /// <param name="value">The string to search in.</param>
    /// <param name="substring">The substring to search for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="value" /> contains <paramref name="substring" /> ignoring case;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// if (userInput.ContainsIgnoreCase("error"))
    /// {
    ///     // User mentioned an error
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="ContainsOrdinal" />
    public static bool ContainsIgnoreCase(this string? value, string? substring)
    {
        if (value is null || substring is null)
            return false;

        return value.Contains(substring, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Determines whether this string contains any of the specified substrings using ordinal comparison.
    /// </summary>
    /// <param name="value">The string to search in.</param>
    /// <param name="substrings">The substrings to search for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="value" /> contains any of <paramref name="substrings" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// if (line.ContainsAnyOrdinal("TODO", "FIXME", "HACK"))
    /// {
    ///     // Found a code comment marker
    /// }
    /// </code>
    /// </example>
    public static bool ContainsAnyOrdinal(this string? value, params string[] substrings)
    {
        if (value is null)
            return false;

        foreach (var substring in substrings)
            if (value.Contains(substring, StringComparison.Ordinal))
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether this string contains any of the specified substrings using case-insensitive ordinal comparison.
    /// </summary>
    /// <param name="value">The string to search in.</param>
    /// <param name="substrings">The substrings to search for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="value" /> contains any of <paramref name="substrings" /> ignoring case;
    ///     otherwise, <c>false</c>.
    /// </returns>
    public static bool ContainsAnyIgnoreCase(this string? value, params string[] substrings)
    {
        if (value is null)
            return false;

        foreach (var substring in substrings)
            if (value.Contains(substring, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    // ========== StartsWith/EndsWith Comparisons ==========

    /// <summary>
    ///     Determines whether this string starts with the specified prefix using ordinal comparison.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="prefix">The prefix to check for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="value" /> starts with <paramref name="prefix" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// if (line.StartsWithOrdinal("//"))
    /// {
    ///     // It's a comment
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="StartsWithIgnoreCase" />
    public static bool StartsWithOrdinal(this string? value, string prefix)
    {
        if (value is null)
            return false;

        return value.StartsWith(prefix, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Determines whether this string starts with the specified prefix using case-insensitive ordinal comparison.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="prefix">The prefix to check for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="value" /> starts with <paramref name="prefix" /> ignoring case;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// if (url.StartsWithIgnoreCase("https://"))
    /// {
    ///     // It's a secure URL
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="StartsWithOrdinal" />
    public static bool StartsWithIgnoreCase(this string? value, string prefix)
    {
        if (value is null)
            return false;

        return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Determines whether this string ends with the specified suffix using ordinal comparison.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="suffix">The suffix to check for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="value" /> ends with <paramref name="suffix" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// if (fileName.EndsWithOrdinal(".cs"))
    /// {
    ///     // It's a C# source file
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="EndsWithIgnoreCase" />
    public static bool EndsWithOrdinal(this string? value, string suffix)
    {
        if (value is null)
            return false;

        return value.EndsWith(suffix, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Determines whether this string ends with the specified suffix using case-insensitive ordinal comparison.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="suffix">The suffix to check for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="value" /> ends with <paramref name="suffix" /> ignoring case;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// if (fileName.EndsWithIgnoreCase(".XML"))
    /// {
    ///     // It's an XML file (case doesn't matter on Windows)
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="EndsWithOrdinal" />
    public static bool EndsWithIgnoreCase(this string? value, string suffix)
    {
        if (value is null)
            return false;

        return value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Determines whether this string starts with any of the specified prefixes using ordinal comparison.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="prefixes">The prefixes to check for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="value" /> starts with any of <paramref name="prefixes" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// if (typeName.StartsWithAnyOrdinal("System.", "Microsoft."))
    /// {
    ///     // It's a framework type
    /// }
    /// </code>
    /// </example>
    public static bool StartsWithAnyOrdinal(this string? value, params string[] prefixes)
    {
        if (value is null)
            return false;

        foreach (var prefix in prefixes)
            if (value.StartsWith(prefix, StringComparison.Ordinal))
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether this string starts with any of the specified prefixes using case-insensitive ordinal comparison.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="prefixes">The prefixes to check for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="value" /> starts with any of <paramref name="prefixes" /> ignoring case;
    ///     otherwise, <c>false</c>.
    /// </returns>
    public static bool StartsWithAnyIgnoreCase(this string? value, params string[] prefixes)
    {
        if (value is null)
            return false;

        foreach (var prefix in prefixes)
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether this string ends with any of the specified suffixes using ordinal comparison.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="suffixes">The suffixes to check for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="value" /> ends with any of <paramref name="suffixes" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// if (fileName.EndsWithAnyOrdinal(".cs", ".vb", ".fs"))
    /// {
    ///     // It's a .NET source file
    /// }
    /// </code>
    /// </example>
    public static bool EndsWithAnyOrdinal(this string? value, params string[] suffixes)
    {
        if (value is null)
            return false;

        foreach (var suffix in suffixes)
            if (value.EndsWith(suffix, StringComparison.Ordinal))
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether this string ends with any of the specified suffixes using case-insensitive ordinal comparison.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="suffixes">The suffixes to check for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="value" /> ends with any of <paramref name="suffixes" /> ignoring case;
    ///     otherwise, <c>false</c>.
    /// </returns>
    public static bool EndsWithAnyIgnoreCase(this string? value, params string[] suffixes)
    {
        if (value is null)
            return false;

        foreach (var suffix in suffixes)
            if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    // ========== IndexOf Comparisons ==========

    /// <summary>
    ///     Reports the zero-based index of the first occurrence of the specified string using ordinal comparison.
    /// </summary>
    /// <param name="value">The string to search in.</param>
    /// <param name="substring">The string to search for.</param>
    /// <returns>
    ///     The zero-based index of <paramref name="substring" /> if found; otherwise, -1.
    ///     Returns -1 if <paramref name="value" /> is <c>null</c>.
    /// </returns>
    /// <seealso cref="IndexOfIgnoreCase" />
    public static int IndexOfOrdinal(this string? value, string substring)
    {
        if (value is null)
            return -1;

        return value.IndexOf(substring, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Reports the zero-based index of the first occurrence of the specified character using ordinal comparison.
    /// </summary>
    /// <param name="value">The string to search in.</param>
    /// <param name="character">The character to search for.</param>
    /// <returns>
    ///     The zero-based index of <paramref name="character" /> if found; otherwise, -1.
    ///     Returns -1 if <paramref name="value" /> is <c>null</c>.
    /// </returns>
    public static int IndexOfOrdinal(this string? value, char character)
    {
        if (value is null)
            return -1;

        return value.IndexOf(character);
    }

    /// <summary>
    ///     Reports the zero-based index of the first occurrence of the specified string using case-insensitive ordinal
    ///     comparison.
    /// </summary>
    /// <param name="value">The string to search in.</param>
    /// <param name="substring">The string to search for.</param>
    /// <returns>
    ///     The zero-based index of <paramref name="substring" /> if found; otherwise, -1.
    ///     Returns -1 if <paramref name="value" /> is <c>null</c>.
    /// </returns>
    /// <seealso cref="IndexOfOrdinal(string?, string)" />
    public static int IndexOfIgnoreCase(this string? value, string substring)
    {
        if (value is null)
            return -1;

        return value.IndexOf(substring, StringComparison.OrdinalIgnoreCase);
    }

    // ========== Replace Comparisons ==========

    /// <summary>
    ///     Replaces all occurrences of a specified string using case-insensitive comparison.
    /// </summary>
    /// <param name="value">The string to modify.</param>
    /// <param name="oldValue">The string to be replaced.</param>
    /// <param name="newValue">The string to replace all occurrences of <paramref name="oldValue" />.</param>
    /// <returns>
    ///     A new string with all occurrences of <paramref name="oldValue" /> replaced by <paramref name="newValue" />,
    ///     or the original string if <paramref name="value" /> is <c>null</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// var result = "Hello WORLD".ReplaceIgnoreCase("world", "everyone");
    /// // result: "Hello everyone"
    /// </code>
    /// </example>
    public static string? ReplaceIgnoreCase(this string? value, string oldValue, string newValue)
    {
        if (value is null)
            return null;

        if (string.IsNullOrEmpty(oldValue))
            return value;

        var sb = new StringBuilder(value.Length);
        var currentIndex = 0;

        while (currentIndex < value.Length)
        {
            var matchIndex = value.IndexOf(oldValue, currentIndex, StringComparison.OrdinalIgnoreCase);
            if (matchIndex < 0)
            {
                sb.Append(value, currentIndex, value.Length - currentIndex);
                break;
            }

            sb.Append(value, currentIndex, matchIndex - currentIndex);
            sb.Append(newValue);
            currentIndex = matchIndex + oldValue.Length;
        }

        return sb.ToString();
    }

    // ========== Null/Empty Checks ==========

    /// <summary>
    ///     Determines whether this string is <c>null</c> or empty.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns><c>true</c> if <paramref name="value" /> is <c>null</c> or empty; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     <para>
    ///         This is a more fluent way to write <see cref="string.IsNullOrEmpty" />.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// if (input.IsNullOrEmpty())
    /// {
    ///     return defaultValue;
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="IsNullOrWhiteSpace" />
    /// <seealso cref="HasValue" />
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value)
        => string.IsNullOrEmpty(value);

    /// <summary>
    ///     Determines whether this string is <c>null</c>, empty, or consists only of white-space characters.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="value" /> is <c>null</c>, empty, or consists only of white-space characters;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// if (input.IsNullOrWhiteSpace())
    /// {
    ///     return defaultValue;
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="IsNullOrEmpty" />
    /// <seealso cref="HasContent" />
    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? value)
        => string.IsNullOrWhiteSpace(value);

    /// <summary>
    ///     Determines whether this string is not <c>null</c> and not empty.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns><c>true</c> if <paramref name="value" /> has a value (not null and not empty); otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     <para>
    ///         This is the inverse of <see cref="IsNullOrEmpty" /> and provides better readability in some contexts.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// if (name.HasValue())
    /// {
    ///     greetings.Add($"Hello, {name}!");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="IsNullOrEmpty" />
    /// <seealso cref="HasContent" />
    public static bool HasValue([NotNullWhen(true)] this string? value)
        => !string.IsNullOrEmpty(value);

    /// <summary>
    ///     Determines whether this string has meaningful content (not null, not empty, not just whitespace).
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="value" /> contains at least one non-whitespace character;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This is the inverse of <see cref="IsNullOrWhiteSpace" /> and provides better readability in some contexts.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// if (description.HasContent())
    /// {
    ///     AddDescription(description);
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="IsNullOrWhiteSpace" />
    /// <seealso cref="HasValue" />
    public static bool HasContent([NotNullWhen(true)] this string? value)
        => !string.IsNullOrWhiteSpace(value);

    /// <summary>
    ///     Returns <c>null</c> if the string is empty, otherwise returns the string.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>
    ///     <c>null</c> if <paramref name="value" /> is <c>null</c> or empty;
    ///     otherwise, <paramref name="value" />.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Useful for normalizing empty strings to <c>null</c> when you want to use null-coalescing operators.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var name = input.NullIfEmpty() ?? "Default";
    /// </code>
    /// </example>
    /// <seealso cref="NullIfWhiteSpace" />
    public static string? NullIfEmpty(this string? value)
        => string.IsNullOrEmpty(value) ? null : value;

    /// <summary>
    ///     Returns <c>null</c> if the string is empty or whitespace, otherwise returns the string.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>
    ///     <c>null</c> if <paramref name="value" /> is <c>null</c>, empty, or whitespace;
    ///     otherwise, <paramref name="value" />.
    /// </returns>
    /// <example>
    ///     <code>
    /// var description = input.NullIfWhiteSpace() ?? "No description provided";
    /// </code>
    /// </example>
    /// <seealso cref="NullIfEmpty" />
    public static string? NullIfWhiteSpace(this string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    // ========== Truncation ==========

    /// <summary>
    ///     Truncates the string to the specified maximum length.
    /// </summary>
    /// <param name="value">The string to truncate.</param>
    /// <param name="maxLength">The maximum length of the resulting string.</param>
    /// <returns>
    ///     The original string if it's shorter than or equal to <paramref name="maxLength" />;
    ///     otherwise, the first <paramref name="maxLength" /> characters.
    ///     Returns <c>null</c> if <paramref name="value" /> is <c>null</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// var shortName = longName.Truncate(50);
    /// </code>
    /// </example>
    /// <seealso cref="TruncateWithEllipsis" />
    public static string? Truncate(this string? value, int maxLength)
    {
        if (value is null || value.Length <= maxLength)
            return value;

        return value[..maxLength];
    }

    /// <summary>
    ///     Truncates the string to the specified maximum length, appending an ellipsis if truncated.
    /// </summary>
    /// <param name="value">The string to truncate.</param>
    /// <param name="maxLength">The maximum length of the resulting string (including ellipsis).</param>
    /// <param name="ellipsis">The ellipsis string to append. Defaults to "...".</param>
    /// <returns>
    ///     The original string if it's shorter than or equal to <paramref name="maxLength" />;
    ///     otherwise, a truncated string with <paramref name="ellipsis" /> appended.
    ///     Returns <c>null</c> if <paramref name="value" /> is <c>null</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// var preview = description.TruncateWithEllipsis(100);
    /// // "This is a very long description that..." (100 chars total)
    ///
    /// var title = text.TruncateWithEllipsis(50, "…"); // Unicode ellipsis
    /// </code>
    /// </example>
    /// <seealso cref="Truncate" />
    public static string? TruncateWithEllipsis(this string? value, int maxLength, string ellipsis = "...")
    {
        if (value is null || value.Length <= maxLength)
            return value;

        var truncateLength = maxLength - ellipsis.Length;
        if (truncateLength <= 0)
            return ellipsis[..maxLength];

        return value[..truncateLength] + ellipsis;
    }
}
