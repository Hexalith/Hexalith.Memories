[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('C1.15')]
    [string]$Gate,

    [Parameter(Mandatory)]
    [ValidateSet('PG-ONPREM-1')]
    [string]$ProfileId,

    [Parameter(Mandatory)]
    [string]$EvidenceDirectory,

    [ValidateRange(1, 300)]
    [int]$CommandTimeoutSeconds = 30
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$namespace = 'hexalith-memories'
$expectedContext = 'jpiquot@local'
$targetSelector = 'app.kubernetes.io/name=memories-access-telemetry'
$expectedAppId = 'memories-access-telemetry'
$expectedActorType = 'AccessTelemetryLifecycleActor'
$script:commandLedger = [System.Collections.Generic.List[object]]::new()
$script:sourceLedger = [System.Collections.Generic.List[object]]::new()

function Get-TextSha256 {
    param([AllowEmptyString()][string]$Text)

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
    return [System.Convert]::ToHexString($hash).ToLowerInvariant()
}

function Add-SourceHash {
    param(
        [Parameter(Mandatory)][string]$Source,
        [AllowEmptyString()][string]$Content
    )

    $script:sourceLedger.Add([ordered]@{
        source = $Source
        sha256 = Get-TextSha256 $Content
    })
}

function Assert-SecretSafeOutput {
    param([AllowEmptyString()][string]$Text)

    if ($Text -match '(?i)(C1[_-]?SECRET[_-]?CANARY|SECRET[_-]?CANARY|(?:authorization|dapr[-_]?api[-_]?token)\s*[:=]\s*[^\s"'']+|\b(?:hvs|hvb|hvr)\.[A-Za-z0-9_-]{8,})') {
        throw 'secret-shaped-output'
    }
    if ($Text -match '(?i)"(?:authorization|dapr[-_]?api[-_]?token)"\s*:\s*"(?:[^"\\]|\\.)+"') {
        throw 'secret-shaped-output'
    }
}

function Invoke-KubectlObservation {
    param(
        [Parameter(Mandatory)][string]$Purpose,
        [Parameter(Mandatory)][string[]]$Arguments,
        [switch]$SkipSourceHash
    )

    $commandIdentity = 'kubectl ' + ($Arguments -join [char]0x1f)
    $script:commandLedger.Add([ordered]@{
        purpose = $Purpose
        sha256 = Get-TextSha256 $commandIdentity
    })

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'kubectl'
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    foreach ($argument in $Arguments) {
        $startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    $stdoutCapture = [System.IO.MemoryStream]::new()
    $stderrCapture = [System.IO.MemoryStream]::new()
    try {
        try {
            if (-not $process.Start()) {
                throw 'process-start-returned-false'
            }
        }
        catch {
            throw "kubectl-$Purpose-execution-failed"
        }

        $maximumCaptureBytes = 1MB
        $stdoutExceeded = $false
        $stderrExceeded = $false
        $stdoutBuffer = [byte[]]::new(8192)
        $stderrBuffer = [byte[]]::new(8192)
        $stdoutTask = $process.StandardOutput.BaseStream.ReadAsync($stdoutBuffer, 0, $stdoutBuffer.Length)
        $stderrTask = $process.StandardError.BaseStream.ReadAsync($stderrBuffer, 0, $stderrBuffer.Length)
        $deadline = [DateTimeOffset]::UtcNow.AddSeconds($CommandTimeoutSeconds)
        while ($null -ne $stdoutTask -or $null -ne $stderrTask) {
            $remaining = $deadline - [DateTimeOffset]::UtcNow
            if ($remaining -le [TimeSpan]::Zero) {
                try {
                    $process.Kill($true)
                }
                catch {
                    # The stable timeout blocker below is authoritative even if cleanup races.
                }
                throw "kubectl-$Purpose-timeout"
            }

            $pendingTasks = [System.Collections.Generic.List[System.Threading.Tasks.Task]]::new()
            if ($null -ne $stdoutTask) {
                $pendingTasks.Add($stdoutTask)
            }
            if ($null -ne $stderrTask) {
                $pendingTasks.Add($stderrTask)
            }
            try {
                $completedTask = [System.Threading.Tasks.Task]::WhenAny(
                    [System.Threading.Tasks.Task[]]$pendingTasks).WaitAsync($remaining).GetAwaiter().GetResult()
            }
            catch [System.TimeoutException] {
                try {
                    $process.Kill($true)
                }
                catch {
                    # The stable timeout blocker below is authoritative even if cleanup races.
                }
                throw "kubectl-$Purpose-timeout"
            }

            if ($null -ne $stdoutTask -and [object]::ReferenceEquals($completedTask, $stdoutTask)) {
                $count = $stdoutTask.GetAwaiter().GetResult()
                if ($count -eq 0) {
                    $stdoutTask = $null
                }
                else {
                    $remainingCapture = [int][Math]::Max(0, $maximumCaptureBytes - $stdoutCapture.Length)
                    $captured = [Math]::Min($remainingCapture, $count)
                    if ($captured -gt 0) {
                        $stdoutCapture.Write($stdoutBuffer, 0, $captured)
                    }
                    if ($captured -ne $count) {
                        $stdoutExceeded = $true
                    }
                    $stdoutTask = $process.StandardOutput.BaseStream.ReadAsync($stdoutBuffer, 0, $stdoutBuffer.Length)
                }
            }
            if ($null -ne $stderrTask -and [object]::ReferenceEquals($completedTask, $stderrTask)) {
                $count = $stderrTask.GetAwaiter().GetResult()
                if ($count -eq 0) {
                    $stderrTask = $null
                }
                else {
                    $remainingCapture = [int][Math]::Max(0, $maximumCaptureBytes - $stderrCapture.Length)
                    $captured = [Math]::Min($remainingCapture, $count)
                    if ($captured -gt 0) {
                        $stderrCapture.Write($stderrBuffer, 0, $captured)
                    }
                    if ($captured -ne $count) {
                        $stderrExceeded = $true
                    }
                    $stderrTask = $process.StandardError.BaseStream.ReadAsync($stderrBuffer, 0, $stderrBuffer.Length)
                }
            }
        }

        $remainingMilliseconds = [int][Math]::Max(
            0,
            [Math]::Ceiling(($deadline - [DateTimeOffset]::UtcNow).TotalMilliseconds))
        if (-not $process.WaitForExit($remainingMilliseconds)) {
            try {
                $process.Kill($true)
            }
            catch {
                # The stable timeout blocker below is authoritative even if cleanup races.
            }
            throw "kubectl-$Purpose-timeout"
        }

        $stdout = [System.Text.Encoding]::UTF8.GetString($stdoutCapture.ToArray())
        $stderr = [System.Text.Encoding]::UTF8.GetString($stderrCapture.ToArray())
        $exitCode = $process.ExitCode
        if ($stdoutExceeded -or $stderrExceeded) {
            throw "kubectl-$Purpose-output-too-large"
        }
    }
    finally {
        $process.Dispose()
        $stdoutCapture.Dispose()
        $stderrCapture.Dispose()
    }

    Assert-SecretSafeOutput $stdout
    Assert-SecretSafeOutput $stderr
    if (-not $SkipSourceHash) {
        Add-SourceHash "kubectl:$Purpose:stdout" $stdout
        Add-SourceHash "kubectl:$Purpose:stderr" $stderr
    }

    if ($exitCode -ne 0) {
        throw "kubectl-$Purpose-exit-$exitCode"
    }

    return $stdout.Trim()
}

function Get-RequiredProperty {
    param(
        [Parameter(Mandatory)][object]$Object,
        [Parameter(Mandatory)][string[]]$Names,
        [Parameter(Mandatory)][string]$FailureCode
    )

    foreach ($name in $Names) {
        $property = $Object.PSObject.Properties[$name]
        if ($null -ne $property) {
            return ,$property.Value
        }
    }

    throw $FailureCode
}

function ConvertTo-ExplicitBoolean {
    param(
        [AllowEmptyString()][string]$Value,
        [Parameter(Mandatory)][string]$FailureCode
    )

    $parsed = $false
    if (-not [bool]::TryParse($Value, [ref]$parsed)) {
        throw $FailureCode
    }

    return $parsed
}

function ConvertTo-ValidatedStringArray {
    param(
        [AllowNull()][object]$Value,
        [Parameter(Mandatory)][string]$Pattern,
        [Parameter(Mandatory)][int]$MaximumLength,
        [Parameter(Mandatory)][bool]$AllowEmpty,
        [Parameter(Mandatory)][string]$FailureCode
    )

    if ($null -eq $Value -or $Value -isnot [System.Array]) {
        throw $FailureCode
    }

    $rawValues = @($Value)
    if (-not $AllowEmpty -and $rawValues.Count -eq 0) {
        throw $FailureCode
    }

    $validated = [System.Collections.Generic.List[string]]::new()
    foreach ($rawValue in $rawValues) {
        if ($rawValue -isnot [string] -or [string]::IsNullOrWhiteSpace($rawValue) -or
            $rawValue.Length -gt $MaximumLength -or $rawValue -notmatch $Pattern) {
            throw $FailureCode
        }
        $validated.Add($rawValue)
    }

    $normalized = @($validated | Sort-Object -CaseSensitive -Unique)
    if ($normalized.Count -ne $rawValues.Count) {
        throw $FailureCode
    }
    return $normalized
}

function Get-CollectionIdentity {
    param([AllowEmptyCollection()][object[]]$Values)

    return (@($Values) -join [char]0x1f)
}

function Write-ImmutablePacket {
    param(
        [Parameter(Mandatory)][object]$Packet,
        [Parameter(Mandatory)][string]$Directory
    )

    $resolvedDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Directory)
    [System.IO.Directory]::CreateDirectory($resolvedDirectory) | Out-Null
    $timestamp = [DateTimeOffset]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
    $captureId = [Guid]::NewGuid().ToString('N')
    $path = Join-Path $resolvedDirectory "c1.15-runtime-control-plane-identity-$timestamp-$captureId.json"
    $json = ($Packet | ConvertTo-Json -Depth 14) + [Environment]::NewLine
    $encoding = [System.Text.UTF8Encoding]::new($false)
    $stream = [System.IO.File]::Open($path, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
    try {
        $writer = [System.IO.StreamWriter]::new($stream, $encoding)
        try {
            $writer.Write($json)
        }
        finally {
            $writer.Dispose()
        }
    }
    finally {
        if ($null -ne $stream) {
            $stream.Dispose()
        }
    }

    try {
        [System.IO.File]::SetAttributes(
            $path,
            [System.IO.File]::GetAttributes($path) -bor [System.IO.FileAttributes]::ReadOnly)
    }
    catch {
        try {
            [System.IO.File]::Delete($path)
        }
        catch {
            # The stable packet-finalization blocker below remains authoritative.
        }
        throw 'packet-immutability-failed'
    }

    return $path
}

$capturedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
$observations = [ordered]@{
    pods = @()
    runtimeVersions = @()
    sidecarImageIds = @()
    sidecarImageDigests = @()
    appIds = @()
    schedulerConnectedAddresses = @()
    actorTypes = @()
    enabledFeatures = @()
    alphaOptIn = [ordered]@{
        componentIsAlpha = $null
        allowAlphaComponent = $null
    }
}
$producerStatus = 'blocked'
$blockers = [System.Collections.Generic.List[string]]::new()
$context = $null

try {
    $script:sourceLedger.Add([ordered]@{
        source = 'tools/verify-access-telemetry-c1.ps1'
        sha256 = (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant()
    })

    $context = Invoke-KubectlObservation -Purpose 'current-context' -Arguments @('config', 'current-context')
    if (-not [string]::Equals($context, $expectedContext, [StringComparison]::Ordinal)) {
        throw 'profile-context-mismatch'
    }

    $podsJson = Invoke-KubectlObservation -Purpose 'lifecycle-pods' -Arguments @(
        '--context', $context,
        '-n', $namespace,
        'get', 'pods',
        '-l', $targetSelector,
        '-o', 'json'
    )
    try {
        $podsPayload = $podsJson | ConvertFrom-Json -Depth 30
    }
    catch {
        throw 'malformed-pod-list-json'
    }
    if ($null -eq $podsPayload.items -or $podsPayload.items -isnot [System.Array]) {
        throw 'malformed-pod-list-json'
    }

    $runningPods = @($podsPayload.items | Where-Object {
        [string]::Equals([string]$_.status.phase, 'Running', [StringComparison]::Ordinal)
    })
    if ($runningPods.Count -eq 0) {
        throw 'no-running-lifecycle-pod'
    }

    $perPod = [System.Collections.Generic.List[object]]::new()
    $seenPodNames = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($pod in $runningPods) {
        $podName = [string]$pod.metadata.name
        if ([string]::IsNullOrWhiteSpace($podName)) {
            throw 'running-pod-name-missing'
        }
        if (-not $seenPodNames.Add($podName)) {
            throw 'duplicate-running-pod'
        }
        $podUid = [string]$pod.metadata.uid
        if ([string]::IsNullOrWhiteSpace($podUid)) {
            throw 'running-pod-uid-missing'
        }

        $podLabel = [string]$pod.metadata.labels.'app.kubernetes.io/name'
        if (-not [string]::Equals($podLabel, $expectedAppId, [StringComparison]::Ordinal)) {
            throw 'running-pod-label-mismatch'
        }
        if ($null -ne $pod.metadata.PSObject.Properties['deletionTimestamp'] -and
            $null -ne $pod.metadata.deletionTimestamp) {
            throw 'running-pod-not-stable'
        }
        $readyConditions = @($pod.status.conditions | Where-Object { $_.type -eq 'Ready' })
        if ($readyConditions.Count -ne 1 -or
            -not [string]::Equals([string]$readyConditions[0].status, 'True', [StringComparison]::Ordinal)) {
            throw 'running-pod-not-stable'
        }

        $containerStatuses = @($pod.status.containerStatuses)
        $lifecycleStatus = @($containerStatuses | Where-Object { $_.name -eq 'lifecycle' })
        $sidecarStatus = @($containerStatuses | Where-Object { $_.name -eq 'daprd' })
        if ($lifecycleStatus.Count -ne 1 -or $sidecarStatus.Count -ne 1 -or
            $lifecycleStatus[0].ready -isnot [bool] -or $sidecarStatus[0].ready -isnot [bool] -or
            -not $lifecycleStatus[0].ready -or -not $sidecarStatus[0].ready) {
            throw 'running-pod-containers-not-ready'
        }

        $imageId = [string]$sidecarStatus[0].imageID
        $digestMatch = [regex]::Match($imageId, 'sha256:[0-9a-f]{64}$')
        if (-not $digestMatch.Success) {
            throw 'sidecar-imageid-digest-missing'
        }

        $daprdVersion = Invoke-KubectlObservation -Purpose "daprd-version:$podName" -Arguments @(
            '--context', $context,
            '-n', $namespace,
            'exec', $podName,
            '-c', 'daprd',
            '--', '/daprd', '--version'
        )
        if ([string]::IsNullOrWhiteSpace($daprdVersion) -or
            $daprdVersion.Trim() -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(?:[-+][0-9A-Za-z.-]+)?$') {
            throw 'daprd-version-invalid'
        }

        $metadataProbe = 'if [ -z "${DAPR_API_TOKEN:-}" ]; then echo "required runtime credential unavailable" >&2; exit 72; fi; metadata="$(wget -qO- --timeout=5 --header="dapr-api-token: ${DAPR_API_TOKEN}" http://127.0.0.1:3500/v1.0/metadata)" || exit $?; case "$metadata" in *"$DAPR_API_TOKEN"*) echo "secret-shaped-output" >&2; exit 73;; esac; printf "%s" "$metadata"'
        $metadataJson = Invoke-KubectlObservation -Purpose "metadata:$podName" -Arguments @(
            '--context', $context,
            '-n', $namespace,
            'exec', $podName,
            '-c', 'lifecycle',
            '--', '/bin/sh', '-ec', $metadataProbe
        ) -SkipSourceHash
        try {
            $metadata = $metadataJson | ConvertFrom-Json -Depth 30
        }
        catch {
            throw 'malformed-metadata-json'
        }

        $appId = [string](Get-RequiredProperty -Object $metadata -Names @('id') -FailureCode 'metadata-app-id-missing')
        if (-not [string]::Equals($appId, $expectedAppId, [StringComparison]::Ordinal)) {
            throw 'metadata-app-id-mismatch'
        }

        $runtimeVersion = [string](Get-RequiredProperty -Object $metadata -Names @('runtimeVersion') -FailureCode 'metadata-runtime-version-missing')
        if ([string]::IsNullOrWhiteSpace($runtimeVersion) -or
            $runtimeVersion.Trim() -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(?:[-+][0-9A-Za-z.-]+)?$') {
            throw 'metadata-runtime-version-invalid'
        }
        if (-not [string]::Equals($runtimeVersion.Trim(), $daprdVersion.Trim(), [StringComparison]::Ordinal)) {
            throw 'runtime-version-mismatch'
        }

        $scheduler = Get-RequiredProperty -Object $metadata -Names @('scheduler') -FailureCode 'metadata-scheduler-missing'
        if ($null -ne $scheduler.PSObject.Properties['connectedAddresses'] -and
            $null -ne $scheduler.PSObject.Properties['connected_addresses']) {
            throw 'metadata-scheduler-addresses-ambiguous'
        }
        $schedulerAddresses = @(ConvertTo-ValidatedStringArray `
            -Value (Get-RequiredProperty -Object $scheduler -Names @('connectedAddresses', 'connected_addresses') -FailureCode 'metadata-scheduler-addresses-missing') `
            -Pattern '^[A-Za-z0-9][A-Za-z0-9._-]{0,252}:[0-9]{1,5}$' `
            -MaximumLength 259 `
            -AllowEmpty $false `
            -FailureCode 'metadata-scheduler-addresses-invalid'
        )
        foreach ($schedulerAddress in $schedulerAddresses) {
            $port = 0
            $portText = $schedulerAddress.Substring($schedulerAddress.LastIndexOf(':') + 1)
            if (-not [int]::TryParse($portText, [ref]$port) -or $port -lt 1 -or $port -gt 65535) {
                throw 'metadata-scheduler-addresses-invalid'
            }
        }

        $actorsValue = Get-RequiredProperty -Object $metadata -Names @('actors') -FailureCode 'metadata-actors-missing'
        if ($null -eq $actorsValue -or $actorsValue -isnot [System.Array]) {
            throw 'metadata-actors-invalid'
        }
        $rawActorTypes = @(
            @($actorsValue) |
                ForEach-Object {
                    Get-RequiredProperty -Object $_ -Names @('type') -FailureCode 'metadata-actor-type-invalid'
                }
        )
        $actorTypes = @(ConvertTo-ValidatedStringArray `
            -Value $rawActorTypes `
            -Pattern '^[A-Za-z][A-Za-z0-9._-]{0,127}$' `
            -MaximumLength 128 `
            -AllowEmpty $false `
            -FailureCode 'metadata-actor-type-invalid'
        )
        if ($actorTypes.Count -eq 0 -or
            @($actorTypes | Where-Object { [string]::Equals($_, $expectedActorType, [StringComparison]::Ordinal) }).Count -eq 0) {
            throw 'metadata-actor-type-missing'
        }

        $enabledFeaturesValue = Get-RequiredProperty -Object $metadata -Names @('enabledFeatures') -FailureCode 'metadata-enabled-features-missing'
        $enabledFeatures = @(ConvertTo-ValidatedStringArray `
            -Value $enabledFeaturesValue `
            -Pattern '^[A-Za-z][A-Za-z0-9._/-]{0,127}$' `
            -MaximumLength 128 `
            -AllowEmpty $true `
            -FailureCode 'metadata-enabled-features-invalid'
        )

        $metadataProjection = [ordered]@{
            id = $appId
            runtimeVersion = $runtimeVersion.Trim()
            schedulerConnectedAddresses = $schedulerAddresses
            actorTypes = $actorTypes
            enabledFeatures = $enabledFeatures
        }
        $metadataProjectionJson = $metadataProjection | ConvertTo-Json -Compress -Depth 5
        Assert-SecretSafeOutput $metadataProjectionJson
        Add-SourceHash "kubectl:metadata:${podName}:allowlisted" $metadataProjectionJson

        $alphaProbe = 'printf "%s\n%s\n" "${AccessTelemetryLifecycle__ComponentIsAlpha-__MISSING__}" "${AccessTelemetryLifecycle__AllowAlphaComponent-__MISSING__}"'
        $alphaOutput = Invoke-KubectlObservation -Purpose "alpha-opt-in:$podName" -Arguments @(
            '--context', $context,
            '-n', $namespace,
            'exec', $podName,
            '-c', 'lifecycle',
            '--', '/bin/sh', '-ec', $alphaProbe
        )
        $alphaLines = @($alphaOutput -split '\r?\n')
        if ($alphaLines.Count -ne 2) {
            throw 'alpha-opt-in-output-invalid'
        }
        $componentIsAlpha = ConvertTo-ExplicitBoolean -Value $alphaLines[0] -FailureCode 'component-is-alpha-missing'
        $allowAlphaComponent = ConvertTo-ExplicitBoolean -Value $alphaLines[1] -FailureCode 'allow-alpha-component-missing'
        if ($componentIsAlpha -and -not $allowAlphaComponent) {
            throw 'alpha-component-not-explicitly-allowed'
        }

        $perPod.Add([ordered]@{
            pod = $podName
            podUid = $podUid
            runtimeVersion = $runtimeVersion.Trim()
            sidecarImageId = $imageId
            sidecarImageDigest = $digestMatch.Value
            appId = $appId
            schedulerConnectedAddresses = $schedulerAddresses
            actorTypes = $actorTypes
            enabledFeatures = $enabledFeatures
            alphaOptIn = [ordered]@{
                componentIsAlpha = $componentIsAlpha
                allowAlphaComponent = $allowAlphaComponent
            }
        })
    }

    $podsAfterJson = Invoke-KubectlObservation -Purpose 'lifecycle-pods-recheck' -Arguments @(
        '--context', $context,
        '-n', $namespace,
        'get', 'pods',
        '-l', $targetSelector,
        '-o', 'json'
    )
    try {
        $podsAfterPayload = $podsAfterJson | ConvertFrom-Json -Depth 30
    }
    catch {
        throw 'malformed-pod-list-json'
    }
    if ($null -eq $podsAfterPayload.items -or $podsAfterPayload.items -isnot [System.Array]) {
        throw 'malformed-pod-list-json'
    }

    $runningPodsAfter = @($podsAfterPayload.items | Where-Object {
        [string]::Equals([string]$_.status.phase, 'Running', [StringComparison]::Ordinal)
    })
    if ($runningPodsAfter.Count -ne $perPod.Count) {
        throw 'running-pod-changed'
    }
    $initialPods = [System.Collections.Generic.Dictionary[string, object]]::new([StringComparer]::Ordinal)
    foreach ($observedPod in $perPod) {
        $initialPods.Add([string]$observedPod.pod, $observedPod)
    }
    $seenAfterPodNames = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($podAfter in $runningPodsAfter) {
        $podAfterName = [string]$podAfter.metadata.name
        if ([string]::IsNullOrWhiteSpace($podAfterName) -or
            -not $seenAfterPodNames.Add($podAfterName) -or
            -not $initialPods.ContainsKey($podAfterName)) {
            throw 'running-pod-changed'
        }
        $initialPod = $initialPods[$podAfterName]
        $podAfterUid = [string]$podAfter.metadata.uid
        $podAfterLabel = [string]$podAfter.metadata.labels.'app.kubernetes.io/name'
        $readyConditionsAfter = @($podAfter.status.conditions | Where-Object { $_.type -eq 'Ready' })
        $containerStatusesAfter = @($podAfter.status.containerStatuses)
        $lifecycleStatusAfter = @($containerStatusesAfter | Where-Object { $_.name -eq 'lifecycle' })
        $sidecarStatusAfter = @($containerStatusesAfter | Where-Object { $_.name -eq 'daprd' })
        if (-not [string]::Equals($podAfterUid, [string]$initialPod.podUid, [StringComparison]::Ordinal) -or
            -not [string]::Equals($podAfterLabel, $expectedAppId, [StringComparison]::Ordinal) -or
            ($null -ne $podAfter.metadata.PSObject.Properties['deletionTimestamp'] -and
                $null -ne $podAfter.metadata.deletionTimestamp) -or
            $readyConditionsAfter.Count -ne 1 -or
            -not [string]::Equals([string]$readyConditionsAfter[0].status, 'True', [StringComparison]::Ordinal) -or
            $lifecycleStatusAfter.Count -ne 1 -or $sidecarStatusAfter.Count -ne 1 -or
            $lifecycleStatusAfter[0].ready -isnot [bool] -or $sidecarStatusAfter[0].ready -isnot [bool] -or
            -not $lifecycleStatusAfter[0].ready -or -not $sidecarStatusAfter[0].ready -or
            -not [string]::Equals([string]$sidecarStatusAfter[0].imageID, [string]$initialPod.sidecarImageId, [StringComparison]::Ordinal)) {
            throw 'running-pod-changed'
        }
    }

    $runtimeVersions = @($perPod | ForEach-Object { $_.runtimeVersion } | Sort-Object -CaseSensitive -Unique)
    $sidecarImageIds = @($perPod | ForEach-Object { $_.sidecarImageId } | Sort-Object -CaseSensitive -Unique)
    $sidecarImageDigests = @($perPod | ForEach-Object { $_.sidecarImageDigest } | Sort-Object -CaseSensitive -Unique)
    $appIds = @($perPod | ForEach-Object { $_.appId } | Sort-Object -CaseSensitive -Unique)
    $schedulerAddresses = @($perPod | ForEach-Object { $_.schedulerConnectedAddresses } | Sort-Object -CaseSensitive -Unique)
    $actorTypes = @($perPod | ForEach-Object { $_.actorTypes } | Sort-Object -CaseSensitive -Unique)
    $enabledFeatures = @($perPod | ForEach-Object { $_.enabledFeatures } | Sort-Object -CaseSensitive -Unique)
    $componentAlphaValues = @($perPod | ForEach-Object { $_.alphaOptIn.componentIsAlpha } | Sort-Object -Unique)
    $allowAlphaValues = @($perPod | ForEach-Object { $_.alphaOptIn.allowAlphaComponent } | Sort-Object -Unique)
    $schedulerIdentities = @($perPod | ForEach-Object {
        Get-CollectionIdentity -Values @($_.schedulerConnectedAddresses)
    } | Sort-Object -CaseSensitive -Unique)
    $actorIdentities = @($perPod | ForEach-Object {
        Get-CollectionIdentity -Values @($_.actorTypes)
    } | Sort-Object -CaseSensitive -Unique)
    $featureIdentities = @($perPod | ForEach-Object {
        Get-CollectionIdentity -Values @($_.enabledFeatures)
    } | Sort-Object -CaseSensitive -Unique)

    if ($runtimeVersions.Count -ne 1 -or $sidecarImageDigests.Count -ne 1 -or $appIds.Count -ne 1 -or
        $componentAlphaValues.Count -ne 1 -or $allowAlphaValues.Count -ne 1 -or
        $schedulerIdentities.Count -ne 1 -or $actorIdentities.Count -ne 1 -or $featureIdentities.Count -ne 1) {
        throw 'running-target-identity-drift'
    }

    $observations = [ordered]@{
        pods = @($perPod)
        runtimeVersions = $runtimeVersions
        sidecarImageIds = $sidecarImageIds
        sidecarImageDigests = $sidecarImageDigests
        appIds = $appIds
        schedulerConnectedAddresses = $schedulerAddresses
        actorTypes = $actorTypes
        enabledFeatures = $enabledFeatures
        alphaOptIn = [ordered]@{
            componentIsAlpha = $componentAlphaValues[0]
            allowAlphaComponent = $allowAlphaValues[0]
        }
    }
    $producerStatus = 'observed'
}
catch {
    $safeFailure = [string]$_.Exception.Message
    if ($safeFailure -notmatch '^[a-z0-9:-]+$') {
        $safeFailure = 'producer-execution-failed'
    }
    $blockers.Add($safeFailure)
}

$packet = [ordered]@{
    schemaVersion = 'hexalith.access-telemetry.c1.evidence/v1'
    gate = $Gate
    profileId = $ProfileId
    capturedAtUtc = $capturedAtUtc
    context = $context
    namespace = $namespace
    targetSelector = $targetSelector
    producerStatus = $producerStatus
    gateStatus = 'not-evaluated'
    productionGatePassed = $false
    productionLifecycleWrites = 'not-evaluated'
    observations = $observations
    blockers = @($blockers)
    sources = @($script:sourceLedger)
    commands = @($script:commandLedger)
}

$packetPath = Write-ImmutablePacket -Packet $packet -Directory $EvidenceDirectory
Write-Output $packetPath
if ($producerStatus -ne 'observed') {
    exit 1
}
