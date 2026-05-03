# Start InventoryManagement.Api (SQLite dev). Fixes trimmed PATH in some terminals.
param(
    [switch]$FreePort
)

$ErrorActionPreference = "Stop"

$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" +
    [System.Environment]::GetEnvironmentVariable("Path", "User")

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host @"

'dotnet' was not found in PATH.

Install .NET 8 SDK:
  winget install Microsoft.DotNet.SDK.8 --accept-package-agreements --accept-source-agreements

Then close and reopen this terminal (or sign out/in), or run dotnet from:
  ""C:\Program Files\dotnet\dotnet.exe""

"@ -ForegroundColor Yellow
    exit 1
}

if ($FreePort) {
    $pids = @(Get-NetTCPConnection -LocalPort 5188 -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique)
    foreach ($procId in $pids) {
        if ($procId -gt 0) {
            Write-Host "Stopping process $procId using port 5188..." -ForegroundColor Cyan
            Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
        }
    }
    Start-Sleep -Milliseconds 400
}

$env:ASPNETCORE_ENVIRONMENT = "Development"
Set-Location $PSScriptRoot

dotnet run --project "InventoryManagement.Api\InventoryManagement.Api.csproj" `
    --urls "http://127.0.0.1:5188" `
    --no-launch-profile
