namespace ANcpLua.Roslyn.Utilities.Text;

/// <summary>
///     Environment-variable readers with type-safe defaults — collapses the <c>GetEnvironmentVariable + TryParse + fallback</c>
///     dance that otherwise repeats on every <c>QYL_*</c> / <c>OTEL_*</c> / <c>PORT</c> lookup.
/// </summary>
/// <remarks>
///     Empty and whitespace-only values are treated as unset (the supplied default is returned).
///     Reads go straight to <see cref="Environment.GetEnvironmentVariable(string)" /> — no caching, no
///     process-wide mutation, safe to call from DI factories.
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class EnvConfig
{
    /// <summary>Trimmed string value of <paramref name="name" />, or <paramref name="defaultValue" /> when unset/whitespace.</summary>
    public static string? ReadString(string name, string? defaultValue = null)
    {
        if (name is null) throw new ArgumentNullException(nameof(name));
        var raw = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(raw) ? defaultValue : raw!.Trim();
    }

    /// <summary>
    ///     Parses <paramref name="name" /> as <see cref="int" />. Returns <paramref name="defaultValue" /> when unset,
    ///     malformed, or outside <paramref name="min" />/<paramref name="max" /> (when either bound is supplied).
    /// </summary>
    public static int ReadInt(string name, int defaultValue, int? min = null, int? max = null)
    {
        var raw = ReadString(name);
        if (raw is null || !int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return defaultValue;
        if (min is int lo && parsed < lo) return defaultValue;
        if (max is int hi && parsed > hi) return defaultValue;
        return parsed;
    }

    /// <summary>Parses <paramref name="name" /> as <see cref="double" />. Returns <paramref name="defaultValue" /> when unset or malformed.</summary>
    public static double ReadDouble(string name, double defaultValue)
    {
        var raw = ReadString(name);
        return raw is not null && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : defaultValue;
    }

    /// <summary>
    ///     Parses <paramref name="name" /> as <see cref="bool" />, accepting <c>true/false/1/0/yes/no/on/off</c> case-insensitively.
    ///     Returns <paramref name="defaultValue" /> for anything else.
    /// </summary>
    public static bool ReadBool(string name, bool defaultValue)
    {
        var raw = ReadString(name);
        if (raw is null) return defaultValue;
        if (bool.TryParse(raw, out var parsed)) return parsed;
        return raw.ToUpperInvariant() switch
        {
            "1" or "YES" or "ON" or "Y" => true,
            "0" or "NO" or "OFF" or "N" => false,
            _ => defaultValue
        };
    }

    /// <summary>Parses <paramref name="name" /> as <typeparamref name="TEnum" /> case-insensitively. Returns <paramref name="defaultValue" /> on miss.</summary>
    public static TEnum ReadEnum<TEnum>(string name, TEnum defaultValue) where TEnum : struct, Enum
    {
        var raw = ReadString(name);
        return raw is not null && Enum.TryParse<TEnum>(raw, ignoreCase: true, out var parsed)
            ? parsed
            : defaultValue;
    }

    /// <summary>Parses <paramref name="name" /> as an absolute <see cref="Uri" />. Returns <paramref name="defaultValue" /> on miss.</summary>
    public static Uri? ReadUri(string name, Uri? defaultValue = null)
    {
        var raw = ReadString(name);
        return raw is not null && Uri.TryCreate(raw, UriKind.Absolute, out var parsed) ? parsed : defaultValue;
    }
}
