using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OtoCAD.Avalonia.Services;
using System.Linq;

namespace OtoCAD.Avalonia.Dialogs;

/// <summary>
/// T8 设置对话框 — 主题/AutoSave 间隔/清最近文件.
/// 关闭后通过 Result 属性返回新设置 (null = 取消).
/// </summary>
public partial class SettingsDialog : Window
{
    public AppSettings? Result { get; private set; }
    private readonly AppSettings _initial;
    private readonly RecentFiles _recent;

    public SettingsDialog() : this(new AppSettings(), new RecentFiles()) { }

    public SettingsDialog(AppSettings initial, RecentFiles recent)
    {
        InitializeComponent();
        _initial = initial;
        _recent = recent;

        var themeCombo = this.FindControl<ComboBox>("ThemeCombo")!;
        var dimStandardCombo = this.FindControl<ComboBox>("DimensionStandardCombo")!;
        var autoSpin = this.FindControl<NumericUpDown>("AutoSaveSpin")!;
        var recentCount = this.FindControl<TextBlock>("RecentCountText")!;
        var clearBtn = this.FindControl<Button>("ClearRecentBtn")!;
        var okBtn = this.FindControl<Button>("OkBtn")!;
        var cancelBtn = this.FindControl<Button>("CancelBtn")!;

        // 初值
        var themeIdx = initial.Theme switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0,
        };
        themeCombo.SelectedIndex = themeIdx;
        var dimStandardKey = DimensionStandardService.NormalizeKey(initial.DimensionStandard);
        dimStandardCombo.SelectedIndex = dimStandardKey switch
        {
            DimensionStandardService.Standard => 0,
            DimensionStandardService.Iso25 => 1,
            DimensionStandardService.Gb => 2,
            DimensionStandardService.GbOptical => 3,
            DimensionStandardService.Company => 4,
            _ => 3,
        };
        autoSpin.Value = initial.AutoSaveSeconds;
        recentCount.Text = $"当前 {recent.Items.Count} 条 (磁盘存在 {recent.ExistingItems().Count} 条)";

        clearBtn.Click += (_, _) =>
        {
            foreach (var p in recent.Items.ToList()) recent.Remove(p);
            recentCount.Text = "已清空";
            clearBtn.IsEnabled = false;
        };

        okBtn.Click += (_, _) =>
        {
            var pick = themeCombo.SelectedItem as ComboBoxItem;
            var dimPick = dimStandardCombo.SelectedItem as ComboBoxItem;
            Result = new AppSettings
            {
                Theme = pick?.Tag?.ToString() ?? "Default",
                DimensionStandard = DimensionStandardService.NormalizeKey(dimPick?.Tag?.ToString()),
                AutoSaveSeconds = (int)(autoSpin.Value ?? 30m),
                RestoreLastScene = initial.RestoreLastScene,
            };
            Close();
        };

        cancelBtn.Click += (_, _) =>
        {
            Result = null;
            Close();
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
