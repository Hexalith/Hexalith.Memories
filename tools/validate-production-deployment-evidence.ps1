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

    if ($null -eq $health.PSObject.Properties['transcript'] -or [string]::IsNullOrWhiteSpace([string]$health.transcript)) {
        throw "Health response evidence '$($file.Name)' does not contain a non-empty transcript."
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

# The disposable verifier stages TLS OpenBao and leaves the production hashicorp.vault
# secret stores unmodified (substitutionPerformed=false). Kubernetes-store substitution is
# rejected. A packet that omits or misstates the disclosure / OpenBao bootstrap record is invalid.
#
# PRESENCE is gated on a succeeded run; SHAPE is validated on both. The disclosure is written
# after OpenBao bootstrap and the verbatim apply, so REQUIRING it unconditionally made every
# honest earlier failure unvalidatable - and, because the CI validate step runs with
# `if: always()`, replaced the genuine terminal error with a message blaming a missing
# disclosure. A failed run that did get far enough to write one is held to the same structural
# contract, with only the 'was it verified' obligation relaxed.
#
# The file is REQUIRED on a succeeded run. Making it optional made its absence the off-switch
# for its own gate. Absence of the positive unmodified assertion is a real defect.
$substitutionPath = Join-Path $evidencePath 'secret-store-substitution.json'
$openBaoPinnedImage = 'quay.io/openbao/openbao:2.6.0@sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653'
$openBaoRequiredStages = @(
    'service-ready',
    'initialized',
    'unsealed',
    'kv-v2',
    'policies',
    'seeded',
    'scoped-tokens',
    'isolation-verified',
    'bootstrap-secrets',
    'root-revoked'
)

function Assert-OpenBaoBootstrapSucceededShape {
    param([Parameter(Mandatory)]$Bootstrap)

    if ($Bootstrap.schemaVersion -ne 1) {
        throw "Production deployment evidence OpenBao bootstrap disclosure declares schemaVersion '$($Bootstrap.schemaVersion)'; this validator only accepts schemaVersion 1."
    }
    if ($Bootstrap.skipVerify -isnot [bool] -or $Bootstrap.skipVerify) {
        throw 'Production deployment evidence OpenBao bootstrap disclosure must state skipVerify=false.'
    }
    if ($Bootstrap.tlsVerify -isnot [bool] -or -not $Bootstrap.tlsVerify) {
        throw 'Production deployment evidence OpenBao bootstrap disclosure must state tlsVerify=true.'
    }
    if ([string]$Bootstrap.endpoint -cnotlike 'https://hexalith-keys.openbao.svc.cluster.local:8200') {
        throw "Production deployment evidence OpenBao bootstrap disclosure endpoint '$($Bootstrap.endpoint)' is not the production TLS endpoint."
    }
    if ([string]$Bootstrap.image -cne $openBaoPinnedImage) {
        throw "Production deployment evidence OpenBao bootstrap disclosure image '$($Bootstrap.image)' is not the pinned OpenBao digest."
    }

    $stages = @($Bootstrap.stages | ForEach-Object { [string]$_ })
    $missingStages = @($openBaoRequiredStages | Where-Object { $stages -notcontains $_ })
    if ($missingStages.Count -gt 0) {
        throw "Production deployment evidence OpenBao bootstrap disclosure is missing required stage(s): [$($missingStages -join ', ')]."
    }
    # Require the succeeded sequence in order (extra intermediate stages are allowed).
    $cursor = 0
    foreach ($required in $openBaoRequiredStages) {
        $foundAt = -1
        for ($i = $cursor; $i -lt $stages.Count; $i++) {
            if ($stages[$i] -ceq $required) {
                $foundAt = $i
                break
            }
        }
        if ($foundAt -lt 0) {
            throw "Production deployment evidence OpenBao bootstrap disclosure stage sequence is missing '$required' in order."
        }
        $cursor = $foundAt + 1
    }

    $isolation = $Bootstrap.isolation
    if ($null -eq $isolation) {
        throw 'Production deployment evidence OpenBao bootstrap disclosure must include isolation results.'
    }
    foreach ($flag in @('runtimeAllowed', 'runtimeDeniedCrossPrefix', 'accessAllowed', 'accessDeniedCrossPrefix')) {
        $value = $isolation.$flag
        if ($value -isnot [bool] -or -not $value) {
            throw "Production deployment evidence OpenBao bootstrap disclosure must state isolation.$flag=true."
        }
    }
}

function Assert-OpenBaoBootstrapFailedRunClaims {
    param([Parameter(Mandatory)]$Bootstrap)

    # Failed packets may be incomplete, but must not assert contradictory false-success claims.
    if ($Bootstrap.skipVerify -is [bool] -and $Bootstrap.skipVerify) {
        throw 'Production deployment evidence OpenBao bootstrap disclosure on a failed run must not claim skipVerify=true.'
    }

    $isolation = $Bootstrap.isolation
    if ($null -eq $isolation) {
        return
    }

    $successFlags = @(
        ($isolation.runtimeAllowed -is [bool] -and $isolation.runtimeAllowed),
        ($isolation.runtimeDeniedCrossPrefix -is [bool] -and $isolation.runtimeDeniedCrossPrefix),
        ($isolation.accessAllowed -is [bool] -and $isolation.accessAllowed),
        ($isolation.accessDeniedCrossPrefix -is [bool] -and $isolation.accessDeniedCrossPrefix)
    )
    if (($successFlags | Where-Object { $_ }).Count -eq 4) {
        $stages = @($Bootstrap.stages | ForEach-Object { [string]$_ })
        if ($stages -notcontains 'isolation-verified' -or $stages -notcontains 'root-revoked') {
            throw 'Production deployment evidence OpenBao bootstrap disclosure on a failed run claims full isolation success without isolation-verified and root-revoked stages.'
        }
    }
}

function Assert-VerifiedVaultComponents {
    param([Parameter(Mandatory)]$Disclosure, [Parameter(Mandatory)][string]$RunOutcome)

    $verified = @($Disclosure.verifiedVaultComponents | Where-Object { $null -ne $_ })
    foreach ($name in @('secretstore', 'access-telemetry-secrets')) {
        $match = @($verified | Where-Object {
                [string]$_.name -ceq $name -and [string]$_.observedType -ceq 'secretstores.hashicorp.vault'
            })
        if ($match.Count -eq 0) {
            throw "Production deployment evidence secret-store substitution disclosure on a $RunOutcome unmodified run must name verifiedVaultComponents '$name' as secretstores.hashicorp.vault."
        }
    }
}

# ONE structural contract for both outcomes. The failed-run branch previously checked 2 of the 8
# fields, so a failed packet could assert substitutionPerformed=true naming components that were
# never touched, with an empty reason and any schemaVersion, and still validate clean - the exact
# class of false record the branch was added to close. Both call sites now run the same checks;
# only the "was it verified" obligation differs, because a failed run may legitimately record a
# substitution it could not verify.
function Assert-SubstitutionDisclosureShape {
    param([Parameter(Mandatory)]$Disclosure, [Parameter(Mandatory)][string]$RunOutcome)

    if ($Disclosure.schemaVersion -ne 2) {
        throw "Production deployment evidence secret-store substitution disclosure on a $RunOutcome run declares schemaVersion '$($Disclosure.schemaVersion)'; this validator only accepts schemaVersion 2."
    }
    if ($Disclosure.originalType -cne 'secretstores.hashicorp.vault' -or
        $Disclosure.substitutedType -cne 'secretstores.kubernetes') {
        throw "Production deployment evidence secret-store substitution disclosure on a $RunOutcome run does not match the declared verification-scoped substitution."
    }
    # The reason narrative is the field an auditor reads; an empty or absent one discloses
    # nothing while satisfying every structural check.
    if ([string]::IsNullOrWhiteSpace([string]$Disclosure.reason)) {
        throw "Production deployment evidence secret-store substitution disclosure on a $RunOutcome run must carry a non-empty reason."
    }
    # A string "false" is truthy in PowerShell, so a non-boolean here reads as a performed
    # substitution. Reject the type rather than coercing it.
    if ($Disclosure.substitutionPerformed -isnot [bool]) {
        throw "Production deployment evidence secret-store substitution disclosure on a $RunOutcome run must state substitutionPerformed as a boolean."
    }

    # Filter out $null before counting: a missing property yields a single-element $null array,
    # which would otherwise satisfy a bare Count check and pass the gate vacuously.
    $substitutedComponents = @($Disclosure.substitutedComponents | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    $observedComponents = @($Disclosure.observedComponents | Where-Object { $null -ne $_ })

    if ($Disclosure.substitutionPerformed) {
        if ($substitutedComponents.Count -eq 0) {
            throw "Production deployment evidence secret-store substitution disclosure on a $RunOutcome run claims a substitution but names no component."
        }
        # Bind CARDINALITY, not just set membership. `-notcontains` on both sides is a set
        # comparison, so a disclosure claiming three substitutions while observing two - or
        # repeating one name - produced empty missing/unexpected lists and validated clean.
        $duplicateNames = @($substitutedComponents | Group-Object | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name })
        if ($duplicateNames.Count -gt 0) {
            throw "Production deployment evidence secret-store substitution disclosure on a $RunOutcome run names the same component more than once: [$($duplicateNames -join ', ')]."
        }
        # Validate PER COMPONENT. Checking only that the set of observed types equals the
        # expected literal left the names unverified, so a disclosure naming a component that
        # was never touched - or omitting one that was - passed unchallenged.
        $observedNames = @($observedComponents | ForEach-Object { [string]$_.name })
        $missing = @($substitutedComponents | Where-Object { $observedNames -notcontains [string]$_ })
        $extra = @($observedNames | Where-Object { $substitutedComponents -notcontains [string]$_ })
        if ($missing.Count -gt 0 -or $extra.Count -gt 0) {
            throw "Production deployment evidence secret-store substitution disclosure on a $RunOutcome run does not observe exactly the components it claims to have substituted (missing: [$($missing -join ', ')]; unexpected: [$($extra -join ', ')])."
        }
        if ($observedNames.Count -ne $substitutedComponents.Count) {
            throw "Production deployment evidence secret-store substitution disclosure on a $RunOutcome run claims $($substitutedComponents.Count) substituted component(s) but observes $($observedNames.Count)."
        }
        # Scoped to a SUCCEEDED run (2026-07-31, Administrator decision arm (b)). A failed run now
        # records every substituted component with the type actually read back - including the wrong
        # type, and an explicit marker for one that vanished - because that is exactly the evidence
        # an auditor needs from a partially rewritten cluster, and AC6 requires the per-Component
        # readback on every run. Applying this rule to failed packets made the verifier's own honest
        # failure disclosure unvalidatable and, under the CI step's `if: always()`, replaced the
        # genuine terminal error with a complaint about the disclosure's shape.
        if ($RunOutcome -eq 'succeeded') {
            $wrongType = @($observedComponents | Where-Object { [string]$_.observedType -cne 'secretstores.kubernetes' })
            if ($wrongType.Count -gt 0) {
                throw "Production deployment evidence secret-store substitution disclosure on a $RunOutcome run records a component whose observed post-patch type is not secretstores.kubernetes: [$(($wrongType | ForEach-Object { "$($_.name)=$($_.observedType)" }) -join ', ')]."
            }
        }
    }
    else {
        # An unmodified run must assert emptiness rather than merely omitting the fields.
        if ($substitutedComponents.Count -gt 0 -or $observedComponents.Count -gt 0) {
            throw "Production deployment evidence secret-store substitution disclosure on a $RunOutcome run states substitutionPerformed=false while naming substituted components."
        }
    }

    # Cross-check the auditor-facing summary field against the per-component record. The verifier
    # writes observedPostPatchTypes into every packet and nothing validated it, so a packet could
    # contradict itself - all components recorded as secretstores.kubernetes while this field
    # asserted secretstores.hashicorp.vault.
    # Constrain the verification fields on BOTH outcomes. The failed branch runs only this shape
    # function, which never touched them, so a failed packet could assert
    # substitutionVerified: "definitely-not-a-boolean" - a truthy STRING - with an empty
    # verificationFailures while naming surviving vault-typed components, and validate clean. That
    # is the same false-record class the shared-shape refactor was written to close.
    if ($Disclosure.substitutionVerified -isnot [bool]) {
        throw "Production deployment evidence secret-store substitution disclosure on a $RunOutcome run must state substitutionVerified as a boolean."
    }
    $declaredFailures = @($Disclosure.verificationFailures | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    $declaredResidual = @($Disclosure.residualVaultComponents | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    # Scoped to a FAILED run. A succeeded run is already held to the stricter, better-worded rules
    # below (substitutionVerified must be true, verificationFailures and residualVaultComponents
    # must be empty); firing here first would replace those specific diagnostics with this generic
    # one for the case they exist to name.
    if ($RunOutcome -eq 'failed' -and $Disclosure.substitutionVerified -and ($declaredFailures.Count -gt 0 -or $declaredResidual.Count -gt 0)) {
        throw "Production deployment evidence secret-store substitution disclosure on a $RunOutcome run claims substitutionVerified=true while recording verification failures [$($declaredFailures -join '; ')] or surviving vault-typed components [$($declaredResidual -join ', ')]."
    }

    $expectedTypes = @($observedComponents | ForEach-Object { [string]$_.observedType } | Sort-Object -Unique)
    $declaredTypes = @($Disclosure.observedPostPatchTypes | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    if (@(Compare-Object -ReferenceObject $expectedTypes -DifferenceObject $declaredTypes -CaseSensitive).Count -gt 0) {
        throw "Production deployment evidence secret-store substitution disclosure on a $RunOutcome run declares observedPostPatchTypes [$($declaredTypes -join ', ')] which disagrees with its own per-component record [$($expectedTypes -join ', ')]."
    }

    return $substitutedComponents
}

# Presence rule restated at the call site above; see the gating comment there.
if ($result.status -eq 'succeeded') {
    if (-not (Test-Path -LiteralPath $substitutionPath -PathType Leaf)) {
        throw 'Production deployment evidence must disclose the verification-scoped secret-store substitution (secret-store-substitution.json is missing). A succeeded run always writes this record, including when no substitution was performed.'
    }
    $openBaoBootstrapPath = Join-Path $evidencePath 'openbao-bootstrap.json'
    if (-not (Test-Path -LiteralPath $openBaoBootstrapPath -PathType Leaf)) {
        throw 'Production deployment evidence must disclose disposable OpenBao bootstrap (openbao-bootstrap.json is missing).'
    }
    try {
        $openBaoBootstrap = (Get-Content -LiteralPath $openBaoBootstrapPath -Raw) | ConvertFrom-Json
    }
    catch {
        throw "Production deployment evidence OpenBao bootstrap disclosure is not parsable JSON: $($_.Exception.Message)"
    }
    Assert-OpenBaoBootstrapSucceededShape -Bootstrap $openBaoBootstrap
    try {
        $substitution = (Get-Content -LiteralPath $substitutionPath -Raw) | ConvertFrom-Json
    }
    catch {
        throw "Production deployment evidence secret-store substitution disclosure is not parsable JSON: $($_.Exception.Message)"
    }

    if ($substitution.substitutionPerformed -is [bool] -and $substitution.substitutionPerformed) {
        throw 'Succeeded production deployment evidence must not substitute OpenBao-backed secret stores with secretstores.kubernetes; disposable OpenBao staging is required.'
    }

    [void](Assert-SubstitutionDisclosureShape -Disclosure $substitution -RunOutcome 'succeeded')
    # A succeeded run must additionally have VERIFIED the unmodified vault stores. The verifier
    # writes the disclosure before raising, so a packet can legitimately record an unverified
    # confirmation - but only on a failed run.
    if ($substitution.substitutionVerified -isnot [bool] -or -not $substitution.substitutionVerified) {
        throw 'Production deployment evidence secret-store substitution disclosure on a succeeded run must state substitutionVerified=true.'
    }
    $verificationFailures = @($substitution.verificationFailures | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    if ($verificationFailures.Count -gt 0) {
        throw "Production deployment evidence secret-store substitution disclosure on a succeeded run records verification failures: [$($verificationFailures -join '; ')]."
    }
    $residualVault = @($substitution.residualVaultComponents | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    if ($residualVault.Count -gt 0) {
        throw "Production deployment evidence secret-store substitution disclosure on a succeeded unmodified run records unexpected residual vault-typed components: [$($residualVault -join ', ')]."
    }

    if (-not $substitution.substitutionPerformed) {
        Assert-VerifiedVaultComponents -Disclosure $substitution -RunOutcome 'succeeded'
        Write-Host 'Secret-store substitution disclosure records substitutionPerformed=false; the run applied the production secret stores unmodified.'
    }
}
else {
    $openBaoBootstrapPath = Join-Path $evidencePath 'openbao-bootstrap.json'
    if (Test-Path -LiteralPath $openBaoBootstrapPath -PathType Leaf) {
        try {
            $failedOpenBaoBootstrap = (Get-Content -LiteralPath $openBaoBootstrapPath -Raw) | ConvertFrom-Json
        }
        catch {
            throw "Production deployment evidence OpenBao bootstrap disclosure is not parsable JSON: $($_.Exception.Message)"
        }
        Assert-OpenBaoBootstrapFailedRunClaims -Bootstrap $failedOpenBaoBootstrap
    }

    if (Test-Path -LiteralPath $substitutionPath -PathType Leaf) {
        # A failed run may legitimately carry a disclosure: the verifier writes it before raising.
        # Validate the SAME shape as a succeeded run - only substitutionVerified may be false here.
        try {
            $failedSubstitution = (Get-Content -LiteralPath $substitutionPath -Raw) | ConvertFrom-Json
        }
        catch {
            throw "Production deployment evidence secret-store substitution disclosure is not parsable JSON: $($_.Exception.Message)"
        }
        [void](Assert-SubstitutionDisclosureShape -Disclosure $failedSubstitution -RunOutcome 'failed')
    }
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
    'verification-access-telemetry-marker',
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
