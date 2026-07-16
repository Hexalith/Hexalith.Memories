# Story 7.1 Task 8.1 — dev-only packaging verification script.
# Runs `dotnet pack`, installs the resulting global tool, invokes `memories --version`, then uninstalls.
# Do NOT include this in CI test suites (per anti-pattern #8 — keep the unit suite in-process).

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$ArtifactsDir = (Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts')
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$cliProject = Join-Path $repoRoot 'src/Hexalith.Memories.Cli/Hexalith.Memories.Cli.csproj'

if (-not (Test-Path $cliProject)) {
    throw "CLI project not found at $cliProject"
}

if (-not (Test-Path $ArtifactsDir)) {
    New-Item -ItemType Directory -Path $ArtifactsDir | Out-Null
}

Write-Host "[1/4] dotnet pack $cliProject -c $Configuration -o $ArtifactsDir"
& dotnet pack $cliProject -c $Configuration -o $ArtifactsDir
if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed ($LASTEXITCODE)" }

Write-Host "[2/4] dotnet tool install --global --add-source $ArtifactsDir Hexalith.Memories.Cli"
# Uninstall first in case a prior version is lingering — ignore failure.
& dotnet tool uninstall --global Hexalith.Memories.Cli 2>&1 | Out-Null
& dotnet tool install --global --add-source $ArtifactsDir Hexalith.Memories.Cli
if ($LASTEXITCODE -ne 0) { throw "dotnet tool install failed ($LASTEXITCODE)" }

Write-Host "[3/5] memories --version"
$memoriesExe = Get-Command memories -ErrorAction SilentlyContinue
if (-not $memoriesExe) {
    $toolsDir = if ($IsWindows) {
        Join-Path $env:USERPROFILE '.dotnet\tools'
    } else {
        Join-Path $env:HOME '.dotnet/tools'
    }
    Write-Error @"
'memories' command not found on PATH after install.
Check that '$toolsDir' is on your PATH.
See docs/dev/cli-config.md (PATH troubleshooting) for per-shell remediation.
"@
    throw 'PATH check failed.'
}

& memories --version
if ($LASTEXITCODE -ne 0) { throw "'memories --version' failed ($LASTEXITCODE)" }

Write-Host "[4/7] Story 7.2 format surface smoke (help-only, no server required)"
& memories search query --help | Out-Null
if ($LASTEXITCODE -ne 0) { throw "'memories search query --help' failed ($LASTEXITCODE)" }
& memories --format json tenant list --help | Out-Null
if ($LASTEXITCODE -ne 0) { throw "'memories --format json tenant list --help' failed ($LASTEXITCODE)" }

# Story 7.3 Task 8.3: confirm the error-translation surface survived packaging. The call targets a
# nonexistent tenant against whatever endpoint is configured (typically unreachable in this dev
# loop), so exit code 1 (domain) or 2 (plumbing) is expected — we only check the binary didn't
# crash or silently exit 0.
Write-Host "[5/7] Story 7.3 error-translation smoke (expect exit 1 or 2, NOT 0)"
& memories search inspect --tenant nonexistent --case x --id y 2>&1 | Out-Null
$errExit = $LASTEXITCODE
if ($errExit -eq 0) {
    throw "Unexpected success (exit 0) from 'memories search inspect --tenant nonexistent'; the error-translation surface is broken."
}
if ($errExit -ne 1 -and $errExit -ne 2) {
    throw "Unexpected exit code $errExit from error smoke call; expected 1 (domain) or 2 (plumbing)."
}
Write-Host "Error smoke exited $errExit as expected."

# Story 7.4 Task 14.2/14.3: --help must exit 0 regardless of server state and embed the NFR30
# example block. Do NOT substitute --dry-run — the wizard still constructs MemoriesClient at DI
# resolution time which requires a resolvable endpoint; --help bypasses the DI graph entirely.
Write-Host "[6/7] Story 7.4 quickstart --help smoke (expect exit 0 with 'Example' in stdout)"
$helpOutput = & memories quickstart --help
if ($LASTEXITCODE -ne 0) { throw "'memories quickstart --help' failed ($LASTEXITCODE)" }
$helpText = ($helpOutput | Out-String)
if ($helpText -notmatch '(?i)\bExamples?\b') {
    throw "'memories quickstart --help' output missing the 'Example' keyword — NFR30 contract violated."
}
Write-Host 'Quickstart --help smoke passed.'

# Story 7.5 Task 14.1: status telemetry --help must exit 0 and include an example.
Write-Host "[6b/7] Story 7.5 status telemetry --help smoke (expect exit 0 with 'Example' in stdout)"
$telemetryHelpOutput = & memories status telemetry --help
if ($LASTEXITCODE -ne 0) { throw "'memories status telemetry --help' failed ($LASTEXITCODE)" }
$telemetryHelpText = ($telemetryHelpOutput | Out-String)
if ($telemetryHelpText -notmatch '(?i)\bExamples?\b') {
    throw "'memories status telemetry --help' output missing the 'Example' keyword — NFR30 contract violated."
}
Write-Host 'status telemetry --help smoke passed.'

Write-Host "[7/7] dotnet tool uninstall --global Hexalith.Memories.Cli"
& dotnet tool uninstall --global Hexalith.Memories.Cli
if ($LASTEXITCODE -ne 0) { throw "dotnet tool uninstall failed ($LASTEXITCODE)" }

Write-Host 'OK — packaging pipeline verified.'
