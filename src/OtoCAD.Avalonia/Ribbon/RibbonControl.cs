using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace OtoCAD.Avalonia.Ribbon;

/// <summary>
/// 自研 Ribbon 控件 (POC-03).
///
/// 设计目标:
///   - 视觉接近 Office 2019 Fluent (白底, 浅蓝高亮)
///   - 支持多 Tab, 每 Tab 多 Group, 每 Group 多按钮
///   - 仅做"大按钮" (64×80) 一种规格, 不实现大/中/小切换
///   - 不引 MVVM, 直接事件绑定
///
/// 不做的事 (out of scope, 后续 Phase 1 再加):
///   - Backstage 菜单 / QAT / KeyTips / 折叠动画 / 主题切换 / 工具提示
/// </summary>
public sealed class RibbonControl : UserControl
{
    private static readonly IBrush TabBarBackground   = new SolidColorBrush(Color.Parse("#F3F3F3"));
    private static readonly IBrush ContentBackground  = Brushes.White;
    private static readonly IBrush BorderColor        = new SolidColorBrush(Color.Parse("#E0E0E0"));
    private static readonly IBrush ActiveTabColor     = new SolidColorBrush(Color.Parse("#E5F1FB"));
    private static readonly IBrush GroupTitleColor    = new SolidColorBrush(Color.Parse("#666666"));
    private static readonly IBrush IconColor          = new SolidColorBrush(Color.Parse("#333333"));

    private readonly List<RibbonTab> _tabs = new();
    private RibbonTab? _activeTab;

    private readonly StackPanel _tabBar;
    private readonly StackPanel _contentArea;

    public RibbonControl()
    {
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
            MinHeight = 110,  // 3 行小图标 + 组标题 (26*3 + 间距 + 标题)
            Child = contentScroll
        };

        var rootGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto")
        };
        rootGrid.Children.Add(tabBarBorder);
        Grid.SetRow(tabBarBorder, 0);
        rootGrid.Children.Add(contentBorder);
        Grid.SetRow(contentBorder, 1);

        Content = rootGrid;
    }

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

        // 第一个 Tab: 只占位 + 高亮, 不展开内容. 因为此时 tab.Groups 还是空的
        // (caller 后续才 AddGroup/AddButton). 真正的 hydration 由
        // RefreshActiveTab() 在 caller 全部 AddGroup 完后触发.
        if (_activeTab is null)
        {
            _activeTab = tab;
            tabButton.Background = ActiveTabColor;
        }
        return tab;
    }

    /// <summary>
    /// 重新渲染当前激活 Tab 的内容. caller (RibbonConfigLoader.Apply / BuildRibbon)
    /// 应在 AddTab + AddGroup + AddButton 全部完成后调一次, 否则首个 Tab 显示空白.
    /// </summary>
    public void RefreshActiveTab()
    {
        if (_activeTab is not null) ActivateTab(_activeTab);
    }

    private void ActivateTab(RibbonTab tab)
    {
        _activeTab = tab;
        _contentArea.Children.Clear();
        foreach (var group in tab.Groups)
        {
            _contentArea.Children.Add(BuildGroupView(group));
        }
        // 高亮
        foreach (var t in _tabs)
            if (t.TabButton is not null)
                t.TabButton.Background = ReferenceEquals(t, tab) ? ActiveTabColor : Brushes.Transparent;
    }

    private Control BuildGroupView(RibbonGroup group)
    {
        // 按组定制: IconOnly 组用纯小图标 (几何图元可辨识),
        // 其余用图标+文字横排 (标记类抽象符号需文字辨识). 均三个一列.
        var buttonsWrap = new WrapPanel
        {
            Orientation = Orientation.Vertical,   // 列优先: 填满 3 行再换列
            Height = 3 * 28,                       // 每列最多 3 个
            VerticalAlignment = VerticalAlignment.Top
        };
        if (group.IconOnly)
        {
            buttonsWrap.ItemWidth = 30;
            buttonsWrap.ItemHeight = 28;
            foreach (var btn in group.Buttons)
                buttonsWrap.Children.Add(BuildButtonView(btn));
        }
        else
        {
            buttonsWrap.ItemHeight = 28;           // 列宽自适应文字, 不设 ItemWidth
            foreach (var btn in group.Buttons)
                buttonsWrap.Children.Add(BuildLabeledButton(btn));
        }

        var groupLabel = new TextBlock
        {
            Text = group.Title,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 10,
            Foreground = GroupTitleColor,
            Margin = new Thickness(0, 2, 0, 0)
        };

        var panel = new StackPanel { Orientation = Orientation.Vertical };
        panel.Children.Add(buttonsWrap);
        panel.Children.Add(groupLabel);

        return new Border
        {
            Padding = new Thickness(4, 2),
            BorderBrush = BorderColor,
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = panel
        };
    }

    /// <summary>次要按钮: 纯小图标 (无文字), 名称走 tooltip. 三个一列.</summary>
    private Control BuildButtonView(RibbonButton btn)
    {
        Control icon = BuildIcon(btn, 18);

        var button = new Button
        {
            Content = icon,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Height = 26,
            Width = 28
        };
        ToolTip.SetTip(button, btn.Tooltip ?? btn.Label);
        button.Click += (_, _) => btn.OnClick();
        return button;
    }

    /// <summary>带文字按钮: 图标在左 + 文字在右的横排小按钮 (标记类组, 需文字辨识).</summary>
    private Control BuildLabeledButton(RibbonButton btn)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 4
        };
        row.Children.Add(BuildIcon(btn, 16));
        row.Children.Add(new TextBlock
        {
            Text = btn.Label,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.Black
        });

        var button = new Button
        {
            Content = row,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4, 0),
            Height = 26,
            HorizontalContentAlignment = HorizontalAlignment.Left
        };
        ToolTip.SetTip(button, btn.Tooltip ?? btn.Label);
        button.Click += (_, _) => btn.OnClick();
        return button;
    }

    /// <summary>构建按钮图标: 有 SVG 路径用矢量图, 否则回退 emoji/文字. size 为图标边长.</summary>
    private Control BuildIcon(RibbonButton btn, double size)
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
            catch { /* SVG 加载失败 → 回退 emoji */ }
        }

        return new TextBlock
        {
            Text = btn.Icon,
            FontSize = size * 0.9,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = IconColor
        };
    }
}
