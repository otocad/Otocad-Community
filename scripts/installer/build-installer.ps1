<#
.SYNOPSIS
  完整发布流水 — publish + Inno Setup → 用户安装包 .exe

.DESCRIPTION
  1. 调用 scripts/publish-avalonia.ps1 生成 self-contained 单文件 + zip
  2. (可选) 调用 Inno Setup ISCC.exe 编译 scripts/installer/installer.iss → 安装包 .exe

  Inno Setup 未安装时, 跳过第 2 步, 用户拿 zip 解压即可使用 (功能等价).

  支持 Inno Setup 5/6/7, 自动找 C:\Program Files[ (x86)]\Inno Setup *\ISCC.exe;
  没找到再 fallback 走 PATH (Chocolatey / scoop 装的 iscc).

  中文界面: scripts/installer/ChineseSimplified.isl 随仓附带 (来源
  https://github.com/kira-96/Inno-Setup-Chinese-Simplified-Translation, MIT),
  无需用户单独下载. 安装时用户可选 English 或 简体中文.

.PARAMETER Version
  发布版本号, 默认 0.1.0

.PARAMETER SkipPublish
  跳过 dotnet publish (用现有 src/BuildOutput/Publish/win-x64/), 仅重做 installer

.PARAMETER SkipInstaller
  仅 publish, 不做 installer (Inno Setup 未装时也可这么用)

.NOTES
  Windows PowerShell 5.1 兼容. .ps1 文件必须 UTF-8 BOM (script 含中文).
#>
[CmdletBinding()]
param(
    [string]$Version = '0.1.0',
    [switch]$SkipPublish,
    [switch]$SkipInstaller
)

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$publishOut = Join-Path $root 'src\BuildOutput\Publish\win-x64'
$installerScript = Join-Path $PSScriptRoot 'installer.iss'

# Step 1: dotnet publish
if (-not $SkipPublish) {
    Write-Host '==> Step 1/2: dotnet publish' -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot '..\publish-avalonia.ps1') -Configuration Release -Version $Version
    if ($LASTEXITCODE -ne 0) { Write-Error 'publish 失败'; exit $LASTEXITCODE }
} else {
    Write-Host '==> Step 1/2: 跳过 publish (用现有 BuildOutput/Publish/win-x64/)' -ForegroundColor Yellow
    if (-not (Test-Path $publishOut)) {
        Write-Error "找不到 $publishOut — 先跑一次不带 -SkipPublish"
        exit 1
    }
}

# Step 2: Inno Setup
if ($SkipInstaller) {
    Write-Host '==> Step 2/2: 跳过 installer (-SkipInstaller)' -ForegroundColor Yellow
    Write-Host ''
    Write-Host '✅ Portable zip 已准备好可分发.' -ForegroundColor Green
    exit 0
}

Write-Host ''
Write-Host '==> Step 2/2: Inno Setup compile' -ForegroundColor Cyan

# 找 ISCC.exe (兼容 Inno Setup 5/6/7)
$isccCandidates = @(
    'C:\Program Files (x86)\Inno Setup 7\ISCC.exe',
    'C:\Program Files\Inno Setup 7\ISCC.exe',
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe',
    'C:\Program Files (x86)\Inno Setup 5\ISCC.exe',
    'C:\Program Files\Inno Setup 5\ISCC.exe'
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
# 也试 PATH (Chocolatey / scoop 装的)
if (-not $iscc) {
    $cmd = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($cmd) { $iscc = $cmd.Source }
}

if (-not $iscc) {
    Write-Warning 'Inno Setup 5/6/7 未找到. 请安装 (https://jrsoftware.org/isdl.php) 或加入 PATH.'
    Write-Host ''
    Write-Host '✅ Portable zip 仍可用. 安装包跳过.' -ForegroundColor Green
    exit 0
}

Write-Host "Using ISCC: $iscc" -ForegroundColor DarkGray

# 编译 .iss
& $iscc $installerScript /DMyAppVersion=$Version
if ($LASTEXITCODE -ne 0) { Write-Error 'Inno Setup 编译失败'; exit $LASTEXITCODE }

$installerExe = Join-Path $root "src\BuildOutput\Publish\OtoCAD-Setup-v$Version.exe"
if (Test-Path $installerExe) {
    $size = [Math]::Round((Get-Item $installerExe).Length / 1MB, 1)
    Write-Host ''
    Write-Host "✅ 安装包: $installerExe ($size MB)" -ForegroundColor Green
} else {
    Write-Warning "Inno Setup 没报错但找不到输出 $installerExe"
}


