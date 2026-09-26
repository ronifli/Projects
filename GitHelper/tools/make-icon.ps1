# Renders the GitHelper icon at several sizes and packs them into a .ico.
# The glyph is a commit graph: a trunk with two commits and a branch off it,
# which still reads as "git" at 16px where a wrench or a pen would not.

param([string]$OutIco, [string]$PngDir)

Add-Type -AssemblyName System.Drawing

function New-Bitmap([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode     = 'AntiAlias'
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.PixelOffsetMode   = 'HighQuality'
    $g.Clear([System.Drawing.Color]::Transparent)

    # --- rounded-square plate, indigo -> blue top-left to bottom-right ---
    $r = [Math]::Max(2, [int]($s * 0.22))
    $rect = New-Object System.Drawing.RectangleF(0, 0, $s, $s)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $path.AddArc(0, 0, $d, $d, 180, 90)
    $path.AddArc($s - $d, 0, $d, $d, 270, 90)
    $path.AddArc($s - $d, $s - $d, $d, $d, 0, 90)
    $path.AddArc(0, $s - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $rect,
        [System.Drawing.Color]::FromArgb(255, 47, 91, 214),
        [System.Drawing.Color]::FromArgb(255, 24, 43, 122),
        45.0)
    $g.FillPath($brush, $path)

    # --- commit graph -------------------------------------------------------
    $white = [System.Drawing.Color]::FromArgb(255, 255, 255, 255)
    $accent = [System.Drawing.Color]::FromArgb(255, 255, 190, 70)   # the branch tip

    # Below ~24px there is no room for a curve: the bezier and the dots blur
    # into one blob. Small sizes get the same graph drawn with straight strokes
    # and slightly larger dots, which stays legible on the taskbar.
    $small = $s -le 24

    $lineW = if ($small) { [Math]::Max(1.5, $s * 0.11) } else { $s * 0.085 }
    $pen = New-Object System.Drawing.Pen($white, $lineW)
    $pen.StartCap = 'Round'; $pen.EndCap = 'Round'

    $x  = $s * 0.34          # trunk
    $y1 = $s * 0.24          # top commit
    $y2 = $s * 0.76          # bottom commit
    $bx = $s * 0.72          # branch commit
    $by = $s * 0.30

    $g.DrawLine($pen, $x, $y1, $x, $y2)

    # branch: out of the trunk, up to its own commit
    if ($small) {
        $g.DrawLine($pen, $x, ($s * 0.55), $bx, $by)
    } else {
        $curve = New-Object System.Drawing.Drawing2D.GraphicsPath
        $curve.AddBezier($x, ($s * 0.60), ($x + $s * 0.20), ($s * 0.60), $bx, ($s * 0.50), $bx, $by)
        $g.DrawPath($pen, $curve)
    }

    $rad = if ($small) { $s * 0.17 } else { $s * 0.135 }
    $fillW = New-Object System.Drawing.SolidBrush($white)
    $fillA = New-Object System.Drawing.SolidBrush($accent)
    foreach ($c in @(@($x, $y1, $fillW), @($x, $y2, $fillW), @($bx, $by, $fillA))) {
        $g.FillEllipse($c[2], [float]($c[0] - $rad), [float]($c[1] - $rad), [float]($rad * 2), [float]($rad * 2))
    }

    $g.Dispose()
    return $bmp
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$streams = @()
foreach ($s in $sizes) {
    $bmp = New-Bitmap $s
    if ($PngDir) { $bmp.Save((Join-Path $PngDir "icon-$s.png"), [System.Drawing.Imaging.ImageFormat]::Png) }
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $streams += ,@($s, $ms.ToArray())
    $bmp.Dispose(); $ms.Dispose()
}

# --- ICO container: 6-byte header, 16 bytes per entry, then the PNG payloads.
# PNG-compressed entries are what Windows itself uses for the larger sizes and
# are understood everywhere from Vista on.
$fs = [System.IO.File]::Create($OutIco)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([UInt16]0)                 # reserved
$bw.Write([UInt16]1)                 # type: icon
$bw.Write([UInt16]$streams.Count)

$offset = 6 + 16 * $streams.Count
foreach ($e in $streams) {
    $size = $e[0]; $bytes = $e[1]
    $bw.Write([Byte]($(if ($size -ge 256) { 0 } else { $size })))   # 0 means 256
    $bw.Write([Byte]($(if ($size -ge 256) { 0 } else { $size })))
    $bw.Write([Byte]0)               # palette
    $bw.Write([Byte]0)               # reserved
    $bw.Write([UInt16]1)             # colour planes
    $bw.Write([UInt16]32)            # bits per pixel
    $bw.Write([UInt32]$bytes.Length)
    $bw.Write([UInt32]$offset)
    $offset += $bytes.Length
}
foreach ($e in $streams) { $bw.Write($e[1]) }
$bw.Flush(); $bw.Close(); $fs.Close()

"wrote $OutIco ($((Get-Item $OutIco).Length) bytes, $($streams.Count) sizes)"
