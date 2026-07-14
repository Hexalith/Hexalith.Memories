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

$currentLogs = @(Get-ChildItem -LiteralPath $evidencePath -File -Filter '*-current.log')
$previousLogs = @(Get-ChildItem -LiteralPath $evidencePath -File -Filter '*-previous.log')
if ($currentLogs.Count -eq 0 -or $previousLogs.Count -eq 0) {
    throw 'Production deployment evidence must include current and previous container log captures.'
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
