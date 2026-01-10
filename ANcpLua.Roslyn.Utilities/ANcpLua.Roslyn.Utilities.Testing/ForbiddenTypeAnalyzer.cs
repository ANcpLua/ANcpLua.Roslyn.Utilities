using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using ANcpLua.Roslyn.Utilities.Testing.Analysis;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Analyzes generator outputs for forbidden Roslyn types that should not be cached.
/// </summary>
/// <remarks>
///     <para>
///         This analyzer walks the object graph of generator pipeline outputs to detect
///         instances of Roslyn runtime types. These types contain compilation state and
///         caching them breaks incremental generation and causes memory issues.
///     </para>
///     <para>
///         Forbidden types include:
///         <list type="bullet">
///             <item>
///                 <description><see cref="ISymbol" /> and all derivatives</description>
///             </item>
///             <item>
///                 <description><see cref="Compilation" /></description>
///             </item>
///             <item>
///                 <description><see cref="SemanticModel" /></description>
///             </item>
///             <item>
///                 <description><see cref="SyntaxNode" /> and all derivatives</description>
///             </item>
///             <item>
///                 <description><see cref="SyntaxTree" /></description>
///             </item>
///             <item>
///                 <description><see cref="IOperation" /></description>
///             </item>
///         </list>
///     </para>
///     <para>
///         The analyzer uses reference equality tracking to avoid infinite loops when
///         traversing cyclic object graphs, and caps the maximum number of reported
///         violations at 256 for performance.
///     </para>
/// </remarks>
/// <seealso cref="ForbiddenTypeViolation" />
/// <seealso cref="GeneratorCachingReport" />
internal static class ForbiddenTypeAnalyzer
{
    /// <summary>
    ///     The set of Roslyn types that are forbidden in generator pipeline outputs.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Both the exact types and any types assignable to them are considered forbidden.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 These types hold references to compilation state that should not persist
    ///                 across incremental runs.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    private static readonly HashSet<Type> ForbiddenTypes =
    [
        typeof(ISymbol),
        typeof(Compilation),
        typeof(SemanticModel),
        typeof(SyntaxNode),
        typeof(SyntaxTree),
        typeof(IOperation)
    ];

    /// <summary>
    ///     Thread-safe cache of reflected fields for each analyzed type.
    /// </summary>
    /// <remarks>
    ///     Caches <see cref="FieldInfo" /> arrays to avoid repeated reflection overhead
    ///     when analyzing the same types across multiple generator runs.
    /// </remarks>
    private static readonly ConcurrentDictionary<Type, FieldInfo[]> FieldCache = new();

    /// <summary>
    ///     Analyzes a generator run result for forbidden type violations.
    /// </summary>
    /// <param name="run">The generator driver run result to analyze.</param>
    /// <returns>
    ///     A read-only list of <see cref="ForbiddenTypeViolation" /> instances representing
    ///     each forbidden type found in the generator outputs, capped at 256 violations.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Iterates through all tracked steps and their outputs from all generator results.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Uses a recursive object graph traversal to detect forbidden types in nested structures.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Employs reference equality tracking to prevent infinite loops in cyclic graphs.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Returns early once 256 violations are found to prevent performance degradation.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="ForbiddenTypeViolation" />
    public static IReadOnlyList<ForbiddenTypeViolation> AnalyzeGeneratorRun(GeneratorDriverRunResult run)
    {
        List<ForbiddenTypeViolation> violations = [];
        HashSet<object> visited = new(ReferenceEqualityComparer.Instance);

        foreach (var (stepName, steps) in run.Results.SelectMany(static r => r.TrackedSteps))
        foreach (var step in steps)
        foreach (var (output, _) in step.Outputs)
        {
            Visit(output, stepName, "Output");
            if (violations.Count >= 256) return violations;
        }

        return violations;

        void Visit(object? node, string step, string path)
        {
            if (node is null) return;

            var type = node.GetType();

            if (!type.IsValueType && !visited.Add(node)) return;

            if (IsForbiddenType(type))
            {
                violations.Add(new ForbiddenTypeViolation(step, type, path));
                return;
            }

            if (IsAllowedType(type)) return;

            if (node is IEnumerable collection and not string)
            {
                var index = 0;
                foreach (var element in collection)
                {
                    Visit(element, step, $"{path}[{index++}]");
                    if (violations.Count >= 256) return;
                }

                return;
            }

            foreach (var field in GetRelevantFields(type))
            {
                Visit(field.GetValue(node), step, $"{path}.{field.Name}");
                if (violations.Count >= 256) return;
            }
        }
    }

    /// <summary>
    ///     Gets the relevant fields for a given type, using cached reflection data.
    /// </summary>
    /// <param name="type">The type to get fields for.</param>
    /// <returns>
    ///     An enumerable of <see cref="FieldInfo" /> objects representing instance fields
    ///     that may contain forbidden types.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Retrieves both public and non-public instance fields declared on the type.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Filters out fields whose types are in the allowed list (primitives, enums, etc.).
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Results are cached in <see cref="FieldCache" /> to avoid repeated reflection.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    private static IEnumerable<FieldInfo> GetRelevantFields(Type type) =>
        FieldCache.GetOrAdd(type,
            static t => t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                    BindingFlags.DeclaredOnly).Where(static f => !IsAllowedType(f.FieldType))
                .ToArray());

    /// <summary>
    ///     Determines whether the specified type is a forbidden Roslyn type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>
    ///     <see langword="true" /> if the type is forbidden or derives from a forbidden type;
    ///     otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Checks for exact type matches in <see cref="ForbiddenTypes" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Also checks if the type is assignable to any forbidden type
    ///                 (i.e., implements or inherits from it).
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    private static bool IsForbiddenType(Type type) => ForbiddenTypes.Contains(type) ||
                                                      ForbiddenTypes.Any(forbidden => forbidden.IsAssignableFrom(type));

    /// <summary>
    ///     Determines whether the specified type is an allowed type that should not be traversed.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>
    ///     <see langword="true" /> if the type is a primitive, enum, or well-known immutable type;
    ///     otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Allowed types include primitives, enums, <see cref="string" />,
    ///                 <see cref="decimal" />, <see cref="DateTime" />, <see cref="Guid" />,
    ///                 and <see cref="TimeSpan" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Nullable wrappers of allowed types are also considered allowed.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 These types are safe to cache and do not require further graph traversal.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    private static bool IsAllowedType(Type type) =>
        type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) ||
        type == typeof(DateTime) || type == typeof(Guid) || type == typeof(TimeSpan) ||
        (Nullable.GetUnderlyingType(type) is { } underlying && IsAllowedType(underlying));
}
