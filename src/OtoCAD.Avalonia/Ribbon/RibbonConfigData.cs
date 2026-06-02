using System.Collections.Generic;

namespace OtoCAD.Avalonia.Ribbon;

/// <summary>
/// Ribbon 布局 JSON 反序列化目标 (POCO records).
/// 配套加载: <see cref="RibbonConfigLoader"/>.
/// </summary>
public sealed class RibbonLayoutConfig
{
    public string Version { get; set; } = "1.0";
    public List<RibbonTabConfig> Tabs { get; set; } = new();
}

public sealed class RibbonTabConfig
{
    public string Title { get; set; } = "";
    public List<RibbonGroupConfig> Groups { get; set; } = new();
}

public sealed class RibbonGroupConfig
{
    public string Title { get; set; } = "";

    /// <summary>true = 纯小图标三个一列; false (默认) = 图标+文字横排.</summary>
    public bool IconOnly { get; set; }

    public List<RibbonButtonConfig> Buttons { get; set; } = new();
}

public sealed class RibbonButtonConfig
{
    public string Label { get; set; } = "";
    public string Icon { get; set; } = "";

    /// <summary>命令名, 经 CommandRegistry 解析到具体 Action (Phase 1.5).</summary>
    public string Command { get; set; } = "";
}
