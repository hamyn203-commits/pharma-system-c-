#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds the AlNeda solution.
.DESCRIPTION
    Builds the entire AlNeda solution in Release or Debug configuration.
    Supports -Configuration, -Clean switches.
.EXAMPLE
    ./build.ps1
    ./build.ps1 -Configuration Release
    ./build.ps1 -Clean
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$Clean,
    [switch]$RunTests
)

$SolutionDir = Split-Path -Parent $PSScriptRoot
$SolutionPath = Join-Path $SolutionDir "AlNeda.sln"

Write-Host "=== AlNeda Build Script ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Solution: $SolutionPath" -ForegroundColor Gray

if ($Clean) {
    Write-Host "`nCleaning..." -ForegroundColor Yellow
    dotnet clean $SolutionPath --configuration $Configuration -v q
}

Write-Host "`nRestoring packages..." -ForegroundColor Yellow
dotnet restore $SolutionPath
if ($LASTEXITCODE -ne 0) { Write-Host "Restore failed!" -ForegroundColor Red; exit 1 }

Write-Host "`nBuilding..." -ForegroundColor Yellow
dotnet build $SolutionPath --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { Write-Host "Build failed!" -ForegroundColor Red; exit 1 }

Write-Host "`nBuild succeeded!" -ForegroundColor Green

if ($RunTests) {
    Write-Host "`nRunning tests..." -ForegroundColor Yellow
    dotnet test $SolutionPath --configuration $Configuration --no-build
    if ($LASTEXITCODE -ne 0) { Write-Host "Tests failed!" -ForegroundColor Red; exit 1 }
    Write-Host "All tests passed!" -ForegroundColor Green
}
