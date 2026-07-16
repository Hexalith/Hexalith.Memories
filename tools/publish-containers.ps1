[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [string]$Registry = 'registry.hexalith.com',

    [string]$RepositoryPrefix = 'memories',

    [string]$OutputDirectory = 'artifacts/containers/release',

    [string]$RepositoryRoot,

    [ValidateNotNullOrEmpty()]
    [string]$SkopeoCommand = 'skopeo',

    [switch]$Push
)

$ErrorActionPreference = 'Stop'

if (-not $PSBoundParameters.ContainsKey('Registry') -and
    -not [string]::IsNullOrWhiteSpace($env:HEXALITH_ZOT_REGISTRY)) {
    $Registry = $env:HEXALITH_ZOT_REGISTRY
}

$registryUri = $null
if ($Registry -cne $Registry.Trim() -or
    $Registry -match '\s' -or
    -not [Uri]::TryCreate("https://$Registry/", [UriKind]::Absolute, [ref]$registryUri) -or
    $registryUri.HostNameType -eq [UriHostNameType]::Unknown -or
    -not [string]::IsNullOrEmpty($registryUri.UserInfo) -or
    $registryUri.AbsolutePath -ne '/' -or
    -not [string]::IsNullOrEmpty($registryUri.Query) -or
    -not [string]::IsNullOrEmpty($registryUri.Fragment)) {
    throw "Registry '$Registry' is invalid. HEXALITH_ZOT_REGISTRY must be a registry host with an optional port and no scheme, path, credentials, query, fragment, or whitespace."
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
    $secretValues = @(
        foreach ($secretName in @(
            'GITHUB_TOKEN',
            'GH_TOKEN',
            'CR_PAT',
            'NUGET_API_KEY',
            'HEXALITH_ZOT_USERNAME',
            'HEXALITH_ZOT_API_KEY')) {
            $secret = [Environment]::GetEnvironmentVariable($secretName)
            if (-not [string]::IsNullOrWhiteSpace($secret)) {
                $secret
            }
        }
    )
    foreach ($secret in @($secretValues | Select-Object -Unique | Sort-Object -Property Length -Descending)) {
        $sanitized = $sanitized.Replace($secret, '***', [StringComparison]::Ordinal)
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

function New-RegistryAuthFile {
    # The docker daemon must not be used for release pushes: this registry allows anonymous
    # read, so zot answers the daemon's unauthenticated GET /v2/ ping with HTTP 200 and the
    # daemon then never sends credentials on push (project-zot/zot#2928). skopeo honors the Basic
    # challenge advertised by Zot at /v2/, and this scoped credential file hands it the exact bytes
    # that verify-container-registry.ps1 proved carry write authorization. The verifier also fails
    # closed if an ingress replaces /v2/ with a synthetic response that strips that challenge.
    $username = $env:HEXALITH_ZOT_USERNAME
    $apiKey = $env:HEXALITH_ZOT_API_KEY
    $credentialBytes = [Text.Encoding]::UTF8.GetBytes("$username`:$apiKey")
    $credential = [Convert]::ToBase64String($credentialBytes)
    [Array]::Clear($credentialBytes, 0, $credentialBytes.Length)

    $authFilePath = [System.IO.Path]::GetTempFileName()
    $authDocument = [ordered]@{
        auths = [ordered]@{
            $Registry = [ordered]@{ auth = $credential }
        }
    }
    try {
        $authDocument | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $authFilePath -Encoding utf8
        $credential = $null
        if (-not $IsWindows) {
            & chmod 600 $authFilePath
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to restrict registry credential file permissions (chmod exited with $LASTEXITCODE)."
            }
        }
    }
    catch {
        Remove-Item -LiteralPath $authFilePath -Force -ErrorAction SilentlyContinue
        throw
    }

    return $authFilePath
}

function Get-ManifestConfigDigest {
    param([AllowEmptyString()][string]$ManifestJson)

    try {
        $manifest = $ManifestJson | ConvertFrom-Json
    }
    catch {
        return $null
    }

    $digest = [string]$manifest.config.digest
    if ([string]::IsNullOrWhiteSpace($digest)) {
        return $null
    }

    return $digest
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
        recovery = 'If the exact tag exists, repair registry authorization and dispatch Recover Partial Release from main. Matching immutable remote tags are reconciled by config digest; conflicting tags fail closed.'
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
    param(
        [Parameter(Mandatory)]$Image,
        [Parameter(Mandatory)][string]$AuthFile
    )

    $imageReference = "$Registry/$($Image.repository):$Version"
    if (-not (Test-Path -LiteralPath $Image.archive) -or (Get-Item -LiteralPath $Image.archive).Length -eq 0) {
        return New-Outcome -Image $Image -Status 'failed' -ExitCode 1 -Error "Prebuilt archive '$($Image.archive)' is required before release publication." -Disposition 'archive-missing'
    }

    $archiveInspect = Invoke-NativeCommand -Command $SkopeoCommand -Arguments @('inspect', '--raw', "docker-archive:$($Image.archive)")
    if ($archiveInspect.ExitCode -ne 0) {
        return New-Outcome -Image $Image -Status 'failed' -ExitCode $archiveInspect.ExitCode -Error (Get-FailureText $archiveInspect) -Disposition 'archive-inspect-failed'
    }

    $localImageDigest = Get-ManifestConfigDigest -ManifestJson $archiveInspect.Stdout
    if ($null -eq $localImageDigest) {
        return New-Outcome -Image $Image -Status 'failed' -ExitCode 1 -Error "Prebuilt archive '$($Image.archive)' did not expose a single-platform config digest." -Disposition 'archive-inspect-failed'
    }

    # Fail closed only on positive evidence of a mislabeled archive; image-ID-only archives
    # (no embedded references) were accepted by the previous daemon-based flow and still are.
    $archiveReferences = Invoke-NativeCommand -Command $SkopeoCommand -Arguments @('inspect', "docker-archive:$($Image.archive)")
    if ($archiveReferences.ExitCode -eq 0) {
        $repoTags = @()
        try {
            $repoTags = @(($archiveReferences.Stdout | ConvertFrom-Json).RepoTags |
                    Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        }
        catch {
            $repoTags = @()
        }

        $expectedReferences = @("$($Image.repository):$Version", $imageReference)
        if ($repoTags.Count -gt 0 -and @($repoTags | Where-Object { $expectedReferences -ccontains $_ }).Count -eq 0) {
            $foundText = $repoTags -join ', '
            $expectedText = $expectedReferences -join ' or '
            return New-Outcome -Image $Image -Status 'failed' -ExitCode 1 -Error "Prebuilt archive '$($Image.archive)' is tagged [$foundText] but this member must publish [$expectedText]." -Disposition 'archive-reference-invalid'
        }
    }

    $remoteInspect = Invoke-NativeCommand -Command $SkopeoCommand -Arguments @('inspect', '--raw', '--authfile', $AuthFile, "docker://$imageReference")
    if ($remoteInspect.ExitCode -eq 0) {
        $remoteImageDigest = Get-ManifestConfigDigest -ManifestJson $remoteInspect.Stdout
        if ($null -eq $remoteImageDigest) {
            return New-Outcome -Image $Image -Status 'failed' -ExitCode 1 -Error "Remote manifest for '$imageReference' did not expose a single-platform config digest." -Disposition 'remote-inspect-failed'
        }

        if (-not [string]::Equals($remoteImageDigest, $localImageDigest, [StringComparison]::OrdinalIgnoreCase)) {
            return New-Outcome -Image $Image -Status 'failed' -ExitCode 1 -Error "Immutable tag '$imageReference' already exists with config digest '$remoteImageDigest', expected '$localImageDigest'." -Disposition 'digest-conflict'
        }

        return New-Outcome -Image $Image -Status 'succeeded' -ExitCode 0 -Error $null -Disposition 'already-present'
    }

    $push = Invoke-NativeCommand -Command $SkopeoCommand -Arguments @('copy', '--authfile', $AuthFile, "docker-archive:$($Image.archive)", "docker://$imageReference")
    if ($push.ExitCode -ne 0) {
        $pushFailure = Get-FailureText $push
        $combinedFailure = @(
            "Remote inspection before push: $(Get-FailureText $remoteInspect)",
            "Push: $pushFailure"
        ) -join [Environment]::NewLine
        if ($pushFailure -match '(?i)(unauthorized|authentication required|requested access.+denied|access denied|forbidden|invalid username/password)') {
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

    $authFile = $null
    if ($Push) {
        if ([string]::IsNullOrWhiteSpace($env:HEXALITH_ZOT_USERNAME) -or
            [string]::IsNullOrWhiteSpace($env:HEXALITH_ZOT_API_KEY)) {
            $reason = 'Container registry authentication failed: Container publication requires HEXALITH_ZOT_USERNAME and HEXALITH_ZOT_API_KEY.'
            $outcomes = @($images | ForEach-Object {
                New-Outcome -Image $_ -Status 'not-attempted' -ExitCode $null -Error $reason -Disposition 'authentication-failed'
            })
            Write-ContainerSummary -Status 'publish-failed' -StartedAt $startedAt -Outcomes $outcomes -SummaryPath $summaryPath
            throw $reason
        }

        if ($null -eq (Get-Command -Name $SkopeoCommand -CommandType Application -ErrorAction SilentlyContinue)) {
            $reason = "Container publication requires the '$SkopeoCommand' CLI on PATH; it pushes release images because the docker daemon never sends credentials to this registry (see docs/dev/release-runbook.md)."
            $outcomes = @($images | ForEach-Object {
                New-Outcome -Image $_ -Status 'not-attempted' -ExitCode $null -Error $reason -Disposition 'tooling-missing'
            })
            Write-ContainerSummary -Status 'publish-failed' -StartedAt $startedAt -Outcomes $outcomes -SummaryPath $summaryPath
            throw $reason
        }

        $authFile = New-RegistryAuthFile
        Write-Host "Prepared scoped registry credential file for $Registry; repository write authorization is verified separately."
    }

    try {
        $outcomes = foreach ($image in $images) {
            $imageReference = "$Registry/$($image.repository):$Version"
            if ($Push) {
                Write-Host "Publishing prebuilt container unit member $imageReference"
                Publish-ContainerArchive -Image $image -AuthFile $authFile
            }
            else {
                Write-Host "Building container unit member $imageReference"
                Build-ContainerArchive -Image $image
            }
        }
    }
    finally {
        if ($null -ne $authFile) {
            Remove-Item -LiteralPath $authFile -Force -ErrorAction SilentlyContinue
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
