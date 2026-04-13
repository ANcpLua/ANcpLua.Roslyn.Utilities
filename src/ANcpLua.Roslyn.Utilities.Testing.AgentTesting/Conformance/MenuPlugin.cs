// Licensed to the .NET Foundation under one or more agreements.

using System.ComponentModel;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance;

/// <summary>
///     A tiny restaurant-menu function-tool plugin used by the conformance suite to verify
///     end-to-end function calling on every provider.
/// </summary>
public static class MenuPlugin
{
    /// <summary>Returns a fixed list of three menu specials.</summary>
    [Description("Provides a list of specials from the menu.")]
    public static string GetSpecials() =>
        "Special Soup: Clam Chowder\nSpecial Salad: Cobb Salad\nSpecial Drink: Chai Tea";

    /// <summary>Returns a fixed price for any menu item.</summary>
    [Description("Provides the price of the requested menu item.")]
    public static string GetItemPrice(
        [Description("The name of the menu item.")] string menuItem) => "$9.99";
}
