using System.Globalization;
using lcdb.DrawingFrame;
using lcdb.Optic;

namespace lcdb.Drawing;

/// <summary>
/// GB 单透镜图纸的属性驱动入口 (落地分期 P1) —— 先从透镜建 <see cref="DrawingPropertyBag"/>,
/// 再由包填 <see cref="OpticalDrawingFrame"/> (属性区三列 + 标题栏命名字段)。
///
/// 证明 "属性驱动渲染" 跑通: 渲染内容来自包, 不再是图框里散落的字段。
/// 簇键与图框既有命名字段键对齐 (DrawingNumber 等), 避免词汇漂移; 属性区代号沿用 GB 默认值。
/// 不改图框默认 EnsureColumns 路径 (未调 ApplyBag 的旧用法照旧渲染)。
/// </summary>
public static class GbLensSheet
{
    /// <summary>从透镜 + GB 默认代号建属性包 (标题栏 / 材料 / 前表面 / 后表面 四簇)。</summary>
    public static DrawingPropertyBag BuildBag(OpticalLens lens)
    {
        var bag = new DrawingPropertyBag();

        // 标题栏 (键 = 图框命名字段键, 单一真值)。product_number ≈ DrawingNumber (图号)。
        var title = bag.GetOrAddCluster("title", "标题栏");
        title.Add("DrawingNumber", "图号", "");
        title.Add("DrawingTitle", "名称", $"{lens.MaterialName} 透镜");
        title.Add("Material", "材料", lens.MaterialName);
        title.Add("Designer", "制图", "");
        title.Add("DesignDate", "日期", "");
        title.Add("Scale", "比例", "1:1");

        // 材料区 (GB/T 13323-2009 §2): 牌号 / n_d / v_d / 0/应力 1/气泡 2/不均匀。
        var mat = bag.GetOrAddCluster("material", "材料");
        mat.Add("material_name", "牌号", lens.MaterialName);
        mat.Add("n_d", "折射率 n_d", "n_d " + lens.RefractiveIndex.ToString("F4", CultureInfo.InvariantCulture));
        mat.Add("v_d", "阿贝数 v_d", "v_d " + lens.AbbeNumber.ToString("F2", CultureInfo.InvariantCulture));
        mat.Add("birefringence", "应力双折射", "0/20");
        mat.Add("bubble", "气泡度", "1/3×0.16");
        mat.Add("inhomogeneity", "不均匀性", "2/2;2");

        // 前/后表面区 (ISO 10110: 3/面形 5/疵病 4/对中 + 样板 + 纹理)。
        AddSurfaceCluster(bag, "surface-front", "前表面");
        AddSurfaceCluster(bag, "surface-back", "后表面");

        return bag;
    }

    private static void AddSurfaceCluster(DrawingPropertyBag bag, string key, string label)
    {
        var c = bag.GetOrAddCluster(key, label);
        c.Add("form_error", "面形精度 (3/)", "3/3(0.5)");
        c.Add("surface_imperf", "表面疵病 (5/)", "5/3×0.1");
        c.Add("centering_tol", "中心偏差 (4/)", "C=3'");
        c.Add("sample_grade", "样板精度", "ΔR=A");
        c.Add("surface_texture", "表面结构", "P3");
    }

    /// <summary>建包并填一张 GB 光学图框 (A4 纵)。属性驱动: 图框内容全来自包。</summary>
    public static OpticalDrawingFrame BuildFrame(OpticalLens lens,
        DrawingFrameTemplates.PaperSize size = DrawingFrameTemplates.PaperSize.A4,
        DrawingFrameTemplates.Orientation orient = DrawingFrameTemplates.Orientation.Portrait)
    {
        var bag = BuildBag(lens);
        var frame = DrawingFrameTemplates.CreateOptical(size, orient);
        frame.ApplyBag(bag);
        return frame;
    }
}
