using System;
using System.Collections.Generic;
using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// Phase 1C: 偏移 (简化版 — 沿当前选中 bbox 中心 → 鼠标方向的法向量, 距离 = 鼠标距离).
/// 实际 CAD 偏移对每条曲线计算法向, MVP 版本仅做"克隆 + 平移", 后续按实体类型增强.
/// </summary>
public sealed class OffsetCmd : ICadCommand
{
    private readonly List<Entity> _targets;
    private readonly CopyExecutor _executor;
    private ICadCommandHost? _host;

    public string Name => "偏移";

    public OffsetCmd(IEnumerable<Entity> selection, CopyExecutor executor)
    {
        _targets = new List<Entity>(selection);
        _executor = executor;
    }

    public void Start(ICadCommandHost host)
    {
        _host = host;
        if (_targets.Count == 0) { host.SetPrompt("[偏移] 未选中"); host.FinishCommand(); return; }
        host.SetPrompt($"[偏移] 选中 {_targets.Count} 个, 单击指定偏移方向/距离 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        // 用第一个目标实体的中心作为参考基点 (简化)
        var anchor = GetTargetAnchor();
        var delta = p - anchor;
        _executor(_targets, delta);
        _host.SetPrompt($"[偏移] 已克隆 {_targets.Count} 个, Δ=({delta.X:F2},{delta.Y:F2})");
        _host.FinishCommand();
    }

    public void OnMouseMove(Vector2 p) { }

    public void Cancel()
    {
        if (_host is null) return;
        _host.SetPrompt("[偏移] 已取消");
        _host.FinishCommand();
    }

    private Vector2 GetTargetAnchor()
    {
        if (_targets.Count == 0) return new Vector2(0, 0);
        try
        {
            var b = _targets[0].bounding;
            // 反射读取
            var t = b.GetType();
            var minP = t.GetProperty("min")?.GetValue(b) ?? t.GetProperty("Min")?.GetValue(b);
            var maxP = t.GetProperty("max")?.GetValue(b) ?? t.GetProperty("Max")?.GetValue(b);
            if (minP is not null && maxP is not null)
            {
                var minX = (double)minP.GetType().GetField("X")!.GetValue(minP)!;
                var minY = (double)minP.GetType().GetField("Y")!.GetValue(minP)!;
                var maxX = (double)maxP.GetType().GetField("X")!.GetValue(maxP)!;
                var maxY = (double)maxP.GetType().GetField("Y")!.GetValue(maxP)!;
                return new Vector2((minX + maxX) / 2, (minY + maxY) / 2);
            }
        }
        catch { }
        return new Vector2(0, 0);
    }
}
