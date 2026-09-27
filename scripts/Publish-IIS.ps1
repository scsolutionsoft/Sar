param([string]$Output = "$PSScriptRoot\..\artifacts\publish")
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\SchoolPortfolioPR\SchoolPortfolioPR.csproj'
dotnet publish $project -c Release -r win-x64 --self-contained false -o $Output
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
Write-Host "Published to $Output"
Write-Host 'Install Chromium for the IIS app pool identity or configure a shared PLAYWRIGHT_BROWSERS_PATH before enabling PDF exports.'
