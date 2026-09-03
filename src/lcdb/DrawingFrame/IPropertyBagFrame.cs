#nullable enable
namespace lcdb.DrawingFrame;

/// <summary>
/// 属性包驱动图框契约: 内容由文档级属性包 (<see cref="lcdb.Drawing.DrawingPropertyBag"/>, 单一真值源) 填充,
/// 改格回写包. 画布装载生成图纸时采纳 <see cref="Bag"/> 为 <c>db.DrawingProperties</c>;
/// 一键重出 = 重新 <see cref="ApplyBag"/> (保留用户改动). 由 GB 中文框与数据驱动图框实现,
/// 出图引擎 / 画布 / 适配器按此契约操作, 不认识具体图框类.
/// </summary>
public interface IPropertyBagFrame
{
    /// <summary>绑定的文档级属性包 (未 ApplyBag 过则 null). 瞬态, 不序列化.</summary>
    lcdb.Drawing.DrawingPropertyBag? Bag { get; }

    /// <summary>由属性包填充图框内容并绑定 (之后改格回写包).</summary>
    void ApplyBag(lcdb.Drawing.DrawingPropertyBag bag);
}
