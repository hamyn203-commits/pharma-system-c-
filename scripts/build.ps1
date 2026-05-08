#Requires -RunAsAdministrator
param(
    [string]$Configuration = "Release",
    [string]$OutputPath = ".\publish",
    [switch]$SkipZip
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  AlNeda Pharmacy - Build & Publish" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$SolutionDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Join-Path $SolutionDir "src\AlNeda.Admin"

if (-not (Test-Path $ProjectDir)) {
    $ProjectDir = "D:\New folder (3)\src\AlNeda.Admin"
}

Write-Host "[1/4] Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path $OutputPath) {
    Remove-Item -Path $OutputPath -Recurse -Force
    New-Item -ItemType Directory -Path $OutputPath | Out-Null
}

Write-Host "[2/4] Restoring dependencies..." -ForegroundColor Yellow
Push-Location $ProjectDir
try {
    dotnet restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed" }
}
finally {
    Pop-Location
}

Write-Host "[3/4] Building ($Configuration)..." -ForegroundColor Yellow
Push-Location $ProjectDir
try {
    dotnet build -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }
}
finally {
    Pop-Location
}

Write-Host "[4/4] Publishing..." -ForegroundColor Yellow
$PublishPath = Join-Path $OutputPath "AlNeda"
Push-Location $ProjectDir
try {
    dotnet publish -c $Configuration -o $PublishPath --self-contained true -r win-x64
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }
}
finally {
    Pop-Location
}

if (-not $SkipZip) {
    Write-Host "Creating ZIP archive..." -ForegroundColor Yellow
    $ZipPath = Join-Path $OutputPath "AlNeda-$((Get-Date).ToString('yyyyMMdd-HHmmss')).zip"
    Compress-Archive -Path $PublishPath -DestinationPath $ZipPath -Force
    Write-Host "Created: $ZipPath" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Build completed successfully!" -ForegroundColor Green
Write-Host "  Output: $PublishPath" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green