namespace JetBrains.Annotations;

/// <summary>
///     Indicates that IEnumerable passed as a parameter is not enumerated.
///     Use this annotation to suppress the 'Possible multiple enumeration of IEnumerable' inspection.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[ExcludeFromCodeCoverage]
internal sealed class NoEnumerationAttribute : Attribute
{
}