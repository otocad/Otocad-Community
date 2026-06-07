using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>Phase 1E: 多行文本 (单击放置, 默认内容 "多行\n文本", 高度 2.5).</summary>
public sealed class MTextCmd : ICadCommand
{
    private ICadCommandHost? _host;
    public string Name => "多行文本";

    public void Start(ICadCommandHost host)
    {
        _host = host;
        host.SetPrompt("[多行文本] 单击放置 (内容后续在属性面板编辑)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        _host.AddEntity(new MText("多行\n文本", p, 2.5));
        _host.SetPrompt($"[多行文本] 已放置 @ ({p.X:F2},{p.Y:F2})");
        _host.FinishCommand();
    }

    public void OnMouseMove(Vector2 p) { }
    public void Cancel() { _host?.SetPrompt("[多行文本] 已取消"); _host?.FinishCommand(); }
}
