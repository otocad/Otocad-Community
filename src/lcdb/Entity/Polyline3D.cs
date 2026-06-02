using System;
using System.Collections.Generic;
using System.Linq;
using LitMath;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 3D多段�?
    /// </summary>
    public class Polyline3D : Entity
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className
        {
            get { return "Polyline3D"; }
        }

        /// <summary>
        /// 顶点列表
        /// </summary>
        private List<Vector3> _vertices = new List<Vector3>();

        /// <summary>
        /// 是否闭合
        /// </summary>
        private bool _closed = false;

        /// <summary>
        /// 多段线类�?
        /// </summary>
        private Polyline3DType _polyType = Polyline3DType.SimplePoly;

        /// <summary>
        /// 获取顶点数量
        /// </summary>
        public int NumberOfVertices
        {
            get { return _vertices.Count; }
        }

        /// <summary>
        /// 获取或设置顶点列�?
        /// </summary>
        public List<Vector3> Vertices
        {
            get { return _vertices; }
            set { _vertices = value ?? new List<Vector3>(); }
        }

        /// <summary>
        /// 获取或设置是否闭�?
        /// </summary>
        public bool IsClosed
        {
            get { return _closed; }
            set { _closed = value; }
        }
        
        /// <summary>
        /// 是否闭合（便捷属性）
        /// </summary>
        public bool Closed
        {
            get { return _closed; }
            set { _closed = value; }
        }

        /// <summary>
        /// 获取或设置多段线类型
        /// </summary>
        public Polyline3DType PolyType
        {
            get { return _polyType; }
            set { _polyType = value; }
        }

        /// <summary>
        /// 添加顶点
        /// </summary>
        public void AddVertex(Vector3 vertex)
        {
            _vertices.Add(vertex);
        }

        /// <summary>
        /// 在指定索引处添加顶点
        /// </summary>
        public void AddVertexAt(int index, Vector3 vertex)
        {
            _vertices.Insert(index, vertex);
        }

        /// <summary>
        /// 移除指定索引处的顶点
        /// </summary>
        public void RemoveVertexAt(int index)
        {
            if (index >= 0 && index < _vertices.Count)
            {
                _vertices.RemoveAt(index);
            }
        }

        /// <summary>
        /// 获取指定索引处的顶点
        /// </summary>
        public Vector3 GetVertexAt(int index)
        {
            if (index >= 0 && index < _vertices.Count)
            {
                return _vertices[index];
            }
            throw new IndexOutOfRangeException("Vertex index out of range");
        }

        /// <summary>
        /// 设置指定索引处的顶点
        /// </summary>
        public void SetVertexAt(int index, Vector3 vertex)
        {
            if (index >= 0 && index < _vertices.Count)
            {
                _vertices[index] = vertex;
            }
        }

        /// <summary>
        /// 获取多段线长�?
        /// </summary>
        public double GetLength()
        {
            double length = 0.0;
            for (int i = 0; i < _vertices.Count - 1; i++)
            {
                length += Vector3.Distance(_vertices[i], _vertices[i + 1]);
            }
            
            if (_closed && _vertices.Count > 2)
            {
                length += Vector3.Distance(_vertices[_vertices.Count - 1], _vertices[0]);
            }
            
            return length;
        }

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                if (_vertices.Count > 0)
                {
                    double minX = double.MaxValue;
                    double minY = double.MaxValue;
                    double maxX = double.MinValue;
                    double maxY = double.MinValue;

                    foreach (Vector3 vertex in _vertices)
                    {
                        minX = Math.Min(minX, vertex.X);
                        minY = Math.Min(minY, vertex.Y);
                        maxX = Math.Max(maxX, vertex.X);
                        maxY = Math.Max(maxY, vertex.Y);
                    }

                    return new Bounding(new Vector2(minX, minY), new Vector2(maxX, maxY));
                }
                else
                {
                    return new Bounding();
                }
            }
        }

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            if (_vertices.Count < 2)
                return;

            // 绘制线段
            for (int i = 0; i < _vertices.Count - 1; i++)
            {
                Vector2 p1 = new Vector2(_vertices[i].x, _vertices[i].y);
                Vector2 p2 = new Vector2(_vertices[i + 1].x, _vertices[i + 1].y);
                gd.DrawLine(p1, p2);
            }

            // 如果闭合，绘制最后一条线�?
            if (_closed && _vertices.Count > 2)
            {
                Vector2 p1 = new Vector2(_vertices[_vertices.Count - 1].x, _vertices[_vertices.Count - 1].y);
                Vector2 p2 = new Vector2(_vertices[0].x, _vertices[0].y);
                gd.DrawLine(p1, p2);
            }
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            Vector3 translation3D = new Vector3(translation.X, translation.Y, 0);
            for (int i = 0; i < _vertices.Count; i++)
            {
                _vertices[i] = _vertices[i] + translation3D;
            }
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            Vector3 center3D = new Vector3(center.X, center.Y, 0);
            
            for (int i = 0; i < _vertices.Count; i++)
            {
                Vector3 relativePos = _vertices[i] - center3D;
                // Rotate around Z axis
                double cos = Math.Cos(angle);
                double sin = Math.Sin(angle);
                double newX = relativePos.X * cos - relativePos.Y * sin;
                double newY = relativePos.X * sin + relativePos.Y * cos;
                _vertices[i] = new Vector3(newX, newY, relativePos.Z) + center3D;
            }
        }

        /// <summary>
        /// 变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            for (int i = 0; i < _vertices.Count; i++)
            {
                Vector2 vertex2D = new Vector2(_vertices[i].x, _vertices[i].y);
                Vector2 transformed = transform * vertex2D;
                _vertices[i] = new Vector3(transformed.X, transformed.Y, _vertices[i].z);
            }
        }

        /// <summary>
        /// 对象捕捉�?
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPnts = new List<ObjectSnapPoint>();
            
            for (int i = 0; i < _vertices.Count; i++)
            {
                // 端点
                Vector2 vertex2D = new Vector2(_vertices[i].x, _vertices[i].y);
                snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.End, vertex2D));
                
                // 中点
                if (i < _vertices.Count - 1)
                {
                    Vector2 nextVertex2D = new Vector2(_vertices[i + 1].x, _vertices[i + 1].y);
                    Vector2 midPoint = (vertex2D + nextVertex2D) / 2;
                    snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, midPoint));
                }
            }
            
            // 闭合多段线的最后一条线段中�?
            if (_closed && _vertices.Count > 2)
            {
                Vector2 lastVertex = new Vector2(_vertices[_vertices.Count - 1].x, _vertices[_vertices.Count - 1].y);
                Vector2 firstVertex = new Vector2(_vertices[0].x, _vertices[0].y);
                Vector2 midPoint = (lastVertex + firstVertex) / 2;
                snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, midPoint));
            }
            
            return snapPnts;
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPnts = new List<GripPoint>();
            
            // 顶点夹点
            for (int i = 0; i < _vertices.Count; i++)
            {
                Vector2 vertex2D = new Vector2(_vertices[i].x, _vertices[i].y);
                gripPnts.Add(new GripPoint(GripPointType.End, vertex2D));
            }
            
            // 中点夹点
            for (int i = 0; i < _vertices.Count - 1; i++)
            {
                Vector2 vertex1 = new Vector2(_vertices[i].x, _vertices[i].y);
                Vector2 vertex2 = new Vector2(_vertices[i + 1].x, _vertices[i + 1].y);
                Vector2 midPoint = (vertex1 + vertex2) / 2;
                
                GripPoint midGripPnt = new GripPoint(GripPointType.Mid, midPoint);
                midGripPnt.xData1 = _vertices[i];
                midGripPnt.xData2 = _vertices[i + 1];
                gripPnts.Add(midGripPnt);
            }
            
            // 闭合多段线的最后一条线段中点夹�?
            if (_closed && _vertices.Count > 2)
            {
                Vector2 lastVertex = new Vector2(_vertices[_vertices.Count - 1].x, _vertices[_vertices.Count - 1].y);
                Vector2 firstVertex = new Vector2(_vertices[0].x, _vertices[0].y);
                Vector2 midPoint = (lastVertex + firstVertex) / 2;
                
                GripPoint midGripPnt = new GripPoint(GripPointType.Mid, midPoint);
                midGripPnt.xData1 = _vertices[_vertices.Count - 1];
                midGripPnt.xData2 = _vertices[0];
                gripPnts.Add(midGripPnt);
            }
            
            return gripPnts;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            switch (gripPoint.type)
            {
                case GripPointType.End:
                    {
                        if (index >= 0 && index < _vertices.Count)
                        {
                            double z = _vertices[index].z;
                            _vertices[index] = new Vector3(newPosition.X, newPosition.Y, z);
                        }
                    }
                    break;
                    
                case GripPointType.Mid:
                    {
                        int numOfVertices = _vertices.Count;
                        int i = index - numOfVertices;
                        if (i >= 0 && i < numOfVertices)
                        {
                            int vIndex1st = i;
                            int vIndex2nd = (i + 1) % numOfVertices;
                            
                            if (!_closed && vIndex2nd == 0)
                                break;
                            
                            Vector2 translation = newPosition - gripPoint.position;
                            Vector3 translation3D = new Vector3(translation.X, translation.Y, 0);
                            
                            Vector3 vertex1 = (Vector3)gripPoint.xData1;
                            Vector3 vertex2 = (Vector3)gripPoint.xData2;
                            
                            _vertices[vIndex1st] = vertex1 + translation3D;
                            _vertices[vIndex2nd] = vertex2 + translation3D;
                        }
                    }
                    break;
                    
                default:
                    break;
            }
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Polyline3D polyline3D = base.Clone() as Polyline3D;
            polyline3D._vertices = new List<Vector3>(_vertices);
            polyline3D._closed = _closed;
            polyline3D._polyType = _polyType;
            return polyline3D;
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new Polyline3D();
        }

        /// <summary>
        /// 写XML
        /// </summary>

        /// <summary>
        /// 读XML
        /// </summary>
    }

    /// <summary>
    /// 3D多段线类�?
    /// </summary>
    public enum Polyline3DType
    {
        /// <summary>
        /// 简单多段线
        /// </summary>
        SimplePoly = 0,
        
        /// <summary>
        /// 二次B样条
        /// </summary>
        QuadSplinePoly = 1,
        
        /// <summary>
        /// 三次B样条
        /// </summary>
        CubicSplinePoly = 2
    }
}