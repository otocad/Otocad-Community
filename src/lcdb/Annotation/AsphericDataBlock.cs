using System;
using System.Collections.Generic;
using System.Globalization;
using LitMath;
using lcdb.Optic;
using OtoCAD;

namespace lcdb.Annotation;

/// <summary>
/// 非球面数据块 (ISO 10110-12) — 一个可放置的图面数据块实体, 复现标准非球面图面要素:
///   ① 表面指引标号 "n——非球面"   ② even-asphere 矢高方程 z(h) (堆叠分式 + 根号排版)
///   ③ 矢高表 (h | z | Δz | 斜率公差)   ④ 系数块 (R / K / A4..A10 / 斜率取样长度 / 取样步长)
///
/// 对标 docs/需求/图面样例-非球面透镜1.png。方程形态是 ISO 固定式 (只有系数变), 故方程作为
/// 定式排版; 矢高表 z 列由 <see cref="AsphericSurface.Sag"/> 真算; Δz/斜率公差为设计公差 (用户可填)。
///
/// 是独立图面块 (落在 NOTES 区), 不是面属性区表格行 —— 样例明确把它画在图框外。
/// 缓存生成实体, 变换时清缓存 (同 <see cref="CoatingMark"/> 等标记)。
/// </summary>
public sealed class AsphericDataBlock : Entity
{
    public override string className => "AsphericDataBlock";

    /// <summary>块左上锚点 (模型坐标, 块向右下生长)。</summary>
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>图上引线指向该非球面的标号 (样例为 "1")。</summary>
    public string SurfaceLabel { get; set; } = "1";

    /// <summary>基础曲率半径 R (顶点球面半径)。</summary>
    public double BaseRadius { get; set; } = 50.0;

    /// <summary>圆锥常数 K。</summary>
    public double ConicConstant { get; set; } = 0.0;

    /// <summary>偶次项系数 [A4, A6, A8, A10, ...]。</summary>
    public double[] EvenCoefficients { get; set; } = Array.Empty<double>();

    /// <summary>矢高表的 h 取样半径行 (mm)。</summary>
    public double[] SampleHeights { get; set; } = { 0.0, 5.0, 10.0, 15.0, 20.0 };

    /// <summary>矢高表 Δz (矢高公差) 列, 与 <see cref="SampleHeights"/> 同序; 短缺行留空。设计公差, 非计算值。</summary>
    public double[] SagTolerances { get; set; } = Array.Empty<double>();

    /// <summary>矢高表 斜率公差 列 (如 "0.3'"), 与 <see cref="SampleHeights"/> 同序; 短缺行留空。</summary>
    public string[] SlopeTolerances { get; set; } = Array.Empty<string>();

    /// <summary>斜率取样长度 (mm) — 系数块项。</summary>
    public double SlopeSampleLength { get; set; } = 1.0;

    /// <summary>取样步长 (mm) — 系数块项。</summary>
    public double SlopeSampleStep { get; set; } = 0.1;

    /// <summary>基准字高 (mm); 全块尺寸按它缩放。</summary>
    public double TextHeight { get; set; } = 2.5;

    private readonly List<Entity> _entities = new();

    public AsphericDataBlock() { }

    /// <summary>从一个非球面构造数据块 (半口径 → 末行取样半径; 系数/R/K 取自面)。其余公差留空待填。</summary>
    public static AsphericDataBlock FromSurface(AsphericSurface surf, double semiAperture, string label, Vector2 position)
    {
        double h = semiAperture > 0 ? semiAperture : Math.Abs(surf.SemiAperture);
        if (h <= 0) h = 10.0;
        // 取样 0, 1/4, 1/2, 3/4, 满口径 (整到 0.5)
        var heights = new[] { 0.0, Round(h * 0.25), Round(h * 0.5), Round(h * 0.75), Round(h) };
        return new AsphericDataBlock
        {
            Position = position,
            SurfaceLabel = label,
            BaseRadius = surf.Radius,
            ConicConstant = surf.ConicConstant,
            EvenCoefficients = (double[])surf.EvenCoefficients.Clone(),
            SampleHeights = heights,
        };
        static double Round(double v) => Math.Round(v * 2.0) / 2.0;
    }

    // -------- 生成 --------

    private static readonly lcdb.Colors.Color Ink = lcdb.Colors.Color.FromRGB(0, 0, 0);

    /// <summary>估算文字宽度 (与 MText 内部估算一致: 字数 × 字高 × 0.6)。</summary>
    private static double EstW(string s, double h) => (s ?? "").Length * h * 0.6;

    private void EnsureGenerated()
    {
        if (_entities.Count > 0) return;
        Generate();
    }

    private void Generate()
    {
        _entities.Clear();
        double em = TextHeight <= 0 ? 2.5 : TextHeight;
        double x0 = Position.X;
        double y = Position.Y;

        // ① 标号 "1——非球面"
        AddText($"{SurfaceLabel}——非球面", x0, y - em * 0.5, em, MTextAttachmentPoint.MiddleLeft);
        y -= em * 2.0;

        // ② 方程 z(h) = h²/(R(1+√(1−(1+k)h²/R²))) + Σ(i=2..5) (A₂ᵢ·h²ⁱ)
        double yBar = y - em * 1.3;          // 分式横线 Y (留出分子空间)
        BuildEquation(x0, yBar, em);
        // 第二行 h = √(x²+y²)
        double yH = yBar - em * 2.8;
        BuildRadicalExpr("h = ", "x²+y²", x0, yH, em, out _);
        y = yH - em * 2.2;

        // ③ 矢高表 + ④ 系数块 (并排)
        double tableRight = BuildSagTable(x0, y, em);
        BuildCoeffBlock(tableRight + em * 2.0, y - em * 0.2, em);
    }

    /// <summary>方程主体 (堆叠分式 + 根号 + 求和)。</summary>
    private void BuildEquation(double x0, double yBar, double em)
    {
        double x = x0;
        AddText("z = ", x, yBar, em, MTextAttachmentPoint.MiddleLeft);
        x += EstW("z = ", em);

        // 分式: 分子 h² / 分母 R(1 + √(...))
        const string num = "h²";
        // 分母分段 (左→右), 其中根式段带上划线
        const string denPre = "R(1 + ";
        const string radSign = "√";
        const string radicand = "1 − (1+k)h²/R²";
        const string denPost = ")";
        double denW = EstW(denPre, em) + EstW(radSign, em) + EstW(radicand, em) + EstW(denPost, em);
        double barW = Math.Max(EstW(num, em), denW);

        double barX0 = x, barX1 = x + barW;
        AddLine(barX0, yBar, barX1, yBar);                                   // 分式横线
        AddText(num, (barX0 + barX1) * 0.5, yBar + em * 0.95, em, MTextAttachmentPoint.MiddleCenter);  // 分子居中

        // 分母居中铺在横线下
        double dx = barX0 + (barW - denW) * 0.5;
        double yDen = yBar - em * 0.95;
        AddText(denPre, dx, yDen, em, MTextAttachmentPoint.MiddleLeft); dx += EstW(denPre, em);
        AddText(radSign, dx, yDen, em, MTextAttachmentPoint.MiddleLeft); dx += EstW(radSign, em);
        double radX0 = dx;
        AddText(radicand, dx, yDen, em, MTextAttachmentPoint.MiddleLeft); dx += EstW(radicand, em);
        AddLine(radX0, yDen + em * 0.62, dx, yDen + em * 0.62);             // 根号上划线
        AddText(denPost, dx, yDen, em, MTextAttachmentPoint.MiddleLeft);

        x = barX1 + em * 0.5;
        AddText("+", x, yBar, em, MTextAttachmentPoint.MiddleLeft);
        x += EstW("+ ", em);

        // 求和 Σ (上限 5 / 下限 i=2) + 项 (A₂ᵢ·h²ⁱ)
        double sigmaH = em * 1.6;
        double sigmaW = EstW("Σ", sigmaH);
        AddText("Σ", x, yBar, sigmaH, MTextAttachmentPoint.MiddleLeft);
        double xSumC = x + sigmaW * 0.5;
        AddText("5", xSumC, yBar + em * 1.05, em * 0.55, MTextAttachmentPoint.MiddleCenter);
        AddText("i=2", xSumC, yBar - em * 1.05, em * 0.55, MTextAttachmentPoint.MiddleCenter);
        x += sigmaW + em * 0.3;
        // 项 (A₂ᵢ·h²ⁱ): "2i" 下标/上标用小号升降文字 (避罕见 Unicode 上下标字形在 Arial 缺字)。
        double sub = em * 0.6;
        AddText("(A", x, yBar, em, MTextAttachmentPoint.MiddleLeft); x += EstW("(A", em);
        AddText("2i", x, yBar - em * 0.35, sub, MTextAttachmentPoint.MiddleLeft); x += EstW("2i", sub);
        AddText("·h", x, yBar, em, MTextAttachmentPoint.MiddleLeft); x += EstW("·h", em);
        AddText("2i", x, yBar + em * 0.45, sub, MTextAttachmentPoint.MiddleLeft); x += EstW("2i", sub);
        AddText(")", x, yBar, em, MTextAttachmentPoint.MiddleLeft);
    }

    /// <summary>"prefix √‾radicand" 形式 (带上划线根号)。out 返回末端 X。</summary>
    private void BuildRadicalExpr(string prefix, string radicand, double x0, double y, double em, out double endX)
    {
        double x = x0;
        AddText(prefix, x, y, em, MTextAttachmentPoint.MiddleLeft); x += EstW(prefix, em);
        AddText("√", x, y, em, MTextAttachmentPoint.MiddleLeft); x += EstW("√", em);
        double radX0 = x;
        AddText(radicand, x, y, em, MTextAttachmentPoint.MiddleLeft); x += EstW(radicand, em);
        AddLine(radX0, y + em * 0.62, x, y + em * 0.62);
        endX = x;
    }

    /// <summary>矢高表 (h | z | Δz | 斜率公差)。返回表右边界 X。</summary>
    private double BuildSagTable(double x0, double yTop, double em)
    {
        string[] headers = { "h", "z", "Δz", "斜率公差" };
        double[] colW = { em * 5.0, em * 6.0, em * 6.0, em * 7.5 };
        double rowH = em * 1.9;
        int rows = (SampleHeights?.Length ?? 0) + 1;     // +表头
        double tableW = 0; foreach (var w in colW) tableW += w;
        double yBottom = yTop - rows * rowH;

        // 外框 + 列竖线 + 表头横线 + 行横线
        AddLine(x0, yTop, x0 + tableW, yTop);
        AddLine(x0, yBottom, x0 + tableW, yBottom);
        double cx = x0;
        for (int c = 0; c <= colW.Length; c++)
        {
            AddLine(cx, yTop, cx, yBottom);
            if (c < colW.Length) cx += colW[c];
        }
        for (int r = 1; r < rows; r++)
        {
            double yr = yTop - r * rowH;
            AddLine(x0, yr, x0 + tableW, yr);
        }

        // 表头文字 (居中)
        double hx = x0;
        for (int c = 0; c < headers.Length; c++)
        {
            AddText(headers[c], hx + colW[c] * 0.5, yTop - rowH * 0.5, em, MTextAttachmentPoint.MiddleCenter);
            hx += colW[c];
        }

        // 数据行
        var asph = new AsphericSurface { Radius = BaseRadius, ConicConstant = ConicConstant, EvenCoefficients = EvenCoefficients ?? Array.Empty<double>() };
        for (int i = 0; i < (SampleHeights?.Length ?? 0); i++)
        {
            double h = SampleHeights![i];
            double z = asph.Sag(Math.Abs(h));
            string dz = (SagTolerances != null && i < SagTolerances.Length) ? SagTolerances[i].ToString("F3", CultureInfo.InvariantCulture) : "";
            string slope = (SlopeTolerances != null && i < SlopeTolerances.Length) ? SlopeTolerances[i] ?? "" : "";
            string[] cells = { h.ToString("F1", CultureInfo.InvariantCulture), z.ToString("F3", CultureInfo.InvariantCulture), dz, slope };

            double yc = yTop - (i + 1) * rowH - rowH * 0.5;
            double cellx = x0;
            for (int c = 0; c < cells.Length; c++)
            {
                AddText(cells[c], cellx + colW[c] * 0.5, yc, em, MTextAttachmentPoint.MiddleCenter);
                cellx += colW[c];
            }
        }
        return x0 + tableW;
    }

    /// <summary>系数块 (R / K / A4.. / 斜率取样长度 / 取样步长)。</summary>
    private void BuildCoeffBlock(double x0, double yTop, double em)
    {
        var lines = new List<string>
        {
            $"R = {BaseRadius.ToString("0.###", CultureInfo.InvariantCulture)}",
            $"K = {ConicConstant.ToString("0.###", CultureInfo.InvariantCulture)}",
        };
        var coeffs = EvenCoefficients ?? Array.Empty<double>();
        for (int i = 0; i < coeffs.Length; i++)
            lines.Add($"A{4 + 2 * i} = {coeffs[i].ToString("0.#####E+00", CultureInfo.InvariantCulture)}");
        lines.Add($"斜率取样长度 = {SlopeSampleLength.ToString("0.###", CultureInfo.InvariantCulture)}");
        lines.Add($"取样步长 = {SlopeSampleStep.ToString("0.###", CultureInfo.InvariantCulture)}");

        double rowH = em * 1.7;
        for (int i = 0; i < lines.Count; i++)
            AddText(lines[i], x0, yTop - i * rowH - rowH * 0.5, em, MTextAttachmentPoint.MiddleLeft);
    }

    private void AddText(string s, double x, double y, double h, MTextAttachmentPoint ap)
    {
        if (string.IsNullOrEmpty(s)) return;
        _entities.Add(new MText(s, new Vector2(x, y), h)
        {
            AttachmentPoint = ap,
            color = Ink,
        });
    }

    private void AddLine(double x1, double y1, double x2, double y2)
        => _entities.Add(new Line(new Vector2(x1, y1), new Vector2(x2, y2)) { color = Ink });

    // -------- Entity 覆写 --------

    public override Bounding bounding
    {
        get
        {
            EnsureGenerated();
            Bounding? acc = null;
            foreach (var e in _entities)
            {
                var b = e.bounding;
                if (!b.IsValid) continue;
                if (acc is null) acc = new Bounding(b);
                else { var u = acc.Value; u.Union(b); acc = u; }
            }
            return acc ?? new Bounding(Position, TextHeight, TextHeight);
        }
    }

    public override void Draw(IGraphicsDraw gd)
    {
        EnsureGenerated();
        foreach (var e in _entities) e.Draw(gd);
    }

    public override void InvalidateRenderCache() => _entities.Clear();

    protected override DBObject CreateInstance() => new AsphericDataBlock();

    public override object Clone()
    {
        var c = (AsphericDataBlock)base.Clone();
        c.Position = Position;
        c.SurfaceLabel = SurfaceLabel;
        c.BaseRadius = BaseRadius;
        c.ConicConstant = ConicConstant;
        c.EvenCoefficients = (double[])(EvenCoefficients ?? Array.Empty<double>()).Clone();
        c.SampleHeights = (double[])(SampleHeights ?? Array.Empty<double>()).Clone();
        c.SagTolerances = (double[])(SagTolerances ?? Array.Empty<double>()).Clone();
        c.SlopeTolerances = (string[])(SlopeTolerances ?? Array.Empty<string>()).Clone();
        c.SlopeSampleLength = SlopeSampleLength;
        c.SlopeSampleStep = SlopeSampleStep;
        c.TextHeight = TextHeight;
        c._entities.Clear();
        return c;
    }

    public override void Translate(Vector2 translation)
    {
        Position += translation;
        _entities.Clear();
    }

    public override void Rotate(Vector2 center, double angle)
    {
        // 数据块文字始终水平 (同 NOTES 块); 旋转仅移动锚点。
        Position = Vector2.RotateInRadian(Position, center, angle);
        _entities.Clear();
    }

    public override void TransformBy(Matrix3 transform)
    {
        Position = transform * Position;
        var scale = transform.GetScale();
        if (scale.X > 0) TextHeight *= scale.X;
        _entities.Clear();
    }

    public override List<GripPoint> GetGripPoints()
        => new() { new GripPoint(GripPointType.Center, Position) };

    public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
    {
        if (index == 0) Position = newPosition;
        _entities.Clear();
    }
}
