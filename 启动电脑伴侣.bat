@echo off
echo ========================================
echo  电脑伴侣 - 完整启动脚本
echo ========================================
echo.

cd /d "%~dp0"

echo [1/4] 检查发布版本...
if not exist "bin\Release\net8.0\win-x64\publish\ComputerCompanion.exe" (
    echo [2/4] 创建自包含发布版本...
    echo 这可能需要几分钟时间，请耐心等待...
    echo.
    dotnet publish --configuration Release --runtime win-x64 --self-contained true
)

echo.
echo [3/4] 检查发布结果...
if exist "bin\Release\net8.0\win-x64\publish\ComputerCompanion.exe" (
    echo ✅ 发布成功！
    echo.
    echo [4/4] 启动应用程序...
    cd "bin\Release\net8.0\win-x64\publish"
    start ComputerCompanion.exe
    echo.
    echo 应用程序已启动！
    echo 请检查系统托盘区域是否有图标出现。
) else (
    echo ❌ 发布失败！
    echo 请手动运行以下命令：
    echo dotnet publish --configuration Release --runtime win-x64 --self-contained true
)

echo.
pause