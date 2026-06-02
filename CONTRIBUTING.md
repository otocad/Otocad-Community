# 贡献指南

感谢有兴趣为 OtoCAD 贡献! 本指南帮你最快上手。

> ⚠️ 本仓库是上游主仓的**脱敏快照镜像**, 每次发布 force-push 覆盖、历史重置。
> 直接 merge 进本仓的 PR 会在下次快照被覆盖 —— maintainer 会把你的改动应用到上游,
> 随下次快照回到这里, 贡献者记入致谢。详见 [README 的「贡献」节](README.md#贡献)。

## 准备环境

最小依赖 (社区版 Avalonia, 无 DevExpress):

- **.NET 10 SDK** (10.0.102+) — [下载](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git
- 任意 .NET IDE: VS 2022 / VS Code / Rider / dotnet CLI

```bash
git clone https://github.com/otocad/Otocad-Community.git
cd Otocad-Community
dotnet build src/OtoCAD.Avalonia/OtoCAD.Avalonia.csproj
dotnet run --project src/OtoCAD.Avalonia/OtoCAD.Avalonia.csproj
```

## 第一次贡献的优先方向

按上手难度从易到难:

### 🟢 入门 (1-2 小时)
- **新增 ICadCommand**: 复制 `src/OtoCAD.Avalonia/Commands/LineCmd.cs` 改造成你的命令 (例如 BoltCircle 螺栓圆), 在 `MainWindow.BuildCommandRegistry` 注册一行, 在 `Config/RibbonLayout.json` 加按钮
- **修复 Mark tooltip 文案**: 找 ribbon 中 "Phase 2 待接入" 的命令,实现真正逻辑

### 🟡 中级 (1 天)
- **新增 Annotation Mark**: 在 `src/lcdb/Annotation/` 创建实体 (继承 Entity, 实现 Draw/Translate/Rotate/TransformBy), Avalonia 端 1 行 `RegMark(...)` 即可上 Ribbon
- **PropertyPanel 专用编辑器**: 现在 `GenericPropertyBuilder` 用反射, 复杂 Mark (如 OpticalFrame) 适合写专用 Expander
- **更多 dimensions**: Angular2LineDimension / CenterThicknessDimension / EdgeThickness / SagittaDimension — 已占位待实现

### 🔴 进阶 (1 周+)
- **Layer 面板**: 数据库已有 LayerTable, 缺 UI; 需新 LayerPanel UserControl + 切换 active layer
- **Frame mode**: 双模式画布 (Drawing / Frame 模式各自实体集), 完整实现 ToggleFrameMode
- **Light/Optical 透镜向导**: SingleLens/DoubletLens 现 obsolete, 改写为新数据结构 + 配置向导
- **DXF 双向兼容**: netDxf 已 vendor, 但 lcdb ↔ DXF 映射有空白点

## 提交规范

按 [Conventional Commits](https://www.conventionalcommits.org/) 简化版:

```
feat(模块): 一句话描述

详细说明 (可选).
- 列表
- ...

Co-Authored-By: 你的署名 <email@example.com>
```

类型: `feat` / `fix` / `docs` / `refactor` / `test` / `build` / `ci` / `chore`

例:
- `feat(phase1f): Ctrl+A 全选 + 状态栏选中计数`
- `fix(render): 中文字体豆腐块 — SKFontManager.MatchCharacter 强制 CJK 兜底`

## 代码规范

- **C#**: `<LangVersion>latest</LangVersion>`, 用 nullable, file-scoped namespace
- **命名空间严格按目录**: namespace 必须与文件夹路径一致 (如 `src/OtoCAD.Avalonia/Commands/` → `namespace OtoCAD.Avalonia.Commands`)
- **新增代码必须 build 0 错误**: PR CI 会跑 Windows build (`src/OtoCAD.slnx`)
- **避免过度设计**: 能用静态方法/扩展方法解决就不新建类; 单一实现不抽接口; 不为"未来可能用到"预留空抽象; 先复用现有代码再造新轮子
- **写测试**: 关键命令/算法尽量加 MSTest (`src/OtoCAD.Tests/`)

## CI

提交 PR 后自动触发 [.github/workflows/ci.yml](.github/workflows/ci.yml):

1. **build** (Windows): `dotnet build src/OtoCAD.slnx` (Debug + Release)
2. **test** (Windows): `dotnet test src/OtoCAD.Tests`

社区版解决方案不含 DevExpress / 闭源后端, 普通 fork 无需任何 secret 即可跑通。
(跨平台 Linux/macOS 二进制由 `scripts/publish-avalonia.ps1 -Runtime <rid>` 本地/手动产出, 当前 CI 暂只验证 Windows build。)

## 行为准则

简版: 尊重 + 专业 + 中肯. 严肃的事认真讨论, 不要人身攻击.

详情: 我们采用 [Contributor Covenant 2.1](https://www.contributor-covenant.org/version/2/1/code_of_conduct/).

## 问题反馈

- Bug / 功能请求: [GitHub Issues](https://github.com/otocad/Otocad-Community/issues)
- 设计讨论: [GitHub Discussions](https://github.com/otocad/Otocad-Community/discussions)
- 安全漏洞: 不要开公开 issue, 联系 maintainer 私下报告

## 谢谢

每一个 PR 都让 OtoCAD 更好。即使只改了一个 typo 也欢迎提交 — 我们没有 "太小的贡献" 这一说。
