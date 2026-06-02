using System;
using System.Collections.Generic;
using LitMath;
using lcdb.Optical;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 标准圆弧实体
    /// 原生OtoCAD实现，不依赖外部�?
    /// </summary>
    [Serializable]
    public class Arc : Entity
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className
        {
            get { return "Arc"; }
        }

        /// <summary>
        /// 圆心
        /// </summary>
        private Vector2 _center = new Vector2();
        public Vector2 center
        {
            get { return _center; }
            set { _center = value; }
        }

        /// <summary>
        /// 半径
        /// </summary>
        private double _radius = 1.0;
        public double radius
        {
            get { return _radius; }
            set { _radius = Math.Max(0.0, value); }
        }

        /// <summary>
        /// 起始角度（弧度）
        /// </summary>
        private double _startAngle = 0.0;
        public double startAngle
        {
            get { return _startAngle; }
            set { _startAngle = NormalizeAngle(value); }
        }

        /// <summary>
        /// 结束角度（弧度）
        /// </summary>
        private double _endAngle = Math.PI;
        public double endAngle
        {
            get { return _endAngle; }
            set { _endAngle = NormalizeAngle(value); }
        }

        /// <summary>
        /// 厚度
        /// </summary>
        public double thickness { get; set; } = 0.0;

        /// <summary>
        /// 圆弧角度范围（弧度）
        /// </summary>
        public double angleSpan
        {
            get 
            { 
                double span = _endAngle - _startAngle;
                if (span <= 0)
                    span += 2 * Math.PI;
                return span;
            }
        }

        /// <summary>
        /// 起点
        /// </summary>
        public Vector2 startPoint
        {
            get 
            { 
                return _center + new Vector2(
                    _radius * Math.Cos(_startAngle),
                    _radius * Math.Sin(_startAngle)
                );
            }
        }

        /// <summary>
        /// 终点
        /// </summary>
        public Vector2 endPoint
        {
            get 
            { 
                return _center + new Vector2(
                    _radius * Math.Cos(_endAngle),
                    _radius * Math.Sin(_endAngle)
                );
            }
        }

        /// <summary>
        /// 中点
        /// </summary>
        public Vector2 midPoint
        {
            get 
            { 
                double midAngle = _startAngle + angleSpan * 0.5;
                return _center + new Vector2(
                    _radius * Math.Cos(midAngle),
                    _radius * Math.Sin(midAngle)
                );
            }
        }

        /// <summary>
        /// 圆弧长度
        /// </summary>
        public double length
        {
            get { return _radius * angleSpan; }
        }
        
        /// <summary>
        /// 光学类型
        /// </summary>
        private OpticType _opticType = OpticType.None;
        public OpticType opticType
        {
            get { return _opticType; }
            set { _opticType = value; }
        }
        
        /// <summary>
        /// 光学属性字�?
        /// </summary>
        private Dictionary<string, object> _properties = new Dictionary<string, object>();
        public Dictionary<string, object> properties
        {
            get { return _properties; }
            set { _properties = value ?? new Dictionary<string, object>(); }
        }

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                List<Vector2> boundingPoints = new List<Vector2>();
                boundingPoints.Add(startPoint);
                boundingPoints.Add(endPoint);

                // 检查四个象限点是否在圆弧范围内
                for (double ang = 0; ang < 2 * Math.PI; ang += Math.PI / 2)
                {
                    if (IsAngleInRange(ang))
                    {
                        boundingPoints.Add(_center + new Vector2(
                            _radius * Math.Cos(ang),
                            _radius * Math.Sin(ang)
                        ));
                    }
                }

                double minX = double.MaxValue;
                double minY = double.MaxValue;
                double maxX = double.MinValue;
                double maxY = double.MinValue;

                foreach (Vector2 point in boundingPoints)
                {
                    minX = Math.Min(minX, point.X);
                    minY = Math.Min(minY, point.Y);
                    maxX = Math.Max(maxX, point.X);
                    maxY = Math.Max(maxY, point.Y);
                }

                return new Bounding(new Vector2(minX, minY), new Vector2(maxX, maxY));
            }
        }

        /// <summary>
        /// 构造函�?
        /// </summary>
        public Arc()
        {
        }

        /// <summary>
        /// 构造函�?- 指定圆心、半径和角度
        /// </summary>
        public Arc(Vector2 centerPnt, double radiusValue, double startAng, double endAng)
        {
            _center = centerPnt;
            _radius = radiusValue;
            _startAngle = NormalizeAngle(startAng);
            _endAngle = NormalizeAngle(endAng);
        }

        /// <summary>
        /// 构造函�?- 通过三点创建圆弧
        /// </summary>
        public Arc(Vector2 startPnt, Vector2 midPnt, Vector2 endPnt)
        {
            if (CreateArcFrom3Points(startPnt, midPnt, endPnt))
            {
                // 已在CreateArcFrom3Points中设置了参数
            }
            else
            {
                // 三点共线，创建默认圆�?
                _center = startPnt;
                _radius = 1.0;
                _startAngle = 0.0;
                _endAngle = Math.PI;
            }
        }

        /// <summary>
        /// 构造函数 - 指定圆心、半径、角度和来源
        /// </summary>
        /// <param name="centerPnt">圆心</param>
        /// <param name="radiusValue">半径</param>
        /// <param name="startAng">起始角度</param>
        /// <param name="endAng">结束角度</param>
        /// <param name="source">实体来源</param>
        /// <param name="parentId">父组件ID</param>
        public Arc(Vector2 centerPnt, double radiusValue, double startAng, double endAng, EntitySource source, ObjectId? parentId = null)
            : this(centerPnt, radiusValue, startAng, endAng)
        {
            this.Source = source;
            this.ParentComponentId = parentId;
        }

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            // 仅当自身显式指定 RGB 时覆盖颜色, 否则沿用外部默认 (ByLayer 不在此解析)
            if (color.colorMethod == Colors.ColorMethod.ByColor
                || color.colorMethod == Colors.ColorMethod.ByEntity)
            {
                var prevColor = gd.CurrentColor;
                try
                {
                    gd.CurrentColor = color.ToDrawingColor();
                    gd.DrawArc(_center, _radius, _startAngle, _endAngle);
                }
                finally { gd.CurrentColor = prevColor; }
            }
            else
            {
                gd.DrawArc(_center, _radius, _startAngle, _endAngle);
            }
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Arc arc = base.Clone() as Arc;
            arc._center = _center;
            arc._radius = _radius;
            arc._startAngle = _startAngle;
            arc._endAngle = _endAngle;
            arc.thickness = thickness;
            arc._opticType = _opticType;
            arc._properties = new Dictionary<string, object>(_properties);
            return arc;
        }

        protected override DBObject CreateInstance()
        {
            return new Arc();
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            _center += translation;
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 rotCenter, double angle)
        {
            _center = Vector2.RotateInRadian(_center, rotCenter, angle);
            _startAngle += angle;
            _endAngle += angle;
            _startAngle = NormalizeAngle(_startAngle);
            _endAngle = NormalizeAngle(_endAngle);
        }

        /// <summary>
        /// 变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            _center = transform * _center;
            // 注意：变换可能改变圆弧的形状，这里简化处�?
        }

        /// <summary>
        /// 对象捕捉�?
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPnts = new List<ObjectSnapPoint>();
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.End, startPoint));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.End, endPoint));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, midPoint));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Center, _center));
            return snapPnts;
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPnts = new List<GripPoint>();
            gripPnts.Add(new GripPoint(GripPointType.End, startPoint));
            gripPnts.Add(new GripPoint(GripPointType.End, endPoint));
            gripPnts.Add(new GripPoint(GripPointType.Mid, midPoint));
            gripPnts.Add(new GripPoint(GripPointType.Center, _center));
            return gripPnts;
        }

        /// <summary>
        /// 设置夹点位置
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            switch (index)
            {
                case 0: // 起点
                    {
                        Vector2 vec = newPosition - _center;
                        _radius = vec.length;
                        _startAngle = Math.Atan2(vec.Y, vec.X);
                    }
                    break;
                case 1: // 终点
                    {
                        Vector2 vec = newPosition - _center;
                        _radius = vec.length;
                        _endAngle = Math.Atan2(vec.Y, vec.X);
                    }
                    break;
                case 2: // 中点
                    {
                        Vector2 vec = newPosition - _center;
                        _radius = vec.length;
                    }
                    break;
                case 3: // 圆心
                    _center = newPosition;
                    break;
            }
        }

        /// <summary>
        /// 判断角度是否在圆弧范围内
        /// </summary>
        private bool IsAngleInRange(double angle)
        {
            // 检查是否为完整圆（角度范围接近或等于2π）
            if (Math.Abs(angleSpan - 2 * Math.PI) < 1e-10)
            {
                return true; // 完整圆包含所有角度
            }
            
            angle = NormalizeAngle(angle);
            
            if (_endAngle >= _startAngle)
            {
                return angle >= _startAngle && angle <= _endAngle;
            }
            else
            {
                // 跨越0度的情况
                return angle >= _startAngle || angle <= _endAngle;
            }
        }

        /// <summary>
        /// 角度标准化到 [0, 2π) 范围
        /// </summary>
        private static double NormalizeAngle(double angle)
        {
            while (angle < 0)
                angle += 2 * Math.PI;
            while (angle >= 2 * Math.PI)
                angle -= 2 * Math.PI;
            return angle;
        }

        /// <summary>
        /// 通过三点创建圆弧
        /// </summary>
        private bool CreateArcFrom3Points(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            // 计算外接圆圆�?
            double d = 2 * (p1.X * (p2.Y - p3.Y) + p2.X * (p3.Y - p1.Y) + p3.X * (p1.Y - p2.Y));
            if (Math.Abs(d) < 1e-10)
                return false; // 三点共线

            double ux = ((p1.X * p1.X + p1.Y * p1.Y) * (p2.Y - p3.Y) + 
                         (p2.X * p2.X + p2.Y * p2.Y) * (p3.Y - p1.Y) + 
                         (p3.X * p3.X + p3.Y * p3.Y) * (p1.Y - p2.Y)) / d;
            
            double uy = ((p1.X * p1.X + p1.Y * p1.Y) * (p3.X - p2.X) + 
                         (p2.X * p2.X + p2.Y * p2.Y) * (p1.X - p3.X) + 
                         (p3.X * p3.X + p3.Y * p3.Y) * (p2.X - p1.X)) / d;

            _center = new Vector2(ux, uy);
            _radius = (_center - p1).length;
            
            _startAngle = Math.Atan2(p1.Y - _center.Y, p1.X - _center.X);
            _endAngle = Math.Atan2(p3.Y - _center.Y, p3.X - _center.X);
            
            return true;
        }

        /// <summary>
        /// 点到圆弧的距�?
        /// </summary>
        public double DistanceToPoint(Vector2 point)
        {
            double distToCenter = (_center - point).length;
            
            Vector2 vec = point - _center;
            double angle = Math.Atan2(vec.Y, vec.X);
            
            if (IsAngleInRange(angle))
            {
                return Math.Abs(distToCenter - _radius);
            }
            else
            {
                double distToStart = (point - startPoint).length;
                double distToEnd = (point - endPoint).length;
                return Math.Min(distToStart, distToEnd);
            }
        }

        /// <summary>
        /// 获取圆弧上最近的�?
        /// </summary>
        public Vector2 GetClosestPoint(Vector2 point)
        {
            Vector2 vec = point - _center;
            double angle = Math.Atan2(vec.Y, vec.X);
            
            if (IsAngleInRange(angle))
            {
                return _center + new Vector2(
                    _radius * Math.Cos(angle),
                    _radius * Math.Sin(angle)
                );
            }
            else
            {
                double distToStart = (point - startPoint).length;
                double distToEnd = (point - endPoint).length;
                return distToStart < distToEnd ? startPoint : endPoint;
            }
        }

        /// <summary>
        /// 字符串表�?
        /// </summary>
        public override string ToString()
        {
            return string.Format("Arc: Center({0:F3}, {1:F3}) Radius={2:F3} Start={3:F1}° End={4:F1}°", 
                _center.X, _center.Y, _radius, 
                _startAngle * 180.0 / Math.PI, _endAngle * 180.0 / Math.PI);
        }
        
        /// <summary>
        /// 写XML
        /// </summary>
        
        /// <summary>
        /// 读XML
        /// </summary>
    }
}