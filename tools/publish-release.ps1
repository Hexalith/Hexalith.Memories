[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [string]$PackageDirectory = 'artifacts/packages/release',

    [string]$ContainerOutputDirectory = 'artifacts/containers/release',

    [string]$SummaryDirectory = 'artifacts/release',

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
    $families += Invoke-Publisher `
        -Name 'NuGet packages' `
        -Script './tools/publish-nuget.ps1' `
        -ChildSummaryPath (Join-Path $resolvedPackageDirectory 'publish-summary.json') `
        -AdditionalArguments @('-PackageDirectory', $resolvedPackageDirectory)

    $families += Invoke-Publisher `
        -Name 'container images' `
        -Script './tools/publish-containers.ps1' `
        -ChildSummaryPath (Join-Path $resolvedContainerDirectory 'publish-summary.json') `
        -AdditionalArguments @('-OutputDirectory', $resolvedContainerDirectory, '-Push')
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
    if ($null -ne $family.summary) {
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
    packageDirectory = [System.IO.Path]::GetRelativePath($repoRoot, $resolvedSummaryDirectory).Replace('\', '/')
    source = 'NuGet and registry.hexalith.com'
    startedAt = $startedAt.ToString('o')
    completedAt = [DateTime]::UtcNow.ToString('o')
    families = $families
    pushed = $pushed
    failed = $failed
    notAttempted = $notAttempted
    recovery = 'If the exact tag or any package exists, do not republish or delete NuGet packages. Repair the failed dependency and dispatch Recover Partial Release from main.'
}
$summary | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $summaryPath -Encoding utf8
Write-Host "Aggregate release publication summary: $summaryPath ($status)"

if ($status -ne 'succeeded') {
    throw "Release publication failed with aggregate status '$status'. See $summaryPath for both publisher outcomes."
}
