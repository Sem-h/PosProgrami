Add-Type -AssemblyName System.Drawing

# --- Sidebar image (164x314) ---
$bmp = New-Object System.Drawing.Bitmap(164, 314)
$g = [System.Drawing.Graphics]::FromImage($bmp)

# Dark background
$g.Clear([System.Drawing.Color]::FromArgb(18, 20, 30))

# Bottom gradient accent
$rect = New-Object System.Drawing.Rectangle(0, 280, 164, 34)
$gradBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, [System.Drawing.Color]::FromArgb(52, 152, 219), [System.Drawing.Color]::FromArgb(0, 188, 212), 'Horizontal')
$g.FillRectangle($gradBrush, $rect)

# Title
$font = New-Object System.Drawing.Font('Segoe UI', [float]22, [System.Drawing.FontStyle]::Bold)
$brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(52, 152, 219))
$g.DrawString('Verimek', $font, $brush, [float]14, [float]120)

# Subtitle
$font2 = New-Object System.Drawing.Font('Segoe UI', [float]9, [System.Drawing.FontStyle]::Regular)
$brush2 = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(160, 170, 190))
$g.DrawString('POS Sistemi', $font2, $brush2, [float]14, [float]155)
$g.DrawString('v1.1.3', $font2, $brush2, [float]14, [float]175)

$g.Dispose()
$bmp.Save("$PSScriptRoot\wizard_sidebar.bmp", [System.Drawing.Imaging.ImageFormat]::Bmp)
$bmp.Dispose()
Write-Host "Sidebar image created"

# --- Small header image (55x58) ---
$bmp2 = New-Object System.Drawing.Bitmap(55, 58)
$g2 = [System.Drawing.Graphics]::FromImage($bmp2)

$g2.Clear([System.Drawing.Color]::FromArgb(18, 20, 30))

$font3 = New-Object System.Drawing.Font('Segoe UI', [float]14, [System.Drawing.FontStyle]::Bold)
$brush3 = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(52, 152, 219))
$g2.DrawString('V', $font3, $brush3, [float]16, [float]16)

$g2.Dispose()
$bmp2.Save("$PSScriptRoot\wizard_small.bmp", [System.Drawing.Imaging.ImageFormat]::Bmp)
$bmp2.Dispose()
Write-Host "Small image created"
