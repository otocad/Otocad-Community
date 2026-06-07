using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OtoCAD.Avalonia.Services;
using System;
using System.Linq;
using lcdb.Annotation;
using lcdb.Standards;

namespace OtoCAD.Avalonia.Dialogs;

/// <summary>
/// 出图标准(出图惯例)编辑对话框 — 选基准 → 逐项改 → 改任一项提示「已修改」→ 另存为。
/// 关闭后:Result = 要设为当前的惯例(null = 取消);ShouldSave = 是否存入用户自定义库。
/// </summary>
public partial class DrawingConventionDialog : Window
{
    public DrawingConvention? Result { get; private set; }
    public bool ShouldSave { get; private set; }

    private readonly ComboBox _baseCombo, _sqCombo, _faCombo, _roCombo, _ldCombo, _unitsCombo, _dimCombo, _styleCombo;
    private readonly TextBox _nameBox;
    private readonly TextBlock _modifiedLabel;
    private bool _loading;
    private string _baseName = "";

    public DrawingConventionDialog() : this(DrawingConventionService.Active) { }

    public DrawingConventionDialog(DrawingConvention initial)
    {
        InitializeComponent();
        _baseCombo = this.FindControl<ComboBox>("BaseCombo")!;
        _nameBox = this.FindControl<TextBox>("NameBox")!;
        _modifiedLabel = this.FindControl<TextBlock>("ModifiedLabel")!;
        _sqCombo = this.FindControl<ComboBox>("SurfaceQualityCombo")!;
        _faCombo = this.FindControl<ComboBox>("FormAccuracyCombo")!;
        _roCombo = this.FindControl<ComboBox>("RoughnessCombo")!;
        _ldCombo = this.FindControl<ComboBox>("LaserDamageCombo")!;
        _unitsCombo = this.FindControl<ComboBox>("UnitsCombo")!;
        _dimCombo = this.FindControl<ComboBox>("DimKeyCombo")!;
        _styleCombo = this.FindControl<ComboBox>("StyleCombo")!;

        // 枚举/取值填充
        _sqCombo.ItemsSource = Enum.GetValues<SurfaceQualityStandard>();
        _faCombo.ItemsSource = Enum.GetValues<FormAccuracyStandard>();
        _roCombo.ItemsSource = Enum.GetValues<RoughnessStandard>();
        _ldCombo.ItemsSource = Enum.GetValues<LaserDamageTestStandard>();
        _unitsCombo.ItemsSource = Enum.GetValues<DrawingUnits>();
        _dimCombo.ItemsSource = DimensionStandardService.AllKeys;
        _styleCombo.ItemsSource = new[] { "GB", "ISO" };

        _baseCombo.ItemsSource = DrawingConventionService.AllNames.ToList();
        _baseCombo.SelectionChanged += (_, _) =>
        {
            if (_loading) return;
            if (_baseCombo.SelectedItem is string name && DrawingConventionService.Find(name) is { } c)
                LoadFrom(c);
        };

        foreach (var combo in new[] { _sqCombo, _faCombo, _roCombo, _ldCombo, _unitsCombo, _dimCombo, _styleCombo })
            combo.SelectionChanged += (_, _) => MarkModified();

        LoadFrom(initial);
        _baseCombo.SelectedItem = DrawingConventionService.Find(initial.Name) is not null ? initial.Name : null;

        this.FindControl<Button>("OkBtn")!.Click += (_, _) => Commit(forceSaveAs: false);
        this.FindControl<Button>("SaveAsBtn")!.Click += (_, _) => Commit(forceSaveAs: true);
        this.FindControl<Button>("CancelBtn")!.Click += (_, _) => { Result = null; Close(); };
    }

    private void LoadFrom(DrawingConvention c)
    {
        _loading = true;
        _baseName = c.Name;
        _nameBox.Text = c.Name;
        _sqCombo.SelectedItem = c.SurfaceQuality;
        _faCombo.SelectedItem = c.FormAccuracy;
        _roCombo.SelectedItem = c.Roughness;
        _ldCombo.SelectedItem = c.LaserDamage;
        _unitsCombo.SelectedItem = c.Units;
        _dimCombo.SelectedItem = DimensionStandardService.NormalizeKey(c.DimensionStandardKey);
        _styleCombo.SelectedItem = c.DrawingStandardName == "ISO" ? "ISO" : "GB";
        _modifiedLabel.IsVisible = false;
        _loading = false;
    }

    private void MarkModified()
    {
        if (_loading) return;
        _modifiedLabel.IsVisible = true;
        // 改了内置基准 → 自动建议新名,避免覆盖内置
        if (DrawingConventionService.BuiltIns.Any(b => b.Name == _nameBox.Text))
            _nameBox.Text = _baseName + "-修改";
    }

    private DrawingConvention Gather()
    {
        return new DrawingConvention
        {
            Name = string.IsNullOrWhiteSpace(_nameBox.Text) ? _baseName : _nameBox.Text!.Trim(),
            BaseName = _baseName,
            SurfaceQuality = (SurfaceQualityStandard)(_sqCombo.SelectedItem ?? SurfaceQualityStandard.ISO_10110_7),
            FormAccuracy = (FormAccuracyStandard)(_faCombo.SelectedItem ?? FormAccuracyStandard.ISO_10110_5),
            Roughness = (RoughnessStandard)(_roCombo.SelectedItem ?? RoughnessStandard.ISO),
            LaserDamage = (LaserDamageTestStandard)(_ldCombo.SelectedItem ?? LaserDamageTestStandard.ISO21254),
            Units = (DrawingUnits)(_unitsCombo.SelectedItem ?? DrawingUnits.Millimeter),
            DimensionStandardKey = _dimCombo.SelectedItem as string ?? "GB-Optical",
            DrawingStandardName = _styleCombo.SelectedItem as string ?? "GB",
        };
    }

    private void Commit(bool forceSaveAs)
    {
        bool modified = _modifiedLabel.IsVisible;
        var builtin = DrawingConventionService.BuiltIns.FirstOrDefault(b => b.Name == _baseName);

        // 未改 + 仍是内置基准 + 非强制另存 → 直接设为当前,不入库
        if (!modified && !forceSaveAs && builtin is not null && _nameBox.Text == _baseName)
        {
            Result = builtin;
            ShouldSave = false;
            Close();
            return;
        }

        var conv = Gather();
        // 不允许覆盖内置名
        if (DrawingConventionService.BuiltIns.Any(b => b.Name == conv.Name))
            conv.Name = _baseName + "-修改";
        conv.IsBuiltIn = false;
        Result = conv;
        ShouldSave = true;
        Close();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
