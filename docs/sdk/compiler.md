# Compiler & Code Quality Settings

All settings are configured automatically. You don't need to set any of these in your project file.

## Configured Settings

| Setting | Value | Benefit |
|---------|-------|---------|
| `LangVersion` | `latest` | Always latest C# features |
| `Nullable` | `enable` | Null-safety enforcement |
| `ImplicitUsings` | `enable` | Less boilerplate |
| `AnalysisLevel` | `latest-all` | Maximum analyzer coverage |
| `TreatWarningsAsErrors` | `true` (CI) | Clean codebase |
| `Deterministic` | `true` | Reproducible builds |
| `SourceLink` | auto-enabled | Debuggable NuGet packages |

## SDK-Owned Properties

These properties are managed by the SDK. **Do not set them in your csproj:**

- `LangVersion` - Set by SDK
- `Nullable` - Set by SDK
- `Version` / `VersionPrefix` / `VersionSuffix` - Use `Directory.Build.props`

## CI Detection

The SDK automatically detects CI environments and adjusts behavior:

| CI Platform | Detection |
|------------|-----------|
| GitHub Actions | `GITHUB_ACTIONS=true` |
| Azure DevOps | `TF_BUILD=true` |
| GitLab CI | `GITLAB_CI=true` |
| Jenkins | `JENKINS_URL` set |
| TeamCity | `TEAMCITY_VERSION` set |
| CircleCI | `CIRCLECI=true` |
| Travis CI | `TRAVIS=true` |
| AppVeyor | `APPVEYOR=true` |
| Bitbucket Pipelines | `BITBUCKET_BUILD_NUMBER` set |
| AWS CodeBuild | `CODEBUILD_BUILD_ID` set |
| Drone | `DRONE=true` |
| Buddy | `BUDDY=true` |

On CI, additional settings are enabled:
- `TreatWarningsAsErrors=true`
- `ContinuousIntegrationBuild=true`
