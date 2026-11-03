[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [string]$PackageDirectory = 'artifacts/packages/release',

    [string]$ContainerOutputDirectory = 'artifacts/containers/release',

    [string]$SummaryDirectory = 'artifacts/release',

    [string]$DeploymentPath = 'artifacts/deployment/hexalith-memories-production.yaml',

    [string]$PowerShellExecutable = 'pwsh'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

function Resolve-RepoPath {
    param([Parameter(Mandatory)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $repoRoot $Path
}

$resolvedPackageDirectory = Resolve-RepoPath $PackageDirectory
$resolvedContainerDirectory = Resolve-RepoPath $ContainerOutputDirectory
$resolvedSummaryDirectory = Resolve-RepoPath $SummaryDirectory
$resolvedDeploymentPath = Resolve-RepoPath $DeploymentPath
$summaryPath = Join-Path $resolvedSummaryDirectory 'publish-summary.json'
New-Item -ItemType Directory -Path $resolvedSummaryDirectory -Force | Out-Null
Remove-Item -LiteralPath $summaryPath -Force -ErrorAction SilentlyContinue

$startedAt = [DateTime]::UtcNow
$families = @()

function Get-ChildSummary {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return $null
    }

    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function Invoke-Publisher {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Script,
        [Parameter(Mandatory)][string]$ChildSummaryPath,
        [string[]]$AdditionalArguments = @()
    )

    $arguments = @('-NoLogo', '-NoProfile', '-File', $Script, '-Version', $Version) + $AdditionalArguments
    $publisherOutput = @(& $PowerShellExecutable @arguments 2>&1)
    $exitCode = $LASTEXITCODE
    foreach ($line in $publisherOutput) {
        Write-Host $line
    }
    $childSummary = Get-ChildSummary -Path $ChildSummaryPath
    $status = if ($exitCode -eq 0) {
        'succeeded'
    }
    elseif ($null -ne $childSummary -and -not [string]::IsNullOrWhiteSpace([string]$childSummary.status)) {
        [string]$childSummary.status
    }
    else {
        'publish-failed'
    }

    return [ordered]@{
        name = $Name
        status = $status
        exitCode = $exitCode
        summaryPath = [System.IO.Path]::GetRelativePath($repoRoot, $ChildSummaryPath).Replace('\', '/')
        summary = $childSummary
    }
}

Push-Location $repoRoot
try {
    # All source/mapping/archive proofs and the shared publish phase precede either family.
    & python3 ./tools/container-publication-contract.py --phase authorize --version $Version `
        --output-directory $resolvedContainerDirectory
    if ($LASTEXITCODE -ne 0) {
        $families = @(foreach ($familyName in @('NuGet packages', 'container images')) {
            [ordered]@{
                name = $familyName
                status = 'not-attempted'
                exitCode = 1
                summaryPath = $null
                summary = $null
            }
        })
    }
    else {
        $families += Invoke-Publisher `
            -Name 'NuGet packages' `
            -Script './tools/publish-nuget.ps1' `
            -ChildSummaryPath (Join-Path $resolvedPackageDirectory 'publish-summary.json') `
            -AdditionalArguments @('-PackageDirectory', $resolvedPackageDirectory)

        $families += Invoke-Publisher `
            -Name 'container images' `
            -Script './tools/publish-containers.ps1' `
            -ChildSummaryPath (Join-Path $resolvedContainerDirectory 'publish-summary.json') `
            -AdditionalArguments @('-OutputDirectory', $resolvedContainerDirectory, '-PublicationContract', '-Push')
    }
}
finally {
    Pop-Location
}

$anySucceeded = @($families | Where-Object status -eq 'succeeded').Count -gt 0
$anyPartial = @($families | Where-Object status -eq 'partial-publish').Count -gt 0
$anyFailed = @($families | Where-Object status -ne 'succeeded').Count -gt 0
$status = if (-not $anyFailed) {
    'succeeded'
}
elseif ($anySucceeded -or $anyPartial) {
    'partial-publish'
}
else {
    'publish-failed'
}

$pushed = @()
$failed = @()
$notAttempted = @()
foreach ($family in $families) {
    if ($family.status -eq 'not-attempted') {
        $notAttempted += [ordered]@{
            package = $family.name
            reason = 'Frozen five-image publication authorization failed before release writes; inspect artifacts/release/container-evidence.'
        }
    }
    elseif ($null -ne $family.summary) {
        $pushed += @($family.summary.pushed | ForEach-Object { "$($family.name): $_" })
        $failed += @($family.summary.failed | ForEach-Object {
            [ordered]@{
                package = "$($family.name): $($_.package)"
                exitCode = $_.exitCode
                error = $_.error
            }
        })
        $notAttempted += @($family.summary.notAttempted | ForEach-Object {
            [ordered]@{
                package = "$($family.name): $($_.package)"
                reason = $_.reason
            }
        })
    }
    elseif ($family.status -eq 'succeeded') {
        $pushed += "$($family.name): completed"
    }
    else {
        $failed += [ordered]@{
            package = $family.name
            exitCode = $family.exitCode
            error = 'Publisher exited without writing its required child summary.'
        }
    }
}

$summary = [ordered]@{
    schemaVersion = 1
    artifactKind = 'release-artifacts'
    status = $status
    version = $Version
    sourceSha = @($families | Where-Object name -eq 'container images')[0].summary.sourceSha
    setSha256 = @($families | Where-Object name -eq 'container images')[0].summary.setSha256
    packageDirectory = [System.IO.Path]::GetRelativePath($repoRoot, $resolvedSummaryDirectory).Replace('\', '/')
    source = "NuGet and $(@($families | Where-Object name -eq 'container images')[0].summary.source)"
    startedAt = $startedAt.ToString('o')
    completedAt = [DateTime]::UtcNow.ToString('o')
    families = $families
    pushed = $pushed
    failed = $failed
    notAttempted = $notAttempted
    recovery = 'If the exact tag or any package exists, do not republish or delete NuGet packages. For current ten-package releases, manually reconcile NuGet publication and remaining artifacts with the release owner. Recover Partial Release is reserved for historical nine-package tagged releases.'
}
$summary | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $summaryPath -Encoding utf8
Write-Host "Aggregate release publication summary: $summaryPath ($status)"

if ($status -ne 'succeeded') {
    throw "Release publication failed with aggregate status '$status'. See $summaryPath for both publisher outcomes."
}

Push-Location $repoRoot
try {
    & python3 (Join-Path $PSScriptRoot 'archive-release-evidence.py') `
        --container-directory $resolvedContainerDirectory --package-directory $resolvedPackageDirectory `
        --summary-directory $resolvedSummaryDirectory --deployment-path $resolvedDeploymentPath `
        --output (Join-Path $resolvedSummaryDirectory 'hexalith-memories-release-evidence.tar.gz')
    $evidenceExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}
if ($evidenceExitCode -ne 0) {
    $summary.status = 'partial-publish'
    $summary.evidenceStatus = 'incomplete'
    $summary.failed += [ordered]@{
        package = 'release evidence'
        exitCode = $evidenceExitCode
        error = 'Publication side effects completed, but the complete durable release evidence packet was not retained.'
    }
    $summary.completedAt = [DateTime]::UtcNow.ToString('o')
    $summary | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $summaryPath -Encoding utf8
    throw 'Successful publication is missing the complete durable release evidence packet.'
}
