using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace lcdb.Checklist;

/// <summary>
/// 出图清单定义 — 从 YAML 规则文件反序列化而来。
///
/// 设计: 规则是数据 (YAML, 支持注释写国标条款出处), 检查逻辑是 <see cref="ChecklistEvaluator"/>
/// 里的少数"动词"。加规则改 YAML (热加载, 不重编译); 加动词才写 C#。
/// </summary>
public sealed class ChecklistDefinition
{
    public int Version { get; set; } = 1;

    /// <summary>清单名 (如 gb-singlet-default)。</summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 声明本图使用的图框 ("using frame", 如 iso-lens / gb-optical / 将来用户自定义 GB-my-1)。
    /// 非空时引擎自动生成一条隐式 error 项校验"在场图框就是声明的那个"。
    /// 空 = 不约束图框种类 (兼容 v1 行为)。注册表见 <see cref="ChecklistEvaluator.FrameRegistry"/>。
    /// </summary>
    public string Frame { get; set; } = "";

    public List<ChecklistItemDef> Items { get; set; } = new();

    /// <summary>从 YAML 文本解析。语法/字段错误抛 <see cref="YamlDotNet.Core.YamlException"/>。</summary>
    public static ChecklistDefinition Parse(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        return deserializer.Deserialize<ChecklistDefinition>(yaml) ?? new ChecklistDefinition();
    }
}

/// <summary>
/// 一条清单项。<see cref="Check"/> 选动词, 其余字段是该动词的参数 (未用到的留默认)。
/// 动词与参数对应见 <see cref="ChecklistEvaluator"/>。
/// </summary>
public sealed class ChecklistItemDef
{
    /// <summary>稳定标识 (面板状态/去重用)。</summary>
    public string Key { get; set; } = "";

    /// <summary>面板显示名。</summary>
    public string Label { get; set; } = "";

    /// <summary>动词: exists / count / owned-count / surface-annotation / no-overlap / inside-frame / zone-rows / zone-row-prefix。</summary>
    public string Check { get; set; } = "";

    /// <summary>实体类型名 (类名 / 抽象基类 / 接口, 经 IsInstanceOfType 匹配派生)。用于 exists/count/surface-annotation。</summary>
    public string Type { get; set; } = "";

    /// <summary>最小数量阈值 (count / owned-count)。默认 1。</summary>
    public int Min { get; set; } = 1;

    /// <summary>参与互不重叠判定的类型名集合 (no-overlap)。</summary>
    public List<string> Among { get; set; } = new();

    /// <summary>面型过滤 (surface-annotation): aspheric / spherical。</summary>
    public string Surface { get; set; } = "";

    /// <summary>属性区列下标 (zone-rows / zone-row-prefix): 0=前表面 1=材料 2=后表面。</summary>
    public int Column { get; set; }

    /// <summary>必须出现的代号前缀集合 (zone-row-prefix), 每个前缀至少一行匹配。</summary>
    public List<string> Prefixes { get; set; } = new();

    /// <summary>属性键 (property): 校验图框属性包中该键存在且非空 (如 product_number / material_name)。</summary>
    public string Prop { get; set; } = "";

    /// <summary>严重度: error (默认, 拒收级) / warning。</summary>
    public string Severity { get; set; } = "error";

    /// <summary>一键重出动作名 (UI 层映射到实际操作)。空 = 需人工处理。</summary>
    public string Regen { get; set; } = "";

    public CheckSeverity SeverityEnum =>
        Severity?.Trim().ToLowerInvariant() == "warning" ? CheckSeverity.Warning : CheckSeverity.Error;
}
