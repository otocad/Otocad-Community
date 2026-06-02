using System.Collections.Generic;

namespace lcdb.DrawingFrame;

/// <summary>
/// 工程图图框预设 — A0–A4 × 横/纵, GB/T 14689 标准尺寸 (mm).
/// 调 <see cref="Create"/> 拿到模板, 进一步可 Clone 修改具体字段.
/// </summary>
public static class DrawingFrameTemplates
{
    public enum PaperSize { A0, A1, A2, A3, A4 }
    public enum Orientation { Landscape, Portrait }

    /// <summary>GB/T 14689 标称尺寸 (mm) — 长边 × 短边.</summary>
    private static readonly Dictionary<PaperSize, (double Long, double Short)> _sizes = new()
    {
        [PaperSize.A0] = (1189, 841),
        [PaperSize.A1] = (841,  594),
        [PaperSize.A2] = (594,  420),
        [PaperSize.A3] = (420,  297),
        [PaperSize.A4] = (297,  210),
    };

    /// <summary>
    /// 装订边: 横向 25mm, 其余 5mm; A4 因纸小常用 20mm. GB/T 14689 规定.
    /// </summary>
    public static DrawingFrame Create(PaperSize size, Orientation orientation, bool withTitleBlock = true)
    {
        var (lng, shrt) = _sizes[size];
        double w = orientation == Orientation.Landscape ? lng : shrt;
        double h = orientation == Orientation.Landscape ? shrt : lng;
        double binding = size == PaperSize.A4 ? 20 : 25;

        return new DrawingFrame
        {
            PaperWidth = w,
            PaperHeight = h,
            MarginLeft = binding,
            MarginRight = 5,
            MarginTop = 5,
            MarginBottom = 5,
            ShowTitleBlock = withTitleBlock,
            TitleBlockWidth = size == PaperSize.A4 ? 140 : 180,
            TitleBlockHeight = 56,
        };
    }

    /// <summary>所有 (Size, Orientation) 组合的中文显示名 — UI 下拉/选择列表用.</summary>
    public static IEnumerable<(PaperSize Size, Orientation Orient, string Display)> All()
    {
        foreach (PaperSize s in System.Enum.GetValues(typeof(PaperSize)))
        {
            yield return (s, Orientation.Landscape, $"{s} 横向 ({_sizes[s].Long}×{_sizes[s].Short})");
            yield return (s, Orientation.Portrait,  $"{s} 纵向 ({_sizes[s].Short}×{_sizes[s].Long})");
        }
    }

    /// <summary>
    /// 光学图框模板 — 同 <see cref="Create"/> 但返回 <see cref="OpticalDrawingFrame"/>,
    /// 自带 "对材料的要求" + "对零件的要求" 双表 (GB/T 13323-2009 §1.2 / §2).
    /// </summary>
    public static OpticalDrawingFrame CreateOptical(PaperSize size, Orientation orientation, bool withTitleBlock = true)
    {
        var (lng, shrt) = _sizes[size];
        double w = orientation == Orientation.Landscape ? lng : shrt;
        double h = orientation == Orientation.Landscape ? shrt : lng;
        double binding = size == PaperSize.A4 ? 20 : 25;

        return new OpticalDrawingFrame
        {
            PaperWidth = w,
            PaperHeight = h,
            MarginLeft = binding,
            MarginRight = 5,
            MarginTop = 5,
            MarginBottom = 5,
            ShowTitleBlock = withTitleBlock,
            TitleBlockWidth = size == PaperSize.A4 ? 140 : 180,
            TitleBlockHeight = 56,
        };
    }
}
