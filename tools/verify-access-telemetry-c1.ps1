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
$expectedContext = 'jpiquot@local'
$targetSelector = 'app.kubernetes.io/name=memories-access-telemetry'
$expectedAppId = 'memories-access-telemetry'
$expectedActorType = 'AccessTelemetryLifecycleActor'
$podIdentityOutput = "jsonpath-as-json={range .items[*]}{['metadata','status']}{end}"
$script:commandLedger = [System.Collections.Generic.List[object]]::new()
$script:sourceLedger = [System.Collections.Generic.List[object]]::new()
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
                $stdout = $strictUtf8.GetString($stdoutCapture.ToArray())
                $stderr = $strictUtf8.GetString($stderrCapture.ToArray())
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
        $commandEntry.stdoutSha256 = Get-TextSha256 $stdout
        $commandEntry.stderrSha256 = Get-TextSha256 $stderr
        $commandEntry.streamSafety = 'validated'
        if ($exitCode -eq 0 -and -not [string]::IsNullOrWhiteSpace($stdout)) {
            $commandEntry.resultCount = 1
            $commandEntry.failureCount = 0
        }
    }
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
    Add-SourceHash 'kubectl:lifecycle-pods:identity' $podsJson

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
    Add-SourceHash 'kubectl:lifecycle-pods-recheck:identity' $podsAfterJson

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
    $packet.profileIdentity = 'postgresql-v2-dapr-1.18.1-postgresql-18.6-onprem-k8s1-openebs-local-retain-400g-v2'
    $packet.profileSha256 = '7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe'
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

$packetPath = Write-ImmutablePacket -Packet $packet -Directory $EvidenceDirectory
Write-Output $packetPath
if ($producerStatus -ne 'observed') {
    exit 1
}
