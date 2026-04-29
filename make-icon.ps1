param(
    [string]$Source = "icon-source.png",
    [string]$Output = "app.ico"
)

Add-Type -AssemblyName System.Drawing

$src = [System.Drawing.Image]::FromFile((Resolve-Path $Source).Path)

# Pad to square with transparent background
$dim = [Math]::Max($src.Width, $src.Height)
$square = New-Object System.Drawing.Bitmap($dim, $dim, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($square)
$g.Clear([System.Drawing.Color]::Transparent)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.DrawImage($src, [int](($dim - $src.Width) / 2), [int](($dim - $src.Height) / 2), $src.Width, $src.Height)
$g.Dispose()
$src.Dispose()

$sizes = @(16, 20, 24, 32, 40, 48, 64, 96, 128, 256)
$entries = @()
foreach ($s in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g2 = [System.Drawing.Graphics]::FromImage($bmp)
    $g2.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g2.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g2.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g2.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g2.DrawImage($square, 0, 0, $s, $s)
    $g2.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $entries += [pscustomobject]@{ Size = $s; Data = $ms.ToArray() }
}
$square.Dispose()

$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($out)
$bw.Write([uint16]0)
$bw.Write([uint16]1)
$bw.Write([uint16]$entries.Count)

$offset = 6 + (16 * $entries.Count)
foreach ($e in $entries) {
    $w = if ($e.Size -ge 256) { 0 } else { $e.Size }
    $h = if ($e.Size -ge 256) { 0 } else { $e.Size }
    $bw.Write([byte]$w)
    $bw.Write([byte]$h)
    $bw.Write([byte]0)
    $bw.Write([byte]0)
    $bw.Write([uint16]1)
    $bw.Write([uint16]32)
    $bw.Write([uint32]$e.Data.Length)
    $bw.Write([uint32]$offset)
    $offset += $e.Data.Length
}
foreach ($e in $entries) {
    $bw.Write($e.Data)
}
$bw.Flush()
[System.IO.File]::WriteAllBytes((Join-Path (Get-Location) $Output), $out.ToArray())
Write-Output "Wrote $Output with sizes: $($sizes -join ', ')"
