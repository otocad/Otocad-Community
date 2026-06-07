using System;
using System.Collections.Generic;
using System.Globalization;
using lcdb;
using lcdb.Common;
using lcdb.Optic;
using LitMath;
using OtoCAD.Avalonia.Services;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// "一键自动尺寸标注" — 给一个 OpticalLens 或 CementedLens 自动生成 R1/R2/d/φ
/// 等核心尺寸标注 (GB/T 13323-2009 §3 光学零件图必标项).
///
/// 不是 ICadCommand (没有用户多步交互), 而是 MainWindow 把它注册成普通 Action:
/// 拿到当前选中的 OpticalLens/CementedLens → 调 Apply() → 4-5 个 Dimension 实体加入画布.
///
/// 几何与布局分离 (重构后):
/// - **几何**: R 标注用真球心 + rim 锚点; measurement 自然 = |R|, 无 displayedMeasurementOverride.
/// - **布局**: 引线长度 / kink 方向 / d/φ 偏移量等"放在哪"的参数,
///   全部从 <see cref="AutoDimLayoutPolicy"/> 取 (来自 <see cref="DimensionStandardService.CreateLayoutPolicy"/>),
///   不再硬编码. 未来不同 standard 可定义不同 policy.
/// </summary>
public static class AutoDimensionLensCmd
{
    /// <summary>
    /// 入口: 给一个 lens (OpticalLens 或 CementedLens) 生成尺寸标注实体列表.
    /// 调用方负责把列表加入画布 (推荐 <c>canvas.AddEntitiesBatch(list)</c> 让整批共享一条 undo).
    /// 返回空列表 = 类型不支持.
    /// </summary>
    /// <summary>自动标注统一着色 (区别于黑色零件轮廓) — 蓝色标注层惯例.</summary>
    private static readonly lcdb.Colors.Color DimColor = lcdb.Colors.Color.FromRGB(0x15, 0x65, 0xC0);

    public static IReadOnlyList<Entity> Build(Entity lens, bool includeRadial = true)
    {
        if (lens is null) return Array.Empty<Entity>();
        var list = new List<Entity>(8);
        var style = CreateAutoOpticalStyle();
        var layout = DimensionStandardService.CreateLayoutPolicy();
        switch (lens)
        {
            case OpticalLens ol: ApplyToSingle(ol, list.Add, style, layout, includeRadial); break;
            case CementedLens cl: ApplyToCemented(cl, list.Add, style, layout, includeRadial); break;
        }
        // 统一着色: 尺寸标注用蓝色与零件轮廓区分; 镀膜标记保留按膜系的语义色 (AR=深绿等)
        foreach (var e in list)
            if (e is not lcdb.Annotation.CoatingMark) e.color = DimColor;
        return list;
    }

    /// <summary>
    /// 单独的"中心厚度"标注 (d) — 选中一个透镜一键生成单条 LinearDimension.
    /// 复用 AutoDim 的 d 逻辑 (水平标注, dimLine 在镜片上方). 返回 null = 类型不支持.
    /// </summary>
    public static LinearDimension? BuildCenterThickness(Entity lens)
    {
        var style = CreateAutoOpticalStyle();
        var layout = DimensionStandardService.CreateLayoutPolicy();
        switch (lens)
        {
            case OpticalLens ol:
            {
                double halfT = ol.Thickness * 0.5, halfD = ol.Diameter * 0.5;
                double px = ol.Position.X, py = ol.Position.Y;
                double horizOffset = halfD * layout.DAboveLensFactor + layout.DAboveLensMin;
                var d = CreateLinearDimension(
                    new Vector2(px - halfT, py), new Vector2(px + halfT, py),
                    new Vector2(px, py + horizOffset), 0.0, style);
                d.userText = BuildLinearUserTextTemplate(ol.ThicknessTolerance, "d");
                return d;
            }
            case CementedLens cl:
            {
                double halfTotal = (cl.T1 + cl.T2) * 0.5, halfD = cl.Diameter * 0.5;
                double px = cl.Position.X, py = cl.Position.Y;
                double horizOffset = halfD * layout.DAboveLensFactor + layout.DAboveLensMin;
                var d = CreateLinearDimension(
                    new Vector2(px - halfTotal, py), new Vector2(px + halfTotal, py),
                    new Vector2(px, py + horizOffset), 0.0, style);
                d.userText = BuildLinearUserTextTemplate(null, "d");
                return d;
            }
        }
        return null;
    }

    /// <summary>
    /// 单独的"边缘厚度"标注 (t) — rim 处前后表面边缘点间的水平距离.
    /// 边缘 X = apexX + 带符号 sag (与 OpticalLens.Generate 同一约定), 任意凹凸都正确.
    /// 返回 null = 类型不支持.
    /// </summary>
    public static LinearDimension? BuildEdgeThickness(Entity lens)
    {
        var style = CreateAutoOpticalStyle();
        var layout = DimensionStandardService.CreateLayoutPolicy();
        switch (lens)
        {
            case OpticalLens ol:
            {
                double halfT = ol.Thickness * 0.5, halfD = ol.Diameter * 0.5;
                double px = ol.Position.X, py = ol.Position.Y;
                double frontEdgeX = px - halfT + OpticSurfaceGeometry.ComputeSag(ol.R1, halfD);
                double backEdgeX  = px + halfT + OpticSurfaceGeometry.ComputeSag(ol.R2, halfD);
                return BuildEdgeDim(frontEdgeX, backEdgeX, py + halfD, halfD, layout, style);
            }
            case CementedLens cl:
            {
                double halfTotal = (cl.T1 + cl.T2) * 0.5, halfD = cl.Diameter * 0.5;
                double px = cl.Position.X, py = cl.Position.Y;
                double frontEdgeX = px - halfTotal + OpticSurfaceGeometry.ComputeSag(cl.R1, halfD);
                double backEdgeX  = px + halfTotal + OpticSurfaceGeometry.ComputeSag(cl.R3, halfD);
                return BuildEdgeDim(frontEdgeX, backEdgeX, py + halfD, halfD, layout, style);
            }
        }
        return null;
    }

    private static LinearDimension BuildEdgeDim(
        double frontEdgeX, double backEdgeX, double rimY, double halfD,
        AutoDimLayoutPolicy layout, DimensionStyle style)
    {
        double dimY = rimY + halfD * layout.DAboveLensFactor + layout.DAboveLensMin;
        var t = CreateLinearDimension(
            new Vector2(frontEdgeX, rimY), new Vector2(backEdgeX, rimY),
            new Vector2((frontEdgeX + backEdgeX) * 0.5, dimY), 0.0, style);
        t.userText = BuildLinearUserTextTemplate(null, "t");
        return t;
    }

    private static int ApplyToSingle(OpticalLens lens, Action<Entity> add, DimensionStyle dimStyle, AutoDimLayoutPolicy layout, bool includeRadial = true)
    {
        double halfT = lens.Thickness * 0.5;
        double halfD = lens.Diameter * 0.5;
        double px = lens.Position.X;
        double py = lens.Position.Y;

        double horizOffset = halfD + layout.DAboveLensMin;  // 始终在镜片上边缘之上固定间隙 (大镜片也不会落进内部)
        double vertOffset  = halfT * layout.PhiSideOfLensFactor + layout.PhiSideOfLensMin;
        int radialCount = 0;

        if (includeRadial)
        {
            // R1: 前表面 apex = (px - halfT, py)
            radialCount += EmitRadialDim(
                r: lens.R1, tol: lens.R1Tolerance, prefix: "R1",
                apexX: px - halfT, axisY: py, halfD: halfD,
                planarLabelPos: new Vector2(px - halfT * 1.5 - halfD * 0.3, py + halfD * 0.5),
                add, dimStyle, layout);

            // R2: 后表面 apex = (px + halfT, py)
            radialCount += EmitRadialDim(
                r: lens.R2, tol: lens.R2Tolerance, prefix: "R2",
                apexX: px + halfT, axisY: py, halfD: halfD,
                planarLabelPos: new Vector2(px + halfT * 1.5 + halfD * 0.1, py + halfD * 0.5),
                add, dimStyle, layout);
        }

        // d: 中心厚度 (水平 LinearDimension, dimLine 在镜片上方)
        var dThk = CreateLinearDimension(
            new Vector2(px - halfT, py),
            new Vector2(px + halfT, py),
            new Vector2(px, py + horizOffset),
            0.0, dimStyle);
        dThk.textReferencePoint = new Vector2(px, py + horizOffset + dimStyle.TextHeight * 0.9 + dimStyle.DimensionLineGap);
        dThk.userText = BuildLinearUserTextTemplate(lens.ThicknessTolerance, "d");
        add(dThk);

        // φ: 通光直径 (竖直 LinearDimension, dimLine 在镜片右侧)
        var dDia = CreateLinearDimension(
            new Vector2(px, py - halfD),
            new Vector2(px, py + halfD),
            new Vector2(px + vertOffset, py),
            -Math.PI / 2, dimStyle);
        dDia.textReferencePoint = new Vector2(px + vertOffset + dimStyle.TextHeight * 1.2 + dimStyle.DimensionLineGap, py);
        dDia.userText = BuildLinearUserTextTemplate(lens.DiameterTolerance, "Ø");
        add(dDia);

        // 左右外表面 AR 镀膜标记 (无文字, 只贴面)
        EmitCoatingMark(px - halfT, py, halfD, lens.R1, glassOnPlusX: true,  add);  // 前表面 (左外)
        EmitCoatingMark(px + halfT, py, halfD, lens.R2, glassOnPlusX: false, add);  // 后表面 (右外)

        return 2 + radialCount;
    }

    private static int ApplyToCemented(CementedLens lens, Action<Entity> add, DimensionStyle dimStyle, AutoDimLayoutPolicy layout, bool includeRadial = true)
    {
        double totalT = lens.T1 + lens.T2;
        double halfTotal = totalT * 0.5;
        double halfD = lens.Diameter * 0.5;
        double px = lens.Position.X;
        double py = lens.Position.Y;

        double horizOffset = halfD + layout.DAboveLensMin;  // 始终在镜片上边缘之上固定间隙 (大镜片也不会落进内部)
        double vertOffset  = halfTotal * layout.PhiSideOfLensFactor + layout.PhiSideOfLensMin;
        double t2Stack = halfD * layout.CementedT2StackFactor + layout.CementedT2StackMin;

        double frontX   = px - halfTotal;
        double contactX = frontX + lens.T1;
        double backX    = contactX + lens.T2;

        int count = 0;

        if (includeRadial)
        {
            count += EmitRadialDim(lens.R1, lens.R1Tolerance, "R1",
                apexX: frontX, axisY: py, halfD: halfD,
                planarLabelPos: new Vector2(frontX - halfD * 0.4, py + halfD * 0.5),
                add, dimStyle, layout);

            count += EmitRadialDim(lens.RContact, lens.RContactTolerance, "RC",
                apexX: contactX, axisY: py, halfD: halfD,
                planarLabelPos: new Vector2(contactX + 2, py + halfD * 0.8),
                add, dimStyle, layout);

            count += EmitRadialDim(lens.R3, lens.R3Tolerance, "R3",
                apexX: backX, axisY: py, halfD: halfD,
                planarLabelPos: new Vector2(backX + halfD * 0.1, py + halfD * 0.5),
                add, dimStyle, layout);
        }

        // T1: 片 1 中心厚 — 上方
        var dT1 = CreateLinearDimension(
            new Vector2(frontX, py),
            new Vector2(contactX, py),
            new Vector2((frontX + contactX) * 0.5, py + horizOffset),
            0.0, dimStyle);
        dT1.textReferencePoint = new Vector2((frontX + contactX) * 0.5, py + horizOffset + dimStyle.TextHeight * 0.9 + dimStyle.DimensionLineGap);
        dT1.userText = BuildLinearUserTextTemplate(lens.T1Tolerance, "d1");
        add(dT1); count++;

        // T2: 片 2 中心厚 — 上方 (再上一层, 错开 T1 标注)
        var dT2 = CreateLinearDimension(
            new Vector2(contactX, py),
            new Vector2(backX, py),
            new Vector2((contactX + backX) * 0.5, py + horizOffset + t2Stack),
            0.0, dimStyle);
        dT2.textReferencePoint = new Vector2((contactX + backX) * 0.5, py + horizOffset + t2Stack + dimStyle.TextHeight * 0.9 + dimStyle.DimensionLineGap);
        dT2.userText = BuildLinearUserTextTemplate(lens.T2Tolerance, "d2");
        add(dT2); count++;

        // φ: 通光直径 — 右侧竖直
        var dDia = CreateLinearDimension(
            new Vector2(px, py - halfD),
            new Vector2(px, py + halfD),
            new Vector2(px + vertOffset, py),
            -Math.PI / 2, dimStyle);
        dDia.textReferencePoint = new Vector2(px + vertOffset + dimStyle.TextHeight * 1.2 + dimStyle.DimensionLineGap, py);
        dDia.userText = BuildLinearUserTextTemplate(lens.DiameterTolerance, "Ø");
        add(dDia); count++;

        // 左右外表面 AR 镀膜标记 (无文字, 只贴面; 胶合内表面 RContact 不镀外膜)
        EmitCoatingMark(frontX, py, halfD, lens.R1, glassOnPlusX: true,  add);  // 前表面 (左外)
        EmitCoatingMark(backX,  py, halfD, lens.R3, glassOnPlusX: false, add);  // 后表面 (右外)

        return count;
    }

    /// <summary>
    /// R 标注核心 — 真几何 + kinked-leader 排版.
    ///
    /// 几何:
    ///   球心 = <c>apex + (R, 0)</c> (R 带符号, ISO 10110 / OpticSurfaceGeometry 统一约定).
    ///   锚点 = 弧中段 <c>(apex + sag(halfD/2), axisY + halfD/2)</c> — 不落在弧与边线的顶点.
    ///   measurement = distance(球心, 锚点) = |R|, 自动跟随.
    ///
    /// 排版 (kinked leader / L 形引线):
    ///   1. arrow 落在 rim, 沿径向短距离引出 (RLeaderArmMin / Factor)
    ///   2. 文字 Y 与引线终点对齐 → shoulder 是水平直线
    ///   3. 文字 X 水平外推 horizKick 距离, 方向由 direction.X 符号决定
    ///      (R1 向左, R2 向右, 自然避开 lens 与 d/φ)
    /// </summary>
    private static int EmitRadialDim(
        double r, ToleranceValue? tol, string prefix,
        double apexX, double axisY, double halfD,
        Vector2 planarLabelPos,
        Action<Entity> add, DimensionStyle style, AutoDimLayoutPolicy layout)
    {
        bool planar = IsPlanarRadius(r) || (tol?.IsPlanar ?? false);
        if (planar)
        {
            add(new MText($"{prefix} = R∞", planarLabelPos, 2.5));
            return 1;
        }
        if (!IsFiniteRadius(r)) return 0;

        var sphereCenter = new Vector2(apexX + r, axisY);
        // 锚点取弧中段 (半口径的一半处), 避免落在弧与上边线相接的顶点上.
        // 弧上任意点到球心都是 |R|, 故 measurement 不变.
        double anchorH = halfD * 0.5;
        double sag = OpticSurfaceGeometry.ComputeSag(r, anchorH);
        var rimPoint = new Vector2(apexX + sag, axisY + anchorH);

        double leaderArm = Math.Max(layout.RLeaderArmMin, style.TextHeight * layout.RLeaderArmFactor);
        var rd = new RadialDimension(sphereCenter, rimPoint, leaderArm)
        {
            style = style.Clone(),
            userText = BuildRadiusUserTextTemplate(tol, prefix)
        };

        // Kinked-leader 排版
        Vector2 direction = (rimPoint - sphereCenter).normalized;
        Vector2 leaderEnd = rimPoint + direction * leaderArm;
        double horizKick = Math.Max(halfD * layout.RTextKickFactor, style.TextHeight * layout.RTextKickMinFactor);
        int kickSign = Math.Abs(direction.X) > 0.05 ? Math.Sign(direction.X) : 1; // 近垂直引线默认右推
        double textX = leaderEnd.X + kickSign * horizKick;
        rd.textReferencePoint = new Vector2(textX, leaderEnd.Y);

        add(rd);
        return 1;
    }

    /// <summary>镀膜标记固定尺寸 (模型单位): 不随镜片大小缩放, 保证清晰可辨。</summary>
    private const double CoatingMarkSize = 8.0;

    /// <summary>
    /// 在透镜外表面下半弧放一个 AR 镀膜标记: 固定尺寸、无文字、只贴面不旋转 (圆与面相切坐在面外侧)。
    /// 放在下半弧 (负高度) 以避开上方的 R 引线与 d 标注。
    /// </summary>
    private static void EmitCoatingMark(
        double apexX, double axisY, double halfD, double r, bool glassOnPlusX, Action<Entity> add)
    {
        double h = -halfD * 0.5;   // 下半弧
        // 表面 X(y) = apexX + sag(|y|); 球面 sag 关于高度对称, 故用 |y|。
        double X(double y) => apexX + OpticSurfaceGeometry.ComputeSag(r, Math.Abs(y));
        var pt = new Vector2(X(h), axisY + h);

        Vector2 outward;
        if (IsPlanarRadius(r))
        {
            outward = new Vector2(glassOnPlusX ? -1 : 1, 0);   // 平面: 外法线沿 ±X
        }
        else
        {
            const double d = 0.05;
            double slope = (X(h + d) - X(h - d)) / (2 * d);     // dX/dy 数值求导
            var nPlusX = new Vector2(1, -slope).normalized;     // 指向 +X 的法线
            outward = glassOnPlusX ? -nPlusX : nPlusX;          // 玻璃在 +X 侧 → 外法线朝 -X
        }

        var mark = new lcdb.Annotation.CoatingMark(pt, lcdb.Annotation.CoatingType.AR, CoatingMarkSize)
        {
            ShowText = false
        };
        mark.AttachToSurface(pt, outward);
        add(mark);
    }

    /// <summary>"R1 = &lt;&gt;" / "R1 = &lt;&gt;±0.10" / "R1 = &lt;&gt; +0.10/-0.05" / "R1 = R∞".</summary>
    private static string BuildRadiusUserTextTemplate(ToleranceValue? tol, string prefix)
        => prefix + " = " + BuildMeasuredValueTemplate(tol, allowPlanar: true);

    private static string BuildLinearUserTextTemplate(ToleranceValue? tol, string prefix)
        => prefix + " = " + BuildMeasuredValueTemplate(tol, allowPlanar: false);

    private static string BuildMeasuredValueTemplate(ToleranceValue? tol, bool allowPlanar)
    {
        if (tol is null) return "<>";
        if (allowPlanar && tol.IsPlanar) return "R∞";

        bool hasTol = tol.Plus != 0 || tol.Minus != 0;
        bool symmetric = Math.Abs(tol.Plus - tol.Minus) < 1e-9;

        return tol.Kind switch
        {
            ValueKind.Reference => "(<>)",
            ValueKind.Actual when hasTol && symmetric
                => "<>±" + tol.Plus.ToString("F2", CultureInfo.InvariantCulture),
            // 上下偏差不等 → MTEXT 堆叠码, Text 实体会把上/下偏差摞起来绘制 (规范公差标法)
            ValueKind.Actual when hasTol
                => "<>\\S+" + tol.Plus.ToString("F2", CultureInfo.InvariantCulture)
                   + "^-" + tol.Minus.ToString("F2", CultureInfo.InvariantCulture) + ";",
            _ => "<>"
        };
    }

    /// <summary>R 是否为有限值 (非 ±∞, 非 NaN, 绝对值不超过 1e7 视为可绘半径).</summary>
    private static bool IsFiniteRadius(double r)
        => !double.IsNaN(r) && !double.IsInfinity(r) && Math.Abs(r) < 1e7 && Math.Abs(r) > 1e-6;

    /// <summary>R 是否表示平面 (∞ / NaN / 极大值).</summary>
    private static bool IsPlanarRadius(double r)
        => double.IsNaN(r) || double.IsInfinity(r) || Math.Abs(r) >= 1e7;

    private static LinearDimension CreateLinearDimension(
        Vector2 firstPoint,
        Vector2 secondPoint,
        Vector2 dimLinePosition,
        double rotation,
        DimensionStyle style)
    {
        Vector2 midRef = (firstPoint + secondPoint) * 0.5;
        Vector2 dimDir = new Vector2(Math.Cos(rotation), Math.Sin(rotation));
        Vector2 perpDir = Vector2.Perpendicular(dimDir);
        double offset = Vector2.Dot(dimLinePosition - midRef, perpDir);

        if (offset < 0)
        {
            rotation += Math.PI;
            offset = -offset;
        }

        return new LinearDimension(firstPoint, secondPoint, offset, rotation, style.Clone());
    }

    private static DimensionStyle CreateAutoOpticalStyle()
    {
        var style = DimensionStandardService.CreateStyle();

        // 自动标注放大一档, 字/箭头更清晰; 间隙加大避免拥挤。
        style.TextHeight = Math.Max(style.TextHeight, 3.5);
        style.ArrowSize = Math.Max(style.ArrowSize, 3.0);
        style.ExtensionLineExtend = Math.Max(style.ExtensionLineExtend, 2.0);
        style.ExtensionLineOffset = Math.Max(style.ExtensionLineOffset, 0.8);
        style.DimensionLineGap = Math.Max(style.DimensionLineGap, 1.6);

        // kinked-leader 需要 RadialDimension.Generate 在 text-X 偏移时画 shoulder
        style.TextInsideHorizontal = true;

        return style;
    }
}
