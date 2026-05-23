using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class LanguageVersionExtensionsTests
{
    [Fact]
    public void IsCSharpVersionAtLeast_UsesNamedLanguageVersionBoundaries()
    {
        LanguageVersion.CSharp10.IsCSharp10OrAbove().Should().BeTrue();
        LanguageVersion.CSharp11.IsCSharp11OrAbove().Should().BeTrue();
        LanguageVersion.CSharp10.IsCSharp11OrAbove().Should().BeFalse();
        LanguageVersion.CSharp12.IsCSharp12OrAbove().Should().BeTrue();
        LanguageVersion.CSharp12.IsCSharp13OrAbove().Should().BeFalse();
        LanguageVersion.CSharp14.IsCSharp14OrAbove().Should().BeTrue();
    }

    [Fact]
    public void LatestAndPreview_AreTreatedAsCurrentMaximum()
    {
        LanguageVersion.Preview.IsCSharp14OrAbove().Should().BeTrue();
        LanguageVersion.Latest.IsCSharp14OrAbove().Should().BeTrue();
    }
}
