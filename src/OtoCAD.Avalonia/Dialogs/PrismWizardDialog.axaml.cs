using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using lcdb.Optic;

namespace OtoCAD.Avalonia.Dialogs;

/// <summary>
/// 棱镜参数向导. 关闭后通过 <see cref="Result"/> 拿到配好的 Prism (尚未设 Position).
/// </summary>
public partial class PrismWizardDialog : Window
{
    public Prism? Result { get; private set; }

    private ComboBox _typeCombo = null!;
    private NumericUpDown _widthSpin = null!;
    private NumericUpDown _heightSpin = null!;
    private ComboBox _materialCombo = null!;

    public PrismWizardDialog()
    {
        InitializeComponent();

        _typeCombo = this.FindControl<ComboBox>("TypeCombo")!;
        _widthSpin = this.FindControl<NumericUpDown>("WidthSpin")!;
        _heightSpin = this.FindControl<NumericUpDown>("HeightSpin")!;
        _materialCombo = this.FindControl<ComboBox>("MaterialCombo")!;

        // P0 只实做 RightAngle, 其它三类暂禁用 (避免用户选了仍渲染直角棱镜的视觉错觉)
        for (int i = 1; i < _typeCombo.ItemCount; i++)
        {
            if (_typeCombo.Items[i] is ComboBoxItem item)
                item.IsEnabled = false;
        }
        _typeCombo.SelectedIndex = 0;
        _widthSpin.Value = 20m;
        _heightSpin.Value = 20m;

        foreach (var g in GlassLibrary.All)
            _materialCombo.Items.Add($"{g.Name}  (n={g.Nd:F4}, V={g.Vd:F1})");
        _materialCombo.SelectedIndex = 0;

        this.FindControl<Button>("OkBtn")!.Click += (_, _) => { Result = Build(); Close(); };
        this.FindControl<Button>("CancelBtn")!.Click += (_, _) => { Result = null; Close(); };
    }

    private Prism Build()
    {
        var prism = new Prism
        {
            Type = (PrismType)System.Math.Max(0, _typeCombo.SelectedIndex),
            Width = (double)(_widthSpin.Value ?? 20m),
            Height = (double)(_heightSpin.Value ?? 20m),
        };
        var idx = _materialCombo.SelectedIndex;
        if (idx >= 0 && idx < GlassLibrary.All.Count)
            prism.ApplyMaterial(GlassLibrary.All[idx].Name);
        return prism;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
