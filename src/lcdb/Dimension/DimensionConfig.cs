using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using lcdb.Colors;

namespace lcdb
{
    /// <summary>
    /// 标注配置类
    /// </summary>
    public class DimensionConfig
    {
        /// <summary>
        /// 配置版本
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// 默认样式名称
        /// </summary>
        public string DefaultStyleName { get; set; } = "GB";

        /// <summary>
        /// 标注样式列表
        /// </summary>
        public List<DimensionStyleConfig> Styles { get; set; } = new List<DimensionStyleConfig>();

        /// <summary>
        /// 全局设置
        /// </summary>
        public GlobalDimensionSettings GlobalSettings { get; set; } = new GlobalDimensionSettings();

        /// <summary>
        /// 光学特定设置
        /// </summary>
        public OpticalDimensionSettings OpticalSettings { get; set; } = new OpticalDimensionSettings();
    }

    /// <summary>
    /// 标注样式配置
    /// </summary>
    public class DimensionStyleConfig
    {
        /// <summary>
        /// 样式名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 样式描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 基于的样式（用于继承）
        /// </summary>
        public string BaseStyle { get; set; }

        /// <summary>
        /// 文字高度
        /// </summary>
        public double? TextHeight { get; set; }

        /// <summary>
        /// 箭头大小
        /// </summary>
        public double? ArrowSize { get; set; }

        /// <summary>
        /// 文字是否水平放置
        /// </summary>
        public bool? TextInsideHorizontal { get; set; }

        /// <summary>
        /// 文字颜色（颜色名称或RGB值）
        /// </summary>
        public string TextColor { get; set; }

        /// <summary>
        /// 尺寸界线超出尺寸线的距离
        /// </summary>
        public double? ExtensionLineExtend { get; set; }

        /// <summary>
        /// 尺寸界线起点偏移
        /// </summary>
        public double? ExtensionLineOffset { get; set; }

        /// <summary>
        /// 文字与尺寸线的间隙
        /// </summary>
        public double? DimensionLineGap { get; set; }

        /// <summary>
        /// 小数格式
        /// </summary>
        public string DecimalFormat { get; set; }

        /// <summary>
        /// 线宽名称
        /// </summary>
        public string LineWeight { get; set; }

        /// <summary>
        /// 是否显示尺寸界线1
        /// </summary>
        public bool? ShowExtensionLine1 { get; set; }

        /// <summary>
        /// 是否显示尺寸界线2
        /// </summary>
        public bool? ShowExtensionLine2 { get; set; }

        /// <summary>
        /// 公差显示模式
        /// </summary>
        public string ToleranceDisplay { get; set; }

        /// <summary>
        /// 上偏差值
        /// </summary>
        public double? TolerancePlus { get; set; }

        /// <summary>
        /// 下偏差值
        /// </summary>
        public double? ToleranceMinus { get; set; }

        /// <summary>
        /// 公差文字高度比例
        /// </summary>
        public double? ToleranceTextScale { get; set; }

        /// <summary>
        /// 公差精度
        /// </summary>
        public int? TolerancePrecision { get; set; }

        /// <summary>
        /// 角度单位
        /// </summary>
        public string AngleUnit { get; set; }

        /// <summary>
        /// 前缀文字
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// 后缀文字
        /// </summary>
        public string Suffix { get; set; }

        /// <summary>
        /// 转换为DimensionStyle对象
        /// </summary>
        public DimensionStyle ToDimensionStyle()
        {
            var style = new DimensionStyle();
            ApplyToDimensionStyle(style);
            return style;
        }

        /// <summary>
        /// 应用配置到DimensionStyle对象
        /// </summary>
        public void ApplyToDimensionStyle(DimensionStyle style)
        {
            if (!string.IsNullOrEmpty(Name))
                style.Name = Name;

            if (TextHeight.HasValue)
                style.TextHeight = TextHeight.Value;

            if (ArrowSize.HasValue)
                style.ArrowSize = ArrowSize.Value;

            if (TextInsideHorizontal.HasValue)
                style.TextInsideHorizontal = TextInsideHorizontal.Value;

            if (!string.IsNullOrEmpty(TextColor))
            {
                // 解析颜色
                if (TextColor.Equals("ByLayer", StringComparison.OrdinalIgnoreCase))
                    style.TextColor = Color.ByLayer;
                else if (TextColor.Equals("ByBlock", StringComparison.OrdinalIgnoreCase))
                    style.TextColor = Color.ByBlock;
                else
                {
                    // 尝试解析为颜色名称
                    try
                    {
                        var systemColor = System.Drawing.Color.FromName(TextColor);
                        style.TextColor = Color.FromColor(systemColor);
                    }
                    catch
                    {
                        // 如果解析失败，使用默认颜色
                        style.TextColor = Color.ByLayer;
                    }
                }
            }

            if (ExtensionLineExtend.HasValue)
                style.ExtensionLineExtend = ExtensionLineExtend.Value;

            if (ExtensionLineOffset.HasValue)
                style.ExtensionLineOffset = ExtensionLineOffset.Value;

            if (DimensionLineGap.HasValue)
                style.DimensionLineGap = DimensionLineGap.Value;

            if (!string.IsNullOrEmpty(DecimalFormat))
                style.DecimalFormat = DecimalFormat;

            if (!string.IsNullOrEmpty(LineWeight))
            {
                if (Enum.TryParse<LineWeight>(LineWeight, out var lw))
                    style.LineWeight = lw;
            }

            if (ShowExtensionLine1.HasValue)
                style.ShowExtensionLine1 = ShowExtensionLine1.Value;

            if (ShowExtensionLine2.HasValue)
                style.ShowExtensionLine2 = ShowExtensionLine2.Value;

            if (!string.IsNullOrEmpty(ToleranceDisplay))
            {
                if (Enum.TryParse<ToleranceDisplayMode>(ToleranceDisplay, out var td))
                    style.ToleranceDisplay = td;
            }

            if (TolerancePlus.HasValue)
                style.TolerancePlus = TolerancePlus.Value;

            if (ToleranceMinus.HasValue)
                style.ToleranceMinus = ToleranceMinus.Value;

            if (ToleranceTextScale.HasValue)
                style.ToleranceTextScale = ToleranceTextScale.Value;

            if (TolerancePrecision.HasValue)
                style.TolerancePrecision = TolerancePrecision.Value;

            if (!string.IsNullOrEmpty(AngleUnit))
            {
                if (Enum.TryParse<AngleUnit>(AngleUnit, out var au))
                    style.AngleUnit = au;
            }

            if (Prefix != null)
                style.Prefix = Prefix;

            if (Suffix != null)
                style.Suffix = Suffix;
        }

        /// <summary>
        /// 从DimensionStyle创建配置
        /// </summary>
        public static DimensionStyleConfig FromDimensionStyle(DimensionStyle style)
        {
            return new DimensionStyleConfig
            {
                Name = style.Name,
                TextHeight = style.TextHeight,
                ArrowSize = style.ArrowSize,
                TextInsideHorizontal = style.TextInsideHorizontal,
                TextColor = style.TextColor == Color.ByLayer ? "ByLayer" : 
                           style.TextColor == Color.ByBlock ? "ByBlock" : 
                           style.TextColor.Name,
                ExtensionLineExtend = style.ExtensionLineExtend,
                ExtensionLineOffset = style.ExtensionLineOffset,
                DimensionLineGap = style.DimensionLineGap,
                DecimalFormat = style.DecimalFormat,
                LineWeight = style.LineWeight.ToString(),
                ShowExtensionLine1 = style.ShowExtensionLine1,
                ShowExtensionLine2 = style.ShowExtensionLine2,
                ToleranceDisplay = style.ToleranceDisplay.ToString(),
                TolerancePlus = style.TolerancePlus,
                ToleranceMinus = style.ToleranceMinus,
                ToleranceTextScale = style.ToleranceTextScale,
                TolerancePrecision = style.TolerancePrecision,
                AngleUnit = style.AngleUnit.ToString(),
                Prefix = style.Prefix,
                Suffix = style.Suffix
            };
        }
    }

    /// <summary>
    /// 全局标注设置
    /// </summary>
    public class GlobalDimensionSettings
    {
        /// <summary>
        /// 自动调整文字位置
        /// </summary>
        public bool AutoAdjustTextPosition { get; set; } = true;

        /// <summary>
        /// 关联标注（标注随实体变化）
        /// </summary>
        public bool AssociativeDimensions { get; set; } = true;

        /// <summary>
        /// 标注单位
        /// </summary>
        public string DimensionUnit { get; set; } = "Millimeters";

        /// <summary>
        /// 标注比例因子
        /// </summary>
        public double DimensionScale { get; set; } = 1.0;

        /// <summary>
        /// 角度精度（小数位数）
        /// </summary>
        public int AnglePrecision { get; set; } = 0;

        /// <summary>
        /// 抑制前导零
        /// </summary>
        public bool SuppressLeadingZeros { get; set; } = false;

        /// <summary>
        /// 抑制后续零
        /// </summary>
        public bool SuppressTrailingZeros { get; set; } = true;
    }

    /// <summary>
    /// 光学标注特定设置
    /// </summary>
    public class OpticalDimensionSettings
    {
        /// <summary>
        /// 默认公差等级
        /// </summary>
        public string DefaultToleranceGrade { get; set; } = "IT7";

        /// <summary>
        /// 显示表面粗糙度
        /// </summary>
        public bool ShowSurfaceRoughness { get; set; } = false;

        /// <summary>
        /// 默认表面粗糙度值
        /// </summary>
        public double DefaultRoughnessValue { get; set; } = 1.6;

        /// <summary>
        /// 光学表面标记
        /// </summary>
        public bool ShowOpticalSurfaceMarks { get; set; } = true;

        /// <summary>
        /// 曲率半径标注格式
        /// </summary>
        public string RadiusDimensionFormat { get; set; } = "R{0}";

        /// <summary>
        /// 厚度标注格式
        /// </summary>
        public string ThicknessDimensionFormat { get; set; } = "t={0}";

        /// <summary>
        /// 折射率标注格式
        /// </summary>
        public string RefractiveIndexFormat { get; set; } = "n={0}";

        /// <summary>
        /// 焦距标注格式
        /// </summary>
        public string FocalLengthFormat { get; set; } = "f={0}";

        /// <summary>
        /// 公差标准
        /// </summary>
        public List<ToleranceStandard> ToleranceStandards { get; set; } = new List<ToleranceStandard>();
    }

    /// <summary>
    /// 公差标准
    /// </summary>
    public class ToleranceStandard
    {
        /// <summary>
        /// 标准名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 适用范围（最小值）
        /// </summary>
        public double MinValue { get; set; }

        /// <summary>
        /// 适用范围（最大值）
        /// </summary>
        public double MaxValue { get; set; }

        /// <summary>
        /// 公差值
        /// </summary>
        public double ToleranceValue { get; set; }

        /// <summary>
        /// 公差等级
        /// </summary>
        public string Grade { get; set; }
    }
}