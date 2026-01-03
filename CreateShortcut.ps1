# CreateShortcut.ps1 - Creates a desktop shortcut for TextExtractor
# Run this script after building the application

param(
    [string]$ExePath = "",
    [string]$ShortcutName = "TextExtractor"
)

# If no exe path provided, try to find it in common build locations
if ([string]::IsNullOrEmpty($ExePath)) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $possiblePaths = @(
        "$scriptDir\bin\Release\net8.0-windows10.0.22621.0\win-x64\TextExtractorWin.exe",
        "$scriptDir\bin\Release\net8.0-windows10.0.22621.0\TextExtractorWin.exe",
        "$scriptDir\bin\Debug\net8.0-windows10.0.22621.0\win-x64\TextExtractorWin.exe",
        "$scriptDir\bin\Debug\net8.0-windows10.0.22621.0\TextExtractorWin.exe"
    )

    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            $ExePath = $path
            break
        }
    }
}

if ([string]::IsNullOrEmpty($ExePath) -or !(Test-Path $ExePath)) {
    Write-Error "Could not find TextExtractorWin.exe. Please build the project first or specify the path with -ExePath"
    Write-Host ""
    Write-Host "Usage: .\CreateShortcut.ps1 -ExePath 'C:\path\to\TextExtractorWin.exe'"
    exit 1
}

$ExePath = Resolve-Path $ExePath

# Get icon path (same directory as exe or Assets folder)
$exeDir = Split-Path -Parent $ExePath
$iconPath = Join-Path $exeDir "Assets\app.ico"
if (!(Test-Path $iconPath)) {
    $iconPath = $ExePath  # Use exe's embedded icon
}

# Create desktop shortcut
$desktopPath = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktopPath "$ShortcutName.lnk"

$WScriptShell = New-Object -ComObject WScript.Shell
$shortcut = $WScriptShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $ExePath
$shortcut.WorkingDirectory = Split-Path -Parent $ExePath
$shortcut.IconLocation = $iconPath
$shortcut.Description = "TextExtractor - OCR text extraction for Windows 11"
$shortcut.Save()

Write-Host "Desktop shortcut created: $shortcutPath" -ForegroundColor Green
Write-Host ""
Write-Host "Shortcut details:"
Write-Host "  Target: $ExePath"
Write-Host "  Icon: $iconPath"
Write-Host ""

# Optionally create Start Menu shortcut
$startMenuPath = Join-Path ([Environment]::GetFolderPath("StartMenu")) "Programs"
$startMenuShortcut = Join-Path $startMenuPath "$ShortcutName.lnk"

$shortcut2 = $WScriptShell.CreateShortcut($startMenuShortcut)
$shortcut2.TargetPath = $ExePath
$shortcut2.WorkingDirectory = Split-Path -Parent $ExePath
$shortcut2.IconLocation = $iconPath
$shortcut2.Description = "TextExtractor - OCR text extraction for Windows 11"
$shortcut2.Save()

Write-Host "Start Menu shortcut created: $startMenuShortcut" -ForegroundColor Green
