namespace ANcpLua.Roslyn.Utilities.OTel;

/// <summary>
///     Parser for the <c>"semconv-X.Y.Z"</c> version string that subscribers and collectors exchange during
///     schema negotiation. Major-version mismatch means incompatible; minor/patch drift means compatible with
///     potential attribute renames.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class SemconvVersion
{
    /// <summary>Schema version prefix produced by the OTel tooling (<c>"semconv-"</c>).</summary>
    public const string Prefix = "semconv-";

    /// <summary>
    ///     Parses <c>"semconv-major.minor[.patch]"</c> case-insensitively. Missing patch defaults to 0.
    ///     Returns <c>false</c> for any other shape; <paramref name="major" />/<paramref name="minor" />/<paramref name="patch" /> are set to 0.
    /// </summary>
    public static bool TryParse(string? value, out int major, out int minor, out int patch)
    {
        major = minor = patch = 0;
        if (string.IsNullOrEmpty(value)) return false;
        if (!value!.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) return false;

        var parts = value.Substring(Prefix.Length).Split('.');
        if (parts.Length < 2) return false;
        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out major)) return false;
        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out minor)) return false;
        if (parts.Length >= 3 && !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out patch))
            return false;
        return true;
    }

    /// <summary>
    ///     True when <paramref name="requested" /> and <paramref name="deployed" /> share the same major version
    ///     (the subscription-negotiation contract) — both unparseable → <c>true</c> (permissive).
    /// </summary>
    public static bool IsMajorCompatible(string? requested, string? deployed)
    {
        if (!TryParse(requested, out var rMajor, out _, out _)) return true;
        if (!TryParse(deployed, out var dMajor, out _, out _)) return true;
        return rMajor == dMajor;
    }
}
