using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>Phase 1B: 构造线 (无限长, 经 2 点定义方向).</summary>
public sealed class XlineCmd : ICadCommand
{
    private enum State { AwaitingP1, AwaitingP2 }
    private State _state = State.AwaitingP1;
    private Vector2 _p1;
    private ICadCommandHost? _host;
    public string Name => "构造线";

    public void Start(ICadCommandHost host)
    {
        _host = host;
        _state = State.AwaitingP1;
        host.SetPrompt("[构造线] 请指定基点 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        if (_state == State.AwaitingP1)
        {
            _p1 = p;
            _state = State.AwaitingP2;
            _host.SetPrompt("[构造线] 请指定方向点");
        }
        else
        {
            var dir = p - _p1;
            if (dir.length < 1e-6) { _host.SetPrompt("[构造线] 方向不明确"); return; }
            _host.AddEntity(new Xline(_p1, dir));
            _host.SetPreview(null);
            _host.SetPrompt("[构造线] 已创建");
            _host.FinishCommand();
        }
    }

    public void OnMouseMove(Vector2 p)
    {
        if (_state != State.AwaitingP2 || _host is null) return;
        var dir = p - _p1;
        if (dir.length > 1e-6) _host.SetPreview(new Xline(_p1, dir));
    }

    public void Cancel()
    {
        if (_host is null) return;
        _host.SetPreview(null);
        _host.SetPrompt("[构造线] 已取消");
        _host.FinishCommand();
    }
}
