using System;
using System.Collections.Generic;
using LitMath;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 引线实体
    /// 原生OtoCAD实现，用于创建带箭头的引线，可附加文本、公差或块注�?
    /// </summary>
    [Serializable]
    public class Leader : Entity
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "Leader";

        #region 字段

        // 引线顶点列表
        private List<Vector2> _vertexes = new List<Vector2>();
        
        // 引线样式
        private DimensionStyle _style;
        
        // 是否显示箭头
        private bool _showArrowhead = true;
        
        // 路径类型
        private LeaderPathType _pathType = LeaderPathType.StraightLineSegments;
        
        // 注释类型
        private LeaderAnnotationType _annotationType = LeaderAnnotationType.None;
        
        // 文本注释
        private string _textAnnotation = string.Empty;
        
        // 文本高度
        private double _textHeight = 2.5;
        
        // 文本宽度（用于多行文本）
        private double _textWidth = 0;
        
        // 是否有勾�?
        private bool _hasHookline = true;
        
        // 勾线方向
        private Vector2 _hooklineDirection = Vector2.UnitX;
        
        // 高程（Z坐标�?
        private double _elevation = 0.0;
        
        // 注释偏移
        private Vector2 _annotationOffset = Vector2.Zero;

        #endregion

        #region 属�?

        /// <summary>
        /// 引线顶点列表
        /// </summary>
        public List<Vector2> Vertexes
        {
            get { return _vertexes; }
            set 
            { 
                if (value == null || value.Count < 2)
                    throw new ArgumentException("引线至少需要两个顶点");
                _vertexes = value; 
            }
        }

        /// <summary>
        /// 引线样式
        /// </summary>
        public DimensionStyle Style
        {
            get { return _style; }
            set { _style = value ?? DimensionStyle.Default; }
        }

        /// <summary>
        /// 是否显示箭头
        /// </summary>
        public bool ShowArrowhead
        {
            get { return _showArrowhead; }
            set { _showArrowhead = value; }
        }

        /// <summary>
        /// 路径类型
        /// </summary>
        public LeaderPathType PathType
        {
            get { return _pathType; }
            set { _pathType = value; }
        }

        /// <summary>
        /// 注释类型
        /// </summary>
        public LeaderAnnotationType AnnotationType
        {
            get { return _annotationType; }
            set { _annotationType = value; }
        }

        /// <summary>
        /// 文本注释
        /// </summary>
        public string TextAnnotation
        {
            get { return _textAnnotation; }
            set 
            { 
                _textAnnotation = value ?? string.Empty;
                if (!string.IsNullOrEmpty(_textAnnotation))
                    _annotationType = LeaderAnnotationType.Text;
            }
        }

        /// <summary>
        /// 文本高度
        /// </summary>
        public double TextHeight
        {
            get { return _textHeight; }
            set { _textHeight = Math.Max(0.1, value); }
        }

        /// <summary>
        /// 文本宽度�?表示单行文本�?
        /// </summary>
        public double TextWidth
        {
            get { return _textWidth; }
            set { _textWidth = Math.Max(0, value); }
        }

        /// <summary>
        /// 是否有勾�?
        /// </summary>
        public bool HasHookline
        {
            get { return _hasHookline; }
            set { _hasHookline = value; }
        }

        /// <summary>
        /// 勾线方向
        /// </summary>
        public Vector2 HooklineDirection
        {
            get { return _hooklineDirection; }
            set { _hooklineDirection = value.normalized; }
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
        /// 注释偏移
        /// </summary>
        public Vector2 AnnotationOffset
        {
            get { return _annotationOffset; }
            set { _annotationOffset = value; }
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

                foreach (Vector2 vertex in _vertexes)
                {
                    minX = Math.Min(minX, vertex.X);
                    minY = Math.Min(minY, vertex.Y);
                    maxX = Math.Max(maxX, vertex.X);
                    maxY = Math.Max(maxY, vertex.Y);
                }

                // 考虑文本注释的边�?
                if (_annotationType == LeaderAnnotationType.Text && !string.IsNullOrEmpty(_textAnnotation))
                {
                    Vector2 textPos = GetTextPosition();
                    double textEstWidth = _textWidth > 0 ? _textWidth : _textHeight * _textAnnotation.Length * 0.6;
                    
                    minX = Math.Min(minX, textPos.X);
                    minY = Math.Min(minY, textPos.Y);
                    maxX = Math.Max(maxX, textPos.X + textEstWidth);
                    maxY = Math.Max(maxY, textPos.Y + _textHeight);
                }

                return new Bounding(new Vector2(minX, minY), new Vector2(maxX, maxY));
            }
        }

        #endregion

        #region 构造函�?

        /// <summary>
        /// 创建引线
        /// </summary>
        public Leader() : base()
        {
            _vertexes = new List<Vector2>();
            _style = DimensionStyle.Default;
        }

        /// <summary>
        /// 创建引线
        /// </summary>
        /// <param name="vertexes">顶点列表</param>
        public Leader(IEnumerable<Vector2> vertexes) : base()
        {
            _vertexes = new List<Vector2>(vertexes);
            if (_vertexes.Count < 2)
                throw new ArgumentException("引线至少需要两个顶点");
            _style = DimensionStyle.Default;
            CalculateHooklineDirection();
        }

        /// <summary>
        /// 创建带文本注释的引线
        /// </summary>
        /// <param name="text">文本注释</param>
        /// <param name="vertexes">顶点列表</param>
        public Leader(string text, IEnumerable<Vector2> vertexes) : base()
        {
            _vertexes = new List<Vector2>(vertexes);
            if (_vertexes.Count < 2)
                throw new ArgumentException("引线至少需要两个顶点");
            _style = DimensionStyle.Default;
            _textAnnotation = text ?? string.Empty;
            _annotationType = LeaderAnnotationType.Text;
            CalculateHooklineDirection();
        }

        #endregion

        #region 方法

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            if (_vertexes.Count < 2)
                return;

            // 绘制引线路径
            if (_pathType == LeaderPathType.StraightLineSegments)
            {
                for (int i = 0; i < _vertexes.Count - 1; i++)
                {
                    gd.DrawLine(_vertexes[i], _vertexes[i + 1]);
                }
            }
            else if (_pathType == LeaderPathType.Spline)
            {
                // 简化处理：使用折线代替样条曲线
                for (int i = 0; i < _vertexes.Count - 1; i++)
                {
                    gd.DrawLine(_vertexes[i], _vertexes[i + 1]);
                }
            }

            // 绘制箭头
            if (_showArrowhead && _vertexes.Count >= 2)
            {
                Vector2 dir = (_vertexes[1] - _vertexes[0]).normalized;
                DrawArrowhead(gd, _vertexes[0], dir);
            }

            // 绘制勾线
            if (_hasHookline && _vertexes.Count >= 2)
            {
                Vector2 lastPoint = _vertexes[_vertexes.Count - 1];
                double hooklineLength = _style?.TextHeight ?? _textHeight;
                Vector2 hookEnd = lastPoint + _hooklineDirection * hooklineLength;
                gd.DrawLine(lastPoint, hookEnd);
            }

            // 绘制注释
            DrawAnnotation(gd);
        }

        /// <summary>
        /// 绘制箭头
        /// </summary>
        private void DrawArrowhead(IGraphicsDraw gd, Vector2 tip, Vector2 direction)
        {
            if (_style == null)
                return;

            double size = _style.ArrowSize;
            Vector2 dir = direction.normalized;
            Vector2 perpDir = new Vector2(-dir.Y, dir.X);
            
            Vector2 p1 = tip;
            Vector2 p2 = tip - dir * size + perpDir * (size * 0.167);
            Vector2 p3 = tip - dir * size - perpDir * (size * 0.167);
            
            // 使用GDI绘制填充三角�?
            gd.DrawTriangle(p1, p2, p3);
        }

        /// <summary>
        /// 绘制注释
        /// </summary>
        private void DrawAnnotation(IGraphicsDraw gd)
        {
            if (_annotationType == LeaderAnnotationType.Text && !string.IsNullOrEmpty(_textAnnotation))
            {
                Vector2 textPos = GetTextPosition();
                
                if (_textWidth > 0)
                {
                    // 多行文本
                    gd.DrawText(textPos, _textAnnotation, _textHeight, "", (lcdb.TextAlignment)TextAlignment.LeftBottom, 0);
                }
                else
                {
                    // 单行文本
                    gd.DrawText(textPos, _textAnnotation, _textHeight, "", (lcdb.TextAlignment)TextAlignment.LeftBottom, 0);
                }
            }
            // 其他注释类型（公差、块等）暂不实现
        }

        /// <summary>
        /// 获取文本位置
        /// </summary>
        private Vector2 GetTextPosition()
        {
            if (_vertexes.Count < 2)
                return Vector2.Zero;

            Vector2 lastPoint = _vertexes[_vertexes.Count - 1];
            
            if (_hasHookline)
            {
                double hooklineLength = _style?.TextHeight ?? _textHeight;
                return lastPoint + _hooklineDirection * hooklineLength + _annotationOffset;
            }
            else
            {
                return lastPoint + _annotationOffset;
            }
        }

        /// <summary>
        /// 计算勾线方向
        /// </summary>
        private void CalculateHooklineDirection()
        {
            if (_vertexes.Count < 2)
                return;

            // 根据最后一段的方向确定勾线方向
            Vector2 lastDir = (_vertexes[_vertexes.Count - 1] - _vertexes[_vertexes.Count - 2]).normalized;
            
            // 如果最后一段接近水平，勾线继续水平方向
            if (Math.Abs(lastDir.X) > Math.Abs(lastDir.Y))
            {
                _hooklineDirection = lastDir.X > 0 ? Vector2.UnitX : -Vector2.UnitX;
            }
            else
            {
                // 否则，勾线指向右�?
                _hooklineDirection = Vector2.UnitX;
            }
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new Leader();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Leader leader = base.Clone() as Leader;
            leader._vertexes = new List<Vector2>(_vertexes);
            leader._style = _style;
            leader._showArrowhead = _showArrowhead;
            leader._pathType = _pathType;
            leader._annotationType = _annotationType;
            leader._textAnnotation = _textAnnotation;
            leader._textHeight = _textHeight;
            leader._textWidth = _textWidth;
            leader._hasHookline = _hasHookline;
            leader._hooklineDirection = _hooklineDirection;
            leader._elevation = _elevation;
            leader._annotationOffset = _annotationOffset;
            return leader;
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            for (int i = 0; i < _vertexes.Count; i++)
            {
                _vertexes[i] += translation;
            }
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            for (int i = 0; i < _vertexes.Count; i++)
            {
                _vertexes[i] = Vector2.RotateInRadian(_vertexes[i], center, angle);
            }
            _hooklineDirection = Vector2.RotateInRadian(_hooklineDirection, Vector2.Zero, angle);
        }

        /// <summary>
        /// 变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            for (int i = 0; i < _vertexes.Count; i++)
            {
                _vertexes[i] = transform * _vertexes[i];
            }
            _hooklineDirection = (transform * _hooklineDirection).normalized;
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            
            // 每个顶点都是夹点
            for (int i = 0; i < _vertexes.Count; i++)
            {
                GripPointType type = i == 0 ? GripPointType.End : 
                                   i == _vertexes.Count - 1 ? GripPointType.End : 
                                   GripPointType.Center;
                gripPoints.Add(new GripPoint(type, _vertexes[i]));
            }
            
            // 文本位置夹点
            if (_annotationType == LeaderAnnotationType.Text && !string.IsNullOrEmpty(_textAnnotation))
            {
                gripPoints.Add(new GripPoint(GripPointType.Center, GetTextPosition()));
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
                _vertexes[index] = newPosition;
                
                // 如果修改了最后两个点，重新计算勾线方�?
                if (index >= _vertexes.Count - 2)
                {
                    CalculateHooklineDirection();
                }
            }
            else if (index == _vertexes.Count)
            {
                // 修改文本位置
                Vector2 currentTextPos = GetTextPosition();
                _annotationOffset = newPosition - (currentTextPos - _annotationOffset);
            }
        }

        /// <summary>
        /// 获取捕捉�?
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPoints = new List<ObjectSnapPoint>();
            
            // 顶点捕捉�?
            foreach (Vector2 vertex in _vertexes)
            {
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, vertex));
            }
            
            // 中点捕捉�?
            for (int i = 0; i < _vertexes.Count - 1; i++)
            {
                Vector2 midPoint = (_vertexes[i] + _vertexes[i + 1]) * 0.5;
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, midPoint));
            }
            
            return snapPoints;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 添加顶点
        /// </summary>
        public void AddVertex(Vector2 vertex)
        {
            _vertexes.Add(vertex);
            CalculateHooklineDirection();
        }

        /// <summary>
        /// 插入顶点
        /// </summary>
        public void InsertVertex(int index, Vector2 vertex)
        {
            if (index >= 0 && index <= _vertexes.Count)
            {
                _vertexes.Insert(index, vertex);
                CalculateHooklineDirection();
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
                CalculateHooklineDirection();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 设置文本注释
        /// </summary>
        public void SetTextAnnotation(string text)
        {
            _textAnnotation = text ?? string.Empty;
            _annotationType = string.IsNullOrEmpty(_textAnnotation) ? 
                            LeaderAnnotationType.None : 
                            LeaderAnnotationType.Text;
        }

        #endregion
    }

    /// <summary>
    /// 引线路径类型
    /// </summary>
    public enum LeaderPathType
    {
        /// <summary>
        /// 直线�?
        /// </summary>
        StraightLineSegments = 0,
        
        /// <summary>
        /// 样条曲线
        /// </summary>
        Spline = 1
    }

    /// <summary>
    /// 引线注释类型
    /// </summary>
    public enum LeaderAnnotationType
    {
        /// <summary>
        /// 无注�?
        /// </summary>
        None = 0,
        
        /// <summary>
        /// 文本注释
        /// </summary>
        Text = 1,
        
        /// <summary>
        /// 公差注释
        /// </summary>
        Tolerance = 2,
        
        /// <summary>
        /// 块注�?
        /// </summary>
        Block = 3
    }
}