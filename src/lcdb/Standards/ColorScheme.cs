using System;
using System.Collections.Generic;
using System.Linq;
using lcdb.Colors;

namespace lcdb.Standards
{
    /// <summary>
    /// 颜色方案定义
    /// </summary>
    public class ColorScheme : ICloneable
    {
        #region Properties

        /// <summary>
        /// 方案名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 主色
        /// </summary>
        public Color PrimaryColor { get; set; }

        /// <summary>
        /// 次色
        /// </summary>
        public Color SecondaryColor { get; set; }

        /// <summary>
        /// 强调色
        /// </summary>
        public Color AccentColor { get; set; }

        /// <summary>
        /// 背景色
        /// </summary>
        public Color BackgroundColor { get; set; }

        /// <summary>
        /// 文本色
        /// </summary>
        public Color TextColor { get; set; }

        /// <summary>
        /// 高亮色
        /// </summary>
        public Color HighlightColor { get; set; }

        /// <summary>
        /// 网格色
        /// </summary>
        public Color GridColor { get; set; }

        /// <summary>
        /// 选中色
        /// </summary>
        public Color SelectionColor { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 用途类型
        /// </summary>
        public ColorUsage Usage { get; set; }

        /// <summary>
        /// 扩展颜色字典
        /// </summary>
        public Dictionary<string, Color> ExtendedColors { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ColorScheme()
        {
            Name = "Default";
            PrimaryColor = Color.FromRGB(0, 0, 0);
            SecondaryColor = Color.FromRGB(128, 128, 128);
            AccentColor = Color.FromRGB(255, 0, 0);
            BackgroundColor = Color.FromRGB(255, 255, 255);
            TextColor = Color.FromRGB(0, 0, 0);
            HighlightColor = Color.FromRGB(255, 255, 0);
            GridColor = Color.FromRGB(192, 192, 192);
            SelectionColor = Color.FromRGB(0, 0, 255);
            Usage = ColorUsage.General;
            ExtendedColors = new Dictionary<string, Color>();
        }

        /// <summary>
        /// 参数化构造函数
        /// </summary>
        public ColorScheme(string name, Color primary, Color secondary = default, Color accent = default)
        {
            Name = name;
            PrimaryColor = primary;
            SecondaryColor = secondary.Equals(default(Color)) ? Color.FromRGB(128, 128, 128) : secondary;
            AccentColor = accent.Equals(default(Color)) ? Color.FromRGB(255, 0, 0) : accent;
            BackgroundColor = Color.FromRGB(255, 255, 255);
            TextColor = Color.FromRGB(0, 0, 0);
            HighlightColor = Color.FromRGB(255, 255, 0);
            GridColor = Color.FromRGB(192, 192, 192);
            SelectionColor = Color.FromRGB(0, 0, 255);
            Usage = ColorUsage.General;
            ExtendedColors = new Dictionary<string, Color>();
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// 创建默认颜色方案
        /// </summary>
        public static ColorScheme CreateDefault()
        {
            return new ColorScheme("Default", Color.FromRGB(0, 0, 0))
            {
                Description = "默认颜色方案",
                Usage = ColorUsage.General
            };
        }

        /// <summary>
        /// 创建技术绘图颜色方案
        /// </summary>
        public static ColorScheme CreateTechnicalDrawing()
        {
            var scheme = new ColorScheme("TechnicalDrawing", Color.FromRGB(0, 0, 0))
            {
                SecondaryColor = Color.FromRGB(128, 128, 128),
                AccentColor = Color.FromRGB(255, 0, 0),
                BackgroundColor = Color.FromRGB(255, 255, 255),
                TextColor = Color.FromRGB(0, 0, 0),
                HighlightColor = Color.FromRGB(255, 255, 0),
                GridColor = Color.FromRGB(192, 192, 192),
                SelectionColor = Color.FromRGB(0, 0, 255),
                Description = "技术绘图颜色方案",
                Usage = ColorUsage.Technical
            };

            // 添加扩展颜色
            scheme.ExtendedColors.Add("HiddenLine", Color.FromRGB(128, 128, 128));
            scheme.ExtendedColors.Add("CenterLine", Color.FromRGB(0, 128, 0));
            scheme.ExtendedColors.Add("DimensionLine", Color.FromRGB(255, 0, 0));
            scheme.ExtendedColors.Add("ConstructionLine", Color.FromRGB(255, 192, 0));

            return scheme;
        }

        /// <summary>
        /// 创建光学设计颜色方案
        /// </summary>
        public static ColorScheme CreateOpticalDesign()
        {
            var scheme = new ColorScheme("OpticalDesign", Color.FromRGB(0, 0, 128))
            {
                SecondaryColor = Color.FromRGB(0, 128, 255),
                AccentColor = Color.FromRGB(255, 128, 0),
                BackgroundColor = Color.FromRGB(248, 248, 255),
                TextColor = Color.FromRGB(0, 0, 0),
                HighlightColor = Color.FromRGB(255, 255, 128),
                GridColor = Color.FromRGB(200, 200, 220),
                SelectionColor = Color.FromRGB(255, 0, 128),
                Description = "光学设计专用颜色方案",
                Usage = ColorUsage.Optical
            };

            // 光学元件专用颜色
            scheme.ExtendedColors.Add("LensOutline", Color.FromRGB(0, 0, 128));
            scheme.ExtendedColors.Add("LensSurface", Color.FromRGB(0, 128, 255));
            scheme.ExtendedColors.Add("CoatingAR", Color.FromRGB(0, 255, 0));
            scheme.ExtendedColors.Add("CoatingHR", Color.FromRGB(255, 0, 0));
            scheme.ExtendedColors.Add("CoatingPR", Color.FromRGB(0, 0, 255));
            scheme.ExtendedColors.Add("CoatingBBAR", Color.FromRGB(128, 0, 128));
            scheme.ExtendedColors.Add("OpticalAxis", Color.FromRGB(255, 128, 0));
            scheme.ExtendedColors.Add("LightRay", Color.FromRGB(255, 255, 0));

            return scheme;
        }

        /// <summary>
        /// 创建暗色主题颜色方案
        /// </summary>
        public static ColorScheme CreateDarkTheme()
        {
            var scheme = new ColorScheme("DarkTheme", Color.FromRGB(255, 255, 255))
            {
                SecondaryColor = Color.FromRGB(192, 192, 192),
                AccentColor = Color.FromRGB(0, 255, 255),
                BackgroundColor = Color.FromRGB(32, 32, 32),
                TextColor = Color.FromRGB(255, 255, 255),
                HighlightColor = Color.FromRGB(255, 255, 0),
                GridColor = Color.FromRGB(64, 64, 64),
                SelectionColor = Color.FromRGB(0, 128, 255),
                Description = "暗色主题颜色方案",
                Usage = ColorUsage.DarkTheme
            };

            return scheme;
        }

        /// <summary>
        /// 创建高对比度颜色方案
        /// </summary>
        public static ColorScheme CreateHighContrast()
        {
            var scheme = new ColorScheme("HighContrast", Color.FromRGB(0, 0, 0))
            {
                SecondaryColor = Color.FromRGB(128, 128, 128),
                AccentColor = Color.FromRGB(255, 0, 0),
                BackgroundColor = Color.FromRGB(255, 255, 255),
                TextColor = Color.FromRGB(0, 0, 0),
                HighlightColor = Color.FromRGB(255, 255, 0),
                GridColor = Color.FromRGB(192, 192, 192),
                SelectionColor = Color.FromRGB(0, 0, 255),
                Description = "高对比度颜色方案",
                Usage = ColorUsage.HighContrast
            };

            return scheme;
        }

        /// <summary>
        /// 创建色盲友好颜色方案
        /// </summary>
        public static ColorScheme CreateColorBlindFriendly()
        {
            var scheme = new ColorScheme("ColorBlindFriendly", Color.FromRGB(0, 0, 0))
            {
                SecondaryColor = Color.FromRGB(128, 128, 128),
                AccentColor = Color.FromRGB(230, 159, 0),  // 橙色
                BackgroundColor = Color.FromRGB(255, 255, 255),
                TextColor = Color.FromRGB(0, 0, 0),
                HighlightColor = Color.FromRGB(240, 228, 66),  // 黄色
                GridColor = Color.FromRGB(192, 192, 192),
                SelectionColor = Color.FromRGB(0, 114, 178),  // 蓝色
                Description = "色盲友好颜色方案",
                Usage = ColorUsage.Accessibility
            };

            // 色盲友好的扩展颜色
            scheme.ExtendedColors.Add("Safe1", Color.FromRGB(0, 114, 178));    // 蓝色
            scheme.ExtendedColors.Add("Safe2", Color.FromRGB(230, 159, 0));    // 橙色
            scheme.ExtendedColors.Add("Safe3", Color.FromRGB(0, 158, 115));    // 青绿色
            scheme.ExtendedColors.Add("Safe4", Color.FromRGB(204, 121, 167));  // 粉红色
            scheme.ExtendedColors.Add("Safe5", Color.FromRGB(86, 180, 233));   // 天蓝色

            return scheme;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 添加扩展颜色
        /// </summary>
        /// <param name="key">颜色键名</param>
        /// <param name="color">颜色值</param>
        public void AddExtendedColor(string key, Color color)
        {
            ExtendedColors[key] = color;
        }

        /// <summary>
        /// 获取扩展颜色
        /// </summary>
        /// <param name="key">颜色键名</param>
        /// <returns>颜色值，如果不存在则返回主色</returns>
        public Color GetExtendedColor(string key)
        {
            return ExtendedColors.ContainsKey(key) ? ExtendedColors[key] : PrimaryColor;
        }

        /// <summary>
        /// 移除扩展颜色
        /// </summary>
        /// <param name="key">颜色键名</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveExtendedColor(string key)
        {
            return ExtendedColors.Remove(key);
        }

        /// <summary>
        /// 获取用于指定用途的颜色
        /// </summary>
        /// <param name="purpose">颜色用途</param>
        /// <returns>对应的颜色</returns>
        public Color GetColorForPurpose(ColorPurpose purpose)
        {
            switch (purpose)
            {
                case ColorPurpose.Primary:
                    return PrimaryColor;
                case ColorPurpose.Secondary:
                    return SecondaryColor;
                case ColorPurpose.Accent:
                    return AccentColor;
                case ColorPurpose.Background:
                    return BackgroundColor;
                case ColorPurpose.Text:
                    return TextColor;
                case ColorPurpose.Highlight:
                    return HighlightColor;
                case ColorPurpose.Grid:
                    return GridColor;
                case ColorPurpose.Selection:
                    return SelectionColor;
                default:
                    return PrimaryColor;
            }
        }

        /// <summary>
        /// 验证颜色方案
        /// </summary>
        /// <returns>验证结果</returns>
        public ColorSchemeValidationResult Validate()
        {
            var result = new ColorSchemeValidationResult();

            if (string.IsNullOrEmpty(Name))
            {
                result.Errors.Add("颜色方案名称不能为空");
            }

            // 检查主要颜色是否设置
            if (PrimaryColor.Equals(default(Color)))
            {
                result.Errors.Add("必须设置主色");
            }

            // 检查对比度
            if (AreColorsToSimilar(PrimaryColor, BackgroundColor))
            {
                result.Warnings.Add("主色与背景色对比度过低，可能影响可见性");
            }

            if (AreColorsToSimilar(TextColor, BackgroundColor))
            {
                result.Warnings.Add("文本色与背景色对比度过低，可能影响可读性");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// 创建互补色方案
        /// </summary>
        /// <returns>互补色方案</returns>
        public ColorScheme CreateComplementary()
        {
            var complement = (ColorScheme)Clone();
            complement.Name = Name + "-Complement";
            complement.PrimaryColor = GetComplementaryColor(PrimaryColor);
            complement.AccentColor = GetComplementaryColor(AccentColor);
            return complement;
        }

        /// <summary>
        /// 调整方案亮度
        /// </summary>
        /// <param name="factor">亮度因子 (0.0 - 2.0)</param>
        /// <returns>调整后的颜色方案</returns>
        public ColorScheme AdjustBrightness(double factor)
        {
            var adjusted = (ColorScheme)Clone();
            adjusted.Name = Name + $"-Brightness{factor:F1}";
            
            adjusted.PrimaryColor = AdjustColorBrightness(PrimaryColor, factor);
            adjusted.SecondaryColor = AdjustColorBrightness(SecondaryColor, factor);
            adjusted.AccentColor = AdjustColorBrightness(AccentColor, factor);
            adjusted.TextColor = AdjustColorBrightness(TextColor, factor);
            adjusted.HighlightColor = AdjustColorBrightness(HighlightColor, factor);
            adjusted.GridColor = AdjustColorBrightness(GridColor, factor);

            // 调整扩展颜色
            var adjustedExtended = new Dictionary<string, Color>();
            foreach (var kvp in ExtendedColors)
            {
                adjustedExtended[kvp.Key] = AdjustColorBrightness(kvp.Value, factor);
            }
            adjusted.ExtendedColors = adjustedExtended;

            return adjusted;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// 检查两个颜色是否过于相似
        /// </summary>
        private bool AreColorsToSimilar(Color color1, Color color2)
        {
            int rDiff = Math.Abs(color1.r - color2.r);
            int gDiff = Math.Abs(color1.g - color2.g);
            int bDiff = Math.Abs(color1.b - color2.b);
            
            return (rDiff + gDiff + bDiff) < 100; // 阈值可调
        }

        /// <summary>
        /// 获取互补色
        /// </summary>
        private Color GetComplementaryColor(Color color)
        {
            return Color.FromRGB((byte)(255 - color.r), (byte)(255 - color.g), (byte)(255 - color.b));
        }

        /// <summary>
        /// 调整颜色亮度
        /// </summary>
        private Color AdjustColorBrightness(Color color, double factor)
        {
            int r = Math.Min(255, Math.Max(0, (int)(color.r * factor)));
            int g = Math.Min(255, Math.Max(0, (int)(color.g * factor)));
            int b = Math.Min(255, Math.Max(0, (int)(color.b * factor)));
            
            return Color.FromRGB((byte)r, (byte)g, (byte)b);
        }

        #endregion

        #region ICloneable Implementation

        public object Clone()
        {
            var cloned = new ColorScheme
            {
                Name = Name,
                PrimaryColor = PrimaryColor,
                SecondaryColor = SecondaryColor,
                AccentColor = AccentColor,
                BackgroundColor = BackgroundColor,
                TextColor = TextColor,
                HighlightColor = HighlightColor,
                GridColor = GridColor,
                SelectionColor = SelectionColor,
                Description = Description,
                Usage = Usage
            };

            // 复制扩展颜色
            foreach (var kvp in ExtendedColors)
            {
                cloned.ExtendedColors[kvp.Key] = kvp.Value;
            }

            return cloned;
        }

        #endregion

        #region Override Methods

        public override string ToString()
        {
            return $"{Name} ({Usage})";
        }

        public override bool Equals(object obj)
        {
            if (obj is ColorScheme other)
            {
                return Name == other.Name && Usage == other.Usage;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0;
        }

        #endregion
    }

    /// <summary>
    /// 颜色用途枚举
    /// </summary>
    public enum ColorUsage
    {
        /// <summary>
        /// 通用
        /// </summary>
        General,

        /// <summary>
        /// 技术绘图
        /// </summary>
        Technical,

        /// <summary>
        /// 光学设计
        /// </summary>
        Optical,

        /// <summary>
        /// 暗色主题
        /// </summary>
        DarkTheme,

        /// <summary>
        /// 高对比度
        /// </summary>
        HighContrast,

        /// <summary>
        /// 可访问性
        /// </summary>
        Accessibility,

        /// <summary>
        /// 打印
        /// </summary>
        Print,

        /// <summary>
        /// 演示
        /// </summary>
        Presentation
    }

    /// <summary>
    /// 颜色目的枚举
    /// </summary>
    public enum ColorPurpose
    {
        Primary,
        Secondary,
        Accent,
        Background,
        Text,
        Highlight,
        Grid,
        Selection
    }

    /// <summary>
    /// 颜色方案验证结果
    /// </summary>
    public class ColorSchemeValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// 颜色方案管理器
    /// </summary>
    public static class ColorSchemeManager
    {
        /// <summary>
        /// 获取所有预定义的颜色方案
        /// </summary>
        /// <returns>预定义方案字典</returns>
        public static Dictionary<string, ColorScheme> GetPredefinedSchemes()
        {
            return new Dictionary<string, ColorScheme>
            {
                { "Default", ColorScheme.CreateDefault() },
                { "TechnicalDrawing", ColorScheme.CreateTechnicalDrawing() },
                { "OpticalDesign", ColorScheme.CreateOpticalDesign() },
                { "DarkTheme", ColorScheme.CreateDarkTheme() },
                { "HighContrast", ColorScheme.CreateHighContrast() },
                { "ColorBlindFriendly", ColorScheme.CreateColorBlindFriendly() }
            };
        }

        /// <summary>
        /// 根据用途获取推荐的颜色方案
        /// </summary>
        /// <param name="usage">颜色用途</param>
        /// <returns>推荐的颜色方案</returns>
        public static ColorScheme GetRecommendedScheme(ColorUsage usage)
        {
            switch (usage)
            {
                case ColorUsage.Technical:
                    return ColorScheme.CreateTechnicalDrawing();
                case ColorUsage.Optical:
                    return ColorScheme.CreateOpticalDesign();
                case ColorUsage.DarkTheme:
                    return ColorScheme.CreateDarkTheme();
                case ColorUsage.HighContrast:
                    return ColorScheme.CreateHighContrast();
                case ColorUsage.Accessibility:
                    return ColorScheme.CreateColorBlindFriendly();
                default:
                    return ColorScheme.CreateDefault();
            }
        }

        /// <summary>
        /// 创建符合ISO标准的颜色方案
        /// </summary>
        /// <returns>ISO标准颜色方案</returns>
        public static Dictionary<string, ColorScheme> CreateISOColorSchemes()
        {
            var schemes = new Dictionary<string, ColorScheme>();

            // ISO 5807标准颜色
            var isoBasic = new ColorScheme("ISO-Basic", Color.FromRGB(0, 0, 0))
            {
                Description = "ISO基础颜色方案",
                Usage = ColorUsage.Technical
            };
            schemes.Add("ISO-Basic", isoBasic);

            return schemes;
        }

        /// <summary>
        /// 创建符合GB标准的颜色方案
        /// </summary>
        /// <returns>GB标准颜色方案</returns>
        public static Dictionary<string, ColorScheme> CreateGBColorSchemes()
        {
            var schemes = new Dictionary<string, ColorScheme>();

            // GB/T标准颜色
            var gbBasic = new ColorScheme("GB-Basic", Color.FromRGB(0, 0, 0))
            {
                Description = "GB基础颜色方案",
                Usage = ColorUsage.Technical
            };
            schemes.Add("GB-Basic", gbBasic);

            return schemes;
        }
    }
}