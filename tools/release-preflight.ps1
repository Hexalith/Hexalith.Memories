param(
    [string] $RepositoryPath = ".",
    [string] $RemoteName = "origin",
    [string] $TagFormat = 'v${version}',
    [string] $NextVersion,
    [string] $SemanticReleaseDryRunOutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$script:SemverPattern = '^[0-9]+\.[0-9]+\.[0-9]+(?:[-+][0-9A-Za-z.-]+)?$'

function Invoke-GitCommand {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments,

        [int[]] $AllowedExitCodes = @(0)
    )

    $stdoutLines = [System.Collections.Generic.List[string]]::new()
    $stderrLines = [System.Collections.Generic.List[string]]::new()

    foreach ($item in (& git -C $script:ResolvedRepositoryPath @Arguments 2>&1)) {
        if ($item -is [System.Management.Automation.ErrorRecord]) {
            $stderrLines.Add($item.ToString())
        }
        else {
            $stdoutLines.Add([string]$item)
        }
    }

    $exitCode = $LASTEXITCODE
    if ($AllowedExitCodes -notcontains $exitCode) {
        $joinedOutput = (@($stdoutLines) + @($stderrLines)) -join [Environment]::NewLine
        $joinedOutput = $joinedOutput.Trim()
        if ([string]::IsNullOrWhiteSpace($joinedOutput)) {
            $joinedOutput = "<no output>"
        }

        throw "git $($Arguments -join ' ') failed with exit code $exitCode. Output: $joinedOutput"
    }

    [pscustomobject]@{
        ExitCode = $exitCode
        Output = $stdoutLines.ToArray()
        StdErr = $stderrLines.ToArray()
    }
}

function Get-ReleaseTagName {
    param(
        [Parameter(Mandatory)]
        [string] $Version,

        [Parameter(Mandatory)]
        [string] $Format
    )

    $token = '${version}'
    $tokenCount = [regex]::Matches($Format, [regex]::Escape($token)).Count
    if ($tokenCount -ne 1) {
        throw "TagFormat must contain the '$token' token exactly once. Current value: '$Format'."
    }

    $Format.Replace($token, $Version)
}

function Get-SemanticReleaseDryRunOutput {
    if (![string]::IsNullOrWhiteSpace($SemanticReleaseDryRunOutputPath)) {
        if (!(Test-Path -LiteralPath $SemanticReleaseDryRunOutputPath -PathType Leaf)) {
            throw "Semantic-release dry-run output file was not found: $SemanticReleaseDryRunOutputPath"
        }

        return Get-Content -Raw -LiteralPath $SemanticReleaseDryRunOutputPath
    }

    Push-Location -LiteralPath $script:ResolvedRepositoryPath
    try {
        $output = & npm run release:dry-run --silent 2>&1
        $exitCode = $LASTEXITCODE
        if ($exitCode -ne 0) {
            throw "semantic-release dry-run failed with exit code $exitCode. Inspect the release preflight log and fix semantic-release configuration or repository credentials before publishing."
        }

        $output -join [Environment]::NewLine
    }
    finally {
        Pop-Location
    }
}

function Get-NextReleaseVersionFromDryRun {
    param([Parameter(Mandatory)][string] $DryRunOutput)

    if ($DryRunOutput -match "There are no relevant changes, so no new version is released\.") {
        return $null
    }

    $versionMatches = [regex]::Matches(
        $DryRunOutput,
        "The next release version is (?<version>[0-9]+\.[0-9]+\.[0-9]+(?:[-+][0-9A-Za-z.-]+)?)")

    if ($versionMatches.Count -eq 0) {
        throw "semantic-release dry-run completed, but the next release version could not be parsed. Expected 'The next release version is <version>' or the no-release message."
    }

    $distinctVersions = @($versionMatches | ForEach-Object { $_.Groups["version"].Value } | Select-Object -Unique)
    if ($distinctVersions.Count -gt 1) {
        $candidates = $distinctVersions -join ', '
        throw "semantic-release dry-run reported multiple distinct next-release versions ($candidates). The release preflight refuses to choose between them; resolve the semantic-release output before publishing."
    }

    $distinctVersions[0]
}

function Test-LocalTagCollision {
    param([Parameter(Mandatory)][string] $TagRef)

    $result = Invoke-GitCommand -Arguments @("show-ref", "--verify", "--quiet", $TagRef) -AllowedExitCodes @(0, 1)
    if ($result.ExitCode -eq 0) {
        throw "$TagRef already exists locally. Delete the stale local tag or choose a new semantic-release version before running release publish work."
    }
}

function Test-RemoteTagCollision {
    param(
        [Parameter(Mandatory)]
        [string] $TagRef,

        [Parameter(Mandatory)]
        [string] $Remote
    )

    # `git remote get-url` exits 2 on most builds but 128 on some when the remote is missing;
    # both must be treated as "remote not configured" so the preflight can skip the check loudly
    # via a warning instead of failing the release on the wrong code path.
    $remoteResult = Invoke-GitCommand -Arguments @("remote", "get-url", $Remote) -AllowedExitCodes @(0, 2, 128)
    if ($remoteResult.ExitCode -ne 0) {
        Write-Warning "Remote '$Remote' is not configured; skipping remote stale-tag check."
        return
    }

    $result = Invoke-GitCommand -Arguments @("ls-remote", "--tags", $Remote, $TagRef)
    foreach ($line in $result.Output) {
        $fields = "$line" -split "\s+"
        if ($fields.Count -lt 2) {
            continue
        }

        $refField = $fields[1].TrimEnd()
        if ($refField -eq $TagRef -or $refField -eq "$TagRef^{}") {
            throw "$TagRef already exists on remote '$Remote'. Delete the stale remote tag only after release-owner review, or choose a new semantic-release version before running release publish work."
        }
    }
}

try {
    # Force a known LASTEXITCODE baseline so the Invoke-GitCommand wrapper cannot inherit a stale
    # value from a prior step in the same pwsh session if the first git invocation fails to launch.
    $global:LASTEXITCODE = 0

    $script:ResolvedRepositoryPath = (Resolve-Path -LiteralPath $RepositoryPath).Path
    Invoke-GitCommand -Arguments @("rev-parse", "--git-dir") | Out-Null

    $version = $NextVersion
    if ([string]::IsNullOrWhiteSpace($version)) {
        $dryRunOutput = Get-SemanticReleaseDryRunOutput
        $version = Get-NextReleaseVersionFromDryRun -DryRunOutput $dryRunOutput
    }
    elseif ($version -notmatch $script:SemverPattern) {
        throw "NextVersion '$version' does not match the expected semver shape '<major>.<minor>.<patch>[-prerelease|+build]'. Pass the bare semver value (no leading 'v') or omit -NextVersion to let semantic-release dry-run compute it."
    }

    if ([string]::IsNullOrWhiteSpace($version)) {
        Write-Host "semantic-release dry-run reported no release; release preflight passed with no tag check required."
        exit 0
    }

    $tagName = Get-ReleaseTagName -Version $version -Format $TagFormat
    $tagRef = "refs/tags/$tagName"

    Test-LocalTagCollision -TagRef $tagRef
    Test-RemoteTagCollision -TagRef $tagRef -Remote $RemoteName

    Write-Host "No stale release tag found for $tagRef."
    exit 0
}
catch {
    Write-Error -Message $_.Exception.Message
    exit 1
}
