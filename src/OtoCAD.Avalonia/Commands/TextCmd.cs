using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// Phase 1B: 文字 (单击放置默认文本 "文本", 高度 2.5).
/// 真正编辑文本内容预留给后续 PropertyManager (右键选中后编辑).
/// </summary>
public sealed class TextCmd : ICadCommand
{
    private ICadCommandHost? _host;
    public string Name => "文字";

    public void Start(ICadCommandHost host)
    {
        _host = host;
        host.SetPrompt("[文字] 单击放置 (默认 \"文本\", 后续可在属性面板编辑) (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        var t = new Text("文本", new Vector3(p.X, p.Y, 0), 2.5);
        _host.AddEntity(t);
        _host.SetPrompt($"[文字] 已放置 \"文本\" @ ({p.X:F2},{p.Y:F2})");
        _host.FinishCommand();
    }

    public void OnMouseMove(Vector2 p) { }

    public void Cancel()
    {
        if (_host is null) return;
        _host.SetPrompt("[文字] 已取消");
        _host.FinishCommand();
    }
}
