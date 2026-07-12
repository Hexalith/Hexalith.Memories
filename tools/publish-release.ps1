[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$failures = @()

& pwsh -NoLogo -NoProfile -File ./tools/publish-nuget.ps1 `
    -Version $Version `
    -PackageDirectory ./artifacts/packages/release
if ($LASTEXITCODE -ne 0) {
    $failures += "NuGet publication failed with exit code $LASTEXITCODE"
}

& pwsh -NoLogo -NoProfile -File ./tools/publish-containers.ps1 -Version $Version -Push
if ($LASTEXITCODE -ne 0) {
    $failures += "container publication failed with exit code $LASTEXITCODE"
}

if ($failures.Count -gt 0) {
    throw "Release publication failed: $($failures -join '; '). Both publish summaries contain retry-safe per-artifact outcomes."
}
