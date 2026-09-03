using System;
using System.Collections.Generic;
using System.Linq;
using lcdb.Colors;
using lcdb.Annotation;

namespace lcdb.Standards
{
    /// <summary>
    /// 镀膜标记样式定义
    /// </summary>
    public class CoatingMarkStyle : ICloneable
    {
        #region Properties

        /// <summary>
        /// 样式名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 镀膜类型
        /// </summary>
        public CoatingType CoatingType { get; set; }

        /// <summary>
        /// 标记形状
        /// </summary>
        public CoatingMarkShape MarkShape { get; set; }

        /// <summary>
        /// 标记颜色
        /// </summary>
        public Color MarkColor { get; set; }

        /// <summary>
        /// 标记大小
        /// </summary>
        public double MarkSize { get; set; }

        /// <summary>
        /// 线宽
        /// </summary>
        public LineWeight LineWeight { get; set; }

        /// <summary>
        /// 文本样式名称
        /// </summary>
        public string TextStyleName { get; set; }

        /// <summary>
        /// 文本颜色
        /// </summary>
        public Color TextColor { get; set; }

        /// <summary>
        /// 文本大小
        /// </summary>
        public double TextSize { get; set; }

        /// <summary>
        /// 文本偏移距离
        /// </summary>
        public double TextOffset { get; set; }

        /// <summary>
        /// 默认文本位置
        /// </summary>
        public CoatingTextPosition DefaultTextPosition { get; set; }

        /// <summary>
        /// 是否显示文本
        /// </summary>
        public bool ShowText { get; set; }

        /// <summary>
        /// 是否填充标记
        /// </summary>
        public bool FillMark { get; set; }

        /// <summary>
        /// 填充颜色
        /// </summary>
        public Color FillColor { get; set; }

        /// <summary>
        /// 填充透明度 (0.0-1.0)
        /// </summary>
        public double FillOpacity { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 标准类型
        /// </summary>
        public CoatingStandard Standard { get; set; }

        /// <summary>
        /// 扩展属性
        /// </summary>
        public Dictionary<string, object> ExtendedProperties { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public CoatingMarkStyle()
        {
            Name = "Default";
            CoatingType = CoatingType.AR;
            MarkShape = CoatingMarkShape.Triangle;
            MarkColor = Color.FromRGB(0, 255, 0);
            MarkSize = 2.0;
            LineWeight = LineWeight.LineWeight025;
            TextStyleName = "Standard";
            TextColor = Color.FromRGB(0, 255, 0);
            TextSize = 1.5;
            TextOffset = 3.0;
            DefaultTextPosition = CoatingTextPosition.Right;
            ShowText = true;
            FillMark = false;
            FillColor = Color.FromRGB(0, 255, 0);
            FillOpacity = 0.3;
            Standard = CoatingStandard.Custom;
            ExtendedProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// 参数化构造函数
        /// </summary>
        public CoatingMarkStyle(CoatingType coatingType, CoatingMarkShape shape, Color color, double size = 2.0)
        {
            Name = coatingType.ToString();
            CoatingType = coatingType;
            MarkShape = shape;
            MarkColor = color;
            MarkSize = size;
            LineWeight = LineWeight.LineWeight025;
            TextStyleName = "Standard";
            TextColor = color;
            TextSize = 1.5;
            TextOffset = 3.0;
            DefaultTextPosition = CoatingTextPosition.Right;
            ShowText = true;
            FillMark = false;
            FillColor = color;
            FillOpacity = 0.3;
            Standard = CoatingStandard.Custom;
            ExtendedProperties = new Dictionary<string, object>();
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// 创建AR镀膜样式
        /// </summary>
        public static CoatingMarkStyle CreateARStyle()
        {
            return new CoatingMarkStyle(CoatingType.AR, CoatingMarkShape.Triangle, Color.FromRGB(0, 255, 0))
            {
                Name = "AR-Standard",
                Description = "增透膜标准样式",
                Standard = CoatingStandard.ISO
            };
        }

        /// <summary>
        /// 创建HR镀膜样式
        /// </summary>
        public static CoatingMarkStyle CreateHRStyle()
        {
            return new CoatingMarkStyle(CoatingType.HR, CoatingMarkShape.Square, Color.FromRGB(255, 0, 0))
            {
                Name = "HR-Standard",
                Description = "高反膜标准样式",
                Standard = CoatingStandard.ISO
            };
        }

        /// <summary>
        /// 创建PR镀膜样式
        /// </summary>
        public static CoatingMarkStyle CreatePRStyle()
        {
            return new CoatingMarkStyle(CoatingType.PR, CoatingMarkShape.Circle, Color.FromRGB(0, 0, 255))
            {
                Name = "PR-Standard",
                Description = "部分反射膜标准样式",
                Standard = CoatingStandard.ISO
            };
        }

        /// <summary>
        /// 创建BBAR镀膜样式
        /// </summary>
        public static CoatingMarkStyle CreateBBARStyle()
        {
            return new CoatingMarkStyle(CoatingType.BBAR, CoatingMarkShape.Diamond, Color.FromRGB(128, 0, 128))
            {
                Name = "BBAR-Standard",
                Description = "宽带增透膜标准样式",
                Standard = CoatingStandard.ISO
            };
        }

        /// <summary>
        /// 创建ISO标准样式集合
        /// </summary>
        public static Dictionary<CoatingType, CoatingMarkStyle> CreateISOStandardStyles()
        {
            var styles = new Dictionary<CoatingType, CoatingMarkStyle>();

            // ISO 10110标准镀膜标记
            styles[CoatingType.AR] = new CoatingMarkStyle(CoatingType.AR, CoatingMarkShape.Triangle, Color.FromRGB(0, 128, 0))
            {
                Name = "ISO-AR",
                MarkSize = 2.5,
                TextSize = 2.0,
                Description = "ISO 10110增透膜标记",
                Standard = CoatingStandard.ISO
            };

            styles[CoatingType.HR] = new CoatingMarkStyle(CoatingType.HR, CoatingMarkShape.Square, Color.FromRGB(255, 0, 0))
            {
                Name = "ISO-HR",
                MarkSize = 2.5,
                TextSize = 2.0,
                Description = "ISO 10110高反膜标记",
                Standard = CoatingStandard.ISO
            };

            styles[CoatingType.PR] = new CoatingMarkStyle(CoatingType.PR, CoatingMarkShape.Circle, Color.FromRGB(0, 0, 255))
            {
                Name = "ISO-PR",
                MarkSize = 2.5,
                TextSize = 2.0,
                Description = "ISO 10110部分反射膜标记",
                Standard = CoatingStandard.ISO
            };

            styles[CoatingType.BBAR] = new CoatingMarkStyle(CoatingType.BBAR, CoatingMarkShape.Diamond, Color.FromRGB(128, 0, 128))
            {
                Name = "ISO-BBAR",
                MarkSize = 2.5,
                TextSize = 2.0,
                Description = "ISO 10110宽带增透膜标记",
                Standard = CoatingStandard.ISO
            };

            return styles;
        }

        /// <summary>
        /// 创建GB标准样式集合
        /// </summary>
        public static Dictionary<CoatingType, CoatingMarkStyle> CreateGBStandardStyles()
        {
            var styles = new Dictionary<CoatingType, CoatingMarkStyle>();

            // GB国标镀膜标记
            styles[CoatingType.AR] = new CoatingMarkStyle(CoatingType.AR, CoatingMarkShape.Triangle, Color.FromRGB(0, 128, 0))
            {
                Name = "GB-AR",
                MarkSize = 3.0,
                TextSize = 2.5,
                TextStyleName = "GB-Standard",
                Description = "GB国标增透膜标记",
                Standard = CoatingStandard.GB
            };

            styles[CoatingType.HR] = new CoatingMarkStyle(CoatingType.HR, CoatingMarkShape.Square, Color.FromRGB(255, 0, 0))
            {
                Name = "GB-HR",
                MarkSize = 3.0,
                TextSize = 2.5,
                TextStyleName = "GB-Standard",
                Description = "GB国标高反膜标记",
                Standard = CoatingStandard.GB
            };

            styles[CoatingType.PR] = new CoatingMarkStyle(CoatingType.PR, CoatingMarkShape.Circle, Color.FromRGB(0, 0, 255))
            {
                Name = "GB-PR",
                MarkSize = 3.0,
                TextSize = 2.5,
                TextStyleName = "GB-Standard",
                Description = "GB国标部分反射膜标记",
                Standard = CoatingStandard.GB
            };

            styles[CoatingType.BBAR] = new CoatingMarkStyle(CoatingType.BBAR, CoatingMarkShape.Diamond, Color.FromRGB(128, 0, 128))
            {
                Name = "GB-BBAR",
                MarkSize = 3.0,
                TextSize = 2.5,
                TextStyleName = "GB-Standard",
                Description = "GB国标宽带增透膜标记",
                Standard = CoatingStandard.GB
            };

            return styles;
        }

        /// <summary>
        /// 创建紧凑型样式集合
        /// </summary>
        public static Dictionary<CoatingType, CoatingMarkStyle> CreateCompactStyles()
        {
            var styles = new Dictionary<CoatingType, CoatingMarkStyle>();

            foreach (CoatingType type in Enum.GetValues(typeof(CoatingType)))
            {
                if (type == CoatingType.Custom) continue;

                var style = new CoatingMarkStyle(type, CoatingMarkShape.Circle, GetDefaultColorForType(type))
                {
                    Name = $"Compact-{type}",
                    MarkSize = 1.5,
                    TextSize = 1.2,
                    TextOffset = 2.0,
                    ShowText = false,  // 紧凑型不显示文本
                    FillMark = true,
                    FillOpacity = 0.5,
                    Description = $"紧凑型{type}标记",
                    Standard = CoatingStandard.Custom
                };

                styles[type] = style;
            }

            return styles;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 应用样式到镀膜标记
        /// </summary>
        /// <param name="coatingMark">目标镀膜标记</param>
        public void ApplyToCoatingMark(CoatingMark coatingMark)
        {
            if (coatingMark == null) return;

            coatingMark.MarkShape = MarkShape;
            coatingMark.Size = MarkSize;
            coatingMark.TextSize = TextSize;
            coatingMark.TextOffset = TextOffset;
            coatingMark.TextPosition = DefaultTextPosition;
            coatingMark.ShowText = ShowText;
            coatingMark.color = MarkColor;

            // 如果有指定文本样式，可以在这里设置
            // coatingMark.textStyleName = TextStyleName;
        }

        /// <summary>
        /// 创建标记实例
        /// </summary>
        /// <param name="center">标记中心位置</param>
        /// <returns>新的镀膜标记实例</returns>
        public CoatingMark CreateMarkInstance(LitMath.Vector2 center)
        {
            var mark = new CoatingMark(center, CoatingType, MarkSize)
            {
                MarkShape = MarkShape,
                TextSize = TextSize,
                TextOffset = TextOffset,
                TextPosition = DefaultTextPosition,
                ShowText = ShowText,
                color = MarkColor
            };

            return mark;
        }

        /// <summary>
        /// 验证样式设置
        /// </summary>
        /// <returns>验证结果</returns>
        public CoatingMarkStyleValidationResult Validate()
        {
            var result = new CoatingMarkStyleValidationResult();

            if (string.IsNullOrEmpty(Name))
            {
                result.Errors.Add("镀膜标记样式名称不能为空");
            }

            if (MarkSize <= 0)
            {
                result.Errors.Add("标记大小必须大于0");
            }

            if (TextSize <= 0)
            {
                result.Errors.Add("文本大小必须大于0");
            }

            if (TextOffset < 0)
            {
                result.Errors.Add("文本偏移不能为负数");
            }

            if (FillOpacity < 0 || FillOpacity > 1)
            {
                result.Errors.Add("填充透明度必须在0.0到1.0之间");
            }

            // 颜色对比度检查
            if (ShowText && AreColorsToSimilar(MarkColor, TextColor))
            {
                result.Warnings.Add("标记颜色和文本颜色过于相似，可能影响可读性");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// 创建样式变体
        /// </summary>
        /// <param name="newName">新样式名称</param>
        /// <param name="modifications">修改操作</param>
        /// <returns>新的样式变体</returns>
        public CoatingMarkStyle CreateVariant(string newName, Action<CoatingMarkStyle> modifications)
        {
            var variant = (CoatingMarkStyle)Clone();
            variant.Name = newName;
            modifications?.Invoke(variant);
            return variant;
        }

        /// <summary>
        /// 添加扩展属性
        /// </summary>
        /// <param name="key">属性键</param>
        /// <param name="value">属性值</param>
        public void AddExtendedProperty(string key, object value)
        {
            ExtendedProperties[key] = value;
        }

        /// <summary>
        /// 获取扩展属性
        /// </summary>
        /// <param name="key">属性键</param>
        /// <returns>属性值</returns>
        public T GetExtendedProperty<T>(string key, T defaultValue = default(T))
        {
            if (ExtendedProperties.ContainsKey(key) && ExtendedProperties[key] is T)
            {
                return (T)ExtendedProperties[key];
            }
            return defaultValue;
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
            
            return (rDiff + gDiff + bDiff) < 100;
        }

        /// <summary>
        /// 获取镀膜类型的默认颜色
        /// </summary>
        public static Color GetDefaultColorForType(CoatingType type)
        {
            switch (type)
            {
                case CoatingType.AR:
                    return Color.FromRGB(0, 255, 0);
                case CoatingType.HR:
                    return Color.FromRGB(255, 0, 0);
                case CoatingType.PR:
                    return Color.FromRGB(0, 0, 255);
                case CoatingType.BBAR:
                    return Color.FromRGB(128, 0, 128);
                default:
                    return Color.FromRGB(255, 255, 0);
            }
        }

        #endregion

        #region ICloneable Implementation

        public object Clone()
        {
            var cloned = new CoatingMarkStyle
            {
                Name = Name,
                CoatingType = CoatingType,
                MarkShape = MarkShape,
                MarkColor = MarkColor,
                MarkSize = MarkSize,
                LineWeight = LineWeight,
                TextStyleName = TextStyleName,
                TextColor = TextColor,
                TextSize = TextSize,
                TextOffset = TextOffset,
                DefaultTextPosition = DefaultTextPosition,
                ShowText = ShowText,
                FillMark = FillMark,
                FillColor = FillColor,
                FillOpacity = FillOpacity,
                Description = Description,
                Standard = Standard
            };

            // 复制扩展属性
            foreach (var kvp in ExtendedProperties)
            {
                cloned.ExtendedProperties[kvp.Key] = kvp.Value;
            }

            return cloned;
        }

        #endregion

        #region Override Methods

        public override string ToString()
        {
            return $"{Name} ({CoatingType}-{MarkShape})";
        }

        public override bool Equals(object obj)
        {
            if (obj is CoatingMarkStyle other)
            {
                return Name == other.Name && 
                       CoatingType == other.CoatingType && 
                       MarkShape == other.MarkShape;
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
    /// 镀膜标准类型枚举
    /// </summary>
    public enum CoatingStandard
    {
        /// <summary>
        /// ISO标准
        /// </summary>
        ISO,

        /// <summary>
        /// GB国标
        /// </summary>
        GB,

        /// <summary>
        /// ANSI标准
        /// </summary>
        ANSI,

        /// <summary>
        /// DIN标准
        /// </summary>
        DIN,

        /// <summary>
        /// 自定义
        /// </summary>
        Custom
    }

    /// <summary>
    /// 镀膜标记样式验证结果
    /// </summary>
    public class CoatingMarkStyleValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// 镀膜标记样式管理器
    /// </summary>
    public static class CoatingMarkStyleManager
    {
        /// <summary>
        /// 获取所有预定义的镀膜标记样式
        /// </summary>
        /// <returns>预定义样式字典</returns>
        public static Dictionary<string, CoatingMarkStyle> GetPredefinedStyles()
        {
            var styles = new Dictionary<string, CoatingMarkStyle>();

            // 添加基础样式
            styles.Add("AR-Default", CoatingMarkStyle.CreateARStyle());
            styles.Add("HR-Default", CoatingMarkStyle.CreateHRStyle());
            styles.Add("PR-Default", CoatingMarkStyle.CreatePRStyle());
            styles.Add("BBAR-Default", CoatingMarkStyle.CreateBBARStyle());

            return styles;
        }

        /// <summary>
        /// 根据标准获取样式集合
        /// </summary>
        /// <param name="standard">镀膜标准</param>
        /// <returns>样式集合</returns>
        public static Dictionary<CoatingType, CoatingMarkStyle> GetStylesByStandard(CoatingStandard standard)
        {
            switch (standard)
            {
                case CoatingStandard.ISO:
                    return CoatingMarkStyle.CreateISOStandardStyles();
                case CoatingStandard.GB:
                    return CoatingMarkStyle.CreateGBStandardStyles();
                default:
                    return CoatingMarkStyle.CreateCompactStyles();
            }
        }

        /// <summary>
        /// 创建自定义样式集合
        /// </summary>
        /// <param name="baseName">基础名称</param>
        /// <param name="configuration">配置操作</param>
        /// <returns>自定义样式集合</returns>
        public static Dictionary<CoatingType, CoatingMarkStyle> CreateCustomStyles(string baseName, 
            Action<CoatingMarkStyle> configuration = null)
        {
            var styles = new Dictionary<CoatingType, CoatingMarkStyle>();

            foreach (CoatingType type in Enum.GetValues(typeof(CoatingType)))
            {
                if (type == CoatingType.Custom) continue;

                var style = new CoatingMarkStyle(type, CoatingMarkShape.Triangle, 
                    CoatingMarkStyle.GetDefaultColorForType(type))
                {
                    Name = $"{baseName}-{type}",
                    Standard = CoatingStandard.Custom
                };

                configuration?.Invoke(style);
                styles[type] = style;
            }

            return styles;
        }

        /// <summary>
        /// 验证样式集合的一致性
        /// </summary>
        /// <param name="styles">样式集合</param>
        /// <returns>验证结果</returns>
        public static CoatingMarkStyleValidationResult ValidateStyleSet(Dictionary<CoatingType, CoatingMarkStyle> styles)
        {
            var result = new CoatingMarkStyleValidationResult();

            if (styles == null || styles.Count == 0)
            {
                result.Errors.Add("样式集合不能为空");
                result.IsValid = false;
                return result;
            }

            // 检查每个样式的有效性
            foreach (var kvp in styles)
            {
                var styleResult = kvp.Value.Validate();
                if (!styleResult.IsValid)
                {
                    result.Errors.AddRange(styleResult.Errors.Select(e => $"{kvp.Key}: {e}"));
                }
                result.Warnings.AddRange(styleResult.Warnings.Select(w => $"{kvp.Key}: {w}"));
            }

            // 检查标准一致性
            var standards = styles.Values.Select(s => s.Standard).Distinct().ToList();
            if (standards.Count > 1)
            {
                result.Warnings.Add($"样式集合包含多种标准: {string.Join(", ", standards)}");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }
    }
}