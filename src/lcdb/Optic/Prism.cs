using System;
using System.Collections.Generic;
using LitMath;
using OtoCAD;

namespace lcdb.Optic;

/// <summary>
/// 棱镜类型 (P0 只实做 RightAngle, 其余 P1).
/// </summary>
public enum PrismType
{
    /// <summary>直角棱镜 — 1 个 90° 角, 两腰相等; 用于全反射/折叠光路.</summary>
    RightAngle,
    /// <summary>屋脊棱镜 — 在直角棱镜斜面上加屋脊; 像翻转用.</summary>
    Roof,
    /// <summary>道威棱镜 — 用于像旋转.</summary>
    Dove,
    /// <summary>五角棱镜 — 90° 偏转, 不翻转像.</summary>
    Penta,
}

/// <summary>
/// 棱镜实体 (2D 剖面表示) — GB/T 13323-2009 §3 与透镜并列.
///
/// 几何约定 (RightAngle):
/// - <see cref="Position"/> = 剖面 bounding box 中心
/// - 剖面三角形:
///     A = Position + (-W/2, -H/2)  直角顶点 (左下)
///     B = Position + ( W/2, -H/2)  底边右端
///     C = Position + (-W/2,  H/2)  竖边上端
///   斜边 BC.
/// - <see cref="Width"/> = 底边长 (沿 +X), <see cref="Height"/> = 竖边高 (沿 +Y)
/// - <see cref="Rotation"/> 度 (绕 Position 转剖面)
///
/// 渲染缓存模式 (同 <see cref="OpticalLens"/>): _markEntities + InvalidateRenderCache.
/// </summary>
public sealed class Prism : Entity
{
    public override string className => "Prism";

    // -------- 几何 --------

    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>底边长 (mm), 沿 +X.</summary>
    public double Width { get; set; } = 20.0;

    /// <summary>竖边高 (mm), 沿 +Y. 直角棱镜常 Width = Height (45°).</summary>
    public double Height { get; set; } = 20.0;

    /// <summary>棱镜类型 (P0 只 RightAngle 渲染, 其余 fallback).</summary>
    public PrismType Type { get; set; } = PrismType.RightAngle;

    /// <summary>剖面旋转 (度, 绕 Position).</summary>
    public double Rotation { get; set; } = 0.0;

    // -------- 材料 (沿用 GlassLibrary) --------

    public string MaterialName { get; set; } = GlassLibrary.Default.Name;
    public double RefractiveIndex { get; set; } = GlassLibrary.Default.Nd;
    public double AbbeNumber { get; set; } = GlassLibrary.Default.Vd;

    public void ApplyMaterial(string name)
    {
        MaterialName = name;
        var g = GlassLibrary.Find(name);
        if (g is not null) { RefractiveIndex = g.Nd; AbbeNumber = g.Vd; }
    }

    // -------- 公差 (T3 复用) --------

    public Common.ToleranceValue? WidthTolerance { get; set; }
    public Common.ToleranceValue? HeightTolerance { get; set; }
    /// <summary>θⅠ (入射角公差, P1 给标注用; P0 暂不渲染).</summary>
    public Common.ToleranceValue? Theta1Tolerance { get; set; }

    // -------- 缓存 --------

    private List<Entity> _markEntities = new();

    public override Bounding bounding
    {
        get
        {
            double halfW = Width * 0.5;
            double halfH = Height * 0.5;
            // Rotation 时取轴对齐扩展 (略宽估算)
            double pad = Math.Sqrt(halfW * halfW + halfH * halfH);
            return new Bounding(
                new Vector2(Position.X - pad, Position.Y - pad),
                new Vector2(Position.X + pad, Position.Y + pad));
        }
    }

    private void Generate()
    {
        _markEntities.Clear();

        double halfW = Width * 0.5;
        double halfH = Height * 0.5;

        // P0: 默认 RightAngle 三角形. 其余类型先渲染同形作为占位 (P1 再细化).
        var A = new Vector2(Position.X - halfW, Position.Y - halfH);  // 直角
        var B = new Vector2(Position.X + halfW, Position.Y - halfH);
        var C = new Vector2(Position.X - halfW, Position.Y + halfH);

        switch (Type)
        {
            case PrismType.RightAngle:
            default:
                _markEntities.Add(new Line(A, B) { color = color });  // 底边
                _markEntities.Add(new Line(A, C) { color = color });  // 竖边
                _markEntities.Add(new Line(B, C) { color = color });  // 斜边
                break;
        }

        if (Rotation != 0)
        {
            double rad = Rotation * Math.PI / 180.0;
            foreach (var ent in _markEntities) ent.Rotate(Position, rad);
        }
    }

    public override void Draw(IGraphicsDraw gd)
    {
        if (_markEntities.Count == 0) Generate();
        foreach (var e in _markEntities) e.Draw(gd);
    }

    // -------- Entity 覆写 --------

    public override void InvalidateRenderCache() => _markEntities.Clear();

    protected override DBObject CreateInstance() => new Prism();

    public override object Clone()
    {
        var c = (Prism)base.Clone();
        c.Position = Position;
        c.Width = Width;
        c.Height = Height;
        c.Type = Type;
        c.Rotation = Rotation;
        c.MaterialName = MaterialName;
        c.RefractiveIndex = RefractiveIndex;
        c.AbbeNumber = AbbeNumber;
        c.WidthTolerance = WidthTolerance?.Clone();
        c.HeightTolerance = HeightTolerance?.Clone();
        c.Theta1Tolerance = Theta1Tolerance?.Clone();
        c._markEntities = new List<Entity>();
        return c;
    }

    public override void Translate(Vector2 translation)
    {
        Position += translation;
        _markEntities.Clear();
    }

    public override void Rotate(Vector2 center, double angle)
    {
        Position = Vector2.RotateInRadian(Position, center, angle);
        Rotation += angle * 180.0 / Math.PI;
        _markEntities.Clear();
    }

    public override void TransformBy(Matrix3 transform)
    {
        Position = transform * Position;
        _markEntities.Clear();
    }

    public override List<GripPoint> GetGripPoints()
    {
        double halfW = Width * 0.5;
        double halfH = Height * 0.5;
        return new List<GripPoint>
        {
            new(GripPointType.Center, Position),
            new(GripPointType.Corner, Position + new Vector2(-halfW, -halfH)),  // 直角顶点
            new(GripPointType.Corner, Position + new Vector2( halfW, -halfH)),  // 底边右端 (改 Width)
            new(GripPointType.Corner, Position + new Vector2(-halfW,  halfH)),  // 竖边上端 (改 Height)
        };
    }

    public override List<ObjectSnapPoint> GetSnapPoints()
    {
        double halfW = Width * 0.5;
        double halfH = Height * 0.5;
        return new List<ObjectSnapPoint>
        {
            new(ObjectSnapMode.Center, Position),
            new(ObjectSnapMode.End, Position + new Vector2(-halfW, -halfH)),
            new(ObjectSnapMode.End, Position + new Vector2( halfW, -halfH)),
            new(ObjectSnapMode.End, Position + new Vector2(-halfW,  halfH)),
        };
    }

    public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
    {
        double halfW = Width * 0.5;
        double halfH = Height * 0.5;
        switch (index)
        {
            case 0:
                // Center grip → 整体平移
                Position = newPosition;
                break;
            case 1:
                // 直角顶点 (左下) — 拖动 = 整体平移 Position, 保持 W/H 不变
                Position = newPosition + new Vector2(halfW, halfH);
                break;
            case 2:
                // 底边右端 — 改 Width (相对 Position 沿 +X 的距离 ×2)
                Width = Math.Max(2.0, (newPosition.X - Position.X) * 2);
                break;
            case 3:
                // 竖边上端 — 改 Height
                Height = Math.Max(2.0, (newPosition.Y - Position.Y) * 2);
                break;
        }
        _markEntities.Clear();
    }
}
