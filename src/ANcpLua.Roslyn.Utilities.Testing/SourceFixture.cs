namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Builds C# test sources around the fragment under test, so an analyzer or generator test states only the
///     statements it checks and the stub types they call.
/// </summary>
/// <example>
///     <code>
/// var source = SourceFixture.InMethod(
///     "activity.SetTag(\"user.id\", id);",
///     declarations: "using System.Diagnostics;",
///     parameters: "Activity activity, string id");
/// </code>
/// </example>
public static class SourceFixture
{
    /// <summary>
    ///     Places <paramref name="statements" /> in the body of an <c>internal void</c> method of an
    ///     <c>internal sealed class</c> that follows <paramref name="declarations" />.
    /// </summary>
    /// <param name="statements">The statements the method body runs.</param>
    /// <param name="declarations">
    ///     Top-level code before the class: <c>using</c> directives, then stub types and namespaces.
    /// </param>
    /// <param name="members">Further members of the class: fields, constants, helper methods.</param>
    /// <param name="parameters">The method's parameter list, without parentheses.</param>
    /// <param name="typeName">The name of the class.</param>
    /// <param name="methodName">The name of the method.</param>
    /// <returns>A compilation unit with nullable reference types enabled.</returns>
    public static string InMethod(
        string statements,
        string declarations = "",
        string members = "",
        string parameters = "",
        string typeName = "Sink",
        string methodName = "Emit") =>
        $$"""
          #nullable enable
          {{declarations}}

          internal sealed class {{typeName}}
          {
              {{members}}

              internal void {{methodName}}({{parameters}})
              {
                  {{statements}}
              }
          }
          """;
}
