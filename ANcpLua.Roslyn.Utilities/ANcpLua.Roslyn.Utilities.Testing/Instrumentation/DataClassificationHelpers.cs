// =============================================================================
// Data Classification Helpers - Production utilities for log redaction
// Custom data classifications and redactors for compliance
// =============================================================================

using System.Security.Cryptography;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;

namespace ANcpLua.Roslyn.Utilities.Instrumentation;

/// <summary>
///     Common data classifications for structured logging redaction.
/// </summary>
public static class DataClassifications
{
    /// <summary>
    ///     Personally Identifiable Information - will be fully erased.
    ///     Examples: email, phone, names, addresses.
    /// </summary>
    public static DataClassification Pii => new("PII", "GDPR:PersonalData");

    /// <summary>
    ///     Secret/credential data - will be hashed for correlation.
    ///     Examples: API keys, tokens, passwords.
    /// </summary>
    public static DataClassification Secret => new("Secret", "Security:Credential");

    /// <summary>
    ///     Internal identifiers - safe to log internally.
    ///     Examples: user IDs, session IDs, trace IDs.
    /// </summary>
    public static DataClassification InternalId => new("InternalId", "Internal:Identifier");

    /// <summary>
    ///     User-generated content - may contain sensitive data, truncated.
    ///     Examples: prompts, messages, form inputs.
    /// </summary>
    public static DataClassification UserContent => new("UserContent", "Content:UserGenerated");

    /// <summary>
    ///     AI-generated content - typically safe but may be long.
    ///     Examples: completions, responses, summaries.
    /// </summary>
    public static DataClassification AiContent => new("AiContent", "Content:AiGenerated");
}

/// <summary>
///     Attributes for marking properties with data classifications.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class PiiDataAttribute : DataClassificationAttribute
{
    public PiiDataAttribute() : base(DataClassifications.Pii)
    {
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class SecretDataAttribute : DataClassificationAttribute
{
    public SecretDataAttribute() : base(DataClassifications.Secret)
    {
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class InternalIdAttribute : DataClassificationAttribute
{
    public InternalIdAttribute() : base(DataClassifications.InternalId)
    {
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class UserContentAttribute : DataClassificationAttribute
{
    public UserContentAttribute() : base(DataClassifications.UserContent)
    {
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class AiContentAttribute : DataClassificationAttribute
{
    public AiContentAttribute() : base(DataClassifications.AiContent)
    {
    }
}

/// <summary>
///     Redactor that truncates long content with a suffix.
/// </summary>
public sealed class TruncatingRedactor : Redactor
{
    private readonly int _maxLength;
    private readonly string _suffix;

    public TruncatingRedactor(int maxLength = 100, string suffix = "...[truncated]")
    {
        _maxLength = maxLength;
        _suffix = suffix;
    }

    public override int GetRedactedLength(ReadOnlySpan<char> input)
    {
        return input.Length <= _maxLength ? input.Length : _maxLength + _suffix.Length;
    }

    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        if (source.Length <= _maxLength)
        {
            source.CopyTo(destination);
            return source.Length;
        }

        source[.._maxLength].CopyTo(destination);
        _suffix.AsSpan().CopyTo(destination[_maxLength..]);
        return _maxLength + _suffix.Length;
    }
}

/// <summary>
///     Redactor that produces a stable hash for correlation without revealing the value.
///     Uses SHA256 truncated to specified length.
/// </summary>
public sealed class HashingRedactor : Redactor
{
    private readonly int _hashLength;
    private readonly string _prefix;

    public HashingRedactor(int hashLength = 16, string prefix = "hash:")
    {
        _hashLength = hashLength;
        _prefix = prefix;
    }

    public override int GetRedactedLength(ReadOnlySpan<char> input) => _prefix.Length + _hashLength;

    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        var byteCount = Encoding.UTF8.GetByteCount(source);
        Span<byte> bytes = byteCount <= 256 ? stackalloc byte[byteCount] : new byte[byteCount];
        Encoding.UTF8.GetBytes(source, bytes);
        var hash = SHA256.HashData(bytes);
        var hashString = Convert.ToHexString(hash)[.._hashLength];

        _prefix.AsSpan().CopyTo(destination);
        hashString.AsSpan().CopyTo(destination[_prefix.Length..]);

        return _prefix.Length + _hashLength;
    }
}

/// <summary>
///     Redactor that completely erases the content.
/// </summary>
public sealed class ErasingRedactor : Redactor
{
    private const string Replacement = "[REDACTED]";

    public override int GetRedactedLength(ReadOnlySpan<char> input) => Replacement.Length;

    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        Replacement.AsSpan().CopyTo(destination);
        return Replacement.Length;
    }
}

/// <summary>
///     Redactor that passes through content unchanged.
/// </summary>
public sealed class NullRedactor : Redactor
{
    public override int GetRedactedLength(ReadOnlySpan<char> input) => input.Length;

    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        source.CopyTo(destination);
        return source.Length;
    }
}

/// <summary>
///     Redactor that masks all but the last N characters.
/// </summary>
public sealed class PartialMaskRedactor : Redactor
{
    private readonly int _visibleChars;
    private readonly char _maskChar;

    public PartialMaskRedactor(int visibleChars = 4, char maskChar = '*')
    {
        _visibleChars = visibleChars;
        _maskChar = maskChar;
    }

    public override int GetRedactedLength(ReadOnlySpan<char> input) => input.Length;

    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        if (source.Length <= _visibleChars)
        {
            source.CopyTo(destination);
            return source.Length;
        }

        var maskCount = source.Length - _visibleChars;
        destination[..maskCount].Fill(_maskChar);
        source[maskCount..].CopyTo(destination[maskCount..]);

        return source.Length;
    }
}

/// <summary>
///     Extension methods for configuring redaction.
/// </summary>
public static class RedactionExtensions
{
    /// <summary>
    ///     Configures standard redactors for common data classifications.
    /// </summary>
    public static IRedactionBuilder AddStandardRedactors(this IRedactionBuilder builder)
    {
        // PII: Completely erase
        builder.SetRedactor<ErasingRedactor>(DataClassifications.Pii);

        // Secrets: Hash for correlation
        builder.SetRedactor<HashingRedactor>(DataClassifications.Secret);

        // Internal IDs: Pass through (safe internally)
        builder.SetRedactor<NullRedactor>(DataClassifications.InternalId);

        // User content: Truncate long content
        builder.SetRedactor<TruncatingRedactor>(DataClassifications.UserContent);

        // AI content: Truncate (usually longer) - use the same redactor type
        builder.SetRedactor<TruncatingRedactor>(DataClassifications.AiContent);

        return builder;
    }
}
