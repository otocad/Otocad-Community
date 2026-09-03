using System;
using System.Collections.Generic;
using LitMath;
using lcdb;
using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 标注实体基类
    /// 原生OtoCAD实现，不依赖外部�?
    /// </summary>
    [Serializable]
    public abstract class DimensionBase : Entity
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "Dimension";

        #region 字段

        // 定义�?- 用于确定标注位置的参考点
        protected Vector2 _definitionPoint = new Vector2();
        
        // 文本参考点 - 标注文本的中心位�?
        protected Vector2 _textReferencePoint = new Vector2();
        
        // 标注类型
        protected DimensionType _dimensionType;
        
        // 标注样式
        protected DimensionStyle _style;
        
        // 文本旋转角度（弧度）
        protected double _textRotation = 0.0;
        
        // 用户自定义文�?
        protected string _userText = string.Empty;
        
        // 高程（Z坐标�?
        protected double _elevation = 0.0;
        
        // 文本是否手动定位
        protected bool _textPositionManuallySet = false;
        
        // 生成的标注实体列表（线条、箭头、文本等�?
        protected List<Entity> _generatedEntities = new List<Entity>();
        
        // 公差对象
        protected BasicTolerance _tolerance = null;

        #endregion

        #region 属�?

        /// <summary>
        /// 定义�?
        /// </summary>
        public Vector2 definitionPoint
        {
            get { return _definitionPoint; }
            set { _definitionPoint = value; }
        }

        /// <summary>
        /// 文本参考点
        /// </summary>
        public Vector2 textReferencePoint
        {
            get { return _textReferencePoint; }
            set 
            { 
                _textReferencePoint = value;
                _textPositionManuallySet = true;
            }
        }

        /// <summary>
        /// 标注类型
        /// </summary>
        public DimensionType dimensionType
        {
            get { return _dimensionType; }
        }

        /// <summary>
        /// 标注样式
        /// </summary>
        public DimensionStyle style
        {
            get { return _style; }
            set { _style = value ?? DimensionStyle.Default; }
        }

        /// <summary>
        /// 文本旋转角度（弧度）
        /// </summary>
        public double textRotation
        {
            get { return _textRotation; }
            set { _textRotation = value; }
        }

        /// <summary>
        /// 用户自定义文�?
        /// </summary>
        public string userText
        {
            get { return _userText; }
            set { _userText = value ?? string.Empty; }
        }

        /// <summary>
        /// 高程
        /// </summary>
        public double elevation
        {
            get { return _elevation; }
            set { _elevation = value; }
        }

        /// <summary>
        /// 文本是否手动定位
        /// </summary>
        public bool textPositionManuallySet
        {
            get { return _textPositionManuallySet; }
            set { _textPositionManuallySet = value; }
        }

        /// <summary>
        /// 获取实际测量�?
        /// </summary>
        public abstract double measurement { get; }

        
        /// <summary>
        /// 公差对象，用于存储和管理标注的公差信息
        /// </summary>
        public BasicTolerance Tolerance
        {
            get { return _tolerance; }
            set { _tolerance = value; }
        }

        #endregion

        #region 构造函�?

        /// <summary>
        /// 初始化标注基�?
        /// </summary>
        protected DimensionBase(DimensionType type)
        {
            _dimensionType = type;
            _style = DimensionStyle.Default;
        }

        #endregion

        #region 抽象方法

        /// <summary>
        /// 生成标注图形
        /// </summary>
        public abstract void Generate();

        /// <summary>
        /// 重新计算派生的参考点(definitionPoint、自动文本位置等).
        /// 子类在几何字段变更后, 由本基类的 <see cref="Update"/> 在 Generate 之前自动调用.
        /// 默认空实现 — 子类按需重写.
        /// </summary>
        protected virtual void CalculateReferencePoints() { }

        /// <summary>
        /// 更新标注 — 先重算参考点, 再重生成图形.
        /// 等价于 netDxf 的 Dimension.Update(): 在样式或几何字段修改后手动调用一次即可同步显示.
        /// </summary>
        public void Update()
        {
            CalculateReferencePoints();
            Generate();
        }

        #endregion

        #region 基类方法重写

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            // 生成标注图形
            Generate();

            // 绘制生成的实�?
            foreach (var entity in _generatedEntities)
            {
                entity.Draw(gd);
            }
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            _definitionPoint += translation;
            _textReferencePoint += translation;
            
            // 平移生成的实�?
            foreach (var entity in _generatedEntities)
            {
                entity.Translate(translation);
            }
            
            // 更新特定类型的标注点
            TranslateSpecific(translation);
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            _definitionPoint = Vector2.RotateInRadian(_definitionPoint, center, angle);
            _textReferencePoint = Vector2.RotateInRadian(_textReferencePoint, center, angle);
            _textRotation += angle;
            
            // 旋转生成的实�?
            foreach (var entity in _generatedEntities)
            {
                entity.Rotate(center, angle);
            }
            
            // 旋转特定类型的标注点
            RotateSpecific(center, angle);
        }

        /// <summary>
        /// 变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            _definitionPoint = transform * _definitionPoint;
            _textReferencePoint = transform * _textReferencePoint;
            
            // 变换生成的实�?
            foreach (var entity in _generatedEntities)
            {
                entity.TransformBy(transform);
            }
            
            // 变换特定类型的标注点
            TransformBySpecific(transform);
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            DimensionBase dimension = base.Clone() as DimensionBase;
            dimension._definitionPoint = _definitionPoint;
            dimension._textReferencePoint = _textReferencePoint;
            dimension._dimensionType = _dimensionType;
            dimension._style = _style;
            dimension._textRotation = _textRotation;
            dimension._userText = _userText;
            dimension._elevation = _elevation;
            dimension._textPositionManuallySet = _textPositionManuallySet;
            
            // 克隆公差
            if (_tolerance != null)
            {
                dimension._tolerance = _tolerance.Clone() as BasicTolerance;
            }
            
            // 克隆生成的实�?
            dimension._generatedEntities = new List<Entity>();
            foreach (var entity in _generatedEntities)
            {
                dimension._generatedEntities.Add(entity.Clone() as Entity);
            }
            
            return dimension;
        }

        #endregion

        #region 虚方法（供子类重写）

        /// <summary>
        /// 子类特定的平移操�?
        /// </summary>
        protected virtual void TranslateSpecific(Vector2 translation) { }

        /// <summary>
        /// 子类特定的旋转操�?
        /// </summary>
        protected virtual void RotateSpecific(Vector2 center, double angle) { }

        /// <summary>
        /// 子类特定的变换操�?
        /// </summary>
        protected virtual void TransformBySpecific(Matrix3 transform) { }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取格式化的标注文本
        /// </summary>
        protected string GetFormattedText()
        {
            string baseText;
            if (!string.IsNullOrEmpty(_userText))
            {
                // 替换<>为实际测量�?
                baseText = _userText.Replace("<>", FormatMeasurement(measurement));
            }
            else
            {
                baseText = FormatMeasurement(measurement);
            }
            
            // 如果有公差，添加公差文本
            if (_tolerance != null)
            {
                return baseText + " " + _tolerance.GetFormattedString();
            }
            
            return baseText;
        }

        /// <summary>
        /// 格式化测量�?
        /// </summary>
        protected virtual string FormatMeasurement(double value)
        {
            // 根据样式中的设置格式化数�?
            if (_style != null)
            {
                return value.ToString(_style.DecimalFormat);
            }
            return value.ToString("F2");
        }

        /// <summary>
        /// 创建箭头实体
        /// </summary>
        protected Solid CreateArrowhead(Vector2 tip, Vector2 direction, double size)
        {
            // 计算箭头的三个顶�?
            Vector2 dir = direction.normalized;
            Vector2 perpDir = new Vector2(-dir.Y, dir.X);
            
            Vector2 p1 = tip;
            Vector2 p2 = tip - dir * size + perpDir * (size * 0.167);
            Vector2 p3 = tip - dir * size - perpDir * (size * 0.167);
            
            return new Solid
            {
                firstPoint = p1,
                secondPoint = p2,
                thirdPoint = p3,
                fourthPoint = p3, // 三角形箭头，第四点与第三点相�?
                color = this.color,
                layer = this.layer
            };
        }

        /// <summary>
        /// 创建文本实体
        /// </summary>
        protected Text CreateDimensionText(Vector2 position, string text, double height)
        {
            return new Text
            {
                Position = new Vector3(position.X, position.Y, _elevation),
                Value = text,
                Height = height,
                angle = _textRotation,
                alignment = TextAlignment.CenterBottom,
                color = this.color,
                layer = this.layer
            };
        }

        #endregion
    }

    /// <summary>
    /// 标注类型枚举
    /// </summary>
    public enum DimensionType
    {
        /// <summary>
        /// 线性标�?
        /// </summary>
        Linear,
        
        /// <summary>
        /// 对齐标注
        /// </summary>
        Aligned,
        
        /// <summary>
        /// 角度标注（两线）
        /// </summary>
        Angular2Line,
        
        /// <summary>
        /// 角度标注（三点）
        /// </summary>
        Angular3Point,
        
        /// <summary>
        /// 半径标注
        /// </summary>
        Radial,
        
        /// <summary>
        /// 直径标注
        /// </summary>
        Diametric,
        
        /// <summary>
        /// 坐标标注
        /// </summary>
        Ordinate
    }
}