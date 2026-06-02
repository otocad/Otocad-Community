using System;
using System.Collections.Generic;
using LitMath;


namespace lcdb
{
    /// <summary>
    /// 角度标注实体（两线角度）
    /// 原生OtoCAD实现，用于标注两条线之间的角度
    /// </summary>
    [Serializable]
    public class Angular2LineDimension : DimensionBase
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "Angular2LineDimension";

        #region 字段

        // 第一条线的起点
        private Vector2 _line1Start = new Vector2();
        
        // 第一条线的终点
        private Vector2 _line1End = new Vector2();
        
        // 第二条线的起点
        private Vector2 _line2Start = new Vector2();
        
        // 第二条线的终点
        private Vector2 _line2End = new Vector2();
        
        // 圆弧位置点（定义圆弧半径）
        private Vector2 _arcPoint = new Vector2();

        #endregion

        #region 属性

        /// <summary>
        /// 第一条线的起点
        /// </summary>
        public Vector2 line1Start
        {
            get { return _line1Start; }
            set 
            { 
                _line1Start = value;
                UpdateDefinitionPoint();
            }
        }

        /// <summary>
        /// 第一条线的终点
        /// </summary>
        public Vector2 line1End
        {
            get { return _line1End; }
            set 
            { 
                _line1End = value;
                UpdateDefinitionPoint();
            }
        }

        /// <summary>
        /// 第二条线的起点
        /// </summary>
        public Vector2 line2Start
        {
            get { return _line2Start; }
            set 
            { 
                _line2Start = value;
                UpdateDefinitionPoint();
            }
        }

        /// <summary>
        /// 第二条线的终点
        /// </summary>
        public Vector2 line2End
        {
            get { return _line2End; }
            set 
            { 
                _line2End = value;
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
        /// 第一条线起点（便利属性）
        /// </summary>
        public Vector2 firstStart
        {
            get { return _line1Start; }
            set 
            { 
                _line1Start = value;
                UpdateDefinitionPoint();
            }
        }
        
        /// <summary>
        /// 第一条线终点（便利属性）
        /// </summary>
        public Vector2 firstEnd
        {
            get { return _line1End; }
            set 
            { 
                _line1End = value;
                UpdateDefinitionPoint();
            }
        }
        
        /// <summary>
        /// 第二条线起点（便利属性）
        /// </summary>
        public Vector2 secondStart
        {
            get { return _line2Start; }
            set 
            { 
                _line2Start = value;
                UpdateDefinitionPoint();
            }
        }
        
        /// <summary>
        /// 第二条线终点（便利属性）
        /// </summary>
        public Vector2 secondEnd
        {
            get { return _line2End; }
            set 
            { 
                _line2End = value;
                UpdateDefinitionPoint();
            }
        }
        
        /// <summary>
        /// 中心点（便利属性，计算得出）
        /// </summary>
        public Vector2 center
        {
            get 
            { 
                // 计算两条线的交点
                var intersection = GetLinesIntersection(_line1Start, _line1End, _line2Start, _line2End);
                return intersection.HasValue ? intersection.Value : new Vector2();
            }
            set 
            { 
                // 中心点是计算属性，设置无效
            }
        }
        
        /// <summary>
        /// 尺寸圆弧（便利属性）
        /// </summary>
        public Vector2 dimArc
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
                // 计算两条线的方向向量
                Vector2 dir1 = (_line1End - _line1Start).normalized;
                Vector2 dir2 = (_line2End - _line2Start).normalized;
                
                // 计算角度
                double angle = Math.Acos(Vector2.Dot(dir1, dir2));
                
                // 确定角度方向
                double cross = dir1.X * dir2.Y - dir1.Y * dir2.X;
                if (cross < 0)
                    angle = 2 * Math.PI - angle;
                
                return angle;
            }
        }

        /// <summary>
        /// 获取两线交点
        /// </summary>
        public Vector2? intersectionPoint
        {
            get
            {
                return GetLinesIntersection(_line1Start, _line1End, _line2Start, _line2End);
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
                points.Add(_line1Start);
                points.Add(_line1End);
                points.Add(_line2Start);
                points.Add(_line2End);
                points.Add(_arcPoint);
                
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
        /// 创建角度标注
        /// </summary>
        public Angular2LineDimension() : base(DimensionType.Angular2Line)
        {
        }

        /// <summary>
        /// 创建角度标注
        /// </summary>
        public Angular2LineDimension(Vector2 l1Start, Vector2 l1End, Vector2 l2Start, Vector2 l2End, Vector2 arcPt) 
            : base(DimensionType.Angular2Line)
        {
            _line1Start = l1Start;
            _line1End = l1End;
            _line2Start = l2Start;
            _line2End = l2End;
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

            // 获取交点
            Vector2? intersection = intersectionPoint;
            if (!intersection.HasValue)
                return; // 两线平行，无法标注

            Vector2 center = intersection.Value;

            // 计算圆弧半径
            double radius = (_arcPoint - center).length;

            // 计算两条线的方向向量
            Vector2 dir1 = (_line1End - _line1Start).normalized;
            Vector2 dir2 = (_line2End - _line2Start).normalized;

            // 计算起始和结束角度
            Vector2 arcDir = (_arcPoint - center).normalized;
            double startAngle, endAngle;
            
            // 确定哪条线是起始线
            Vector2 toArc = arcDir;
            double dot1 = Vector2.Dot(dir1, toArc);
            double dot2 = Vector2.Dot(dir2, toArc);
            
            if (Math.Abs(dot1) > Math.Abs(dot2))
            {
                startAngle = Math.Atan2(dir2.Y, dir2.X);
                endAngle = Math.Atan2(dir1.Y, dir1.X);
            }
            else
            {
                startAngle = Math.Atan2(dir1.Y, dir1.X);
                endAngle = Math.Atan2(dir2.Y, dir2.X);
            }

            // 确保角度在正确的方向
            if (endAngle < startAngle)
                endAngle += 2 * Math.PI;

            // 创建圆弧
            Arc arc = new Arc
            {
                center = center,
                radius = radius,
                startAngle = startAngle,
                endAngle = endAngle,
                color = this.color,
                layer = this.layer
            };
            _generatedEntities.Add(arc);

            // 创建延长线
            Vector2 extStart1 = center + dir1 * (radius - _style.ExtensionLineExtend);
            Vector2 extEnd1 = center + dir1 * (radius + _style.ExtensionLineExtend);
            Line extLine1 = new Line(extStart1, extEnd1)
            {
                color = this.color,
                layer = this.layer
            };
            _generatedEntities.Add(extLine1);

            Vector2 extStart2 = center + dir2 * (radius - _style.ExtensionLineExtend);
            Vector2 extEnd2 = center + dir2 * (radius + _style.ExtensionLineExtend);
            Line extLine2 = new Line(extStart2, extEnd2)
            {
                color = this.color,
                layer = this.layer
            };
            _generatedEntities.Add(extLine2);

            // 创建箭头
            double midAngle = (startAngle + endAngle) / 2;
            Vector2 arcStart = center + new Vector2(Math.Cos(startAngle), Math.Sin(startAngle)) * radius;
            Vector2 arcEnd = center + new Vector2(Math.Cos(endAngle), Math.Sin(endAngle)) * radius;
            
            // 箭头方向（沿圆弧切线）
            Vector2 tangent1 = new Vector2(-Math.Sin(startAngle), Math.Cos(startAngle));
            Vector2 tangent2 = new Vector2(Math.Sin(endAngle), -Math.Cos(endAngle));
            
            Solid arrow1 = CreateArrowhead(arcStart, tangent1, _style.ArrowSize);
            Solid arrow2 = CreateArrowhead(arcEnd, tangent2, _style.ArrowSize);
            _generatedEntities.Add(arrow1);
            _generatedEntities.Add(arrow2);

            // 计算文本位置
            if (!_textPositionManuallySet)
            {
                Vector2 midDir = new Vector2(Math.Cos(midAngle), Math.Sin(midAngle));
                _textReferencePoint = center + midDir * (radius + _style.DimensionLineGap);
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
            return new Angular2LineDimension();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Angular2LineDimension dimension = base.Clone() as Angular2LineDimension;
            dimension._line1Start = _line1Start;
            dimension._line1End = _line1End;
            dimension._line2Start = _line2Start;
            dimension._line2End = _line2End;
            dimension._arcPoint = _arcPoint;
            return dimension;
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            gripPoints.Add(new GripPoint(GripPointType.End, _line1Start));
            gripPoints.Add(new GripPoint(GripPointType.End, _line1End));
            gripPoints.Add(new GripPoint(GripPointType.End, _line2Start));
            gripPoints.Add(new GripPoint(GripPointType.End, _line2End));
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
                case 0:
                    _line1Start = newPosition;
                    break;
                case 1:
                    _line1End = newPosition;
                    break;
                case 2:
                    _line2Start = newPosition;
                    break;
                case 3:
                    _line2End = newPosition;
                    break;
                case 4:
                    _arcPoint = newPosition;
                    break;
                case 5:
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
            
            // 线端点
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _line1Start));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _line1End));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _line2Start));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _line2End));
            
            // 交点
            Vector2? intersection = intersectionPoint;
            if (intersection.HasValue)
            {
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Ins, intersection.Value));
            }
            
            return snapPoints;
        }

        #endregion

        #region 重写基类方法

        /// <summary>
        /// 子类特定的平移操作
        /// </summary>
        protected override void TranslateSpecific(Vector2 translation)
        {
            _line1Start += translation;
            _line1End += translation;
            _line2Start += translation;
            _line2End += translation;
            _arcPoint += translation;
        }

        /// <summary>
        /// 子类特定的旋转操作
        /// </summary>
        protected override void RotateSpecific(Vector2 center, double angle)
        {
            _line1Start = Vector2.RotateInRadian(_line1Start, center, angle);
            _line1End = Vector2.RotateInRadian(_line1End, center, angle);
            _line2Start = Vector2.RotateInRadian(_line2Start, center, angle);
            _line2End = Vector2.RotateInRadian(_line2End, center, angle);
            _arcPoint = Vector2.RotateInRadian(_arcPoint, center, angle);
        }

        /// <summary>
        /// 子类特定的变换操作
        /// </summary>
        protected override void TransformBySpecific(Matrix3 transform)
        {
            _line1Start = transform * _line1Start;
            _line1End = transform * _line1End;
            _line2Start = transform * _line2Start;
            _line2End = transform * _line2End;
            _arcPoint = transform * _arcPoint;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 更新定义点
        /// </summary>
        private void UpdateDefinitionPoint()
        {
            Vector2? intersection = intersectionPoint;
            if (intersection.HasValue)
            {
                _definitionPoint = intersection.Value;
            }
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
        /// 计算两条线的交点
        /// </summary>
        private Vector2? GetLinesIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            double x1 = p1.X, y1 = p1.Y;
            double x2 = p2.X, y2 = p2.Y;
            double x3 = p3.X, y3 = p3.Y;
            double x4 = p4.X, y4 = p4.Y;

            double denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            
            if (Math.Abs(denom) < 1e-10)
                return null; // 平行线

            double t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denom;
            
            double x = x1 + t * (x2 - x1);
            double y = y1 + t * (y2 - y1);
            
            return new Vector2(x, y);
        }

        #endregion
    }
}