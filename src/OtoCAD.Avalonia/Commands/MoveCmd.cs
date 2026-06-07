using System.Collections.Generic;
using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// Phase 1A: 移动选中实体 (经典 2 点法: 基点 → 目标点).
/// 命令启动时 snapshot 选中集合, 鼠标移动时整体跟随预览,
/// 第二点点击时 commit Translate 并 push undo.
/// </summary>
public sealed class MoveCmd : ICadCommand
{
    private enum State { AwaitingBase, AwaitingTarget }

    private readonly List<Entity> _targets = new();
    private State _state = State.AwaitingBase;
    private Vector2 _basePoint;
    private Vector2 _lastPreviewTarget;
    private bool _previewApplied;
    private ICadCommandHost? _host;
    private MoveOpExecutor? _executor;

    public string Name => "移动";

    /// <summary>外部 (CommandRegistry/Canvas) 必须在 Start 前注入 host + 目标 + 提交器.</summary>
    public MoveCmd(IEnumerable<Entity> selection, MoveOpExecutor executor)
    {
        _targets.AddRange(selection);
        _executor = executor;
    }

    public void Start(ICadCommandHost host)
    {
        _host = host;
        if (_targets.Count == 0)
        {
            host.SetPrompt("[移动] 当前未选中任何实体, 请先选中再执行");
            host.FinishCommand();
            return;
        }
        host.SetPrompt($"[移动] 选中 {_targets.Count} 个实体, 请指定基点 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        if (_state == State.AwaitingBase)
        {
            _basePoint = p;
            _lastPreviewTarget = p;
            _state = State.AwaitingTarget;
            _host.SetPrompt($"[移动] 基点 ({p.X:F2},{p.Y:F2}), 请指定目标点");
        }
        else
        {
            // commit: 抵消上一次预览位移 (如果有), 再应用最终位移作为单次 op
            CancelPreviewTranslate();
            var delta = p - _basePoint;
            _executor!(_targets, delta);
            _host.SetPrompt($"[移动] 已移动 {_targets.Count} 个实体 Δ=({delta.X:F2},{delta.Y:F2})");
            _host.FinishCommand();
        }
    }

    public void OnMouseMove(Vector2 p)
    {
        if (_state != State.AwaitingTarget || _host is null) return;
        // 把上次预览还原, 再应用新的预览
        CancelPreviewTranslate();
        var delta = p - _basePoint;
        foreach (var e in _targets) e.Translate(delta);
        _lastPreviewTarget = p;
        _previewApplied = true;
        // 通过触发 host SetPreview(null) 引发重绘 (没有具体预览实体, 实际实体已移)
        _host.SetPreview(null);
    }

    private void CancelPreviewTranslate()
    {
        if (!_previewApplied) return;
        var revertDelta = _basePoint - _lastPreviewTarget;
        foreach (var e in _targets) e.Translate(revertDelta);
        _previewApplied = false;
    }

    public void Cancel()
    {
        if (_host is null) return;
        CancelPreviewTranslate();
        _host.SetPrompt("[移动] 已取消");
        _host.FinishCommand();
    }
}

/// <summary>
/// Canvas 注入的"提交位移"执行器, 内部应:
/// 1. 对每个目标实体调用 Translate(delta)
/// 2. push 一个 MoveEntitiesOp 到 undo 栈
/// 这样 MoveCmd 不需要直接知道 Undo 栈结构.
/// </summary>
public delegate void MoveOpExecutor(IReadOnlyList<Entity> targets, Vector2 delta);
