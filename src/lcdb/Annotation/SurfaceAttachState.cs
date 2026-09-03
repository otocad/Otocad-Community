using System;
using LitMath;

namespace lcdb.Annotation
{
    /// <summary>
    /// 贴面标记的"宿主曲面"约束 (球面/平面): 记住所贴表面的顶点/半径/玻璃侧,
    /// 拖动标记时把光标投影回曲面 → 锚点沿弯月面滑动、朝向随法线, 不脱面。
    /// 顶点为绝对坐标 (随标记平移一起更新)。半径 ∞/极大 = 平面。
    /// </summary>
    public sealed class SurfaceAttachState
    {
        /// <summary>表面顶点 (绝对坐标, 落在光轴上)。</summary>
        public Vector2 Apex { get; set; }
        /// <summary>球面半径 (∞/极大 = 平面)。</summary>
        public double Radius { get; set; } = double.PositiveInfinity;
        /// <summary>玻璃在该面 +X 侧 (外法线朝 -X)。</summary>
        public bool GlassOnPlusX { get; set; }

        /// <summary>标记相对顶点的轴向高度 (吸附点 Y − Apex.Y)。拖动标记沿面滑时更新, 关联重锚据此保持高度。</summary>
        public double Height { get; set; }

        public SurfaceAttachState() { }
        public SurfaceAttachState(Vector2 apex, double radius, bool glassOnPlusX)
        {
            Apex = apex; Radius = radius; GlassOnPlusX = glassOnPlusX;
        }

        /// <summary>把光标 (按其 Y 取面上同高度点) 投影到曲面: 返回面上吸附点 + 外法线。</summary>
        public (Vector2 point, Vector2 normal) Project(double cursorY)
        {
            double X(double t) => Apex.X + Optic.OpticSurfaceGeometry.ComputeSag(Radius, Math.Abs(t));
            double h = cursorY - Apex.Y;
            var pt = new Vector2(X(h), Apex.Y + h);
            const double d = 0.05;
            double slope = (X(h + d) - X(h - d)) / (2 * d);    // dX/dh (含符号)
            var nPlusX = new Vector2(1, -slope).normalized;
            var outward = GlassOnPlusX ? -nPlusX : nPlusX;
            return (pt, outward);
        }

        public SurfaceAttachState Clone() => new(Apex, Radius, GlassOnPlusX);
    }
}
