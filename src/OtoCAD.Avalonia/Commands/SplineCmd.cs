using System.Collections.Generic;
using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// Phase 1B: 样条曲线 (N 点法, 控制点序列).
/// 左键添加控制点; 右键/回车完成 (至少 3 点); ESC 取消.
/// </summary>
public sealed class SplineCmd : ICadCommand
{
    private readonly List<Vector2> _points = new();
    private ICadCommandHost? _host;
    public string Name => "样条";

    public void Start(ICadCommandHost host)
    {
        _host = host;
        host.SetPrompt("[样条] 请指定起点 (右键/回车 完成, ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        _points.Add(p);
        _host.SetPrompt($"[样条] {_points.Count} 个控制点, 继续点击或右键完成");
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
            _host.SetPrompt("[样条] 至少需要 3 个控制点, 已取消");
            _host.FinishCommand();
            return;
        }
        _host.AddEntity(new Spline(_points));
        _host.SetPreview(null);
        _host.SetPrompt($"[样条] 已创建 {_points.Count} 个控制点");
        _host.FinishCommand();
    }

    private void UpdatePreview(Vector2 hover)
    {
        if (_host is null) return;
        var pts = new List<Vector2>(_points) { hover };
        if (pts.Count >= 2)
        {
            // 预览用 Polyline 折线即可 (无需真 Spline 计算)
            var pl = new Polyline();
            for (int i = 0; i < pts.Count; i++) pl.AddVertexAt(i, pts[i]);
            _host.SetPreview(pl);
        }
    }

    public void Cancel()
    {
        if (_host is null) return;
        _host.SetPreview(null);
        _host.SetPrompt("[样条] 已取消");
        _host.FinishCommand();
    }
}
