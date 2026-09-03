using System;
using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// Phase 1B: 椭圆 (中心法: 中心 + X 轴端点 + Y 轴距离).
/// 简化: 仅支持轴对齐椭圆 (radiusX = |center→p2|, radiusY = |p3 距中心的垂直分量|).
/// </summary>
public sealed class EllipseCmd : ICadCommand
{
    private enum State { AwaitingCenter, AwaitingAxisX, AwaitingAxisY }
    private State _state = State.AwaitingCenter;
    private Vector2 _center;
    private double _radiusX;
    private ICadCommandHost? _host;
    public string Name => "椭圆";

    public void Start(ICadCommandHost host)
    {
        _host = host;
        _state = State.AwaitingCenter;
        host.SetPrompt("[椭圆] 请指定中心 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        switch (_state)
        {
            case State.AwaitingCenter:
                _center = p;
                _state = State.AwaitingAxisX;
                _host.SetPrompt("[椭圆] 请指定 X 轴端点");
                break;
            case State.AwaitingAxisX:
                _radiusX = (p - _center).length;
                _state = State.AwaitingAxisY;
                _host.SetPrompt($"[椭圆] X 半径 {_radiusX:F3}, 请指定 Y 半径距离");
                break;
            case State.AwaitingAxisY:
                var radiusY = System.Math.Abs(p.Y - _center.Y);
                if (radiusY < 1e-6) { _host.SetPrompt("[椭圆] Y 半径过小"); return; }
                _host.AddEntity(new Ellipse(_center, _radiusX, radiusY));
                _host.SetPreview(null);
                _host.SetPrompt($"[椭圆] 已创建 {_radiusX:F3} × {radiusY:F3}");
                _host.FinishCommand();
                break;
        }
    }

    public void OnMouseMove(Vector2 p)
    {
        if (_host is null) return;
        switch (_state)
        {
            case State.AwaitingAxisX:
                // 点完中心 → 拖 X 轴: 预览一个圆 (半径 = center→cursor 距离)
                // 让用户看清当前 X 半径会落在哪
                var rx = (p - _center).length;
                if (rx > 1e-6) _host.SetPreview(new Ellipse(_center, rx, rx));
                break;
            case State.AwaitingAxisY:
                var ry = System.Math.Abs(p.Y - _center.Y);
                if (ry > 1e-6) _host.SetPreview(new Ellipse(_center, _radiusX, ry));
                break;
        }
    }

    public void Cancel()
    {
        if (_host is null) return;
        _host.SetPreview(null);
        _host.SetPrompt("[椭圆] 已取消");
        _host.FinishCommand();
    }
}
