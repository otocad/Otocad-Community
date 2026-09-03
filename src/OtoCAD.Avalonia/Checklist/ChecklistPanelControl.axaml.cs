using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using lcdb.Checklist;

namespace OtoCAD.Avalonia.Checklist;

/// <summary>
/// 出图清单面板 — 显示当前图纸对清单的逐项判定 (状态灯 + 详情),
/// 顶部"一键重出"重建所有可自动修复的未通过项, 单项亦可"重出"。
///
/// 自身不持有画布: 经 <see cref="RefreshRequested"/> / <see cref="RegenRequested"/>
/// 事件由 MainWindow 驱动 (求值与 regen 映射在 MainWindow, 保持分层)。
/// </summary>
public partial class ChecklistPanelControl : UserControl
{
    /// <summary>用户点"刷新": 请求重新求值。</summary>
    public event Action? RefreshRequested;

    /// <summary>用户点"一键重出"/单项"重出": 请求执行这些 regen 动作名 (去重)。</summary>
    public event Action<IReadOnlyList<string>>? RegenRequested;

    public ChecklistPanelControl()
    {
        InitializeComponent();
        var refreshBtn = this.FindControl<Button>("RefreshBtn")!;
        var regenAllBtn = this.FindControl<Button>("RegenAllBtn")!;
        refreshBtn.Click += (_, _) => RefreshRequested?.Invoke();
        regenAllBtn.Click += (_, _) => RegenRequested?.Invoke(_lastRegenActions);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private IReadOnlyList<string> _lastRegenActions = Array.Empty<string>();

    /// <summary>用判定结果刷新面板。null = 无清单 (显示提示, 隐藏列表)。</summary>
    public void SetResult(ChecklistResult? result)
    {
        var emptyHint = this.FindControl<TextBlock>("EmptyHint")!;
        var itemsHost = this.FindControl<ItemsControl>("ItemsHost")!;
        var badge = this.FindControl<Border>("SummaryBadge")!;
        var summary = this.FindControl<TextBlock>("SummaryText")!;
        var regenAllBtn = this.FindControl<Button>("RegenAllBtn")!;

        if (result is null)
        {
            emptyHint.IsVisible = true;
            itemsHost.ItemsSource = null;
            badge.IsVisible = false;
            regenAllBtn.IsEnabled = false;
            return;
        }

        emptyHint.IsVisible = false;
        itemsHost.ItemsSource = result.Items.Select(ToRow).ToList();

        _lastRegenActions = result.RegenActions;
        regenAllBtn.IsEnabled = _lastRegenActions.Count > 0;

        // 摘要徽章: 可交付=绿; 有 Error 阻塞=红; 仅 warning=琥珀。
        badge.IsVisible = true;
        if (result.CanDeliver && result.FailCount == 0)
        {
            badge.Background = Brushes.SeaGreen;
            summary.Text = "全部通过";
        }
        else if (!result.CanDeliver)
        {
            badge.Background = new SolidColorBrush(Color.Parse("#D32F2F"));
            int blocking = result.Items.Count(i => i.IsBlocking);
            summary.Text = $"阻塞 {blocking} 项";
        }
        else
        {
            badge.Background = new SolidColorBrush(Color.Parse("#F9A825"));
            summary.Text = $"建议修 {result.WarningCount} 项";
        }
    }

    private void OnItemRegenClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string action } && !string.IsNullOrEmpty(action))
            RegenRequested?.Invoke(new[] { action });
    }

    // 状态 → 字形 + 颜色 (✓ 绿 / ✗ 红 / ! 琥珀 / – 灰N/A)
    private static ChecklistRow ToRow(ChecklistItemResult r)
    {
        (string glyph, IBrush brush) = r.Status switch
        {
            CheckStatus.Pass          => ("✓", (IBrush)Brushes.SeaGreen),
            CheckStatus.NotApplicable => ("–", (IBrush)Brushes.Gray),
            CheckStatus.Fail when r.Severity == CheckSeverity.Warning
                                      => ("!", (IBrush)new SolidColorBrush(Color.Parse("#F9A825"))),
            _                         => ("✗", (IBrush)new SolidColorBrush(Color.Parse("#D32F2F"))),
        };
        return new ChecklistRow
        {
            Glyph = glyph,
            GlyphBrush = brush,
            Label = r.Label,
            Detail = r.Detail,
            CanRegen = r.Status == CheckStatus.Fail && !string.IsNullOrEmpty(r.Regen),
            Regen = r.Regen,
        };
    }
}

/// <summary>清单面板一行的绑定视图模型 (DataTemplate 用, 公有属性)。</summary>
public sealed class ChecklistRow
{
    public string Glyph { get; init; } = "";
    public IBrush GlyphBrush { get; init; } = Brushes.Gray;
    public string Label { get; init; } = "";
    public string Detail { get; init; } = "";
    public bool CanRegen { get; init; }
    public string Regen { get; init; } = "";
}
