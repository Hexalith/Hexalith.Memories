[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot

# Drive the release-blocking unit/contract test inventory from the shared file used by
# tools/test.{sh,ps1} and .github/workflows/ci.yml so PR CI and the release lane cannot drift
# (Story 11.1 Task 0.7 single-source-of-truth requirement). Project-specific filters live in
# the dictionary below, keyed by the inventory entry. The test-release lane intentionally
# excludes Hexalith.Memories.Benchmarks (Category=Benchmark is opt-in only).
$inventoryPath = Join-Path $repoRoot "tools/test-projects.unit-contract.txt"
if (-not (Test-Path -LiteralPath $inventoryPath)) {
    throw "Test project inventory '$inventoryPath' was not found."
}

$projects = @(Get-Content -LiteralPath $inventoryPath |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and -not $_.TrimStart().StartsWith('#') } |
    ForEach-Object { $_.Trim() })

if ($projects.Count -eq 0) {
    throw "Inventory '$inventoryPath' contained no test projects."
}

# Benchmarks are opt-in (Category=Benchmark); release lane must skip them per 11.1 Task 0.4.
$benchmarkExcludeFilter = "Category!=Benchmark"

Push-Location $repoRoot
try {
    foreach ($project in $projects) {
        $arguments = @(
            "test",
            $project,
            "--configuration",
            $Configuration,
            "--no-build",
            "--logger",
            "console;verbosity=minimal",
            "--blame-hang-timeout",
            "120s",
            "--blame-hang-dump-type",
            "none"
        )

        $arguments += @("--filter", $benchmarkExcludeFilter)

        Write-Host ("dotnet {0}" -f ($arguments -join " "))
        & dotnet @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Release test run failed for $project."
        }
    }
}
finally {
    Pop-Location
}
