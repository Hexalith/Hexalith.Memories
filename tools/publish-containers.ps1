[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [string]$Registry = 'registry.hexalith.com',

    [string]$RepositoryPrefix = 'memories',

    [string]$OutputDirectory = 'artifacts/containers/release',

    [string]$RepositoryRoot,

    [switch]$Push
)

$ErrorActionPreference = 'Stop'

if (-not $PSBoundParameters.ContainsKey('Registry') -and
    -not [string]::IsNullOrWhiteSpace($env:HEXALITH_ZOT_REGISTRY)) {
    $Registry = $env:HEXALITH_ZOT_REGISTRY
}

# Build metadata (+meta) is a valid semver suffix but an invalid OCI/Docker image tag character,
# so it is rejected here rather than flowing into -p:ContainerImageTag / docker tag / kind load.
if ($Version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$') {
    throw "Version '$Version' is not a valid image-taggable semantic version (build metadata '+meta' is not allowed)."
}

$repoRoot = if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    Split-Path -Parent $PSScriptRoot
}
else {
    (Resolve-Path -LiteralPath $RepositoryRoot).Path
}
$outputPath = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory
}
else {
    Join-Path $repoRoot $OutputDirectory
}
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null

$images = @(
    [ordered]@{
        name = 'server'
        project = 'src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj'
        repository = $RepositoryPrefix
        archive = Join-Path $outputPath 'server.tar.gz'
    },
    [ordered]@{
        name = 'mcp'
        project = 'src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj'
        repository = "$RepositoryPrefix-mcp"
        archive = Join-Path $outputPath 'mcp.tar.gz'
    }
)

function Protect-LogText {
    param([AllowEmptyString()][string]$Text)

    $sanitized = $Text
    foreach ($secretName in @(
        'GITHUB_TOKEN',
        'GH_TOKEN',
        'CR_PAT',
        'NUGET_API_KEY',
        'HEXALITH_ZOT_USERNAME',
        'HEXALITH_ZOT_API_KEY')) {
        $secret = [Environment]::GetEnvironmentVariable($secretName)
        if (-not [string]::IsNullOrWhiteSpace($secret)) {
            $sanitized = $sanitized.Replace($secret, '***', [StringComparison]::Ordinal)
        }
    }

    return $sanitized.Trim()
}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory)][string]$Command,
        [Parameter(Mandatory)][string[]]$Arguments
    )

    $stderrPath = [System.IO.Path]::GetTempFileName()
    try {
        try {
            $stdoutLines = @(& $Command @Arguments 2> $stderrPath)
            $exitCode = $LASTEXITCODE
            $stderr = if ((Get-Item -LiteralPath $stderrPath).Length -gt 0) {
                Get-Content -LiteralPath $stderrPath -Raw
            }
            else {
                ''
            }

            return [pscustomobject]@{
                ExitCode = $exitCode
                Stdout = $stdoutLines -join [Environment]::NewLine
                Stderr = $stderr
            }
        }
        catch {
            return [pscustomobject]@{
                ExitCode = 127
                Stdout = ''
                Stderr = $_.Exception.Message
            }
        }
    }
    finally {
        Remove-Item -LiteralPath $stderrPath -Force -ErrorAction SilentlyContinue
    }
}

function Get-FailureText {
    param([Parameter(Mandatory)]$Result)

    return Protect-LogText ((@($Result.Stdout, $Result.Stderr) |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join [Environment]::NewLine)
}

function Connect-ContainerRegistry {
    $username = $env:HEXALITH_ZOT_USERNAME
    $apiKey = $env:HEXALITH_ZOT_API_KEY
    if ([string]::IsNullOrWhiteSpace($username) -or [string]::IsNullOrWhiteSpace($apiKey)) {
        return [pscustomobject]@{
            ExitCode = 1
            Stdout = ''
            Stderr = 'Container publication requires HEXALITH_ZOT_USERNAME and HEXALITH_ZOT_API_KEY.'
        }
    }

    $stderrPath = [System.IO.Path]::GetTempFileName()
    try {
        try {
            $stdoutLines = @($apiKey | & docker login $Registry --username $username --password-stdin 2> $stderrPath)
            $exitCode = $LASTEXITCODE
            $stderr = if ((Get-Item -LiteralPath $stderrPath).Length -gt 0) {
                Get-Content -LiteralPath $stderrPath -Raw
            }
            else {
                ''
            }

            return [pscustomobject]@{
                ExitCode = $exitCode
                Stdout = $stdoutLines -join [Environment]::NewLine
                Stderr = $stderr
            }
        }
        catch {
            return [pscustomobject]@{
                ExitCode = 127
                Stdout = ''
                Stderr = $_.Exception.Message
            }
        }
    }
    finally {
        Remove-Item -LiteralPath $stderrPath -Force -ErrorAction SilentlyContinue
    }
}

function Get-ContainerStatus {
    param([Parameter(Mandatory)][array]$Outcomes)

    $failed = @($Outcomes | Where-Object status -eq 'failed')
    $succeeded = @($Outcomes | Where-Object status -eq 'succeeded')
    if ($failed.Count -eq 0 -and $succeeded.Count -eq $images.Count) {
        return 'succeeded'
    }

    if ($succeeded.Count -gt 0) {
        return 'partial-publish'
    }

    return 'publish-failed'
}

function Write-ContainerSummary {
    param(
        [Parameter(Mandatory)][string]$Status,
        [Parameter(Mandatory)][datetime]$StartedAt,
        [Parameter(Mandatory)][array]$Outcomes,
        [Parameter(Mandatory)][string]$SummaryPath
    )

    $succeeded = @($Outcomes | Where-Object status -eq 'succeeded')
    $failed = @($Outcomes | Where-Object status -eq 'failed')
    $notAttempted = @($Outcomes | Where-Object status -eq 'not-attempted')
    $summary = [ordered]@{
        schemaVersion = 1
        artifactKind = 'container-images'
        status = $Status
        version = $Version
        push = [bool]$Push
        packageDirectory = [System.IO.Path]::GetRelativePath($repoRoot, $outputPath).Replace('\', '/')
        source = $Registry
        startedAt = $StartedAt.ToString('o')
        completedAt = [DateTime]::UtcNow.ToString('o')
        images = $Outcomes
        pushed = @($succeeded | ForEach-Object image)
        failed = @($failed | ForEach-Object {
            [ordered]@{ package = $_.image; exitCode = $_.exitCode; error = $_.error }
        })
        notAttempted = @($notAttempted | ForEach-Object {
            [ordered]@{ package = $_.image; reason = $_.error }
        })
        recovery = 'Rerun the Release workflow. Matching immutable remote tags are reconciled by digest; conflicting tags fail closed.'
    }
    $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $SummaryPath -Encoding utf8
    Write-Host "Container publication summary: $SummaryPath ($Status)"

    foreach ($failure in $failed) {
        Write-Warning "Container member $($failure.image) failed: $($failure.error)"
    }
}

function New-Outcome {
    param(
        [Parameter(Mandatory)]$Image,
        [Parameter(Mandatory)][string]$Status,
        [AllowNull()][Nullable[int]]$ExitCode,
        [AllowNull()][string]$Error,
        [AllowNull()][string]$Disposition
    )

    return [ordered]@{
        name = $Image.name
        image = "$Registry/$($Image.repository):$Version"
        status = $Status
        exitCode = $ExitCode
        error = $Error
        disposition = $Disposition
    }
}

function Build-ContainerArchive {
    param([Parameter(Mandatory)]$Image)

    Remove-Item -LiteralPath $Image.archive -Force -ErrorAction SilentlyContinue
    $arguments = @(
        'publish',
        $Image.project,
        '--configuration', 'Release',
        '-t:PublishContainer',
        "-p:Version=$Version",
        "-p:PackageVersion=$Version",
        "-p:ContainerRegistry=$Registry",
        "-p:ContainerRepository=$($Image.repository)",
        "-p:ContainerImageTag=$Version",
        '-p:ContinuousIntegrationBuild=true',
        "-p:ContainerArchiveOutputPath=$($Image.archive)"
    )
    $result = Invoke-NativeCommand -Command 'dotnet' -Arguments $arguments
    if ($result.ExitCode -ne 0) {
        return New-Outcome -Image $Image -Status 'failed' -ExitCode $result.ExitCode -Error (Get-FailureText $result) -Disposition 'build-failed'
    }

    if (-not (Test-Path -LiteralPath $Image.archive) -or (Get-Item -LiteralPath $Image.archive).Length -eq 0) {
        return New-Outcome -Image $Image -Status 'failed' -ExitCode 1 -Error "dotnet publish exited successfully but did not create non-empty archive '$($Image.archive)'." -Disposition 'archive-missing'
    }

    return New-Outcome -Image $Image -Status 'succeeded' -ExitCode 0 -Error $null -Disposition 'built'
}

function Publish-ContainerArchive {
    param([Parameter(Mandatory)]$Image)

    $imageReference = "$Registry/$($Image.repository):$Version"
    if (-not (Test-Path -LiteralPath $Image.archive) -or (Get-Item -LiteralPath $Image.archive).Length -eq 0) {
        return New-Outcome -Image $Image -Status 'failed' -ExitCode 1 -Error "Prebuilt archive '$($Image.archive)' is required before release publication." -Disposition 'archive-missing'
    }

    $load = Invoke-NativeCommand -Command 'docker' -Arguments @('load', '--input', $Image.archive)
    if ($load.ExitCode -ne 0) {
        return New-Outcome -Image $Image -Status 'failed' -ExitCode $load.ExitCode -Error (Get-FailureText $load) -Disposition 'load-failed'
    }

    $loadOutput = @($load.Stdout, $load.Stderr) -join [Environment]::NewLine
    $loadedMatches = [regex]::Matches($loadOutput, '(?m)^Loaded image(?<id> ID)?:\s*(?<reference>\S+)\s*$')
    if ($loadedMatches.Count -ne 1) {
        $loadEvidence = Get-FailureText $load
        if ([string]::IsNullOrWhiteSpace($loadEvidence)) {
            $loadEvidence = '<no docker load output>'
        }

        return New-Outcome -Image $Image -Status 'failed' -ExitCode 1 -Error "Expected exactly one loaded image reference for '$imageReference', but found $($loadedMatches.Count). Docker load output: $loadEvidence" -Disposition 'load-reference-invalid'
    }

    $loadedMatch = $loadedMatches[0]
    $loadedReference = $loadedMatch.Groups['reference'].Value
    $archiveReference = "$($Image.repository):$Version"
    $isImageId = $loadedMatch.Groups['id'].Success -and $loadedReference -match '^sha256:[0-9a-fA-F]{64}$'
    $isExpectedReference =
        [string]::Equals($loadedReference, $archiveReference, [StringComparison]::Ordinal) -or
        [string]::Equals($loadedReference, $imageReference, [StringComparison]::Ordinal)
    if (-not $isImageId -and -not $isExpectedReference) {
        return New-Outcome -Image $Image -Status 'failed' -ExitCode 1 -Error "Loaded image reference '$loadedReference' does not match expected archive reference '$archiveReference' or canonical reference '$imageReference'." -Disposition 'load-reference-invalid'
    }

    if (-not [string]::Equals($loadedReference, $imageReference, [StringComparison]::Ordinal)) {
        $tag = Invoke-NativeCommand -Command 'docker' -Arguments @('tag', $loadedReference, $imageReference)
        if ($tag.ExitCode -ne 0) {
            $tagEvidence = @(
                "Docker load: $(Get-FailureText $load)",
                "Docker tag: $(Get-FailureText $tag)"
            ) -join [Environment]::NewLine
            return New-Outcome -Image $Image -Status 'failed' -ExitCode $tag.ExitCode -Error $tagEvidence -Disposition 'tag-failed'
        }
    }

    $localInspect = Invoke-NativeCommand -Command 'docker' -Arguments @('image', 'inspect', $imageReference, '--format={{.Id}}')
    if ($localInspect.ExitCode -ne 0) {
        return New-Outcome -Image $Image -Status 'failed' -ExitCode $localInspect.ExitCode -Error (Get-FailureText $localInspect) -Disposition 'inspect-failed'
    }
    $localImageDigest = $localInspect.Stdout.Trim()

    $remoteInspect = Invoke-NativeCommand -Command 'docker' -Arguments @('manifest', 'inspect', '--verbose', $imageReference)
    if ($remoteInspect.ExitCode -eq 0) {
        try {
            $remoteManifest = $remoteInspect.Stdout | ConvertFrom-Json
            $remoteImageDigest = [string]$remoteManifest.SchemaV2Manifest.config.digest
        }
        catch {
            return New-Outcome -Image $Image -Status 'failed' -ExitCode 1 -Error "Remote manifest for '$imageReference' was not valid JSON: $(Protect-LogText $_.Exception.Message)" -Disposition 'remote-inspect-failed'
        }

        if ([string]::IsNullOrWhiteSpace($remoteImageDigest)) {
            return New-Outcome -Image $Image -Status 'failed' -ExitCode 1 -Error "Remote manifest for '$imageReference' did not expose a single-platform config digest." -Disposition 'remote-inspect-failed'
        }

        if (-not [string]::Equals($remoteImageDigest, $localImageDigest, [StringComparison]::OrdinalIgnoreCase)) {
            return New-Outcome -Image $Image -Status 'failed' -ExitCode 1 -Error "Immutable tag '$imageReference' already exists with config digest '$remoteImageDigest', expected '$localImageDigest'." -Disposition 'digest-conflict'
        }

        return New-Outcome -Image $Image -Status 'succeeded' -ExitCode 0 -Error $null -Disposition 'already-present'
    }

    $push = Invoke-NativeCommand -Command 'docker' -Arguments @('push', $imageReference)
    if ($push.ExitCode -ne 0) {
        $pushFailure = Get-FailureText $push
        $combinedFailure = @(
            "Remote inspection before push: $(Get-FailureText $remoteInspect)",
            "Push: $pushFailure"
        ) -join [Environment]::NewLine
        if ($pushFailure -match '(?i)(unauthorized|authentication required|requested access.+denied|access denied)') {
            $authorizationFailure = "Container registry rejected write authorization for '$imageReference'. Confirm the HEXALITH_ZOT_USERNAME/API-key pair and grant push access to repository '$($Image.repository)'." + [Environment]::NewLine + $combinedFailure
            return New-Outcome -Image $Image -Status 'failed' -ExitCode $push.ExitCode -Error $authorizationFailure -Disposition 'authorization-failed'
        }

        return New-Outcome -Image $Image -Status 'failed' -ExitCode $push.ExitCode -Error $combinedFailure -Disposition 'push-failed'
    }

    return New-Outcome -Image $Image -Status 'succeeded' -ExitCode 0 -Error $null -Disposition 'pushed'
}

$startedAt = [DateTime]::UtcNow
$summaryName = if ($Push) { 'publish-summary.json' } else { 'build-summary.json' }
$summaryPath = Join-Path $outputPath $summaryName
$deploymentPath = Join-Path $outputPath 'production-deployment.yaml'
Remove-Item -LiteralPath $summaryPath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $deploymentPath -Force -ErrorAction SilentlyContinue

Push-Location $repoRoot
try {
    $serverImage = "$Registry/$RepositoryPrefix`:$Version"
    $mcpImage = "$Registry/$RepositoryPrefix-mcp`:$Version"
    $render = Invoke-NativeCommand -Command 'pwsh' -Arguments @(
        '-NoLogo', '-NoProfile', '-File', './tools/render-production-deployment.ps1',
        '-Version', $Version,
        '-ServerImage', $serverImage,
        '-McpImage', $mcpImage,
        '-OutputPath', $deploymentPath)
    if ($render.ExitCode -ne 0) {
        $reason = "Production release deployment render failed: $(Get-FailureText $render)"
        $outcomes = @($images | ForEach-Object {
            New-Outcome -Image $_ -Status 'not-attempted' -ExitCode $null -Error $reason -Disposition 'render-failed'
        })
        Write-ContainerSummary -Status 'publish-failed' -StartedAt $startedAt -Outcomes $outcomes -SummaryPath $summaryPath
        throw $reason
    }

    if ($Push) {
        $login = Connect-ContainerRegistry
        if ($login.ExitCode -ne 0) {
            $reason = "Container registry authentication failed: $(Get-FailureText $login)"
            $outcomes = @($images | ForEach-Object {
                New-Outcome -Image $_ -Status 'not-attempted' -ExitCode $null -Error $reason -Disposition 'authentication-failed'
            })
            Write-ContainerSummary -Status 'publish-failed' -StartedAt $startedAt -Outcomes $outcomes -SummaryPath $summaryPath
            throw $reason
        }

        Write-Host "Stored container registry credentials for $Registry; repository write authorization is verified separately."
    }

    $outcomes = foreach ($image in $images) {
        $imageReference = "$Registry/$($image.repository):$Version"
        if ($Push) {
            Write-Host "Publishing prebuilt container unit member $imageReference"
            Publish-ContainerArchive -Image $image
        }
        else {
            Write-Host "Building container unit member $imageReference"
            Build-ContainerArchive -Image $image
        }
    }

    $status = Get-ContainerStatus -Outcomes $outcomes
    Write-ContainerSummary -Status $status -StartedAt $startedAt -Outcomes $outcomes -SummaryPath $summaryPath

    if ($status -ne 'succeeded') {
        if ($env:GITHUB_ACTIONS -eq 'true') {
            $failedCount = @($outcomes | Where-Object status -eq 'failed').Count
            $title = if ($status -eq 'partial-publish') { 'PARTIAL CONTAINER PUBLISH' } elseif ($Push) { 'CONTAINER PUBLISH FAILED' } else { 'CONTAINER BUILD FAILED' }
            Write-Host "::error title=$title::$failedCount of 2 release images failed for $Version."
        }

        throw "Container publication finished with status '$status'."
    }
}
finally {
    Pop-Location
}
