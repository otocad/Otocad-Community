using System;
using System.Collections.Generic;
using LitMath;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 形状实体
    /// 表示从SHX文件中定义的形状符号
    /// </summary>
    public class Shape : Entity
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className
        {
            get { return "Shape"; }
        }

        /// <summary>
        /// 形状名称
        /// </summary>
        private string _name = string.Empty;

        /// <summary>
        /// 形状样式ID
        /// </summary>
        private ObjectId _shapeStyleId = ObjectId.Null;

        /// <summary>
        /// 插入�?
        /// </summary>
        private Vector3 _position = new Vector3();

        /// <summary>
        /// 大小（缩放因子）
        /// </summary>
        private double _size = 1.0;

        /// <summary>
        /// 旋转角度（弧度）
        /// </summary>
        private double _rotation = 0.0;

        /// <summary>
        /// 倾斜角度（弧度）
        /// </summary>
        private double _obliqueAngle = 0.0;

        /// <summary>
        /// 宽度因子
        /// </summary>
        private double _widthFactor = 1.0;

        /// <summary>
        /// 厚度
        /// </summary>
        private double _thickness = 0.0;

        /// <summary>
        /// 获取或设置形状名�?
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value ?? string.Empty; }
        }

        /// <summary>
        /// 获取或设置形状样式ID
        /// </summary>
        public ObjectId ShapeStyleId
        {
            get { return _shapeStyleId; }
            set { _shapeStyleId = value; }
        }

        /// <summary>
        /// 获取或设置形状样式名�?
        /// </summary>
        public string ShapeStyle
        {
            get
            {
                // TODO: 从数据库获取样式名称
                return "";
            }
            set
            {
                // TODO: 从数据库查找样式ID
            }
        }

        /// <summary>
        /// 获取或设置插入点
        /// </summary>
        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// 获取或设置大小（必须大于0�?
        /// </summary>
        public double Size
        {
            get { return _size; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Size must be greater than 0");
                _size = value;
            }
        }

        /// <summary>
        /// 获取或设置旋转角度（弧度�?
        /// </summary>
        public double Rotation
        {
            get { return _rotation; }
            set { _rotation = value; }
        }

        /// <summary>
        /// 获取或设置倾斜角度（弧度，-85°�?5°�?
        /// </summary>
        public double ObliqueAngle
        {
            get { return _obliqueAngle; }
            set
            {
                double degrees = value * 180.0 / Math.PI;
                if (degrees < -85 || degrees > 85)
                    throw new ArgumentException("ObliqueAngle must be between -85 and 85 degrees");
                _obliqueAngle = value;
            }
        }

        /// <summary>
        /// 获取或设置宽度因子（不能�?�?
        /// </summary>
        public double WidthFactor
        {
            get { return _widthFactor; }
            set
            {
                if (value == 0)
                    throw new ArgumentException("WidthFactor cannot be 0");
                _widthFactor = value;
            }
        }

        /// <summary>
        /// 获取或设置厚�?
        /// </summary>
        public double Thickness
        {
            get { return _thickness; }
            set { _thickness = value; }
        }

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                // 由于形状的实际几何存储在SHX文件中，
                // 这里返回一个基于位置和大小的估计边�?
                double halfSize = Math.Abs(_size) / 2.0;
                Vector2 min = new Vector2(_position.X - halfSize, _position.Y - halfSize);
                Vector2 max = new Vector2(_position.X + halfSize, _position.Y + halfSize);
                return new Bounding(min, max);
            }
        }

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            // 由于OtoCAD不支持SHX文件，绘制一个占位符
            // 绘制一个带叉的方框表示形状位置
            
            Vector2 pos2D = new Vector2(_position.X, _position.Y);
            double halfSize = _size / 2.0;
            
            // 应用旋转变换
            double cos = Math.Cos(_rotation);
            double sin = Math.Sin(_rotation);
            
            // 方框的四个角点（相对于原点）
            Vector2[] corners = new Vector2[]
            {
                new Vector2(-halfSize, -halfSize),
                new Vector2(halfSize, -halfSize),
                new Vector2(halfSize, halfSize),
                new Vector2(-halfSize, halfSize)
            };
            
            // 应用宽度因子和倾斜
            for (int i = 0; i < corners.Length; i++)
            {
                double x = corners[i].X * _widthFactor;
                double y = corners[i].Y;
                
                // 应用倾斜
                if (_obliqueAngle != 0)
                {
                    x += y * Math.Tan(_obliqueAngle);
                }
                
                corners[i] = new Vector2(x, y);
            }
            
            // 旋转并平移到位置
            for (int i = 0; i < corners.Length; i++)
            {
                double x = corners[i].X * cos - corners[i].Y * sin;
                double y = corners[i].X * sin + corners[i].Y * cos;
                corners[i] = pos2D + new Vector2(x, y);
            }
            
            // 绘制方框
            gd.DrawLine(corners[0], corners[1]);
            gd.DrawLine(corners[1], corners[2]);
            gd.DrawLine(corners[2], corners[3]);
            gd.DrawLine(corners[3], corners[0]);
            
            // 绘制对角�?
            gd.DrawLine(corners[0], corners[2]);
            gd.DrawLine(corners[1], corners[3]);
            
            // 如果有名称，在中心绘制文�?
            if (!string.IsNullOrEmpty(_name))
            {
                // TODO: 绘制形状名称
            }
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            _position = new Vector3(_position.X + translation.X, 
                                   _position.Y + translation.Y, 
                                   _position.Z);
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            // 旋转位置
            Vector2 pos2D = new Vector2(_position.X, _position.Y);
            Vector2 relativePos = pos2D - center;
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            double newX = relativePos.X * cos - relativePos.Y * sin;
            double newY = relativePos.X * sin + relativePos.Y * cos;
            _position = new Vector3(center.X + newX, center.Y + newY, _position.Z);
            
            // 累加旋转角度
            _rotation += angle;
        }

        /// <summary>
        /// 变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            // 变换位置
            Vector2 pos2D = new Vector2(_position.X, _position.Y);
            Vector2 transformedPos = transform * pos2D;
            _position = new Vector3(transformedPos.X, transformedPos.Y, _position.Z);
            
            // 变换大小（取X和Y缩放的平均值）
            Vector2 scaleX = transform * new Vector2(1, 0) - transform * new Vector2(0, 0);
            Vector2 scaleY = transform * new Vector2(0, 1) - transform * new Vector2(0, 0);
            double avgScale = (scaleX.Modulus() + scaleY.Modulus()) / 2.0;
            _size *= avgScale;
            
            // 变换旋转角度
            double rotation = Math.Atan2(scaleX.Y, scaleX.X);
            _rotation += rotation;
            
            // 变换宽度因子
            _widthFactor *= scaleX.Modulus() / scaleY.Modulus();
        }

        /// <summary>
        /// 对象捕捉�?
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPnts = new List<ObjectSnapPoint>();
            
            // 插入�?
            Vector2 insertPoint = new Vector2(_position.X, _position.Y);
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Node, insertPoint));
            
            // 边界框的角点和中�?
            double halfSize = _size / 2.0;
            Vector2[] corners = new Vector2[]
            {
                new Vector2(-halfSize, -halfSize),
                new Vector2(halfSize, -halfSize),
                new Vector2(halfSize, halfSize),
                new Vector2(-halfSize, halfSize)
            };
            
            // 应用变换
            double cos = Math.Cos(_rotation);
            double sin = Math.Sin(_rotation);
            
            for (int i = 0; i < corners.Length; i++)
            {
                double x = corners[i].X * _widthFactor;
                double y = corners[i].Y;
                
                if (_obliqueAngle != 0)
                {
                    x += y * Math.Tan(_obliqueAngle);
                }
                
                double rotX = x * cos - y * sin;
                double rotY = x * sin + y * cos;
                Vector2 cornerPoint = insertPoint + new Vector2(rotX, rotY);
                
                snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.End, cornerPoint));
            }
            
            return snapPnts;
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPnts = new List<GripPoint>();
            
            // 插入点夹�?
            Vector2 insertPoint = new Vector2(_position.X, _position.Y);
            gripPnts.Add(new GripPoint(GripPointType.Center, insertPoint));
            
            // 尺寸控制夹点（右上角�?
            double halfSize = _size / 2.0;
            double cos = Math.Cos(_rotation);
            double sin = Math.Sin(_rotation);
            
            double x = halfSize * _widthFactor;
            double y = halfSize;
            if (_obliqueAngle != 0)
            {
                x += y * Math.Tan(_obliqueAngle);
            }
            
            double rotX = x * cos - y * sin;
            double rotY = x * sin + y * cos;
            Vector2 sizeGrip = insertPoint + new Vector2(rotX, rotY);
            
            GripPoint sizeGripPoint = new GripPoint(GripPointType.End, sizeGrip);
            sizeGripPoint.xData1 = "size";
            gripPnts.Add(sizeGripPoint);
            
            return gripPnts;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            if (index == 0) // 插入�?
            {
                Vector2 delta = newPosition - new Vector2(_position.X, _position.Y);
                Translate(delta);
            }
            else if (index == 1 && gripPoint.xData1 as string == "size") // 尺寸夹点
            {
                Vector2 insertPoint = new Vector2(_position.X, _position.Y);
                Vector2 delta = newPosition - insertPoint;
                double newSize = delta.Modulus() * 2.0;
                if (newSize > 0)
                {
                    _size = newSize;
                }
            }
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Shape shape = base.Clone() as Shape;
            shape._name = _name;
            shape._shapeStyleId = _shapeStyleId;
            shape._position = _position;
            shape._size = _size;
            shape._rotation = _rotation;
            shape._obliqueAngle = _obliqueAngle;
            shape._widthFactor = _widthFactor;
            shape._thickness = _thickness;
            return shape;
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new Shape();
        }

        /// <summary>
        /// 写XML
        /// </summary>

        /// <summary>
        /// 读XML
        /// </summary>
    }
}