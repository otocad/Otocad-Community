using System.Collections.Generic;
using System.Linq;

namespace lcdb.Drawing;

/// <summary>
/// 一条图纸属性 = 属性区/标题栏里的一行。
/// <see cref="Key"/> 稳定标识 (清单按键查、重出按键回填); <see cref="Label"/> 显示名;
/// <see cref="Value"/> 已格式化的渲染值串 (代号内嵌, 与现属性区行字符串一致)。
/// </summary>
public sealed class DrawingProperty
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";

    public DrawingProperty() { }
    public DrawingProperty(string key, string label, string value)
    {
        Key = key; Label = label; Value = value;
    }
}

/// <summary>属性簇 = 一个区 (标题栏 / 材料 / 前表面 / 后表面 …), 内含若干属性行。</summary>
public sealed class DrawingPropertyCluster
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public List<DrawingProperty> Properties { get; set; } = new();

    public DrawingPropertyCluster() { }
    public DrawingPropertyCluster(string key, string label) { Key = key; Label = label; }

    public DrawingProperty Add(string key, string label, string value)
    {
        var p = new DrawingProperty(key, label, value);
        Properties.Add(p);
        return p;
    }
}

/// <summary>
/// 文档级属性包 —— 一张图纸所有可见属性的单一真值 (簇=区, 属性=行)。
///
/// 数据流 (落地分期): 生成器先建包 → 包填图框 (属性驱动渲染); 后续期把包挂到文档 (CadCanvas)
/// 并 .otocad 序列化; 属性面板改格写回包 (按键); 一键重出从包重建 → 用户填的值不丢; 清单按键查。
/// 引擎无头, 不依赖 UI/命令系统。
/// </summary>
public sealed class DrawingPropertyBag
{
    public List<DrawingPropertyCluster> Clusters { get; set; } = new();

    public DrawingPropertyCluster GetOrAddCluster(string key, string label)
    {
        var c = Clusters.FirstOrDefault(x => x.Key == key);
        if (c is null) { c = new DrawingPropertyCluster(key, label); Clusters.Add(c); }
        return c;
    }

    public DrawingPropertyCluster? Cluster(string key) => Clusters.FirstOrDefault(x => x.Key == key);

    /// <summary>按属性键取值 (全局首个匹配)。键唯一时直接用 (如 DrawingNumber); 同名行用 <see cref="Cluster"/> 限定区。</summary>
    public string? GetValue(string key)
        => AllProperties.FirstOrDefault(p => p.Key == key)?.Value;

    /// <summary>按属性键写值 (全局首个匹配)。命中返回 true (P3 回写用)。</summary>
    public bool SetValue(string key, string value)
    {
        var p = AllProperties.FirstOrDefault(x => x.Key == key);
        if (p is null) return false;
        p.Value = value;
        return true;
    }

    /// <summary>全部属性 (跨簇)。</summary>
    public IEnumerable<DrawingProperty> AllProperties => Clusters.SelectMany(c => c.Properties);

    /// <summary>扁平 簇键.属性键 → 值 (跨区唯一; 同名行如 surface-front/back 各有 form_error 用区前缀区分)。</summary>
    public IReadOnlyDictionary<string, string> Flatten()
    {
        var d = new Dictionary<string, string>();
        foreach (var c in Clusters)
            foreach (var p in c.Properties)
                d[$"{c.Key}.{p.Key}"] = p.Value;
        return d;
    }
}
