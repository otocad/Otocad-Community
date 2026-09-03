using System;
using System.Collections.Generic;

namespace lcdb.IO;

/// <summary>
/// V4 "文档型" 元数据 — 与 CAD 几何 (MODEL 段) 分开持久化.
///
/// 特性: 稳定 schema, 低频改动, key-value 易序列化, 不需 polymorphic.
/// DXF 导出时仅取 MODEL 段, DocumentMeta 全部丢弃 (不影响 CAD 几何).
/// </summary>
public sealed class DocumentMeta
{
    /// <summary>文档标题 (默认文件名).</summary>
    public string Title { get; set; } = "";

    /// <summary>创建时间 (ISO 8601, UTC).</summary>
    public DateTime Created { get; set; } = DateTime.UtcNow;

    /// <summary>最后修改时间.</summary>
    public DateTime Modified { get; set; } = DateTime.UtcNow;

    /// <summary>作者 / 设计人.</summary>
    public string Author { get; set; } = "";

    /// <summary>客户 / 项目方.</summary>
    public string Client { get; set; } = "";

    /// <summary>项目编号.</summary>
    public string ProjectId { get; set; } = "";

    /// <summary>单位 ("mm" / "inch" / "um"). 默认 mm.</summary>
    public string Units { get; set; } = "mm";

    /// <summary>修订版次 (例如 "Rev. A" / "v1.2").</summary>
    public string Revision { get; set; } = "";

    /// <summary>
    /// 外部设计文件引用 — 按工具分组的路径列表 (相对工作目录).
    /// 例: { "zemax": ["designs/objective.zmx"], "codev": [...] }
    /// </summary>
    public Dictionary<string, List<string>> ExternalRefs { get; set; } = new();

    /// <summary>自由 tag/metadata (审批状态/隐私分级/批注等).</summary>
    public Dictionary<string, string> Tags { get; set; } = new();

    /// <summary>图层定义 (颜色/线宽/线型/描述). 实体通过 layer 名引用.</summary>
    public List<LayerDto> Layers { get; set; } = new();

    /// <summary>
    /// 文档内嵌的出图标准(出图惯例)——发图给别人不丢惯例。
    /// 保存时写入当前 Active;加载时被 OtocadFileFormatV4 采纳为 Active(自定义则入库)。
    /// 老文件无此字段 → null → 回落默认 GB-ISO。
    /// </summary>
    public lcdb.Standards.DrawingConvention? Convention { get; set; }

    /// <summary>
    /// 文档级出图属性包(单一真值源)——标题栏/规格区的键值属性集中存这里。
    /// 保存时写入 db.DrawingProperties;加载时回灌。老文件无此字段 → null → 空包。
    /// </summary>
    public lcdb.Drawing.DrawingPropertyBag? DrawingProperties { get; set; }
}

/// <summary>
/// V4 图层 DTO — 不直接序列化 lcdb.Layer (Layer 是 DBTableRecord 带 ObjectId/parent 等 IO 不友好字段).
/// 反序列化时重建 Layer 加入 db.layerTable.
/// </summary>
public sealed class LayerDto
{
    public string Name { get; set; } = "0";
    public int ColorR { get; set; } = 255;
    public int ColorG { get; set; } = 255;
    public int ColorB { get; set; } = 255;
    public string Description { get; set; } = "";
    /// <summary>线型 (LineType enum 整数值).</summary>
    public int LineType { get; set; } = 0;
    /// <summary>线宽 (LineWeight enum 整数值).</summary>
    public int LineWeight { get; set; } = 0;
    /// <summary>锁定 — 该图层实体不可编辑 (老 V4 文件没此字段, 默认 false).</summary>
    public bool IsLocked { get; set; } = false;
    /// <summary>可见 — false 时跳过渲染 (默认 true 兼容老文件).</summary>
    public bool IsVisible { get; set; } = true;
    /// <summary>冻结 — 等同更彻底的隐藏 (默认 false).</summary>
    public bool IsFrozen { get; set; } = false;

    public static LayerDto FromLayer(Layer layer) => new()
    {
        Name = layer.name,
        ColorR = layer.color.r,
        ColorG = layer.color.g,
        ColorB = layer.color.b,
        Description = layer.description,
        LineType = (int)layer.lineType,
        LineWeight = (int)layer.lineWeight,
        IsLocked = layer.IsLocked,
        IsVisible = layer.IsVisible,
        IsFrozen = layer.IsFrozen,
    };

    public Layer ToLayer() => new(Name)
    {
        color = lcdb.Colors.Color.FromRGB((byte)ColorR, (byte)ColorG, (byte)ColorB),
        description = Description,
        lineType = (LineType)LineType,
        lineWeight = (LineWeight)LineWeight,
        IsLocked = IsLocked,
        IsVisible = IsVisible,
        IsFrozen = IsFrozen,
    };
}
