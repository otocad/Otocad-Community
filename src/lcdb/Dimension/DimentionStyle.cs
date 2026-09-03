using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using lcdb.Colors;

namespace lcdb
{
    /// <summary>
    /// 标注样式
    /// </summary>
    public class DimensionStyle
    {
        #region 静态实例

        private static DimensionStyle _default;
        private static DimensionStyle _iso25;
        private static DimensionStyle _gb;
        private static DimensionStyle _gbOptical;

        /// <summary>
        /// 默认标注样式
        /// </summary>
        public static DimensionStyle Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new DimensionStyle
                    {
                        Name = "Standard",
                        TextHeight = 2.5,
                        ArrowSize = 2.5,
                        TextInsideHorizontal = true,
                        TextColor = lcdb.Colors.Color.ByLayer,
                        ExtensionLineExtend = 1.25,
                        ExtensionLineOffset = 0.625,
                        DimensionLineGap = 0.625,
                        DecimalFormat = "F2"
                    };
                }
                return _default;
            }
        }

        /// <summary>
        /// ISO-25标准样式
        /// </summary>
        public static DimensionStyle Iso25
        {
            get
            {
                if (_iso25 == null)
                {
                    _iso25 = new DimensionStyle
                    {
                        Name = "ISO-25",
                        TextHeight = 2.5,
                        ArrowSize = 2.5,
                        TextInsideHorizontal = true,
                        TextColor = lcdb.Colors.Color.ByLayer,
                        ExtensionLineExtend = 1.25,
                        ExtensionLineOffset = 0.625,
                        DimensionLineGap = 0.625,
                        DecimalFormat = "F2"
                    };
                }
                return _iso25;
            }
        }

        /// <summary>
        /// 国标样式（符合GB/T 13362光学制图标准）
        /// </summary>
        public static DimensionStyle GB
        {
            get
            {
                if (_gb == null)
                {
                    _gb = new DimensionStyle
                    {
                        Name = "GB",
                        // 文字高度符合GB/T 13362要求（3.5mm）
                        TextHeight = 3.5,
                        // 箭头大小适当（3mm）
                        ArrowSize = 3.0,
                        // 文字保持水平
                        TextInsideHorizontal = true,
                        // 颜色随层
                        TextColor = lcdb.Colors.Color.ByLayer,
                        // 尺寸界线超出尺寸线2mm
                        ExtensionLineExtend = 2.0,
                        // 尺寸界线起点偏移为0（贴近被标注对象）
                        ExtensionLineOffset = 0.0,
                        // 文字与尺寸线间隙
                        DimensionLineGap = 0.7,
                        // 数字格式保留2位小数
                        DecimalFormat = "F2",
                        // 线宽使用默认（0.35mm）
                        LineWeight = LineWeight.ByLayer,
                        // 显示两条尺寸界线
                        ShowExtensionLine1 = true,
                        ShowExtensionLine2 = true
                    };
                }
                return _gb;
            }
        }

        /// <summary>
        /// 国标光学样式（专用于光学制图）
        /// </summary>
        public static DimensionStyle GBOptical
        {
            get
            {
                if (_gbOptical == null)
                {
                    _gbOptical = new DimensionStyle
                    {
                        Name = "GB-Optical",
                        // 文字高度符合光学制图要求（3.5mm）
                        TextHeight = 3.5,
                        // 箭头尺寸（2.5mm，光学图纸通常较精密）
                        ArrowSize = 2.5,
                        // 文字保持水平
                        TextInsideHorizontal = true,
                        // 颜色随层
                        TextColor = lcdb.Colors.Color.ByLayer,
                        // 尺寸界线超出尺寸线1.5mm（光学图纸更紧凑）
                        ExtensionLineExtend = 1.5,
                        // 尺寸界线起点偏移0.5mm（避免与光学元件轮廓重叠）
                        ExtensionLineOffset = 0.5,
                        // 文字与尺寸线间隙
                        DimensionLineGap = 0.5,
                        // 数字格式保留3位小数（光学零件精度要求高）
                        DecimalFormat = "F3",
                        // 线宽使用细线（0.25mm）
                        LineWeight = LineWeight.ByLayer,
                        // 显示两条尺寸界线
                        ShowExtensionLine1 = true,
                        ShowExtensionLine2 = true
                    };
                }
                return _gbOptical;
            }
        }

        #endregion

        #region 属性

        /// <summary>
        /// 样式名称
        /// </summary>
        public string Name { get; set; } = "Standard";

        /// <summary>
        /// 文字高度
        /// </summary>
        public double TextHeight { get; set; } = 2.5;

        /// <summary>
        /// 箭头大小
        /// </summary>
        public double ArrowSize { get; set; } = 2.5;

        /// <summary>
        /// 文字是否水平放置
        /// </summary>
        public bool TextInsideHorizontal { get; set; } = true;

        /// <summary>
        /// 竖直标注 (Ø 等, 方向≈±90°) 的文字是否竖排对齐尺寸线。
        /// true = 竖排 (GB/ISO 默认); false = 即便竖直标注文字也水平书写 (部分用户/标准习惯)。
        /// 跟随出图标准/用户格式设定。
        /// </summary>
        public bool VerticalTextAligned { get; set; } = true;

        /// <summary>
        /// 文字颜色
        /// </summary>
        public lcdb.Colors.Color TextColor { get; set; } = lcdb.Colors.Color.ByLayer;

        /// <summary>
        /// 尺寸界线超出尺寸线的距离
        /// </summary>
        public double ExtensionLineExtend { get; set; } = 1.25;

        /// <summary>
        /// 尺寸界线起点偏移
        /// </summary>
        public double ExtensionLineOffset { get; set; } = 0.625;

        /// <summary>
        /// 文字与尺寸线的间隙
        /// </summary>
        public double DimensionLineGap { get; set; } = 0.625;

        /// <summary>
        /// 小数格式
        /// </summary>
        public string DecimalFormat { get; set; } = "F2";

        /// <summary>
        /// 线宽
        /// </summary>
        public LineWeight LineWeight { get; set; } = LineWeight.ByLayer;

        /// <summary>
        /// 是否显示尺寸界线1
        /// </summary>
        public bool ShowExtensionLine1 { get; set; } = true;

        /// <summary>
        /// 是否显示尺寸界线2
        /// </summary>
        public bool ShowExtensionLine2 { get; set; } = true;

        /// <summary>
        /// 公差显示模式
        /// </summary>
        public ToleranceDisplayMode ToleranceDisplay { get; set; } = ToleranceDisplayMode.None;

        /// <summary>
        /// 上偏差值
        /// </summary>
        public double TolerancePlus { get; set; } = 0.0;

        /// <summary>
        /// 下偏差值
        /// </summary>
        public double ToleranceMinus { get; set; } = 0.0;

        /// <summary>
        /// 公差文字高度比例（相对于主文字）
        /// </summary>
        public double ToleranceTextScale { get; set; } = 0.7;

        /// <summary>
        /// 公差精度（小数位数）
        /// </summary>
        public int TolerancePrecision { get; set; } = 3;

        /// <summary>
        /// 角度标注单位
        /// </summary>
        public AngleUnit AngleUnit { get; set; } = AngleUnit.Degrees;

        /// <summary>
        /// 前缀文字
        /// </summary>
        public string Prefix { get; set; } = "";

        /// <summary>
        /// 后缀文字
        /// </summary>
        public string Suffix { get; set; } = "";

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建新的标注样式
        /// </summary>
        public DimensionStyle()
        {
        }

        /// <summary>
        /// 创建新的标注样式
        /// </summary>
        public DimensionStyle(string name)
        {
            Name = name;
        }

        #endregion

        #region 方法

        /// <summary>
        /// 克隆标注样式
        /// </summary>
        public DimensionStyle Clone()
        {
            return new DimensionStyle
            {
                Name = Name,
                TextHeight = TextHeight,
                ArrowSize = ArrowSize,
                TextInsideHorizontal = TextInsideHorizontal,
                VerticalTextAligned = VerticalTextAligned,
                TextColor = TextColor,
                ExtensionLineExtend = ExtensionLineExtend,
                ExtensionLineOffset = ExtensionLineOffset,
                DimensionLineGap = DimensionLineGap,
                DecimalFormat = DecimalFormat,
                LineWeight = LineWeight,
                ShowExtensionLine1 = ShowExtensionLine1,
                ShowExtensionLine2 = ShowExtensionLine2,
                ToleranceDisplay = ToleranceDisplay,
                TolerancePlus = TolerancePlus,
                ToleranceMinus = ToleranceMinus,
                ToleranceTextScale = ToleranceTextScale,
                TolerancePrecision = TolerancePrecision,
                AngleUnit = AngleUnit,
                Prefix = Prefix,
                Suffix = Suffix
            };
        }

        #endregion
    }

    /// <summary>
    /// 公差显示模式
    /// </summary>
    public enum ToleranceDisplayMode
    {
        /// <summary>
        /// 不显示公差
        /// </summary>
        None,
        
        /// <summary>
        /// 对称公差（±）
        /// </summary>
        Symmetrical,
        
        /// <summary>
        /// 偏差公差（上下偏差）
        /// </summary>
        Deviation,
        
        /// <summary>
        /// 极限尺寸
        /// </summary>
        Limits,
        
        /// <summary>
        /// 基本尺寸（带方框）
        /// </summary>
        Basic
    }

    /// <summary>
    /// 角度单位
    /// </summary>
    public enum AngleUnit
    {
        /// <summary>
        /// 度
        /// </summary>
        Degrees,
        
        /// <summary>
        /// 度分秒
        /// </summary>
        DegreesMinutesSeconds,
        
        /// <summary>
        /// 弧度
        /// </summary>
        Radians,
        
        /// <summary>
        /// 百分度
        /// </summary>
        Grads
    }
}
