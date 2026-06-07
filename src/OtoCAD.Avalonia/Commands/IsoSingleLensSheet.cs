using System;
using System.Collections.Generic;
using LitMath;
using lcdb;
using lcdb.Common;
using lcdb.DrawingFrame;
using lcdb.Optic;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// ISO 10110 单透镜零件图 "快速生成器" (optiland #458 / lutzerb 原型样例风格).
///
/// 输入透镜规格 → 输出一整套图纸实体: 竖版 ISO 图框 (三列表 + 标题栏 + NOTES)
/// + 玻璃浅蓝填充的透镜剖面 + Ø / 中心厚 尺寸 + 光轴标签. 图框/透镜居中于 A4 竖版.
///
/// 启动场景默认生成平凸样例 (PCX-001); 其它透镜可调 <see cref="Build"/> 复用.
/// </summary>
public static class IsoSingleLensSheet
{
    /// <summary>玻璃剖面浅蓝.</summary>
    private static readonly lcdb.Colors.Color GlassFill = lcdb.Colors.Color.FromRGB(0xCD, 0xE4, 0xF3);

    /// <summary>规格输入 — 一个透镜图纸所需的全部参数 (默认即平凸样例).</summary>
    public sealed class Spec
    {
        // 透镜几何
        public double Diameter = 70.0;
        public double? MechanicalDiameter = null;      // 机械外径 (>净口径时画平肩)
        public double FrontSemiAperture = 0.0;         // 前表面逐面净口径半径 (0=用 Diameter/2)
        public double BackSemiAperture = 0.0;          // 后表面逐面净口径半径
        public double Thickness = 20.0;
        public double R1 = 90.0;                       // 前表面 (CX)
        public double R2 = double.PositiveInfinity;    // 后表面 (平)
        public double DiameterTol = 0.1;
        public double ThicknessTol = 0.1;
        public string MaterialName = "N-BK7";
        public double Nd = 1.5168;
        public double Vd = 64.17;

        // 三列表内容
        public List<string> Surface1Rows = new()
        {
            "R = +90.000 +0.5/-0.5 CX",
            "Øe 62.00 +0/-0.5",
            "Chamfer 0.3-0.8 × 45°",
            "3/ (0.5/0.25)",
            "4/ 0.5'",
            "5/ 2×0.04; L1×0.001; E0.5",
            "7/ AR 400-700 nm, R<0.5%",
        };
        public List<string> MaterialRows = new()
        {
            "N-BK7",
            "n_d = 1.5168",
            "v_d = 64.17",
            "0/ 10",
            "1/ 1×0.16",
            "2/ 2; A",
        };
        public List<string> Surface2Rows = new()
        {
            "R = ∞",
            "Øe 62.00 +0/-0.5",
            "Chamfer 0.3-0.8 × 45°",
            "3/ (1.0)",
            "4/ 0.5'",
            "5/ 2×0.063; C1×0.016",
            "7/ AR 400-700 nm, R<0.5%",
        };

        // 标题栏
        public string ProjectPart = "Plano-Convex Demo    PCX-001";
        public string DrawnBy = "B. Lutzer";
        public string ApprovedBy = "—";
        public string DrawDate = "";          // 空 = 用今天
        public string DrawScale = "2:1";
        public string Sheet = "1 / 1";
        public string Rev = "A";

        public string Notes = "Plano-convex singlet; clean with lens tissue only.";
    }

    /// <summary>生成默认平凸样例图纸.</summary>
    public static List<Entity> BuildSample() => Build(new Spec());

    /// <summary>从已有透镜抽取规格 (几何 + 材料 + R 行), 其它 ISO 代码用默认值.</summary>
    public static Spec SpecFromLens(OpticalLens lens)
    {
        var s = new Spec
        {
            Diameter = lens.Diameter,
            MechanicalDiameter = lens.MechanicalDiameter,
            FrontSemiAperture = lens.FrontSurface.SemiAperture,
            BackSemiAperture = lens.BackSurface.SemiAperture,
            Thickness = lens.Thickness,
            R1 = lens.R1,
            R2 = lens.R2,
            MaterialName = lens.MaterialName,
            Nd = lens.RefractiveIndex,
            Vd = lens.AbbeNumber,
            DiameterTol = lens.DiameterTolerance?.Plus ?? 0.1,
            ThicknessTol = lens.ThicknessTolerance?.Plus ?? 0.1,
            DrawScale = "1:1",
            ProjectPart = $"{lens.MaterialName} Lens",
        };
        s.Surface1Rows = SurfaceRows(lens.R1, isFront: true);
        s.Surface2Rows = SurfaceRows(lens.R2, isFront: false);
        s.MaterialRows = new List<string>
        {
            lens.MaterialName,
            $"n_d = {lens.RefractiveIndex:F4}",
            $"v_d = {lens.AbbeNumber:F2}",
            "0/ 10",
            "1/ 1×0.16",
            "2/ 2; A",
        };
        return s;
    }

    private static List<string> SurfaceRows(double r, bool isFront)
    {
        string rRow;
        if (double.IsInfinity(r) || System.Math.Abs(r) > 1e6)
            rRow = "R = ∞";
        else
        {
            bool convex = isFront ? r > 0 : r < 0;
            rRow = $"R = {(r > 0 ? "+" : "")}{r:F3} {(convex ? "CX" : "CC")}";
        }
        return new List<string>
        {
            rRow,
            "Øe —",
            "Chamfer 0.3-0.8 × 45°",
            "3/ (1.0)",
            "4/ 0.5'",
            "5/ 2×0.063",
            "7/ AR 400-700 nm, R<0.5%",
        };
    }

    /// <summary>按规格生成一整套图纸实体 (A4 竖版).</summary>
    public static List<Entity> Build(Spec spec)
    {
        const double paperW = 210, paperH = 297;
        var entities = new List<Entity>();

        var frame = new IsoLensDrawingFrame
        {
            Origin = new Vector2(0, 0),
            PaperWidth = paperW,
            PaperHeight = paperH,
            Columns = new List<IsoSpecColumn>
            {
                new() { Title = "SURFACE 1  (FRONT)", Rows = spec.Surface1Rows },
                new() { Title = "MATERIAL",           Rows = spec.MaterialRows },
                new() { Title = "SURFACE 2  (REAR)",  Rows = spec.Surface2Rows },
            },
            ProjectPart = spec.ProjectPart,
            DrawnBy = spec.DrawnBy,
            ApprovedBy = spec.ApprovedBy,
            DrawDate = string.IsNullOrEmpty(spec.DrawDate) ? DateTime.Today.ToString("yyyy-MM-dd") : spec.DrawDate,
            DrawScale = spec.DrawScale,
            Sheet = spec.Sheet,
            Rev = spec.Rev,
            Notes = spec.Notes,
        };
        entities.Add(frame);

        // 绘图区 = 主图框内、底部块之上. 估算底部块高度与图框一致.
        double border = frame.BorderMargin;
        int rowCount = Math.Max(Math.Max(spec.Surface1Rows.Count, spec.Surface2Rows.Count), spec.MaterialRows.Count);
        double blockH = frame.NotesRowHeight + frame.TitleRowHeight + (rowCount + 1) * frame.TableRowHeight;
        double areaBottom = border + blockH;
        double areaTop = paperH - border;
        double cx = paperW * 0.5;
        double cy = (areaBottom + areaTop) * 0.5;

        var lens = new OpticalLens
        {
            Position = new Vector2(cx, cy),
            Diameter = spec.Diameter,
            MechanicalDiameter = spec.MechanicalDiameter,
            Thickness = spec.Thickness,
            R1 = spec.R1,
            R2 = spec.R2,
            MaterialName = spec.MaterialName,
            RefractiveIndex = spec.Nd,
            AbbeNumber = spec.Vd,
            FillColor = GlassFill,
            AxisPadding = 30.0,
            DiameterTolerance = new ToleranceValue(spec.Diameter, spec.DiameterTol, spec.DiameterTol),
            ThicknessTolerance = new ToleranceValue(spec.Thickness, spec.ThicknessTol, spec.ThicknessTol),
        };
        // 逐面净口径 (R-setter 建的是 SemiAperture=0 的球面, 这里补回, 保持与预览一致的逐面弧/平肩)
        if (spec.FrontSemiAperture > 1e-9) lens.FrontSurface.SemiAperture = spec.FrontSemiAperture;
        if (spec.BackSemiAperture > 1e-9) lens.BackSurface.SemiAperture = spec.BackSemiAperture;
        entities.Add(lens);

        // 尺寸 (ISO 样例风格): 中心厚在下 / 边厚(参考值,括号)在上 / Ø 在右.
        // R 在三列表里, 图上不画 R 引线.
        double halfT = spec.Thickness * 0.5;
        double halfD = spec.Diameter * 0.5;
        double frontApexX = cx - halfT, backApexX = cx + halfT;
        double frontEdgeX = frontApexX + lens.FrontSurface.Sag(halfD);
        double backEdgeX  = backApexX  + lens.BackSurface.Sag(halfD);
        var style = IsoDimStyle();
        string diaTol = $"±{spec.DiameterTol:F3}";   // 对称公差 → 规范 ± 标法 (非 +/-)
        string ctTol  = $"±{spec.ThicknessTol:F3}";

        // 中心厚 (下方, 顶点到顶点)
        AddLinearDim(entities, lens, style,
            new Vector2(frontApexX, cy), new Vector2(backApexX, cy),
            new Vector2(cx, cy - halfD - 12), 0.0, $"<>{ctTol}");
        // 边厚 (上方, 参考值括号; 前/后表面在边缘的横向距)
        AddLinearDim(entities, lens, style,
            new Vector2(frontEdgeX, cy + halfD), new Vector2(backEdgeX, cy + halfD),
            new Vector2(cx, cy + halfD + 10), 0.0, "(<>)");
        // Ø 通光直径 (右方, 竖直)
        AddLinearDim(entities, lens, style,
            new Vector2(cx, cy - halfD), new Vector2(cx, cy + halfD),
            new Vector2(backApexX + 22, cy), -System.Math.PI / 2, $"Ø<>{diaTol}");

        // 光轴标签 (落在光轴左端)
        double axisLeft = frontApexX - lens.AxisPadding;
        entities.Add(new MText
        {
            Position = new Vector2(axisLeft + 1.0, cy + 1.5),
            Value = "opt. axis",
            Height = 2.2,
        });

        // 表面粗糙度标记 (∇ + Ra) — 真实可选中实体, 贴在前/后表面上
        AddSurfaceFinishMark(entities, lens.FrontSurface, frontApexX, cy, halfD * 0.62, glassOnPlusX: true, 5.0);
        AddSurfaceFinishMark(entities, lens.BackSurface,  backApexX,  cy, halfD * 0.45, glassOnPlusX: false, 5.0);
        // 第一角投影符号现由图框 (IsoLensDrawingFrame) 自绘, 归图框层

        return entities;
    }

    // ===================== 双胶合 (CementedLens) =====================

    /// <summary>片 2 (火石玻璃) 填充浅暖色, 与片 1 浅蓝区分.</summary>
    private static readonly lcdb.Colors.Color GlassFill2 = lcdb.Colors.Color.FromRGB(0xF3, 0xE6, 0xCD);

    /// <summary>默认双胶合消色差样例 (ACH-001).</summary>
    public static List<Entity> BuildDoubletSample()
    {
        var src = new CementedLens
        {
            Diameter = 60.0,
            T1 = 12.0,
            T2 = 6.0,
            R1 = 130.0,
            RContact = -95.0,
            R3 = -340.0,
            Material1 = "N-BK7", Nd1 = 1.5168, Vd1 = 64.17,
            Material2 = "F2", Nd2 = 1.6200, Vd2 = 36.37,
            DiameterTolerance = new ToleranceValue(60.0, 0.1, 0.1),
            T1Tolerance = new ToleranceValue(12.0, 0.1, 0.1),
            T2Tolerance = new ToleranceValue(6.0, 0.1, 0.1),
        };
        return BuildDoublet(src, "Achromatic Doublet    ACH-001", "B. Lutzer", "1:1",
            "Cemented achromat; protect cement joint from solvents.");
    }

    /// <summary>按一个 CementedLens 生成整套 ISO 双胶合图纸 (A4 竖版, 4 列规格表).</summary>
    public static List<Entity> BuildDoublet(CementedLens src, string projectPart, string drawnBy, string scale, string notes)
    {
        const double paperW = 210, paperH = 297;
        var entities = new List<Entity>();

        double diaTolV = src.DiameterTolerance?.Plus ?? 0.1;
        double dia = src.Diameter;

        var frame = new IsoLensDrawingFrame
        {
            Origin = new Vector2(0, 0),
            PaperWidth = paperW,
            PaperHeight = paperH,
            Columns = new List<IsoSpecColumn>
            {
                new() { Title = "SURFACE 1 (FRONT)", Rows = CementedSurfaceRows(src.R1, frontElementFront: true, dia) },
                new() { Title = "SURFACE 2 (CEMENT)", Rows = CementContactRows(src.RContact, dia) },
                new() { Title = "SURFACE 3 (REAR)",  Rows = CementedSurfaceRows(src.R3, frontElementFront: false, dia) },
                new() { Title = "MATERIALS",          Rows = CementedMaterialRows(src) },
            },
            ProjectPart = projectPart,
            DrawnBy = drawnBy,
            ApprovedBy = "—",
            DrawDate = DateTime.Today.ToString("yyyy-MM-dd"),
            DrawScale = scale,
            Sheet = "1 / 1",
            Rev = "A",
            Notes = notes,
        };
        entities.Add(frame);

        // 布局: 绘图区中心
        double border = frame.BorderMargin;
        int rowCount = 0;
        foreach (var col in frame.Columns) rowCount = Math.Max(rowCount, col.Rows.Count);
        double blockH = frame.NotesRowHeight + frame.TitleRowHeight + (rowCount + 1) * frame.TableRowHeight;
        double cx = paperW * 0.5;
        double cy = (border + blockH + (paperH - border)) * 0.5;

        // 克隆 src (保留逐面表面/净口径/机械外径/非球面/材料), 仅重置位置与图纸用填充. 不再从 R 标量重建(会丢逐面口径).
        var lens = (CementedLens)src.Clone();
        lens.Position = new Vector2(cx, cy);
        lens.AxisPadding = 30.0;
        lens.FillColor1 = GlassFill;
        lens.FillColor2 = GlassFill2;
        entities.Add(lens);

        // 尺寸: Ø(右) / 总中心厚(下) / 边厚参考(上). R 在表里.
        double halfD = dia * 0.5;
        double halfT = (src.T1 + src.T2) * 0.5;
        double frontApexX = cx - halfT, backApexX = cx + halfT;
        double frontEdgeX = frontApexX + lens.FrontSurface.Sag(halfD);
        double backEdgeX  = backApexX  + lens.BackSurface.Sag(halfD);
        var style = IsoDimStyle();
        string diaTol = $"±{diaTolV:F3}";   // 对称公差 → 规范 ± 标法

        AddLinearDim(entities, lens, style,
            new Vector2(frontApexX, cy), new Vector2(backApexX, cy),
            new Vector2(cx, cy - halfD - 12), 0.0, $"CT <>{diaTol}");
        AddLinearDim(entities, lens, style,
            new Vector2(frontEdgeX, cy + halfD), new Vector2(backEdgeX, cy + halfD),
            new Vector2(cx, cy + halfD + 10), 0.0, "(<>)");
        AddLinearDim(entities, lens, style,
            new Vector2(cx, cy - halfD), new Vector2(cx, cy + halfD),
            new Vector2(backApexX + 22, cy), -System.Math.PI / 2, $"Ø<>{diaTol}");

        // 光轴标签
        entities.Add(new MText
        {
            Position = new Vector2(frontApexX - lens.AxisPadding + 1.0, cy + 1.5),
            Value = "opt. axis",
            Height = 2.2,
        });

        // 表面粗糙度标记 (∇ + Ra) — 真实可选中实体, 贴在前/后外表面上
        AddSurfaceFinishMark(entities, lens.FrontSurface, frontApexX, cy, halfD * 0.62, glassOnPlusX: true, 5.0);
        AddSurfaceFinishMark(entities, lens.BackSurface,  backApexX,  cy, halfD * 0.45, glassOnPlusX: false, 5.0);
        // 第一角投影符号现由图框 (IsoLensDrawingFrame) 自绘, 归图框层

        return entities;
    }

    /// <summary>双胶合外表面 (前/后) 的 ISO 行. frontElementFront=true → 前表面 (CX⇔R>0).</summary>
    private static List<string> CementedSurfaceRows(double r, bool frontElementFront, double dia)
    {
        string rRow;
        if (double.IsInfinity(r) || Math.Abs(r) > 1e6)
            rRow = "R = ∞";
        else
        {
            bool convex = frontElementFront ? r > 0 : r < 0;
            rRow = $"R = {(r > 0 ? "+" : "")}{r:F3} {(convex ? "CX" : "CC")}";
        }
        return new List<string>
        {
            rRow,
            $"Øe {dia:F2}",
            "Chamfer 0.3-0.8 × 45°",
            "3/ (1.0)",
            "4/ 0.5'",
            "5/ 2×0.063",
            "7/ AR 400-700 nm, R<0.5%",
        };
    }

    /// <summary>胶合面 (内表面) 的 ISO 行 — 规格较少.</summary>
    private static List<string> CementContactRows(double r, double dia)
    {
        bool convex = r < 0;   // 作为片1后表面: R<0 凸
        string rRow = (double.IsInfinity(r) || Math.Abs(r) > 1e6)
            ? "R = ∞"
            : $"R = {(r > 0 ? "+" : "")}{r:F3} {(convex ? "CX" : "CC")}";
        return new List<string>
        {
            rRow,
            $"Øe {dia:F2}",
            "(cemented)",
            "3/ (1.0)",
            "4/ 0.5'",
        };
    }

    /// <summary>双胶合材料列: 两片玻璃 + 厚度 + 元件级 ISO 代码.</summary>
    private static List<string> CementedMaterialRows(CementedLens c)
    {
        return new List<string>
        {
            $"G1: {c.Material1}",
            $"nd {c.Nd1:F4} / vd {c.Vd1:F2}",
            $"G2: {c.Material2}",
            $"nd {c.Nd2:F4} / vd {c.Vd2:F2}",
            $"T1 = {c.T1:F1}  T2 = {c.T2:F1}",
            "0/ 10",
            "1/ 1×0.16",
            "2/ 2; A",
        };
    }

    /// <summary>
    /// 在透镜表面半口径 h 处放一个真·表面粗糙度标记 (GB/T 131 ∇ + Ra 值) — 贴面相切:
    /// V 顶点落在表面上、符号轴沿表面外法线竖立。是真正的 SurfaceRoughnessMark 实体, 可选中/编辑。
    /// (取代原先 3 条裸 Line 的简化指示符号。)
    /// </summary>
    /// <param name="glassOnPlusX">true = 玻璃在该表面 +X 侧 (前表面), 外法线朝 -X; false = 后表面, 朝 +X。</param>
    private static void AddSurfaceFinishMark(
        List<Entity> dst, OpticalSurface surf, double apexX, double cy, double h, bool glassOnPlusX, double size)
    {
        var pt = new Vector2(apexX + surf.Sag(h), cy + h);
        // 外法线: 表面参数化 P(h)=(apexX+Sag(h), cy+h), 切向 (Sag'(h),1), 法向 (1,-Sag'(h)) 指向 +X。
        const double d = 0.05;
        double slope = (surf.Sag(h + d) - surf.Sag(h - d)) / (2 * d);   // dSag/dh (数值求导)
        var nPlusX = new Vector2(1, -slope).normalized;
        var outward = glassOnPlusX ? -nPlusX : nPlusX;                  // 玻璃在 +X 侧 → 外侧朝 -X
        var mark = new lcdb.Annotation.SurfaceRoughnessMark(pt, size) { ShowText = true };
        mark.AttachToSurface(pt, outward);
        dst.Add(mark);
    }

    private static lcdb.DimensionStyle IsoDimStyle()
    {
        var s = OtoCAD.Avalonia.Services.DimensionStandardService.CreateStyle();
        s.TextHeight = 2.6;
        s.ArrowSize = 2.0;
        s.ExtensionLineExtend = 1.2;
        s.ExtensionLineOffset = 0.6;
        s.DecimalFormat = "F2";   // 20.00 / 70.00 / 12.92 (与样例一致)
        return s;
    }

    private static void AddLinearDim(List<Entity> dst, Entity owner, lcdb.DimensionStyle style,
        Vector2 a, Vector2 b, Vector2 dimLinePos, double rot, string userText)
    {
        var mid = (a + b) * 0.5;
        var dir = new Vector2(System.Math.Cos(rot), System.Math.Sin(rot));
        var perp = Vector2.Perpendicular(dir);
        double off = Vector2.Dot(dimLinePos - mid, perp);
        if (off < 0) { rot += System.Math.PI; off = -off; }
        dst.Add(new LinearDimension(a, b, off, rot, style.Clone()) { userText = userText, Owner = owner });
    }
}
