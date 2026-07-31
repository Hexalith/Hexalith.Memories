[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ServerArchive,

    [Parameter(Mandatory)]
    [string]$McpArchive,

    [Parameter(Mandatory)]
    [string]$AccessTelemetryArchive,

    [Parameter(Mandatory)]
    [string]$AccessTelemetryClockArchive,

    [Parameter(Mandatory)]
    [string]$Version,

    [string]$ClusterName = "hexalith-memories-verification",

    [string]$DaprRuntimeVersion = "1.18.1",

    [string]$KindNodeImage = "kindest/node:v1.35.0",

    [string]$EvidenceDirectory = "artifacts/production-deployment-verification",

    [switch]$KeepCluster
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
# Health-response parsing lives in its own dot-sourceable file so it can be exercised
# directly by tests/tooling/production_deployment_evidence with real transcripts.
. (Join-Path $PSScriptRoot 'production-deployment-health.ps1')
$namespace = 'hexalith-memories'
$serverImage = "registry.hexalith.com/memories:$Version"
$mcpImage = "registry.hexalith.com/memories-mcp:$Version"
$accessTelemetryImage = "registry.hexalith.com/memories-access-telemetry:$Version"
$accessTelemetryClockImage = "registry.hexalith.com/memories-access-telemetry-clock:$Version"
$manifestPath = Join-Path ([System.IO.Path]::GetTempPath()) "hexalith-memories-production-$Version.yaml"
$kubeconfigPath = Join-Path ([System.IO.Path]::GetTempPath()) "hexalith-memories-kubeconfig-$([Guid]::NewGuid().ToString('N'))"
$originalKubeconfig = $env:KUBECONFIG
$clusterCreated = $false
$healthEvidenceAttempts = @{}
$daprTokenFaultInjected = $false
$originalMemoriesDeploymentState = $null
$memoriesDeploymentStateChanged = $false
$verificationError = $null
$verificationSucceeded = $false
$verificationStage = 'preflight'
$evidencePath = if ([System.IO.Path]::IsPathRooted($EvidenceDirectory)) {
    $EvidenceDirectory
}
else {
    Join-Path $repoRoot $EvidenceDirectory
}
New-Item -ItemType Directory -Path $evidencePath -Force | Out-Null
$ownedEvidenceNames = @(
    'verification-result.json',
    # Without this entry a reused evidence directory let a run that died before the
    # substitution inherit the previous run's disclosure and pass the validator's gate on a
    # substitution it never performed.
    'secret-store-substitution.json',
    'last-stage.txt',
    'pods.txt',
    'events.txt',
    'describe-pods.txt',
    'describe-workloads.txt',
    'pods.json',
    'logs-enumeration-error.txt'
)
Get-ChildItem -LiteralPath $evidencePath -File -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Name -in $ownedEvidenceNames -or
        $_.Name -like '*-current.log' -or
        $_.Name -like '*-previous.log' -or
        $_.Name -like 'health-*.json'
    } |
    Remove-Item -Force

function Assert-DisposableClusterContext {
    param([Parameter(Mandatory)][string]$ClusterName)

    # Extracted from the substitution block so it can be executed by a test. Inverting this
    # comparison in place previously left the whole suite green, and the mutation inverts the
    # safety property exactly: the verifier would refuse on the disposable cluster and proceed
    # to rewrite Dapr Component spec.types on any OTHER context.
    #
    # Read the context WITHOUT merging stderr. Invoke-Checked folds stderr into its output, so a
    # single kubectl deprecation/warning line would concatenate onto the context name and make
    # this exact-equality guard refuse a valid disposable cluster - aborting the lane after the
    # manifests were already applied. Same hazard the RBAC probe defends against with 2>$null.
    $activeContext = (& kubectl config current-context 2>$null) -join ''
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($activeContext)) {
        throw 'Refusing to substitute secret stores: could not read the active kubectl context.'
    }
    # Case-SENSITIVE. kubectl context names are case-sensitive, so `KIND-hexalith-...` is a
    # genuinely different context that `-ne` admitted - and this is the sole barrier before the
    # script rewrites Dapr Component spec.types. Every other exactness check in this file uses -cne.
    if ($activeContext.Trim() -cne "kind-$ClusterName") {
        throw "Refusing to substitute secret stores: active kubectl context is '$($activeContext.Trim())', not the disposable cluster 'kind-$ClusterName'."
    }
}

function Invoke-SecretStoreSubstitution {
    # Extracted 2026-07-31 by code review so the substitution and its disclosure can be EXECUTED
    # by a test. While this block was inline, seven independent mutations of it survived the whole
    # suite - the disclosure write, the performed flag, both post-patch checks, the
    # observedPostPatchTypes field, and the narrative - because its only coverage was source-text
    # assertIn over the file. Extraction alone is not coverage: the call site is pinned separately,
    # because moving Assert-DisposableClusterContext into a function is exactly what let its
    # invocation be deleted while the pinned string still matched inside the function body.
    param(
        [Parameter(Mandatory)][string]$Namespace,
        [Parameter(Mandatory)][string]$ClusterName,
        [Parameter(Mandatory)][string]$DisclosurePath
    )

    Assert-DisposableClusterContext $ClusterName

    # Enumerate the vault-typed stores from the cluster rather than hardcoding two names, so a
    # third such component cannot be silently left unpatched while the disclosure still passes.
    $componentsJson = (Invoke-Checked kubectl @('get', 'components.dapr.io', '-n', $Namespace, '-o', 'json')) -join [Environment]::NewLine
    try {
        $componentList = $componentsJson | ConvertFrom-Json
    }
    catch {
        throw "Could not parse the Dapr Component list while preparing the secret-store substitution. kubectl exited 0 but its output was not JSON (a warning line on stderr is folded in by Invoke-Checked). Parser error: $($_.Exception.Message)"
    }

    # A FAILED enumeration must not look like the by-design "nothing to substitute" end state.
    # kubectl can exit 0 with a payload carrying no `items` at all (a `Status` object, for
    # example); `@($payload.items)` then yields a one-element array holding $null, `$null.spec.type`
    # is $null, and the vault-typed set came out empty - so a render/apply/namespace regression was
    # recorded as `substitutionPerformed=false` and the validator printed "the run applied the
    # production secret stores unmodified" over a cluster nothing had verified.
    if ($null -eq $componentList.PSObject.Properties['items']) {
        throw "Could not enumerate Dapr Components while preparing the secret-store substitution: kubectl exited 0 but its JSON payload carried no 'items' property, so an enumeration failure is indistinguishable from an empty result."
    }
    $componentItems = @($componentList.items | Where-Object { $null -ne $_ })
    if ($componentItems.Count -eq 0) {
        throw "Could not enumerate Dapr Components while preparing the secret-store substitution: namespace '$Namespace' reports zero Dapr Components after the verbatim apply, which is a render or apply regression rather than the Story 31.2 end state (that state has components, none of them vault-typed)."
    }
    $vaultComponents = @($componentItems |
        Where-Object { $_.spec.type -eq 'secretstores.hashicorp.vault' } |
        ForEach-Object { [string]$_.metadata.name } |
        Sort-Object)
    # No throw when nothing is vault-typed. Story 31.2 delivers the real OpenBao path, after
    # which a run applies the production manifests unmodified and has nothing to substitute -
    # the desired end state, and the state DW 27.3-CR29/CR30 must be able to discharge. Making
    # the substitution unconditional would have made AC6's own lane permanently incapable of
    # proving the Production secret-resolution path it exists to qualify.
    $secretStoreSubstitution = '{"spec":{"type":"secretstores.kubernetes","version":"v1","metadata":[]}}'
    foreach ($component in $vaultComponents) {
        Invoke-Checked kubectl @('patch', 'component', $component, '-n', $Namespace, '--type', 'merge', '-p', $secretStoreSubstitution) | Out-Null
    }

    # Re-read the observed post-patch type PER COMPONENT. `Invoke-Checked` proves only that
    # kubectl exited 0 - a no-op patch also exits 0 - so an asserted disclosure could never
    # disagree with the cluster. A set-of-types readback (Sort-Object -Unique) was not enough
    # either: it collapses N components to one entry, so a component that disappeared between
    # the patch loop and the readback still yielded a clean single type while the disclosure
    # claimed every component was substituted.
    # COLLECT verification failures instead of throwing on the spot. Every throw between the patch
    # loop and the disclosure write left a cluster whose Component spec.types had already been
    # rewritten while `secret-store-substitution.json` was never written at all - and the validator
    # skips its disclosure branch entirely when the file is absent, so the packet validated clean
    # over a silently mutated cluster. The disclosure is now written on every path and the failure
    # is raised afterwards.
    $substitutionFailures = [System.Collections.Generic.List[string]]::new()
    $observedItems = @()
    $observedByName = @{}
    $observedJson = (Invoke-Checked kubectl @('get', 'components.dapr.io', '-n', $Namespace, '-o', 'json')) -join [Environment]::NewLine
    try {
        $observedPayload = $observedJson | ConvertFrom-Json
        if ($null -eq $observedPayload.PSObject.Properties['items']) {
            $substitutionFailures.Add('the post-patch Dapr Component list carried no items property')
        }
        else {
            $observedItems = @($observedPayload.items | Where-Object { $null -ne $_ })
        }
    }
    catch {
        $substitutionFailures.Add("the post-patch Dapr Component list could not be parsed (parser error: $($_.Exception.Message))")
    }
    foreach ($item in $observedItems) {
        $observedByName[[string]$item.metadata.name] = [string]$item.spec.type
    }
    $observedComponents = @()
    foreach ($component in $vaultComponents) {
        if (-not $observedByName.ContainsKey($component)) {
            $substitutionFailures.Add("component '$component' was patched but is absent from the post-patch read")
            continue
        }
        if ($observedByName[$component] -cne 'secretstores.kubernetes') {
            $substitutionFailures.Add("component '$component' observed post-patch type '$($observedByName[$component])'")
            continue
        }
        $observedComponents += [ordered]@{ name = $component; observedType = $observedByName[$component] }
    }

    # Scan the WHOLE post-patch list for a surviving vault-typed component, not only the names this
    # run enumerated before patching. A component the pre-patch read missed (informer lag right
    # after the apply) or one a controller re-created between the patch loop and the readback was
    # never examined, so the run succeeded and the disclosure asserted it named "exactly the
    # components observed and patched in this run" while a vault-typed store reached the health stages.
    $residualVaultComponents = @($observedItems |
        Where-Object { $_.spec.type -eq 'secretstores.hashicorp.vault' } |
        ForEach-Object { [string]$_.metadata.name } |
        Sort-Object)
    if ($residualVaultComponents.Count -gt 0) {
        $substitutionFailures.Add("vault-typed component(s) survived the substitution: $($residualVaultComponents -join ', ')")
    }

    # ALWAYS write the disclosure, including for a run that substituted nothing. Making the file
    # optional made its absence indistinguishable from a substituted run whose disclosure was
    # never written or was lost from the packet, and the validator then printed an unverified
    # claim about production secret-store topology. An explicit substituted:false record is a
    # positive assertion the validator can check; a missing file is not.
    $substitutionPerformed = $vaultComponents.Count -gt 0
    $substitutionReason = if ($substitutionPerformed) {
        # Build the narrative from the components actually enumerated. A hardcoded two-component
        # story re-introduced the staleness that dynamic enumeration was added to remove: a third
        # vault-typed component would be correctly patched and listed while the auditor-facing
        # narrative kept describing a topology that no longer existed, and the validator only
        # checks that this field is non-empty.
        "DW 27.3-CR17: the disposable cluster has no OpenBao, so the $($vaultComponents.Count) production hashicorp.vault secret store(s) applied by this run - $($vaultComponents -join ', ') - are substituted with verification-scoped kubernetes stores after the verbatim apply. Consumers whose secretKeyRefs name a seeded verification Secret resolve through the substituted store; a consumer naming a Secret this lane does not seed cannot resolve it, and any such component must stay at replicas 0 or the health stages will observe its component-load failure. Concretely, and restored 2026-07-31 after the dynamic rewrite dropped it: `access-telemetry-store` resolves its password through Secret `access-telemetry-postgresql` via the `memories-access-telemetry-secret-reader` RBAC binding, which this lane does not create, so that component must remain at replicas 0 - the auditor otherwise cannot tell which component is at risk, and the validator only checks that this field is non-empty. Components are enumerated from the cluster, so this record names exactly the components observed and patched in this run. The OpenBao path is not exercised by this lane (Story 31.2 scope)."
    }
    else {
        'No secretstores.hashicorp.vault component was applied, so no substitution was performed and the production secret stores ran unmodified. Expected once Story 31.2 delivers the OpenBao path; before that, this state means the rendered manifests no longer carry the production vault-typed stores and AC6 no longer describes the applied topology.'
    }

    [ordered]@{
        schemaVersion = 2
        substitutionPerformed = $substitutionPerformed
        reason = $substitutionReason
        substitutedComponents = $vaultComponents
        observedComponents = $observedComponents
        originalType = 'secretstores.hashicorp.vault'
        substitutedType = 'secretstores.kubernetes'
        observedPostPatchTypes = @($observedComponents | ForEach-Object { $_.observedType } | Sort-Object -Unique)
        substitutionVerified = ($substitutionFailures.Count -eq 0)
        verificationFailures = @($substitutionFailures)
        residualVaultComponents = $residualVaultComponents
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $DisclosurePath -Encoding utf8

    # Raise AFTER the disclosure has reached disk, so a partially rewritten cluster is always
    # accompanied by a packet that says so.
    if ($substitutionFailures.Count -gt 0) {
        throw "Secret-store substitution could not be verified; the cluster may be partially rewritten and secret-store-substitution.json records this failure. $($substitutionFailures -join '; ')."
    }

    if ($substitutionPerformed) {
        Write-Host "Substituted secret stores $($vaultComponents -join ', ') with verification-scoped secretstores.kubernetes aliases (disclosed in secret-store-substitution.json)."
    }
    else {
        Write-Host 'No secretstores.hashicorp.vault component found; recorded substitutionPerformed=false in secret-store-substitution.json.'
    }
}

function Protect-EvidenceText {
    param([AllowEmptyString()][string]$Text)

    $sanitized = $Text
    $secrets = @(
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
    )
    foreach ($secret in $secrets) {
        if (-not [string]::IsNullOrWhiteSpace($secret)) {
            $sanitized = $sanitized.Replace($secret, '***', [StringComparison]::Ordinal)
        }
    }

    return $sanitized
}

function Set-VerificationStage {
    param([Parameter(Mandatory)][string]$Stage)

    $script:verificationStage = $Stage
    $Stage | Set-Content -LiteralPath (Join-Path $evidencePath 'last-stage.txt') -Encoding utf8
    Write-Host "[$Stage]"
}

function Invoke-Checked {
    param([Parameter(Mandatory)][string]$File, [Parameter(ValueFromRemainingArguments)][string[]]$Arguments)

    $output = @(& $File @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "$File $($Arguments -join ' ') failed with exit code $LASTEXITCODE`n$($output -join [Environment]::NewLine)"
    }

    return $output
}

function Apply-GeneratedSecret {
    param([Parameter(Mandatory)][string]$Name, [Parameter(Mandatory)][string[]]$Literals)

    $arguments = @('create', 'secret', 'generic', $Name, '--namespace', $namespace, '--dry-run=client', '-o', 'yaml')
    foreach ($literal in $Literals) {
        $arguments += "--from-literal=$literal"
    }

    $yaml = Invoke-Checked kubectl $arguments
    $yaml | & kubectl apply -f - | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to apply generated Secret/$Name."
    }
}

function Get-RunningContainerObservation {
    param(
        [Parameter(Mandatory)][string]$AppName,
        [Parameter(Mandatory)][string]$Container,
        [string]$RequiredAnnotationName = '',
        [string]$RequiredAnnotationValue = ''
    )

    $json = @(& kubectl --request-timeout=15s get pods -n $namespace -l "app.kubernetes.io/name=$AppName" -o json 2>$null) -join [Environment]::NewLine
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($json)) {
        return $null
    }

    try {
        $items = @(($json | ConvertFrom-Json).items)
    }
    catch {
        return $null
    }

    $pod = @($items |
        Where-Object {
            $annotationMatches = if ([string]::IsNullOrWhiteSpace($RequiredAnnotationName)) {
                $true
            }
            elseif ($null -eq $_.metadata.annotations) {
                $false
            }
            else {
                $property = $_.metadata.annotations.PSObject.Properties[$RequiredAnnotationName]
                $null -ne $property -and [string]$property.Value -eq $RequiredAnnotationValue
            }

            $null -eq $_.metadata.deletionTimestamp -and
            $_.status.phase -eq 'Running' -and
            $annotationMatches -and
            @($_.status.containerStatuses | Where-Object {
                    $_.name -eq $Container -and $null -ne $_.state.running
                }).Count -eq 1
        } |
        Sort-Object { [DateTime]$_.metadata.creationTimestamp } -Descending |
        Select-Object -First 1)
    if ($pod.Count -eq 0) {
        return $null
    }

    $containerStatus = @($pod[0].status.containerStatuses |
        Where-Object { $_.name -eq $Container -and $null -ne $_.state.running })[0]
    $startedAt = if ($null -eq $containerStatus.state.running.startedAt) {
        $null
    }
    else {
        ([DateTime]$containerStatus.state.running.startedAt).ToUniversalTime()
    }
    $readyCondition = @($pod[0].status.conditions |
        Where-Object { $_.type -eq 'Ready' -and $_.status -eq 'True' } |
        Select-Object -First 1)
    $readyAt = if ($readyCondition.Count -eq 0 -or $null -eq $readyCondition[0].lastTransitionTime) {
        $null
    }
    else {
        ([DateTime]$readyCondition[0].lastTransitionTime).ToUniversalTime()
    }

    return [pscustomobject]@{
        PodName = [string]$pod[0].metadata.name
        ContainerStartedAt = $startedAt
        ReadyAt = $readyAt
    }
}

function Get-RunningPodName {
    param(
        [Parameter(Mandatory)][string]$AppName,
        [Parameter(Mandatory)][string]$Container,
        [string]$RequiredAnnotationName = '',
        [string]$RequiredAnnotationValue = ''
    )

    $observation = Get-RunningContainerObservation @PSBoundParameters
    if ($null -eq $observation) {
        return ''
    }

    return [string]$observation.PodName
}

function Get-FreeLoopbackPort {
    # A fixed port let a leaked forward from a previous poll answer the next one - plausibly
    # from a pod deleted during fault injection - and be recorded as this pod's health. Bind
    # port 0 and let the OS pick, so a leaked forward or a concurrent run cannot collide.
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    try {
        $listener.Start()
        return [int]$listener.LocalEndpoint.Port
    }
    finally {
        $listener.Stop()
    }
}

function Get-CapturedProcessText {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return ''
    }

    # Distinguish "unreadable" from "empty". The still-running kubectl port-forward holds this
    # file open, so the read can fail while the file genuinely has content; returning '' for
    # both dropped the bind/upgrade error that explains the failed capture, with no marker.
    $captureError = $null
    $captured = Get-Content -LiteralPath $Path -Raw -ErrorAction SilentlyContinue -ErrorVariable captureError
    if ($null -eq $captured) {
        if ($captureError) {
            return "<unreadable: $($captureError[0].Exception.Message)>"
        }
        return ''
    }

    return ([string]$captured).Trim()
}

function Get-PodApplicationToken {
    param([Parameter(Mandatory)][string]$Pod, [Parameter(Mandatory)][string]$Container)

    # Read the pod's own APP_API_TOKEN instead of repeating the seeded literal. A hardcoded
    # copy made the app-token authentication contract unfalsifiable by this lane: a pod whose
    # token was wrong or unmounted answered 401 in-container, the runner-side probe then
    # presented the correct literal, and the stage passed on the substitute credential.
    # Discard stderr rather than merging it: kubectl exits 0 while writing deprecation and
    # warning lines to stderr, and `-join ''` would concatenate them onto the token, yielding a
    # corrupt dapr-api-token whose 401 the caller's @(200, 503) allowlist then discards - so a
    # valid cluster looks like a pod that never answered. Same reason the RBAC probe uses 2>$null.
    # --request-timeout bounds this call like every other kubectl invocation in the per-poll
    # path. Without it this exec was the one unbounded term in the poll budget, so a wedged API
    # server could stall a poll indefinitely while ProbeTimeoutBudgetTests still reported the
    # worst case as bounded.
    $token = (& kubectl --request-timeout=15s exec -n $namespace $Pod -c $Container -- /bin/sh -c 'printf %s "${APP_API_TOKEN}"' 2>$null) -join ''
    if ($LASTEXITCODE -ne 0) {
        return '<unavailable: kubectl exec failed>'
    }

    # An unset or empty APP_API_TOKEN makes `printf %s ""` write nothing and exit 0, which was
    # indistinguishable from the exec-failed path: both returned ''. The caller then sent an empty
    # dapr-api-token, and the resulting 401 was discarded by its @(200, 503) allowlist, so the
    # packet recorded neither the empty token nor the dropped probe. Every branch emits a marker.
    if ([string]::IsNullOrEmpty($token)) {
        return '<unavailable: APP_API_TOKEN is unset or empty in the container>'
    }

    return $token
}

function Get-HealthResponseViaPortForward {
    param([Parameter(Mandatory)][string]$Pod, [Parameter(Mandatory)][string]$Container)

    # Runner-side last-resort body capture. BusyBox wget discards HTTP-error bodies, and the
    # in-container netcat fallback returned zero bytes with exit 0 on every kind-cluster
    # redis-fault poll of CI run 30402973401 while the same binaries capture delayed 503
    # bodies in every local-docker and live-cluster reproduction (cause unresolved; the
    # in-container branch is instrumented with markers so its behavior is recorded). GNU curl
    # on the runner always returns HTTP-error bodies, so the aggregate JSON the stage contract
    # requires is captured through a short-lived pod port-forward. Trade-off, disclosed: this
    # path traverses the API server rather than the pod's own loopback, so it proves the
    # endpoint's response, not the image-native client path the primary probe exercises.
    #
    # Every branch emits a marker, for the same reason the in-container fallback is
    # instrumented: a silent transcript cannot be told apart from a branch that never ran.
    $localPort = Get-FreeLoopbackPort
    $forward = $null
    $stdoutPath = Join-Path ([System.IO.Path]::GetTempPath()) ('pf-{0}.out' -f [guid]::NewGuid().ToString('N'))
    $stderrPath = [System.IO.Path]::ChangeExtension($stdoutPath, 'err')
    $transcript = [System.Collections.Generic.List[string]]::new()
    $transcript.Add("port-forward begin (pod $Pod, local port $localPort)")
    try {
        $forward = Start-Process -FilePath kubectl -ArgumentList @(
            'port-forward', '-n', $namespace, "pod/$Pod", "${localPort}:8080") `
            -PassThru -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath

        # Wait for the listener explicitly. The previous connection-refused retry flag masked
        # the "nothing is listening" case by retrying it, making a failed bind
        # indistinguishable from a slow one. Bounded at 2s so the capture stays inside its
        # documented budget.
        $established = $false
        $establishDeadline = [DateTime]::UtcNow.AddSeconds(2)
        while ([DateTime]::UtcNow -lt $establishDeadline -and -not $forward.HasExited) {
            $probe = [System.Net.Sockets.TcpClient]::new()
            try {
                if ($probe.ConnectAsync([System.Net.IPAddress]::Loopback, $localPort).Wait(200)) {
                    $established = $true
                    break
                }
            }
            catch {
                # Not listening yet; retry until the establish deadline.
            }
            finally {
                $probe.Dispose()
            }
            Start-Sleep -Milliseconds 100
        }

        if ($established) {
            # One request, no --retry: curl treats HTTP 503 as transient, so retrying would discard
            # the exact response the fault stages exist to capture and multiply the persisted
            # transcript. --max-time 6 keeps this capture inside its documented budget.
            $token = Get-PodApplicationToken $Pod $Container
            $curlOutput = @(& curl -sS -D - --max-time 6 `
                -H "dapr-api-token: $token" "http://127.0.0.1:${localPort}/ready" 2>&1)
            $curlExit = $LASTEXITCODE
            foreach ($line in $curlOutput) {
                $transcript.Add([string]$line)
            }
            $transcript.Add("port-forward end (curl exit $curlExit)")
        }
        else {
            $transcript.Add("port-forward end (not established; forward exited=$($forward.HasExited))")
        }
        $forwardError = Get-CapturedProcessText $stderrPath
        if (-not [string]::IsNullOrWhiteSpace($forwardError)) {
            $transcript.Add("port-forward stderr: $forwardError")
        }
    }
    finally {
        if ($null -ne $forward -and -not $forward.HasExited) {
            Stop-Process -Id $forward.Id -Force -ErrorAction SilentlyContinue
            # Await the exit. Polls are ~2s apart, so an un-awaited kill could leave the
            # previous forward still holding its port when the next poll starts.
            $exited = $forward.WaitForExit(2000)
            if (-not $exited) {
                $transcript.Add('port-forward kill: process did not exit within 2s; its port may still be held')
            }
        }
        foreach ($path in @($stdoutPath, $stderrPath)) {
            Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
        }
    }

    # Join AFTER the try/finally. PowerShell materializes a `return` expression BEFORE unwinding
    # through `finally`, so joining inside the try wrote the kill marker to a list nobody read:
    # a leaked port-forward left no trace in any evidence packet while a source-text pin asserted
    # the marker existed. Every marker appended during cleanup is now part of the transcript.
    return ($transcript -join [Environment]::NewLine)
}

function Get-HealthResponse {
    param([Parameter(Mandatory)][string]$Pod, [Parameter(Mandatory)][string]$Container)

    # Use the same image-native client and application-token header as the Kubernetes probes.
    # BusyBox wget intentionally omits the response body for HTTP errors, so retain the raw
    # authenticated response as a fallback for expected 503 fault-injection states.
    $probeCommand = @'
set +e
wgetOutput="$(wget -S -O- -T 6 --header="dapr-api-token: ${APP_API_TOKEN}" http://127.0.0.1:8080/ready 2>&1)"
wgetExit=$?
printf '%s\n' "$wgetOutput"
if [ "$wgetExit" -ne 0 ]; then
    # The netcat fallback exists for every wget failure that still has a retrievable body,
    # not only for an already-parsed 503. Gating it on a 503 line meant the slow-but-healthy
    # and timed-out cases - exactly what the fallback was added for - died on the deadline.
    # In CI run 30402973401 this fallback exited 0 with zero bytes on all 34 redis-fault polls
    # while the same binaries capture delayed 503 bodies in every local and live-cluster
    # reproduction, so it is instrumented with explicit markers: the transcript must show
    # whether the branch ran and how nc exited, instead of silence that cannot be told apart
    # from a skipped branch. The runner-side port-forward fallback below is the deterministic
    # body producer when this in-container path yields nothing.
    #
    # Timeout budget, re-derived 2026-07-29 by code review (chunk 2). This branch is
    # DIAGNOSTIC; the port-forward capture is the deterministic body producer, so the long
    # grace window this branch briefly carried (sleep 4 | nc -w 8, ~12s) is not needed and was
    # never sized against the startup budget - the comment that had justified the original
    # bound was deleted in the same change that raised it. Restored to sleep 2 | nc -w 4
    # (~4s). Worst case charged to the application is now wget -T 6 plus ~4s, about 10s per
    # poll, against the 60-second startup budget with polls every ~2s - inside the ~11s the
    # budget was originally sized for. The port-forward capture is excluded from the budget
    # arithmetic in Wait-AggregateStatus because it is verifier-side work performed after the
    # in-container probe already completed.
    printf 'nc-fallback begin (wget exit %s)\n' "$wgetExit"
    { printf "GET /ready HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\ndapr-api-token: %s\r\n\r\n" "$APP_API_TOKEN"; sleep 2; } | nc -w 4 127.0.0.1 8080 2>&1
    printf '\nnc-fallback end (nc exit %s)\n' "$?"
fi
'@
    # The repository's checkout policy materializes this PowerShell file as CRLF. The
    # container shell must receive LF-only commands or BusyBox reports a syntax error.
    $probeCommand = $probeCommand.Replace("`r", '')
    $output = @(& kubectl exec -n $namespace $Pod -c $Container -- /bin/sh -ec $probeCommand 2>&1)

    # Parse the RAW response. Redaction is an evidence-write concern only: sanitizing first
    # ran unanchored String.Replace over $env:HEXALITH_ZOT_USERNAME/_API_KEY - values this
    # script does not control - across the very text the status regex and ConvertFrom-Json
    # depend on, so a short or common username value corrupted the health decision itself.
    # Save-HealthResponseEvidence already redacts at the write.
    $text = $output -join [Environment]::NewLine
    $statusCode = Get-HealthStatusCode $text
    $body = Get-HealthJsonBody $text

    # The stage contract needs the aggregate JSON, not only the status line. When neither the
    # image-native probe nor its in-container fallback yielded a status-bearing JSON object,
    # capture the body deterministically from the runner side and fold the transcript in, so
    # the packet records both what the in-container path produced and what the endpoint
    # actually serves.
    $aggregate = $null
    try {
        $aggregate = $body | ConvertFrom-Json
    }
    catch {
        # Raw fallback text is not JSON; the port-forward capture below supplies the body.
    }
    $fallbackSeconds = 0.0
    if ($null -eq $aggregate -or $null -eq $aggregate.status) {
        $fallbackStartedAt = [DateTime]::UtcNow
        $fallbackText = Get-HealthResponseViaPortForward $Pod $Container
        $fallbackSeconds = ([DateTime]::UtcNow - $fallbackStartedAt).TotalSeconds
        $text = $text + [Environment]::NewLine + 'port-forward fallback:' + [Environment]::NewLine + $fallbackText

        # Parse the fallback transcript in ISOLATION. Get-HealthJsonBody carries its brace and
        # in-string state across the whole input, so an unbalanced '{' or an odd unescaped '"'
        # in the in-container half swallows the clean fallback object and returns the raw
        # transcript instead - and a malformed in-container transcript is precisely the
        # precondition for firing this fallback, so scanning the concatenation defeated it.
        # Get-HealthJsonBody returns its input unchanged when it finds no status-bearing
        # object, so an unchanged result means the isolated scan found nothing.
        $fallbackBody = Get-HealthJsonBody $fallbackText
        if ($fallbackBody -ne $fallbackText) {
            $body = $fallbackBody
        }
        else {
            $body = Get-HealthJsonBody $text
        }

        # Only a status the stage contract recognizes may replace the in-container observation.
        # Accepting any three-digit code let a probe-side 401 or an API-server 502 overwrite the
        # pod's real answer, so the stage burned its deadline reporting the wrong code.
        $fallbackStatusCode = Get-HealthStatusCode $fallbackText
        if ($fallbackStatusCode -in @(200, 503)) {
            $statusCode = $fallbackStatusCode
        }
    }

    return [pscustomobject]@{
        StatusCode = $statusCode
        Body = $body
        Raw = $text
        FallbackSeconds = $fallbackSeconds
    }
}

function Save-HealthResponseEvidence {
    param(
        [Parameter(Mandatory)][string]$Stage,
        [Nullable[int]]$StatusCode,
        [AllowEmptyString()][string]$Body,
        [AllowEmptyString()][string]$Transcript = ''
    )

    # Two stage names differing only outside [0-9A-Za-z_.-] used to collapse onto one file
    # name, and every poll of a stage overwrote the previous one, so only the final attempt
    # survived. The validator's "must include both HTTP 200 and HTTP 503" then rested on
    # incidental final-poll timing rather than on a recorded transition. Disambiguate the
    # stage and keep every poll.
    $safeStage = $Stage -replace '[^0-9A-Za-z_.-]', '_'
    if ($safeStage -cne $Stage) {
        $stageBytes = [System.Text.Encoding]::UTF8.GetBytes($Stage)
        $stageHash = [System.Convert]::ToHexString(
            [System.Security.Cryptography.SHA256]::HashData($stageBytes)).Substring(0, 8).ToLowerInvariant()
        $safeStage = "$safeStage.$stageHash"
    }

    $script:healthEvidenceAttempts[$safeStage] = 1 + ($script:healthEvidenceAttempts[$safeStage] ?? 0)
    $attempt = ($script:healthEvidenceAttempts[$safeStage]).ToString('000')
    $evidence = [ordered]@{
        schemaVersion = 1
        stage = $Stage
        attempt = [int]$script:healthEvidenceAttempts[$safeStage]
        statusCode = $StatusCode
        body = Protect-EvidenceText $Body
        # The full capture transcript, not only the decided body. Without it a
        # port-forward-derived pass was byte-indistinguishable from an image-native pass:
        # when the fallback yields clean JSON, Get-HealthJsonBody returns just that object, so
        # the 'port-forward fallback:' and 'nc-fallback begin/end' markers were dropped and the
        # packet could not answer which capture path decided the stage.
        transcript = Protect-EvidenceText $Transcript
    }
    $evidence | ConvertTo-Json -Depth 5 |
        Set-Content -LiteralPath (Join-Path $evidencePath "health-$safeStage-$attempt.json") -Encoding utf8
}

function Wait-AggregateStatus {
    param(
        [Parameter(Mandatory)][string]$AppName,
        [Parameter(Mandatory)][string]$Container,
        [Parameter(Mandatory)][string]$ExpectedStatus,
        [Parameter(Mandatory)][string]$Stage,
        [int]$TimeoutSeconds = 60,
        [switch]$MeasureFromContainerRunning,
        [string]$RequiredPodAnnotationName = '',
        [string]$RequiredPodAnnotationValue = ''
    )

    Set-VerificationStage $Stage
    $deadline = [DateTime]::UtcNow.AddMinutes(4)
    $runningAt = $null
    $readyAt = $null
    $runningContainerInstance = ''
    $lastBody = ''
    $lastStatusCode = $null
    $probeOverheadSeconds = 0.0
    $expectedHttpStatus = if ($ExpectedStatus -eq 'Unhealthy') { 503 } else { 200 }
    while ([DateTime]::UtcNow -lt $deadline) {
        $observation = Get-RunningContainerObservation $AppName $Container $RequiredPodAnnotationName $RequiredPodAnnotationValue
        if ($null -ne $observation) {
            $pod = [string]$observation.PodName
            $containerStartedAt = $observation.ContainerStartedAt
            $readyAt = $observation.ReadyAt
            $containerInstance = if ($null -eq $containerStartedAt) {
                $pod
            }
            else {
                "$pod|$($containerStartedAt.ToString('o'))"
            }
            if ($null -eq $runningAt -or $containerInstance -ne $runningContainerInstance) {
                # A Kubernetes container can restart inside the same pod. Measure the startup
                # budget from the current container instance so a recovered restart is not charged
                # the elapsed lifetime of its failed predecessor.
                $runningAt = if ($null -eq $containerStartedAt) { [DateTime]::UtcNow } else { $containerStartedAt }
                $runningContainerInstance = $containerInstance
                # Capture overhead accrued while probing the PREVIOUS container instance was charged
                # against that instance's budget. Carrying it forward would pre-pay the replacement's
                # budget, so a crash-looping container could earn its successor an arbitrarily large
                # startup allowance. Reset it with $runningAt, for the same reason $runningAt resets.
                $probeOverheadSeconds = 0.0
            }

            $response = Get-HealthResponse $pod $Container
            $lastBody = $response.Body
            $lastStatusCode = $response.StatusCode
            # The runner-side port-forward capture is verifier-side work performed after the
            # in-container probe already returned, so it is not the application's startup
            # latency. Accumulate it and exclude it from the budget arithmetic below; without
            # this, a fallback-heavy stage charged its own capture time to the container and
            # could fail a container that became ready well inside its budget.
            $probeOverheadSeconds += [double]$response.FallbackSeconds
            Save-HealthResponseEvidence -Stage $Stage -StatusCode $lastStatusCode -Body $lastBody -Transcript $response.Raw
            $health = $null
            try {
                $health = $lastBody | ConvertFrom-Json
            }
            catch {
                # kubectl exec may emit a transient startup error; preserve it for the terminal diagnostic.
            }

            if ($null -ne $health -and $health.status -eq $ExpectedStatus -and $lastStatusCode -eq $expectedHttpStatus) {
                # The Server and MCP checks run sequentially. If this probe observes a healthy pod
                # after the wall-clock budget, use Kubernetes' recorded Ready transition to prove
                # that the current container actually became ready inside its startup budget.
                # Subtract capture overhead ONLY when $healthyAt fell back to the runner's own clock.
                # When Kubernetes recorded the Ready transition, ($healthyAt - $runningAt) is the
                # difference of two cluster-side timestamps and contains none of this verifier's
                # port-forward capture time, so subtracting it would credit time never charged and
                # pass a container that genuinely became ready outside its budget.
                # Kubernetes' recorded Ready transition is usable only when it belongs to the
                # CURRENT container instance. A container that restarted inside the pod while the
                # pod-level Ready condition still carried its predecessor's lastTransitionTime gave
                # $readyAt < $runningAt - a NEGATIVE interval that can never exceed the budget, so a
                # restarted container's startup was unenforceable. Fall back to the runner clock.
                $readyAtIsUsable = ($null -ne $readyAt) -and ($readyAt -ge $runningAt)
                $healthyAt = if ($readyAtIsUsable) { $readyAt } else { [DateTime]::UtcNow }
                $startupElapsed = ($healthyAt - $runningAt).TotalSeconds

                # The effective total is capped at $TimeoutSeconds (Administrator decision
                # 2026-07-31). The previous form bounded the CREDIT at $TimeoutSeconds rather than
                # bounding the RESULT, so the effective ceiling reached 2 x $TimeoutSeconds: a
                # container Kubernetes recorded Ready 119s after start passed the "60-second startup
                # limit". Accrued runner-side capture is reported for diagnosis but NOT credited,
                # because a credit bounded only by the budget can always absorb a full budget's
                # overrun. Trade-off, disclosed: a stage whose port-forward capture is genuinely
                # large can now fail a container that became ready inside its own budget; that
                # false red is preferable to a startup contract that silently states 60 and enforces 120.
                if ($MeasureFromContainerRunning -and ($startupElapsed -gt $TimeoutSeconds)) {
                    throw "[$Stage] $AppName reached $ExpectedStatus $([math]::Round($startupElapsed, 1))s after container $Container started, beyond the $TimeoutSeconds-second startup limit. Runner-side port-forward capture accrued $([math]::Round($probeOverheadSeconds, 1))s during this stage and is not credited against the limit."
                }

                return $lastBody
            }

            # This branch compares two runner-side clocks, so the capture overhead IS inside the
            # interval and is legitimately excluded. Cap the credit at the budget itself: the
            # fallback can fire on every poll and cost more than the ~2s cadence, so an uncapped
            # credit grows faster than elapsed time and this throw would never fire, silently
            # handing the outcome to the un-adjusted 4-minute deadline with no budget attribution.
            $failureOverhead = [math]::Min($probeOverheadSeconds, $TimeoutSeconds)
            if ($MeasureFromContainerRunning -and ((([DateTime]::UtcNow - $runningAt).TotalSeconds - $failureOverhead) -gt $TimeoutSeconds)) {
                throw "[$Stage] $AppName did not report HTTP $expectedHttpStatus aggregate $ExpectedStatus within $TimeoutSeconds seconds after container $Container started (excluding $([math]::Round($failureOverhead, 1))s of runner-side port-forward capture, capped at the $TimeoutSeconds-second budget). Last HTTP status: $lastStatusCode. Last response: $lastBody"
            }
        }

        Start-Sleep -Seconds 2
    }

    throw "[$Stage] $AppName did not report HTTP $expectedHttpStatus aggregate $ExpectedStatus. Last HTTP status: $lastStatusCode. Last response: $lastBody"
}

function Save-KubectlEvidence {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string[]]$Arguments
    )

    $kubectlArguments = @('--request-timeout=15s') + $Arguments
    $output = @(& kubectl @kubectlArguments 2>&1) -join [Environment]::NewLine
    Protect-EvidenceText $output | Set-Content -LiteralPath (Join-Path $evidencePath $Name) -Encoding utf8
}

function Write-ClusterDiagnostics {
    param([Parameter(Mandatory)][string]$Status)

    $result = [ordered]@{
        schemaVersion = 1
        status = $Status
        stage = $verificationStage
        capturedAt = [DateTime]::UtcNow.ToString('o')
        error = if ($null -eq $verificationError) { $null } else { Protect-EvidenceText $verificationError.Exception.Message }
    }
    $result | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidencePath 'verification-result.json') -Encoding utf8

    if (-not $clusterCreated) {
        return
    }

    Save-KubectlEvidence 'pods.txt' @('get', 'pods', '-n', $namespace, '-o', 'wide')
    Save-KubectlEvidence 'events.txt' @('get', 'events', '-n', $namespace, '--sort-by=.lastTimestamp')
    Save-KubectlEvidence 'describe-pods.txt' @('describe', 'pods', '-n', $namespace)
    Save-KubectlEvidence 'describe-workloads.txt' @('describe', 'deployments,statefulsets,services', '-n', $namespace)

    $podsJson = @(& kubectl --request-timeout=15s get pods -n $namespace -o json 2>&1) -join [Environment]::NewLine
    Protect-EvidenceText $podsJson | Set-Content -LiteralPath (Join-Path $evidencePath 'pods.json') -Encoding utf8
    if ($LASTEXITCODE -ne 0) {
        return
    }

    try {
        $pods = @(($podsJson | ConvertFrom-Json).items)
    }
    catch {
        Protect-EvidenceText $_.Exception.Message | Set-Content -LiteralPath (Join-Path $evidencePath 'logs-enumeration-error.txt') -Encoding utf8
        return
    }

    foreach ($pod in $pods) {
        $podName = [string]$pod.metadata.name
        $containers = @($pod.spec.initContainers) + @($pod.spec.containers)
        foreach ($container in $containers) {
            if ($null -eq $container) {
                continue
            }

            $containerName = [string]$container.name
            if ([string]::IsNullOrWhiteSpace($containerName)) {
                continue
            }

            $safeName = "$podName-$containerName" -replace '[^0-9A-Za-z_.-]', '_'
            Save-KubectlEvidence "$safeName-current.log" @('logs', $podName, '-n', $namespace, '-c', $containerName, '--timestamps=true')
            Save-KubectlEvidence "$safeName-previous.log" @('logs', $podName, '-n', $namespace, '-c', $containerName, '--previous', '--timestamps=true')
        }
    }
}

function Set-DaprClientTokenFault {
    param([Parameter(Mandatory)][bool]$Faulted)

    $patch = if ($Faulted) {
        '{"spec":{"template":{"metadata":{"annotations":{"verification.hexalith.com/dapr-token-stage":"faulted"}},"spec":{"containers":[{"name":"memories","env":[{"name":"DAPR_API_TOKEN","value":"verification-invalid-dapr-api-token","valueFrom":null}]}]}}}}'
    }
    else {
        '{"spec":{"template":{"metadata":{"annotations":{"verification.hexalith.com/dapr-token-stage":"restored"}},"spec":{"containers":[{"name":"memories","env":[{"name":"DAPR_API_TOKEN","value":null,"valueFrom":{"secretKeyRef":{"name":"dapr-api-token","key":"token"}}}]}]}}}}'
    }

    Invoke-Checked kubectl @('patch', 'deployment/memories', '-n', $namespace, '--type=strategic', '-p', $patch) | Out-Null
}

function Save-MemoriesDeploymentState {
    $deploymentJson = (Invoke-Checked kubectl @('get', 'deployment/memories', '-n', $namespace, '-o', 'json')) -join [Environment]::NewLine
    $deployment = $deploymentJson | ConvertFrom-Json
    if ($null -eq $deployment.spec.replicas -or $null -eq $deployment.spec.strategy) {
        throw 'Deployment/memories did not expose the replicas and rollout strategy required for reversible fault injection.'
    }

    $script:originalMemoriesDeploymentState = [pscustomobject]@{
        Replicas = [int]$deployment.spec.replicas
        Strategy = $deployment.spec.strategy
    }
}

function Set-CapacityPreservingMemoriesRollout {
    if ($null -eq $script:originalMemoriesDeploymentState) {
        throw 'Deployment/memories state must be captured before changing its rollout strategy.'
    }

    $patch = '{"spec":{"strategy":{"type":"RollingUpdate","rollingUpdate":{"maxSurge":0,"maxUnavailable":1}}}}'
    $script:memoriesDeploymentStateChanged = $true
    Invoke-Checked kubectl @('patch', 'deployment/memories', '-n', $namespace, '--type=merge', '-p', $patch) | Out-Null
}

function Restore-MemoriesDeploymentState {
    param(
        [Parameter(Mandatory)][string]$ServerStage,
        [Parameter(Mandatory)][string]$McpStage
    )

    if ($null -eq $script:originalMemoriesDeploymentState) {
        throw 'Deployment/memories state was not captured and cannot be restored.'
    }

    $patch = @(
        [ordered]@{
            op = 'replace'
            path = '/spec/replicas'
            value = $script:originalMemoriesDeploymentState.Replicas
        },
        [ordered]@{
            op = 'replace'
            path = '/spec/strategy'
            value = $script:originalMemoriesDeploymentState.Strategy
        }
    ) | ConvertTo-Json -Depth 10 -Compress
    Invoke-Checked kubectl @('patch', 'deployment/memories', '-n', $namespace, '--type=json', '-p', $patch) | Out-Null
    Invoke-Checked kubectl @('rollout', 'status', 'deployment/memories', '-n', $namespace, '--timeout=120s') | Out-Null
    Wait-AggregateStatus 'memories' 'memories' 'Healthy' -Stage $ServerStage | Out-Null
    Wait-AggregateStatus 'memories-mcp' 'memories-mcp' 'Healthy' -Stage $McpStage | Out-Null

    $deploymentJson = (Invoke-Checked kubectl @('get', 'deployment/memories', '-n', $namespace, '-o', 'json')) -join [Environment]::NewLine
    $deployment = $deploymentJson | ConvertFrom-Json
    $actualStrategy = $deployment.spec.strategy | ConvertTo-Json -Depth 10 -Compress
    $expectedStrategy = $script:originalMemoriesDeploymentState.Strategy | ConvertTo-Json -Depth 10 -Compress
    if ([int]$deployment.spec.replicas -ne $script:originalMemoriesDeploymentState.Replicas -or
        -not [string]::Equals($actualStrategy, $expectedStrategy, [StringComparison]::Ordinal)) {
        throw 'Deployment/memories did not return to its exact captured replicas and rollout strategy.'
    }

    $script:memoriesDeploymentStateChanged = $false
}

function Assert-ImageContract {
    param([Parameter(Mandatory)][string]$Image)

    $user = (Invoke-Checked docker @('image', 'inspect', $Image, '--format={{.Config.User}}')) -join ''
    if ($user -ne '1654') {
        throw "$Image configured user '$user'; expected numeric non-root UID 1654."
    }

    $ports = (Invoke-Checked docker @('image', 'inspect', $Image, '--format={{json .Config.ExposedPorts}}')) -join ''
    if ($ports -notmatch '8080/tcp') {
        throw "$Image does not expose 8080/tcp: $ports"
    }

    $labelVersion = (Invoke-Checked docker @('image', 'inspect', $Image, '--format={{index .Config.Labels "org.opencontainers.image.version"}}')) -join ''
    if ($labelVersion -ne $Version) {
        throw "$Image OCI version label '$labelVersion' does not match release version '$Version'."
    }

    # A production image must not retain the development settings file because it contains the
    # repository's symmetric development signing key. Inspect the built filesystem, not only the
    # project declaration that requests CopyToPublishDirectory=Never.
    Invoke-Checked docker @(
        'run', '--rm', '--entrypoint', '/bin/sh', $Image,
        '-c', 'test ! -e /app/appsettings.Development.json') | Out-Null
}

try {
    # `curl` backs the runner-side port-forward health capture. Without it here, a missing
    # binary raised a terminating CommandNotFoundException from inside the poll loop, aborting
    # mid-stage with an error unrelated to the deployment.
    foreach ($command in @('docker', 'kind', 'kubectl', 'dapr', 'pwsh', 'curl')) {
        if ($null -eq (Get-Command $command -ErrorAction SilentlyContinue)) {
            throw "Required command '$command' is not available. The deployment verifier never skips prerequisites."
        }
    }
    foreach ($archive in @($ServerArchive, $McpArchive)) {
        if (-not (Test-Path -LiteralPath $archive)) {
            throw "Container archive not found: $archive"
        }
    }

    $existingClusters = @(Invoke-Checked kind @('get', 'clusters'))
    if ($existingClusters -contains $ClusterName) {
        throw "Disposable cluster '$ClusterName' already exists; remove it or choose another ClusterName."
    }

    Invoke-Checked kind @(
        'create', 'cluster',
        '--name', $ClusterName,
        '--image', $KindNodeImage,
        '--wait', '120s',
        '--kubeconfig', $kubeconfigPath) | Out-Host
    $clusterCreated = $true
    $env:KUBECONFIG = $kubeconfigPath
    Invoke-Checked dapr @('init', '-k', '--runtime-version', $DaprRuntimeVersion, '--wait') | Out-Host

    # .NET SDK archive mode can emit registry-less RepoTags even when ContainerRegistry is supplied.
    # Parse the reference docker actually loaded, then re-tag it to the canonical reference that
    # Assert-ImageContract and 'kind load' expect.
    foreach ($load in @(
            @{ Archive = $ServerArchive; Target = $serverImage },
            @{ Archive = $McpArchive; Target = $mcpImage },
            @{ Archive = $AccessTelemetryArchive; Target = $accessTelemetryImage },
            @{ Archive = $AccessTelemetryClockArchive; Target = $accessTelemetryClockImage })) {
        $loadOutput = (Invoke-Checked docker @('load', '--input', $load.Archive)) -join [Environment]::NewLine
        $loaded = [regex]::Match($loadOutput, 'Loaded image(?: ID)?:\s*(?<ref>\S+)').Groups['ref'].Value
        if ([string]::IsNullOrWhiteSpace($loaded)) {
            throw "Could not determine the loaded image reference from: $loadOutput"
        }
        if ($loaded -ne $load.Target) {
            Invoke-Checked docker @('tag', $loaded, $load.Target) | Out-Null
        }
    }
    Assert-ImageContract $serverImage
    Assert-ImageContract $mcpImage
    Assert-ImageContract $accessTelemetryImage
    Assert-ImageContract $accessTelemetryClockImage
    Invoke-Checked kind @(
        'load', 'docker-image', '--name', $ClusterName,
        $serverImage, $mcpImage, $accessTelemetryImage, $accessTelemetryClockImage) | Out-Host

    Invoke-Checked kubectl @('create', 'namespace', $namespace) | Out-Null
    Apply-GeneratedSecret 'redis-secret' @('password=verification-redis-password', 'falkordb-password=verification-falkordb-password')
    Apply-GeneratedSecret 'llm-secret' @('OPENAI_API_KEY=verification-openai-key')
    Apply-GeneratedSecret 'google-embedding-api-key' @('google-embedding-api-key=verification-google-key')
    Apply-GeneratedSecret 'memories-embedding-client-secret' @('memories-embedding-client-secret=verification-embedding-secret')
    Apply-GeneratedSecret 'app-api-token' @('token=verification-app-api-token')
    Apply-GeneratedSecret 'dapr-api-token' @('token=verification-dapr-api-token')
    $registrySecret = Invoke-Checked kubectl @(
        'create', 'secret', 'docker-registry', 'registry-credentials', '--namespace', $namespace,
        '--docker-server=registry.hexalith.com', '--docker-username=verification', '--docker-password=verification',
        '--dry-run=client', '-o', 'yaml')
    $registrySecret | & kubectl apply -f - | Out-Null

    Invoke-Checked pwsh @(
        '-NoLogo', '-NoProfile', '-File', (Join-Path $PSScriptRoot 'render-production-deployment.ps1'),
        '-Version', $Version, '-ServerImage', $serverImage, '-McpImage', $mcpImage,
        '-AccessTelemetryImage', $accessTelemetryImage,
        '-AccessTelemetryClockImage', $accessTelemetryClockImage,
        '-OutputPath', $manifestPath) | Out-Host
    Invoke-Checked kubectl @('apply', '--dry-run=client', '-f', $manifestPath) | Out-Host
    Invoke-Checked kubectl @('apply', '-f', $manifestPath) | Out-Host

    # DW 27.3-CR17 (Administrator decision 2026-07-28): the production manifests carry two
    # `secretstores.hashicorp.vault` stores (`secretstore`, `access-telemetry-secrets`) that route
    # component secretKeyRef resolution through an OpenBao this disposable cluster does not have,
    # so daprd exits fatally at component load. The manifests are applied verbatim above; this
    # separate, disclosed step then substitutes both stores with verification-scoped
    # `secretstores.kubernetes` aliases (merge patch: the cluster object keeps name and scopes),
    # so the same secretKeyRef name/key pairs resolve from the seeded verification Secrets.
    #
    # ORDERING, corrected 2026-07-29 by code review (chunk 2). This block previously claimed it
    # "runs while the applications are scaled to zero below, so no consumer pod ever loads the
    # vault-typed component". That was false: `kubectl apply -f` above creates
    # deployment/memories and deployment/memories-mcp at their manifest replica count, and the
    # scale-to-zero happens AFTER this block. The qualifying run shows the window - apply at
    # 22:45:22.193, substitution at 22:45:22.396. Consumer pods can therefore start against the
    # vault-typed store inside that sub-second window and crash-loop at component load; the
    # scale-to-zero below then clears them before the health stages begin. The window cannot be
    # closed by reordering, because the pods are created by the apply itself; closing it would
    # require rendering the manifests at zero replicas, which would no longer apply them
    # verbatim. Recorded rather than papered over.
    #
    # The OpenBao path is NOT exercised by this lane; proving it is Story 31.2 scope.
    # Guard the target before rewriting any Component's type. $namespace is the same name the
    # reviewed live PG-ONPREM-1 target uses, and until now safety rested entirely on
    # $env:KUBECONFIG having been repointed above. These are the first commands in this script
    # that mutate a Dapr Component's spec.type, and `secretstore.yaml` is Story 31.2's artifact.
    Invoke-SecretStoreSubstitution -Namespace $namespace -ClusterName $ClusterName -DisclosurePath (Join-Path $evidencePath 'secret-store-substitution.json')

    # The release manifest assumes external Redis/FalkorDB services already exist. This disposable
    # cluster creates them in the same apply, so keep the applications stopped until those required
    # dependencies are Ready. Otherwise Dapr sidecars can repeatedly exit while Redis DNS/storage is
    # still being provisioned and the verifier incorrectly charges infrastructure bootstrap time to
    # the application's 60-second cold-start contract.
    Invoke-Checked kubectl @('scale', 'deployment/memories', 'deployment/memories-mcp', '-n', $namespace, '--replicas=0') | Out-Null
    Invoke-Checked kubectl @('rollout', 'status', 'deployment/memories', '-n', $namespace, '--timeout=60s') | Out-Null
    Invoke-Checked kubectl @('rollout', 'status', 'deployment/memories-mcp', '-n', $namespace, '--timeout=60s') | Out-Null
    Invoke-Checked kubectl @('rollout', 'status', 'statefulset/redis-stack', '-n', $namespace, '--timeout=180s') | Out-Null
    Invoke-Checked kubectl @('rollout', 'status', 'statefulset/falkordb', '-n', $namespace, '--timeout=180s') | Out-Null
    Invoke-Checked kubectl @('scale', 'deployment/memories', 'deployment/memories-mcp', '-n', $namespace, '--replicas=2') | Out-Null

    # Capture stdout only (2>$null): a kubectl deprecation/warning line on stderr would otherwise be
    # concatenated onto the 'yes'/'no' verdict and fail the exact-equality RBAC check on a valid cluster.
    $canReadLlm = (@(& kubectl auth can-i get secret/llm-secret -n $namespace --as "system:serviceaccount:${namespace}:memories" 2>$null) -join '').Trim()
    if ($LASTEXITCODE -notin @(0, 1)) {
        throw "Unable to evaluate memories Secret RBAC (kubectl exit $LASTEXITCODE)."
    }

    $mcpCanReadLlm = (@(& kubectl auth can-i get secret/llm-secret -n $namespace --as "system:serviceaccount:${namespace}:memories-mcp" 2>$null) -join '').Trim()
    if ($LASTEXITCODE -notin @(0, 1)) {
        throw "Unable to evaluate memories-mcp Secret RBAC (kubectl exit $LASTEXITCODE)."
    }
    if ($canReadLlm -ne 'yes' -or $mcpCanReadLlm -ne 'no') {
        throw "Secret RBAC contract failed: memories=$canReadLlm memories-mcp=$mcpCanReadLlm"
    }

    Wait-AggregateStatus 'memories' 'memories' 'Healthy' -Stage 'initial-server-health' -TimeoutSeconds 60 -MeasureFromContainerRunning | Out-Null
    Wait-AggregateStatus 'memories-mcp' 'memories-mcp' 'Healthy' -Stage 'initial-mcp-health' -TimeoutSeconds 60 -MeasureFromContainerRunning | Out-Null

    Set-VerificationStage 'dapr-allowed-invocation'
    $mcpPod = Get-RunningPodName 'memories-mcp' 'memories-mcp'
    if ([string]::IsNullOrWhiteSpace($mcpPod)) {
        throw 'No running MCP pod was available for the DAPR allowed-invocation check.'
    }

    $allowed = @(& kubectl exec -n $namespace $mcpPod -c memories-mcp -- /bin/sh -ec 'wget -qO- --header="dapr-api-token: $DAPR_API_TOKEN" http://127.0.0.1:3500/v1.0/invoke/memories/method/api/v1/health' 2>&1) -join [Environment]::NewLine
    if ($LASTEXITCODE -ne 0 -or (($allowed | ConvertFrom-Json).status -ne 'Healthy')) {
        throw "MCP-to-Server DAPR ACL health invocation failed: $allowed"
    }

    # A sidecar invoking its own app-id is optimized locally by DAPR and does not
    # exercise workload-identity ACL evaluation. Use a distinct injected caller.
    Set-VerificationStage 'dapr-denied-invocation'
    $deniedCaller = @"
apiVersion: v1
kind: Pod
metadata:
  name: memories-acl-denied
  namespace: $namespace
  annotations:
    dapr.io/enabled: "true"
    dapr.io/app-id: memories-acl-denied
    dapr.io/config: memories-mcp-config
    dapr.io/api-token-secret: dapr-api-token
spec:
  serviceAccountName: memories-mcp
  restartPolicy: Never
  containers:
    - name: client
      image: $mcpImage
      imagePullPolicy: IfNotPresent
      command: ["/bin/sh", "-ec", "sleep 600"]
      env:
        - name: DAPR_API_TOKEN
          valueFrom:
            secretKeyRef:
              name: dapr-api-token
              key: token
      securityContext:
        allowPrivilegeEscalation: false
        capabilities:
          drop: ["ALL"]
        runAsNonRoot: true
        runAsUser: 1654
        seccompProfile:
          type: RuntimeDefault
"@
    $deniedCaller | & kubectl apply -f - | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to create the denied DAPR ACL caller pod.'
    }

    Invoke-Checked kubectl @('wait', 'pod/memories-acl-denied', '-n', $namespace, '--for=condition=Ready', '--timeout=60s') | Out-Null
    & kubectl exec -n $namespace memories-acl-denied -c client -- /bin/sh -ec 'wget -qO- --header="dapr-api-token: $DAPR_API_TOKEN" http://127.0.0.1:3500/v1.0/invoke/memories/method/api/v1/health' *> $null
    if ($LASTEXITCODE -eq 0) {
        throw 'DAPR deny-by-default ACL allowed caller app-id memories-acl-denied to invoke Server.'
    }
    Invoke-Checked kubectl @('delete', 'pod/memories-acl-denied', '-n', $namespace, '--wait=true') | Out-Null

    Set-VerificationStage 'optional-falkordb-fault-injection'
    Invoke-Checked kubectl @('scale', 'statefulset/falkordb', '-n', $namespace, '--replicas=0') | Out-Null
    $degraded = Wait-AggregateStatus 'memories' 'memories' 'Degraded' -Stage 'optional-falkordb-degraded'
    if ($degraded -notmatch 'graph-traversal') {
        throw "Optional FalkorDB degradation did not identify graph capabilities: $degraded"
    }
    Invoke-Checked kubectl @('scale', 'statefulset/falkordb', '-n', $namespace, '--replicas=1') | Out-Null
    Wait-AggregateStatus 'memories' 'memories' 'Healthy' -Stage 'optional-falkordb-restored' | Out-Null

    Set-VerificationStage 'required-redis-fault-injection'
    Invoke-Checked kubectl @('scale', 'statefulset/redis-stack', '-n', $namespace, '--replicas=0') | Out-Null
    Wait-AggregateStatus 'memories' 'memories' 'Unhealthy' -Stage 'required-redis-unhealthy' | Out-Null
    Invoke-Checked kubectl @('scale', 'statefulset/redis-stack', '-n', $namespace, '--replicas=1') | Out-Null
    Wait-AggregateStatus 'memories' 'memories' 'Healthy' -Stage 'required-redis-restored' | Out-Null

    Set-VerificationStage 'required-dapr-token-fault-injection'
    Save-MemoriesDeploymentState
    Set-CapacityPreservingMemoriesRollout
    $daprTokenFaultInjected = $true
    Set-DaprClientTokenFault $true
    $daprUnhealthy = Wait-AggregateStatus 'memories' 'memories' 'Unhealthy' `
        -Stage 'required-dapr-token-unhealthy' `
        -RequiredPodAnnotationName 'verification.hexalith.com/dapr-token-stage' `
        -RequiredPodAnnotationValue 'faulted'
    if ($daprUnhealthy -notmatch '(?i)dapr') {
        throw "Dapr token fault did not identify Dapr dependency evidence: $daprUnhealthy"
    }
    Set-VerificationStage 'required-dapr-token-restoration'
    Set-DaprClientTokenFault $false
    Wait-AggregateStatus 'memories' 'memories' 'Healthy' `
        -Stage 'required-dapr-token-restored' `
        -RequiredPodAnnotationName 'verification.hexalith.com/dapr-token-stage' `
        -RequiredPodAnnotationValue 'restored' | Out-Null
    Invoke-Checked kubectl @('rollout', 'status', 'deployment/memories', '-n', $namespace, '--timeout=120s') | Out-Null
    $daprTokenFaultInjected = $false
    Restore-MemoriesDeploymentState `
        -ServerStage 'required-dapr-token-capacity-restored' `
        -McpStage 'required-dapr-token-mcp-restored'

    Set-VerificationStage 'required-server-fault-injection'
    $memoriesDeploymentStateChanged = $true
    Invoke-Checked kubectl @('scale', 'deployment/memories', '-n', $namespace, '--replicas=0') | Out-Null
    Wait-AggregateStatus 'memories-mcp' 'memories-mcp' 'Unhealthy' -Stage 'required-server-mcp-unhealthy' | Out-Null
    Restore-MemoriesDeploymentState `
        -ServerStage 'required-server-restored' `
        -McpStage 'required-server-mcp-restored'

    $verificationSucceeded = $true
    Write-Host 'Production deployment verification passed with zero skips.'
}
catch {
    $verificationError = $_
    throw
}
finally {
    $terminalStage = $verificationStage

    if ($clusterCreated -and $daprTokenFaultInjected) {
        try {
            Set-DaprClientTokenFault $false
            Invoke-Checked kubectl @('rollout', 'status', 'deployment/memories', '-n', $namespace, '--timeout=120s') | Out-Null
            $daprTokenFaultInjected = $false
        }
        catch {
            Write-Warning "Unable to restore the Dapr client token after verifier failure: $(Protect-EvidenceText $_.Exception.Message)"
        }
    }

    if ($clusterCreated -and $memoriesDeploymentStateChanged) {
        try {
            Restore-MemoriesDeploymentState `
                -ServerStage 'cleanup-server-restored' `
                -McpStage 'cleanup-mcp-restored'
        }
        catch {
            Write-Warning "Unable to restore the deployment strategy and replicas after verifier failure: $(Protect-EvidenceText $_.Exception.Message)"
        }
    }

    try {
        # Capture the live post-cleanup cluster while retaining the terminal verification stage.
        Set-VerificationStage $terminalStage
        Write-ClusterDiagnostics $(if ($verificationSucceeded) { 'succeeded' } else { 'failed' })
    }
    catch {
        Write-Warning "Unable to persist complete production deployment diagnostics: $(Protect-EvidenceText $_.Exception.Message)"
    }

    if (Test-Path -LiteralPath $manifestPath) {
        Remove-Item -LiteralPath $manifestPath -Force
    }

    if ($clusterCreated -and -not $KeepCluster) {
        & kind delete cluster --name $ClusterName | Out-Host
    }

    $env:KUBECONFIG = $originalKubeconfig
    if (-not $KeepCluster -and (Test-Path -LiteralPath $kubeconfigPath)) {
        Remove-Item -LiteralPath $kubeconfigPath -Force
    }
}
