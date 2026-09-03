using System;
using System.Collections.Generic;
using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// Phase 1C: 缩放 (基点 + 参考距离 + 新距离 → 比例 = 新/参考).
/// 状态机: AwaitingBase → AwaitingRefDist → AwaitingNewDist.
/// </summary>
public sealed class ScaleCmd : ICadCommand
{
    private enum State { AwaitingBase, AwaitingRefDist, AwaitingNewDist }
    private readonly List<Entity> _targets;
    private readonly TransformExecutor _executor;
    private State _state = State.AwaitingBase;
    private Vector2 _center;
    private double _refDist;
    private bool _previewApplied;
    private double _lastPreviewFactor = 1.0;
    private ICadCommandHost? _host;

    public string Name => "缩放";

    public ScaleCmd(IEnumerable<Entity> selection, TransformExecutor executor)
    {
        _targets = new List<Entity>(selection);
        _executor = executor;
    }

    public void Start(ICadCommandHost host)
    {
        _host = host;
        if (_targets.Count == 0) { host.SetPrompt("[缩放] 未选中"); host.FinishCommand(); return; }
        host.SetPrompt($"[缩放] 选中 {_targets.Count} 个, 请指定基点");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        switch (_state)
        {
            case State.AwaitingBase:
                _center = p;
                _state = State.AwaitingRefDist;
                _host.SetPrompt($"[缩放] 基点 ({p.X:F2},{p.Y:F2}), 请指定参考距离点");
                break;
            case State.AwaitingRefDist:
                _refDist = (p - _center).length;
                if (_refDist < 1e-6) { _host.SetPrompt("[缩放] 参考距离过小"); return; }
                _state = State.AwaitingNewDist;
                _host.SetPrompt($"[缩放] 参考距离 {_refDist:F3}, 请指定新距离");
                break;
            case State.AwaitingNewDist:
                CancelPreview();
                var newDist = (p - _center).length;
                if (newDist < 1e-6) { _host.SetPrompt("[缩放] 新距离过小"); return; }
                var factor = newDist / _refDist;
                _executor(_targets, BuildScaleMatrix(_center, factor), BuildScaleMatrix(_center, 1.0 / factor));
                _host.SetPrompt($"[缩放] 已缩放 ×{factor:F3}");
                _host.FinishCommand();
                break;
        }
    }

    public void OnMouseMove(Vector2 p)
    {
        if (_state != State.AwaitingNewDist || _host is null) return;
        CancelPreview();
        var newDist = (p - _center).length;
        if (newDist < 1e-6) return;
        var factor = newDist / _refDist;
        foreach (var e in _targets) e.TransformBy(BuildScaleMatrix(_center, factor));
        _previewApplied = true;
        _lastPreviewFactor = factor;
        _host.SetPreview(null);
    }

    private void CancelPreview()
    {
        if (!_previewApplied || _host is null) return;
        foreach (var e in _targets) e.TransformBy(BuildScaleMatrix(_center, 1.0 / _lastPreviewFactor));
        _previewApplied = false;
    }

    public void Cancel()
    {
        if (_host is null) return;
        CancelPreview();
        _host.SetPreview(null);
        _host.SetPrompt("[缩放] 已取消");
        _host.FinishCommand();
    }

    private static Matrix3 BuildScaleMatrix(Vector2 center, double factor)
        => Matrix3.Translate(center) * Matrix3.Scale(new Vector2(factor, factor)) * Matrix3.Translate(new Vector2(-center.X, -center.Y));
}
