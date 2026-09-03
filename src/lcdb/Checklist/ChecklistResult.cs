using System.Collections.Generic;
using System.Linq;

namespace lcdb.Checklist;

/// <summary>出图清单单项的判定状态。</summary>
public enum CheckStatus
{
    /// <summary>通过。</summary>
    Pass,
    /// <summary>未通过 (缺要素 / 重叠 / 出框 …)。</summary>
    Fail,
    /// <summary>不适用 (条件不满足, 如"非球面标注"但无非球面)。视同通过, 不阻塞交付。</summary>
    NotApplicable,
}

/// <summary>单项严重度: Error = 不能交付 (拒收级); Warning = 建议修。</summary>
public enum CheckSeverity
{
    Error,
    Warning,
}

/// <summary>一条出图清单项的判定结果 (评估产物, 不参与序列化)。</summary>
public sealed class ChecklistItemResult
{
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";
    public CheckStatus Status { get; init; }
    public CheckSeverity Severity { get; init; }

    /// <summary>人读详情 (如"非球面 2 个, ISO10110-12 标注 1 个 (缺 1)")。</summary>
    public string Detail { get; init; } = "";

    /// <summary>
    /// 重建动作名 (来自 YAML <c>regen</c>); 由 UI 层映射到实际操作 (如 "auto-dims")。
    /// 空 = 此项无法一键重出 (需人工处理, 如填属性区)。
    /// </summary>
    public string Regen { get; init; } = "";

    /// <summary>是否阻塞交付 = Error 级且未通过。</summary>
    public bool IsBlocking => Severity == CheckSeverity.Error && Status == CheckStatus.Fail;
}

/// <summary>整张图纸对一份清单的评估结果。</summary>
public sealed class ChecklistResult
{
    public string Name { get; init; } = "";
    public IReadOnlyList<ChecklistItemResult> Items { get; init; } = new List<ChecklistItemResult>();

    /// <summary>无任何 Error 级未通过项 = 可交付。</summary>
    public bool CanDeliver => !Items.Any(i => i.IsBlocking);

    public int FailCount => Items.Count(i => i.Status == CheckStatus.Fail);
    public int WarningCount => Items.Count(i => i.Status == CheckStatus.Fail && i.Severity == CheckSeverity.Warning);

    /// <summary>需要一键重出的、未通过且有 regen 动作的项 (去重的动作名)。</summary>
    public IReadOnlyList<string> RegenActions => Items
        .Where(i => i.Status == CheckStatus.Fail && !string.IsNullOrEmpty(i.Regen))
        .Select(i => i.Regen)
        .Distinct()
        .ToList();
}
