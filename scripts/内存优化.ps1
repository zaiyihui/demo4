# 内存优化脚本
# 用于监控和优化 .NET 构建过程中的内存使用

param(
    [switch]$Monitor,
    [switch]$Clean,
    [switch]$Optimize
)

function Get-MemoryUsage {
    $os = Get-CimInstance Win32_OperatingSystem
    $totalMemory = $os.TotalVisibleMemorySize / 1MB
    $freeMemory = $os.FreePhysicalMemory / 1MB
    $usedMemory = $totalMemory - $freeMemory
    $usagePercent = ($usedMemory / $totalMemory) * 100

    return @{
        Total = [math]::Round($totalMemory, 2)
        Used = [math]::Round($usedMemory, 2)
        Free = [math]::Round($freeMemory, 2)
        Percent = [math]::Round($usagePercent, 2)
    }
}

function Get-DotnetProcesses {
    return Get-Process -Name "dotnet" -ErrorAction SilentlyContinue |
           Select-Object Id, ProcessName, @{Name="Memory(MB)";Expression={[math]::Round($_.WorkingSet64/1MB,2)}}
}

function Stop-HighMemoryProcesses {
    param([double]$ThresholdMB = 2000)

    $processes = Get-DotnetProcesses | Where-Object { $_."Memory(MB)" -gt $ThresholdMB }

    foreach ($proc in $processes) {
        Write-Host "终止高内存进程: PID=$($proc.Id), 内存=$($proc.'Memory(MB)') MB" -ForegroundColor Yellow
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
}

function Clear-BuildCache {
    Write-Host "清理构建缓存..." -ForegroundColor Cyan

    # 清理 NuGet 缓存
    dotnet nuget locals all --clear 2>$null

    # 清理 MSBuild 缓存
    $tempPath = $env:TEMP
    Get-ChildItem -Path $tempPath -Filter "msbuild*" -Directory -ErrorAction SilentlyContinue |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

    # 清理项目输出
    dotnet clean 2>$null

    Write-Host "缓存清理完成" -ForegroundColor Green
}

function Set-DotnetEnvironment {
    # 设置 .NET GC 内存限制
    $env:DOTNET_GCHeapHardLimit = "0x400000000"  # 16GB

    # 启用服务器 GC
    $env:DOTNET_gcServer = "1"

    # 设置并发 GC
    $env:DOTNET_gcConcurrent = "1"

    Write-Host ".NET 环境已优化" -ForegroundColor Green
}

# 主逻辑
if ($Monitor) {
    Write-Host "`n=== 内存使用监控 ===" -ForegroundColor Cyan
    $mem = Get-MemoryUsage
    Write-Host "总内存: $($mem.Total) GB"
    Write-Host "已使用: $($mem.Used) GB ($($mem.Percent)%)"
    Write-Host "可用: $($mem.Free) GB"

    Write-Host "`n=== dotnet 进程 ===" -ForegroundColor Cyan
    $processes = Get-DotnetProcesses
    if ($processes) {
        $processes | Format-Table -AutoSize
    } else {
        Write-Host "无 dotnet 进程运行"
    }
}

if ($Clean) {
    Clear-BuildCache
}

if ($Optimize) {
    Set-DotnetEnvironment
    Stop-HighMemoryProcesses -ThresholdMB 2000
}

# 默认显示状态
if (-not $Monitor -and -not $Clean -and -not $Optimize) {
    Write-Host "用法:" -ForegroundColor Yellow
    Write-Host "  .\memory-optimization.ps1 -Monitor    # 监控内存使用"
    Write-Host "  .\memory-optimization.ps1 -Clean      # 清理构建缓存"
    Write-Host "  .\memory-optimization.ps1 -Optimize   # 优化内存设置"
    Write-Host "  .\memory-optimization.ps1 -Monitor -Optimize  # 监控并优化"
}
