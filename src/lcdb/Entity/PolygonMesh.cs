using System;
using System.Collections.Generic;
using LitMath;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 多边形网�?
    /// 表示由U×V个顶点组成的网格曲面
    /// </summary>
    public class PolygonMesh : Entity
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className
        {
            get { return "PolygonMesh"; }
        }

        /// <summary>
        /// 顶点数组（U×V�?
        /// </summary>
        private Vector3[,] _vertices;

        /// <summary>
        /// U方向顶点数（2-256�?
        /// </summary>
        private short _u = 2;

        /// <summary>
        /// V方向顶点数（2-256�?
        /// </summary>
        private short _v = 2;

        /// <summary>
        /// U方向平滑密度�?-201�?
        /// </summary>
        private short _densityU = 6;

        /// <summary>
        /// V方向平滑密度�?-201�?
        /// </summary>
        private short _densityV = 6;

        /// <summary>
        /// U方向是否闭合
        /// </summary>
        private bool _isClosedInU = false;

        /// <summary>
        /// V方向是否闭合
        /// </summary>
        private bool _isClosedInV = false;

        /// <summary>
        /// 平滑类型
        /// </summary>
        private PolylineSmoothType _smoothType = PolylineSmoothType.NoSmooth;

        /// <summary>
        /// 默认U方向表面密度
        /// </summary>
        public static short DefaultSurfU = 6;

        /// <summary>
        /// 默认V方向表面密度
        /// </summary>
        public static short DefaultSurfV = 6;

        /// <summary>
        /// 构造函�?
        /// </summary>
        public PolygonMesh() : this(2, 2)
        {
        }

        /// <summary>
        /// 构造函�?
        /// </summary>
        public PolygonMesh(short u, short v)
        {
            U = u;
            V = v;
        }

        /// <summary>
        /// 获取或设置U方向顶点�?
        /// </summary>
        public short U
        {
            get { return _u; }
            set
            {
                if (value < 2 || value > 256)
                    throw new ArgumentOutOfRangeException("U must be between 2 and 256");
                
                if (_u != value)
                {
                    _u = value;
                    InitializeVertices();
                }
            }
        }

        /// <summary>
        /// 获取或设置V方向顶点�?
        /// </summary>
        public short V
        {
            get { return _v; }
            set
            {
                if (value < 2 || value > 256)
                    throw new ArgumentOutOfRangeException("V must be between 2 and 256");
                
                if (_v != value)
                {
                    _v = value;
                    InitializeVertices();
                }
            }
        }

        /// <summary>
        /// 获取或设置U方向密度
        /// </summary>
        public short DensityU
        {
            get { return _densityU; }
            set
            {
                if (value < 3 || value > 201)
                    throw new ArgumentOutOfRangeException("DensityU must be between 3 and 201");
                _densityU = value;
            }
        }

        /// <summary>
        /// 获取或设置V方向密度
        /// </summary>
        public short DensityV
        {
            get { return _densityV; }
            set
            {
                if (value < 3 || value > 201)
                    throw new ArgumentOutOfRangeException("DensityV must be between 3 and 201");
                _densityV = value;
            }
        }

        /// <summary>
        /// 获取或设置U方向是否闭合
        /// </summary>
        public bool IsClosedInU
        {
            get { return _isClosedInU; }
            set { _isClosedInU = value; }
        }

        /// <summary>
        /// 获取或设置V方向是否闭合
        /// </summary>
        public bool IsClosedInV
        {
            get { return _isClosedInV; }
            set { _isClosedInV = value; }
        }

        /// <summary>
        /// 获取或设置平滑类�?
        /// </summary>
        public PolylineSmoothType SmoothType
        {
            get { return _smoothType; }
            set { _smoothType = value; }
        }

        /// <summary>
        /// 初始化顶点数�?
        /// </summary>
        private void InitializeVertices()
        {
            _vertices = new Vector3[_u, _v];
            for (int i = 0; i < _u; i++)
            {
                for (int j = 0; j < _v; j++)
                {
                    _vertices[i, j] = new Vector3(i, j, 0);
                }
            }
        }

        /// <summary>
        /// 设置顶点
        /// </summary>
        public void SetVertex(int i, int j, Vector3 vertex)
        {
            if (i < 0 || i >= _u)
                throw new IndexOutOfRangeException($"i must be between 0 and {_u - 1}");
            if (j < 0 || j >= _v)
                throw new IndexOutOfRangeException($"j must be between 0 and {_v - 1}");
            
            _vertices[i, j] = vertex;
        }

        /// <summary>
        /// 获取顶点
        /// </summary>
        public Vector3 GetVertex(int i, int j)
        {
            if (i < 0 || i >= _u)
                throw new IndexOutOfRangeException($"i must be between 0 and {_u - 1}");
            if (j < 0 || j >= _v)
                throw new IndexOutOfRangeException($"j must be between 0 and {_v - 1}");
            
            return _vertices[i, j];
        }

        /// <summary>
        /// 获取平滑后的网格顶点（基于密度）
        /// </summary>
        public Vector3[,] MeshVertices()
        {
            // 简化实现：返回原始顶点
            // 实际实现需要根据SmoothType和Density进行B样条插�?
            return (Vector3[,])_vertices.Clone();
        }

        /// <summary>
        /// 转换为Mesh实体
        /// </summary>
        public Mesh ToMesh()
        {
            Mesh mesh = new Mesh();
            
            // 添加所有顶�?
            int[,] vertexIndices = new int[_u, _v];
            for (int i = 0; i < _u; i++)
            {
                for (int j = 0; j < _v; j++)
                {
                    vertexIndices[i, j] = mesh.AddVertex(_vertices[i, j]);
                }
            }
            
            // 创建�?
            for (int i = 0; i < _u - 1; i++)
            {
                for (int j = 0; j < _v - 1; j++)
                {
                    // 创建四边形面
                    mesh.AddQuadFace(
                        vertexIndices[i, j],
                        vertexIndices[i + 1, j],
                        vertexIndices[i + 1, j + 1],
                        vertexIndices[i, j + 1]
                    );
                }
            }
            
            // 处理闭合情况
            if (_isClosedInU && _u > 2)
            {
                for (int j = 0; j < _v - 1; j++)
                {
                    mesh.AddQuadFace(
                        vertexIndices[_u - 1, j],
                        vertexIndices[0, j],
                        vertexIndices[0, j + 1],
                        vertexIndices[_u - 1, j + 1]
                    );
                }
            }
            
            if (_isClosedInV && _v > 2)
            {
                for (int i = 0; i < _u - 1; i++)
                {
                    mesh.AddQuadFace(
                        vertexIndices[i, _v - 1],
                        vertexIndices[i + 1, _v - 1],
                        vertexIndices[i + 1, 0],
                        vertexIndices[i, 0]
                    );
                }
            }
            
            if (_isClosedInU && _isClosedInV && _u > 2 && _v > 2)
            {
                mesh.AddQuadFace(
                    vertexIndices[_u - 1, _v - 1],
                    vertexIndices[0, _v - 1],
                    vertexIndices[0, 0],
                    vertexIndices[_u - 1, 0]
                );
            }
            
            mesh.color = this.color;
            mesh.layerId = this.layerId;
            
            return mesh;
        }

        /// <summary>
        /// 分解为Face3D实体
        /// </summary>
        public List<Face3D> Explode()
        {
            List<Face3D> faces = new List<Face3D>();
            
            for (int i = 0; i < _u - 1; i++)
            {
                for (int j = 0; j < _v - 1; j++)
                {
                    Face3D face = new Face3D();
                    face.FirstVertex = _vertices[i, j];
                    face.SecondVertex = _vertices[i + 1, j];
                    face.ThirdVertex = _vertices[i + 1, j + 1];
                    face.FourthVertex = _vertices[i, j + 1];
                    face.color = this.color;
                    face.layerId = this.layerId;
                    faces.Add(face);
                }
            }
            
            // 处理闭合情况
            if (_isClosedInU && _u > 2)
            {
                for (int j = 0; j < _v - 1; j++)
                {
                    Face3D face = new Face3D();
                    face.FirstVertex = _vertices[_u - 1, j];
                    face.SecondVertex = _vertices[0, j];
                    face.ThirdVertex = _vertices[0, j + 1];
                    face.FourthVertex = _vertices[_u - 1, j + 1];
                    face.color = this.color;
                    face.layerId = this.layerId;
                    faces.Add(face);
                }
            }
            
            if (_isClosedInV && _v > 2)
            {
                for (int i = 0; i < _u - 1; i++)
                {
                    Face3D face = new Face3D();
                    face.FirstVertex = _vertices[i, _v - 1];
                    face.SecondVertex = _vertices[i + 1, _v - 1];
                    face.ThirdVertex = _vertices[i + 1, 0];
                    face.FourthVertex = _vertices[i, 0];
                    face.color = this.color;
                    face.layerId = this.layerId;
                    faces.Add(face);
                }
            }
            
            if (_isClosedInU && _isClosedInV && _u > 2 && _v > 2)
            {
                Face3D face = new Face3D();
                face.FirstVertex = _vertices[_u - 1, _v - 1];
                face.SecondVertex = _vertices[0, _v - 1];
                face.ThirdVertex = _vertices[0, 0];
                face.FourthVertex = _vertices[_u - 1, 0];
                face.color = this.color;
                face.layerId = this.layerId;
                faces.Add(face);
            }
            
            return faces;
        }

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                if (_vertices != null && _u > 0 && _v > 0)
                {
                    double minX = double.MaxValue;
                    double minY = double.MaxValue;
                    double maxX = double.MinValue;
                    double maxY = double.MinValue;

                    for (int i = 0; i < _u; i++)
                    {
                        for (int j = 0; j < _v; j++)
                        {
                            Vector3 vertex = _vertices[i, j];
                            minX = Math.Min(minX, vertex.X);
                            minY = Math.Min(minY, vertex.Y);
                            maxX = Math.Max(maxX, vertex.X);
                            maxY = Math.Max(maxY, vertex.Y);
                        }
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
            if (_vertices == null) return;

            // 绘制U方向的线
            for (int i = 0; i < _u; i++)
            {
                int vLimit = _isClosedInV ? _v : _v - 1;
                for (int j = 0; j < vLimit; j++)
                {
                    int nextJ = (j + 1) % _v;
                    Vector3 v1 = _vertices[i, j];
                    Vector3 v2 = _vertices[i, nextJ];
                    gd.DrawLine(new Vector2(v1.X, v1.Y), new Vector2(v2.X, v2.Y));
                }
            }

            // 绘制V方向的线
            for (int j = 0; j < _v; j++)
            {
                int uLimit = _isClosedInU ? _u : _u - 1;
                for (int i = 0; i < uLimit; i++)
                {
                    int nextI = (i + 1) % _u;
                    Vector3 v1 = _vertices[i, j];
                    Vector3 v2 = _vertices[nextI, j];
                    gd.DrawLine(new Vector2(v1.X, v1.Y), new Vector2(v2.X, v2.Y));
                }
            }
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            Vector3 translation3D = new Vector3(translation.X, translation.Y, 0);
            for (int i = 0; i < _u; i++)
            {
                for (int j = 0; j < _v; j++)
                {
                    _vertices[i, j] = _vertices[i, j] + translation3D;
                }
            }
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            Vector3 center3D = new Vector3(center.X, center.Y, 0);
            
            for (int i = 0; i < _u; i++)
            {
                for (int j = 0; j < _v; j++)
                {
                    Vector3 relativePos = _vertices[i, j] - center3D;
                    // Rotate around Z axis
                    double cos = Math.Cos(angle);
                    double sin = Math.Sin(angle);
                    double newX = relativePos.X * cos - relativePos.Y * sin;
                    double newY = relativePos.X * sin + relativePos.Y * cos;
                    _vertices[i, j] = new Vector3(newX, newY, relativePos.Z) + center3D;
                }
            }
        }

        /// <summary>
        /// 变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            for (int i = 0; i < _u; i++)
            {
                for (int j = 0; j < _v; j++)
                {
                    Vector2 vertex2D = new Vector2(_vertices[i, j].x, _vertices[i, j].y);
                    Vector2 transformed = transform * vertex2D;
                    _vertices[i, j] = new Vector3(transformed.X, transformed.Y, _vertices[i, j].z);
                }
            }
        }

        /// <summary>
        /// 对象捕捉�?
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPnts = new List<ObjectSnapPoint>();
            
            // 所有顶点作为端点捕�?
            for (int i = 0; i < _u; i++)
            {
                for (int j = 0; j < _v; j++)
                {
                    Vector3 vertex = _vertices[i, j];
                    snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.End, new Vector2(vertex.X, vertex.Y)));
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
            for (int i = 0; i < _u; i++)
            {
                for (int j = 0; j < _v; j++)
                {
                    Vector3 vertex = _vertices[i, j];
                    GripPoint grip = new GripPoint(GripPointType.End, new Vector2(vertex.X, vertex.Y));
                    grip.xData1 = new int[] { i, j }; // 存储索引
                    gripPnts.Add(grip);
                }
            }
            
            return gripPnts;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            if (gripPoint.type == GripPointType.End && gripPoint.xData1 is int[] indices)
            {
                if (indices.Length == 2)
                {
                    int i = indices[0];
                    int j = indices[1];
                    if (i >= 0 && i < _u && j >= 0 && j < _v)
                    {
                        double z = _vertices[i, j].z;
                        _vertices[i, j] = new Vector3(newPosition.X, newPosition.Y, z);
                    }
                }
            }
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            PolygonMesh mesh = base.Clone() as PolygonMesh;
            mesh._u = _u;
            mesh._v = _v;
            mesh._densityU = _densityU;
            mesh._densityV = _densityV;
            mesh._isClosedInU = _isClosedInU;
            mesh._isClosedInV = _isClosedInV;
            mesh._smoothType = _smoothType;
            mesh._vertices = new Vector3[_u, _v];
            for (int i = 0; i < _u; i++)
            {
                for (int j = 0; j < _v; j++)
                {
                    mesh._vertices[i, j] = _vertices[i, j];
                }
            }
            return mesh;
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new PolygonMesh();
        }

        /// <summary>
        /// 写XML
        /// </summary>

        /// <summary>
        /// 读XML
        /// </summary>
    }

    /// <summary>
    /// 多段线平滑类�?
    /// </summary>
    public enum PolylineSmoothType
    {
        /// <summary>
        /// 不平�?
        /// </summary>
        NoSmooth = 0,
        
        /// <summary>
        /// 二次B样条
        /// </summary>
        Quadratic = 5,
        
        /// <summary>
        /// 三次B样条
        /// </summary>
        Cubic = 6
    }
}