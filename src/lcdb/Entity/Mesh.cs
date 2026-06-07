using System;
using System.Collections.Generic;
using System.Linq;
using LitMath;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 网格实体
    /// </summary>
    public class Mesh : Entity
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className
        {
            get { return "Mesh"; }
        }

        /// <summary>
        /// 顶点列表
        /// </summary>
        private List<Vector3> _vertices = new List<Vector3>();

        /// <summary>
        /// 面列表（每个面是顶点索引数组�?
        /// </summary>
        private List<int[]> _faces = new List<int[]>();

        /// <summary>
        /// 边列�?
        /// </summary>
        private List<MeshEdge> _edges = new List<MeshEdge>();

        /// <summary>
        /// 细分级别
        /// </summary>
        private byte _subdivisionLevel = 0;

        /// <summary>
        /// 获取或设置顶点列�?
        /// </summary>
        public List<Vector3> Vertices
        {
            get { return _vertices; }
            set { _vertices = value ?? new List<Vector3>(); }
        }

        /// <summary>
        /// 获取或设置面列表
        /// </summary>
        public List<int[]> Faces
        {
            get { return _faces; }
            set { _faces = value ?? new List<int[]>(); }
        }

        /// <summary>
        /// 获取或设置边列表
        /// </summary>
        public List<MeshEdge> Edges
        {
            get { return _edges; }
            set { _edges = value ?? new List<MeshEdge>(); }
        }

        /// <summary>
        /// 获取或设置细分级别（0-255，推�?-5�?
        /// </summary>
        public byte SubdivisionLevel
        {
            get { return _subdivisionLevel; }
            set { _subdivisionLevel = Math.Min(value, (byte)5); }
        }

        /// <summary>
        /// 添加顶点
        /// </summary>
        public int AddVertex(Vector3 vertex)
        {
            _vertices.Add(vertex);
            return _vertices.Count - 1;
        }

        /// <summary>
        /// 添加�?
        /// </summary>
        public void AddFace(params int[] vertexIndices)
        {
            if (vertexIndices == null || vertexIndices.Length < 3)
            {
                throw new ArgumentException("Face must have at least 3 vertices");
            }

            // 验证索引有效�?
            foreach (int index in vertexIndices)
            {
                if (index < 0 || index >= _vertices.Count)
                {
                    throw new IndexOutOfRangeException($"Vertex index {index} is out of range");
                }
            }

            _faces.Add(vertexIndices);
        }

        /// <summary>
        /// 添加�?
        /// </summary>
        public void AddEdge(MeshEdge edge)
        {
            if (edge.StartVertexIndex < 0 || edge.StartVertexIndex >= _vertices.Count ||
                edge.EndVertexIndex < 0 || edge.EndVertexIndex >= _vertices.Count)
            {
                throw new IndexOutOfRangeException("Edge vertex indices are out of range");
            }

            _edges.Add(edge);
        }

        /// <summary>
        /// 创建三角形面
        /// </summary>
        public void AddTriangleFace(int v1, int v2, int v3)
        {
            AddFace(v1, v2, v3);
        }

        /// <summary>
        /// 创建四边形面
        /// </summary>
        public void AddQuadFace(int v1, int v2, int v3, int v4)
        {
            AddFace(v1, v2, v3, v4);
        }

        /// <summary>
        /// 获取面的法向�?
        /// </summary>
        public Vector3 GetFaceNormal(int faceIndex)
        {
            if (faceIndex < 0 || faceIndex >= _faces.Count)
            {
                throw new IndexOutOfRangeException("Face index out of range");
            }

            int[] face = _faces[faceIndex];
            if (face.Length < 3)
            {
                return new Vector3(0, 0, 1);
            }

            Vector3 v0 = _vertices[face[0]];
            Vector3 v1 = _vertices[face[1]];
            Vector3 v2 = _vertices[face[2]];

            Vector3 e1 = v1 - v0;
            Vector3 e2 = v2 - v0;
            Vector3 normal = Vector3.Cross(e1, e2);
            normal.Normalize();

            return normal;
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
            // 绘制所有的�?
            HashSet<string> drawnEdges = new HashSet<string>();

            foreach (int[] face in _faces)
            {
                for (int i = 0; i < face.Length; i++)
                {
                    int v1Index = face[i];
                    int v2Index = face[(i + 1) % face.Length];

                    // 创建边的唯一标识�?
                    string edgeKey = Math.Min(v1Index, v2Index) + "_" + Math.Max(v1Index, v2Index);

                    if (!drawnEdges.Contains(edgeKey))
                    {
                        Vector3 v1 = _vertices[v1Index];
                        Vector3 v2 = _vertices[v2Index];

                        // 投影�?D
                        Vector2 p1 = new Vector2(v1.X, v1.Y);
                        Vector2 p2 = new Vector2(v2.X, v2.Y);

                        gd.DrawLine(p1, p2);
                        drawnEdges.Add(edgeKey);
                    }
                }
            }

            // 绘制额外定义的边（如果有�?
            foreach (MeshEdge edge in _edges)
            {
                if (edge.StartVertexIndex >= 0 && edge.StartVertexIndex < _vertices.Count &&
                    edge.EndVertexIndex >= 0 && edge.EndVertexIndex < _vertices.Count)
                {
                    Vector3 v1 = _vertices[edge.StartVertexIndex];
                    Vector3 v2 = _vertices[edge.EndVertexIndex];

                    Vector2 p1 = new Vector2(v1.X, v1.Y);
                    Vector2 p2 = new Vector2(v2.X, v2.Y);

                    gd.DrawLine(p1, p2);
                }
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
            
            // 顶点捕捉�?
            foreach (Vector3 vertex in _vertices)
            {
                Vector2 vertex2D = new Vector2(vertex.X, vertex.Y);
                snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.End, vertex2D));
            }
            
            // 边的中点捕捉�?
            HashSet<string> processedEdges = new HashSet<string>();
            
            foreach (int[] face in _faces)
            {
                for (int i = 0; i < face.Length; i++)
                {
                    int v1Index = face[i];
                    int v2Index = face[(i + 1) % face.Length];
                    
                    string edgeKey = Math.Min(v1Index, v2Index) + "_" + Math.Max(v1Index, v2Index);
                    
                    if (!processedEdges.Contains(edgeKey))
                    {
                        Vector3 v1 = _vertices[v1Index];
                        Vector3 v2 = _vertices[v2Index];
                        Vector2 midPoint = new Vector2((v1.X + v2.X) / 2, (v1.Y + v2.Y) / 2);
                        snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, midPoint));
                        processedEdges.Add(edgeKey);
                    }
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
            
            // 为每个顶点创建夹�?
            for (int i = 0; i < _vertices.Count; i++)
            {
                Vector2 vertex2D = new Vector2(_vertices[i].x, _vertices[i].y);
                GripPoint grip = new GripPoint(GripPointType.End, vertex2D);
                grip.xData1 = i; // 存储顶点索引
                gripPnts.Add(grip);
            }
            
            return gripPnts;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            if (gripPoint.type == GripPointType.End && gripPoint.xData1 is int vertexIndex)
            {
                if (vertexIndex >= 0 && vertexIndex < _vertices.Count)
                {
                    double z = _vertices[vertexIndex].z;
                    _vertices[vertexIndex] = new Vector3(newPosition.X, newPosition.Y, z);
                }
            }
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Mesh mesh = base.Clone() as Mesh;
            mesh._vertices = new List<Vector3>(_vertices);
            mesh._faces = new List<int[]>();
            foreach (int[] face in _faces)
            {
                mesh._faces.Add((int[])face.Clone());
            }
            mesh._edges = new List<MeshEdge>();
            foreach (MeshEdge edge in _edges)
            {
                mesh._edges.Add(edge.Clone());
            }
            mesh._subdivisionLevel = _subdivisionLevel;
            return mesh;
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new Mesh();
        }

        /// <summary>
        /// 写XML
        /// </summary>

        /// <summary>
        /// 读XML
        /// </summary>
    }

    /// <summary>
    /// 网格�?
    /// </summary>
    public class MeshEdge
    {
        /// <summary>
        /// 起始顶点索引
        /// </summary>
        public int StartVertexIndex { get; set; }

        /// <summary>
        /// 结束顶点索引
        /// </summary>
        public int EndVertexIndex { get; set; }

        /// <summary>
        /// 折痕值（-1表示始终保留�?表示无折痕）
        /// </summary>
        public double Crease { get; set; }

        /// <summary>
        /// 构造函�?
        /// </summary>
        public MeshEdge(int startIndex, int endIndex, double crease = 0)
        {
            StartVertexIndex = startIndex;
            EndVertexIndex = endIndex;
            Crease = crease;
        }

        /// <summary>
        /// 克隆
        /// </summary>
        public MeshEdge Clone()
        {
            return new MeshEdge(StartVertexIndex, EndVertexIndex, Crease);
        }
    }
}