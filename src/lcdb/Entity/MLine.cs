using System;
using System.Collections.Generic;
using System.Linq;
using LitMath;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 多线实体
    /// 原生OtoCAD实现，用于创建由多条平行线组成的复合线条
    /// </summary>
    [Serializable]
    public class MLine : Entity
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "MLine";

        #region 字段

        // 顶点列表
        private List<MLineVertex> _vertexes = new List<MLineVertex>();
        
        // 多线样式
        private MLineStyle _style;
        
        // 缩放比例
        private double _scale = 1.0;
        
        // 对齐方式
        private MLineJustification _justification = MLineJustification.Zero;
        
        // 高程（Z坐标�?
        private double _elevation = 0.0;
        
        // 标志
        private MLineFlags _flags = MLineFlags.Has;
        
        // 是否闭合
        private bool _isClosed = false;

        #endregion

        #region 属�?

        /// <summary>
        /// 顶点列表
        /// </summary>
        public List<MLineVertex> Vertexes
        {
            get { return _vertexes; }
        }

        /// <summary>
        /// 多线样式
        /// </summary>
        public MLineStyle Style
        {
            get { return _style; }
            set { _style = value ?? MLineStyle.Default; }
        }

        /// <summary>
        /// 缩放比例
        /// </summary>
        public double Scale
        {
            get { return _scale; }
            set { _scale = value; }
        }

        /// <summary>
        /// 对齐方式
        /// </summary>
        public MLineJustification Justification
        {
            get { return _justification; }
            set { _justification = value; }
        }

        /// <summary>
        /// 高程
        /// </summary>
        public double Elevation
        {
            get { return _elevation; }
            set { _elevation = value; }
        }

        /// <summary>
        /// 是否闭合
        /// </summary>
        public bool IsClosed
        {
            get { return _isClosed; }
            set 
            { 
                _isClosed = value;
                if (_isClosed)
                    _flags |= MLineFlags.Closed;
                else
                    _flags &= ~MLineFlags.Closed;
            }
        }


        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                if (_vertexes.Count == 0)
                    return new Bounding();

                double minX = double.MaxValue;
                double minY = double.MaxValue;
                double maxX = double.MinValue;
                double maxY = double.MinValue;

                double offset = CalculateMaxOffset();

                foreach (var vertex in _vertexes)
                {
                    minX = Math.Min(minX, vertex.Position.X - offset);
                    minY = Math.Min(minY, vertex.Position.Y - offset);
                    maxX = Math.Max(maxX, vertex.Position.X + offset);
                    maxY = Math.Max(maxY, vertex.Position.Y + offset);
                }

                return new Bounding(new Vector2(minX, minY), new Vector2(maxX, maxY));
            }
        }

        #endregion

        #region 构造函�?

        /// <summary>
        /// 创建多线
        /// </summary>
        public MLine() : base()
        {
            _style = MLineStyle.Default;
        }

        /// <summary>
        /// 创建多线
        /// </summary>
        /// <param name="vertexes">顶点列表</param>
        public MLine(IEnumerable<Vector2> vertexes) : base()
        {
            foreach (var vertex in vertexes)
            {
                _vertexes.Add(new MLineVertex(vertex));
            }
            _style = MLineStyle.Default;
        }

        /// <summary>
        /// 创建多线
        /// </summary>
        /// <param name="vertexes">顶点列表</param>
        /// <param name="isClosed">是否闭合</param>
        public MLine(IEnumerable<Vector2> vertexes, bool isClosed) : base()
        {
            foreach (var vertex in vertexes)
            {
                _vertexes.Add(new MLineVertex(vertex));
            }
            _style = MLineStyle.Default;
            IsClosed = isClosed;
        }

        /// <summary>
        /// 创建多线
        /// </summary>
        /// <param name="vertexes">顶点列表</param>
        /// <param name="style">多线样式</param>
        /// <param name="scale">缩放比例</param>
        public MLine(IEnumerable<Vector2> vertexes, MLineStyle style, double scale) : base()
        {
            foreach (var vertex in vertexes)
            {
                _vertexes.Add(new MLineVertex(vertex));
            }
            _style = style ?? MLineStyle.Default;
            _scale = scale;
        }

        #endregion

        #region 方法

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            if (_vertexes.Count < 2 || _style == null)
                return;

            // 计算每段的方向和法线
            List<Vector2> directions = new List<Vector2>();
            List<Vector2> normals = new List<Vector2>();
            
            for (int i = 0; i < _vertexes.Count - 1; i++)
            {
                Vector2 dir = (_vertexes[i + 1].Position - _vertexes[i].Position).normalized;
                directions.Add(dir);
                normals.Add(new Vector2(-dir.Y, dir.X));
            }
            
            // 如果闭合，添加最后一�?
            if (_isClosed && _vertexes.Count > 2)
            {
                Vector2 dir = (_vertexes[0].Position - _vertexes[_vertexes.Count - 1].Position).normalized;
                directions.Add(dir);
                normals.Add(new Vector2(-dir.Y, dir.X));
            }

            // 绘制每个元素（线条）
            foreach (var element in _style.Elements)
            {
                if (!element.Visible)
                    continue;

                double offset = element.Offset * _scale;
                
                // 根据对齐方式调整偏移
                switch (_justification)
                {
                    case MLineJustification.Top:
                        offset -= _style.GetMaxPositiveOffset() * _scale;
                        break;
                    case MLineJustification.Bottom:
                        offset -= _style.GetMaxNegativeOffset() * _scale;
                        break;
                    case MLineJustification.Zero:
                        // 保持原始偏移
                        break;
                }

                // 绘制每段
                for (int i = 0; i < directions.Count; i++)
                {
                    Vector2 start = _vertexes[i].Position + normals[i] * offset;
                    Vector2 end = _vertexes[i + 1 < _vertexes.Count ? i + 1 : 0].Position + normals[i] * offset;
                    
                    // 处理顶点的斜�?
                    if (i > 0)
                    {
                        start = GetMiterPoint(_vertexes[i].Position, normals[i - 1], normals[i], offset);
                    }
                    if (i < directions.Count - 1)
                    {
                        end = GetMiterPoint(_vertexes[i + 1].Position, normals[i], normals[i + 1], offset);
                    }
                    
                    // 如果闭合，处理首尾连�?
                    if (_isClosed)
                    {
                        if (i == 0)
                        {
                            start = GetMiterPoint(_vertexes[0].Position, normals[normals.Count - 1], normals[0], offset);
                        }
                        if (i == directions.Count - 1)
                        {
                            end = GetMiterPoint(_vertexes[0].Position, normals[normals.Count - 1], normals[0], offset);
                        }
                    }
                    
                    gd.DrawLine(start, end);
                }
            }

            // 绘制起始和结束端�?
            if (!_isClosed && _style.ShowStartEndCaps)
            {
                DrawStartCap(gd, normals[0]);
                DrawEndCap(gd, normals[normals.Count - 1]);
            }
        }

        /// <summary>
        /// 计算斜接�?
        /// </summary>
        private Vector2 GetMiterPoint(Vector2 vertex, Vector2 normal1, Vector2 normal2, double offset)
        {
            // 计算两个法线的平均方�?
            Vector2 avgNormal = (normal1 + normal2).normalized;
            
            // 计算斜接因子
            double dot = Vector2.Dot(normal1, normal2);
            double miterFactor = 1.0 / Math.Max(0.1, (1.0 + dot) / 2.0);
            
            return vertex + avgNormal * offset * miterFactor;
        }

        /// <summary>
        /// 绘制起始端盖
        /// </summary>
        private void DrawStartCap(IGraphicsDraw gd, Vector2 normal)
        {
            if (_vertexes.Count == 0 || _style == null)
                return;

            Vector2 start = _vertexes[0].Position;
            
            // 获取所有可见元素的偏移
            var offsets = _style.Elements
                .Where(e => e.Visible)
                .Select(e => e.Offset * _scale)
                .OrderBy(o => o)
                .ToList();
                
            if (offsets.Count < 2)
                return;
                
            // 根据对齐方式调整偏移
            double adjustment = 0;
            switch (_justification)
            {
                case MLineJustification.Top:
                    adjustment = -_style.GetMaxPositiveOffset() * _scale;
                    break;
                case MLineJustification.Bottom:
                    adjustment = -_style.GetMaxNegativeOffset() * _scale;
                    break;
            }
            
            // 绘制端盖�?
            Vector2 p1 = start + normal * (offsets.First() + adjustment);
            Vector2 p2 = start + normal * (offsets.Last() + adjustment);
            gd.DrawLine(p1, p2);
        }

        /// <summary>
        /// 绘制结束端盖
        /// </summary>
        private void DrawEndCap(IGraphicsDraw gd, Vector2 normal)
        {
            if (_vertexes.Count == 0 || _style == null)
                return;

            Vector2 end = _vertexes[_vertexes.Count - 1].Position;
            
            // 获取所有可见元素的偏移
            var offsets = _style.Elements
                .Where(e => e.Visible)
                .Select(e => e.Offset * _scale)
                .OrderBy(o => o)
                .ToList();
                
            if (offsets.Count < 2)
                return;
                
            // 根据对齐方式调整偏移
            double adjustment = 0;
            switch (_justification)
            {
                case MLineJustification.Top:
                    adjustment = -_style.GetMaxPositiveOffset() * _scale;
                    break;
                case MLineJustification.Bottom:
                    adjustment = -_style.GetMaxNegativeOffset() * _scale;
                    break;
            }
            
            // 绘制端盖�?
            Vector2 p1 = end + normal * (offsets.First() + adjustment);
            Vector2 p2 = end + normal * (offsets.Last() + adjustment);
            gd.DrawLine(p1, p2);
        }

        /// <summary>
        /// 计算最大偏�?
        /// </summary>
        private double CalculateMaxOffset()
        {
            if (_style == null)
                return 0;
                
            double maxOffset = 0;
            foreach (var element in _style.Elements)
            {
                maxOffset = Math.Max(maxOffset, Math.Abs(element.Offset * _scale));
            }
            return maxOffset;
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new MLine();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            MLine mline = base.Clone() as MLine;
            mline._vertexes = new List<MLineVertex>();
            foreach (var vertex in _vertexes)
            {
                mline._vertexes.Add(vertex.Clone() as MLineVertex);
            }
            mline._style = _style;
            mline._scale = _scale;
            mline._justification = _justification;
            mline._elevation = _elevation;
            mline._flags = _flags;
            mline._isClosed = _isClosed;
            return mline;
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            foreach (var vertex in _vertexes)
            {
                vertex.Position += translation;
            }
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            foreach (var vertex in _vertexes)
            {
                vertex.Position = Vector2.RotateInRadian(vertex.Position, center, angle);
                vertex.Direction = Vector2.RotateInRadian(vertex.Direction, Vector2.Zero, angle);
            }
        }

        /// <summary>
        /// 变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            foreach (var vertex in _vertexes)
            {
                vertex.Position = transform * vertex.Position;
                vertex.Direction = (transform * vertex.Direction).normalized;
            }
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            
            foreach (var vertex in _vertexes)
            {
                gripPoints.Add(new GripPoint(GripPointType.Center, vertex.Position));
            }
            
            return gripPoints;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            if (index >= 0 && index < _vertexes.Count)
            {
                _vertexes[index].Position = newPosition;
                // 可能需要重新计算方�?
                UpdateVertexDirections();
            }
        }

        /// <summary>
        /// 获取捕捉�?
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPoints = new List<ObjectSnapPoint>();
            
            // 顶点捕捉�?
            foreach (var vertex in _vertexes)
            {
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, vertex.Position));
            }
            
            // 中点捕捉�?
            for (int i = 0; i < _vertexes.Count - 1; i++)
            {
                Vector2 midPoint = (_vertexes[i].Position + _vertexes[i + 1].Position) * 0.5;
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, midPoint));
            }
            
            if (_isClosed && _vertexes.Count > 2)
            {
                Vector2 midPoint = (_vertexes[_vertexes.Count - 1].Position + _vertexes[0].Position) * 0.5;
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, midPoint));
            }
            
            return snapPoints;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 添加顶点
        /// </summary>
        public void AddVertex(Vector2 position)
        {
            _vertexes.Add(new MLineVertex(position));
            UpdateVertexDirections();
        }

        /// <summary>
        /// 插入顶点
        /// </summary>
        public void InsertVertex(int index, Vector2 position)
        {
            if (index >= 0 && index <= _vertexes.Count)
            {
                _vertexes.Insert(index, new MLineVertex(position));
                UpdateVertexDirections();
            }
        }

        /// <summary>
        /// 删除顶点
        /// </summary>
        public bool RemoveVertex(int index)
        {
            if (index >= 0 && index < _vertexes.Count && _vertexes.Count > 2)
            {
                _vertexes.RemoveAt(index);
                UpdateVertexDirections();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 更新顶点方向
        /// </summary>
        private void UpdateVertexDirections()
        {
            for (int i = 0; i < _vertexes.Count; i++)
            {
                Vector2 dir;
                
                if (i == 0)
                {
                    if (_isClosed && _vertexes.Count > 2)
                    {
                        dir = (_vertexes[1].Position - _vertexes[_vertexes.Count - 1].Position).normalized;
                    }
                    else if (_vertexes.Count > 1)
                    {
                        dir = (_vertexes[1].Position - _vertexes[0].Position).normalized;
                    }
                    else
                    {
                        dir = Vector2.UnitX;
                    }
                }
                else if (i == _vertexes.Count - 1)
                {
                    if (_isClosed && _vertexes.Count > 2)
                    {
                        dir = (_vertexes[0].Position - _vertexes[i - 1].Position).normalized;
                    }
                    else
                    {
                        dir = (_vertexes[i].Position - _vertexes[i - 1].Position).normalized;
                    }
                }
                else
                {
                    dir = (_vertexes[i + 1].Position - _vertexes[i - 1].Position).normalized;
                }
                
                _vertexes[i].Direction = dir;
            }
        }

        #endregion
    }

    /// <summary>
    /// 多线顶点
    /// </summary>
    [Serializable]
    public class MLineVertex : ICloneable
    {
        /// <summary>
        /// 位置
        /// </summary>
        public Vector2 Position { get; set; }
        
        /// <summary>
        /// 方向
        /// </summary>
        public Vector2 Direction { get; set; }
        
        /// <summary>
        /// 斜接方向
        /// </summary>
        public Vector2 Miter { get; set; }

        /// <summary>
        /// 构造函�?
        /// </summary>
        public MLineVertex(Vector2 position)
        {
            Position = position;
            Direction = Vector2.UnitX;
            Miter = Vector2.UnitY;
        }

        /// <summary>
        /// 克隆
        /// </summary>
        public object Clone()
        {
            return new MLineVertex(Position)
            {
                Direction = Direction,
                Miter = Miter
            };
        }
    }

    /// <summary>
    /// 多线对齐方式
    /// </summary>
    public enum MLineJustification
    {
        /// <summary>
        /// 顶部对齐
        /// </summary>
        Top = 0,
        
        /// <summary>
        /// 零点对齐（中心）
        /// </summary>
        Zero = 1,
        
        /// <summary>
        /// 底部对齐
        /// </summary>
        Bottom = 2
    }

    /// <summary>
    /// 多线标志
    /// </summary>
    [Flags]
    public enum MLineFlags
    {
        /// <summary>
        /// 具有顶点
        /// </summary>
        Has = 1,
        
        /// <summary>
        /// 闭合
        /// </summary>
        Closed = 2,
        
        /// <summary>
        /// 抑制起始端盖
        /// </summary>
        NoStartCaps = 4,
        
        /// <summary>
        /// 抑制结束端盖
        /// </summary>
        NoEndCaps = 8
    }

    /// <summary>
    /// 多线样式
    /// </summary>
    [Serializable]
    public class MLineStyle
    {
        private string _name = "Standard";
        private string _description = "";
        private List<MLineStyleElement> _elements = new List<MLineStyleElement>();
        private bool _showStartEndCaps = true;
        private double _startAngle = 90.0;
        private double _endAngle = 90.0;

        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value ?? "Standard"; }
        }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { _description = value ?? ""; }
        }

        /// <summary>
        /// 元素列表
        /// </summary>
        public List<MLineStyleElement> Elements
        {
            get { return _elements; }
        }

        /// <summary>
        /// 是否显示起始和结束端�?
        /// </summary>
        public bool ShowStartEndCaps
        {
            get { return _showStartEndCaps; }
            set { _showStartEndCaps = value; }
        }

        /// <summary>
        /// 起始角度
        /// </summary>
        public double StartAngle
        {
            get { return _startAngle; }
            set { _startAngle = value; }
        }

        /// <summary>
        /// 结束角度
        /// </summary>
        public double EndAngle
        {
            get { return _endAngle; }
            set { _endAngle = value; }
        }

        /// <summary>
        /// 默认样式
        /// </summary>
        public static MLineStyle Default
        {
            get
            {
                MLineStyle style = new MLineStyle();
                style._name = "Standard";
                style._elements.Add(new MLineStyleElement { Offset = 0.5 });
                style._elements.Add(new MLineStyleElement { Offset = -0.5 });
                return style;
            }
        }

        /// <summary>
        /// 获取最大正偏移
        /// </summary>
        public double GetMaxPositiveOffset()
        {
            return _elements.Where(e => e.Offset > 0).Select(e => e.Offset).DefaultIfEmpty(0).Max();
        }

        /// <summary>
        /// 获取最大负偏移
        /// </summary>
        public double GetMaxNegativeOffset()
        {
            return _elements.Where(e => e.Offset < 0).Select(e => e.Offset).DefaultIfEmpty(0).Min();
        }
    }

    /// <summary>
    /// 多线样式元素
    /// </summary>
    [Serializable]
    public class MLineStyleElement
    {
        /// <summary>
        /// 偏移距离
        /// </summary>
        public double Offset { get; set; } = 0.0;
        
        /// <summary>
        /// 颜色
        /// </summary>
        public lcdb.Colors.Color Color { get; set; } = lcdb.Colors.Color.ByLayer;
        
        /// <summary>
        /// 线型
        /// </summary>
        public string Linetype { get; set; } = "ByLayer";
        
        /// <summary>
        /// 是否可见
        /// </summary>
        public bool Visible { get; set; } = true;
    }
}