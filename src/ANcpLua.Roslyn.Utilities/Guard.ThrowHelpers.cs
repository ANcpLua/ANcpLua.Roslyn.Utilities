using System.IO;
using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class Guard
{
    // Cold-path throw methods: [NoInlining] keeps exception construction out of
    // inlined Guard callsites, reducing JIT code size at every call.

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowArgumentNull(string? paramName)
    {
        throw new ArgumentNullException(paramName);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T ThrowArgumentNull<T>(string? paramName)
    {
        throw new ArgumentNullException(paramName);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowArgument(string message, string? paramName)
    {
        throw new ArgumentException(message, paramName);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowIndexOutOfRange(int index, int count, string? paramName)
    {
        throw new ArgumentOutOfRangeException(paramName, index, $"Index must be between 0 and {count - 1}.");
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowOutOfRange(string? paramName, object? value, string message)
    {
        throw new ArgumentOutOfRangeException(paramName, value, message);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidOperation(string message)
    {
        throw new InvalidOperationException(message);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string BuildUnreachableMessage(string? message, string memberName, string filePath, int lineNumber)
    {
        var location = string.IsNullOrEmpty(filePath)
            ? memberName
            : $"{memberName} ({Path.GetFileName(filePath)}:{lineNumber})";

        return string.IsNullOrEmpty(message)
            ? $"Unreachable code executed in {location}"
            : $"{message} (in {location})";
    }

    private static void CheckNoDuplicates<T>(IEnumerable<T> source, IEqualityComparer<T>? comparer, string? paramName)
    {
        var seen = comparer is null ? new HashSet<T>() : new HashSet<T>(comparer);
        foreach (var item in source)
            if (!seen.Add(item))
                ThrowArgument($"Duplicate value found: {item}", paramName);
    }
}
