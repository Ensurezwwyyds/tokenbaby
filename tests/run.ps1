param([switch]$Live)
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$csc = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $csc)) {
    throw '找不到 .NET Framework C# 编译器。'
}
$output = Join-Path $root 'dist\tests'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$core = @(Get-ChildItem -LiteralPath (Join-Path $root 'src\Core') -Filter '*.cs' |
    ForEach-Object { $_.FullName })

$testsExe = Join-Path $output 'QuotaParserTests.exe'
& $csc /nologo /target:exe ('/out:' + $testsExe) $core (Join-Path $PSScriptRoot 'QuotaParserTests.cs')
if ($LASTEXITCODE -ne 0) { throw '额度解析测试编译失败。' }
& $testsExe
if ($LASTEXITCODE -ne 0) { throw '额度解析测试失败。' }

if ($Live) {
    $probeExe = Join-Path $output 'LiveQuotaProbe.exe'
    $probeSources = $core + @((Join-Path $root 'src\Infrastructure\CodexClient.cs'),
        (Join-Path $PSScriptRoot 'LiveQuotaProbe.cs'))
    & $csc /nologo /target:exe ('/out:' + $probeExe) ('/reference:' + (Join-Path $framework 'System.Web.Extensions.dll')) $probeSources
    if ($LASTEXITCODE -ne 0) { throw '实时额度探针编译失败。' }
    & $probeExe
    if ($LASTEXITCODE -ne 0) { throw '实时额度探针失败。' }
}
