param(
    [Parameter(Mandatory = $true)]
    [string] $TenantId,

    [int] $ExpiryMinutes = 15,

    [string] $SigningKey = ""
)

$ErrorActionPreference = "Stop"

function ConvertTo-Base64Url([byte[]] $Bytes) {
    [Convert]::ToBase64String($Bytes).TrimEnd("=").Replace("+", "-").Replace("/", "_")
}

if ([string]::IsNullOrWhiteSpace($SigningKey)) {
    $repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $settingsPath = Join-Path $repoRoot "src/Hexalith.Memories.Mcp/appsettings.Development.json"
    $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    $SigningKey = $settings.Authentication.JwtBearer.SigningKey
}

$issuer = "hexalith-memories-dev"
$audience = "hexalith-memories-mcp"
$now = [DateTimeOffset]::UtcNow
$header = @{ alg = "HS256"; typ = "JWT" } | ConvertTo-Json -Compress
$payload = @{
    sub = "dev-mcp-client"
    iss = $issuer
    aud = $audience
    iat = $now.ToUnixTimeSeconds()
    nbf = $now.ToUnixTimeSeconds()
    exp = $now.AddMinutes($ExpiryMinutes).ToUnixTimeSeconds()
    tenant_id = $TenantId
} | ConvertTo-Json -Compress

$header64 = ConvertTo-Base64Url ([Text.Encoding]::UTF8.GetBytes($header))
$payload64 = ConvertTo-Base64Url ([Text.Encoding]::UTF8.GetBytes($payload))
$unsigned = "$header64.$payload64"
$hmac = [Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($SigningKey))
$signature64 = ConvertTo-Base64Url ($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($unsigned)))

"Authorization: Bearer $unsigned.$signature64"
