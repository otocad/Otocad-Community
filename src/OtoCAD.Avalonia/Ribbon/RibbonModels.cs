using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace OtoCAD.Avalonia.Ribbon;

/// <summary>
/// Ribbon 数据模型: Tab → Group → Button 三层结构.
/// POC-03 阶段不引 MVVM, 用简单数据类 + 事件直接绑定.
/// </summary>
public sealed class RibbonTab
{
    public string Title { get; }
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

public sealed class RibbonGroup
{
    public string Title { get; }
    public List<RibbonButton> Buttons { get; } = new();

    /// <summary>true = 纯小图标三个一列 (几何图元等可辨识图标);
    /// false = 图标+文字横排 (标记类抽象符号, 需文字辨识).</summary>
    public bool IconOnly { get; set; }

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

    /// <summary>SVG 矢量图标绝对路径; null 时回退到 <see cref="Icon"/> (emoji/文字).</summary>
    public string? IconSvgPath { get; set; }

    public RibbonButton(string label, string icon, Action onClick)
    {
        Label = label;
        Icon = icon;
        OnClick = onClick;
    }
}
