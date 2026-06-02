using System;
using System.IO;
using System.Text.Json;

namespace OtoCAD.Avalonia.Ribbon;

/// <summary>
/// RibbonLayoutConfig 加载 + 应用到 RibbonControl.
/// 命令派发委托给 <paramref name="commandResolver"/> (Phase 1.5 接入 CommandRegistry).
/// </summary>
public static class RibbonConfigLoader
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static RibbonLayoutConfig LoadFromFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Ribbon 配置不存在: {path}", path);
        return LoadFromText(File.ReadAllText(path));
    }

    public static RibbonLayoutConfig LoadFromText(string json)
    {
        var cfg = JsonSerializer.Deserialize<RibbonLayoutConfig>(json, JsonOpts);
        return cfg ?? new RibbonLayoutConfig();
    }

    /// <summary>
    /// 应用配置到 ribbon 控件.
    /// </summary>
    /// <param name="ribbon">目标 RibbonControl</param>
    /// <param name="config">已加载的布局</param>
    /// <param name="commandResolver">命令名 → 点击回调; 未识别命令返回占位 (写入 onUnknown).</param>
    /// <param name="onUnknown">未识别命令名时的回调 (传入命令名), 不抛异常.</param>
    public static void Apply(
        RibbonControl ribbon,
        RibbonLayoutConfig config,
        Func<string, Action?> commandResolver,
        Action<string>? onUnknown = null)
    {
        foreach (var tabCfg in config.Tabs)
        {
            var tab = ribbon.AddTab(tabCfg.Title);
            foreach (var groupCfg in tabCfg.Groups)
            {
                var group = tab.AddGroup(groupCfg.Title);
                group.IconOnly = groupCfg.IconOnly;
                foreach (var btnCfg in groupCfg.Buttons)
                {
                    var resolved = commandResolver(btnCfg.Command);
                    Action onClick = resolved ?? (() => onUnknown?.Invoke(btnCfg.Command));
                    var btn = group.AddButton(btnCfg.Label, btnCfg.Icon, onClick);
                    // SVG 矢量图标 (命令名解析); 未映射回退到 btnCfg.Icon (emoji)
                    btn.IconSvgPath = IconResolver.Resolve(btnCfg.Command);
                    // Phase 1F: tooltip 显示命令名 + (Phase 2 缺/未实现) 标记
                    btn.Tooltip = resolved is null
                        ? $"{btnCfg.Label}  ·  {btnCfg.Command} (Phase 2 待接入)"
                        : $"{btnCfg.Label}  ·  {btnCfg.Command}";
                }
            }
        }
        // 必须: AddTab 第一次只占位不渲染内容 (那时 Groups 还空),
        // 全部加完后这里触发首 Tab 的真正 hydration.
        ribbon.RefreshActiveTab();
    }
}
