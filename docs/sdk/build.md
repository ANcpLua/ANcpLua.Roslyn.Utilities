# Build & Package

The SDK configures deterministic, reproducible builds with modern packaging defaults.

## Build Features

| Feature | Benefit |
|---------|---------|
| **Deterministic** | Same source = same binary |
| **SourceLink** | Debug NuGet packages in Visual Studio |
| **Embedded PDBs** | Symbols included in NuGet package |
| **SBOM Generation** | Software Bill of Materials for security |
| **Package Validation** | API compatibility between versions |

## Required Configuration

### Directory.Packages.props

Central package management is required:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="Microsoft.Extensions.Logging" Version="10.0.0" />
    <!-- All package versions defined here -->
  </ItemGroup>
</Project>
```

### Directory.Build.props

Common properties for all projects:

```xml
<Project>
  <PropertyGroup>
    <Deterministic>true</Deterministic>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
  </PropertyGroup>
</Project>
```

## NuGet Package Defaults

Packages created with the SDK include:

| Metadata | Source |
|----------|--------|
| Version | `Directory.Build.props` or git tag |
| Authors | Git config |
| Repository URL | Git remote |
| Commit hash | Git HEAD |
| License | `LICENSE` file |
| README | `README.md` file |

## Banned Packages

These packages are banned by the SDK:

| Package | Reason | Replacement |
|---------|--------|-------------|
| `Microsoft.NET.Test.Sdk` | VSTest legacy | `xunit.v3.mtp-v2` |
| `FluentAssertions` | Abandoned | `AwesomeAssertions` |
| `xunit.runner.visualstudio` | VSTest adapter | Use MTP runner |
| `coverlet.*` | Legacy coverage | MTP built-in coverage |

## CI/CD Integration

The SDK auto-detects 12+ CI platforms:

- GitHub Actions
- Azure DevOps
- GitLab CI
- Jenkins
- TeamCity
- CircleCI
- Travis CI
- AppVeyor
- Bitbucket Pipelines
- AWS CodeBuild
- Drone
- Buddy

When CI is detected:
- `TreatWarningsAsErrors=true`
- `ContinuousIntegrationBuild=true`
- Git metadata embedded in assemblies

## GitHub Actions Example

```yaml
name: Build

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v6
      - uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '10.0.x'
      - run: dotnet build -c Release
      - run: dotnet test -c Release
      - run: dotnet pack -c Release
      - uses: actions/upload-artifact@v6
        with:
          name: packages
          path: '**/*.nupkg'
```
