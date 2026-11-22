<#
.SYNOPSIS
  Builds the release artifacts: an unsigned MSIX for the Microsoft Store and a portable ZIP.

.DESCRIPTION
  Both builds are self-contained, so no separate .NET installation is needed. The MSIX is unsigned:
  the Store signs it on submission. To sideload it for testing, sign it first with a certificate whose
  subject matches -Publisher (see README.md).

.EXAMPLE
  ./packaging/build-release.ps1 -Version 0.1.0
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $Version,

    [ValidateSet('x64', 'arm64')]
    [string] $Architecture = 'x64',

    # Partner Center > Product identity > Package/Identity/Name. Placeholder until the app is reserved.
    [string] $PackageName = 'bogopali',

    # Partner Center > Product identity > Package/Identity/Publisher. Placeholder until the app is reserved.
    [string] $Publisher = 'CN=bogopali',

    [string] $PublisherDisplayName = 'bogopali',

    [string] $OutputDirectory = (Join-Path $PSScriptRoot '..' 'artifacts' 'release')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$project = Join-Path $root 'src' 'Bogopali.App' 'Bogopali.App.csproj'
$runtime = "win-$Architecture"
$work = Join-Path $OutputDirectory 'work'

if (Test-Path $OutputDirectory) { Remove-Item $OutputDirectory -Recurse -Force }
New-Item $work -ItemType Directory -Force | Out-Null

function Invoke-Publish([string] $Destination, [string[]] $Extra) {
    & dotnet publish $project --configuration Release --runtime $runtime --self-contained `
        "-p:Version=$Version" --output $Destination @Extra
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }
}

# Portable ZIP: a single executable that runs without installation or admin rights.
$zipDir = Join-Path $work 'zip'
Invoke-Publish $zipDir @('-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true', '-p:EnableCompressionInSingleFile=true', '-p:DebugType=none')
$zip = Join-Path $OutputDirectory "bogopali-$Version-$runtime.zip"
Compress-Archive -Path (Join-Path $zipDir 'bogopali.exe') -DestinationPath $zip

# MSIX: the published folder plus the manifest and logos.
$msixDir = Join-Path $work 'msix'
Invoke-Publish $msixDir @('-p:DebugType=none')

$manifest = Get-Content (Join-Path $PSScriptRoot 'AppxManifest.xml') -Raw -Encoding utf8
$manifest = $manifest.Replace('$PackageName$', $PackageName).
    Replace('$Publisher$', $Publisher).
    Replace('$PublisherDisplayName$', $PublisherDisplayName).
    Replace('$Version$', "$Version.0").
    Replace('$Architecture$', $Architecture)
Set-Content (Join-Path $msixDir 'AppxManifest.xml') $manifest -Encoding utf8

# Logos: drawn here instead of committed as binaries, matching the tray Status Indicator for Pali Mode.
Add-Type -AssemblyName System.Drawing
$assets = New-Item (Join-Path $msixDir 'Assets') -ItemType Directory -Force
$logos = [ordered]@{
    'StoreLogo.png' = 50
    'Square44x44Logo.png' = 44
    'Square150x150Logo.png' = 150
}
foreach ($name in $logos.Keys) {
    $size = $logos[$name]
    $bitmap = [System.Drawing.Bitmap]::new($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
        $graphics.Clear([System.Drawing.Color]::FromArgb(255, 0xB4, 0x53, 0x09))
        $font = [System.Drawing.Font]::new('Segoe UI', [single]($size * 0.62), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $format = [System.Drawing.StringFormat]::new()
        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        $area = [System.Drawing.RectangleF]::new(0, 0, $size, $size)
        $graphics.DrawString([string][char]0x0101, $font, [System.Drawing.Brushes]::White, $area, $format)
        $bitmap.Save((Join-Path $assets $name), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$makeAppx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\makeappx.exe" -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending | Select-Object -First 1
if (-not $makeAppx) { throw 'makeappx.exe was not found. Install the Windows SDK.' }

$msix = Join-Path $OutputDirectory "bogopali-$Version-$runtime.msix"
& $makeAppx.FullName pack /d $msixDir /p $msix /o
if ($LASTEXITCODE -ne 0) { throw "makeappx failed with exit code $LASTEXITCODE." }

Remove-Item $work -Recurse -Force
Get-ChildItem $OutputDirectory | ForEach-Object { Write-Host "Built $($_.FullName)" }
