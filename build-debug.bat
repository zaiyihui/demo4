@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ================================================
echo 电脑伴侣 - 低内存优化构建脚本
echo ================================================
echo.

set "DOTNET_GCHeapHardLimit=2147483648"
set "DOTNET_HEAPALLOCATIONSTARTUPTHRESHOLD=67108864"
set "DOTNET_TieredPGO=0"
set "DOTNET_ReadyToRun=0"
set "MSBUILDDISABLENODEREUSE=1"

echo [配置] 内存限制: 2GB
echo [配置] 堆分配阈值: 64MB
echo.

echo [步骤] 清理构建缓存...
rmdir /s /q "bin" 2>nul
rmdir /s /q "obj" 2>nul
echo [完成] 缓存清理完成
echo.

echo [步骤] 还原 NuGet 包...
dotnet restore --no-cache --disable-parallel
if %errorlevel% neq 0 (
    echo [错误] NuGet 还原失败
    pause
    exit /b %errorlevel%
)
echo [完成] NuGet 还原成功
echo.

echo [步骤] 开始构建 (Debug)...
echo 命令: dotnet msbuild ComputerCompanion.csproj /t:Build /p:Configuration=Debug /p:Platform=x64 /m:1 /nodeReuse:false /verbosity:minimal

dotnet msbuild ComputerCompanion.csproj ^
    /t:Build ^
    /p:Configuration=Debug ^
    /p:Platform=x64 ^
    /m:1 ^
    /nodeReuse:false ^
    /verbosity:minimal ^
    /p:RunAnalyzers=false ^
    /p:UseSharedCompilation=false ^
    /p:BuildInParallel=false ^
    /p:UseRazorBuildServer=false ^
    /p:AvaloniaCompileXaml=false ^
    /p:AvaloniaResourcePreCompile=false

if %errorlevel% equ 0 (
    echo.
    echo [成功] 构建完成!
    echo 输出目录: bin\Debug\net8.0-windows\win-x64
) else (
    echo.
    echo [失败] 构建失败，错误码: %errorlevel%
)

echo.
echo ================================================
echo 构建结束
echo ================================================
pause