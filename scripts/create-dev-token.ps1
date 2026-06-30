param(
    [string]$UserId,
    [string]$ConfigPath = "src/OpenStore.Api/appsettings.Development.json"
)

$ErrorActionPreference = "Stop"

$configPath = Resolve-Path $ConfigPath -ErrorAction Stop
$config = Get-Content $configPath -Raw | ConvertFrom-Json

$signingKey = $config.DevJwt.SigningKey
$issuer = $config.DevJwt.Issuer
$audience = $config.DevJwt.Audience
$expirationMinutes = [int]$config.DevJwt.ExpirationMinutes

if (-not $UserId) {
    $UserId = [Guid]::NewGuid().ToString()
}

$now = [DateTimeOffset]::UtcNow
$expires = $now.AddMinutes($expirationMinutes)

$header = @{ alg = "HS256"; typ = "JWT" }
$payload = @{
    sub = $UserId
    iss = $issuer
    aud = $audience
    iat = [long]$now.ToUnixTimeSeconds()
    exp = [long]$expires.ToUnixTimeSeconds()
}

function Base64UrlEncode {
    param([byte[]]$bytes)
    return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

$headerJson = $header | ConvertTo-Json -Compress
$payloadJson = $payload | ConvertTo-Json -Compress

$headerBytes = [System.Text.Encoding]::UTF8.GetBytes($headerJson)
$payloadBytes = [System.Text.Encoding]::UTF8.GetBytes($payloadJson)

$headerEncoded = Base64UrlEncode $headerBytes
$payloadEncoded = Base64UrlEncode $payloadBytes

$signingInput = "$headerEncoded.$payloadEncoded"
$signingBytes = [System.Text.Encoding]::UTF8.GetBytes($signingInput)
$secretBytes = [System.Text.Encoding]::UTF8.GetBytes($signingKey)

$hmac = [System.Security.Cryptography.HMACSHA256]::new()
$hmac.Key = $secretBytes
$signature = $hmac.ComputeHash($signingBytes)
$signatureEncoded = Base64UrlEncode $signature

$token = "$headerEncoded.$payloadEncoded.$signatureEncoded"

Write-Host "Token for user $UserId (expires $($expires.ToString('u'))):"
Write-Host $token
