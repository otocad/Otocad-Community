; ============================================================================
; OtoCAD.Avalonia Inno Setup Installer 配置
;
; 用法:
;   1. 先跑 scripts/publish-avalonia.ps1 生成 src/BuildOutput/Publish/win-x64/
;   2. 安装 Inno Setup 6+ (https://jrsoftware.org/isdl.php)
;   3. 双击本 .iss 文件用 Inno Setup IDE 打开, 或命令行:
;      "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" scripts\installer\installer.iss
;   4. 输出: src/BuildOutput/Publish/OtoCAD-Setup-v0.1.0.exe
;
; AppId 是固定 GUID, 不要随意改 — 升级时旧版本会被这个 ID 识别并替换.
; ============================================================================

#define MyAppName "OtoCAD"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "RayPragma"
#define MyAppURL "https://github.com/RayPragma/Otocad"
#define MyAppExeName "OtoCAD.Avalonia.exe"
#define MyAppSourceDir "..\..\src\BuildOutput\Publish\win-x64"

[Setup]
AppId={{8E2A9C6F-3D5B-4A1E-9F7C-2B6E4D8A1C30}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
VersionInfoVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=..\..\LICENSE
OutputDir=..\..\src\BuildOutput\Publish
OutputBaseFilename={#MyAppName}-Setup-v{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
; English 是 Inno Setup 自带 (compiler:Default.isl), 任何版本都有.
; ChineseSimplified.isl 随本仓附带 (来源 https://github.com/kira-96/Inno-Setup-Chinese-Simplified-Translation, MIT),
; 避免 Inno Setup 6+ 默认包不带中文翻译时还要用户手动下载.
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
