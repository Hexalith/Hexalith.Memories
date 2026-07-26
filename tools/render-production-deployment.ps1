[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$ServerImage,

    [Parameter(Mandatory)]
    [string]$McpImage,

    [Parameter(Mandatory)]
    [string]$AccessTelemetryImage,

    [Parameter(Mandatory)]
    [string]$AccessTelemetryClockImage,

    [Parameter(Mandatory)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

# Build metadata (+meta) is a valid semver suffix but an invalid OCI/Docker image tag character,
# so it is rejected here to keep the rendered image references pullable.
if ($Version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$') {
    throw "Version '$Version' is not a valid image-taggable semantic version (build metadata '+meta' is not allowed)."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$overlay = Join-Path $repoRoot 'deploy/kubernetes/overlays/production'
$resolvedOutput = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
}
else {
    Join-Path $repoRoot $OutputPath
}

$stderrPath = [System.IO.Path]::GetTempFileName()
try {
    $rendered = (& kubectl kustomize $overlay 2> $stderrPath) -join [Environment]::NewLine
    $exitCode = $LASTEXITCODE
    $stderr = if ((Get-Item -LiteralPath $stderrPath).Length -gt 0) {
        Get-Content -LiteralPath $stderrPath -Raw
    }
    else {
        ''
    }
}
finally {
    Remove-Item -LiteralPath $stderrPath -Force -ErrorAction SilentlyContinue
}

if ($exitCode -ne 0) {
    throw "kubectl kustomize failed: $stderr"
}

if (-not [string]::IsNullOrWhiteSpace($stderr)) {
    Write-Warning "kubectl kustomize emitted warnings: $($stderr.Trim())"
}

$defaultServerImage = 'registry.hexalith.com/memories:0.0.0'
$defaultMcpImage = 'registry.hexalith.com/memories-mcp:0.0.0'
$defaultAccessTelemetryImage = 'registry.hexalith.com/memories-access-telemetry:0.0.0'
$defaultAccessTelemetryClockImage = 'registry.hexalith.com/memories-access-telemetry-clock:0.0.0'
if (($rendered.Split($defaultServerImage).Count - 1) -ne 1) {
    throw "Rendered deployment must contain exactly one Server image placeholder."
}

if (($rendered.Split($defaultMcpImage).Count - 1) -ne 1) {
    throw "Rendered deployment must contain exactly one MCP image placeholder."
}

if (($rendered.Split($defaultAccessTelemetryImage).Count - 1) -ne 1) {
    throw "Rendered deployment must contain exactly one access-telemetry image placeholder."
}

if (($rendered.Split($defaultAccessTelemetryClockImage).Count - 1) -ne 1) {
    throw "Rendered deployment must contain exactly one access-telemetry-clock image placeholder."
}

$expectedSuffix = ":$Version"
if (-not $ServerImage.EndsWith($expectedSuffix, [StringComparison]::Ordinal) -or
    -not $McpImage.EndsWith($expectedSuffix, [StringComparison]::Ordinal) -or
    -not $AccessTelemetryImage.EndsWith($expectedSuffix, [StringComparison]::Ordinal) -or
    -not $AccessTelemetryClockImage.EndsWith($expectedSuffix, [StringComparison]::Ordinal)) {
    throw "All release image references must end with the semantic-release version '$expectedSuffix'."
}

# Placeholder counts and the version suffix say nothing about WHICH image goes where, so
# transposed or duplicated arguments used to render a valid-looking manifest whose workloads
# run the wrong image. Bind each argument to the repository suffix it must carry, relative to
# the Server image's repository, and reject duplicates outright. The registry host and the
# repository prefix stay parameterized (publish-containers.ps1 overrides both).
$suppliedImages = [ordered]@{
    ServerImage = $ServerImage
    McpImage = $McpImage
    AccessTelemetryImage = $AccessTelemetryImage
    AccessTelemetryClockImage = $AccessTelemetryClockImage
}
$baseRepository = $ServerImage.Substring(0, $ServerImage.Length - $expectedSuffix.Length)
$expectedSuffixes = [ordered]@{
    McpImage = '-mcp'
    AccessTelemetryImage = '-access-telemetry'
    AccessTelemetryClockImage = '-access-telemetry-clock'
}
foreach ($parameter in $expectedSuffixes.Keys) {
    $supplied = [string]$suppliedImages[$parameter]
    $repository = $supplied.Substring(0, $supplied.Length - $expectedSuffix.Length)
    $expectedRepository = "$baseRepository$($expectedSuffixes[$parameter])"
    if (-not [string]::Equals($repository, $expectedRepository, [StringComparison]::Ordinal)) {
        throw "-$parameter must reference repository '$expectedRepository', but names '$repository'."
    }
}

$distinctImages = @($suppliedImages.Values | Sort-Object -Unique)
if ($distinctImages.Count -ne $suppliedImages.Count) {
    throw "All four release image references must be distinct; found $($distinctImages.Count) unique value(s)."
}

$rendered = $rendered.Replace($defaultServerImage, $ServerImage, [StringComparison]::Ordinal)
$rendered = $rendered.Replace($defaultMcpImage, $McpImage, [StringComparison]::Ordinal)
$rendered = $rendered.Replace($defaultAccessTelemetryImage, $AccessTelemetryImage, [StringComparison]::Ordinal)
$rendered = $rendered.Replace($defaultAccessTelemetryClockImage, $AccessTelemetryClockImage, [StringComparison]::Ordinal)
if ($rendered -match '(?m)^kind:\s+Secret\s*$') {
    throw "Production deployment render must not contain literal Kubernetes Secret resources."
}

$outputDirectory = Split-Path -Parent $resolvedOutput
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
Set-Content -LiteralPath $resolvedOutput -Value $rendered -Encoding utf8
Write-Host "Rendered production deployment $resolvedOutput with all release image tags at $Version."
