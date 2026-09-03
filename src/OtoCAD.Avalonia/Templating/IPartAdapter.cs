using System.Collections.Generic;
using lcdb;

namespace OtoCAD.Avalonia.Templating;

/// <summary>
/// 零件适配器 — 把一种光学零件 (透镜/双胶合/棱镜/窗口/镜组…) 翻译成"这张图上要画什么".
/// 集中该零件类型专属的: 规格表内容、剖面视图、应标哪些尺寸.
///
/// Phase 1: 适配器封装现有 IsoSingleLensSheet 的成熟出图逻辑 (整张图);
/// Phase 2 起拆出 IFrameBuilder + LayoutEngine, 适配器只负责 视图/列/尺寸 三件套.
/// </summary>
public interface IPartAdapter
{
    /// <summary>是否能处理该零件实体.</summary>
    bool CanHandle(Entity part);

    /// <summary>为该零件生成一整套标准图纸实体 (图框 + 剖面 + 尺寸 + 表格 + 符号).</summary>
    List<Entity> BuildSheet(Entity part, SheetMeta meta);
}
