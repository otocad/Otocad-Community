using System.Collections.Generic;

namespace lcdb.DrawingFrame;

/// <summary>
/// 图框暴露"扁平 键→值 属性视图"的契约 —— 出图清单按键校验 / AI 整体读取 的统一入口。
/// 由 GB(OpticalDrawingFrame / GbLensDrawingFrame)等图框实现; 值取自文档级属性包(单一真值源)。
/// </summary>
public interface IPropertyExport
{
    /// <summary>扁平 键→值 (跨标题栏 + 规格区所有属性)。</summary>
    IReadOnlyDictionary<string, string> ExportFlat();
}
