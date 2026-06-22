@echo off
REM 低内存构建脚本
REM 使用 MSBuild 单进程模式，避免 dotnet CLI 的内存占用

echo ========================================
echo 低内存构建脚本
echo ========================================

REM 设置环境变量限制 .NET 内存
set DOTNET_GCHeapHardLimit=0x80000000
set DOTNET_gcServer=0
set DOTNET_gcConcurrent=0
set DOTNET_NoLogo=1

REM 禁用构建服务器
set DOTNET_CLI_TELEMETRY_OPTOUT=1
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
set MSBUILDDISABLENODEREUSE=1

echo.
echo [1/3] 清理旧的构建输出...
if exist "bin\Debug" rmdir /s /q "bin\Debug"
if exist "obj\Debug" rmdir /s /q "obj\Debug"

echo.
echo [2/3] 使用 MSBuild 单进程模式构建...
"%ProgramFiles%\dotnet\dotnet.exe" msbuild ComputerCompanion.csproj /t:Build /p:Configuration=Debug /p:Platform=x64 /m:1 /nodeReuse:false /verbosity:minimal

echo.
echo [3/3] 构建完成！
echo.
echo 输出位置: bin\Debug\net8.0-windows\ComputerCompanion.dll
echo.
pause
