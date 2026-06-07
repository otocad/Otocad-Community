using System.Collections.Generic;
using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// Phase 1E: 引线标注 (N 点法, 至少 2 点, 右键/回车完成).
/// 默认文本 "标注", 用户后续在 PropertyManager 编辑.
/// </summary>
public sealed class LeaderCmd : ICadCommand
{
    private readonly List<Vector2> _points = new();
    private ICadCommandHost? _host;
    public string Name => "引线";

    public void Start(ICadCommandHost host)
    {
        _host = host;
        host.SetPrompt("[引线] 请指定箭头起点 (右键/回车 完成, ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        _points.Add(p);
        _host.SetPrompt($"[引线] {_points.Count} 个顶点, 继续点击或右键完成");
        UpdatePreview(p);
    }

    public void OnMouseMove(Vector2 p)
    {
        if (_host is null || _points.Count == 0) return;
        UpdatePreview(p);
    }

    public void OnRightClick(Vector2 _) => Finish();
    public void OnConfirm() => Finish();

    private void Finish()
    {
        if (_host is null) return;
        if (_points.Count < 2)
        {
            _host.SetPreview(null);
            _host.SetPrompt("[引线] 至少需要 2 个顶点, 已取消");
            _host.FinishCommand();
            return;
        }
        _host.AddEntity(new Leader("标注", _points));
        _host.SetPreview(null);
        _host.SetPrompt($"[引线] 已创建 {_points.Count} 个顶点");
        _host.FinishCommand();
    }

    private void UpdatePreview(Vector2 hover)
    {
        if (_host is null) return;
        var pts = new List<Vector2>(_points) { hover };
        // 预览用 Polyline 折线
        var pl = new Polyline();
        for (int i = 0; i < pts.Count; i++) pl.AddVertexAt(i, pts[i]);
        _host.SetPreview(pl);
    }

    public void Cancel() { _host?.SetPreview(null); _host?.SetPrompt("[引线] 已取消"); _host?.FinishCommand(); }
}
