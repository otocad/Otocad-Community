using System.ComponentModel;

namespace lcdb.Standards
{
    /// <summary>
    /// 图纸尺寸枚举
    /// </summary>
    public enum PaperSize
    {
        [Description("A0 (841 × 1189 mm)")]
        A0,
        
        [Description("A1 (594 × 841 mm)")]
        A1,
        
        [Description("A2 (420 × 594 mm)")]
        A2,
        
        [Description("A3 (297 × 420 mm)")]
        A3,
        
        [Description("A4 (210 × 297 mm)")]
        A4
    }
}