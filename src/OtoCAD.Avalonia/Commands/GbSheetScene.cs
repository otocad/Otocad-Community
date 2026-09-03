using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using LitMath;
using lcdb;
using lcdb.Common;
using lcdb.Drawing;
using lcdb.DrawingFrame;
using lcdb.Optic;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// GB 中文光学零件图"场景装配"(对标 docs/需求/图面样例-单透镜2.png).
/// 属性驱动: 用 lcdb.Drawing 的 <see cref="DrawingPropertyBag"/> (单一真值源, 与 master 并行工作共用同一套包)
/// 填 <see cref="GbLensDrawingFrame"/> (中文三列 + GB/T 10609.1 标题栏 + 注 + 其余), 再加透镜剖面 + 自动尺寸.
///
/// 注: 包模型/簇键复用 lcdb.Drawing (避免与 master 的 lcdb.Drawing.GbLensSheet 重复造轮); 这里只做
/// UI 层"图框 + 透镜 + 蓝色自动标注"的装配 (AutoDimensionLensCmd 属 Avalonia, 不能下沉 lcdb).
/// </summary>
public static class GbSheetScene
{

    /// <summary>GB 样例图纸 (弯月 demo 单透镜, BK7) — 属性包驱动 GbLensDrawingFrame.</summary>
    public static List<Entity> BuildSample() => BuildSample(BuildSampleBag());

    /// <summary>用给定属性包(文档级单一真值源)生成 GB 样例图纸 (弯月 demo 透镜).</summary>
    public static List<Entity> BuildSample(DrawingPropertyBag bag)
        => BuildSheet(bag, new OpticalLens
        {
            Diameter = 60.0, Thickness = 10.0, R1 = -60.44, R2 = -50.17,
            MaterialName = "BK7", RefractiveIndex = 1.51872, AbbeNumber = 63.96,
            DiameterTolerance = new ToleranceValue(60.0, 0.1, 0.1),
            ThicknessTolerance = new ToleranceValue(10.0, 0.1, 0.1),
        });

    /// <summary>从任意单透镜出 GB 中文图纸: 属性包从透镜抽 (复用 master lcdb.Drawing.GbLensSheet.BuildBag), 再驱动 GB 图框.</summary>
    public static List<Entity> BuildFromLens(OpticalLens lens)
    {
        var entities = BuildFrameAndLayout(lcdb.Drawing.GbLensSheet.BuildBag(lens), lens.Diameter,
            out double cx, out double cy, out double k);
        var view = IsoSingleLensSheet.AppendScaledLensView(entities, (OpticalLens)lens.Clone(), cx, cy, k);
        AppendAsphericBlocks(entities, lens, view);
        return entities;
    }

    /// <summary>
    /// 非球面 → ISO 10110-12 数据块 (方程 + 矢高表 + 系数), 堆叠在绘图区左上.
    ///
    /// ISO 路径 (IsoSingleLensSheet.Build) 一直有这段, GB 路径漏了 —— 而 GB 是默认惯例,
    /// 于是非球面透镜走默认出图永远缺数据块, 出图清单的 error 级项恒红、图纸不可交付。
    /// 数值取真值透镜 (非缩放显示件), Owner 挂显示件以随重出/关联联动。
    /// </summary>
    private static void AppendAsphericBlocks(List<Entity> entities, OpticalLens real, Entity view)
    {
        var frame = entities.OfType<DrawingFrame>().FirstOrDefault();
        if (frame is null) return;

        double border = FrameBorder(frame);
        var cursor = new Vector2(frame.Origin.X + border + 2.0,
                                 frame.Origin.Y + frame.PaperHeight - border - 2.0);
        double semiAperture = real.Diameter * 0.5;

        IsoSingleLensSheet.EmitAsphericBlock(entities, real.FrontSurface, semiAperture, "1", ref cursor, view);
        IsoSingleLensSheet.EmitAsphericBlock(entities, real.BackSurface, semiAperture, "2", ref cursor, view);
    }

    /// <summary>从任意双胶合出 GB 中文图纸 (4 列属性区 + 双色剖面 + 真值标注).</summary>
    public static List<Entity> BuildDoubletFromLens(CementedLens src)
    {
        var entities = BuildFrameAndLayout(BuildDoubletBag(src), src.Diameter,
            out double cx, out double cy, out double k);
        IsoSingleLensSheet.AppendScaledDoubletView(entities, src, cx, cy, k);
        return entities;
    }

    /// <summary>样例单透镜装配 (弯月 demo).</summary>
    private static List<Entity> BuildSheet(DrawingPropertyBag bag, OpticalLens realLens)
    {
        var entities = BuildFrameAndLayout(bag, realLens.Diameter, out double cx, out double cy, out double k);
        IsoSingleLensSheet.AppendScaledLensView(entities, realLens, cx, cy, k);
        return entities;
    }

    /// <summary>
    /// 共用版面装配 (单/双胶合/棱镜共享): 由属性包定列数 → 底部块高 → scale-to-fit 选纸+比例 →
    /// 建图框 (比例写回包) → 返回含图框的实体表 + 绘图区中心 (cx,cy) + 比例 k, 供调用方贴剖面视图.
    ///
    /// 图框轴: 出图惯例的 FrameTemplateKey 解析到数据驱动定义 (Config/Frames 用户自定义图框) 就用
    /// <see cref="DataDrivenFrame"/>; 否则默认 GB 中文框. 两者都吃同一个属性包 (<see cref="IPropertyBagFrame"/>).
    /// </summary>
    private static List<Entity> BuildFrameAndLayout(DrawingPropertyBag bag, double partDiameter,
        out double cx, out double cy, out double k)
    {
        var entities = new List<Entity>();

        int rowCount = 0;
        foreach (var c in bag.Clusters)
            if (c.Key != "title" && c.Key != "notes") rowCount = Math.Max(rowCount, c.Properties.Count);

        DrawingFrame frame;
        double blockH;
        var def = OtoCAD.Avalonia.Services.FrameTemplateService.ResolveActive();
        if (def is not null)
        {
            var ddf = new DataDrivenFrame(def) { Origin = new Vector2(0, 0) };
            ddf.ApplyBag(bag);                 // 先套包: 属性区行数决定底部块高
            blockH = ddf.BottomBlockHeight();
            frame = ddf;
        }
        else
        {
            var gb = new GbLensDrawingFrame { Origin = new Vector2(0, 0) };
            blockH = gb.TitleBlockHeightGb + (rowCount + 1) * gb.TableRowHeight + 16; /*注区*/
            frame = gb;
        }

        var layout = OtoCAD.Avalonia.Templating.LayoutEngine.ComputeForBlock(partDiameter, blockH);
        frame.PaperWidth = layout.PaperW;
        frame.PaperHeight = layout.PaperH;

        // 标题栏"比例" = 真实绘图比例 (写回包, 兼容 scale/Scale 两种键)
        if (!bag.SetValue("scale", layout.ScaleLabel)) bag.SetValue("Scale", layout.ScaleLabel);

        ((IPropertyBagFrame)frame).ApplyBag(bag);
        entities.Add(frame);

        double border = FrameBorder(frame);
        cx = layout.PaperW * 0.5;
        cy = (border + blockH + (layout.PaperH - border)) * 0.5;
        k = layout.Scale;
        return entities;
    }

    /// <summary>图框主框边距 (绘图区从此往内); 各图框类各自持有该参数.</summary>
    private static double FrameBorder(DrawingFrame frame) => frame switch
    {
        GbLensDrawingFrame gb => gb.BorderMargin,
        DataDrivenFrame ddf => ddf.Definition.Border.Margin,
        IsoLensDrawingFrame iso => iso.BorderMargin,
        _ => 8.0,
    };

    /// <summary>样例属性包 (lcdb.Drawing 包模型). 簇键沿用 master 词汇(surface-front/material/surface-back), 标签照样例(左/右表面).</summary>
    public static DrawingPropertyBag BuildSampleBag()
    {
        var bag = new DrawingPropertyBag();

        var t = bag.GetOrAddCluster("title", "标题栏");
        t.Add("DrawingTitle", "名称", "透镜");
        t.Add("DrawingNumber", "图号", "(图样代号)");
        t.Add("unit", "单位名称", "(单位名称)");
        t.Add("scale", "比例", "1:1");
        t.Add("sheet", "张次", "共 1 张   第 1 张");
        t.Add("general_roughness", "其余", "Ra 1");

        var ls = bag.GetOrAddCluster("surface-front", "左表面");
        ls.Add("radius_left", "R", "R60.44CC");
        ls.Add("centering_left", "中心偏", "⊕ λ₀=520nm");
        ls.Add("chamfer_left", "倒角", "保护性倒角 0.2-0.4");
        ls.Add("form_left", "面形", "3/2(0.5)");
        ls.Add("waviness_left", "波纹度", "4/—");
        ls.Add("imperfection_left", "疵病", "5/5×0.16;L2×0.04;E0.5");

        var mat = bag.GetOrAddCluster("material", "材料技术要求");
        mat.Add("material_name", "牌号", "BK7");
        mat.Add("n_d", "n_d", "n_d=1.51872±0.001");
        mat.Add("v_d", "v_d", "v_d=63.96±0.51%");
        mat.Add("bubble", "气泡度", "0/10");
        mat.Add("inhomogeneity", "不均匀性", "1/5×0.16");
        mat.Add("birefringence", "双折射", "2/1;2");

        var rs = bag.GetOrAddCluster("surface-back", "右表面");
        rs.Add("radius_right", "R", "R50.17CX");
        rs.Add("cement_note", "", "待胶合面");
        rs.Add("chamfer_right", "倒角", "保护性倒角 0.2-0.4");
        rs.Add("form_right", "面形", "3/3(1)");
        rs.Add("waviness_right", "波纹度", "4/2'");
        rs.Add("imperfection_right", "疵病", "5/5×0.16;L2×0.04;E0.5");

        var n = bag.GetOrAddCluster("notes", "注释");
        n.Add("note1", "", "注1: 检测区实体内 1/3×0.1;");
        n.Add("note2", "", "注2: 检测区表面 5/3×0.1, L1×0.04;");
        n.Add("note3", "", "注3: 待胶合面。");

        return bag;
    }

    /// <summary>从双胶合抽属性包: 标题栏 + 前表面/胶合面/后表面 + 材料(两玻璃) 四区, 行带机器键.</summary>
    public static DrawingPropertyBag BuildDoubletBag(CementedLens c)
    {
        var bag = new DrawingPropertyBag();

        var t = bag.GetOrAddCluster("title", "标题栏");
        t.Add("DrawingTitle", "名称", $"{c.Material1}+{c.Material2} 双胶合");
        t.Add("DrawingNumber", "图号", "(图样代号)");
        t.Add("unit", "单位名称", "(单位名称)");
        t.Add("scale", "比例", "1:1");
        t.Add("sheet", "张次", "共 1 张   第 1 张");
        t.Add("general_roughness", "其余", "Ra 1");

        var sf = bag.GetOrAddCluster("surface-front", "前表面");
        sf.Add("radius_front", "R", FormatR(c.R1, frontElementFront: true));
        sf.Add("chamfer_front", "倒角", "保护性倒角 0.2-0.4");
        sf.Add("form_front", "面形", "3/3(1)");
        sf.Add("imperfection_front", "疵病", "5/5×0.16;L2×0.04;E0.5");

        var cm = bag.GetOrAddCluster("cement", "胶合面");
        cm.Add("radius_cement", "R", FormatR(c.RContact, frontElementFront: true));
        cm.Add("cement_note", "", "待胶合面");
        cm.Add("form_cement", "面形", "3/3(1)");

        var sb = bag.GetOrAddCluster("surface-back", "后表面");
        sb.Add("radius_back", "R", FormatR(c.R3, frontElementFront: false));
        sb.Add("chamfer_back", "倒角", "保护性倒角 0.2-0.4");
        sb.Add("form_back", "面形", "3/3(1)");
        sb.Add("imperfection_back", "疵病", "5/5×0.16;L2×0.04;E0.5");

        var m = bag.GetOrAddCluster("material", "材料技术要求");
        m.Add("g1", "G1", c.Material1);
        m.Add("g1_nd", "n_d", $"n_d {c.Nd1:F4}");
        m.Add("g1_vd", "v_d", $"v_d {c.Vd1:F2}");
        m.Add("g2", "G2", c.Material2);
        m.Add("g2_nd", "n_d", $"n_d {c.Nd2:F4}");
        m.Add("g2_vd", "v_d", $"v_d {c.Vd2:F2}");

        return bag;
    }

    /// <summary>半径显示文本: ∞ / R±值CX/CC (frontElementFront=true → R&gt;0 为 CX)。</summary>
    private static string FormatR(double r, bool frontElementFront)
    {
        if (double.IsInfinity(r) || Math.Abs(r) > 1e6) return "R = ∞";
        bool cx = frontElementFront ? r > 0 : r < 0;
        return $"R{(r > 0 ? "+" : "")}{r:F2} {(cx ? "CX" : "CC")}";
    }

    /// <summary>从直角棱镜出 GB 中文图纸: 属性包 (入射面/反射面/出射面 + 材料) 驱动 GB 图框 + 三角剖面 + 真值标注.</summary>
    public static List<Entity> BuildPrismFromPart(Prism prism)
    {
        // scale-to-fit 用外接长边 (棱镜无"口径", 类比透镜 Ø 占的竖向空间)
        double envelope = Math.Max(prism.Width, prism.Height);
        var entities = BuildFrameAndLayout(BuildPrismBag(prism), envelope,
            out double cx, out double cy, out double k);
        IsoSingleLensSheet.AppendScaledPrismView(entities, prism, cx, cy, k);
        return entities;
    }

    /// <summary>
    /// 从直角棱镜抽属性包: 标题栏 + 入射面/反射面/出射面 + 材料 四区 (列数同双胶合).
    ///
    /// 取值原则: 材料 n_d/v_d 等来自实体真值; 面形/疵病用 ISO 10110 通用代号的常见起始值
    /// (工程师在属性区点击改); 棱镜专属的直角偏差与通光孔径**不编造数值** —— 落"注"区标注"待填",
    /// 宁可空着让人填, 也不给一个看着像标准值的假公差 (厂方照假值加工即废件)。
    /// </summary>
    public static DrawingPropertyBag BuildPrismBag(Prism p)
    {
        var bag = new DrawingPropertyBag();

        var t = bag.GetOrAddCluster("title", "标题栏");
        t.Add("DrawingTitle", "名称", $"{p.MaterialName} 直角棱镜");
        t.Add("DrawingNumber", "图号", "(图样代号)");
        t.Add("unit", "单位名称", "(单位名称)");
        t.Add("scale", "比例", "1:1");
        t.Add("sheet", "张次", "共 1 张   第 1 张");
        t.Add("general_roughness", "其余", "Ra 1");

        AddPrismFaceCluster(bag, "surface-entry", "入射面", reflective: false);
        AddPrismFaceCluster(bag, "surface-hypotenuse", "反射面 (斜面)", reflective: true);
        AddPrismFaceCluster(bag, "surface-exit", "出射面", reflective: false);

        var m = bag.GetOrAddCluster("material", "材料技术要求");
        m.Add("material_name", "牌号", p.MaterialName);
        m.Add("n_d", "n_d", $"n_d={p.RefractiveIndex.ToString("F5", CultureInfo.InvariantCulture)}");
        m.Add("v_d", "v_d", $"v_d={p.AbbeNumber.ToString("F2", CultureInfo.InvariantCulture)}");
        m.Add("bubble", "气泡度", "0/10");
        m.Add("inhomogeneity", "不均匀性", "1/5×0.16");
        m.Add("birefringence", "双折射", "2/1;2");

        var n = bag.GetOrAddCluster("notes", "注释");
        n.Add("note_angle", "", "注1: 直角偏差 (待填);");
        n.Add("note_aperture", "", "注2: 通光孔径 (待填);");
        n.Add("note_pyramid", "", "注3: 塔差 (待填)。");

        return bag;
    }

    /// <summary>棱镜单面属性簇. 反射面多一行膜层 (直角棱镜斜面走全反射或镀反射膜, 由工程师定)。</summary>
    private static void AddPrismFaceCluster(DrawingPropertyBag bag, string key, string label, bool reflective)
    {
        string sfx = key.Replace("surface-", "");
        var c = bag.GetOrAddCluster(key, label);
        c.Add($"form_{sfx}", "面形", "3/3(1)");
        c.Add($"imperfection_{sfx}", "疵病", "5/5×0.16;L2×0.04;E0.5");
        c.Add($"coating_{sfx}", "膜层", reflective ? "(反射膜, 待填)" : "(增透膜, 待填)");
        c.Add($"chamfer_{sfx}", "倒角", "保护性倒角 0.2-0.4");
    }
}
