using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class TypeSymbolExtensions
{
    // Test-framework attribute taxonomy lives in static sets so IsUnitTestClass
    // is one foreach + one hashset lookup per attribute, not a chain of switch
    // arms. Adding a framework = add a string; CC of IsUnitTestClass stays flat.
    private static readonly HashSet<string> s_testClassAttributes =
    [
        // MSTest
        "Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute",
        // NUnit
        "NUnit.Framework.TestFixtureAttribute"
    ];

    private static readonly HashSet<string> s_testMethodAttributes =
    [
        // MSTest
        "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute",
        // NUnit
        "NUnit.Framework.TestAttribute",
        "NUnit.Framework.TestCaseAttribute"
    ];

    private const string XunitAttributeNamespacePrefix = "Xunit.";

    /// <summary>
    ///     Determines whether the type symbol represents a unit test class.
    /// </summary>
    /// <param name="symbol">The named type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> is a unit test class; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>This method detects test classes from the following frameworks:</para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <b>MSTest:</b> Classes with <c>[TestClass]</c> attribute or methods with <c>[TestMethod]</c>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <b>NUnit:</b> Classes with <c>[TestFixture]</c> attribute or methods with <c>[Test]</c>/
    ///                 <c>[TestCase]</c>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <b>xUnit:</b> Classes with methods having <c>[Fact]</c>, <c>[Theory]</c>, or other Xunit.*
    ///                 attributes
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static bool IsUnitTestClass(this INamedTypeSymbol symbol)
    {
        if (symbol.TypeKind is not TypeKind.Class)
            return false;

        if (HasAttribute(symbol.GetAttributes(), s_testClassAttributes))
            return true;

        // xUnit doesn't require class-level attributes — scan members for a known test-method attribute.
        foreach (var member in symbol.GetMembers())
            if (member is IMethodSymbol method && HasTestMethodAttribute(method))
                return true;

        return false;
    }

    private static bool HasTestMethodAttribute(IMethodSymbol method)
    {
        foreach (var attr in method.GetAttributes())
        {
            var name = attr.AttributeClass?.ToDisplayString();
            if (name is null)
                continue;

            if (name.StartsWith(XunitAttributeNamespacePrefix, StringComparison.Ordinal))
                return true;

            if (s_testMethodAttributes.Contains(name))
                return true;
        }

        return false;
    }

    private static bool HasAttribute(ImmutableArray<AttributeData> attributes, HashSet<string> knownNames)
    {
        foreach (var attr in attributes)
        {
            var name = attr.AttributeClass?.ToDisplayString();
            if (name is not null && knownNames.Contains(name))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Determines whether a type could potentially be made static.
    /// </summary>
    /// <param name="symbol">The named type symbol to check.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> could be made static; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>A type is considered a candidate for being static if:</para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>It is not abstract or already static</description>
    ///         </item>
    ///         <item>
    ///             <description>It is not implicitly declared</description>
    ///         </item>
    ///         <item>
    ///             <description>It has no interfaces</description>
    ///         </item>
    ///         <item>
    ///             <description>It has no base class other than <see cref="object" /></description>
    ///         </item>
    ///         <item>
    ///             <description>It is not a unit test class</description>
    ///         </item>
    ///         <item>
    ///             <description>It is not a top-level statement container</description>
    ///         </item>
    ///         <item>
    ///             <description>All non-implicitly-declared members are static or operators</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="IsUnitTestClass" />
    public static bool IsPotentialStatic(this INamedTypeSymbol symbol, CancellationToken cancellationToken = default)
    {
        return HasStaticShape(symbol)
               && !symbol.IsUnitTestClass()
               && !symbol.IsTopLevelStatement(cancellationToken)
               && AllMembersStaticOrOperator(symbol);
    }

    private static bool HasStaticShape(INamedTypeSymbol symbol)
    {
        if (symbol is not { IsAbstract: false, IsStatic: false, IsImplicitlyDeclared: false })
            return false;

        if (symbol.Interfaces.Length > 0)
            return false;

        return symbol.BaseType is null || symbol.BaseType.SpecialType is SpecialType.System_Object;
    }

    private static bool AllMembersStaticOrOperator(INamedTypeSymbol symbol)
    {
        foreach (var member in symbol.GetMembers())
        {
            if (member.IsImplicitlyDeclared)
                continue;

            if (!member.IsStatic && !member.IsOperator())
                return false;
        }

        return true;
    }
}
