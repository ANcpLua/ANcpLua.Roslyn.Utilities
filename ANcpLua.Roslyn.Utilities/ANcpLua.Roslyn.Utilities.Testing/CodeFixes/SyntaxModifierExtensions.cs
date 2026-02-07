using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ANcpLua.Roslyn.Utilities.CodeFixes;

/// <summary>
/// Extensions for manipulating syntax modifier lists.
/// </summary>
public static class SyntaxModifierExtensions
{
    /// <summary>
    /// Ensures a modifier is present at the specified position.
    /// </summary>
    public static SyntaxTokenList EnsureModifier(
        this SyntaxTokenList modifiers,
        SyntaxKind kind,
        ModifierPosition position = ModifierPosition.AfterAccessibility)
    {
        if (modifiers.Any(kind)) return modifiers;

        var insertIndex = position switch
        {
            ModifierPosition.Start => 0,
            ModifierPosition.AfterAccessibility => GetAccessibilityEndIndex(modifiers),
            ModifierPosition.BeforePartial => GetPartialIndex(modifiers),
            _ => modifiers.Count
        };

        return modifiers.Insert(insertIndex, SyntaxFactory.Token(kind).WithTrailingTrivia(SyntaxFactory.Space));
    }

    /// <summary>
    /// Removes a modifier from the list if present.
    /// </summary>
    public static SyntaxTokenList RemoveModifier(this SyntaxTokenList modifiers, SyntaxKind kind)
    {
        var index = modifiers.IndexOf(kind);
        return index >= 0 ? modifiers.RemoveAt(index) : modifiers;
    }

    private static int GetAccessibilityEndIndex(SyntaxTokenList modifiers)
    {
        for (var i = 0; i < modifiers.Count; i++)
        {
            if (!IsAccessibilityModifier(modifiers[i].Kind()))
                return i;
        }
        return modifiers.Count;
    }

    private static int GetPartialIndex(SyntaxTokenList modifiers)
    {
        var index = modifiers.IndexOf(SyntaxKind.PartialKeyword);
        return index >= 0 ? index : modifiers.Count;
    }

    private static bool IsAccessibilityModifier(SyntaxKind kind) =>
        kind is SyntaxKind.PublicKeyword or SyntaxKind.PrivateKeyword or
               SyntaxKind.ProtectedKeyword or SyntaxKind.InternalKeyword;
}

/// <summary>
/// Position to insert a modifier.
/// </summary>
public enum ModifierPosition
{
    /// <summary>At the start of the modifier list.</summary>
    Start,
    /// <summary>After accessibility modifiers (public/private/protected/internal).</summary>
    AfterAccessibility,
    /// <summary>Before the partial keyword.</summary>
    BeforePartial,
    /// <summary>At the end of the modifier list.</summary>
    End
}