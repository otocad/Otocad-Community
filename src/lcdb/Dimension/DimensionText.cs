using System;
using System.Collections.Generic;
using System.Drawing;
#if WINDOWS
using System.Drawing.Drawing2D;
#endif
using lcdb;
using LitMath;

namespace lcdb.Dimension
{
    /// <summary>
    /// 标注文本类，处理标注文本的格式化、定位和绘制
    /// </summary>
    public class DimensionText
    {
        #region 字段

        private string _text;
        private Vector2 _position;
        private double _rotation;
        private double _height;
        private TextStyle _textStyle;
        private Color _color;
        private DimensionTextAlignment _alignment;
        private DimensionTextPosition _verticalPosition;
        private bool _isOverridden;
        private string _overrideText;
        private Rectangle2 _boundingBox;
        private bool _hasFrame;
        private double _frameGap;
        private List<TextFragment> _fragments;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化标注文本的新实例
        /// </summary>
        public DimensionText()
        {
            _text = "";
            _position = Vector2.Zero;
            _rotation = 0;
            _height = 2.5;
            _color = Color.Black;
            _alignment = DimensionTextAlignment.Centered;
            _verticalPosition = DimensionTextPosition.Above;
            _isOverridden = false;
            _overrideText = "";
            _hasFrame = false;
            _frameGap = 0.5;
            _fragments = new List<TextFragment>();
        }

        /// <summary>
        /// 使用指定文本初始化标注文本的新实例
        /// </summary>
        public DimensionText(string text) : this()
        {
            _text = text;
            ParseFragments();
        }

        #endregion

        #region 属性

        /// <summary>
        /// 获取或设置文本内容
        /// </summary>
        public string Text
        {
            get { return _isOverridden ? _overrideText : _text; }
            set
            {
                _text = value ?? "";
                if (!_isOverridden)
                {
                    ParseFragments();
                }
            }
        }

        /// <summary>
        /// 获取或设置位置
        /// </summary>
        public Vector2 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// 获取或设置旋转角度（弧度）
        /// </summary>
        public double Rotation
        {
            get { return _rotation; }
            set { _rotation = value; }
        }

        /// <summary>
        /// 获取或设置文本高度
        /// </summary>
        public double Height
        {
            get { return _height; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("文本高度必须大于0");
                _height = value;
            }
        }

        /// <summary>
        /// 获取或设置文本样式
        /// </summary>
        public TextStyle TextStyle
        {
            get { return _textStyle; }
            set { _textStyle = value; }
        }

        /// <summary>
        /// 获取或设置颜色
        /// </summary>
        public Color Color
        {
            get { return _color; }
            set { _color = value; }
        }

        /// <summary>
        /// 获取或设置对齐方式
        /// </summary>
        public DimensionTextAlignment Alignment
        {
            get { return _alignment; }
            set { _alignment = value; }
        }

        /// <summary>
        /// 获取或设置垂直位置
        /// </summary>
        public DimensionTextPosition VerticalPosition
        {
            get { return _verticalPosition; }
            set { _verticalPosition = value; }
        }

        /// <summary>
        /// 获取或设置是否被覆盖
        /// </summary>
        public bool IsOverridden
        {
            get { return _isOverridden; }
            set
            {
                _isOverridden = value;
                if (value && !string.IsNullOrEmpty(_overrideText))
                {
                    ParseFragments(_overrideText);
                }
                else
                {
                    ParseFragments();
                }
            }
        }

        /// <summary>
        /// 获取或设置覆盖文本
        /// </summary>
        public string OverrideText
        {
            get { return _overrideText; }
            set
            {
                _overrideText = value ?? "";
                if (_isOverridden)
                {
                    ParseFragments(_overrideText);
                }
            }
        }

        /// <summary>
        /// 获取边界框
        /// </summary>
        public Rectangle2 BoundingBox
        {
            get { return _boundingBox; }
        }

        /// <summary>
        /// 获取或设置是否有边框
        /// </summary>
        public bool HasFrame
        {
            get { return _hasFrame; }
            set { _hasFrame = value; }
        }

        /// <summary>
        /// 获取或设置边框间隙
        /// </summary>
        public double FrameGap
        {
            get { return _frameGap; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("边框间隙不能为负数");
                _frameGap = value;
            }
        }

        #endregion

        #region 方法

        /// <summary>
        /// 解析文本片段
        /// </summary>
        private void ParseFragments(string text = null)
        {
            _fragments.Clear();
            string parseText = text ?? _text;
            
            // 简单实现：将整个文本作为一个片段
            // 实际应用中可以解析格式代码，如<>表示测量值，[]表示替代单位等
            
            if (parseText.Contains("<>"))
            {
                // 包含测量值占位符
                string[] parts = parseText.Split(new[] { "<>" }, StringSplitOptions.None);
                
                for (int i = 0; i < parts.Length; i++)
                {
                    if (!string.IsNullOrEmpty(parts[i]))
                    {
                        _fragments.Add(new TextFragment
                        {
                            Text = parts[i],
                            Type = TextFragmentType.Normal
                        });
                    }
                    
                    if (i < parts.Length - 1)
                    {
                        _fragments.Add(new TextFragment
                        {
                            Text = "<>",
                            Type = TextFragmentType.MeasuredValue
                        });
                    }
                }
            }
            else
            {
                // 普通文本
                _fragments.Add(new TextFragment
                {
                    Text = parseText,
                    Type = TextFragmentType.Normal
                });
            }
        }

        /// <summary>
        /// 格式化文本
        /// </summary>
        public string FormatText(double measuredValue, DimensionStyle style)
        {
            string result = "";
            
            foreach (var fragment in _fragments)
            {
                switch (fragment.Type)
                {
                    case TextFragmentType.Normal:
                        result += fragment.Text;
                        break;
                        
                    case TextFragmentType.MeasuredValue:
                        result += style.FormatValue(measuredValue);
                        break;
                        
                    case TextFragmentType.Tolerance:
                        // 处理公差
                        result += fragment.Text;
                        break;
                        
                    case TextFragmentType.Symbol:
                        // 处理特殊符号
                        result += ProcessSymbol(fragment.Text);
                        break;
                }
            }
            
            return result;
        }

        /// <summary>
        /// 处理特殊符号
        /// </summary>
        private string ProcessSymbol(string symbol)
        {
            // 处理常见的标注符号
            switch (symbol.ToUpper())
            {
                case "%%D": return "°";  // 度符号
                case "%%P": return "±";  // 正负号
                case "%%C": return "Ø";  // 直径符号
                case "%%R": return "R";  // 半径符号
                case "%%U": return "_";  // 下划线开始/结束
                case "%%O": return "‾";  // 上划线开始/结束
                default: return symbol;
            }
        }

#if WINDOWS
        /// <summary>
        /// 计算文本边界框 (WinForms GDI+ 文本测量; 跨平台未使用)
        /// </summary>
        public void CalculateBoundingBox(Graphics g)
        {
            if (string.IsNullOrEmpty(Text))
            {
                _boundingBox = new Rectangle2(_position, _position);
                return;
            }
            
            // 使用Graphics测量文本
            Font font = GetFont();
            SizeF size = g.MeasureString(Text, font);
            
            double width = size.Width;
            double height = size.Height;
            
            // 根据对齐方式调整位置
            Vector2 basePoint = _position;
            
            switch (_alignment)
            {
                case DimensionTextAlignment.Centered:
                    basePoint = _position - new Vector2(width / 2, 0);
                    break;
                    
                case DimensionTextAlignment.AboveFirstExtensionLine:
                case DimensionTextAlignment.OutsideFirstExtensionLine:
                    // 左对齐
                    break;
                    
                case DimensionTextAlignment.AboveSecondExtensionLine:
                case DimensionTextAlignment.OutsideSecondExtensionLine:
                    basePoint = _position - new Vector2(width, 0);
                    break;
            }
            
            // 根据垂直位置调整
            switch (_verticalPosition)
            {
                case DimensionTextPosition.Above:
                    basePoint = basePoint - new Vector2(0, height);
                    break;
                    
                case DimensionTextPosition.Centered:
                    basePoint = basePoint - new Vector2(0, height / 2);
                    break;
                    
                case DimensionTextPosition.Below:
                    // 基线在下方
                    break;
            }
            
            // 考虑旋转
            if (Math.Abs(_rotation) > 0.001)
            {
                // 计算旋转后的四个角点
                Vector2[] corners = new Vector2[]
                {
                    basePoint,
                    basePoint + new Vector2(width, 0),
                    basePoint + new Vector2(width, height),
                    basePoint + new Vector2(0, height)
                };
                
                // 旋转变换
                Matrix3 rotMatrix = Matrix3.RotateInRadian(_rotation);
                for (int i = 0; i < corners.Length; i++)
                {
                    Vector2 offset = corners[i] - _position;
                    corners[i] = _position + (rotMatrix * offset);
                }
                
                // 计算旋转后的边界
                double minX = corners[0].X;
                double minY = corners[0].Y;
                double maxX = corners[0].X;
                double maxY = corners[0].Y;
                
                for (int i = 1; i < corners.Length; i++)
                {
                    minX = Math.Min(minX, corners[i].X);
                    minY = Math.Min(minY, corners[i].Y);
                    maxX = Math.Max(maxX, corners[i].X);
                    maxY = Math.Max(maxY, corners[i].Y);
                }
                
                _boundingBox = new Rectangle2(new Vector2(minX, minY), new Vector2(maxX, maxY));
            }
            else
            {
                _boundingBox = new Rectangle2(basePoint, basePoint + new Vector2(width, height));
            }
            
            // 如果有边框，扩大边界框
            if (_hasFrame)
            {
                _boundingBox = new Rectangle2(
                    _boundingBox.location - new Vector2(_frameGap, _frameGap),
                    _boundingBox.rightTop + new Vector2(_frameGap, _frameGap)
                );
            }
        }

        /// <summary>
        /// 获取字体
        /// </summary>
        private Font GetFont()
        {
            if (_textStyle != null && !string.IsNullOrEmpty(_textStyle.FontFamilyName))
            {
                System.Drawing.FontStyle fontStyle = System.Drawing.FontStyle.Regular;
                if (_textStyle.FontStyle == lcdb.FontStyle.Bold || 
                    _textStyle.FontStyle == lcdb.FontStyle.BoldItalic)
                {
                    fontStyle |= System.Drawing.FontStyle.Bold;
                }
                if (_textStyle.FontStyle == lcdb.FontStyle.Italic || 
                    _textStyle.FontStyle == lcdb.FontStyle.BoldItalic)
                {
                    fontStyle |= System.Drawing.FontStyle.Italic;
                }
                
                return new Font(_textStyle.FontFamilyName, (float)_height, fontStyle);
            }
            else
            {
                return new Font("Arial", (float)_height);
            }
        }
#endif

        /// <summary>
        /// 移动文本
        /// </summary>
        public void Move(Vector2 offset)
        {
            _position += offset;
            _boundingBox = new Rectangle2(
                _boundingBox.location + offset,
                _boundingBox.rightTop + offset
            );
        }

        /// <summary>
        /// 旋转文本
        /// </summary>
        public void Rotate(double angle, Vector2 center)
        {
            Matrix3 rotMatrix = Matrix3.RotateInRadian(angle);
            Vector2 offset = _position - center;
            _position = center + (rotMatrix * offset);
            _rotation += angle;
        }

        /// <summary>
        /// 检查点是否在文本内
        /// </summary>
        public bool Contains(Vector2 point)
        {
            return point.X >= _boundingBox.location.X &&
                   point.X <= _boundingBox.location.X + _boundingBox.width &&
                   point.Y >= _boundingBox.location.Y &&
                   point.Y <= _boundingBox.location.Y + _boundingBox.height;
        }

        /// <summary>
        /// 创建用于光学标注的格式化文本
        /// </summary>
        public static string CreateOpticalDimensionText(double value, string prefix = "", string suffix = " mm", int precision = 3)
        {
            string format = "F" + precision;
            string valueStr = value.ToString(format);
            
            // 去除尾随零
            if (valueStr.Contains("."))
            {
                valueStr = valueStr.TrimEnd('0').TrimEnd('.');
            }
            
            return prefix + valueStr + suffix;
        }

        /// <summary>
        /// 创建公差文本
        /// </summary>
        public static string CreateToleranceText(double nominal, double upperTolerance, double lowerTolerance, int precision = 3)
        {
            string format = "F" + precision;
            string nominalStr = nominal.ToString(format).TrimEnd('0').TrimEnd('.');
            
            if (Math.Abs(upperTolerance) < 0.0001 && Math.Abs(lowerTolerance) < 0.0001)
            {
                return nominalStr;
            }
            else if (Math.Abs(upperTolerance - lowerTolerance) < 0.0001)
            {
                // 对称公差
                string tolStr = upperTolerance.ToString(format).TrimEnd('0').TrimEnd('.');
                return nominalStr + "±" + tolStr;
            }
            else
            {
                // 非对称公差
                string upperStr = (upperTolerance >= 0 ? "+" : "") + upperTolerance.ToString(format).TrimEnd('0').TrimEnd('.');
                string lowerStr = (lowerTolerance >= 0 ? "+" : "") + lowerTolerance.ToString(format).TrimEnd('0').TrimEnd('.');
                return nominalStr + "^" + upperStr + "_" + lowerStr;
            }
        }

        #endregion

        #region 内部类

        /// <summary>
        /// 文本片段
        /// </summary>
        private class TextFragment
        {
            public string Text { get; set; }
            public TextFragmentType Type { get; set; }
        }

        /// <summary>
        /// 文本片段类型
        /// </summary>
        private enum TextFragmentType
        {
            Normal,
            MeasuredValue,
            Tolerance,
            Symbol
        }

        #endregion
    }
}