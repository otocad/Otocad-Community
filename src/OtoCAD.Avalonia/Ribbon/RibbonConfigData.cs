using System.Collections.Generic;

namespace OtoCAD.Avalonia.Ribbon;

/// <summary>
/// Ribbon 布局 JSON 反序列化目标 (POCO records).
/// 配套加载: <see cref="RibbonConfigLoader"/>.
///
/// v2.0 (2026-06 重构): 单主页+长尾次级页 IA。新增字段:
///   - Qat: 跨页常驻快速访问条
///   - Group.Tier (compact/labeled) / Rows / Accent (optic) / Type (layerCombo)
///   - Button.Children (▾下拉子项) / Menu (聚合菜单命令名)
///   - Tab.DevOnly (仅开发者模式可见)
/// </summary>
public sealed class RibbonLayoutConfig
{
    public string Version { get; set; } = "1.0";
    public RibbonQatConfig? Qat { get; set; }
    public List<RibbonTabConfig> Tabs { get; set; } = new();
}

/// <summary>QAT 快速访问条 (跨页常驻).</summary>
public sealed class RibbonQatConfig
{
    public List<RibbonQatItemConfig> Items { get; set; } = new();
}

public sealed class RibbonQatItemConfig
{
    /// <summary>"separator" = 竖分隔; 否则普通命令/聚合菜单按钮.</summary>
    public string? Type { get; set; }
    public string Label { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Command { get; set; } = "";
    /// <summary>非空 = 聚合菜单 (如"导出"→PNG/DXF/PDF), 列出子命令名.</summary>
    public List<string>? Menu { get; set; }
}

public sealed class RibbonTabConfig
{
    public string Title { get; set; } = "";
    /// <summary>true = 仅开发者模式可见 (承接被移除的 dev/benchmark 项).</summary>
    public bool DevOnly { get; set; }
    public List<RibbonGroupConfig> Groups { get; set; } = new();
}

public sealed class RibbonGroupConfig
{
    public string Title { get; set; } = "";

    /// <summary>true = 纯小图标三个一列 (向后兼容; 等价 Tier=compact).</summary>
    public bool IconOnly { get; set; }

    /// <summary>"compact" = 小图标密排 | "labeled" = 中图标+文字两行. 缺省按 IconOnly 推断.</summary>
    public string? Tier { get; set; }

    /// <summary>每列容量 (column-wrap): compact 默认 3, labeled 默认 2.</summary>
    public int Rows { get; set; }

    /// <summary>"optic" = 光学主角 (浅蓝底+蓝图标+★标题); 缺省 "none".</summary>
    public string? Accent { get; set; }

    /// <summary>"layerCombo" = 图层横长条下拉占首行 + 下方密排尺寸图标的复合组.</summary>
    public string? Type { get; set; }

    public List<RibbonButtonConfig> Buttons { get; set; } = new();
}

public sealed class RibbonButtonConfig
{
    public string Label { get; set; } = "";
    public string Icon { get; set; } = "";

    /// <summary>命令名, 经 CommandRegistry 解析到具体 Action.</summary>
    public string Command { get; set; } = "";

    /// <summary>非空 = ▾下拉子项 (长尾变体). 主按钮执行 Command (若有), ▾ 展开 Children.</summary>
    public List<RibbonButtonConfig>? Children { get; set; }
}
