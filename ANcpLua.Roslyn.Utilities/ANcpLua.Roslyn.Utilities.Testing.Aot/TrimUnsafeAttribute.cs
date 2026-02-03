using System;

namespace ANcpLua.Roslyn.Utilities.Testing.Aot;

/// <summary>
/// Marks code as intentionally NOT trim-compatible.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute to explicitly document that code cannot work in trimmed applications
/// due to its reliance on reflection or dynamic type loading.
/// </para>
/// <para>
/// This attribute serves multiple purposes:
/// <list type="bullet">
///   <item>Documents the design decision that this code requires full metadata</item>
///   <item>Prevents <c>[TrimSafe]</c> code from calling this code</item>
///   <item>Analyzer warns if this attribute is applied unnecessarily</item>
/// </list>
/// </para>
/// <para>
/// Common reasons to use <c>[TrimUnsafe]</c>:
/// <list type="bullet">
///   <item>Reflection over types not known at compile time</item>
///   <item>Dynamic JSON deserialization without source generation</item>
///   <item>Dependency injection with runtime type discovery</item>
///   <item>Plugin loading via Assembly.Load</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [TrimUnsafe("Deserializes arbitrary user-defined OTLP attributes")]
/// private static Dictionary&lt;string, object?&gt; ParseAttributes(string? json)
/// {
///     return JsonSerializer.Deserialize&lt;Dictionary&lt;string, object?&gt;&gt;(json);
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
public sealed class TrimUnsafeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TrimUnsafeAttribute"/> class.
    /// </summary>
    public TrimUnsafeAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TrimUnsafeAttribute"/> class with a reason.
    /// </summary>
    /// <param name="reason">The reason why this code is not trim-compatible.</param>
    public TrimUnsafeAttribute(string reason) => Reason = reason;

    /// <summary>
    /// Gets the reason why this code is not trim-compatible.
    /// </summary>
    public string? Reason { get; }
}
