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
///                 <description>
///                     <see cref="Compilation" />
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <see cref="SemanticModel" />
///                 </description>
///             </item>
///             <item>
///                 <description><see cref="SyntaxNode" /> and all derivatives</description>
///             </item>
///             <item>
///                 <description>
///                     <see cref="SyntaxTree" />
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <see cref="IOperation" />
///                 </description>
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
    ///     Maximum depth to traverse into nested object structures.
    /// </summary>
    /// <remarks>
    ///     Prevents pathological cases where generators produce extremely deep nested structures.
    ///     Set to 100 levels which is sufficient for detecting forbidden types in typical scenarios.
    /// </remarks>
    private const int MaxTraversalDepth = 100;

    /// <summary>
    ///     Maximum number of violations to collect before stopping analysis.
    /// </summary>
    private const int MaxViolations = 256;

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
    ///                 Uses an iterative depth-first traversal with an explicit stack to avoid
    ///                 stack overflow on deeply nested object graphs.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Employs reference equality tracking to prevent infinite loops in cyclic graphs.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Limits traversal depth to <see cref="MaxTraversalDepth" /> (100 levels).
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

        // Use explicit stack for iterative traversal to avoid stack overflow on deep graphs
        Stack<(object Node, string Step, string Path, int Depth)> pending = new();

        // Seed the stack with initial outputs
        foreach (var (stepName, steps) in run.Results.SelectMany(static r => r.TrackedSteps))
        foreach (var step in steps)
        {
            if (step.Outputs.IsDefault)
                continue;
            foreach (var (output, _) in step.Outputs)
                pending.Push((output, stepName, "Output", 0));
        }

        // Iterative depth-first traversal
        while (pending.Count > 0 && violations.Count < MaxViolations)
        {
            var (node, step, path, depth) = pending.Pop();

            // Skip nodes beyond maximum depth
            if (depth >= MaxTraversalDepth)
                continue;

            var type = node.GetType();

            // Cycle detection using reference equality
            if (!type.IsValueType && !visited.Add(node))
                continue;

            if (IsForbiddenType(type))
            {
                violations.Add(new ForbiddenTypeViolation(step, type, path));
                continue;
            }

            if (IsAllowedType(type))
                continue;

            // Handle collections - push elements onto stack
            if (node is IEnumerable collection and not string)
            {
                // Skip default ImmutableArray<T> instances which throw on enumeration
                if (type.IsGenericType &&
                    type.GetGenericTypeDefinition().FullName == "System.Collections.Immutable.ImmutableArray`1")
                {
                    var isDefaultProp = type.GetProperty("IsDefault");
                    if (isDefaultProp?.GetValue(node) is true)
                        continue;
                }

                var index = 0;
                foreach (var element in collection)
                    if (element is not null)
                        pending.Push((element, step, $"{path}[{index++}]", depth + 1));
                continue;
            }

            // Handle object fields - push field values onto stack
            foreach (var field in GetRelevantFields(type))
            {
                var value = field.GetValue(node);
                if (value is not null)
                    pending.Push((value, step, $"{path}.{field.Name}", depth + 1));
            }
        }

        return violations;
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
    private static IEnumerable<FieldInfo> GetRelevantFields(Type type)
    {
        return FieldCache.GetOrAdd(type,
            static t => t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                    BindingFlags.DeclaredOnly).Where(static f => !IsAllowedType(f.FieldType))
                .ToArray());
    }

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
    private static bool IsForbiddenType(Type type)
    {
        return ForbiddenTypes.Contains(type) ||
               ForbiddenTypes.Any(forbidden => forbidden.IsAssignableFrom(type));
    }

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
        Nullable.GetUnderlyingType(type) is { } underlying && IsAllowedType(underlying);
}