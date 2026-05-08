#Requires -RunAsAdministrator
param(
    [Parameter(Mandatory=$false)]
    [string]$InstallPath = "$env:LOCALAPPDATA\AlNeda",
    
    [Parameter(Mandatory=$false)]
    [string]$ShortcutName = "AlNeda Pharmacy",
    
    [Parameter(Mandatory=$false)]
    [switch]$CreateDesktopShortcut,
    
    [Parameter(Mandatory=$false)]
    [switch]$CreateStartMenuShortcut,
    
    [Parameter(Mandatory=$false)]
    [switch]$Uninstall
)

$ErrorActionPreference = "Stop"

function Get-CdpErrors($logFile, $lastN = 5) {
    $err = @()
    if (Test-Path $logFile) {
        $content = Get-Content $logFile -Tail 100
        $err = $content | Where-Object { $_ -match "error|Error|ERROR|Exception" } | Select-Object -Last $lastN
    }
    return $err
}

if ($Uninstall) {
    Write-Host "Uninstalling AlNeda Pharmacy..." -ForegroundColor Yellow
    
    $exePath = Join-Path $InstallPath "AlNeda.exe"
    if (Test-Path $exePath) {
        Stop-Process -Name "AlNeda" -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        Remove-Item -Path $InstallPath -Recurse -Force -ErrorAction SilentlyContinue
    }
    
    $shutcutPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\AlNeda Pharmacy.lnk"
    if (Test-Path $shutcutPath) { Remove-Item $shutcutPath -Force }
    
    $desktopPath = "$env:USERPROFILE\Desktop\AlNeda Pharmacy.lnk"
    if (Test-Path $desktopPath) { Remove-Item $desktopPath -Force }
    
    Write-Host "Uninstalled successfully!" -ForegroundColor Green
    exit 0
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  AlNeda - Silent Install" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$SourcePath = Join-Path $PSScriptRoot "publish\AlNeda"
if (-not (Test-Path $SourcePath)) {
    $SourcePath = "D:\New folder (3)\publish\AlNeda"
}

if (-not (Test-Path $SourcePath)) {
    Write-Host "ERROR: Source path not found. Run build.ps1 first." -ForegroundColor Red
    exit 1
}

Write-Host "Installing to: $InstallPath" -ForegroundColor Yellow

New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null

Write-Host "Copying files..." -ForegroundColor Yellow
Copy-Item -Path "$SourcePath\*" -Destination $InstallPath -Recurse -Force

$exePath = Join-Path $InstallPath "AlNeda.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: AlNeda.exe not found after install" -ForegroundColor Red
    exit 1
}

Write-Host "Creating shortcuts..." -ForegroundColor Yellow

function New-Shortcut($ShortcutPath, $TargetPath) {
    $WshShell = New-Object -ComObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath = $TargetPath
    $Shortcut.WorkingDirectory = Split-Path $TargetPath
    $Shortcut.Description = "AlNeda Pharmacy Management"
    $Shortcut.Save()
}

if ($CreateDesktopShortcut -or -not $CreateStartMenuShortcut) {
    $desktopPath = "$env:USERPROFILE\Desktop\$ShortcutName.lnk"
    New-Shortcut -ShortcutPath $desktopPath -TargetPath $exePath
    Write-Host "  Desktop shortcut created"
}

if ($CreateStartMenuShortcut) {
    $startMenuPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\$ShortcutName.lnk"
    New-Shortcut -ShortcutPath $startMenuPath -TargetPath $exePath
    Write-Host "  Start Menu shortcut created"
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Install completed successfully!" -ForegroundColor Green
Write-Host "  Location: $InstallPath" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

$logDir = Join-Path $InstallPath "logs"
if (Test-Path $logDir) {
    $logFile = Get-ChildItem $logDir -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($logFile) {
        $errors = Get-CdpErrors $logFile.FullName
        if ($errors) {
            Write-Host ""
            Write-Host "Warnings detected:" -ForegroundColor Yellow
            $errors | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
        }
    }
}