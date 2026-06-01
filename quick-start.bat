@echo off
REM Fast startup script - Skip build check
cd /d "%~dp0"

REM Copy SkiaSharp native library
set SKIA_SOURCE=%USERPROFILE%\.nuget\packages\skiasharp.nativeassets.win32\3.119.4-preview.1.1\runtimes\win-x64\native\libSkiaSharp.dll
set SKIA_TARGET=bin\Debug\net8.0\libSkiaSharp.dll

if exist "%SKIA_SOURCE%" (
    copy /Y "%SKIA_SOURCE%" "%SKIA_TARGET%" > nul 2>&1
    echo [OK] SkiaSharp library copied
)

echo [START] Launching application...
echo.
dotnet bin\Debug\net8.0\ComputerCompanion.dll

pause