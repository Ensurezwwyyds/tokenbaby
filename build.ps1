param([switch]$DebugUi, [switch]$Package, [string]$OutputName)
$ErrorActionPreference = 'Stop'
if ($Package -and ($DebugUi -or $OutputName)) {
    throw '打包仅支持默认发布版名称。'
}

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$csc = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $csc)) {
    throw '找不到 .NET Framework C# 编译器。请在 Windows 上安装 .NET Framework 4.8 开发工具。'
}

$configuration = if ($DebugUi) { 'debug' } else { 'release' }
$output = Join-Path (Join-Path $root 'dist') $configuration
New-Item -ItemType Directory -Force -Path $output | Out-Null
$references = @(
    'System.dll',
    'System.Core.dll',
    'System.Drawing.dll',
    'System.Windows.Forms.dll',
    'System.Web.Extensions.dll',
    'System.Xaml.dll',
    'WPF\WindowsBase.dll',
    'WPF\PresentationCore.dll',
    'WPF\PresentationFramework.dll'
) | ForEach-Object { '/reference:' + (Join-Path $framework $_) }

$sources = @(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' -Recurse |
    ForEach-Object { $_.FullName })
$exeName = if ($DebugUi) { 'TokenBaby.Debug.exe' } else { 'TokenBaby.exe' }
if ($OutputName) { $exeName = $OutputName }
$define = if ($DebugUi) { '/define:DEBUG_UI' } else { '/define:RELEASE_UI' }
& $csc /nologo /target:winexe /platform:anycpu /optimize+ $define ('/out:' + (Join-Path $output $exeName)) $references $sources
if ($LASTEXITCODE -ne 0) { throw '编译失败。' }

$assetOutput = Join-Path $output 'assets'
New-Item -ItemType Directory -Force -Path $assetOutput | Out-Null
foreach ($obsolete in @('pet-idle.png', 'pet-wave.png', 'pet-tired.png')) {
    $obsoletePath = Join-Path $assetOutput $obsolete
    if (Test-Path -LiteralPath $obsoletePath) { Remove-Item -LiteralPath $obsoletePath -Force }
}
foreach ($asset in @('pet-low.png', 'pet-mid.png', 'pet-high.png', 'pet-cry.png',
        'pet-glance.png', 'pet-laugh.png', 'PROMPTS.md')) {
    Copy-Item -LiteralPath (Join-Path (Join-Path $root 'assets') $asset) -Destination $assetOutput -Force
}
Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination (Join-Path $output 'README.md') -Force
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination (Join-Path $output 'LICENSE') -Force
Write-Output "已生成 $output\$exeName"

if ($Package) {
    $archive = Join-Path (Join-Path $root 'dist') 'TokenBaby-portable.zip'
    Compress-Archive -LiteralPath (Join-Path $output 'TokenBaby.exe'),
        (Join-Path $output 'README.md'),(Join-Path $output 'LICENSE'),$assetOutput -DestinationPath $archive -Force
    Write-Output "已生成 $archive"
}
