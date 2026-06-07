using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using lcdb;
using lcdb.Optic;

namespace OtoCAD.Avalonia.Dialogs;

/// <summary>
/// 导入光学设计的预览+选择对话框: 左侧光路图实时预览(勾选即所见), 右侧逐元件勾选, 底部生成选项.
/// 关闭后 <see cref="Result"/> = 选中实体; null = 取消. <see cref="MakeLayoutTab"/>/<see cref="MakePerElementTabs"/> = 生成选项.
/// </summary>
public partial class ImportSelectionDialog : Window
{
    private readonly List<(CheckBox cb, Entity e)> _rows = new();
    private CadCanvas? _preview;

    /// <summary>选中的实体 (按原顺序); null = 取消.</summary>
    public List<Entity>? Result { get; private set; }
    /// <summary>是否生成「光路图总览」tab.</summary>
    public bool MakeLayoutTab { get; private set; } = true;
    /// <summary>是否为每个选中元件生成「加工图」tab.</summary>
    public bool MakePerElementTabs { get; private set; } = true;

    public ImportSelectionDialog()
    {
        InitializeComponent();
    }

    public ImportSelectionDialog(IReadOnlyList<Entity> entities, string sourceSummary) : this()
    {
        _preview = this.FindControl<CadCanvas>("PreviewCanvas");
        this.FindControl<TextBlock>("SummaryText")!.Text = sourceSummary;

        var panel = this.FindControl<StackPanel>("ItemsPanel")!;
        int i = 1;
        foreach (var e in entities)
        {
            var cb = new CheckBox
            {
                Content = Describe(i, e),
                IsChecked = true,
                Margin = new Thickness(0, 2, 0, 2),
            };
            cb.IsCheckedChanged += (_, _) => RefreshPreview();
            panel.Children.Add(cb);
            _rows.Add((cb, e));
            i++;
        }

        this.FindControl<Button>("SelectAllBtn")!.Click += (_, _) => SetAll(true);
        this.FindControl<Button>("SelectNoneBtn")!.Click += (_, _) => SetAll(false);
        this.FindControl<Button>("OkBtn")!.Click += (_, _) =>
        {
            Result = _rows.Where(r => r.cb.IsChecked == true).Select(r => r.e).ToList();
            MakeLayoutTab = this.FindControl<CheckBox>("LayoutTabCheck")!.IsChecked == true;
            MakePerElementTabs = this.FindControl<CheckBox>("PerElementCheck")!.IsChecked == true;
            Close();
        };
        this.FindControl<Button>("CancelBtn")!.Click += (_, _) =>
        {
            Result = null;
            Close();
        };

        RefreshPreview();
        // 布局完成后再刷一次, 确保画布有尺寸时能正确适配视图.
        Opened += (_, _) => RefreshPreview();
    }

    private void SetAll(bool value)
    {
        foreach (var (cb, _) in _rows) cb.IsChecked = value;  // 触发各自 RefreshPreview
    }

    /// <summary>把当前勾选的元件 (克隆) 灌进预览画布, 适配视图. 勾选变化即刷新.</summary>
    private void RefreshPreview()
    {
        if (_preview is null) return;
        try
        {
            var clones = _rows.Where(r => r.cb.IsChecked == true)
                              .Select(r => (Entity)r.e.Clone())
                              .ToList();
            _preview.LoadGeneratedSheet(clones);
        }
        catch { /* 预览失败不阻断导入 */ }
    }

    private static string Describe(int index, Entity e)
    {
        string tag = SurfaceTag(e);
        switch (e)
        {
            case OpticalLens l:
                return $"#{index}  单透镜  {l.MaterialName}   R1={R(l.R1)} R2={R(l.R2)}   d={l.Thickness:F2}   净Ø{l.Diameter:F1}{tag}";
            case CementedLens d:
                return $"#{index}  双胶合  {d.Material1}+{d.Material2}   R1={R(d.R1)} Rc={R(d.RContact)} R3={R(d.R3)}   净Ø{d.Diameter:F1}{tag}";
            default:
                return $"#{index}  {e.className}";
        }
    }

    private static string SurfaceTag(Entity e)
    {
        OpticalExtensions? ext = (e as OpticalLens)?.Extensions ?? (e as CementedLens)?.Extensions;
        if (ext != null && ext.Tags.TryGetValue("surfaceIndex", out var s))
            return $"   (面 S{s})";
        return "";
    }

    private static string R(double r)
        => double.IsInfinity(r) ? "∞" : r.ToString("F2", CultureInfo.InvariantCulture);

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
