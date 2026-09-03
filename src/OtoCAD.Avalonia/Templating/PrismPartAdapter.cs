using System.Collections.Generic;
using lcdb;
using lcdb.Optic;
using OtoCAD.Avalonia.Commands;

namespace OtoCAD.Avalonia.Templating;

/// <summary>
/// 棱镜 (Prism) 适配器 — 复用 GbSheetScene 的属性包 + 图框装配, 贴三角剖面视图.
///
/// ⚠️ 只接直角棱镜: lcdb.Prism 的 Generate 目前只真实实现 <see cref="PrismType.RightAngle"/>,
/// Roof / Dove / Penta 全部 fallback 成同一个三角形占位。给五角棱镜出一张三角形的图,
/// 厂方照图加工就是废件 —— 宁可 CanHandle 返回 false 让上层提示"不支持", 也不出错图。
/// 待这些类型的剖面几何在 lcdb.Prism 里实做后, 此处放开即可。
/// </summary>
public sealed class PrismPartAdapter : IPartAdapter
{
    public bool CanHandle(Entity part) => part is Prism { Type: PrismType.RightAngle };

    public List<Entity> BuildSheet(Entity part, SheetMeta meta)
    {
        var prism = (Prism)part;
        var entities = GbSheetScene.BuildPrismFromPart(prism);
        ApplyMeta(entities, meta);
        return entities;
    }

    /// <summary>把 SheetMeta 的非空字段覆盖到已生成图框的标题栏 (空值保留属性包默认)。</summary>
    private static void ApplyMeta(List<Entity> entities, SheetMeta m)
    {
        foreach (var e in entities)
        {
            if (e is not lcdb.DrawingFrame.IPropertyBagFrame f || f.Bag is not { } bag) continue;

            if (!string.IsNullOrEmpty(m.ProjectPart)) bag.SetValue("DrawingTitle", m.ProjectPart);
            if (!string.IsNullOrEmpty(m.Sheet)) bag.SetValue("sheet", m.Sheet);
            // 比例不覆盖: BuildFrameAndLayout 的 scale-to-fit 已写入真实绘图比例
            f.ApplyBag(bag);
            break;
        }
    }
}
