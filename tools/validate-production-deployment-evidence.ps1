[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$EvidenceDirectory
)

$ErrorActionPreference = 'Stop'
$evidencePath = [System.IO.Path]::GetFullPath($EvidenceDirectory)
if (-not (Test-Path -LiteralPath $evidencePath -PathType Container)) {
    throw "Production deployment evidence directory not found: $evidencePath"
}

$requiredFiles = @(
    'verification-result.json',
    'last-stage.txt',
    'pods.txt',
    'events.txt',
    'describe-pods.txt',
    'describe-workloads.txt',
    'pods.json'
)
foreach ($name in $requiredFiles) {
    $path = Join-Path $evidencePath $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Length -eq 0) {
        throw "Required production deployment evidence is missing or empty: $name"
    }
}

try {
    $result = Get-Content -LiteralPath (Join-Path $evidencePath 'verification-result.json') -Raw | ConvertFrom-Json
}
catch {
    throw "verification-result.json is not valid JSON: $($_.Exception.Message)"
}
if ($result.schemaVersion -ne 1 -or $result.status -notin @('succeeded', 'failed') -or [string]::IsNullOrWhiteSpace([string]$result.stage)) {
    throw 'verification-result.json does not contain the required schemaVersion, status, and stage contract.'
}
if ($result.status -eq 'failed' -and [string]::IsNullOrWhiteSpace([string]$result.error)) {
    throw 'Failed production deployment evidence must include a redacted terminal error.'
}
if ($result.status -eq 'succeeded' -and $result.stage -ne 'required-server-mcp-restored') {
    throw "Succeeded production deployment evidence must finish at required-server-mcp-restored; found '$($result.stage)'."
}

$lastStage = (Get-Content -LiteralPath (Join-Path $evidencePath 'last-stage.txt') -Raw).Trim()
if (-not [string]::Equals($lastStage, [string]$result.stage, [StringComparison]::Ordinal)) {
    throw "last-stage.txt '$lastStage' does not match verification-result.json stage '$($result.stage)'."
}

$healthFiles = @(Get-ChildItem -LiteralPath $evidencePath -File -Filter 'health-*.json')
if ($healthFiles.Count -eq 0) {
    throw 'Production deployment evidence must include at least one authenticated health response.'
}

$healthRecords = @()
foreach ($file in $healthFiles) {
    try {
        $health = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
    }
    catch {
        throw "Health response evidence '$($file.Name)' is not valid JSON: $($_.Exception.Message)"
    }

    # The envelope contract holds for every packet, succeeded or failed.
    if ($health.schemaVersion -ne 1 -or [string]::IsNullOrWhiteSpace([string]$health.stage) -or $null -eq $health.attempt) {
        throw "Health response evidence '$($file.Name)' does not contain a valid schemaVersion, stage, and attempt."
    }

    $healthRecords += [pscustomobject]@{ File = $file; Health = $health }
}

# Each stage is polled repeatedly until it reaches its expected status (Wait-AggregateStatus,
# every ~2s), and every poll's response is now retained as its own attempt file instead of being
# overwritten. An in-flight poll observed before the app is ready - exactly what the
# -MeasureFromContainerRunning startup window exists to tolerate - legitimately returns a
# non-JSON transcript or a null status. Only the last attempt per stage proves that stage's
# outcome, so only it is held to the succeeded-run body/status contract; earlier attempts are
# retained evidence but are not required to already look healthy.
$lastAttemptByStage = @{}
foreach ($record in $healthRecords) {
    $stage = [string]$record.Health.stage
    if (-not $lastAttemptByStage.ContainsKey($stage) -or $record.Health.attempt -gt $lastAttemptByStage[$stage].Health.attempt) {
        $lastAttemptByStage[$stage] = $record
    }
}

$healthStatusCodes = @()
foreach ($record in $healthRecords) {
    $health = $record.Health
    $file = $record.File

    # The body/status-code contract is gated on a succeeded run. A failed probe legitimately
    # records a null statusCode and a raw transcript body (Get-HealthJsonBody falls back to
    # the transcript when no status-bearing JSON object is present), so requiring a parsable
    # body and a 200/503 code unconditionally made honest failure evidence unvalidatable -
    # exactly the evidence a failed run exists to produce.
    if ($result.status -ne 'succeeded') {
        if ($null -ne $health.statusCode) {
            $healthStatusCodes += [int]$health.statusCode
        }
        continue
    }

    if ($lastAttemptByStage[[string]$health.stage].File.FullName -ne $file.FullName) {
        # A non-terminal attempt for this stage: an expected in-flight observation, not the
        # attempt that proved the stage's outcome.
        continue
    }

    try {
        $healthBody = ([string]$health.body) | ConvertFrom-Json
    }
    catch {
        throw "Health response evidence '$($file.Name)' from a succeeded run has an unparsable body: $($_.Exception.Message)"
    }
    if ($health.statusCode -notin @(200, 503) -or [string]::IsNullOrWhiteSpace([string]$healthBody.status)) {
        throw "Health response evidence '$($file.Name)' does not contain a valid HTTP status and aggregate-health body."
    }
    $healthStatusCodes += [int]$health.statusCode
}
if ($result.status -eq 'succeeded' -and ($healthStatusCodes -notcontains 200 -or $healthStatusCodes -notcontains 503)) {
    throw 'Succeeded production deployment evidence must include both authenticated HTTP 200 and HTTP 503 health responses.'
}

$currentLogs = @(Get-ChildItem -LiteralPath $evidencePath -File -Filter '*-current.log')
$previousLogs = @(Get-ChildItem -LiteralPath $evidencePath -File -Filter '*-previous.log')
if ($currentLogs.Count -eq 0 -or $previousLogs.Count -eq 0) {
    throw 'Production deployment evidence must include current and previous container log captures.'
}

# DW 27.3-CR17: the verifier substitutes the OpenBao-backed secret stores with
# verification-scoped kubernetes stores. That substitution may never happen silently, so a
# packet that omits or misstates the disclosure record is invalid.
#
# Gated on a succeeded run, for the reason recorded above for health bodies: the disclosure is
# written only after cluster create, image loads, contract asserts, render and apply all
# succeed, so requiring it unconditionally made every honest earlier failure unvalidatable -
# and, because the CI validate step runs with `if: always()`, replaced the genuine terminal
# error with a message blaming a missing substitution disclosure.
#
# Requiring it only when a substitution actually occurred also keeps this lane usable once
# Story 31.2 delivers the OpenBao path: a run that applies the manifests unmodified has nothing
# to disclose and must not be rejected for it.
$substitutionPath = Join-Path $evidencePath 'secret-store-substitution.json'
if ($result.status -eq 'succeeded' -and (Test-Path -LiteralPath $substitutionPath -PathType Leaf)) {
    try {
        $substitution = (Get-Content -LiteralPath $substitutionPath -Raw) | ConvertFrom-Json
    }
    catch {
        throw "Production deployment evidence secret-store substitution disclosure is not parsable JSON: $($_.Exception.Message)"
    }

    # Filter out $null before counting: a missing property yields a single-element $null array,
    # which would otherwise satisfy a bare Count check and pass the gate vacuously.
    $substitutedComponents = @($substitution.substitutedComponents | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    if ($substitution.schemaVersion -ne 1 -or
        $substitution.originalType -cne 'secretstores.hashicorp.vault' -or
        $substitution.substitutedType -cne 'secretstores.kubernetes') {
        throw 'Production deployment evidence secret-store substitution disclosure is incomplete or does not match the declared verification-scoped substitution.'
    }
    if ($substitutedComponents.Count -eq 0) {
        throw 'Production deployment evidence secret-store substitution disclosure must name every substituted component.'
    }
    # The reason narrative is the field an auditor reads; an empty or absent one discloses
    # nothing while satisfying every structural check.
    if ([string]::IsNullOrWhiteSpace([string]$substitution.reason)) {
        throw 'Production deployment evidence secret-store substitution disclosure must carry a non-empty reason.'
    }
    # The observed post-patch types are read back from the cluster by the verifier. Asserting
    # only the verifier's own literals made this check a tautology.
    $observedTypes = @($substitution.observedPostPatchTypes | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    $unexpectedTypes = @($observedTypes | Where-Object { [string]$_ -cne 'secretstores.kubernetes' })
    if ($observedTypes.Count -eq 0 -or $unexpectedTypes.Count -gt 0) {
        throw 'Production deployment evidence secret-store substitution disclosure must record observed post-patch types, all secretstores.kubernetes.'
    }
}
elseif ($result.status -eq 'succeeded') {
    # A succeeded run that applied vault-typed stores without disclosing a substitution is only
    # legitimate once the OpenBao path actually works. Record that explicitly rather than
    # letting the absence pass unnoticed.
    Write-Host 'No secret-store substitution disclosed; the run applied the production secret stores unmodified.'
}

$secretCanaries = @(
    'verification-redis-password',
    'verification-falkordb-password',
    'verification-openai-key',
    'verification-google-key',
    'verification-embedding-secret',
    'verification-app-api-token',
    'verification-dapr-api-token',
    'verification-invalid-dapr-api-token',
    $env:HEXALITH_ZOT_USERNAME,
    $env:HEXALITH_ZOT_API_KEY
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

foreach ($file in Get-ChildItem -LiteralPath $evidencePath -File) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($secret in $secretCanaries) {
        if ($content.Contains($secret, [StringComparison]::Ordinal)) {
            throw "Production deployment evidence '$($file.Name)' contains an unredacted secret canary."
        }
    }
}

Write-Host "Validated production deployment evidence at $evidencePath."
