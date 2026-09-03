using System;
using System.Collections.Generic;

namespace OtoCAD.Avalonia.PropertyPanel;

/// <summary>
/// 属性区"可加指标"目录——点中某区(面/材料)时,属性面板据此列出可勾选添加的指标。
/// 按区类型(表面 / 材料)给出指标的显示标签、行内匹配前缀、添加时的默认行文本。
/// 记法的逐项格式后续按客户自定标准细化;此处只提供可加项与可识别前缀。
/// </summary>
public static class PropertyZoneCatalog
{
    public readonly record struct Indicator(string Label, string Prefix, string Default);

    // 面属性区(前/后表面)可加指标
    private static readonly Indicator[] Surface =
    {
        new("曲率半径 R", "R", "R = "),
        new("有效口径 Φe", "Øe", "Øe "),
        new("倒角 Chamfer", "Chamfer", "Chamfer "),
        new("面形 3/", "3/", "3/ "),
        new("中心偏差 4/", "4/", "4/ "),
        new("疵病 5/", "5/", "5/ "),
        // 纹理无 slash 代号(6/ 是激光损伤, ISO 10110-10 代号表): 用 G/P (ISO 10110-8 / GB13323 附录C)
        new("纹理·磨砂 G", "G", "G "),
        new("纹理·抛光 P", "P", "P "),
    };

    // 材料属性区可加指标
    private static readonly Indicator[] Material =
    {
        new("折射率 n_d", "n_d", "n_d "),
        new("阿贝数 v_d", "v_d", "v_d "),
        new("应力双折射 0/", "0/", "0/ "),
        new("气泡 1/", "1/", "1/ "),
        new("不均匀 2/", "2/", "2/ "),
    };

    /// <summary>按区标题判定区类型(含"材料"/"MATERIAL"→材料,否则表面),返回可加指标列表。</summary>
    public static IReadOnlyList<Indicator> For(string zoneTitle)
    {
        var t = zoneTitle ?? "";
        bool isMaterial = t.Contains("材料") ||
                          t.IndexOf("MATERIAL", StringComparison.OrdinalIgnoreCase) >= 0;
        return isMaterial ? Material : Surface;
    }

    /// <summary>
    /// 按规范顺序计算新增指标的插入下标:让目录内指标保持目录顺序,
    /// 未识别(自定义)行视作排在目录项之后。返回应插入的行下标。
    /// </summary>
    public static int SortedInsertIndex(string zoneTitle, IReadOnlyList<string> rows, Indicator adding)
    {
        var cat = For(zoneTitle);
        int addOrder = OrderInCatalog(cat, adding.Prefix);
        for (int i = 0; i < rows.Count; i++)
        {
            if (RowOrder(cat, rows[i]) > addOrder) return i;   // 在第一个"序号更大"的行前插入
        }
        return rows.Count;
    }

    private static int OrderInCatalog(IReadOnlyList<Indicator> cat, string prefix)
    {
        for (int i = 0; i < cat.Count; i++)
            if (string.Equals(cat[i].Prefix, prefix, StringComparison.OrdinalIgnoreCase)) return i;
        return int.MaxValue;
    }

    private static int RowOrder(IReadOnlyList<Indicator> cat, string row)
    {
        var t = (row ?? "").TrimStart();
        for (int i = 0; i < cat.Count; i++)
            if (!string.IsNullOrEmpty(cat[i].Prefix) &&
                t.StartsWith(cat[i].Prefix, StringComparison.OrdinalIgnoreCase)) return i;
        return int.MaxValue;   // 未识别行排在所有目录项之后
    }
}
