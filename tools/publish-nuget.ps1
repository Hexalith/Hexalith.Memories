[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [string]$PackageDirectory = "artifacts/packages/release"
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$sourceUrl = "https://api.nuget.org/v3/index.json"
$packagePath = if ([System.IO.Path]::IsPathRooted($PackageDirectory)) {
    $PackageDirectory
}
else {
    Join-Path $repoRoot $PackageDirectory
}

function Protect-LogText {
    param([string]$Text)

    if ([string]::IsNullOrEmpty($Text)) {
        return ""
    }

    $sanitized = $Text
    if (-not [string]::IsNullOrWhiteSpace($env:NUGET_API_KEY)) {
        $sanitized = $sanitized.Replace($env:NUGET_API_KEY, "***")
    }

    return $sanitized.Trim()
}

function Get-PackageFiles {
    if (-not (Test-Path -LiteralPath $packagePath)) {
        return @()
    }

    return @(Get-ChildItem -LiteralPath $packagePath -Filter "*.nupkg" |
        Where-Object { $_.Name -notlike "*.snupkg" } |
        Sort-Object Name)
}

function New-RecoveryText {
    return "See docs/dev/release-runbook.md. If the exact tag or any package exists, do not republish or delete NuGet packages; dispatch Recover Partial Release from main after repairing the failed dependency."
}

function Write-PublishSummary {
    param(
        [Parameter(Mandatory)][string]$Status,
        [Parameter(Mandatory)][datetime]$StartedAt,
        [array]$Pushed,
        [array]$Failed,
        [array]$NotAttempted
    )

    if (-not (Test-Path -LiteralPath $packagePath)) {
        New-Item -ItemType Directory -Path $packagePath -Force | Out-Null
    }

    $completedAt = [datetime]::UtcNow
    $summary = [ordered]@{
        schemaVersion = 1
        status = $Status
        version = $Version
        packageDirectory = [System.IO.Path]::GetRelativePath($repoRoot, $packagePath).Replace('\', '/')
        source = $sourceUrl
        startedAt = $StartedAt.ToString("o")
        completedAt = $completedAt.ToString("o")
        pushed = @($Pushed)
        failed = @($Failed)
        notAttempted = @($NotAttempted)
        recovery = New-RecoveryText
    }

    $summaryPath = Join-Path $packagePath "publish-summary.json"
    $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $summaryPath -Encoding utf8

    Write-Host ""
    Write-Host "NuGet publish summary ($Status)"
    Write-Host "Version: $Version"
    Write-Host "Package directory: $($summary.packageDirectory)"
    Write-Host "Pushed: $(@($Pushed).Count)"
    Write-Host "Failed: $(@($Failed).Count)"
    Write-Host "Not attempted: $(@($NotAttempted).Count)"
    Write-Host "Summary: $summaryPath"
    Write-Host (New-RecoveryText)

    if ($env:GITHUB_ACTIONS -eq "true" -and $Status -eq "partial-publish") {
        $message = "Version $Version partially published. See publish-summary.json and docs/dev/release-runbook.md, then rerun the Release workflow."
        $escapedMessage = $message.Replace('%', '%25').Replace("`r", '%0D').Replace("`n", '%0A')
        Write-Host "::error title=PARTIAL PUBLISH - manual reconciliation required::$escapedMessage"
    }

    if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_STEP_SUMMARY)) {
        $markdown = @(
            "## NuGet publish summary",
            "",
            "- Status: $Status",
            "- Version: $Version",
            "- Package directory: $($summary.packageDirectory)",
            "- Pushed: $(@($Pushed).Count)",
            "- Failed: $(@($Failed).Count)",
            "- Not attempted: $(@($NotAttempted).Count)",
            "- Recovery: see docs/dev/release-runbook.md; rerun the Release workflow because --skip-duplicate skips already-published packages and retries missing packages.",
            ""
        )
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value ($markdown -join [Environment]::NewLine)
    }

    return $summaryPath
}

function New-NotAttemptedPackage {
    param(
        [Parameter(Mandatory)][string]$Package,
        [Parameter(Mandatory)][string]$Reason
    )

    return [ordered]@{
        package = $Package
        reason = Protect-LogText $Reason
    }
}

function Complete-PrePushFailure {
    param(
        [Parameter(Mandatory)][datetime]$StartedAt,
        [Parameter(Mandatory)][string]$Reason
    )

    $packages = Get-PackageFiles
    $notAttempted = if ($packages.Count -eq 0) {
        @((New-NotAttemptedPackage -Package "*" -Reason $Reason))
    }
    else {
        @($packages | ForEach-Object { New-NotAttemptedPackage -Package $_.Name -Reason $Reason })
    }

    Write-PublishSummary -Status "publish-failed" -StartedAt $StartedAt -Pushed @() -Failed @() -NotAttempted $notAttempted | Out-Null
    throw $Reason
}

Push-Location $repoRoot
try {
    $startedAt = [datetime]::UtcNow
    if ([string]::IsNullOrWhiteSpace($env:NUGET_API_KEY)) {
        Complete-PrePushFailure -StartedAt $startedAt -Reason "NUGET_API_KEY is required to publish packages."
    }

    $validationOutput = @(& pwsh -NoLogo -NoProfile -File ./tools/validate-release-packages.ps1 -PackageDirectory $packagePath -Version $Version 2>&1)
    if ($LASTEXITCODE -ne 0) {
        $reason = "Package validation failed; refusing to publish. $(Protect-LogText ($validationOutput -join [Environment]::NewLine))"
        Complete-PrePushFailure -StartedAt $startedAt -Reason $reason
    }

    $packages = Get-PackageFiles
    $pushed = @()
    $failed = @()
    $notAttempted = @()

    foreach ($package in $packages) {
        Write-Host "Publishing $($package.Name)"
        $pushOutput = @(& dotnet nuget push $package.FullName --api-key $env:NUGET_API_KEY --source $sourceUrl --skip-duplicate 2>&1)
        if ($LASTEXITCODE -ne 0) {
            $exitCode = $LASTEXITCODE
            $errorText = Protect-LogText ($pushOutput -join [Environment]::NewLine)
            Write-Host "Publish failed for $($package.Name) with exit code $exitCode. Continuing to remaining packages."
            $failed += [ordered]@{
                package = $package.Name
                exitCode = $exitCode
                error = $errorText
            }
            continue
        }

        $pushed += $package.Name
        Write-Host "Published $($package.Name)"
    }

    if ($failed.Count -gt 0 -or $notAttempted.Count -gt 0) {
        $status = if ($pushed.Count -gt 0) { "partial-publish" } else { "publish-failed" }
        Write-PublishSummary -Status $status -StartedAt $startedAt -Pushed $pushed -Failed $failed -NotAttempted $notAttempted | Out-Null
        throw "NuGet publishing finished with $($failed.Count) failed package(s) and $($notAttempted.Count) not-attempted package(s)."
    }
}
finally {
    Pop-Location
}
