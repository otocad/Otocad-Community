# NOTICE

OtoCAD 是 RayPragma 在 SharpCAD 基础上演化而来的二维光学 CAD 程序。本文件列出
项目集成的第三方代码归属信息 (vendored, 即源码包含在本仓 `src/` 里的第三方项目).

## 上游派生

### SharpCAD (initial codebase)
- **作者**: Hisin Wang
- **协议**: MIT License (2018)
- **范围**: 项目骨架, 包括 `lcdb` / `lcinterface` / `LitMath` 三个子项目的核心结构,
  以及 Database / Entity / Layer / Block / Polyline / Arc 等 ObjectARX 风格基础类.
- **本仓 LICENSE 已合并版权声明** (双 copyright, OtoCAD 衍生开发同 MIT 协议)

OtoCAD 后续重写/扩展了大量代码 (Avalonia + SkiaSharp 重构, ISO 10110 光学标注体系,
GB/T 13323-2009 光学制图合规, Annotation 关联系统等), 但核心命名空间与部分基础类
的设计仍可追溯到 SharpCAD.

## 内嵌的第三方代码 (vendored)

### netDxf
- **协议**: MIT
- **来源**: https://github.com/jankozik/netDxf  (commit 26e4d11)
- **位置**: `src/netDxf/`
- **用途**: DXF 文件读写

NuGet 引入的标准 OSS 包 (Avalonia / SkiaSharp / NLog 等) 见各 `*.csproj` 的
`PackageReference`, 按各自 NuGet 元数据中的协议授权, 此处不再单独列出.

## 法律声明

本项目以 MIT License 发布 (见 `LICENSE`). 任何分发本软件 (含修改版) 时,
必须保留 `LICENSE` 与本 `NOTICE.md` 文件以及其中的版权声明.
