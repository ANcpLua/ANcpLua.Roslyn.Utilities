using System;

namespace ANcpLua.Roslyn.Utilities.Testing.Aot;

/// <summary>
/// Marks code as verified trim-compatible.
/// </summary>
/// <remarks>
/// Analyzer AL0032 verifies that code marked with this attribute contains no RequiresUnreferencedCode violations.
/// Code must not use unconstrained reflection patterns that are incompatible with trimming.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TrimSafeAttribute : Attribute
{
}
