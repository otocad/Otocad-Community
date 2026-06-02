using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using LitMath;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 图像实体
    /// 原生OtoCAD实现，用于在图纸中插入光栅图�?
    /// </summary>
    [Serializable]
    public class Image : Entity
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "Image";

        #region 字段

        // 图像定义（包含图像数据和路径�?
        private ImageDefinition _imageDefinition;
        
        // 插入点（左下角）
        private Vector3 _position = new Vector3();
        
        // U向量（宽度方向）
        private Vector3 _uVector = new Vector3(1, 0, 0);
        
        // V向量（高度方向）
        private Vector3 _vVector = new Vector3(0, 1, 0);
        
        // 图像尺寸（像素）
        private Size _imageSize = new Size(1, 1);
        
        // 显示尺寸（图纸单位）
        private Vector2 _displaySize = new Vector2(1, 1);
        
        // 旋转角度（弧度）
        private double _rotation = 0.0;
        
        // 亮度�?-100�?
        private int _brightness = 50;
        
        // 对比度（0-100�?
        private int _contrast = 50;
        
        // 淡入度（0-100�?
        private int _fade = 0;
        
        // 裁剪边界
        private ImageClipBoundary _clipBoundary;
        
        // 是否显示图像
        private bool _showImage = true;
        
        // 是否显示裁剪边界
        private bool _showClipBoundary = false;
        
        // 透明度（0-255, 255为不透明�?
        private byte _transparency = 255;

        #endregion

        #region 属�?

        /// <summary>
        /// 图像定义
        /// </summary>
        public ImageDefinition ImageDefinition
        {
            get { return _imageDefinition; }
            set 
            { 
                _imageDefinition = value;
                if (_imageDefinition != null && _imageDefinition.ImageLoaded)
                {
                    _imageSize = _imageDefinition.ImageSize;
                }
            }
        }
        
        /// <summary>
        /// 图像路径（便利属性）
        /// </summary>
        public string imagePath
        {
            get { return _imageDefinition?.FilePath; }
            set 
            { 
                if (_imageDefinition == null)
                {
                    _imageDefinition = new ImageDefinition(value);
                }
                else
                {
                    // Cannot set FilePath directly - need to create new ImageDefinition
                    _imageDefinition = new ImageDefinition(value);
                }
                if (_imageDefinition.ImageLoaded)
                {
                    _imageSize = _imageDefinition.ImageSize;
                }
            }
        }

        /// <summary>
        /// 插入�?
        /// </summary>
        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// U向量（宽度方向）
        /// </summary>
        public Vector3 UVector
        {
            get { return _uVector; }
            set { _uVector = value; }
        }

        /// <summary>
        /// V向量（高度方向）
        /// </summary>
        public Vector3 VVector
        {
            get { return _vVector; }
            set { _vVector = value; }
        }

        /// <summary>
        /// 图像尺寸（像素）
        /// </summary>
        public Size ImageSize
        {
            get { return _imageSize; }
        }

        /// <summary>
        /// 显示尺寸（图纸单位）
        /// </summary>
        public Vector2 DisplaySize
        {
            get { return _displaySize; }
            set 
            { 
                _displaySize = value;
                UpdateVectors();
            }
        }
        
        /// <summary>
        /// 宽度
        /// </summary>
        public double Width
        {
            get { return _displaySize.X; }
            set 
            { 
                _displaySize = new Vector2(value, _displaySize.Y);
                UpdateVectors();
            }
        }
        
        /// <summary>
        /// 高度
        /// </summary>
        public double Height
        {
            get { return _displaySize.Y; }
            set 
            { 
                _displaySize = new Vector2(_displaySize.X, value);
                UpdateVectors();
            }
        }

        /// <summary>
        /// 旋转角度（弧度）
        /// </summary>
        public double Rotation
        {
            get { return _rotation; }
            set 
            { 
                _rotation = value;
                UpdateVectors();
            }
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
        /// 对比度（0-100�?
        /// </summary>
        public int Contrast
        {
            get { return _contrast; }
            set { _contrast = Math.Max(0, Math.Min(100, value)); }
        }

        /// <summary>
        /// 淡入度（0-100�?
        /// </summary>
        public int Fade
        {
            get { return _fade; }
            set { _fade = Math.Max(0, Math.Min(100, value)); }
        }

        /// <summary>
        /// 裁剪边界
        /// </summary>
        public ImageClipBoundary ClipBoundary
        {
            get { return _clipBoundary; }
            set { _clipBoundary = value; }
        }

        /// <summary>
        /// 是否显示图像
        /// </summary>
        public bool ShowImage
        {
            get { return _showImage; }
            set { _showImage = value; }
        }

        /// <summary>
        /// 是否显示裁剪边界
        /// </summary>
        public bool ShowClipBoundary
        {
            get { return _showClipBoundary; }
            set { _showClipBoundary = value; }
        }

        /// <summary>
        /// 透明度（0-255�?
        /// </summary>
        public byte Transparency
        {
            get { return _transparency; }
            set { _transparency = value; }
        }


        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                Vector2 pos2d = new Vector2(_position.X, _position.Y);
                
                // 计算四个角点
                Vector2[] corners = new Vector2[]
                {
                    pos2d,
                    pos2d + new Vector2(_uVector.X, _uVector.Y),
                    pos2d + new Vector2(_uVector.X + _vVector.X, _uVector.Y + _vVector.Y),
                    pos2d + new Vector2(_vVector.X, _vVector.Y)
                };
                
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

        #region 构造函�?

        /// <summary>
        /// 创建图像
        /// </summary>
        public Image() : base()
        {
        }

        /// <summary>
        /// 创建图像
        /// </summary>
        /// <param name="imagePath">图像文件路径</param>
        /// <param name="position">插入�?/param>
        /// <param name="displaySize">显示尺寸</param>
        public Image(string imagePath, Vector2 position, Vector2 displaySize) : base()
        {
            _position = new Vector3(position.X, position.Y, 0);
            _displaySize = displaySize;
            _imageDefinition = new ImageDefinition(imagePath);
            
            if (_imageDefinition.ImageLoaded)
            {
                _imageSize = _imageDefinition.ImageSize;
            }

            UpdateVectors();
        }

        #endregion

        #region 方法

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            if (!_showImage || _imageDefinition == null || !_imageDefinition.ImageLoaded)
                return;

            // 绘制图像
            // 注意：这需要IGraphicsDraw接口支持图像绘制
            // 这里提供一个简化的实现思路
            
            // 计算图像的四个角�?
            Vector2 p1 = new Vector2(_position.X, _position.Y);
            Vector2 p2 = p1 + new Vector2(_uVector.X, _uVector.Y);
            Vector2 p3 = p1 + new Vector2(_uVector.X + _vVector.X, _uVector.Y + _vVector.Y);
            Vector2 p4 = p1 + new Vector2(_vVector.X, _vVector.Y);
            
            // 如果需要显示边界，绘制边框
            if (_showClipBoundary || !_showImage)
            {
                gd.DrawLine(p1, p2);
                gd.DrawLine(p2, p3);
                gd.DrawLine(p3, p4);
                gd.DrawLine(p4, p1);
            }
            
            // 实际的图像绘制需要GDI+或其他图形库支持
            // 这里只是绘制边框作为占位
        }

        /// <summary>
        /// 更新向量
        /// </summary>
        private void UpdateVectors()
        {
            double cos = Math.Cos(_rotation);
            double sin = Math.Sin(_rotation);
            
            _uVector = new Vector3(_displaySize.X * cos, _displaySize.X * sin, 0);
            _vVector = new Vector3(-_displaySize.Y * sin, _displaySize.Y * cos, 0);
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new Image();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Image image = base.Clone() as Image;
            image._imageDefinition = _imageDefinition; // 共享图像定义
            image._position = _position;
            image._uVector = _uVector;
            image._vVector = _vVector;
            image._imageSize = _imageSize;
            image._displaySize = _displaySize;
            image._rotation = _rotation;
            image._brightness = _brightness;
            image._contrast = _contrast;
            image._fade = _fade;
            image._clipBoundary = _clipBoundary?.Clone() as ImageClipBoundary;
            image._showImage = _showImage;
            image._showClipBoundary = _showClipBoundary;
            image._transparency = _transparency;
            return image;
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            _position = new Vector3(_position.X + translation.X, _position.Y + translation.Y, _position.Z);
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            // 旋转位置
            Vector2 pos2d = new Vector2(_position.X, _position.Y);
            Vector2 rotated = Vector2.RotateInRadian(pos2d, center, angle);
            _position = new Vector3(rotated.X, rotated.Y, _position.Z);
            
            // 更新旋转角度
            _rotation += angle;
            UpdateVectors();
        }

        /// <summary>
        /// 变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            // 变换位置
            Vector2 pos2d = new Vector2(_position.X, _position.Y);
            Vector2 transformed = transform * pos2d;
            _position = new Vector3(transformed.X, transformed.Y, _position.Z);
            
            // 变换向量
            Vector2 u2d = new Vector2(_uVector.X, _uVector.Y);
            Vector2 v2d = new Vector2(_vVector.X, _vVector.Y);
            Vector2 transformedU = transform * u2d - transform * Vector2.Zero;
            Vector2 transformedV = transform * v2d - transform * Vector2.Zero;
            
            _uVector = new Vector3(transformedU.X, transformedU.Y, 0);
            _vVector = new Vector3(transformedV.X, transformedV.Y, 0);
            
            // 更新显示尺寸和旋�?
            _displaySize = new Vector2(transformedU.length, transformedV.length);
            _rotation = Math.Atan2(transformedU.Y, transformedU.X);
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            
            Vector2 p1 = new Vector2(_position.X, _position.Y);
            Vector2 p2 = p1 + new Vector2(_uVector.X, _uVector.Y);
            Vector2 p3 = p1 + new Vector2(_uVector.X + _vVector.X, _uVector.Y + _vVector.Y);
            Vector2 p4 = p1 + new Vector2(_vVector.X, _vVector.Y);
            
            // 四个角点
            gripPoints.Add(new GripPoint(GripPointType.Quad, p1));
            gripPoints.Add(new GripPoint(GripPointType.Quad, p2));
            gripPoints.Add(new GripPoint(GripPointType.Quad, p3));
            gripPoints.Add(new GripPoint(GripPointType.Quad, p4));
            
            // 中心�?
            Vector2 center = (p1 + p3) * 0.5;
            gripPoints.Add(new GripPoint(GripPointType.Center, center));
            
            return gripPoints;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            Vector2 p1 = new Vector2(_position.X, _position.Y);
            Vector2 p2 = p1 + new Vector2(_uVector.X, _uVector.Y);
            Vector2 p3 = p1 + new Vector2(_uVector.X + _vVector.X, _uVector.Y + _vVector.Y);
            Vector2 p4 = p1 + new Vector2(_vVector.X, _vVector.Y);
            
            switch (index)
            {
                case 0: // 左下�?
                    {
                        Vector2 delta = newPosition - p1;
                        _position = new Vector3(newPosition.X, newPosition.Y, _position.Z);
                        _uVector = new Vector3(p2.X - newPosition.X, p2.Y - newPosition.Y, 0);
                        _vVector = new Vector3(p4.X - newPosition.X, p4.Y - newPosition.Y, 0);
                    }
                    break;
                case 1: // 右下�?
                    _uVector = new Vector3(newPosition.X - p1.X, newPosition.Y - p1.Y, 0);
                    break;
                case 2: // 右上�?
                    {
                        Vector2 newU = newPosition - p4;
                        Vector2 newV = newPosition - p2;
                        _uVector = new Vector3(newU.X, newU.Y, 0);
                        _vVector = new Vector3(newV.X, newV.Y, 0);
                    }
                    break;
                case 3: // 左上�?
                    _vVector = new Vector3(newPosition.X - p1.X, newPosition.Y - p1.Y, 0);
                    break;
                case 4: // 中心�?
                    {
                        Vector2 currentCenter = (p1 + p3) * 0.5;
                        Vector2 delta = newPosition - currentCenter;
                        Translate(delta);
                    }
                    break;
            }
            
            // 更新显示尺寸和旋�?
            _displaySize = new Vector2(_uVector.length, _vVector.length);
            _rotation = Math.Atan2(_uVector.Y, _uVector.X);
        }

        /// <summary>
        /// 获取捕捉�?
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPoints = new List<ObjectSnapPoint>();
            
            Vector2 p1 = new Vector2(_position.X, _position.Y);
            Vector2 p2 = p1 + new Vector2(_uVector.X, _uVector.Y);
            Vector2 p3 = p1 + new Vector2(_uVector.X + _vVector.X, _uVector.Y + _vVector.Y);
            Vector2 p4 = p1 + new Vector2(_vVector.X, _vVector.Y);
            
            // 四个角点
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, p1));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, p2));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, p3));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, p4));
            
            // 边中�?
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (p1 + p2) * 0.5));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (p2 + p3) * 0.5));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (p3 + p4) * 0.5));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, (p4 + p1) * 0.5));
            
            // 中心�?
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Center, (p1 + p3) * 0.5));
            
            return snapPoints;
        }

        #endregion
    }

    /// <summary>
    /// 图像定义
    /// </summary>
    [Serializable]
    public class ImageDefinition
    {
        private string _filePath = string.Empty;
#if WINDOWS
        private System.Drawing.Image _image;
#endif
        // 跨平台: 图像加载状态与像素尺寸 (Size 属 System.Drawing.Primitives, 平台无关).
        // GDI+ 实际位图仅在 WINDOWS 加载; 跨平台下 Image 实体为 Phase 2 占位.
        private System.Drawing.Size _imageSize;
        private bool _imageLoaded;
        private string _name = string.Empty;

        /// <summary>
        /// 图像是否已加载 (跨平台安全)
        /// </summary>
        public bool ImageLoaded { get { return _imageLoaded; } }

        /// <summary>
        /// 图像像素尺寸 (跨平台安全; 未加载为默认值)
        /// </summary>
        public System.Drawing.Size ImageSize { get { return _imageSize; } }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath
        {
            get { return _filePath; }
        }

#if WINDOWS
        /// <summary>
        /// 图像对象 (WinForms GDI+; 跨平台未加载, Image 实体为 Phase 2 占位)
        /// </summary>
        public System.Drawing.Image Image
        {
            get { return _image; }
        }
#endif

        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value ?? string.Empty; }
        }

        /// <summary>
        /// 构造函�?
        /// </summary>
        public ImageDefinition(string filePath)
        {
            _filePath = filePath ?? string.Empty;
            _name = Path.GetFileNameWithoutExtension(_filePath);
#if WINDOWS
            LoadImage();
#endif
        }

#if WINDOWS
        /// <summary>
        /// 加载图像 (WinForms GDI+)
        /// </summary>
        private void LoadImage()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    _image = System.Drawing.Image.FromFile(_filePath);
                    _imageSize = _image.Size;
                    _imageLoaded = true;
                }
            }
            catch
            {
                // 忽略加载错误
                _image = null;
                _imageLoaded = false;
            }
        }
#endif
    }

    /// <summary>
    /// 图像裁剪边界
    /// </summary>
    [Serializable]
    public class ImageClipBoundary : ICloneable
    {
        private ImageClipBoundaryType _type = ImageClipBoundaryType.Rectangular;
        private List<Vector2> _vertices = new List<Vector2>();

        /// <summary>
        /// 裁剪边界类型
        /// </summary>
        public ImageClipBoundaryType Type
        {
            get { return _type; }
            set { _type = value; }
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
            ImageClipBoundary boundary = new ImageClipBoundary();
            boundary._type = _type;
            boundary._vertices = new List<Vector2>(_vertices);
            return boundary;
        }
    }

    /// <summary>
    /// 图像裁剪边界类型
    /// </summary>
    public enum ImageClipBoundaryType
    {
        /// <summary>
        /// 矩形
        /// </summary>
        Rectangular,
        
        /// <summary>
        /// 多边�?
        /// </summary>
        Polygonal
    }
}