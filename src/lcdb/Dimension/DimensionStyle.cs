using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Xml;
using lcdb.Colors;
using lcdb;
using LitMath;

namespace lcdb.Dimension
{
    /// <summary>
    /// 标注样式类，定义标注的视觉外观和行为
    /// </summary>
    public class DimensionStyle : ICloneable
    {
        #region 字段

        private string _name;
        private double _textHeight;
        private double _arrowSize;
        private double _extensionLineExtension;
        private double _extensionLineOffset;
        private double _dimensionLineGap;
        private int _textPrecision;
        private string _prefix;
        private string _suffix;
        private lcdb.Colors.Color _textColor;
        private lcdb.Colors.Color _dimensionLineColor;
        private lcdb.Colors.Color _extensionLineColor;
        private LineWeight _dimensionLineWeight;
        private LineWeight _extensionLineWeight;
        private bool _showExtensionLines;
        private bool _showDimensionLine;
        private DimensionTextAlignment _textAlignment;
        private DimensionTextPosition _textPosition;
        private ArrowheadType _arrowheadType;
        private double _textOffset;
        private double _scaleFactor;
        private bool _suppressLeadingZeros;
        private bool _suppressTrailingZeros;
        private string _alternateUnitPrefix;
        private string _alternateUnitSuffix;
        private double _alternateUnitScaleFactor;
        private int _alternateUnitPrecision;
        private bool _showAlternateUnits;

        #endregion

        #region 静态属性

        private static DimensionStyle _default;

        /// <summary>
        /// 获取默认标注样式
        /// </summary>
        public static DimensionStyle Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new DimensionStyle("Standard");
                }
                return _default;
            }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化标注样式的新实例
        /// </summary>
        public DimensionStyle() : this("Standard")
        {
        }

        /// <summary>
        /// 使用指定名称初始化标注样式的新实例
        /// </summary>
        public DimensionStyle(string name)
        {
            _name = name;
            
            // 默认值
            _textHeight = 2.5;
            _arrowSize = 2.5;
            _extensionLineExtension = 1.25;
            _extensionLineOffset = 0.625;
            _dimensionLineGap = 0.625;
            _textPrecision = 2;
            _prefix = "";
            _suffix = "";
            _textColor = lcdb.Colors.Color.Black;
            _dimensionLineColor = lcdb.Colors.Color.Black;
            _extensionLineColor = lcdb.Colors.Color.Black;
            _dimensionLineWeight = LineWeight.ByLayer;
            _extensionLineWeight = LineWeight.ByLayer;
            _showExtensionLines = true;
            _showDimensionLine = true;
            _textAlignment = DimensionTextAlignment.Centered;
            _textPosition = DimensionTextPosition.Above;
            _arrowheadType = ArrowheadType.ClosedFilled;
            _textOffset = 0.625;
            _scaleFactor = 1.0;
            _suppressLeadingZeros = false;
            _suppressTrailingZeros = false;
            _alternateUnitPrefix = "";
            _alternateUnitSuffix = "";
            _alternateUnitScaleFactor = 1.0;
            _alternateUnitPrecision = 2;
            _showAlternateUnits = false;
        }

        #endregion

        #region 属性

        /// <summary>
        /// 获取或设置样式名称
        /// </summary>
        [Category("基本属性")]
        [DisplayName("名称")]
        [Description("标注样式的名称")]
        public string Name
        {
            get { return _name; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("样式名称不能为空");
                _name = value;
            }
        }

        /// <summary>
        /// 获取或设置文本高度
        /// </summary>
        [Category("文本")]
        [DisplayName("文本高度")]
        [Description("标注文本的高度")]
        public double TextHeight
        {
            get { return _textHeight; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("文本高度必须大于0");
                _textHeight = value;
            }
        }

        /// <summary>
        /// 获取或设置箭头大小
        /// </summary>
        [Category("箭头")]
        [DisplayName("箭头大小")]
        [Description("标注箭头的大小")]
        public double ArrowSize
        {
            get { return _arrowSize; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("箭头大小必须大于0");
                _arrowSize = value;
            }
        }

        /// <summary>
        /// 获取或设置尺寸界线超出尺寸线的距离
        /// </summary>
        [Category("尺寸界线")]
        [DisplayName("超出长度")]
        [Description("尺寸界线超出尺寸线的距离")]
        public double ExtensionLineExtension
        {
            get { return _extensionLineExtension; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("超出长度不能为负数");
                _extensionLineExtension = value;
            }
        }

        /// <summary>
        /// 获取或设置尺寸界线的起点偏移
        /// </summary>
        [Category("尺寸界线")]
        [DisplayName("起点偏移")]
        [Description("尺寸界线起点到测量点的偏移距离")]
        public double ExtensionLineOffset
        {
            get { return _extensionLineOffset; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("起点偏移不能为负数");
                _extensionLineOffset = value;
            }
        }

        /// <summary>
        /// 获取或设置文本与尺寸线的间隙
        /// </summary>
        [Category("文本")]
        [DisplayName("文本间隙")]
        [Description("文本与尺寸线之间的间隙")]
        public double DimensionLineGap
        {
            get { return _dimensionLineGap; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("文本间隙不能为负数");
                _dimensionLineGap = value;
            }
        }

        /// <summary>
        /// 获取或设置文本精度（小数位数）
        /// </summary>
        [Category("文本")]
        [DisplayName("精度")]
        [Description("标注文本的小数位数")]
        public int TextPrecision
        {
            get { return _textPrecision; }
            set
            {
                if (value < 0 || value > 8)
                    throw new ArgumentException("精度必须在0到8之间");
                _textPrecision = value;
            }
        }

        /// <summary>
        /// 获取或设置前缀
        /// </summary>
        [Category("文本")]
        [DisplayName("前缀")]
        [Description("标注文本的前缀")]
        public string Prefix
        {
            get { return _prefix; }
            set { _prefix = value ?? ""; }
        }

        /// <summary>
        /// 获取或设置后缀
        /// </summary>
        [Category("文本")]
        [DisplayName("后缀")]
        [Description("标注文本的后缀")]
        public string Suffix
        {
            get { return _suffix; }
            set { _suffix = value ?? ""; }
        }

        /// <summary>
        /// 获取或设置文本颜色
        /// </summary>
        [Category("颜色")]
        [DisplayName("文本颜色")]
        [Description("标注文本的颜色")]
        public lcdb.Colors.Color TextColor
        {
            get { return _textColor; }
            set { _textColor = value; }
        }

        /// <summary>
        /// 获取或设置尺寸线颜色
        /// </summary>
        [Category("颜色")]
        [DisplayName("尺寸线颜色")]
        [Description("尺寸线的颜色")]
        public lcdb.Colors.Color DimensionLineColor
        {
            get { return _dimensionLineColor; }
            set { _dimensionLineColor = value; }
        }

        /// <summary>
        /// 获取或设置尺寸界线颜色
        /// </summary>
        [Category("颜色")]
        [DisplayName("尺寸界线颜色")]
        [Description("尺寸界线的颜色")]
        public lcdb.Colors.Color ExtensionLineColor
        {
            get { return _extensionLineColor; }
            set { _extensionLineColor = value; }
        }

        /// <summary>
        /// 获取或设置尺寸线线宽
        /// </summary>
        [Category("线宽")]
        [DisplayName("尺寸线线宽")]
        [Description("尺寸线的线宽")]
        public LineWeight DimensionLineWeight
        {
            get { return _dimensionLineWeight; }
            set { _dimensionLineWeight = value; }
        }

        /// <summary>
        /// 获取或设置尺寸界线线宽
        /// </summary>
        [Category("线宽")]
        [DisplayName("尺寸界线线宽")]
        [Description("尺寸界线的线宽")]
        public LineWeight ExtensionLineWeight
        {
            get { return _extensionLineWeight; }
            set { _extensionLineWeight = value; }
        }

        /// <summary>
        /// 获取或设置是否显示尺寸界线
        /// </summary>
        [Category("显示")]
        [DisplayName("显示尺寸界线")]
        [Description("是否显示尺寸界线")]
        public bool ShowExtensionLines
        {
            get { return _showExtensionLines; }
            set { _showExtensionLines = value; }
        }

        /// <summary>
        /// 获取或设置是否显示尺寸线
        /// </summary>
        [Category("显示")]
        [DisplayName("显示尺寸线")]
        [Description("是否显示尺寸线")]
        public bool ShowDimensionLine
        {
            get { return _showDimensionLine; }
            set { _showDimensionLine = value; }
        }

        /// <summary>
        /// 获取或设置文本对齐方式
        /// </summary>
        [Category("文本")]
        [DisplayName("文本对齐")]
        [Description("标注文本的对齐方式")]
        public DimensionTextAlignment TextAlignment
        {
            get { return _textAlignment; }
            set { _textAlignment = value; }
        }

        /// <summary>
        /// 获取或设置文本位置
        /// </summary>
        [Category("文本")]
        [DisplayName("文本位置")]
        [Description("标注文本相对于尺寸线的位置")]
        public DimensionTextPosition TextPosition
        {
            get { return _textPosition; }
            set { _textPosition = value; }
        }

        /// <summary>
        /// 获取或设置箭头类型
        /// </summary>
        [Category("箭头")]
        [DisplayName("箭头类型")]
        [Description("标注箭头的类型")]
        public ArrowheadType ArrowheadType
        {
            get { return _arrowheadType; }
            set { _arrowheadType = value; }
        }

        /// <summary>
        /// 获取或设置文本偏移
        /// </summary>
        [Category("文本")]
        [DisplayName("文本偏移")]
        [Description("文本相对于默认位置的偏移距离")]
        public double TextOffset
        {
            get { return _textOffset; }
            set { _textOffset = value; }
        }

        /// <summary>
        /// 获取或设置比例因子
        /// </summary>
        [Category("基本属性")]
        [DisplayName("比例因子")]
        [Description("标注的整体比例因子")]
        public double ScaleFactor
        {
            get { return _scaleFactor; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("比例因子必须大于0");
                _scaleFactor = value;
            }
        }

        /// <summary>
        /// 获取或设置是否消除前导零
        /// </summary>
        [Category("文本")]
        [DisplayName("消除前导零")]
        [Description("是否消除小数前导零")]
        public bool SuppressLeadingZeros
        {
            get { return _suppressLeadingZeros; }
            set { _suppressLeadingZeros = value; }
        }

        /// <summary>
        /// 获取或设置是否消除尾随零
        /// </summary>
        [Category("文本")]
        [DisplayName("消除尾随零")]
        [Description("是否消除小数尾随零")]
        public bool SuppressTrailingZeros
        {
            get { return _suppressTrailingZeros; }
            set { _suppressTrailingZeros = value; }
        }

        /// <summary>
        /// 获取或设置替代单位前缀
        /// </summary>
        [Category("替代单位")]
        [DisplayName("前缀")]
        [Description("替代单位的前缀")]
        public string AlternateUnitPrefix
        {
            get { return _alternateUnitPrefix; }
            set { _alternateUnitPrefix = value ?? ""; }
        }

        /// <summary>
        /// 获取或设置替代单位后缀
        /// </summary>
        [Category("替代单位")]
        [DisplayName("后缀")]
        [Description("替代单位的后缀")]
        public string AlternateUnitSuffix
        {
            get { return _alternateUnitSuffix; }
            set { _alternateUnitSuffix = value ?? ""; }
        }

        /// <summary>
        /// 获取或设置替代单位比例因子
        /// </summary>
        [Category("替代单位")]
        [DisplayName("比例因子")]
        [Description("替代单位的比例因子")]
        public double AlternateUnitScaleFactor
        {
            get { return _alternateUnitScaleFactor; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("替代单位比例因子必须大于0");
                _alternateUnitScaleFactor = value;
            }
        }

        /// <summary>
        /// 获取或设置替代单位精度
        /// </summary>
        [Category("替代单位")]
        [DisplayName("精度")]
        [Description("替代单位的小数位数")]
        public int AlternateUnitPrecision
        {
            get { return _alternateUnitPrecision; }
            set
            {
                if (value < 0 || value > 8)
                    throw new ArgumentException("替代单位精度必须在0到8之间");
                _alternateUnitPrecision = value;
            }
        }

        /// <summary>
        /// 获取或设置是否显示替代单位
        /// </summary>
        [Category("替代单位")]
        [DisplayName("显示替代单位")]
        [Description("是否显示替代单位")]
        public bool ShowAlternateUnits
        {
            get { return _showAlternateUnits; }
            set { _showAlternateUnits = value; }
        }

        #endregion

        #region 方法

        /// <summary>
        /// 格式化标注值
        /// </summary>
        public string FormatValue(double value)
        {
            value *= _scaleFactor;
            
            string format = _suppressTrailingZeros ? "G" : "F" + _textPrecision;
            string result = value.ToString(format);
            
            if (_suppressLeadingZeros && result.StartsWith("0."))
            {
                result = result.Substring(1);
            }
            
            result = _prefix + result + _suffix;
            
            if (_showAlternateUnits)
            {
                double altValue = value * _alternateUnitScaleFactor;
                string altFormat = _suppressTrailingZeros ? "G" : "F" + _alternateUnitPrecision;
                string altResult = altValue.ToString(altFormat);
                
                if (_suppressLeadingZeros && altResult.StartsWith("0."))
                {
                    altResult = altResult.Substring(1);
                }
                
                result += " [" + _alternateUnitPrefix + altResult + _alternateUnitSuffix + "]";
            }
            
            return result;
        }

        /// <summary>
        /// 应用光学标准样式
        /// </summary>
        public void ApplyOpticalStandard()
        {
            _textHeight = 2.5;
            _arrowSize = 2.0;
            _extensionLineExtension = 1.0;
            _extensionLineOffset = 0.5;
            _dimensionLineGap = 0.5;
            _textPrecision = 3;
            _suffix = " mm";
            _textAlignment = DimensionTextAlignment.Centered;
            _textPosition = DimensionTextPosition.Above;
            _arrowheadType = ArrowheadType.ClosedFilled;
            _scaleFactor = 1.0;
            _suppressTrailingZeros = true;
        }

        /// <summary>
        /// 应用ISO标准样式
        /// </summary>
        public void ApplyISOStandard()
        {
            _textHeight = 3.5;
            _arrowSize = 3.0;
            _extensionLineExtension = 1.25;
            _extensionLineOffset = 0.625;
            _dimensionLineGap = 0.625;
            _textPrecision = 2;
            _textAlignment = DimensionTextAlignment.Centered;
            _textPosition = DimensionTextPosition.Above;
            _arrowheadType = ArrowheadType.ClosedFilled;
            _scaleFactor = 1.0;
        }

        /// <summary>
        /// 应用GB标准样式
        /// </summary>
        public void ApplyGBStandard()
        {
            _textHeight = 3.5;
            _arrowSize = 2.5;
            _extensionLineExtension = 2.0;
            _extensionLineOffset = 0.0;
            _dimensionLineGap = 1.0;
            _textPrecision = 2;
            _textAlignment = DimensionTextAlignment.Centered;
            _textPosition = DimensionTextPosition.Above;
            _arrowheadType = ArrowheadType.ClosedFilled;
            _scaleFactor = 1.0;
        }

        /// <summary>
        /// 克隆当前标注样式
        /// </summary>
        public object Clone()
        {
            return new DimensionStyle
            {
                _name = _name + "_Copy",
                _textHeight = _textHeight,
                _arrowSize = _arrowSize,
                _extensionLineExtension = _extensionLineExtension,
                _extensionLineOffset = _extensionLineOffset,
                _dimensionLineGap = _dimensionLineGap,
                _textPrecision = _textPrecision,
                _prefix = _prefix,
                _suffix = _suffix,
                _textColor = _textColor,
                _dimensionLineColor = _dimensionLineColor,
                _extensionLineColor = _extensionLineColor,
                _dimensionLineWeight = _dimensionLineWeight,
                _extensionLineWeight = _extensionLineWeight,
                _showExtensionLines = _showExtensionLines,
                _showDimensionLine = _showDimensionLine,
                _textAlignment = _textAlignment,
                _textPosition = _textPosition,
                _arrowheadType = _arrowheadType,
                _textOffset = _textOffset,
                _scaleFactor = _scaleFactor,
                _suppressLeadingZeros = _suppressLeadingZeros,
                _suppressTrailingZeros = _suppressTrailingZeros,
                _alternateUnitPrefix = _alternateUnitPrefix,
                _alternateUnitSuffix = _alternateUnitSuffix,
                _alternateUnitScaleFactor = _alternateUnitScaleFactor,
                _alternateUnitPrecision = _alternateUnitPrecision,
                _showAlternateUnits = _showAlternateUnits
            };
        }

        /// <summary>
        /// 从XML加载样式
        /// </summary>
        public void LoadFromXml(XmlElement element)
        {
            if (element.HasAttribute("Name"))
                _name = element.GetAttribute("Name");
                
            if (element.HasAttribute("TextHeight"))
                _textHeight = double.Parse(element.GetAttribute("TextHeight"));
                
            if (element.HasAttribute("ArrowSize"))
                _arrowSize = double.Parse(element.GetAttribute("ArrowSize"));
                
            if (element.HasAttribute("ExtensionLineExtension"))
                _extensionLineExtension = double.Parse(element.GetAttribute("ExtensionLineExtension"));
                
            if (element.HasAttribute("ExtensionLineOffset"))
                _extensionLineOffset = double.Parse(element.GetAttribute("ExtensionLineOffset"));
                
            if (element.HasAttribute("DimensionLineGap"))
                _dimensionLineGap = double.Parse(element.GetAttribute("DimensionLineGap"));
                
            if (element.HasAttribute("TextPrecision"))
                _textPrecision = int.Parse(element.GetAttribute("TextPrecision"));
                
            if (element.HasAttribute("Prefix"))
                _prefix = element.GetAttribute("Prefix");
                
            if (element.HasAttribute("Suffix"))
                _suffix = element.GetAttribute("Suffix");
                
            if (element.HasAttribute("ScaleFactor"))
                _scaleFactor = double.Parse(element.GetAttribute("ScaleFactor"));
                
            if (element.HasAttribute("SuppressLeadingZeros"))
                _suppressLeadingZeros = bool.Parse(element.GetAttribute("SuppressLeadingZeros"));
                
            if (element.HasAttribute("SuppressTrailingZeros"))
                _suppressTrailingZeros = bool.Parse(element.GetAttribute("SuppressTrailingZeros"));
                
            if (element.HasAttribute("ShowAlternateUnits"))
                _showAlternateUnits = bool.Parse(element.GetAttribute("ShowAlternateUnits"));
                
            if (element.HasAttribute("TextAlignment"))
                _textAlignment = (DimensionTextAlignment)Enum.Parse(typeof(DimensionTextAlignment), element.GetAttribute("TextAlignment"));
                
            if (element.HasAttribute("TextPosition"))
                _textPosition = (DimensionTextPosition)Enum.Parse(typeof(DimensionTextPosition), element.GetAttribute("TextPosition"));
                
            if (element.HasAttribute("ArrowheadType"))
                _arrowheadType = (ArrowheadType)Enum.Parse(typeof(ArrowheadType), element.GetAttribute("ArrowheadType"));
        }

        /// <summary>
        /// 保存到XML
        /// </summary>
        public void SaveToXml(XmlElement element)
        {
            element.SetAttribute("Name", _name);
            element.SetAttribute("TextHeight", _textHeight.ToString());
            element.SetAttribute("ArrowSize", _arrowSize.ToString());
            element.SetAttribute("ExtensionLineExtension", _extensionLineExtension.ToString());
            element.SetAttribute("ExtensionLineOffset", _extensionLineOffset.ToString());
            element.SetAttribute("DimensionLineGap", _dimensionLineGap.ToString());
            element.SetAttribute("TextPrecision", _textPrecision.ToString());
            element.SetAttribute("Prefix", _prefix);
            element.SetAttribute("Suffix", _suffix);
            element.SetAttribute("ScaleFactor", _scaleFactor.ToString());
            element.SetAttribute("SuppressLeadingZeros", _suppressLeadingZeros.ToString());
            element.SetAttribute("SuppressTrailingZeros", _suppressTrailingZeros.ToString());
            element.SetAttribute("ShowAlternateUnits", _showAlternateUnits.ToString());
            element.SetAttribute("TextAlignment", _textAlignment.ToString());
            element.SetAttribute("TextPosition", _textPosition.ToString());
            element.SetAttribute("ArrowheadType", _arrowheadType.ToString());
        }

        #endregion
    }

    /// <summary>
    /// 标注文本对齐方式
    /// </summary>
    public enum DimensionTextAlignment
    {
        /// <summary>
        /// 居中对齐
        /// </summary>
        Centered,
        
        /// <summary>
        /// 第一尺寸界线上方
        /// </summary>
        AboveFirstExtensionLine,
        
        /// <summary>
        /// 第二尺寸界线上方
        /// </summary>
        AboveSecondExtensionLine,
        
        /// <summary>
        /// 第一尺寸界线外侧
        /// </summary>
        OutsideFirstExtensionLine,
        
        /// <summary>
        /// 第二尺寸界线外侧
        /// </summary>
        OutsideSecondExtensionLine
    }

    /// <summary>
    /// 标注文本位置
    /// </summary>
    public enum DimensionTextPosition
    {
        /// <summary>
        /// 尺寸线上方
        /// </summary>
        Above,
        
        /// <summary>
        /// 尺寸线中间
        /// </summary>
        Centered,
        
        /// <summary>
        /// 尺寸线下方
        /// </summary>
        Below,
        
        /// <summary>
        /// 引线
        /// </summary>
        WithLeader
    }

    /// <summary>
    /// 箭头类型
    /// </summary>
    public enum ArrowheadType
    {
        /// <summary>
        /// 闭合填充
        /// </summary>
        ClosedFilled,
        
        /// <summary>
        /// 闭合空心
        /// </summary>
        ClosedBlank,
        
        /// <summary>
        /// 开放
        /// </summary>
        Open,
        
        /// <summary>
        /// 开放30度
        /// </summary>
        Open30,
        
        /// <summary>
        /// 开放90度
        /// </summary>
        Open90,
        
        /// <summary>
        /// 点
        /// </summary>
        Dot,
        
        /// <summary>
        /// 斜线
        /// </summary>
        Oblique,
        
        /// <summary>
        /// 无
        /// </summary>
        None,
        
        /// <summary>
        /// 自定义块
        /// </summary>
        UserBlock
    }
}