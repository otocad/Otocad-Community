using System;
using System.IO;
using lcdb.Checklist;

namespace OtoCAD.Avalonia.Services;

/// <summary>
/// 出图清单规则文件加载 — 从 <c>Config/Checklists/*.yaml</c> 读取并解析。
///
/// 每次调用都重读文件 (热加载: 编辑 YAML 无需重启即生效, 文件小开销可忽略)。
/// 不做缓存 / 单例 / Manager — 仅一个静态加载点。
/// </summary>
public static class ChecklistService
{
    private const string DefaultFileName = "gb-singlet-default.yaml";

    /// <summary>加载默认 GB 单透镜清单。文件缺失/解析失败返回 null (清单面板隐藏)。</summary>
    public static ChecklistDefinition? LoadDefault()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Config", "Checklists", DefaultFileName);
            return File.Exists(path) ? ChecklistDefinition.Parse(File.ReadAllText(path)) : null;
        }
        catch
        {
            return null;
        }
    }
}
