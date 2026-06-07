using System;
using LitMath;
using netDxf;
using netDxf.Tables;
using System.Drawing;
using lcdb;

namespace lcdb.NetDxfAdapter
{
    /// <summary>
    /// 坐标系统转换器，处理OtoCAD和netDxf之间的坐标转换
    /// </summary>
    public static class CoordinateConverter
    {
        #region 向量转换

        /// <summary>
        /// 将LitMath.Vector2转换为netDxf.Vector2
        /// </summary>
        public static netDxf.Vector2 ToNetDxfVector2(LitMath.Vector2 vector)
        {
            return new netDxf.Vector2(vector.X, vector.Y);
        }

        /// <summary>
        /// 将netDxf.Vector2转换为LitMath.Vector2
        /// </summary>
        public static LitMath.Vector2 ToLitMathVector2(netDxf.Vector2 vector)
        {
            return new LitMath.Vector2(vector.X, vector.Y);
        }

        /// <summary>
        /// 将LitMath.Vector2转换为netDxf.Vector3 (Z=0)
        /// </summary>
        public static netDxf.Vector3 ToNetDxfVector3(LitMath.Vector2 vector)
        {
            return new netDxf.Vector3(vector.X, vector.Y, 0.0);
        }

        /// <summary>
        /// 将netDxf.Vector3转换为LitMath.Vector2 (忽略Z)
        /// </summary>
        public static LitMath.Vector2 ToLitMathVector2(netDxf.Vector3 vector)
        {
            return new LitMath.Vector2(vector.X, vector.Y);
        }

        #endregion

        #region 矩阵转换

        /// <summary>
        /// 将LitMath.Matrix3转换为netDxf.Matrix3
        /// </summary>
        public static netDxf.Matrix3 ToNetDxfMatrix3(LitMath.Matrix3 matrix)
        {
            return new netDxf.Matrix3(
                matrix.m11, matrix.m12, matrix.m13,
                matrix.m21, matrix.m22, matrix.m23,
                matrix.m31, matrix.m32, matrix.m33
            );
        }

        /// <summary>
        /// 将netDxf.Matrix3转换为LitMath.Matrix3
        /// </summary>
        public static LitMath.Matrix3 ToLitMathMatrix3(netDxf.Matrix3 matrix)
        {
            return new LitMath.Matrix3(
                matrix.M11, matrix.M12, matrix.M13,
                matrix.M21, matrix.M22, matrix.M23,
                matrix.M31, matrix.M32, matrix.M33
            );
        }

        #endregion

        #region 颜色转换

        /// <summary>
        /// 将lcdb.Colors.Color转换为netDxf.AciColor
        /// </summary>
        public static AciColor ToNetDxfColor(lcdb.Colors.Color color)
        {
            if (color.colorMethod == lcdb.Colors.ColorMethod.ByLayer)
                return AciColor.ByLayer;
            
            return new AciColor(color.r, color.g, color.b);
        }

        /// <summary>
        /// 将netDxf.AciColor转换为lcdb.Colors.Color
        /// </summary>
        public static lcdb.Colors.Color ToOtoCADColor(AciColor aciColor)
        {
            if (aciColor == null || aciColor.IsByLayer)
                return lcdb.Colors.Color.ByLayer;
            
            return lcdb.Colors.Color.FromRGB(aciColor.R, aciColor.G, aciColor.B);
        }

        #endregion

        #region 角度转换

        /// <summary>
        /// 将弧度转换为角度
        /// </summary>
        public static double RadiansToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        /// <summary>
        /// 将角度转换为弧度
        /// </summary>
        public static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        #endregion

        #region 边界转换

        /// <summary>
        /// 将netDxf.BoundingRectangle转换为OtoCAD.Bounding
        /// </summary>
        public static Bounding ToOtoCADBounding(netDxf.BoundingRectangle boundingRect)
        {
            return new Bounding(
                boundingRect.Min.X, boundingRect.Min.Y,
                boundingRect.Max.X, boundingRect.Max.Y
            );
        }

        /// <summary>
        /// 将OtoCAD.Bounding转换为netDxf.BoundingRectangle
        /// </summary>
        public static netDxf.BoundingRectangle ToNetDxfBoundingRectangle(Bounding bounding)
        {
            return new netDxf.BoundingRectangle(
                new netDxf.Vector2(bounding.left, bounding.bottom),
                new netDxf.Vector2(bounding.right, bounding.top)
            );
        }

        #endregion

        #region 变换操作

        /// <summary>
        /// 创建平移变换矩阵
        /// </summary>
        public static netDxf.Vector3 CreateTranslationMatrix(LitMath.Vector2 translation)
        {
            return new netDxf.Vector3(translation.X, translation.Y, 0.0);
        }

        /// <summary>
        /// 创建旋转变换矩阵
        /// </summary>
        public static netDxf.Matrix3 CreateRotationMatrix(LitMath.Vector2 center, double angle)
        {
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);
            
            // 创建绕指定中心点的旋转矩阵
            // 公式: T(center) * R(angle) * T(-center)
            return new netDxf.Matrix3(
                cos, -sin, center.X - center.X * cos + center.Y * sin,
                sin, cos, center.Y - center.X * sin - center.Y * cos,
                0, 0, 1
            );
        }

        /// <summary>
        /// 创建缩放变换矩阵
        /// </summary>
        public static netDxf.Matrix3 CreateScaleMatrix(LitMath.Vector2 center, double scale)
        {
            // 创建绕指定中心点的缩放矩阵
            // 公式: T(center) * S(scale) * T(-center)
            return new netDxf.Matrix3(
                scale, 0, center.X * (1 - scale),
                0, scale, center.Y * (1 - scale),
                0, 0, 1
            );
        }

        /// <summary>
        /// 创建镜像变换矩阵
        /// </summary>
        public static netDxf.Matrix3 CreateMirrorMatrix(LitMath.Vector2 p1, LitMath.Vector2 p2)
        {
            // 计算镜像轴的方向向量
            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            
            if (IsEqual(length, 0.0))
                return netDxf.Matrix3.Identity;
            
            // 标准化方向向量
            dx /= length;
            dy /= length;
            
            // 计算镜像矩阵
            var a = dx * dx - dy * dy;
            var b = 2 * dx * dy;
            var tx = 2 * (p1.X * dy * dy - p1.Y * dx * dy);
            var ty = 2 * (p1.Y * dx * dx - p1.X * dx * dy);
            
            return new netDxf.Matrix3(
                a, b, tx,
                b, -a, ty,
                0, 0, 1
            );
        }


        #endregion

        #region 精度控制

        /// <summary>
        /// 浮点数精度比较
        /// </summary>
        public static bool IsEqual(double a, double b, double epsilon = 1e-10)
        {
            return Math.Abs(a - b) < epsilon;
        }

        /// <summary>
        /// 向量精度比较
        /// </summary>
        public static bool IsEqual(LitMath.Vector2 a, LitMath.Vector2 b, double epsilon = 1e-10)
        {
            return IsEqual(a.X, b.X, epsilon) && IsEqual(a.Y, b.Y, epsilon);
        }

        #endregion
    }
}