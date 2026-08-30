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

    # Translate the wrapper's VSTest-shaped Category expressions into Microsoft.Testing.Platform
    # trait filters. SDK 10.0.400 selects MTP in global.json; VSTest --filter executes zero tests.
    function Add-MtpFilterArguments([string]$Filter, [System.Collections.Generic.List[string]]$Arguments) {
        if ([string]::IsNullOrWhiteSpace($Filter)) {
            return
        }

        foreach ($part in $Filter.Split('&')) {
            if ($part.StartsWith('Category!=', [System.StringComparison]::Ordinal)) {
                $Arguments.Add('--filter-not-trait')
                $Arguments.Add('Category=' + $part.Substring('Category!='.Length))
            }
            elseif ($part.StartsWith('Category=', [System.StringComparison]::Ordinal)) {
                $Arguments.Add('--filter-trait')
                $Arguments.Add($part)
            }
            else {
                throw "Unsupported test filter '$part' in '$Filter'; Microsoft.Testing.Platform requires Category trait expressions."
            }
        }
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

        $argumentList = [System.Collections.Generic.List[string]]::new()
        foreach ($argument in $arguments) {
            $argumentList.Add($argument)
        }

        Add-MtpFilterArguments $effectiveFilter $argumentList

        $trxPath = $null
        $expectedExecutedTests = $null
        $projectResultsDirectory = $null
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
            $argumentList.Add('--results-directory')
            $argumentList.Add($projectResultsDirectory)
            $argumentList.Add('--report-xunit-trx')
            $argumentList.Add('--report-xunit-trx-filename')
            $argumentList.Add("$safeName.trx")
            if ($Filter -eq 'Category=Benchmark') {
                $expectedExecutedTests = 17
            }
        }

        if ($Coverage) {
            # coverlet.MTP matches the Coverlet include/exclude contract in tests/tests.runsettings.
            # Microsoft.Testing.Extensions.CodeCoverage under-counted Hexalith.Memories.Server hits
            # on GitHub-hosted runners (23% vs ~75% Coverlet), which failed the 76.5% line gate.
            $argumentList.Add('--coverlet')
            $argumentList.Add('--coverlet-output-format')
            $argumentList.Add('cobertura')
            $argumentList.Add('--coverlet-include')
            $argumentList.Add('[Hexalith.Memories.*]*')
            $argumentList.Add('--coverlet-exclude')
            $argumentList.Add('[*.Tests]*')
            $argumentList.Add('--coverlet-exclude')
            $argumentList.Add('[Hexalith.Memories.TestHelpers]*')
            $argumentList.Add('--coverlet-exclude')
            $argumentList.Add('[Hexalith.Memories.Web.Specimens]*')
            $argumentList.Add('--coverlet-exclude')
            $argumentList.Add('[Hexalith.Memories.MigrateEmbeddingVectors]*')
            $argumentList.Add('--coverlet-exclude-by-file')
            $argumentList.Add('**/obj/**')
            $argumentList.Add('--coverlet-exclude-by-attribute')
            $argumentList.Add('GeneratedCodeAttribute')
            $argumentList.Add('--coverlet-exclude-by-attribute')
            $argumentList.Add('ObsoleteAttribute')
            $argumentList.Add('--coverlet-skip-auto-props')
        }

        $arguments = $argumentList.ToArray()
        Write-Host ("dotnet {0}" -f ($arguments -join ' '))
        & dotnet @arguments
        $testExitCode = $LASTEXITCODE

        if ($Coverage -and -not [string]::IsNullOrWhiteSpace($projectResultsDirectory)) {
            $canonical = Join-Path $projectResultsDirectory 'coverage.cobertura.xml'
            if (-not (Test-Path -LiteralPath $canonical)) {
                $found = @(Get-ChildItem -LiteralPath $projectResultsDirectory -File |
                    Where-Object {
                        $_.Name -like 'coverage.cobertura*.xml' -or
                        $_.Name -like '*.coverage.cobertura.xml'
                    } |
                    Sort-Object Name)
                if ($found.Count -eq 0) {
                    throw "Test project produced no Cobertura report under '$projectResultsDirectory'."
                }
                Copy-Item -LiteralPath $found[-1].FullName -Destination $canonical
            }
        }

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
