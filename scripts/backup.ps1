#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Backs up the pharmacy database.
.DESCRIPTION
    Creates a timestamped backup copy of the current pharmacy.db file.
    Optionally cleans backups older than N days.
.EXAMPLE
    ./backup.ps1
    ./backup.ps1 -DbPath "D:\pharma_project\pharmacy.db" -BackupDir "D:\backups" -RetentionDays 30
#>

param(
    [string]$DbPath = "",
    [string]$BackupDir = "",
    [int]$RetentionDays = 0
)

# Auto-detect
if ([string]::IsNullOrEmpty($DbPath)) {
    $candidates = @(
        Join-Path (Split-Path -Parent $PSScriptRoot) "src\AlNeda.Data\pharmacy.db"
        "D:\pharma_project\pharmacy.db"
        ".\pharmacy.db"
    )
    foreach ($c in $candidates) {
        if (Test-Path $c) { $DbPath = $c; break }
    }
}

if ([string]::IsNullOrEmpty($BackupDir)) {
    $BackupDir = Join-Path (Split-Path -Parent $PSScriptRoot) "backups"
}

if ([string]::IsNullOrEmpty($DbPath) -or !(Test-Path $DbPath)) {
    Write-Error "Database not found. Specify -DbPath or place pharmacy.db in the project."
    exit 1
}

New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $BackupDir "pharmacy_$timestamp.db"

Copy-Item -Path $DbPath -Destination $backupFile -Force
Write-Host "Backup created: $backupFile" -ForegroundColor Green

# Cleanup old backups
if ($RetentionDays -gt 0) {
    $cutoff = (Get-Date).AddDays(-$RetentionDays)
    Get-ChildItem -Path $BackupDir -Filter "pharmacy_*.db" | Where-Object {
        $_.LastWriteTime -lt $cutoff
    } | ForEach-Object {
        Remove-Item $_.FullName -Force
        Write-Host "Removed old backup: $($_.Name)" -ForegroundColor Gray
    }
}
