using System;
using System.Collections.Generic;
using LitMath;


namespace lcdb
{
    /// <summary>
    /// 直径标注实体
    /// 原生OtoCAD实现，用于标注圆或圆弧的直径
    /// </summary>
    [Serializable]
    public class DiametricDimension : DimensionBase
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "DiametricDimension";

        #region 字段

        // 圆心点
        private Vector2 _centerPoint = new Vector2();
        
        // 第一个圆弧上的定义点
        private Vector2 _chordPoint1 = new Vector2();
        
        // 第二个圆弧上的定义点（直径对面）
        private Vector2 _chordPoint2 = new Vector2();
        
        // 引线长度
        private double _leaderLength = 0.0;

        #endregion

        #region 属性

        /// <summary>
        /// 圆心点
        /// </summary>
        public Vector2 centerPoint
        {
            get { return _centerPoint; }
            set 
            { 
                _centerPoint = value;
                UpdateDefinitionPoint();
            }
        }

        /// <summary>
        /// 第一个圆弧上的定义点
        /// </summary>
        public Vector2 chordPoint1
        {
            get { return _chordPoint1; }
            set 
            { 
                _chordPoint1 = value;
                UpdateDefinitionPoint();
            }
        }

        /// <summary>
        /// 第二个圆弧上的定义点
        /// </summary>
        public Vector2 chordPoint2
        {
            get { return _chordPoint2; }
            set 
            { 
                _chordPoint2 = value;
                UpdateDefinitionPoint();
            }
        }

        /// <summary>
        /// 引线长度
        /// </summary>
        public double leaderLength
        {
            get { return _leaderLength; }
            set { _leaderLength = Math.Max(0, value); }
        }
        
        /// <summary>
        /// 中心点（便利属性）
        /// </summary>
        public Vector2 center
        {
            get { return _centerPoint; }
            set 
            { 
                _centerPoint = value;
                UpdateDefinitionPoint();
            }
        }
        
        /// <summary>
        /// 弦点（便利属性，映射到chordPoint1）
        /// </summary>
        public Vector2 chordPoint
        {
            get { return _chordPoint1; }
            set 
            { 
                _chordPoint1 = value;
                UpdateDefinitionPoint();
            }
        }
        
        /// <summary>
        /// 远弦点（便利属性，映射到chordPoint2）
        /// </summary>
        public Vector2 farChordPoint
        {
            get { return _chordPoint2; }
            set 
            { 
                _chordPoint2 = value;
                UpdateDefinitionPoint();
            }
        }

        /// <summary>
        /// 获取实际测量值（直径）
        /// </summary>
        public override double measurement
        {
            get
            {
                // 计算直径
                double radius1 = (_chordPoint1 - _centerPoint).length;
                double radius2 = (_chordPoint2 - _centerPoint).length;
                return radius1 + radius2;
            }
        }

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                List<Vector2> points = new List<Vector2>();
                points.Add(_centerPoint);
                points.Add(_chordPoint1);
                points.Add(_chordPoint2);
                
                // 添加引线终点
                if (_leaderLength > 0)
                {
                    Vector2 direction = (_chordPoint2 - _chordPoint1).normalized;
                    Vector2 leaderEnd = _chordPoint2 + direction * _leaderLength;
                    points.Add(leaderEnd);
                }
                
                // 添加文本位置
                if (_textReferencePoint != null)
                {
                    points.Add(_textReferencePoint);
                }

                double minX = double.MaxValue;
                double minY = double.MaxValue;
                double maxX = double.MinValue;
                double maxY = double.MinValue;

                foreach (Vector2 point in points)
                {
                    minX = Math.Min(minX, point.X);
                    minY = Math.Min(minY, point.Y);
                    maxX = Math.Max(maxX, point.X);
                    maxY = Math.Max(maxY, point.Y);
                }

                return new Bounding(new Vector2(minX, minY), new Vector2(maxX, maxY));
            }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建直径标注
        /// </summary>
        public DiametricDimension() : base(DimensionType.Diametric)
        {
        }

        /// <summary>
        /// 创建直径标注
        /// </summary>
        public DiametricDimension(Vector2 center, Vector2 chordPt1, Vector2 chordPt2, double leaderLen = 0) 
            : base(DimensionType.Diametric)
        {
            _centerPoint = center;
            _chordPoint1 = chordPt1;
            _chordPoint2 = chordPt2;
            _leaderLength = leaderLen;
            UpdateDefinitionPoint();
        }

        #endregion

        #region 方法

        /// <summary>
        /// 生成标注图形
        /// </summary>
        public override void Generate()
        {
            _generatedEntities.Clear();

            if (_style == null)
                _style = DimensionStyle.Default;

            Vector2 direction = (_chordPoint2 - _chordPoint1).normalized;
            double diameter = measurement;

            // 创建直径线（穿过圆心）
            Line diameterLine = new Line(_chordPoint1, _chordPoint2)
            {
                color = this.color,
                layer = this.layer
            };
            _generatedEntities.Add(diameterLine);

            // 创建引线（如果需要）
            Vector2 leaderEnd = _chordPoint2;
            if (_leaderLength > 0)
            {
                leaderEnd = _chordPoint2 + direction * _leaderLength;
                Line leaderLine = new Line(_chordPoint2, leaderEnd)
                {
                    color = this.color,
                    layer = this.layer
                };
                _generatedEntities.Add(leaderLine);
            }

            // 创建箭头（两端）
            Solid arrow1 = CreateArrowhead(_chordPoint1, direction, _style.ArrowSize);
            Solid arrow2 = CreateArrowhead(_chordPoint2, -direction, _style.ArrowSize);
            _generatedEntities.Add(arrow1);
            _generatedEntities.Add(arrow2);

            // 计算文本位置
            if (!_textPositionManuallySet)
            {
                if (_leaderLength > 0)
                {
                    // 文本放在引线终点附近
                    _textReferencePoint = leaderEnd + direction * _style.DimensionLineGap;
                }
                else
                {
                    // 文本放在直径线中点偏上
                    Vector2 midPoint = (_chordPoint1 + _chordPoint2) * 0.5;
                    Vector2 perpDir = new Vector2(-direction.Y, direction.X);
                    _textReferencePoint = midPoint + perpDir * _style.DimensionLineGap;
                }
            }

            // 创建文本（带⌀前缀）
            string text = "⌀" + GetFormattedText();
            Text dimText = CreateDimensionText(_textReferencePoint, text, _style.TextHeight);
            Vector2 textAnchor = _leaderLength > 0
                ? leaderEnd
                : (_chordPoint1 + _chordPoint2) * 0.5;
            Vector2 perpDirForText = new Vector2(-direction.Y, direction.X);

            if (_style.TextInsideHorizontal)
            {
                double textSide = Vector2.Dot(_textReferencePoint - textAnchor, perpDirForText);
                if (Math.Abs(perpDirForText.X) >= Math.Abs(perpDirForText.Y))
                {
                    double textPadding = _style.TextHeight * 0.35 + _style.DimensionLineGap;
                    Vector2 shoulderEnd = new Vector2(
                        textSide >= 0 ? _textReferencePoint.X - textPadding : _textReferencePoint.X + textPadding,
                        _textReferencePoint.Y);
                    Line shoulderLine = new Line(textAnchor, shoulderEnd)
                    {
                        color = this.color,
                        layer = this.layer
                    };
                    _generatedEntities.Add(shoulderLine);
                }
            }
            
            // 文本旋转角度
            double angle = Math.Atan2(direction.Y, direction.X);
            if (angle > Math.PI / 2)
            {
                angle -= Math.PI;
            }
            else if (angle < -Math.PI / 2)
            {
                angle += Math.PI;
            }
            
            if (_style.TextInsideHorizontal)
            {
                dimText.angle = 0;
            }
            else
            {
                dimText.angle = angle;
            }

            if (_style.TextInsideHorizontal)
            {
                double textSide = Vector2.Dot(_textReferencePoint - textAnchor, perpDirForText);
                if (Math.Abs(perpDirForText.X) >= Math.Abs(perpDirForText.Y))
                {
                    dimText.alignment = textSide >= 0 ? TextAlignment.LeftMiddle : TextAlignment.RightMiddle;
                }
                else
                {
                    dimText.alignment = textSide >= 0 ? TextAlignment.CenterBottom : TextAlignment.CenterTop;
                }
            }
            else
            {
                dimText.alignment = TextAlignment.CenterBottom;
            }
            _generatedEntities.Add(dimText);

            // 添加中心标记（如果没有引线）
            if (_leaderLength == 0)
            {
                CreateCenterMark();
            }
        }

        /// <summary>
        /// 创建中心标记
        /// </summary>
        private void CreateCenterMark()
        {
            double markSize = _style.ArrowSize * 0.5;
            
            // 水平线
            Line hLine = new Line(
                _centerPoint - new Vector2(markSize, 0),
                _centerPoint + new Vector2(markSize, 0))
            {
                color = this.color,
                layer = this.layer
            };
            _generatedEntities.Add(hLine);
            
            // 垂直线
            Line vLine = new Line(
                _centerPoint - new Vector2(0, markSize),
                _centerPoint + new Vector2(0, markSize))
            {
                color = this.color,
                layer = this.layer
            };
            _generatedEntities.Add(vLine);
        }

        /// <summary>
        /// 更新标注
        /// </summary>
        protected override void CalculateReferencePoints()
        {
            UpdateDefinitionPoint();

            // 确保两个弦点在直径两端
            Vector2 toChord1 = (_chordPoint1 - _centerPoint).normalized;
            Vector2 toChord2 = (_chordPoint2 - _centerPoint).normalized;

            // 如果两点不在直径两端，调整第二点
            double dot = Vector2.Dot(toChord1, toChord2);
            if (dot > -0.9) // 不够接近180度
            {
                double radius = (_chordPoint1 - _centerPoint).length;
                _chordPoint2 = _centerPoint - toChord1 * radius;
            }
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new DiametricDimension();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            DiametricDimension dimension = base.Clone() as DiametricDimension;
            dimension._centerPoint = _centerPoint;
            dimension._chordPoint1 = _chordPoint1;
            dimension._chordPoint2 = _chordPoint2;
            dimension._leaderLength = _leaderLength;
            return dimension;
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            gripPoints.Add(new GripPoint(GripPointType.Center, _centerPoint));
            gripPoints.Add(new GripPoint(GripPointType.End, _chordPoint1));
            gripPoints.Add(new GripPoint(GripPointType.End, _chordPoint2));
            
            // 引线终点夹点
            if (_leaderLength > 0)
            {
                Vector2 direction = (_chordPoint2 - _chordPoint1).normalized;
                Vector2 leaderEnd = _chordPoint2 + direction * _leaderLength;
                gripPoints.Add(new GripPoint(GripPointType.End, leaderEnd));
            }
            
            if (_textPositionManuallySet)
            {
                gripPoints.Add(new GripPoint(GripPointType.Center, _textReferencePoint));
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
                case 0: // 圆心
                    _centerPoint = newPosition;
                    break;
                case 1: // 第一个弦点
                    _chordPoint1 = newPosition;
                    break;
                case 2: // 第二个弦点
                    _chordPoint2 = newPosition;
                    break;
                case 3: // 引线终点
                    if (_leaderLength > 0)
                    {
                        Vector2 direction = (_chordPoint2 - _chordPoint1).normalized;
                        _leaderLength = Vector2.Dot(newPosition - _chordPoint2, direction);
                        _leaderLength = Math.Max(0, _leaderLength);
                    }
                    break;
                case 4: // 文本位置
                    if (_textPositionManuallySet)
                    {
                        _textReferencePoint = newPosition;
                    }
                    break;
            }
            Update();
        }

        /// <summary>
        /// 获取捕捉点
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPoints = new List<ObjectSnapPoint>();
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Center, _centerPoint));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _chordPoint1));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _chordPoint2));
            
            // 引线终点
            if (_leaderLength > 0)
            {
                Vector2 direction = (_chordPoint2 - _chordPoint1).normalized;
                Vector2 leaderEnd = _chordPoint2 + direction * _leaderLength;
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, leaderEnd));
            }
            
            // 直径线中点
            Vector2 midPoint = (_chordPoint1 + _chordPoint2) * 0.5;
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, midPoint));
            
            return snapPoints;
        }

        #endregion

        #region 重写基类方法

        /// <summary>
        /// 子类特定的平移操作
        /// </summary>
        protected override void TranslateSpecific(Vector2 translation)
        {
            _centerPoint += translation;
            _chordPoint1 += translation;
            _chordPoint2 += translation;
        }

        /// <summary>
        /// 子类特定的旋转操作
        /// </summary>
        protected override void RotateSpecific(Vector2 center, double angle)
        {
            _centerPoint = Vector2.RotateInRadian(_centerPoint, center, angle);
            _chordPoint1 = Vector2.RotateInRadian(_chordPoint1, center, angle);
            _chordPoint2 = Vector2.RotateInRadian(_chordPoint2, center, angle);
        }

        /// <summary>
        /// 子类特定的变换操作
        /// </summary>
        protected override void TransformBySpecific(Matrix3 transform)
        {
            _centerPoint = transform * _centerPoint;
            _chordPoint1 = transform * _chordPoint1;
            _chordPoint2 = transform * _chordPoint2;
            // 注意：变换可能改变尺度，需要重新计算引线长度
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 更新定义点
        /// </summary>
        private void UpdateDefinitionPoint()
        {
            _definitionPoint = _centerPoint;
        }

        /// <summary>
        /// 设置引线终点位置
        /// </summary>
        public void SetLeaderEndPoint(Vector2 endPoint)
        {
            Vector2 direction = (_chordPoint2 - _chordPoint1).normalized;
            _leaderLength = Vector2.Dot(endPoint - _chordPoint2, direction);
            _leaderLength = Math.Max(0, _leaderLength);
        }

        /// <summary>
        /// 通过圆上一点创建直径标注
        /// </summary>
        public static DiametricDimension CreateFromCirclePoint(Vector2 center, Vector2 pointOnCircle, double leaderLength = 0)
        {
            Vector2 toPoint = pointOnCircle - center;
            double radius = toPoint.length;
            Vector2 direction = toPoint.normalized;
            
            Vector2 chordPoint1 = center + direction * radius;
            Vector2 chordPoint2 = center - direction * radius;
            
            return new DiametricDimension(center, chordPoint1, chordPoint2, leaderLength);
        }

        #endregion
    }
}
