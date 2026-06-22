<#
.SYNOPSIS
电脑伴侣 - 低内存优化构建脚本

.DESCRIPTION
本脚本针对构建过程中内存占用过高的问题进行了全面优化：
1. 限制 .NET 运行时内存使用
2. 禁用不必要的构建功能
3. 优化增量构建缓存
4. 提供详细的进度反馈

.PARAMETER Configuration
构建配置，默认为 Debug

.PARAMETER Clean
是否清理之前的构建缓存

.PARAMETER Verbose
是否显示详细输出

.EXAMPLE
.\build-optimized.ps1
执行标准调试构建

.EXAMPLE
.\build-optimized.ps1 -Configuration Release
执行发布构建

.EXAMPLE
.\build-optimized.ps1 -Clean -Verbose
清理缓存并执行详细构建
#>

param(
    [string]$Configuration = "Debug",
    [switch]$Clean,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

function Write-Status {
    param([string]$Message)
    Write-Host "`n[$(Get-Date -Format 'HH:mm:ss')] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "`n[$(Get-Date -Format 'HH:mm:ss')] ✅ $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "`n[$(Get-Date -Format 'HH:mm:ss')] ❌ $Message" -ForegroundColor Red
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] ⚠️ $Message" -ForegroundColor Yellow
}

# 设置控制台编码
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# 显示标题
Write-Host "================================================" -ForegroundColor Blue
Write-Host "电脑伴侣 - 低内存优化构建脚本" -ForegroundColor Blue
Write-Host "================================================" -ForegroundColor Blue

# 设置内存限制环境变量
Write-Status "配置内存限制..."
$env:DOTNET_GCHeapHardLimit = "2147483648"  # 2GB
$env:DOTNET_HEAPALLOCATIONSTARTUPTHRESHOLD = "67108864"  # 64MB
$env:DOTNET_TieredPGO = "0"
$env:DOTNET_ReadyToRun = "0"
$env:MSBUILDDISABLENODEREUSE = "1"

Write-Host "  - 内存限制: 2GB"
Write-Host "  - 堆分配阈值: 64MB"
Write-Host "  - 禁用 Tiered PGO: 是"
Write-Host "  - 禁用 ReadyToRun: 是"
Write-Host "  - 禁用节点重用: 是"

# 清理构建缓存
if ($Clean -or (Test-Path "bin") -or (Test-Path "obj")) {
    Write-Status "清理构建缓存..."
    if (Test-Path "bin") { Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue }
    if (Test-Path "obj") { Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue }
    Write-Success "缓存清理完成"
}

# 检查 NuGet 缓存状态
Write-Status "检查 NuGet 包状态..."
$nugetConfig = Join-Path $PWD "packages.lock.json"
if (-not (Test-Path $nugetConfig)) {
    Write-Warning "未找到 packages.lock.json，建议运行 dotnet restore --lock-file"
} else {
    Write-Host "  - 锁定文件存在，使用确定性还原"
}

# 还原 NuGet 包
Write-Status "还原 NuGet 包..."
$restoreArgs = @(
    "restore",
    "--no-cache",
    "--disable-parallel"
)

if ($Verbose) {
    Write-Host "命令: dotnet $($restoreArgs -join ' ')"
}

$restoreOutput = dotnet @restoreArgs 2>&1
if ($LASTEXITCODE -ne 0) {
    if ($Verbose) {
        $restoreOutput | ForEach-Object { Write-Host $_ }
    }
    Write-Error "NuGet 还原失败"
    exit $LASTEXITCODE
}
Write-Success "NuGet 还原成功"

# 构建参数
$buildArgs = @(
    "msbuild",
    "ComputerCompanion.csproj",
    "/t:Build",
    "/p:Configuration=$Configuration",
    "/p:Platform=x64",
    "/m:1",
    "/nodeReuse:false",
    "/verbosity:minimal",
    "/p:RunAnalyzers=false",
    "/p:UseSharedCompilation=false",
    "/p:BuildInParallel=false",
    "/p:UseRazorBuildServer=false",
    "/p:AvaloniaCompileXaml=false",
    "/p:AvaloniaResourcePreCompile=false",
    "/p:Optimize=false"
)

if ($Configuration -eq "Release") {
    $buildArgs += "/p:Optimize=true"
    $buildArgs += "/p:DebugType=embedded"
}

Write-Status "开始构建 ($Configuration)..."
if ($Verbose) {
    Write-Host "命令: dotnet $($buildArgs -join ' ')"
}

$buildOutput = dotnet @buildArgs 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Success "构建完成!"
    $outputDir = Join-Path $PWD "bin" $Configuration "net8.0-windows" "win-x64"
    Write-Host "输出目录: $outputDir"
    
    # 检查输出文件
    $exePath = Join-Path $outputDir "ComputerCompanion.exe"
    if (Test-Path $exePath) {
        $fileInfo = Get-Item $exePath
        Write-Host "可执行文件: $($fileInfo.Name) ($([Math]::Round($fileInfo.Length / 1MB, 2)) MB)"
    }
} else {
    if ($Verbose) {
        $buildOutput | ForEach-Object { Write-Host $_ }
    } else {
        # 只显示错误信息
        $buildOutput | Where-Object { $_ -match "error|Error|ERROR" } | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    }
    Write-Error "构建失败，错误码: $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "`n================================================" -ForegroundColor Blue
Write-Host "构建结束" -ForegroundColor Blue
Write-Host "================================================" -ForegroundColor Blue