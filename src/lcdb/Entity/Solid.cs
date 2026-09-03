using System;
using System.Collections.Generic;
using System.Linq;
using LitMath;
using lcdb;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 实体填充（四边形填充区域�?
    /// </summary>
    public class Solid : Entity
    {
        #region 私有字段

        private Vector2 _firstVertex = new Vector2(0, 0);
        private Vector2 _secondVertex = new Vector2(0, 0);
        private Vector2 _thirdVertex = new Vector2(0, 0);
        private Vector2 _fourthVertex = new Vector2(0, 0);

        #endregion

        #region 属�?

        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "Solid";

        /// <summary>
        /// 第一个顶�?
        /// </summary>
        public Vector2 FirstVertex
        {
            get { return _firstVertex; }
            set { _firstVertex = value; }
        }

        /// <summary>
        /// 第二个顶�?
        /// </summary>
        public Vector2 SecondVertex
        {
            get { return _secondVertex; }
            set { _secondVertex = value; }
        }

        /// <summary>
        /// 第三个顶�?
        /// </summary>
        public Vector2 ThirdVertex
        {
            get { return _thirdVertex; }
            set { _thirdVertex = value; }
        }

        /// <summary>
        /// 第四个顶�?
        /// </summary>
        public Vector2 FourthVertex
        {
            get { return _fourthVertex; }
            set { _fourthVertex = value; }
        }

        // Lowercase property aliases for compatibility
        public Vector2 firstPoint 
        { 
            get { return _firstVertex; } 
            set { _firstVertex = value; } 
        }
        
        public Vector2 secondPoint 
        { 
            get { return _secondVertex; } 
            set { _secondVertex = value; } 
        }
        
        public Vector2 thirdPoint 
        { 
            get { return _thirdVertex; } 
            set { _thirdVertex = value; } 
        }
        
        public Vector2 fourthPoint 
        { 
            get { return _fourthVertex; } 
            set { _fourthVertex = value; } 
        }


        /// <summary>
        /// 顶点列表（只读）
        /// </summary>
        public List<Vector2> Vertices
        {
            get
            {
                return new List<Vector2> { _firstVertex, _secondVertex, _thirdVertex, _fourthVertex };
            }
        }

        /// <summary>
        /// 是否为三角形（第三和第四个顶点相同）
        /// </summary>
        public bool IsTriangle
        {
            get { return (_thirdVertex - _fourthVertex).length < 1e-10; }
        }

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                var vertices = Vertices;
                if (vertices.Count == 0)
                    return new Bounding(Vector2.Zero, 0, 0);

                double minX = vertices.Min(v => v.X);
                double maxX = vertices.Max(v => v.X);
                double minY = vertices.Min(v => v.Y);
                double maxY = vertices.Max(v => v.Y);

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
        public Solid()
        {
            _firstVertex = new Vector2(0, 0);
            _secondVertex = new Vector2(1, 0);
            _thirdVertex = new Vector2(1, 1);
            _fourthVertex = new Vector2(0, 1);
        }

        /// <summary>
        /// 三角形构造函�?
        /// </summary>
        /// <param name="firstVertex">第一个顶�?/param>
        /// <param name="secondVertex">第二个顶�?/param>
        /// <param name="thirdVertex">第三个顶�?/param>
        public Solid(Vector2 firstVertex, Vector2 secondVertex, Vector2 thirdVertex)
        {
            _firstVertex = firstVertex;
            _secondVertex = secondVertex;
            _thirdVertex = thirdVertex;
            _fourthVertex = thirdVertex; // 三角形时第四个顶点与第三个相�?
        }

        /// <summary>
        /// 四边形构造函�?
        /// </summary>
        /// <param name="firstVertex">第一个顶�?/param>
        /// <param name="secondVertex">第二个顶�?/param>
        /// <param name="thirdVertex">第三个顶�?/param>
        /// <param name="fourthVertex">第四个顶�?/param>
        public Solid(Vector2 firstVertex, Vector2 secondVertex, Vector2 thirdVertex, Vector2 fourthVertex)
        {
            _firstVertex = firstVertex;
            _secondVertex = secondVertex;
            _thirdVertex = thirdVertex;
            _fourthVertex = fourthVertex;
        }

        /// <summary>
        /// 矩形构造函�?
        /// </summary>
        /// <param name="corner1">第一个角�?/param>
        /// <param name="corner2">对角�?/param>
        /// <returns>矩形Solid实体</returns>
        public static Solid CreateRectangle(Vector2 corner1, Vector2 corner2)
        {
            return new Solid(
                corner1,
                new Vector2(corner2.X, corner1.Y),
                corner2,
                new Vector2(corner1.X, corner2.Y)
            );
        }

        #endregion

        #region 必须实现的方�?

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            var vertices = Vertices;
            
            if (IsTriangle)
            {
                // 绘制三角�?
                gd.DrawTriangle(_firstVertex, _secondVertex, _thirdVertex);
            }
            else
            {
                // 绘制四边�?
                gd.DrawQuadrilateral(_firstVertex, _secondVertex, _thirdVertex, _fourthVertex);
            }
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new Solid();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Solid solid = base.Clone() as Solid;
            solid._firstVertex = _firstVertex;
            solid._secondVertex = _secondVertex;
            solid._thirdVertex = _thirdVertex;
            solid._fourthVertex = _fourthVertex;
            return solid;
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            _firstVertex += translation;
            _secondVertex += translation;
            _thirdVertex += translation;
            _fourthVertex += translation;
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            _firstVertex = Vector2.RotateInRadian(_firstVertex, center, angle);
            _secondVertex = Vector2.RotateInRadian(_secondVertex, center, angle);
            _thirdVertex = Vector2.RotateInRadian(_thirdVertex, center, angle);
            _fourthVertex = Vector2.RotateInRadian(_fourthVertex, center, angle);
        }

        /// <summary>
        /// 矩阵变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            _firstVertex = transform * _firstVertex;
            _secondVertex = transform * _secondVertex;
            _thirdVertex = transform * _thirdVertex;
            _fourthVertex = transform * _fourthVertex;
        }

        #endregion

        #region 交互功能

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            
            // 为每个顶点添加夹�?
            gripPoints.Add(new GripPoint(GripPointType.Corner, _firstVertex));
            gripPoints.Add(new GripPoint(GripPointType.Corner, _secondVertex));
            gripPoints.Add(new GripPoint(GripPointType.Corner, _thirdVertex));
            
            // 如果不是三角形，添加第四个顶点的夹点
            if (!IsTriangle)
            {
                gripPoints.Add(new GripPoint(GripPointType.Corner, _fourthVertex));
            }
            
            // 添加中心点夹�?
            var center = CalculateCenter();
            gripPoints.Add(new GripPoint(GripPointType.Center, center));
            
            return gripPoints;
        }

        /// <summary>
        /// 对象捕捉�?
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPoints = new List<ObjectSnapPoint>();
            
            // 添加顶点捕捉�?
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _firstVertex));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _secondVertex));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _thirdVertex));
            
            if (!IsTriangle)
            {
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _fourthVertex));
            }
            
            // 添加边的中点
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (_firstVertex + _secondVertex) / 2));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (_secondVertex + _thirdVertex) / 2));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (_thirdVertex + _fourthVertex) / 2));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (_fourthVertex + _firstVertex) / 2));
            
            // 添加中心�?
            var center = CalculateCenter();
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Center, center));
            
            return snapPoints;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            if (index == 0)
            {
                _firstVertex = newPosition;
            }
            else if (index == 1)
            {
                _secondVertex = newPosition;
            }
            else if (index == 2)
            {
                _thirdVertex = newPosition;
                if (IsTriangle)
                {
                    _fourthVertex = newPosition; // 保持三角�?
                }
            }
            else if (index == 3 && !IsTriangle)
            {
                _fourthVertex = newPosition;
            }
            else if (index == (IsTriangle ? 3 : 4))
            {
                // 移动中心�?- 整体平移
                var currentCenter = CalculateCenter();
                var translation = newPosition - currentCenter;
                Translate(translation);
            }
        }

        #endregion

        #region XML序列�?

        /// <summary>
        /// 写XML
        /// </summary>

        /// <summary>
        /// 读XML
        /// </summary>

        #endregion

        #region 几何计算方法

        /// <summary>
        /// 计算中心�?
        /// </summary>
        /// <returns>中心点坐�?/returns>
        public Vector2 CalculateCenter()
        {
            if (IsTriangle)
            {
                return (_firstVertex + _secondVertex + _thirdVertex) / 3.0;
            }
            else
            {
                return (_firstVertex + _secondVertex + _thirdVertex + _fourthVertex) / 4.0;
            }
        }

        /// <summary>
        /// 计算面积
        /// </summary>
        /// <returns>面积�?/returns>
        public double CalculateArea()
        {
            if (IsTriangle)
            {
                return CalculateTriangleArea(_firstVertex, _secondVertex, _thirdVertex);
            }
            else
            {
                // 四边形分解为两个三角形计算面�?
                double area1 = CalculateTriangleArea(_firstVertex, _secondVertex, _thirdVertex);
                double area2 = CalculateTriangleArea(_firstVertex, _thirdVertex, _fourthVertex);
                return Math.Abs(area1) + Math.Abs(area2);
            }
        }

        /// <summary>
        /// 计算三角形面�?
        /// </summary>
        private double CalculateTriangleArea(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            // 使用叉积计算三角形面�?
            return 0.5 * Math.Abs((p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y));
        }

        /// <summary>
        /// 计算周长
        /// </summary>
        /// <returns>周长�?/returns>
        public double CalculatePerimeter()
        {
            double perimeter = 0;
            
            perimeter += (_secondVertex - _firstVertex).length;
            perimeter += (_thirdVertex - _secondVertex).length;
            
            if (IsTriangle)
            {
                perimeter += (_firstVertex - _thirdVertex).length;
            }
            else
            {
                perimeter += (_fourthVertex - _thirdVertex).length;
                perimeter += (_firstVertex - _fourthVertex).length;
            }
            
            return perimeter;
        }

        /// <summary>
        /// 判断点是否在实体内部
        /// </summary>
        /// <param name="point">测试�?/param>
        /// <returns>true表示在内�?/returns>
        public bool ContainsPoint(Vector2 point)
        {
            if (IsTriangle)
            {
                return IsPointInTriangle(point, _firstVertex, _secondVertex, _thirdVertex);
            }
            else
            {
                // 四边形拆分为两个三角形判�?
                return IsPointInTriangle(point, _firstVertex, _secondVertex, _thirdVertex) ||
                       IsPointInTriangle(point, _firstVertex, _thirdVertex, _fourthVertex);
            }
        }

        /// <summary>
        /// 判断点是否在三角形内�?
        /// </summary>
        private bool IsPointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            // 使用重心坐标法判断点是否在三角形�?
            double denom = (b.Y - c.Y) * (a.X - c.X) + (c.X - b.X) * (a.Y - c.Y);
            if (Math.Abs(denom) < 1e-10) return false; // 退化三角形
            
            double alpha = ((b.Y - c.Y) * (point.X - c.X) + (c.X - b.X) * (point.Y - c.Y)) / denom;
            double beta = ((c.Y - a.Y) * (point.X - c.X) + (a.X - c.X) * (point.Y - c.Y)) / denom;
            double gamma = 1 - alpha - beta;
            
            return alpha >= 0 && beta >= 0 && gamma >= 0;
        }

        /// <summary>
        /// 获取边界线段
        /// </summary>
        /// <returns>边界线段列表</returns>
        public List<Line> GetBoundaryLines()
        {
            var lines = new List<Line>();
            
            lines.Add(new Line(_firstVertex, _secondVertex));
            lines.Add(new Line(_secondVertex, _thirdVertex));
            
            if (IsTriangle)
            {
                lines.Add(new Line(_thirdVertex, _firstVertex));
            }
            else
            {
                lines.Add(new Line(_thirdVertex, _fourthVertex));
                lines.Add(new Line(_fourthVertex, _firstVertex));
            }
            
            return lines;
        }

        /// <summary>
        /// 检查是否为有效的Solid（无自相交）
        /// </summary>
        /// <returns>true表示有效</returns>
        public bool IsValid()
        {
            if (IsTriangle)
            {
                // 三角形：检查三点不共线
                var area = CalculateTriangleArea(_firstVertex, _secondVertex, _thirdVertex);
                return area > 1e-10;
            }
            else
            {
                // 四边形：检查无自相�?
                return !DoLinesIntersect(_firstVertex, _secondVertex, _thirdVertex, _fourthVertex) &&
                       !DoLinesIntersect(_secondVertex, _thirdVertex, _fourthVertex, _firstVertex);
            }
        }

        /// <summary>
        /// 判断两条线段是否相交
        /// </summary>
        private bool DoLinesIntersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
        {
            // 使用方向判断�?
            int o1 = Orientation(p1, q1, p2);
            int o2 = Orientation(p1, q1, q2);
            int o3 = Orientation(p2, q2, p1);
            int o4 = Orientation(p2, q2, q1);
            
            // 一般情�?
            if (o1 != o2 && o3 != o4)
                return true;
                
            // 特殊情况：共线且重叠
            if (o1 == 0 && OnSegment(p1, p2, q1)) return true;
            if (o2 == 0 && OnSegment(p1, q2, q1)) return true;
            if (o3 == 0 && OnSegment(p2, p1, q2)) return true;
            if (o4 == 0 && OnSegment(p2, q1, q2)) return true;
            
            return false;
        }

        /// <summary>
        /// 计算三点的方�?
        /// </summary>
        private int Orientation(Vector2 p, Vector2 q, Vector2 r)
        {
            double val = (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);
            if (Math.Abs(val) < 1e-10) return 0; // 共线
            return (val > 0) ? 1 : 2; // 顺时针或逆时�?
        }

        /// <summary>
        /// 判断点q是否在线段pr�?
        /// </summary>
        private bool OnSegment(Vector2 p, Vector2 q, Vector2 r)
        {
            return q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                   q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y);
        }

        #endregion
    }
}