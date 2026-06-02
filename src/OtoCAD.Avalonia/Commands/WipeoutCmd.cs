using System.Collections.Generic;
using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// Phase 1E (实做): 遮罩 Wipeout — N 点法定义多边形遮罩区域.
/// 左键添加顶点, 右键/回车完成 (≥3 点), ESC 取消.
/// </summary>
public sealed class WipeoutCmd : ICadCommand
{
    private readonly List<Vector2> _points = new();
    private ICadCommandHost? _host;
    public string Name => "遮罩";

    public void Start(ICadCommandHost host)
    {
        _host = host;
        host.SetPrompt("[遮罩] 请指定第一点 (右键/回车 完成, ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        _points.Add(p);
        _host.SetPrompt($"[遮罩] {_points.Count} 个顶点, 继续点击或右键完成");
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
        if (_points.Count < 3)
        {
            _host.SetPreview(null);
            _host.SetPrompt("[遮罩] 至少需要 3 个顶点, 已取消");
            _host.FinishCommand();
            return;
        }
        _host.AddEntity(new Wipeout(_points));
        _host.SetPreview(null);
        _host.SetPrompt($"[遮罩] 已创建 {_points.Count} 顶点");
        _host.FinishCommand();
    }

    private void UpdatePreview(Vector2 hover)
    {
        if (_host is null) return;
        // 预览用 Polyline 闭合显示当前形状
        var pts = new List<Vector2>(_points) { hover };
        var pl = new Polyline();
        for (int i = 0; i < pts.Count; i++) pl.AddVertexAt(i, pts[i]);
        if (pts.Count >= 2) pl.AddVertexAt(pts.Count, pts[0]);  // 闭合
        _host.SetPreview(pl);
    }

    public void Cancel()
    {
        _host?.SetPreview(null);
        _host?.SetPrompt("[遮罩] 已取消");
        _host?.FinishCommand();
    }
}
