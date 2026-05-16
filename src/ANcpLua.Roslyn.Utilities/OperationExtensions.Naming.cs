using Microsoft.CodeAnalysis.Operations;
using IOperation = Microsoft.CodeAnalysis.IOperation;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class OperationExtensions
{
    /// <summary>
    ///     Gets a human-readable name for an operation, useful for diagnostic messages.
    /// </summary>
    /// <param name="operation">The operation to get the name from.</param>
    /// <param name="fallback">The fallback name to use if the operation type is not recognized. Defaults to "value".</param>
    /// <returns>
    ///     A string representing the operation:
    ///     <list type="bullet">
    ///         <item><description>For local references: the local variable name</description></item>
    ///         <item><description>For parameter references: the parameter name</description></item>
    ///         <item><description>For property references: the property name</description></item>
    ///         <item><description>For field references: the field name</description></item>
    ///         <item><description>For method invocations: the method name with "()"</description></item>
    ///         <item><description>For string literals: the quoted string value</description></item>
    ///         <item><description>For numeric literals: the string representation</description></item>
    ///         <item><description>For other operations: the fallback value</description></item>
    ///     </list>
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method unwraps implicit conversions before extracting the name, so
    ///         <c>(object)myVariable</c> will return "myVariable".
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // In an analyzer, create a readable diagnostic message:
    /// var operandName = operation.GetOperandName();
    /// var message = $"Consider using {operandName}.EqualsOrdinal(other)";
    /// </code>
    /// </example>
    /// <seealso cref="GetCollectionSourceName" />
    public static string GetOperandName(this IOperation? operation, string fallback = "value")
    {
        if (operation is null)
            return fallback;

        // Unwrap conversions
        operation = operation.UnwrapImplicitConversions();

        return operation switch
        {
            ILocalReferenceOperation local => local.Local.Name,
            IParameterReferenceOperation param => param.Parameter.Name,
            IPropertyReferenceOperation prop => prop.Property.Name,
            IFieldReferenceOperation field => field.Field.Name,
            IInvocationOperation invocation => $"{invocation.TargetMethod.Name}()",
            ILiteralOperation { ConstantValue: { HasValue: true, Value: string s } } => $"\"{s}\"",
            ILiteralOperation { ConstantValue: { HasValue: true, Value: { } v } } => v.ToString() ?? fallback,
            IMemberReferenceOperation member => member.Member.Name,
            IArrayElementReferenceOperation => "element",
            IInstanceReferenceOperation => "this",
            _ => fallback
        };
    }

    /// <summary>
    ///     Gets the source name from a collection operation, useful for foreach analysis.
    /// </summary>
    /// <param name="collection">The collection operation to get the name from.</param>
    /// <returns>
    ///     The name of the source:
    ///     <list type="bullet">
    ///         <item><description>For method invocations: the method name</description></item>
    ///         <item><description>For property references: the property name</description></item>
    ///         <item><description>For field references: the field name</description></item>
    ///         <item><description>For local/parameter references: the variable name</description></item>
    ///         <item><description>For conversions: recursively gets the source name</description></item>
    ///         <item><description>For other operations: <c>null</c></description></item>
    ///     </list>
    /// </returns>
    /// <seealso cref="GetOperandName" />
    public static string? GetCollectionSourceName(this IOperation? collection)
    {
        return collection switch
        {
            null => null,
            IInvocationOperation invocation => invocation.TargetMethod.Name,
            IPropertyReferenceOperation propertyRef => propertyRef.Property.Name,
            IFieldReferenceOperation fieldRef => fieldRef.Field.Name,
            ILocalReferenceOperation localRef => localRef.Local.Name,
            IParameterReferenceOperation paramRef => paramRef.Parameter.Name,
            IConversionOperation conversion => conversion.Operand.GetCollectionSourceName(),
            IParenthesizedOperation paren => paren.Operand.GetCollectionSourceName(),
            _ => null
        };
    }
}
