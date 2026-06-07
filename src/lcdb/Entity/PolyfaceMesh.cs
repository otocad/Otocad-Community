using System;
using System.Collections.Generic;
using System.Linq;
using LitMath;
using lcdb.Colors;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 多面网格
    /// </summary>
    public class PolyfaceMesh : Entity
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className
        {
            get { return "PolyfaceMesh"; }
        }

        /// <summary>
        /// 顶点列表
        /// </summary>
        private List<Vector3> _vertices = new List<Vector3>();

        /// <summary>
        /// 面列�?
        /// </summary>
        private List<PolyfaceMeshFace> _faces = new List<PolyfaceMeshFace>();

        /// <summary>
        /// 获取或设置顶点列�?
        /// </summary>
        public List<Vector3> Vertices
        {
            get { return _vertices; }
            set { _vertices = value ?? new List<Vector3>(); }
        }

        /// <summary>
        /// 获取面列表（只读�?
        /// </summary>
        public IReadOnlyList<PolyfaceMeshFace> Faces
        {
            get { return _faces; }
        }

        /// <summary>
        /// 添加顶点
        /// </summary>
        /// <returns>顶点索引�?-based�?/returns>
        public int AddVertex(Vector3 vertex)
        {
            _vertices.Add(vertex);
            return _vertices.Count; // 返回1-based索引
        }

        /// <summary>
        /// 添加�?
        /// </summary>
        public void AddFace(PolyfaceMeshFace face)
        {
            if (face == null)
            {
                throw new ArgumentNullException(nameof(face));
            }

            // 验证顶点索引
            foreach (short index in face.VertexIndexes)
            {
                int absIndex = Math.Abs(index);
                if (absIndex < 1 || absIndex > _vertices.Count)
                {
                    throw new IndexOutOfRangeException($"Vertex index {index} is out of range");
                }
            }

            _faces.Add(face);
        }

        /// <summary>
        /// 创建三角形面
        /// </summary>
        public void AddTriangleFace(short v1, short v2, short v3, Color color = default(Color), ObjectId layerId = default(ObjectId))
        {
            var face = new PolyfaceMeshFace(new short[] { v1, v2, v3 });
            face.Color = color;
            face.LayerId = layerId;
            AddFace(face);
        }

        /// <summary>
        /// 创建四边形面
        /// </summary>
        public void AddQuadFace(short v1, short v2, short v3, short v4, Color color = default(Color), ObjectId layerId = default(ObjectId))
        {
            var face = new PolyfaceMeshFace(new short[] { v1, v2, v3, v4 });
            face.Color = color;
            face.LayerId = layerId;
            AddFace(face);
        }

        /// <summary>
        /// 分解为基本实�?
        /// </summary>
        public List<Entity> Explode()
        {
            List<Entity> entities = new List<Entity>();

            // 创建顶点�?
            foreach (Vector3 vertex in _vertices)
            {
                Point point = new Point();
                point.position = new Vector2(vertex.X, vertex.Y);
                point.color = this.color;
                point.layerId = this.layerId;
                entities.Add(point);
            }

            // 创建�?
            foreach (PolyfaceMeshFace face in _faces)
            {
                if (face.VertexIndexes.Length >= 3)
                {
                    Face3D face3D = new Face3D();
                    
                    // 设置顶点（注意索引是1-based的）
                    int absIndex1 = Math.Abs(face.VertexIndexes[0]) - 1;
                    int absIndex2 = Math.Abs(face.VertexIndexes[1]) - 1;
                    int absIndex3 = Math.Abs(face.VertexIndexes[2]) - 1;
                    
                    face3D.FirstVertex = _vertices[absIndex1];
                    face3D.SecondVertex = _vertices[absIndex2];
                    face3D.ThirdVertex = _vertices[absIndex3];
                    
                    if (face.VertexIndexes.Length > 3)
                    {
                        int absIndex4 = Math.Abs(face.VertexIndexes[3]) - 1;
                        face3D.FourthVertex = _vertices[absIndex4];
                    }
                    else
                    {
                        face3D.FourthVertex = face3D.ThirdVertex; // 三角�?
                    }
                    
                    // 设置边可见性（负数表示不可见）
                    Face3DEdgeFlags edgeFlags = Face3DEdgeFlags.None;
                    if (face.VertexIndexes[0] < 0) edgeFlags |= Face3DEdgeFlags.FirstEdgeInvisible;
                    if (face.VertexIndexes[1] < 0) edgeFlags |= Face3DEdgeFlags.SecondEdgeInvisible;
                    if (face.VertexIndexes[2] < 0) edgeFlags |= Face3DEdgeFlags.ThirdEdgeInvisible;
                    if (face.VertexIndexes.Length > 3 && face.VertexIndexes[3] < 0) 
                        edgeFlags |= Face3DEdgeFlags.FourthEdgeInvisible;
                    
                    face3D.EdgeFlags = edgeFlags;
                    
                    // 设置颜色和图�?
                    face3D.color = face.Color.Equals(default(Color)) ? this.color : face.Color;
                    face3D.layerId = face.LayerId != ObjectId.Null ? face.LayerId : this.layerId;
                    
                    entities.Add(face3D);
                }
            }

            return entities;
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
            // 绘制所有可见的�?
            HashSet<string> drawnEdges = new HashSet<string>();

            foreach (PolyfaceMeshFace face in _faces)
            {
                // 临时存储当前面的颜色
                System.Drawing.Color? originalColor = null;
                if (face.Color != null && face.Color.colorMethod != ColorMethod.ByLayer)
                {
                    // TODO: 保存并设置面的颜�?
                }

                for (int i = 0; i < face.VertexIndexes.Length; i++)
                {
                    short v1Index = face.VertexIndexes[i];
                    short v2Index = face.VertexIndexes[(i + 1) % face.VertexIndexes.Length];

                    // 检查边是否可见（负数表示不可见�?
                    if (v1Index < 0) continue;

                    int absV1 = Math.Abs(v1Index) - 1; // 转换�?-based
                    int absV2 = Math.Abs(v2Index) - 1;

                    if (absV1 >= 0 && absV1 < _vertices.Count &&
                        absV2 >= 0 && absV2 < _vertices.Count)
                    {
                        // 创建边的唯一标识�?
                        string edgeKey = Math.Min(absV1, absV2) + "_" + Math.Max(absV1, absV2);

                        if (!drawnEdges.Contains(edgeKey))
                        {
                            Vector3 vertex1 = _vertices[absV1];
                            Vector3 vertex2 = _vertices[absV2];

                            // 投影�?D
                            Vector2 p1 = new Vector2(vertex1.X, vertex1.Y);
                            Vector2 p2 = new Vector2(vertex2.X, vertex2.Y);

                            gd.DrawLine(p1, p2);
                            drawnEdges.Add(edgeKey);
                        }
                    }
                }

                // 恢复颜色
                if (originalColor.HasValue)
                {
                    // TODO: 恢复原始颜色
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
                grip.xData1 = i; // 存储顶点索引�?-based�?
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
            PolyfaceMesh mesh = base.Clone() as PolyfaceMesh;
            mesh._vertices = new List<Vector3>(_vertices);
            mesh._faces = new List<PolyfaceMeshFace>();
            foreach (PolyfaceMeshFace face in _faces)
            {
                mesh._faces.Add(face.Clone());
            }
            return mesh;
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new PolyfaceMesh();
        }

        /// <summary>
        /// 写XML
        /// </summary>

        /// <summary>
        /// 读XML
        /// </summary>
    }

    /// <summary>
    /// 多面网格的面
    /// </summary>
    public class PolyfaceMeshFace
    {
        /// <summary>
        /// 顶点索引数组�?-based，负数表示边不可见）
        /// </summary>
        public short[] VertexIndexes { get; set; }

        /// <summary>
        /// 面的颜色（null表示继承自网格）
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// 面的图层（Null表示继承自网格）
        /// </summary>
        public ObjectId LayerId { get; set; }

        /// <summary>
        /// 构造函�?
        /// </summary>
        public PolyfaceMeshFace(short[] vertexIndexes)
        {
            if (vertexIndexes == null || vertexIndexes.Length < 3 || vertexIndexes.Length > 4)
            {
                throw new ArgumentException("Face must have 3 or 4 vertices");
            }
            VertexIndexes = vertexIndexes;
            LayerId = ObjectId.Null;
        }

        /// <summary>
        /// 克隆
        /// </summary>
        public PolyfaceMeshFace Clone()
        {
            var clone = new PolyfaceMeshFace((short[])VertexIndexes.Clone());
            clone.Color = Color;
            clone.LayerId = LayerId;
            return clone;
        }
    }
}