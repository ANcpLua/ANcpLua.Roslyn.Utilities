namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     A singleton <see cref="IServiceProvider" /> that always returns <c>null</c> for all service requests.
/// </summary>
/// <remarks>
///     <para>
///         Use this when an API requires an <see cref="IServiceProvider" /> but no services need to be resolved.
///         Avoids the overhead of <c>new ServiceCollection().BuildServiceProvider()</c> and makes the
///         "no services" intent explicit.
///     </para>
///     <para>
///         This is commonly needed in:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Unit tests that don't require DI</description>
///         </item>
///         <item>
///             <description>Builder patterns that accept an optional <see cref="IServiceProvider" /></description>
///         </item>
///         <item>
///             <description>Default parameter values where <c>null</c> would require null checks</description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
/// // Instead of: new ServiceCollection().BuildServiceProvider()
/// var services = EmptyServiceProvider.Instance;
///
/// // As default parameter value
/// void Configure(IServiceProvider? services = null)
/// {
///     services ??= EmptyServiceProvider.Instance;
/// }
/// </code>
/// </example>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class EmptyServiceProvider : IServiceProvider
{
    /// <summary>
    ///     Gets the singleton instance of <see cref="EmptyServiceProvider" />.
    /// </summary>
    public static readonly EmptyServiceProvider Instance = new();

    private EmptyServiceProvider()
    {
    }

    /// <inheritdoc />
    /// <returns>Always returns <c>null</c>.</returns>
    public object? GetService(Type serviceType) => null;
}
