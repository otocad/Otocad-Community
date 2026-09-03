using System;

namespace OtoCAD.Avalonia.Templating;

/// <summary>
/// 版面引擎 — scale-to-fit + 自动选纸. 让零件按标准比例 (…5:1/2:1/1:1/1:2…) 充满绘图区,
/// 小件放大、大件缩小或换大纸; 标注文字由各 builder 用真值文本显示 (本仓 DimensionStyle 无 DIMLFAC).
///
/// 策略: 取"能装下"的最大标准比例, 优先小纸 (A4); A4 连 1:10 都装不下才升 A3/A2.
/// 绝大多数光学件落在 A4 + 某个比例.
/// </summary>
public static class LayoutEngine
{
    // 竖版 A 系列 (mm)
    private static readonly (double W, double H, string Name)[] Papers =
    {
        (210, 297, "A4"), (297, 420, "A3"), (420, 594, "A2"),
    };

    // 标准绘图比例 (k = 图/真), 由大到小
    private static readonly double[] Scales = { 20, 10, 5, 2, 1, 0.5, 0.2, 0.1 };

    private const double Border = 8.0;     // 主图框边距 (= IsoLensDrawingFrame.BorderMargin 默认)
    private const double DimMargin = 36.0; // 顶/底尺寸 + 标签留白 (mm, 与比例无关)

    /// <summary>图框默认行高 → 底部块高度估算 (NOTES 12 + 标题 16 + (rows+1)*6).</summary>
    public static double BottomBlockHeight(int specRowCount)
        => 12.0 + 16.0 + (specRowCount + 1) * 6.0;

    /// <summary>
    /// 按零件净口径 (直径, mm) + 规格表最大行数, 算出纸张 + 比例.
    /// 选"装得下绘图区高度"的最大标准比例; 装不下则升一号纸.
    /// </summary>
    public static SheetLayout Compute(double partDiameter, int specRowCount)
        => ComputeForBlock(partDiameter, BottomBlockHeight(specRowCount));

    /// <summary>同 Compute, 但直接给底部块高度 (GB 框块高 ≠ ISO, 由调用方算好传入)。</summary>
    public static SheetLayout ComputeForBlock(double partDiameter, double blockH)
    {
        double d = Math.Max(1.0, partDiameter);

        foreach (var p in Papers)
        {
            double availH = p.H - 2 * Border - blockH;
            foreach (double k in Scales)
            {
                if (d * k + DimMargin <= availH)
                    return new SheetLayout { PaperW = p.W, PaperH = p.H, Scale = k, ScaleLabel = Label(k) };
            }
        }
        // 兜底: 最大纸 + 最小比例
        var big = Papers[^1];
        return new SheetLayout { PaperW = big.W, PaperH = big.H, Scale = 0.1, ScaleLabel = Label(0.1) };
    }

    /// <summary>k → 制图比例标签: k≥1 → "k:1", k&lt;1 → "1:(1/k)".</summary>
    public static string Label(double k)
        => k >= 1.0
            ? $"{k:0.#}:1"
            : $"1:{1.0 / k:0.#}";
}
