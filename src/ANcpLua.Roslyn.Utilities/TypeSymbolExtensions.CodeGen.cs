using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class TypeSymbolExtensions
{
    /// <summary>
    ///     Gets the generic type parameter clause for code generation.
    /// </summary>
    /// <param name="type">The named type symbol to get the generic parameter clause from.</param>
    /// <returns>
    ///     The generic clause string (e.g., <c>"&lt;T, U&gt;"</c>), or <c>null</c> if the type is not generic.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method formats type parameters as a C# generic clause suitable for
    ///         emitting in generated source code. It returns only the parameter names,
    ///         not their constraints.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     // For class MyExecutor&lt;T, U&gt;:
    ///     var clause = type.GetGenericParameterClause();
    ///     // clause == "&lt;T, U&gt;"
    ///
    ///     // Use in code generation:
    ///     sb.AppendLine($"partial class {type.Name}{clause} {{");
    ///     </code>
    /// </example>
    /// <seealso cref="SymbolExtensions.GetTypeParameters" />
    public static string? GetGenericParameterClause(this INamedTypeSymbol type)
    {
        // Not IsGenericType: that is true when any *containing* type has type parameters,
        // which would produce a bogus "<>" clause for non-generic nested types.
        return type.TypeParameters.Length > 0
            ? $"<{string.Join(", ", type.TypeParameters.Select(static p => p.Name))}>"
            : null;
    }
}
