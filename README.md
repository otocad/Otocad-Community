# OtoCAD — 导入 Zemax 镜头文件，一键出一整套加工图

**简体中文** | [English](README.en.md)

[![CI Build](https://github.com/otocad/Otocad-Community/actions/workflows/ci.yml/badge.svg)](https://github.com/otocad/Otocad-Community/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/otocad/Otocad-Community?label=release)](https://github.com/otocad/Otocad-Community/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![Platforms](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)

> **🌐 官网 & 下载：[cad.optic.chat](https://cad.optic.chat)** — 国内直连 CDN，免安装解压即用（Windows / Linux / macOS）

OtoCAD 是一款开源的国产二维光学 CAD。**一个 Zemax 镜头文件进来，光路总图 + 每个元件一张标准加工图出来**：
GB / ISO 图框、属性区、尺寸标注、光学代号、技术要求一次到位，出图清单逐项校验，DXF / PDF 直接交付到厂。
底座是一套完整的二维 CAD，向导覆盖不到的地方随手补。

![OtoCAD 一键生成的 GB 单透镜加工图：图框标题栏、左右表面与材料属性区、R/d/φ 标注、粗糙度与镀膜代号、注](./docs/images/sheet.png)

---

## 四步，出一整套图

不是一张一张手画：一个镜头文件进来，一套能交付的图纸出去。

| 步骤 | 做什么 |
|---|---|
| **1. 导入设计** | 整份 Zemax `.zmx` 或 Optic JSON（Optiland）一次导入，自动读取面型、曲率、厚度、材料、净口径；自动识别单透镜与胶合件并归组；预览光路后勾选要出图的元件。 |
| **2. 一键成套** | 光路总图一页，每个元件一页加工图，自动分标签页。库克三片式进去是一套四张，七片镜头进去是一套八张。 |
| **3. 每页都是标准图纸** | 图框与标题栏（GB 中文 / ISO 英文 / 自定义）、左右表面 + 材料属性区、R / d / φ 真值标注、ISO 10110 光学代号、注释，自动选纸与比例。 |
| **4. 校验后交付** | 出图清单按国标逐项亮灯，缺项一键重出；改参数时标注与属性区同步联动；导出 DXF / PDF 发给工厂。 |

![导入光学设计向导：整个 .zmx 文件一次导入，光路预览 + 一键生成光路图 / 加工图分页](./docs/images/import-wizard.png)

---

## 下载

免安装、自包含，解压即用，不需要另装 .NET。

| 平台 | 下载 |
|---|---|
| Windows 10 / 11 (x64) | [OtoCAD-v0.5.0-win-x64.zip](https://dlcdn.optic.chat/otocad/avalonia/v0.5.0/OtoCAD-v0.5.0-win-x64.zip) |
| Linux (x64) | [OtoCAD-v0.5.0-linux-x64.zip](https://dlcdn.optic.chat/otocad/avalonia/v0.5.0/OtoCAD-v0.5.0-linux-x64.zip) |
| macOS Apple 芯片 (arm64) | [OtoCAD-v0.5.0-osx-arm64.zip](https://dlcdn.optic.chat/otocad/avalonia/v0.5.0/OtoCAD-v0.5.0-osx-arm64.zip) |
| macOS Intel (x64) | [OtoCAD-v0.5.0-osx-x64.zip](https://dlcdn.optic.chat/otocad/avalonia/v0.5.0/OtoCAD-v0.5.0-osx-x64.zip) |

国内直连走上表 CDN 链接；GitHub 用户也可在 [Releases](https://github.com/otocad/Otocad-Community/releases) 页取同一批文件。
版本变更见 [CHANGELOG.md](CHANGELOG.md)。

---

## 能出什么图

- **零件类型**：单透镜 · 双胶合 · 直角棱镜，各有出图适配器；非球面自动附 ISO 10110-12 数据块（方程 + 矢高表 + 系数）。
- **图框三选一**：GB/T 13323 中文框（GB/T 10609.1 标题栏 + 左右表面 / 材料技术要求三列）· ISO 10110 英文框 · **自定义图框**。
  自定义图框是一份 JSON 定义（边框 + 注释区 / 标题栏网格 / 属性区列表 + 投影符号等附件），类似 SolidWorks 的图纸格式；
  出厂预设在 `Config/Frames/`，复制改名就是自家图框，在「出图标准编辑」选中后一键出图整套都用它。定义随 `.otocad` 内嵌，换机器打开不依赖本机文件。
- **属性驱动的图纸**：标题栏与属性区的每一行都是带**稳定键名**的文档级属性（类似 SolidWorks 自定义属性），随 `.otocad` 存盘，是图纸的单一真值源。
  点开哪一格就地改即回写；重新出图按键名回填，手填过的公差 / 备注 / 图号不丢；属性区可增删行 = 增删自定义指标。
- **全关联标注**：标注与标记的锚点跟随零件，改口径 / 半径 / 厚度尺寸自动跟随；拖动标注端点吸附零件角点，悬空尺寸灰显提示。
- **出图清单**：YAML 规则驱动的交付前自检（图框 · 自动标注 · 非球面数据块 · 材料代号 · 标注重叠 · 内容出框 …），每条注明国标出处，失败项一键重出；规则是数据文件，可按厂内规矩自行增改。
- **出图标准切换**：GB-ISO 10110 / MIL-ANSI / JIS / DIN 出图惯例一键切换（疵病 / 面形 / 粗糙度 / 激光损伤记法 + 尺寸样式 + 图框模板），一次设置整套复用。
- **导出**：DXF（全部实体类型，含光学标记与图框）· PDF · 打印。

---

## 主要特性

### 光学专用
- **24 种光学标记**：镀膜（13 种膜系）· 黑化 · 抛光 · 喷砂 · 金刚石车削 · 研磨 · 表面粗糙度 · 面形精度 · 中心偏差 · 表面疵病 · 表面质量 · 有效孔径 · 光轴 · 检验 · 加工 · 材料缺陷 等；代号按 GB/T 13323-2009 与 ISO 10110 系列渲染。
- **ISO 10110 系列**：-2 双折射 · -3 气泡 · -4 不均匀性 · -12 非球面 · -14 波前 · 激光损伤阈值。
- **6 种标注**：线性 · 对齐 · 半径 · 直径 · 三点角度 · 坐标；公差支持对称 `±` 与非对称上下偏差摞排。
- **光学元件**：单透镜 / 双胶合 / 通用透镜向导，直角棱镜实体，玻璃剖面材料符号与底色；玻璃库 40+ 牌号（Schott / CDGM / Ohara / Hoya），选牌号自动填 n_d / v_d。

### 绘图与编辑
- **绘图**：直线 · 圆 · 圆弧 · 矩形 · 多段线 · 多边形 · 椭圆 · 样条 · 点 · 射线 · 构造线 · 文字 · 多行文本 · 引线
- **编辑**：撤销 · 重做 · 移动 · 复制 · 旋转 · 缩放 · 镜像 · 偏移 · 删除；鼠标拖动选中实体直接移动
- **选择与捕捉**：点选 · 窗选 / 框选 · Shift 加选 · Ctrl+A；端点 / 中点 / 圆心 / 透镜中心捕捉
- **视图**：滚轮缩放 · 中键平移 · 范围缩放 · 自适应网格 · 图层与线型颜色

### 文档与工作流
- **多文档标签页**：光路总图与各元件加工图分页管理；关闭前未保存自动提示。
- **自动备份与恢复**：每 30 秒增量备份到本地（`%APPDATA%/OtoCAD/autosave`，不覆盖原文件），启动时检测并提示恢复。
- **文件格式**：原生 `.otocad`（JSON）；导入 `.zmx` / Optic JSON / DXF；导出 DXF / PDF。

---

## OtoCAD Community vs OtoCAD Cloud

本仓库是 **OtoCAD Community**（MIT 开源，跨平台桌面端），独立可用，绘图 / 标注 / 光学制图 / 设计导入 / 整套出图全部离线完成，不依赖任何在线服务。
**OtoCAD Cloud** 是 RayPragma 提供的商业在线服务，在此之上加云端能力。

| 能力 | OtoCAD Community | OtoCAD Cloud |
|------|------------------|--------------|
| 绘图 / 编辑 / 选择 / 视图 / 捕捉 | ✅ 完整 | ✅ 完整 |
| 24 种光学标记 / ISO 10110 / GB/T 13323-2009 合规 | ✅ 完整 | ✅ 完整 |
| **设计导入（Zemax .zmx / Optic JSON）→ 一次出一套图** | **✅ 完整**（v0.5.0 起） | ✅ 完整 |
| 标准图纸引擎（GB / ISO / 自定义图框，单透镜 · 双胶合 · 棱镜） | ✅ 完整 | ✅ 完整 |
| 属性驱动图纸 / 出图清单 / 全关联标注 | ✅ 完整 | ✅ 完整 |
| DXF / PDF / 打印 · 多文档 · 自动备份 | ✅ 完整 | ✅ 完整 |
| AI 助手（光学制图问答） | ✅ 基础问答（限速） | ✅ 不限速 |
| 跨设备同步 / 文档云存储 | — | ✅ |
| 模板市场（公共 + 私有） | — | ✅ |
| 团队协作（评论 / 批注 / 多人查看） | — | ✅ |
| 工厂版本（加密 / 工艺集成） | — | ✅ Enterprise |

---

## 从源码构建

### 环境要求
- **.NET 10 SDK**（10.0.102+）
- Windows / macOS / Linux 任一

### 编译运行

```bash
git clone https://github.com/otocad/Otocad-Community.git
cd Otocad-Community
dotnet build src/OtoCAD.Avalonia/OtoCAD.Avalonia.csproj
dotnet run --project src/OtoCAD.Avalonia/OtoCAD.Avalonia.csproj
```

```bash
# 业务层单元测试（序列化 / 光学 / 出图 / 图框 / 清单）
dotnet test src/lcdb.Tests/lcdb.Tests.csproj
```

> 主项目名 `OtoCAD.Avalonia` 是历史遗留（UI 基于 Avalonia + SkiaSharp），未来可能简化为 `OtoCAD`。

### 打包发布

```powershell
# Self-contained 单文件 + zip（用户解压即用，无需安装 .NET）；-Runtime 可选 win-x64 / linux-x64 / osx-x64 / osx-arm64
.\scripts\publish-avalonia.ps1 -Runtime win-x64

# （可选）装了 Inno Setup 6 → 编译 .exe 安装包
.\scripts\installer\build-installer.ps1
```

输出在 `src/BuildOutput/Publish/`。

---

## 项目结构

```
Otocad-Community/
├── src/
│   ├── OtoCAD.Avalonia/     # 桌面主程序：UI · 渲染 · 命令 · 出图引擎 · Config/（Ribbon 布局 / 图框预设 / 出图清单规则）
│   ├── lcdb/                # CAD 数据库：实体 · 光学元件 · 光学标记 · 图框 · 属性包 · 序列化 · 导入解析
│   ├── lcinterface/         # 接口契约（IGraphicsDraw）
│   ├── LitMath/             # 数学库（Vector2 / Matrix3）
│   ├── netDxf/              # DXF 读写（vendored，MIT）
│   ├── lcdb.Tests/          # MSTest 单元测试
│   └── OtoCAD.slnx          # 解决方案
├── docs/
│   ├── 用户指南/            # 入门 / 教程
│   └── images/              # README 图片
├── scripts/
│   ├── publish-avalonia.ps1 # self-contained 打包
│   └── installer/           # Inno Setup 安装包
└── .github/workflows/       # GitHub Actions CI（跨平台 build + 测试）
```

---

## 架构

```
┌──────────────────────────────────────────────────────────┐
│  OtoCAD Community（跨平台桌面端，Avalonia + SkiaSharp）   │
│   命令系统 · Ribbon · 属性面板 · 标准图纸引擎 · 出图清单面板 │
└────────────────────────┬─────────────────────────────────┘
                         │
┌────────────────────────▼─────────────────────────────────┐
│  业务核心（与 UI 无关，可单测）                            │
│   lcinterface（IGraphicsDraw）                            │
│   lcdb（实体 / 光学元件 / 标记 / 图框 / 属性包 / 导入）    │
│   LitMath（Vector2 / Matrix3）                            │
└────────────────────────┬─────────────────────────────────┘
                         │ HTTPS（可选）
                         ▼
┌──────────────────────────────────────────────────────────┐
│  OtoCAD Cloud（可选，闭源 SaaS）                          │
│   AI 助手 · 跨设备同步 · 模板市场 · 团队协作              │
└──────────────────────────────────────────────────────────┘
```

**关键设计**：实体只实现 `Draw(IGraphicsDraw)`，业务层完全独立于渲染后端；同一份几何代码同时驱动屏幕、DXF 导出与测试中的几何断言。
图框、图纸内容、出图清单规则都是数据（JSON / 属性包 / YAML），而不是硬编码。

---

## 贡献

欢迎贡献！优先考虑：
- **跨平台 bug**：Mac / Linux 上的渲染与交互问题（CI 会跑全平台 build）
- **出图能力**：更多零件类型（屋脊 / 道威 / 五角棱镜、窗口、镜组）的剖面与出图适配器
- **图框与清单**：新的图框预设（`Config/Frames/*.json`）、出图清单规则（`Config/Checklists/*.yaml`）
- **GB/T 13323-2009 / ISO 10110 合规细节**：代号格式、属性区行、玻璃库扩展

> ⚠️ **本仓库是上游主仓的「快照镜像」**：每次发布由内部主仓经脱敏导出后
> **force-push 覆盖**到这里，git 历史会被重置为单个 `Snapshot` 提交。
> 因此**直接 merge 进本仓的 PR 会在下次快照时被覆盖丢失** —— 我们不在镜像上原地合并。

贡献流程（改动最终进入上游，随下次快照回到本仓）：
1. 先开 [Issue](https://github.com/otocad/Otocad-Community/issues) / [Discussion](https://github.com/otocad/Otocad-Community/discussions) 描述问题或提案
2. Fork + 分支 `feature/your-feature`，本地 `dotnet build src/OtoCAD.Avalonia/` 与 `dotnet test src/lcdb.Tests/` 验证，提 PR（CI 跑跨平台 build + 测试）
3. maintainer review 后，把改动**应用到上游主仓**（而非在镜像直接 merge），随下一次脱敏快照发布进入本仓
4. 贡献者记入致谢（[NOTICE.md](NOTICE.md)）—— 代码进了快照，署名也一起进

代码规范见 [CONTRIBUTING.md](CONTRIBUTING.md)（环境 / 提交规范 / 代码风格 / CI）。

---

## 隐私

**本开源版不含任何遥测 / 埋点** —— 不发心跳、不上报使用数据。

AI 助手默认连接官方云服务 `cadask.optic.chat` 提供问答；该请求仅包含你的提问内容
与一个随机设备 ID（匿名 GUID），**不含图纸、文件或任何个人信息**。

- 不想用云助手：设环境变量 `OTOCAD_BACKEND_URL` 指向自建后端，或不打开 AI 面板。
- 其余全部 CAD 功能（绘图 / 标注 / 光学 / 导入 / 出图 / 导出）**完全离线**，不依赖任何在线服务。

## 许可证

MIT License — 查看 [LICENSE](LICENSE) 与 [NOTICE.md](NOTICE.md)。

---

## 链接

- 🌐 **官网 & 下载**：https://cad.optic.chat
- 📦 **Releases**：https://github.com/otocad/Otocad-Community/releases
- 📺 **B 站**：https://space.bilibili.com/88091818（教程 / 演示视频）
- 💬 **Discussions**：https://github.com/otocad/Otocad-Community/discussions
- 🐛 **Issues**：https://github.com/otocad/Otocad-Community/issues
