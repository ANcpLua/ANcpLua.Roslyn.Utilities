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
    ///     Scans all (including inherited) members named <paramref name="name" /> and returns <c>true</c>
    ///     if any matches the supplied predicate.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="name">The member name.</param>
    /// <param name="predicate">A predicate applied to each candidate member.</param>
    /// <remarks>
    ///     Internal chokepoint behind <see cref="HasDisposeMethod" />, <see cref="HasDisposeAsyncMethod" />,
    ///     and <see cref="HasCountProperty" />. Centralising the foreach keeps each predicate at CC=1.
    /// </remarks>
    private static bool HasMember(this ITypeSymbol type, string name, Func<ISymbol, bool> predicate)
    {
        foreach (var member in type.GetAllMembers(name))
            if (predicate(member))
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether the specified type has a parameterless <c>Dispose()</c> method that returns <c>void</c>.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> has a <c>void Dispose()</c> method with no parameters; otherwise,
    ///     <c>false</c>.
    /// </returns>
    /// <seealso cref="HasDisposeAsyncMethod" />
    public static bool HasDisposeMethod(this ITypeSymbol type)
    {
        return type.HasMember(
            "Dispose",
            static m => m is IMethodSymbol { Parameters.IsEmpty: true, ReturnsVoid: true });
    }

    /// <summary>
    ///     Determines whether the specified type has a parameterless <c>DisposeAsync()</c> method.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> has a <c>DisposeAsync()</c> method with no parameters; otherwise,
    ///     <c>false</c>.
    /// </returns>
    /// <seealso cref="HasDisposeMethod" />
    public static bool HasDisposeAsyncMethod(this ITypeSymbol type)
    {
        return type.HasMember(
            "DisposeAsync",
            static m => m is IMethodSymbol { Parameters.IsEmpty: true });
    }

    /// <summary>
    ///     Determines whether the specified type has a <c>Count</c> or <c>Length</c> property with a getter.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> has a readable <c>Count</c> or <c>Length</c> property;
    ///     otherwise, <c>false</c>.
    /// </returns>
    public static bool HasCountProperty(this ITypeSymbol type)
    {
        return type.HasMember("Count", IsReadableProperty)
               || type.HasMember("Length", IsReadableProperty);
    }

    private static bool IsReadableProperty(ISymbol member)
    {
        return member is IPropertySymbol { GetMethod: not null };
    }
}
