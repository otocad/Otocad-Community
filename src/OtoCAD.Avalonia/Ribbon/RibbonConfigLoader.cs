using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace OtoCAD.Avalonia.Ribbon;

/// <summary>
/// RibbonLayoutConfig 加载 + 应用到 RibbonControl.
/// 命令派发委托给 commandResolver (CommandRegistry).
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
    /// <param name="commandResolver">命令名 → 点击回调; 未识别返回 null.</param>
    /// <param name="onUnknown">未识别命令名时的回调 (传入命令名), 不抛异常.</param>
    /// <param name="devMode">true 时显示 DevOnly 页签 (承接 dev/benchmark 项).</param>
    public static void Apply(
        RibbonControl ribbon,
        RibbonLayoutConfig config,
        Func<string, Action?> commandResolver,
        Action<string>? onUnknown = null,
        bool devMode = false)
    {
        Action Bind(string command) =>
            commandResolver(command) ?? (() => onUnknown?.Invoke(command));

        string Tip(string label, string command) =>
            commandResolver(command) is null
                ? $"{label}  ·  {command} (待接入)"
                : $"{label}  ·  {command}";

        // ---- QAT (跨页常驻) ----
        if (config.Qat is not null)
        {
            foreach (var item in config.Qat.Items)
            {
                if (string.Equals(item.Type, "separator", StringComparison.OrdinalIgnoreCase))
                {
                    ribbon.AddQatSeparator();
                    continue;
                }
                if (item.Menu is { Count: > 0 })
                {
                    var sub = item.Menu
                        .Select(c => (Label: c, Action: Bind(c)))
                        .ToList();
                    ribbon.AddQatMenu(item.Label, item.Icon, $"{item.Label} ▾", sub);
                }
                else
                {
                    ribbon.AddQatButton(item.Label, item.Icon, Tip(item.Label, item.Command), Bind(item.Command));
                }
            }
        }

        // ---- 页签 / 组 / 按钮 ----
        foreach (var tabCfg in config.Tabs)
        {
            if (tabCfg.DevOnly && !devMode) continue;   // 生产界面隐藏 dev 页签

            var tab = ribbon.AddTab(tabCfg.Title);
            tab.DevOnly = tabCfg.DevOnly;

            foreach (var groupCfg in tabCfg.Groups)
            {
                var group = tab.AddGroup(groupCfg.Title);
                group.IconOnly = groupCfg.IconOnly;
                group.AccentOptic = string.Equals(groupCfg.Accent, "optic", StringComparison.OrdinalIgnoreCase);
                group.IsLayerCombo = string.Equals(groupCfg.Type, "layerCombo", StringComparison.OrdinalIgnoreCase);

                // Tier: 显式 > IconOnly 推断 > 默认(光学/普通各取所宜)
                group.Tier = groupCfg.Tier?.ToLowerInvariant() switch
                {
                    "compact" => RibbonTier.Compact,
                    "labeled" => RibbonTier.Labeled,
                    _ => groupCfg.IconOnly ? RibbonTier.Compact : RibbonTier.Labeled
                };
                group.Rows = groupCfg.Rows > 0
                    ? groupCfg.Rows
                    : (group.Tier == RibbonTier.Compact ? 3 : 2);

                foreach (var btnCfg in groupCfg.Buttons)
                    group.Buttons.Add(BuildButton(btnCfg, Bind, Tip));
            }
        }

        ribbon.RefreshActiveTab();
    }

    private static RibbonButton BuildButton(
        RibbonButtonConfig cfg,
        Func<string, Action> bind,
        Func<string, string, string> tip)
    {
        bool dropdownOnly = string.IsNullOrEmpty(cfg.Command);
        Action onClick = dropdownOnly ? (() => { }) : bind(cfg.Command);

        var btn = new RibbonButton(cfg.Label, cfg.Icon, onClick)
        {
            IconSvgPath = IconResolver.Resolve(cfg.Command),
            Tooltip = dropdownOnly ? cfg.Label : tip(cfg.Label, cfg.Command),
            DropdownOnly = dropdownOnly
        };

        if (cfg.Children is { Count: > 0 })
            btn.Children = cfg.Children.Select(c => BuildButton(c, bind, tip)).ToList();

        return btn;
    }
}
