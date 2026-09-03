using System.Collections.Generic;
using lcdb;
using OtoCAD.Avalonia.Commands;

namespace OtoCAD.Avalonia.Templating;

/// <summary>
/// 标准图纸生成引擎 — 按零件类型分派到对应 <see cref="IPartAdapter"/>.
/// "快速构建标准图纸" 的统一入口: 选中件 → Generate → 整套图纸实体.
///
/// 设计 (见 memory: standard-sheet-engine-design):
///   N 适配器 (按零件) + M 图框构建器 (按标准) + 1 布局引擎.
///   Phase 1 = 适配器分派, 复用现有 IsoSingleLensSheet; 后续期接 DrawingConvention.Active /
///   抽 IFrameBuilder + LayoutEngine (scale-to-fit/自动选纸) / JSON 配置 / 更多适配器.
/// </summary>
public static class StandardSheetEngine
{
    private static readonly IPartAdapter[] Adapters =
    {
        new LensPartAdapter(),
        new DoubletPartAdapter(),
        new PrismPartAdapter(),
    };

    /// <summary>按零件生成整套图纸; 不支持的类型返回 null.</summary>
    public static List<Entity>? Generate(Entity part, SheetMeta meta)
    {
        if (part is null) return null;

        // 图框轴: 惯例选 GB 中文框或用户自定义 (数据驱动) 图框 → 单透镜/双胶合/棱镜 都走属性包驱动场景
        // (GbSheetScene 按惯例造 GbLensDrawingFrame 或 DataDrivenFrame); 其它 (iso-lens 等) 走 ISO 适配器。
        if (UsesPropertyBagScene)
        {
            if (part is lcdb.Optic.OpticalLens lens) return GbSheetScene.BuildFromLens(lens);
            if (part is lcdb.Optic.CementedLens doublet) return GbSheetScene.BuildDoubletFromLens(doublet);
            // 棱镜: 只直角棱镜有真实剖面几何 (其余类型 lcdb.Prism 仍是三角形占位, 出图会给错轮廓)
            if (part is lcdb.Optic.Prism { Type: lcdb.Optic.PrismType.RightAngle } prism)
                return GbSheetScene.BuildPrismFromPart(prism);
        }

        foreach (var a in Adapters)
            if (a.CanHandle(part))
                return a.BuildSheet(part, meta);
        return null;
    }

    /// <summary>当前出图惯例的图框是否走属性包驱动场景: GB 中文框, 或解析得到的数据驱动 (自定义) 图框.</summary>
    private static bool UsesPropertyBagScene
    {
        get
        {
            var key = lcdb.Standards.DrawingConventionService.Active?.FrameTemplateKey;
            return key == "gb-lens" || OtoCAD.Avalonia.Services.FrameTemplateService.Resolve(key) is not null;
        }
    }

    /// <summary>启动/演示样例 — 按惯例图框轴: GB 中文/自定义图框样例 或 ISO 平凸样例.</summary>
    public static List<Entity> SampleSheet()
        => UsesPropertyBagScene ? GbSheetScene.BuildSample() : IsoSingleLensSheet.BuildSample();
}
