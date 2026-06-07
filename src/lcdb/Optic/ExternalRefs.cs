using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace lcdb.Optic;

/// <summary>
/// 光学元件的外部设计引用 — 链回原始光学设计文件 (Zemax / Code V / OSLO).
/// 用于"这个 lens 是 Zemax 文件 X 的第 N 个面"这种追溯, 不参与渲染.
///
/// 仅作记录 (storage-only, 不强校验). 文件路径可相对工作目录, 可绝对.
/// </summary>
public sealed class ZemaxBinding
{
    /// <summary>Zemax 文件路径 (相对或绝对). 例如 "designs/objective.zmx".</summary>
    public string FilePath { get; set; } = "";

    /// <summary>第几个面 (Zemax LDE 表的行号, OBJ=0, STO=Stop, IMA=Image).</summary>
    public int SurfaceIndex { get; set; } = 1;

    /// <summary>Zemax 文件最后同步时间 (用于检测设计变更).</summary>
    public DateTime? LastSync { get; set; }

    /// <summary>可选: Zemax 视图标签/备注.</summary>
    public string? Note { get; set; }
}

/// <summary>
/// 通用外部引用 (用于非 Zemax 的其他设计软件 / 自定义).
/// 字典 Key 是工具名 ("CodeV" / "OSLO" / "ZEMAX" / "Custom"), Value 是该工具的 binding.
/// 序列化时为 object, 反序列化通过 [JsonDerivedType] 多态 (用户可加自己的 Binding 类).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ZemaxBinding), "zemax")]
public abstract class ExternalDesignRef
{
    // 基类只是多态根, 字段由子类定义
}

/// <summary>
/// 自由扩展 metadata — 任意 Key/Value, 用 string 值类型避免 polymorphic 序列化坑.
/// 复杂结构化数据应该用专用的 OpticalSurface 子类 / ExternalDesignRef 子类.
/// </summary>
public sealed class OpticalExtensions
{
    /// <summary>自由文本 metadata (例如: "Notes" / "Tags" / "Vendor" / "PartNumber").</summary>
    public Dictionary<string, string> Tags { get; set; } = new();

    /// <summary>Zemax 文件绑定 (可选). 若有多文件需求, 应推广到 ExternalRefs 列表.</summary>
    public ZemaxBinding? Zemax { get; set; }
}
