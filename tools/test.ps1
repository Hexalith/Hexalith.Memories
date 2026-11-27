#Requires -Version 7.0

# Root test runner for local development and CI.
# Release test baseline waivers belong in the deferred-work ledger. See
# docs/dev/release-runbook.md#release-test-baseline for the evidence to record.
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
        'Category=Integration' {
            Read-ProjectInventory 'tools/test-projects.integration-fast.txt'
            Read-ProjectInventory 'tools/test-projects.integration-slow.txt'
            break
        }
        '*Category=IntegrationSlow*' {
            Read-ProjectInventory 'tools/test-projects.integration-slow.txt'
            break
        }
        '*Category=Performance*' {
            Read-ProjectInventory 'tools/test-projects.integration-slow.txt'
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
            Read-ProjectInventory 'tools/test-projects.unit-contract.txt'
            Read-ProjectInventory 'tools/test-projects.integration-fast.txt'
            Read-ProjectInventory 'tools/test-projects.integration-slow.txt'
            Read-ProjectInventory 'tools/test-projects.benchmark.txt'
        }
    }

    if ($projectPaths.Count -eq 0) {
        throw 'Selected test project inventory is empty.'
    }

    $resultsRoot = $null
    if (-not [string]::IsNullOrWhiteSpace($ResultsDirectory)) {
        if ([System.IO.Path]::IsPathRooted($ResultsDirectory)) {
            throw '-ResultsDirectory must be a repository-relative path.'
        }
        $repoRootFull = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar)
        $resultsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $ResultsDirectory))
        $repoPrefix = $repoRootFull + [System.IO.Path]::DirectorySeparatorChar
        $comparison = if ($IsWindows) { [System.StringComparison]::OrdinalIgnoreCase } else { [System.StringComparison]::Ordinal }
        $resultsRoot = [System.IO.Path]::TrimEndingDirectorySeparator($resultsRoot)
        if ($resultsRoot.Length -le $repoPrefix.Length -or -not $resultsRoot.StartsWith($repoPrefix, $comparison)) {
            throw "-ResultsDirectory must resolve beneath the repository root."
        }

        $components = $resultsRoot.Substring($repoPrefix.Length).Split([System.IO.Path]::DirectorySeparatorChar)
        if ($components[0] -ieq '.git') {
            throw "-ResultsDirectory must stay outside the repository's .git metadata path."
        }

        $current = $repoRootFull
        foreach ($component in $components) {
            $current = [System.IO.Path]::Combine($current, $component)
            try {
                $attributes = [System.IO.File]::GetAttributes($current)
            }
            catch [System.IO.FileNotFoundException], [System.IO.DirectoryNotFoundException] {
                $attributes = [System.IO.Directory]::CreateDirectory($current).Attributes
            }

            if ($attributes -band [System.IO.FileAttributes]::ReparsePoint) {
                throw "-ResultsDirectory must not traverse a symbolic link or reparse point at '$current'."
            }
            if (-not ($attributes -band [System.IO.FileAttributes]::Directory)) {
                throw "-ResultsDirectory component '$current' is not a directory."
            }
        }

        # Use the same ownership marker as tools/test.sh and keep it during cleanup.
        $markerName = '.hexalith-memories-test-results'
        $signature = "Hexalith.Memories test results v1`n"
        $entries = @(Get-ChildItem -LiteralPath $resultsRoot -Force)
        if ($entries.Count -eq 0) {
            $markerPath = [System.IO.Path]::Combine($resultsRoot, $markerName)
            $markerStream = [System.IO.File]::Open($markerPath, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write)
            try {
                $bytes = [System.Text.Encoding]::UTF8.GetBytes($signature)
                $markerStream.Write($bytes, 0, $bytes.Length)
            }
            finally {
                $markerStream.Dispose()
            }
        }
        else {
            $marker = $entries | Where-Object { $_.Name -ceq $markerName }
            if ($marker -isnot [System.IO.FileInfo] -or
                ($marker.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -or
                $marker.Length -ne $signature.Length -or
                [System.IO.File]::ReadAllText($marker.FullName) -cne $signature) {
                throw "-ResultsDirectory must name an empty or runner-owned directory. Refusing to clean '$resultsRoot'."
            }

            foreach ($entry in $entries) {
                if ($entry.Name -cne $markerName) {
                    Remove-Item -LiteralPath $entry.FullName -Recurse -Force
                }
            }
        }
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
            $arguments += @('--project', $projectPath)
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
        $dotnetCommand = Get-Command dotnet -CommandType Application -ErrorAction Stop |
            Select-Object -First 1
        $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
        $startInfo.FileName = $dotnetCommand.Source
        $startInfo.UseShellExecute = $false
        $startInfo.WorkingDirectory = $repoRoot
        foreach ($argument in $arguments) {
            [void]$startInfo.ArgumentList.Add($argument)
        }

        $process = [System.Diagnostics.Process]::Start($startInfo)
        if ($null -eq $process) {
            throw 'Failed to start dotnet test.'
        }

        try {
            $process.WaitForExit()
            $testExitCode = $process.ExitCode
        }
        finally {
            $process.Dispose()
        }

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
