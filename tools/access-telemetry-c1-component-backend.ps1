# Loaded by verify-access-telemetry-c1.ps1 after literal profile/mode validation.
# This collector observes identities and advertisements; it grants no behavioral credit.

function Assert-C1CredentialFields {
    param([AllowNull()][AllowEmptyCollection()][object]$Value)

    if ($null -eq $Value) { return }
    if ($Value -is [System.Array]) {
        foreach ($item in $Value) { Assert-C1CredentialFields -Value $item }
    }
    elseif ($Value -is [System.Management.Automation.PSCustomObject]) {
        foreach ($property in $Value.PSObject.Properties) {
            $credential = $property.Value
            $nonempty = $null -ne $credential
            if ($credential -is [string]) { $nonempty = $credential.Length -gt 0 }
            elseif ($credential -is [System.Array]) { $nonempty = $credential.Count -gt 0 }
            elseif ($credential -is [System.Management.Automation.PSCustomObject]) {
                $nonempty = @($credential.PSObject.Properties).Count -gt 0
            }
            if ($nonempty -and $property.Name -match '^(?i:authorization|dapr[-_]?api[-_]?token|password|passwd|token|secret|client[-_]?secret|access[-_]?token|connection[-_]?string)$') {
                throw 'secret-shaped-output'
            }
            Assert-C1CredentialFields -Value $credential
        }
    }
}

function Assert-C1DecodedOutputSafety {
    param([AllowEmptyString()][string]$Text)

    # Scan Unicode-escaped output on both streams, including discarded fields and error JSON,
    # before any source hashing. Structural validation remains the strict JSON parser's job.
    $decoded = [regex]::Replace($Text, '\\u([0-9a-fA-F]{4})', {
        param($match)
        return [string][char][Convert]::ToInt32($match.Groups[1].Value, 16)
    })
    Assert-SecretSafeOutput $decoded
    # Inspect each raw property occurrence so diagnostic prefixes or duplicate JSON keys
    # cannot hide an earlier credential value from the parsed-object guard.
    if ($decoded -match '(?i)"(?:authorization|dapr[-_]?api[-_]?token|password|passwd|token|secret|client[-_]?secret|access[-_]?token|connection[-_]?string)"\s*:\s*(?:"(?:[^"\\]|\\.)+"|\[\s*[^\s\]]|\{\s*[^\s\}]|(?:true|false)(?=\s|[,}\]]|$)|-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?(?=\s|[,}\]]|$))') {
        throw 'secret-shaped-output'
    }
    $jsonValue = $null
    try { $jsonValue = ConvertFrom-Json -InputObject $Text -Depth 30 -NoEnumerate }
    catch { }
    Assert-C1CredentialFields -Value $jsonValue
}

function Read-C1SelectedJson {
    param([string]$Json, [string]$FailureCode)

    try {
        Assert-MetadataJsonShape -Json $Json
        $value = ConvertFrom-Json -InputObject $Json -Depth 30
        Assert-SecretSafeMetadata -Value $value
        Assert-C1CredentialFields -Value $value
    }
    catch {
        if ($_.Exception.Message -eq 'secret-shaped-output') {
            throw 'secret-shaped-output'
        }
        throw $FailureCode
    }
    return $value
}

function Get-C1RequiredString {
    param([object]$Object, [string]$Name, [string]$Pattern, [string]$FailureCode)

    $value = Get-RequiredProperty -Object $Object -Names @($Name) -FailureCode $FailureCode
    if ($value -isnot [string] -or $value.Length -gt 512 -or $value -cnotmatch $Pattern) {
        throw $FailureCode
    }
    return $value
}

function Get-C1ComponentIdentity {
    param([string]$Context)

    # Go-template emits only selected safe fields. The connectionString value is never emitted,
    # including when an unexpected inline credential replaces or accompanies the reference.
    $projection = 'go-template={{printf "{\"apiVersion\":%q,\"kind\":%q,\"name\":%q,\"namespace\":%q,\"uid\":%q,\"resourceVersion\":%q,\"type\":%q,\"version\":%q,\"initTimeout\":%q,\"secretStore\":%q,\"scopes\":[" .apiVersion .kind .metadata.name .metadata.namespace .metadata.uid .metadata.resourceVersion .spec.type .spec.version .spec.initTimeout .auth.secretStore}}{{range $i, $v := .scopes}}{{if $i}},{{end}}{{printf "%q" $v}}{{end}}],"settings":[{{range .spec.metadata}}{{if or (eq .name "tablePrefix") (eq .name "metadataTableName") (eq .name "timeout") (eq .name "cleanupInterval") (eq .name "maxConns") (eq .name "connectionMaxIdleTime") (eq .name "actorStateStore")}}{{printf "{\"name\":%q,\"value\":%q,\"hasSecretReference\":" .name .value}}{{$hasRef := false}}{{range $key, $value := .}}{{if eq $key "secretKeyRef"}}{{$hasRef = true}}{{end}}{{end}}{{if $hasRef}}true{{else}}false{{end}}},{{end}}{{end}}null],"connectionReferences":[{{range .spec.metadata}}{{if eq .name "connectionString"}}{{printf "{\"name\":%q,\"secretName\":%q,\"secretKey\":%q,\"hasInlineValue\":" .name .secretKeyRef.name .secretKeyRef.key}}{{$hasInline := false}}{{range $key, $value := .}}{{if eq $key "value"}}{{$hasInline = true}}{{end}}{{end}}{{if $hasInline}}true{{else}}false{{end}}},{{end}}{{end}}null]}'
    $raw = Invoke-KubectlObservation -Purpose 'component-identity' -Arguments @(
        '--context', $Context, '-n', $namespace,
        'get', 'component', 'access-telemetry-store', '-o', $projection
    ) -SkipSourceHash
    $component = Read-C1SelectedJson -Json $raw -FailureCode 'malformed-component-json'
    $expectedFields = [ordered]@{
        apiVersion = 'dapr.io/v1alpha1'
        kind = 'Component'
        name = 'access-telemetry-store'
        namespace = $namespace
        type = 'state.postgresql'
        version = 'v2'
        initTimeout = '1m'
        secretStore = 'access-telemetry-secrets'
    }
    foreach ($field in $expectedFields.Keys) {
        $value = Get-C1RequiredString $component $field '^[A-Za-z0-9./_-]+$' 'component-identity-invalid'
        if ($value -cne $expectedFields[$field]) {
            throw 'component-identity-mismatch'
        }
    }
    $uid = Get-C1RequiredString $component 'uid' '^[A-Za-z0-9][A-Za-z0-9-]{0,127}$' 'component-uid-invalid'
    $resourceVersion = Get-C1RequiredString $component 'resourceVersion' '^[0-9]{1,128}$' 'component-resource-version-invalid'
    $scopes = @(ConvertTo-ValidatedStringArray -Value $component.scopes `
        -Pattern '^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$' -MaximumLength 128 `
        -AllowEmpty $false -FailureCode 'component-scopes-invalid')
    if ($scopes.Count -ne 1 -or $scopes[0] -cne $expectedAppId) {
        throw 'component-scopes-mismatch'
    }
    $expectedSettings = [ordered]@{
        tablePrefix = 'access_telemetry.lifecycle_'
        metadataTableName = 'access_telemetry.dapr_metadata'
        timeout = '3s'
        cleanupInterval = '5m'
        maxConns = '40'
        connectionMaxIdleTime = '5m'
        actorStateStore = 'true'
    }
    # The projection's final null keeps empty arrays observable and avoids comma ambiguities.
    if ($component.settings -isnot [System.Array] -or
        $component.settings.Count -ne ($expectedSettings.Count + 1) -or
        $null -ne $component.settings[-1]) {
        throw 'component-settings-invalid'
    }
    $settings = [ordered]@{}
    foreach ($entry in @($component.settings | Where-Object { $null -ne $_ })) {
        $name = Get-C1RequiredString $entry 'name' '^[A-Za-z][A-Za-z0-9]{0,63}$' 'component-settings-invalid'
        $value = Get-C1RequiredString $entry 'value' '^[A-Za-z0-9._]+$' 'component-settings-invalid'
        if (-not $expectedSettings.Contains($name) -or $settings.Contains($name) -or
            $entry.hasSecretReference -isnot [bool] -or $entry.hasSecretReference -or
            $value -cne $expectedSettings[$name]) {
            throw 'component-settings-mismatch'
        }
        $settings[$name] = $value
    }
    # Persist settings in deterministic manifest order for the before/after comparison.
    $orderedSettings = [ordered]@{}
    foreach ($name in $expectedSettings.Keys) {
        if (-not $settings.Contains($name)) { throw 'component-settings-invalid' }
        $orderedSettings[$name] = $settings[$name]
    }
    if ($component.connectionReferences -isnot [System.Array] -or
        $component.connectionReferences.Count -ne 2 -or
        $null -ne $component.connectionReferences[-1]) {
        throw 'component-connection-reference-invalid'
    }
    $reference = $component.connectionReferences[0]
    if ($null -eq $reference -or $reference.name -isnot [string] -or
        $reference.name -cne 'connectionString' -or
        $reference.secretName -isnot [string] -or $reference.secretName -cne 'access-telemetry-postgresql' -or
        $reference.secretKey -isnot [string] -or $reference.secretKey -cne 'connectionString' -or
        $reference.hasInlineValue -isnot [bool] -or $reference.hasInlineValue) {
        throw 'component-connection-reference-invalid'
    }
    $identity = [ordered]@{
        apiVersion = $component.apiVersion
        kind = $component.kind
        name = $component.name
        namespace = $component.namespace
        uid = $uid
        resourceVersion = $resourceVersion
        type = $component.type
        version = $component.version
        initTimeout = $component.initTimeout
        scopes = $scopes
        settings = $orderedSettings
        secretStore = $component.secretStore
        connectionReference = [ordered]@{
            name = $reference.secretName
            key = $reference.secretKey
        }
    }
    Add-SourceHash 'kubectl:component-identity:allowlisted' ($identity | ConvertTo-Json -Compress -Depth 8)
    return $identity
}

function Get-C1StablePods {
    param([string]$Context, [string]$Label, [string[]]$Containers, [string]$Purpose)

    $raw = Invoke-KubectlObservation -Purpose $Purpose -Arguments @(
        '--context', $Context, '-n', $namespace, 'get', 'pods',
        '-l', "app.kubernetes.io/name=$Label", '-o', $podIdentityOutput
    ) -SkipSourceHash
    $payload = ConvertFrom-PodIdentityProjection -Json $raw
    Assert-C1CredentialFields -Value $payload.items
    foreach ($pod in $payload.items) {
        [void](Get-C1RequiredString $pod.status 'phase' '^[A-Za-z]+$' 'running-pod-phase-invalid')
    }
    $pods = @($payload.items | Where-Object { $_.status.phase -ceq 'Running' })
    if ($pods.Count -eq 0) { throw 'no-running-target-pod' }
    $names = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $uids = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $identities = [System.Collections.Generic.List[object]]::new()
    foreach ($pod in $pods) {
        $name = Get-C1RequiredString $pod.metadata 'name' '^[a-z0-9][a-z0-9.-]{0,252}$' 'running-pod-identity-invalid'
        $uid = Get-C1RequiredString $pod.metadata 'uid' '^[A-Za-z0-9][A-Za-z0-9-]{0,127}$' 'running-pod-identity-invalid'
        if (-not $names.Add($name) -or -not $uids.Add($uid)) { throw 'duplicate-running-pod-identity' }
        if ($pod.metadata.labels.'app.kubernetes.io/name' -isnot [string] -or
            $pod.metadata.labels.'app.kubernetes.io/name' -cne $Label -or
            $null -ne $pod.metadata.deletionTimestamp -or
            $pod.status.conditions -isnot [System.Array] -or
            $pod.status.containerStatuses -isnot [System.Array]) {
            throw 'running-pod-not-stable'
        }
        foreach ($condition in $pod.status.conditions) {
            [void](Get-C1RequiredString $condition 'type' '^[A-Za-z0-9][A-Za-z0-9._/-]{0,255}$' 'running-pod-condition-invalid')
        }
        foreach ($status in $pod.status.containerStatuses) {
            [void](Get-C1RequiredString $status 'name' '^[a-z0-9][a-z0-9-]{0,62}$' 'running-pod-container-invalid')
        }
        $ready = @($pod.status.conditions | Where-Object { $_.type -ceq 'Ready' })
        if ($ready.Count -ne 1 -or $ready[0].status -isnot [string] -or $ready[0].status -cne 'True') {
            throw 'running-pod-not-stable'
        }
        $images = [ordered]@{}
        foreach ($container in $Containers) {
            $statuses = @($pod.status.containerStatuses | Where-Object { $_.name -ceq $container })
            if ($statuses.Count -ne 1 -or $statuses[0].ready -isnot [bool] -or -not $statuses[0].ready) {
                throw 'running-pod-containers-not-ready'
            }
            $imageId = Get-C1RequiredString $statuses[0] 'imageID' `
                '^(?:[A-Za-z0-9._:/@-]+)?sha256:[0-9a-f]{64}$' 'running-pod-image-invalid'
            # The closed OCI index and its authenticated linux/amd64 child describe the same
            # reviewed image. Preserve the raw imageID so a representation change still blocks.
            $postgresqlPattern = 'sha256:(?:3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a|d93de42662696f278fb34354b06fdaa90ad7ca3106d6f72fbd01d16da006d2cf)$'
            if ($ProfileId -ceq 'PG-ONPREM-2') {
                $postgresqlPattern = 'sha256:(?:5a5a84b19854a9ffaa54082c166ff4ec27473a361e496e5ea167f298f2da9722|0377e72c5289ed2f98cf61b1a9c2db9eb9d300317fe14244492fbc94343b3d04)$'
                if ($container -ceq 'daprd' -and
                    $imageId -cnotmatch 'sha256:(?:b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8|edbe3fc30d7efc90869411666fd03b70bb89eafed382bb37ff9a6de2fcab914b)$') {
                    throw 'runtime-image-mismatch'
                }
            }
            if ($container -ceq 'postgresql' -and $imageId -cnotmatch $postgresqlPattern) {
                throw 'backend-image-mismatch'
            }
            $images[$container] = $imageId
        }
        $identities.Add([ordered]@{ pod = $name; podUid = $uid; containerImageIds = $images })
    }
    $result = @($identities | Sort-Object { $_.pod } -CaseSensitive)
    Add-SourceHash "kubectl:${Purpose}:allowlisted" (ConvertTo-Json -InputObject $result -Compress -Depth 8)
    return $result
}

function Get-C1LoadedComponent {
    param([string]$Context, [string]$Pod)

    $probe = 'if [ -z "${DAPR_API_TOKEN:-}" ]; then echo "required runtime credential unavailable" >&2; exit 72; fi; metadata="$(wget -qO- --timeout=5 --header="dapr-api-token: ${DAPR_API_TOKEN}" http://127.0.0.1:3500/v1.0/metadata)" || exit $?; case "$metadata" in *"$DAPR_API_TOKEN"*) echo "secret-shaped-output" >&2; exit 73;; esac; printf "%s" "$metadata"'
    $raw = Invoke-KubectlObservation -Purpose "component-metadata:$Pod" -Arguments @(
        '--context', $Context, '-n', $namespace, 'exec', $Pod, '-c', 'lifecycle',
        '--', '/bin/sh', '-ec', $probe
    ) -SkipSourceHash
    $metadata = Read-C1SelectedJson -Json $raw -FailureCode 'malformed-metadata-json'
    $app = Get-C1RequiredString $metadata 'id' '^[A-Za-z0-9._-]+$' 'metadata-app-id-invalid'
    $runtime = Get-C1RequiredString $metadata 'runtimeVersion' '^[0-9]+\.[0-9]+\.[0-9]+$' 'metadata-runtime-version-invalid'
    if ($app -cne $expectedAppId -or $runtime -cne '1.18.1') { throw 'metadata-identity-mismatch' }
    if ($metadata.components -isnot [System.Array]) { throw 'metadata-components-invalid' }
    $names = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $selected = @()
    foreach ($component in $metadata.components) {
        $name = Get-C1RequiredString $component 'name' '^[A-Za-z0-9._-]+$' 'metadata-components-invalid'
        if (-not $names.Add($name)) { throw 'metadata-component-duplicate' }
        if ($name -ceq 'access-telemetry-store') { $selected += $component }
    }
    if ($selected.Count -ne 1) { throw 'metadata-component-missing' }
    $type = Get-C1RequiredString $selected[0] 'type' '^[A-Za-z0-9._-]+$' 'metadata-component-invalid'
    $version = Get-C1RequiredString $selected[0] 'version' '^v[0-9]+$' 'metadata-component-invalid'
    if ($type -cne 'state.postgresql' -or $version -cne 'v2') { throw 'metadata-component-mismatch' }
    $capabilities = @(ConvertTo-ValidatedStringArray -Value $selected[0].capabilities `
        -Pattern '^[A-Z][A-Z_]{0,63}$' -MaximumLength 64 -AllowEmpty $false `
        -FailureCode 'metadata-component-capabilities-invalid')
    # Dapr 1.18.1 adds ACTOR when ETAG and TRANSACTIONAL are advertised.
    # Preserve historical PG1 collection; require the exact PG2 runtime set.
    $expectedCapabilities = @('ETAG', 'KEYS_LIKE', 'TRANSACTIONAL', 'TTL')
    if ($ProfileId -ceq 'PG-ONPREM-2') { $expectedCapabilities = @('ACTOR', 'ETAG', 'KEYS_LIKE', 'TRANSACTIONAL', 'TTL') }
    if ((Get-CollectionIdentity $capabilities) -cne
        (Get-CollectionIdentity $expectedCapabilities)) {
        throw 'metadata-component-capabilities-mismatch'
    }
    $result = [ordered]@{
        pod = $Pod
        appId = $app
        runtimeVersion = $runtime
        name = $selected[0].name
        type = $type
        version = $version
        advertisedCapabilities = $capabilities
    }
    Add-SourceHash "kubectl:component-metadata:${Pod}:allowlisted" ($result | ConvertTo-Json -Compress -Depth 5)
    return $result
}

function Invoke-C1ComponentBackendCapture {
    $capturedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    $context = $null
    $observations = [ordered]@{ component = $null; lifecyclePods = @(); loadedComponents = @(); backend = $null }
    $producerStatus = 'blocked'
    $blockers = [System.Collections.Generic.List[string]]::new()
    try {
        foreach ($source in @($PSCommandPath, (Join-Path $PSScriptRoot 'verify-access-telemetry-c1.ps1'))) {
            $script:sourceLedger.Add([ordered]@{
                source = 'tools/' + [System.IO.Path]::GetFileName($source)
                sha256 = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
            })
        }
        if ($ProfileId -ceq 'PG-ONPREM-2') {
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
            $repositoryRoot = Split-Path $PSScriptRoot -Parent
            foreach ($relative in $approvedInputs.Keys) {
                $sourcePath = Join-Path $repositoryRoot $relative
                if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) { throw 'approved-profile-input-unavailable' }
                $sourceSha256 = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash.ToLowerInvariant()
                if ($sourceSha256 -cne $approvedInputs[$relative]) { throw 'approved-profile-input-drift' }
                $script:sourceLedger.Add([ordered]@{ source = $relative; sha256 = $sourceSha256 })
            }
        }
        $observedContext = Invoke-KubectlObservation -Purpose 'current-context' -Arguments @('config', 'current-context')
        if ($observedContext -cne $expectedContext) { throw 'profile-context-mismatch' }
        $context = $observedContext
        $component = Get-C1ComponentIdentity $context
        $lifecyclePods = @(Get-C1StablePods $context $expectedAppId @('lifecycle', 'daprd') 'lifecycle-pods')
        foreach ($container in @('lifecycle', 'daprd')) {
            $digests = @($lifecyclePods | ForEach-Object {
                [regex]::Match($_.containerImageIds[$container], 'sha256:[0-9a-f]{64}$').Value
            } | Sort-Object -CaseSensitive -Unique)
            if ($digests.Count -ne 1) { throw 'running-target-image-drift' }
        }
        $backendPods = @(Get-C1StablePods $context 'access-telemetry-postgresql' @('postgresql') 'backend-pods')
        if ($backendPods.Count -ne 1) { throw 'backend-pod-count-invalid' }
        $loaded = @()
        foreach ($pod in $lifecyclePods) { $loaded += Get-C1LoadedComponent $context $pod.pod }
        $backendPod = $backendPods[0].pod
        # -X disables psqlrc; the fixed local socket, no password prompt/file/environment,
        # and read-only settings use the manifest's postgres -> memories_admin peer map.
        $sql = "SELECT json_build_object('serverVersion', current_setting('server_version'), 'serverVersionNum', current_setting('server_version_num'), 'database', current_database(), 'user', current_user, 'systemUser', system_user, 'localSocket', inet_client_addr() IS NULL)::text;"
        $raw = Invoke-KubectlObservation -Purpose 'backend-server-identity' -Arguments @(
            '--context', $context, '-n', $namespace, 'exec', $backendPod, '-c', 'postgresql',
            '--', 'env', '-u', 'PGPASSWORD', '-u', 'PGSERVICE', '-u', 'PGSERVICEFILE',
            'PGPASSFILE=/dev/null', 'PGCONNECT_TIMEOUT=5',
            'PGOPTIONS=-c default_transaction_read_only=on -c statement_timeout=5000',
            'psql', '-X', '--no-password', '--set=ON_ERROR_STOP=1',
            '--host=/var/run/postgresql', '--port=5432', '--username=memories_admin',
            '--dbname=memories_access_telemetry', '--tuples-only', '--no-align', '--command', $sql
        ) -SkipSourceHash
        $server = Read-C1SelectedJson $raw 'malformed-backend-json'
        $versionPattern = '^18\.4(?: \([A-Za-z0-9.+~: _/-]+\))?$'
        $versionNumPattern = '^180004$'
        if ($ProfileId -ceq 'PG-ONPREM-2') {
            $versionPattern = '^18\.6(?: \([A-Za-z0-9.+~: _/-]+\))?$'
            $versionNumPattern = '^180006$'
        }
        $serverVersion = Get-C1RequiredString $server 'serverVersion' $versionPattern 'backend-version-invalid'
        $serverVersionNum = Get-C1RequiredString $server 'serverVersionNum' $versionNumPattern 'backend-version-invalid' 
        $database = Get-C1RequiredString $server 'database' '^memories_access_telemetry$' 'backend-database-invalid'
        $user = Get-C1RequiredString $server 'user' '^memories_admin$' 'backend-peer-user-invalid'
        $systemUser = Get-C1RequiredString $server 'systemUser' '^peer:postgres$' 'backend-peer-system-user-invalid'
        if ($server.localSocket -isnot [bool] -or -not $server.localSocket) { throw 'backend-local-socket-invalid' }
        $backend = [ordered]@{
            pod = $backendPod
            podUid = $backendPods[0].podUid
            imageId = $backendPods[0].containerImageIds.postgresql
            serverVersion = $serverVersion
            serverVersionNum = $serverVersionNum
            database = $database
            user = $user
            systemUser = $systemUser
            localSocket = $server.localSocket
        }
        Add-SourceHash 'kubectl:backend-server-identity:allowlisted' ($backend | ConvertTo-Json -Compress -Depth 4)
        $componentAfter = Get-C1ComponentIdentity $context
        if (($component | ConvertTo-Json -Compress -Depth 8) -cne
            ($componentAfter | ConvertTo-Json -Compress -Depth 8)) { throw 'component-changed' }
        $lifecycleAfter = @(Get-C1StablePods $context $expectedAppId @('lifecycle', 'daprd') 'lifecycle-pods-recheck')
        $backendAfter = @(Get-C1StablePods $context 'access-telemetry-postgresql' @('postgresql') 'backend-pods-recheck')
        if ((ConvertTo-Json -InputObject $lifecyclePods -Compress -Depth 8) -cne
            (ConvertTo-Json -InputObject $lifecycleAfter -Compress -Depth 8) -or
            (ConvertTo-Json -InputObject $backendPods -Compress -Depth 8) -cne
            (ConvertTo-Json -InputObject $backendAfter -Compress -Depth 8)) { throw 'running-pod-changed' }
        $observations = [ordered]@{
            component = $component
            lifecyclePods = $lifecyclePods
            loadedComponents = $loaded
            backend = $backend
        }
        $producerStatus = 'observed'
    }
    catch {
        $failure = [string]$_.Exception.Message
        if ($failure -notmatch '^[a-z0-9:-]+$') { $failure = 'producer-execution-failed' }
        $blockers.Add($failure)
    }
    $packet = [ordered]@{
        schemaVersion = 'hexalith.access-telemetry.c1.evidence/v1'
        gate = $Gate
        profileId = $ProfileId
        historicalProfileCapture = ($ProfileId -ceq 'PG-ONPREM-1')
        profileDisposition = $(if ($ProfileId -ceq 'PG-ONPREM-1') { 'closed-historical' } else { 'approved-current' })
        profileSha256 = $(if ($ProfileId -ceq 'PG-ONPREM-1') { 'dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14' } else { '7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe' })
        connectionLinkage = 'not-evaluated'
        capturedAtUtc = $capturedAtUtc
        context = $context
        namespace = $namespace
        targetSelector = $targetSelector
        backendSelector = 'app.kubernetes.io/name=access-telemetry-postgresql'
        producerStatus = $producerStatus
        gateStatus = 'not-evaluated'
        productionGatePassed = $false
        productionLifecycleWrites = 'not-evaluated'
        componentBehavior = 'not-evaluated'
        observations = $observations
        blockers = @($blockers)
        sources = @($script:sourceLedger)
        commands = @($script:commandLedger)
    }
    Write-Output (Write-ImmutablePacket $packet $EvidenceDirectory -FilePrefix 'c1.16-component-backend-identity')
    if ($producerStatus -ne 'observed') { exit 1 }
}
