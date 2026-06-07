using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using lcdb;
using System;

namespace OtoCAD.Avalonia.Dialogs;

/// <summary>
/// 公差输入对话框. 关闭后通过 <see cref="Result"/> 拿到配好的 BasicTolerance (null = 取消).
/// 两种模式: 自定义上下偏差 / 标准公差等级 (IT6-IT11, 走 ToleranceTable 查表).
/// </summary>
public partial class ToleranceInputDialog : Window
{
    public BasicTolerance? Result { get; private set; }
    public bool ShowFrame { get; private set; } = true;

    private NumericUpDown _nominalSpin = null!;
    private NumericUpDown _upperSpin = null!;
    private NumericUpDown _lowerSpin = null!;
    private RadioButton _customRadio = null!;
    private RadioButton _standardRadio = null!;
    private ComboBox _gradeCombo = null!;
    private CheckBox _showFrameCheck = null!;
    private TextBlock _previewText = null!;

    private static readonly ToleranceGrade[] Grades =
        { ToleranceGrade.IT6, ToleranceGrade.IT7, ToleranceGrade.IT8,
          ToleranceGrade.IT9, ToleranceGrade.IT10, ToleranceGrade.IT11 };

    public ToleranceInputDialog() : this(10.0) { }

    public ToleranceInputDialog(double nominalSize)
    {
        InitializeComponent();

        _nominalSpin    = this.FindControl<NumericUpDown>("NominalSpin")!;
        _upperSpin      = this.FindControl<NumericUpDown>("UpperSpin")!;
        _lowerSpin      = this.FindControl<NumericUpDown>("LowerSpin")!;
        _customRadio    = this.FindControl<RadioButton>("CustomRadio")!;
        _standardRadio  = this.FindControl<RadioButton>("StandardRadio")!;
        _gradeCombo     = this.FindControl<ComboBox>("GradeCombo")!;
        _showFrameCheck = this.FindControl<CheckBox>("ShowFrameCheck")!;
        _previewText    = this.FindControl<TextBlock>("PreviewText")!;

        _nominalSpin.Value = (decimal)Math.Round(nominalSize, 3);
        _upperSpin.Value = 0.05m;
        _lowerSpin.Value = -0.05m;
        foreach (var g in Grades) _gradeCombo.Items.Add(g.ToString());
        _gradeCombo.SelectedIndex = 1; // IT7

        _customRadio.IsCheckedChanged   += (_, _) => UpdateEnabled();
        _standardRadio.IsCheckedChanged += (_, _) => UpdateEnabled();
        _nominalSpin.ValueChanged += (_, _) => UpdatePreview();
        _upperSpin.ValueChanged   += (_, _) => UpdatePreview();
        _lowerSpin.ValueChanged   += (_, _) => UpdatePreview();
        _gradeCombo.SelectionChanged += (_, _) => UpdatePreview();
        UpdateEnabled();

        this.FindControl<Button>("OkBtn")!.Click += (_, _) =>
        {
            Result = Build();
            ShowFrame = _showFrameCheck.IsChecked == true;
            Close();
        };
        this.FindControl<Button>("CancelBtn")!.Click += (_, _) =>
        {
            Result = null;
            Close();
        };
    }

    private void UpdateEnabled()
    {
        bool std = _standardRadio.IsChecked == true;
        _upperSpin.IsEnabled = !std;
        _lowerSpin.IsEnabled = !std;
        _gradeCombo.IsEnabled = std;
        UpdatePreview();
    }

    private BasicTolerance Build()
    {
        double nominal = (double)(_nominalSpin.Value ?? 0m);
        if (_standardRadio.IsChecked == true)
        {
            var grade = Grades[Math.Max(0, _gradeCombo.SelectedIndex)];
            var bt = new ToleranceTable().GetStandardTolerance(nominal, grade);
            if (bt != null) { bt.ToleranceGrade = grade; return bt; }
            return new BasicTolerance(nominal, 0, 0); // 查表失败回退
        }
        double up = (double)(_upperSpin.Value ?? 0m);
        double lo = (double)(_lowerSpin.Value ?? 0m);
        return new BasicTolerance(nominal, up, lo);
    }

    private void UpdatePreview()
    {
        if (_previewText is null) return;
        try { _previewText.Text = "预览:  " + Build().GetFormattedString(); }
        catch { _previewText.Text = ""; }
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
