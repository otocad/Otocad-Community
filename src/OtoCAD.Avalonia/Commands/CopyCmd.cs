using System.Collections.Generic;
using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>Phase 1C: 复制 (基点 + 目标点 → clone 选中实体并位移).</summary>
public sealed class CopyCmd : ICadCommand
{
    private enum State { AwaitingBase, AwaitingTarget }
    private readonly List<Entity> _targets;
    private readonly CopyExecutor _executor;
    private State _state = State.AwaitingBase;
    private Vector2 _basePoint;
    private ICadCommandHost? _host;

    public string Name => "复制";

    public CopyCmd(IEnumerable<Entity> selection, CopyExecutor executor)
    {
        _targets = new List<Entity>(selection);
        _executor = executor;
    }

    public void Start(ICadCommandHost host)
    {
        _host = host;
        if (_targets.Count == 0)
        {
            host.SetPrompt("[复制] 未选中实体, 请先选中再执行");
            host.FinishCommand();
            return;
        }
        host.SetPrompt($"[复制] 选中 {_targets.Count} 个, 请指定基点 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        if (_state == State.AwaitingBase)
        {
            _basePoint = p;
            _state = State.AwaitingTarget;
            _host.SetPrompt($"[复制] 基点 ({p.X:F2},{p.Y:F2}), 请指定目标点");
        }
        else
        {
            var delta = p - _basePoint;
            _executor(_targets, delta);
            _host.SetPreview(null);
            _host.SetPrompt($"[复制] 已复制 {_targets.Count} 个, Δ=({delta.X:F2},{delta.Y:F2})");
            _host.FinishCommand();
        }
    }

    public void OnMouseMove(Vector2 p)
    {
        // 简化: 不显示克隆预览 (避免 Clone 开销), 仅原位高亮
    }

    public void Cancel()
    {
        if (_host is null) return;
        _host.SetPreview(null);
        _host.SetPrompt("[复制] 已取消");
        _host.FinishCommand();
    }
}

public delegate System.Collections.Generic.IReadOnlyList<Entity> CopyExecutor(IReadOnlyList<Entity> originals, Vector2 delta);
