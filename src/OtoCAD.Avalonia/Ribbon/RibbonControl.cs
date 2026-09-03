using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace OtoCAD.Avalonia.Ribbon;

/// <summary>
/// 自研 Ribbon 控件.
///
/// v2.0 (2026-06 重构, 见 docs/ux-designs/ux-Otocad-2026-06-02/EXPERIENCE.md):
///   - 单主页 + 长尾次级页 IA; QAT 跨页常驻条
///   - 按钮分级: Compact (小图标密排) / Labeled (中图标+文字两行)
///   - 光学组 Accent (浅蓝底 + 蓝图标 + ★), 不靠大按钮区分主次
///   - ▾下拉 (SplitButton / DropDownButton); 图层横长条复合组
/// </summary>
public sealed class RibbonControl : UserControl
{
    private static readonly IBrush QatBackground      = new SolidColorBrush(Color.Parse("#F3F6FA"));
    private static readonly IBrush TabBarBackground   = new SolidColorBrush(Color.Parse("#F3F3F3"));
    private static readonly IBrush ContentBackground  = Brushes.White;
    private static readonly IBrush BorderColor        = new SolidColorBrush(Color.Parse("#E0E0E0"));
    private static readonly IBrush ActiveTabColor     = new SolidColorBrush(Color.Parse("#E5F1FB"));
    private static readonly IBrush HeaderBorder       = new SolidColorBrush(Color.Parse("#B8D4EA"));
    private static readonly IBrush GroupTitleColor    = new SolidColorBrush(Color.Parse("#999999"));
    private static readonly IBrush IconColor          = new SolidColorBrush(Color.Parse("#555555"));
    // 光学主角
    private static readonly IBrush OpticAccent        = new SolidColorBrush(Color.Parse("#0E5FA8"));
    private static readonly IBrush OpticBg            = new SolidColorBrush(Color.Parse("#EAF3FB"));
    private static readonly IBrush OpticBgBorder      = new SolidColorBrush(Color.Parse("#D6E6F4"));

    private readonly List<RibbonTab> _tabs = new();
    private RibbonTab? _activeTab;

    // -------- 图层▾ 动态下拉 (当前图层选择器) --------
    /// <summary>返回当前所有图层名 (供「图层▾」动态列出); null 时仅用静态 children.</summary>
    public Func<IEnumerable<string>>? LayerListProvider { get; set; }
    /// <summary>选中某图层 → 设为当前层.</summary>
    public Action<string>? OnLayerSelected { get; set; }
    /// <summary>返回当前图层名 (供「图层▾」按钮显示).</summary>
    public Func<string>? CurrentLayerProvider { get; set; }
    private TextBlock? _layerComboText;
    private RibbonButton? _layerButton;

    /// <summary>当前图层变化后, 刷新「图层▾」按钮上显示的图层名.
    /// 下拉列表不在这里动 — 每次点开都新建 MenuFlyout (见 BuildWideDropdown), 天然新鲜.</summary>
    public void RefreshLayerCombo()
    {
        if (_layerComboText is not null && CurrentLayerProvider is not null)
            _layerComboText.Text = CurrentLayerProvider();
    }

    /// <summary>动态层列表 + 静态管理项 (新建图层等). 构建时填一次, 之后每次点开前重建 —
    /// 不能在 Flyout.Opening 里填充: DropDownButton+MenuFlyout 组合下开弹时填充不渲染, 弹层不出.</summary>
    private List<Control> BuildLayerItems()
    {
        var items = new List<Control>();
        if (LayerListProvider is not null)
        {
            foreach (var name in LayerListProvider())
            {
                var mi = new MenuItem { Header = name };
                var captured = name;
                mi.Click += (_, _) => { OnLayerSelected?.Invoke(captured); RefreshLayerCombo(); };
                items.Add(mi);
            }
        }
        if (_layerButton?.Children is { Count: > 0 })
        {
            if (items.Count > 0) items.Add(new Separator());
            foreach (var child in _layerButton.Children)
            {
                var mi = new MenuItem { Header = child.Label };
                var act = child.OnClick;
                mi.Click += (_, _) => { act(); RefreshLayerCombo(); };
                items.Add(mi);
            }
        }
        return items;
    }

    private readonly StackPanel _qatBar;
    private readonly StackPanel _tabBar;
    private readonly StackPanel _contentArea;

    public RibbonControl()
    {
        _qatBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2,
            Margin = new Thickness(8, 2, 8, 2),
            VerticalAlignment = VerticalAlignment.Center
        };

        _tabBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2,
            Margin = new Thickness(8, 4, 8, 0)
        };

        _contentArea = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 0,
            Margin = new Thickness(8, 4, 8, 4)
        };

        var qatBorder = new Border
        {
            Background = QatBackground,
            BorderBrush = HeaderBorder,
            BorderThickness = new Thickness(0, 0, 0, 1),
            MinHeight = 26,
            Child = _qatBar
        };

        var tabBarBorder = new Border
        {
            Background = TabBarBackground,
            BorderBrush = BorderColor,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = _tabBar
        };

        var contentScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _contentArea
        };

        var contentBorder = new Border
        {
            Background = ContentBackground,
            BorderBrush = BorderColor,
            BorderThickness = new Thickness(0, 0, 0, 1),
            MinHeight = 110,
            Child = contentScroll
        };

        var rootGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto")
        };
        rootGrid.Children.Add(qatBorder);     Grid.SetRow(qatBorder, 0);
        rootGrid.Children.Add(tabBarBorder);  Grid.SetRow(tabBarBorder, 1);
        rootGrid.Children.Add(contentBorder); Grid.SetRow(contentBorder, 2);

        Content = rootGrid;
    }

    // ============================ QAT ============================

    public void AddQatButton(string label, string icon, string? tooltip, Action onClick)
    {
        var btn = new Button
        {
            Content = QatIcon(icon),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(5, 0),
            Height = 22,
            VerticalAlignment = VerticalAlignment.Center
        };
        ToolTip.SetTip(btn, tooltip ?? label);
        btn.Click += (_, _) => onClick();
        _qatBar.Children.Add(btn);
    }

    public void AddQatSeparator()
    {
        _qatBar.Children.Add(new Border
        {
            Width = 1,
            Height = 14,
            Background = new SolidColorBrush(Color.Parse("#CCCCCC")),
            Margin = new Thickness(3, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
    }

    public void AddQatMenu(string label, string icon, string? tooltip, List<(string Label, Action Action)> items)
    {
        var flyout = new MenuFlyout();
        var menuItems = new List<MenuItem>();
        foreach (var it in items)
        {
            var mi = new MenuItem { Header = it.Label };
            var act = it.Action;
            mi.Click += (_, _) => act();
            menuItems.Add(mi);
        }
        flyout.ItemsSource = menuItems;

        var btn = new DropDownButton
        {
            Content = QatIcon(icon),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(5, 0),
            Height = 22,
            VerticalAlignment = VerticalAlignment.Center,
            Flyout = flyout
        };
        ToolTip.SetTip(btn, tooltip ?? label);
        _qatBar.Children.Add(btn);
    }

    private static Control QatIcon(string icon) => new TextBlock
    {
        Text = icon,
        FontSize = 13,
        Foreground = IconColor,
        VerticalAlignment = VerticalAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Center
    };

    // ============================ Tabs ============================

    public RibbonTab AddTab(string title)
    {
        var tab = new RibbonTab(title);
        _tabs.Add(tab);

        var tabButton = new Button
        {
            Content = title,
            Padding = new Thickness(14, 6),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = Brushes.Black,
            FontSize = 14
        };
        tabButton.Click += (_, _) => ActivateTab(tab);
        _tabBar.Children.Add(tabButton);
        tab.TabButton = tabButton;

        if (_activeTab is null)
        {
            _activeTab = tab;
            tabButton.Background = ActiveTabColor;
        }
        return tab;
    }

    /// <summary>caller 应在 AddTab + AddGroup + AddButton 全部完成后调一次.</summary>
    public void RefreshActiveTab()
    {
        if (_activeTab is not null) ActivateTab(_activeTab);
    }

    private void ActivateTab(RibbonTab tab)
    {
        _activeTab = tab;
        _contentArea.Children.Clear();
        foreach (var group in tab.Groups)
            _contentArea.Children.Add(BuildGroupView(group));

        foreach (var t in _tabs)
            if (t.TabButton is not null)
                t.TabButton.Background = ReferenceEquals(t, tab) ? ActiveTabColor : Brushes.Transparent;
    }

    // ============================ Groups ============================

    private Control BuildGroupView(RibbonGroup group)
    {
        var iconBrush = group.AccentOptic ? OpticAccent : IconColor;

        Control body = group.IsLayerCombo
            ? BuildLayerComboBody(group, iconBrush)
            : BuildStandardBody(group, iconBrush);

        var groupLabel = new TextBlock
        {
            Text = group.AccentOptic ? "★ " + group.Title : group.Title,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 10,
            FontWeight = group.AccentOptic ? FontWeight.SemiBold : FontWeight.Normal,
            Foreground = group.AccentOptic ? OpticAccent : GroupTitleColor,
            Margin = new Thickness(0, 2, 0, 0)
        };

        // 组体统一高度: 否则 rows=1 的页签 (设置/帮助) 整条 ribbon 比主页矮十几像素,
        // 切页时画布/标签整体上下跳一下。内容按 Top 对齐, 组名始终落在同一条基线上。
        var bodyHost = new Panel { Height = BodyHeight, VerticalAlignment = VerticalAlignment.Top };
        bodyHost.Children.Add(body);

        var panel = new StackPanel { Orientation = Orientation.Vertical };
        panel.Children.Add(bodyHost);
        panel.Children.Add(groupLabel);

        return new Border
        {
            Padding = new Thickness(6, 2),
            Background = group.AccentOptic ? OpticBg : Brushes.Transparent,
            CornerRadius = group.AccentOptic ? new CornerRadius(5) : default,
            BorderBrush = group.AccentOptic ? OpticBgBorder : BorderColor,
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = panel
        };
    }

    /// <summary>列容量高度: rows*rowH 精确等高会被布局取整坑死 —— 非 100% DPI 下按钮 30 实测 30.4、
    /// 46 实测 46.4 (Avalonia 把控件对齐到设备像素), 3*30.4=91.2 &gt; 90 → 最后一行掉出去,
    /// 表现为"小按钮只有两行 / 文字按钮只有一行"。每行留 2px 余量 (远小于 rowH, 不会多塞一行)。</summary>
    private static double WrapHeight(int rows, int rowH) => rows * (rowH + 2);

    /// <summary>所有组的按钮区统一高 = 满编一组 (文字 2 行 / 小图标 3 行, 两者都是 96).</summary>
    private static readonly double BodyHeight = WrapHeight(2, 46);

    private Control BuildStandardBody(RibbonGroup group, IBrush iconBrush)
    {
        int rowH = group.Tier == RibbonTier.Compact ? 30 : 46;
        var wrap = new WrapPanel
        {
            Orientation = Orientation.Vertical,
            Height = WrapHeight(group.Rows, rowH),
            VerticalAlignment = VerticalAlignment.Top
        };
        foreach (var btn in group.Buttons)
            wrap.Children.Add(BuildButton(btn, group.Tier, iconBrush));
        return wrap;
    }

    /// <summary>图层横长条下拉占首行 + 下方密排尺寸图标 (复合组). Buttons[0]=图层(带 Children), 其余=尺寸.</summary>
    private Control BuildLayerComboBody(RibbonGroup group, IBrush iconBrush)
    {
        var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 3 };

        if (group.Buttons.Count > 0)
        {
            var layerBtn = group.Buttons[0];
            stack.Children.Add(BuildWideDropdown(layerBtn, iconBrush));
        }

        var dimWrap = new WrapPanel
        {
            Orientation = Orientation.Vertical,
            Height = WrapHeight(2, 30),
            VerticalAlignment = VerticalAlignment.Top
        };
        for (int i = 1; i < group.Buttons.Count; i++)
            dimWrap.Children.Add(BuildButton(group.Buttons[i], RibbonTier.Compact, iconBrush));
        stack.Children.Add(dimWrap);

        return stack;
    }

    private Control BuildWideDropdown(RibbonButton btn, IBrush iconBrush)
    {
        var label = new TextBlock
        {
            Text = CurrentLayerProvider is not null ? CurrentLayerProvider() : btn.Label,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center
        };
        _layerComboText = label;

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        row.Children.Add(BuildIcon(btn, 13, iconBrush));
        row.Children.Add(label);
        row.Children.Add(new TextBlock
        {
            Text = "▾",
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.Parse("#666666")),
            VerticalAlignment = VerticalAlignment.Center
        });

        // 普通 Button + 点击时手动 new MenuFlyout + ShowAt — 不用 DropDownButton:
        //   1) DropDownButton 吞 Click, 无法在开弹前重建列表;
        //   2) MenuFlyout presenter 首开快照 items, 复用同一 flyout 改 ItemsSource 不刷新
        //      (实测: 新增图层后列表死活是旧的)。每开必新建 = 列表永远新鲜。
        var dd = new Button
        {
            Content = row,
            MinWidth = 96,
            Height = 26,
            Padding = new Thickness(6, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Background = new SolidColorBrush(Color.Parse("#FAFCFE")),
            BorderBrush = HeaderBorder,
            BorderThickness = new Thickness(1)
        };

        if (LayerListProvider is not null || btn.Children is { Count: > 0 })
        {
            _layerButton = btn;
            dd.Click += (_, _) =>
            {
                var flyout = new MenuFlyout { ItemsSource = BuildLayerItems() };
                flyout.ShowAt(dd);
            };
        }

        ToolTip.SetTip(dd, "当前绘图图层 — 点选切换, 新建实体归此层");
        return dd;
    }

    // ============================ Buttons ============================

    private Control BuildButton(RibbonButton btn, RibbonTier tier, IBrush iconBrush)
    {
        // 有下拉子项 → SplitButton (主按钮有动作) / DropDownButton (纯触发)
        if (btn.Children is { Count: > 0 })
            return BuildDropdownButton(btn, tier, iconBrush);

        return tier == RibbonTier.Compact
            ? BuildCompactButton(btn, iconBrush)
            : BuildLabeledButton(btn, iconBrush);
    }

    /// <summary>Compact: 纯小图标 (无文字), 名称走 tooltip. 30×30.</summary>
    private Control BuildCompactButton(RibbonButton btn, IBrush iconBrush)
    {
        var button = new Button
        {
            Content = BuildIcon(btn, 16, iconBrush),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Height = 30,
            Width = 30,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        ToolTip.SetTip(button, btn.Tooltip ?? btn.Label);
        button.Click += (_, _) => btn.OnClick();
        return button;
    }

    /// <summary>Labeled: 图标在上 + 文字在下的中按钮. ~58×46.</summary>
    private Control BuildLabeledButton(RibbonButton btn, IBrush iconBrush)
    {
        var col = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 2
        };
        col.Children.Add(BuildIcon(btn, 18, iconBrush));
        col.Children.Add(new TextBlock
        {
            Text = btn.Label,
            FontSize = 10,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brushes.Black
        });

        var button = new Button
        {
            Content = col,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(2, 0),
            Width = 58,
            Height = 46,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        ToolTip.SetTip(button, btn.Tooltip ?? btn.Label);
        button.Click += (_, _) => btn.OnClick();
        return button;
    }

    private Control BuildDropdownButton(RibbonButton btn, RibbonTier tier, IBrush iconBrush)
    {
        Control content = tier == RibbonTier.Compact
            ? BuildIcon(btn, 16, iconBrush)
            : LabeledContent(btn, iconBrush);

        var flyout = BuildFlyout(btn.Children!);

        if (btn.DropdownOnly)
        {
            var dd = new DropDownButton
            {
                Content = content,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(2, 0),
                Height = tier == RibbonTier.Compact ? 30 : 46,
                Flyout = flyout,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            ToolTip.SetTip(dd, btn.Tooltip ?? btn.Label);
            return dd;
        }

        var split = new SplitButton
        {
            Content = content,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(2, 0),
            Height = tier == RibbonTier.Compact ? 30 : 46,
            Flyout = flyout,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        split.Click += (_, _) => btn.OnClick();
        ToolTip.SetTip(split, btn.Tooltip ?? btn.Label);
        return split;
    }

    private Control LabeledContent(RibbonButton btn, IBrush iconBrush)
    {
        var col = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 2
        };
        col.Children.Add(BuildIcon(btn, 18, iconBrush));
        col.Children.Add(new TextBlock
        {
            Text = btn.Label,
            FontSize = 10,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brushes.Black
        });
        return col;
    }

    private MenuFlyout BuildFlyout(List<RibbonButton> children)
    {
        var items = new List<MenuItem>();
        foreach (var child in children)
        {
            var mi = new MenuItem { Header = child.Label };
            var act = child.OnClick;
            mi.Click += (_, _) => act();
            items.Add(mi);
        }
        return new MenuFlyout { ItemsSource = items };
    }

    /// <summary>构建按钮图标: 有 SVG 路径用矢量图, 否则回退字形/文字. size 为图标边长.</summary>
    private Control BuildIcon(RibbonButton btn, double size, IBrush iconBrush)
    {
        if (!string.IsNullOrEmpty(btn.IconSvgPath))
        {
            try
            {
                var source = global::Avalonia.Svg.Skia.SvgSource.Load(btn.IconSvgPath!, null);
                if (source is not null)
                {
                    return new Image
                    {
                        Source = new global::Avalonia.Svg.Skia.SvgImage { Source = source },
                        Width = size,
                        Height = size,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }
            }
            catch { /* SVG 加载失败 → 回退字形 */ }
        }

        return new TextBlock
        {
            Text = btn.Icon,
            FontSize = size * 0.9,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = iconBrush
        };
    }
}
