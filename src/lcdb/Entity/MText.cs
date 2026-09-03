using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LitMath;
using lcdb;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 多行文本实体
    /// </summary>
    /// <remarks>
    /// 支持多行文本显示，包括格式化代码�?
    /// \L 开始下划线, \l 停止下划�?
    /// \O 开始上划线, \o 停止上划�? 
    /// \K 开始删除线, \k 停止删除�?
    /// \P 新段落（换行�?
    /// \Q 倾斜文本角度 - �?\Q30;
    /// \H 文本高度 - �?\H3x;
    /// \W 文本宽度 - �?\W0.8x;
    /// \F 字体选择
    /// \S 堆叠、分�?
    /// \A 对齐方式
    /// \C 颜色更改
    /// \T 字符间距
    /// \~ 不换行空�?
    /// {} 大括�?- 定义受代码影响的文本区域
    /// \ 转义字符
    /// </remarks>
    public class MText : Entity
    {
        #region 私有字段

        private Vector2 _position = Vector2.Zero;
        private double _rectangleWidth = 0.0;
        private double _height = 1.0;
        private double _rotation = 0.0;
        private double _lineSpacing = 1.0;
        private MTextLineSpacingStyle _lineSpacingStyle = MTextLineSpacingStyle.AtLeast;
        private MTextDrawingDirection _drawingDirection = MTextDrawingDirection.ByStyle;
        private MTextAttachmentPoint _attachmentPoint = MTextAttachmentPoint.TopLeft;
        private string _text = string.Empty;

        #endregion

        #region 属�?

        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "MText";

        /// <summary>
        /// 文本位置
        /// </summary>
        public Vector2 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// 文本内容
        /// </summary>
        public string Value
        {
            get { return _text; }
            set { _text = value ?? string.Empty; }
        }

        /// <summary>
        /// 文本高度
        /// </summary>
        public double Height
        {
            get { return _height; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "MText高度必须大于0");
                }
                _height = value;
            }
        }

        /// <summary>
        /// 文本旋转角度（弧度）
        /// </summary>
        public double Rotation
        {
            get { return _rotation; }
            set { _rotation = value; }
        }

        /// <summary>
        /// 引用矩形宽度
        /// </summary>
        /// <remarks>
        /// 此值定义文本适合的框的宽度�?
        /// 如果段落宽度超过矩形宽度，将在几行中分割，使用单词空间作为分割点�?
        /// 如果指定宽度�?，则关闭自动换行，多行文本对象的宽度与最长行文本一样宽�?
        /// </remarks>
        public double RectangleWidth
        {
            get { return _rectangleWidth; }
            set
            {
                if (value < 0.0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "MText矩形宽度必须大于等于0");
                }
                _rectangleWidth = value;
            }
        }

        /// <summary>
        /// 行间距系�?
        /// </summary>
        /// <remarks>
        /// 要应用的默认行间距的百分比。有效值范围从0.25�?.0，默认�?.0�?
        /// </remarks>
        public double LineSpacingFactor
        {
            get { return _lineSpacing; }
            set
            {
                if (value < 0.25 || value > 4.0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "MText行间距系数的有效值范围是0.25-4.0");
                }
                _lineSpacing = value;
            }
        }

        /// <summary>
        /// 行间距样�?
        /// </summary>
        public MTextLineSpacingStyle LineSpacingStyle
        {
            get { return _lineSpacingStyle; }
            set
            {
                if (value == MTextLineSpacingStyle.Default || value == MTextLineSpacingStyle.Multiple)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Default和Multiple选项仅适用于段落选项对象");
                }
                _lineSpacingStyle = value;
            }
        }

        /// <summary>
        /// 文本绘制方向
        /// </summary>
        public MTextDrawingDirection DrawingDirection
        {
            get { return _drawingDirection; }
            set { _drawingDirection = value; }
        }

        /// <summary>
        /// 文本附着�?
        /// </summary>
        public MTextAttachmentPoint AttachmentPoint
        {
            get { return _attachmentPoint; }
            set { _attachmentPoint = value; }
        }

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                var textLines = GetPlainTextLines();
                if (textLines.Count == 0)
                {
                    return new Bounding(_position, _height, _height);
                }

                double totalHeight = textLines.Count * _height * _lineSpacing;
                double maxWidth = _rectangleWidth > 0 ? _rectangleWidth : 
                    textLines.Max(line => EstimateTextWidth(line, _height));

                // 根据附着点调整边界框位置
                var adjustedPosition = GetAdjustedPositionForBounding(maxWidth, totalHeight);
                
                return new Bounding(adjustedPosition, maxWidth, totalHeight);
            }
        }

        #endregion

        #region 构造函�?

        /// <summary>
        /// 默认构造函�?
        /// </summary>
        public MText()
        {
            _position = Vector2.Zero;
            _text = string.Empty;
            _height = 1.0;
            _rectangleWidth = 0.0;
            _rotation = 0.0;
            _lineSpacing = 1.0;
            _lineSpacingStyle = MTextLineSpacingStyle.AtLeast;
            _drawingDirection = MTextDrawingDirection.ByStyle;
            _attachmentPoint = MTextAttachmentPoint.TopLeft;
        }

        /// <summary>
        /// 构造函�?
        /// </summary>
        /// <param name="text">文本内容</param>
        public MText(string text) : this()
        {
            _text = text ?? string.Empty;
        }

        /// <summary>
        /// 构造函�?
        /// </summary>
        /// <param name="position">文本位置</param>
        /// <param name="height">文本高度</param>
        public MText(Vector2 position, double height) : this()
        {
            _position = position;
            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "MText高度必须大于0");
            }
            _height = height;
        }

        /// <summary>
        /// 构造函�?
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <param name="position">文本位置</param>
        /// <param name="height">文本高度</param>
        public MText(string text, Vector2 position, double height) : this(position, height)
        {
            _text = text ?? string.Empty;
        }

        /// <summary>
        /// 构造函�?
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <param name="position">文本位置</param>
        /// <param name="height">文本高度</param>
        /// <param name="rectangleWidth">矩形宽度</param>
        public MText(string text, Vector2 position, double height, double rectangleWidth) 
            : this(text, position, height)
        {
            if (rectangleWidth < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(rectangleWidth), rectangleWidth, "MText矩形宽度必须大于等于0");
            }
            _rectangleWidth = rectangleWidth;
        }

        /// <summary>
        /// 完整构造函�?
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <param name="position">文本位置</param>
        /// <param name="height">文本高度</param>
        /// <param name="rectangleWidth">矩形宽度</param>
        /// <param name="attachmentPoint">附着�?/param>
        public MText(string text, Vector2 position, double height, double rectangleWidth, MTextAttachmentPoint attachmentPoint)
            : this(text, position, height, rectangleWidth)
        {
            _attachmentPoint = attachmentPoint;
        }

        #endregion

        #region 必须实现的方�?

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            if (string.IsNullOrEmpty(_text))
                return;

            var textLines = GetPlainTextLines();
            if (textLines.Count == 0)
                return;

            // 计算每行的实际位�?
            var linePositions = CalculateLinePositions(textLines);

            // 仅当自身显式指定 RGB 时覆盖颜色, 否则沿用外部默认 (ByLayer 不在此解析)
            var prevColor = gd.CurrentColor;
            bool overrideColor = color.colorMethod == Colors.ColorMethod.ByColor
                || color.colorMethod == Colors.ColorMethod.ByEntity;
            if (overrideColor) gd.CurrentColor = color.ToDrawingColor();
            try
            {
                // 绘制每一�?
                for (int i = 0; i < textLines.Count && i < linePositions.Count; i++)
                {
                    var linePosition = linePositions[i];
                    var lineText = textLines[i];

                    if (!string.IsNullOrEmpty(lineText))
                    {
                        var textAlign = ConvertAttachmentPointToTextAlignment(_attachmentPoint);
                        gd.DrawText(linePosition, lineText, _height, "Arial", (lcdb.TextAlignment)textAlign, _rotation);
                    }
                }
            }
            finally
            {
                if (overrideColor) gd.CurrentColor = prevColor;
            }
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new MText();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            MText mtext = base.Clone() as MText;
            mtext._position = _position;
            mtext._text = _text;
            mtext._height = _height;
            mtext._rectangleWidth = _rectangleWidth;
            mtext._rotation = _rotation;
            mtext._lineSpacing = _lineSpacing;
            mtext._lineSpacingStyle = _lineSpacingStyle;
            mtext._drawingDirection = _drawingDirection;
            mtext._attachmentPoint = _attachmentPoint;
            return mtext;
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            _position += translation;
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            _position = Vector2.RotateInRadian(_position, center, angle);
            _rotation += angle;
        }

        /// <summary>
        /// 矩阵变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            _position = transform * _position;
            
            // 提取缩放和旋转信�?
            var scale = transform.GetScale();
            var rotation = transform.GetRotation();
            
            _height *= scale.X; // 假设等比缩放
            _rotation += rotation;
            
            if (_rectangleWidth > 0)
            {
                _rectangleWidth *= scale.X;
            }
        }

        #endregion

        #region 交互功能

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            
            var bounds = bounding;
            
            // 添加四个角的夹点
            gripPoints.Add(new GripPoint(GripPointType.Corner, 
                new Vector2(bounds.center.X - bounds.width/2, bounds.center.Y + bounds.height/2))); // 左上
            gripPoints.Add(new GripPoint(GripPointType.Corner, 
                new Vector2(bounds.center.X + bounds.width/2, bounds.center.Y + bounds.height/2))); // 右上
            gripPoints.Add(new GripPoint(GripPointType.Corner, 
                new Vector2(bounds.center.X + bounds.width/2, bounds.center.Y - bounds.height/2))); // 右下
            gripPoints.Add(new GripPoint(GripPointType.Corner, 
                new Vector2(bounds.center.X - bounds.width/2, bounds.center.Y - bounds.height/2))); // 左下
            
            // 添加文本位置�?
            gripPoints.Add(new GripPoint(GripPointType.Center, _position));
            
            return gripPoints;
        }

        /// <summary>
        /// 对象捕捉�?
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPoints = new List<ObjectSnapPoint>();
            
            var bounds = bounding;
            
            // 添加四个角的捕捉�?
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, 
                new Vector2(bounds.center.X - bounds.width/2, bounds.center.Y + bounds.height/2))); // 左上
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, 
                new Vector2(bounds.center.X + bounds.width/2, bounds.center.Y + bounds.height/2))); // 右上
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, 
                new Vector2(bounds.center.X + bounds.width/2, bounds.center.Y - bounds.height/2))); // 右下
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, 
                new Vector2(bounds.center.X - bounds.width/2, bounds.center.Y - bounds.height/2))); // 左下
            
            // 添加中心�?
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Center, bounds.center));
            
            // 添加插入�?
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Ins, _position));
            
            return snapPoints;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            if (index >= 0 && index <= 3)
            {
                // 调整边界框大小（简化处理：仅移动位置）
                var bounds = bounding;
                var translation = newPosition - gripPoint.position;
                Translate(translation * 0.1); // 减小移动幅度
            }
            else if (index == 4)
            {
                // 移动文本位置
                _position = newPosition;
            }
        }

        #endregion

        #region XML序列�?

        /// <summary>
        /// 写XML
        /// </summary>

        /// <summary>
        /// 读XML
        /// </summary>

        #endregion

        #region 文本处理方法

        /// <summary>
        /// 获取纯文本行（移除格式化代码�?
        /// </summary>
        /// <returns>文本行列�?/returns>
        public List<string> GetPlainTextLines()
        {
            if (string.IsNullOrEmpty(_text))
                return new List<string>();

            // 移除格式化代码的简化实�?
            string plainText = RemoveFormattingCodes(_text);
            
            // 按\P或换行符分割
            var lines = plainText.Split(new string[] { "\\P", "\n", "\r\n" }, StringSplitOptions.None).ToList();
            
            // 如果设置了矩形宽度，进行自动换行
            if (_rectangleWidth > 0)
            {
                lines = ApplyWordWrap(lines, _rectangleWidth);
            }
            
            return lines;
        }

        /// <summary>
        /// 移除格式化代�?
        /// </summary>
        private string RemoveFormattingCodes(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // 移除常见的格式化代码
            string result = text;
            
            // 移除简单的开关代�?
            result = Regex.Replace(result, @"\\[LlOoKk]", "");
            
            // 移除带参数的代码
            result = Regex.Replace(result, @"\\[QHWFACTSP][^;]*;", "");
            
            // 移除堆叠代码
            result = Regex.Replace(result, @"\\S[^;]*;", "");
            
            // 移除大括�?
            result = result.Replace("{", "").Replace("}", "");
            
            // 处理转义字符
            result = result.Replace("\\\\", "\\");
            result = result.Replace("\\{", "{");
            result = result.Replace("\\}", "}");
            
            return result;
        }

        /// <summary>
        /// 应用自动换行
        /// </summary>
        private List<string> ApplyWordWrap(List<string> lines, double maxWidth)
        {
            var wrappedLines = new List<string>();
            
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    wrappedLines.Add(line);
                    continue;
                }
                
                var estimatedWidth = EstimateTextWidth(line, _height);
                if (estimatedWidth <= maxWidth)
                {
                    wrappedLines.Add(line);
                }
                else
                {
                    // 简单的单词换行
                    var words = line.Split(' ');
                    var currentLine = new StringBuilder();
                    
                    foreach (var word in words)
                    {
                        var testLine = currentLine.Length > 0 ? $"{currentLine} {word}" : word;
                        var testWidth = EstimateTextWidth(testLine, _height);
                        
                        if (testWidth <= maxWidth)
                        {
                            currentLine.Append(currentLine.Length > 0 ? $" {word}" : word);
                        }
                        else
                        {
                            if (currentLine.Length > 0)
                            {
                                wrappedLines.Add(currentLine.ToString());
                                currentLine.Clear();
                            }
                            currentLine.Append(word);
                        }
                    }
                    
                    if (currentLine.Length > 0)
                    {
                        wrappedLines.Add(currentLine.ToString());
                    }
                }
            }
            
            return wrappedLines;
        }

        /// <summary>
        /// 估算文本宽度
        /// </summary>
        private double EstimateTextWidth(string text, double height)
        {
            if (string.IsNullOrEmpty(text))
                return 0.0;
                
            // 简化的宽度估算：每个字符约为高度的0.6�?
            return text.Length * height * 0.6;
        }

        /// <summary>
        /// 计算每行文本的位�?
        /// </summary>
        private List<Vector2> CalculateLinePositions(List<string> textLines)
        {
            var positions = new List<Vector2>();
            
            if (textLines.Count == 0)
                return positions;
            
            double lineHeight = _height * _lineSpacing;
            double totalHeight = textLines.Count * lineHeight;
            
            // 根据附着点计算起始位�?
            var startPosition = GetAdjustedPositionForText(totalHeight);
            
            for (int i = 0; i < textLines.Count; i++)
            {
                var linePosition = new Vector2(
                    startPosition.X,
                    startPosition.Y - i * lineHeight
                );
                positions.Add(linePosition);
            }
            
            return positions;
        }

        /// <summary>
        /// 根据附着点获取调整后的文本位�?
        /// </summary>
        private Vector2 GetAdjustedPositionForText(double totalHeight)
        {
            var result = _position;
            
            switch (_attachmentPoint)
            {
                case MTextAttachmentPoint.TopLeft:
                case MTextAttachmentPoint.TopCenter:
                case MTextAttachmentPoint.TopRight:
                    // 顶部对齐，位置不�?
                    break;
                    
                case MTextAttachmentPoint.MiddleLeft:
                case MTextAttachmentPoint.MiddleCenter:
                case MTextAttachmentPoint.MiddleRight:
                    // 中间对齐
                    result.Y += totalHeight / 2;
                    break;
                    
                case MTextAttachmentPoint.BottomLeft:
                case MTextAttachmentPoint.BottomCenter:
                case MTextAttachmentPoint.BottomRight:
                    // 底部对齐
                    result.Y += totalHeight;
                    break;
            }
            
            return result;
        }

        /// <summary>
        /// 根据附着点获取调整后的边界框位置
        /// </summary>
        private Vector2 GetAdjustedPositionForBounding(double width, double height)
        {
            var result = _position;
            
            // 水平调整
            switch (_attachmentPoint)
            {
                case MTextAttachmentPoint.TopLeft:
                case MTextAttachmentPoint.MiddleLeft:
                case MTextAttachmentPoint.BottomLeft:
                    result.X += width / 2;
                    break;
                    
                case MTextAttachmentPoint.TopCenter:
                case MTextAttachmentPoint.MiddleCenter:
                case MTextAttachmentPoint.BottomCenter:
                    // 中心对齐，位置不�?
                    break;
                    
                case MTextAttachmentPoint.TopRight:
                case MTextAttachmentPoint.MiddleRight:
                case MTextAttachmentPoint.BottomRight:
                    result.X -= width / 2;
                    break;
            }
            
            // 垂直调整
            switch (_attachmentPoint)
            {
                case MTextAttachmentPoint.TopLeft:
                case MTextAttachmentPoint.TopCenter:
                case MTextAttachmentPoint.TopRight:
                    result.Y -= height / 2;
                    break;
                    
                case MTextAttachmentPoint.MiddleLeft:
                case MTextAttachmentPoint.MiddleCenter:
                case MTextAttachmentPoint.MiddleRight:
                    // 中间对齐，位置不�?
                    break;
                    
                case MTextAttachmentPoint.BottomLeft:
                case MTextAttachmentPoint.BottomCenter:
                case MTextAttachmentPoint.BottomRight:
                    result.Y += height / 2;
                    break;
            }
            
            return result;
        }

        /// <summary>
        /// 将附着点转换为文本对齐方式
        /// </summary>
        private TextAlignment ConvertAttachmentPointToTextAlignment(MTextAttachmentPoint attachmentPoint)
        {
            switch (attachmentPoint)
            {
                case MTextAttachmentPoint.TopLeft:
                    return TextAlignment.LeftTop;
                case MTextAttachmentPoint.TopCenter:
                    return TextAlignment.CenterTop;
                case MTextAttachmentPoint.TopRight:
                    return TextAlignment.RightTop;
                case MTextAttachmentPoint.MiddleLeft:
                    return TextAlignment.LeftMiddle;
                case MTextAttachmentPoint.MiddleCenter:
                    return TextAlignment.CenterMiddle;
                case MTextAttachmentPoint.MiddleRight:
                    return TextAlignment.RightMiddle;
                case MTextAttachmentPoint.BottomLeft:
                    return TextAlignment.LeftBottom;
                case MTextAttachmentPoint.BottomCenter:
                    return TextAlignment.CenterBottom;
                case MTextAttachmentPoint.BottomRight:
                    return TextAlignment.RightBottom;
                default:
                    return TextAlignment.LeftTop;
            }
        }

        #endregion

        #region 格式化文本方�?

        /// <summary>
        /// 添加文本到当前内�?
        /// </summary>
        /// <param name="text">要添加的文本</param>
        public void Write(string text)
        {
            _text += text;
        }

        /// <summary>
        /// 添加新段�?
        /// </summary>
        public void EndParagraph()
        {
            _text += "\\P";
        }

        /// <summary>
        /// 添加下划线文�?
        /// </summary>
        /// <param name="text">文本内容</param>
        public void WriteUnderline(string text)
        {
            _text += $"\\L{text}\\l";
        }

        /// <summary>
        /// 添加上划线文�?
        /// </summary>
        /// <param name="text">文本内容</param>
        public void WriteOverstrike(string text)
        {
            _text += $"\\O{text}\\o";
        }

        /// <summary>
        /// 添加删除线文�?
        /// </summary>
        /// <param name="text">文本内容</param>
        public void WriteStrikeThrough(string text)
        {
            _text += $"\\K{text}\\k";
        }

        /// <summary>
        /// 设置文本高度
        /// </summary>
        /// <param name="height">高度�?/param>
        public void SetTextHeight(double height)
        {
            _text += $"\\H{height}X;";
        }

        /// <summary>
        /// 设置文本颜色
        /// </summary>
        /// <param name="colorIndex">颜色索引</param>
        public void SetTextColor(int colorIndex)
        {
            _text += $"\\C{colorIndex};";
        }

        #endregion
    }
}