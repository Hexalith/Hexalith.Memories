[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$ServerImage,

    [Parameter(Mandatory)]
    [string]$McpImage,

    [Parameter(Mandatory)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

if ($Version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$') {
    throw "Version '$Version' is not a valid semantic version."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$overlay = Join-Path $repoRoot 'deploy/kubernetes/overlays/production'
$resolvedOutput = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
}
else {
    Join-Path $repoRoot $OutputPath
}

$rendered = (& kubectl kustomize $overlay 2>&1) -join [Environment]::NewLine
if ($LASTEXITCODE -ne 0) {
    throw "kubectl kustomize failed: $rendered"
}

$defaultServerImage = 'registry.hexalith.com/hexalith/memories-server:0.0.0'
$defaultMcpImage = 'registry.hexalith.com/hexalith/memories-mcp:0.0.0'
if (($rendered.Split($defaultServerImage).Count - 1) -ne 1) {
    throw "Rendered deployment must contain exactly one Server image placeholder."
}

if (($rendered.Split($defaultMcpImage).Count - 1) -ne 1) {
    throw "Rendered deployment must contain exactly one MCP image placeholder."
}

$expectedSuffix = ":$Version"
if (-not $ServerImage.EndsWith($expectedSuffix, [StringComparison]::Ordinal) -or
    -not $McpImage.EndsWith($expectedSuffix, [StringComparison]::Ordinal)) {
    throw "Both release image references must end with the semantic-release version '$expectedSuffix'."
}

$rendered = $rendered.Replace($defaultServerImage, $ServerImage, [StringComparison]::Ordinal)
$rendered = $rendered.Replace($defaultMcpImage, $McpImage, [StringComparison]::Ordinal)
if ($rendered -match '(?m)^kind:\s+Secret\s*$') {
    throw "Production deployment render must not contain literal Kubernetes Secret resources."
}

$outputDirectory = Split-Path -Parent $resolvedOutput
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
Set-Content -LiteralPath $resolvedOutput -Value $rendered -Encoding utf8
Write-Host "Rendered production deployment $resolvedOutput with Server and MCP tag $Version."
