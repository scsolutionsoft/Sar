$ErrorActionPreference = 'Stop'
$secretPath = Join-Path $env:APPDATA 'Microsoft\UserSecrets\SchoolPortfolioPR-Sar-2026\secrets.json'
$settings = Get-Content -Raw -LiteralPath $secretPath | ConvertFrom-Json
Write-Host ('Email: ' + $settings.'SeedAdmin:Email')
Write-Host ('Password: ' + $settings.'SeedAdmin:Password')
