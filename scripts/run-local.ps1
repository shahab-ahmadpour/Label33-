<#
.SYNOPSIS
  Run 33 Label locally with database + migrations (Windows).

.PARAMETER Mode
  LocalDb   - SQL Server LocalDB (default; Visual Studio / SQL LocalDB)
  DockerSql - SQL Server in Docker Compose
  Sqlite    - file DB, no SQL Server (smoke only; uses EnsureCreated)

.EXAMPLE
  .\scripts\run-local.ps1
  .\scripts\run-local.ps1 -Mode DockerSql
  .\scripts\run-local.ps1 -Mode Sqlite
#>
param(
    [ValidateSet('LocalDb', 'DockerSql', 'Sqlite')]
    [string]$Mode = 'LocalDb',
    [string]$Urls = 'http://localhost:5072'
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

Write-Host "==> Restoring..." -ForegroundColor Cyan
dotnet restore Label33.sln | Out-Host

function Ensure-DotnetEf {
    $ef = Get-Command dotnet-ef -ErrorAction SilentlyContinue
    if ($ef) { return }
    Write-Host "==> Installing dotnet-ef tool..." -ForegroundColor Cyan
    dotnet tool install -g dotnet-ef --version 8.0.11
    if ($LASTEXITCODE -ne 0) {
        dotnet tool update -g dotnet-ef --version 8.0.11
    }
    $tools = Join-Path $env:USERPROFILE ".dotnet\tools"
    if ($env:PATH -notlike "*$tools*") {
        $env:PATH = "$tools;$env:PATH"
    }
    if (-not (Get-Command dotnet-ef -ErrorAction SilentlyContinue)) {
        throw "dotnet-ef still not found. Close PowerShell, reopen, then run: dotnet tool install -g dotnet-ef --version 8.0.11"
    }
}

switch ($Mode) {
    'LocalDb' {
        $env:ASPNETCORE_ENVIRONMENT = 'Local'
        Write-Host "==> Mode: LocalDB + EF migrations" -ForegroundColor Cyan
        Ensure-DotnetEf
        Write-Host "==> Applying migrations..." -ForegroundColor Cyan
        dotnet ef database update `
            --project src/Label33.Infrastructure `
            --startup-project src/Label33.Web
        if ($LASTEXITCODE -ne 0) {
            throw @"
Migration failed.
1) Confirm LocalDB: sqllocaldb start MSSQLLocalDB
2) Confirm EF tool: dotnet ef --version
"@
        }
    }
    'DockerSql' {
        $env:ASPNETCORE_ENVIRONMENT = 'DockerSql'
        Write-Host "==> Mode: Docker SQL Server + EF migrations" -ForegroundColor Cyan
        if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
            throw "Docker not found. Install Docker Desktop or use -Mode LocalDb"
        }
        Write-Host "==> Starting SQL Server container..." -ForegroundColor Cyan
        docker compose up -d sql
        Write-Host "==> Waiting for SQL Server..." -ForegroundColor Cyan
        Start-Sleep -Seconds 25
        Ensure-DotnetEf
        $ok = $false
        for ($i = 1; $i -le 20; $i++) {
            try {
                dotnet ef database update `
                    --project src/Label33.Infrastructure `
                    --startup-project src/Label33.Web
                if ($LASTEXITCODE -eq 0) { $ok = $true; break }
            } catch { }
            Write-Host "    retry $i/20..." -ForegroundColor DarkYellow
            Start-Sleep -Seconds 5
        }
        if (-not $ok) { throw "Could not apply migrations to Docker SQL Server." }
    }
    'Sqlite' {
        $env:ASPNETCORE_ENVIRONMENT = 'Development'
        Write-Host "==> Mode: Sqlite (EnsureCreated on startup, no SQL Server)" -ForegroundColor Cyan
    }
}

Write-Host ""
Write-Host "Storefront:  $Urls" -ForegroundColor Green
Write-Host "Ops Console: $Urls/ops-33-console/login" -ForegroundColor Green
Write-Host "SuperAdmin:  superadmin@33label.local / ChangeMe_33Label!" -ForegroundColor Green
Write-Host ""
Write-Host "==> Running web app..." -ForegroundColor Cyan
dotnet run --project src/Label33.Web --launch-profile $(if ($Mode -eq 'Sqlite') { 'http' } elseif ($Mode -eq 'DockerSql') { 'DockerSql' } else { 'Local' }) --urls $Urls
