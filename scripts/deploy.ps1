#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploys the AlNeda application to a target directory.
.DESCRIPTION
    Builds the solution and copies output to the specified deployment path.
.EXAMPLE
    ./deploy.ps1 -TargetPath "C:\AlNeda"
#>

param(
    [Parameter(Mandatory)]
    [string]$TargetPath,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$SolutionDir = Split-Path -Parent $PSScriptRoot
$ScriptDir = $PSScriptRoot
$AdminOutput = Join-Path $SolutionDir "src\AlNeda.Admin\bin\$Configuration\net9.0-windows"

Write-Host "=== AlNeda Deploy Script ===" -ForegroundColor Cyan

# Build first
& "$ScriptDir\build.ps1" -Configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit 1 }

# Create target directory
New-Item -ItemType Directory -Path $TargetPath -Force | Out-Null

# Copy files excluding .pdb and dev files
Write-Host "`nDeploying to: $TargetPath" -ForegroundColor Yellow
Get-ChildItem -Path $AdminOutput -File | Where-Object {
    $_.Extension -notin @('.pdb', '.xml', '.config') -or
    $_.Name -like "AlNeda*"
} | Copy-Item -Destination $TargetPath -Force

# Copy database views script
Copy-Item -Path (Join-Path $SolutionDir "database\views\reporting_views.sql") -Destination $TargetPath -Force

Write-Host "Deploy complete! Files: $( (Get-ChildItem $TargetPath | Measure-Object).Count )" -ForegroundColor Green
