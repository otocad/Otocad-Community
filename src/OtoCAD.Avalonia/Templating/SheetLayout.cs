namespace OtoCAD.Avalonia.Templating;

/// <summary>
/// 一张图纸的版面决策 (由 <see cref="LayoutEngine"/> 算出): 纸张尺寸 + 绘图比例 + 比例标签.
/// Scale = 图上画的 / 真实 (k); k&gt;1 放大 (小件), k&lt;1 缩小 (大件), 标注文字始终显示真值.
/// </summary>
public sealed class SheetLayout
{
    public double PaperW = 210;
    public double PaperH = 297;
    public double Scale = 1.0;          // k = drawn/real
    public string ScaleLabel = "1:1";

    public static SheetLayout A4_1to1() => new();
}
