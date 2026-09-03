using System;
using System.Collections.Generic;
using LitMath;
using OtoCAD;

namespace lcdb.Optic;

/// <summary>
/// 干净重设计的单透镜 (2D meridian 截面表示).
///
/// 几何约定:
/// - <see cref="Position"/> = 光轴上、镜片中心厚度的中点
/// - 光轴沿水平 (Rotation=0 时), 通过 <see cref="Rotation"/> 度调整方向
/// - <see cref="R1"/> 是前表面 (左侧, 入射方向) 曲率半径: 正=凸 (中心向右凸), 负=凹, double.PositiveInfinity=平
/// - <see cref="R2"/> 是后表面 (右侧, 出射方向) 曲率半径: 正=凸 (中心向左凸即对称双凸), 负=凹, +∞=平
///   注: 符号约定 R&gt;0 ⇔ 曲率中心在表面 *右侧* (光行进方向, ISO 10110-1 & Zemax 约定)
/// - <see cref="Diameter"/> 通光直径, <see cref="Thickness"/> 中心厚度
/// - <see cref="MaterialName"/> "N-BK7" / "F2" / "Custom"; 配合 <see cref="RefractiveIndex"/> + <see cref="AbbeNumber"/>
///
/// 渲染 (Generate): 4 段图元
///   - 前表面 Arc (R1)
///   - 后表面 Arc (R2)
///   - 上边界 Line (顶点 R1↑ → R2↑)
///   - 下边界 Line (顶点 R1↓ → R2↓)
/// 平面 (R=∞) 时退化为 Line.
/// </summary>
public sealed class OpticalLens : Entity, IPierceable
{
    public override string className => "OpticalLens";

    /// <inheritdoc/>
    /// <remarks>返回 lens 内部生成的 Arc/Line 子实体 (确保已 Generate 过).</remarks>
    public System.Collections.Generic.IEnumerable<Entity> GetPierceableSubEntities()
    {
        if (_markEntities.Count == 0) Generate();
        return _markEntities;
    }

    // -------- 几何参数 --------

    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>通光直径 (净口径, mm). 表面在此口径内弯曲.</summary>
    public double Diameter { get; set; } = 25.4;

    private double? _mechanicalDiameter;

    /// <summary>
    /// 机械外径 (边缘/装夹直径, mm). &gt; 通光直径 <see cref="Diameter"/> 时, 通光区外画一圈平肩 (land) 到机械边.
    /// **物理约束: 机械外径不得小于净口径** —— 设入 ≤ Diameter 的值会被钳到 Diameter (即无肩). 来自 Zemax MEMA.
    /// </summary>
    public double? MechanicalDiameter
    {
        // 读时钳到 ≥ Diameter (与赋值/反序列化顺序无关): 机械外径不可能小于净口径.
        get => _mechanicalDiameter.HasValue ? (double?)System.Math.Max(_mechanicalDiameter.Value, Diameter) : null;
        set => _mechanicalDiameter = value;
    }

    /// <summary>中心厚度 (mm).</summary>
    public double Thickness { get; set; } = 5.0;

    /// <summary>前表面 — 默认球面 R=50. 可换 SphericalSurface / AsphericSurface / ...</summary>
    public OpticalSurface FrontSurface { get; set; } = OpticalSurface.Sphere(50.0);

    /// <summary>后表面 — 默认球面 R=-50.</summary>
    public OpticalSurface BackSurface { get; set; } = OpticalSurface.Sphere(-50.0);

    /// <summary>
    /// R1 = 前表面基础曲率半径 (向后兼容老 API + 简单用例).
    /// SET: 若当前 FrontSurface 是球面则改半径; 否则替换为 SphericalSurface.
    /// GET: 返回 FrontSurface.Radius (球面/非球面都有 Radius 字段).
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public double R1
    {
        get => FrontSurface.Radius;
        set
        {
            if (FrontSurface is SphericalSurface)
                FrontSurface.Radius = value;
            else
                FrontSurface = OpticalSurface.Sphere(value);
        }
    }

    /// <summary>R2 = 后表面基础曲率半径 (向后兼容老 API).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public double R2
    {
        get => BackSurface.Radius;
        set
        {
            if (BackSurface is SphericalSurface)
                BackSurface.Radius = value;
            else
                BackSurface = OpticalSurface.Sphere(value);
        }
    }

    /// <summary>光轴方向 (度), 0 = 水平向右, 逆时针为正.</summary>
    public double Rotation { get; set; } = 0.0;

    /// <summary>显示光轴 (双点画线, GB/T 13323-2009 §2.1 — 光学件区别于普通几何件的核心视觉特征).</summary>
    public bool ShowOpticalAxis { get; set; } = true;

    /// <summary>光轴超出镜片厚度两端的延伸量 (mm). 默认与 Diameter 同量级.</summary>
    public double AxisPadding { get; set; } = 6.0;

    // -------- 自动标注显示开关 (默认全开 = 推荐标准集) --------
    // AutoDimensionLensCmd.Build 读这些开关决定生成哪些标注; 属性面板把它们反射成复选框,
    // 取消勾选 → RefreshAutoDimensionsFor 重生成 → 对应标注实时消失 (associativity).

    /// <summary>标 R1 前表面半径 (默认开).</summary>
    public bool ShowR1 { get; set; } = true;
    /// <summary>标 R2 后表面半径 (默认开).</summary>
    public bool ShowR2 { get; set; } = true;
    /// <summary>标 中心厚度 d (默认开).</summary>
    public bool ShowThickness { get; set; } = true;
    /// <summary>标 通光口径 Ø (默认开).</summary>
    public bool ShowDiameter { get; set; } = true;
    /// <summary>标 AR 镀膜符号 (默认开).</summary>
    public bool ShowCoating { get; set; } = true;

    /// <summary>
    /// 玻璃剖面填充色 (null = 不填充, 仅描边). 设为浅蓝可得"玻璃"观感 (ISO 制图样例风格).
    /// 填充用实心多边形, 描边仍走轮廓子图元.
    /// </summary>
    public Colors.Color? FillColor { get; set; }

    /// <summary>玻璃剖面填充样式 (可选). 默认 <see cref="GlassPattern.Symbol"/> = GB 光学三斜线符号.</summary>
    public GlassPattern Pattern { get; set; } = GlassPattern.Symbol;

    // -------- 公差 (GB/T 13323-2009 §1.1 三类值) --------

    /// <summary>
    /// R1 公差 (null = 公称值, 不带公差). 设值后 AutoDimensionLensCmd 在标注上
    /// 用 <see cref="Common.ToleranceValue.ToString"/> 替代纯数值 (例如 "60.00 +0.10/-0.05").
    /// 注意: <see cref="R1"/> 字段仍是底层曲率源, 公差仅影响标注显示, 不影响渲染.
    /// </summary>
    public Common.ToleranceValue? R1Tolerance { get; set; }

    /// <summary>R2 公差, 同 <see cref="R1Tolerance"/>.</summary>
    public Common.ToleranceValue? R2Tolerance { get; set; }

    /// <summary>通光直径 φ 公差.</summary>
    public Common.ToleranceValue? DiameterTolerance { get; set; }

    /// <summary>中心厚度 d 公差.</summary>
    public Common.ToleranceValue? ThicknessTolerance { get; set; }

    // -------- 材料 --------

    public string MaterialName { get; set; } = GlassLibrary.Default.Name;
    public double RefractiveIndex { get; set; } = GlassLibrary.Default.Nd;
    public double AbbeNumber { get; set; } = GlassLibrary.Default.Vd;

    // -------- 扩展 metadata (Zemax 绑定 / 标签等) --------

    /// <summary>外部设计文件引用 + 自由 metadata (可选).</summary>
    public OpticalExtensions? Extensions { get; set; }

    // -------- Composite: 从属标注 --------

    /// <summary>
    /// "贴在这个透镜上" 的子实体 (CoatingMark / SurfaceQualityMark / Dimension 等).
    /// 子实体 Position 是绝对模型坐标 (lens.Translate 会级联调用 child.Translate).
    /// V4 起序列化, EntityJsonConverter 自动处理 List&lt;Entity&gt; 多态.
    /// 反序列化用 init-able 列表 (System.Text.Json 会调 add).
    /// </summary>
    public List<Entity> OwnedAnnotations { get; set; } = new();

    /// <summary>把标注贴到该透镜上 (level 1 children, 暂不嵌套).</summary>
    public void AddOwnedAnnotation(Entity ann)
    {
        if (ann is null) return;
        OwnedAnnotations.Add(ann);
        _markEntities.Clear();
    }

    /// <summary>按 GlassLibrary 查的牌号自动填 n/V (设 Custom 时 no-op).</summary>
    public void ApplyMaterial(string materialName)
    {
        MaterialName = materialName;
        var g = GlassLibrary.Find(materialName);
        if (g is not null)
        {
            RefractiveIndex = g.Nd;
            AbbeNumber = g.Vd;
        }
    }

    // -------- 渲染缓存 --------

    private List<Entity> _markEntities = new();

    public override Bounding bounding
    {
        get
        {
            double lensNetHalf = Diameter * 0.5;
            double frontHalf = OpticSurfaceGeometry.DrawnSemiAperture(FrontSurface, lensNetHalf);
            double backHalf  = OpticSurfaceGeometry.DrawnSemiAperture(BackSurface, lensNetHalf);
            double mechHalf = MechanicalDiameter.HasValue ? MechanicalDiameter.Value * 0.5 : 0.0;
            mechHalf = Math.Max(mechHalf, Math.Max(frontHalf, backHalf));
            double halfW = Thickness * 0.5 + Math.Max(Math.Abs(FrontSurface.Sag(frontHalf)), Math.Abs(BackSurface.Sag(backHalf)));
            var min = new Vector2(Position.X - halfW, Position.Y - mechHalf);
            var max = new Vector2(Position.X + halfW, Position.Y + mechHalf);
            return new Bounding(min, max);
        }
    }

    /// <summary>球面 sag (向后兼容; 实际走 OpticSurfaceGeometry).</summary>
    public static double ComputeSag(double radius, double semiAperture)
        => OpticSurfaceGeometry.ComputeSag(radius, semiAperture);

    private void Generate()
    {
        _markEntities.Clear();

        double halfT = Thickness * 0.5;
        // 逐面净口径: 每个面弯到自己的 SemiAperture (无则用镜片 Diameter/2), 再钳到该面 |R|.
        double lensNetHalf = Diameter * 0.5;
        double frontHalf = OpticSurfaceGeometry.DrawnSemiAperture(FrontSurface, lensNetHalf);
        double backHalf  = OpticSurfaceGeometry.DrawnSemiAperture(BackSurface, lensNetHalf);
        // 机械外径 (OD) = 机械直径/2 与各面净口径取最大, 保证包住所有面.
        double mechHalf = MechanicalDiameter.HasValue ? MechanicalDiameter.Value * 0.5 : 0.0;
        mechHalf = Math.Max(mechHalf, Math.Max(frontHalf, backHalf));

        double frontApexX = Position.X - halfT;
        double backApexX  = Position.X + halfT;
        double frontEdgeX = frontApexX + FrontSurface.Sag(frontHalf);  // 肩根 X (表面在自身净口径处)
        double backEdgeX  = backApexX  + BackSurface.Sag(backHalf);

        double odT = Position.Y + mechHalf, odB = Position.Y - mechHalf;

        // 机械外径上/下边线 (OD)
        _markEntities.Add(new Line(new Vector2(frontEdgeX, odT), new Vector2(backEdgeX, odT)) { color = color });
        _markEntities.Add(new Line(new Vector2(frontEdgeX, odB), new Vector2(backEdgeX, odB)) { color = color });

        // 平肩 (land): 各面从自身净口径边竖直到机械外径 (仅当该面净口径 < 机械外径).
        AddLand(frontEdgeX, frontHalf, mechHalf);
        AddLand(backEdgeX,  backHalf,  mechHalf);

        // 前/后表面弧 — 各只在自身净口径内弯曲.
        FrontSurface.AppendRenderEntities(_markEntities, frontApexX, Position.Y, frontHalf, color);
        BackSurface.AppendRenderEntities(_markEntities, backApexX, Position.Y, backHalf, color);

        // 光轴 (双点画线, GB/T 13323-2009 §2.1)
        if (ShowOpticalAxis)
        {
            double pad = Math.Max(AxisPadding, 0);
            // 沿镜片实际占用 X 范围 (含 sag) 两端再各加 pad
            double xLeft  = Math.Min(frontApexX, frontEdgeX) - pad;
            double xRight = Math.Max(backApexX,  backEdgeX)  + pad;
            _markEntities.Add(new Line(
                new Vector2(xLeft,  Position.Y),
                new Vector2(xRight, Position.Y))
            {
                // 光轴: 细灰长点画线 (ISO 128-50 / GB/T 13323 光轴线), 区别于零件轮廓
                color = Colors.Color.FromRGB(0x66, 0x66, 0x66),
                lineType = LineType.DashDotDot,
            });
        }

        // 应用 Rotation (绕 Position)
        if (Rotation != 0)
        {
            double rad = Rotation * Math.PI / 180.0;
            foreach (var ent in _markEntities)
            {
                ent.Rotate(Position, rad);
            }
        }
    }

    /// <summary>画某面的平肩 (land): 从该面净口径边 (±surfHalf) 竖直到机械外径 (±mechHalf), 等 X.</summary>
    private void AddLand(double edgeX, double surfHalf, double mechHalf)
    {
        if (mechHalf <= surfHalf + 1e-6) return;
        _markEntities.Add(new Line(
            new Vector2(edgeX, Position.Y + surfHalf), new Vector2(edgeX, Position.Y + mechHalf)) { color = color });
        _markEntities.Add(new Line(
            new Vector2(edgeX, Position.Y - surfHalf), new Vector2(edgeX, Position.Y - mechHalf)) { color = color });
    }

    public override void Draw(IGraphicsDraw gd)
    {
        if (_markEntities.Count == 0) Generate();

        // 玻璃剖面: 轮廓之前先画 (轮廓盖在上面). FillColor 实心底色与 Pattern 线纹相互独立.
        if (FillColor.HasValue || Pattern != GlassPattern.None)
        {
            var poly = BuildCrossSectionPolygon(48);
            if (FillColor.HasValue)
            {
                var prev = gd.CurrentColor;
                gd.CurrentColor = FillColor.Value.ToDrawingColor();
                gd.DrawFilledPolygon(poly);
                gd.CurrentColor = prev;
            }
            foreach (var (a, b) in OpticSurfaceGeometry.GlassPatternSegments(poly, Pattern, Diameter, System.Math.PI / 4))
                new Line(a, b) { color = OpticSurfaceGeometry.GlassHatch }.Draw(gd);
        }

        foreach (var e in _markEntities) e.Draw(gd);
        // 渲染从属标注 (相对 lens 已绝对定位; 后续可加变换矩阵)
        foreach (var ann in OwnedAnnotations) ann.Draw(gd);
    }

    /// <summary>
    /// 玻璃剖面闭合多边形 (绝对模型坐标, 已应用 Rotation). 前表面 top→bottom + 后表面 bottom→top.
    /// 用于 <see cref="FillColor"/> 填充. seg = 每个表面的离散段数.
    /// </summary>
    private List<Vector2> BuildCrossSectionPolygon(int seg)
    {
        if (seg < 2) seg = 2;
        double halfT = Thickness * 0.5;
        double lensNetHalf = Diameter * 0.5;
        double frontHalf = OpticSurfaceGeometry.DrawnSemiAperture(FrontSurface, lensNetHalf);
        double backHalf  = OpticSurfaceGeometry.DrawnSemiAperture(BackSurface, lensNetHalf);
        double mechHalf = MechanicalDiameter.HasValue ? MechanicalDiameter.Value * 0.5 : 0.0;
        mechHalf = Math.Max(mechHalf, Math.Max(frontHalf, backHalf));
        double frontApexX = Position.X - halfT;
        double backApexX  = Position.X + halfT;
        var pts = new List<Vector2>(seg * 2 + 2);
        // 前表面剖面 (+mechHalf → -mechHalf): 净口径内随面弯, 净口径外为平肩 (X 固定在肩根).
        for (int i = 0; i <= seg; i++)
        {
            double h = mechHalf - 2.0 * mechHalf * i / seg;
            double hc = Math.Min(Math.Abs(h), frontHalf);
            pts.Add(new Vector2(frontApexX + FrontSurface.Sag(hc), Position.Y + h));
        }
        // 后表面剖面 (-mechHalf → +mechHalf)
        for (int i = 0; i <= seg; i++)
        {
            double h = -mechHalf + 2.0 * mechHalf * i / seg;
            double hc = Math.Min(Math.Abs(h), backHalf);
            pts.Add(new Vector2(backApexX + BackSurface.Sag(hc), Position.Y + h));
        }
        if (Rotation != 0)
        {
            double rad = Rotation * Math.PI / 180.0;
            for (int i = 0; i < pts.Count; i++)
                pts[i] = Vector2.RotateInRadian(pts[i], Position, rad);
        }
        return pts;
    }

    // -------- Entity 重写 --------

    /// <summary>外部修改 R/T/D/Material 等渲染相关属性后调本方法, 下次 Draw 重新 Generate.</summary>
    public override void InvalidateRenderCache() => _markEntities.Clear();

    protected override DBObject CreateInstance() => new OpticalLens();

    public override object Clone()
    {
        var c = (OpticalLens)base.Clone();
        c.Position = Position;
        c.Diameter = Diameter;
        c.MechanicalDiameter = MechanicalDiameter;
        c.Thickness = Thickness;
        c.FrontSurface = FrontSurface.Clone();
        c.BackSurface = BackSurface.Clone();
        c.Rotation = Rotation;
        c.ShowOpticalAxis = ShowOpticalAxis;
        c.AxisPadding = AxisPadding;
        c.ShowR1 = ShowR1;
        c.ShowR2 = ShowR2;
        c.ShowThickness = ShowThickness;
        c.ShowDiameter = ShowDiameter;
        c.ShowCoating = ShowCoating;
        c.FillColor = FillColor;
        c.Pattern = Pattern;
        c.R1Tolerance = R1Tolerance?.Clone();
        c.R2Tolerance = R2Tolerance?.Clone();
        c.DiameterTolerance = DiameterTolerance?.Clone();
        c.ThicknessTolerance = ThicknessTolerance?.Clone();
        c.MaterialName = MaterialName;
        c.RefractiveIndex = RefractiveIndex;
        c.AbbeNumber = AbbeNumber;
        c.Extensions = Extensions is null ? null : new OpticalExtensions
        {
            Tags = new System.Collections.Generic.Dictionary<string, string>(Extensions.Tags),
            Zemax = Extensions.Zemax is null ? null : new ZemaxBinding
            {
                FilePath = Extensions.Zemax.FilePath,
                SurfaceIndex = Extensions.Zemax.SurfaceIndex,
                LastSync = Extensions.Zemax.LastSync,
                Note = Extensions.Zemax.Note,
            }
        };
        c.OwnedAnnotations = new List<Entity>(OwnedAnnotations.Count);
        foreach (var ann in OwnedAnnotations)
            c.OwnedAnnotations.Add((Entity)ann.Clone());
        c._markEntities = new List<Entity>();
        return c;
    }

    public override void Translate(Vector2 translation)
    {
        Position += translation;
        foreach (var ann in OwnedAnnotations) ann.Translate(translation);
        _markEntities.Clear();
    }

    public override void Rotate(Vector2 center, double angle)
    {
        Position = Vector2.RotateInRadian(Position, center, angle);
        Rotation += angle * 180.0 / Math.PI;
        foreach (var ann in OwnedAnnotations) ann.Rotate(center, angle);
        _markEntities.Clear();
    }

    public override void TransformBy(Matrix3 transform)
    {
        Position = transform * Position;
        // 不强行缩放镜片 (光学元件应保持物理尺寸); 只跟随平移
        foreach (var ann in OwnedAnnotations) ann.TransformBy(transform);
        _markEntities.Clear();
    }

    public override List<GripPoint> GetGripPoints()
    {
        var grips = new List<GripPoint>
        {
            new(GripPointType.Center, Position),
        };
        // 上下边缘 (改直径) + 左右顶点 (改厚度)
        double halfT = Thickness * 0.5;
        double halfD = Diameter * 0.5;
        grips.Add(new(GripPointType.Corner, Position + new Vector2(0, +halfD)));
        grips.Add(new(GripPointType.Corner, Position + new Vector2(0, -halfD)));
        grips.Add(new(GripPointType.Corner, Position + new Vector2(-halfT, 0)));
        grips.Add(new(GripPointType.Corner, Position + new Vector2(+halfT, 0)));
        return grips;
    }

    public override List<ObjectSnapPoint> GetSnapPoints()
    {
        double hD = Diameter * 0.5, hT = Thickness * 0.5;
        double feX = OpticSurfaceGeometry.ComputeSag(R1, hD);   // 前表面在净口径处的带符号矢高 → 边缘角点 X 偏移
        double beX = OpticSurfaceGeometry.ComputeSag(R2, hD);
        return new List<ObjectSnapPoint>
        {
            new(ObjectSnapMode.Center, Position),
            new(ObjectSnapMode.End, Position + new Vector2(-hT, 0)),   // 前顶点
            new(ObjectSnapMode.End, Position + new Vector2(+hT, 0)),   // 后顶点
            // 通光口径上/下端 (轴向 X, 供 Ø 标注端点吸附)
            new(ObjectSnapMode.End, Position + new Vector2(0, +hD)),
            new(ObjectSnapMode.End, Position + new Vector2(0, -hD)),
            // 剖面四角 (上/下 × 前/后边缘, 供边厚标注端点吸附)
            new(ObjectSnapMode.End, Position + new Vector2(-hT + feX, +hD)),
            new(ObjectSnapMode.End, Position + new Vector2(-hT + feX, -hD)),
            new(ObjectSnapMode.End, Position + new Vector2(+hT + beX, +hD)),
            new(ObjectSnapMode.End, Position + new Vector2(+hT + beX, -hD)),
        };
    }

    public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
    {
        switch (index)
        {
            case 0:   // 中心
                Position = newPosition;
                break;
            case 1:   // 上边缘 → 改半径
            case 2:   // 下边缘
                Diameter = Math.Max(0.5, Math.Abs(newPosition.Y - Position.Y) * 2);
                break;
            case 3:   // 左顶点 → 改厚度
            case 4:   // 右顶点
                Thickness = Math.Max(0.1, Math.Abs(newPosition.X - Position.X) * 2);
                break;
        }
        _markEntities.Clear();
    }
}
