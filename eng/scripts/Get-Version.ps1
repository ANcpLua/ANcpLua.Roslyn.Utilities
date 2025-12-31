param(
    [string]$FallbackVersion = "0.0.1"
)

# Determine version from latest tag or fall back
$tag = git describe --tags --abbrev=0 2>$null
if ($LASTEXITCODE -ne 0 -or !$tag) {
    $major = 0; $minor = 0; $patch = 1
} else {
    $tag = $tag -replace '^v', ''
    $parts = $tag -split '\.'
    $major = [int]$parts[0]
    $minor = [int]$parts[1]
    $patch = [int]$parts[2] + 1
}

$commits = git rev-list --count HEAD
$sha = git rev-parse --short HEAD

# Emit MSBuild-friendly outputs
Write-Output "VERSION=$major.$minor.$patch"
Write-Output "VERSION_SUFFIX=-ci.$commits+$sha"
Write-Output "INFORMATIONAL_VERSION=$major.$minor.$patch-ci.$commits+$sha"
