[CmdletBinding()]
param(
    [string]$Registry = 'registry.hexalith.com',

    [string[]]$Repositories = @(
        'memories',
        'memories-mcp',
        'memories-access-telemetry',
        'memories-access-telemetry-clock')
)

$ErrorActionPreference = 'Stop'

# release-preflight.ps1 invokes semantic-release only to classify the next version. Semantic-release
# still calls verifyRelease plugins in dry-run mode, so that subprocess marks itself explicitly.
# The actual publish-capable semantic-release step does not receive this process-scoped flag.
if ($env:HEXALITH_RELEASE_CLASSIFICATION_ONLY -eq 'true') {
    Write-Host 'Registry write-scope verification is deferred from release classification to the publish-capable semantic-release run.'
    exit 0
}

if (-not $PSBoundParameters.ContainsKey('Registry') -and
    -not [string]::IsNullOrWhiteSpace($env:HEXALITH_ZOT_REGISTRY)) {
    $Registry = $env:HEXALITH_ZOT_REGISTRY
}

$username = $env:HEXALITH_ZOT_USERNAME
$apiKey = $env:HEXALITH_ZOT_API_KEY
if ([string]::IsNullOrWhiteSpace($username) -or [string]::IsNullOrWhiteSpace($apiKey)) {
    throw 'Container publication requires HEXALITH_ZOT_USERNAME and HEXALITH_ZOT_API_KEY.'
}

if ([string]::IsNullOrWhiteSpace($Registry)) {
    throw 'Container registry must not be empty.'
}

$registryUriText = if ($Registry -match '^https?://') {
    $Registry
}
else {
    "https://$Registry"
}

$registryUri = [Uri]::new($registryUriText.TrimEnd('/') + '/', [UriKind]::Absolute)
if ($registryUri.Scheme -notin @('http', 'https') -or
    $registryUri.AbsolutePath -ne '/' -or
    -not [string]::IsNullOrEmpty($registryUri.Query) -or
    -not [string]::IsNullOrEmpty($registryUri.Fragment)) {
    throw "Container registry '$Registry' must be an HTTP(S) origin without a path, query, or fragment."
}

if ($Repositories.Count -eq 0) {
    throw 'At least one container repository is required.'
}

foreach ($repository in $Repositories) {
    if ($repository -notmatch '^[a-z0-9]+(?:[._-][a-z0-9]+)*(?:/[a-z0-9]+(?:[._-][a-z0-9]+)*)*$') {
        throw "Container repository '$repository' is not a safe OCI repository name."
    }
}

$credentialBytes = [Text.Encoding]::UTF8.GetBytes("$username`:$apiKey")
$credential = [Convert]::ToBase64String($credentialBytes)
[Array]::Clear($credentialBytes, 0, $credentialBytes.Length)

$handler = [Net.Http.HttpClientHandler]::new()
$handler.AllowAutoRedirect = $false
$client = [Net.Http.HttpClient]::new($handler)
$client.Timeout = [TimeSpan]::FromSeconds(30)
$sessions = [Collections.Generic.List[object]]::new()
$probeFailure = $null
$cancellationFailures = [Collections.Generic.List[string]]::new()

function New-RegistryRequest {
    param(
        [Parameter(Mandatory)][Net.Http.HttpMethod]$Method,
        [Parameter(Mandatory)][Uri]$Uri,
        [switch]$IncludeAuthorization,
        [switch]$EmptyContent
    )

    $request = [Net.Http.HttpRequestMessage]::new($Method, $Uri)
    if ($IncludeAuthorization) {
        $request.Headers.Authorization = [Net.Http.Headers.AuthenticationHeaderValue]::new('Basic', $credential)
    }
    if ($EmptyContent) {
        $request.Content = [Net.Http.ByteArrayContent]::new([byte[]]::new(0))
    }

    return $request
}

try {
    try {
        $challengeUri = [Uri]::new($registryUri, 'v2/')
        $request = New-RegistryRequest -Method ([Net.Http.HttpMethod]::Get) -Uri $challengeUri
        try {
            $response = $client.Send($request)
            try {
                $statusCode = [int]$response.StatusCode
                if ($statusCode -notin @(200, 401)) {
                    throw "Container registry authentication negotiation failed with HTTP $statusCode at /v2/."
                }

                $basicChallenge = @($response.Headers.WwwAuthenticate |
                        Where-Object { [string]::Equals($_.Scheme, 'Basic', [StringComparison]::OrdinalIgnoreCase) } |
                        Select-Object -First 1)
                if ($basicChallenge.Count -ne 1 -or
                    [string]::IsNullOrWhiteSpace($basicChallenge[0].Parameter) -or
                    $basicChallenge[0].Parameter -notmatch '(?i)(?:^|,)\s*realm\s*=') {
                    throw 'Container registry /v2/ did not advertise a Basic WWW-Authenticate realm. Challenge-driven OCI clients would omit credentials from later write requests; ensure the ingress routes /v2/ to Zot without replacing its response headers.'
                }
            }
            finally {
                $response.Dispose()
            }
        }
        finally {
            $request.Dispose()
        }

        foreach ($repository in $Repositories) {
            $encodedRepository = ($repository.Split('/') | ForEach-Object { [Uri]::EscapeDataString($_) }) -join '/'
            $uploadUri = [Uri]::new($registryUri, "v2/$encodedRepository/blobs/uploads/")
            $request = New-RegistryRequest -Method ([Net.Http.HttpMethod]::Post) -Uri $uploadUri -IncludeAuthorization -EmptyContent
            try {
                $response = $client.Send($request)
                try {
                    if ([int]$response.StatusCode -ne 202) {
                        throw "Container registry write-scope verification failed for repository '$repository' with HTTP $([int]$response.StatusCode). Confirm the Zot username/API-key pair and grant push authorization for this repository."
                    }

                    $location = $response.Headers.Location
                    if ($null -eq $location) {
                        throw "Container registry write-scope verification for repository '$repository' returned HTTP 202 without an upload location."
                    }

                    $cancellationUri = if ($location.IsAbsoluteUri) {
                        $location
                    }
                    else {
                        [Uri]::new($registryUri, $location)
                    }

                    $sameOrigin =
                        [string]::Equals($cancellationUri.Scheme, $registryUri.Scheme, [StringComparison]::OrdinalIgnoreCase) -and
                        [string]::Equals($cancellationUri.Authority, $registryUri.Authority, [StringComparison]::OrdinalIgnoreCase)
                    $sessions.Add([pscustomobject]@{
                            Repository = $repository
                            Uri = $cancellationUri
                            IncludeAuthorization = $sameOrigin
                        })

                    if ($cancellationUri.Scheme -notin @('http', 'https') -or
                        ($registryUri.Scheme -eq 'https' -and $cancellationUri.Scheme -ne 'https')) {
                        throw "Container registry write-scope verification for repository '$repository' returned an unsafe upload location."
                    }
                }
                finally {
                    $response.Dispose()
                }
            }
            finally {
                $request.Dispose()
            }
        }
    }
    catch {
        $probeFailure = $_.Exception.Message
    }
    finally {
        foreach ($session in $sessions) {
            $request = New-RegistryRequest -Method ([Net.Http.HttpMethod]::Delete) -Uri $session.Uri `
                -IncludeAuthorization:$session.IncludeAuthorization -EmptyContent
            try {
                try {
                    $response = $client.Send($request)
                    try {
                        if ([int]$response.StatusCode -ne 204) {
                            $cancellationFailures.Add("repository '$($session.Repository)' returned HTTP $([int]$response.StatusCode)")
                        }
                    }
                    finally {
                        $response.Dispose()
                    }
                }
                catch {
                    $cancellationFailures.Add("repository '$($session.Repository)' cancellation failed")
                }
            }
            finally {
                $request.Dispose()
            }
        }
    }
}
finally {
    $client.Dispose()
    $handler.Dispose()
    $credential = $null
}

if ($cancellationFailures.Count -gt 0) {
    $cancellationFailure = $cancellationFailures -join '; '
    if ($null -ne $probeFailure) {
        throw "$probeFailure Upload-session cleanup also failed: $cancellationFailure."
    }

    throw "Container registry write-scope verification could not cancel every upload session: $cancellationFailure."
}

if ($null -ne $probeFailure) {
    throw $probeFailure
}

Write-Host "Verified container registry Basic authentication negotiation and write scope for $($Repositories.Count) repositories at $($registryUri.Authority)."
