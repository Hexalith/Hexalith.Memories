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
    # It now fires on every failed poll rather than only a 503-line match, so its own timeouts
    # are kept short (worst case ~5s, not ~11s): polling happens every ~2s against a 60s startup
    # budget, and a same-loopback response does not need a long grace window.
    { printf "GET /ready HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\ndapr-api-token: %s\r\n\r\n" "$APP_API_TOKEN"; sleep 2; } | nc -w 3 127.0.0.1 8080
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

    return [pscustomobject]@{
        StatusCode = $statusCode
        Body = $body
        Raw = $text
    }
}

function Save-HealthResponseEvidence {
    param(
        [Parameter(Mandatory)][string]$Stage,
        [Nullable[int]]$StatusCode,
        [AllowEmptyString()][string]$Body
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
            }

            $response = Get-HealthResponse $pod $Container
            $lastBody = $response.Body
            $lastStatusCode = $response.StatusCode
            Save-HealthResponseEvidence -Stage $Stage -StatusCode $lastStatusCode -Body $lastBody
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
                $healthyAt = if ($null -eq $readyAt) { [DateTime]::UtcNow } else { $readyAt }
                if ($MeasureFromContainerRunning -and (($healthyAt - $runningAt).TotalSeconds -gt $TimeoutSeconds)) {
                    throw "[$Stage] $AppName reached $ExpectedStatus after the $TimeoutSeconds-second startup limit."
                }

                return $lastBody
            }

            if ($MeasureFromContainerRunning -and (([DateTime]::UtcNow - $runningAt).TotalSeconds -gt $TimeoutSeconds)) {
                throw "[$Stage] $AppName did not report HTTP $expectedHttpStatus aggregate $ExpectedStatus within $TimeoutSeconds seconds after container $Container started. Last HTTP status: $lastStatusCode. Last response: $lastBody"
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
    foreach ($command in @('docker', 'kind', 'kubectl', 'dapr', 'pwsh')) {
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
