using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>Phase 1B: 射线 (基点 + 方向点).</summary>
public sealed class RayCmd : ICadCommand
{
    private enum State { AwaitingBase, AwaitingDir }
    private State _state = State.AwaitingBase;
    private Vector2 _base;
    private ICadCommandHost? _host;
    public string Name => "射线";

    public void Start(ICadCommandHost host)
    {
        _host = host;
        _state = State.AwaitingBase;
        host.SetPrompt("[射线] 请指定起点 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        if (_state == State.AwaitingBase)
        {
            _base = p;
            _state = State.AwaitingDir;
            _host.SetPrompt("[射线] 请指定方向点");
        }
        else
        {
            var dir = p - _base;
            if (dir.length < 1e-6) { _host.SetPrompt("[射线] 方向不明确"); return; }
            _host.AddEntity(new Ray(_base, dir));
            _host.SetPreview(null);
            _host.SetPrompt($"[射线] 已创建");
            _host.FinishCommand();
        }
    }

    public void OnMouseMove(Vector2 p)
    {
        if (_state != State.AwaitingDir || _host is null) return;
        var dir = p - _base;
        if (dir.length > 1e-6) _host.SetPreview(new Ray(_base, dir));
    }

    public void Cancel()
    {
        if (_host is null) return;
        _host.SetPreview(null);
        _host.SetPrompt("[射线] 已取消");
        _host.FinishCommand();
    }
}
