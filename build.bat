@echo off
setlocal

echo ========================================
echo TextExtractor for Windows - Build Script
echo ========================================
echo.

:: Check for .NET SDK
where dotnet >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: .NET SDK not found. Please install .NET 8 SDK from https://dotnet.microsoft.com/download
    exit /b 1
)

:: Get script directory
set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

:: Restore packages
echo Restoring NuGet packages...
dotnet restore
if %ERRORLEVEL% neq 0 (
    echo ERROR: Package restore failed
    exit /b 1
)
echo.

:: Build Release
echo Building Release configuration (x64)...
dotnet build -c Release -p:Platform=x64
if %ERRORLEVEL% neq 0 (
    echo ERROR: Build failed
    exit /b 1
)
echo.

:: Publish self-contained
echo Publishing self-contained application...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
if %ERRORLEVEL% neq 0 (
    echo ERROR: Publish failed
    exit /b 1
)
echo.

:: Find output
set "OUTPUT_DIR=%SCRIPT_DIR%bin\Release\net8.0-windows10.0.22621.0\win-x64\publish"
if exist "%OUTPUT_DIR%\TextExtractorWin.exe" (
    echo ========================================
    echo BUILD SUCCESSFUL!
    echo ========================================
    echo.
    echo Output location:
    echo   %OUTPUT_DIR%
    echo.
    echo To create desktop shortcut, run:
    echo   powershell -ExecutionPolicy Bypass -File CreateShortcut.ps1 -ExePath "%OUTPUT_DIR%\TextExtractorWin.exe"
    echo.
) else (
    echo WARNING: Could not find published executable. Check build output.
)

endlocal
