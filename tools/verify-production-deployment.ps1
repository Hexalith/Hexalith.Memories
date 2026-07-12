[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ServerArchive,

    [Parameter(Mandatory)]
    [string]$McpArchive,

    [Parameter(Mandatory)]
    [string]$Version,

    [string]$ClusterName = "hexalith-memories-verification",

    [string]$DaprRuntimeVersion = "1.18.1",

    [switch]$KeepCluster
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$namespace = 'hexalith-memories'
$serverImage = "registry.hexalith.com/hexalith/memories-server:$Version"
$mcpImage = "registry.hexalith.com/hexalith/memories-mcp:$Version"
$manifestPath = Join-Path ([System.IO.Path]::GetTempPath()) "hexalith-memories-production-$Version.yaml"
$kubeconfigPath = Join-Path ([System.IO.Path]::GetTempPath()) "hexalith-memories-kubeconfig-$([Guid]::NewGuid().ToString('N'))"
$originalKubeconfig = $env:KUBECONFIG
$clusterCreated = $false

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

function Get-PodName {
    param([Parameter(Mandatory)][string]$AppName)

    $json = @(& kubectl get pods -n $namespace -l "app.kubernetes.io/name=$AppName" -o json 2>$null) -join [Environment]::NewLine
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($json)) {
        return ''
    }

    $pod = @((ConvertFrom-Json $json).items |
        Sort-Object { [DateTime]$_.metadata.creationTimestamp } -Descending |
        Select-Object -First 1)
    if ($pod.Count -eq 0) {
        return ''
    }

    return [string]$pod[0].metadata.name
}

function Test-ContainerRunning {
    param([Parameter(Mandatory)][string]$Pod, [Parameter(Mandatory)][string]$Container)

    $json = Invoke-Checked kubectl @('get', 'pod', $Pod, '-n', $namespace, '-o', 'json') | ConvertFrom-Json
    $status = @($json.status.containerStatuses | Where-Object { $_.name -eq $Container })
    return $status.Count -eq 1 -and $null -ne $status[0].state.running
}

function Get-HealthBody {
    param([Parameter(Mandatory)][string]$Pod, [Parameter(Mandatory)][string]$Container)

    # BusyBox wget is fast for the 200 Healthy/Degraded responses but discards 503
    # bodies. Fall back to its netcat applet for fail-closed aggregate documents;
    # keep stdin open beyond the longest (3 second) backend health-check timeout.
    $probeCommand = 'body=$(wget -qO- http://127.0.0.1:8080/ready 2>/dev/null) && { printf "%s" "$body"; exit 0; }; { printf "GET /ready HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n"; sleep 5; } | nc -w 6 127.0.0.1 8080'
    $output = @(& kubectl exec -n $namespace $Pod -c $Container -- /bin/sh -ec $probeCommand 2>&1)
    $text = $output -join [Environment]::NewLine
    $start = $text.IndexOf('{')
    $end = $text.LastIndexOf('}')
    if ($start -ge 0 -and $end -ge $start) {
        return $text.Substring($start, $end - $start + 1)
    }

    return $text
}

function Wait-AggregateStatus {
    param(
        [Parameter(Mandatory)][string]$AppName,
        [Parameter(Mandatory)][string]$Container,
        [Parameter(Mandatory)][string]$ExpectedStatus,
        [int]$TimeoutSeconds = 60,
        [switch]$MeasureFromContainerRunning
    )

    $deadline = [DateTime]::UtcNow.AddMinutes(4)
    $runningAt = $null
    $lastBody = ''
    while ([DateTime]::UtcNow -lt $deadline) {
        $pod = Get-PodName $AppName
        if (-not [string]::IsNullOrWhiteSpace($pod) -and (Test-ContainerRunning $pod $Container)) {
            if ($null -eq $runningAt) {
                $runningAt = [DateTime]::UtcNow
            }

            $lastBody = Get-HealthBody $pod $Container
            $health = $null
            try {
                $health = $lastBody | ConvertFrom-Json
            }
            catch {
                # kubectl exec may emit a transient startup error; preserve it for the terminal diagnostic.
            }

            if ($null -ne $health -and $health.status -eq $ExpectedStatus) {
                if ($MeasureFromContainerRunning -and (([DateTime]::UtcNow - $runningAt).TotalSeconds -gt $TimeoutSeconds)) {
                    throw "$AppName reached $ExpectedStatus after the $TimeoutSeconds-second startup limit."
                }

                return $lastBody
            }

            if ($MeasureFromContainerRunning -and (([DateTime]::UtcNow - $runningAt).TotalSeconds -gt $TimeoutSeconds)) {
                throw "$AppName did not report aggregate $ExpectedStatus within $TimeoutSeconds seconds after container $Container started. Last response: $lastBody"
            }
        }

        Start-Sleep -Seconds 2
    }

    throw "$AppName did not report aggregate $ExpectedStatus. Last response: $lastBody"
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
}

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

try {
    $existingClusters = @(Invoke-Checked kind @('get', 'clusters'))
    if ($existingClusters -contains $ClusterName) {
        throw "Disposable cluster '$ClusterName' already exists; remove it or choose another ClusterName."
    }

    Invoke-Checked kind @('create', 'cluster', '--name', $ClusterName, '--wait', '120s', '--kubeconfig', $kubeconfigPath) | Out-Host
    $clusterCreated = $true
    $env:KUBECONFIG = $kubeconfigPath
    Invoke-Checked dapr @('init', '-k', '--runtime-version', $DaprRuntimeVersion, '--wait') | Out-Host

    Invoke-Checked docker @('load', '--input', $ServerArchive) | Out-Host
    Invoke-Checked docker @('load', '--input', $McpArchive) | Out-Host
    Invoke-Checked docker @('tag', "hexalith/memories-server:$Version", $serverImage) | Out-Null
    Invoke-Checked docker @('tag', "hexalith/memories-mcp:$Version", $mcpImage) | Out-Null
    Assert-ImageContract $serverImage
    Assert-ImageContract $mcpImage
    Invoke-Checked kind @('load', 'docker-image', '--name', $ClusterName, $serverImage, $mcpImage) | Out-Host

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
        '-OutputPath', $manifestPath) | Out-Host
    Invoke-Checked kubectl @('apply', '--dry-run=client', '-f', $manifestPath) | Out-Host
    Invoke-Checked kubectl @('apply', '-f', $manifestPath) | Out-Host

    $canReadLlm = @(& kubectl auth can-i get secret/llm-secret -n $namespace --as "system:serviceaccount:${namespace}:memories" 2>&1) -join ''
    if ($LASTEXITCODE -notin @(0, 1)) {
        throw "Unable to evaluate memories Secret RBAC (kubectl exit $LASTEXITCODE): $canReadLlm"
    }

    $mcpCanReadLlm = @(& kubectl auth can-i get secret/llm-secret -n $namespace --as "system:serviceaccount:${namespace}:memories-mcp" 2>&1) -join ''
    if ($LASTEXITCODE -notin @(0, 1)) {
        throw "Unable to evaluate memories-mcp Secret RBAC (kubectl exit $LASTEXITCODE): $mcpCanReadLlm"
    }
    if ($canReadLlm -ne 'yes' -or $mcpCanReadLlm -ne 'no') {
        throw "Secret RBAC contract failed: memories=$canReadLlm memories-mcp=$mcpCanReadLlm"
    }

    Wait-AggregateStatus 'memories' 'memories' 'Healthy' -TimeoutSeconds 60 -MeasureFromContainerRunning | Out-Null
    Wait-AggregateStatus 'memories-mcp' 'memories-mcp' 'Healthy' -TimeoutSeconds 60 -MeasureFromContainerRunning | Out-Null

    $mcpPod = Get-PodName 'memories-mcp'
    $allowed = @(& kubectl exec -n $namespace $mcpPod -c memories-mcp -- /bin/sh -ec 'wget -qO- --header="dapr-api-token: $DAPR_API_TOKEN" http://127.0.0.1:3500/v1.0/invoke/memories/method/api/v1/health' 2>&1) -join [Environment]::NewLine
    if ($LASTEXITCODE -ne 0 -or (($allowed | ConvertFrom-Json).status -ne 'Healthy')) {
        throw "MCP-to-Server DAPR ACL health invocation failed: $allowed"
    }

    # A sidecar invoking its own app-id is optimized locally by DAPR and does not
    # exercise workload-identity ACL evaluation. Use a distinct injected caller.
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

    Invoke-Checked kubectl @('scale', 'statefulset/falkordb', '-n', $namespace, '--replicas=0') | Out-Null
    $degraded = Wait-AggregateStatus 'memories' 'memories' 'Degraded'
    if ($degraded -notmatch 'graph-traversal') {
        throw "Optional FalkorDB degradation did not identify graph capabilities: $degraded"
    }
    Invoke-Checked kubectl @('scale', 'statefulset/falkordb', '-n', $namespace, '--replicas=1') | Out-Null
    Wait-AggregateStatus 'memories' 'memories' 'Healthy' | Out-Null

    Invoke-Checked kubectl @('scale', 'statefulset/redis-stack', '-n', $namespace, '--replicas=0') | Out-Null
    Wait-AggregateStatus 'memories' 'memories' 'Unhealthy' | Out-Null
    Invoke-Checked kubectl @('scale', 'statefulset/redis-stack', '-n', $namespace, '--replicas=1') | Out-Null
    Wait-AggregateStatus 'memories' 'memories' 'Healthy' | Out-Null

    Invoke-Checked kubectl @('patch', 'deployment/memories', '-n', $namespace, '-p', '{"spec":{"template":{"metadata":{"annotations":{"dapr.io/enabled":"false"}}}}}') | Out-Null
    Wait-AggregateStatus 'memories' 'memories' 'Unhealthy' | Out-Null
    Invoke-Checked kubectl @('patch', 'deployment/memories', '-n', $namespace, '-p', '{"spec":{"template":{"metadata":{"annotations":{"dapr.io/enabled":"true"}}}}}') | Out-Null
    Wait-AggregateStatus 'memories' 'memories' 'Healthy' | Out-Null

    Invoke-Checked kubectl @('scale', 'deployment/memories', '-n', $namespace, '--replicas=0') | Out-Null
    Wait-AggregateStatus 'memories-mcp' 'memories-mcp' 'Unhealthy' | Out-Null

    Write-Host 'Production deployment verification passed with zero skips.'
}
finally {
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
