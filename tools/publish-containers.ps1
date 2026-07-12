[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [string]$Registry = 'registry.hexalith.com',

    [string]$RepositoryPrefix = 'hexalith/memories',

    [string]$OutputDirectory = 'artifacts/containers/release',

    [switch]$Push
)

$ErrorActionPreference = 'Stop'

# Build metadata (+meta) is a valid semver suffix but an invalid OCI/Docker image tag character,
# so it is rejected here rather than flowing into -p:ContainerImageTag / docker tag / kind load.
if ($Version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$') {
    throw "Version '$Version' is not a valid image-taggable semantic version (build metadata '+meta' is not allowed)."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory
}
else {
    Join-Path $repoRoot $OutputDirectory
}
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null

$images = @(
    # Default release members are registry.hexalith.com/hexalith/memories-server and
    # registry.hexalith.com/hexalith/memories-mcp. RepositoryPrefix keeps CI registries overridable.
    [ordered]@{
        name = 'server'
        project = 'src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj'
        repository = "$RepositoryPrefix-server"
    },
    [ordered]@{
        name = 'mcp'
        project = 'src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj'
        repository = "$RepositoryPrefix-mcp"
    }
)

function Protect-LogText {
    param([string]$Text)

    $sanitized = $Text
    foreach ($secretName in @('GITHUB_TOKEN', 'GH_TOKEN', 'CR_PAT')) {
        $secret = [Environment]::GetEnvironmentVariable($secretName)
        if (-not [string]::IsNullOrWhiteSpace($secret)) {
            $sanitized = $sanitized.Replace($secret, '***', [StringComparison]::Ordinal)
        }
    }

    return $sanitized.Trim()
}

$startedAt = [DateTime]::UtcNow
$outcomes = @()
Push-Location $repoRoot
try {
    foreach ($image in $images) {
        $imageReference = "$Registry/$($image.repository):$Version"
        $arguments = @(
            'publish',
            $image.project,
            '--configuration', 'Release',
            '-t:PublishContainer',
            "-p:Version=$Version",
            "-p:PackageVersion=$Version",
            "-p:ContainerRegistry=$Registry",
            "-p:ContainerRepository=$($image.repository)",
            "-p:ContainerImageTag=$Version",
            '-p:ContinuousIntegrationBuild=true'
        )
        if (-not $Push) {
            $archive = Join-Path $outputPath "$($image.name).tar.gz"
            $arguments += "-p:ContainerArchiveOutputPath=$archive"
        }

        Write-Host "Publishing container unit member $imageReference (push=$Push)"
        $output = @(& dotnet @arguments 2>&1)
        $exitCode = $LASTEXITCODE
        $outcomes += [ordered]@{
            name = $image.name
            image = $imageReference
            status = if ($exitCode -eq 0) { 'succeeded' } else { 'failed' }
            exitCode = $exitCode
            error = if ($exitCode -eq 0) { $null } else { Protect-LogText ($output -join [Environment]::NewLine) }
        }
    }

    $failed = @($outcomes | Where-Object status -eq 'failed')
    $succeeded = @($outcomes | Where-Object status -eq 'succeeded')
    $status = if ($failed.Count -eq 0) {
        'succeeded'
    }
    elseif ($succeeded.Count -gt 0) {
        'partial-publish'
    }
    else {
        'publish-failed'
    }

    $serverImage = "$Registry/$RepositoryPrefix-server`:$Version"
    $mcpImage = "$Registry/$RepositoryPrefix-mcp`:$Version"
    & pwsh -NoLogo -NoProfile -File ./tools/render-production-deployment.ps1 `
        -Version $Version `
        -ServerImage $serverImage `
        -McpImage $mcpImage `
        -OutputPath (Join-Path $outputPath 'production-deployment.yaml')
    if ($LASTEXITCODE -ne 0) {
        throw 'Production release deployment render failed.'
    }

    $summary = [ordered]@{
        schemaVersion = 1
        artifactKind = 'container-images'
        status = $status
        version = $Version
        push = [bool]$Push
        packageDirectory = [System.IO.Path]::GetRelativePath($repoRoot, $outputPath).Replace('\', '/')
        source = $Registry
        startedAt = $startedAt.ToString('o')
        completedAt = [DateTime]::UtcNow.ToString('o')
        images = $outcomes
        pushed = @($succeeded | ForEach-Object image)
        failed = @($failed | ForEach-Object {
            [ordered]@{ package = $_.image; exitCode = $_.exitCode; error = $_.error }
        })
        notAttempted = @()
        recovery = 'Rerun the Release workflow. Publishing an existing immutable image tag is idempotent; both members are retried and the summary is replaced.'
    }
    $summaryName = if ($Push) { 'publish-summary.json' } else { 'build-summary.json' }
    $summaryPath = Join-Path $outputPath $summaryName
    $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $summaryPath -Encoding utf8
    Write-Host "Container publication summary: $summaryPath ($status)"

    if ($status -ne 'succeeded') {
        if ($env:GITHUB_ACTIONS -eq 'true') {
            Write-Host "::error title=PARTIAL CONTAINER PUBLISH::$($failed.Count) of 2 release images failed for $Version."
        }

        throw "Container publication finished with status '$status'."
    }
}
finally {
    Pop-Location
}
