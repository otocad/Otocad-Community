using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using lcdb.Optic;
using System;

namespace OtoCAD.Avalonia.Dialogs;

/// <summary>
/// 单透镜参数向导. 关闭后通过 <see cref="Result"/> 拿到配好的 OpticalLens (尚未设 Position).
/// Result == null 表示用户取消.
/// </summary>
public partial class LensWizardDialog : Window
{
    public OpticalLens? Result { get; private set; }

    private NumericUpDown _diameterSpin = null!;
    private NumericUpDown _thicknessSpin = null!;
    private NumericUpDown _r1Spin = null!;
    private NumericUpDown _r2Spin = null!;
    private CheckBox _r1Flat = null!;
    private CheckBox _r2Flat = null!;
    private ComboBox _materialCombo = null!;
    private NumericUpDown _ndSpin = null!;
    private NumericUpDown _vdSpin = null!;
    // P4 Aspheric:
    private CheckBox _r1Aspheric = null!;
    private CheckBox _r2Aspheric = null!;
    private NumericUpDown _r1KSpin = null!;
    private NumericUpDown _r2KSpin = null!;
    private TextBox _r1CoeffsText = null!;
    private TextBox _r2CoeffsText = null!;

    public LensWizardDialog()
    {
        InitializeComponent();

        _diameterSpin   = this.FindControl<NumericUpDown>("DiameterSpin")!;
        _thicknessSpin  = this.FindControl<NumericUpDown>("ThicknessSpin")!;
        _r1Spin         = this.FindControl<NumericUpDown>("R1Spin")!;
        _r2Spin         = this.FindControl<NumericUpDown>("R2Spin")!;
        _r1Flat         = this.FindControl<CheckBox>("R1FlatCheck")!;
        _r2Flat         = this.FindControl<CheckBox>("R2FlatCheck")!;
        _materialCombo  = this.FindControl<ComboBox>("MaterialCombo")!;
        _ndSpin         = this.FindControl<NumericUpDown>("NdSpin")!;
        _vdSpin         = this.FindControl<NumericUpDown>("VdSpin")!;
        _r1Aspheric     = this.FindControl<CheckBox>("R1AsphericCheck")!;
        _r2Aspheric     = this.FindControl<CheckBox>("R2AsphericCheck")!;
        _r1KSpin        = this.FindControl<NumericUpDown>("R1KSpin")!;
        _r2KSpin        = this.FindControl<NumericUpDown>("R2KSpin")!;
        _r1CoeffsText   = this.FindControl<TextBox>("R1CoeffsText")!;
        _r2CoeffsText   = this.FindControl<TextBox>("R2CoeffsText")!;

        // 默认值
        _diameterSpin.Value = 25.4m;
        _thicknessSpin.Value = 5m;
        _r1Spin.Value = 50m;
        _r2Spin.Value = -50m;
        _r1Flat.IsChecked = false;
        _r2Flat.IsChecked = false;

        // 平面 checkbox 控制
        _r1Flat.IsCheckedChanged += (_, _) => _r1Spin.IsEnabled = _r1Flat.IsChecked != true;
        _r2Flat.IsCheckedChanged += (_, _) => _r2Spin.IsEnabled = _r2Flat.IsChecked != true;

        // 材料下拉
        foreach (var g in GlassLibrary.All)
            _materialCombo.Items.Add($"{g.Name}  (n={g.Nd:F4}, V={g.Vd:F1})");
        _materialCombo.Items.Add("Custom (自定义)");
        _materialCombo.SelectedIndex = 0;
        _ndSpin.Value = (decimal)GlassLibrary.Default.Nd;
        _vdSpin.Value = (decimal)GlassLibrary.Default.Vd;

        _materialCombo.SelectionChanged += (_, _) =>
        {
            var idx = _materialCombo.SelectedIndex;
            if (idx >= 0 && idx < GlassLibrary.All.Count)
            {
                var g = GlassLibrary.All[idx];
                _ndSpin.Value = (decimal)g.Nd;
                _vdSpin.Value = (decimal)g.Vd;
            }
            // Custom: 保留用户当前值, 不动 spin
        };

        // 预设
        this.FindControl<Button>("PresetBiconvexBtn")!.Click    += (_, _) => ApplyPreset(50, -50);
        this.FindControl<Button>("PresetBiconcaveBtn")!.Click   += (_, _) => ApplyPreset(-50, 50);
        this.FindControl<Button>("PresetPlanoConvexBtn")!.Click += (_, _) => ApplyPreset(50, double.PositiveInfinity);
        this.FindControl<Button>("PresetPlanoConcaveBtn")!.Click += (_, _) => ApplyPreset(-50, double.PositiveInfinity);
        this.FindControl<Button>("PresetMeniscusBtn")!.Click    += (_, _) => ApplyPreset(50, 100);

        this.FindControl<Button>("OkBtn")!.Click += (_, _) =>
        {
            Result = BuildLens();
            Close();
        };
        this.FindControl<Button>("CancelBtn")!.Click += (_, _) =>
        {
            Result = null;
            Close();
        };
    }

    private void ApplyPreset(double r1, double r2)
    {
        _r1Flat.IsChecked = double.IsInfinity(r1);
        _r2Flat.IsChecked = double.IsInfinity(r2);
        if (!double.IsInfinity(r1)) _r1Spin.Value = (decimal)r1;
        if (!double.IsInfinity(r2)) _r2Spin.Value = (decimal)r2;
    }

    private OpticalLens BuildLens()
    {
        var lens = new OpticalLens
        {
            Diameter = (double)(_diameterSpin.Value ?? 25.4m),
            Thickness = (double)(_thicknessSpin.Value ?? 5m),
            RefractiveIndex = (double)(_ndSpin.Value ?? 1.5168m),
            AbbeNumber = (double)(_vdSpin.Value ?? 64.17m),
        };

        // P4: 前/后表面 — Spherical 或 Aspheric
        lens.FrontSurface = BuildSurface(
            isFlat: _r1Flat.IsChecked == true,
            r: (double)(_r1Spin.Value ?? 50m),
            isAspheric: _r1Aspheric.IsChecked == true,
            k: (double)(_r1KSpin.Value ?? 0m),
            coeffsText: _r1CoeffsText.Text);

        lens.BackSurface = BuildSurface(
            isFlat: _r2Flat.IsChecked == true,
            r: (double)(_r2Spin.Value ?? -50m),
            isAspheric: _r2Aspheric.IsChecked == true,
            k: (double)(_r2KSpin.Value ?? 0m),
            coeffsText: _r2CoeffsText.Text);

        // 材料名
        var idx = _materialCombo.SelectedIndex;
        if (idx >= 0 && idx < GlassLibrary.All.Count)
            lens.MaterialName = GlassLibrary.All[idx].Name;
        else
            lens.MaterialName = "Custom";

        return lens;
    }

    private static OpticalSurface BuildSurface(bool isFlat, double r, bool isAspheric, double k, string? coeffsText)
    {
        double radius = isFlat ? double.PositiveInfinity : r;
        if (!isAspheric) return OpticalSurface.Sphere(radius);

        return new AsphericSurface
        {
            Radius = radius,
            ConicConstant = k,
            EvenCoefficients = ParseCoeffs(coeffsText),
        };
    }

    /// <summary>解析 "1e-5, -2e-9" 这种逗号分隔系数串. 解析失败返回空数组.</summary>
    private static double[] ParseCoeffs(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<double>();
        var parts = text.Split(new[] { ',', ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new System.Collections.Generic.List<double>(parts.Length);
        foreach (var p in parts)
        {
            if (double.TryParse(p, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var v))
                result.Add(v);
        }
        return result.ToArray();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
