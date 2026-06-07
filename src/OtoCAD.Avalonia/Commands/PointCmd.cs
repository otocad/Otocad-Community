using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>Phase 1B: 单击放置一个 Point 实体.</summary>
public sealed class PointCmd : ICadCommand
{
    private ICadCommandHost? _host;
    public string Name => "点";

    public void Start(ICadCommandHost host)
    {
        _host = host;
        host.SetPrompt("[点] 单击放置 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        _host.AddEntity(new Point(p));
        _host.SetPrompt($"[点] 已创建 ({p.X:F2},{p.Y:F2})");
        _host.FinishCommand();
    }

    public void OnMouseMove(Vector2 p) { }

    public void Cancel()
    {
        if (_host is null) return;
        _host.SetPrompt("[点] 已取消");
        _host.FinishCommand();
    }
}
