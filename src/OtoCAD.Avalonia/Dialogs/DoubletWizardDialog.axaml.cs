using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using lcdb.Optic;
using System;
using System.Linq;

namespace OtoCAD.Avalonia.Dialogs;

public partial class DoubletWizardDialog : Window
{
    public CementedLens? Result { get; private set; }

    private NumericUpDown _diameter = null!, _t1 = null!, _t2 = null!;
    private NumericUpDown _r1 = null!, _rc = null!, _r3 = null!;
    private CheckBox _r1Flat = null!, _rcFlat = null!, _r3Flat = null!;
    private ComboBox _mat1 = null!, _mat2 = null!;

    public DoubletWizardDialog()
    {
        InitializeComponent();

        _diameter = this.FindControl<NumericUpDown>("DiameterSpin")!;
        _t1 = this.FindControl<NumericUpDown>("T1Spin")!;
        _t2 = this.FindControl<NumericUpDown>("T2Spin")!;
        _r1 = this.FindControl<NumericUpDown>("R1Spin")!;
        _rc = this.FindControl<NumericUpDown>("RcSpin")!;
        _r3 = this.FindControl<NumericUpDown>("R3Spin")!;
        _r1Flat = this.FindControl<CheckBox>("R1FlatCheck")!;
        _rcFlat = this.FindControl<CheckBox>("RcFlatCheck")!;
        _r3Flat = this.FindControl<CheckBox>("R3FlatCheck")!;
        _mat1 = this.FindControl<ComboBox>("Material1Combo")!;
        _mat2 = this.FindControl<ComboBox>("Material2Combo")!;

        // 材料下拉
        foreach (var g in GlassLibrary.All)
        {
            _mat1.Items.Add($"{g.Name}  (n={g.Nd:F4}, V={g.Vd:F1})");
            _mat2.Items.Add($"{g.Name}  (n={g.Nd:F4}, V={g.Vd:F1})");
        }

        // 默认 = Achromat 预设
        ApplyAchromatPreset();

        _r1Flat.IsCheckedChanged += (_, _) => _r1.IsEnabled = _r1Flat.IsChecked != true;
        _rcFlat.IsCheckedChanged += (_, _) => _rc.IsEnabled = _rcFlat.IsChecked != true;
        _r3Flat.IsCheckedChanged += (_, _) => _r3.IsEnabled = _r3Flat.IsChecked != true;

        this.FindControl<Button>("PresetAchromatBtn")!.Click += (_, _) => ApplyAchromatPreset();
        this.FindControl<Button>("PresetMeniscusBtn")!.Click += (_, _) =>
        {
            SetR(_r1, _r1Flat, 100); SetR(_rc, _rcFlat, 50); SetR(_r3, _r3Flat, 80);
            _t1.Value = 3m; _t2.Value = 2m;
            SelectMaterial(_mat1, "N-BK7"); SelectMaterial(_mat2, "N-SF5");
        };
        this.FindControl<Button>("PresetCustomBtn")!.Click += (_, _) =>
        {
            _diameter.Value = 25.4m;
            SetR(_r1, _r1Flat, 50); SetR(_rc, _rcFlat, -30); SetR(_r3, _r3Flat, -50);
            _t1.Value = 3m; _t2.Value = 3m;
        };

        this.FindControl<Button>("OkBtn")!.Click += (_, _) =>
        {
            Result = Build();
            Close();
        };
        this.FindControl<Button>("CancelBtn")!.Click += (_, _) =>
        {
            Result = null;
            Close();
        };
    }

    private void ApplyAchromatPreset()
    {
        _diameter.Value = 25.4m;
        _t1.Value = 4m; _t2.Value = 2.5m;
        SetR(_r1, _r1Flat, 60);
        SetR(_rc, _rcFlat, -40);
        SetR(_r3, _r3Flat, -120);
        SelectMaterial(_mat1, "N-BK7");
        SelectMaterial(_mat2, "F2");
    }

    private static void SetR(NumericUpDown spin, CheckBox flat, double v)
    {
        flat.IsChecked = false;
        spin.Value = (decimal)v;
        spin.IsEnabled = true;
    }

    private void SelectMaterial(ComboBox combo, string name)
    {
        var idx = GlassLibrary.All.ToList().FindIndex(g =>
            string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0) combo.SelectedIndex = idx;
    }

    private CementedLens Build()
    {
        var d = new CementedLens
        {
            Diameter = (double)(_diameter.Value ?? 25.4m),
            T1 = (double)(_t1.Value ?? 4m),
            T2 = (double)(_t2.Value ?? 2.5m),
            R1 = _r1Flat.IsChecked == true ? double.PositiveInfinity : (double)(_r1.Value ?? 60m),
            RContact = _rcFlat.IsChecked == true ? double.PositiveInfinity : (double)(_rc.Value ?? -40m),
            R3 = _r3Flat.IsChecked == true ? double.PositiveInfinity : (double)(_r3.Value ?? -120m),
        };

        ApplySelectedMaterial(_mat1, d.ApplyMaterial1);
        ApplySelectedMaterial(_mat2, d.ApplyMaterial2);
        return d;
    }

    private static void ApplySelectedMaterial(ComboBox combo, Action<string> apply)
    {
        var idx = combo.SelectedIndex;
        if (idx >= 0 && idx < GlassLibrary.All.Count)
            apply(GlassLibrary.All[idx].Name);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
