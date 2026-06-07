using System.Drawing;
using lcdb.Colors;

namespace lcdb.Colors
{
    /// <summary>
    /// Color类型转换扩展方法
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// 将System.Drawing.Color转换为lcdb.Colors.Color
        /// </summary>
        public static Color ToColor(this System.Drawing.Color drawingColor)
        {
            return Color.FromColor(drawingColor);
        }

        /// <summary>
        /// 将lcdb.Colors.Color转换为System.Drawing.Color
        /// </summary>
        public static System.Drawing.Color ToDrawingColor(this Color color)
        {
            // 如果是特殊颜色方法，返回默认颜色
            if (color.colorMethod != ColorMethod.ByColor)
                return System.Drawing.Color.Black;
                
            return System.Drawing.Color.FromArgb(255, color.r, color.g, color.b);
        }
        
        /// <summary>
        /// 获取Color的ARGB值
        /// </summary>
        public static int ToArgb(this Color color)
        {
            // Color是struct，不需要null检查
            // 如果是特殊颜色方法，返回默认值
            if (color.colorMethod != ColorMethod.ByColor)
                return 0;
                
            // 转换RGB值到ARGB整数
            return System.Drawing.Color.FromArgb(255, color.r, color.g, color.b).ToArgb();
        }
    }
}