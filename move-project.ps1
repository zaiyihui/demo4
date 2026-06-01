# ComputerCompanion 项目移动脚本
# 用于将项目从 demo4 子目录移动到父目录

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ComputerCompanion 项目移动工具" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$sourceDir = "d:\BC\xiangmu\demo4\demo4"
$targetDir = "d:\BC\xiangmu\demo4"

# 检查源目录是否存在
if (-not (Test-Path $sourceDir)) {
    Write-Host "❌ 错误: 源目录不存在: $sourceDir" -ForegroundColor Red
    pause
    exit 1
}

Write-Host "📁 源目录: $sourceDir" -ForegroundColor Green
Write-Host "📁 目标目录: $targetDir" -ForegroundColor Green
Write-Host ""

# 排除的目录和文件
$excludeDirs = @("bin", "obj", ".vs", ".git")
$excludeFiles = @("*.user", "*.suo")

# 获取所有需要移动的项目
Write-Host "🔍 正在扫描源目录..." -ForegroundColor Yellow
$items = Get-ChildItem -Path $sourceDir -Force
Write-Host "✅ 找到 $($items.Count) 个项目" -ForegroundColor Green
Write-Host ""

# 移动文件
$successCount = 0
$skipCount = 0
$errorCount = 0

foreach ($item in $items) {
    # 检查是否需要排除
    if ($item.PSIsContainer -and $item.Name -in $excludeDirs) {
        Write-Host "⏭️  跳过目录: $($item.Name)" -ForegroundColor Gray
        $skipCount++
        continue
    }

    if (-not $item.PSIsContainer -and ($excludeFiles | Where-Object { $item.Name -like $_ })) {
        Write-Host "⏭️  跳过文件: $($item.Name)" -ForegroundColor Gray
        $skipCount++
        continue
    }

    $destPath = Join-Path $targetDir $item.Name
    
    # 检查目标是否已存在
    if (Test-Path $destPath) {
        Write-Host "⚠️  已存在，跳过: $($item.Name)" -ForegroundColor Yellow
        $skipCount++
        continue
    }

    # 执行移动
    try {
        Move-Item -Path $item.FullName -Destination $destPath -Force -ErrorAction Stop
        Write-Host "✅ 移动成功: $($item.Name)" -ForegroundColor Green
        $successCount++
    }
    catch {
        Write-Host "❌ 移动失败: $($item.Name) - $($_.Exception.Message)" -ForegroundColor Red
        $errorCount++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  移动完成!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✅ 成功: $successCount" -ForegroundColor Green
Write-Host "⚠️  跳过: $skipCount" -ForegroundColor Yellow
Write-Host "❌ 失败: $errorCount" -ForegroundColor Red
Write-Host ""

# 检查结果
if (Test-Path (Join-Path $targetDir "ComputerCompanion.csproj")) {
    Write-Host "🎉 项目文件已成功移动到: $targetDir" -ForegroundColor Green
    Write-Host ""
    Write-Host "下一步操作:" -ForegroundColor Yellow
    Write-Host "1. 关闭当前的终端/VS Code"
    Write-Host "2. 在新的终端中导航到: $targetDir"
    Write-Host "3. 运行: dotnet build --configuration Debug"
    Write-Host "4. 运行: dotnet run --configuration Debug"
} else {
    Write-Host "⚠️  警告: 未找到 ComputerCompanion.csproj" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "按任意键退出..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
