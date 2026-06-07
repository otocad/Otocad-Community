using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.PropertyPanel;

/// <summary>
/// POC-04: lcdb.Line 的 INotifyPropertyChanged 包装.
///
/// 注意: lcdb.Vector2 是 struct, 修改 X/Y 必须替换整个 struct.
/// 任何属性变化都会触发 OnChanged 回调, 用于画布重绘.
/// </summary>
public sealed class LineViewModel : INotifyPropertyChanged
{
    private readonly Line _line;
    private readonly Action _onChanged;

    public LineViewModel(Line line, Action onChanged)
    {
        _line = line;
        _onChanged = onChanged;
    }

    public double StartX
    {
        get => _line.startPoint.X;
        set
        {
            if (Math.Abs(value - _line.startPoint.X) < 1e-9) return;
            _line.startPoint = new Vector2(value, _line.startPoint.Y);
            OnPropertyChanged();
            OnPropertyChanged(nameof(Length));
            _onChanged();
        }
    }

    public double StartY
    {
        get => _line.startPoint.Y;
        set
        {
            if (Math.Abs(value - _line.startPoint.Y) < 1e-9) return;
            _line.startPoint = new Vector2(_line.startPoint.X, value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(Length));
            _onChanged();
        }
    }

    public double EndX
    {
        get => _line.endPoint.X;
        set
        {
            if (Math.Abs(value - _line.endPoint.X) < 1e-9) return;
            _line.endPoint = new Vector2(value, _line.endPoint.Y);
            OnPropertyChanged();
            OnPropertyChanged(nameof(Length));
            _onChanged();
        }
    }

    public double EndY
    {
        get => _line.endPoint.Y;
        set
        {
            if (Math.Abs(value - _line.endPoint.Y) < 1e-9) return;
            _line.endPoint = new Vector2(_line.endPoint.X, value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(Length));
            _onChanged();
        }
    }

    /// <summary>线段长度 (只读, 派生自 start/end)</summary>
    public double Length => _line.length;

    public string EntityTypeName => "Line";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
