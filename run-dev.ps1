# Run API + web UI (requires .NET 8 SDK + Node). Dev API uses SQLite by default — see appsettings.Development.json.
$ErrorActionPreference = "Stop"
$repo = $PSScriptRoot

# Merge machine PATH so `dotnet` resolves in terminals that inherit a trimmed PATH (e.g. some IDE shells).
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" +
    [System.Environment]::GetEnvironmentVariable("Path", "User")

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
  Write-Host "The .NET SDK was not found. Install .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
  exit 1
}
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
  Write-Host "Node.js / npm was not found. Install Node LTS: https://nodejs.org/" -ForegroundColor Yellow
  exit 1
}

$backend = Join-Path $repo "backend"
$csproj = Join-Path $backend "InventoryManagement.Api\InventoryManagement.Api.csproj"
Write-Host "Starting API on http://127.0.0.1:5188 (matches Vite proxy in vite.config.ts)..." -ForegroundColor Cyan
Start-Process pwsh -ArgumentList @(
  "-NoExit",
  "-Command",
  "`$env:Path = [System.Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [System.Environment]::GetEnvironmentVariable('Path', 'User'); `$env:ASPNETCORE_ENVIRONMENT='Development'; Set-Location `"$backend`"; dotnet run --project `"$csproj`" --urls `"http://127.0.0.1:5188`" --no-launch-profile"
) | Out-Null

Set-Location (Join-Path $repo "frontend")
if (-not (Test-Path "node_modules")) { npm install }
Write-Host "Starting Vite on http://localhost:5173 ..." -ForegroundColor Cyan
npm run dev
