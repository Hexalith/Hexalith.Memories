[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [string]$OutputDirectory = "artifacts/packages/release",

    [string]$Configuration = "Release",

    [string]$ContainerOutputDirectory = "artifacts/containers/release",

    [string]$DeploymentOutputPath = "artifacts/deployment/hexalith-memories-production.yaml",

    [switch]$PackageOnly,

    [string]$PowerShellExecutable = "pwsh"
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$inventoryPath = Join-Path $repoRoot "tools/release-packages.json"
$outputPath = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory
}
else {
    Join-Path $repoRoot $OutputDirectory
}

if ($Version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$') {
    throw "Version '$Version' is not a valid semantic version."
}

Push-Location $repoRoot
try {
    & $PowerShellExecutable -NoLogo -NoProfile -File ./tools/validate-release-packages.ps1
    if ($LASTEXITCODE -ne 0) {
        throw "Project/package inventory validation failed."
    }

    if (Test-Path -LiteralPath $outputPath) {
        Remove-Item -LiteralPath $outputPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $outputPath | Out-Null

    $inventory = Get-Content -LiteralPath $inventoryPath -Raw | ConvertFrom-Json

    # Single solution-wide build with the release version pinned, so transitive dependency
    # assemblies share a consistent embedded version. Then pack each package without rebuilding.
    Write-Host "Building solution with Version=$Version"
    & dotnet build Hexalith.Memories.slnx --configuration $Configuration -p:Version=$Version -p:PackageVersion=$Version -p:ContinuousIntegrationBuild=true
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed for Hexalith.Memories.slnx."
    }

    foreach ($package in @($inventory.packages)) {
        Write-Host "Packing $($package.packageId)"
        & dotnet pack $package.project --configuration $Configuration --no-build --output $outputPath -p:Version=$Version -p:PackageVersion=$Version -p:ContinuousIntegrationBuild=true
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet pack failed for $($package.packageId)."
        }
    }

    & $PowerShellExecutable -NoLogo -NoProfile -File ./tools/validate-release-packages.ps1 -PackageDirectory $outputPath -Version $Version
    if ($LASTEXITCODE -ne 0) {
        throw "Generated package validation failed."
    }

    if ($PackageOnly) {
        Write-Host "Package-only validation completed successfully."
        return
    }

    & $PowerShellExecutable -NoLogo -NoProfile -File ./tools/publish-containers.ps1 `
        -Version $Version `
        -OutputDirectory $ContainerOutputDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "Release container preparation failed."
    }

    $containerOutput = if ([System.IO.Path]::IsPathRooted($ContainerOutputDirectory)) {
        $ContainerOutputDirectory
    }
    else {
        Join-Path $repoRoot $ContainerOutputDirectory
    }
    $deploymentOutput = if ([System.IO.Path]::IsPathRooted($DeploymentOutputPath)) {
        $DeploymentOutputPath
    }
    else {
        Join-Path $repoRoot $DeploymentOutputPath
    }
    New-Item -ItemType Directory -Path (Split-Path -Parent $deploymentOutput) -Force | Out-Null
    Copy-Item `
        -LiteralPath (Join-Path $containerOutput "production-deployment.yaml") `
        -Destination $deploymentOutput `
        -Force
}
finally {
    Pop-Location
}
