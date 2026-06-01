@echo off
setlocal enabledelayedexpansion

cd /d "%~dp0"

echo ========================================
echo        电脑伴侣 - 发布脚本
echo ========================================
echo.
echo 此脚本将创建自包含的发布版本
echo 发布后可直接运行，无需安装 .NET 运行时
echo.

:: 检查 .NET SDK 是否安装
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ 错误：未安装 .NET SDK
    pause
    exit /b 1
)

echo [1/2] 开始发布...
echo 这可能需要几分钟时间，请耐心等待...
echo.

dotnet publish ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output "bin\Release\publish" ^
    /p:PublishSingleFile=true ^
    /p:PublishTrimmed=true

if !errorlevel! neq 0 (
    echo.
    echo ❌ 发布失败！
    pause
    exit /b 1
)

echo.
echo ✅ 发布成功！
echo.
echo [2/2] 发布信息：
echo ----------------------------------------
echo 发布目录: bin\Release\publish
echo 主程序: ComputerCompanion.exe
echo 运行方式: 双击运行或命令行执行
echo.
echo 启动发布版本:
echo bin\Release\publish\ComputerCompanion.exe
echo.
pause