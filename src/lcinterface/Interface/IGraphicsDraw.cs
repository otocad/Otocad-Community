using System;
using System.Drawing;

namespace OtoCAD
{
    public interface IGraphicsDraw
    {
        /// <summary>
        /// 当前渲染模式(AD-1, Story 10-1)。
        /// 只读,由 Presenter 在构造 IGraphicsDraw 实现时注入。
        /// Entity.Draw 实现可基于此短路颜色逻辑(参考 CoatingMark Monochrome 黑色覆盖)。
        /// </summary>
        RenderMode CurrentMode { get; }

        /// <summary>
        /// 当前线型 (GB/T 13323-2009 §2.1: 光轴用双点画线 <see cref="lcdb.LineType.DashDotDot"/>,
        /// 中心线用单点画线 <see cref="lcdb.LineType.DashDot"/>).
        ///
        /// Entity.Draw 实现应在画线前后保存/恢复此值, 例如:
        /// <code>
        /// var prev = gd.CurrentLineType;
        /// gd.CurrentLineType = this.lineType;
        /// gd.DrawLine(a, b);
        /// gd.CurrentLineType = prev;
        /// </code>
        /// 默认实现 (老 WinForms CanvasDraw / WorldDraw) 返回 Solid 且忽略 set —
        /// 老画布不渲染虚线; 仅 SkiaGraphicsDraw (Avalonia 新画布) 提供完整支持.
        /// </summary>
        lcdb.LineType CurrentLineType
        {
            get => lcdb.LineType.Solid;
            set { /* default no-op for legacy implementations */ }
        }

        /// <summary>
        /// 当前绘制颜色. 与 <see cref="CurrentLineType"/> 同样的扩展约定:
        /// Entity.Draw 可在画图元前后保存/恢复, 仅当自身 color 是显式 RGB
        /// (ByColor / ByEntity) 时才覆盖, 否则沿用外部 (Presenter / 画布) 设定的默认色:
        /// <code>
        /// var prev = gd.CurrentColor;
        /// if (color.colorMethod is ByColor or ByEntity) gd.CurrentColor = color.ToDrawingColor();
        /// gd.DrawLine(a, b);
        /// gd.CurrentColor = prev;
        /// </code>
        /// 用 System.Drawing.Color 而非 lcdb.Colors.Color — lcinterface 不引用 lcdb (循环引用).
        /// 默认实现 (老 WinForms CanvasDraw / WorldDraw) get 返回 default、set 忽略 —
        /// 老画布颜色仍由 Presenter 按实体设 GDI 笔, 此扩展只服务新 SkiaGraphicsDraw.
        /// </summary>
        System.Drawing.Color CurrentColor
        {
            get => default;
            set { /* default no-op for legacy implementations */ }
        }

        /// <summary>
        /// 用当前颜色填充一个闭合多边形 (实心). 供透镜剖面等需要 "玻璃底色" 的图元.
        /// 默认实现 (老画布) 为 no-op — 仅 SkiaGraphicsDraw 支持.
        /// </summary>
        void DrawFilledPolygon(System.Collections.Generic.IReadOnlyList<LitMath.Vector2> points) { /* default no-op */ }

        void DrawPoint(LitMath.Vector2 endPoint);

        void DrawLine(LitMath.Vector2 startPoint, LitMath.Vector2 endPoint);
        void DrawLineDimension(LitMath.Vector2 startPoint, LitMath.Vector2 endPoint);
        void DrawXLine(LitMath.Vector2 basePoint, LitMath.Vector2 direction);

        void DrawRay(LitMath.Vector2 basePoint, LitMath.Vector2 direction);

        void DrawCircle(LitMath.Vector2 center, double radius);

        void DrawEllipse(LitMath.Vector2 center, double radiusX, double radiusY);

        /// <summary>
        /// 绘制圆弧
        /// 以逆时针方式绘制
        /// </summary>
        /// <param name="center">圆弧中心</param>
        /// <param name="radius">圆弧半径</param>
        /// <param name="startAngle">起始角度(弧度)</param>
        /// <param name="endAngle">结束角度(弧度)</param>
        void DrawArc(LitMath.Vector2 center, double radius, double startAngle, double endAngle);

        void DrawRectangle(LitMath.Vector2 position, double width, double height);

        /// <summary>
        /// 绘制实体三角形
        /// </summary>
        /// <param name="vertex1">第一个顶点</param>
        /// <param name="vertex2">第二个顶点</param>
        /// <param name="vertex3">第三个顶点</param>
        void DrawTriangle(LitMath.Vector2 vertex1, LitMath.Vector2 vertex2, LitMath.Vector2 vertex3);

        /// <summary>
        /// 绘制实体四边形
        /// </summary>
        /// <param name="vertex1">第一个顶点</param>
        /// <param name="vertex2">第二个顶点</param>
        /// <param name="vertex3">第三个顶点</param>
        /// <param name="vertex4">第四个顶点</param>
        void DrawQuadrilateral(LitMath.Vector2 vertex1, LitMath.Vector2 vertex2, LitMath.Vector2 vertex3, LitMath.Vector2 vertex4);

        LitMath.Vector2 DrawText(LitMath.Vector2 position, string text, double height, string font, lcdb.TextAlignment textAlign, double angle);
        LitMath.Vector2 DrawText(LitMath.Vector3 position, string text, double height, string font, lcdb.TextAlignment textAlign, double angle);

    }
}
