using System;
using LitMath;
using lcdb;
using lcdb.Annotation;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// 贴面标记放置命令 (GB/ISO 表面/工艺标注通用):
/// 与 PlaceMarkCmd 一样单点放置, 但当鼠标停在某条面 (Line/Arc/Circle, 含透镜表面) 上、
/// 且工厂产出的标记实现了 <see cref="ISurfaceAttachable"/> 时, 自动调用 AttachToSurface
/// 让标记贴到面上 (符号坐在面上、沿外法线竖立)。空白处或非贴面标记则退化为竖直放置。
///
/// 外法线方向取自「原始光标 → 吸附点」的偏移 (Nearest snap 把光标投影到面上, 残差正好沿法线),
/// 因此符号总是出现在用户悬停的那一侧。
/// </summary>
public sealed class PlaceSurfaceMarkCmd : ICadCommand
{
    // 仅判断"吸附点是否落在某条面上"。Nearest snap 已把点投影到面上 (残差≈0),
    // 故用很小的容差; 太大会在光标只是"靠近"面、并未吸附时误触发贴面 (符号悬空)。
    private const double PickTolerance = 0.5;

    private readonly Func<Vector2, Entity> _factory;
    private readonly string _displayName;
    private ICadCommandHost? _host;

    public PlaceSurfaceMarkCmd(string displayName, Func<Vector2, Entity> factory)
    {
        _displayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public string Name => _displayName;

    /// <summary>Mark 应贴在实体边上, 只 snap 到 Nearest (实体边最近点)。</summary>
    public OtoCAD.Avalonia.Snap.SnapType? PreferredSnapTypes => OtoCAD.Avalonia.Snap.SnapType.Nearest;

    public void Start(ICadCommandHost host)
    {
        _host = host;
        host.SetPrompt($"[{_displayName}] 移到面上自动贴面, 单击放置 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        var mark = Build(p);
        _host.AddEntity(mark);
        _host.SetPreview(null);
        _host.SetPrompt($"[{_displayName}] 已放置");
        _host.FinishCommand();
    }

    public void OnMouseMove(Vector2 p)
    {
        if (_host is null) return;
        try { _host.SetPreview(Build(p)); }
        catch { _host.SetPreview(null); }
    }

    public void Cancel()
    {
        _host?.SetPreview(null);
        _host?.SetPrompt($"[{_displayName}] 已取消");
        _host?.FinishCommand();
    }

    /// <summary>
    /// 在吸附点 p 处构造标记。若 p 落在某条面上且标记可贴面, 则调用 AttachToSurface 贴到面上;
    /// 否则保持工厂的默认放置 (Center/Position = p)。
    /// </summary>
    private Entity Build(Vector2 p)
    {
        var mark = _factory(p);
        if (mark is ISurfaceAttachable attachable)
        {
            var normal = OutwardNormalAt(p);
            if (normal.HasValue)
                attachable.AttachToSurface(p, normal.Value);
        }
        return mark;
    }

    /// <summary>
    /// 求吸附点 p 处被测面的外法线 (单位向量); p 不在任何面上时返回 null。
    /// 优先用「原始光标 - 吸附点」(自然沿法线、且指向用户所在一侧);
    /// 退化时 (光标恰在面上) 用面的几何法线兜底。
    /// </summary>
    private Vector2? OutwardNormalAt(Vector2 p)
    {
        if (_host is null) return null;
        var ent = _host.PickEntityAt(p, PickTolerance, pierce: true);
        if (ent is null) return null;  // 空白处 → 不贴面

        var d = _host.RawCursorModel - p;
        if (d.length > 1e-6) return d.normalized;

        // 退化: 光标恰好压在面上, 改用面的几何法线
        switch (ent)
        {
            case Arc a:    return (p - a.center).normalized;
            case Circle c: return (p - c.center).normalized;
            case Line ln:
                var t = (ln.endPoint - ln.startPoint).normalized;
                return new Vector2(-t.Y, t.X);  // 左法线 (任取一侧)
            default:       return new Vector2(0, 1);
        }
    }
}
