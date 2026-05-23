using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using System;
using System.Globalization;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class TryExtensionsParsingTests
{
    [Fact]
    public void TryParseDouble_UsesInvariantCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");

            "1.5".TryParseDouble().Should().Be(1.5);
            "1,5".TryParseDouble().Should().BeNull();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void TryParseDateTime_UsesInvariantCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");

            "12/31/2026".TryParseDateTime().Should().Be(new DateTime(2026, 12, 31));

            var offset = "12/31/2026".TryParseDateTimeOffset();
            offset.Should().NotBeNull();
            offset.Value.Date.Should().Be(new DateTime(2026, 12, 31));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
