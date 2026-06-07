<#
.SYNOPSIS
  打包 OtoCAD.Avalonia 为 self-contained Windows 可执行 — v0.1 发布脚本.

.DESCRIPTION
  用 `dotnet publish -r win-x64 --self-contained` 生成单文件 exe (含 .NET 10 运行时).
  输出目录: src/BuildOutput/Publish/win-x64/

  完整 MSI 打包需 WiX Toolset (留 v0.2+).
  此脚本生成的 zip 包可直接分发 (用户解压即用, 无需安装 .NET).

.NOTES
  Windows PowerShell 5.1 兼容. 不需要 pwsh 7.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release',
    [ValidateSet('win-x64','linux-x64','osx-x64','osx-arm64')]
    [string]$Runtime = 'win-x64',
    [switch]$NoZip,
    [string]$Version = '0.4.0'
)

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$proj = Join-Path $root 'src/OtoCAD.Avalonia/OtoCAD.Avalonia.csproj'
$outDir = Join-Path $root "src/BuildOutput/Publish/$Runtime"

Write-Host "==> 发布 OtoCAD.Avalonia v$Version" -ForegroundColor Cyan
Write-Host "    Config:  $Configuration"
Write-Host "    Runtime: $Runtime"
Write-Host "    Output:  $outDir"
Write-Host ''

# 清空旧输出
if (Test-Path $outDir) {
    Write-Host "清理 $outDir ..."
    Remove-Item -Recurse -Force $outDir
}

# Publish (self-contained + ReadyToRun + 单文件)
& dotnet publish $proj `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=embedded `
    -o $outDir `
    --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ dotnet publish 失败"
    exit $LASTEXITCODE
}

# 复制 LICENSE / README / CHANGELOG (用户解压即看)
foreach ($doc in @('LICENSE', 'README.md', 'CHANGELOG.md')) {
    $src = Join-Path $root $doc
    if (Test-Path $src) { Copy-Item $src (Join-Path $outDir $doc) -Force }
}

$exe = Get-ChildItem $outDir -Filter 'OtoCAD.Avalonia.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($exe) {
    $size = [Math]::Round($exe.Length / 1MB, 1)
    Write-Host ''
    Write-Host "✅ 已生成: $($exe.FullName) ($size MB)" -ForegroundColor Green
}

# 打 zip 包 (除非 -NoZip)
if (-not $NoZip) {
    $zipName = "OtoCAD-v$Version-$Runtime.zip"
    $zipPath = Join-Path (Split-Path $outDir -Parent) $zipName
    if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
    Compress-Archive -Path "$outDir/*" -DestinationPath $zipPath
    $zipSize = [Math]::Round((Get-Item $zipPath).Length / 1MB, 1)
    Write-Host "✅ 已打包: $zipPath ($zipSize MB)" -ForegroundColor Green
}

Write-Host ''
Write-Host '发布完成. 下一步:' -ForegroundColor Yellow
Write-Host '  1. 测试: 双击 OtoCAD.Avalonia.exe 启动'
Write-Host '  2. 上传: gh release create v0.1.0 --notes-file CHANGELOG.md <zip-path>'

