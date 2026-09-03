# 电脑伴侣 - 运行时性能基准测试脚本
# 测量：启动时间、空闲内存、1h 常驻内存曲线、CPU 占用、悬浮窗开销

param(
    [int]$DurationMinutes = 5,
    [int]$IntervalSeconds = 30
)

$exePath = ".\bin\Debug\net8.0-windows\win-x64\ComputerCompanion.exe"
if (-not (Test-Path $exePath)) {
    $exePath = ".\bin\Release\net8.0-windows\win-x64\ComputerCompanion.exe"
}

if (-not (Test-Path $exePath)) {
    Write-Host "错误: 找不到编译产物，请先执行 dotnet build" -ForegroundColor Red
    exit 1
}

Write-Host "开始性能基准测试 (持续 $DurationMinutes 分钟, 每 $IntervalSeconds 秒采样)" -ForegroundColor Cyan

# 启动进程并测量启动时间
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$proc = Start-Process -FilePath $exePath -PassThru
$sw.Stop()
$startupMs = $sw.ElapsedMilliseconds

Write-Host "启动时间: ${startupMs}ms"

# 采样数据
$results = @()
$startTime = Get-Date

for ($i = 0; $i -lt ($DurationMinutes * 60 / $IntervalSeconds); $i++) {
    Start-Sleep -Seconds $IntervalSeconds

    if ($proc.HasExited) {
        Write-Host "进程已退出" -ForegroundColor Yellow
        break
    }

    $proc.Refresh()
    $memMB = [math]::Round($proc.WorkingSet64 / 1MB, 1)
    $cpuS = $proc.TotalProcessorTime.TotalSeconds
    $elapsedS = ((Get-Date) - $startTime).TotalSeconds
    $cpuPercent = [math]::Round(($cpuS / $elapsedS) * 100, 2)
    $managedMB = [math]::Round((Get-Process -Id $proc.Id -ErrorAction SilentlyContinue).PrivateMemorySize64 / 1MB, 1)

    $results += [PSCustomObject]@{
        Time = "$([math]::Round($elapsedS, 0))s"
        WorkingSet_MB = $memMB
        PrivateMemory_MB = $managedMB
        CPU_Percent = $cpuPercent
    }

    Write-Host "  $($elapsedS.ToString('0'))s | 内存: ${memMB}MB | CPU: ${cpuPercent}%"
}

# 停止进程
try { $proc.Kill() } catch {}

# 汇总
$avgMem = ($results | Measure-Object WorkingSet_MB -Average).Average
$maxMem = ($results | Measure-Object WorkingSet_MB -Maximum).Maximum
$avgCpu = ($results | Measure-Object CPU_Percent -Average).Average

Write-Host ""
Write-Host "===== 性能基准结果 =====" -ForegroundColor Green
Write-Host "启动时间: ${startupMs}ms"
Write-Host "平均内存: $([math]::Round($avgMem, 1))MB"
Write-Host "峰值内存: $([math]::Round($maxMem, 1))MB"
Write-Host "平均 CPU: $([math]::Round($avgCpu, 2))%"
Write-Host ""

# 生成 Markdown 表格
$markdown = @"
## 运行时性能基准

| 指标 | 数值 |
|------|------|
| 启动时间 | ${startupMs}ms |
| 平均内存占用 | $([math]::Round($avgMem, 1))MB |
| 峰值内存占用 | $([math]::Round($maxMem, 1))MB |
| 平均 CPU 占用 | $([math]::Round($avgCpu, 2))% |
| 测试时长 | ${DurationMinutes} 分钟 |
| 采样间隔 | ${IntervalSeconds} 秒 |

### 内存占用曲线

| 时间 | 内存 (MB) | CPU (%) |
|------|-----------|---------|
$($results | ForEach-Object { "| $($_.Time) | $($_.WorkingSet_MB) | $($_.CPU_Percent) |" })
"@

$markdown | Out-File -FilePath ".\benchmark-results.md" -Encoding UTF8
Write-Host "结果已保存到 benchmark-results.md"
