using System;

namespace ANcpLua.Roslyn.Utilities.Testing.Aot;

/// <summary>
/// Marks code as verified AOT-compatible.
///
/// The analyzer AL0033 verifies that this attribute is only applied to code with no
/// RequiresDynamicCodeAttribute violations.
///
/// Marked code must not use runtime code generation such as:
/// - Expression.Compile()
/// - System.Reflection.Emit.DynamicMethod
/// - Other Reflection.Emit APIs
///
/// Note: AotSafe is stricter than TrimSafe, as it prevents all runtime code generation,
/// not just trimming-related issues.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class AotSafeAttribute : Attribute
{
}
