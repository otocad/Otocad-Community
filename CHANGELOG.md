# 更新日志

OtoCAD Community 的所有重要变更都会记录在本文件中。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，
并遵循[语义化版本](https://semver.org/lang/zh-CN/)。

## [0.3.1]

自 0.1.0 以来的社区版变更。

### 新增

- **跨平台二进制发布**：Windows / Linux / macOS（Intel + Apple Silicon）self-contained
  单文件包，解压即用，无需安装 .NET。
- **标记贴面放置**：`ISurfaceAttachable` + 通用 `PlaceSurfaceMarkCmd`，标记可吸附到实体表面。
- **AI 助手面板**（Community 为 UI 脚手架，接入 OtoCAD Cloud 后启用）：消息可选择 / 复制、
  轻量 Markdown 渲染、离线自动重连。

### 变更

- 默认图纸的表面粗糙度改用真实标记实体渲染。

### 修复

- 选区 / 删除不再误删锁定图层（图框）；`Ctrl+A` 全选行为修正。

## [0.1.0] — 首个公开版本

OtoCAD Community 的首个开源版本 —— 基于 Avalonia + SkiaSharp 构建的跨平台二维光学
CAD 绘图应用，与桌面版共享同一 `lcdb` 业务核心。

### 新增

- **跨平台桌面端**（Avalonia + SkiaSharp），共享 `lcdb` 业务核心：
  `lcdb`（实体/数据库）· `lcinterface`（接口）· `LitMath`（数学库）。
- **绘图**：直线 · 圆 · 圆弧 · 矩形 · 多段线 · 多边形 · 椭圆 · 样条 ·
  点 · 射线 · 构造线 · 单行文本 · 多行文本 · 引线。
- **编辑**：移动 · 复制 · 旋转 · 缩放 · 镜像 · 偏移 · 删除 ·
  全选 / 取消选择，支持基于操作的撤销 / 重做。
- **标注**：线性 · 对齐 · 半径 · 直径 · 角度 · 坐标，标注线支持夹点拖拽。
- **光学标记**：表面处理、粗糙度、面形精度、定心公差、表面疵病/质量，
  以及符合 ISO 10110 / GB/T 标准的标记。
- **交互**：单击与框选、精确拾取、捕捉引擎（端点 / 中点 / 圆心）、
  平移 / 缩放、自适应网格。
- **界面**：JSON 驱动的 Ribbon 布局、反射构建的属性面板、
  键盘快捷键、状态栏、浅色 / 深色 / 默认主题。
- **文件**：`.otocad`（自定义 JSON）格式，新建 / 打开 / 保存 / 另存为、
  最近文件、自动保存与滚动备份。
- **导入 / 导出**：DXF（AutoCAD 2000+）与 PDF（A4 横向矢量）。
- **打包**：自包含单文件 Windows 构建 + Inno Setup 安装包。
- **持续集成**：GitHub Actions 跨 Windows / macOS / Linux 构建。
