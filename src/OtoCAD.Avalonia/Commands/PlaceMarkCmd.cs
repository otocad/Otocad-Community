using System;
using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// Phase 1.6: 通用单点放置标记命令.
/// 给定 (Vector2 → Entity) 工厂, 用户点击一次即创建并放置标记.
/// 适用于绝大多数 Annotation Mark (Blackening/Polishing/Sandblasting/Coating 等).
///
/// 鼠标移动时显示"幽灵预览" — 调用工厂创建实例作为 preview, 跟随光标.
/// </summary>
public sealed class PlaceMarkCmd : ICadCommand
{
    private readonly Func<Vector2, Entity> _factory;
    private readonly string _displayName;
    private ICadCommandHost? _host;

    public PlaceMarkCmd(string displayName, Func<Vector2, Entity> factory)
    {
        _displayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public string Name => _displayName;

    /// <summary>Mark 应贴在实体边上, 不应吸到端点/中点/圆心.</summary>
    public OtoCAD.Avalonia.Snap.SnapType? PreferredSnapTypes => OtoCAD.Avalonia.Snap.SnapType.Nearest;

    public void Start(ICadCommandHost host)
    {
        _host = host;
        host.SetPrompt($"[{_displayName}] 单击放置位置 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;
        var mark = _factory(p);
        _host.AddEntity(mark);
        _host.SetPreview(null);
        _host.SetPrompt($"[{_displayName}] 已放置: ({p.X:F2},{p.Y:F2})");
        _host.FinishCommand();
    }

    public void OnMouseMove(Vector2 p)
    {
        if (_host is null) return;
        try
        {
            var preview = _factory(p);
            _host.SetPreview(preview);
        }
        catch
        {
            // 某些 mark 工厂可能在某些点会抛 (例如 0 size), 忽略并清空预览
            _host.SetPreview(null);
        }
    }

    public void Cancel()
    {
        if (_host is null) return;
        _host.SetPreview(null);
        _host.SetPrompt($"[{_displayName}] 已取消");
        _host.FinishCommand();
    }
}
