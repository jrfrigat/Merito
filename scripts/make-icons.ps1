# Renders the PWA icons (icon-192.png, icon-512.png) from the same shapes as wwwroot/favicon.svg:
# a violet rounded square with a yellow star. The star sits inside the maskable safe zone (80%).
# Run from the repository root: pwsh scripts/make-icons.ps1
Add-Type -AssemblyName System.Drawing

$out = Join-Path $PSScriptRoot '..' 'src' 'Merito.Client' 'wwwroot'

foreach ($size in 192, 512) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

    $rect = New-Object System.Drawing.Rectangle 0, 0, $size, $size
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, `
        ([System.Drawing.Color]::FromArgb(0x7c, 0x4d, 0xff)), ([System.Drawing.Color]::FromArgb(0x67, 0x50, 0xa4)), 45.0
    $g.FillRectangle($brush, $rect)

    # Five-pointed star centred in the icon, outer radius 30% of the size.
    $cx = $size / 2.0; $cy = $size * 0.52; $outer = $size * 0.30; $inner = $outer * 0.45
    $points = New-Object 'System.Drawing.PointF[]' 10
    for ($i = 0; $i -lt 10; $i++) {
        $r = if ($i % 2 -eq 0) { $outer } else { $inner }
        $angle = [Math]::PI / 5 * $i - [Math]::PI / 2
        $points[$i] = New-Object System.Drawing.PointF ([float]($cx + $r * [Math]::Cos($angle))), ([float]($cy + $r * [Math]::Sin($angle)))
    }
    $g.FillPolygon((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(0xff, 0xd5, 0x4f))), $points)

    $bmp.Save((Join-Path $out "icon-$size.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
}
