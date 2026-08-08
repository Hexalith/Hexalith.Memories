[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$SourceRoot,

    [Parameter(Mandatory)]
    [string]$ContainerSummaryPath,

    [Parameter(Mandatory)]
    [string]$DeploymentPath,

    [string]$Repository = $env:GITHUB_REPOSITORY,

    [string]$Registry = 'registry.hexalith.com',

    [string]$OutputDirectory = 'artifacts/recovery',

    [string]$NuGetBaseUri = 'https://api.nuget.org/v3-flatcontainer',

    [switch]$Publish
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

if (-not $PSBoundParameters.ContainsKey('Registry') -and
    -not [string]::IsNullOrWhiteSpace($env:HEXALITH_ZOT_REGISTRY)) {
    $Registry = $env:HEXALITH_ZOT_REGISTRY
}

if ($Version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$') {
    throw "Version '$Version' is not a valid image-taggable semantic version."
}

if ([string]::IsNullOrWhiteSpace($Repository)) {
    throw 'GITHUB_REPOSITORY is required to complete a partial release.'
}

if ([string]::IsNullOrWhiteSpace($Registry) -or $Registry -match '[/\\\s]') {
    throw "Container registry '$Registry' must be a host name with an optional port."
}

$resolvedSourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
$resolvedContainerSummary = (Resolve-Path -LiteralPath $ContainerSummaryPath).Path
$resolvedDeployment = (Resolve-Path -LiteralPath $DeploymentPath).Path
$outputPath = if ([IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory
}
else {
    Join-Path $resolvedSourceRoot $OutputDirectory
}
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null

$evidencePath = Join-Path $outputPath 'completion-summary.json'
$packagePath = Join-Path $outputPath 'packages'
$assetPath = Join-Path $outputPath 'assets'
$downloadPath = Join-Path $outputPath 'verified-assets'
$tag = "v$Version"
$startedAt = [DateTime]::UtcNow
$packageEvidence = @()
$imageEvidence = @()
$releaseAssetEvidence = @()
$closedIssues = @()
$status = 'failed'
$failure = $null

function Protect-LogText {
    param([AllowEmptyString()][string]$Text)

    $sanitized = $Text
    foreach ($secretName in @(
        'GITHUB_TOKEN',
        'GH_TOKEN',
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

    $stderrPath = [IO.Path]::GetTempFileName()
    try {
        try {
            $stdout = @(& $Command @Arguments 2> $stderrPath) -join [Environment]::NewLine
            $exitCode = $LASTEXITCODE
            $stderr = if ((Get-Item -LiteralPath $stderrPath).Length -gt 0) {
                Get-Content -LiteralPath $stderrPath -Raw
            }
            else {
                ''
            }

            return [pscustomobject]@{
                ExitCode = $exitCode
                Stdout = $stdout
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

function Assert-NativeSuccess {
    param(
        [Parameter(Mandatory)]$Result,
        [Parameter(Mandatory)][string]$Action
    )

    if ($Result.ExitCode -ne 0) {
        $detail = Protect-LogText ((@($Result.Stdout, $Result.Stderr) |
                    Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join [Environment]::NewLine)
        throw "$Action failed: $detail"
    }
}

function Get-PackageMetadata {
    param([Parameter(Mandatory)][string]$PackageFile)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($PackageFile)
    try {
        $entry = $archive.Entries |
            Where-Object { $_.FullName.EndsWith('.nuspec', [StringComparison]::OrdinalIgnoreCase) } |
            Select-Object -First 1
        if ($null -eq $entry) {
            throw "Package '$PackageFile' does not contain a nuspec."
        }

        $reader = [IO.StreamReader]::new($entry.Open())
        try {
            [xml]$nuspec = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }

    $metadata = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']")
    return [pscustomobject]@{
        Id = $metadata.SelectSingleNode("*[local-name()='id']").InnerText
        Version = $metadata.SelectSingleNode("*[local-name()='version']").InnerText
    }
}

try {
    $inventoryPath = Join-Path $resolvedSourceRoot 'tools/release-packages.json'
    if (-not (Test-Path -LiteralPath $inventoryPath)) {
        throw "Tagged release inventory not found: $inventoryPath"
    }

    $tagCheck = Invoke-NativeCommand -Command 'git' -Arguments @('-C', $resolvedSourceRoot, 'show-ref', '--verify', '--quiet', "refs/tags/$tag")
    if ($tagCheck.ExitCode -ne 0) {
        throw "Trusted release tag '$tag' does not exist in tagged source."
    }

    $headResult = Invoke-NativeCommand -Command 'git' -Arguments @('-C', $resolvedSourceRoot, 'rev-parse', 'HEAD')
    Assert-NativeSuccess -Result $headResult -Action 'Resolve checked-out recovery source'
    $tagCommitResult = Invoke-NativeCommand -Command 'git' -Arguments @('-C', $resolvedSourceRoot, 'rev-list', '-n', '1', $tag)
    Assert-NativeSuccess -Result $tagCommitResult -Action "Resolve trusted release tag '$tag'"
    if (-not [string]::Equals($headResult.Stdout.Trim(), $tagCommitResult.Stdout.Trim(), [StringComparison]::OrdinalIgnoreCase)) {
        throw "Recovery source HEAD does not match trusted release tag '$tag'."
    }

    $containerSummary = Get-Content -LiteralPath $resolvedContainerSummary -Raw | ConvertFrom-Json
    if ($containerSummary.status -ne 'succeeded' -or $containerSummary.version -ne $Version) {
        throw "Container recovery did not report success for '$Version'."
    }

    $images = @($containerSummary.images)
    # Required baseline for every Memories release; access-telemetry members appear only on
    # tags that published them (v2.16.0+). Accept the exact set publish-containers emitted.
    $requiredImages = @(
        "$Registry/memories:$Version",
        "$Registry/memories-mcp:$Version"
    )
    $allowedImages = @(
        $requiredImages +
        @(
            "$Registry/memories-access-telemetry:$Version",
            "$Registry/memories-access-telemetry-clock:$Version"
        )
    )
    $summaryImages = @($images.image)
    if ($summaryImages.Count -notin @(2, 4) -or
        @($requiredImages | Where-Object { $_ -notin $summaryImages }).Count -gt 0 -or
        @($summaryImages | Where-Object { $_ -notin $allowedImages }).Count -gt 0 -or
        $summaryImages.Count -ne @($summaryImages | Select-Object -Unique).Count) {
        throw "Container recovery evidence must include the Server/MCP unit as a 2- or 4-image set of allowed repositories for '$Version'."
    }

    foreach ($image in $images) {
        if ($image.status -ne 'succeeded' -or $image.disposition -notin @('pushed', 'already-present')) {
            throw "Container recovery evidence for '$($image.image)' is not verified."
        }

        $imageEvidence += [ordered]@{
            image = [string]$image.image
            disposition = [string]$image.disposition
        }
    }

    $inventory = Get-Content -LiteralPath $inventoryPath -Raw | ConvertFrom-Json
    $packages = @($inventory.packages)
    if ($packages.Count -ne 9) {
        throw "Tagged release inventory must contain exactly nine packages; found $($packages.Count)."
    }

    foreach ($path in @($packagePath, $assetPath, $downloadPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Recurse -Force
        }
        New-Item -ItemType Directory -Path $path | Out-Null
    }

    foreach ($package in $packages) {
        $packageId = [string]$package.packageId
        $packageFileName = "$packageId.$Version.nupkg"
        $packageFile = Join-Path $packagePath $packageFileName
        $lowerId = $packageId.ToLowerInvariant()
        $lowerVersion = $Version.ToLowerInvariant()
        $packageUri = "$($NuGetBaseUri.TrimEnd('/'))/$lowerId/$lowerVersion/$lowerId.$lowerVersion.nupkg"
        try {
            Invoke-WebRequest -Uri $packageUri -OutFile $packageFile -MaximumRedirection 3
        }
        catch {
            throw "Published NuGet package '$packageId' version '$Version' could not be downloaded."
        }

        if ((Get-Item -LiteralPath $packageFile).Length -eq 0) {
            throw "Published NuGet package '$packageFileName' was empty."
        }

        $metadata = Get-PackageMetadata -PackageFile $packageFile
        if (-not [string]::Equals($metadata.Id, $packageId, [StringComparison]::Ordinal) -or
            -not [string]::Equals($metadata.Version, $Version, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Published NuGet package '$packageFileName' metadata did not match the tagged inventory/version."
        }

        $assetFile = Join-Path $assetPath $packageFileName
        Copy-Item -LiteralPath $packageFile -Destination $assetFile
        $packageEvidence += [ordered]@{
            packageId = $packageId
            file = $packageFileName
            sha256 = (Get-FileHash -LiteralPath $assetFile -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }

    $deploymentAsset = Join-Path $assetPath 'hexalith-memories-production.yaml'
    Copy-Item -LiteralPath $resolvedDeployment -Destination $deploymentAsset
    $deploymentText = Get-Content -LiteralPath $deploymentAsset -Raw
    foreach ($expectedImage in $summaryImages) {
        if (-not $deploymentText.Contains($expectedImage, [StringComparison]::Ordinal)) {
            throw "Versioned deployment does not reference expected image '$expectedImage'."
        }
    }

    $expectedAssetFiles = @(
        $packageEvidence | ForEach-Object { Join-Path $assetPath $_.file }
    ) + @($deploymentAsset)
    $expectedAssetNames = @($expectedAssetFiles | ForEach-Object { Split-Path -Leaf $_ } | Sort-Object)

    if ($Publish) {
        $releaseView = Invoke-NativeCommand -Command 'gh' -Arguments @(
            'release', 'view', $tag, '--repo', $Repository,
            '--json', 'tagName,isDraft,isPrerelease,assets')
        $releaseExists = $releaseView.ExitCode -eq 0
        if (-not $releaseExists -and
            (Protect-LogText ((@($releaseView.Stdout, $releaseView.Stderr) -join [Environment]::NewLine))) -notmatch '(?i)(release not found|HTTP 404)') {
            Assert-NativeSuccess -Result $releaseView -Action "Inspect GitHub Release '$tag'"
        }

        if (-not $releaseExists) {
            $previousTagResult = Invoke-NativeCommand -Command 'git' -Arguments @(
                '-C', $resolvedSourceRoot, 'describe', '--tags', '--abbrev=0', "$tag^")
            Assert-NativeSuccess -Result $previousTagResult -Action "Resolve the tag preceding '$tag'"
            $arguments = @(
                'release', 'create', $tag, '--repo', $Repository,
                '--verify-tag', '--title', $tag, '--generate-notes',
                '--notes-start-tag', $previousTagResult.Stdout.Trim()
            ) + $expectedAssetFiles
            $create = Invoke-NativeCommand -Command 'gh' -Arguments $arguments
            Assert-NativeSuccess -Result $create -Action "Create GitHub Release '$tag'"
        }
        else {
            $release = $releaseView.Stdout | ConvertFrom-Json
            if ($release.tagName -ne $tag -or $release.isDraft -or $release.isPrerelease) {
                throw "Existing GitHub Release '$tag' does not match the required stable release state."
            }

            $existingAssetNames = @($release.assets | ForEach-Object { [string]$_.name })
            $unexpectedAssets = @($existingAssetNames | Where-Object { $_ -notin $expectedAssetNames })
            if ($unexpectedAssets.Count -gt 0) {
                throw "Existing GitHub Release '$tag' contains unexpected assets: $($unexpectedAssets -join ', ')."
            }

            $missingAssetFiles = @($expectedAssetFiles | Where-Object { (Split-Path -Leaf $_) -notin $existingAssetNames })
            if ($missingAssetFiles.Count -gt 0) {
                $upload = Invoke-NativeCommand -Command 'gh' -Arguments (@(
                        'release', 'upload', $tag, '--repo', $Repository) + $missingAssetFiles)
                Assert-NativeSuccess -Result $upload -Action "Upload missing assets for GitHub Release '$tag'"
            }
        }

        $download = Invoke-NativeCommand -Command 'gh' -Arguments @(
            'release', 'download', $tag, '--repo', $Repository, '--dir', $downloadPath)
        Assert-NativeSuccess -Result $download -Action "Download GitHub Release '$tag' assets for verification"

        $downloadedAssets = @(Get-ChildItem -LiteralPath $downloadPath -File)
        $downloadedNames = @($downloadedAssets.Name | Sort-Object)
        if (Compare-Object -ReferenceObject $expectedAssetNames -DifferenceObject $downloadedNames) {
            throw "GitHub Release '$tag' asset inventory did not match the required release unit."
        }

        foreach ($expectedAsset in $expectedAssetFiles) {
            $name = Split-Path -Leaf $expectedAsset
            $expectedHash = (Get-FileHash -LiteralPath $expectedAsset -Algorithm SHA256).Hash
            $downloadedHash = (Get-FileHash -LiteralPath (Join-Path $downloadPath $name) -Algorithm SHA256).Hash
            if (-not [string]::Equals($expectedHash, $downloadedHash, [StringComparison]::OrdinalIgnoreCase)) {
                throw "GitHub Release '$tag' asset '$name' did not match the verified source artifact."
            }

            $releaseAssetEvidence += [ordered]@{
                file = $name
                sha256 = $expectedHash.ToLowerInvariant()
            }
        }

        $issueTitle = "PARTIAL PUBLISH $Version - manual reconciliation required"
        $issuesResult = Invoke-NativeCommand -Command 'gh' -Arguments @(
            'issue', 'list', '--repo', $Repository, '--state', 'open',
            '--search', $issueTitle, '--json', 'number,title')
        Assert-NativeSuccess -Result $issuesResult -Action 'Query partial-publish incidents'
        $issues = @($issuesResult.Stdout | ConvertFrom-Json)
        $matchingIssues = @($issues | Where-Object { $_.title -eq $issueTitle })
        if ($matchingIssues.Count -gt 1) {
            throw "Multiple open partial-publish incidents match version '$Version'."
        }

        if ($matchingIssues.Count -eq 1) {
            $issueNumber = [string]$matchingIssues[0].number
            $comment = Invoke-NativeCommand -Command 'gh' -Arguments @(
                'issue', 'comment', $issueNumber, '--repo', $Repository,
                '--body', "Recovery verified tag $tag, nine NuGet packages, $($images.Count) immutable image(s), the versioned deployment, and all GitHub Release assets. Evidence: partial-release-completion-v$Version.")
            Assert-NativeSuccess -Result $comment -Action "Attach recovery evidence to issue #$issueNumber"
            $close = Invoke-NativeCommand -Command 'gh' -Arguments @(
                'issue', 'close', $issueNumber, '--repo', $Repository,
                '--reason', 'completed')
            Assert-NativeSuccess -Result $close -Action "Close recovered issue #$issueNumber"
            $closedIssues += [int]$issueNumber
        }
    }

    $status = if ($Publish) { 'succeeded' } else { 'validated' }
}
catch {
    $failure = Protect-LogText $_.Exception.Message
    throw $failure
}
finally {
    $sourceCommitResult = Invoke-NativeCommand -Command 'git' -Arguments @(
        '-C', $resolvedSourceRoot, 'rev-list', '-n', '1', $tag)
    $summary = [ordered]@{
        schemaVersion = 1
        artifactKind = 'partial-release-completion'
        status = $status
        version = $Version
        tag = $tag
        sourceCommit = if ($sourceCommitResult.ExitCode -eq 0) { $sourceCommitResult.Stdout.Trim() } else { $null }
        startedAt = $startedAt.ToString('o')
        completedAt = [DateTime]::UtcNow.ToString('o')
        packages = $packageEvidence
        images = $imageEvidence
        releaseAssets = $releaseAssetEvidence
        closedIssues = $closedIssues
        error = $failure
    }
    $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $evidencePath -Encoding utf8
    Write-Host "Partial-release completion summary: $evidencePath ($status)"
}
