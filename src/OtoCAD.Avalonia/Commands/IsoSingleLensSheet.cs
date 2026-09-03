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
    /// <summary>
    /// 光轴伸出零件两端的图面长度 (mm). 是制图装饰而非零件几何 —— 不能乘绘图比例:
    /// 旧码 30*k 在 k=5 时给出 150mm, 超过 A4 纸宽一半, 把光轴与 "opt. axis" 标签推出图框
    /// (触发出图清单 inside-frame 告警)。固定图面值对任何比例都稳, 且够放标签。
    /// </summary>
    private const double AxisPaddingSheet = 16.0;

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

        // 版面 (由 LayoutEngine 决定; 默认 A4 1:1)
        public double PaperW = 210;
        public double PaperH = 297;
        public double ScaleK = 1.0;   // 图上画的 / 真实 (k); 标注始终显示真值

        // 真实面模型 (仅用于检测非球面并产出 ISO 10110-12 数据块; null = 无, 不影响球面图).
        // 注: 不参与显示几何 (显示仍按 R1/R2 球面轮廓), 数据块携带真值 R/K/系数。
        public OpticalSurface? FrontSurfaceModel = null;
        public OpticalSurface? BackSurfaceModel = null;
    }

    /// <summary>生成默认平凸样例图纸 (自动 scale-to-fit).</summary>
    public static List<Entity> BuildSample()
    {
        var spec = new Spec();
        ApplyLayout(spec);
        return Build(spec);
    }

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
            FrontSurfaceModel = lens.FrontSurface,
            BackSurfaceModel = lens.BackSurface,
        };
        s.Surface1Rows = SurfaceRows(lens.R1, isFront: true, lens.Diameter, lens.MechanicalDiameter);
        s.Surface2Rows = SurfaceRows(lens.R2, isFront: false, lens.Diameter, lens.MechanicalDiameter);
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

    private static List<string> SurfaceRows(double r, bool isFront, double clearDia, double? mechDia)
    {
        string rRow;
        if (double.IsInfinity(r) || System.Math.Abs(r) > 1e6)
            rRow = "R = ∞";
        else
        {
            bool convex = isFront ? r > 0 : r < 0;
            rRow = $"R = {(r > 0 ? "+" : "")}{r:F3} {(convex ? "CX" : "CC")}";
        }
        var rows = new List<string> { rRow };
        // 有效口径: 仅当与机械外径不同才标 (用户规则: Øe == 机械口径时可不标)。
        if (mechDia.HasValue && mechDia.Value > clearDia + 1e-6)
            rows.Add($"Øe {clearDia:F2}");
        rows.AddRange(new[]
        {
            "Chamfer 0.3-0.8 × 45°",
            "3/ (1.0)",
            "4/ 0.5'",
            "5/ 2×0.063",
            "7/ AR 400-700 nm, R<0.5%",
        });
        return rows;
    }

    /// <summary>按规格生成一整套图纸实体 (A4 竖版).</summary>
    public static List<Entity> Build(Spec spec)
    {
        double paperW = spec.PaperW, paperH = spec.PaperH;
        double k = spec.ScaleK <= 0 ? 1.0 : spec.ScaleK;   // 图/真 比例 (放大/缩小)
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

        // 真值几何透镜 (含逐面净口径 + 公差) → 共享视图装配按 k 缩放绘制 + 真值标注/镀膜/粗糙度.
        var realLens = new OpticalLens
        {
            Diameter = spec.Diameter,
            MechanicalDiameter = spec.MechanicalDiameter,
            Thickness = spec.Thickness,
            R1 = spec.R1, R2 = spec.R2,
            MaterialName = spec.MaterialName, RefractiveIndex = spec.Nd, AbbeNumber = spec.Vd,
            DiameterTolerance = new ToleranceValue(spec.Diameter, spec.DiameterTol, spec.DiameterTol),
            ThicknessTolerance = new ToleranceValue(spec.Thickness, spec.ThicknessTol, spec.ThicknessTol),
        };
        if (spec.FrontSemiAperture > 1e-9) realLens.FrontSurface.SemiAperture = spec.FrontSemiAperture;
        if (spec.BackSemiAperture > 1e-9) realLens.BackSurface.SemiAperture = spec.BackSemiAperture;
        var lens = AppendScaledLensView(entities, realLens, cx, cy, k);
        // 第一角投影符号现由图框 (IsoLensDrawingFrame) 自绘, 归图框层

        // ISO 10110-12 非球面数据块 (有非球面才产出; 真值 R/K/系数, 落绘图区左上). 真口径半径用真值.
        double realHalf = spec.Diameter * 0.5;
        var blkCursor = new Vector2(border + 2.0, areaTop - 2.0);
        EmitAsphericBlock(entities, spec.FrontSurfaceModel, realHalf, "1", ref blkCursor, lens);
        EmitAsphericBlock(entities, spec.BackSurfaceModel, realHalf, "2", ref blkCursor, lens);

        return entities;
    }

    /// <summary>
    /// 共享剖面视图装配 (ISO/GB 两路径共用): 把真值几何透镜按 k 缩放绘制 (填充),
    /// 居中于 (cx,cy), 加 真值黑标注 (中心厚/边厚/Ø/R) + 表面粗糙度 + AR 镀膜. 返回缩放后的显示透镜 (供加挂额外标记)。
    /// </summary>
    internal static OpticalLens AppendScaledLensView(List<Entity> dst, OpticalLens real, double cx, double cy, double k)
    {
        double D = real.Diameter, T = real.Thickness;
        double diaTolV = real.DiameterTolerance?.Plus ?? 0;
        double ctTolV = real.ThicknessTolerance?.Plus ?? 0;
        double frontSemi = real.FrontSurface.SemiAperture, backSemi = real.BackSurface.SemiAperture;

        var lens = new OpticalLens
        {
            Position = new Vector2(cx, cy),
            Diameter = D * k,
            MechanicalDiameter = real.MechanicalDiameter.HasValue ? real.MechanicalDiameter.Value * k : (double?)null,
            Thickness = T * k,
            R1 = ScaleR(real.R1, k), R2 = ScaleR(real.R2, k),
            MaterialName = real.MaterialName, RefractiveIndex = real.RefractiveIndex, AbbeNumber = real.AbbeNumber,
            FillColor = GlassFill, AxisPadding = AxisPaddingSheet,
        };
        if (frontSemi > 1e-9) lens.FrontSurface.SemiAperture = frontSemi * k;
        if (backSemi > 1e-9) lens.BackSurface.SemiAperture = backSemi * k;
        dst.Add(lens);

        double halfT = T * 0.5 * k, halfD = D * 0.5 * k;
        double frontApexX = cx - halfT, backApexX = cx + halfT;
        double frontEdgeX = frontApexX + lens.FrontSurface.Sag(halfD);
        double backEdgeX = backApexX + lens.BackSurface.Sag(halfD);
        var style = IsoDimStyle();
        string diaTol = diaTolV > 0 ? $"±{diaTolV:F3}" : "";
        string ctTol = ctTolV > 0 ? $"±{ctTolV:F3}" : "";
        double edgeReal = (backEdgeX - frontEdgeX) / k;

        AddLinearDim(dst, lens, style, new Vector2(frontApexX, cy), new Vector2(backApexX, cy),
            new Vector2(cx, cy - halfD - 12), 0.0, $"{T:F2}{ctTol}", anchorA: 1, anchorB: 2);   // CT: 前顶↔后顶
        AddLinearDim(dst, lens, style, new Vector2(frontEdgeX, cy + halfD), new Vector2(backEdgeX, cy + halfD),
            new Vector2(cx, cy + halfD + 10), 0.0, $"({edgeReal:F2})", anchorA: 5, anchorB: 7);   // 边厚: 前上角↔后上角
        AddLinearDim(dst, lens, style, new Vector2(backEdgeX, cy - halfD), new Vector2(backEdgeX, cy + halfD),
            new Vector2(backApexX + 22, cy), -System.Math.PI / 2, $"Ø{D:F2}{diaTol}", anchorA: 8, anchorB: 7);   // Ø: 后下角↔后上角(端点落在弯月后表面边缘)
        SurfaceProbe pFront = () => { double hT = lens.Thickness * 0.5; return (lens.Position.X - hT, lens.Position.Y, lens.Diameter * 0.5, lens.FrontSurface); };
        SurfaceProbe pBack = () => { double hT = lens.Thickness * 0.5; return (lens.Position.X + hT, lens.Position.Y, lens.Diameter * 0.5, lens.BackSurface); };
        AddRadialDim(dst, lens, real.R1, front: true, style, pFront);
        AddRadialDim(dst, lens, real.R2, front: false, style, pBack);

        double axisLeft = frontApexX - lens.AxisPadding;
        dst.Add(new MText { Position = new Vector2(axisLeft + 1.0, cy + 1.5), Value = "opt. axis", Height = 2.2, Owner = lens });

        AddSurfaceFinishMark(dst, lens, glassOnPlusX: true, hFrac: 0.62, size: 5.0, pFront);
        AddSurfaceFinishMark(dst, lens, glassOnPlusX: false, hFrac: 0.45, size: 5.0, pBack);
        lcdb.Annotation.CoatingMark? frontCoat = null, backCoat = null;
        AutoDimensionLensCmd.EmitCoatingMark(frontApexX, cy, halfD, lens.R1, glassOnPlusX: true,
            e => { e.Owner = lens; if (e is lcdb.Annotation.CoatingMark cm) frontCoat = cm; dst.Add(e); });
        AutoDimensionLensCmd.EmitCoatingMark(backApexX, cy, halfD, lens.R2, glassOnPlusX: false,
            e => { e.Owner = lens; if (e is lcdb.Annotation.CoatingMark cm) backCoat = cm; dst.Add(e); });
        if (frontCoat != null) SetCoatingFollow(frontCoat, glassOnPlusX: true, pFront);
        if (backCoat != null) SetCoatingFollow(backCoat, glassOnPlusX: false, pBack);
        return lens;
    }

    /// <summary>
    /// 共享双胶合剖面视图 (ISO/GB 共用): 克隆真值双胶合按 k 缩放(双色填充, 居中) +
    /// 真值黑标注(总厚 CT/边厚/Ø/前胶后三 R) + 表面粗糙度 + 前后外膜. 返回缩放后的显示件。
    /// </summary>
    internal static CementedLens AppendScaledDoubletView(List<Entity> dst, CementedLens src, double cx, double cy, double k)
    {
        double dia = src.Diameter, diaTolV = src.DiameterTolerance?.Plus ?? 0;

        var lens = (CementedLens)src.Clone();
        ScaleSurface(lens.FrontSurface, k);
        ScaleSurface(lens.ContactSurface, k);
        ScaleSurface(lens.BackSurface, k);
        lens.Diameter = dia * k;
        lens.T1 = src.T1 * k;
        lens.T2 = src.T2 * k;
        if (lens.MechanicalDiameter.HasValue) lens.MechanicalDiameter = lens.MechanicalDiameter.Value * k;
        lens.Position = new Vector2(cx, cy);
        lens.AxisPadding = AxisPaddingSheet;
        lens.FillColor1 = GlassFill;
        lens.FillColor2 = GlassFill2;
        dst.Add(lens);

        double halfD = dia * 0.5 * k;
        double halfT = (src.T1 + src.T2) * 0.5 * k;
        double frontApexX = cx - halfT, backApexX = cx + halfT;
        double contactX = frontApexX + src.T1 * k;
        double frontEdgeX = frontApexX + lens.FrontSurface.Sag(halfD);
        double backEdgeX = backApexX + lens.BackSurface.Sag(halfD);
        var style = IsoDimStyle();
        string diaTol = diaTolV > 0 ? $"±{diaTolV:F3}" : "";
        double ctReal = src.T1 + src.T2;
        double edgeReal = (backEdgeX - frontEdgeX) / k;

        // 双胶合捕捉点序 (CementedLens.GetSnapPoints): 0中心/1前顶/2后顶/3胶合顶/4口径上/5口径下/6前上角/7前下角/8后上角/9后下角。
        AddLinearDim(dst, lens, style, new Vector2(frontApexX, cy), new Vector2(backApexX, cy),
            new Vector2(cx, cy - halfD - 12), 0.0, $"CT {ctReal:F2}{diaTol}", anchorA: 1, anchorB: 2);   // CT: 前顶↔后顶
        AddLinearDim(dst, lens, style, new Vector2(frontEdgeX, cy + halfD), new Vector2(backEdgeX, cy + halfD),
            new Vector2(cx, cy + halfD + 10), 0.0, $"({edgeReal:F2})", anchorA: 6, anchorB: 8);   // 边厚: 前上角↔后上角
        AddLinearDim(dst, lens, style, new Vector2(backEdgeX, cy - halfD), new Vector2(backEdgeX, cy + halfD),
            new Vector2(backApexX + 22, cy), -System.Math.PI / 2, $"Ø{dia:F2}{diaTol}", anchorA: 9, anchorB: 8);   // Ø: 后下角↔后上角(贴面)
        SurfaceProbe pF = () => { double hT = (lens.T1 + lens.T2) * 0.5; return (lens.Position.X - hT, lens.Position.Y, lens.Diameter * 0.5, lens.FrontSurface); };
        SurfaceProbe pC = () => { double hT = (lens.T1 + lens.T2) * 0.5; return (lens.Position.X - hT + lens.T1, lens.Position.Y, lens.Diameter * 0.5, lens.ContactSurface); };
        SurfaceProbe pB = () => { double hT = (lens.T1 + lens.T2) * 0.5; return (lens.Position.X + hT, lens.Position.Y, lens.Diameter * 0.5, lens.BackSurface); };
        AddRadialDim(dst, lens, src.R1, front: true, style, pF);
        AddRadialDim(dst, lens, src.RContact, front: true, style, pC);
        AddRadialDim(dst, lens, src.R3, front: false, style, pB);

        dst.Add(new MText { Position = new Vector2(frontApexX - lens.AxisPadding + 1.0, cy + 1.5), Value = "opt. axis", Height = 2.2, Owner = lens });
        AddSurfaceFinishMark(dst, lens, glassOnPlusX: true, hFrac: 0.62, size: 5.0, pF);
        AddSurfaceFinishMark(dst, lens, glassOnPlusX: false, hFrac: 0.45, size: 5.0, pB);
        lcdb.Annotation.CoatingMark? fCoat = null, bCoat = null;
        AutoDimensionLensCmd.EmitCoatingMark(frontApexX, cy, halfD, lens.R1, glassOnPlusX: true,
            e => { e.Owner = lens; if (e is lcdb.Annotation.CoatingMark cm) fCoat = cm; dst.Add(e); });
        AutoDimensionLensCmd.EmitCoatingMark(backApexX, cy, halfD, lens.R3, glassOnPlusX: false,
            e => { e.Owner = lens; if (e is lcdb.Annotation.CoatingMark cm) bCoat = cm; dst.Add(e); });
        if (fCoat != null) SetCoatingFollow(fCoat, glassOnPlusX: true, pF);
        if (bCoat != null) SetCoatingFollow(bCoat, glassOnPlusX: false, pB);
        return lens;
    }

    /// <summary>面是非球面则产出一个 ISO 10110-12 数据块, 落在 cursor 处并把 cursor 下移 (供堆叠下一个)。</summary>
    internal static void EmitAsphericBlock(List<Entity> dst, OpticalSurface? surf, double semiApertureReal,
        string label, ref Vector2 cursor, Entity owner)
    {
        if (surf is not AsphericSurface a) return;
        if (double.IsInfinity(a.Radius) || System.Math.Abs(a.Radius) > 1e6) return;
        var blk = lcdb.Annotation.AsphericDataBlock.FromSurface(a, semiApertureReal, label, cursor);
        blk.Owner = owner;
        dst.Add(blk);
        cursor = new Vector2(cursor.X, cursor.Y - blk.bounding.height - 6.0);
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
        var entities = new List<Entity>();

        double dia = src.Diameter;

        var cols = new List<IsoSpecColumn>
        {
            new() { Title = "SURFACE 1 (FRONT)", Rows = CementedSurfaceRows(src.R1, frontElementFront: true, dia, src.MechanicalDiameter) },
            new() { Title = "SURFACE 2 (CEMENT)", Rows = CementContactRows(src.RContact, dia, src.MechanicalDiameter) },
            new() { Title = "SURFACE 3 (REAR)",  Rows = CementedSurfaceRows(src.R3, frontElementFront: false, dia, src.MechanicalDiameter) },
            new() { Title = "MATERIALS",          Rows = CementedMaterialRows(src) },
        };
        int rowCount = 0;
        foreach (var col in cols) rowCount = Math.Max(rowCount, col.Rows.Count);

        // 自动选纸 + scale-to-fit (覆盖传入 scale 标签)
        var layout = OtoCAD.Avalonia.Templating.LayoutEngine.Compute(dia, rowCount);
        double paperW = layout.PaperW, paperH = layout.PaperH, k = layout.Scale;

        var frame = new IsoLensDrawingFrame
        {
            Origin = new Vector2(0, 0),
            PaperWidth = paperW,
            PaperHeight = paperH,
            Columns = cols,
            ProjectPart = projectPart,
            DrawnBy = drawnBy,
            ApprovedBy = "—",
            DrawDate = DateTime.Today.ToString("yyyy-MM-dd"),
            DrawScale = layout.ScaleLabel,
            Sheet = "1 / 1",
            Rev = "A",
            Notes = notes,
        };
        entities.Add(frame);

        // 布局: 绘图区中心
        double border = frame.BorderMargin;
        double blockH = frame.NotesRowHeight + frame.TitleRowHeight + (rowCount + 1) * frame.TableRowHeight;
        double cx = paperW * 0.5;
        double cy = (border + blockH + (paperH - border)) * 0.5;

        // 共享双胶合剖面视图: 缩放剖面(双色填充) + 真值黑标注(CT/边厚/Ø/R) + 镀膜 + 粗糙度.
        var lens = AppendScaledDoubletView(entities, src, cx, cy, k);
        // 第一角投影符号现由图框 (IsoLensDrawingFrame) 自绘, 归图框层

        // ISO 10110-12 非球面数据块 (三面各检, 用 src 真值面). 落绘图区左上, 向下堆叠.
        double realHalfD = dia * 0.5;
        double areaTop = paperH - border;
        var blkCursor = new Vector2(border + 2.0, areaTop - 2.0);
        EmitAsphericBlock(entities, src.FrontSurface,   realHalfD, "1", ref blkCursor, lens);
        EmitAsphericBlock(entities, src.ContactSurface, realHalfD, "2", ref blkCursor, lens);
        EmitAsphericBlock(entities, src.BackSurface,    realHalfD, "3", ref blkCursor, lens);

        return entities;
    }

    /// <summary>双胶合外表面 (前/后) 的 ISO 行. frontElementFront=true → 前表面 (CX⇔R>0).</summary>
    private static List<string> CementedSurfaceRows(double r, bool frontElementFront, double dia, double? mechDia)
    {
        string rRow;
        if (double.IsInfinity(r) || Math.Abs(r) > 1e6)
            rRow = "R = ∞";
        else
        {
            bool convex = frontElementFront ? r > 0 : r < 0;
            rRow = $"R = {(r > 0 ? "+" : "")}{r:F3} {(convex ? "CX" : "CC")}";
        }
        var rows = new List<string> { rRow };
        if (mechDia.HasValue && mechDia.Value > dia + 1e-6) rows.Add($"Øe {dia:F2}");  // Øe == 机械口径时不标
        rows.AddRange(new[]
        {
            "Chamfer 0.3-0.8 × 45°",
            "3/ (1.0)",
            "4/ 0.5'",
            "5/ 2×0.063",
            "7/ AR 400-700 nm, R<0.5%",
        });
        return rows;
    }

    /// <summary>胶合面 (内表面) 的 ISO 行 — 规格较少.</summary>
    private static List<string> CementContactRows(double r, double dia, double? mechDia)
    {
        bool convex = r < 0;   // 作为片1后表面: R<0 凸
        string rRow = (double.IsInfinity(r) || Math.Abs(r) > 1e6)
            ? "R = ∞"
            : $"R = {(r > 0 ? "+" : "")}{r:F3} {(convex ? "CX" : "CC")}";
        var rows = new List<string> { rRow };
        if (mechDia.HasValue && mechDia.Value > dia + 1e-6) rows.Add($"Øe {dia:F2}");  // Øe == 机械口径时不标
        rows.AddRange(new[] { "(cemented)", "3/ (1.0)", "4/ 0.5'" });
        return rows;
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
    /// <summary>表面几何探针: 返回锚定对象当前 (顶点X, 轴Y, 半口径, 表面)。单透镜/双胶合各自捕获自身字段。</summary>
    private delegate (double apexX, double cy, double halfD, OpticalSurface surf) SurfaceProbe();

    /// <summary>
    /// 在表面半口径 hFrac 处放真·粗糙度标记 (∇), 贴面相切; 并装关联刷新 —
    /// 锚定对象(透镜)改厚度/口径/半径/平移后, 由 probe 重取表面几何, ∇ 跟随重贴面。
    /// 用户拖动沿面滑后保持高度 (AttachState.Height)。
    /// </summary>
    private static void AddSurfaceFinishMark(
        List<Entity> dst, Entity owner, bool glassOnPlusX, double hFrac, double size, SurfaceProbe probe)
    {
        var mark = new lcdb.Annotation.SurfaceRoughnessMark(Vector2.Zero, size) { ShowText = true, Owner = owner };
        mark.AttachState = new lcdb.Annotation.SurfaceAttachState(Vector2.Zero, 0, glassOnPlusX);
        bool firstRun = true;
        double lAx = double.NaN, lCy = 0, lR = 0, lH = 0;
        bool Follow()
        {
            var (ax, cy, hd, surf) = probe();
            double hh = firstRun ? hd * hFrac : mark.AttachState!.Height;   // 首帧用默认半口径分数, 之后保持(拖动)高度
            if (!firstRun && ax == lAx && cy == lCy && surf.Radius == lR && hh == lH) return false;
            firstRun = false; lAx = ax; lCy = cy; lR = surf.Radius; lH = hh;
            mark.AttachState!.Apex = new Vector2(ax, cy);
            mark.AttachState.Radius = surf.Radius;
            var pt = new Vector2(ax + surf.Sag(hh), cy + hh);
            const double d = 0.05;
            double slope = (surf.Sag(hh + d) - surf.Sag(hh - d)) / (2 * d);   // dSag/dh
            var nPlusX = new Vector2(1, -slope).normalized;
            mark.AttachToSurface(pt, glassOnPlusX ? -nPlusX : nPlusX);        // 玻璃在+X侧 → 外法线朝-X
            return true;
        }
        Follow();
        mark.AssociativeRefresh = Follow;
        dst.Add(mark);
    }

    /// <summary>给镀膜 ⊕ 标记装关联刷新: 锚定对象改厚度/口径/半径/平移后 ⊕ 跟随重贴面 (下半弧)。</summary>
    private static void SetCoatingFollow(lcdb.Annotation.CoatingMark m, bool glassOnPlusX, SurfaceProbe probe)
    {
        bool firstRun = true;
        double lAx = double.NaN, lCy = 0, lR = 0, lH = 0;
        bool Follow()
        {
            var (ax, cy, hd, surf) = probe();
            double hh = firstRun ? -hd * 0.5 : (m.AttachState?.Height ?? -hd * 0.5);   // 下半弧
            if (!firstRun && ax == lAx && cy == lCy && surf.Radius == lR && hh == lH) return false;
            firstRun = false; lAx = ax; lCy = cy; lR = surf.Radius; lH = hh;
            if (m.AttachState != null) { m.AttachState.Apex = new Vector2(ax, cy); m.AttachState.Radius = surf.Radius; }
            double X(double y) => ax + lcdb.Optic.OpticSurfaceGeometry.ComputeSag(surf.Radius, System.Math.Abs(y));
            var pt = new Vector2(X(hh), cy + hh);
            const double d = 0.05;
            double slope = (X(hh + d) - X(hh - d)) / (2 * d);
            var nPlusX = new Vector2(1, -slope).normalized;
            m.AttachToSurface(pt, glassOnPlusX ? -nPlusX : nPlusX);
            return true;
        }
        Follow();
        m.AssociativeRefresh = Follow;
    }

    /// <summary>
    /// 在表面上加一条 R 径向标注引线 — R 既入三列表又上图(用户要求两者都要)。
    /// 平面(∞)不画引线(表里已写 R=∞)。锚点取半口径中段, 弧上任意点到球心皆 |R|, measurement 自动 = |R|。
    /// 用 ISO 图纸样式(黑色), Owner=透镜(随关联刷新/清单 owned-count 一并管理)。
    /// </summary>
    private static void AddRadialDim(List<Entity> dst, Entity owner, double rReal, bool front,
        lcdb.DimensionStyle style, SurfaceProbe probe)
    {
        if (double.IsNaN(rReal) || double.IsInfinity(rReal) || System.Math.Abs(rReal) > 1e6) return;
        var (apexX, cy, halfD, surf) = probe();
        // 几何用缩放半径 (球心在缩放轴上, = surf.Radius 已缩放), 文字用真值 R.
        var center = new Vector2(apexX + surf.Radius, cy);
        double anchorH = halfD * 0.5;
        var rim = new Vector2(apexX + surf.Sag(anchorH), cy + anchorH);
        double leaderArm = System.Math.Max(2.0, style.TextHeight * 1.5);
        var rd = new RadialDimension(center, rim, leaderArm)
        {
            style = style.Clone(),
            userText = $"R{rReal:F2}",
            color = lcdb.Colors.Color.FromRGB(0, 0, 0),
            Owner = owner,
        };
        var dir = (rim - center).normalized;
        var leaderEnd = rim + dir * leaderArm;
        double kick = halfD * 0.35;
        rd.textReferencePoint = new Vector2(leaderEnd.X + (front ? -kick : kick), leaderEnd.Y);

        // 关联: 球心/弧锚随表面重derive (改厚度→顶点移, 改半径→球心移, 改口径→弧锚移)。
        // 文字引线随弧锚刚性平移, 保留用户对引线的相对调整。
        {
            double lAx = double.NaN, lCy = 0, lR = 0;
            rd.AssociativeRefresh = () =>
            {
                var (ax, cyy, hd, s) = probe();
                if (double.IsInfinity(s.Radius) || System.Math.Abs(s.Radius) > 1e6) return false;   // 平面无球心
                if (ax == lAx && cyy == lCy && s.Radius == lR) return false;
                lAx = ax; lCy = cyy; lR = s.Radius;
                double hh = hd * 0.5;
                var newRim = new Vector2(ax + s.Sag(hh), cyy + hh);
                var delta = newRim - rd.chordPoint;
                rd.centerPoint = new Vector2(ax + s.Radius, cyy);
                rd.chordPoint = newRim;
                rd.textReferencePoint += delta;
                rd.Update();
                return true;
            };
        }
        dst.Add(rd);
    }

    /// <summary>半径按比例缩放 (∞/极大值保持平面).</summary>
    private static double ScaleR(double r, double k)
        => (double.IsInfinity(r) || System.Math.Abs(r) > 1e6) ? r : r * k;

    /// <summary>把一个表面按 k 缩放 (半径 + 净口径; 平面保持平面). 球面/非球面共用 (锥常数不缩放).</summary>
    private static void ScaleSurface(OpticalSurface s, double k)
    {
        if (!double.IsInfinity(s.Radius) && System.Math.Abs(s.Radius) <= 1e6) s.Radius *= k;
        s.SemiAperture *= k;
    }

    /// <summary>按零件直径 + 规格行数算版面 (纸张/比例), 写回 Spec.</summary>
    public static void ApplyLayout(Spec s)
    {
        int rows = Math.Max(Math.Max(s.Surface1Rows.Count, s.Surface2Rows.Count), s.MaterialRows.Count);
        var l = OtoCAD.Avalonia.Templating.LayoutEngine.Compute(s.Diameter, rows);
        s.PaperW = l.PaperW;
        s.PaperH = l.PaperH;
        s.ScaleK = l.Scale;
        s.DrawScale = l.ScaleLabel;
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
        Vector2 a, Vector2 b, Vector2 dimLinePos, double rot, string userText,
        int anchorA = -1, int anchorB = -1)
    {
        var mid = (a + b) * 0.5;
        var dir = new Vector2(System.Math.Cos(rot), System.Math.Sin(rot));
        var perp = Vector2.Perpendicular(dir);
        double off = Vector2.Dot(dimLinePos - mid, perp);
        if (off < 0) { rot += System.Math.PI; off = -off; }
        var dim = new LinearDimension(a, b, off, rot, style.Clone()) { userText = userText, Owner = owner };
        // 关联锚点: 端点绑到宿主透镜的捕捉点索引 → 透镜改尺寸后端点跟随 (GetSnapPoints 序: 0中心/1前顶/2后顶/3口径上/4口径下/5前上角/7后上角)
        if (anchorA >= 0) dim.FirstAnchor = new lcdb.DimAnchor(owner, anchorA);
        if (anchorB >= 0) dim.SecondAnchor = new lcdb.DimAnchor(owner, anchorB);
        dst.Add(dim);
    }

    /// <summary>
    /// 共享棱镜视图装配 (ISO/GB 两路径共用): 把真值棱镜按 k 缩放绘制, 居中于 (cx,cy),
    /// 加真值黑标注 (底边 / 竖边 / 直角). 返回缩放后的显示棱镜 (供加挂额外标记)。
    ///
    /// ⚠️ 只对 <see cref="PrismType.RightAngle"/> 成立: 其余类型在 lcdb.Prism 里仍 fallback 成
    /// 同一个三角形占位 (五角棱镜会画成三角形), 照图加工即废件 — 由 PrismPartAdapter.CanHandle 拦截。
    ///
    /// 零件图按标准姿态画, 故显示件 Rotation 归零 (装配姿态不属于零件图)。
    /// </summary>
    internal static Prism AppendScaledPrismView(List<Entity> dst, Prism real, double cx, double cy, double k)
    {
        double W = real.Width, H = real.Height;
        double wTolV = real.WidthTolerance?.Plus ?? 0;
        double hTolV = real.HeightTolerance?.Plus ?? 0;

        var prism = new Prism
        {
            Position = new Vector2(cx, cy),
            Width = W * k,
            Height = H * k,
            Type = real.Type,
            Rotation = 0,
            MaterialName = real.MaterialName,
            RefractiveIndex = real.RefractiveIndex,
            AbbeNumber = real.AbbeNumber,
            FillColor = GlassFill,
        };
        dst.Add(prism);

        // 与 lcdb.Prism.Generate 同一几何约定: A=直角顶(左下) B=底边右端 C=竖边上端
        double halfW = W * 0.5 * k, halfH = H * 0.5 * k;
        var A = new Vector2(cx - halfW, cy - halfH);
        var B = new Vector2(cx + halfW, cy - halfH);
        var C = new Vector2(cx - halfW, cy + halfH);
        var style = IsoDimStyle();

        string wTol = wTolV > 0 ? $"±{wTolV:F3}" : "";
        string hTol = hTolV > 0 ? $"±{hTolV:F3}" : "";

        // 底边 A→B (尺寸线在下) / 竖边 A→C (尺寸线在左). 锚点索引见 Prism.GetSnapPoints: 1=A 2=B 3=C
        AddLinearDim(dst, prism, style, A, B, new Vector2(cx, cy - halfH - 12), 0.0,
            $"{W:F2}{wTol}", anchorA: 1, anchorB: 2);
        AddLinearDim(dst, prism, style, A, C, new Vector2(cx - halfW - 14, cy), -System.Math.PI / 2,
            $"{H:F2}{hTol}", anchorA: 1, anchorB: 3);

        // 直角标注 (底边 ∠ 竖边) — 直角棱镜的定形角. 角度值由几何真算 (90°), 不写死文本;
        // 直角偏差公差不在 Prism 实体上, 走属性区表格行由工程师填 (见 GbSheetScene.BuildPrismBag)。
        var arcPt = A + new Vector2(halfW * 0.30, halfH * 0.30);
        dst.Add(new Angular2LineDimension(A, B, A, C, arcPt)
        {
            style = style.Clone(),
            Owner = prism,
        });

        return prism;
    }
}
