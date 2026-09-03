using System;
using System.Collections.Generic;
using System.ComponentModel;
using LitMath;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 文本实体
    /// </summary>
    [Serializable]
    public class Text : Entity
    {
        #region 成员变量

        private Vector3 _position = new Vector3();
        private string _value = string.Empty;
        private double _height = 1.0;
        private double _widthFactor = 1.0;
        private double _rotation = 0.0;
        private double _obliqueAngle = 0.0;
        private TextAlignment _alignment = TextAlignment.LeftBottom;
        private string _textStyle = "Standard";
        private bool _isMirrored = false;
        private bool _isUpsideDown = false;

        #endregion

        #region 属性

        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "Text";

        /// <summary>
        /// 文本位置
        /// </summary>
        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// 文本内容
        /// </summary>
        public string Value
        {
            get { return _value; }
            set { _value = value ?? string.Empty; }
        }

        /// <summary>
        /// 文本高度
        /// </summary>
        public double Height
        {
            get { return _height; }
            set { _height = Math.Max(0.01, value); }
        }

        /// <summary>
        /// 宽度因子
        /// </summary>
        public double WidthFactor
        {
            get { return _widthFactor; }
            set { _widthFactor = Math.Max(0.01, value); }
        }

        /// <summary>
        /// 旋转角度
        /// </summary>
        public double Rotation
        {
            get { return _rotation; }
            set { _rotation = value; }
        }

        /// <summary>
        /// 角度（小写属性名，为了向后兼容）
        /// </summary>
        [Browsable(false)]
        public double angle
        {
            get { return _rotation; }
            set { _rotation = value; }
        }

        /// <summary>
        /// 倾斜角度
        /// </summary>
        public double ObliqueAngle
        {
            get { return _obliqueAngle; }
            set { _obliqueAngle = value; }
        }

        /// <summary>
        /// 对齐方式
        /// </summary>
        public TextAlignment Alignment
        {
            get { return _alignment; }
            set { _alignment = value; }
        }

        /// <summary>
        /// 对齐方式（小写属性名，为了向后兼容）
        /// </summary>
        [Browsable(false)]
        public TextAlignment alignment
        {
            get { return _alignment; }
            set { _alignment = value; }
        }

        /// <summary>
        /// 文本样式
        /// </summary>
        public string TextStyle
        {
            get { return _textStyle; }
            set { _textStyle = value ?? "Standard"; }
        }

        /// <summary>
        /// 是否镜像
        /// </summary>
        public bool IsMirrored
        {
            get { return _isMirrored; }
            set { _isMirrored = value; }
        }

        /// <summary>
        /// 是否倒置
        /// </summary>
        public bool IsUpsideDown
        {
            get { return _isUpsideDown; }
            set { _isUpsideDown = value; }
        }


        /// <summary>
        /// 边界框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                // 计算文本边界框
                double textWidth = EstimateTextWidth(_value, _height, _widthFactor);
                double textHeight = _height;
                
                Vector2 pos2d = new Vector2(_position.X, _position.Y);
                Vector2 minPoint = pos2d;
                Vector2 maxPoint = pos2d;
                
                // 根据对齐方式计算边界
                switch (_alignment)
                {
                    case TextAlignment.LeftBottom:
                    case TextAlignment.LeftTop:
                    case TextAlignment.LeftMiddle:
                        maxPoint = pos2d + new Vector2(textWidth, textHeight);
                        break;
                        
                    case TextAlignment.CenterBottom:
                    case TextAlignment.CenterTop:
                    case TextAlignment.CenterMiddle:
                        minPoint = pos2d - new Vector2(textWidth / 2, 0);
                        maxPoint = pos2d + new Vector2(textWidth / 2, textHeight);
                        break;
                        
                    case TextAlignment.RightBottom:
                    case TextAlignment.RightTop:
                    case TextAlignment.RightMiddle:
                        minPoint = pos2d - new Vector2(textWidth, 0);
                        maxPoint = pos2d + new Vector2(0, textHeight);
                        break;
                }
                
                // 处理旋转
                if (Math.Abs(_rotation) > 1e-10)
                {
                    Vector2[] corners = new Vector2[]
                    {
                        minPoint,
                        new Vector2(maxPoint.X, minPoint.Y),
                        maxPoint,
                        new Vector2(minPoint.X, maxPoint.Y)
                    };
                    
                    double minX = double.MaxValue, minY = double.MaxValue;
                    double maxX = double.MinValue, maxY = double.MinValue;
                    
                    foreach (var corner in corners)
                    {
                        Vector2 rotated = Vector2.RotateInRadian(corner, pos2d, _rotation);
                        minX = Math.Min(minX, rotated.X);
                        minY = Math.Min(minY, rotated.Y);
                        maxX = Math.Max(maxX, rotated.X);
                        maxY = Math.Max(maxY, rotated.Y);
                    }
                    
                    return new Bounding(new Vector2(minX, minY), new Vector2(maxX, maxY));
                }
                
                return new Bounding(minPoint, maxPoint);
            }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public Text() : base()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="value">文本内容</param>
        /// <param name="position">位置</param>
        /// <param name="height">高度</param>
        public Text(string value, Vector3 position, double height) : base()
        {
            _value = value ?? string.Empty;
            _position = position;
            _height = Math.Max(0.01, height);
        }

        /// <summary>
        /// 构造函数 - 指定文本内容、位置、高度和来源
        /// </summary>
        /// <param name="value">文本内容</param>
        /// <param name="position">位置</param>
        /// <param name="height">高度</param>
        /// <param name="source">实体来源</param>
        /// <param name="parentId">父组件ID</param>
        public Text(string value, Vector3 position, double height, EntitySource source, ObjectId? parentId = null)
            : this(value, position, height)
        {
            this.Source = source;
            this.ParentComponentId = parentId;
        }

        #endregion

        #region 方法

        /// <summary>
        /// 设置文本
        /// </summary>
        public void SetText(string text)
        {
            _value = text ?? string.Empty;
        }

        /// <summary>
        /// 设置位置
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            _position = position;
        }

        /// <summary>
        /// 设置高度
        /// </summary>
        public void SetHeight(double height)
        {
            _height = Math.Max(0.01, height);
        }

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            if (string.IsNullOrEmpty(_value))
                return;

            // 与 Line.Draw 同约定: 仅当自身显式指定 RGB (ByColor/ByEntity) 时覆盖颜色, 否则沿用外部默认.
            // 否则镀膜符号等复合实体的彩色文字标签会落到环境色 (出图保真); ByLayer 文字不受影响.
            var prevColor = gd.CurrentColor;
            try
            {
                if (color.colorMethod == Colors.ColorMethod.ByColor
                    || color.colorMethod == Colors.ColorMethod.ByEntity)
                    gd.CurrentColor = color.ToDrawingColor();
                DrawPossiblyStacked(gd, new Vector2(_position.X, _position.Y), _value, _height,
                    _textStyle, (lcdb.TextAlignment)_alignment, _rotation);
            }
            finally
            {
                gd.CurrentColor = prevColor;
            }
        }

        /// <summary>
        /// 绘制文本; 若含 MTEXT 公差堆叠码 <c>\S上^下;</c>, 则把上偏差/下偏差分两行小字"摞起来"绘制 (规范公差标法),
        /// 其余文字正常. 无该码时按普通文本绘制. 旋转文本暂以清理后的行内形式回退.
        /// </summary>
        public static void DrawPossiblyStacked(IGraphicsDraw gd, Vector2 pos, string value,
            double height, string font, lcdb.TextAlignment align, double angle)
        {
            int s = value.IndexOf("\\S", System.StringComparison.Ordinal);
            int semi = s < 0 ? -1 : value.IndexOf(';', s);
            if (s < 0 || semi < 0)
            {
                gd.DrawText(pos, value, height, font, align, angle);
                return;
            }

            string before = StripMTextCodes(value.Substring(0, s));
            string stack = value.Substring(s + 2, semi - (s + 2));     // "上^下"
            string after = StripMTextCodes(value.Substring(semi + 1));
            int caret = stack.IndexOf('^');
            string up = caret >= 0 ? stack.Substring(0, caret) : stack;
            string lo = caret >= 0 ? stack.Substring(caret + 1) : "";

            // 旋转文本: 堆叠几何复杂, 回退为清理后的行内形式
            if (System.Math.Abs(angle) > 1e-9)
            {
                gd.DrawText(pos, $"{before} {up}/{lo} {after}".Trim(), height, font, align, angle);
                return;
            }

            var end = string.IsNullOrEmpty(before) ? pos : gd.DrawText(pos, before, height, font, align, 0);
            double dh = height * 0.62, gap = height * 0.12;
            var upEnd = gd.DrawText(new Vector2(end.X + gap, pos.Y + height * 0.48), up, dh, font, align, 0);
            var loEnd = gd.DrawText(new Vector2(end.X + gap, pos.Y - height * 0.10), lo, dh, font, align, 0);
            double devRight = System.Math.Max(upEnd.X, loEnd.X);
            if (!string.IsNullOrEmpty(after))
                gd.DrawText(new Vector2(devRight + gap, pos.Y), after, height, font, align, 0);
        }

        /// <summary>去除 MTEXT 控制码 (\H0.7x; 等、花括号、\S、^), 留下可读文字.</summary>
        private static string StripMTextCodes(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\\[A-Za-z][^;]*;", "");
            return s.Replace("{", "").Replace("}", "").Replace("\\S", "").Replace("^", "/").Trim();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Text text = base.Clone() as Text;
            text._position = _position;
            text._value = _value;
            text._height = _height;
            text._widthFactor = _widthFactor;
            text._rotation = _rotation;
            text._obliqueAngle = _obliqueAngle;
            text._alignment = _alignment;
            text._textStyle = _textStyle;
            text._isMirrored = _isMirrored;
            text._isUpsideDown = _isUpsideDown;
            return text;
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            _position = new Vector3(_position.X + translation.X, _position.Y + translation.Y, _position.Z);
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            Vector2 pos2d = new Vector2(_position.X, _position.Y);
            Vector2 rotated = Vector2.RotateInRadian(pos2d, center, angle);
            _position = new Vector3(rotated.X, rotated.Y, _position.Z);
            _rotation += angle;
        }

        /// <summary>
        /// 缩放
        /// </summary>
        public void Scale(Vector2 center, double factor)
        {
            Vector2 pos2d = new Vector2(_position.X, _position.Y);
            Vector2 offset = (pos2d - center) * factor;
            _position = new Vector3(center.X + offset.X, center.Y + offset.Y, _position.Z);
            _height *= factor;
        }

        /// <summary>
        /// 镜像
        /// </summary>
        public void Mirror(Vector2 point1, Vector2 point2)
        {
            Vector2 pos2d = new Vector2(_position.X, _position.Y);
            Vector2 mirrored = MirrorPoint(pos2d, point1, point2);
            _position = new Vector3(mirrored.X, mirrored.Y, _position.Z);
            
            // 更新镜像后的角度
            double axisAngle = Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            _rotation = 2 * axisAngle - _rotation;
            
            _isMirrored = !_isMirrored;
        }

        /// <summary>
        /// 变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            Vector2 pos2d = new Vector2(_position.X, _position.Y);
            Vector2 transformed = transform * pos2d;
            _position = new Vector3(transformed.X, transformed.Y, _position.Z);
        }

        /// <summary>
        /// 获取捕捉点
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPoints = new List<ObjectSnapPoint>();
            Vector2 pos2d = new Vector2(_position.X, _position.Y);
            
            // 插入点
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Node, pos2d));
            
            // 边界框四个角点
            Bounding bounds = this.bounding;
            Vector2 leftBottom = bounds.minPoint;
            Vector2 rightTop = bounds.maxPoint;
            Vector2 leftTop = new Vector2(leftBottom.X, rightTop.Y);
            Vector2 rightBottom = new Vector2(rightTop.X, leftBottom.Y);
            
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, leftBottom));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, rightBottom));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, rightTop));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, leftTop));
            
            // 边界框中点
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (leftBottom + rightBottom) * 0.5));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (rightBottom + rightTop) * 0.5));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (rightTop + leftTop) * 0.5));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (leftTop + leftBottom) * 0.5));
            
            // 中心点
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Center, bounds.center));
            
            return snapPoints;
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            Vector2 pos2d = new Vector2(_position.X, _position.Y);
            gripPoints.Add(new GripPoint(GripPointType.Center, pos2d));
            return gripPoints;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            if (index == 0)
            {
                _position = new Vector3(newPosition.X, newPosition.Y, _position.Z);
            }
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new Text();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 估算文本宽度
        /// </summary>
        private double EstimateTextWidth(string text, double height, double widthFactor)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
                
            // 简单估算：每个字符宽度约为高度的0.6倍
            return text.Length * height * 0.6 * widthFactor;
        }

        /// <summary>
        /// 镜像点
        /// </summary>
        private Vector2 MirrorPoint(Vector2 point, Vector2 mirrorPoint1, Vector2 mirrorPoint2)
        {
            Vector2 mirrorVector = mirrorPoint2 - mirrorPoint1;
            Vector2 normalVector = new Vector2(-mirrorVector.Y, mirrorVector.X).normalized;
            
            Vector2 pointVector = point - mirrorPoint1;
            double distance = Vector2.Dot(pointVector, normalVector);
            
            return point - 2 * distance * normalVector;
        }

        #endregion
    }
}