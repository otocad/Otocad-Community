using System;
using System.Collections.Generic;
using LitMath;
using OtoCAD;

namespace lcdb.Optic;

/// <summary>
/// 双胶合透镜 (Cemented Doublet) — 2 片 OpticalLens 共享胶合面 (无空气间隙).
///
/// 最常见用途: 消色差透镜 (Achromat) — 冠玻璃 (低色散 N-BK7) + 火石玻璃 (高色散 F2).
///
/// 几何约定:
/// - <see cref="Position"/> = 整组的几何中心 (沿光轴, 总厚度中点)
/// - 沿 +X 方向: [前表面 R1] → [片 1 厚度 T1, 材料 1] → [胶合面 RContact] → [片 2 厚度 T2, 材料 2] → [后表面 R3]
/// - 通光直径 <see cref="Diameter"/> 两片共享 (胶合面 sag &lt; 半口径 ⇒ 物理可制)
/// - 符号: R&gt;0 中心在右 (ISO 10110-1)
/// </summary>
public sealed class CementedLens : Entity, IPierceable
{
    public override string className => "CementedLens";

    /// <inheritdoc/>
    public System.Collections.Generic.IEnumerable<Entity> GetPierceableSubEntities()
    {
        if (_markEntities.Count == 0) Generate();
        return _markEntities;
    }

    // -------- 几何 --------

    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>通光直径 (净口径, mm). 两片共享, 表面在此口径内弯曲.</summary>
    public double Diameter { get; set; } = 25.4;

    private double? _mechanicalDiameter;

    /// <summary>
    /// 机械外径 (mm). &gt; 通光直径时, 前后表面外侧画平肩 (land) 到机械边. null/≤通光 = 无肩.
    /// **物理约束: 机械外径不得小于净口径** —— 设入 ≤ Diameter 的值会被钳到 Diameter (即无肩).
    /// </summary>
    public double? MechanicalDiameter
    {
        // 读时钳到 ≥ Diameter (与赋值/反序列化顺序无关): 机械外径不可能小于净口径.
        get => _mechanicalDiameter.HasValue ? (double?)System.Math.Max(_mechanicalDiameter.Value, Diameter) : null;
        set => _mechanicalDiameter = value;
    }

    public double Rotation { get; set; } = 0.0;

    /// <summary>显示光轴 (双点画线, GB/T 13323-2009 §2.1).</summary>
    public bool ShowOpticalAxis { get; set; } = true;

    /// <summary>光轴两端延伸量 (mm).</summary>
    public double AxisPadding { get; set; } = 6.0;

    // -------- 自动标注显示开关 (默认全开 = 推荐标准集) --------
    // AutoDimensionLensCmd.Build 读这些开关决定生成哪些标注; 属性面板反射成复选框,
    // 取消勾选 → RefreshAutoDimensionsFor 重生成 → 对应标注实时消失.

    /// <summary>标 R1 前表面半径 (默认开).</summary>
    public bool ShowR1 { get; set; } = true;
    /// <summary>标 Rc 胶合面半径 (默认开).</summary>
    public bool ShowRc { get; set; } = true;
    /// <summary>标 R3 后表面半径 (默认开).</summary>
    public bool ShowR3 { get; set; } = true;
    /// <summary>标 中心厚度 d1/d2 (默认开).</summary>
    public bool ShowThickness { get; set; } = true;
    /// <summary>标 通光口径 Ø (默认开).</summary>
    public bool ShowDiameter { get; set; } = true;
    /// <summary>标 AR 镀膜符号 (默认开).</summary>
    public bool ShowCoating { get; set; } = true;

    /// <summary>片 1 (前→胶合面) 玻璃填充色 (null = 不填充).</summary>
    public Colors.Color? FillColor1 { get; set; }
    /// <summary>片 2 (胶合面→后) 玻璃填充色 (null = 不填充).</summary>
    public Colors.Color? FillColor2 { get; set; }

    /// <summary>玻璃剖面填充样式 (可选). 默认 <see cref="GlassPattern.Symbol"/> = GB 光学三斜线符号; 两片方向相反.</summary>
    public GlassPattern Pattern { get; set; } = GlassPattern.Symbol;

    /// <summary>前表面 (球面/非球面/...).</summary>
    public OpticalSurface FrontSurface { get; set; } = OpticalSurface.Sphere(60.0);
    /// <summary>胶合面 (= 片 1 后表面 = 片 2 前表面).</summary>
    public OpticalSurface ContactSurface { get; set; } = OpticalSurface.Sphere(-40.0);
    /// <summary>后表面.</summary>
    public OpticalSurface BackSurface { get; set; } = OpticalSurface.Sphere(-120.0);

    /// <summary>R1 = 前表面 Radius (向后兼容).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public double R1
    {
        get => FrontSurface.Radius;
        set
        {
            if (FrontSurface is SphericalSurface) FrontSurface.Radius = value;
            else FrontSurface = OpticalSurface.Sphere(value);
        }
    }
    /// <summary>RContact = 胶合面 Radius (向后兼容).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public double RContact
    {
        get => ContactSurface.Radius;
        set
        {
            if (ContactSurface is SphericalSurface) ContactSurface.Radius = value;
            else ContactSurface = OpticalSurface.Sphere(value);
        }
    }
    /// <summary>R3 = 后表面 Radius (向后兼容).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public double R3
    {
        get => BackSurface.Radius;
        set
        {
            if (BackSurface is SphericalSurface) BackSurface.Radius = value;
            else BackSurface = OpticalSurface.Sphere(value);
        }
    }

    /// <summary>片 1 中心厚度.</summary>
    public double T1 { get; set; } = 4.0;
    /// <summary>片 2 中心厚度.</summary>
    public double T2 { get; set; } = 2.5;

    // -------- 公差 (GB/T 13323-2009 §1.1) --------

    public Common.ToleranceValue? R1Tolerance { get; set; }
    public Common.ToleranceValue? RContactTolerance { get; set; }
    public Common.ToleranceValue? R3Tolerance { get; set; }
    public Common.ToleranceValue? DiameterTolerance { get; set; }
    public Common.ToleranceValue? T1Tolerance { get; set; }
    public Common.ToleranceValue? T2Tolerance { get; set; }

    // -------- 材料 --------

    public string Material1 { get; set; } = GlassLibrary.Default.Name;
    public double Nd1 { get; set; } = GlassLibrary.Default.Nd;
    public double Vd1 { get; set; } = GlassLibrary.Default.Vd;

    public string Material2 { get; set; } = "F2";
    public double Nd2 { get; set; } = 1.6200;
    public double Vd2 { get; set; } = 36.37;

    /// <summary>外部设计文件引用 + 自由 metadata (可选).</summary>
    public OpticalExtensions? Extensions { get; set; }

    /// <summary>从属标注 (CoatingMark / SurfaceQualityMark / Dimension 等). 跟随 lens 移动/旋转/克隆. V4 起持久化.</summary>
    public List<Entity> OwnedAnnotations { get; set; } = new();

    public void AddOwnedAnnotation(Entity ann)
    {
        if (ann is null) return;
        OwnedAnnotations.Add(ann);
        _markEntities.Clear();
    }

    public void ApplyMaterial1(string name)
    {
        Material1 = name;
        var g = GlassLibrary.Find(name);
        if (g is not null) { Nd1 = g.Nd; Vd1 = g.Vd; }
    }

    public void ApplyMaterial2(string name)
    {
        Material2 = name;
        var g = GlassLibrary.Find(name);
        if (g is not null) { Nd2 = g.Nd; Vd2 = g.Vd; }
    }

    /// <summary>消色差预设: N-BK7 + F2, R1=+60, RContact=-40, R3=-120, T1=4, T2=2.5.</summary>
    public void ApplyAchromatPreset()
    {
        R1 = 60.0; RContact = -40.0; R3 = -120.0;
        T1 = 4.0; T2 = 2.5;
        ApplyMaterial1("N-BK7");
        ApplyMaterial2("F2");
    }

    // -------- 渲染缓存 --------

    private List<Entity> _markEntities = new();

    public override Bounding bounding
    {
        get
        {
            double totalT = T1 + T2;
            double lensNetHalf = Diameter * 0.5;
            double fH = OpticSurfaceGeometry.DrawnSemiAperture(FrontSurface, lensNetHalf);
            double bH = OpticSurfaceGeometry.DrawnSemiAperture(BackSurface, lensNetHalf);
            double cH = OpticSurfaceGeometry.DrawnSemiAperture(ContactSurface, lensNetHalf);
            double mechHalf = MechanicalDiameter.HasValue ? MechanicalDiameter.Value * 0.5 : 0.0;
            mechHalf = Math.Max(mechHalf, Math.Max(fH, Math.Max(cH, bH)));
            double leftPad  = Math.Max(0, -FrontSurface.Sag(fH));
            double rightPad = Math.Max(0,  BackSurface.Sag(bH));
            var min = new Vector2(Position.X - totalT * 0.5 - leftPad, Position.Y - mechHalf);
            var max = new Vector2(Position.X + totalT * 0.5 + rightPad, Position.Y + mechHalf);
            return new Bounding(min, max);
        }
    }

    private void Generate()
    {
        _markEntities.Clear();

        // 逐面净口径: 每面弯到自己的 SemiAperture (无则用 Diameter/2), 再钳到该面 |R|.
        double lensNetHalf  = Diameter * 0.5;
        double frontHalf    = OpticSurfaceGeometry.DrawnSemiAperture(FrontSurface, lensNetHalf);
        double contactHalf  = OpticSurfaceGeometry.DrawnSemiAperture(ContactSurface, lensNetHalf);
        double backHalf     = OpticSurfaceGeometry.DrawnSemiAperture(BackSurface, lensNetHalf);
        // 机械外径 (OD) = 机械直径/2 与各面净口径取最大, 包住所有面.
        double mechHalf = MechanicalDiameter.HasValue ? MechanicalDiameter.Value * 0.5 : 0.0;
        mechHalf = Math.Max(mechHalf, Math.Max(frontHalf, Math.Max(contactHalf, backHalf)));
        double totalT = T1 + T2;

        // 顶点 X (沿光轴): 整体居中 Position.X
        double frontApexX   = Position.X - totalT * 0.5;
        double contactApexX = frontApexX + T1;
        double backApexX    = contactApexX + T2;

        // 各面肩根 X (表面在自身净口径处)
        double frontEdgeX   = frontApexX   + FrontSurface.Sag(frontHalf);
        double contactEdgeX = contactApexX + ContactSurface.Sag(contactHalf);
        double backEdgeX    = backApexX    + BackSurface.Sag(backHalf);

        double odT  = Position.Y + mechHalf, odB = Position.Y - mechHalf;

        // OD 上/下边线 — 每片各一段 (胶合面处分段).
        _markEntities.Add(new Line(new Vector2(frontEdgeX,   odT), new Vector2(contactEdgeX, odT)) { color = color });
        _markEntities.Add(new Line(new Vector2(contactEdgeX, odT), new Vector2(backEdgeX,    odT)) { color = color });
        _markEntities.Add(new Line(new Vector2(frontEdgeX,   odB), new Vector2(contactEdgeX, odB)) { color = color });
        _markEntities.Add(new Line(new Vector2(contactEdgeX, odB), new Vector2(backEdgeX,    odB)) { color = color });

        // 平肩 (land): 各面从自身净口径边竖直到机械外径.
        AddLand(frontEdgeX,   frontHalf,   mechHalf);
        AddLand(contactEdgeX, contactHalf, mechHalf);
        AddLand(backEdgeX,    backHalf,    mechHalf);

        // 3 表面弧 — 各只在自身净口径内弯曲
        FrontSurface.AppendRenderEntities(_markEntities, frontApexX, Position.Y, frontHalf, color);
        ContactSurface.AppendRenderEntities(_markEntities, contactApexX, Position.Y, contactHalf, color);
        BackSurface.AppendRenderEntities(_markEntities, backApexX, Position.Y, backHalf, color);

        // 光轴 (双点画线, GB/T 13323-2009 §2.1) — 贯穿整组
        if (ShowOpticalAxis)
        {
            double pad = Math.Max(AxisPadding, 0);
            double xLeft  = Math.Min(frontApexX, frontEdgeX) - pad;
            double xRight = Math.Max(backApexX,  backEdgeX)  + pad;
            _markEntities.Add(new Line(
                new Vector2(xLeft,  Position.Y),
                new Vector2(xRight, Position.Y))
            {
                // 光轴: 细灰长点画线 (与 OpticalLens 一致)
                color = Colors.Color.FromRGB(0x66, 0x66, 0x66),
                lineType = LineType.DashDotDot,
            });
        }

        // Rotation (绕 Position)
        if (Rotation != 0)
        {
            double rad = Rotation * Math.PI / 180.0;
            foreach (var ent in _markEntities) ent.Rotate(Position, rad);
        }
    }

    /// <summary>画某面平肩 (land): 净口径边 (±surfHalf) 竖直到机械外径 (±mechHalf), 等 X.</summary>
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

        // 双片玻璃剖面 (轮廓之前). 片 1 = 前表面→胶合面, 片 2 = 胶合面→后表面.
        // FillColor1/2 实心底色与 Pattern 线纹相互独立; 两片线纹方向相反 (GB 相邻不同材料惯例).
        if (Pattern != GlassPattern.None || FillColor1.HasValue || FillColor2.HasValue)
        {
            double halfT = (T1 + T2) * 0.5;
            double frontApexX   = Position.X - halfT;
            double contactApexX = frontApexX + T1;
            double backApexX    = contactApexX + T2;
            var prev = gd.CurrentColor;
            var poly1 = BuildElementPolygon(frontApexX, FrontSurface, contactApexX, ContactSurface, 40);
            var poly2 = BuildElementPolygon(contactApexX, ContactSurface, backApexX, BackSurface, 40);
            if (FillColor1.HasValue)
            {
                gd.CurrentColor = FillColor1.Value.ToDrawingColor();
                gd.DrawFilledPolygon(poly1);
            }
            if (FillColor2.HasValue)
            {
                gd.CurrentColor = FillColor2.Value.ToDrawingColor();
                gd.DrawFilledPolygon(poly2);
            }
            gd.CurrentColor = prev;
            // 两片方向相反 (+45° / -45°): 相邻不同材料用反向剖面线区分.
            foreach (var (a, b) in OpticSurfaceGeometry.GlassPatternSegments(poly1, Pattern, Diameter, Math.PI / 4))
                new Line(a, b) { color = OpticSurfaceGeometry.GlassHatch }.Draw(gd);
            foreach (var (a, b) in OpticSurfaceGeometry.GlassPatternSegments(poly2, Pattern, Diameter, -Math.PI / 4))
                new Line(a, b) { color = OpticSurfaceGeometry.GlassHatch }.Draw(gd);
        }

        foreach (var e in _markEntities) e.Draw(gd);
        foreach (var ann in OwnedAnnotations) ann.Draw(gd);
    }

    /// <summary>
    /// 一片玻璃的闭合剖面多边形 (绝对坐标, 已应用 Rotation):
    /// 左表面 top→bottom + 右表面 bottom→top.
    /// </summary>
    private List<Vector2> BuildElementPolygon(
        double leftApexX, OpticalSurface leftSurf,
        double rightApexX, OpticalSurface rightSurf, int seg)
    {
        if (seg < 2) seg = 2;
        // 机械外径 (OD, 镜片级): 各面净口径与机械直径取最大. 各面在自身净口径内弯, 外侧平肩到 OD.
        double lensNetHalf = Diameter * 0.5;
        double fH = OpticSurfaceGeometry.DrawnSemiAperture(FrontSurface, lensNetHalf);
        double cH = OpticSurfaceGeometry.DrawnSemiAperture(ContactSurface, lensNetHalf);
        double bH = OpticSurfaceGeometry.DrawnSemiAperture(BackSurface, lensNetHalf);
        double mechHalf = MechanicalDiameter.HasValue ? MechanicalDiameter.Value * 0.5 : 0.0;
        mechHalf = Math.Max(mechHalf, Math.Max(fH, Math.Max(cH, bH)));
        double leftHalf  = OpticSurfaceGeometry.DrawnSemiAperture(leftSurf, lensNetHalf);
        double rightHalf = OpticSurfaceGeometry.DrawnSemiAperture(rightSurf, lensNetHalf);
        var pts = new List<Vector2>(seg * 2 + 2);
        for (int i = 0; i <= seg; i++)   // 左表面 +mechHalf → -mechHalf (净口径外为平肩)
        {
            double h = mechHalf - 2.0 * mechHalf * i / seg;
            double hc = Math.Min(Math.Abs(h), leftHalf);
            pts.Add(new Vector2(leftApexX + leftSurf.Sag(hc), Position.Y + h));
        }
        for (int i = 0; i <= seg; i++)   // 右表面 -mechHalf → +mechHalf
        {
            double h = -mechHalf + 2.0 * mechHalf * i / seg;
            double hc = Math.Min(Math.Abs(h), rightHalf);
            pts.Add(new Vector2(rightApexX + rightSurf.Sag(hc), Position.Y + h));
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

    public override void InvalidateRenderCache() => _markEntities.Clear();

    protected override DBObject CreateInstance() => new CementedLens();

    public override object Clone()
    {
        var c = (CementedLens)base.Clone();
        c.Position = Position;
        c.Diameter = Diameter;
        c.MechanicalDiameter = MechanicalDiameter;
        c.Rotation = Rotation;
        c.ShowOpticalAxis = ShowOpticalAxis;
        c.AxisPadding = AxisPadding;
        c.ShowR1 = ShowR1;
        c.ShowRc = ShowRc;
        c.ShowR3 = ShowR3;
        c.ShowThickness = ShowThickness;
        c.ShowDiameter = ShowDiameter;
        c.ShowCoating = ShowCoating;
        c.FillColor1 = FillColor1;
        c.FillColor2 = FillColor2;
        c.Pattern = Pattern;
        c.FrontSurface = FrontSurface.Clone();
        c.ContactSurface = ContactSurface.Clone();
        c.BackSurface = BackSurface.Clone();
        c.T1 = T1; c.T2 = T2;
        c.R1Tolerance = R1Tolerance?.Clone();
        c.RContactTolerance = RContactTolerance?.Clone();
        c.R3Tolerance = R3Tolerance?.Clone();
        c.DiameterTolerance = DiameterTolerance?.Clone();
        c.T1Tolerance = T1Tolerance?.Clone();
        c.T2Tolerance = T2Tolerance?.Clone();
        c.Material1 = Material1; c.Nd1 = Nd1; c.Vd1 = Vd1;
        c.Material2 = Material2; c.Nd2 = Nd2; c.Vd2 = Vd2;
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
        foreach (var ann in OwnedAnnotations) ann.TransformBy(transform);
        _markEntities.Clear();
    }

    public override List<GripPoint> GetGripPoints()
    {
        var halfT = (T1 + T2) * 0.5;
        var halfD = Diameter * 0.5;
        return new List<GripPoint>
        {
            new(GripPointType.Center, Position),
            new(GripPointType.Corner, Position + new Vector2(0,      +halfD)),
            new(GripPointType.Corner, Position + new Vector2(0,      -halfD)),
            new(GripPointType.Corner, Position + new Vector2(-halfT, 0)),
            new(GripPointType.Corner, Position + new Vector2(+halfT, 0)),
        };
    }

    public override List<ObjectSnapPoint> GetSnapPoints()
    {
        var halfT = (T1 + T2) * 0.5;
        double hD = Diameter * 0.5;
        double feX = FrontSurface.Sag(hD);   // 前表面净口径处矢高 → 前边缘角 X 偏移
        double beX = BackSurface.Sag(hD);    // 后表面
        return new List<ObjectSnapPoint>
        {
            new(ObjectSnapMode.Center, Position),
            new(ObjectSnapMode.End, Position + new Vector2(-halfT, 0)),       // 1 前顶
            new(ObjectSnapMode.End, Position + new Vector2(+halfT, 0)),       // 2 后顶
            new(ObjectSnapMode.End, Position + new Vector2(-halfT + T1, 0)),  // 3 胶合面顶点
            new(ObjectSnapMode.End, Position + new Vector2(0, +hD)),          // 4 口径上
            new(ObjectSnapMode.End, Position + new Vector2(0, -hD)),          // 5 口径下
            new(ObjectSnapMode.End, Position + new Vector2(-halfT + feX, +hD)),  // 6 前上角
            new(ObjectSnapMode.End, Position + new Vector2(-halfT + feX, -hD)),  // 7 前下角
            new(ObjectSnapMode.End, Position + new Vector2(+halfT + beX, +hD)),  // 8 后上角
            new(ObjectSnapMode.End, Position + new Vector2(+halfT + beX, -hD)),  // 9 后下角
        };
    }

    public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
    {
        switch (index)
        {
            case 0: Position = newPosition; break;
            case 1:
            case 2:
                Diameter = Math.Max(0.5, Math.Abs(newPosition.Y - Position.Y) * 2);
                break;
            case 3:
            case 4:
                // 拖 X 端点等比缩放 T1 / T2
                double newHalf = Math.Max(0.2, Math.Abs(newPosition.X - Position.X));
                double oldHalf = (T1 + T2) * 0.5;
                double ratio = newHalf / oldHalf;
                T1 *= ratio;
                T2 *= ratio;
                break;
        }
        _markEntities.Clear();
    }
}
