using System;
using System.Collections.Generic;
using LitMath;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 视口实体
    /// 原生OtoCAD实现，用于在布局空间中显示模型空间的视图
    /// </summary>
    [Serializable]
    public class Viewport : Entity
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "Viewport";

        #region 字段

        // 视口中心点（在纸空间中）
        private Vector3 _centerPoint = new Vector3();
        
        // 视口宽度
        private double _width = 100.0;
        
        // 视口高度
        private double _height = 100.0;
        
        // 视图中心点（在模型空间中�?
        private Vector3 _viewCenter = new Vector3();
        
        // 视图方向
        private Vector3 _viewDirection = new Vector3(0, 0, 1);
        
        // 视图目标�?
        private Vector3 _viewTarget = new Vector3();
        
        // 视图高度（模型空间单位）
        private double _viewHeight = 100.0;
        
        // 视图扭转角度
        private double _twistAngle = 0.0;
        
        // 缩放比例
        private double _customScale = 1.0;
        
        // 是否锁定
        private bool _isLocked = false;
        
        // 是否打开
        private bool _isOn = true;
        
        // 是否冻结
        private bool _isFrozen = false;
        
        // 是否透视模式
        private bool _isPerspective = false;
        
        // 裁剪边界
        private ViewportClipBoundary _clipBoundary;
        
        // 视口ID
        private int _viewportId = 1;
        
        // 是否显示网格
        private bool _showGrid = false;
        
        // 是否捕捉到网�?
        private bool _snapToGrid = false;
        
        // 网格间距
        private Vector2 _gridSpacing = new Vector2(10, 10);

        #endregion

        #region 属�?

        /// <summary>
        /// 视口中心点（纸空间）
        /// </summary>
        public Vector3 CenterPoint
        {
            get { return _centerPoint; }
            set { _centerPoint = value; }
        }

        /// <summary>
        /// 视口宽度
        /// </summary>
        public double Width
        {
            get { return _width; }
            set { _width = Math.Max(0.1, value); }
        }

        /// <summary>
        /// 视口高度
        /// </summary>
        public double Height
        {
            get { return _height; }
            set { _height = Math.Max(0.1, value); }
        }

        /// <summary>
        /// 视图中心点（模型空间�?
        /// </summary>
        public Vector3 ViewCenter
        {
            get { return _viewCenter; }
            set { _viewCenter = value; }
        }

        /// <summary>
        /// 视图方向
        /// </summary>
        public Vector3 ViewDirection
        {
            get { return _viewDirection; }
            set { _viewDirection = value.normalized; }
        }

        /// <summary>
        /// 视图目标�?
        /// </summary>
        public Vector3 ViewTarget
        {
            get { return _viewTarget; }
            set { _viewTarget = value; }
        }

        /// <summary>
        /// 视图高度（模型空间单位）
        /// </summary>
        public double ViewHeight
        {
            get { return _viewHeight; }
            set { _viewHeight = Math.Max(0.1, value); }
        }

        /// <summary>
        /// 视图扭转角度（弧度）
        /// </summary>
        public double TwistAngle
        {
            get { return _twistAngle; }
            set { _twistAngle = value; }
        }

        /// <summary>
        /// 自定义缩放比�?
        /// </summary>
        public double CustomScale
        {
            get { return _customScale; }
            set { _customScale = Math.Max(0.001, value); }
        }

        /// <summary>
        /// 是否锁定
        /// </summary>
        public new bool IsLocked
        {
            get { return _isLocked; }
            set { _isLocked = value; }
        }

        /// <summary>
        /// 是否打开
        /// </summary>
        public bool IsOn
        {
            get { return _isOn; }
            set { _isOn = value; }
        }

        /// <summary>
        /// 是否冻结
        /// </summary>
        public bool IsFrozen
        {
            get { return _isFrozen; }
            set { _isFrozen = value; }
        }

        /// <summary>
        /// 是否透视模式
        /// </summary>
        public bool IsPerspective
        {
            get { return _isPerspective; }
            set { _isPerspective = value; }
        }

        /// <summary>
        /// 裁剪边界
        /// </summary>
        public ViewportClipBoundary ClipBoundary
        {
            get { return _clipBoundary; }
            set { _clipBoundary = value; }
        }

        /// <summary>
        /// 视口ID
        /// </summary>
        public int ViewportId
        {
            get { return _viewportId; }
            set { _viewportId = value; }
        }

        /// <summary>
        /// 是否显示网格
        /// </summary>
        public bool ShowGrid
        {
            get { return _showGrid; }
            set { _showGrid = value; }
        }

        /// <summary>
        /// 是否捕捉到网�?
        /// </summary>
        public bool SnapToGrid
        {
            get { return _snapToGrid; }
            set { _snapToGrid = value; }
        }

        /// <summary>
        /// 网格间距
        /// </summary>
        public Vector2 GridSpacing
        {
            get { return _gridSpacing; }
            set { _gridSpacing = new Vector2(Math.Max(0.1, value.X), Math.Max(0.1, value.Y)); }
        }

        /// <summary>
        /// 获取缩放因子（纸空间到模型空间）
        /// </summary>
        public double ScaleFactor
        {
            get { return _viewHeight / _height; }
        }


        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                Vector2 center2d = new Vector2(_centerPoint.X, _centerPoint.Y);
                Vector2 halfSize = new Vector2(_width / 2, _height / 2);
                
                return new Bounding(
                    center2d - halfSize,
                    center2d + halfSize
                );
            }
        }

        #endregion

        #region 构造函�?

        /// <summary>
        /// 创建视口
        /// </summary>
        public Viewport() : base()
        {
        }

        /// <summary>
        /// 创建视口
        /// </summary>
        /// <param name="center">中心�?/param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        public Viewport(Vector2 center, double width, double height) : base()
        {
            _centerPoint = new Vector3(center.X, center.Y, 0);
            _width = width;
            _height = height;
            _viewHeight = height; // 初始1:1比例
        }

        #endregion

        #region 方法

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            // 绘制视口边框
            Vector2 center2d = new Vector2(_centerPoint.X, _centerPoint.Y);
            Vector2 halfSize = new Vector2(_width / 2, _height / 2);
            
            Vector2 p1 = center2d + new Vector2(-halfSize.X, -halfSize.Y);
            Vector2 p2 = center2d + new Vector2(halfSize.X, -halfSize.Y);
            Vector2 p3 = center2d + new Vector2(halfSize.X, halfSize.Y);
            Vector2 p4 = center2d + new Vector2(-halfSize.X, halfSize.Y);
            
            // 如果视口关闭或冻结，使用虚线
            if (!_isOn || _isFrozen)
            {
                // 简化处理：仍然使用实线，但可以在实际实现中使用虚线
            }
            
            gd.DrawLine(p1, p2);
            gd.DrawLine(p2, p3);
            gd.DrawLine(p3, p4);
            gd.DrawLine(p4, p1);
            
            // 如果有裁剪边界，绘制裁剪边界
            if (_clipBoundary != null && _clipBoundary.IsEnabled)
            {
                DrawClipBoundary(gd);
            }
            
            // 绘制锁定标记（如果锁定）
            if (_isLocked)
            {
                DrawLockSymbol(gd, center2d);
            }
            
            // 注意：实际的模型空间内容渲染需要更复杂的实�?
            // 这里只是绘制视口框架
        }

        /// <summary>
        /// 绘制裁剪边界
        /// </summary>
        private void DrawClipBoundary(IGraphicsDraw gd)
        {
            if (_clipBoundary.Vertices.Count < 3)
                return;
                
            for (int i = 0; i < _clipBoundary.Vertices.Count; i++)
            {
                Vector2 start = _clipBoundary.Vertices[i];
                Vector2 end = _clipBoundary.Vertices[(i + 1) % _clipBoundary.Vertices.Count];
                gd.DrawLine(start, end);
            }
        }

        /// <summary>
        /// 绘制锁定符号
        /// </summary>
        private void DrawLockSymbol(IGraphicsDraw gd, Vector2 center)
        {
            // 在视口角落绘制一个小锁符�?
            double symbolSize = Math.Min(_width, _height) * 0.05;
            Vector2 symbolPos = center + new Vector2(_width / 2 - symbolSize * 2, _height / 2 - symbolSize * 2);
            
            // 简化的锁符号（矩形�?
            Vector2 lockP1 = symbolPos + new Vector2(-symbolSize / 2, -symbolSize / 2);
            Vector2 lockP2 = symbolPos + new Vector2(symbolSize / 2, -symbolSize / 2);
            Vector2 lockP3 = symbolPos + new Vector2(symbolSize / 2, symbolSize / 2);
            Vector2 lockP4 = symbolPos + new Vector2(-symbolSize / 2, symbolSize / 2);
            
            gd.DrawLine(lockP1, lockP2);
            gd.DrawLine(lockP2, lockP3);
            gd.DrawLine(lockP3, lockP4);
            gd.DrawLine(lockP4, lockP1);
        }

        /// <summary>
        /// 将纸空间点转换为模型空间�?
        /// </summary>
        public Vector2 PaperToModel(Vector2 paperPoint)
        {
            // 相对于视口中心的偏移
            Vector2 offset = paperPoint - new Vector2(_centerPoint.X, _centerPoint.Y);
            
            // 应用缩放
            offset *= ScaleFactor;
            
            // 应用旋转
            if (Math.Abs(_twistAngle) > 1e-10)
            {
                double cos = Math.Cos(-_twistAngle);
                double sin = Math.Sin(-_twistAngle);
                double x = offset.X * cos - offset.Y * sin;
                double y = offset.X * sin + offset.Y * cos;
                offset = new Vector2(x, y);
            }
            
            // 加上模型空间中心
            return new Vector2(_viewCenter.X, _viewCenter.Y) + offset;
        }

        /// <summary>
        /// 将模型空间点转换为纸空间�?
        /// </summary>
        public Vector2 ModelToPaper(Vector2 modelPoint)
        {
            // 相对于视图中心的偏移
            Vector2 offset = modelPoint - new Vector2(_viewCenter.X, _viewCenter.Y);
            
            // 应用旋转
            if (Math.Abs(_twistAngle) > 1e-10)
            {
                double cos = Math.Cos(_twistAngle);
                double sin = Math.Sin(_twistAngle);
                double x = offset.X * cos - offset.Y * sin;
                double y = offset.X * sin + offset.Y * cos;
                offset = new Vector2(x, y);
            }
            
            // 应用缩放
            offset /= ScaleFactor;
            
            // 加上纸空间中�?
            return new Vector2(_centerPoint.X, _centerPoint.Y) + offset;
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new Viewport();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Viewport viewport = base.Clone() as Viewport;
            viewport._centerPoint = _centerPoint;
            viewport._width = _width;
            viewport._height = _height;
            viewport._viewCenter = _viewCenter;
            viewport._viewDirection = _viewDirection;
            viewport._viewTarget = _viewTarget;
            viewport._viewHeight = _viewHeight;
            viewport._twistAngle = _twistAngle;
            viewport._customScale = _customScale;
            viewport._isLocked = _isLocked;
            viewport._isOn = _isOn;
            viewport._isFrozen = _isFrozen;
            viewport._isPerspective = _isPerspective;
            viewport._clipBoundary = _clipBoundary?.Clone() as ViewportClipBoundary;
            viewport._viewportId = _viewportId;
            viewport._showGrid = _showGrid;
            viewport._snapToGrid = _snapToGrid;
            viewport._gridSpacing = _gridSpacing;
            return viewport;
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            _centerPoint = new Vector3(_centerPoint.X + translation.X, _centerPoint.Y + translation.Y, _centerPoint.Z);
            
            // 平移裁剪边界
            if (_clipBoundary != null)
            {
                for (int i = 0; i < _clipBoundary.Vertices.Count; i++)
                {
                    _clipBoundary.Vertices[i] += translation;
                }
            }
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            Vector2 center2d = new Vector2(_centerPoint.X, _centerPoint.Y);
            Vector2 rotated = Vector2.RotateInRadian(center2d, center, angle);
            _centerPoint = new Vector3(rotated.X, rotated.Y, _centerPoint.Z);
            
            // 注意：通常视口本身不旋转，只是位置旋转
        }

        /// <summary>
        /// 变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            Vector2 center2d = new Vector2(_centerPoint.X, _centerPoint.Y);
            Vector2 transformed = transform * center2d;
            _centerPoint = new Vector3(transformed.X, transformed.Y, _centerPoint.Z);
            
            // 变换尺寸（假设均匀缩放�?
            Vector2 sizeVec = new Vector2(_width, 0);
            Vector2 transformedSize = transform * sizeVec - transform * Vector2.Zero;
            double scale = transformedSize.length;
            _width *= scale;
            _height *= scale;
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            
            Vector2 center2d = new Vector2(_centerPoint.X, _centerPoint.Y);
            Vector2 halfSize = new Vector2(_width / 2, _height / 2);
            
            // 四个角点
            gripPoints.Add(new GripPoint(GripPointType.Corner, center2d + new Vector2(-halfSize.X, -halfSize.Y)));
            gripPoints.Add(new GripPoint(GripPointType.Corner, center2d + new Vector2(halfSize.X, -halfSize.Y)));
            gripPoints.Add(new GripPoint(GripPointType.Corner, center2d + new Vector2(halfSize.X, halfSize.Y)));
            gripPoints.Add(new GripPoint(GripPointType.Corner, center2d + new Vector2(-halfSize.X, halfSize.Y)));
            
            // 中心�?
            gripPoints.Add(new GripPoint(GripPointType.Center, center2d));
            
            return gripPoints;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            Vector2 center2d = new Vector2(_centerPoint.X, _centerPoint.Y);
            
            switch (index)
            {
                case 0: // 左下�?
                case 1: // 右下�?
                case 2: // 右上�?
                case 3: // 左上�?
                    {
                        // 调整大小
                        Vector2 opposite = center2d;
                        switch (index)
                        {
                            case 0: opposite = center2d + new Vector2(_width / 2, _height / 2); break;
                            case 1: opposite = center2d + new Vector2(-_width / 2, _height / 2); break;
                            case 2: opposite = center2d + new Vector2(-_width / 2, -_height / 2); break;
                            case 3: opposite = center2d + new Vector2(_width / 2, -_height / 2); break;
                        }
                        
                        Vector2 newCenter = (newPosition + opposite) * 0.5;
                        _centerPoint = new Vector3(newCenter.X, newCenter.Y, _centerPoint.Z);
                        _width = Math.Abs(newPosition.X - opposite.X);
                        _height = Math.Abs(newPosition.Y - opposite.Y);
                    }
                    break;
                case 4: // 中心�?
                    _centerPoint = new Vector3(newPosition.X, newPosition.Y, _centerPoint.Z);
                    break;
            }
        }

        /// <summary>
        /// 获取捕捉�?
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPoints = new List<ObjectSnapPoint>();
            
            Vector2 center2d = new Vector2(_centerPoint.X, _centerPoint.Y);
            Vector2 halfSize = new Vector2(_width / 2, _height / 2);
            
            // 四个角点
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, center2d + new Vector2(-halfSize.X, -halfSize.Y)));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, center2d + new Vector2(halfSize.X, -halfSize.Y)));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, center2d + new Vector2(halfSize.X, halfSize.Y)));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, center2d + new Vector2(-halfSize.X, halfSize.Y)));
            
            // 边中�?
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, center2d + new Vector2(0, -halfSize.Y)));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, center2d + new Vector2(halfSize.X, 0)));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, center2d + new Vector2(0, halfSize.Y)));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, center2d + new Vector2(-halfSize.X, 0)));
            
            // 中心�?
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Center, center2d));
            
            return snapPoints;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 设置标准视图
        /// </summary>
        public void SetStandardView(StandardView view)
        {
            switch (view)
            {
                case StandardView.Top:
                    _viewDirection = new Vector3(0, 0, 1);
                    _twistAngle = 0;
                    break;
                case StandardView.Bottom:
                    _viewDirection = new Vector3(0, 0, -1);
                    _twistAngle = 0;
                    break;
                case StandardView.Front:
                    _viewDirection = new Vector3(0, -1, 0);
                    _twistAngle = 0;
                    break;
                case StandardView.Back:
                    _viewDirection = new Vector3(0, 1, 0);
                    _twistAngle = 0;
                    break;
                case StandardView.Left:
                    _viewDirection = new Vector3(-1, 0, 0);
                    _twistAngle = 0;
                    break;
                case StandardView.Right:
                    _viewDirection = new Vector3(1, 0, 0);
                    _twistAngle = 0;
                    break;
            }
        }

        /// <summary>
        /// 缩放到范�?
        /// </summary>
        public void ZoomExtents(Bounding modelBounding)
        {
            if (!modelBounding.IsValid)
                return;
                
            _viewCenter = new Vector3(modelBounding.center.X, modelBounding.center.Y, 0);
            
            // 计算需要的视图高度以适应边界
            double requiredWidth = modelBounding.width * 1.1; // 10%边距
            double requiredHeight = modelBounding.height * 1.1;
            
            // 考虑视口宽高�?
            double aspectRatio = _width / _height;
            
            if (requiredWidth / requiredHeight > aspectRatio)
            {
                _viewHeight = requiredWidth / aspectRatio;
            }
            else
            {
                _viewHeight = requiredHeight;
            }
        }

        #endregion
    }

    /// <summary>
    /// 视口裁剪边界
    /// </summary>
    [Serializable]
    public class ViewportClipBoundary : ICloneable
    {
        private bool _isEnabled = false;
        private List<Vector2> _vertices = new List<Vector2>();

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
        /// 克隆
        /// </summary>
        public object Clone()
        {
            ViewportClipBoundary boundary = new ViewportClipBoundary();
            boundary._isEnabled = _isEnabled;
            boundary._vertices = new List<Vector2>(_vertices);
            return boundary;
        }
    }

    /// <summary>
    /// 标准视图
    /// </summary>
    public enum StandardView
    {
        /// <summary>
        /// 顶视�?
        /// </summary>
        Top,
        
        /// <summary>
        /// 底视�?
        /// </summary>
        Bottom,
        
        /// <summary>
        /// 前视�?
        /// </summary>
        Front,
        
        /// <summary>
        /// 后视�?
        /// </summary>
        Back,
        
        /// <summary>
        /// 左视�?
        /// </summary>
        Left,
        
        /// <summary>
        /// 右视�?
        /// </summary>
        Right
    }
}