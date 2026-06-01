@echo off
chcp 65001 >nul
echo ========================================
echo    电脑伴侣 - 快速构建和启动
echo ========================================
echo.

echo [1/3] 正在清理旧的构建文件...
dotnet clean --verbosity quiet

echo.
echo [2/3] 正在构建项目...
dotnet build --configuration Debug --verbosity minimal

if %ERRORLEVEL% neq 0 (
    echo.
    echo 构建失败！请检查上面的错误信息。
    pause
    exit /b 1
)

echo.
echo [3/3] 正在启动应用程序...
echo.
dotnet run --configuration Debug --no-build

if %ERRORLEVEL% neq 0 (
    echo.
    echo 应用程序启动失败！请检查上面的错误信息。
    pause
)
