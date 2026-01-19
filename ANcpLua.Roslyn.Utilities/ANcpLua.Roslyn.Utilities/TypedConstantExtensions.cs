// Licensed under the MIT License.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides helpers for rendering <see cref="TypedConstant" /> values as C# literals.
/// </summary>
/// <remarks>
///     <para>
///         These helpers are intended for source generators that need to emit literal values
///         directly into generated code.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Preserves the base literal formatting from Roslyn.</description>
///         </item>
///         <item>
///             <description>Adds numeric suffixes (e.g., <c>L</c>, <c>u</c>, <c>m</c>) when required.</description>
///         </item>
///         <item>
///             <description>Produces output suitable for embedding in generated source.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="TypedConstant" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class TypedConstantExtensions
{
    /// <summary>
    ///     Converts a <see cref="TypedConstant" /> to a C# literal string with numeric suffixes when needed.
    /// </summary>
    /// <param name="constant">The constant to render.</param>
    /// <returns>A C# literal string suitable for source generation.</returns>
    public static string ToCSharpStringWithPostfix(this TypedConstant constant)
    {
        var str = constant.ToCSharpString();
        return constant.Type?.SpecialType switch
        {
            SpecialType.System_Int64 => $"{str}L",
            SpecialType.System_UInt32 => $"{str}u",
            SpecialType.System_UInt64 => $"{str}uL",
            SpecialType.System_Single => $"{str}f",
            SpecialType.System_Double => $"{str}d",
            SpecialType.System_Decimal => $"{str}m",
            _ => str
        };
    }
}
