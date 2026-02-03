using System;

namespace ANcpLua.Roslyn.Utilities.Testing.Aot;

/// <summary>
/// Marks code as intentionally NOT AOT-compatible.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute to explicitly document that code cannot work in AOT-compiled applications
/// due to its reliance on reflection, dynamic code generation, or other runtime-only features.
/// </para>
/// <para>
/// This attribute serves multiple purposes:
/// <list type="bullet">
///   <item>Documents the design decision that this code requires JIT compilation</item>
///   <item>Prevents <c>[AotSafe]</c> code from calling this code (AL0052)</item>
///   <item>Analyzer AL0053 warns if this attribute is applied unnecessarily</item>
/// </list>
/// </para>
/// <para>
/// Common reasons to use <c>[AotUnsafe]</c>:
/// <list type="bullet">
///   <item>Duck-typing via reflection (Type.GetProperty, PropertyInfo.GetValue)</item>
///   <item>Dynamic JSON deserialization to Dictionary&lt;string, object?&gt;</item>
///   <item>Expression tree compilation</item>
///   <item>Reflection.Emit usage</item>
///   <item>Plugin systems using Assembly.Load</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [AotUnsafe("Duck-typing SDK responses requires runtime reflection")]
/// public static void ExtractResponse(Activity activity, object response)
/// {
///     var type = response.GetType();
///     var usageProp = type.GetProperty("Usage");
///     // ...
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
public sealed class AotUnsafeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AotUnsafeAttribute"/> class.
    /// </summary>
    public AotUnsafeAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AotUnsafeAttribute"/> class with a reason.
    /// </summary>
    /// <param name="reason">The reason why this code is not AOT-compatible.</param>
    public AotUnsafeAttribute(string reason) => Reason = reason;

    /// <summary>
    /// Gets the reason why this code is not AOT-compatible.
    /// </summary>
    public string? Reason { get; }
}
