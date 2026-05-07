namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension helpers for weak references.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class WeakReferenceExtensions
{
    /// <summary>
    ///     Attempts to get the target of the non-generic weak reference.
    /// </summary>
    /// <param name="weakReference">The weak reference.</param>
    /// <param name="target">The target when it is alive; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> when the target is still alive.</returns>
    public static bool TryGetTarget(this WeakReference weakReference, [NotNullWhen(true)] out object? target)
    {
        Guard.NotNull(weakReference);
        target = weakReference.Target;
        return target is not null;
    }

    /// <summary>
    ///     Returns the weak reference target when available; otherwise <c>null</c>.
    /// </summary>
    /// <param name="weakReference">The weak reference.</param>
    /// <returns>The target instance, or <c>null</c> if collected.</returns>
    public static object? GetTargetOrDefault(this WeakReference weakReference)
    {
        Guard.NotNull(weakReference);
        return weakReference.TryGetTarget(out var target) ? target : default;
    }

    /// <summary>
    ///     Returns the weak reference target when available and typed; otherwise <c>null</c>.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="weakReference">The weak reference.</param>
    /// <returns>The target instance, or <c>null</c> if collected.</returns>
    public static T? GetTargetOrDefault<T>(this WeakReference<T> weakReference)
        where T : class
    {
        Guard.NotNull(weakReference);
        return weakReference.TryGetTarget(out T? target) ? target : default;
    }
}
