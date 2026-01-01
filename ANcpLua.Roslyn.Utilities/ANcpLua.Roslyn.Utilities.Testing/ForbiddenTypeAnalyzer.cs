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
/// </remarks>
internal static class ForbiddenTypeAnalyzer
{
    private static readonly HashSet<Type> ForbiddenTypes =
    [
        typeof(ISymbol),
        typeof(Compilation),
        typeof(SemanticModel),
        typeof(SyntaxNode),
        typeof(SyntaxTree),
        typeof(IOperation)
    ];

    private static readonly ConcurrentDictionary<Type, FieldInfo[]> FieldCache = new();

    /// <summary>
    ///     Analyzes a generator run result for forbidden type violations.
    /// </summary>
    /// <param name="run">The generator run result to analyze.</param>
    /// <returns>A list of violations found, capped at 256 for performance.</returns>
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

    private static IEnumerable<FieldInfo> GetRelevantFields(Type type) =>
        FieldCache.GetOrAdd(type,
            static t => t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                    BindingFlags.DeclaredOnly).Where(static f => !IsAllowedType(f.FieldType))
                .ToArray());

    private static bool IsForbiddenType(Type type) => ForbiddenTypes.Contains(type) ||
                                                      ForbiddenTypes.Any(forbidden => forbidden.IsAssignableFrom(type));

    private static bool IsAllowedType(Type type) =>
        type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) ||
        type == typeof(DateTime) || type == typeof(Guid) || type == typeof(TimeSpan) ||
        (Nullable.GetUnderlyingType(type) is { } underlying && IsAllowedType(underlying));
}