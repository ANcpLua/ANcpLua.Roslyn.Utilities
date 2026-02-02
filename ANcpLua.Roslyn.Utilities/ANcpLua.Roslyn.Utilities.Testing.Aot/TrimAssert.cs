using System;
using System.Diagnostics.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing.Aot;

/// <summary>
/// Helper class for asserting type trimming behavior in AOT and trimming scenarios.
/// </summary>
public static class TrimAssert
{
    /// <summary>
    /// Asserts that a type was trimmed away and does not exist at runtime.
    /// </summary>
    /// <param name="typeName">The fully qualified name of the type (e.g., "MyNamespace.MyType").</param>
    /// <param name="assemblyName">The name of the assembly containing the type (e.g., "MyAssembly").</param>
    /// <exception cref="InvalidOperationException">Thrown indirectly via Environment.Exit(-1) if the type still exists at runtime.</exception>
    /// <remarks>
    /// This method uses reflection to verify that a type has been trimmed away during the trimming process.
    /// If the type is unexpectedly found at runtime, an error is written to the error stream and the process exits with code -1.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Intentional runtime type lookup to verify trimming behavior")]
    public static void TypeTrimmed(string typeName, string assemblyName)
    {
        var type = Type.GetType($"{typeName}, {assemblyName}");

        if (type != null)
        {
            Console.Error.WriteLine($"FAIL: Type '{typeName}' from assembly '{assemblyName}' was expected to be trimmed away but was found at runtime.");
            Environment.Exit(-1);
        }
    }

    /// <summary>
    /// Asserts that a type survived trimming and exists at runtime.
    /// </summary>
    /// <param name="typeName">The fully qualified name of the type (e.g., "MyNamespace.MyType").</param>
    /// <param name="assemblyName">The name of the assembly containing the type (e.g., "MyAssembly").</param>
    /// <exception cref="InvalidOperationException">Thrown indirectly via Environment.Exit(-2) if the type was unexpectedly trimmed.</exception>
    /// <remarks>
    /// This method uses reflection to verify that a type has been preserved during the trimming process.
    /// If the type is unexpectedly trimmed and not found at runtime, an error is written to the error stream and the process exits with code -2.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Intentional runtime type lookup to verify trimming behavior")]
    public static void TypePreserved(string typeName, string assemblyName)
    {
        var type = Type.GetType($"{typeName}, {assemblyName}");

        if (type == null)
        {
            Console.Error.WriteLine($"FAIL: Type '{typeName}' from assembly '{assemblyName}' was expected to be preserved but was trimmed away.");
            Environment.Exit(-2);
        }
    }
}
