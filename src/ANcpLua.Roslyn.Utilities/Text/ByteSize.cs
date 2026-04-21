namespace ANcpLua.Roslyn.Utilities.Text;

/// <summary>Human-readable byte counts — IEC binary (<c>1 KiB = 1024</c>), decimal output, no locale-specific formatting.</summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class ByteSize
{
    private static readonly string[] Suffixes = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];

    /// <summary>
    ///     Formats <paramref name="bytes" /> as e.g. <c>"256.50 MB"</c>. Negative values get a leading minus sign.
    ///     Values below 1 KiB are shown as whole bytes (no decimals).
    /// </summary>
    public static string Humanize(long bytes, int decimals = 2)
    {
        if (decimals < 0) throw new ArgumentOutOfRangeException(nameof(decimals), decimals, "Must be non-negative.");
        if (bytes == 0) return "0 B";
        var negative = bytes < 0;
        var abs = negative ? -(double)bytes : bytes;
        var suffixIndex = 0;
        while (abs >= 1024 && suffixIndex < Suffixes.Length - 1)
        {
            abs /= 1024;
            suffixIndex++;
        }
        var rendered = suffixIndex == 0
            ? abs.ToString("F0", CultureInfo.InvariantCulture)
            : abs.ToString("F" + decimals, CultureInfo.InvariantCulture);
        return (negative ? "-" : string.Empty) + rendered + " " + Suffixes[suffixIndex];
    }

    /// <summary>
    ///     <paramref name="bytes" /> expressed in mebibytes (IEC: <c>1 MiB = 1024×1024</c>), rounded to
    ///     <paramref name="decimals" /> fractional digits. For dashboard/health JSON payloads that want a raw
    ///     numeric rather than the <see cref="Humanize" /> string.
    /// </summary>
    public static double Megabytes(long bytes, int decimals = 2)
    {
        if (decimals < 0) throw new ArgumentOutOfRangeException(nameof(decimals), decimals, "Must be non-negative.");
        return Math.Round(bytes / (1024.0 * 1024.0), decimals);
    }

    /// <summary><paramref name="bytes" /> in gibibytes (IEC: <c>1 GiB = 1024³</c>), rounded to <paramref name="decimals" /> digits.</summary>
    public static double Gigabytes(long bytes, int decimals = 2)
    {
        if (decimals < 0) throw new ArgumentOutOfRangeException(nameof(decimals), decimals, "Must be non-negative.");
        return Math.Round(bytes / (1024.0 * 1024.0 * 1024.0), decimals);
    }
}
