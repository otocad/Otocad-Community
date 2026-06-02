using System;
using System.Collections.Generic;
using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// Phase 1C: 镜像 (沿 2 点定义的轴反射选中实体).
/// 简化版: 仅"修改原实体" (不保留原始). 完整版后续扩展.
/// 反射矩阵是自反 (fwd == inv).
/// </summary>
public sealed class MirrorCmd : ICadCommand
{
    private enum State { AwaitingP1, AwaitingP2 }
    private readonly List<Entity> _targets;
    private readonly TransformExecutor _executor;
    private State _state = State.AwaitingP1;
    private Vector2 _p1;
    private ICadCommandHost? _host;

    public string Name => "镜像";

    public MirrorCmd(IEnumerable<Entity> selection, TransformExecutor executor)
    {
        _targets = new List<Entity>(selection);
        _executor = executor;
    }

    public void Start(ICadCommandHost host)
    {
        _host = host;
        if (_targets.Count == 0) { host.SetPrompt("[镜像] 未选中"); host.FinishCommand(); return; }
        host.SetPrompt($"[镜像] 选中 {_targets.Count} 个, 请指定镜像轴第一点");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        if (_state == State.AwaitingP1)
        {
            _p1 = p;
            _state = State.AwaitingP2;
            _host.SetPrompt("[镜像] 请指定镜像轴第二点");
        }
        else
        {
            var m = BuildReflection(_p1, p);
            _executor(_targets, m, m);  // 反射是自反矩阵
            _host.SetPrompt($"[镜像] 已镜像 {_targets.Count} 个实体");
            _host.FinishCommand();
        }
    }

    public void OnMouseMove(Vector2 p) { /* 预览反射代价高, 暂不实现 */ }

    public void Cancel()
    {
        if (_host is null) return;
        _host.SetPrompt("[镜像] 已取消");
        _host.FinishCommand();
    }

    /// <summary>
    /// 沿过 p1, p2 的直线反射的 3x3 矩阵.
    /// 平移到 p1 → 旋转使轴对齐 X → 沿 X 轴反射 (y→-y) → 反旋转 → 平移回来.
    /// </summary>
    private static Matrix3 BuildReflection(Vector2 p1, Vector2 p2)
    {
        var dir = p2 - p1;
        var angle = Math.Atan2(dir.Y, dir.X);
        // 沿 X 轴的反射 (y → -y) = Scale(1, -1)
        var T = Matrix3.Translate(p1);
        var Tinv = Matrix3.Translate(new Vector2(-p1.X, -p1.Y));
        var R = Matrix3.RotateInRadian(angle);
        var Rinv = Matrix3.RotateInRadian(-angle);
        var S = Matrix3.Scale(new Vector2(1, -1));
        return T * R * S * Rinv * Tinv;
    }
}
