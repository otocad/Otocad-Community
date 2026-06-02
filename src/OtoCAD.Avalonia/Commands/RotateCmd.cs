using System;
using System.Collections.Generic;
using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// Phase 1C: 旋转 (基点 + 参考角 + 新角 → TransformBy 旋转矩阵).
/// 状态机: AwaitingBase → AwaitingRefAngle → AwaitingNewAngle.
/// </summary>
public sealed class RotateCmd : ICadCommand
{
    private enum State { AwaitingBase, AwaitingRefAngle, AwaitingNewAngle }
    private readonly List<Entity> _targets;
    private readonly TransformExecutor _executor;
    private State _state = State.AwaitingBase;
    private Vector2 _center;
    private double _refAngle;
    private bool _previewApplied;
    private double _lastPreviewAngle;
    private ICadCommandHost? _host;

    public string Name => "旋转";

    public RotateCmd(IEnumerable<Entity> selection, TransformExecutor executor)
    {
        _targets = new List<Entity>(selection);
        _executor = executor;
    }

    public void Start(ICadCommandHost host)
    {
        _host = host;
        if (_targets.Count == 0)
        {
            host.SetPrompt("[旋转] 未选中实体");
            host.FinishCommand();
            return;
        }
        host.SetPrompt($"[旋转] 选中 {_targets.Count} 个, 请指定基点");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        switch (_state)
        {
            case State.AwaitingBase:
                _center = p;
                _state = State.AwaitingRefAngle;
                _host.SetPrompt($"[旋转] 基点 ({p.X:F2},{p.Y:F2}), 请指定参考角点");
                break;
            case State.AwaitingRefAngle:
                _refAngle = Math.Atan2(p.Y - _center.Y, p.X - _center.X);
                _state = State.AwaitingNewAngle;
                _host.SetPrompt($"[旋转] 参考角 {_refAngle * 180 / Math.PI:F1}°, 请指定新角");
                break;
            case State.AwaitingNewAngle:
                CancelPreview();
                var newAngle = Math.Atan2(p.Y - _center.Y, p.X - _center.X);
                var delta = newAngle - _refAngle;
                _executor(_targets, BuildRotationMatrix(_center, delta), BuildRotationMatrix(_center, -delta));
                _host.SetPrompt($"[旋转] 已旋转 {delta * 180 / Math.PI:F1}°");
                _host.FinishCommand();
                break;
        }
    }

    public void OnMouseMove(Vector2 p)
    {
        if (_state != State.AwaitingNewAngle || _host is null) return;
        CancelPreview();
        var newAngle = Math.Atan2(p.Y - _center.Y, p.X - _center.X);
        var delta = newAngle - _refAngle;
        var fwd = BuildRotationMatrix(_center, delta);
        foreach (var e in _targets) e.TransformBy(fwd);
        _previewApplied = true;
        _lastPreviewAngle = delta;
        _host.SetPreview(null);
    }

    private void CancelPreview()
    {
        if (!_previewApplied || _host is null) return;
        var inv = BuildRotationMatrix(_center, -_lastPreviewAngle);
        foreach (var e in _targets) e.TransformBy(inv);
        _previewApplied = false;
    }

    public void Cancel()
    {
        if (_host is null) return;
        CancelPreview();
        _host.SetPreview(null);
        _host.SetPrompt("[旋转] 已取消");
        _host.FinishCommand();
    }

    private static Matrix3 BuildRotationMatrix(Vector2 center, double angle)
        => Matrix3.Translate(center) * Matrix3.RotateInRadian(angle) * Matrix3.Translate(new Vector2(-center.X, -center.Y));
}

public delegate void TransformExecutor(IReadOnlyList<Entity> targets, Matrix3 forward, Matrix3 inverse);
