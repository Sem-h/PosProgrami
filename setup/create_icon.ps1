Add-Type -AssemblyName System.Drawing

$png = [System.Drawing.Image]::FromFile("$PSScriptRoot\..\image\icon.png")
$icoPath = "$PSScriptRoot\..\PosProjesi\Resources\app.ico"

# Create multi-size ICO from PNG
$sizes = @(16, 32, 48, 64, 128, 256)
$ms = New-Object System.IO.MemoryStream

$bw = New-Object System.IO.BinaryWriter($ms)

# ICO header
$bw.Write([UInt16]0)       # reserved
$bw.Write([UInt16]1)       # type: ICO
$bw.Write([UInt16]$sizes.Count) # image count

# Generate bitmaps for each size
$imageData = @()
foreach ($size in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($png, $size, $size)
    $pngMs = New-Object System.IO.MemoryStream
    $bmp.Save($pngMs, [System.Drawing.Imaging.ImageFormat]::Png)
    $imageData += , ($pngMs.ToArray())
    $pngMs.Dispose()
    $bmp.Dispose()
}

# Calculate offsets (header=6, each entry=16)
$offset = 6 + ($sizes.Count * 16)

for ($i = 0; $i -lt $sizes.Count; $i++) {
    $size = $sizes[$i]
    $data = $imageData[$i]
    
    $w = if ($size -ge 256) { 0 } else { $size }
    $h = if ($size -ge 256) { 0 } else { $size }
    
    $bw.Write([byte]$w)           # width
    $bw.Write([byte]$h)           # height
    $bw.Write([byte]0)            # color palette
    $bw.Write([byte]0)            # reserved
    $bw.Write([UInt16]1)          # color planes
    $bw.Write([UInt16]32)         # bits per pixel
    $bw.Write([UInt32]$data.Length) # image size
    $bw.Write([UInt32]$offset)    # image offset
    
    $offset += $data.Length
}

# Write image data
foreach ($data in $imageData) {
    $bw.Write($data)
}

$bw.Flush()
[System.IO.File]::WriteAllBytes($icoPath, $ms.ToArray())

$bw.Dispose()
$ms.Dispose()
$png.Dispose()

Write-Host "ICO created at: $icoPath"
