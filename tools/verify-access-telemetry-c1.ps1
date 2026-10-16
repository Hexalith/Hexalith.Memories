[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('C1.15', 'C1.16')]
    [string]$Gate,

    [Parameter(Mandatory)]
    [ValidateSet('PG-ONPREM-1', 'PG-ONPREM-2')]
    [string]$ProfileId,

    [Parameter(Mandatory)]
    [string]$EvidenceDirectory,

    [switch]$AllowHistoricalProfileCapture,

    [string]$QualificationSessionId,

    [ValidateRange(1, 300)]
    [int]$CommandTimeoutSeconds = 30
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# Exact literals apply to successor and historical dispatch. The legacy
# C1.15/PG1 ValidateSet behavior remains case-insensitive.
if ($ProfileId -ieq 'PG-ONPREM-2') {
    if ($Gate -cnotin @('C1.15', 'C1.16') -or $ProfileId -cne 'PG-ONPREM-2' -or $AllowHistoricalProfileCapture) {
        throw 'unsupported-successor-gate-or-historical-opt-in'
    }
}
elseif ($Gate -ieq 'C1.16') {
    if ($Gate -cne 'C1.16' -or $ProfileId -cne 'PG-ONPREM-1') {
        throw 'unsupported-historical-gate-or-profile'
    }
    if (-not $AllowHistoricalProfileCapture) {
        throw 'historical-profile-capture-opt-in-required'
    }
}

$namespace = 'hexalith-memories'
$expectedContext = $(if ($ProfileId -ceq 'PG-ONPREM-2') { 'owner@memories-qualification-27-4' } else { 'jpiquot@local' })
$targetSelector = 'app.kubernetes.io/name=memories-access-telemetry'
$expectedAppId = 'memories-access-telemetry'
$expectedActorType = 'AccessTelemetryLifecycleActor'
$podIdentityOutput = "jsonpath-as-json={range .items[*]}{['metadata','status']}{end}"
$script:commandLedger = [System.Collections.Generic.List[object]]::new()
$script:sourceLedger = [System.Collections.Generic.List[object]]::new()
$script:retainedStreams = [System.Collections.Generic.List[object]]::new()
$script:retainedStreamBytes = 0L
$script:pg2RuntimeCapture = ($Gate -ceq 'C1.15' -and $ProfileId -ceq 'PG-ONPREM-2')
if ($script:pg2RuntimeCapture) {
    if ($QualificationSessionId -cnotmatch '\A[A-Za-z0-9][A-Za-z0-9._-]{0,127}\z') {
        throw 'qualification-session-required-or-invalid'
    }
}
elseif ($PSBoundParameters.ContainsKey('QualificationSessionId')) {
    throw 'qualification-session-unsupported-mode'
}


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
    if ($script:pg2RuntimeCapture -and ($Text -match '(?i)\b(?:authorization|dapr[-_]?api[-_]?token|password|passwd|token|secret|client[-_]?secret|access[-_]?token|connection[-_]?string)\s*[:=]\s*["'']?[^\s"'']+' -or
        $Text -match '(?i)postgres(?:ql)?://[^\s:/]+:[^\s@]+@')) { throw 'secret-shaped-output' }
    if ($Text -match '(?i)"(?:authorization|dapr[-_]?api[-_]?token)"\s*:\s*"(?:[^"\\]|\\.)+"') {
        throw 'secret-shaped-output'
    }
}

function Assert-SecretSafeMetadata {
    param([AllowNull()][AllowEmptyString()][AllowEmptyCollection()][object]$Value)

    if ($null -eq $Value) {
        return
    }
    if ($Value -is [string]) {
        Assert-SecretSafeOutput $Value
        return
    }
    if ($Value -is [System.Array]) {
        foreach ($item in $Value) {
            Assert-SecretSafeMetadata -Value $item
        }
        return
    }
    if ($Value -is [System.Management.Automation.PSCustomObject]) {
        foreach ($property in $Value.PSObject.Properties) {
            Assert-SecretSafeOutput $property.Name
            if ($property.Value -is [string]) {
                # Preserve the existing nonempty credential-property rule after names decode.
                $stringProperty = [ordered]@{}
                $stringProperty[$property.Name] = $property.Value
                Assert-SecretSafeOutput (ConvertTo-Json -InputObject $stringProperty -Compress -Depth 2)
            }
            Assert-SecretSafeMetadata -Value $property.Value
        }
    }
}

function Assert-UniqueMetadataJsonProperties {
    param([Parameter(Mandatory)][System.Text.Json.JsonElement]$Element)

    if ($Element.ValueKind -eq [System.Text.Json.JsonValueKind]::Object) {
        $names = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($property in $Element.EnumerateObject()) {
            $name = $property.Name
            if ($null -eq $name -or -not $names.Add($name)) {
                throw 'malformed-metadata-json'
            }
            Assert-UniqueMetadataJsonProperties -Element $property.Value
        }
    }
    elseif ($Element.ValueKind -eq [System.Text.Json.JsonValueKind]::Array) {
        foreach ($item in $Element.EnumerateArray()) {
            Assert-UniqueMetadataJsonProperties -Element $item
        }
    }
}

function Assert-MetadataJsonShape {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Json)

    $options = [System.Text.Json.JsonDocumentOptions]::new()
    $options.MaxDepth = 30
    try {
        $document = [System.Text.Json.JsonDocument]::Parse($Json, $options)
    }
    catch {
        throw 'malformed-metadata-json'
    }
    try {
        if ($document.RootElement.ValueKind -ne [System.Text.Json.JsonValueKind]::Object) {
            throw 'malformed-metadata-json'
        }
        # Inspect decoded names before ConvertFrom-Json can overwrite duplicates or unwrap arrays.
        Assert-UniqueMetadataJsonProperties -Element $document.RootElement
    }
    finally {
        $document.Dispose()
    }
}

function Invoke-KubectlObservation {
    param(
        [Parameter(Mandatory)][string]$Purpose,
        [Parameter(Mandatory)][string[]]$Arguments,
        [switch]$SkipSourceHash
    )

    $commandIdentity = 'kubectl ' + ($Arguments -join [char]0x1f)
    $commandEntry = [ordered]@{
        purpose = $Purpose
        sha256 = Get-TextSha256 $commandIdentity
    }
    if ($script:pg2RuntimeCapture) {
        $commandEntry.executable = 'kubectl'
        $commandEntry.arguments = @($Arguments)
        $commandEntry.argumentsSha256 = Get-C1JsonSha256 @($Arguments)
        $commandEntry.startedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
        $commandEntry.finishedAtUtc = $null
        $commandEntry.exitCode = $null
        $commandEntry.stdoutSha256 = $null
        $commandEntry.stderrSha256 = $null
        $commandEntry.streamSafety = 'not-validated'
        $commandEntry.resultCount = 0
        $commandEntry.failureCount = 1
        $commandEntry.skipCount = 0
    }
    $script:commandLedger.Add($commandEntry)

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

        if ($script:pg2RuntimeCapture) {
            $strictUtf8 = [System.Text.UTF8Encoding]::new($false, $true)
            try {
                $stdoutBytes = $stdoutCapture.ToArray()
                $stderrBytes = $stderrCapture.ToArray()
                $stdout = $strictUtf8.GetString($stdoutBytes)
                $stderr = $strictUtf8.GetString($stderrBytes)
            }
            catch { throw "kubectl-$Purpose-invalid-utf8" }
        }
        else {
            $stdout = [System.Text.Encoding]::UTF8.GetString($stdoutCapture.ToArray())
            $stderr = [System.Text.Encoding]::UTF8.GetString($stderrCapture.ToArray())
        }
        $exitCode = $process.ExitCode
        if ($stdoutExceeded -or $stderrExceeded) {
            throw "kubectl-$Purpose-output-too-large"
        }
    }
    finally {
        if ($script:pg2RuntimeCapture) {
            $commandEntry.finishedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
            try {
                if ($process.WaitForExit(1000)) { $commandEntry.exitCode = $process.ExitCode }
            }
            catch { }
        }
        $process.Dispose()
        $stdoutCapture.Dispose()
        $stderrCapture.Dispose()
    }

    Assert-SecretSafeOutput $stdout
    Assert-SecretSafeOutput $stderr
    if ($Gate -ceq 'C1.16' -or $script:pg2RuntimeCapture) {
        Assert-C1DecodedOutputSafety $stdout
        Assert-C1DecodedOutputSafety $stderr
    }
    if ($script:pg2RuntimeCapture) {
        # Persistence uses the pure inspector's broader raw/decoded credential
        # spellings, including named pairs in diagnostic JSON fragments.
        foreach ($name in @('stdout', 'stderr')) {
            try { [C1PrivateStreamWriter]::AssertSafe($(if ($name -ceq 'stdout') { $stdout } else { $stderr })) }
            catch {
                $failure = [string]$_.Exception.InnerException.Message
                if ($failure -ceq 'secret-shaped-output') { throw $failure }
                if ($failure -cmatch '\Ac115-stream-(?:invalid-json|json-depth-exceeded|duplicate-json-field|integer-lexeme-too-long|nonfinite-json-number|unpaired-unicode-surrogate)\z') {
                    # Preserve existing purpose-specific malformed stdout refusals.
                    if ($name -ceq 'stdout' -and $Purpose.StartsWith('metadata:')) { throw 'malformed-metadata-json' }
                    if ($name -ceq 'stdout' -and $Purpose -cin @('lifecycle-pods', 'lifecycle-pods-recheck')) { throw 'malformed-pod-list-json' }
                    throw $failure
                }
                throw 'retained-stream-safety-failed'
            }
        }
        $commandEntry.stdoutSha256 = Get-TextSha256 $stdout
        $commandEntry.stderrSha256 = Get-TextSha256 $stderr
        $commandEntry.streamSafety = 'validated'
        $script:retainedStreamBytes += $stdoutBytes.Length + $stderrBytes.Length
        if ($script:retainedStreamBytes -gt 32MB) { throw 'retained-stream-aggregate-byte-budget-exceeded' }
        # The validated originals survive MemoryStream disposal; parsing still trims only text.
        $script:retainedStreams.Add([ordered]@{ purpose = $Purpose; stdout = $stdoutBytes; stderr = $stderrBytes })
        if ($exitCode -eq 0 -and -not [string]::IsNullOrWhiteSpace($stdout)) {
            $commandEntry.resultCount = 1
            $commandEntry.failureCount = 0
        }
    }
    if (-not $SkipSourceHash) {
        Add-SourceHash "kubectl:${Purpose}:stdout" $stdout
        Add-SourceHash "kubectl:${Purpose}:stderr" $stderr
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

function ConvertFrom-PodIdentityProjection {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Json)

    if ($null -eq ('C1PodIdentityJsonSequence' -as [type])) {
        # Parse the bounded kubectl sequence without requiring newer multiple-value JSON APIs.
        Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

/// <summary>Reads the bounded per-Pod JSON frames emitted by kubectl range.</summary>
public static class C1PodIdentityJsonSequence
{
    /// <summary>Parses every JSON value strictly and retains its original JSON text.</summary>
    /// <param name="json">The already bounded selected-field response.</param>
    /// <returns>Detached frame elements whose documents have been disposed.</returns>
    public static JsonElement[] Parse(string json)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        var frames = new List<JsonElement>();
        var options = new JsonReaderOptions { MaxDepth = 30 };
        int offset = 0;
        while (offset < bytes.Length)
        {
            var reader = new Utf8JsonReader(bytes.AsSpan(offset), options);
            if (!reader.Read())
            {
                break;
            }
            using (JsonDocument document = JsonDocument.ParseValue(ref reader))
            {
                frames.Add(document.RootElement.Clone());
            }
            offset += checked((int)reader.BytesConsumed);
        }
        return frames.ToArray();
    }
}
'@
    }

    $items = [System.Collections.Generic.List[object]]::new()
    try {
        $frames = [C1PodIdentityJsonSequence]::Parse($Json)
        foreach ($frame in $frames) {
            if ($frame.ValueKind -ne [System.Text.Json.JsonValueKind]::Array -or
                $frame.GetArrayLength() -ne 2) {
                throw 'malformed-pod-list-json'
            }
            Assert-UniqueMetadataJsonProperties -Element $frame
            $projection = ConvertFrom-Json -InputObject $frame.GetRawText() -Depth 30 -NoEnumerate
            $metadata = $projection[0]
            $status = $projection[1]
            if ($metadata -isnot [System.Management.Automation.PSCustomObject] -or
                $status -isnot [System.Management.Automation.PSCustomObject] -or
                ($null -eq $metadata.PSObject.Properties['name'] -and
                    $null -eq $metadata.PSObject.Properties['uid']) -or
                $null -eq $status.PSObject.Properties['phase']) {
                throw 'malformed-pod-list-json'
            }
            # Preserve raw types and fully scan decoded selected fields before any provenance.
            Assert-SecretSafeMetadata -Value $projection
            $items.Add([pscustomobject]@{
                metadata = $metadata
                status = $status
            })
        }
    }
    catch {
        if ($_.Exception.Message -eq 'secret-shaped-output') {
            throw 'secret-shaped-output'
        }
        throw 'malformed-pod-list-json'
    }
    return [pscustomobject]@{ items = $items.ToArray() }
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

    if ($script:pg2RuntimeCapture) {
        $Pattern = '\A(?:' + $Pattern.Substring(1, $Pattern.Length - 2) + ')\z'
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

function Get-C1PG2PodIdentities {
    param([object[]]$Items)

    $identities = [System.Collections.Generic.List[object]]::new()
    foreach ($pod in $Items) {
        $phase = Get-C1RequiredString $pod.status 'phase' '\A[A-Za-z]+\z' 'running-pod-phase-invalid'
        if ($phase -cne 'Running') { continue }
        $name = Get-C1RequiredString $pod.metadata 'name' '\A[a-z0-9][a-z0-9.-]{0,252}\z' 'running-pod-identity-invalid'
        $uid = Get-C1RequiredString $pod.metadata 'uid' '\A[A-Za-z0-9][A-Za-z0-9-]{0,127}\z' 'running-pod-identity-invalid'
        $label = Get-C1RequiredString $pod.metadata.labels 'app.kubernetes.io/name' '\A[a-z0-9][a-z0-9.-]{0,252}\z' 'running-pod-label-mismatch'
        if ($label -cne $expectedAppId) { throw 'running-pod-label-mismatch' }
        $deleting = $null -ne $pod.metadata.PSObject.Properties['deletionTimestamp'] -and $null -ne $pod.metadata.deletionTimestamp
        if ($deleting -or $pod.status.conditions -isnot [System.Array] -or
            $pod.status.containerStatuses -isnot [System.Array]) { throw 'running-pod-not-stable' }
        $readyConditions = [System.Collections.Generic.List[object]]::new()
        foreach ($condition in $pod.status.conditions) {
            $conditionType = Get-C1RequiredString $condition 'type' '\A[A-Za-z]+\z' 'running-pod-not-stable'
            if ($conditionType -ceq 'Ready') { $readyConditions.Add($condition) }
        }
        if ($readyConditions.Count -ne 1) { throw 'running-pod-not-stable' }
        $readyState = Get-C1RequiredString $readyConditions[0] 'status' '\A(?:True|False|Unknown)\z' 'running-pod-not-stable'
        if ($readyState -cne 'True') { throw 'running-pod-not-stable' }
        $containers = [ordered]@{}
        foreach ($container in $pod.status.containerStatuses) {
            $containerName = Get-C1RequiredString $container 'name' '\A[a-z0-9][a-z0-9-]{0,62}\z' 'running-pod-containers-not-ready'
            if ($containerName -cnotin @('lifecycle', 'daprd')) { continue }
            if ($containers.Contains($containerName) -or $container.ready -isnot [bool] -or -not $container.ready) {
                throw 'running-pod-containers-not-ready'
            }
            $image = Get-C1RequiredString $container 'imageID' '\A(?:[A-Za-z0-9._:/@-]+)?sha256:[0-9a-f]{64}\z' 'runtime-image-mismatch'
            $containers[$containerName] = [ordered]@{ name = $containerName; ready = $container.ready; imageId = $image }
        }
        if ($containers.Count -ne 2) { throw 'running-pod-containers-not-ready' }
        $identities.Add([ordered]@{
            pod = $name; podUid = $uid; appLabel = $label; deleting = $deleting; phase = $phase
            readyCondition = [ordered]@{ type = 'Ready'; status = $readyState }
            containers = @($containers['lifecycle'], $containers['daprd'])
        })
    }
    return @($identities | Sort-Object { $_.pod } -CaseSensitive)
}

function Get-CollectionIdentity {
    param([AllowEmptyCollection()][object[]]$Values)

    return (@($Values) -join [char]0x1f)
}

function Initialize-C1RetentionCustody {
    param([Parameter(Mandatory)][string]$Directory)

    if ($null -eq ('C1PrivateStreamWriter' -as [type])) {
        Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

/// <summary>Linux descriptor-relative private custody for exact C1.15 streams.</summary>
public sealed class C1PrivateStreamWriter : IDisposable
{
    [StructLayout(LayoutKind.Explicit, Size = 256)]
    private struct Metadata
    {
        [FieldOffset(0)] public uint Mask;
        [FieldOffset(16)] public uint Links;
        [FieldOffset(20)] public uint Uid;
        [FieldOffset(24)] public uint Gid;
        [FieldOffset(28)] public ushort Mode;
        [FieldOffset(32)] public ulong Inode;
        [FieldOffset(40)] public ulong Size;
        [FieldOffset(96)] public long Ctime;
        [FieldOffset(104)] public uint CtimeNs;
        [FieldOffset(112)] public long Mtime;
        [FieldOffset(120)] public uint MtimeNs;
        [FieldOffset(136)] public uint DeviceMajor;
        [FieldOffset(140)] public uint DeviceMinor;
    }
    private sealed class DirectoryEntry
    {
        public int Descriptor;
        public string Name;
        public Metadata Before;
    }
    private sealed class Artifact
    {
        public int Parent;
        public string Name;
        public FileStream Stream;
        public SafeFileHandle Handle;
        public byte[] Bytes;
        public Metadata Before;
    }
    [DllImport("libc", SetLastError = true)] private static extern int open(string path, int flags);
    [DllImport("libc", SetLastError = true)] private static extern int openat(int directory, string path, int flags, uint mode);
    [DllImport("libc", SetLastError = true)] private static extern int mkdirat(int directory, string path, uint mode);
    [DllImport("libc", SetLastError = true)] private static extern int unlinkat(int directory, string path, int flags);
    [DllImport("libc", SetLastError = true)] private static extern int close(int descriptor);
    [DllImport("libc", SetLastError = true)] private static extern IntPtr fdopendir(int descriptor);
    [DllImport("libc", SetLastError = true)] private static extern IntPtr readdir(IntPtr directory);
    [DllImport("libc", SetLastError = true)] private static extern int closedir(IntPtr directory);
    [DllImport("libc")] private static extern IntPtr __errno_location();
    [DllImport("libc", SetLastError = true)] private static extern int fchmod(int descriptor, uint mode);
    [DllImport("libc", SetLastError = true)] private static extern uint geteuid();
    [DllImport("libc", SetLastError = true)] private static extern int statx(int directory, string path, int flags, uint mask, out Metadata metadata);
    private const int DirectoryFlags = 0x10000 | 0x20000 | 0x80000;
    private readonly List<DirectoryEntry> _directories = new List<DirectoryEntry>();
    private readonly List<Artifact> _artifacts = new List<Artifact>();
    private readonly uint _uid;
    private readonly int _rootDepth;
    private int _sidecar = -1;
    private Artifact _published;
    private int _publicationRoot = -1;
    private readonly int _rootCharacters;

    private static readonly System.Text.RegularExpressions.Regex Credential = new System.Text.RegularExpressions.Regex(
        @"\A(?:authorization|dapr[-_]?api[-_]?token|password|passwd|token|secret|credential|api[-_]?key|client[-_]?secret|(?:access|refresh|id)[-_]?token|connection[-_]?string)\z",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static readonly System.Text.RegularExpressions.Regex SecretText = new System.Text.RegularExpressions.Regex(
        "(?i)(-----BEGIN [A-Z ]*PRIVATE KEY-----|\\bbearer\\s+\\S+|(?:password|passwd|api[_-]?key|client[_-]?secret|authorization|(?:access|refresh|id)[_-]?token)\\s*[:=]\\s*\\S+|C1[_-]?SECRET[_-]?CANARY|SECRET[_-]?CANARY|\\b(?:hvs|hvb|hvr)\\.[A-Za-z0-9_-]{8,}|\\b(?:token|secret|credential|dapr[-_]?api[-_]?token|connection[-_]?string)\\s*[:=]\\s*[\\\"']?[^\\s\\\"']+|postgres(?:ql)?://[^\\s:/]+:[^\\s@]+@)",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static readonly System.Text.RegularExpressions.Regex RawCredential = new System.Text.RegularExpressions.Regex(
        "(?i)\"(?:authorization|dapr[-_]?api[-_]?token|password|passwd|token|secret|credential|api[-_]?key|client[-_]?secret|(?:access|refresh|id)[-_]?token|connection[-_]?string)\"\\s*:\\s*(?:\"(?:[^\"\\\\]|\\\\.)+\"|\\[\\s*[^\\s\\]]|\\{\\s*[^\\s}]|(?:true|false)(?=\\s|[,}\\]]|$)|-?(?:0|[1-9][0-9]*)(?:\\.[0-9]+)?(?:[eE][+-]?[0-9]+)?(?=\\s|[,}\\]]|$))",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static readonly System.Text.RegularExpressions.Regex StringTokens = new System.Text.RegularExpressions.Regex("\"(?:[^\"\\\\]|\\\\.)*\"");
    private static readonly System.Text.RegularExpressions.Regex JsonStarts = new System.Text.RegularExpressions.Regex(@"[\[{]\s*(?:[""\[\]{}0-9-]|true\b|false\b|null\b|NaN\b|Infinity\b)");
    private static string SafetyWhitespace(string text)
    {
        return text.Replace('\u001c', ' ').Replace('\u001d', ' ').Replace('\u001e', ' ').Replace('\u001f', ' ');
    }
    private static void SafePatterns(string text)
    {
        // Python's regex whitespace includes these four separators. Normalize
        // only the safety matching view; byte hashing and parsing keep originals.
        text = SafetyWhitespace(text);
        if (SecretText.IsMatch(text) || RawCredential.IsMatch(text)) throw new IOException("secret-shaped-output");
    }
    private static bool Nonempty(System.Text.Json.JsonElement element)
    {
        switch (element.ValueKind)
        {
            case System.Text.Json.JsonValueKind.Null: return false;
            case System.Text.Json.JsonValueKind.String: return element.GetString().Length != 0;
            case System.Text.Json.JsonValueKind.Array: return element.GetArrayLength() != 0;
            case System.Text.Json.JsonValueKind.Object: return element.EnumerateObject().MoveNext();
            default: return true;
        }
    }
    private static void SafeDepth(string text)
    {
        int depth = 0;
        bool quoted = false, escaped = false;
        foreach (char character in text)
        {
            if (quoted)
            {
                if (escaped) escaped = false;
                else if (character == '\\') escaped = true;
                else if (character == '"') quoted = false;
            }
            else if (character == '"') quoted = true;
            else if (character == '{' || character == '[')
            {
                if (++depth > 30) throw new IOException("c115-stream-json-depth-exceeded");
            }
            else if (character == '}' || character == ']') depth = Math.Max(0, depth - 1);
        }
    }
    private static void SafeTree(System.Text.Json.JsonElement element, int nesting, int depth = 0)
    {
        if ((element.ValueKind == System.Text.Json.JsonValueKind.Object || element.ValueKind == System.Text.Json.JsonValueKind.Array) && depth >= 30)
            throw new IOException("c115-stream-json-depth-exceeded");
        if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            bool namedCredential = false, nonemptyValue = false;
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (System.Text.Json.JsonProperty property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new IOException("c115-stream-duplicate-json-field");
                if (property.Name.Equals("name", StringComparison.OrdinalIgnoreCase) &&
                    property.Value.ValueKind == System.Text.Json.JsonValueKind.String && Credential.IsMatch(property.Value.GetString()))
                    namedCredential = true;
                if (property.Name.Equals("value", StringComparison.OrdinalIgnoreCase) &&
                    property.Value.ValueKind != System.Text.Json.JsonValueKind.Null &&
                    (property.Value.ValueKind != System.Text.Json.JsonValueKind.String || property.Value.GetString().Length != 0))
                    nonemptyValue = true;
                if (Credential.IsMatch(property.Name) && Nonempty(property.Value)) throw new IOException("secret-shaped-output");
                SafeScan(property.Name, nesting);
                SafeTree(property.Value, nesting, depth + 1);
            }
            if (namedCredential && nonemptyValue) throw new IOException("secret-shaped-output");
        }
        else if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
            foreach (System.Text.Json.JsonElement child in element.EnumerateArray()) SafeTree(child, nesting, depth + 1);
        else if (element.ValueKind == System.Text.Json.JsonValueKind.String) SafeScan(element.GetString(), nesting);
        else if (element.ValueKind == System.Text.Json.JsonValueKind.Number)
        {
            string lexeme = element.GetRawText();
            if (lexeme.IndexOfAny(new[] { '.', 'e', 'E' }) < 0)
            {
                if (lexeme.Length > 380) throw new IOException("c115-stream-integer-lexeme-too-long");
            }
            else
            {
                double number;
                if (!element.TryGetDouble(out number) || !double.IsFinite(number)) throw new IOException("c115-stream-nonfinite-json-number");
            }
        }
    }
    private static void SafeConstants(string text, int offset)
    {
        bool quoted = false, escaped = false;
        int depth = 0;
        for (int index = offset; index < text.Length; ++index)
        {
            char character = text[index];
            if (quoted)
            {
                if (escaped) escaped = false;
                else if (character == '\\') escaped = true;
                else if (character == '"') quoted = false;
                continue;
            }
            if (character == '"') { quoted = true; continue; }
            if (character == '{' || character == '[') ++depth;
            else if (character == '}' || character == ']') { if (--depth == 0) break; }
            if ((index == offset || " \t\r\n[:,".Contains(text[index - 1])) &&
                (text.AsSpan(index).StartsWith("NaN", StringComparison.Ordinal) ||
                 text.AsSpan(index).StartsWith("Infinity", StringComparison.Ordinal) ||
                 text.AsSpan(index).StartsWith("-Infinity", StringComparison.Ordinal)))
                throw new IOException("c115-stream-nonfinite-json-number");
        }
    }
    private static System.Text.Json.JsonDocument SafeDecode(string text, byte[] bytes, int offset,
        ref int previous, ref int byteOffset, out int end)
    {
        byteOffset += System.Text.Encoding.UTF8.GetByteCount(text.AsSpan(previous, offset - previous));
        previous = offset;
        var reader = new System.Text.Json.Utf8JsonReader(bytes.AsSpan(byteOffset), new System.Text.Json.JsonReaderOptions { MaxDepth = 30 });
        System.Text.Json.JsonDocument document;
        SafeConstants(text, offset);
        try { document = System.Text.Json.JsonDocument.ParseValue(ref reader); }
        catch (System.Text.Json.JsonException) { throw new IOException("c115-stream-invalid-json"); }
        end = offset + System.Text.Encoding.UTF8.GetCharCount(bytes.AsSpan(byteOffset, checked((int)reader.BytesConsumed)));
        return document;
    }
    private static bool LiteralOpening(string text, System.Text.RegularExpressions.Match candidate, System.Text.RegularExpressions.Match token)
    {
        int marker = candidate.Index + 1, tokenEnd = token.Index + token.Length;
        while (marker < tokenEnd - 1 && " \t\r\n".Contains(text[marker])) ++marker;
        if (marker != tokenEnd - 1) return false;
        var key = StringTokens.Match(text, marker);
        if (!key.Success || key.Index != marker) return true;
        int finish = key.Index + key.Length;
        while (finish < text.Length && " \t\r\n".Contains(text[finish])) ++finish;
        return finish < text.Length && !(text[candidate.Index] == '{' ? ":}" : ",]:}").Contains(text[finish]);
    }
    private static readonly System.Text.RegularExpressions.Regex ScalarNumber = new System.Text.RegularExpressions.Regex(
        @"\A(?:NaN|-?Infinity|-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?)\z");
    private static void SafeScan(string text, int nesting)
    {
        if (nesting > 30) throw new IOException("c115-stream-json-depth-exceeded");
        for (int index = 0; index < text.Length; ++index)
        {
            if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1])) ++index;
            else if (char.IsSurrogate(text[index])) throw new IOException("c115-stream-unpaired-unicode-surrogate");
        }
        SafeDepth(text);
        SafePatterns(text);
        SafePatterns(System.Text.RegularExpressions.Regex.Replace(text, @"\\u([0-9a-fA-F]{4})",
            match => ((char)Convert.ToInt32(match.Groups[1].Value, 16)).ToString()));
        string leading = text.Trim();
        if (nesting == 0 && (leading == "{" || leading == "[")) throw new IOException("c115-stream-invalid-json");
        if (nesting == 0 && ScalarNumber.IsMatch(leading))
        {
            if (leading == "NaN" || leading.EndsWith("Infinity", StringComparison.Ordinal)) throw new IOException("c115-stream-nonfinite-json-number");
            using (var document = System.Text.Json.JsonDocument.Parse(leading)) SafeTree(document.RootElement, nesting + 1);
        }
        var tokens = StringTokens.Matches(text);
        var candidates = JsonStarts.Matches(SafetyWhitespace(text));
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
        int position = 0, tokenIndex = 0, candidateIndex = 0, previous = 0, byteOffset = 0, end;
        while (position < text.Length)
        {
            while (tokenIndex < tokens.Count && tokens[tokenIndex].Index < position) ++tokenIndex;
            while (candidateIndex < candidates.Count && candidates[candidateIndex].Index < position) ++candidateIndex;
            var token = tokenIndex < tokens.Count ? tokens[tokenIndex] : null;
            var candidate = candidateIndex < candidates.Count ? candidates[candidateIndex] : null;
            if (candidate != null && (token == null || candidate.Index < token.Index))
            {
                using (var document = SafeDecode(text, bytes, candidate.Index, ref previous, ref byteOffset, out end)) SafeTree(document.RootElement, nesting + 1);
                position = end;
            }
            else if (token != null)
            {
                int tokenEnd = token.Index + token.Length;
                while (candidateIndex < candidates.Count && candidates[candidateIndex].Index < tokenEnd)
                {
                    candidate = candidates[candidateIndex];
                    if (!LiteralOpening(text, candidate, token))
                    {
                        using (var document = SafeDecode(text, bytes, candidate.Index, ref previous, ref byteOffset, out end)) SafeTree(document.RootElement, nesting + 1);
                        while (candidateIndex < candidates.Count && candidates[candidateIndex].Index < end) ++candidateIndex;
                    }
                    else ++candidateIndex;
                }
                System.Text.Json.JsonDocument diagnostic = null;
                try { diagnostic = System.Text.Json.JsonDocument.Parse(token.Value); }
                catch (System.Text.Json.JsonException) { }
                if (diagnostic != null) using (diagnostic) SafeTree(diagnostic.RootElement, nesting + 1);
                position = tokenEnd;
            }
            else break;
        }
    }
    /// <summary>Validate the pure stream inspector's bounded raw/decoded safety rules before retention.</summary>
    /// <param name="text">Strictly decoded original stream text.</param>
    public static void AssertSafe(string text) { SafeScan(text, 0); }
    private static void Require(bool condition)
    {
        if (!condition) throw new IOException("retained-stream-custody-unavailable");
    }
    private static Metadata Named(int directory, string name)
    {
        Metadata value;
        Require(statx(directory, name, 0x100 | 0x800 | 0x1000, 0x7ff, out value) == 0 && (value.Mask & 0x7ff) == 0x7ff);
        return value;
    }
    private static Metadata Held(int descriptor) { return Named(descriptor, ""); }
    private void Private(Metadata value, bool directory)
    {
        Require(value.Uid == _uid && (value.Mode & 0x3f) == 0 &&
            (value.Mode & 0xf000) == (directory ? 0x4000 : 0x8000) && (directory || value.Links == 1));
    }
    private static bool Identity(Metadata first, Metadata second, bool directory)
    {
        return first.DeviceMajor == second.DeviceMajor && first.DeviceMinor == second.DeviceMinor &&
            first.Inode == second.Inode && first.Mode == second.Mode && first.Uid == second.Uid &&
            first.Gid == second.Gid && (directory || first.Links == second.Links);
    }
    private static bool SameFile(Metadata first, Metadata second)
    {
        return Identity(first, second, false) && first.Size == second.Size &&
            first.Ctime == second.Ctime && first.CtimeNs == second.CtimeNs &&
            first.Mtime == second.Mtime && first.MtimeNs == second.MtimeNs;
    }
    private void CheckDirectories(bool frozen)
    {
        Require(geteuid() == _uid);
        for (int index = _directories.Count - 1; index >= 0; --index)
        {
            DirectoryEntry entry = _directories[index];
            Metadata named = index == 0 ? Named(-100, "/") : Named(_directories[index - 1].Descriptor, entry.Name);
            Metadata held = Held(entry.Descriptor);
            Require(Identity(entry.Before, named, true) && Identity(entry.Before, held, true));
            if (index >= _rootDepth)
            {
                Private(named, true);
                if (frozen) Require(entry.Before.Ctime == named.Ctime && entry.Before.CtimeNs == named.CtimeNs &&
                    entry.Before.Ctime == held.Ctime && entry.Before.CtimeNs == held.CtimeNs);
            }
        }
    }
    /// <summary>Hold a non-following chain and require an owner-only evidence root.</summary>
    /// <param name="root">Canonical absolute evidence root; missing directories are created privately.</param>
    public C1PrivateStreamWriter(string root)
    {
        Require(OperatingSystem.IsLinux() && root.StartsWith("/", StringComparison.Ordinal) && root != "/");
        foreach (var rune in root.EnumerateRunes()) ++_rootCharacters;
        // The fixed timestamp and GUID filename must fit the file reader's
        // canonical 512-character packet-path budget before target collection.
        Require(_rootCharacters + 1 + "c1.15-runtime-control-plane-identity-00000000T000000000Z-00000000000000000000000000000000.json".Length <= 512);
        string[] parts = root.Substring(1).Split('/');
        _rootDepth = parts.Length;
        _uid = geteuid();
        try
        {
            int descriptor = open("/", DirectoryFlags);
            Require(descriptor >= 0);
            var rootEntry = new DirectoryEntry { Descriptor = descriptor, Name = "/" };
            _directories.Add(rootEntry);
            rootEntry.Before = Held(descriptor);
            foreach (string part in parts)
            {
                Require(part.Length != 0 && part != "." && part != ".." && !part.Contains('\\'));
                descriptor = openat(_directories[_directories.Count - 1].Descriptor, part, DirectoryFlags, 0);
                if (descriptor < 0 && Marshal.GetLastPInvokeError() == 2)
                {
                    Require(mkdirat(_directories[_directories.Count - 1].Descriptor, part, 0x1c0) == 0);
                    descriptor = openat(_directories[_directories.Count - 1].Descriptor, part, DirectoryFlags, 0);
                }
                Require(descriptor >= 0);
                var entry = new DirectoryEntry { Descriptor = descriptor, Name = part };
                _directories.Add(entry);
                entry.Before = Held(descriptor);
            }
            Private(_directories[_rootDepth].Before, true);
            CheckDirectories(true);
        }
        catch { Dispose(); throw; }
    }
    private Artifact Write(int parent, string name, byte[] bytes)
    {
        Require(bytes != null && bytes.Length <= 1048576);
        CheckDirectories(false);
        int descriptor = openat(parent, name, 2 | 0x40 | 0x80 | 0x20000 | 0x800 | 0x80000, 0x180);
        Require(descriptor >= 0);
        var artifact = new Artifact { Parent = parent, Name = name, Bytes = bytes };
        artifact.Handle = new SafeFileHandle((IntPtr)descriptor, true);
        try { artifact.Stream = new FileStream(artifact.Handle, FileAccess.ReadWrite); }
        catch { artifact.Handle.Dispose(); throw; }
        _artifacts.Add(artifact);
        Private(Held(descriptor), false);
        artifact.Stream.Write(bytes, 0, bytes.Length);
        artifact.Stream.Flush(true);
        Require(fchmod(descriptor, 0x100) == 0);
        artifact.Before = Held(descriptor);
        Private(artifact.Before, false);
        Readback(artifact);
        CheckDirectories(false);
        return artifact;
    }
    private void Readback(Artifact artifact)
    {
        int descriptor = artifact.Stream.SafeFileHandle.DangerousGetHandle().ToInt32();
        Require(SameFile(artifact.Before, Named(artifact.Parent, artifact.Name)) && SameFile(artifact.Before, Held(descriptor)));
        artifact.Stream.Position = 0;
        byte[] buffer = new byte[8192];
        int offset = 0, count;
        while ((count = artifact.Stream.Read(buffer, 0, buffer.Length)) != 0)
        {
            Require(offset + count <= artifact.Bytes.Length);
            for (int index = 0; index < count; ++index) Require(buffer[index] == artifact.Bytes[offset + index]);
            offset += count;
        }
        Require(offset == artifact.Bytes.Length && SameFile(artifact.Before, Held(descriptor)) &&
            SameFile(artifact.Before, Named(artifact.Parent, artifact.Name)));
    }
    private void CheckEntries(int count)
    {
        var expected = new HashSet<string>(StringComparer.Ordinal) { "manifest.json" };
        for (int index = 0; index < count; ++index)
        {
            string stem = index.ToString("D4", System.Globalization.CultureInfo.InvariantCulture);
            expected.Add(stem + ".stdout"); expected.Add(stem + ".stderr");
        }
        int copy = openat(_sidecar, ".", DirectoryFlags, 0);
        Require(copy >= 0);
        IntPtr directory = fdopendir(copy);
        if (directory == IntPtr.Zero) { close(copy); Require(false); }
        try
        {
            while (true)
            {
                Marshal.WriteInt32(__errno_location(), 0);
                IntPtr entry = readdir(directory);
                if (entry == IntPtr.Zero) { Require(Marshal.ReadInt32(__errno_location()) == 0); break; }
                string name = Marshal.PtrToStringUTF8(IntPtr.Add(entry, 19));
                if (name == "." || name == "..") continue;
                Require(expected.Remove(name));
            }
            Require(expected.Count == 0);
        }
        finally { Require(closedir(directory) == 0); }
    }
    /// <summary>Complete and recheck the exact sidecar before creating the immutable packet.</summary>
    /// <param name="name">Fresh packet filename without path separators.</param>
    /// <param name="packet">Exact serialized packet bytes.</param>
    /// <param name="streams">Validated stdout/stderr occurrences in receipt order, or empty for blocked packets.</param>
    /// <param name="manifest">Exact closed manifest bytes, or null for blocked packets.</param>
    public void Publish(string name, byte[] packet, byte[][] streams, byte[] manifest)
    {
        Require(!name.Contains('/') && !name.Contains('\\') && name.EndsWith(".json", StringComparison.Ordinal));
        int nameCharacters = 0;
        foreach (var rune in name.EnumerateRunes()) ++nameCharacters;
        Require(_rootCharacters + 1 + nameCharacters <= 512);
        long total = packet.Length;
        foreach (byte[] raw in streams) { Require(raw.Length <= 1048576); total += raw.Length; }
        Require(packet.Length <= 1048576 && total <= 33554432 && (manifest == null || manifest.Length <= 1048576));
        CheckDirectories(true);
        int root = _directories[_rootDepth].Descriptor;
        Artifact published = null;
        try
        {
            if (manifest != null)
            {
                Require(streams.Length % 2 == 0 && streams.Length <= 20000);
                string sidecarName = name + ".streams";
                Require(mkdirat(root, sidecarName, 0x1c0) == 0);
                _sidecar = openat(root, sidecarName, DirectoryFlags, 0);
                Require(_sidecar >= 0);
                var sidecarEntry = new DirectoryEntry { Descriptor = _sidecar, Name = sidecarName };
                _directories.Add(sidecarEntry);
                sidecarEntry.Before = Held(_sidecar);
                Private(Held(_sidecar), true);
                for (int index = 0; index < streams.Length; ++index)
                    Write(_sidecar, (index / 2).ToString("D4", System.Globalization.CultureInfo.InvariantCulture) +
                        (index % 2 == 0 ? ".stdout" : ".stderr"), streams[index]);
                Write(_sidecar, "manifest.json", manifest);
                CheckEntries(streams.Length / 2);
                foreach (Artifact artifact in _artifacts) Readback(artifact);
                CheckDirectories(false);
            }
            published = Write(root, name, packet);
            _published = published;
            _publicationRoot = root;
            CheckDirectories(false);
            if (manifest != null) CheckEntries(streams.Length / 2);
            // Freeze private directory change metadata after our last intentional creation.
            foreach (DirectoryEntry entry in _directories) entry.Before = Held(entry.Descriptor);
            foreach (Artifact artifact in _artifacts) Readback(artifact);
            if (manifest != null) CheckEntries(streams.Length / 2);
            CheckDirectories(true);
            foreach (Artifact artifact in _artifacts)
                Require(SameFile(artifact.Before, Named(artifact.Parent, artifact.Name)) &&
                    SameFile(artifact.Before, Held(artifact.Stream.SafeFileHandle.DangerousGetHandle().ToInt32())));
        }
        catch
        {
            // Remove only this writer's packet identity; leave private safe orphan
            // sidecars for operator handling instead of following substituted names.
            if (published == null) published = _artifacts.Find(artifact => artifact.Parent == root && artifact.Name == name);
            Rollback(root, published);
            throw;
        }
    }
    private static void Rollback(int root, Artifact packet)
    {
        if (root < 0 || packet == null) return;
        try
        {
            Metadata current = Named(root, packet.Name);
            // Keep packet disposal last. If a disposal threw after releasing its
            // handle, only its complete stable metadata can identify that name.
            bool owned = packet.Handle.IsClosed ? SameFile(current, packet.Before) :
                Identity(current, Held(packet.Handle.DangerousGetHandle().ToInt32()), false);
            if (owned) unlinkat(root, packet.Name, 0);
        }
        catch { }
    }
    /// <summary>Attempt every release; roll back an owned packet on bounded cleanup failure.</summary>
    public void Dispose()
    {
        bool failed = false;
        foreach (Artifact artifact in _artifacts)
        {
            try { artifact.Stream.Dispose(); }
            catch { failed = true; Rollback(_publicationRoot, _published); }
            finally
            {
                // A managed stream disposal may throw before releasing its
                // native handle; attempt that independent release as well.
                try { artifact.Handle.Dispose(); }
                catch { failed = true; Rollback(_publicationRoot, _published); }
            }
        }
        _artifacts.Clear();
        for (int index = _directories.Count - 1; index >= 0; --index)
        {
            try
            {
                if (close(_directories[index].Descriptor) != 0) { failed = true; Rollback(_publicationRoot, _published); }
            }
            catch { failed = true; Rollback(_publicationRoot, _published); }
        }
        _directories.Clear();
        _publicationRoot = -1;
        _published = null;
        if (failed) throw new IOException("retained-stream-cleanup-failed");
    }
}
'@
    }
    $script:retentionWriter = [C1PrivateStreamWriter]::new($Directory)
}

function Write-C1RetainedPacket {
    param([Parameter(Mandatory)][object]$Packet)

    $timestamp = [DateTimeOffset]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
    $captureId = [Guid]::NewGuid().ToString('N')
    $filename = "c1.15-runtime-control-plane-identity-$timestamp-$captureId.json"
    $encoding = [System.Text.UTF8Encoding]::new($false, $true)
    $packetBytes = $encoding.GetBytes(($Packet | ConvertTo-Json -Depth 14) + [Environment]::NewLine)
    $manifestBytes = $null
    $streams = [System.Collections.Generic.List[byte[]]]::new()
    if ($Packet.producerStatus -ceq 'observed') {
        if ($script:retainedStreams.Count -ne $script:commandLedger.Count) { throw 'retained-stream-pairs-incomplete' }
        $receipts = [System.Collections.Generic.List[object]]::new()
        for ($index = 0; $index -lt $script:commandLedger.Count; $index++) {
            $command = $script:commandLedger[$index]
            $pair = $script:retainedStreams[$index]
            if ($pair.purpose -cne $command.purpose -or $command.streamSafety -cne 'validated') {
                throw 'retained-stream-pairs-incomplete'
            }
            foreach ($name in @('stdout', 'stderr')) {
                $raw = $pair[$name]
                $hash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($raw)).ToLowerInvariant()
                if ($raw.Length -gt 1MB -or $hash -cne $command[$name + 'Sha256']) { throw 'retained-stream-hash-mismatch' }
                $streams.Add($raw)
            }
            $receipts.Add([ordered]@{ index = $index; purpose = $command.purpose;
                stdoutSha256 = $command.stdoutSha256; stderrSha256 = $command.stderrSha256 })
        }
        $manifest = [ordered]@{
            schemaVersion = 'hexalith.access-telemetry.c1.retained-streams/v1'
            captureSha256 = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($packetBytes)).ToLowerInvariant()
            receipts = @($receipts)
        }
        $manifestBytes = $encoding.GetBytes(($manifest | ConvertTo-Json -Depth 14) + [Environment]::NewLine)
    }
    $script:retentionWriter.Publish($filename, $packetBytes, $streams.ToArray(), $manifestBytes)
    return Join-Path $resolvedEvidenceDirectory $filename
}

function Write-ImmutablePacket {
    param(
        [Parameter(Mandatory)][object]$Packet,
        [Parameter(Mandatory)][string]$Directory,
        [string]$FilePrefix = 'c1.15-runtime-control-plane-identity'
    )

    $resolvedDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Directory)
    [System.IO.Directory]::CreateDirectory($resolvedDirectory) | Out-Null
    $timestamp = [DateTimeOffset]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
    $captureId = [Guid]::NewGuid().ToString('N')
    $path = Join-Path $resolvedDirectory "$FilePrefix-$timestamp-$captureId.json"
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

if ($script:pg2RuntimeCapture) {
    Assert-SecretSafeOutput $QualificationSessionId
    if ($QualificationSessionId -match '(?i)(?:token|secret|password|authorization|credential|connectionstring)') {
        throw 'qualification-session-required-or-invalid'
    }
    $resolvedEvidenceDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($EvidenceDirectory)
    Assert-SecretSafeOutput $resolvedEvidenceDirectory
    $script:captureStartedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    $script:repositoryRoot = Split-Path $PSScriptRoot -Parent
    try { Assert-SecretSafeOutput $script:repositoryRoot }
    catch { [Console]::Error.WriteLine('secret-shaped-output'); exit 1 }
    $script:runtimeSourceSnapshots = [ordered]@{}
    $strictSourceUtf8 = [System.Text.UTF8Encoding]::new($false, $true)
    # The executing command's parsed text is the source authority, not a later file read.
    $parsedMainText = $MyInvocation.MyCommand.ScriptBlock.Ast.Extent.StartScriptPosition.GetFullScript()
    $script:runtimeSourceSnapshots['tools/verify-access-telemetry-c1.ps1'] = $strictSourceUtf8.GetBytes($parsedMainText)
    foreach ($helperName in @('access-telemetry-c1-profile.ps1', 'access-telemetry-c1-component-backend.ps1')) {
        $helperPath = Join-Path $PSScriptRoot $helperName
        if (-not (Test-Path -LiteralPath $helperPath -PathType Leaf) -or
            $null -ne (Get-Item -LiteralPath $helperPath).LinkType) { throw 'producer-source-unavailable' }
        $helperBytes = [System.IO.File]::ReadAllBytes($helperPath)
        $helperText = $strictSourceUtf8.GetString($helperBytes)
        $helperTokens = $null; $helperErrors = $null
        # Retaining the filename in the AST preserves PSCommandPath/PSScriptRoot semantics.
        $helperAst = [System.Management.Automation.Language.Parser]::ParseInput(
            $helperText, $helperPath, [ref]$helperTokens, [ref]$helperErrors)
        if ($helperErrors.Count) { throw 'producer-source-invalid' }
        $script:runtimeSourceSnapshots['tools/' + $helperName] = $helperBytes
        . $helperAst.GetScriptBlock()
    }
    $script:sourceCommandLedger = [System.Collections.Generic.List[object]]::new()
    $approvedProfileInputs = @(Assert-C1ApprovedProfileInputs $script:repositoryRoot)
    Initialize-C1RuntimeProvenance $approvedProfileInputs
    try { Initialize-C1RetentionCustody $resolvedEvidenceDirectory }
    catch { [Console]::Error.WriteLine('retained-stream-custody-unavailable'); exit 1 }
    $invocationArguments = @('-Gate', $Gate, '-ProfileId', $ProfileId,
        '-QualificationSessionId', $QualificationSessionId, '-EvidenceDirectory',
        $resolvedEvidenceDirectory, '-CommandTimeoutSeconds', [string]$CommandTimeoutSeconds)
}

if ($Gate -ceq 'C1.16') {
    . (Join-Path $PSScriptRoot 'access-telemetry-c1-component-backend.ps1')
    Invoke-C1ComponentBackendCapture
    exit 0
}

$capturedAtUtc = $(if ($script:pg2RuntimeCapture) { $script:captureStartedAtUtc } else { [DateTimeOffset]::UtcNow.ToString('o') })
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
        sha256 = $(if ($script:pg2RuntimeCapture) {
            [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData(
                $script:runtimeSourceSnapshots['tools/verify-access-telemetry-c1.ps1'])).ToLowerInvariant()
        } else { (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant() })
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
        '-o', $podIdentityOutput
    ) -SkipSourceHash
    $podsPayload = ConvertFrom-PodIdentityProjection -Json $podsJson
    if ($script:pg2RuntimeCapture) {
        Assert-C1CredentialFields -Value $podsPayload.items
        foreach ($selectedPod in $podsPayload.items) {
            [void](Get-C1RequiredString $selectedPod.status 'phase' '\A[A-Za-z]+\z' 'running-pod-phase-invalid')
            if ($selectedPod.status.conditions -isnot [System.Array] -or
                $selectedPod.status.containerStatuses -isnot [System.Array]) { throw 'running-pod-not-stable' }
        }
    }
    if ($script:pg2RuntimeCapture) {
        $script:sourceLedger.Add([ordered]@{
            source = 'kubectl:lifecycle-pods:identity'
            sha256 = $script:commandLedger[$script:commandLedger.Count - 1].stdoutSha256
        })
    }
    else {
        Add-SourceHash 'kubectl:lifecycle-pods:identity' $podsJson
    }

    $runningPods = @($podsPayload.items | Where-Object {
        [string]::Equals([string]$_.status.phase, 'Running', [StringComparison]::Ordinal)
    })
    if ($runningPods.Count -eq 0) {
        throw 'no-running-lifecycle-pod'
    }

    if ($script:pg2RuntimeCapture) { $initialSelectedPods = @(Get-C1PG2PodIdentities $podsPayload.items) }
    $seenPodNames = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $seenPodUids = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $validatedPodUids = [System.Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
    foreach ($pod in $runningPods) {
        $rawPodName = $pod.metadata.name
        if ($rawPodName -isnot [string] -or [string]::IsNullOrWhiteSpace($rawPodName)) {
            throw 'running-pod-name-missing'
        }
        if ($script:pg2RuntimeCapture) {
            [void](Get-C1RequiredString $pod.metadata 'name' '\A[a-z0-9][a-z0-9.-]{0,252}\z' 'running-pod-identity-invalid')
            [void](Get-C1RequiredString $pod.metadata 'uid' '\A[A-Za-z0-9][A-Za-z0-9-]{0,127}\z' 'running-pod-identity-invalid')
        }
        $podName = [string]$rawPodName
        if (-not $seenPodNames.Add($podName)) {
            throw 'duplicate-running-pod'
        }
        $rawPodUid = $pod.metadata.uid
        if ($rawPodUid -isnot [string] -or [string]::IsNullOrWhiteSpace($rawPodUid)) {
            throw 'running-pod-uid-missing'
        }
        $podUid = [string]$rawPodUid
        if (-not $seenPodUids.Add($podUid)) {
            throw 'duplicate-running-pod-uid'
        }
        $validatedPodUids.Add($podName, $podUid)
    }

    $perPod = [System.Collections.Generic.List[object]]::new()
    foreach ($pod in $runningPods) {
        $podName = [string]$pod.metadata.name
        $podUid = $validatedPodUids[$podName]

        $podLabel = [string]$pod.metadata.labels.'app.kubernetes.io/name'
        if (-not [string]::Equals($podLabel, $expectedAppId, [StringComparison]::Ordinal)) {
            throw 'running-pod-label-mismatch'
        }
        if ($null -ne $pod.metadata.PSObject.Properties['deletionTimestamp'] -and
            $null -ne $pod.metadata.deletionTimestamp) {
            throw 'running-pod-not-stable'
        }
        $readyConditions = @($pod.status.conditions | Where-Object {
            [string]::Equals([string]$_.type, 'Ready', [StringComparison]::Ordinal)
        })
        if ($readyConditions.Count -ne 1 -or
            -not [string]::Equals([string]$readyConditions[0].status, 'True', [StringComparison]::Ordinal)) {
            throw 'running-pod-not-stable'
        }

        $containerStatuses = @($pod.status.containerStatuses)
        $lifecycleStatus = @($containerStatuses | Where-Object {
            [string]::Equals([string]$_.name, 'lifecycle', [StringComparison]::Ordinal)
        })
        $sidecarStatus = @($containerStatuses | Where-Object {
            [string]::Equals([string]$_.name, 'daprd', [StringComparison]::Ordinal)
        })
        if ($lifecycleStatus.Count -ne 1 -or $sidecarStatus.Count -ne 1 -or
            $lifecycleStatus[0].ready -isnot [bool] -or $sidecarStatus[0].ready -isnot [bool] -or
            -not $lifecycleStatus[0].ready -or -not $sidecarStatus[0].ready) {
            throw 'running-pod-containers-not-ready'
        }

        $imageId = [string]$sidecarStatus[0].imageID
        if ($script:pg2RuntimeCapture -and ($sidecarStatus[0].imageID -isnot [string] -or
            $imageId -cnotmatch '\A(?:[A-Za-z0-9._:/@-]+)?sha256:(?:b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8|edbe3fc30d7efc90869411666fd03b70bb89eafed382bb37ff9a6de2fcab914b)\z')) {
            throw 'runtime-image-mismatch'
        }
        $digestPattern = $(if ($script:pg2RuntimeCapture) { 'sha256:[0-9a-f]{64}\z' } else { 'sha256:[0-9a-f]{64}$' })
        $digestMatch = [regex]::Match($imageId, $digestPattern)
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

        if ($script:pg2RuntimeCapture -and $daprdVersion -cne '1.18.1') { throw 'runtime-version-pin-mismatch' }

        $metadataProbe = 'if [ -z "${DAPR_API_TOKEN:-}" ]; then echo "required runtime credential unavailable" >&2; exit 72; fi; metadata="$(wget -qO- --timeout=5 --header="dapr-api-token: ${DAPR_API_TOKEN}" http://127.0.0.1:3500/v1.0/metadata)" || exit $?; case "$metadata" in *"$DAPR_API_TOKEN"*) echo "secret-shaped-output" >&2; exit 73;; esac; printf "%s" "$metadata"'
        $metadataJson = Invoke-KubectlObservation -Purpose "metadata:$podName" -Arguments @(
            '--context', $context,
            '-n', $namespace,
            'exec', $podName,
            '-c', 'lifecycle',
            '--', '/bin/sh', '-ec', $metadataProbe
        ) -SkipSourceHash
        Assert-MetadataJsonShape -Json $metadataJson
        try {
            $metadata = $metadataJson | ConvertFrom-Json -Depth 30
        }
        catch {
            throw 'malformed-metadata-json'
        }
        Assert-SecretSafeMetadata -Value $metadata
        if ($script:pg2RuntimeCapture) {
            Assert-C1CredentialFields -Value $metadata
            if ($metadata.runtimeVersion -isnot [string] -or $metadata.runtimeVersion -cne '1.18.1') {
                throw 'runtime-version-pin-mismatch'
            }
        }

        $rawAppId = Get-RequiredProperty -Object $metadata -Names @('id') -FailureCode 'metadata-app-id-missing'
        if ($script:pg2RuntimeCapture -and $rawAppId -isnot [string]) { throw 'metadata-app-id-mismatch' }
        $appId = [string]$rawAppId
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
        '-o', $podIdentityOutput
    ) -SkipSourceHash
    $podsAfterPayload = ConvertFrom-PodIdentityProjection -Json $podsAfterJson
    if ($script:pg2RuntimeCapture) {
        Assert-C1CredentialFields -Value $podsAfterPayload.items
        try { $recheckedSelectedPods = @(Get-C1PG2PodIdentities $podsAfterPayload.items) }
        catch { throw 'running-pod-changed' }
        if ((Get-C1JsonSha256 $initialSelectedPods) -cne (Get-C1JsonSha256 $recheckedSelectedPods)) {
            throw 'running-pod-changed'
        }
    }
    if ($script:pg2RuntimeCapture) {
        $script:sourceLedger.Add([ordered]@{
            source = 'kubectl:lifecycle-pods-recheck:identity'
            sha256 = $script:commandLedger[$script:commandLedger.Count - 1].stdoutSha256
        })
    }
    else {
        Add-SourceHash 'kubectl:lifecycle-pods-recheck:identity' $podsAfterJson
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
        $readyConditionsAfter = @($podAfter.status.conditions | Where-Object {
            [string]::Equals([string]$_.type, 'Ready', [StringComparison]::Ordinal)
        })
        $containerStatusesAfter = @($podAfter.status.containerStatuses)
        $lifecycleStatusAfter = @($containerStatusesAfter | Where-Object {
            [string]::Equals([string]$_.name, 'lifecycle', [StringComparison]::Ordinal)
        })
        $sidecarStatusAfter = @($containerStatusesAfter | Where-Object {
            [string]::Equals([string]$_.name, 'daprd', [StringComparison]::Ordinal)
        })
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

    if ($runtimeVersions.Count -ne 1 -or (-not $script:pg2RuntimeCapture -and $sidecarImageDigests.Count -ne 1) -or $appIds.Count -ne 1 -or
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

if ($script:pg2RuntimeCapture) {
    $sourceRecheck = 'unchanged'
    try { Assert-C1RuntimeSourcesUnchanged }
    catch {
        $sourceRecheck = 'changed-or-unavailable'
        $producerStatus = 'blocked'
        if (-not $blockers.Contains('producer-source-changed')) { $blockers.Add('producer-source-changed') }
    }
    if ($producerStatus -ne 'observed') {
        $observations = [ordered]@{
            pods = @(); runtimeVersions = @(); sidecarImageIds = @(); sidecarImageDigests = @()
            appIds = @(); schedulerConnectedAddresses = @(); actorTypes = @(); enabledFeatures = @()
            alphaOptIn = [ordered]@{ componentIsAlpha = $null; allowAlphaComponent = $null }
        }
    }
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

if ($script:pg2RuntimeCapture) {
    $packet.schemaVersion = 'hexalith.access-telemetry.c1.evidence/v2'
    $packet.profileIdentity = 'postgresql-v2-dapr-1.18.1-postgresql-18.6-onprem-k8s1-openebs-local-retain-400g-qualification-kvm-v3'
    $packet.profileSha256 = '95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb'
    $packet.workloadId = 'adr-27.1-two-writer-500eps'
    $packet.workloadSha256 = '71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f'
    $packet.qualificationSessionId = $QualificationSessionId
    $packet.target = [ordered]@{ context = $expectedContext; namespace = $namespace; selector = $targetSelector; appId = $expectedAppId; actorType = $expectedActorType }
    $packet.targetSha256 = Get-C1JsonSha256 $packet.target
    $packet.producer = [ordered]@{
        path = 'tools/verify-access-telemetry-c1.ps1'
        identity = 'repository-collector'
        identityAuthentication = 'not-evaluated'
        arguments = $invocationArguments
        argumentsSha256 = Get-C1JsonSha256 $invocationArguments
        startedAtUtc = $capturedAtUtc
        finishedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
        exitCode = $(if ($producerStatus -ceq 'observed') { 0 } else { 1 })
    }
    $packet.sourceCommands = @($script:sourceCommandLedger)
    $packet.sourceCommit = $script:sourceCommit
    $packet.sourceDisposition = $script:sourceDisposition
    $packet.worktreeDirty = $script:worktreeDirty
    $packet.producerSources = @($script:producerSources)
    $packet.finalSourceRecheck = $sourceRecheck
    $packet.resultCount = $(if ($producerStatus -ceq 'observed') { $observations.pods.Count } else { 0 })
    $packet.failureCount = $blockers.Count
    $packet.skipCount = 0
    $packet.independentDisposition = 'pending'
}

if ($script:pg2RuntimeCapture) {
    try {
        $packetPath = Write-C1RetainedPacket -Packet $packet
        $script:retentionWriter.Dispose()
    }
    catch {
        try { $script:retentionWriter.Dispose() } catch { }
        [Console]::Error.WriteLine('retained-stream-publication-failed')
        exit 1
    }
}
else { $packetPath = Write-ImmutablePacket -Packet $packet -Directory $EvidenceDirectory }
Write-Output $packetPath
if ($producerStatus -ne 'observed') {
    exit 1
}
