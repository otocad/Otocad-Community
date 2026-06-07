using System;

namespace LitMath
{
    public struct Circle2
    {
        public Vector2 center;
        public double radius;

        public double diameter
        {
            get { return radius * 2; }
        }

        public Circle2(Vector2 center, double radius)
        {
            this.center = center;
            this.radius = radius;
        }

        public override string ToString()
        {
            return string.Format("Circle2(({0}, {1}), {2})",
                this.center.X, this.center.Y, this.radius);
        }

        /// <summary>
        /// 求通过3点的圆
        /// http://blog.csdn.net/liyuanbhu/article/details/52891868
        /// </summary>
        public static Circle2 From3Points(Vector2 pnt1, Vector2 pnt2, Vector2 pnt3)
        {
            Circle2 circle = new Circle2();

            double a = pnt1.X - pnt2.X;
            double b = pnt1.Y - pnt2.Y;
            double c = pnt1.X - pnt3.X;
            double d = pnt1.Y - pnt3.Y;
            double e = (pnt1.X * pnt1.X - pnt2.X * pnt2.X + pnt1.Y * pnt1.Y - pnt2.Y * pnt2.Y) / 2.0;
            double f = (pnt1.X * pnt1.X - pnt3.X * pnt3.X + pnt1.Y * pnt1.Y - pnt3.Y * pnt3.Y) / 2.0;
            double det = b * c - a * d;

            if (Math.Abs(det) < Utils.EPSILON)
            {
                circle.center = new Vector2(0, 0);
                circle.radius = -1;
            }

            circle.center.X = -(d * e - b * f) / det;
            circle.center.Y = -(a * f - c * e) / det;
            circle.radius = Math.Sqrt(
                (pnt1.X - circle.center.X) * (pnt1.X - circle.center.X)
                + (pnt1.Y - circle.center.Y) * (pnt1.Y - circle.center.Y));

            return circle;
        }
    }
}
