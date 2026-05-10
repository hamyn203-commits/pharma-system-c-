#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Initializes the AlNeda project for first use.
.DESCRIPTION
    Runs restore, build, migration, and optionally imports legacy data.
.EXAMPLE
    ./init.ps1
    ./init.ps1 -ImportLegacy
#>

param(
    [switch]$ImportLegacy,
    [string]$LegacyDbPath = ""
)

$SolutionDir = Split-Path -Parent $PSScriptRoot
$ScriptDir = $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($LegacyDbPath)) {
    $LegacyDbPath = Join-Path $SolutionDir "pharmacy.db"
}

Write-Host "=== AlNeda Initialization ===" -ForegroundColor Cyan
Write-Host "Solution: $SolutionDir" -ForegroundColor Gray

# Step 1: Restore packages
Write-Host "`n[1/4] Restoring packages..." -ForegroundColor Yellow
dotnet restore (Join-Path $SolutionDir "AlNeda.sln")
if ($LASTEXITCODE -ne 0) { exit 1 }

# Step 2: Build
Write-Host "`n[2/4] Building..." -ForegroundColor Yellow
dotnet build (Join-Path $SolutionDir "AlNeda.sln") --configuration Debug --no-restore
if ($LASTEXITCODE -ne 0) { exit 1 }

# Step 3: Run EF Core migration
Write-Host "`n[3/4] Applying database migration..." -ForegroundColor Yellow
$env:Path = "$env:Path;$([System.Environment]::GetEnvironmentVariable('PATH','User'))"
dotnet ef database update --project (Join-Path $SolutionDir "src\AlNeda.Data\AlNeda.Data.csproj")
if ($LASTEXITCODE -ne 0) { Write-Warning "Migration failed. You may need to install dotnet-ef: dotnet tool install --global dotnet-ef" }

# Step 4: Import legacy data (optional)
if ($ImportLegacy) {
    Write-Host "`n[4/4] Importing legacy data from $LegacyDbPath..." -ForegroundColor Yellow
    if (Test-Path $LegacyDbPath) {
        dotnet test (Join-Path $SolutionDir "tests\AlNeda.Tests\AlNeda.Tests.csproj") --filter "LegacyImport"
    } else {
        Write-Warning "Legacy DB not found at $LegacyDbPath. Skipping import."
    }
} else {
    Write-Host "`n[4/4] Skipped (use -ImportLegacy to import from existing pharmacy.db)" -ForegroundColor Gray
}

Write-Host "`nInitialization complete!" -ForegroundColor Green
