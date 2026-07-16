# Root test runner for local development and CI.
# Examples:
#   ./tools/test.ps1
#   ./tools/test.ps1 -Filter 'Category!=Integration'
#   ./tools/test.ps1 -Filter 'Category=Integration&Category!=IntegrationSlow&Category!=Performance'   # PR-fast integration lane
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

    $resultsRoot = $null
    if (-not [string]::IsNullOrWhiteSpace($ResultsDirectory)) {
        $repoRootFull = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar)
        $resultsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $ResultsDirectory))
        $repoPrefix = $repoRootFull + [System.IO.Path]::DirectorySeparatorChar
        if ($resultsRoot -eq $repoRootFull -or
            -not $resultsRoot.StartsWith($repoPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "-ResultsDirectory must resolve beneath the repository root."
        }

        if (Test-Path -LiteralPath $resultsRoot) {
            Remove-Item -LiteralPath $resultsRoot -Recurse -Force
        }
        New-Item -ItemType Directory -Force -Path $resultsRoot | Out-Null
    }

    foreach ($projectPath in $projectPaths) {
        $arguments = @('test')
        $effectiveFilter = if ($Filter -eq 'Category!=Integration') {
            'Category!=Integration&Category!=Benchmark'
        }
        elseif ($Filter -eq 'Category=Benchmark') {
            $null
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
        $expectedExecutedTests = $null
        if (-not [string]::IsNullOrWhiteSpace($ResultsDirectory)) {
            $safeName = if ([string]::IsNullOrWhiteSpace($projectPath)) {
                'solution'
            }
            else {
                [System.IO.Path]::GetFileNameWithoutExtension($projectPath)
            }

            $projectResultsDirectory = Join-Path $resultsRoot $safeName
            New-Item -ItemType Directory -Force -Path $projectResultsDirectory | Out-Null
            $trxPath = Join-Path $projectResultsDirectory "$safeName.trx"
            $arguments += @(
                '--logger',
                "trx;LogFileName=$safeName.trx",
                '--results-directory',
                $projectResultsDirectory
            )
            if ($Filter -eq 'Category=Benchmark') {
                $expectedExecutedTests = 17
            }
        }

        if ($Coverage) {
            $arguments += @('--collect', 'XPlat Code Coverage', '--settings', 'tests/tests.runsettings')
        }

        Write-Host ("dotnet {0}" -f ($arguments -join ' '))
        & dotnet @arguments
        $testExitCode = $LASTEXITCODE

        if ($trxPath) {
            if (-not (Test-Path -LiteralPath $trxPath)) {
                throw "Expected TRX file '$trxPath' was not produced."
            }

            [xml]$trx = Get-Content -LiteralPath $trxPath
            $executed = [int]$trx.TestRun.ResultSummary.Counters.executed
            $notExecuted = [int]$trx.TestRun.ResultSummary.Counters.notExecuted
            if ($executed -le 0) {
                throw "Test project '$projectPath' executed zero tests for filter '$effectiveFilter'."
            }
            if ($null -ne $expectedExecutedTests -and
                ($executed -ne $expectedExecutedTests -or $notExecuted -ne 0)) {
                throw "Test project '$projectPath' must execute exactly $expectedExecutedTests tests with none skipped; TRX reported executed=$executed, notExecuted=$notExecuted."
            }

            Write-Host "Executed $executed tests for $projectPath"
        }

        if ($testExitCode -ne 0) {
            throw "dotnet test failed ($testExitCode)"
        }
    }
}
finally {
    Pop-Location
}
