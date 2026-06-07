using System;
using System.Collections.Generic;
using System.Linq;
using lcdb.Colors;

namespace lcdb.Standards
{
    /// <summary>
    /// 线型样式定义
    /// </summary>
    public class LineStyle : ICloneable
    {
        #region Properties

        /// <summary>
        /// 样式名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 线型类型
        /// </summary>
        public LineType LineType { get; set; }

        /// <summary>
        /// 线宽
        /// </summary>
        public LineWeight LineWeight { get; set; }

        /// <summary>
        /// 颜色
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// 虚线模式（用于自定义线型）
        /// </summary>
        public double[] DashPattern { get; set; }

        /// <summary>
        /// 线型比例
        /// </summary>
        public double Scale { get; set; } = 1.0;

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 用途标签
        /// </summary>
        public LineUsage Usage { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public LineStyle()
        {
            Name = "Default";
            LineType = LineType.Solid;
            LineWeight = LineWeight.LineWeight025;
            Color = Color.ByLayer;
            Usage = LineUsage.General;
        }

        /// <summary>
        /// 参数化构造函数
        /// </summary>
        public LineStyle(string name, LineType lineType, LineWeight lineWeight, Color color)
        {
            Name = name;
            LineType = lineType;
            LineWeight = lineWeight;
            Color = color;
            Usage = LineUsage.General;
        }

        /// <summary>
        /// 完整参数构造函数
        /// </summary>
        public LineStyle(string name, LineType lineType, LineWeight lineWeight, Color color, 
                        LineUsage usage, double scale = 1.0, string description = "")
        {
            Name = name;
            LineType = lineType;
            LineWeight = lineWeight;
            Color = color;
            Usage = usage;
            Scale = scale;
            Description = description;
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// 创建轮廓线样式
        /// </summary>
        public static LineStyle CreateOutlineStyle()
        {
            return new LineStyle("Outline", LineType.Solid, LineWeight.LineWeight050, 
                               Color.FromRGB(0, 0, 0), LineUsage.Outline, 1.0, "对象轮廓线");
        }

        /// <summary>
        /// 创建隐藏线样式
        /// </summary>
        public static LineStyle CreateHiddenStyle()
        {
            return new LineStyle("Hidden", LineType.Dash, LineWeight.LineWeight025, 
                               Color.FromRGB(128, 128, 128), LineUsage.Hidden, 1.0, "隐藏线")
            {
                DashPattern = new double[] { 3.0, 1.5 }
            };
        }

        /// <summary>
        /// 创建中心线样式
        /// </summary>
        public static LineStyle CreateCenterLineStyle()
        {
            return new LineStyle("CenterLine", LineType.DashDot, LineWeight.LineWeight025, 
                               Color.FromRGB(0, 128, 0), LineUsage.CenterLine, 1.0, "中心线")
            {
                DashPattern = new double[] { 12.0, 2.0, 2.0, 2.0 }
            };
        }

        /// <summary>
        /// 创建尺寸线样式
        /// </summary>
        public static LineStyle CreateDimensionStyle()
        {
            return new LineStyle("Dimension", LineType.Solid, LineWeight.LineWeight020, 
                               Color.FromRGB(255, 0, 0), LineUsage.Dimension, 1.0, "尺寸标注线");
        }

        /// <summary>
        /// 创建引出线样式
        /// </summary>
        public static LineStyle CreateLeaderStyle()
        {
            return new LineStyle("Leader", LineType.Solid, LineWeight.LineWeight020, 
                               Color.FromRGB(0, 0, 255), LineUsage.Leader, 1.0, "引出线");
        }

        /// <summary>
        /// 创建剖面线样式
        /// </summary>
        public static LineStyle CreateSectionStyle()
        {
            return new LineStyle("Section", LineType.Solid, LineWeight.LineWeight060, 
                               Color.FromRGB(255, 0, 0), LineUsage.Section, 1.0, "剖面线");
        }

        /// <summary>
        /// 创建断裂线样式
        /// </summary>
        public static LineStyle CreateBreakLineStyle()
        {
            return new LineStyle("BreakLine", LineType.Custom, LineWeight.LineWeight030, 
                               Color.FromRGB(128, 64, 0), LineUsage.BreakLine, 1.0, "断裂线")
            {
                DashPattern = new double[] { 5.0, 2.0, 1.0, 2.0, 1.0, 2.0 }
            };
        }

        /// <summary>
        /// 创建边界线样式
        /// </summary>
        public static LineStyle CreateBoundaryStyle()
        {
            return new LineStyle("Boundary", LineType.DashDotDot, LineWeight.LineWeight040, 
                               Color.FromRGB(128, 0, 128), LineUsage.Boundary, 1.0, "边界线")
            {
                DashPattern = new double[] { 10.0, 2.0, 2.0, 2.0, 2.0, 2.0 }
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 应用样式到实体
        /// </summary>
        /// <param name="entity">目标实体</param>
        public void ApplyToEntity(Entity entity)
        {
            if (entity == null) return;

            entity.color = Color;
            entity.lineWeight = LineWeight;
            entity.lineType = LineType;
        }

        /// <summary>
        /// 获取线宽的实际值（毫米）
        /// </summary>
        /// <returns>线宽值</returns>
        public double GetLineWidthInMM()
        {
            return (double)LineWeight / 100.0;
        }

        /// <summary>
        /// 验证样式有效性
        /// </summary>
        /// <returns>验证结果</returns>
        public LineStyleValidationResult Validate()
        {
            var result = new LineStyleValidationResult();

            if (string.IsNullOrEmpty(Name))
            {
                result.Errors.Add("线型样式名称不能为空");
            }

            if (Scale <= 0)
            {
                result.Errors.Add("线型比例必须大于0");
            }

            if (LineType == LineType.Custom && (DashPattern == null || DashPattern.Length == 0))
            {
                result.Errors.Add("自定义线型必须定义虚线模式");
            }

            if (DashPattern != null)
            {
                foreach (var dash in DashPattern)
                {
                    if (dash <= 0)
                    {
                        result.Errors.Add("虚线模式中的值必须大于0");
                        break;
                    }
                }
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// 创建变体样式
        /// </summary>
        /// <param name="newName">新样式名称</param>
        /// <param name="modifications">修改操作</param>
        /// <returns>新的样式变体</returns>
        public LineStyle CreateVariant(string newName, Action<LineStyle> modifications)
        {
            var variant = (LineStyle)Clone();
            variant.Name = newName;
            modifications?.Invoke(variant);
            return variant;
        }

        #endregion

        #region ICloneable Implementation

        public object Clone()
        {
            var cloned = new LineStyle
            {
                Name = Name,
                LineType = LineType,
                LineWeight = LineWeight,
                Color = Color,
                Scale = Scale,
                Description = Description,
                Usage = Usage
            };

            if (DashPattern != null)
            {
                cloned.DashPattern = new double[DashPattern.Length];
                Array.Copy(DashPattern, cloned.DashPattern, DashPattern.Length);
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
            if (obj is LineStyle other)
            {
                return Name == other.Name && 
                       LineType == other.LineType && 
                       LineWeight == other.LineWeight &&
                       Color.Equals(other.Color);
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
    /// 线型用途枚举
    /// </summary>
    public enum LineUsage
    {
        /// <summary>
        /// 通用线型
        /// </summary>
        General,

        /// <summary>
        /// 轮廓线
        /// </summary>
        Outline,

        /// <summary>
        /// 隐藏线
        /// </summary>
        Hidden,

        /// <summary>
        /// 中心线
        /// </summary>
        CenterLine,

        /// <summary>
        /// 尺寸线
        /// </summary>
        Dimension,

        /// <summary>
        /// 引出线
        /// </summary>
        Leader,

        /// <summary>
        /// 剖面线
        /// </summary>
        Section,

        /// <summary>
        /// 断裂线
        /// </summary>
        BreakLine,

        /// <summary>
        /// 边界线
        /// </summary>
        Boundary,

        /// <summary>
        /// 辅助线
        /// </summary>
        Construction,

        /// <summary>
        /// 文字边框
        /// </summary>
        TextBorder,

        /// <summary>
        /// 网格线
        /// </summary>
        Grid,

        /// <summary>
        /// 镀膜标记
        /// </summary>
        CoatingMark
    }

    /// <summary>
    /// 线型样式验证结果
    /// </summary>
    public class LineStyleValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// 线型样式管理器
    /// </summary>
    public static class LineStyleManager
    {
        /// <summary>
        /// 获取所有预定义的线型样式
        /// </summary>
        /// <returns>预定义样式字典</returns>
        public static Dictionary<string, LineStyle> GetPredefinedStyles()
        {
            return new Dictionary<string, LineStyle>
            {
                { "Outline", LineStyle.CreateOutlineStyle() },
                { "Hidden", LineStyle.CreateHiddenStyle() },
                { "CenterLine", LineStyle.CreateCenterLineStyle() },
                { "Dimension", LineStyle.CreateDimensionStyle() },
                { "Leader", LineStyle.CreateLeaderStyle() },
                { "Section", LineStyle.CreateSectionStyle() },
                { "BreakLine", LineStyle.CreateBreakLineStyle() },
                { "Boundary", LineStyle.CreateBoundaryStyle() }
            };
        }

        /// <summary>
        /// 根据用途获取推荐的线型样式
        /// </summary>
        /// <param name="usage">线型用途</param>
        /// <returns>推荐的线型样式</returns>
        public static LineStyle GetRecommendedStyle(LineUsage usage)
        {
            switch (usage)
            {
                case LineUsage.Outline:
                    return LineStyle.CreateOutlineStyle();
                case LineUsage.Hidden:
                    return LineStyle.CreateHiddenStyle();
                case LineUsage.CenterLine:
                    return LineStyle.CreateCenterLineStyle();
                case LineUsage.Dimension:
                    return LineStyle.CreateDimensionStyle();
                case LineUsage.Leader:
                    return LineStyle.CreateLeaderStyle();
                case LineUsage.Section:
                    return LineStyle.CreateSectionStyle();
                case LineUsage.BreakLine:
                    return LineStyle.CreateBreakLineStyle();
                case LineUsage.Boundary:
                    return LineStyle.CreateBoundaryStyle();
                default:
                    return new LineStyle();
            }
        }

        /// <summary>
        /// 创建符合ISO标准的线型样式集合
        /// </summary>
        /// <returns>ISO标准线型样式</returns>
        public static Dictionary<string, LineStyle> CreateISOLineStyles()
        {
            var styles = new Dictionary<string, LineStyle>();

            // ISO 128标准线型
            styles.Add("ISO-Continuous", new LineStyle("ISO-Continuous", LineType.Solid, 
                      LineWeight.LineWeight050, Color.FromRGB(0, 0, 0), LineUsage.Outline));

            styles.Add("ISO-Dashed", new LineStyle("ISO-Dashed", LineType.Dash, 
                      LineWeight.LineWeight025, Color.FromRGB(0, 0, 0), LineUsage.Hidden)
            {
                DashPattern = new double[] { 12, 3 }
            });

            styles.Add("ISO-Chain", new LineStyle("ISO-Chain", LineType.DashDot, 
                      LineWeight.LineWeight025, Color.FromRGB(0, 0, 0), LineUsage.CenterLine)
            {
                DashPattern = new double[] { 24, 3, 6, 3 }
            });

            styles.Add("ISO-ChainDouble", new LineStyle("ISO-ChainDouble", LineType.DashDotDot, 
                      LineWeight.LineWeight025, Color.FromRGB(0, 0, 0), LineUsage.Boundary)
            {
                DashPattern = new double[] { 24, 3, 6, 3, 6, 3 }
            });

            return styles;
        }

        /// <summary>
        /// 创建符合GB标准的线型样式集合
        /// </summary>
        /// <returns>GB标准线型样式</returns>
        public static Dictionary<string, LineStyle> CreateGBLineStyles()
        {
            var styles = new Dictionary<string, LineStyle>();

            // GB/T 4457.4标准线型
            styles.Add("GB-Continuous", new LineStyle("GB-Continuous", LineType.Solid, 
                      LineWeight.LineWeight050, Color.FromRGB(0, 0, 0), LineUsage.Outline));

            styles.Add("GB-Dashed", new LineStyle("GB-Dashed", LineType.Dash, 
                      LineWeight.LineWeight025, Color.FromRGB(0, 0, 0), LineUsage.Hidden)
            {
                DashPattern = new double[] { 8, 2 }
            });

            styles.Add("GB-Chain", new LineStyle("GB-Chain", LineType.DashDot, 
                      LineWeight.LineWeight025, Color.FromRGB(0, 0, 0), LineUsage.CenterLine)
            {
                DashPattern = new double[] { 20, 2, 4, 2 }
            });

            styles.Add("GB-ChainDouble", new LineStyle("GB-ChainDouble", LineType.DashDotDot, 
                      LineWeight.LineWeight025, Color.FromRGB(0, 0, 0), LineUsage.Boundary)
            {
                DashPattern = new double[] { 20, 2, 4, 2, 4, 2 }
            });

            return styles;
        }
    }
}