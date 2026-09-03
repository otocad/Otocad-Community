using System;
using System.Collections.Generic;
using LitMath;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 3D面实体
    /// 原生OtoCAD实现，表示三维空间中的三角形或四边形面
    /// </summary>
    [Serializable]
    public class Face3D : Entity
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "Face3D";

        #region 字段

        // 第一个顶点
        private Vector3 _firstVertex = new Vector3();
        
        // 第二个顶点
        private Vector3 _secondVertex = new Vector3();
        
        // 第三个顶点
        private Vector3 _thirdVertex = new Vector3();
        
        // 第四个顶点
        private Vector3 _fourthVertex = new Vector3();
        
        // 边可见性标志
        private Face3DEdgeFlags _edgeFlags = Face3DEdgeFlags.None;

        #endregion

        #region 属性

        /// <summary>
        /// 第一个顶点
        /// </summary>
        public Vector3 FirstVertex
        {
            get { return _firstVertex; }
            set { _firstVertex = value; }
        }

        /// <summary>
        /// 第二个顶点
        /// </summary>
        public Vector3 SecondVertex
        {
            get { return _secondVertex; }
            set { _secondVertex = value; }
        }

        /// <summary>
        /// 第三个顶点
        /// </summary>
        public Vector3 ThirdVertex
        {
            get { return _thirdVertex; }
            set { _thirdVertex = value; }
        }

        /// <summary>
        /// 第四个顶点
        /// </summary>
        public Vector3 FourthVertex
        {
            get { return _fourthVertex; }
            set { _fourthVertex = value; }
        }

        /// <summary>
        /// 边可见性标志
        /// </summary>
        public Face3DEdgeFlags EdgeFlags
        {
            get { return _edgeFlags; }
            set { _edgeFlags = value; }
        }

        /// <summary>
        /// 是否为三角形（第三和第四个顶点相同）
        /// </summary>
        public bool IsTriangle
        {
            get 
            { 
                return (_thirdVertex.X == _fourthVertex.X && 
                        _thirdVertex.Y == _fourthVertex.Y && 
                        _thirdVertex.Z == _fourthVertex.Z);
            }
        }

        /// <summary>
        /// 获取面的法向量
        /// </summary>
        public Vector3 FaceNormal
        {
            get
            {
                Vector3 v1 = _secondVertex - _firstVertex;
                Vector3 v2 = _thirdVertex - _firstVertex;
                Vector3 normal = Vector3.Cross(v1, v2);
                return normal.normalized;
            }
        }

        /// <summary>
        /// 获取面的中心点
        /// </summary>
        public Vector3 Center
        {
            get
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
        }


        /// <summary>
        /// 外围边框（2D投影）
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                double minX = Math.Min(Math.Min(_firstVertex.X, _secondVertex.X), 
                                     Math.Min(_thirdVertex.X, _fourthVertex.X));
                double minY = Math.Min(Math.Min(_firstVertex.Y, _secondVertex.Y), 
                                     Math.Min(_thirdVertex.Y, _fourthVertex.Y));
                double maxX = Math.Max(Math.Max(_firstVertex.X, _secondVertex.X), 
                                     Math.Max(_thirdVertex.X, _fourthVertex.X));
                double maxY = Math.Max(Math.Max(_firstVertex.Y, _secondVertex.Y), 
                                     Math.Max(_thirdVertex.Y, _fourthVertex.Y));
                
                return new Bounding(new Vector2(minX, minY), new Vector2(maxX, maxY));
            }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建3D面
        /// </summary>
        public Face3D() : base()
        {
        }

        /// <summary>
        /// 创建三角形3D面
        /// </summary>
        /// <param name="v1">第一个顶点</param>
        /// <param name="v2">第二个顶点</param>
        /// <param name="v3">第三个顶点</param>
        public Face3D(Vector3 v1, Vector3 v2, Vector3 v3) : base()
        {
            _firstVertex = v1;
            _secondVertex = v2;
            _thirdVertex = v3;
            _fourthVertex = v3; // 三角形时第四个顶点等于第三个
        }

        /// <summary>
        /// 创建四边形3D面
        /// </summary>
        /// <param name="v1">第一个顶点</param>
        /// <param name="v2">第二个顶点</param>
        /// <param name="v3">第三个顶点</param>
        /// <param name="v4">第四个顶点</param>
        public Face3D(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) : base()
        {
            _firstVertex = v1;
            _secondVertex = v2;
            _thirdVertex = v3;
            _fourthVertex = v4;
        }

        /// <summary>
        /// 从2D点创建3D面（Z=0）
        /// </summary>
        /// <param name="v1">第一个顶点</param>
        /// <param name="v2">第二个顶点</param>
        /// <param name="v3">第三个顶点</param>
        public Face3D(Vector2 v1, Vector2 v2, Vector2 v3) : base()
        {
            _firstVertex = new Vector3(v1.X, v1.Y, 0);
            _secondVertex = new Vector3(v2.X, v2.Y, 0);
            _thirdVertex = new Vector3(v3.X, v3.Y, 0);
            _fourthVertex = _thirdVertex;
        }

        /// <summary>
        /// 从2D点创建3D面（Z=0）
        /// </summary>
        /// <param name="v1">第一个顶点</param>
        /// <param name="v2">第二个顶点</param>
        /// <param name="v3">第三个顶点</param>
        /// <param name="v4">第四个顶点</param>
        public Face3D(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4) : base()
        {
            _firstVertex = new Vector3(v1.X, v1.Y, 0);
            _secondVertex = new Vector3(v2.X, v2.Y, 0);
            _thirdVertex = new Vector3(v3.X, v3.Y, 0);
            _fourthVertex = new Vector3(v4.X, v4.Y, 0);
        }

        #endregion

        #region 方法

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            // 2D投影绘制
            Vector2[] vertices = GetProjectedVertices();
            
            if (IsTriangle)
            {
                // 绘制三角形
                if ((_edgeFlags & Face3DEdgeFlags.FirstEdgeInvisible) == 0)
                    gd.DrawLine(vertices[0], vertices[1]);
                if ((_edgeFlags & Face3DEdgeFlags.SecondEdgeInvisible) == 0)
                    gd.DrawLine(vertices[1], vertices[2]);
                if ((_edgeFlags & Face3DEdgeFlags.ThirdEdgeInvisible) == 0)
                    gd.DrawLine(vertices[2], vertices[0]);
            }
            else
            {
                // 绘制四边形
                if ((_edgeFlags & Face3DEdgeFlags.FirstEdgeInvisible) == 0)
                    gd.DrawLine(vertices[0], vertices[1]);
                if ((_edgeFlags & Face3DEdgeFlags.SecondEdgeInvisible) == 0)
                    gd.DrawLine(vertices[1], vertices[2]);
                if ((_edgeFlags & Face3DEdgeFlags.ThirdEdgeInvisible) == 0)
                    gd.DrawLine(vertices[2], vertices[3]);
                if ((_edgeFlags & Face3DEdgeFlags.FourthEdgeInvisible) == 0)
                    gd.DrawLine(vertices[3], vertices[0]);
            }
            
            // 注意：实际的填充效果需要在渲染层处理
        }

        /// <summary>
        /// 获取投影后的2D顶点
        /// </summary>
        private Vector2[] GetProjectedVertices()
        {
            return new Vector2[]
            {
                new Vector2(_firstVertex.X, _firstVertex.Y),
                new Vector2(_secondVertex.X, _secondVertex.Y),
                new Vector2(_thirdVertex.X, _thirdVertex.Y),
                new Vector2(_fourthVertex.X, _fourthVertex.Y)
            };
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new Face3D();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Face3D face = base.Clone() as Face3D;
            face._firstVertex = _firstVertex;
            face._secondVertex = _secondVertex;
            face._thirdVertex = _thirdVertex;
            face._fourthVertex = _fourthVertex;
            face._edgeFlags = _edgeFlags;
            return face;
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            _firstVertex = new Vector3(_firstVertex.X + translation.X, _firstVertex.Y + translation.Y, _firstVertex.Z);
            _secondVertex = new Vector3(_secondVertex.X + translation.X, _secondVertex.Y + translation.Y, _secondVertex.Z);
            _thirdVertex = new Vector3(_thirdVertex.X + translation.X, _thirdVertex.Y + translation.Y, _thirdVertex.Z);
            _fourthVertex = new Vector3(_fourthVertex.X + translation.X, _fourthVertex.Y + translation.Y, _fourthVertex.Z);
        }

        /// <summary>
        /// 旋转（绕Z轴）
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            _firstVertex = RotateVertex(_firstVertex, center, angle);
            _secondVertex = RotateVertex(_secondVertex, center, angle);
            _thirdVertex = RotateVertex(_thirdVertex, center, angle);
            _fourthVertex = RotateVertex(_fourthVertex, center, angle);
        }

        /// <summary>
        /// 旋转单个顶点
        /// </summary>
        private Vector3 RotateVertex(Vector3 vertex, Vector2 center, double angle)
        {
            Vector2 v2d = new Vector2(vertex.X, vertex.Y);
            Vector2 rotated = Vector2.RotateInRadian(v2d, center, angle);
            return new Vector3(rotated.X, rotated.Y, vertex.Z);
        }

        /// <summary>
        /// 变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            // 对每个顶点应用2D变换，保持Z坐标
            Vector2 v1 = transform * new Vector2(_firstVertex.X, _firstVertex.Y);
            Vector2 v2 = transform * new Vector2(_secondVertex.X, _secondVertex.Y);
            Vector2 v3 = transform * new Vector2(_thirdVertex.X, _thirdVertex.Y);
            Vector2 v4 = transform * new Vector2(_fourthVertex.X, _fourthVertex.Y);
            
            _firstVertex = new Vector3(v1.X, v1.Y, _firstVertex.Z);
            _secondVertex = new Vector3(v2.X, v2.Y, _secondVertex.Z);
            _thirdVertex = new Vector3(v3.X, v3.Y, _thirdVertex.Z);
            _fourthVertex = new Vector3(v4.X, v4.Y, _fourthVertex.Z);
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            
            // 顶点夹点
            gripPoints.Add(new GripPoint(GripPointType.End, new Vector2(_firstVertex.X, _firstVertex.Y)));
            gripPoints.Add(new GripPoint(GripPointType.End, new Vector2(_secondVertex.X, _secondVertex.Y)));
            gripPoints.Add(new GripPoint(GripPointType.End, new Vector2(_thirdVertex.X, _thirdVertex.Y)));
            
            if (!IsTriangle)
            {
                gripPoints.Add(new GripPoint(GripPointType.End, new Vector2(_fourthVertex.X, _fourthVertex.Y)));
            }
            
            // 中心点夹点
            Vector3 center3d = Center;
            gripPoints.Add(new GripPoint(GripPointType.Center, new Vector2(center3d.X, center3d.Y)));
            
            return gripPoints;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            if (IsTriangle && index < 4)
            {
                // 三角形模式
                switch (index)
                {
                    case 0:
                        _firstVertex = new Vector3(newPosition.X, newPosition.Y, _firstVertex.Z);
                        break;
                    case 1:
                        _secondVertex = new Vector3(newPosition.X, newPosition.Y, _secondVertex.Z);
                        break;
                    case 2:
                        _thirdVertex = new Vector3(newPosition.X, newPosition.Y, _thirdVertex.Z);
                        _fourthVertex = _thirdVertex;
                        break;
                    case 3: // 中心点
                        Vector3 currentCenter = Center;
                        Vector2 delta = newPosition - new Vector2(currentCenter.X, currentCenter.Y);
                        Translate(delta);
                        break;
                }
            }
            else if (!IsTriangle && index < 5)
            {
                // 四边形模式
                switch (index)
                {
                    case 0:
                        _firstVertex = new Vector3(newPosition.X, newPosition.Y, _firstVertex.Z);
                        break;
                    case 1:
                        _secondVertex = new Vector3(newPosition.X, newPosition.Y, _secondVertex.Z);
                        break;
                    case 2:
                        _thirdVertex = new Vector3(newPosition.X, newPosition.Y, _thirdVertex.Z);
                        break;
                    case 3:
                        _fourthVertex = new Vector3(newPosition.X, newPosition.Y, _fourthVertex.Z);
                        break;
                    case 4: // 中心点
                        Vector3 currentCenter = Center;
                        Vector2 delta = newPosition - new Vector2(currentCenter.X, currentCenter.Y);
                        Translate(delta);
                        break;
                }
            }
        }

        /// <summary>
        /// 获取捕捉点
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPoints = new List<ObjectSnapPoint>();
            
            // 顶点捕捉点
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, new Vector2(_firstVertex.X, _firstVertex.Y)));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, new Vector2(_secondVertex.X, _secondVertex.Y)));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, new Vector2(_thirdVertex.X, _thirdVertex.Y)));
            
            if (!IsTriangle)
            {
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, new Vector2(_fourthVertex.X, _fourthVertex.Y)));
            }
            
            // 边中点捕捉点
            Vector2[] vertices = GetProjectedVertices();
            if (IsTriangle)
            {
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (vertices[0] + vertices[1]) * 0.5));
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (vertices[1] + vertices[2]) * 0.5));
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (vertices[2] + vertices[0]) * 0.5));
            }
            else
            {
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (vertices[0] + vertices[1]) * 0.5));
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (vertices[1] + vertices[2]) * 0.5));
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (vertices[2] + vertices[3]) * 0.5));
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (vertices[3] + vertices[0]) * 0.5));
            }
            
            // 中心点捕捉点
            Vector3 center3d = Center;
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Center, new Vector2(center3d.X, center3d.Y)));
            
            return snapPoints;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 计算面积
        /// </summary>
        public double GetArea()
        {
            if (IsTriangle)
            {
                // 使用海伦公式计算三角形面积
                Vector3 v1 = _secondVertex - _firstVertex;
                Vector3 v2 = _thirdVertex - _firstVertex;
                Vector3 cross = Vector3.Cross(v1, v2);
                return 0.5 * cross.length;
            }
            else
            {
                // 将四边形分成两个三角形计算面积
                Vector3 v1 = _secondVertex - _firstVertex;
                Vector3 v2 = _thirdVertex - _firstVertex;
                Vector3 cross1 = Vector3.Cross(v1, v2);
                
                Vector3 v3 = _fourthVertex - _firstVertex;
                Vector3 cross2 = Vector3.Cross(v2, v3);
                
                return 0.5 * (cross1.length + cross2.length);
            }
        }

        /// <summary>
        /// 反转面的方向（改变顶点顺序）
        /// </summary>
        public void Reverse()
        {
            if (IsTriangle)
            {
                // 交换第二和第三个顶点
                Vector3 temp = _secondVertex;
                _secondVertex = _thirdVertex;
                _thirdVertex = temp;
                _fourthVertex = _thirdVertex;
            }
            else
            {
                // 交换第二和第四个顶点
                Vector3 temp = _secondVertex;
                _secondVertex = _fourthVertex;
                _fourthVertex = temp;
            }
        }

        /// <summary>
        /// 设置Z坐标
        /// </summary>
        public void SetElevation(double z)
        {
            _firstVertex = new Vector3(_firstVertex.X, _firstVertex.Y, z);
            _secondVertex = new Vector3(_secondVertex.X, _secondVertex.Y, z);
            _thirdVertex = new Vector3(_thirdVertex.X, _thirdVertex.Y, z);
            _fourthVertex = new Vector3(_fourthVertex.X, _fourthVertex.Y, z);
        }

        #endregion
    }

    /// <summary>
    /// 3D面边可见性标志
    /// </summary>
    [Flags]
    public enum Face3DEdgeFlags
    {
        /// <summary>
        /// 所有边都可见
        /// </summary>
        None = 0,
        
        /// <summary>
        /// 第一条边不可见
        /// </summary>
        FirstEdgeInvisible = 1,
        
        /// <summary>
        /// 第二条边不可见
        /// </summary>
        SecondEdgeInvisible = 2,
        
        /// <summary>
        /// 第三条边不可见
        /// </summary>
        ThirdEdgeInvisible = 4,
        
        /// <summary>
        /// 第四条边不可见
        /// </summary>
        FourthEdgeInvisible = 8,
        
        /// <summary>
        /// 所有边都不可见
        /// </summary>
        AllEdgesInvisible = FirstEdgeInvisible | SecondEdgeInvisible | ThirdEdgeInvisible | FourthEdgeInvisible
    }
}