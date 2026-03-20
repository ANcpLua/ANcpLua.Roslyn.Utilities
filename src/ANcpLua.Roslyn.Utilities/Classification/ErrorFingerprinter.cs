using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Computes stable fingerprints for error grouping by normalizing exception details
///     and producing a truncated SHA256 hash.
/// </summary>
/// <remarks>
///     <para>
///         The fingerprinter normalizes stack traces (removing line numbers and file paths),
///         messages (replacing GUIDs, URLs, and large numbers with placeholders), and optionally
///         incorporates GenAI-specific dimensions (operation, provider, model, finish reason, category)
///         to produce a deterministic 64-bit fingerprint suitable for error aggregation.
///     </para>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class ErrorFingerprinter
{
    private static readonly Regex s_lineNumberRegex =
        new(@" in [^\s]+:\s*line \d+", RegexOptions.Compiled);

    private static readonly Regex s_filePathRegex =
        new(@" in [/\\][^\s]+\.(cs|fs|vb)", RegexOptions.Compiled);

    private static readonly Regex s_guidRegex =
        new(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.Compiled);

    private static readonly Regex s_standaloneNumberRegex =
        new(@"(?<![a-zA-Z])\d{5,}(?![a-zA-Z])", RegexOptions.Compiled);

    private static readonly Regex s_urlRegex =
        new(@"https?://[^\s]+", RegexOptions.Compiled);

    /// <summary>
    ///     Computes a stable 64-bit (16-hex-char) fingerprint for the given error details.
    /// </summary>
    /// <param name="exceptionType">The fully-qualified exception type name.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="stackTrace">The exception stack trace, or <c>null</c>.</param>
    /// <param name="genAiOperation">The GenAI operation name, or <c>null</c>.</param>
    /// <param name="genAiProvider">The GenAI provider name, or <c>null</c>.</param>
    /// <param name="genAiModel">The GenAI model name, or <c>null</c>.</param>
    /// <param name="finishReason">The GenAI finish reason, or <c>null</c>.</param>
    /// <param name="category">
    ///     The error category (e.g. <c>"rate_limit"</c>, <c>"content_filter"</c>, <c>"token_limit"</c>).
    ///     When provided, GenAI-aware grouping dimensions are applied.
    /// </param>
    /// <returns>A 16-character lowercase hexadecimal fingerprint string.</returns>
    public static string Compute(
        string exceptionType,
        string message,
        string? stackTrace,
        string? genAiOperation = null,
        string? genAiProvider = null,
        string? genAiModel = null,
        string? finishReason = null,
        string? category = null)
    {
        var normalizedStack = NormalizeStackTrace(stackTrace);
        var normalizedMessage = NormalizeMessage(message);

        var input = $"{exceptionType}\n{normalizedMessage}\n{normalizedStack}";
        if (!string.IsNullOrEmpty(genAiOperation))
            input = $"{input}\n{genAiOperation}";

        // GenAI-aware grouping: add dimensions based on error category
        if (!string.IsNullOrEmpty(category))
        {
            switch (category)
            {
                case "rate_limit" when !string.IsNullOrEmpty(genAiProvider):
                    // Group rate limit errors by provider (same provider = same fingerprint)
                    input = $"rate_limit\n{genAiProvider}";
                    break;
                case "content_filter" when !string.IsNullOrEmpty(genAiModel):
                    // Group content filter errors by model
                    input = $"content_filter\n{genAiModel}";
                    break;
                case "token_limit" when !string.IsNullOrEmpty(genAiModel):
                    // Group token limit errors by model
                    input = $"token_limit\n{genAiModel}";
                    break;
                default:
                    // For other GenAI errors, include provider and finish reason as dimensions
                    if (!string.IsNullOrEmpty(genAiProvider))
                        input = $"{input}\n{genAiProvider}";
                    if (!string.IsNullOrEmpty(finishReason))
                        input = $"{input}\n{finishReason}";
                    break;
            }
        }

        var bytes = Encoding.UTF8.GetBytes(input);

#if NET5_0_OR_GREATER
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).Substring(0, 16).ToLowerInvariant();
#else
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hash, 0, 8).Replace("-", "").ToLowerInvariant();
#endif
    }

    /// <summary>
    ///     Normalizes a stack trace by removing line numbers and file paths
    ///     so that recompilation does not change the fingerprint.
    /// </summary>
    private static string NormalizeStackTrace(string? stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace)) return "";
        var result = s_lineNumberRegex.Replace(stackTrace, "");
        result = s_filePathRegex.Replace(result, "");
        return result.Trim();
    }

    /// <summary>
    ///     Normalizes an exception message by replacing volatile tokens
    ///     (GUIDs, large numbers, URLs) with stable placeholders.
    /// </summary>
    private static string NormalizeMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return "";
        var result = s_guidRegex.Replace(message, "<GUID>");
        result = s_standaloneNumberRegex.Replace(result, "<N>");
        result = s_urlRegex.Replace(result, "<URL>");
        return result;
    }
}
