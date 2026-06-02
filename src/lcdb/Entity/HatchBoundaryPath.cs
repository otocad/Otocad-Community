using System;
using System.Collections.Generic;
using System.Linq;
using LitMath;

namespace lcdb
{
    /// <summary>
    /// 填充边界路径类型标志
    /// </summary>
    [Flags]
    public enum HatchBoundaryPathTypeFlags
    {
        /// <summary>
        /// 默认
        /// </summary>
        Default = 0,

        /// <summary>
        /// 外部边界
        /// </summary>
        External = 1,

        /// <summary>
        /// 多段线边界
        /// </summary>
        Polyline = 2,

        /// <summary>
        /// 派生边界
        /// </summary>
        Derived = 4,

        /// <summary>
        /// 文本框边界
        /// </summary>
        Textbox = 8,

        /// <summary>
        /// 最外层边界
        /// </summary>
        Outermost = 16
    }

    /// <summary>
    /// 填充边界路径
    /// </summary>
    /// <remarks>
    /// 表示填充的边界循环。边界可以由线条、轻量级多段线、多段线、圆、弧、椭圆和样条曲线的任意组合组成。
    /// 定义循环的实体应该定义一个封闭路径，它们必须与填充在同一平面上，并具有相同的法向；
    /// 如果不满足这些条件，结果可能无法预测。
    /// </remarks>
    public class HatchBoundaryPath : ICloneable
    {
        #region 边界路径边类型

        /// <summary>
        /// 边界路径边类型
        /// </summary>
        public enum EdgeType
        {
            /// <summary>
            /// 多段线
            /// </summary>
            Polyline = 0,

            /// <summary>
            /// 直线
            /// </summary>
            Line = 1,

            /// <summary>
            /// 圆弧
            /// </summary>
            Arc = 2,

            /// <summary>
            /// 椭圆
            /// </summary>
            Ellipse = 3,

            /// <summary>
            /// 样条曲线
            /// </summary>
            Spline = 4
        }

        /// <summary>
        /// 边界路径边基类
        /// </summary>
        public abstract class Edge : ICloneable
        {
            /// <summary>
            /// 获取边类型
            /// </summary>
            public readonly EdgeType Type;

            protected Edge(EdgeType type)
            {
                Type = type;
            }

            /// <summary>
            /// 转换为对应的实体对象
            /// </summary>
            /// <returns>等效的实体对象</returns>
            public abstract Entity ConvertToEntity();

            /// <summary>
            /// 克隆边对象
            /// </summary>
            /// <returns>边的副本</returns>
            public abstract object Clone();

            /// <summary>
            /// 获取边的起点
            /// </summary>
            /// <returns>起点坐标</returns>
            public abstract Vector2 GetStartPoint();

            /// <summary>
            /// 获取边的终点
            /// </summary>
            /// <returns>终点坐标</returns>
            public abstract Vector2 GetEndPoint();
        }

        /// <summary>
        /// 直线边
        /// </summary>
        public class LineEdge : Edge
        {
            public Vector2 StartPoint { get; set; }
            public Vector2 EndPoint { get; set; }
            
            // 便利属性
            public Vector2 start 
            { 
                get { return StartPoint; }
                set { StartPoint = value; }
            }
            
            public Vector2 end
            {
                get { return EndPoint; }
                set { EndPoint = value; }
            }

            public LineEdge() : base(EdgeType.Line)
            {
                StartPoint = Vector2.Zero;
                EndPoint = Vector2.Zero;
            }

            public LineEdge(Vector2 startPoint, Vector2 endPoint) : base(EdgeType.Line)
            {
                StartPoint = startPoint;
                EndPoint = endPoint;
            }

            public override Entity ConvertToEntity()
            {
                var line = new lcdb.Line();
                line.startPoint = StartPoint;
                line.endPoint = EndPoint;
                return line;
            }

            public override object Clone()
            {
                return new LineEdge(StartPoint, EndPoint);
            }

            public override Vector2 GetStartPoint() => StartPoint;
            public override Vector2 GetEndPoint() => EndPoint;
        }

        /// <summary>
        /// 圆弧边
        /// </summary>
        public class ArcEdge : Edge
        {
            public Vector2 Center { get; set; }
            public double Radius { get; set; }
            public double StartAngle { get; set; }
            public double EndAngle { get; set; }
            public bool IsCounterclockwise { get; set; }
            
            // 便利属性
            public Vector2 center 
            { 
                get { return Center; }
                set { Center = value; }
            }
            
            public double radius
            {
                get { return Radius; }
                set { Radius = value; }
            }
            
            public double startAngle
            {
                get { return StartAngle; }
                set { StartAngle = value; }
            }
            
            public double endAngle
            {
                get { return EndAngle; }
                set { EndAngle = value; }
            }
            
            public bool isCounterClockwise
            {
                get { return IsCounterclockwise; }
                set { IsCounterclockwise = value; }
            }

            public ArcEdge() : base(EdgeType.Arc)
            {
                Center = Vector2.Zero;
                Radius = 1.0;
                StartAngle = 0.0;
                EndAngle = Math.PI;
                IsCounterclockwise = true;
            }

            public ArcEdge(Vector2 center, double radius, double startAngle, double endAngle, bool counterclockwise = true) 
                : base(EdgeType.Arc)
            {
                Center = center;
                Radius = radius;
                StartAngle = startAngle;
                EndAngle = endAngle;
                IsCounterclockwise = counterclockwise;
            }

            public override Entity ConvertToEntity()
            {
                var arc = new lcdb.Arc(Center, Radius, StartAngle, EndAngle);
                return arc;
            }

            public override object Clone()
            {
                return new ArcEdge(Center, Radius, StartAngle, EndAngle, IsCounterclockwise);
            }

            public override Vector2 GetStartPoint()
            {
                return Center + new Vector2(Radius * Math.Cos(StartAngle), Radius * Math.Sin(StartAngle));
            }

            public override Vector2 GetEndPoint()
            {
                return Center + new Vector2(Radius * Math.Cos(EndAngle), Radius * Math.Sin(EndAngle));
            }
        }

        /// <summary>
        /// 多段线边
        /// </summary>
        public class PolylineEdge : Edge
        {
            public List<Vector2> Vertices { get; set; }
            public List<double> Bulges { get; set; }
            public bool IsClosed { get; set; }

            public PolylineEdge() : base(EdgeType.Polyline)
            {
                Vertices = new List<Vector2>();
                Bulges = new List<double>();
                IsClosed = false;
            }

            public PolylineEdge(IEnumerable<Vector2> vertices, bool isClosed = true) : base(EdgeType.Polyline)
            {
                Vertices = new List<Vector2>(vertices);
                Bulges = new List<double>(new double[Vertices.Count]);
                IsClosed = isClosed;
            }

            public override Entity ConvertToEntity()
            {
                var polyline = new Polyline();
                for (int i = 0; i < Vertices.Count; i++)
                {
                    double bulge = i < Bulges.Count ? Bulges[i] : 0.0;
                    polyline.AddVertexAt(Vertices[i], bulge);
                }
                polyline.IsClosed = IsClosed;
                return polyline;
            }

            public override object Clone()
            {
                var cloned = new PolylineEdge(Vertices, IsClosed);
                cloned.Bulges = new List<double>(Bulges);
                return cloned;
            }

            public override Vector2 GetStartPoint()
            {
                return Vertices.Count > 0 ? Vertices[0] : Vector2.Zero;
            }

            public override Vector2 GetEndPoint()
            {
                return Vertices.Count > 0 ? Vertices[Vertices.Count - 1] : Vector2.Zero;
            }
        }
        
        /// <summary>
        /// 椭圆边
        /// </summary>
        public class EllipseEdge : Edge
        {
            public Vector2 Center { get; set; }
            public Vector2 MajorAxis { get; set; }
            public double MinorAxisRatio { get; set; }
            public double StartAngle { get; set; }
            public double EndAngle { get; set; }
            public bool IsCounterclockwise { get; set; }
            
            // 便利属性
            public Vector2 center 
            { 
                get { return Center; }
                set { Center = value; }
            }
            
            public Vector2 majorAxis
            {
                get { return MajorAxis; }
                set { MajorAxis = value; }
            }
            
            public double minorAxisRatio
            {
                get { return MinorAxisRatio; }
                set { MinorAxisRatio = value; }
            }
            
            public double startAngle
            {
                get { return StartAngle; }
                set { StartAngle = value; }
            }
            
            public double endAngle
            {
                get { return EndAngle; }
                set { EndAngle = value; }
            }
            
            public bool isCounterClockwise
            {
                get { return IsCounterclockwise; }
                set { IsCounterclockwise = value; }
            }

            public EllipseEdge() : base(EdgeType.Ellipse)
            {
                Center = Vector2.Zero;
                MajorAxis = new Vector2(1, 0);
                MinorAxisRatio = 1.0;
                StartAngle = 0;
                EndAngle = 2 * Math.PI;
                IsCounterclockwise = true;
            }

            public override Entity ConvertToEntity()
            {
                double majorAxisLength = MajorAxis.length;
                double minorAxisLength = majorAxisLength * MinorAxisRatio;
                double rotation = Math.Atan2(MajorAxis.Y, MajorAxis.X);
                
                return new lcdb.Ellipse
                {
                    center = Center,
                    radiusX = majorAxisLength,
                    radiusY = minorAxisLength,
                    rotation = rotation,
                    startAngle = StartAngle,
                    endAngle = EndAngle
                };
            }

            public override object Clone()
            {
                return new EllipseEdge
                {
                    Center = Center,
                    MajorAxis = MajorAxis,
                    MinorAxisRatio = MinorAxisRatio,
                    StartAngle = StartAngle,
                    EndAngle = EndAngle,
                    IsCounterclockwise = IsCounterclockwise
                };
            }

            public override Vector2 GetStartPoint()
            {
                double cos = Math.Cos(StartAngle);
                double sin = Math.Sin(StartAngle);
                Vector2 point = new Vector2(cos, sin * MinorAxisRatio);
                
                // Rotate by major axis angle
                double rotation = Math.Atan2(MajorAxis.Y, MajorAxis.X);
                point = Vector2.Rotate(point * MajorAxis.length, rotation);
                
                return Center + point;
            }

            public override Vector2 GetEndPoint()
            {
                double cos = Math.Cos(EndAngle);
                double sin = Math.Sin(EndAngle);
                Vector2 point = new Vector2(cos, sin * MinorAxisRatio);
                
                // Rotate by major axis angle
                double rotation = Math.Atan2(MajorAxis.Y, MajorAxis.X);
                point = Vector2.Rotate(point * MajorAxis.length, rotation);
                
                return Center + point;
            }
        }
        
        /// <summary>
        /// Line 类型别名，用于兼容旧代码
        /// </summary>
        public class Line : LineEdge
        {
            public Line() : base() { }
            public Line(Vector2 startPoint, Vector2 endPoint) : base(startPoint, endPoint) { }
        }
        
        /// <summary>
        /// Arc 类型别名，用于兼容旧代码
        /// </summary>
        public class Arc : ArcEdge
        {
            public Arc() : base() { }
            public Arc(Vector2 center, double radius, double startAngle, double endAngle, bool counterclockwise = true) 
                : base(center, radius, startAngle, endAngle, counterclockwise) { }
        }
        
        /// <summary>
        /// Ellipse 类型别名，用于兼容旧代码
        /// </summary>
        public class Ellipse : EllipseEdge
        {
            public Ellipse() : base() { }
        }

        #endregion

        #region 私有字段

        private List<Edge> _edges;
        private List<Entity> _pathEntities;
        private HatchBoundaryPathTypeFlags _pathTypeFlags;

        #endregion

        #region 属性

        /// <summary>
        /// 边界路径的边集合
        /// </summary>
        public List<Edge> Edges
        {
            get { return _edges; }
            set { _edges = value ?? new List<Edge>(); }
        }
        
        /// <summary>
        /// 边界路径的边集合（便利属性）
        /// </summary>
        public List<Edge> edges
        {
            get { return _edges; }
            set { _edges = value ?? new List<Edge>(); }
        }

        /// <summary>
        /// 路径实体集合（关联边界）
        /// </summary>
        public List<Entity> PathEntities
        {
            get { return _pathEntities; }
            set { _pathEntities = value ?? new List<Entity>(); }
        }

        /// <summary>
        /// 路径类型标志
        /// </summary>
        public HatchBoundaryPathTypeFlags PathTypeFlags
        {
            get { return _pathTypeFlags; }
            set { _pathTypeFlags = value; }
        }

        /// <summary>
        /// 检查路径是否封闭
        /// </summary>
        public bool IsClosed
        {
            get
            {
                if (_edges.Count < 2)
                    return false;

                var firstStart = _edges[0].GetStartPoint();
                var lastEnd = _edges[_edges.Count - 1].GetEndPoint();
                
                return (firstStart - lastEnd).length < 1e-6;
            }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public HatchBoundaryPath()
        {
            _edges = new List<Edge>();
            _pathEntities = new List<Entity>();
            _pathTypeFlags = HatchBoundaryPathTypeFlags.Default;
        }

        /// <summary>
        /// 从边集合构造
        /// </summary>
        /// <param name="edges">边集合</param>
        public HatchBoundaryPath(IEnumerable<Edge> edges)
        {
            _edges = new List<Edge>(edges);
            _pathEntities = new List<Entity>();
            _pathTypeFlags = HatchBoundaryPathTypeFlags.Default;
        }

        /// <summary>
        /// 从实体集合构造
        /// </summary>
        /// <param name="entities">实体集合</param>
        public HatchBoundaryPath(IEnumerable<Entity> entities)
        {
            _edges = new List<Edge>();
            _pathEntities = new List<Entity>(entities);
            _pathTypeFlags = HatchBoundaryPathTypeFlags.Default;

            // 从实体创建边
            CreateEdgesFromEntities();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 添加边
        /// </summary>
        /// <param name="edge">要添加的边</param>
        public void AddEdge(Edge edge)
        {
            if (edge != null)
            {
                _edges.Add(edge);
            }
        }

        /// <summary>
        /// 添加实体到路径
        /// </summary>
        /// <param name="entity">要添加的实体</param>
        public void AddEntity(Entity entity)
        {
            if (entity != null)
            {
                _pathEntities.Add(entity);
                
                // 根据实体创建对应的边
                var edge = CreateEdgeFromEntity(entity);
                if (edge != null)
                {
                    _edges.Add(edge);
                }
            }
        }

        /// <summary>
        /// 清除轮廓（移除关联实体）
        /// </summary>
        public void ClearContour()
        {
            _pathEntities.Clear();
        }

        /// <summary>
        /// 计算边界路径的边界框
        /// </summary>
        /// <returns>边界框</returns>
        public Bounding GetBounding()
        {
            if (_edges.Count == 0)
                return new Bounding(Vector2.Zero, 0, 0);

            var points = new List<Vector2>();
            
            foreach (var edge in _edges)
            {
                points.Add(edge.GetStartPoint());
                points.Add(edge.GetEndPoint());
                
                // 对于圆弧，还需要考虑边界点
                if (edge is ArcEdge arc)
                {
                    AddArcExtremumPoints(arc, points);
                }
            }

            if (points.Count == 0)
                return new Bounding(Vector2.Zero, 0, 0);

            double minX = points.Min(p => p.X);
            double maxX = points.Max(p => p.X);
            double minY = points.Min(p => p.Y);
            double maxY = points.Max(p => p.Y);

            return new Bounding(
                new Vector2((minX + maxX) / 2, (minY + maxY) / 2),
                maxX - minX,
                maxY - minY
            );
        }

        /// <summary>
        /// 检查点是否在边界路径内部
        /// </summary>
        /// <param name="point">测试点</param>
        /// <returns>true表示在内部</returns>
        public bool ContainsPoint(Vector2 point)
        {
            if (!IsClosed)
                return false;

            // 使用射线投射算法
            int crossings = 0;
            var rayEnd = new Vector2(point.X + 10000, point.Y); // 向右的射线

            foreach (var edge in _edges)
            {
                if (edge is LineEdge line)
                {
                    if (DoesRayIntersectLine(point, rayEnd, line.StartPoint, line.EndPoint))
                        crossings++;
                }
                else if (edge is ArcEdge arc)
                {
                    crossings += CountRayArcIntersections(point, rayEnd, arc);
                }
                // 其他边类型可以根据需要添加
            }

            return (crossings % 2) == 1;
        }

        /// <summary>
        /// 克隆边界路径
        /// </summary>
        /// <returns>路径的副本</returns>
        public object Clone()
        {
            var cloned = new HatchBoundaryPath();
            cloned._edges = _edges.Select(edge => (Edge)edge.Clone()).ToList();
            cloned._pathEntities = new List<Entity>(_pathEntities);
            cloned._pathTypeFlags = _pathTypeFlags;
            return cloned;
        }

        /// <summary>
        /// 转换为实体集合
        /// </summary>
        /// <returns>实体集合</returns>
        public List<Entity> ToEntities()
        {
            return _edges.Select(edge => edge.ConvertToEntity()).ToList();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 从实体创建边
        /// </summary>
        private void CreateEdgesFromEntities()
        {
            _edges.Clear();
            foreach (var entity in _pathEntities)
            {
                var edge = CreateEdgeFromEntity(entity);
                if (edge != null)
                {
                    _edges.Add(edge);
                }
            }
        }

        /// <summary>
        /// 从实体创建边
        /// </summary>
        /// <param name="entity">实体</param>
        /// <returns>对应的边，如果不支持则返回null</returns>
        private Edge CreateEdgeFromEntity(Entity entity)
        {
            switch (entity)
            {
                case lcdb.Line line:
                    return new LineEdge(line.startPoint, line.endPoint);
                    
                case lcdb.Arc arc:
                    return new ArcEdge(arc.center, arc.radius, arc.startAngle, arc.endAngle);
                    
                case Circle circle:
                    return new ArcEdge(circle.center, circle.radius, 0, 2 * Math.PI);
                    
                case Polyline polyline:
                    var vertices = polyline.GetVertices();
                    var bulges = polyline.GetBulges();
                    var polyEdge = new PolylineEdge(vertices, polyline.IsClosed);
                    polyEdge.Bulges = bulges.ToList();
                    return polyEdge;
                    
                default:
                    return null; // 不支持的实体类型
            }
        }

        /// <summary>
        /// 添加圆弧的极值点
        /// </summary>
        private void AddArcExtremumPoints(ArcEdge arc, List<Vector2> points)
        {
            // 检查是否跨越极值角度
            double[] extremumAngles = { 0, Math.PI / 2, Math.PI, 3 * Math.PI / 2 };
            
            foreach (double angle in extremumAngles)
            {
                if (IsAngleInArc(angle, arc.StartAngle, arc.EndAngle, arc.IsCounterclockwise))
                {
                    points.Add(arc.Center + new Vector2(
                        arc.Radius * Math.Cos(angle),
                        arc.Radius * Math.Sin(angle)
                    ));
                }
            }
        }

        /// <summary>
        /// 检查角度是否在圆弧范围内
        /// </summary>
        private bool IsAngleInArc(double angle, double startAngle, double endAngle, bool counterclockwise)
        {
            // 标准化角度到 [0, 2π]
            angle = NormalizeAngle(angle);
            startAngle = NormalizeAngle(startAngle);
            endAngle = NormalizeAngle(endAngle);

            if (counterclockwise)
            {
                return startAngle <= endAngle ? 
                    (angle >= startAngle && angle <= endAngle) :
                    (angle >= startAngle || angle <= endAngle);
            }
            else
            {
                return startAngle >= endAngle ?
                    (angle <= startAngle && angle >= endAngle) :
                    (angle <= startAngle || angle >= endAngle);
            }
        }

        /// <summary>
        /// 标准化角度到 [0, 2π]
        /// </summary>
        private double NormalizeAngle(double angle)
        {
            while (angle < 0) angle += 2 * Math.PI;
            while (angle >= 2 * Math.PI) angle -= 2 * Math.PI;
            return angle;
        }

        /// <summary>
        /// 射线与直线段相交检测
        /// </summary>
        private bool DoesRayIntersectLine(Vector2 rayStart, Vector2 rayEnd, Vector2 lineStart, Vector2 lineEnd)
        {
            // 简化的射线投射算法
            if ((lineStart.Y > rayStart.Y) == (lineEnd.Y > rayStart.Y))
                return false;

            double intersectX = lineStart.X + (rayStart.Y - lineStart.Y) / (lineEnd.Y - lineStart.Y) * (lineEnd.X - lineStart.X);
            return intersectX > rayStart.X;
        }

        /// <summary>
        /// 计算射线与圆弧的交点数量
        /// </summary>
        private int CountRayArcIntersections(Vector2 rayStart, Vector2 rayEnd, ArcEdge arc)
        {
            // 简化实现：将圆弧近似为多个线段
            int segments = 16;
            int crossings = 0;
            
            for (int i = 0; i < segments; i++)
            {
                double t1 = (double)i / segments;
                double t2 = (double)(i + 1) / segments;
                
                double angle1 = arc.StartAngle + t1 * (arc.EndAngle - arc.StartAngle);
                double angle2 = arc.StartAngle + t2 * (arc.EndAngle - arc.StartAngle);
                
                var p1 = arc.Center + new Vector2(arc.Radius * Math.Cos(angle1), arc.Radius * Math.Sin(angle1));
                var p2 = arc.Center + new Vector2(arc.Radius * Math.Cos(angle2), arc.Radius * Math.Sin(angle2));
                
                if (DoesRayIntersectLine(rayStart, rayEnd, p1, p2))
                    crossings++;
            }
            
            return crossings;
        }

        #endregion
    }
}