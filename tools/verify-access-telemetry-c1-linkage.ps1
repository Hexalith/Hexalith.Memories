[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Gate,
    [Parameter(Mandatory)][string]$ProfileId,
    [Parameter(Mandatory)][string]$Mode,
    [Parameter(Mandatory)][string]$LifecyclePod,
    [Parameter(Mandatory)][string]$ScopeFile,
    [Parameter(Mandatory)][string]$EvidenceDirectory,
    [ValidateRange(1, 30)][int]$CommandTimeoutSeconds = 10
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
# Validate the entire invocation/scope before target contact or output creation.
if ($Gate -cne 'C1.16' -or $ProfileId -cne 'PG-ONPREM-2' -or $Mode -cne 'Observe') {
    throw 'unsupported-linkage-mode-or-profile'
}
if ($LifecyclePod -cnotmatch '^[a-z0-9][a-z0-9.-]{0,252}$') { throw 'selected-pod-invalid' }
$namespace = 'hexalith-memories'
$expectedContext = 'jpiquot@local'
$expectedAppId = 'memories-access-telemetry'
$profileSha256 = '7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe'
$podIdentityOutput = "jsonpath-as-json={range .items[*]}{['metadata','status']}{end}"
$script:commandLedger = [System.Collections.Generic.List[object]]::new()
$script:sourceLedger = [System.Collections.Generic.List[object]]::new()

# Bounded transport/JSON/immutable-envelope utilities follow the re-verified C1
# producer pattern. The producer and its historical invocation remain unchanged.
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
    if ($Text -match '(?i)\b(?:password|passwd|token|secret|connection[-_]?string)\s*[:=]\s*[^\s"'']+' -or
        $Text -match '(?i)postgres(?:ql)?://[^\s:/]+:[^\s@]+@') {
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
        [switch]$SkipSourceHash,
        [switch]$PreserveOutput,
        [string[]]$CommandTemplate
    )

    Assert-LinkageScopeActive
    $metadataRequest = $Purpose.StartsWith('component-metadata:', [StringComparison]::Ordinal)
    if ($metadataRequest) {
        # Reuse the helper's identity validation, replacing only this collector's
        # transport. Direct loopback TCP cannot follow redirects or use proxies.
        $Arguments[-1] = Get-LinkageHttpProbe '/v1.0/metadata'
        $PreserveOutput = $true
    }
    $identityArguments = $(if ($CommandTemplate) { $CommandTemplate } else { $Arguments })
    $commandIdentity = 'kubectl ' + ($identityArguments -join [char]0x1f)
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
        $scopeDeadline = Read-LinkageTime $scope.authorizedUntilUtc
        if ($scopeDeadline -lt $deadline) { $deadline = $scopeDeadline }
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

        $encoding = [System.Text.UTF8Encoding]::new($false, $true)
        $stdout = $encoding.GetString($stdoutCapture.ToArray())
        $stderr = $encoding.GetString($stderrCapture.ToArray())
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
    if ($Gate -ceq 'C1.16') {
        Assert-C1DecodedOutputSafety $stdout
        Assert-C1DecodedOutputSafety $stderr
    }
    if (-not $SkipSourceHash) {
        Add-SourceHash "kubectl:${Purpose}:stdout" $stdout
        Add-SourceHash "kubectl:${Purpose}:stderr" $stderr
    }

    if ($exitCode -ne 0) {
        throw "kubectl-$Purpose-exit-$exitCode"
    }

    if ($metadataRequest) { return Read-LinkageHttpResponse $stdout 200 }
    if ($PreserveOutput) { return $stdout }
    return $stdout.Trim()
}

function Get-LinkageHttpProbe {
    param([string]$Path)

    # The last pipeline process owns the native exit status. Response bytes stay
    # exclusively on stdout; they cannot manufacture a successful transport exit.
    $validation = 'if [ -z "${DAPR_API_TOKEN:-}" ]; then echo "required runtime credential unavailable" >&2; exit 72; fi; command -v grep >/dev/null 2>&1 || { echo "required runtime validator unavailable" >&2; exit 72; }; command -v nc >/dev/null 2>&1 || { echo "required loopback client unavailable" >&2; exit 72; }; set +e; printf "%s" "$DAPR_API_TOKEN" | grep -q ''[[:space:][:cntrl:]]''; validation=$?; set -e; case "$validation" in 1) ;; 0) echo "required runtime credential invalid" >&2; exit 72;; *) echo "required runtime validation failed" >&2; exit 72;; esac; '
    return $validation + 'printf "GET ' + $Path + ' HTTP/1.1\r\nHost: 127.0.0.1:3500\r\nConnection: close\r\ndapr-api-token: %s\r\n\r\n" "$DAPR_API_TOKEN" | nc -w 5 127.0.0.1 3500 2>/dev/null'
}

function Read-LinkageHttpResponse {
    param([string]$Raw, [int]$ExpectedStatus)

    $end = $Raw.IndexOf("`r`n`r`n", [StringComparison]::Ordinal)
    if ($end -lt 0 -or $end -gt 16384) { throw 'invalid-loopback-http-response' }
    $lines = $Raw.Substring(0, $end).Split("`r`n", [StringSplitOptions]::None)
    if ($lines[0] -cnotmatch ('^HTTP/1\.[01] ' + $ExpectedStatus + '(?: [\x20-\x7e]*)?$')) { throw 'loopback-http-status-refused' }
    $length = $null
    foreach ($line in @($lines | Select-Object -Skip 1)) {
        if ($line -cnotmatch '^[-A-Za-z0-9]+:[\t\x20-\x7e]*$' -or $line -match '^(?:Transfer-Encoding|Content-Encoding):') {
            throw 'invalid-loopback-http-response'
        }
        if ($line -match '^Content-Length:\s*(\d+)\s*$') {
            if ($null -ne $length -or $Matches[1].Length -gt 7) { throw 'invalid-loopback-http-response' }
            $length = [int]$Matches[1]
        }
        elseif ($line -match '^Content-Length:') { throw 'invalid-loopback-http-response' }
    }
    $body = $Raw.Substring($end + 4)
    $bodyBytes = [Text.Encoding]::UTF8.GetByteCount($body)
    if (($null -ne $length -and $length -ne $bodyBytes) -or
        ($ExpectedStatus -eq 204 -and $bodyBytes -ne 0) -or
        ($ExpectedStatus -eq 200 -and ($null -eq $length -or $bodyBytes -eq 0))) { throw 'invalid-loopback-http-response' }
    return $body
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
            $projection = ConvertFrom-Json -InputObject $frame.GetRawText() -Depth 30 -NoEnumerate -DateKind String
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

function Read-LinkageApprovedHelper {
    # No helper code executes until its exact bytes and this collector are bound
    # to the bounded scope input. Full scope validation follows without contact.
    $path = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ScopeFile)
    if (-not [IO.File]::Exists($path) -or ([IO.FileInfo]$path).Length -gt 16384) { throw 'linkage-scope-unavailable' }
    $raw = [IO.File]::ReadAllText($path)
    Assert-MetadataJsonShape $raw
    $candidate = ConvertFrom-Json -InputObject $raw -Depth 30 -DateKind String
    if ($candidate.collectorSha256 -isnot [string] -or
        (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant() -cne $candidate.collectorSha256) {
        throw 'linkage-approved-source-drift'
    }
    $helperPath = Join-Path $PSScriptRoot 'access-telemetry-c1-component-backend.ps1'
    $buffer = [byte[]]::new(131073)
    $stream = [IO.File]::Open($helperPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete)
    try {
        $count = 0
        while ($count -lt $buffer.Length) {
            $read = $stream.Read($buffer, $count, $buffer.Length - $count)
            if ($read -eq 0) { break }
            $count += $read
        }
    }
    finally { $stream.Dispose() }
    if ($count -eq 0 -or $count -gt 131072) { throw 'linkage-helper-source-unbounded' }
    $bytes = [byte[]]::new($count)
    [Array]::Copy($buffer, $bytes, $count)
    $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
    if ($candidate.identityHelperSha256 -isnot [string] -or $hash -cne $candidate.identityHelperSha256) { throw 'linkage-approved-source-drift' }
    $text = [Text.UTF8Encoding]::new($false, $true).GetString($bytes).TrimStart([char]0xfeff)
    return [ScriptBlock]::Create($text)
}

$approvedHelper = Read-LinkageApprovedHelper
. $approvedHelper

function Read-LinkageJson {
    param([string]$Json, [string]$FailureCode)

    try {
        Assert-MetadataJsonShape $Json
        $value = ConvertFrom-Json -InputObject $Json -Depth 30 -DateKind String
        Assert-SecretSafeMetadata $value
        Assert-C1CredentialFields $value
    }
    catch {
        if ($_.Exception.Message -ceq 'secret-shaped-output') { throw 'secret-shaped-output' }
        throw $FailureCode
    }
    return $value
}

function Assert-LinkageFields {
    param([object]$Value, [string[]]$Fields, [string]$FailureCode)

    if ($Value -isnot [System.Management.Automation.PSCustomObject] -or
        (Get-CollectionIdentity @($Value.PSObject.Properties.Name | Sort-Object -CaseSensitive)) -cne
        (Get-CollectionIdentity @($Fields | Sort-Object -CaseSensitive))) { throw $FailureCode }
}

function Read-LinkageTime {
    param([object]$Value)

    if ($Value -isnot [string] -or $Value -cnotmatch '^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?Z$') {
        throw 'linkage-timestamp-invalid'
    }
    $parsed = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParse($Value, [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::AssumeUniversal, [ref]$parsed)) { throw 'linkage-timestamp-invalid' }
    return $parsed
}

function Read-LinkageScope {
    $path = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ScopeFile)
    if (-not [IO.File]::Exists($path) -or ([IO.FileInfo]$path).Length -gt 16384) { throw 'linkage-scope-unavailable' }
    $raw = [IO.File]::ReadAllText($path)
    Assert-C1DecodedOutputSafety $raw
    $scope = Read-LinkageJson $raw 'linkage-scope-invalid'
    Assert-LinkageFields $scope @('schemaVersion', 'gate', 'profileId', 'profileSha256', 'context',
        'namespace', 'lifecyclePod', 'evidenceDirectory', 'authorizedFromUtc', 'authorizedUntilUtc',
        'authorizationReference', 'independentReviewer', 'exclusiveReadWindow',
        'exclusiveReadWindowEvidenceSha256', 'collectorSha256', 'identityHelperSha256', 'lifecycleImageSha256') 'linkage-scope-invalid'
    $expected = [ordered]@{
        schemaVersion = 'hexalith.access-telemetry.c1.linkage.scope/v1'
        gate = $Gate; profileId = $ProfileId; profileSha256 = $profileSha256
        context = $expectedContext; namespace = $namespace; lifecyclePod = $LifecyclePod
    }
    foreach ($field in $expected.Keys) {
        if ($scope.$field -isnot [string] -or $scope.$field -cne $expected[$field]) { throw 'linkage-scope-mismatch' }
    }
    $archive = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($EvidenceDirectory)
    if ($scope.evidenceDirectory -isnot [string] -or -not [IO.Path]::IsPathFullyQualified($EvidenceDirectory) -or
        -not [IO.Path]::IsPathFullyQualified($scope.evidenceDirectory) -or
        [IO.Path]::GetFullPath($scope.evidenceDirectory) -cne $archive) { throw 'linkage-archive-mismatch' }
    $ancestor = $archive
    while ($ancestor) {
        if ([IO.File]::Exists($ancestor)) { throw 'linkage-archive-is-file' }
        $ancestor = [IO.Path]::GetDirectoryName($ancestor)
    }
    foreach ($field in @('authorizationReference', 'independentReviewer')) {
        [void](Get-C1RequiredString $scope $field '^[A-Za-z0-9][A-Za-z0-9._:/@ -]{0,255}$' 'linkage-scope-invalid')
    }
    if ($scope.exclusiveReadWindow -isnot [bool] -or -not $scope.exclusiveReadWindow) {
        throw 'exclusive-read-window-required'
    }
    foreach ($field in @('exclusiveReadWindowEvidenceSha256', 'collectorSha256', 'identityHelperSha256', 'lifecycleImageSha256')) {
        [void](Get-C1RequiredString $scope $field '^[0-9a-f]{64}$' 'linkage-scope-invalid')
    }
    $from = Read-LinkageTime $scope.authorizedFromUtc
    $until = Read-LinkageTime $scope.authorizedUntilUtc
    $now = [DateTimeOffset]::UtcNow
    if ($from -gt $now -or $until -le $now -or $until -le $from -or ($until - $from).TotalMinutes -gt 15) {
        throw 'linkage-scope-expired-or-unbounded'
    }
    foreach ($entry in @(
        @{ file = $PSCommandPath; field = 'collectorSha256' },
        @{ file = (Join-Path $PSScriptRoot 'access-telemetry-c1-component-backend.ps1'); field = 'identityHelperSha256' }
    )) {
        $hash = (Get-FileHash -LiteralPath $entry.file -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($hash -cne $scope.($entry.field)) { throw 'linkage-approved-source-drift' }
        $script:sourceLedger.Add([ordered]@{ source = 'tools/' + [IO.Path]::GetFileName($entry.file); sha256 = $hash })
    }
    Add-SourceHash 'operator:linkage-scope' $raw
    return $scope
}

function Assert-LinkageScopeActive {
    if ([DateTimeOffset]::UtcNow -ge (Read-LinkageTime $scope.authorizedUntilUtc)) { throw 'linkage-scope-expired-or-unbounded' }
}

function Assert-LinkageSourcesStable {
    foreach ($entry in @(
        @{ file = $PSCommandPath; hash = $scope.collectorSha256 },
        @{ file = (Join-Path $PSScriptRoot 'access-telemetry-c1-component-backend.ps1'); hash = $scope.identityHelperSha256 }
    )) {
        if ((Get-FileHash -LiteralPath $entry.file -Algorithm SHA256).Hash.ToLowerInvariant() -cne $entry.hash) {
            throw 'linkage-approved-source-drift'
        }
    }
    foreach ($relative in $approvedInputs.Keys) {
        if ((Get-FileHash -LiteralPath (Join-Path $repositoryRoot $relative) -Algorithm SHA256).Hash.ToLowerInvariant() -cne $approvedInputs[$relative]) {
            throw 'approved-profile-input-drift'
        }
    }
}

function Get-LinkagePod {
    param([string]$Label, [string[]]$Containers, [string]$Purpose, [string]$SelectedName)

    $arguments = @('--context', $expectedContext, '-n', $namespace, 'get', 'pods', '-l', "app.kubernetes.io/name=$Label")
    if ($SelectedName) { $arguments += @('--field-selector', "metadata.name=$SelectedName") }
    $raw = Invoke-KubectlObservation $Purpose ($arguments + @('-o', $podIdentityOutput)) -SkipSourceHash
    $payload = ConvertFrom-PodIdentityProjection $raw
    Assert-C1CredentialFields $payload.items
    if ($payload.items.Count -ne 1) { throw 'selected-pod-count-invalid' }
    $pod = $payload.items[0]
    $name = Get-C1RequiredString $pod.metadata 'name' '^[a-z0-9][a-z0-9.-]{0,252}$' 'selected-pod-invalid'
    $uid = Get-C1RequiredString $pod.metadata 'uid' '^[A-Za-z0-9][A-Za-z0-9-]{0,127}$' 'selected-pod-invalid'
    if (($SelectedName -and $name -cne $SelectedName) -or
        $pod.metadata.labels.'app.kubernetes.io/name' -isnot [string] -or
        $pod.metadata.labels.'app.kubernetes.io/name' -cne $Label -or $null -ne $pod.metadata.deletionTimestamp -or
        $pod.status.phase -isnot [string] -or $pod.status.phase -cne 'Running' -or $pod.status.conditions -isnot [System.Array] -or
        $pod.status.containerStatuses -isnot [System.Array] -or
        $pod.status.containerStatuses.Count -ne $Containers.Count) { throw 'selected-pod-not-stable' }
    $ready = @($pod.status.conditions | Where-Object { $_.type -ceq 'Ready' })
    if ($ready.Count -ne 1 -or $ready[0].status -isnot [string] -or $ready[0].status -cne 'True') { throw 'selected-pod-not-stable' }
    foreach ($condition in $pod.status.conditions) {
        if ($condition.type -isnot [string]) { throw 'selected-pod-not-stable' }
    }
    foreach ($containerStatus in $pod.status.containerStatuses) {
        if ($containerStatus.name -isnot [string]) { throw 'selected-pod-not-stable' }
    }
    $ip = Get-C1RequiredString $pod.status 'podIP' '^[0-9a-fA-F:.]{2,45}$' 'selected-pod-ip-invalid'
    $parsedIp = $null
    if (-not [Net.IPAddress]::TryParse($ip, [ref]$parsedIp) -or $parsedIp.ToString() -cne $ip) { throw 'selected-pod-ip-invalid' }
    $identities = [ordered]@{}
    foreach ($container in $Containers) {
        $statuses = @($pod.status.containerStatuses | Where-Object { $_.name -ceq $container })
        if ($statuses.Count -ne 1 -or $statuses[0].ready -isnot [bool] -or -not $statuses[0].ready) { throw 'selected-container-not-ready' }
        $status = $statuses[0]
        $image = Get-C1RequiredString $status 'imageID' '^(?:[A-Za-z0-9._:/@-]+)?sha256:[0-9a-f]{64}$' 'selected-image-invalid'
        if ($container -ceq 'postgresql' -and $image -cnotmatch 'sha256:(?:5a5a84b19854a9ffaa54082c166ff4ec27473a361e496e5ea167f298f2da9722|0377e72c5289ed2f98cf61b1a9c2db9eb9d300317fe14244492fbc94343b3d04)$') { throw 'backend-image-mismatch' }
        if ($container -ceq 'daprd' -and $image -cnotmatch 'sha256:(?:b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8|edbe3fc30d7efc90869411666fd03b70bb89eafed382bb37ff9a6de2fcab914b)$') { throw 'runtime-image-mismatch' }
        if ($container -ceq 'lifecycle' -and -not $image.EndsWith('sha256:' + $scope.lifecycleImageSha256, [StringComparison]::Ordinal)) { throw 'lifecycle-image-mismatch' }
        $id = Get-C1RequiredString $status 'containerID' '^[A-Za-z0-9._-]+://[a-zA-Z0-9._-]{1,128}$' 'container-incarnation-invalid'
        if (($status.restartCount -isnot [long] -and $status.restartCount -isnot [int]) -or $status.restartCount -lt 0 -or
            $null -eq $status.state.running -or @($status.state.PSObject.Properties).Count -ne 1) { throw 'container-incarnation-invalid' }
        $started = Read-LinkageTime $status.state.running.startedAt
        if ($started -gt [DateTimeOffset]::UtcNow) { throw 'container-incarnation-invalid' }
        $identities[$container] = [ordered]@{ imageId = $image; containerId = $id; restartCount = $status.restartCount; startedAtUtc = $status.state.running.startedAt }
    }
    $result = [ordered]@{ pod = $name; podUid = $uid; podIp = $ip; containers = $identities }
    Add-SourceHash "kubectl:${Purpose}:allowlisted" ($result | ConvertTo-Json -Compress -Depth 8)
    return $result
}

function Get-LinkageSessions {
    param([string]$Purpose)

    # Fixed role/database and selected pod IP only; never retrieve SQL, keys, payloads,
    # application_name, client certificate names, or credentials. LIMIT bounds rows.
    $sql = @"
WITH sessions AS MATERIALIZED (
 SELECT a.pid, a.usename, a.datname, host(a.client_addr) AS "clientAddr", a.client_port AS "clientPort",
 to_char(a.backend_start AT TIME ZONE 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS.US"Z"') AS "backendStartUtc",
 to_char(a.query_start AT TIME ZONE 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS.US"Z"') AS "queryStartUtc",
 to_char(a.state_change AT TIME ZONE 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS.US"Z"') AS "stateChangeUtc",
 a.state, s.ssl, s.version AS "tlsVersion"
 FROM pg_stat_activity a LEFT JOIN pg_stat_ssl s ON s.pid = a.pid
 WHERE a.client_addr = '$($lifecycle.podIp)'::inet AND a.backend_type = 'client backend'
 ORDER BY a.pid LIMIT 41
)
SELECT json_build_object('serverVersion', current_setting('server_version'),
 'serverVersionNum', current_setting('server_version_num'), 'database', current_database(),
 'user', current_user, 'systemUser', system_user, 'localSocket', inet_client_addr() IS NULL,
 'observedAtUtc', to_char(clock_timestamp() AT TIME ZONE 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
 'sessions', COALESCE((SELECT json_agg(sessions) FROM sessions), '[]'::json))::text;
"@
    $raw = Invoke-KubectlObservation $Purpose @(
        '--context', $expectedContext, '-n', $namespace, 'exec', $backend.pod, '-c', 'postgresql',
        '--', 'env', '-u', 'PGPASSWORD', '-u', 'PGSERVICE', '-u', 'PGSERVICEFILE', '-u', 'PGHOSTADDR',
        'PGPASSFILE=/dev/null', 'PGCONNECT_TIMEOUT=5',
        'PGOPTIONS=-c default_transaction_read_only=on -c statement_timeout=5000',
        'psql', '-X', '--no-password', '--set=ON_ERROR_STOP=1', '--host=/var/run/postgresql',
        '--port=5432', '--username=memories_admin', '--dbname=memories_access_telemetry',
        '--tuples-only', '--no-align', '--command', $sql
    ) -SkipSourceHash
    $value = Read-LinkageJson $raw 'malformed-session-json'
    Assert-LinkageFields $value @('serverVersion', 'serverVersionNum', 'database', 'user', 'systemUser', 'localSocket', 'observedAtUtc', 'sessions') 'session-fields-invalid'
    [void](Get-C1RequiredString $value 'serverVersion' '^18\.6(?: \([A-Za-z0-9.+~: _/-]+\))?$' 'backend-version-invalid')
    [void](Get-C1RequiredString $value 'serverVersionNum' '^180006$' 'backend-version-invalid')
    foreach ($field in @('database', 'user', 'systemUser')) {
        if ($value.$field -isnot [string]) { throw 'backend-peer-identity-invalid' }
    }
    if ($value.database -cne 'memories_access_telemetry' -or $value.user -cne 'memories_admin' -or
        $value.systemUser -cne 'peer:postgres' -or $value.localSocket -isnot [bool] -or -not $value.localSocket) { throw 'backend-peer-identity-invalid' }
    $observed = Read-LinkageTime $value.observedAtUtc
    if ([Math]::Abs(([DateTimeOffset]::UtcNow - $observed).TotalSeconds) -gt 5) { throw 'session-observation-stale' }
    if ($value.sessions -isnot [System.Array] -or $value.sessions.Count -ne 1) { throw 'session-attribution-ambiguous' }
    $session = $value.sessions[0]
    Assert-LinkageFields $session @('pid', 'usename', 'datname', 'clientAddr', 'clientPort',
        'backendStartUtc', 'queryStartUtc', 'stateChangeUtc', 'state', 'ssl', 'tlsVersion') 'session-fields-invalid'
    foreach ($field in @('pid', 'clientPort')) {
        if (($session.$field -isnot [long] -and $session.$field -isnot [int]) -or $session.$field -le 0 -or
            $session.$field -gt $(if ($field -ceq 'clientPort') { 65535 } else { [int]::MaxValue })) { throw 'session-identity-invalid' }
    }
    foreach ($field in @('usename', 'datname', 'clientAddr', 'state', 'tlsVersion')) {
        if ($session.$field -isnot [string]) { throw 'session-identity-or-tls-invalid' }
    }
    if ($session.usename -cne 'memories_access_telemetry_runtime' -or $session.datname -cne 'memories_access_telemetry' -or
        $session.clientAddr -cne $lifecycle.podIp -or $session.state -cne 'idle' -or
        $session.ssl -isnot [bool] -or -not $session.ssl -or $session.tlsVersion -cnotin @('TLSv1.2', 'TLSv1.3')) { throw 'session-identity-or-tls-invalid' }
    $start = Read-LinkageTime $session.backendStartUtc
    foreach ($containerStart in @($lifecycle.containers.daprd.startedAtUtc, $backend.containers.postgresql.startedAtUtc)) {
        if ($start -lt (Read-LinkageTime $containerStart).AddSeconds(-5)) { throw 'session-predates-container-incarnation' }
    }
    $query = Read-LinkageTime $session.queryStartUtc
    $changed = Read-LinkageTime $session.stateChangeUtc
    if ($start -gt $query -or $query -gt $changed -or $changed -gt $observed) { throw 'session-timestamp-order-invalid' }
    Add-SourceHash "kubectl:${Purpose}:allowlisted" ($value | ConvertTo-Json -Compress -Depth 8)
    return $value
}

# Invalid scope and approved configuration drift never create an evidence directory.
$scope = Read-LinkageScope
$repositoryRoot = Split-Path $PSScriptRoot -Parent
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
foreach ($relative in $approvedInputs.Keys) {
    $path = Join-Path $repositoryRoot $relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw 'approved-profile-input-unavailable' }
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($hash -cne $approvedInputs[$relative]) { throw 'approved-profile-input-drift' }
    $script:sourceLedger.Add([ordered]@{ source = $relative; sha256 = $hash })
}

$capturedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
$observations = $null
$status = 'blocked'
$blockers = [System.Collections.Generic.List[string]]::new()
try {
    Assert-LinkageScopeActive
    $context = Invoke-KubectlObservation 'current-context' @('config', 'current-context')
    if ($context -cne $expectedContext) { throw 'profile-context-mismatch' }
    $component = Get-C1ComponentIdentity $context
    $lifecycle = Get-LinkagePod $expectedAppId @('lifecycle', 'daprd') 'linkage-lifecycle-pod' $LifecyclePod
    $backend = Get-LinkagePod 'access-telemetry-postgresql' @('postgresql') 'linkage-backend-pod' ''
    $loaded = Get-C1LoadedComponent $context $LifecyclePod
    $before = Get-LinkageSessions 'linkage-sessions-before'
    Assert-LinkageScopeActive
    $challengeStart = [DateTimeOffset]::UtcNow
    # A cryptographically random synthetic key is never exported, and no state is
    # written. Discard any response body; only one direct HTTP 204 is acceptable.
    $key = 'c1-linkage-absent-' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32)).ToLowerInvariant()
    $probe = Get-LinkageHttpProbe '/v1.0/state/access-telemetry-store/__KEY__?consistency=strong'
    $templateArguments = @(
        '--context', $context, '-n', $namespace, 'exec', $LifecyclePod, '-c', 'lifecycle',
        '--', '/bin/sh', '-ec', $probe
    )
    $arguments = [string[]]$templateArguments.Clone()
    $arguments[-1] = $probe.Replace('__KEY__', $key)
    $raw = Invoke-KubectlObservation 'linkage-absent-state-get' $arguments -SkipSourceHash -PreserveOutput -CommandTemplate $templateArguments
    [void](Read-LinkageHttpResponse $raw 204)
    $challengeEnd = [DateTimeOffset]::UtcNow
    $read = [ordered]@{ httpStatus = 204; responseBodyExported = $false }
    Add-SourceHash 'kubectl:linkage-absent-state-get:allowlisted' ($read | ConvertTo-Json -Compress)
    $after = Get-LinkageSessions 'linkage-sessions-after'
    $first = $before.sessions[0]
    $last = $after.sessions[0]
    foreach ($field in @('pid', 'usename', 'datname', 'clientAddr', 'clientPort', 'backendStartUtc', 'ssl', 'tlsVersion')) {
        if ($first.$field -cne $last.$field) { throw 'session-replaced-or-reused' }
    }
    $beforeTime = Read-LinkageTime $before.observedAtUtc
    $afterTime = Read-LinkageTime $after.observedAtUtc
    $lastQuery = Read-LinkageTime $last.queryStartUtc
    $lastChange = Read-LinkageTime $last.stateChangeUtc
    if ($afterTime -le $beforeTime -or ($afterTime - $beforeTime).TotalSeconds -gt 30 -or
        $lastQuery -le (Read-LinkageTime $first.queryStartUtc) -or $lastChange -le (Read-LinkageTime $first.stateChangeUtc) -or
        $lastQuery -lt $beforeTime -or $lastChange -gt $afterTime -or
        $lastQuery -lt $challengeStart.AddSeconds(-1) -or $lastChange -gt $challengeEnd.AddSeconds(1)) { throw 'session-activity-not-attributable' }
    $componentAfter = Get-C1ComponentIdentity $context
    $lifecycleAfter = Get-LinkagePod $expectedAppId @('lifecycle', 'daprd') 'linkage-lifecycle-pod-recheck' $LifecyclePod
    $backendAfter = Get-LinkagePod 'access-telemetry-postgresql' @('postgresql') 'linkage-backend-pod-recheck' ''
    foreach ($pair in @(@($component, $componentAfter), @($lifecycle, $lifecycleAfter), @($backend, $backendAfter))) {
        if (($pair[0] | ConvertTo-Json -Compress -Depth 10) -cne ($pair[1] | ConvertTo-Json -Compress -Depth 10)) { throw 'selected-identity-changed' }
    }
    Assert-LinkageScopeActive
    Assert-LinkageSourcesStable
    $observations = [ordered]@{
        component = $component; loadedComponent = $loaded; lifecyclePod = $lifecycle; backendPod = $backend
        before = $before; after = $after
        stateRead = [ordered]@{ method = 'GET'; component = 'access-telemetry-store'; consistency = 'strong';
            syntheticKeyAbsent = $true; httpStatus = 204; responseBodyExported = $false
            startedAtUtc = $challengeStart.ToString('o'); completedAtUtc = $challengeEnd.ToString('o') }
        attribution = 'candidate-session-correlation-requires-independent-exclusive-window-verification'
    }
    $status = 'observed'
}
catch {
    $failure = [string]$_.Exception.Message
    if ($failure -cnotmatch '^[a-z0-9:-]+$') { $failure = 'linkage-collection-failed' }
    $blockers.Add($failure)
}
$packet = [ordered]@{
    schemaVersion = 'hexalith.access-telemetry.c1.linkage.evidence/v1'
    gate = $Gate; profileId = $ProfileId; profileSha256 = $profileSha256
    capturedAtUtc = $capturedAtUtc; context = $expectedContext; namespace = $namespace
    selectedLifecyclePod = $LifecyclePod; collectorStatus = $status
    gateStatus = 'not-evaluated'; connectionLinkage = 'not-evaluated'; componentBehavior = 'not-evaluated'
    productionLifecycleWrites = 'not-evaluated'; productionGatePassed = $false; independentDisposition = 'pending'
    scope = [ordered]@{ authorizationReference = $scope.authorizationReference; independentReviewer = $scope.independentReviewer
        authorizedFromUtc = $scope.authorizedFromUtc; authorizedUntilUtc = $scope.authorizedUntilUtc
        exclusiveReadWindowEvidenceSha256 = $scope.exclusiveReadWindowEvidenceSha256 }
    observations = $observations; blockers = @($blockers)
    sources = @($script:sourceLedger); commands = @($script:commandLedger)
}
Write-Output (Write-ImmutablePacket $packet $EvidenceDirectory -FilePrefix 'c1.16-connection-linkage-candidate')
if ($status -ne 'observed') { exit 1 }
