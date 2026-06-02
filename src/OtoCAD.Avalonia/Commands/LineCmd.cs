using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// POC-05: 经典两点画线命令 (LineCmd 模板).
///
/// 状态机:
///   AwaitingFirst   — 等待用户点击第一点
///   AwaitingSecond  — 第一点已记录, 等待第二点, 鼠标移动时画橡皮筋预览
/// </summary>
public sealed class LineCmd : ICadCommand
{
    private enum State { AwaitingFirst, AwaitingSecond }

    private State _state = State.AwaitingFirst;
    private Vector2 _firstPoint;
    private ICadCommandHost? _host;

    public string Name => "直线";

    public void Start(ICadCommandHost host)
    {
        _host = host;
        _state = State.AwaitingFirst;
        host.SetPrompt("[直线] 请选择起点 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null) return;

        if (_state == State.AwaitingFirst)
        {
            _firstPoint = p;
            _state = State.AwaitingSecond;
            _host.SetPrompt($"[直线] 起点 ({p.X:F2},{p.Y:F2}), 请选择终点 (ESC 取消)");
        }
        else // AwaitingSecond
        {
            var line = new Line(_firstPoint, p);
            _host.AddEntity(line);
            _host.SetPreview(null);
            _host.SetPrompt($"[直线] 已创建: ({_firstPoint.X:F2},{_firstPoint.Y:F2}) → ({p.X:F2},{p.Y:F2}), 长度 {line.length:F3}");
            _host.FinishCommand();
        }
    }

    public void OnMouseMove(Vector2 p)
    {
        if (_state != State.AwaitingSecond || _host is null) return;
        var preview = new Line(_firstPoint, p);
        _host.SetPreview(preview);
    }

    public void Cancel()
    {
        if (_host is null) return;
        _host.SetPreview(null);
        _host.SetPrompt("[直线] 已取消");
        _host.FinishCommand();
    }
}
