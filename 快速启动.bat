@echo off
setlocal enabledelayedexpansion

cd /d "%~dp0"

set "SKIA_SOURCE=%USERPROFILE%\.nuget\packages\skiasharp.nativeassets.win32\3.119.4-preview.1.1\runtimes\win-x64\native\libSkiaSharp.dll"
set "SKIA_TARGET=bin\Debug\net8.0\libSkiaSharp.dll"

echo ========================================
echo        电脑伴侣 - 快速启动脚本
echo ========================================
echo.

:: 检查 .NET SDK 是否安装
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ 错误：未安装 .NET SDK
    echo 请安装 .NET 8.0 SDK：
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo [1/3] 检查并复制 SkiaSharp 库...
if exist "%SKIA_SOURCE%" (
    if not exist "%SKIA_TARGET%" (
        copy /Y "%SKIA_SOURCE%" "%SKIA_TARGET%" >nul 2>&1
        if !errorlevel! equ 0 (
            echo ✅ 已复制 SkiaSharp 原生库
        ) else (
            echo ⚠️  复制 SkiaSharp 库失败，将尝试自动修复
        )
    ) else (
        echo ✅ SkiaSharp 库已存在
    )
) else (
    echo ⚠️  SkiaSharp 原生库未找到，将尝试自动修复
)

echo.
echo [2/3] 检查构建状态...
if not exist "bin\Debug\net8.0\ComputerCompanion.dll" (
    echo ⚠️  未找到构建产物，开始构建...
    dotnet build --configuration Debug --verbosity minimal
    if !errorlevel! neq 0 (
        echo ❌ 构建失败！
        pause
        exit /b 1
    )
    echo ✅ 构建成功
) else (
    echo ✅ 构建产物已存在
)

echo.
echo [3/3] 启动应用程序...
echo.
dotnet bin\Debug\net8.0\ComputerCompanion.dll

if !errorlevel! neq 0 (
    echo.
    echo ❌ 启动失败！尝试重新构建...
    dotnet restore
    dotnet build --configuration Debug
    dotnet bin\Debug\net8.0\ComputerCompanion.dll
)

pause