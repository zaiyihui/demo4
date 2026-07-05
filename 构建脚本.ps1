<#
.SYNOPSIS
ComputerCompanion - Unified Build Script
#>

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("Build", "Run", "Publish", "Clean")]
    [string]$Action,
    
    [string]$Configuration = "Debug",
    
    [switch]$Clean = $true,
    
    [switch]$VerboseOutput
)

function Write-Status {
    param([string]$Message)
    Write-Host "`n[$(Get-Date -Format 'HH:mm:ss')] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $Message" -ForegroundColor Green
}

function Write-ErrorMsg {
    param([string]$Message)
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $Message" -ForegroundColor Red
}

function Write-WarningMsg {
    param([string]$Message)
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $Message" -ForegroundColor Yellow
}

function Set-MemoryOptimization {
    Write-Status "Configuring memory optimization..."
    $env:DOTNET_GCHeapHardLimit = "2147483648"
    $env:MSBUILDDISABLENODEREUSE = "1"
    $env:DOTNET_NoLogo = "1"
}

function Invoke-Clean {
    Write-Status "Cleaning build cache..."
    dotnet clean --verbosity minimal 2>&1 | Out-Null
    Write-Success "Cache cleaned"
}

function Invoke-Build {
    param([string]$Config)
    
    Set-MemoryOptimization
    
    if ($Clean) {
        Invoke-Clean
    }
    
    Write-Status "Building ($Config)..."
    
    $cmd = "dotnet build -c $Config /m:1 /nodeReuse:false -v minimal"
    if ($VerboseOutput) {
        Write-Host "Command: $cmd"
    }
    
    Invoke-Expression $cmd
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Build completed!"
        $outputDir = "$PWD\bin\$Config\net8.0-windows\win-x64"
        Write-Host "Output directory: $outputDir"
        
        $dllPath = "$outputDir\ComputerCompanion.dll"
        if (Test-Path $dllPath) {
            $fileInfo = Get-Item $dllPath
            $fileSize = [Math]::Round($fileInfo.Length / 1MB, 2)
            Write-Host "Output file: $($fileInfo.Name) ($fileSize MB)"
        }
    } else {
        Write-ErrorMsg "Build failed with code: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}

function Invoke-Run {
    Set-MemoryOptimization
    
    $outputPath = "$PWD\bin\$Configuration\net8.0-windows\win-x64\ComputerCompanion.dll"
    
    if (-not (Test-Path $outputPath)) {
        Write-WarningMsg "No compiled output found, building..."
        Invoke-Build -Config $Configuration
    }
    
    Write-Status "Starting application..."
    Write-Host "Working directory: $PWD"
    Write-Host "Output path: $outputPath"
    
    & dotnet $outputPath
    
    if ($LASTEXITCODE -ne 0) {
        Write-ErrorMsg "Application exited with code: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}

function Invoke-Publish {
    Set-MemoryOptimization
    
    if ($Clean) {
        Invoke-Clean
    }
    
    Write-Status "Publishing..."
    Write-Host "This may take several minutes, please wait..."
    
    $cmd = "dotnet publish -c Release -r win-x64 --self-contained true --output bin\Release\publish /p:PublishSingleFile=true /p:PublishTrimmed=true /m:1 /nodeReuse:false -v minimal"
    if ($VerboseOutput) {
        Write-Host "Command: $cmd"
    }
    
    Invoke-Expression $cmd
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Publish successful!"
        Write-Host ""
        Write-Host "Publish info:" -ForegroundColor Cyan
        Write-Host "----------------------------------------"
        Write-Host "Publish directory: bin\Release\publish"
        Write-Host "Main executable: ComputerCompanion.exe"
        Write-Host ""
        
        $exePath = "$PWD\bin\Release\publish\ComputerCompanion.exe"
        if (Test-Path $exePath) {
            $fileInfo = Get-Item $exePath
            $fileSize = [Math]::Round($fileInfo.Length / 1MB, 2)
            Write-Host "File size: $fileSize MB" -ForegroundColor Green
        }
    } else {
        Write-ErrorMsg "Publish failed with code: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}

Write-Host "================================================" -ForegroundColor Blue
Write-Host "ComputerCompanion - Unified Build Script" -ForegroundColor Blue
Write-Host "================================================" -ForegroundColor Blue
Write-Host "Action: $Action | Config: $Configuration" -ForegroundColor Cyan

try {
    switch ($Action) {
        "Build" {
            Invoke-Build -Config $Configuration
        }
        "Run" {
            Invoke-Run
        }
        "Publish" {
            Invoke-Publish
        }
        "Clean" {
            Invoke-Clean
        }
    }
} catch {
    Write-ErrorMsg "Execution failed: $_"
    exit 1
}

Write-Host "`n================================================" -ForegroundColor Blue
Write-Host "Operation completed" -ForegroundColor Blue
Write-Host "================================================" -ForegroundColor Blue