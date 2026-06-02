using System;
using System.Collections.Generic;
using System.Text;

namespace LitMath
{
    public class Utils
    {
        public const double EPSILON = 1E-05;
        public const double PI = Math.PI; //3.14159265358979323846;
        public const double TwoPI = Math.PI * 2;
        public const double HalfPI = Math.PI * 0.5;

        public static double Clamp(double value, double minv, double maxv)
        {
            return Math.Max(Math.Min(value, maxv), minv);
        }

        public static double DegreeToRadian(double angle)
        {
            return ((angle * PI) / 180.0);
        }

        public static bool IsEqual(double x, double y, double epsilon = EPSILON)
        {
            return IsEqualZero(x - y, epsilon);
        }

        public static bool IsEqualZero(double x, double epsilon = EPSILON)
        {
            return (Math.Abs(x) < epsilon);
        }

        public static double RadianToDegree(double angle)
        {
            return ((angle * 180.0) / PI);
        }

        /// <summary>
        /// Normalizes angle to [0, 2π).
        /// </summary>
        public static double NormalizeAngle(double angle)
        {
            double a = angle % TwoPI;
            if (a < 0) a += TwoPI;
            return a;
        }

        /// <summary>
        /// Perpendicular distance from point p to the infinite line through origin with direction dir.
        /// </summary>
        public static double PointLineDistance(Vector2 p, Vector2 origin, Vector2 dir)
        {
            Vector2 d = dir.normalized;
            Vector2 op = p - origin;
            return Math.Abs(op.X * d.Y - op.Y * d.X);
        }

        /// <summary>
        /// Intersection of two infinite lines defined by (point, direction) pairs.
        /// Returns Vector2 with isvalid=false if the lines are parallel within EPSILON.
        /// </summary>
        public static Vector2 FindIntersection(Vector2 p0, Vector2 dir0, Vector2 p1, Vector2 dir1)
        {
            double cross = Vector2.Cross(dir0, dir1);
            if (Math.Abs(cross) < EPSILON)
                return new Vector2(0, 0, false);

            Vector2 d = p1 - p0;
            double t = (d.X * dir1.Y - d.Y * dir1.X) / cross;
            return p0 + dir0 * t;
        }
    }
}
