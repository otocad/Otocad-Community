using System;
using System.Collections.Generic;
using LitMath;
using lcdb.Optical;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 标准直线实体
    /// 原生OtoCAD实现，不依赖外部�?
    /// </summary>
    [Serializable]
    public class Line : Entity
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className
        {
            get { return "Line"; }
        }

        /// <summary>
        /// 起点
        /// </summary>
        private Vector2 _startPoint = new Vector2();
        public Vector2 startPoint
        {
            get { return _startPoint; }
            set { _startPoint = value; }
        }

        /// <summary>
        /// 终点
        /// </summary>
        private Vector2 _endPoint = new Vector2();
        public Vector2 endPoint
        {
            get { return _endPoint; }
            set { _endPoint = value; }
        }

        /// <summary>
        /// 线段厚度
        /// </summary>
        public double thickness { get; set; } = 0.0;

        /// <summary>
        /// 线段长度
        /// </summary>
        public double length
        {
            get { return (_endPoint - _startPoint).length; }
        }

        /// <summary>
        /// 线段方向向量（单位向量）
        /// </summary>
        public Vector2 direction
        {
            get 
            { 
                Vector2 dir = _endPoint - _startPoint;
                return dir.normalized;
            }
        }

        /// <summary>
        /// 线段中点
        /// </summary>
        public Vector2 midPoint
        {
            get { return (_startPoint + _endPoint) * 0.5; }
        }

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                return new Bounding(_startPoint, _endPoint);
            }
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
        /// 构造函�?
        /// </summary>
        public Line()
        {
        }

        /// <summary>
        /// 构造函�?- 指定起点和终�?
        /// </summary>
        public Line(Vector2 startPnt, Vector2 endPnt)
        {
            _startPoint = startPnt;
            _endPoint = endPnt;
        }

        /// <summary>
        /// 构造函数 - 指定起点、终点和来源
        /// </summary>
        public Line(Vector2 startPnt, Vector2 endPnt, EntitySource source, ObjectId? parentId = null)
            : this(startPnt, endPnt)
        {
            this.Source = source;
            this.ParentComponentId = parentId;
        }

        /// <summary>
        /// 构造函数 - 指定坐标
        /// </summary>
        public Line(double x1, double y1, double x2, double y2)
        {
            _startPoint = new Vector2(x1, y1);
            _endPoint = new Vector2(x2, y2);
        }

        /// <summary>
        /// 绘制函数 — 尊重 <see cref="Entity.lineType"/> (GB/T 13323-2009: 光轴=双点画线).
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            var prev = gd.CurrentLineType;
            var prevColor = gd.CurrentColor;
            var lt = this.lineType;
            // ByLayer / ByBlock — 暂不解析图层, 退化为 Solid (光学透镜目前不靠图层定义线型)
            if (lt == LineType.ByLayer || lt == LineType.ByBlock) lt = LineType.Solid;
            try
            {
                gd.CurrentLineType = lt;
                // 仅当自身显式指定 RGB 时覆盖颜色, 否则沿用外部默认 (ByLayer 不在此解析)
                if (color.colorMethod == Colors.ColorMethod.ByColor
                    || color.colorMethod == Colors.ColorMethod.ByEntity)
                    gd.CurrentColor = color.ToDrawingColor();
                gd.DrawLine(_startPoint, _endPoint);
            }
            finally
            {
                gd.CurrentLineType = prev;
                gd.CurrentColor = prevColor;
            }
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Line line = base.Clone() as Line;
            line._startPoint = _startPoint;
            line._endPoint = _endPoint;
            line.thickness = thickness;
            line._opticType = _opticType;
            line._properties = new Dictionary<string, object>(_properties);
            return line;
        }

        protected override DBObject CreateInstance()
        {
            return new Line();
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            _startPoint += translation;
            _endPoint += translation;
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            _startPoint = Vector2.RotateInRadian(_startPoint, center, angle);
            _endPoint = Vector2.RotateInRadian(_endPoint, center, angle);
        }

        /// <summary>
        /// 变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            _startPoint = transform * _startPoint;
            _endPoint = transform * _endPoint;
        }

        /// <summary>
        /// 对象捕捉�?
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPnts = new List<ObjectSnapPoint>();
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.End, _startPoint));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.End, _endPoint));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, midPoint));
            return snapPnts;
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPnts = new List<GripPoint>();
            gripPnts.Add(new GripPoint(GripPointType.End, _startPoint));
            gripPnts.Add(new GripPoint(GripPointType.End, _endPoint));
            gripPnts.Add(new GripPoint(GripPointType.Mid, midPoint));
            return gripPnts;
        }

        /// <summary>
        /// 设置夹点位置
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            switch (index)
            {
                case 0:
                    _startPoint = newPosition;
                    break;
                case 1:
                    _endPoint = newPosition;
                    break;
                case 2:
                    Vector2 translation = newPosition - midPoint;
                    _startPoint += translation;
                    _endPoint += translation;
                    break;
            }
        }

        /// <summary>
        /// 点到直线的距�?
        /// </summary>
        public double DistanceToPoint(Vector2 point)
        {
            Vector2 lineVec = _endPoint - _startPoint;
            Vector2 pointVec = point - _startPoint;
            
            double lineLength = lineVec.length;
            if (lineLength < 1e-10)
                return pointVec.length;
                
            double t = Vector2.Dot(pointVec, lineVec) / (lineLength * lineLength);
            
            if (t < 0.0)
                return (point - _startPoint).length;
            else if (t > 1.0)
                return (point - _endPoint).length;
            else
            {
                Vector2 projection = _startPoint + lineVec * t;
                return (point - projection).length;
            }
        }

        /// <summary>
        /// 获取线段上最近的�?
        /// </summary>
        public Vector2 GetClosestPoint(Vector2 point)
        {
            Vector2 lineVec = _endPoint - _startPoint;
            Vector2 pointVec = point - _startPoint;
            
            double lineLength = lineVec.length;
            if (lineLength < 1e-10)
                return _startPoint;
                
            double t = Vector2.Dot(pointVec, lineVec) / (lineLength * lineLength);
            t = Math.Max(0.0, Math.Min(1.0, t));
            
            return _startPoint + lineVec * t;
        }

        /// <summary>
        /// 判断点是否在线段�?
        /// </summary>
        public bool IsPointOnLine(Vector2 point, double tolerance = 1e-6)
        {
            return DistanceToPoint(point) <= tolerance;
        }

        /// <summary>
        /// 延长线段
        /// </summary>
        public void Extend(double startExtension, double endExtension)
        {
            Vector2 dir = direction;
            _startPoint -= dir * startExtension;
            _endPoint += dir * endExtension;
        }

        /// <summary>
        /// 字符串表�?
        /// </summary>
        public override string ToString()
        {
            return string.Format("Line: ({0:F3}, {1:F3}) to ({2:F3}, {3:F3})", 
                _startPoint.X, _startPoint.Y, _endPoint.X, _endPoint.Y);
        }
        
        /// <summary>
        /// 写XML
        /// </summary>
        
        /// <summary>
        /// 读XML
        /// </summary>
    }
}