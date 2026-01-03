# CreateDist.ps1 - Creates a distribution folder from the working build
$ErrorActionPreference = "Stop"

$source = "G:\Projects\Git\TextExtractorWin\bin\Release\net8.0-windows10.0.26100.0\win-x64"
$dest = "G:\Projects\Git\TextExtractorWin\dist"

Write-Host "Creating distribution from: $source"
Write-Host "Destination: $dest"

# Remove old dist if exists
if (Test-Path $dest) {
    Write-Host "Removing old dist folder..."
    Remove-Item -Path $dest -Recurse -Force
}

# Create dist folder
New-Item -ItemType Directory -Path $dest -Force | Out-Null

# Copy all files from source (excluding publish subfolder)
Write-Host "Copying files..."
Get-ChildItem -Path $source -File | Copy-Item -Destination $dest

# Copy all required subfolders (excluding publish)
Get-ChildItem -Path $source -Directory | Where-Object { $_.Name -ne "publish" -and $_.Name -ne "dist" } | ForEach-Object {
    Write-Host "Copying folder: $($_.Name)"
    Copy-Item -Path $_.FullName -Destination (Join-Path $dest $_.Name) -Recurse
}

Write-Host ""
Write-Host "Distribution created successfully!"
Write-Host "Location: $dest"
Write-Host ""
Write-Host "Main executable: $dest\TextExtractorWin.exe"
