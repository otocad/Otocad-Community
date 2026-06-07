using System;
using System.Collections.Generic;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// 命令名 → 可调用 Action 的中央注册表 (Phase 1.5).
/// 统一 Ribbon / 菜单 / 快捷键 / 命令行的入口.
///
/// 设计:
/// - 注册期: <see cref="Register"/> / <see cref="RegisterCadCommand"/> 提前装入字典.
/// - 解析期: <see cref="Resolve"/> 返回 Action 或 null (供 UI 决定回退/提示).
/// - 不持有 canvas/UI 引用, 由调用方在闭包内捕获 (避免 registry 强依赖 UI).
/// </summary>
public sealed class CommandRegistry
{
    private readonly Dictionary<string, Action> _actions = new(StringComparer.Ordinal);

    /// <summary>注册任意 Action (适用于场景切换/视图操作/文件 I/O 等).</summary>
    public void Register(string command, Action action)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("命令名不能为空", nameof(command));
        ArgumentNullException.ThrowIfNull(action);
        _actions[command] = action;
    }

    /// <summary>
    /// 注册一个 ICadCommand 工厂 (适用于 LineCmd/CircleCmd 等多步交互命令).
    /// 触发时实例化新命令并交给 starter (通常是 canvas.StartCommand).
    /// </summary>
    public void RegisterCadCommand(string command, Func<ICadCommand> factory, Action<ICadCommand> starter)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(starter);
        Register(command, () => starter(factory()));
    }

    /// <summary>未注册命令返回 null (由调用方决定占位/提示).</summary>
    public Action? Resolve(string command)
        => _actions.TryGetValue(command, out var action) ? action : null;

    /// <summary>已注册命令的总数 (诊断/测试用).</summary>
    public int Count => _actions.Count;

    /// <summary>是否已注册指定命令.</summary>
    public bool Contains(string command) => _actions.ContainsKey(command);

    /// <summary>遍历所有已注册命令名 (诊断/测试用).</summary>
    public IEnumerable<string> Commands => _actions.Keys;
}
