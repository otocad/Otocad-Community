using System;
using LitMath;

namespace lcdb
{
    /// <summary>
    /// 边界框
    /// </summary>
    public class BoundingBox
    {
        /// <summary>
        /// 最小点
        /// </summary>
        public Vector2 Min { get; set; }

        /// <summary>
        /// 最大点
        /// </summary>
        public Vector2 Max { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        public double Width => Max.X - Min.X;

        /// <summary>
        /// 高度
        /// </summary>
        public double Height => Max.Y - Min.Y;

        /// <summary>
        /// 中心点
        /// </summary>
        public Vector2 Center => new Vector2((Min.X + Max.X) / 2, (Min.Y + Max.Y) / 2);

        /// <summary>
        /// 构造函数
        /// </summary>
        public BoundingBox()
        {
            Min = new Vector2(double.MaxValue, double.MaxValue);
            Max = new Vector2(double.MinValue, double.MinValue);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public BoundingBox(Vector2 min, Vector2 max)
        {
            Min = min;
            Max = max;
        }

        /// <summary>
        /// 扩展边界框以包含点
        /// </summary>
        public void Extend(Vector2 point)
        {
            Min = new Vector2(System.Math.Min(Min.X, point.X), System.Math.Min(Min.Y, point.Y));
            Max = new Vector2(System.Math.Max(Max.X, point.X), System.Math.Max(Max.Y, point.Y));
        }

        /// <summary>
        /// 扩展边界框以包含另一个边界框
        /// </summary>
        public void Extend(BoundingBox other)
        {
            Extend(other.Min);
            Extend(other.Max);
        }

        /// <summary>
        /// 判断点是否在边界框内
        /// </summary>
        public bool Contains(Vector2 point)
        {
            return point.X >= Min.X && point.X <= Max.X &&
                   point.Y >= Min.Y && point.Y <= Max.Y;
        }

        /// <summary>
        /// 判断边界框是否相交
        /// </summary>
        public bool Intersects(BoundingBox other)
        {
            return Min.X <= other.Max.X && Max.X >= other.Min.X &&
                   Min.Y <= other.Max.Y && Max.Y >= other.Min.Y;
        }

        /// <summary>
        /// 获取膨胀后的边界框
        /// </summary>
        public BoundingBox Inflate(double amount)
        {
            return new BoundingBox(
                new Vector2(Min.X - amount, Min.Y - amount),
                new Vector2(Max.X + amount, Max.Y + amount)
            );
        }

        /// <summary>
        /// 变换边界框
        /// </summary>
        public BoundingBox Transform(Matrix3 matrix)
        {
            var corners = new[]
            {
                matrix * Min,
                matrix * new Vector2(Max.X, Min.Y),
                matrix * Max,
                matrix * new Vector2(Min.X, Max.Y)
            };

            var result = new BoundingBox();
            foreach (var corner in corners)
            {
                result.Extend(corner);
            }
            return result;
        }
    }
}