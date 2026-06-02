using System;
using System.Collections.Generic;
using LitMath;


namespace lcdb
{
    /// <summary>
    /// 角度标注实体（三点角度）
    /// 原生OtoCAD实现，通过三个点定义角度：顶点和两个端点
    /// </summary>
    [Serializable]
    public class Angular3PointDimension : DimensionBase
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "Angular3PointDimension";

        #region 字段

        // 角度顶点（中心点）
        private Vector2 _centerPoint = new Vector2();
        
        // 第一个角度点
        private Vector2 _firstPoint = new Vector2();
        
        // 第二个角度点
        private Vector2 _secondPoint = new Vector2();
        
        // 圆弧位置点（定义圆弧半径）
        private Vector2 _arcPoint = new Vector2();

        #endregion

        #region 属性

        /// <summary>
        /// 角度顶点（中心点）
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
        /// 第一个角度点
        /// </summary>
        public Vector2 firstPoint
        {
            get { return _firstPoint; }
            set 
            { 
                _firstPoint = value;
                UpdateDefinitionPoint();
            }
        }

        /// <summary>
        /// 第二个角度点
        /// </summary>
        public Vector2 secondPoint
        {
            get { return _secondPoint; }
            set 
            { 
                _secondPoint = value;
                UpdateDefinitionPoint();
            }
        }

        /// <summary>
        /// 圆弧位置点
        /// </summary>
        public Vector2 arcPoint
        {
            get { return _arcPoint; }
            set { _arcPoint = value; }
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
        /// 定义点（便利属性）
        /// </summary>
        public Vector2 defPoint
        {
            get { return _arcPoint; }
            set { _arcPoint = value; }
        }

        /// <summary>
        /// 获取实际测量值（角度，弧度制）
        /// </summary>
        public override double measurement
        {
            get
            {
                // 计算两条射线的方向向量
                Vector2 dir1 = (_firstPoint - _centerPoint).normalized;
                Vector2 dir2 = (_secondPoint - _centerPoint).normalized;
                
                // 计算角度
                double angle = Math.Acos(Math.Max(-1.0, Math.Min(1.0, Vector2.Dot(dir1, dir2))));
                
                // 确定角度方向（使用叉积判断）
                double cross = dir1.X * dir2.Y - dir1.Y * dir2.X;
                
                // 检查圆弧点在哪一侧，决定是否需要用大角度
                Vector2 toArc = (_arcPoint - _centerPoint).normalized;
                double angle1 = Math.Atan2(dir1.Y, dir1.X);
                double angle2 = Math.Atan2(dir2.Y, dir2.X);
                double angleArc = Math.Atan2(toArc.Y, toArc.X);
                
                // 归一化角度到[0, 2π]
                if (angle1 < 0) angle1 += 2 * Math.PI;
                if (angle2 < 0) angle2 += 2 * Math.PI;
                if (angleArc < 0) angleArc += 2 * Math.PI;
                
                // 检查圆弧点是否在角度范围内
                bool inRange = false;
                if (angle1 < angle2)
                {
                    inRange = angleArc >= angle1 && angleArc <= angle2;
                }
                else
                {
                    inRange = angleArc >= angle1 || angleArc <= angle2;
                }
                
                // 如果圆弧点不在小角度范围内，使用大角度
                if (!inRange)
                {
                    angle = 2 * Math.PI - angle;
                }
                
                return angle;
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
                points.Add(_firstPoint);
                points.Add(_secondPoint);
                points.Add(_arcPoint);
                
                if (_textReferencePoint != null)
                {
                    points.Add(_textReferencePoint);
                }

                // 计算圆弧半径
                double radius = (_arcPoint - _centerPoint).length;
                
                // 添加圆弧的极值点（如果需要）
                double startAngle = Math.Atan2((_firstPoint - _centerPoint).Y, (_firstPoint - _centerPoint).X);
                double endAngle = Math.Atan2((_secondPoint - _centerPoint).Y, (_secondPoint - _centerPoint).X);
                
                // 检查是否包含0°、90°、180°、270°
                CheckAndAddCardinalPoint(points, _centerPoint, radius, startAngle, endAngle, 0);
                CheckAndAddCardinalPoint(points, _centerPoint, radius, startAngle, endAngle, Math.PI / 2);
                CheckAndAddCardinalPoint(points, _centerPoint, radius, startAngle, endAngle, Math.PI);
                CheckAndAddCardinalPoint(points, _centerPoint, radius, startAngle, endAngle, 3 * Math.PI / 2);

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
        /// 创建三点角度标注
        /// </summary>
        public Angular3PointDimension() : base(DimensionType.Angular3Point)
        {
        }

        /// <summary>
        /// 创建三点角度标注
        /// </summary>
        /// <param name="center">角度顶点</param>
        /// <param name="first">第一个角度点</param>
        /// <param name="second">第二个角度点</param>
        /// <param name="arcPt">圆弧位置点</param>
        public Angular3PointDimension(Vector2 center, Vector2 first, Vector2 second, Vector2 arcPt) 
            : base(DimensionType.Angular3Point)
        {
            _centerPoint = center;
            _firstPoint = first;
            _secondPoint = second;
            _arcPoint = arcPt;
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

            // 计算圆弧半径
            double radius = (_arcPoint - _centerPoint).length;

            // 计算起始和结束角度
            Vector2 dir1 = (_firstPoint - _centerPoint).normalized;
            Vector2 dir2 = (_secondPoint - _centerPoint).normalized;
            double startAngle = Math.Atan2(dir1.Y, dir1.X);
            double endAngle = Math.Atan2(dir2.Y, dir2.X);
            
            // 确保角度按正确方向
            double testAngle = Math.Atan2((_arcPoint - _centerPoint).Y, (_arcPoint - _centerPoint).X);
            if (!IsAngleInRange(testAngle, startAngle, endAngle))
            {
                // 交换起始和结束角度
                double temp = startAngle;
                startAngle = endAngle;
                endAngle = temp;
            }

            // 创建圆弧
            Arc arc = new Arc
            {
                center = _centerPoint,
                radius = radius,
                startAngle = startAngle,
                endAngle = endAngle,
                color = this.color,
                layer = this.layer
            };
            _generatedEntities.Add(arc);

            // 创建延长线
            Vector2 extStart1 = _centerPoint + dir1 * (radius - _style.ExtensionLineExtend);
            Vector2 extEnd1 = _centerPoint + dir1 * (radius + _style.ExtensionLineExtend);
            Line extLine1 = new Line(extStart1, extEnd1)
            {
                color = this.color,
                layer = this.layer
            };
            _generatedEntities.Add(extLine1);

            Vector2 extStart2 = _centerPoint + dir2 * (radius - _style.ExtensionLineExtend);
            Vector2 extEnd2 = _centerPoint + dir2 * (radius + _style.ExtensionLineExtend);
            Line extLine2 = new Line(extStart2, extEnd2)
            {
                color = this.color,
                layer = this.layer
            };
            _generatedEntities.Add(extLine2);

            // 创建箭头
            Vector2 arcStart = _centerPoint + dir1 * radius;
            Vector2 arcEnd = _centerPoint + dir2 * radius;
            
            // 箭头方向（沿圆弧切线）
            Vector2 tangent1 = new Vector2(-dir1.Y, dir1.X);
            Vector2 tangent2 = new Vector2(dir2.Y, -dir2.X);
            
            // 根据角度方向调整切线方向
            if (endAngle < startAngle)
            {
                tangent1 = -tangent1;
                tangent2 = -tangent2;
            }
            
            Solid arrow1 = CreateArrowhead(arcStart, tangent1, _style.ArrowSize);
            Solid arrow2 = CreateArrowhead(arcEnd, tangent2, _style.ArrowSize);
            _generatedEntities.Add(arrow1);
            _generatedEntities.Add(arrow2);

            // 计算文本位置
            if (!_textPositionManuallySet)
            {
                double midAngle = GetMidAngle(startAngle, endAngle);
                Vector2 midDir = new Vector2(Math.Cos(midAngle), Math.Sin(midAngle));
                _textReferencePoint = _centerPoint + midDir * (radius + _style.DimensionLineGap);
            }

            // 创建文本（角度值）
            string text = GetFormattedAngleText();
            Text dimText = CreateDimensionText(_textReferencePoint, text, _style.TextHeight);
            dimText.angle = 0; // 角度标注文本通常保持水平
            dimText.alignment = TextAlignment.CenterBottom;
            _generatedEntities.Add(dimText);
        }

        /// <summary>
        /// 更新标注
        /// </summary>
        protected override void CalculateReferencePoints()
        {
            UpdateDefinitionPoint();
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new Angular3PointDimension();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Angular3PointDimension dimension = base.Clone() as Angular3PointDimension;
            dimension._centerPoint = _centerPoint;
            dimension._firstPoint = _firstPoint;
            dimension._secondPoint = _secondPoint;
            dimension._arcPoint = _arcPoint;
            return dimension;
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            gripPoints.Add(new GripPoint(GripPointType.Center, _centerPoint));
            gripPoints.Add(new GripPoint(GripPointType.End, _firstPoint));
            gripPoints.Add(new GripPoint(GripPointType.End, _secondPoint));
            gripPoints.Add(new GripPoint(GripPointType.Center, _arcPoint));
            
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
                case 0: // 中心点
                    {
                        Vector2 delta = newPosition - _centerPoint;
                        _centerPoint = newPosition;
                        _firstPoint += delta;
                        _secondPoint += delta;
                        _arcPoint += delta;
                    }
                    break;
                case 1: // 第一个角度点
                    _firstPoint = newPosition;
                    break;
                case 2: // 第二个角度点
                    _secondPoint = newPosition;
                    break;
                case 3: // 圆弧位置点
                    _arcPoint = newPosition;
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
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _firstPoint));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _secondPoint));
            
            // 圆弧端点
            double radius = (_arcPoint - _centerPoint).length;
            Vector2 dir1 = (_firstPoint - _centerPoint).normalized;
            Vector2 dir2 = (_secondPoint - _centerPoint).normalized;
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _centerPoint + dir1 * radius));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _centerPoint + dir2 * radius));
            
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
            _firstPoint += translation;
            _secondPoint += translation;
            _arcPoint += translation;
        }

        /// <summary>
        /// 子类特定的旋转操作
        /// </summary>
        protected override void RotateSpecific(Vector2 center, double angle)
        {
            _centerPoint = Vector2.RotateInRadian(_centerPoint, center, angle);
            _firstPoint = Vector2.RotateInRadian(_firstPoint, center, angle);
            _secondPoint = Vector2.RotateInRadian(_secondPoint, center, angle);
            _arcPoint = Vector2.RotateInRadian(_arcPoint, center, angle);
        }

        /// <summary>
        /// 子类特定的变换操作
        /// </summary>
        protected override void TransformBySpecific(Matrix3 transform)
        {
            _centerPoint = transform * _centerPoint;
            _firstPoint = transform * _firstPoint;
            _secondPoint = transform * _secondPoint;
            _arcPoint = transform * _arcPoint;
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
        /// 获取格式化的角度文本
        /// </summary>
        private string GetFormattedAngleText()
        {
            double angleDegrees = measurement * 180.0 / Math.PI;
            
            if (!string.IsNullOrEmpty(_userText))
            {
                // 替换<>为实际角度值
                return _userText.Replace("<>", FormatAngleMeasurement(angleDegrees));
            }
            return FormatAngleMeasurement(angleDegrees);
        }

        /// <summary>
        /// 格式化角度测量值
        /// </summary>
        private string FormatAngleMeasurement(double degrees)
        {
            if (_style != null)
            {
                return degrees.ToString(_style.DecimalFormat) + "°";
            }
            return degrees.ToString("F1") + "°";
        }

        /// <summary>
        /// 检查角度是否在范围内
        /// </summary>
        private bool IsAngleInRange(double angle, double start, double end)
        {
            // 归一化角度到[0, 2π]
            while (angle < 0) angle += 2 * Math.PI;
            while (angle >= 2 * Math.PI) angle -= 2 * Math.PI;
            while (start < 0) start += 2 * Math.PI;
            while (start >= 2 * Math.PI) start -= 2 * Math.PI;
            while (end < 0) end += 2 * Math.PI;
            while (end >= 2 * Math.PI) end -= 2 * Math.PI;
            
            if (start < end)
            {
                return angle >= start && angle <= end;
            }
            else
            {
                return angle >= start || angle <= end;
            }
        }

        /// <summary>
        /// 获取中间角度
        /// </summary>
        private double GetMidAngle(double start, double end)
        {
            if (end < start)
            {
                end += 2 * Math.PI;
            }
            double mid = (start + end) / 2;
            while (mid >= 2 * Math.PI) mid -= 2 * Math.PI;
            return mid;
        }

        /// <summary>
        /// 检查并添加基本方向点
        /// </summary>
        private void CheckAndAddCardinalPoint(List<Vector2> points, Vector2 center, double radius, double start, double end, double angle)
        {
            if (IsAngleInRange(angle, start, end))
            {
                points.Add(center + new Vector2(Math.Cos(angle), Math.Sin(angle)) * radius);
            }
        }

        #endregion
    }
}