using System;
using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// Phase 1.6: 圆 (中心点 + 半径点 两点画圆).
///
/// 状态机:
///   AwaitingCenter — 等待用户点击中心
///   AwaitingRadius — 中心已记录, 等待半径点, 鼠标移动画橡皮筋
/// </summary>
public sealed class CircleCmd : ICadCommand
{
    private enum State { AwaitingCenter, AwaitingRadius }

    private State _state = State.AwaitingCenter;
    private Vector2 _center;
    private ICadCommandHost? _host;

    public string Name => "圆";

    public void Start(ICadCommandHost host)
    {
        _host = host;
        _state = State.AwaitingCenter;
        host.SetPrompt("[圆] 请选择圆心 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;

        if (_state == State.AwaitingCenter)
        {
            _center = p;
            _state = State.AwaitingRadius;
            _host.SetPrompt($"[圆] 圆心 ({p.X:F2},{p.Y:F2}), 请选择半径点 (ESC 取消)");
        }
        else // AwaitingRadius
        {
            var radius = (p - _center).length;
            if (radius < 1e-6)
            {
                _host.SetPrompt("[圆] 半径过小, 请重选半径点");
                return;
            }
            var circle = new Circle(_center, radius);
            _host.AddEntity(circle);
            _host.SetPreview(null);
            _host.SetPrompt($"[圆] 已创建: 圆心 ({_center.X:F2},{_center.Y:F2}), 半径 {radius:F3}");
            _host.FinishCommand();
        }
    }

    public void OnMouseMove(Vector2 p)
    {
        if (_state != State.AwaitingRadius || _host is null) return;
        var radius = (p - _center).length;
        if (radius < 1e-6) return;
        var preview = new Circle(_center, radius);
        _host.SetPreview(preview);
    }

    public void Cancel()
    {
        if (_host is null) return;
        _host.SetPreview(null);
        _host.SetPrompt("[圆] 已取消");
        _host.FinishCommand();
    }
}
