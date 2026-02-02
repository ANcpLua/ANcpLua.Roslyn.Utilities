// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET5_0_OR_GREATER

namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Suppresses reporting of a specific rule violation, allowing multiple suppressions on a single code artifact.
/// </summary>
/// <remarks>
/// This attribute is polyfilled for netstandard2.0 compatibility.
/// </remarks>
[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
internal sealed class UnconditionalSuppressMessageAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnconditionalSuppressMessageAttribute"/> class.
    /// </summary>
    /// <param name="category">The category for the attribute.</param>
    /// <param name="checkId">The identifier of the analysis rule the attribute applies to.</param>
    public UnconditionalSuppressMessageAttribute(string category, string checkId)
    {
        Category = category;
        CheckId = checkId;
    }

    /// <summary>Gets the category identifying the classification of the attribute.</summary>
    public string Category { get; }

    /// <summary>Gets the identifier of the analysis tool rule to be suppressed.</summary>
    public string CheckId { get; }

    /// <summary>Gets or sets the scope of the code that is relevant for the attribute.</summary>
    public string? Scope { get; set; }

    /// <summary>Gets or sets a fully qualified path that represents the target of the attribute.</summary>
    public string? Target { get; set; }

    /// <summary>Gets or sets an optional argument expanding on exclusion criteria.</summary>
    public string? MessageId { get; set; }

    /// <summary>Gets or sets the justification for suppressing the code analysis message.</summary>
    public string? Justification { get; set; }
}

#endif
