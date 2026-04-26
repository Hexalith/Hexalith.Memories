[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [string]$PackageDirectory = "artifacts/packages/release"
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$packagePath = if ([System.IO.Path]::IsPathRooted($PackageDirectory)) {
    $PackageDirectory
}
else {
    Join-Path $repoRoot $PackageDirectory
}

if ([string]::IsNullOrWhiteSpace($env:NUGET_API_KEY)) {
    throw "NUGET_API_KEY is required to publish packages."
}

Push-Location $repoRoot
try {
    & pwsh -NoLogo -NoProfile -File ./tools/validate-release-packages.ps1 -PackageDirectory $packagePath -Version $Version
    if ($LASTEXITCODE -ne 0) {
        throw "Package validation failed; refusing to publish."
    }

    $packages = @(Get-ChildItem -LiteralPath $packagePath -Filter "*.nupkg" |
        Where-Object { $_.Name -notlike "*.snupkg" } |
        Sort-Object Name)

    foreach ($package in $packages) {
        Write-Host "Publishing $($package.Name)"
        & dotnet nuget push $package.FullName --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet nuget push failed for $($package.Name)."
        }
    }
}
finally {
    Pop-Location
}
