using System;
using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// Phase 1B: 正多边形 (中心 + 外接圆顶点).
/// 默认 6 边 (可在 PropertyManager 编辑顶点数).
/// </summary>
public sealed class PolygonCmd : ICadCommand
{
    private enum State { AwaitingCenter, AwaitingVertex }
    private readonly int _sides;
    private State _state = State.AwaitingCenter;
    private Vector2 _center;
    private ICadCommandHost? _host;

    public string Name => "多边形";

    public PolygonCmd(int sides = 6)
    {
        _sides = System.Math.Max(3, sides);
    }

    public void Start(ICadCommandHost host)
    {
        _host = host;
        _state = State.AwaitingCenter;
        host.SetPrompt($"[多边形] {_sides} 边形, 请指定中心 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        if (_state == State.AwaitingCenter)
        {
            _center = p;
            _state = State.AwaitingVertex;
            _host.SetPrompt($"[多边形] 中心 ({p.X:F2},{p.Y:F2}), 请指定顶点");
        }
        else
        {
            var pl = BuildPolygon(_center, p, _sides);
            _host.AddEntity(pl);
            _host.SetPreview(null);
            var r = (p - _center).length;
            _host.SetPrompt($"[多边形] 已创建 {_sides} 边形, 半径 {r:F3}");
            _host.FinishCommand();
        }
    }

    public void OnMouseMove(Vector2 p)
    {
        if (_state != State.AwaitingVertex || _host is null) return;
        _host.SetPreview(BuildPolygon(_center, p, _sides));
    }

    public void Cancel()
    {
        if (_host is null) return;
        _host.SetPreview(null);
        _host.SetPrompt("[多边形] 已取消");
        _host.FinishCommand();
    }

    private static Polyline BuildPolygon(Vector2 center, Vector2 vertex, int sides)
    {
        var dx = vertex.X - center.X;
        var dy = vertex.Y - center.Y;
        var r = System.Math.Sqrt(dx * dx + dy * dy);
        var startAngle = System.Math.Atan2(dy, dx);
        var pl = new Polyline();
        for (int i = 0; i < sides; i++)
        {
            var a = startAngle + i * 2.0 * System.Math.PI / sides;
            pl.AddVertexAt(i, new Vector2(center.X + r * System.Math.Cos(a), center.Y + r * System.Math.Sin(a)));
        }
        pl.AddVertexAt(sides, new Vector2(center.X + r * System.Math.Cos(startAngle), center.Y + r * System.Math.Sin(startAngle)));
        return pl;
    }
}
