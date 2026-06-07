using System.Collections.Generic;
using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// Phase 1B: 多段线 (N 点法).
/// 左键添加顶点; 右键 / 回车完成 (至少 2 顶点); ESC 取消.
/// 鼠标移动时显示当前段橡皮筋.
/// </summary>
public sealed class PolylineCmd : ICadCommand
{
    private readonly List<Vector2> _points = new();
    private Vector2 _hover;
    private ICadCommandHost? _host;
    public string Name => "多段线";

    public void Start(ICadCommandHost host)
    {
        _host = host;
        host.SetPrompt("[多段线] 请指定起点 (右键/回车 完成, ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        _points.Add(p);
        _host.SetPrompt($"[多段线] {_points.Count} 个顶点, 继续点击或右键完成");
        UpdatePreview(p);
    }

    public void OnMouseMove(Vector2 p)
    {
        if (_host is null || _points.Count == 0) return;
        _hover = p;
        UpdatePreview(p);
    }

    public void OnRightClick(Vector2 modelPoint) => Finish();
    public void OnConfirm() => Finish();

    private void Finish()
    {
        if (_host is null) return;
        if (_points.Count < 2)
        {
            _host.SetPreview(null);
            _host.SetPrompt("[多段线] 至少需要 2 个顶点, 已取消");
            _host.FinishCommand();
            return;
        }
        var pl = new Polyline();
        for (int i = 0; i < _points.Count; i++)
            pl.AddVertexAt(i, _points[i]);
        _host.AddEntity(pl);
        _host.SetPreview(null);
        _host.SetPrompt($"[多段线] 已创建 {_points.Count} 个顶点");
        _host.FinishCommand();
    }

    private void UpdatePreview(Vector2 hover)
    {
        if (_host is null || _points.Count == 0) return;
        var pl = new Polyline();
        for (int i = 0; i < _points.Count; i++)
            pl.AddVertexAt(i, _points[i]);
        pl.AddVertexAt(_points.Count, hover);
        _host.SetPreview(pl);
    }

    public void Cancel()
    {
        if (_host is null) return;
        _host.SetPreview(null);
        _host.SetPrompt("[多段线] 已取消");
        _host.FinishCommand();
    }
}
