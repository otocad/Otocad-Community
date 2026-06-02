using System;
using System.Collections.Generic;
using LitMath;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 属性定义实�?
    /// 原生OtoCAD实现，用于在块定义中定义可变文本占位�?
    /// </summary>
    [Serializable]
    public class AttributeDefinition : Entity
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "AttributeDefinition";

        #region 字段

        // 属性标记（唯一标识符）
        private string _tag = string.Empty;
        
        // 属性提�?
        private string _prompt = string.Empty;
        
        // 默认�?
        private string _defaultValue = string.Empty;
        
        // 当前�?
        private string _value = string.Empty;
        
        // 插入�?
        private Vector2 _position = new Vector2();
        
        // 文字高度
        private double _height = 2.5;
        
        // 旋转角度（弧度）
        private double _rotation = 0.0;
        
        // 宽度比例
        private double _widthFactor = 1.0;
        
        // 倾斜角度（弧度）
        private double _obliqueAngle = 0.0;
        
        // 文字样式名称
        private string _textStyle = "Standard";
        
        // 对齐方式
        private TextAlignment _alignment = TextAlignment.LeftBottom;
        
        // 属性标�?
        private AttributeFlags _flags = AttributeFlags.None;
        
        // 对齐点（当对齐方式不是左下时使用�?
        private Vector2 _alignmentPoint = new Vector2();
        
        // 是否向后绘制
        private bool _isBackward = false;
        
        // 是否上下颠�?
        private bool _isUpsideDown = false;

        #endregion

        #region 属�?

        /// <summary>
        /// 属性标�?
        /// </summary>
        public string Tag
        {
            get { return _tag; }
            set { _tag = value ?? string.Empty; }
        }

        /// <summary>
        /// 属性提�?
        /// </summary>
        public string Prompt
        {
            get { return _prompt; }
            set { _prompt = value ?? string.Empty; }
        }

        /// <summary>
        /// 默认�?
        /// </summary>
        public string DefaultValue
        {
            get { return _defaultValue; }
            set { _defaultValue = value ?? string.Empty; }
        }

        /// <summary>
        /// 当前�?
        /// </summary>
        public string Value
        {
            get { return _value; }
            set { _value = value ?? string.Empty; }
        }

        /// <summary>
        /// 插入�?
        /// </summary>
        public Vector2 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// 文字高度
        /// </summary>
        public double Height
        {
            get { return _height; }
            set { _height = Math.Max(0.001, value); }
        }

        /// <summary>
        /// 旋转角度（弧度）
        /// </summary>
        public double Rotation
        {
            get { return _rotation; }
            set { _rotation = value; }
        }

        /// <summary>
        /// 宽度比例
        /// </summary>
        public double WidthFactor
        {
            get { return _widthFactor; }
            set { _widthFactor = Math.Max(0.001, value); }
        }

        /// <summary>
        /// 倾斜角度（弧度）
        /// </summary>
        public double ObliqueAngle
        {
            get { return _obliqueAngle; }
            set { _obliqueAngle = value; }
        }

        /// <summary>
        /// 文字样式
        /// </summary>
        public string TextStyle
        {
            get { return _textStyle; }
            set { _textStyle = value ?? "Standard"; }
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
        /// 属性标�?
        /// </summary>
        public AttributeFlags Flags
        {
            get { return _flags; }
            set { _flags = value; }
        }

        /// <summary>
        /// 对齐�?
        /// </summary>
        public Vector2 AlignmentPoint
        {
            get { return _alignmentPoint; }
            set { _alignmentPoint = value; }
        }

        /// <summary>
        /// 是否向后绘制
        /// </summary>
        public bool IsBackward
        {
            get { return _isBackward; }
            set { _isBackward = value; }
        }

        /// <summary>
        /// 是否上下颠�?
        /// </summary>
        public bool IsUpsideDown
        {
            get { return _isUpsideDown; }
            set { _isUpsideDown = value; }
        }

        /// <summary>
        /// 是否隐藏
        /// </summary>
        public bool IsInvisible
        {
            get { return (_flags & AttributeFlags.Invisible) != 0; }
            set
            {
                if (value)
                    _flags |= AttributeFlags.Invisible;
                else
                    _flags &= ~AttributeFlags.Invisible;
            }
        }

        /// <summary>
        /// 是否常量
        /// </summary>
        public bool IsConstant
        {
            get { return (_flags & AttributeFlags.Constant) != 0; }
            set
            {
                if (value)
                    _flags |= AttributeFlags.Constant;
                else
                    _flags &= ~AttributeFlags.Constant;
            }
        }

        /// <summary>
        /// 是否验证
        /// </summary>
        public bool IsVerify
        {
            get { return (_flags & AttributeFlags.Verify) != 0; }
            set
            {
                if (value)
                    _flags |= AttributeFlags.Verify;
                else
                    _flags &= ~AttributeFlags.Verify;
            }
        }

        /// <summary>
        /// 是否预设
        /// </summary>
        public bool IsPreset
        {
            get { return (_flags & AttributeFlags.Preset) != 0; }
            set
            {
                if (value)
                    _flags |= AttributeFlags.Preset;
                else
                    _flags &= ~AttributeFlags.Preset;
            }
        }


        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                // 使用默认值或标记作为显示文本
                string displayText = string.IsNullOrEmpty(_value) ? 
                    (string.IsNullOrEmpty(_defaultValue) ? _tag : _defaultValue) : _value;
                
                double textWidth = _height * displayText.Length * _widthFactor * 0.7;
                double textHeight = _height;

                // 根据对齐方式计算边界�?
                Vector2 minPoint, maxPoint;
                CalculateBounds(_position, textWidth, textHeight, _alignment, out minPoint, out maxPoint);

                // 考虑旋转
                if (Math.Abs(_rotation) > 1e-10)
                {
                    Vector2[] corners = new Vector2[]
                    {
                        minPoint,
                        new Vector2(maxPoint.X, minPoint.Y),
                        maxPoint,
                        new Vector2(minPoint.X, maxPoint.Y)
                    };

                    for (int i = 0; i < corners.Length; i++)
                    {
                        corners[i] = Vector2.RotateInRadian(corners[i], _position, _rotation);
                    }

                    minPoint = corners[0];
                    maxPoint = corners[0];
                    for (int i = 1; i < corners.Length; i++)
                    {
                        minPoint = new Vector2(
                            Math.Min(minPoint.X, corners[i].X),
                            Math.Min(minPoint.Y, corners[i].Y)
                        );
                        maxPoint = new Vector2(
                            Math.Max(maxPoint.X, corners[i].X),
                            Math.Max(maxPoint.Y, corners[i].Y)
                        );
                    }
                }

                return new Bounding(minPoint, maxPoint);
            }
        }

        #endregion

        #region 构造函�?

        /// <summary>
        /// 创建属性定�?
        /// </summary>
        public AttributeDefinition() : base()
        {
        }

        /// <summary>
        /// 创建属性定�?
        /// </summary>
        /// <param name="tag">标记</param>
        /// <param name="position">位置</param>
        /// <param name="height">高度</param>
        public AttributeDefinition(string tag, Vector2 position, double height) : base()
        {
            _tag = tag;
            _position = position;
            _height = height;
        }

        /// <summary>
        /// 创建属性定�?
        /// </summary>
        /// <param name="tag">标记</param>
        /// <param name="prompt">提示</param>
        /// <param name="defaultValue">默认�?/param>
        /// <param name="position">位置</param>
        /// <param name="height">高度</param>
        public AttributeDefinition(string tag, string prompt, string defaultValue, Vector2 position, double height) : base()
        {
            _tag = tag;
            _prompt = prompt;
            _defaultValue = defaultValue;
            _position = position;
            _height = height;
        }

        #endregion

        #region 方法

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            // 使用默认值或标记作为显示文本
            string displayText = string.IsNullOrEmpty(_value) ? 
                (string.IsNullOrEmpty(_defaultValue) ? _tag : _defaultValue) : _value;

            // 创建临时文本实体用于绘制
            Text text = new Text
            {
                Position = new Vector3(_position.X, _position.Y, 0),
                Height = _height,
                angle = _rotation,
                Value = displayText,
                alignment = _alignment,
                color = this.color,
                layer = this.layer
            };

            text.Draw(gd);
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new AttributeDefinition();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            AttributeDefinition attrDef = base.Clone() as AttributeDefinition;
            attrDef._tag = _tag;
            attrDef._prompt = _prompt;
            attrDef._defaultValue = _defaultValue;
            attrDef._value = _value;
            attrDef._position = _position;
            attrDef._height = _height;
            attrDef._rotation = _rotation;
            attrDef._widthFactor = _widthFactor;
            attrDef._obliqueAngle = _obliqueAngle;
            attrDef._textStyle = _textStyle;
            attrDef._alignment = _alignment;
            attrDef._flags = _flags;
            attrDef._alignmentPoint = _alignmentPoint;
            attrDef._isBackward = _isBackward;
            attrDef._isUpsideDown = _isUpsideDown;
            return attrDef;
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            _position += translation;
            _alignmentPoint += translation;
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            _position = Vector2.RotateInRadian(_position, center, angle);
            _alignmentPoint = Vector2.RotateInRadian(_alignmentPoint, center, angle);
            _rotation += angle;
        }

        /// <summary>
        /// 变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            _position = transform * _position;
            _alignmentPoint = transform * _alignmentPoint;
            
            // 从变换矩阵提取缩放和旋转
            Vector2 xAxis = transform * new Vector2(1, 0) - transform * Vector2.Zero;
            Vector2 yAxis = transform * new Vector2(0, 1) - transform * Vector2.Zero;
            
            double scaleX = xAxis.length;
            double scaleY = yAxis.length;
            
            _height *= scaleY;
            _widthFactor *= scaleX / scaleY;
            
            double newRotation = Math.Atan2(xAxis.Y, xAxis.X);
            _rotation = newRotation;
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            
            // 插入点夹�?
            gripPoints.Add(new GripPoint(GripPointType.Center, _position));
            
            // 如果有对齐点且不同于插入点，添加对齐点夹�?
            if (_alignment != TextAlignment.LeftBottom && 
                (_alignmentPoint - _position).length > 1e-10)
            {
                gripPoints.Add(new GripPoint(GripPointType.Center, _alignmentPoint));
            }
            
            return gripPoints;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            switch (index)
            {
                case 0: // 插入�?
                    {
                        Vector2 delta = newPosition - _position;
                        _position = newPosition;
                        if (_alignment != TextAlignment.LeftBottom)
                        {
                            _alignmentPoint += delta;
                        }
                    }
                    break;
                case 1: // 对齐�?
                    if (_alignment != TextAlignment.LeftBottom)
                    {
                        _alignmentPoint = newPosition;
                    }
                    break;
            }
        }

        /// <summary>
        /// 获取捕捉�?
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPoints = new List<ObjectSnapPoint>();
            
            // 插入�?
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Ins, _position));
            
            // 边界框的四个角点和中�?
            var bounds = bounding;
            var leftBottom = new Vector2(bounds.left, bounds.bottom);
            var rightTop = new Vector2(bounds.right, bounds.top);
            var leftTop = new Vector2(bounds.left, bounds.top);
            var rightBottom = new Vector2(bounds.right, bounds.bottom);
            
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, leftBottom));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, rightTop));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, leftTop));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, rightBottom));
            
            // 边中�?
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, 
                new Vector2((bounds.left + bounds.right) / 2, bounds.bottom)));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, 
                new Vector2((bounds.left + bounds.right) / 2, bounds.top)));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, 
                new Vector2(bounds.left, (bounds.bottom + bounds.top) / 2)));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, 
                new Vector2(bounds.right, (bounds.bottom + bounds.top) / 2)));
            
            return snapPoints;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 计算文本边界
        /// </summary>
        private void CalculateBounds(Vector2 position, double width, double height, 
            TextAlignment alignment, out Vector2 minPoint, out Vector2 maxPoint)
        {
            switch (alignment)
            {
                case TextAlignment.LeftBottom:
                    minPoint = position;
                    maxPoint = position + new Vector2(width, height);
                    break;
                case TextAlignment.CenterBottom:
                    minPoint = position - new Vector2(width / 2, 0);
                    maxPoint = position + new Vector2(width / 2, height);
                    break;
                case TextAlignment.RightBottom:
                    minPoint = position - new Vector2(width, 0);
                    maxPoint = position + new Vector2(0, height);
                    break;
                case TextAlignment.LeftMiddle:
                    minPoint = position - new Vector2(0, height / 2);
                    maxPoint = position + new Vector2(width, height / 2);
                    break;
                case TextAlignment.CenterMiddle:
                    minPoint = position - new Vector2(width / 2, height / 2);
                    maxPoint = position + new Vector2(width / 2, height / 2);
                    break;
                case TextAlignment.RightMiddle:
                    minPoint = position - new Vector2(width, height / 2);
                    maxPoint = position + new Vector2(0, height / 2);
                    break;
                case TextAlignment.LeftTop:
                    minPoint = position - new Vector2(0, height);
                    maxPoint = position + new Vector2(width, 0);
                    break;
                case TextAlignment.CenterTop:
                    minPoint = position - new Vector2(width / 2, height);
                    maxPoint = position + new Vector2(width / 2, 0);
                    break;
                case TextAlignment.RightTop:
                    minPoint = position - new Vector2(width, height);
                    maxPoint = position;
                    break;
                default:
                    minPoint = position;
                    maxPoint = position + new Vector2(width, height);
                    break;
            }
        }

        /// <summary>
        /// 创建属性实�?
        /// </summary>
        public Attribute CreateAttribute()
        {
            Attribute attr = new Attribute
            {
                Tag = _tag,
                Value = string.IsNullOrEmpty(_value) ? _defaultValue : _value,
                Position = _position,
                Height = _height,
                Rotation = _rotation,
                WidthFactor = _widthFactor,
                ObliqueAngle = _obliqueAngle,
                TextStyle = _textStyle,
                Alignment = _alignment,
                Flags = _flags,
                AlignmentPoint = _alignmentPoint,
                IsBackward = _isBackward,
                IsUpsideDown = _isUpsideDown,
                color = this.color,
                layer = this.layer
            };
            
            return attr;
        }

        #endregion
    }

    /// <summary>
    /// 属性标�?
    /// </summary>
    [Flags]
    public enum AttributeFlags
    {
        /// <summary>
        /// 无标�?
        /// </summary>
        None = 0,
        
        /// <summary>
        /// 不可�?
        /// </summary>
        Invisible = 1,
        
        /// <summary>
        /// 常量
        /// </summary>
        Constant = 2,
        
        /// <summary>
        /// 验证
        /// </summary>
        Verify = 4,
        
        /// <summary>
        /// 预设
        /// </summary>
        Preset = 8
    }
}