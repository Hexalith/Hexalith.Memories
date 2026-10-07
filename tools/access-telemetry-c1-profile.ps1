# Shared exact PG2 inputs; no target calls or output creation occur here.
function Assert-C1ApprovedProfileInputs {
    param([string]$RepositoryRoot)

    $approvedInputs = [ordered]@{
        'deploy/dapr/components/access-telemetry-config.yaml' = '5072909673df235c463a8f64bca8c65644ec22f435aebb663d2d9fc52d7c1b4a'
        'deploy/dapr/components/access-telemetry-secrets.yaml' = '0f34c483c8f531c107d6d318c1416b11d0448007acc1158dc6a2ab921b1f7c03'
        'deploy/dapr/components/access-telemetry-store.yaml' = '4ce8c049b6990a01a046ac372aa9ec1ebb93527afcef155232127a3b4a89c303'
        'deploy/kubernetes/base/access-telemetry-deployments.yaml' = 'aba898858bd7e82eb042b6ee50a76cd16821b154a65a7ab83f8e79ef45b835b6'
        'deploy/kubernetes/base/access-telemetry-postgresql.yaml' = 'b73b43cdb3683a5dbdc7de593c7e17caa300b857952f1ae592d13d496499a4f4'
        'deploy/kubernetes/base/dapr/access-telemetry-clock-config.yaml' = 'ee10d7831533ac0258829af1d82bf1b02e9b22d1d3c0f428ca6d67a41065de9e'
        'deploy/kubernetes/base/dapr/access-telemetry-config-store.yaml' = 'b458851266b2559192caf84f6a5737837336a5470754f7e7f609eadf5efe6303'
        'deploy/kubernetes/base/dapr/access-telemetry-lifecycle-config.yaml' = '981eac21ad9b40980887c0fe907ca5c6c70a166cd9494f9b3476db5e590eecf5'
        'deploy/kubernetes/base/dapr/access-telemetry-secrets.yaml' = '5bd7c2f0caa741df4e3fe45adb408d40dac7b5b37acdf227ea8c88680abf88d7'
        'deploy/kubernetes/base/dapr/access-telemetry-store.yaml' = '457e440c74563d4c2323cef003c393edbd1c5d56c6ef4ad2c005e812bbbb0270'
        'deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml' = '0c2b4b836d14be457ab8ccc79a534cd9997193f9c7b10f3d11b8b100a8b40ab2'
        'deploy/kubernetes/overlays/production/kustomization.yaml' = 'f1ec26757295a6da23f1f165453bbb023cae935b9dcdd7000596a0e512913c4a'
        'deploy/kubernetes/overlays/qualification/physical-evidence-reporter-job.yaml' = '59651af0c528d065f0f8686a6802df0cbbf8edae4a4f31455a8bf58cae680711'
        'deploy/openbao/service-account-hardening.yaml' = '44571c24d6b7383428bb1fa0da1f30843e289fa722984f90fa86caf959d6b039'
        'deploy/openbao/smoke-test.yaml' = 'c1f4eb2c21b82a0544eb81bab189275d6edf15864932e51c481d47354dedeee7'
        'deploy/openbao/values.yaml' = '4d6e8909695100901be5008b5a4cd11d108ee03b495d26243d23a22ab25a0caa'
    }
    $result = [System.Collections.Generic.List[object]]::new()
    foreach ($relative in $approvedInputs.Keys) {
        $sourcePath = Join-Path $RepositoryRoot $relative
        if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf) -or
            $null -ne (Get-Item -LiteralPath $sourcePath).LinkType) {
            throw 'approved-profile-input-unavailable'
        }
        $sourceBytes = [System.IO.File]::ReadAllBytes($sourcePath)
        $sourceSha256 = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($sourceBytes)).ToLowerInvariant()
        if ($sourceSha256 -cne $approvedInputs[$relative]) { throw 'approved-profile-input-drift' }
        if ($script:pg2RuntimeCapture) { $script:runtimeSourceSnapshots[$relative] = $sourceBytes }
        $result.Add([ordered]@{ source = $relative; sha256 = $sourceSha256 })
    }
    return $result.ToArray()
}

function Get-C1JsonSha256 {
    param([object]$Value)

    return Get-TextSha256 (ConvertTo-Json -InputObject $Value -Compress -Depth 14)
}

function Invoke-C1SourceGit {
    param([string[]]$Arguments)

    $receipt = [ordered]@{
        executable = 'git'
        arguments = @($Arguments)
        argumentsSha256 = Get-C1JsonSha256 @($Arguments)
        startedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
        finishedAtUtc = $null
        exitCode = $null
        stdoutSha256 = $null
        stderrSha256 = $null
        streamSafety = 'not-validated'
        resultCount = 0
        failureCount = 1
        skipCount = 0
    }
    $script:sourceCommandLedger.Add($receipt)
    $start = [System.Diagnostics.ProcessStartInfo]::new()
    $start.FileName = 'git'
    $start.WorkingDirectory = $script:repositoryRoot
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $start
    $output = [System.IO.MemoryStream]::new()
    $errors = [System.IO.MemoryStream]::new()
    $started = $false
    try {
        $deadline = [DateTimeOffset]::UtcNow.AddSeconds(5)
        if (-not $process.Start()) { throw 'source-git-unavailable' }
        $started = $true
        $stdoutBuffer = [byte[]]::new(8192)
        $stderrBuffer = [byte[]]::new(8192)
        $stdoutTask = $process.StandardOutput.BaseStream.ReadAsync($stdoutBuffer, 0, $stdoutBuffer.Length)
        $stderrTask = $process.StandardError.BaseStream.ReadAsync($stderrBuffer, 0, $stderrBuffer.Length)
        while ($null -ne $stdoutTask -or $null -ne $stderrTask) {
            $remaining = $deadline - [DateTimeOffset]::UtcNow
            if ($remaining -le [TimeSpan]::Zero) { throw 'source-git-timeout' }
            $pending = [System.Collections.Generic.List[System.Threading.Tasks.Task]]::new()
            if ($null -ne $stdoutTask) { $pending.Add($stdoutTask) }
            if ($null -ne $stderrTask) { $pending.Add($stderrTask) }
            try {
                $completed = [System.Threading.Tasks.Task]::WhenAny(
                    [System.Threading.Tasks.Task[]]$pending).WaitAsync($remaining).GetAwaiter().GetResult()
            }
            catch [System.TimeoutException] { throw 'source-git-timeout' }
            if ($null -ne $stdoutTask -and [object]::ReferenceEquals($completed, $stdoutTask)) {
                $count = $stdoutTask.GetAwaiter().GetResult()
                if ($count -eq 0) { $stdoutTask = $null }
                else {
                    if ($output.Length + $count -gt 1MB) { throw 'source-git-output-too-large' }
                    $output.Write($stdoutBuffer, 0, $count)
                    $stdoutTask = $process.StandardOutput.BaseStream.ReadAsync($stdoutBuffer, 0, $stdoutBuffer.Length)
                }
            }
            if ($null -ne $stderrTask -and [object]::ReferenceEquals($completed, $stderrTask)) {
                $count = $stderrTask.GetAwaiter().GetResult()
                if ($count -eq 0) { $stderrTask = $null }
                else {
                    if ($errors.Length + $count -gt 1MB) { throw 'source-git-output-too-large' }
                    $errors.Write($stderrBuffer, 0, $count)
                    $stderrTask = $process.StandardError.BaseStream.ReadAsync($stderrBuffer, 0, $stderrBuffer.Length)
                }
            }
        }
        $remainingMilliseconds = [int][Math]::Max(0,
            [Math]::Ceiling(($deadline - [DateTimeOffset]::UtcNow).TotalMilliseconds))
        if (-not $process.WaitForExit($remainingMilliseconds)) { throw 'source-git-timeout' }
        $receipt.stdoutSha256 = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($output.ToArray())).ToLowerInvariant()
        $receipt.stderrSha256 = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($errors.ToArray())).ToLowerInvariant()
        $receipt.streamSafety = 'hash-only-source-provenance'
        if ($process.ExitCode -ne 0) { throw 'source-git-unavailable' }
        $receipt.resultCount = 1
        $receipt.failureCount = 0
        return ,$output.ToArray()
    }
    finally {
        if ($started) {
            # Attempt tree cleanup even when an exited parent left inherited pipes open.
            if ($receipt.streamSafety -ceq 'not-validated') {
                try { $process.Kill($true) } catch { }
            }
            try { $process.StandardOutput.Close() } catch { }
            try { $process.StandardError.Close() } catch { }
            try { if ($process.HasExited) { $receipt.exitCode = $process.ExitCode } } catch { }
        }
        $receipt.finishedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
        $process.Dispose(); $output.Dispose(); $errors.Dispose()
    }
}

function Get-C1SourceState {
    param([string]$Relative, [AllowNull()][byte[]]$LoadedBytes = $null)

    $path = Join-Path $script:repositoryRoot $Relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or
        $null -ne (Get-Item -LiteralPath $path).LinkType) { throw 'producer-source-unavailable' }
    $currentBytes = [System.IO.File]::ReadAllBytes($path)
    $bytes = $currentBytes
    if ($null -ne $LoadedBytes) {
        if (-not [System.Linq.Enumerable]::SequenceEqual[byte]($currentBytes, $LoadedBytes)) {
            throw 'producer-source-changed'
        }
        $bytes = $LoadedBytes
    }
    # The closed source set contains UTF-8 text with Git's CRLF -> LF clean rule.
    # Confirm the normalized bytes against Git's actual clean-filter object identity.
    $utf8 = [System.Text.UTF8Encoding]::new($false, $true)
    $normalizedBytes = $utf8.GetBytes($utf8.GetString($bytes).Replace("`r`n", "`n"))
    $objectId = $utf8.GetString((Invoke-C1SourceGit @('hash-object', "--path=$Relative", '--', $path))).Trim()
    $header = [System.Text.Encoding]::ASCII.GetBytes("blob $($normalizedBytes.Length)" + [char]0)
    $blobBytes = [byte[]]::new($header.Length + $normalizedBytes.Length)
    [Array]::Copy($header, $blobBytes, $header.Length)
    [Array]::Copy($normalizedBytes, 0, $blobBytes, $header.Length, $normalizedBytes.Length)
    $blobHash = [Convert]::ToHexString([System.Security.Cryptography.SHA1]::HashData($blobBytes)).ToLowerInvariant()
    if ($objectId -cne $blobHash) { throw 'producer-source-normalization-unsupported' }
    $headObject = $utf8.GetString((Invoke-C1SourceGit @('ls-tree', $script:sourceCommit, '--', $Relative))).Trim()
    $headSha256 = $null
    if ($headObject) {
        $headBytes = Invoke-C1SourceGit @('show', "$($script:sourceCommit):$Relative")
        $headSha256 = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($headBytes)).ToLowerInvariant()
    }
    $status = $utf8.GetString((Invoke-C1SourceGit @('status', '--porcelain=v1', '--untracked-files=all', '--', $Relative))).Trim()
    # Git reads the live path; prove it still identifies the snapshot after every query.
    if (-not [System.Linq.Enumerable]::SequenceEqual[byte]($bytes, [System.IO.File]::ReadAllBytes($path))) {
        throw 'producer-source-changed'
    }
    $normalizedSha256 = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($normalizedBytes)).ToLowerInvariant()
    return [ordered]@{
        source = $Relative
        gitNormalizedSha256 = $normalizedSha256
        executedBytesSha256 = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
        gitBlobOid = $objectId
        headGitNormalizedSha256 = $headSha256
        trackedAtHead = [bool]$headObject
        modified = [bool]$status
        matchesHead = ($null -ne $headSha256 -and $headSha256 -ceq $normalizedSha256)
    }
}

function Assert-C1RuntimeSourceSnapshotsUnchanged {
    foreach ($relative in $script:runtimeSourceSnapshots.Keys) {
        $path = Join-Path $script:repositoryRoot $relative
        if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or
            $null -ne (Get-Item -LiteralPath $path).LinkType -or
            -not [System.Linq.Enumerable]::SequenceEqual[byte](
                $script:runtimeSourceSnapshots[$relative], [System.IO.File]::ReadAllBytes($path))) {
            throw 'producer-source-changed'
        }
    }
}

function Initialize-C1RuntimeProvenance {
    param([object[]]$ApprovedInputs)

    $utf8 = [System.Text.Encoding]::UTF8
    $script:sourceCommit = $utf8.GetString((Invoke-C1SourceGit @('rev-parse', '--verify', 'HEAD'))).Trim()
    if ($script:sourceCommit -cnotmatch '^[0-9a-f]{40}$') { throw 'source-commit-invalid' }
    $script:worktreeDirty = $utf8.GetString((Invoke-C1SourceGit @('status', '--porcelain=v1', '--untracked-files=all'))).Length -gt 0
    $script:producerSources = [System.Collections.Generic.List[object]]::new()
    foreach ($relative in @('tools/verify-access-telemetry-c1.ps1', 'tools/access-telemetry-c1-profile.ps1',
        'tools/access-telemetry-c1-component-backend.ps1') + @($ApprovedInputs | ForEach-Object { $_.source })) {
        $state = Get-C1SourceState $relative $script:runtimeSourceSnapshots[$relative]
        $approved = @($ApprovedInputs | Where-Object { $_.source -ceq $relative })
        if ($approved.Count -eq 1 -and $state.executedBytesSha256 -cne $approved[0].sha256) { throw 'approved-profile-input-drift' }
        $script:producerSources.Add($state)
    }
    Assert-C1RuntimeSourceSnapshotsUnchanged
    $script:sourceDisposition = 'clean'
    if ($script:worktreeDirty -or @($script:producerSources | Where-Object { $_.modified -or -not $_.matchesHead }).Count) {
        $script:sourceDisposition = 'dirty-development'
    }
}

function Assert-C1RuntimeSourcesUnchanged {
    $changed = $false
    foreach ($source in $script:producerSources) {
        $after = Get-C1SourceState $source.source
        if ((Get-C1JsonSha256 $after) -cne (Get-C1JsonSha256 $source)) { $changed = $true }
    }
    $utf8 = [System.Text.Encoding]::UTF8
    $headNow = $utf8.GetString((Invoke-C1SourceGit @('rev-parse', '--verify', 'HEAD'))).Trim()
    $script:worktreeDirty = $utf8.GetString((Invoke-C1SourceGit @('status', '--porcelain=v1', '--untracked-files=all'))).Length -gt 0
    if ($script:worktreeDirty) { $script:sourceDisposition = 'dirty-development' }
    # This byte-only sweep is last: later Git children cannot invalidate earlier checks.
    Assert-C1RuntimeSourceSnapshotsUnchanged
    if ($headNow -cne $script:sourceCommit -or $changed) { throw 'producer-source-changed' }
}
