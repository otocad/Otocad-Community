using System;
using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// 对齐标注 — 推荐 selection-based: 点 Line → 一键创建, 默认 8mm offset.
/// 没拾到 Line → 退回 3 点输入 (P1 + P2 + dimLine).
/// </summary>
public sealed class AlignedDimensionCmd : ICadCommand
{
    private const double PickTolerance = 8.0;
    private const double DefaultOffset = 8.0;
    private enum State { AwaitingLineOrP1, AwaitingP2, AwaitingDimLine }
    private State _state = State.AwaitingLineOrP1;
    private Vector2 _p1, _p2;
    private ICadCommandHost? _host;
    public string Name => "对齐标注";

    public OtoCAD.Avalonia.Snap.SnapType? PreferredSnapTypes =>
        _state == State.AwaitingLineOrP1
            ? OtoCAD.Avalonia.Snap.SnapType.Nearest | OtoCAD.Avalonia.Snap.SnapType.Endpoint | OtoCAD.Avalonia.Snap.SnapType.Center
            : OtoCAD.Avalonia.Snap.SnapType.Endpoint | OtoCAD.Avalonia.Snap.SnapType.Center;

    public void Start(ICadCommandHost host)
    {
        _host = host; _state = State.AwaitingLineOrP1;
        host.SetPrompt("[对齐标注] 点 Line 一键标注, 或依次指定 2 端点 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        switch (_state)
        {
            case State.AwaitingLineOrP1:
                {
                    var picked = _host.PickEntityAt(p, tolerance: PickTolerance, pierce: true);
                    if (picked is Line ln && AutoDimFromLine(ln, p)) return;
                    // fallback: 用 p 当 P1
                    _p1 = p; _state = State.AwaitingP2;
                    _host.SetPrompt("[对齐标注] 请指定第二点");
                    return;
                }
            case State.AwaitingP2:
                _p2 = p; _state = State.AwaitingDimLine;
                _host.SetPrompt("[对齐标注] 请指定尺寸线位置");
                return;
            case State.AwaitingDimLine:
                _host.AddEntity(new AlignedDimension(_p1, _p2, p));
                _host.SetPreview(null); _host.SetPrompt("[对齐标注] 已创建");
                _host.FinishCommand();
                return;
        }
    }

    private bool AutoDimFromLine(Line ln, Vector2 clickPos)
    {
        if (_host is null) return false;
        var p1 = ln.startPoint;
        var p2 = ln.endPoint;
        var dir = (p2 - p1);
        if (dir.length < 1e-9) return false;
        var perp = new Vector2(-dir.normalized.Y, dir.normalized.X);
        var mid = (p1 + p2) * 0.5;
        double side = Math.Sign(Vector2.Dot(clickPos - mid, perp));
        if (side == 0) side = 1;
        Vector2 dimLinePos = mid + perp * (side * DefaultOffset);
        _host.AddEntity(new AlignedDimension(p1, p2, dimLinePos));
        _host.SetPreview(null);
        _host.SetPrompt($"[对齐标注] 已创建, 长度={dir.length:F2}, 拖中点 grip 可调整位置");
        _host.FinishCommand();
        return true;
    }

    public void OnMouseMove(Vector2 p)
    {
        if (_state == State.AwaitingDimLine && _host is not null)
            try { _host.SetPreview(new AlignedDimension(_p1, _p2, p)); } catch { }
    }
    public void Cancel() { _host?.SetPreview(null); _host?.SetPrompt("[对齐标注] 已取消"); _host?.FinishCommand(); }
}

/// <summary>
/// 线性标注 — 同对齐标注的 selection-based 一键模式; 当前底层走 AlignedDimension 实现.
/// </summary>
public sealed class LinearDimensionCmd : ICadCommand
{
    private const double PickTolerance = 8.0;
    private const double DefaultOffset = 8.0;
    private enum State { AwaitingLineOrP1, AwaitingP2, AwaitingDimLine }
    private State _state = State.AwaitingLineOrP1;
    private Vector2 _p1, _p2;
    private ICadCommandHost? _host;
    public string Name => "线性标注";

    public OtoCAD.Avalonia.Snap.SnapType? PreferredSnapTypes =>
        _state == State.AwaitingLineOrP1
            ? OtoCAD.Avalonia.Snap.SnapType.Nearest | OtoCAD.Avalonia.Snap.SnapType.Endpoint | OtoCAD.Avalonia.Snap.SnapType.Center
            : OtoCAD.Avalonia.Snap.SnapType.Endpoint | OtoCAD.Avalonia.Snap.SnapType.Center;

    public void Start(ICadCommandHost host)
    {
        _host = host; _state = State.AwaitingLineOrP1;
        host.SetPrompt("[线性标注] 点 Line 一键标注, 或依次指定 2 端点 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        switch (_state)
        {
            case State.AwaitingLineOrP1:
                {
                    var picked = _host.PickEntityAt(p, tolerance: PickTolerance, pierce: true);
                    if (picked is Line ln && AutoDimFromLine(ln, p)) return;
                    _p1 = p; _state = State.AwaitingP2;
                    _host.SetPrompt("[线性标注] 请指定第二点");
                    return;
                }
            case State.AwaitingP2:
                _p2 = p; _state = State.AwaitingDimLine;
                _host.SetPrompt("[线性标注] 请指定尺寸线位置");
                return;
            case State.AwaitingDimLine:
                _host.AddEntity(new AlignedDimension(_p1, _p2, p));
                _host.SetPreview(null); _host.SetPrompt("[线性标注] 已创建");
                _host.FinishCommand();
                return;
        }
    }

    private bool AutoDimFromLine(Line ln, Vector2 clickPos)
    {
        if (_host is null) return false;
        var p1 = ln.startPoint;
        var p2 = ln.endPoint;
        var dir = (p2 - p1);
        if (dir.length < 1e-9) return false;
        var perp = new Vector2(-dir.normalized.Y, dir.normalized.X);
        var mid = (p1 + p2) * 0.5;
        double side = Math.Sign(Vector2.Dot(clickPos - mid, perp));
        if (side == 0) side = 1;
        Vector2 dimLinePos = mid + perp * (side * DefaultOffset);
        _host.AddEntity(new AlignedDimension(p1, p2, dimLinePos));
        _host.SetPreview(null);
        _host.SetPrompt($"[线性标注] 已创建, 长度={dir.length:F2}, 拖中点 grip 可调整位置");
        _host.FinishCommand();
        return true;
    }

    public void OnMouseMove(Vector2 p)
    {
        if (_state == State.AwaitingDimLine && _host is not null)
            try { _host.SetPreview(new AlignedDimension(_p1, _p2, p)); } catch { }
    }
    public void Cancel() { _host?.SetPreview(null); _host?.SetPrompt("[线性标注] 已取消"); _host?.FinishCommand(); }
}

/// <summary>
/// 半径标注 — 老 WinForms 行为 (selection-based):
/// 半径标注 — **单击**圆/弧边即可创建. 点击位置的角度自然成为 chord 方向, 引线径向外延.
///
/// 用户期望: 选了半径标注命令 → 在圆/弧上随便点一下 → 完事. 不再追"指定引线终点".
/// 旧"两点输入(圆心+弦)"流程对普通用户太反直觉, 已删除 — 标注半径必须先选中圆/弧.
/// </summary>
public sealed class RadialDimensionCmd : ICadCommand
{
    private const double PickTolerance = 12.0;   // 比一般实体 hit 大, 圆周容易点中
    private const double DefaultLeaderArmFactor = 0.4;  // leaderLen ≈ 0.4 × radius
    private const double DefaultLeaderArmMin = 6.0;     // 最低 6mm 引线
    private ICadCommandHost? _host;
    public string Name => "半径标注";

    /// <summary>偏好 Nearest snap, 让光标贴到圆周边.</summary>
    public OtoCAD.Avalonia.Snap.SnapType? PreferredSnapTypes =>
        OtoCAD.Avalonia.Snap.SnapType.Nearest;

    public void Start(ICadCommandHost host)
    {
        _host = host;
        host.SetPrompt("[半径标注] 请点击圆或弧 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        var picked = _host.PickEntityAt(p, tolerance: PickTolerance, pierce: true);

        Vector2 center;
        double radius;
        if (picked is Circle c) { center = c.center; radius = c.radius; }
        else if (picked is Arc a) { center = a.center; radius = a.radius; }
        else
        {
            _host.SetPrompt("[半径标注] 没命中圆/弧, 请精确点弧边再试 (ESC 取消)");
            return;  // 不结束命令, 等用户重点
        }

        // 把点击位置投影到圆周得到 chord (用户点击位置的角度成为标注方向)
        Vector2 dir = p - center;
        Vector2 chord = dir.length > 1e-9
            ? center + dir.normalized * radius
            : center + new Vector2(radius, 0);   // 退化: 点正中央, 默认水平向右

        double leaderArm = Math.Max(DefaultLeaderArmMin, radius * DefaultLeaderArmFactor);
        _host.AddEntity(new RadialDimension(center, chord, leaderArm));
        _host.SetPreview(null);
        _host.SetPrompt($"[半径标注] 已创建 R={radius:F2}");
        _host.FinishCommand();
    }

    public void OnMouseMove(Vector2 p)
    {
        // 单击命令, 无 preview 阶段
    }

    public void Cancel() { _host?.SetPreview(null); _host?.SetPrompt("[半径标注] 已取消"); _host?.FinishCommand(); }
}

/// <summary>
/// 直径标注 — selection-based:
/// Step1: 点 Circle/Arc → 自动用 entity 的 center, 弦点取直径方向
/// Step2: 点放置位置
/// Fallback: 3 点输入 (中心 + 弦点 1 + 弦点 2)
/// </summary>
public sealed class DiametricDimensionCmd : ICadCommand
{
    private const double PickTolerance = 12.0;
    private ICadCommandHost? _host;
    public string Name => "直径标注";

    public OtoCAD.Avalonia.Snap.SnapType? PreferredSnapTypes =>
        OtoCAD.Avalonia.Snap.SnapType.Nearest;

    public void Start(ICadCommandHost host)
    {
        _host = host;
        host.SetPrompt("[直径标注] 请点击圆或弧 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        var picked = _host.PickEntityAt(p, tolerance: PickTolerance, pierce: true);

        Vector2 center;
        double radius;
        if (picked is Circle c) { center = c.center; radius = c.radius; }
        else if (picked is Arc a) { center = a.center; radius = a.radius; }
        else
        {
            _host.SetPrompt("[直径标注] 没命中圆/弧, 请精确点弧边再试 (ESC 取消)");
            return;
        }

        // 点击位置自然指定直径方向 (穿圆心两端弦点)
        Vector2 dir = p - center;
        Vector2 unit = dir.length > 1e-9 ? dir.normalized : new Vector2(1, 0);
        Vector2 p1 = center + unit * radius;
        Vector2 p2 = center - unit * radius;

        _host.AddEntity(new DiametricDimension(center, p1, p2));
        _host.SetPreview(null);
        _host.SetPrompt($"[直径标注] 已创建 ⌀={2 * radius:F2}");
        _host.FinishCommand();
    }

    public void OnMouseMove(Vector2 p) { /* 单击命令, 无 preview */ }
    public void Cancel() { _host?.SetPreview(null); _host?.SetPrompt("[直径标注] 已取消"); _host?.FinishCommand(); }
}

/// <summary>Phase 1D: 3 点角度标注 (顶点 + 2 边点 + 弧位置).</summary>
public sealed class Angular3PointDimensionCmd : ICadCommand
{
    private enum State { AwaitingCenter, AwaitingFirst, AwaitingSecond, AwaitingArc }
    private State _state = State.AwaitingCenter;
    private Vector2 _center, _first, _second;
    private ICadCommandHost? _host;
    public string Name => "三点角度";

    public void Start(ICadCommandHost host)
    {
        _host = host; _state = State.AwaitingCenter;
        host.SetPrompt("[三点角度] 请指定角顶点 (ESC 取消)");
    }
    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        switch (_state)
        {
            case State.AwaitingCenter: _center = p; _state = State.AwaitingFirst; _host.SetPrompt("[三点角度] 请指定第一边端点"); break;
            case State.AwaitingFirst: _first = p; _state = State.AwaitingSecond; _host.SetPrompt("[三点角度] 请指定第二边端点"); break;
            case State.AwaitingSecond: _second = p; _state = State.AwaitingArc; _host.SetPrompt("[三点角度] 请指定弧线位置"); break;
            case State.AwaitingArc:
                _host.AddEntity(new Angular3PointDimension(_center, _first, _second, p));
                _host.SetPreview(null); _host.SetPrompt("[三点角度] 已创建");
                _host.FinishCommand(); break;
        }
    }
    public void OnMouseMove(Vector2 p)
    {
        if (_state == State.AwaitingArc && _host is not null)
            try { _host.SetPreview(new Angular3PointDimension(_center, _first, _second, p)); } catch { }
    }
    public void Cancel() { _host?.SetPreview(null); _host?.SetPrompt("[三点角度] 已取消"); _host?.FinishCommand(); }
}

/// <summary>Phase 1D: 坐标标注 (特征点 + 引线终点, 简化 useX 默认 true).</summary>
public sealed class OrdinateDimensionCmd : ICadCommand
{
    private enum State { AwaitingFeature, AwaitingLeader }
    private State _state = State.AwaitingFeature;
    private Vector2 _feature;
    private ICadCommandHost? _host;
    public string Name => "坐标标注";

    public void Start(ICadCommandHost host)
    {
        _host = host; _state = State.AwaitingFeature;
        host.SetPrompt("[坐标标注] 请指定特征点 (ESC 取消)");
    }
    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        if (_state == State.AwaitingFeature) { _feature = p; _state = State.AwaitingLeader; _host.SetPrompt("[坐标标注] 请指定引线终点"); }
        else
        {
            _host.AddEntity(new OrdinateDimension(_feature, p));
            _host.SetPreview(null); _host.SetPrompt("[坐标标注] 已创建");
            _host.FinishCommand();
        }
    }
    public void OnMouseMove(Vector2 p)
    {
        if (_state == State.AwaitingLeader && _host is not null)
            try { _host.SetPreview(new OrdinateDimension(_feature, p)); } catch { }
    }
    public void Cancel() { _host?.SetPreview(null); _host?.SetPrompt("[坐标标注] 已取消"); _host?.FinishCommand(); }
}

/// <summary>
/// 两线角度标注 — 点第一条 Line → 点第二条 Line → 指定圆弧位置.
/// 底层 lcdb.Angular2LineDimension 自动求交点并算夹角; 两线平行则放弃.
/// </summary>
public sealed class Angular2LineDimensionCmd : ICadCommand
{
    private const double PickTolerance = 8.0;
    private enum State { AwaitingFirstLine, AwaitingSecondLine, AwaitingArc }
    private State _state = State.AwaitingFirstLine;
    private Line? _first, _second;
    private ICadCommandHost? _host;
    public string Name => "两线角度";

    public OtoCAD.Avalonia.Snap.SnapType? PreferredSnapTypes =>
        _state == State.AwaitingArc
            ? null
            : OtoCAD.Avalonia.Snap.SnapType.Nearest | OtoCAD.Avalonia.Snap.SnapType.Endpoint;

    public void Start(ICadCommandHost host)
    {
        _host = host; _state = State.AwaitingFirstLine;
        host.SetPrompt("[两线角度] 请点击第一条直线 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        switch (_state)
        {
            case State.AwaitingFirstLine:
                if (_host.PickEntityAt(p, PickTolerance, pierce: true) is Line l1)
                {
                    _first = l1; _state = State.AwaitingSecondLine;
                    _host.SetPrompt("[两线角度] 请点击第二条直线");
                }
                else _host.SetPrompt("[两线角度] 没命中直线, 请重点第一条直线");
                return;
            case State.AwaitingSecondLine:
                if (_host.PickEntityAt(p, PickTolerance, pierce: true) is Line l2 && !ReferenceEquals(l2, _first))
                {
                    _second = l2; _state = State.AwaitingArc;
                    _host.SetPrompt("[两线角度] 请指定圆弧位置");
                }
                else _host.SetPrompt("[两线角度] 没命中另一条直线, 请重点第二条直线");
                return;
            case State.AwaitingArc:
                var dim = TryBuild(p);
                if (dim is null)
                {
                    _host.SetPreview(null);
                    _host.SetPrompt("[两线角度] 两线平行, 无法标注, 已取消");
                    _host.FinishCommand();
                    return;
                }
                _host.AddEntity(dim);
                _host.SetPreview(null);
                _host.SetPrompt("[两线角度] 已创建");
                _host.FinishCommand();
                return;
        }
    }

    private Angular2LineDimension? TryBuild(Vector2 arcPos)
    {
        if (_first is null || _second is null) return null;
        var dim = new Angular2LineDimension(
            _first.startPoint, _first.endPoint,
            _second.startPoint, _second.endPoint, arcPos);
        return dim.intersectionPoint.HasValue ? dim : null;
    }

    public void OnMouseMove(Vector2 p)
    {
        if (_state == State.AwaitingArc && _host is not null)
            try { var d = TryBuild(p); if (d is not null) _host.SetPreview(d); } catch { }
    }

    public void Cancel() { _host?.SetPreview(null); _host?.SetPrompt("[两线角度] 已取消"); _host?.FinishCommand(); }
}

/// <summary>
/// 矢高标注 — 点击圆弧/圆 → 矢高 sag = R(1-cos(θ/2)) → 用 LinearDimension 标注弦,
/// 文本固定显示 sag 值. 再点一次指定标注线位置.
/// </summary>
public sealed class SagittaDimensionCmd : ICadCommand
{
    private const double PickTolerance = 12.0;
    private enum State { AwaitingArc, AwaitingDimLine }
    private State _state = State.AwaitingArc;
    private Vector2 _chordStart, _chordEnd;
    private double _sag;
    private ICadCommandHost? _host;
    public string Name => "矢高";

    public OtoCAD.Avalonia.Snap.SnapType? PreferredSnapTypes =>
        _state == State.AwaitingArc ? OtoCAD.Avalonia.Snap.SnapType.Nearest : null;

    public void Start(ICadCommandHost host)
    {
        _host = host; _state = State.AwaitingArc;
        host.SetPrompt("[矢高] 请点击圆弧或圆 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        if (_state == State.AwaitingArc)
        {
            var picked = _host.PickEntityAt(p, PickTolerance, pierce: true);
            Vector2 center; double radius, startA, endA;
            if (picked is Arc a) { center = a.center; radius = a.radius; startA = a.startAngle; endA = a.endAngle; }
            else if (picked is Circle c) { center = c.center; radius = c.radius; startA = 0; endA = Math.PI; }
            else { _host.SetPrompt("[矢高] 没命中圆弧/圆, 请重点弧边"); return; }

            double chordAngle = Math.Abs(endA - startA);
            _sag = radius * (1 - Math.Cos(chordAngle / 2));
            _chordStart = center + new Vector2(radius * Math.Cos(startA), radius * Math.Sin(startA));
            _chordEnd   = center + new Vector2(radius * Math.Cos(endA),   radius * Math.Sin(endA));
            _state = State.AwaitingDimLine;
            _host.SetPrompt($"[矢高] sag={_sag:F3}, 请指定标注线位置");
            return;
        }
        _host.AddEntity(BuildDim(p));
        _host.SetPreview(null);
        _host.SetPrompt($"[矢高] 已创建 sag={_sag:F3}");
        _host.FinishCommand();
    }

    private LinearDimension BuildDim(Vector2 dimLinePos)
    {
        Vector2 mid = (_chordStart + _chordEnd) * 0.5;
        Vector2 dir = _chordEnd - _chordStart;
        double rotation = dir.length > 1e-9 ? Math.Atan2(dir.Y, dir.X) : 0.0;
        Vector2 dimDir = new Vector2(Math.Cos(rotation), Math.Sin(rotation));
        Vector2 perp = Vector2.Perpendicular(dimDir);
        double offset = Vector2.Dot(dimLinePos - mid, perp);
        if (offset < 0) { rotation += Math.PI; offset = -offset; }
        var dim = new LinearDimension(_chordStart, _chordEnd, offset, rotation,
            OtoCAD.Avalonia.Services.DimensionStandardService.CreateStyle());
        dim.userText = $"sag {_sag:F3}";
        return dim;
    }

    public void OnMouseMove(Vector2 p)
    {
        if (_state == State.AwaitingDimLine && _host is not null)
            try { _host.SetPreview(BuildDim(p)); } catch { }
    }

    public void Cancel() { _host?.SetPreview(null); _host?.SetPrompt("[矢高] 已取消"); _host?.FinishCommand(); }
}
