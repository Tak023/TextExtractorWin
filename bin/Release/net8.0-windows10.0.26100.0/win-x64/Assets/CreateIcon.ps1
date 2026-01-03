# PowerShell script to create application icon
# This creates a simple icon using System.Drawing

Add-Type -AssemblyName System.Drawing

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$iconPath = "$PSScriptRoot\app.ico"

# Create a collection of images for the icon
$images = @()

foreach ($size in $sizes) {
    $bitmap = New-Object System.Drawing.Bitmap($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

    # Background - rounded rectangle with gradient
    $rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
    $cornerRadius = [int]($size * 0.2)

    # Create gradient brush (Indigo to Purple)
    $gradientBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $rect,
        [System.Drawing.Color]::FromArgb(255, 99, 102, 241),  # Indigo-500
        [System.Drawing.Color]::FromArgb(255, 139, 92, 246), # Violet-500
        [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal
    )

    # Draw rounded rectangle background
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0, 0, $cornerRadius * 2, $cornerRadius * 2, 180, 90)
    $path.AddArc($size - $cornerRadius * 2, 0, $cornerRadius * 2, $cornerRadius * 2, 270, 90)
    $path.AddArc($size - $cornerRadius * 2, $size - $cornerRadius * 2, $cornerRadius * 2, $cornerRadius * 2, 0, 90)
    $path.AddArc(0, $size - $cornerRadius * 2, $cornerRadius * 2, $cornerRadius * 2, 90, 90)
    $path.CloseFigure()

    $graphics.FillPath($gradientBrush, $path)

    # Draw "T" letter (for TextExtractor)
    $fontSize = [int]($size * 0.55)
    $font = New-Object System.Drawing.Font("Segoe UI", $fontSize, [System.Drawing.FontStyle]::Bold)
    $whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)

    $stringFormat = New-Object System.Drawing.StringFormat
    $stringFormat.Alignment = [System.Drawing.StringAlignment]::Center
    $stringFormat.LineAlignment = [System.Drawing.StringAlignment]::Center

    $textRect = New-Object System.Drawing.RectangleF(0, 0, $size, $size)
    $graphics.DrawString("T", $font, $whiteBrush, $textRect, $stringFormat)

    # Cleanup
    $font.Dispose()
    $whiteBrush.Dispose()
    $gradientBrush.Dispose()
    $graphics.Dispose()
    $path.Dispose()

    $images += $bitmap
}

# Save as ICO using a memory stream
$ms = New-Object System.IO.MemoryStream

# ICO header
$iconDir = New-Object byte[] 6
$iconDir[0] = 0  # Reserved
$iconDir[1] = 0
$iconDir[2] = 1  # Type: ICO
$iconDir[3] = 0
$iconDir[4] = [byte]$images.Count  # Number of images
$iconDir[5] = 0

$ms.Write($iconDir, 0, 6)

# Calculate offsets
$offset = 6 + ($images.Count * 16)  # Header + entries
$pngData = @()

foreach ($img in $images) {
    $pngStream = New-Object System.IO.MemoryStream
    $img.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngData += ,($pngStream.ToArray())
    $pngStream.Dispose()
}

# Write icon directory entries
for ($i = 0; $i -lt $images.Count; $i++) {
    $size = $sizes[$i]
    $data = $pngData[$i]

    $entry = New-Object byte[] 16
    $entry[0] = if ($size -eq 256) { 0 } else { [byte]$size }  # Width
    $entry[1] = if ($size -eq 256) { 0 } else { [byte]$size }  # Height
    $entry[2] = 0  # Color palette
    $entry[3] = 0  # Reserved
    $entry[4] = 1  # Color planes
    $entry[5] = 0
    $entry[6] = 32  # Bits per pixel
    $entry[7] = 0

    # Size of image data
    $dataSize = $data.Length
    $entry[8] = [byte]($dataSize -band 0xFF)
    $entry[9] = [byte](($dataSize -shr 8) -band 0xFF)
    $entry[10] = [byte](($dataSize -shr 16) -band 0xFF)
    $entry[11] = [byte](($dataSize -shr 24) -band 0xFF)

    # Offset
    $entry[12] = [byte]($offset -band 0xFF)
    $entry[13] = [byte](($offset -shr 8) -band 0xFF)
    $entry[14] = [byte](($offset -shr 16) -band 0xFF)
    $entry[15] = [byte](($offset -shr 24) -band 0xFF)

    $ms.Write($entry, 0, 16)
    $offset += $dataSize
}

# Write image data
foreach ($data in $pngData) {
    $ms.Write($data, 0, $data.Length)
}

# Save to file
[System.IO.File]::WriteAllBytes($iconPath, $ms.ToArray())

# Cleanup
foreach ($img in $images) {
    $img.Dispose()
}
$ms.Dispose()

Write-Host "Icon created at: $iconPath"
