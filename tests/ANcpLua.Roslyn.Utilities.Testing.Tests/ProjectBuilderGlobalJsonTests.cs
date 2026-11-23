using System.Text.Json;
using ANcpLua.Roslyn.Utilities.Testing.MSBuild;
using AwesomeAssertions;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

// The global.json a ProjectBuilder writes must pin the SDK it runs, or the downloaded SDK refuses the project.
public sealed class ProjectBuilderGlobalJsonTests
{
    [Fact]
    public async Task DefaultProject_PinsTheNet11Sdk()
    {
        await using var project = new ProjectBuilder();

        SdkPin(project).Should().Be("11.0.100");
    }

    [Fact]
    public async Task SelectingAnSdk_MovesThePin_AndKeepsTheMtpRunner()
    {
        await using var project = new ProjectBuilder();
        project.WithMtpMode().WithDotnetSdkVersion(NetSdkVersion.Net100);

        SdkPin(project).Should().Be("10.0.100");
        TestRunner(project).Should().Be("Microsoft.Testing.Platform");
    }

    [Fact]
    public async Task AmbientSdk_DropsThePin_AndKeepsTheMtpRunner()
    {
        await using var project = new ProjectBuilder();
        project.WithDotnetSdkVersion(NetSdkVersion.Ambient).WithMtpMode();

        using var json = Read(project);
        json.RootElement.TryGetProperty("sdk", out _).Should().BeFalse();
        TestRunner(project).Should().Be("Microsoft.Testing.Platform");
    }

    [Fact]
    public async Task AmbientSdk_WithoutMtp_WritesNoGlobalJson()
    {
        await using var project = new ProjectBuilder();
        project.WithDotnetSdkVersion(NetSdkVersion.Ambient);

        File.Exists(project.RootFolder / "global.json").Should().BeFalse();
    }

    private static string? SdkPin(ProjectBuilder project)
    {
        using var json = Read(project);
        return json.RootElement.GetProperty("sdk").GetProperty("version").GetString();
    }

    private static string? TestRunner(ProjectBuilder project)
    {
        using var json = Read(project);
        return json.RootElement.GetProperty("test").GetProperty("runner").GetString();
    }

    private static JsonDocument Read(ProjectBuilder project) =>
        JsonDocument.Parse(File.ReadAllText(project.RootFolder / "global.json"));
}
