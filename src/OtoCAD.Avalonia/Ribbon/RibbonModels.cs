using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace OtoCAD.Avalonia.Ribbon;

/// <summary>
/// Ribbon 数据模型: Tab → Group → Button 三层结构.
/// 不引 MVVM, 用简单数据类 + 事件直接绑定.
/// </summary>
public sealed class RibbonTab
{
    public string Title { get; }
    public bool DevOnly { get; set; }
    public List<RibbonGroup> Groups { get; } = new();

    /// <summary>UI 层暂存的 Tab 按钮引用, 用于切换时高亮.</summary>
    internal Button? TabButton { get; set; }

    public RibbonTab(string title) { Title = title; }

    public RibbonGroup AddGroup(string title)
    {
        var g = new RibbonGroup(title);
        Groups.Add(g);
        return g;
    }
}

/// <summary>组按钮渲染分级.</summary>
public enum RibbonTier { Compact, Labeled }

public sealed class RibbonGroup
{
    public string Title { get; }
    public List<RibbonButton> Buttons { get; } = new();

    /// <summary>向后兼容: 等价 Tier=Compact.</summary>
    public bool IconOnly { get; set; }

    /// <summary>Compact = 纯小图标密排 | Labeled = 中图标+文字两行.</summary>
    public RibbonTier Tier { get; set; } = RibbonTier.Labeled;

    /// <summary>每列容量 (column-wrap).</summary>
    public int Rows { get; set; } = 3;

    /// <summary>true = 光学主角 (浅蓝底+蓝图标+★标题).</summary>
    public bool AccentOptic { get; set; }

    /// <summary>true = 图层横长条下拉占首行 + 下方密排尺寸图标 (复合组).</summary>
    public bool IsLayerCombo { get; set; }

    public RibbonGroup(string title) { Title = title; }

    public RibbonButton AddButton(string label, string icon, Action onClick)
    {
        var btn = new RibbonButton(label, icon, onClick);
        Buttons.Add(btn);
        return btn;
    }
}

public sealed class RibbonButton
{
    public string Label { get; }
    public string Icon { get; }
    public Action OnClick { get; }

    /// <summary>悬停提示 (默认 = Label, 调用方可注入命令名/快捷键).</summary>
    public string? Tooltip { get; set; }

    /// <summary>SVG 矢量图标绝对路径; null 时回退到 <see cref="Icon"/> (字形/文字).</summary>
    public string? IconSvgPath { get; set; }

    /// <summary>非空 = ▾下拉子项 (长尾变体).</summary>
    public List<RibbonButton>? Children { get; set; }

    /// <summary>true = 主按钮无实际默认动作 (纯下拉触发器, 如 标准▾/公差▾).</summary>
    public bool DropdownOnly { get; set; }

    public RibbonButton(string label, string icon, Action onClick)
    {
        Label = label;
        Icon = icon;
        OnClick = onClick;
    }
}
