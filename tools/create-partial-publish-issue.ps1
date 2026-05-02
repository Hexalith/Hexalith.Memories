[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$SummaryPath,

    [string]$Repository = $env:GITHUB_REPOSITORY,

    [string]$RunUrl
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $SummaryPath)) {
    throw "Publish summary not found: $SummaryPath"
}

if ([string]::IsNullOrWhiteSpace($Repository)) {
    throw "GITHUB_REPOSITORY is required to create or update the partial-publish issue."
}

if ([string]::IsNullOrWhiteSpace($RunUrl)) {
    if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_SERVER_URL) -and
        -not [string]::IsNullOrWhiteSpace($env:GITHUB_REPOSITORY) -and
        -not [string]::IsNullOrWhiteSpace($env:GITHUB_RUN_ID)) {
        $RunUrl = "$($env:GITHUB_SERVER_URL)/$($env:GITHUB_REPOSITORY)/actions/runs/$($env:GITHUB_RUN_ID)"
    }
    else {
        $RunUrl = "GitHub Actions run URL unavailable"
    }
}

$summary = Get-Content -LiteralPath $SummaryPath -Raw | ConvertFrom-Json
if ($summary.status -ne "partial-publish") {
    Write-Host "Publish summary status is '$($summary.status)'; skipping partial-publish issue alert."
    return
}

$title = "PARTIAL PUBLISH $($summary.version) - manual reconciliation required"

function Format-ListSection {
    param(
        [Parameter(Mandatory)][string]$Title,
        [array]$Items
    )

    $lines = @("### $Title", "")
    if ($Items.Count -eq 0) {
        $lines += "- None"
        return ($lines -join [Environment]::NewLine)
    }

    foreach ($item in $Items) {
        if ($item -is [string]) {
            $lines += "- $item"
        }
        elseif ($null -ne $item.exitCode) {
            $errorText = ([string]$item.error).Trim()
            if ($errorText.Length -gt 500) {
                $errorText = $errorText.Substring(0, 500) + "..."
            }

            $lines += "- $($item.package) (exit code $($item.exitCode)): $errorText"
        }
        else {
            $lines += "- $($item.package): $($item.reason)"
        }
    }

    return ($lines -join [Environment]::NewLine)
}

$bodyParts = @(
    "A NuGet publish run partially succeeded and requires manual reconciliation.",
    "",
    "- Run: $RunUrl",
    "- Version: $($summary.version)",
    "- Package directory: $($summary.packageDirectory)",
    "- Source: $($summary.source)",
    "",
    (Format-ListSection -Title "Pushed packages" -Items @($summary.pushed)),
    "",
    (Format-ListSection -Title "Failed packages" -Items @($summary.failed)),
    "",
    (Format-ListSection -Title "Not-attempted packages" -Items @($summary.notAttempted)),
    "",
    "### Recovery",
    "",
    "See `docs/dev/release-runbook.md`. Rerun the Release workflow; `--skip-duplicate` skips already-published packages and retries failed or not-attempted packages."
)
$body = $bodyParts -join [Environment]::NewLine

$issuesJson = & gh issue list --repo $Repository --state open --search $title --json number,title
if ($LASTEXITCODE -ne 0) {
    throw "Failed to query existing partial-publish issues."
}

$issues = @($issuesJson | ConvertFrom-Json)
$existing = @($issues | Where-Object { $_.title -eq $title } | Select-Object -First 1)
if ($existing.Count -gt 0) {
    $comment = "Rerun detected for the same partial publish version." + [Environment]::NewLine + [Environment]::NewLine + $body
    $bodyFile = [System.IO.Path]::GetTempFileName()
    try {
        Set-Content -LiteralPath $bodyFile -Value $comment -Encoding utf8
        & gh issue comment $existing[0].number --repo $Repository --body-file $bodyFile
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to comment on existing partial-publish issue #$($existing[0].number)."
        }
    }
    finally {
        Remove-Item -LiteralPath $bodyFile -Force -ErrorAction SilentlyContinue
    }

    Write-Host "Commented on existing partial-publish issue #$($existing[0].number)."
}
else {
    $bodyFile = [System.IO.Path]::GetTempFileName()
    try {
        Set-Content -LiteralPath $bodyFile -Value $body -Encoding utf8
        & gh issue create --repo $Repository --title $title --body-file $bodyFile
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create partial-publish issue."
        }
    }
    finally {
        Remove-Item -LiteralPath $bodyFile -Force -ErrorAction SilentlyContinue
    }

    Write-Host "Created partial-publish issue: $title"
}
