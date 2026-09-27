param([string]$Email = 'admin@school.local')
$ErrorActionPreference = 'Stop'
$connection = Read-Host 'SQL Server connection string' -AsSecureString
$password = Read-Host 'Initial SuperAdmin password (12+ characters)' -AsSecureString
function Get-PlainValue([Security.SecureString]$Value) {
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
}
$secretDir = Join-Path $env:APPDATA 'Microsoft\UserSecrets\SchoolPortfolioPR-Sar-2026'
New-Item -ItemType Directory -Force -Path $secretDir | Out-Null
@{ 'ConnectionStrings:DefaultConnection' = (Get-PlainValue $connection); 'SeedAdmin:Email' = $Email; 'SeedAdmin:Password' = (Get-PlainValue $password) } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $secretDir 'secrets.json') -Encoding UTF8
Write-Host 'Saved development configuration outside the repository.'
