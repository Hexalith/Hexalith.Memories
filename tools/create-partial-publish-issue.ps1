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

try {
    $summary = ConvertFrom-Json -InputObject (Get-Content -LiteralPath $SummaryPath -Raw) -NoEnumerate
    if ($null -eq $summary -or $summary -is [array] -or $summary -isnot [pscustomobject] -or
        [string]::IsNullOrWhiteSpace([string]$summary.status)) {
        throw "Expected a publish summary object with a status."
    }
}
catch {
    throw "Cannot read publish summary '$SummaryPath' as a JSON summary object. Preserve the original publish failure and summary evidence; repair or regenerate the summary before rerunning the alert helper."
}
if ($summary.status -ne "partial-publish") {
    Write-Host "Publish summary status is '$($summary.status)'; skipping partial-publish issue alert."
    return
}

$artifactKind = if ([string]::IsNullOrWhiteSpace([string]$summary.artifactKind)) {
    "NuGet packages"
}
else {
    [string]$summary.artifactKind
}

$title = "PARTIAL PUBLISH $($summary.version) - manual reconciliation required"
$script:RemainingGitHubRetryWaitSeconds = 180

function Protect-GitHubDiagnostic {
    param([string]$Text)

    foreach ($token in @($env:GH_TOKEN, $env:GITHUB_TOKEN)) {
        if (-not [string]::IsNullOrEmpty($token)) {
            $Text = $Text.Replace($token, '[REDACTED]')
        }
    }
    $Text = $Text -replace '(?im)\b(authorization|proxy-authorization|x-github-token)\s*:[^\r\n]*', '$1: [REDACTED]'
    $Text = $Text -replace '(?i)\b(Bearer|Basic)\s+\S+', '$1 [REDACTED]'
    $Text = $Text -replace '(?i)(https?://)[^/\s@]+@', '$1[REDACTED]@'
    $Text = $Text -replace '\s+', ' '
    if ($Text.Length -gt 500) {
        $Text = $Text.Substring(0, 500) + '...'
    }
    return $Text.Trim()
}

function Wait-GitHubRetry {
    param([long]$Seconds, [string]$Operation, [string]$Diagnostic)

    if ($Seconds -gt $script:RemainingGitHubRetryWaitSeconds) {
        throw "Cannot retry $Operation within the alert helper's 180-second total wait budget. Preserve the publish summary '$SummaryPath' and retry after GitHub's rate-limit window. GitHub diagnostic: $Diagnostic"
    }
    $script:RemainingGitHubRetryWaitSeconds -= $Seconds
    Write-Warning "Transient GitHub failure trying to $Operation. Retrying in $Seconds second(s)."
    Start-Sleep -Seconds $Seconds
}

function Invoke-GitHubOnce {
    param([string[]]$Arguments)

    # Keep native stderr separate from JSON stdout, including when the caller
    # enables native exit-code errors in its PowerShell profile/environment.
    $PSNativeCommandUseErrorActionPreference = $false
    $errorFile = [System.IO.Path]::GetTempFileName()
    try {
        $output = & gh @Arguments 2> $errorFile
        $exitCode = $LASTEXITCODE
        return [pscustomobject]@{
            ExitCode = $exitCode
            Output = $output -join "`n"
            Error = Get-Content -LiteralPath $errorFile -Raw
        }
    }
    finally {
        Remove-Item -LiteralPath $errorFile -Force
    }
}

function Invoke-GitHubWithRetry {
    param(
        [string[]]$Arguments,
        [string]$Operation,
        [scriptblock]$Reconcile
    )

    for ($attempt = 1; $attempt -le 3; $attempt++) {
        $result = Invoke-GitHubOnce -Arguments $Arguments
        if ($result.ExitCode -eq 0) {
            return $result.Output
        }

        $diagnostic = $result.Error + "`n" + $result.Output
        $safeDiagnostic = Protect-GitHubDiagnostic -Text $diagnostic
        $rateLimited = $diagnostic -match '(?i)\bHTTP\s+429\b|\b(API rate limit exceeded|secondary rate limit|rate limit exceeded)\b'
        $transient = $rateLimited -or $diagnostic -match
            '(?i)\bHTTP\s+(429|5\d\d)\b|\b(timeout|timed out|connection reset|connection refused|unexpected EOF|temporary failure in name resolution)\b'
        if (-not $transient) {
            throw "Failed to $Operation (exit code $($result.ExitCode)); not retrying a permanent or unclassified failure. Preserve the publish summary '$SummaryPath'. GitHub diagnostic: $safeDiagnostic"
        }

        # Rate limits apply to readback too. Honour timing surfaced by gh;
        # otherwise wait at least a minute with exponential backoff.
        if ($rateLimited -and ($attempt -lt 3 -or $null -ne $Reconcile)) {
            $delaySeconds = [long](60 * [Math]::Pow(2, $attempt - 1))
            if ($diagnostic -match '(?im)\bretry-after\s*:\s*(\d+)\b') {
                $delaySeconds = [long]$Matches[1]
            }
            elseif ($diagnostic -match '(?im)\bx-ratelimit-reset\s*:\s*(\d+)\b') {
                $delaySeconds = [Math]::Max([long]1, [long]$Matches[1] - [DateTimeOffset]::UtcNow.ToUnixTimeSeconds() + 1)
            }
            Wait-GitHubRetry -Seconds $delaySeconds -Operation $Operation -Diagnostic $safeDiagnostic
        }

        # A write may have reached GitHub even when its response was lost.
        # Stop if readback fails; never repeat a write without reconciling it.
        if ($null -ne $Reconcile -and (& $Reconcile)) {
            Write-Warning "Recovered GitHub response failure: confirmed $Operation by readback."
            return
        }
        if ($attempt -eq 3) {
            throw "Failed to $Operation after 3 attempts. Preserve the publish summary '$SummaryPath' and reconcile the incident before rerunning the alert helper. GitHub diagnostic: $safeDiagnostic"
        }

        if (-not $rateLimited) {
            Wait-GitHubRetry -Seconds $attempt -Operation $Operation -Diagnostic $safeDiagnostic
        }
    }
}

function Find-PartialPublishIssue {
    param([switch]$Readback)

    $arguments = @(
        'issue', 'list', '--repo', $Repository, '--state', 'open',
        '--limit', '100', '--json', $(if ($Readback) { 'number,title,body' } else { 'number,title' })
    )
    if (-not $Readback) {
        $arguments += @('--search', "in:title `"$title`"")
    }
    # Direct listing avoids search-index lag when a create response was lost.
    $issuesJson = Invoke-GitHubWithRetry -Operation 'query existing partial-publish issues' -Arguments $arguments
    if ($Readback -and [string]::IsNullOrWhiteSpace($issuesJson)) {
        throw "GitHub returned blank issue readback; refusing to repeat the alert write. Preserve the publish summary '$SummaryPath'."
    }
    $issues = @()
    if (-not [string]::IsNullOrWhiteSpace($issuesJson)) {
        try {
            $issues = ConvertFrom-Json -InputObject $issuesJson -NoEnumerate
            if ($issues -isnot [array]) {
                throw "Expected an issue array."
            }
            foreach ($issue in $issues) {
                if ($null -eq $issue -or $issue.title -isnot [string] -or
                    $issue.number -isnot [long] -or $issue.number -le 0 -or
                    ($Readback -and $issue.body -isnot [string])) {
                    throw "Expected issue number and title."
                }
            }
        }
        catch {
            throw "GitHub returned invalid issue lookup JSON; no alert write will be attempted. Preserve the publish summary '$SummaryPath'."
        }
    }
    return $issues | Where-Object { $_.title -ceq $title } | Select-Object -First 1
}

function Add-PartialPublishComment {
    param([long]$IssueNumber, [string]$CommentBody)

    $bodyFile = [System.IO.Path]::GetTempFileName()
    try {
        Set-Content -LiteralPath $bodyFile -Value $CommentBody -Encoding utf8
        Invoke-GitHubWithRetry -Operation "comment on existing partial-publish issue #$IssueNumber" -Arguments @(
            'issue', 'comment', [string]$IssueNumber, '--repo', $Repository, '--body-file', $bodyFile
        ) -Reconcile {
            $issueJson = Invoke-GitHubWithRetry -Operation 'read back partial-publish comments' -Arguments @(
                'issue', 'view', [string]$IssueNumber, '--repo', $Repository, '--json', 'comments'
            )
            try {
                $issue = ConvertFrom-Json -InputObject $issueJson -NoEnumerate
                if ($issue -is [array] -or $issue -isnot [pscustomobject] -or $issue.comments -isnot [array]) {
                    throw "Expected a comments array."
                }
                foreach ($comment in $issue.comments) {
                    if ($null -eq $comment -or $comment.body -isnot [string]) {
                        throw "Expected a comment body."
                    }
                }
            }
            catch {
                throw "GitHub returned invalid comment readback JSON; refusing to repeat the alert write. Preserve the publish summary '$SummaryPath'."
            }
            return @($issue.comments | Where-Object {
                $_.body.Replace("`r`n", "`n").TrimEnd() -ceq $CommentBody.Replace("`r`n", "`n").TrimEnd()
            }).Count -gt 0
        } | Out-Null
    }
    finally {
        Remove-Item -LiteralPath $bodyFile -Force
    }
    Write-Host "Commented on existing partial-publish issue #$IssueNumber."
}

function Format-ListSection {
    param(
        [Parameter(Mandatory)][string]$Title,
        [array]$Items
    )

    $Items = @($Items | Where-Object { $null -ne $_ })
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
    "A release publish run partially succeeded and requires manual reconciliation.",
    "",
    "- Run: $RunUrl",
    "- Version: $($summary.version)",
    "- Evidence directory: $($summary.packageDirectory)",
    "- Source: $($summary.source)",
    "- Artifact kind: $artifactKind",
    "",
    (Format-ListSection -Title "Published artifacts" -Items @($summary.pushed)),
    "",
    (Format-ListSection -Title "Failed artifacts" -Items @($summary.failed)),
    "",
    (Format-ListSection -Title "Not-attempted artifacts" -Items @($summary.notAttempted)),
    "",
    "### Recovery",
    "",
    "See `docs/dev/release-runbook.md`. Do not delete or republish NuGet packages. If the exact release tag exists and is reachable from `main`, repair Zot push authorization for both repositories, then dispatch **Recover Partial Release** from `main` with this bare version. The trusted workflow reconciles both immutable images, verifies all nine NuGet packages, creates or completes the GitHub Release with ten assets, and closes this incident only after the full release unit is verified."
)
$body = $bodyParts -join [Environment]::NewLine
$comment = "Rerun detected for the same partial publish version." + [Environment]::NewLine + [Environment]::NewLine + $body

$existing = Find-PartialPublishIssue
if ($null -ne $existing) {
    Add-PartialPublishComment -IssueNumber $existing.number -CommentBody $comment
}
else {
    $bodyFile = [System.IO.Path]::GetTempFileName()
    try {
        Set-Content -LiteralPath $bodyFile -Value $body -Encoding utf8
        Invoke-GitHubWithRetry -Operation 'create partial-publish issue' -Arguments @(
            'issue', 'create', '--repo', $Repository, '--title', $title, '--body-file', $bodyFile
        ) -Reconcile {
            $recovered = Find-PartialPublishIssue -Readback
            if ($null -eq $recovered) {
                return $false
            }
            if ($recovered.body.Replace("`r`n", "`n").TrimEnd() -cne $body.Replace("`r`n", "`n").TrimEnd()) {
                Add-PartialPublishComment -IssueNumber $recovered.number -CommentBody $comment
            }
            return $true
        } | Out-Null
    }
    finally {
        if (Test-Path -LiteralPath $bodyFile) {
            Remove-Item -LiteralPath $bodyFile -Force
        }
    }

    Write-Host "Created partial-publish issue: $title"
}
