@echo off
cd /d "%~dp0bin\Release\net8.0-windows10.0.26100.0\win-x64\publish"
echo Starting TextExtractorWin.exe...
echo.
TextExtractorWin.exe
echo.
echo Exit code: %ERRORLEVEL%
pause
