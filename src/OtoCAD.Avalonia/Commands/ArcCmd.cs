using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// Phase 1.6: 圆弧 (三点法: 起点 + 通过点 + 终点).
/// 复用 lcdb.Arc 的 3 点构造.
///
/// 状态机:
///   AwaitingStart  — 等待起点
///   AwaitingMid    — 起点已记录, 等待中间点 (鼠标移动画橡皮筋直线: 起点→当前)
///   AwaitingEnd    — 起点+中点已记录, 等待终点 (鼠标移动画橡皮筋弧: 三点弧)
/// </summary>
public sealed class ArcCmd : ICadCommand
{
    private enum State { AwaitingStart, AwaitingMid, AwaitingEnd }

    private State _state = State.AwaitingStart;
    private Vector2 _start;
    private Vector2 _mid;
    private ICadCommandHost? _host;

    public string Name => "圆弧";

    public void Start(ICadCommandHost host)
    {
        _host = host;
        _state = State.AwaitingStart;
        host.SetPrompt("[圆弧] 请选择起点 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;

        switch (_state)
        {
            case State.AwaitingStart:
                _start = p;
                _state = State.AwaitingMid;
                _host.SetPrompt($"[圆弧] 起点 ({p.X:F2},{p.Y:F2}), 请选择中间点");
                break;
            case State.AwaitingMid:
                _mid = p;
                _state = State.AwaitingEnd;
                _host.SetPrompt($"[圆弧] 中点 ({p.X:F2},{p.Y:F2}), 请选择终点");
                break;
            case State.AwaitingEnd:
                var arc = new Arc(_start, _mid, p);
                _host.AddEntity(arc);
                _host.SetPreview(null);
                _host.SetPrompt($"[圆弧] 已创建: 起 ({_start.X:F2},{_start.Y:F2}) 中 ({_mid.X:F2},{_mid.Y:F2}) 终 ({p.X:F2},{p.Y:F2}), 半径 {arc.radius:F3}");
                _host.FinishCommand();
                break;
        }
    }

    public void OnMouseMove(Vector2 p)
    {
        if (_host is null) return;
        switch (_state)
        {
            case State.AwaitingMid:
                // 橡皮筋: 起点 → 当前指针 (直线提示)
                _host.SetPreview(new Line(_start, p));
                break;
            case State.AwaitingEnd:
                // 橡皮筋: 完整三点圆弧
                var preview = new Arc(_start, _mid, p);
                if (preview.radius > 1e-6)
                    _host.SetPreview(preview);
                break;
        }
    }

    public void Cancel()
    {
        if (_host is null) return;
        _host.SetPreview(null);
        _host.SetPrompt("[圆弧] 已取消");
        _host.FinishCommand();
    }
}
