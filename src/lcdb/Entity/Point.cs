using System;
using System.Collections.Generic;
using LitMath;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 标准点实�?
    /// 原生OtoCAD实现，不依赖外部�?
    /// </summary>
    [Serializable]
    public class Point : Entity
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className
        {
            get { return "Point"; }
        }

        /// <summary>
        /// 点位�?
        /// </summary>
        private Vector2 _position = new Vector2();
        public Vector2 position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// X坐标
        /// </summary>
        public double x
        {
            get { return _position.X; }
            set { _position.X = value; }
        }

        /// <summary>
        /// Y坐标
        /// </summary>
        public double y
        {
            get { return _position.Y; }
            set { _position.Y = value; }
        }

        /// <summary>
        /// 点的厚度
        /// </summary>
        public double thickness { get; set; } = 0.0;

        /// <summary>
        /// 点的显示角度（弧度）
        /// </summary>
        public double angle { get; set; } = 0.0;

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                // 点的边框是自身位置加上小的容�?
                double tolerance = 0.001;
                return new Bounding(
                    new Vector2(_position.X - tolerance, _position.Y - tolerance),
                    new Vector2(_position.X + tolerance, _position.Y + tolerance)
                );
            }
        }

        /// <summary>
        /// 构造函�?
        /// </summary>
        public Point()
        {
        }

        /// <summary>
        /// 构造函�?- 指定位置
        /// </summary>
        public Point(Vector2 pos)
        {
            _position = pos;
        }

        /// <summary>
        /// 构造函�?- 指定坐标
        /// </summary>
        public Point(double x, double y)
        {
            _position = new Vector2(x, y);
        }

        /// <summary>
        /// 构造函�?- 指定坐标和角�?
        /// </summary>
        public Point(double x, double y, double angle)
        {
            _position = new Vector2(x, y);
            this.angle = angle;
        }

        /// <summary>
        /// 到另一点的距离
        /// </summary>
        public double DistanceTo(Point other)
        {
            if (other == null) return double.MaxValue;
            return (_position - other._position).length;
        }

        /// <summary>
        /// 到指定位置的距离
        /// </summary>
        public double DistanceTo(Vector2 point)
        {
            return (_position - point).length;
        }

        /// <summary>
        /// 获取到指定点的方向向量（单位向量�?
        /// </summary>
        public Vector2 DirectionTo(Point other)
        {
            if (other == null) return new Vector2(1, 0);
            Vector2 dir = other._position - _position;
            return dir.normalized;
        }

        /// <summary>
        /// 获取到指定位置的方向向量（单位向量）
        /// </summary>
        public Vector2 DirectionTo(Vector2 point)
        {
            Vector2 dir = point - _position;
            return dir.normalized;
        }

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            gd.DrawPoint(_position);
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Point point = base.Clone() as Point;
            point._position = _position;
            point.thickness = thickness;
            point.angle = angle;
            return point;
        }

        protected override DBObject CreateInstance()
        {
            return new Point();
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            _position += translation;
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double rotationAngle)
        {
            _position = Vector2.RotateInRadian(_position, center, rotationAngle);
            angle += rotationAngle;
        }

        /// <summary>
        /// 变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            _position = transform * _position;
        }

        /// <summary>
        /// 对象捕捉�?
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPnts = new List<ObjectSnapPoint>();
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.End, _position));
            return snapPnts;
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPnts = new List<GripPoint>();
            gripPnts.Add(new GripPoint(GripPointType.End, _position));
            return gripPnts;
        }

        /// <summary>
        /// 设置夹点位置
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            if (index == 0)
            {
                _position = newPosition;
            }
        }

        /// <summary>
        /// 判断点是否在指定位置附近
        /// </summary>
        public bool IsNear(Vector2 point, double tolerance = 1e-6)
        {
            return (_position - point).length <= tolerance;
        }

        /// <summary>
        /// 判断点是否在指定位置附近
        /// </summary>
        public bool IsNear(Point other, double tolerance = 1e-6)
        {
            if (other == null) return false;
            return (_position - other._position).length <= tolerance;
        }

        /// <summary>
        /// 获取最近点（总是返回点自身）
        /// </summary>
        public Vector2 GetClosestPoint(Vector2 point)
        {
            return _position;
        }

        /// <summary>
        /// 相等性比�?
        /// </summary>
        public bool Equals(Point other, double tolerance = 1e-6)
        {
            if (other == null) return false;
            return IsNear(other, tolerance);
        }

        /// <summary>
        /// 重写相等性比�?
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as Point);
        }

        /// <summary>
        /// 重写哈希�?
        /// </summary>
        public override int GetHashCode()
        {
            return _position.GetHashCode();
        }

        /// <summary>
        /// 字符串表�?
        /// </summary>
        public override string ToString()
        {
            return string.Format("Point: ({0:F3}, {1:F3})", _position.X, _position.Y);
        }

        /// <summary>
        /// 运算符重�?- 相等
        /// </summary>
        public static bool operator ==(Point left, Point right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        /// <summary>
        /// 运算符重�?- 不等
        /// </summary>
        public static bool operator !=(Point left, Point right)
        {
            return !(left == right);
        }

        /// <summary>
        /// 隐式转换到Vector2
        /// </summary>
        public static implicit operator Vector2(Point point)
        {
            return point?._position ?? Vector2.Zero;
        }

        /// <summary>
        /// 隐式转换从Vector2
        /// </summary>
        public static implicit operator Point(Vector2 vector)
        {
            return new Point(vector);
        }

        /// <summary>
        /// 点的加法（位移）
        /// </summary>
        public static Point operator +(Point point, Vector2 offset)
        {
            if (point == null) return new Point(offset);
            return new Point(point._position + offset);
        }

        /// <summary>
        /// 点的减法（位移）
        /// </summary>
        public static Point operator -(Point point, Vector2 offset)
        {
            if (point == null) return new Point(-offset);
            return new Point(point._position - offset);
        }

        /// <summary>
        /// 两点之间的向�?
        /// </summary>
        public static Vector2 operator -(Point point1, Point point2)
        {
            if (point1 == null || point2 == null) return Vector2.Zero;
            return point1._position - point2._position;
        }
    }
}