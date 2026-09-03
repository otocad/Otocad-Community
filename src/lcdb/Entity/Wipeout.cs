using System;
using System.Collections.Generic;
using LitMath;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 遮罩实体
    /// 原生OtoCAD实现，用于遮盖其他实体的多边形区�?
    /// </summary>
    [Serializable]
    public class Wipeout : Entity
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "Wipeout";

        #region 字段

        // 边界顶点列表（闭合多边形�?
        private List<Vector2> _vertices = new List<Vector2>();
        
        // 是否显示边框
        private bool _showFrame = false;
        
        // 裁剪边界
        private ClippingBoundary _clippingBoundary;
        
        // 对比度（0-100�?
        private int _contrast = 50;
        
        // 亮度�?-100�?
        private int _brightness = 50;
        
        // 淡入淡出值（0-100�?
        private int _fade = 0;

        #endregion

        #region 属�?

        /// <summary>
        /// 边界顶点列表
        /// </summary>
        public List<Vector2> Vertices
        {
            get { return _vertices; }
            set { _vertices = value ?? new List<Vector2>(); }
        }

        /// <summary>
        /// 是否显示边框
        /// </summary>
        public bool ShowFrame
        {
            get { return _showFrame; }
            set { _showFrame = value; }
        }

        /// <summary>
        /// 裁剪边界
        /// </summary>
        public ClippingBoundary ClippingBoundary
        {
            get { return _clippingBoundary; }
            set { _clippingBoundary = value; }
        }

        /// <summary>
        /// 对比度（0-100�?
        /// </summary>
        public int Contrast
        {
            get { return _contrast; }
            set { _contrast = Math.Max(0, Math.Min(100, value)); }
        }

        /// <summary>
        /// 亮度�?-100�?
        /// </summary>
        public int Brightness
        {
            get { return _brightness; }
            set { _brightness = Math.Max(0, Math.Min(100, value)); }
        }

        /// <summary>
        /// 淡入淡出值（0-100�?
        /// </summary>
        public int Fade
        {
            get { return _fade; }
            set { _fade = Math.Max(0, Math.Min(100, value)); }
        }


        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                if (_vertices.Count < 3)
                {
                    return new Bounding(Vector2.Zero, Vector2.Zero);
                }

                double minX = double.MaxValue;
                double minY = double.MaxValue;
                double maxX = double.MinValue;
                double maxY = double.MinValue;

                foreach (Vector2 vertex in _vertices)
                {
                    minX = Math.Min(minX, vertex.X);
                    minY = Math.Min(minY, vertex.Y);
                    maxX = Math.Max(maxX, vertex.X);
                    maxY = Math.Max(maxY, vertex.Y);
                }

                return new Bounding(new Vector2(minX, minY), new Vector2(maxX, maxY));
            }
        }

        /// <summary>
        /// 中心�?
        /// </summary>
        public Vector2 Center
        {
            get
            {
                if (_vertices.Count == 0)
                    return Vector2.Zero;

                double sumX = 0, sumY = 0;
                foreach (var vertex in _vertices)
                {
                    sumX += vertex.X;
                    sumY += vertex.Y;
                }
                return new Vector2(sumX / _vertices.Count, sumY / _vertices.Count);
            }
        }

        #endregion

        #region 构造函�?

        /// <summary>
        /// 创建遮罩
        /// </summary>
        public Wipeout() : base()
        {
        }

        /// <summary>
        /// 创建遮罩
        /// </summary>
        /// <param name="vertices">边界顶点</param>
        public Wipeout(List<Vector2> vertices) : base()
        {
            _vertices = vertices ?? new List<Vector2>();
        }

        /// <summary>
        /// 创建矩形遮罩
        /// </summary>
        /// <param name="corner1">第一个角�?/param>
        /// <param name="corner2">对角�?/param>
        public static Wipeout CreateRectangular(Vector2 corner1, Vector2 corner2)
        {
            double minX = Math.Min(corner1.X, corner2.X);
            double maxX = Math.Max(corner1.X, corner2.X);
            double minY = Math.Min(corner1.Y, corner2.Y);
            double maxY = Math.Max(corner1.Y, corner2.Y);

            List<Vector2> vertices = new List<Vector2>
            {
                new Vector2(minX, minY),
                new Vector2(maxX, minY),
                new Vector2(maxX, maxY),
                new Vector2(minX, maxY)
            };

            return new Wipeout(vertices);
        }

        /// <summary>
        /// 创建圆形遮罩
        /// </summary>
        /// <param name="center">中心�?/param>
        /// <param name="radius">半径</param>
        /// <param name="segments">段数</param>
        public static Wipeout CreateCircular(Vector2 center, double radius, int segments = 36)
        {
            List<Vector2> vertices = new List<Vector2>();
            double angleStep = 2 * Math.PI / segments;

            for (int i = 0; i < segments; i++)
            {
                double angle = i * angleStep;
                vertices.Add(center + new Vector2(
                    Math.Cos(angle) * radius,
                    Math.Sin(angle) * radius
                ));
            }

            return new Wipeout(vertices);
        }

        #endregion

        #region 方法

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            if (_vertices.Count < 3)
                return;

            // 绘制遮罩多边形（通常是填充白色）
            // 实际渲染时需要特殊处理以遮盖下层实体
            
            // 如果显示边框，绘制边界线
            if (_showFrame)
            {
                for (int i = 0; i < _vertices.Count; i++)
                {
                    Vector2 start = _vertices[i];
                    Vector2 end = _vertices[(i + 1) % _vertices.Count];
                    gd.DrawLine(start, end);
                }
            }
            
            // 注意：实际的遮罩效果需要在渲染层特殊处�?
            // 这里只是绘制边框用于显示和选择
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new Wipeout();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Wipeout wipeout = base.Clone() as Wipeout;
            wipeout._vertices = new List<Vector2>(_vertices);
            wipeout._showFrame = _showFrame;
            wipeout._clippingBoundary = _clippingBoundary?.Clone() as ClippingBoundary;
            wipeout._contrast = _contrast;
            wipeout._brightness = _brightness;
            wipeout._fade = _fade;
            return wipeout;
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            for (int i = 0; i < _vertices.Count; i++)
            {
                _vertices[i] += translation;
            }
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            for (int i = 0; i < _vertices.Count; i++)
            {
                _vertices[i] = Vector2.RotateInRadian(_vertices[i], center, angle);
            }
        }

        /// <summary>
        /// 变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            for (int i = 0; i < _vertices.Count; i++)
            {
                _vertices[i] = transform * _vertices[i];
            }
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            
            // 每个顶点作为夹点
            foreach (var vertex in _vertices)
            {
                gripPoints.Add(new GripPoint(GripPointType.End, vertex));
            }
            
            // 中心点夹�?
            gripPoints.Add(new GripPoint(GripPointType.Center, Center));
            
            return gripPoints;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            if (index < _vertices.Count)
            {
                // 移动顶点
                _vertices[index] = newPosition;
            }
            else if (index == _vertices.Count)
            {
                // 移动中心点（整体移动�?
                Vector2 currentCenter = Center;
                Vector2 delta = newPosition - currentCenter;
                Translate(delta);
            }
        }

        /// <summary>
        /// 获取捕捉�?
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPoints = new List<ObjectSnapPoint>();
            
            // 顶点捕捉�?
            foreach (var vertex in _vertices)
            {
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, vertex));
            }
            
            // 边中点捕捉点
            for (int i = 0; i < _vertices.Count; i++)
            {
                Vector2 start = _vertices[i];
                Vector2 end = _vertices[(i + 1) % _vertices.Count];
                Vector2 midpoint = (start + end) * 0.5;
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, midpoint));
            }
            
            // 中心点捕捉点
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Center, Center));
            
            return snapPoints;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 添加顶点
        /// </summary>
        public void AddVertex(Vector2 vertex)
        {
            _vertices.Add(vertex);
        }

        /// <summary>
        /// 插入顶点
        /// </summary>
        public void InsertVertex(int index, Vector2 vertex)
        {
            if (index >= 0 && index <= _vertices.Count)
            {
                _vertices.Insert(index, vertex);
            }
        }

        /// <summary>
        /// 移除顶点
        /// </summary>
        public bool RemoveVertex(int index)
        {
            if (index >= 0 && index < _vertices.Count && _vertices.Count > 3)
            {
                _vertices.RemoveAt(index);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 反转顶点顺序
        /// </summary>
        public void ReverseVertices()
        {
            _vertices.Reverse();
        }

        /// <summary>
        /// 点是否在遮罩�?
        /// </summary>
        public bool ContainsPoint(Vector2 point)
        {
            if (_vertices.Count < 3)
                return false;

            // 使用射线法判断点是否在多边形�?
            int intersections = 0;
            for (int i = 0; i < _vertices.Count; i++)
            {
                Vector2 p1 = _vertices[i];
                Vector2 p2 = _vertices[(i + 1) % _vertices.Count];

                if ((p1.Y <= point.Y && point.Y < p2.Y) || (p2.Y <= point.Y && point.Y < p1.Y))
                {
                    double x = p1.X + (point.Y - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y);
                    if (x > point.X)
                    {
                        intersections++;
                    }
                }
            }

            return (intersections % 2) == 1;
        }

        #endregion
    }

    /// <summary>
    /// 裁剪边界
    /// </summary>
    [Serializable]
    public class ClippingBoundary : ICloneable
    {
        private bool _isEnabled = false;
        private List<Vector2> _vertices = new List<Vector2>();
        private bool _isInverted = false;

        /// <summary>
        /// 是否启用裁剪
        /// </summary>
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { _isEnabled = value; }
        }

        /// <summary>
        /// 顶点列表
        /// </summary>
        public List<Vector2> Vertices
        {
            get { return _vertices; }
        }

        /// <summary>
        /// 是否反转裁剪
        /// </summary>
        public bool IsInverted
        {
            get { return _isInverted; }
            set { _isInverted = value; }
        }

        /// <summary>
        /// 克隆
        /// </summary>
        public object Clone()
        {
            ClippingBoundary boundary = new ClippingBoundary();
            boundary._isEnabled = _isEnabled;
            boundary._vertices = new List<Vector2>(_vertices);
            boundary._isInverted = _isInverted;
            return boundary;
        }
    }
}