using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using lcdb;
using LitMath;

namespace lcdb.Dimension
{
    /// <summary>
    /// 引线类，用于标注中的引线绘制和管理
    /// </summary>
    public class LeaderLine : ICloneable
    {
        #region 字段

        private List<Vector2> _points;
        private ArrowheadType _arrowheadType;
        private double _arrowSize;
        private Color _color;
        private LineWeight _lineWeight;
        private bool _showArrowhead;
        private LeaderType _leaderType;
        private double _landingGap;
        private bool _enableLanding;
        private double _landingLength;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化引线的新实例
        /// </summary>
        public LeaderLine()
        {
            _points = new List<Vector2>();
            _arrowheadType = ArrowheadType.ClosedFilled;
            _arrowSize = 2.5;
            _color = Color.Black;
            _lineWeight = LineWeight.ByLayer;
            _showArrowhead = true;
            _leaderType = LeaderType.Straight;
            _landingGap = 1.0;
            _enableLanding = true;
            _landingLength = 5.0;
        }

        /// <summary>
        /// 使用指定点初始化引线的新实例
        /// </summary>
        public LeaderLine(IEnumerable<Vector2> points) : this()
        {
            _points.AddRange(points);
        }

        #endregion

        #region 属性

        /// <summary>
        /// 获取引线点集合
        /// </summary>
        public List<Vector2> Points
        {
            get { return _points; }
        }

        /// <summary>
        /// 获取或设置箭头类型
        /// </summary>
        public ArrowheadType ArrowheadType
        {
            get { return _arrowheadType; }
            set { _arrowheadType = value; }
        }

        /// <summary>
        /// 获取或设置箭头大小
        /// </summary>
        public double ArrowSize
        {
            get { return _arrowSize; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("箭头大小必须大于0");
                _arrowSize = value;
            }
        }

        /// <summary>
        /// 获取或设置颜色
        /// </summary>
        public Color Color
        {
            get { return _color; }
            set { _color = value; }
        }

        /// <summary>
        /// 获取或设置线宽
        /// </summary>
        public LineWeight LineWeight
        {
            get { return _lineWeight; }
            set { _lineWeight = value; }
        }

        /// <summary>
        /// 获取或设置是否显示箭头
        /// </summary>
        public bool ShowArrowhead
        {
            get { return _showArrowhead; }
            set { _showArrowhead = value; }
        }

        /// <summary>
        /// 获取或设置引线类型
        /// </summary>
        public LeaderType LeaderType
        {
            get { return _leaderType; }
            set { _leaderType = value; }
        }

        /// <summary>
        /// 获取或设置着陆间隙
        /// </summary>
        public double LandingGap
        {
            get { return _landingGap; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("着陆间隙不能为负数");
                _landingGap = value;
            }
        }

        /// <summary>
        /// 获取或设置是否启用着陆线
        /// </summary>
        public bool EnableLanding
        {
            get { return _enableLanding; }
            set { _enableLanding = value; }
        }

        /// <summary>
        /// 获取或设置着陆线长度
        /// </summary>
        public double LandingLength
        {
            get { return _landingLength; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("着陆线长度必须大于0");
                _landingLength = value;
            }
        }

        /// <summary>
        /// 获取起点
        /// </summary>
        public Vector2 StartPoint
        {
            get { return _points.Count > 0 ? _points[0] : Vector2.Zero; }
        }

        /// <summary>
        /// 获取终点
        /// </summary>
        public Vector2 EndPoint
        {
            get { return _points.Count > 0 ? _points[_points.Count - 1] : Vector2.Zero; }
        }

        #endregion

        #region 方法

        /// <summary>
        /// 添加点
        /// </summary>
        public void AddPoint(Vector2 point)
        {
            _points.Add(point);
        }

        /// <summary>
        /// 插入点
        /// </summary>
        public void InsertPoint(int index, Vector2 point)
        {
            if (index < 0 || index > _points.Count)
                throw new ArgumentOutOfRangeException("index");
                
            _points.Insert(index, point);
        }

        /// <summary>
        /// 移除点
        /// </summary>
        public void RemovePoint(int index)
        {
            if (index < 0 || index >= _points.Count)
                throw new ArgumentOutOfRangeException("index");
                
            _points.RemoveAt(index);
        }

        /// <summary>
        /// 清空所有点
        /// </summary>
        public void Clear()
        {
            _points.Clear();
        }

        /// <summary>
        /// 计算引线路径
        /// </summary>
        public List<Vector2> CalculatePath()
        {
            if (_points.Count < 2)
                return new List<Vector2>(_points);
                
            List<Vector2> path = new List<Vector2>();
            
            switch (_leaderType)
            {
                case LeaderType.Straight:
                    path.AddRange(_points);
                    break;
                    
                case LeaderType.Spline:
                    path = CalculateSplinePath();
                    break;
                    
                case LeaderType.RightAngle:
                    path = CalculateRightAnglePath();
                    break;
            }
            
            // 添加着陆线
            if (_enableLanding && path.Count >= 2)
            {
                Vector2 lastPoint = path[path.Count - 1];
                Vector2 secondLastPoint = path[path.Count - 2];
                Vector2 direction = (lastPoint - secondLastPoint).normalized;
                
                // 计算垂直方向
                Vector2 perpendicular = new Vector2(-direction.Y, direction.X);
                
                // 添加着陆点
                Vector2 landingPoint = lastPoint + perpendicular * _landingLength;
                path.Add(landingPoint);
            }
            
            return path;
        }

        /// <summary>
        /// 计算样条曲线路径
        /// </summary>
        private List<Vector2> CalculateSplinePath()
        {
            // 简化实现：使用贝塞尔曲线
            List<Vector2> path = new List<Vector2>();
            
            if (_points.Count == 2)
            {
                // 两点直接连接
                path.AddRange(_points);
            }
            else if (_points.Count == 3)
            {
                // 三点二次贝塞尔曲线
                for (double t = 0; t <= 1.0; t += 0.1)
                {
                    Vector2 p = QuadraticBezier(_points[0], _points[1], _points[2], t);
                    path.Add(p);
                }
            }
            else
            {
                // 多点使用分段贝塞尔曲线
                for (int i = 0; i < _points.Count - 1; i++)
                {
                    Vector2 p0 = _points[i];
                    Vector2 p1 = _points[i + 1];
                    
                    // 计算控制点
                    Vector2 c0 = p0;
                    Vector2 c1 = p1;
                    
                    if (i > 0)
                    {
                        Vector2 prev = _points[i - 1];
                        c0 = p0 + (p0 - prev) * 0.3;
                    }
                    
                    if (i < _points.Count - 2)
                    {
                        Vector2 next = _points[i + 2];
                        c1 = p1 - (next - p1) * 0.3;
                    }
                    
                    // 生成曲线段
                    for (double t = 0; t < 1.0; t += 0.1)
                    {
                        Vector2 p = CubicBezier(p0, c0, c1, p1, t);
                        path.Add(p);
                    }
                }
                
                path.Add(_points[_points.Count - 1]);
            }
            
            return path;
        }

        /// <summary>
        /// 计算直角路径
        /// </summary>
        private List<Vector2> CalculateRightAnglePath()
        {
            List<Vector2> path = new List<Vector2>();
            
            if (_points.Count < 2)
                return new List<Vector2>(_points);
                
            path.Add(_points[0]);
            
            for (int i = 1; i < _points.Count; i++)
            {
                Vector2 prev = path[path.Count - 1];
                Vector2 curr = _points[i];
                
                // 添加中间直角点
                if (Math.Abs(prev.X - curr.X) > 0.001 && Math.Abs(prev.Y - curr.Y) > 0.001)
                {
                    // 优先水平移动
                    Vector2 corner = new Vector2(curr.X, prev.Y);
                    path.Add(corner);
                }
                
                path.Add(curr);
            }
            
            return path;
        }

        /// <summary>
        /// 二次贝塞尔曲线
        /// </summary>
        private Vector2 QuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, double t)
        {
            double u = 1 - t;
            return u * u * p0 + 2 * u * t * p1 + t * t * p2;
        }

        /// <summary>
        /// 三次贝塞尔曲线
        /// </summary>
        private Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, double t)
        {
            double u = 1 - t;
            return u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
        }

        /// <summary>
        /// 获取箭头顶点
        /// </summary>
        public List<Vector2> GetArrowheadVertices()
        {
            List<Vector2> vertices = new List<Vector2>();
            
            if (!_showArrowhead || _points.Count < 2)
                return vertices;
                
            Vector2 tip = _points[0];
            Vector2 next = _points[1];
            Vector2 direction = (next - tip).normalized;
            
            switch (_arrowheadType)
            {
                case ArrowheadType.ClosedFilled:
                case ArrowheadType.ClosedBlank:
                    {
                        // 计算箭头两翼
                        double angle = Math.PI / 6; // 30度
                        Vector2 wing1 = RotateVector(direction * _arrowSize, angle);
                        Vector2 wing2 = RotateVector(direction * _arrowSize, -angle);
                        
                        vertices.Add(tip);
                        vertices.Add(tip + wing1);
                        vertices.Add(tip + wing2);
                    }
                    break;
                    
                case ArrowheadType.Open:
                case ArrowheadType.Open30:
                case ArrowheadType.Open90:
                    {
                        double angle = _arrowheadType == ArrowheadType.Open30 ? Math.PI / 6 :
                                      _arrowheadType == ArrowheadType.Open90 ? Math.PI / 2 :
                                      Math.PI / 4;
                                      
                        Vector2 wing1 = RotateVector(direction * _arrowSize, angle);
                        Vector2 wing2 = RotateVector(direction * _arrowSize, -angle);
                        
                        vertices.Add(tip + wing1);
                        vertices.Add(tip);
                        vertices.Add(tip + wing2);
                    }
                    break;
                    
                case ArrowheadType.Dot:
                    // 点型箭头用圆表示，这里返回中心点
                    vertices.Add(tip);
                    break;
                    
                case ArrowheadType.Oblique:
                    {
                        // 斜线型箭头
                        Vector2 perpendicular = new Vector2(-direction.Y, direction.X) * _arrowSize * 0.5;
                        vertices.Add(tip + perpendicular);
                        vertices.Add(tip - perpendicular);
                    }
                    break;
            }
            
            return vertices;
        }

        /// <summary>
        /// 旋转向量
        /// </summary>
        private Vector2 RotateVector(Vector2 v, double angle)
        {
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
        }

        /// <summary>
        /// 计算边界框
        /// </summary>
        public Bounding GetBounding()
        {
            if (_points.Count == 0)
                return new Bounding();
                
            double minX = _points.Min(p => p.X);
            double minY = _points.Min(p => p.Y);
            double maxX = _points.Max(p => p.X);
            double maxY = _points.Max(p => p.Y);
            
            // 考虑箭头大小
            if (_showArrowhead)
            {
                minX -= _arrowSize;
                minY -= _arrowSize;
                maxX += _arrowSize;
                maxY += _arrowSize;
            }
            
            // 考虑着陆线
            if (_enableLanding)
            {
                minX -= _landingLength;
                minY -= _landingLength;
                maxX += _landingLength;
                maxY += _landingLength;
            }
            
            return new Bounding(new Vector2(minX, minY), new Vector2(maxX, maxY));
        }

        /// <summary>
        /// 克隆当前引线
        /// </summary>
        public object Clone()
        {
            LeaderLine clone = new LeaderLine
            {
                _arrowheadType = _arrowheadType,
                _arrowSize = _arrowSize,
                _color = _color,
                _lineWeight = _lineWeight,
                _showArrowhead = _showArrowhead,
                _leaderType = _leaderType,
                _landingGap = _landingGap,
                _enableLanding = _enableLanding,
                _landingLength = _landingLength
            };
            
            clone._points.AddRange(_points);
            
            return clone;
        }

        /// <summary>
        /// 创建用于光学标注的标准引线
        /// </summary>
        public static LeaderLine CreateOpticalLeader(Vector2 startPoint, Vector2 endPoint, string annotation)
        {
            LeaderLine leader = new LeaderLine();
            leader._points.Add(startPoint);
            
            // 计算中间点，使引线更美观
            Vector2 midPoint = startPoint + (endPoint - startPoint) * 0.7;
            leader._points.Add(midPoint);
            leader._points.Add(endPoint);
            
            // 设置光学标注标准样式
            leader._arrowheadType = ArrowheadType.ClosedFilled;
            leader._arrowSize = 2.0;
            leader._enableLanding = true;
            leader._landingLength = 10.0; // 适合放置文本
            leader._leaderType = LeaderType.Straight;
            
            return leader;
        }

        #endregion
    }

    /// <summary>
    /// 引线类型
    /// </summary>
    public enum LeaderType
    {
        /// <summary>
        /// 直线
        /// </summary>
        Straight,
        
        /// <summary>
        /// 样条曲线
        /// </summary>
        Spline,
        
        /// <summary>
        /// 直角
        /// </summary>
        RightAngle
    }
}