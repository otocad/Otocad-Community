using System;
using System.Collections.Generic;
using LitMath;
using OtoCAD;

namespace lcdb.Optic;

/// <summary>
/// 通用透镜系统 — 沿光轴排列多个 OpticalLens, 之间有空气间隙.
/// 适合: 物镜组 / 目镜组 / Cooke 三胶合 / 等大型光学系统的截面图.
///
/// 几何约定:
/// - <see cref="AxisStart"/> = 光轴起点 (沿 +X 方向延伸; 旋转通过 <see cref="AxisAngle"/> 调整)
/// - 每个 <see cref="AssemblyItem"/> 含 GapBefore (前空气间隙) + Lens (OpticalLens 模板)
/// - 累加: lens[0].Pos.X = AxisStart.X + items[0].GapBefore + lens[0].Thickness/2
///         lens[i].Pos.X = lens[i-1].Pos.X + lens[i-1].Thickness/2 + items[i].GapBefore + lens[i].Thickness/2
///         lens[i].Pos.Y = AxisStart.Y
/// - 选中放置/移动/旋转 整组级联
/// </summary>
public sealed class OpticalAssembly : Entity
{
    public override string className => "OpticalAssembly";

    /// <summary>光轴起点 (第一片镜前空气间隙开始处).</summary>
    public Vector2 AxisStart { get; set; } = Vector2.Zero;

    /// <summary>光轴方向 (度, 0=水平 +X 方向, 逆时针为正).</summary>
    public double AxisAngle { get; set; } = 0.0;

    /// <summary>沿光轴排列的镜片项 (有顺序).</summary>
    public List<AssemblyItem> Items { get; set; } = new();

    /// <summary>显示光轴 (点划线, 贯穿整组).</summary>
    public bool ShowAxis { get; set; } = true;

    /// <summary>是否将每个透镜的 Position 同步到 AxisStart-Items 派生位置 (Generate 时强制同步).</summary>
    private bool _autoLayout = true;

    private List<Entity> _markEntities = new();

    public override Bounding bounding
    {
        get
        {
            // 累加: 总长度 = Σ (gap + thickness); 高 = 最大 diameter
            double totalLen = 0;
            double maxHalfD = 0;
            foreach (var it in Items)
            {
                totalLen += Math.Max(0, it.GapBefore);
                if (it.Lens is not null)
                {
                    totalLen += it.Lens.Thickness;
                    maxHalfD = Math.Max(maxHalfD, it.Lens.Diameter * 0.5);
                }
            }
            if (maxHalfD == 0) maxHalfD = 12.7;  // 默认半径 (空 assembly)

            // 暂不考虑 Rotation, 取轴对齐 bounding (旋转后渲染时实体已转, 这个 bounding 略宽估算 OK)
            var min = new Vector2(AxisStart.X - 2, AxisStart.Y - maxHalfD - 2);
            var max = new Vector2(AxisStart.X + Math.Max(totalLen, 10) + 2, AxisStart.Y + maxHalfD + 2);
            return new Bounding(min, max);
        }
    }

    private void Generate()
    {
        _markEntities.Clear();
        if (Items.Count == 0) return;

        // 累计沿 +X 方向布局 (Rotation 在最后一次性绕 AxisStart 转)
        double cursor = AxisStart.X;
        double y = AxisStart.Y;
        double maxHalfD = 0;

        foreach (var item in Items)
        {
            cursor += Math.Max(0, item.GapBefore);
            if (item.Lens is null) continue;

            var lens = item.Lens;
            if (_autoLayout)
            {
                lens.Position = new Vector2(cursor + lens.Thickness * 0.5, y);
            }
            lens.Rotation = 0;  // 整组在最后一起转, lens 本身不带方向

            cursor += lens.Thickness;
            maxHalfD = Math.Max(maxHalfD, lens.Diameter * 0.5);

            // 渲染 lens (走 OpticalLens.Generate 内含 _markEntities)
            // 把 lens 自己渲染的 entities 提取到本 assembly
            ExtractLensRenderEntities(lens);
        }

        // 光轴 (双点画线, GB/T 13323-2009 §2.1) — 贯穿整组
        if (ShowAxis && maxHalfD > 0)
        {
            double axisPad = 5;
            _markEntities.Add(new Line(
                new Vector2(AxisStart.X - axisPad, y),
                new Vector2(cursor + axisPad, y))
            {
                color = color,
                lineType = LineType.DashDotDot,
            });
        }

        // Rotation: 绕 AxisStart 转
        if (AxisAngle != 0)
        {
            double rad = AxisAngle * Math.PI / 180.0;
            foreach (var ent in _markEntities) ent.Rotate(AxisStart, rad);
        }
    }

    /// <summary>调 lens.Draw 时它自己更新 _markEntities, 取出加到本 assembly 的渲染列表.</summary>
    private void ExtractLensRenderEntities(OpticalLens lens)
    {
        // OpticalLens 的私有 _markEntities 不可访问. 用 reflection 强制触发 Generate
        // 然后通过本 assembly 的 GD recorder 抓.
        // 简化: 直接 clear lens cache + 调它的 Generate 反射, 然后读 field.
        try
        {
            // Assembly 自己画整组的光轴 (一条贯穿), 抑制每片透镜各自画一条 — 避免重影
            bool prevShowAxis = lens.ShowOpticalAxis;
            lens.ShowOpticalAxis = false;
            try
            {
                var t = typeof(OpticalLens);
                var gen = t.GetMethod("Generate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                gen?.Invoke(lens, null);
                var fld = t.GetField("_markEntities", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fld?.GetValue(lens) is List<Entity> sub)
                {
                    foreach (var e in sub) _markEntities.Add(e);
                }
            }
            finally
            {
                lens.ShowOpticalAxis = prevShowAxis;
            }
        }
        catch { }
    }

    public override void Draw(IGraphicsDraw gd)
    {
        if (_markEntities.Count == 0) Generate();
        foreach (var e in _markEntities) e.Draw(gd);
    }

    // -------- Entity 重写 --------

    public override void InvalidateRenderCache()
    {
        _markEntities.Clear();
        // 级联: 子镜片自己的缓存也清掉, 下次 Generate 时一并重算
        foreach (var it in Items) it.Lens?.InvalidateRenderCache();
    }

    protected override DBObject CreateInstance() => new OpticalAssembly();

    public override object Clone()
    {
        var c = (OpticalAssembly)base.Clone();
        c.AxisStart = AxisStart;
        c.AxisAngle = AxisAngle;
        c.ShowAxis = ShowAxis;
        c.Items = new List<AssemblyItem>(Items.Count);
        foreach (var it in Items)
        {
            c.Items.Add(new AssemblyItem
            {
                GapBefore = it.GapBefore,
                Lens = it.Lens is null ? null : (OpticalLens)it.Lens.Clone(),
            });
        }
        c._markEntities = new List<Entity>();
        return c;
    }

    public override void Translate(Vector2 translation)
    {
        AxisStart += translation;
        _markEntities.Clear();
    }

    public override void Rotate(Vector2 center, double angle)
    {
        AxisStart = Vector2.RotateInRadian(AxisStart, center, angle);
        AxisAngle += angle * 180.0 / Math.PI;
        _markEntities.Clear();
    }

    public override void TransformBy(Matrix3 transform)
    {
        AxisStart = transform * AxisStart;
        _markEntities.Clear();
    }

    public override List<GripPoint> GetGripPoints()
    {
        return new List<GripPoint>
        {
            new(GripPointType.Center, AxisStart),
        };
    }

    public override List<ObjectSnapPoint> GetSnapPoints()
    {
        return new List<ObjectSnapPoint>
        {
            new(ObjectSnapMode.End, AxisStart),
        };
    }

    public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
    {
        if (index == 0)
        {
            AxisStart = newPosition;
            _markEntities.Clear();
        }
    }
}

/// <summary>OpticalAssembly 的一个槽位: 前空气间隙 + 一个 OpticalLens.</summary>
public sealed class AssemblyItem
{
    /// <summary>距前一镜后表面 (或 AxisStart) 的空气间隙 (mm). 0 = 紧挨, &lt;0 视为 0.</summary>
    public double GapBefore { get; set; } = 5.0;

    /// <summary>该槽位的透镜 (null = 空槽, 渲染时跳过).</summary>
    public OpticalLens? Lens { get; set; }
}
