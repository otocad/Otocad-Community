using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>Phase 1B: 矩形 (2 点对角): 构造为闭合 Polyline.</summary>
public sealed class RectangleCmd : ICadCommand
{
    private enum State { AwaitingP1, AwaitingP2 }
    private State _state = State.AwaitingP1;
    private Vector2 _p1;
    private ICadCommandHost? _host;

    public string Name => "矩形";

    public void Start(ICadCommandHost host)
    {
        _host = host;
        _state = State.AwaitingP1;
        host.SetPrompt("[矩形] 请指定第一角点 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        if (_state == State.AwaitingP1)
        {
            _p1 = p;
            _state = State.AwaitingP2;
            _host.SetPrompt($"[矩形] 第一点 ({p.X:F2},{p.Y:F2}), 请指定对角点");
        }
        else
        {
            var rect = BuildRect(_p1, p);
            _host.AddEntity(rect);
            _host.SetPreview(null);
            _host.SetPrompt($"[矩形] 已创建 {System.Math.Abs(p.X - _p1.X):F2} × {System.Math.Abs(p.Y - _p1.Y):F2}");
            _host.FinishCommand();
        }
    }

    public void OnMouseMove(Vector2 p)
    {
        if (_state != State.AwaitingP2 || _host is null) return;
        _host.SetPreview(BuildRect(_p1, p));
    }

    public void Cancel()
    {
        if (_host is null) return;
        _host.SetPreview(null);
        _host.SetPrompt("[矩形] 已取消");
        _host.FinishCommand();
    }

    private static Polyline BuildRect(Vector2 a, Vector2 b)
    {
        var pl = new Polyline();
        pl.AddVertexAt(0, new Vector2(a.X, a.Y));
        pl.AddVertexAt(1, new Vector2(b.X, a.Y));
        pl.AddVertexAt(2, new Vector2(b.X, b.Y));
        pl.AddVertexAt(3, new Vector2(a.X, b.Y));
        pl.AddVertexAt(4, new Vector2(a.X, a.Y));  // close
        return pl;
    }
}
