using System;
using System.Collections.Generic;
using System.Linq;
using LitMath;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 填充实体
    /// </summary>
    /// <remarks>
    /// 填充实体表示由一个或多个边界路径定义的填充区域�?
    /// 填充可以是实体填充、图案填充或渐变填充�?
    /// </remarks>
    public class Hatch : Entity
    {
        #region 私有字段

        private List<HatchBoundaryPath> _boundaryPaths;
        private HatchPattern _pattern;
        private Vector2 _patternOrigin;
        private double _patternAngle;
        private double _patternScale;
        private HatchStyle _hatchStyle;
        private bool _associative;
        private bool _isGradient;
        private Vector3 _normal;
        private double _elevation;

        #endregion

        #region 属�?

        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "Hatch";

        /// <summary>
        /// 边界路径集合
        /// </summary>
        public List<HatchBoundaryPath> BoundaryPaths
        {
            get { return _boundaryPaths; }
            set { _boundaryPaths = value ?? new List<HatchBoundaryPath>(); }
        }
        
        /// <summary>
        /// 边界路径集合（便利属性）
        /// </summary>
        public List<HatchBoundaryPath> boundaryPaths
        {
            get { return _boundaryPaths; }
            set { _boundaryPaths = value ?? new List<HatchBoundaryPath>(); }
        }

        /// <summary>
        /// 填充图案
        /// </summary>
        public HatchPattern Pattern
        {
            get { return _pattern; }
            set { _pattern = value ?? HatchPattern.Solid; }
        }

        /// <summary>
        /// 图案原点
        /// </summary>
        public Vector2 PatternOrigin
        {
            get { return _patternOrigin; }
            set { _patternOrigin = value; }
        }

        /// <summary>
        /// 图案角度（弧度）
        /// </summary>
        public double PatternAngle
        {
            get { return _patternAngle; }
            set { _patternAngle = value; }
        }

        /// <summary>
        /// 图案比例
        /// </summary>
        public double PatternScale
        {
            get { return _patternScale; }
            set { _patternScale = value > 0 ? value : 1.0; }
        }

        /// <summary>
        /// 填充样式
        /// </summary>
        public HatchStyle HatchStyle
        {
            get { return _hatchStyle; }
            set { _hatchStyle = value; }
        }

        /// <summary>
        /// 是否关联边界
        /// </summary>
        /// <remarks>
        /// 当为true时，如果边界实体被修改，填充会自动更�?
        /// </remarks>
        public bool Associative
        {
            get { return _associative; }
            set { _associative = value; }
        }

        /// <summary>
        /// 是否为渐变填�?
        /// </summary>
        public bool IsGradient
        {
            get { return _isGradient; }
            set { _isGradient = value; }
        }

        /// <summary>
        /// 法向量（对于3D支持�?
        /// </summary>
        public Vector3 Normal
        {
            get { return _normal; }
            set { _normal = value; }
        }

        /// <summary>
        /// 标高
        /// </summary>
        public double Elevation
        {
            get { return _elevation; }
            set { _elevation = value; }
        }


        /// <summary>
        /// 边界数量
        /// </summary>
        public int NumberOfPaths => _boundaryPaths.Count;

        /// <summary>
        /// 填充面积（近似）
        /// </summary>
        public double Area
        {
            get
            {
                double totalArea = 0;
                foreach (var path in _boundaryPaths)
                {
                    totalArea += CalculatePathArea(path);
                }
                return totalArea;
            }
        }

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                if (_boundaryPaths.Count == 0)
                    return new Bounding(Vector2.Zero, 0, 0);

                var allBounds = _boundaryPaths.Select(path => path.GetBounding()).ToList();
                if (allBounds.Count == 0)
                    return new Bounding(Vector2.Zero, 0, 0);

                double minX = allBounds.Min(b => b.center.X - b.width / 2);
                double maxX = allBounds.Max(b => b.center.X + b.width / 2);
                double minY = allBounds.Min(b => b.center.Y - b.height / 2);
                double maxY = allBounds.Max(b => b.center.Y + b.height / 2);

                return new Bounding(
                    new Vector2((minX + maxX) / 2, (minY + maxY) / 2),
                    maxX - minX,
                    maxY - minY
                );
            }
        }

        #endregion

        #region 构造函�?

        /// <summary>
        /// 默认构造函�?
        /// </summary>
        public Hatch()
        {
            _boundaryPaths = new List<HatchBoundaryPath>();
            _pattern = HatchPattern.Solid;
            _patternOrigin = Vector2.Zero;
            _patternAngle = 0.0;
            _patternScale = 1.0;
            _hatchStyle = HatchStyle.Normal;
            _associative = false;
            _isGradient = false;
            _normal = new Vector3(0, 0, 1);
            _elevation = 0.0;
        }

        /// <summary>
        /// 从边界路径构�?
        /// </summary>
        /// <param name="boundaryPath">边界路径</param>
        /// <param name="pattern">填充图案</param>
        public Hatch(HatchBoundaryPath boundaryPath, HatchPattern pattern = null) : this()
        {
            if (boundaryPath != null)
            {
                _boundaryPaths.Add(boundaryPath);
            }
            _pattern = pattern ?? HatchPattern.Solid;
        }

        /// <summary>
        /// 从多个边界路径构�?
        /// </summary>
        /// <param name="boundaryPaths">边界路径集合</param>
        /// <param name="pattern">填充图案</param>
        public Hatch(IEnumerable<HatchBoundaryPath> boundaryPaths, HatchPattern pattern = null) : this()
        {
            if (boundaryPaths != null)
            {
                _boundaryPaths.AddRange(boundaryPaths);
            }
            _pattern = pattern ?? HatchPattern.Solid;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 添加边界路径
        /// </summary>
        /// <param name="boundaryPath">边界路径</param>
        public void AppendPath(HatchBoundaryPath boundaryPath)
        {
            if (boundaryPath != null)
            {
                _boundaryPaths.Add(boundaryPath);
            }
        }

        /// <summary>
        /// 移除边界路径
        /// </summary>
        /// <param name="index">路径索引</param>
        public void RemovePath(int index)
        {
            if (index >= 0 && index < _boundaryPaths.Count)
            {
                _boundaryPaths.RemoveAt(index);
            }
        }

        /// <summary>
        /// 获取指定索引的边界路�?
        /// </summary>
        /// <param name="index">路径索引</param>
        /// <returns>边界路径</returns>
        public HatchBoundaryPath GetPath(int index)
        {
            if (index >= 0 && index < _boundaryPaths.Count)
            {
                return _boundaryPaths[index];
            }
            return null;
        }

        /// <summary>
        /// 设置填充图案
        /// </summary>
        /// <param name="pattern">图案</param>
        /// <param name="angle">角度（弧度）</param>
        /// <param name="scale">比例</param>
        public void SetPattern(HatchPattern pattern, double angle = 0.0, double scale = 1.0)
        {
            _pattern = pattern ?? HatchPattern.Solid;
            _patternAngle = angle;
            _patternScale = scale > 0 ? scale : 1.0;
        }

        /// <summary>
        /// 从实体集合创建边界路�?
        /// </summary>
        /// <param name="entities">实体集合</param>
        /// <param name="associative">是否关联</param>
        public void CreateBoundaryFromEntities(IEnumerable<Entity> entities, bool associative = false)
        {
            if (entities != null)
            {
                var boundaryPath = new HatchBoundaryPath(entities);
                _boundaryPaths.Add(boundaryPath);
                _associative = associative;
            }
        }

        /// <summary>
        /// 检查点是否在填充内�?
        /// </summary>
        /// <param name="point">测试�?/param>
        /// <returns>true表示在内�?/returns>
        public bool ContainsPoint(Vector2 point)
        {
            int insideCount = 0;
            
            foreach (var path in _boundaryPaths)
            {
                if (path.ContainsPoint(point))
                {
                    insideCount++;
                }
            }
            
            // 奇数次包含表示在填充内部（处理带孔的填充�?
            return (insideCount % 2) == 1;
        }

        /// <summary>
        /// 评估填充是否有效
        /// </summary>
        /// <returns>true表示有效</returns>
        public bool Evaluate()
        {
            if (_boundaryPaths.Count == 0)
                return false;

            // 检查所有边界路径是否封�?
            foreach (var path in _boundaryPaths)
            {
                if (!path.IsClosed)
                    return false;
            }

            // 检查图案是否有�?
            return _pattern?.IsValid() ?? false;
        }

        #endregion

        #region Entity 重写方法

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            if (_pattern.FillType == HatchFillType.SolidFill)
            {
                // 实体填充：绘制填充的边界
                DrawSolidFill(gd);
            }
            else
            {
                // 图案填充：绘制图案线�?
                DrawPatternFill(gd);
            }
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Hatch hatch = base.Clone() as Hatch;
            hatch._boundaryPaths = _boundaryPaths.Select(path => (HatchBoundaryPath)path.Clone()).ToList();
            hatch._pattern = (HatchPattern)_pattern.Clone();
            hatch._patternOrigin = _patternOrigin;
            hatch._patternAngle = _patternAngle;
            hatch._patternScale = _patternScale;
            hatch._hatchStyle = _hatchStyle;
            hatch._associative = _associative;
            hatch._isGradient = _isGradient;
            hatch._normal = _normal;
            hatch._elevation = _elevation;
            return hatch;
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new Hatch();
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            foreach (var path in _boundaryPaths)
            {
                foreach (var edge in path.Edges)
                {
                    if (edge is HatchBoundaryPath.LineEdge lineEdge)
                    {
                        lineEdge.StartPoint += translation;
                        lineEdge.EndPoint += translation;
                    }
                    else if (edge is HatchBoundaryPath.ArcEdge arcEdge)
                    {
                        arcEdge.Center += translation;
                    }
                    else if (edge is HatchBoundaryPath.PolylineEdge polyEdge)
                    {
                        for (int i = 0; i < polyEdge.Vertices.Count; i++)
                        {
                            polyEdge.Vertices[i] += translation;
                        }
                    }
                }
            }
            _patternOrigin += translation;
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            foreach (var path in _boundaryPaths)
            {
                foreach (var edge in path.Edges)
                {
                    if (edge is HatchBoundaryPath.LineEdge lineEdge)
                    {
                        lineEdge.StartPoint = Vector2.RotateInRadian(lineEdge.StartPoint, center, angle);
                        lineEdge.EndPoint = Vector2.RotateInRadian(lineEdge.EndPoint, center, angle);
                    }
                    else if (edge is HatchBoundaryPath.ArcEdge arcEdge)
                    {
                        arcEdge.Center = Vector2.RotateInRadian(arcEdge.Center, center, angle);
                        arcEdge.StartAngle += angle;
                        arcEdge.EndAngle += angle;
                    }
                    else if (edge is HatchBoundaryPath.PolylineEdge polyEdge)
                    {
                        for (int i = 0; i < polyEdge.Vertices.Count; i++)
                        {
                            polyEdge.Vertices[i] = Vector2.RotateInRadian(polyEdge.Vertices[i], center, angle);
                        }
                    }
                }
            }
            _patternOrigin = Vector2.RotateInRadian(_patternOrigin, center, angle);
            _patternAngle += angle;
        }

        /// <summary>
        /// 变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            foreach (var path in _boundaryPaths)
            {
                foreach (var edge in path.Edges)
                {
                    if (edge is HatchBoundaryPath.LineEdge lineEdge)
                    {
                        lineEdge.StartPoint = transform * lineEdge.StartPoint;
                        lineEdge.EndPoint = transform * lineEdge.EndPoint;
                    }
                    else if (edge is HatchBoundaryPath.ArcEdge arcEdge)
                    {
                        Vector2 oldCenter = arcEdge.Center;
                        Vector2 radiusPoint = oldCenter + new Vector2(arcEdge.Radius, 0);
                        
                        arcEdge.Center = transform * oldCenter;
                        Vector2 transformedRadiusPoint = transform * radiusPoint;
                        arcEdge.Radius = (transformedRadiusPoint - arcEdge.Center).length;
                    }
                    else if (edge is HatchBoundaryPath.PolylineEdge polyEdge)
                    {
                        for (int i = 0; i < polyEdge.Vertices.Count; i++)
                        {
                            polyEdge.Vertices[i] = transform * polyEdge.Vertices[i];
                        }
                    }
                }
            }
            _patternOrigin = transform * _patternOrigin;
        }

        /// <summary>
        /// 对象捕捉�?
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPnts = new List<ObjectSnapPoint>();
            
            foreach (var path in _boundaryPaths)
            {
                foreach (var edge in path.Edges)
                {
                    snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.End, edge.GetStartPoint()));
                    snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.End, edge.GetEndPoint()));
                }
            }
            
            return snapPnts;
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPnts = new List<GripPoint>();
            
            // 添加边界关键点作为夹�?
            foreach (var path in _boundaryPaths)
            {
                foreach (var edge in path.Edges)
                {
                    gripPnts.Add(new GripPoint(GripPointType.End, edge.GetStartPoint()));
                    if (edge is HatchBoundaryPath.ArcEdge arcEdge)
                    {
                        gripPnts.Add(new GripPoint(GripPointType.Center, arcEdge.Center));
                    }
                }
            }
            
            return gripPnts;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            // 简化实现：移动整个填充
            if (index == 0 && _boundaryPaths.Count > 0)
            {
                var firstEdge = _boundaryPaths[0].Edges.FirstOrDefault();
                if (firstEdge != null)
                {
                    Vector2 offset = newPosition - firstEdge.GetStartPoint();
                    Translate(offset);
                }
            }
        }

        /// <summary>
        /// 写XML
        /// </summary>

        /// <summary>
        /// 读XML
        /// </summary>

        #endregion

        #region 私有方法

        /// <summary>
        /// 绘制实体填充
        /// </summary>
        private void DrawSolidFill(IGraphicsDraw gd)
        {
            foreach (var path in _boundaryPaths)
            {
                if (path.Edges.Count >= 3)
                {
                    // 简化实现：将边界转换为三角形或四边形进行填�?
                    var vertices = path.Edges.Select(edge => edge.GetStartPoint()).ToList();
                    
                    if (vertices.Count >= 3)
                    {
                        // 绘制三角形扇形填�?
                        var center = vertices.Aggregate(Vector2.Zero, (sum, v) => sum + v) / vertices.Count;
                        
                        for (int i = 0; i < vertices.Count; i++)
                        {
                            int nextIndex = (i + 1) % vertices.Count;
                            gd.DrawTriangle(center, vertices[i], vertices[nextIndex]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 绘制图案填充
        /// </summary>
        private void DrawPatternFill(IGraphicsDraw gd)
        {
            foreach (var path in _boundaryPaths)
            {
                // 绘制边界线条
                foreach (var edge in path.Edges)
                {
                    if (edge is HatchBoundaryPath.LineEdge lineEdge)
                    {
                        gd.DrawLine(lineEdge.StartPoint, lineEdge.EndPoint);
                    }
                    else if (edge is HatchBoundaryPath.ArcEdge arcEdge)
                    {
                        gd.DrawArc(arcEdge.Center, arcEdge.Radius, arcEdge.StartAngle, arcEdge.EndAngle);
                    }
                }
                
                // 简化的图案绘制：在边界内绘制图案线�?
                DrawPatternLines(gd, path);
            }
        }

        /// <summary>
        /// 在指定边界内绘制图案线条
        /// </summary>
        private void DrawPatternLines(IGraphicsDraw gd, HatchBoundaryPath boundaryPath)
        {
            if (_pattern.LineDefinitions.Count == 0)
                return;
                
            var bounds = boundaryPath.GetBounding();
            double spacing = _patternScale * 0.1; // 简化的间距计算
            
            // 绘制第一个线条定�?
            var firstLineDef = _pattern.LineDefinitions[0];
            double angle = firstLineDef.Angle + _patternAngle;
            
            // 在边界框内绘制平行线�?
            Vector2 direction = new Vector2(Math.Cos(angle), Math.Sin(angle));
            Vector2 perpendicular = new Vector2(-direction.Y, direction.X) * spacing;
            
            Vector2 startCorner = bounds.center - new Vector2(bounds.width / 2, bounds.height / 2);
            Vector2 endCorner = bounds.center + new Vector2(bounds.width / 2, bounds.height / 2);
            
            // 简化实现：绘制几条代表性的图案线条
            for (int i = 0; i < 10; i++)
            {
                Vector2 lineStart = startCorner + perpendicular * i;
                Vector2 lineEnd = lineStart + direction * Math.Max(bounds.width, bounds.height);
                
                // 只在边界内的部分绘制线条
                if (boundaryPath.ContainsPoint(lineStart) || boundaryPath.ContainsPoint(lineEnd))
                {
                    gd.DrawLine(lineStart, lineEnd);
                }
            }
        }

        /// <summary>
        /// 计算路径面积（简化算法）
        /// </summary>
        private double CalculatePathArea(HatchBoundaryPath path)
        {
            if (path.Edges.Count < 3)
                return 0;
                
            double area = 0;
            var vertices = path.Edges.Select(edge => edge.GetStartPoint()).ToList();
            
            // 使用鞋带公式计算多边形面�?
            for (int i = 0; i < vertices.Count; i++)
            {
                int j = (i + 1) % vertices.Count;
                area += vertices[i].X * vertices[j].Y;
                area -= vertices[j].X * vertices[i].Y;
            }
            
            return Math.Abs(area) / 2.0;
        }

        #endregion
    }
}