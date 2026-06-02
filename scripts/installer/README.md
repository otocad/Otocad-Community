# OtoCAD 安装包构建

把 publish 产物打成 Windows 用户一键 .exe 安装包.

## 一键构建

```powershell
# 从仓库根目录跑:
.\scripts\installer\build-installer.ps1
```

流程:
1. `scripts/publish-avalonia.ps1` 生成 self-contained 单文件 `OtoCAD.Avalonia.exe`
2. Inno Setup ISCC.exe 编译 [installer.iss](installer.iss) → `OtoCAD-Setup-v<VER>.exe`

输出位置: `src/BuildOutput/Publish/OtoCAD-Setup-v<VER>.exe`

参数:
- `-Version 0.1.0`  指定版本号
- `-SkipPublish`    跳过 dotnet publish (用现有 publish 产物, 仅重打安装包)
- `-SkipInstaller`  仅 publish, 不做安装包

## 依赖

- **.NET 10 SDK** — `dotnet publish` 用
- **Inno Setup 5/6/7** — 安装包打包. 下载: https://jrsoftware.org/isdl.php
  - 脚本自动找 `C:\Program Files[ (x86)]\Inno Setup *\ISCC.exe`
  - 没装时优雅 fallback 到 publish zip 路径, 不报错

## 文件说明

| 文件 | 作用 |
|------|------|
| [build-installer.ps1](build-installer.ps1) | 一键脚本 (publish + ISCC 编译) |
| [installer.iss](installer.iss) | Inno Setup 工程配置 (AppId 固定, 升级时同 ID 自动覆盖旧版本) |
| [ChineseSimplified.isl](ChineseSimplified.isl) | 中文(简体)安装界面翻译, 来源 [kira-96/Inno-Setup-Chinese-Simplified-Translation](https://github.com/kira-96/Inno-Setup-Chinese-Simplified-Translation) (MIT). 随仓附带, 避免 Inno Setup 6+ 默认包不带中文还要用户单独下载 |

## 升级安装包版本

改 `build-installer.ps1` 调用的 `-Version` 参数, 例如:
```powershell
.\scripts\installer\build-installer.ps1 -Version 0.2.0
```

Inno Setup 用 `installer.iss` 顶部 `AppId={{8E2A9C6F-3D5B-4A1E-9F7C-2B6E4D8A1C30}` 识别 — 同 AppId 的新版本会覆盖旧版本, 不会两份并存.

## 自定义

- **改安装目录默认值**: `installer.iss` 的 `DefaultDirName`
- **加桌面快捷方式以外的图标**: `[Icons]` section
- **要求管理员权限**: 改 `PrivilegesRequired=admin` (默认 `lowest`, 装用户目录)
