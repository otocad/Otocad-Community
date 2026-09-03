using System;
using System.IO;

namespace lcdb
{
    /// <summary>
    /// 文本样式类
    /// </summary>
    public class TextStyle : DBTableRecord
    {
        #region Private Fields
        
        private string _fontFile = "arial.ttf";
        private string _bigFont = "";
        private double _height = 0.0;
        private double _widthFactor = 1.0;
        private double _obliqueAngle = 0.0;
        private bool _isVertical = false;
        private bool _isBackward = false;
        private bool _isUpsideDown = false;
        private string _fontFamilyName = "";
        private FontStyle _fontStyle = FontStyle.Regular;
        
        #endregion

        #region Constants
        
        /// <summary>
        /// 默认文本样式名称
        /// </summary>
        public const string DefaultName = "Standard";

        /// <summary>
        /// ISO标准文本样式名称
        /// </summary>
        public const string ISOStandardName = "ISO-Standard";

        /// <summary>
        /// GB国标文本样式名称
        /// </summary>
        public const string GBStandardName = "GB-Standard";

        #endregion

        #region Constructors

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public TextStyle()
            : base()
        {
            _name = DefaultName;
        }

        /// <summary>
        /// 使用字体文件创建文本样式
        /// </summary>
        /// <param name="fontFile">字体文件路径</param>
        public TextStyle(string fontFile)
            : this(Path.GetFileNameWithoutExtension(fontFile), fontFile)
        {
        }

        /// <summary>
        /// 使用名称和字体文件创建文本样式
        /// </summary>
        /// <param name="name">样式名称</param>
        /// <param name="fontFile">字体文件路径</param>
        public TextStyle(string name, string fontFile)
            : base()
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name), "文本样式名称不能为空");
            
            if (string.IsNullOrEmpty(fontFile))
                throw new ArgumentNullException(nameof(fontFile), "字体文件不能为空");

            _name = name;
            _fontFile = fontFile;
            _fontFamilyName = "";
            _fontStyle = FontStyle.Regular;
        }

        /// <summary>
        /// 使用字体族名称和样式创建文本样式（用于TrueType字体）
        /// </summary>
        /// <param name="name">样式名称</param>
        /// <param name="fontFamilyName">字体族名称</param>
        /// <param name="fontStyle">字体样式</param>
        public TextStyle(string name, string fontFamilyName, FontStyle fontStyle)
            : base()
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name), "文本样式名称不能为空");
                
            if (string.IsNullOrEmpty(fontFamilyName))
                throw new ArgumentNullException(nameof(fontFamilyName), "字体族名称不能为空");

            _name = name;
            _fontFile = "";
            _fontFamilyName = fontFamilyName;
            _fontStyle = fontStyle;
        }

        #endregion

        #region Properties

        /// <summary>
        /// 类名
        /// </summary>
        public override string className
        {
            get { return "TextStyle"; }
        }

        /// <summary>
        /// 字体文件路径
        /// </summary>
        public string FontFile
        {
            get { return _fontFile; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException(nameof(value), "字体文件不能为空");

                _fontFile = value;
                _fontFamilyName = "";
                _fontStyle = FontStyle.Regular;
            }
        }

        /// <summary>
        /// 大字体文件（用于亚洲语言）
        /// </summary>
        public string BigFont
        {
            get { return _bigFont; }
            set { _bigFont = value ?? ""; }
        }

        /// <summary>
        /// 字体族名称（TrueType字体）
        /// </summary>
        public string FontFamilyName
        {
            get { return _fontFamilyName; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException(nameof(value), "字体族名称不能为空");

                _fontFamilyName = value;
                _fontFile = "";
                _fontStyle = FontStyle.Regular;
            }
        }

        /// <summary>
        /// 字体样式
        /// </summary>
        public FontStyle FontStyle
        {
            get { return _fontStyle; }
            set 
            { 
                if (string.IsNullOrEmpty(_fontFile))
                    _fontStyle = value;
            }
        }

        /// <summary>
        /// 固定文本高度（0表示不固定）
        /// </summary>
        public double Height
        {
            get { return _height; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "文本高度必须大于等于0");
                _height = value;
            }
        }

        /// <summary>
        /// 宽度因子（0.01-100.0）
        /// </summary>
        public double WidthFactor
        {
            get { return _widthFactor; }
            set
            {
                if (value < 0.01 || value > 100.0)
                    throw new ArgumentOutOfRangeException(nameof(value), "宽度因子必须在0.01到100.0之间");
                _widthFactor = value;
            }
        }

        /// <summary>
        /// 倾斜角度（-85到85度）
        /// </summary>
        public double ObliqueAngle
        {
            get { return _obliqueAngle; }
            set
            {
                if (value < -85.0 || value > 85.0)
                    throw new ArgumentOutOfRangeException(nameof(value), "倾斜角度必须在-85到85度之间");
                _obliqueAngle = value;
            }
        }

        /// <summary>
        /// 是否垂直文字
        /// </summary>
        public bool IsVertical
        {
            get { return _isVertical; }
            set { _isVertical = value; }
        }

        /// <summary>
        /// 是否反向（X轴镜像）
        /// </summary>
        public bool IsBackward
        {
            get { return _isBackward; }
            set { _isBackward = value; }
        }

        /// <summary>
        /// 是否颠倒（Y轴镜像）
        /// </summary>
        public bool IsUpsideDown
        {
            get { return _isUpsideDown; }
            set { _isUpsideDown = value; }
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// 创建默认文本样式
        /// </summary>
        public static TextStyle CreateDefault()
        {
            return new TextStyle(DefaultName, "arial.ttf")
            {
                Height = 0.0,
                WidthFactor = 1.0,
                ObliqueAngle = 0.0
            };
        }

        /// <summary>
        /// 创建ISO标准文本样式
        /// </summary>
        public static TextStyle CreateISOStandard()
        {
            return new TextStyle(ISOStandardName, "Arial", FontStyle.Regular)
            {
                Height = 2.5,
                WidthFactor = 1.0,
                ObliqueAngle = 0.0,
                IsVertical = false
            };
        }

        /// <summary>
        /// 创建GB国标文本样式
        /// </summary>
        public static TextStyle CreateGBStandard()
        {
            return new TextStyle(GBStandardName, "SimSun", FontStyle.Regular)
            {
                Height = 3.5,
                WidthFactor = 0.8,
                ObliqueAngle = 0.0,
                IsVertical = false
            };
        }

        #endregion

        #region Override Methods

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new TextStyle();
        }

        /// <summary>
        /// 克隆
        /// </summary>
        public override object Clone()
        {
            TextStyle cloned = base.Clone() as TextStyle;
            cloned._fontFile = _fontFile;
            cloned._bigFont = _bigFont;
            cloned._height = _height;
            cloned._widthFactor = _widthFactor;
            cloned._obliqueAngle = _obliqueAngle;
            cloned._isVertical = _isVertical;
            cloned._isBackward = _isBackward;
            cloned._isUpsideDown = _isUpsideDown;
            cloned._fontFamilyName = _fontFamilyName;
            cloned._fontStyle = _fontStyle;
            return cloned;
        }

        #endregion
    }

    /// <summary>
    /// 字体样式枚举
    /// </summary>
    public enum FontStyle
    {
        Regular = 0,
        Bold = 1,
        Italic = 2,
        BoldItalic = 3
    }
}