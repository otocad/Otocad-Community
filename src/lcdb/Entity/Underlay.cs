using System;
using System.Collections.Generic;
using System.IO;
using LitMath;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 底图实体
    /// 原生OtoCAD实现，用于附加PDF、DWF或DGN文件作为参考底�?
    /// </summary>
    [Serializable]
    public class Underlay : Entity
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "Underlay";

        #region 字段

        // 底图定义
        private UnderlayDefinition _definition;
        
        // 插入�?
        private Vector2 _position = new Vector2();
        
        // 缩放比例
        private Vector2 _scale = new Vector2(1.0, 1.0);
        
        // 旋转角度（弧度）
        private double _rotation = 0.0;
        
        // 对比度（20-100�?
        private short _contrast = 100;
        
        // 淡化度（0-80�?
        private short _fade = 0;
        
        // 显示选项
        private UnderlayDisplayFlags _displayOptions = UnderlayDisplayFlags.ShowUnderlay;
        
        // 裁剪边界
        private UnderlayClippingBoundary _clippingBoundary;

        #endregion

        #region 属�?

        /// <summary>
        /// 底图定义
        /// </summary>
        public UnderlayDefinition Definition
        {
            get { return _definition; }
            set { _definition = value ?? throw new ArgumentNullException(nameof(value)); }
        }

        /// <summary>
        /// 插入�?
        /// </summary>
        public Vector2 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// 缩放比例
        /// </summary>
        public Vector2 Scale
        {
            get { return _scale; }
            set 
            { 
                if (value.X <= 0 || value.Y <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "缩放比例必须大于0");
                _scale = value; 
            }
        }

        /// <summary>
        /// 旋转角度（弧度）
        /// </summary>
        public double Rotation
        {
            get { return _rotation; }
            set { _rotation = value; }
        }

        /// <summary>
        /// 对比度（20-100�?
        /// </summary>
        public short Contrast
        {
            get { return _contrast; }
            set 
            { 
                if (value < 20 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(value), "对比度必须在20�?00之间");
                _contrast = value; 
            }
        }

        /// <summary>
        /// 淡化度（0-80�?
        /// </summary>
        public short Fade
        {
            get { return _fade; }
            set 
            { 
                if (value < 0 || value > 80)
                    throw new ArgumentOutOfRangeException(nameof(value), "淡化度必须在0�?0之间");
                _fade = value; 
            }
        }

        /// <summary>
        /// 显示选项
        /// </summary>
        public UnderlayDisplayFlags DisplayOptions
        {
            get { return _displayOptions; }
            set { _displayOptions = value; }
        }

        /// <summary>
        /// 裁剪边界
        /// </summary>
        public UnderlayClippingBoundary ClippingBoundary
        {
            get { return _clippingBoundary; }
            set { _clippingBoundary = value; }
        }

        /// <summary>
        /// 是否显示
        /// </summary>
        public bool IsOn
        {
            get { return (_displayOptions & UnderlayDisplayFlags.ShowUnderlay) != 0; }
            set
            {
                if (value)
                    _displayOptions |= UnderlayDisplayFlags.ShowUnderlay;
                else
                    _displayOptions &= ~UnderlayDisplayFlags.ShowUnderlay;
            }
        }

        /// <summary>
        /// 是否单色显示
        /// </summary>
        public bool IsMonochrome
        {
            get { return (_displayOptions & UnderlayDisplayFlags.Monochrome) != 0; }
            set
            {
                if (value)
                    _displayOptions |= UnderlayDisplayFlags.Monochrome;
                else
                    _displayOptions &= ~UnderlayDisplayFlags.Monochrome;
            }
        }

        /// <summary>
        /// 是否根据主图调整颜色
        /// </summary>
        public bool AdjustForBackground
        {
            get { return (_displayOptions & UnderlayDisplayFlags.AdjustForBackground) != 0; }
            set
            {
                if (value)
                    _displayOptions |= UnderlayDisplayFlags.AdjustForBackground;
                else
                    _displayOptions &= ~UnderlayDisplayFlags.AdjustForBackground;
            }
        }

        /// <summary>
        /// 是否裁剪
        /// </summary>
        public bool IsClipped
        {
            get { return (_displayOptions & UnderlayDisplayFlags.ClipUnderlay) != 0; }
            set
            {
                if (value)
                    _displayOptions |= UnderlayDisplayFlags.ClipUnderlay;
                else
                    _displayOptions &= ~UnderlayDisplayFlags.ClipUnderlay;
            }
        }


        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                if (_definition == null)
                    return new Bounding(Vector2.Zero, Vector2.Zero);

                // 获取底图的原始尺�?
                Vector2 size = _definition.GetSize();
                
                // 应用缩放
                size = new Vector2(size.X * _scale.X, size.Y * _scale.Y);
                
                // 计算四个角点
                Vector2[] corners = new Vector2[]
                {
                    new Vector2(0, 0),
                    new Vector2(size.X, 0),
                    new Vector2(size.X, size.Y),
                    new Vector2(0, size.Y)
                };
                
                // 应用旋转和平�?
                for (int i = 0; i < corners.Length; i++)
                {
                    corners[i] = Vector2.RotateInRadian(corners[i], Vector2.Zero, _rotation);
                    corners[i] += _position;
                }
                
                // 计算边界�?
                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue;
                
                foreach (var corner in corners)
                {
                    minX = Math.Min(minX, corner.X);
                    minY = Math.Min(minY, corner.Y);
                    maxX = Math.Max(maxX, corner.X);
                    maxY = Math.Max(maxY, corner.Y);
                }
                
                return new Bounding(new Vector2(minX, minY), new Vector2(maxX, maxY));
            }
        }

        #endregion
        
        #region 便捷属�?
        
        /// <summary>
        /// 底图路径（便捷属性）
        /// </summary>
        public string underlayPath
        {
            get { return _definition?.File; }
            set
            {
                if (_definition == null)
                {
                    // 根据文件扩展名自动创建相应的定义
                    string ext = System.IO.Path.GetExtension(value).ToLower();
                    switch (ext)
                    {
                        case ".pdf":
                            _definition = new PdfUnderlayDefinition();
                            break;
                        case ".dwf":
                        case ".dwfx":
                            _definition = new DwfUnderlayDefinition();
                            break;
                        case ".dgn":
                            _definition = new DgnUnderlayDefinition();
                            break;
                        default:
                            _definition = new PdfUnderlayDefinition(); // 默认PDF
                            break;
                    }
                }
                _definition.File = value;
            }
        }
        
        /// <summary>
        /// 底图名称（便捷属性）
        /// </summary>
        public string underlayName
        {
            get { return _definition?.Name; }
            set
            {
                if (_definition != null)
                    _definition.Name = value;
            }
        }
        
        /// <summary>
        /// 底图类型（便捷属性）
        /// </summary>
        public UnderlayType underlayType
        {
            get { return _definition?.Type ?? UnderlayType.DWF; }
            set
            {
                // 如果类型改变，需要重新创建定�?
                if (_definition?.Type != value)
                {
                    string file = _definition?.File;
                    string name = _definition?.Name;
                    
                    switch (value)
                    {
                        case UnderlayType.PDF:
                            _definition = new PdfUnderlayDefinition();
                            break;
                        case UnderlayType.DWF:
                            _definition = new DwfUnderlayDefinition();
                            break;
                        case UnderlayType.DGN:
                            _definition = new DgnUnderlayDefinition();
                            break;
                    }
                    
                    if (_definition != null)
                    {
                        _definition.File = file;
                        _definition.Name = name;
                    }
                }
            }
        }
        
        /// <summary>
        /// 缩放（便捷属性，同Scale�?
        /// </summary>
        public Vector2 scale
        {
            get { return _scale; }
            set { _scale = value; }
        }
        
        /// <summary>
        /// 旋转（便捷属性，同Rotation�?
        /// </summary>
        public double rotation
        {
            get { return _rotation; }
            set { _rotation = value; }
        }
        
        /// <summary>
        /// 裁剪边界（便捷属性）
        /// </summary>
        public List<Vector2> clippingBoundary
        {
            get 
            { 
                if (_clippingBoundary == null)
                    _clippingBoundary = new UnderlayClippingBoundary();
                return _clippingBoundary.Vertices; 
            }
            set
            {
                if (_clippingBoundary == null)
                    _clippingBoundary = new UnderlayClippingBoundary();
                _clippingBoundary.Vertices = value ?? new List<Vector2>();
            }
        }
        
        #endregion

        #region 构造函�?

        /// <summary>
        /// 创建底图
        /// </summary>
        public Underlay() : base()
        {
        }

        /// <summary>
        /// 创建底图
        /// </summary>
        /// <param name="definition">底图定义</param>
        public Underlay(UnderlayDefinition definition) : base()
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        /// <summary>
        /// 创建底图
        /// </summary>
        /// <param name="definition">底图定义</param>
        /// <param name="position">插入�?/param>
        /// <param name="scale">缩放比例</param>
        /// <param name="rotation">旋转角度（弧度）</param>
        public Underlay(UnderlayDefinition definition, Vector2 position, double scale, double rotation = 0.0) : base()
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _position = position;
            if (scale <= 0)
                throw new ArgumentOutOfRangeException(nameof(scale), "缩放比例必须大于0");
            _scale = new Vector2(scale, scale);
            _rotation = rotation;
        }

        #endregion

        #region 方法

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            if (_definition == null || !IsOn)
                return;

            // 获取底图尺寸
            Vector2 size = _definition.GetSize();
            size = new Vector2(size.X * _scale.X, size.Y * _scale.Y);
            
            // 计算四个角点
            Vector2[] corners = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(size.X, 0),
                new Vector2(size.X, size.Y),
                new Vector2(0, size.Y)
            };
            
            // 应用旋转和平�?
            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = Vector2.RotateInRadian(corners[i], Vector2.Zero, _rotation);
                corners[i] += _position;
            }
            
            // 绘制边框
            gd.DrawLine(corners[0], corners[1]);
            gd.DrawLine(corners[1], corners[2]);
            gd.DrawLine(corners[2], corners[3]);
            gd.DrawLine(corners[3], corners[0]);
            
            // 绘制对角线（表示这是一个底图）
            gd.DrawLine(corners[0], corners[2]);
            gd.DrawLine(corners[1], corners[3]);
            
            // 注意：实际的底图内容渲染需要更复杂的实现，
            // 可能需要集成PDF/DWF/DGN渲染�?
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new Underlay();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Underlay underlay = base.Clone() as Underlay;
            underlay._definition = _definition?.Clone() as UnderlayDefinition;
            underlay._position = _position;
            underlay._scale = _scale;
            underlay._rotation = _rotation;
            underlay._contrast = _contrast;
            underlay._fade = _fade;
            underlay._displayOptions = _displayOptions;
            underlay._clippingBoundary = _clippingBoundary?.Clone() as UnderlayClippingBoundary;
            return underlay;
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            _position += translation;
            
            // 平移裁剪边界
            if (_clippingBoundary != null)
            {
                for (int i = 0; i < _clippingBoundary.Vertices.Count; i++)
                {
                    _clippingBoundary.Vertices[i] += translation;
                }
            }
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            _position = Vector2.RotateInRadian(_position, center, angle);
            _rotation += angle;
            
            // 旋转裁剪边界
            if (_clippingBoundary != null)
            {
                for (int i = 0; i < _clippingBoundary.Vertices.Count; i++)
                {
                    _clippingBoundary.Vertices[i] = Vector2.RotateInRadian(_clippingBoundary.Vertices[i], center, angle);
                }
            }
        }

        /// <summary>
        /// 变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            _position = transform * _position;
            
            // 从变换矩阵提取缩放和旋转
            Vector2 xAxis = transform * new Vector2(1, 0) - transform * Vector2.Zero;
            Vector2 yAxis = transform * new Vector2(0, 1) - transform * Vector2.Zero;
            
            double scaleX = xAxis.length;
            double scaleY = yAxis.length;
            
            _scale = new Vector2(_scale.X * scaleX, _scale.Y * scaleY);
            
            double newRotation = Math.Atan2(xAxis.Y, xAxis.X);
            _rotation = newRotation;
            
            // 变换裁剪边界
            if (_clippingBoundary != null)
            {
                for (int i = 0; i < _clippingBoundary.Vertices.Count; i++)
                {
                    _clippingBoundary.Vertices[i] = transform * _clippingBoundary.Vertices[i];
                }
            }
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            
            // 获取底图尺寸
            Vector2 size = _definition != null ? _definition.GetSize() : new Vector2(100, 100);
            size = new Vector2(size.X * _scale.X, size.Y * _scale.Y);
            
            // 计算四个角点
            Vector2[] corners = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(size.X, 0),
                new Vector2(size.X, size.Y),
                new Vector2(0, size.Y)
            };
            
            // 应用旋转和平�?
            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = Vector2.RotateInRadian(corners[i], Vector2.Zero, _rotation);
                corners[i] += _position;
                gripPoints.Add(new GripPoint(GripPointType.Corner, corners[i]));
            }
            
            // 中心点夹�?
            Vector2 center = (corners[0] + corners[2]) * 0.5;
            gripPoints.Add(new GripPoint(GripPointType.Center, center));
            
            return gripPoints;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            if (_definition == null)
                return;
                
            Vector2 size = _definition.GetSize();
            size = new Vector2(size.X * _scale.X, size.Y * _scale.Y);
            
            if (index < 4)
            {
                // 角点夹点 - 调整大小
                Vector2[] corners = new Vector2[]
                {
                    new Vector2(0, 0),
                    new Vector2(size.X, 0),
                    new Vector2(size.X, size.Y),
                    new Vector2(0, size.Y)
                };
                
                // 应用当前旋转
                for (int i = 0; i < corners.Length; i++)
                {
                    corners[i] = Vector2.RotateInRadian(corners[i], Vector2.Zero, _rotation);
                    corners[i] += _position;
                }
                
                // 计算新的缩放
                Vector2 opposite = corners[(index + 2) % 4];
                Vector2 newCenter = (newPosition + opposite) * 0.5;
                
                // 计算新尺�?
                Vector2 delta = newPosition - opposite;
                double newWidth = Math.Abs(delta.length * Math.Cos(Math.Atan2(delta.Y, delta.X) - _rotation));
                double newHeight = Math.Abs(delta.length * Math.Sin(Math.Atan2(delta.Y, delta.X) - _rotation));
                
                if (_definition.GetSize().X > 0 && _definition.GetSize().Y > 0)
                {
                    _scale = new Vector2(newWidth / _definition.GetSize().X, newHeight / _definition.GetSize().Y);
                }
                
                _position = newCenter - Vector2.RotateInRadian(size * 0.5, Vector2.Zero, _rotation);
            }
            else if (index == 4)
            {
                // 中心点夹�?- 移动
                Vector2 currentCenter = _position + Vector2.RotateInRadian(size * 0.5, Vector2.Zero, _rotation);
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
            
            if (_definition == null)
                return snapPoints;
                
            // 获取底图尺寸
            Vector2 size = _definition.GetSize();
            size = new Vector2(size.X * _scale.X, size.Y * _scale.Y);
            
            // 计算四个角点
            Vector2[] corners = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(size.X, 0),
                new Vector2(size.X, size.Y),
                new Vector2(0, size.Y)
            };
            
            // 应用旋转和平�?
            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = Vector2.RotateInRadian(corners[i], Vector2.Zero, _rotation);
                corners[i] += _position;
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, corners[i]));
            }
            
            // 边中�?
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (corners[0] + corners[1]) * 0.5));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (corners[1] + corners[2]) * 0.5));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (corners[2] + corners[3]) * 0.5));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (corners[3] + corners[0]) * 0.5));
            
            // 中心�?
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Center, (corners[0] + corners[2]) * 0.5));
            
            return snapPoints;
        }

        #endregion
    }

    /// <summary>
    /// 底图定义
    /// </summary>
    [Serializable]
    public abstract class UnderlayDefinition : DBObject
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "UnderlayDefinition";

        protected string _name;
        protected string _file;
        protected UnderlayType _type;

        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value ?? string.Empty; }
        }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string File
        {
            get { return _file; }
            set 
            { 
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException(nameof(value));
                _file = value; 
            }
        }

        /// <summary>
        /// 底图类型
        /// </summary>
        public UnderlayType Type
        {
            get { return _type; }
        }

        /// <summary>
        /// 获取底图尺寸
        /// </summary>
        public abstract Vector2 GetSize();

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            throw new NotImplementedException("Use derived classes");
        }
    }

    /// <summary>
    /// PDF底图定义
    /// </summary>
    [Serializable]
    public class PdfUnderlayDefinition : UnderlayDefinition
    {
        private int _pageNumber = 1;
        private Vector2 _pageSize = new Vector2(210, 297); // A4 默认尺寸（毫米）

        public PdfUnderlayDefinition() 
        {
            _type = UnderlayType.PDF;
        }

        public PdfUnderlayDefinition(string name, string file) : this()
        {
            _name = name;
            _file = file;
        }

        /// <summary>
        /// 页码
        /// </summary>
        public int PageNumber
        {
            get { return _pageNumber; }
            set { _pageNumber = Math.Max(1, value); }
        }

        /// <summary>
        /// 页面尺寸
        /// </summary>
        public Vector2 PageSize
        {
            get { return _pageSize; }
            set { _pageSize = value; }
        }

        public override Vector2 GetSize()
        {
            return _pageSize;
        }

        protected override DBObject CreateInstance()
        {
            return new PdfUnderlayDefinition();
        }

        public override object Clone()
        {
            PdfUnderlayDefinition def = base.Clone() as PdfUnderlayDefinition;
            def._pageNumber = _pageNumber;
            def._pageSize = _pageSize;
            return def;
        }
    }

    /// <summary>
    /// DWF底图定义
    /// </summary>
    [Serializable]
    public class DwfUnderlayDefinition : UnderlayDefinition
    {
        private string _sheetName = string.Empty;
        private Vector2 _sheetSize = new Vector2(420, 594); // A2 默认尺寸（毫米）

        public DwfUnderlayDefinition() 
        {
            _type = UnderlayType.DWF;
        }

        public DwfUnderlayDefinition(string name, string file) : this()
        {
            _name = name;
            _file = file;
        }

        /// <summary>
        /// 图纸名称
        /// </summary>
        public string SheetName
        {
            get { return _sheetName; }
            set { _sheetName = value ?? string.Empty; }
        }

        /// <summary>
        /// 图纸尺寸
        /// </summary>
        public Vector2 SheetSize
        {
            get { return _sheetSize; }
            set { _sheetSize = value; }
        }

        public override Vector2 GetSize()
        {
            return _sheetSize;
        }

        protected override DBObject CreateInstance()
        {
            return new DwfUnderlayDefinition();
        }

        public override object Clone()
        {
            DwfUnderlayDefinition def = base.Clone() as DwfUnderlayDefinition;
            def._sheetName = _sheetName;
            def._sheetSize = _sheetSize;
            return def;
        }
    }

    /// <summary>
    /// DGN底图定义
    /// </summary>
    [Serializable]
    public class DgnUnderlayDefinition : UnderlayDefinition
    {
        private string _modelName = string.Empty;
        private Vector2 _modelSize = new Vector2(1000, 1000); // 默认尺寸

        public DgnUnderlayDefinition() 
        {
            _type = UnderlayType.DGN;
        }

        public DgnUnderlayDefinition(string name, string file) : this()
        {
            _name = name;
            _file = file;
        }

        /// <summary>
        /// 模型名称
        /// </summary>
        public string ModelName
        {
            get { return _modelName; }
            set { _modelName = value ?? string.Empty; }
        }

        /// <summary>
        /// 模型尺寸
        /// </summary>
        public Vector2 ModelSize
        {
            get { return _modelSize; }
            set { _modelSize = value; }
        }

        public override Vector2 GetSize()
        {
            return _modelSize;
        }

        protected override DBObject CreateInstance()
        {
            return new DgnUnderlayDefinition();
        }

        public override object Clone()
        {
            DgnUnderlayDefinition def = base.Clone() as DgnUnderlayDefinition;
            def._modelName = _modelName;
            def._modelSize = _modelSize;
            return def;
        }
    }

    /// <summary>
    /// 底图类型
    /// </summary>
    public enum UnderlayType
    {
        /// <summary>
        /// PDF文件
        /// </summary>
        PDF,
        
        /// <summary>
        /// DWF文件
        /// </summary>
        DWF,
        
        /// <summary>
        /// DGN文件
        /// </summary>
        DGN
    }

    /// <summary>
    /// 底图显示标志
    /// </summary>
    [Flags]
    public enum UnderlayDisplayFlags
    {
        /// <summary>
        /// �?
        /// </summary>
        None = 0,
        
        /// <summary>
        /// 显示底图
        /// </summary>
        ShowUnderlay = 1,
        
        /// <summary>
        /// 单色显示
        /// </summary>
        Monochrome = 2,
        
        /// <summary>
        /// 根据背景调整颜色
        /// </summary>
        AdjustForBackground = 4,
        
        /// <summary>
        /// 裁剪底图
        /// </summary>
        ClipUnderlay = 8,
        
        /// <summary>
        /// 使用打印时透明�?
        /// </summary>
        UseTransparencyForPlot = 16
    }

    /// <summary>
    /// 底图裁剪边界
    /// </summary>
    [Serializable]
    public class UnderlayClippingBoundary : ICloneable
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
            set { _vertices = value ?? new List<Vector2>(); }
        }

        /// <summary>
        /// 是否反转裁剪（裁剪外部）
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
            UnderlayClippingBoundary boundary = new UnderlayClippingBoundary();
            boundary._isEnabled = _isEnabled;
            boundary._vertices = new List<Vector2>(_vertices);
            boundary._isInverted = _isInverted;
            return boundary;
        }
    }
}