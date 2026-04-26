# Root test runner for local development and CI.
# Examples:
#   ./tools/test.ps1
#   ./tools/test.ps1 -Filter 'Category!=Integration'
#   ./tools/test.ps1 -Filter 'Category=Integration&Category!=IntegrationSlow'   # PR-fast integration lane
#   ./tools/test.ps1 -Filter 'Category=IntegrationSlow'                          # Nightly-only slow lane
#   ./tools/test.ps1 -Coverage

[CmdletBinding()]
param(
    [switch]$Coverage,
    [string]$Filter,
    [string]$Configuration = 'Debug',
    [switch]$NoBuild,
    [string]$ResultsDirectory
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    function Read-ProjectInventory([string]$InventoryFile) {
        $path = Join-Path $repoRoot $InventoryFile
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Test project inventory '$InventoryFile' was not found."
        }

        @(Get-Content -LiteralPath $path |
            ForEach-Object { $_.Trim() } |
            Where-Object { $_.Length -gt 0 -and -not $_.StartsWith('#') })
    }

    $projectPaths = switch -Wildcard ($Filter) {
        'Category!=Integration' {
            Read-ProjectInventory 'tools/test-projects.unit-contract.txt'
            break
        }
        '*Category=IntegrationSlow*' {
            Read-ProjectInventory 'tools/test-projects.integration-fast.txt'
            break
        }
        '*Category=Integration*' {
            Read-ProjectInventory 'tools/test-projects.integration-fast.txt'
            break
        }
        'Category=Benchmark' {
            Read-ProjectInventory 'tools/test-projects.benchmark.txt'
            break
        }
        default {
            @()
        }
    }

    if ($projectPaths.Count -eq 0) {
        $projectPaths = @($null)
    }

    foreach ($projectPath in $projectPaths) {
        $arguments = @('test')
        $effectiveFilter = if ($Filter -eq 'Category!=Integration') {
            'Category!=Integration&Category!=Benchmark'
        }
        else {
            $Filter
        }

        if (-not [string]::IsNullOrWhiteSpace($projectPath)) {
            $arguments += $projectPath
        }

        $arguments += @('--configuration', $Configuration)

        if ($NoBuild) {
            $arguments += '--no-build'
        }

        if (-not [string]::IsNullOrWhiteSpace($effectiveFilter)) {
            $arguments += @('--filter', $effectiveFilter)
        }

        $trxPath = $null
        if (-not [string]::IsNullOrWhiteSpace($ResultsDirectory)) {
            $safeName = if ([string]::IsNullOrWhiteSpace($projectPath)) {
                'solution'
            }
            else {
                [System.IO.Path]::GetFileNameWithoutExtension($projectPath)
            }

            $projectResultsDirectory = Join-Path $repoRoot (Join-Path $ResultsDirectory $safeName)
            New-Item -ItemType Directory -Force -Path $projectResultsDirectory | Out-Null
            $trxPath = Join-Path $projectResultsDirectory "$safeName.trx"
            $arguments += @(
                '--logger',
                "trx;LogFileName=$safeName.trx",
                '--results-directory',
                $projectResultsDirectory
            )
        }

        if ($Coverage) {
            $arguments += @('--collect', 'XPlat Code Coverage', '--settings', 'tests/tests.runsettings')
        }

        Write-Host ("dotnet {0}" -f ($arguments -join ' '))
        & dotnet @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet test failed ($LASTEXITCODE)"
        }

        if ($trxPath) {
            if (-not (Test-Path -LiteralPath $trxPath)) {
                throw "Expected TRX file '$trxPath' was not produced."
            }

            [xml]$trx = Get-Content -LiteralPath $trxPath
            $executed = [int]$trx.TestRun.ResultSummary.Counters.executed
            if ($executed -le 0) {
                throw "Test project '$projectPath' executed zero tests for filter '$effectiveFilter'."
            }

            Write-Host "Executed $executed tests for $projectPath"
        }
    }
}
finally {
    Pop-Location
}
