# launch.json Path Fix Script
# Usage: Run this script in PowerShell

Write-Host "Fixing launch.json paths..." -ForegroundColor Cyan

$launchJsonPath = "d:\BC\xiangmu\demo4\demo4\.vscode\launch.json"

if (!(Test-Path $launchJsonPath)) {
    Write-Host "ERROR: launch.json not found" -ForegroundColor Red
    exit 1
}

# Read file content
$content = Get-Content $launchJsonPath -Raw

# Check if already fixed
if ($content -notmatch 'win-x64') {
    Write-Host "launch.json is already fixed" -ForegroundColor Green
    exit 0
}

# Backup original file
$backupPath = "$launchJsonPath.backup"
Copy-Item $launchJsonPath $backupPath -Force
Write-Host "Backed up original file" -ForegroundColor Yellow

# Replace paths (remove win-x64/ part)
$newContent = $content -replace '/bin/Debug/net8\.0/win-x64/', '/bin/Debug/net8.0/'

# Write new content
Set-Content -Path $launchJsonPath -Value $newContent -NoNewline -Encoding UTF8

Write-Host "Path fix completed!" -ForegroundColor Green
Write-Host ""
Write-Host "Verification:" -ForegroundColor Cyan
Get-Content $launchJsonPath | Select-String -Pattern 'program'

Write-Host ""
Write-Host "You can now press F5 to start debugging!" -ForegroundColor Green
