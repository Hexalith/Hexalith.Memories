# Root test runner for local development and CI.
# Examples:
#   ./tools/test.ps1
#   ./tools/test.ps1 -Filter 'Category!=Integration'
#   ./tools/test.ps1 -Coverage

[CmdletBinding()]
param(
    [switch]$Coverage,
    [string]$Filter
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    $projectPaths = switch ($Filter) {
        'Category!=Integration' {
            @(
                'tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj',
                'tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj',
                'tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj',
                'tests/Hexalith.Memories.Benchmarks/Hexalith.Memories.Benchmarks.csproj'
            )
            break
        }
        'Category=Integration' {
            @('tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj')
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

        if (-not [string]::IsNullOrWhiteSpace($projectPath)) {
            $arguments += $projectPath
        }

        if (-not [string]::IsNullOrWhiteSpace($Filter)) {
            $arguments += @('--filter', $Filter)
        }

        if ($Coverage) {
            $arguments += @('--collect', 'XPlat Code Coverage', '--settings', 'tests/tests.runsettings')
        }

        Write-Host ("dotnet {0}" -f ($arguments -join ' '))
        & dotnet @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet test failed ($LASTEXITCODE)"
        }
    }
}
finally {
    Pop-Location
}
