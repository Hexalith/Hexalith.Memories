# Disposable TLS OpenBao staging for production-deployment verification.
# Dot-sourced by tools/verify-production-deployment.ps1. Never logs secret values.

# Immutable digest pin. Never overwrite this from OPENBAO_IMAGE or any other caller input.
$script:OpenBaoPinnedImage = 'quay.io/openbao/openbao:2.6.0@sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653'
$script:OpenBaoImagePin = $script:OpenBaoPinnedImage
$script:OpenBaoNamespace = 'openbao'
$script:OpenBaoServiceDns = 'hexalith-keys.openbao.svc.cluster.local'
$script:OpenBaoRuntimePrefix = 'hexalith/memories/runtime'
$script:OpenBaoAccessPrefix = 'hexalith/memories/access-telemetry'
$script:OpenBaoRuntimePolicy = 'hexalith-memories-runtime'
$script:OpenBaoAccessPolicy = 'hexalith-memories-access-telemetry'
$script:OpenBaoRedactionSecrets = [System.Collections.Generic.List[string]]::new()

function Register-OpenBaoRedactionSecret {
    param([AllowEmptyString()][string]$Value)

    if (-not [string]::IsNullOrWhiteSpace($Value) -and -not $script:OpenBaoRedactionSecrets.Contains($Value)) {
        $script:OpenBaoRedactionSecrets.Add($Value) | Out-Null
    }
}

function Get-OpenBaoRedactionSecrets {
    return @($script:OpenBaoRedactionSecrets)
}

function New-OpenBaoTlsMaterial {
    param([Parameter(Mandatory)][string]$WorkDirectory)

    $caKey = Join-Path $WorkDirectory 'ca.key'
    $caCrt = Join-Path $WorkDirectory 'ca.crt'
    $serverKey = Join-Path $WorkDirectory 'tls.key'
    $serverCrt = Join-Path $WorkDirectory 'tls.crt'
    $serverCsr = Join-Path $WorkDirectory 'server.csr'
    $extFile = Join-Path $WorkDirectory 'server.ext'

    Invoke-Checked openssl @(
        'req', '-x509', '-newkey', 'rsa:2048', '-nodes',
        '-keyout', $caKey, '-out', $caCrt, '-days', '1',
        '-subj', '/CN=hexalith-memories-verification-ca') | Out-Null

    @"
subjectAltName = DNS:$script:OpenBaoServiceDns,DNS:hexalith-keys,DNS:localhost,IP:127.0.0.1
extendedKeyUsage = serverAuth
"@ | Set-Content -LiteralPath $extFile -Encoding ascii

    Invoke-Checked openssl @(
        'req', '-newkey', 'rsa:2048', '-nodes',
        '-keyout', $serverKey, '-out', $serverCsr,
        '-subj', "/CN=$script:OpenBaoServiceDns") | Out-Null

    Invoke-Checked openssl @(
        'x509', '-req', '-in', $serverCsr, '-CA', $caCrt, '-CAkey', $caKey,
        '-CAcreateserial', '-out', $serverCrt, '-days', '1', '-extfile', $extFile) | Out-Null

    $caPem = (Get-Content -LiteralPath $caCrt -Raw).Trim()
    $serverKeyPem = (Get-Content -LiteralPath $serverKey -Raw).Trim()
    Register-OpenBaoRedactionSecret $serverKeyPem
    Register-OpenBaoRedactionSecret ((Get-Content -LiteralPath $caKey -Raw).Trim())

    return [ordered]@{
        CaPem = $caPem
        ServerCertificatePath = $serverCrt
        ServerKeyPath = $serverKey
        CaCertificatePath = $caCrt
    }
}

function Invoke-OpenBaoPod {
    param([Parameter(Mandatory)][string[]]$BaoArguments)

    $command = @(
        'exec', '-n', $script:OpenBaoNamespace, 'deploy/hexalith-keys', '--',
        'env',
        'BAO_ADDR=https://127.0.0.1:8200',
        'BAO_CACERT=/openbao/userconfig/openbao-server-tls/ca.crt'
    ) + $BaoArguments
    # Invoke-Checked prepends --request-timeout for kubectl.
    return Invoke-Checked kubectl $command
}

function Invoke-OpenBaoPodRaw {
    param([Parameter(Mandatory)][string[]]$BaoArguments)

    $output = @(
        & kubectl --request-timeout=12s exec -n $script:OpenBaoNamespace deploy/hexalith-keys -- `
            env BAO_ADDR=https://127.0.0.1:8200 BAO_CACERT=/openbao/userconfig/openbao-server-tls/ca.crt `
            @BaoArguments 2>&1)
    return @{
        ExitCode = $LASTEXITCODE
        Output = ($output -join [Environment]::NewLine)
    }
}

function Deploy-DisposableOpenBaoService {
    param(
        [Parameter(Mandatory)]$TlsMaterial,
        [Parameter(Mandatory)][string]$Image
    )

    if ($Image -cne $script:OpenBaoPinnedImage) {
        throw "Disposable OpenBao image '$Image' is not the immutable pinned production image '$script:OpenBaoPinnedImage'."
    }

    Invoke-Checked kubectl @('create', 'namespace', $script:OpenBaoNamespace) | Out-Null

    $tlsArgs = @(
        'create', 'secret', 'generic', 'openbao-server-tls',
        '-n', $script:OpenBaoNamespace,
        ('--from-file=tls.crt=' + $TlsMaterial.ServerCertificatePath),
        ('--from-file=tls.key=' + $TlsMaterial.ServerKeyPath),
        ('--from-file=ca.crt=' + $TlsMaterial.CaCertificatePath)
    )
    Invoke-Checked kubectl $tlsArgs | Out-Null

    $config = @"
ui = false
disable_mlock = true
api_addr = "https://$script:OpenBaoServiceDns`:8200"
listener "tcp" {
  address = "[::]:8200"
  tls_cert_file = "/openbao/userconfig/openbao-server-tls/tls.crt"
  tls_key_file = "/openbao/userconfig/openbao-server-tls/tls.key"
  tls_client_ca_file = "/openbao/userconfig/openbao-server-tls/ca.crt"
  tls_min_version = "tls12"
}
storage "file" {
  path = "/openbao/data"
}
"@
    $configPath = Join-Path ([System.IO.Path]::GetTempPath()) ("openbao-config-" + [Guid]::NewGuid().ToString('N') + ".hcl")
    try {
        Set-Content -LiteralPath $configPath -Value $config -Encoding ascii
        Invoke-Checked kubectl @(
            'create', 'configmap', 'hexalith-keys-config',
            '-n', $script:OpenBaoNamespace,
            ('--from-file=extraconfig-from-values.hcl=' + $configPath)) | Out-Null
    }
    finally {
        if (Test-Path -LiteralPath $configPath) {
            Remove-Item -LiteralPath $configPath -Force
        }
    }

    $manifest = @"
apiVersion: v1
kind: Service
metadata:
  name: hexalith-keys
  namespace: $script:OpenBaoNamespace
  labels:
    app.kubernetes.io/name: hexalith-keys
spec:
  type: ClusterIP
  selector:
    app.kubernetes.io/name: hexalith-keys
  ports:
    - name: https
      port: 8200
      targetPort: 8200
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: hexalith-keys
  namespace: $script:OpenBaoNamespace
  labels:
    app.kubernetes.io/name: hexalith-keys
spec:
  replicas: 1
  selector:
    matchLabels:
      app.kubernetes.io/name: hexalith-keys
  template:
    metadata:
      labels:
        app.kubernetes.io/name: hexalith-keys
    spec:
      automountServiceAccountToken: false
      securityContext:
        runAsNonRoot: true
        runAsUser: 100
        runAsGroup: 1000
        fsGroup: 1000
        seccompProfile:
          type: RuntimeDefault
      containers:
        - name: openbao
          image: $Image
          imagePullPolicy: IfNotPresent
          command: ["bao", "server", "-config=/openbao/config/extraconfig-from-values.hcl"]
          ports:
            - name: https
              containerPort: 8200
          env:
            - name: BAO_ADDR
              value: https://127.0.0.1:8200
            - name: BAO_CACERT
              value: /openbao/userconfig/openbao-server-tls/ca.crt
          readinessProbe:
            tcpSocket:
              port: 8200
            initialDelaySeconds: 2
            periodSeconds: 2
            failureThreshold: 30
          livenessProbe:
            tcpSocket:
              port: 8200
            initialDelaySeconds: 10
            periodSeconds: 10
            failureThreshold: 6
          securityContext:
            allowPrivilegeEscalation: false
            capabilities:
              drop: ["ALL"]
            readOnlyRootFilesystem: false
          volumeMounts:
            - name: config
              mountPath: /openbao/config
              readOnly: true
            - name: tls
              mountPath: /openbao/userconfig/openbao-server-tls
              readOnly: true
            - name: data
              mountPath: /openbao/data
      volumes:
        - name: config
          configMap:
            name: hexalith-keys-config
        - name: tls
          secret:
            secretName: openbao-server-tls
            defaultMode: 288
            items:
              - key: tls.crt
                path: tls.crt
              - key: tls.key
                path: tls.key
              - key: ca.crt
                path: ca.crt
        - name: data
          emptyDir: {}
"@
    $manifest | & kubectl apply -f - | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to apply the disposable OpenBao Service/Deployment.'
    }

    Invoke-Checked kubectl @(
        'rollout', 'status', 'deployment/hexalith-keys',
        '-n', $script:OpenBaoNamespace, '--timeout=180s') | Out-Null
}

function Initialize-DisposableOpenBao {
    param([Parameter(Mandatory)][string]$CaPem)

    Set-VerificationStage 'openbao-initialize'
    $init = Invoke-OpenBaoPodRaw @('bao', 'operator', 'init', '-key-shares=1', '-key-threshold=1', '-format=json')
    if ($init.ExitCode -ne 0) {
        throw "OpenBao initialize failed at stage openbao-initialize (exit $($init.ExitCode))."
    }

    try {
        $initJson = $init.Output | ConvertFrom-Json
    }
    catch {
        throw 'OpenBao initialize returned non-JSON output; refusing to continue.'
    }

    # CLI -format=json emits unseal_keys_b64 (not the HTTP API's keys_base64).
    $rootToken = [string]$initJson.root_token
    $unsealKeys = @($initJson.unseal_keys_b64)
    $unsealKey = if ($unsealKeys.Count -gt 0) { [string]$unsealKeys[0] } else { '' }
    if ([string]::IsNullOrWhiteSpace($rootToken) -or [string]::IsNullOrWhiteSpace($unsealKey)) {
        throw 'OpenBao initialize did not return a root token and unseal_keys_b64[0].'
    }
    Register-OpenBaoRedactionSecret $rootToken
    Register-OpenBaoRedactionSecret $unsealKey

    Set-VerificationStage 'openbao-unseal'
    $unseal = Invoke-OpenBaoPodRaw @('bao', 'operator', 'unseal', $unsealKey)
    if ($unseal.ExitCode -ne 0) {
        throw "OpenBao unseal failed at stage openbao-unseal (exit $($unseal.ExitCode))."
    }

    Set-VerificationStage 'openbao-enable-kv'
    $envRoot = @("BAO_TOKEN=$rootToken")
    $mount = Invoke-OpenBaoPodRaw (@('env') + $envRoot + @(
            'bao', 'secrets', 'enable', '-path=secret', 'kv-v2'))
    if ($mount.ExitCode -ne 0 -and $mount.Output -notmatch 'path is already in use') {
        throw "OpenBao KV v2 mount failed at stage openbao-enable-kv (exit $($mount.ExitCode))."
    }

    return [ordered]@{
        RootToken = $rootToken
        UnsealKey = $unsealKey
        CaPem = $CaPem
    }
}

function Install-OpenBaoReadPolicy {
    param(
        [Parameter(Mandatory)][string]$RootToken,
        [Parameter(Mandatory)][string]$PolicyName,
        [Parameter(Mandatory)][string]$Prefix
    )

    $policy = @"
path "secret/data/$Prefix/*" {
  capabilities = ["read"]
}
"@
    # Encode in-memory and write inside the pod; no host temp file.
    $encoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($policy))
    $write = Invoke-OpenBaoPodRaw @(
        'env', "BAO_TOKEN=$RootToken",
        'sh', '-ec',
        "printf '%s' '$encoded' | base64 -d > /tmp/policy.hcl && bao policy write $PolicyName /tmp/policy.hcl && rm -f /tmp/policy.hcl"
    )
    if ($write.ExitCode -ne 0) {
        throw "OpenBao policy '$PolicyName' installation failed (exit $($write.ExitCode))."
    }
}

function Set-OpenBaoSecretMap {
    param(
        [Parameter(Mandatory)][string]$RootToken,
        [Parameter(Mandatory)][string]$Prefix,
        [Parameter(Mandatory)][string]$SecretName,
        [Parameter(Mandatory)][hashtable]$Fields
    )

    $pairs = @()
    foreach ($key in $Fields.Keys) {
        $value = [string]$Fields[$key]
        Register-OpenBaoRedactionSecret $value
        $pairs += "$key=$value"
    }
    $put = Invoke-OpenBaoPodRaw (@('env', "BAO_TOKEN=$RootToken") + @(
            'bao', 'kv', 'put', "-mount=secret", "$Prefix/$SecretName") + $pairs)
    if ($put.ExitCode -ne 0) {
        throw "OpenBao seed of '$Prefix/$SecretName' failed (exit $($put.ExitCode))."
    }
}

function New-OpenBaoScopedToken {
    param(
        [Parameter(Mandatory)][string]$RootToken,
        [Parameter(Mandatory)][string]$PolicyName
    )

    $create = Invoke-OpenBaoPodRaw @(
        'env', "BAO_TOKEN=$RootToken",
        'bao', 'token', 'create',
        '-policy', $PolicyName,
        '-orphan',
        '-no-default-policy',
        '-ttl=24h',
        '-explicit-max-ttl=24h',
        '-format=json'
    )
    if ($create.ExitCode -ne 0) {
        throw "OpenBao scoped token creation for '$PolicyName' failed (exit $($create.ExitCode))."
    }
    try {
        $createJson = $create.Output | ConvertFrom-Json
    }
    catch {
        throw "OpenBao scoped token creation for '$PolicyName' returned non-JSON output at stage openbao-scoped-tokens."
    }
    $token = [string]$createJson.auth.client_token
    if ([string]::IsNullOrWhiteSpace($token)) {
        throw "OpenBao scoped token creation for '$PolicyName' returned an empty token."
    }
    Register-OpenBaoRedactionSecret $token
    return $token
}

function Assert-OpenBaoAclDenial {
    param(
        [Parameter(Mandatory)]$Result,
        [Parameter(Mandatory)][string]$Context
    )

    if ($Result.ExitCode -eq 0) {
        throw "$Context unexpectedly succeeded (fail-closed broken)."
    }

    $text = [string]$Result.Output
    if ($text -notmatch '(?i)permission denied|not authorized|permission to perform|access denied|\b403\b') {
        $detail = if (Get-Command Protect-EvidenceText -ErrorAction SilentlyContinue) {
            Protect-EvidenceText $text
        }
        else {
            'redacted non-ACL diagnostic'
        }
        throw "$Context failed with a non-ACL error (exit $($Result.ExitCode)); refusing to treat transport/CLI failure as isolation success. $detail"
    }
}

function Assert-OpenBaoPrefixIsolation {
    param(
        [Parameter(Mandatory)][string]$RuntimeToken,
        [Parameter(Mandatory)][string]$AccessToken
    )

    Set-VerificationStage 'openbao-access-isolation'

    $runtimeAllowed = Invoke-OpenBaoPodRaw @(
        'env', "BAO_TOKEN=$RuntimeToken",
        'bao', 'kv', 'get', '-mount=secret', '-field=password', "$script:OpenBaoRuntimePrefix/redis-secret")
    if ($runtimeAllowed.ExitCode -ne 0) {
        throw 'Runtime OpenBao identity could not read the allow-listed runtime redis-secret password field.'
    }
    Register-OpenBaoRedactionSecret $runtimeAllowed.Output.Trim()

    $runtimeDenied = Invoke-OpenBaoPodRaw @(
        'env', "BAO_TOKEN=$RuntimeToken",
        'bao', 'kv', 'get', '-mount=secret', '-field=access-telemetry-marker-key',
        "$script:OpenBaoAccessPrefix/access-telemetry-marker-key")
    Assert-OpenBaoAclDenial -Result $runtimeDenied -Context 'Runtime OpenBao identity cross-prefix access-telemetry read'

    $accessAllowed = Invoke-OpenBaoPodRaw @(
        'env', "BAO_TOKEN=$AccessToken",
        'bao', 'kv', 'get', '-mount=secret', '-field=access-telemetry-marker-key',
        "$script:OpenBaoAccessPrefix/access-telemetry-marker-key")
    if ($accessAllowed.ExitCode -ne 0) {
        throw 'Access-telemetry OpenBao identity could not read its allow-listed marker secret.'
    }
    Register-OpenBaoRedactionSecret $accessAllowed.Output.Trim()

    $accessDenied = Invoke-OpenBaoPodRaw @(
        'env', "BAO_TOKEN=$AccessToken",
        'bao', 'kv', 'get', '-mount=secret', '-field=password',
        "$script:OpenBaoRuntimePrefix/redis-secret")
    Assert-OpenBaoAclDenial -Result $accessDenied -Context 'Access-telemetry OpenBao identity cross-prefix runtime read'
}

function Publish-OpenBaoBootstrapSecrets {
    param(
        [Parameter(Mandatory)][string]$ApplicationNamespace,
        [Parameter(Mandatory)][string]$RuntimeToken,
        [Parameter(Mandatory)][string]$AccessToken,
        [Parameter(Mandatory)][string]$CaPem
    )

    Set-VerificationStage 'openbao-bootstrap-secrets'
    $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('openbao-bootstrap-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
    try {
        $caPath = Join-Path $tempRoot 'ca.pem'
        $runtimeTokenPath = Join-Path $tempRoot 'runtime.token'
        $accessTokenPath = Join-Path $tempRoot 'access.token'
        Set-Content -LiteralPath $caPath -Value $CaPem -Encoding ascii -NoNewline
        Set-Content -LiteralPath $runtimeTokenPath -Value $RuntimeToken -Encoding ascii -NoNewline
        Set-Content -LiteralPath $accessTokenPath -Value $AccessToken -Encoding ascii -NoNewline

        foreach ($pair in @(
                @{ Name = 'openbao-runtime-bootstrap'; TokenPath = $runtimeTokenPath },
                @{ Name = 'openbao-access-telemetry-bootstrap'; TokenPath = $accessTokenPath }
            )) {
            $yaml = Invoke-Checked kubectl @(
                'create', 'secret', 'generic', $pair.Name,
                '--namespace', $ApplicationNamespace,
                ('--from-file=token=' + $pair.TokenPath),
                ('--from-file=ca.pem=' + $caPath),
                '--dry-run=client', '-o', 'yaml')
            $yaml | & kubectl apply -f - | Out-Null
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to apply bootstrap Secret/$($pair.Name)."
            }
        }
    }
    finally {
        if (Test-Path -LiteralPath $tempRoot) {
            Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

function Invoke-DisposableOpenBaoBootstrap {
    param(
        [Parameter(Mandatory)][string]$ApplicationNamespace,
        [Parameter(Mandatory)][string]$ClusterName,
        [Parameter(Mandatory)][string]$EvidencePath,
        [string]$Image = $script:OpenBaoPinnedImage
    )

    Assert-DisposableClusterContext $ClusterName -RefusalPrefix 'Refusing to stage disposable OpenBao'
    Set-VerificationStage 'openbao-bootstrap'

    if ($Image -cne $script:OpenBaoPinnedImage) {
        throw "OPENBAO image argument '$Image' does not match the immutable pinned image '$script:OpenBaoPinnedImage'."
    }
    $Image = $script:OpenBaoPinnedImage

    $workDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('openbao-verify-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $workDirectory -Force | Out-Null
    $bootstrapRecord = [ordered]@{
        schemaVersion = 1
        image = $Image
        endpoint = "https://$script:OpenBaoServiceDns`:8200"
        tlsVerify = $true
        skipVerify = $false
        runtimePrefix = $script:OpenBaoRuntimePrefix
        accessTelemetryPrefix = $script:OpenBaoAccessPrefix
        runtimePolicy = $script:OpenBaoRuntimePolicy
        accessTelemetryPolicy = $script:OpenBaoAccessPolicy
        stages = @()
        seededSecretNames = @{
            runtime = @()
            accessTelemetry = @()
        }
        isolation = [ordered]@{
            runtimeAllowed = $false
            runtimeDeniedCrossPrefix = $false
            accessAllowed = $false
            accessDeniedCrossPrefix = $false
        }
        bootstrapSecretNames = @('openbao-runtime-bootstrap', 'openbao-access-telemetry-bootstrap')
        bootstrapSecretKeys = @('token', 'ca.pem')
    }

    try {
        $tls = New-OpenBaoTlsMaterial -WorkDirectory $workDirectory
        Deploy-DisposableOpenBaoService -TlsMaterial $tls -Image $Image
        $bootstrapRecord.stages += 'service-ready'

        $session = Initialize-DisposableOpenBao -CaPem $tls.CaPem
        $bootstrapRecord.stages += @('initialized', 'unsealed', 'kv-v2')

        Set-VerificationStage 'openbao-policies'
        Install-OpenBaoReadPolicy -RootToken $session.RootToken -PolicyName $script:OpenBaoRuntimePolicy -Prefix $script:OpenBaoRuntimePrefix
        Install-OpenBaoReadPolicy -RootToken $session.RootToken -PolicyName $script:OpenBaoAccessPolicy -Prefix $script:OpenBaoAccessPrefix
        $bootstrapRecord.stages += 'policies'

        Set-VerificationStage 'openbao-seed'
        $markerValue = 'verification-access-telemetry-marker'
        Register-OpenBaoRedactionSecret $markerValue

        Set-OpenBaoSecretMap -RootToken $session.RootToken -Prefix $script:OpenBaoRuntimePrefix -SecretName 'redis-secret' -Fields @{
            password = 'verification-redis-password'
            'falkordb-password' = 'verification-falkordb-password'
        }
        Set-OpenBaoSecretMap -RootToken $session.RootToken -Prefix $script:OpenBaoRuntimePrefix -SecretName 'llm-secret' -Fields @{
            OPENAI_API_KEY = 'verification-openai-key'
        }
        Set-OpenBaoSecretMap -RootToken $session.RootToken -Prefix $script:OpenBaoRuntimePrefix -SecretName 'google-embedding-api-key' -Fields @{
            'google-embedding-api-key' = 'verification-google-key'
        }
        Set-OpenBaoSecretMap -RootToken $session.RootToken -Prefix $script:OpenBaoRuntimePrefix -SecretName 'memories-embedding-client-secret' -Fields @{
            'memories-embedding-client-secret' = 'verification-embedding-secret'
        }
        $bootstrapRecord.seededSecretNames.runtime = @(
            'redis-secret', 'llm-secret', 'google-embedding-api-key', 'memories-embedding-client-secret'
        )

        Set-OpenBaoSecretMap -RootToken $session.RootToken -Prefix $script:OpenBaoAccessPrefix -SecretName 'redis-secret' -Fields @{
            password = 'verification-redis-password'
        }
        Set-OpenBaoSecretMap -RootToken $session.RootToken -Prefix $script:OpenBaoAccessPrefix -SecretName 'access-telemetry-marker-key' -Fields @{
            'access-telemetry-marker-key' = $markerValue
        }
        $bootstrapRecord.seededSecretNames.accessTelemetry = @('redis-secret', 'access-telemetry-marker-key')
        $bootstrapRecord.stages += 'seeded'

        Set-VerificationStage 'openbao-scoped-tokens'
        $runtimeToken = New-OpenBaoScopedToken -RootToken $session.RootToken -PolicyName $script:OpenBaoRuntimePolicy
        $accessToken = New-OpenBaoScopedToken -RootToken $session.RootToken -PolicyName $script:OpenBaoAccessPolicy
        $bootstrapRecord.stages += 'scoped-tokens'

        Assert-OpenBaoPrefixIsolation -RuntimeToken $runtimeToken -AccessToken $accessToken
        $bootstrapRecord.isolation.runtimeAllowed = $true
        $bootstrapRecord.isolation.runtimeDeniedCrossPrefix = $true
        $bootstrapRecord.isolation.accessAllowed = $true
        $bootstrapRecord.isolation.accessDeniedCrossPrefix = $true
        $bootstrapRecord.stages += 'isolation-verified'

        Publish-OpenBaoBootstrapSecrets `
            -ApplicationNamespace $ApplicationNamespace `
            -RuntimeToken $runtimeToken `
            -AccessToken $accessToken `
            -CaPem $session.CaPem
        $bootstrapRecord.stages += 'bootstrap-secrets'

        Set-VerificationStage 'openbao-revoke-root'
        $revoke = Invoke-OpenBaoPodRaw @('env', "BAO_TOKEN=$($session.RootToken)", 'bao', 'token', 'revoke', '-self')
        if ($revoke.ExitCode -ne 0) {
            throw "OpenBao root-token revoke failed at stage openbao-revoke-root (exit $($revoke.ExitCode))."
        }
        $bootstrapRecord.stages += 'root-revoked'

        $bootstrapRecord | ConvertTo-Json -Depth 6 |
            Set-Content -LiteralPath (Join-Path $EvidencePath 'openbao-bootstrap.json') -Encoding utf8
        Write-Host 'Disposable TLS OpenBao staged, seeded, and bootstrap Secrets published.'
    }
    catch {
        try {
            $bootstrapRecord.errorStage = $script:verificationStage
            $bootstrapRecord.error = (Protect-EvidenceText $_.Exception.Message)
            $bootstrapRecord | ConvertTo-Json -Depth 6 |
                Set-Content -LiteralPath (Join-Path $EvidencePath 'openbao-bootstrap.json') -Encoding utf8
        }
        catch {
            Write-Warning "Unable to persist OpenBao bootstrap evidence: $(Protect-EvidenceText $_.Exception.Message)"
        }
        throw
    }
    finally {
        if (Test-Path -LiteralPath $workDirectory) {
            Remove-Item -LiteralPath $workDirectory -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
