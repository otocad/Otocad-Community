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
    /// 解析纸张代码 "A4P" / "A4L" / "A3L" … (A0–A4 + P 纵 / L 横, 大小写不敏感) → 宽 × 高 (mm).
    /// "auto" / 空 / 未知 → false (数据驱动图框定义 paper 字段用).
    /// </summary>
    public static bool TryParsePaper(string code, out double width, out double height)
    {
        width = height = 0;
        if (string.IsNullOrWhiteSpace(code)) return false;
        var s = code.Trim().ToUpperInvariant();
        if (s.Length != 3 || s[0] != 'A' || (s[2] != 'P' && s[2] != 'L')) return false;
        if (!Enum.TryParse<PaperSize>("A" + s[1], out var size) || !_sizes.TryGetValue(size, out var dim)) return false;
        if (s[2] == 'L') { width = dim.Long; height = dim.Short; }
        else { width = dim.Short; height = dim.Long; }
        return true;
    }

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
