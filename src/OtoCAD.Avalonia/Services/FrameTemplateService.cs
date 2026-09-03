#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using lcdb.DrawingFrame;

namespace OtoCAD.Avalonia.Services;

/// <summary>
/// 图框模板键解析 — 出图惯例 (<c>DrawingConvention.FrameTemplateKey</c>) 的图框轴落点.
///
/// 内置键 <c>gb-lens</c> / <c>iso-lens</c> 仍走硬编码图框类 (数据驱动预设同名文件留待 2b 金样对齐后替换);
/// 其它键按 <c>Config/Frames/*.json</c> 里的定义名 (大小写不敏感) 解析成 <see cref="FrameDefinition"/>,
/// 出图引擎据此用 <see cref="DataDrivenFrame"/> 出图 —— 用户自定义图框由此进入"一键出图".
/// 热加载: 每次解析都重读目录 (与清单规则同策略), 改 JSON 无需重启.
/// </summary>
public static class FrameTemplateService
{
    public static readonly string[] BuiltInKeys = { "gb-lens", "iso-lens" };

    public static string PresetsDir => Path.Combine(AppContext.BaseDirectory, "Config", "Frames");

    public static bool IsBuiltIn(string? key)
        => key is not null && Array.Exists(BuiltInKeys, k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>可选图框键: 内置 + 目录里合法的数据驱动定义名 (与内置同名者除外).</summary>
    public static IReadOnlyList<string> AllKeys()
    {
        var keys = new List<string>(BuiltInKeys);
        foreach (var def in LoadAll())
            if (!IsBuiltIn(def.Name) && !keys.Contains(def.Name)) keys.Add(def.Name);
        return keys;
    }

    /// <summary>按定义名解析数据驱动图框定义; 内置键 / 空 / 找不到 / 定义非法 → null (调用方回落硬编码图框).</summary>
    public static FrameDefinition? Resolve(string? key)
    {
        if (string.IsNullOrWhiteSpace(key) || IsBuiltIn(key)) return null;
        foreach (var def in LoadAll())
            if (string.Equals(def.Name, key, StringComparison.OrdinalIgnoreCase)) return def;
        return null;
    }

    /// <summary>当前出图惯例的图框键 → 数据驱动定义 (无则 null).</summary>
    public static FrameDefinition? ResolveActive()
        => Resolve(lcdb.Standards.DrawingConventionService.Active?.FrameTemplateKey);

    /// <summary>读目录下全部合法定义 (单个文件坏了只跳过并记日志, 不影响其它).</summary>
    private static IEnumerable<FrameDefinition> LoadAll()
    {
        if (!Directory.Exists(PresetsDir)) yield break;
        foreach (var file in Directory.EnumerateFiles(PresetsDir, "*.json"))
        {
            FrameDefinition? def = null;
            try
            {
                var d = FrameDefinition.Load(file);
                var errors = d.Validate();
                if (errors.Count == 0) def = d;
                else System.Diagnostics.Debug.WriteLine($"[FrameTemplateService] {file} 定义有误: {string.Join("; ", errors)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FrameTemplateService] {file} 读取失败: {ex.Message}");
            }
            if (def is not null) yield return def;
        }
    }
}
