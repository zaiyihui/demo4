# 清理构建缓存脚本
# 用于释放内存和磁盘空间

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "清理构建缓存" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 步骤 1：停止所有 dotnet 进程
Write-Host "[1/5] 停止 dotnet 进程..." -ForegroundColor Yellow
$dotnetProcesses = Get-Process dotnet -ErrorAction SilentlyContinue
if ($dotnetProcesses) {
    $dotnetProcesses | Stop-Process -Force
    Write-Host "  已停止 $($dotnetProcesses.Count) 个进程" -ForegroundColor Green
} else {
    Write-Host "  无 dotnet 进程运行" -ForegroundColor Gray
}

# 步骤 2：清理项目输出
Write-Host "[2/5] 清理项目输出..." -ForegroundColor Yellow
$foldersToClean = @("bin", "obj")
foreach ($folder in $foldersToClean) {
    if (Test-Path $folder) {
        Remove-Item -Recurse $folder -Force -ErrorAction SilentlyContinue
        Write-Host "  已删除 $folder" -ForegroundColor Green
    }
}

# 步骤 3：清理 NuGet 缓存
Write-Host "[3/5] 清理 NuGet 缓存..." -ForegroundColor Yellow
try {
    dotnet nuget locals all --clear 2>$null
    Write-Host "  NuGet 缓存已清理" -ForegroundColor Green
} catch {
    Write-Host "  NuGet 缓存清理失败: $_" -ForegroundColor Red
}

# 步骤 4：清理 MSBuild 临时文件
Write-Host "[4/5] 清理 MSBuild 临时文件..." -ForegroundColor Yellow
$tempPath = $env:TEMP
$msbuildFolders = Get-ChildItem -Path $tempPath -Filter "msbuild*" -Directory -ErrorAction SilentlyContinue
if ($msbuildFolders) {
    $msbuildFolders | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  已删除 $($msbuildFolders.Count) 个 MSBuild 临时文件夹" -ForegroundColor Green
} else {
    Write-Host "  无 MSBuild 临时文件" -ForegroundColor Gray
}

# 步骤 5：清理 .NET 临时文件
Write-Host "[5/5] 清理 .NET 临时文件..." -ForegroundColor Yellow
$dotnetTemp = Join-Path $tempPath ".dotnet"
if (Test-Path $dotnetTemp) {
    Remove-Item -Recurse $dotnetTemp -Force -ErrorAction SilentlyContinue
    Write-Host "  已删除 .dotnet 临时文件夹" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "清理完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# 显示内存状态
Write-Host ""
Write-Host "当前内存状态:" -ForegroundColor Cyan
$os = Get-CimInstance Win32_OperatingSystem
$totalMemory = [math]::Round($os.TotalVisibleMemorySize / 1MB, 2)
$freeMemory = [math]::Round($os.FreePhysicalMemory / 1MB, 2)
$usedMemory = $totalMemory - $freeMemory
$usedPercent = [math]::Round(($usedMemory / $totalMemory) * 100, 2)

Write-Host "  总内存: $totalMemory GB"
Write-Host "  已使用: $([math]::Round($usedMemory, 2)) GB ($usedPercent%)"
Write-Host "  可用: $freeMemory GB"
