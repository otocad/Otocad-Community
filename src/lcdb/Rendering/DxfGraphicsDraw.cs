using System;
using System.Collections.Generic;
using LitMath;
using OtoCAD;

namespace lcdb.Rendering
{
    /// <summary>
    /// DXF 实现的 <see cref="IGraphicsDraw"/> — 让"任何 lcdb 实体"都能出 DXF.
    ///
    /// 与 <see cref="SvgGraphicsDraw"/> / SkiaGraphicsDraw 并列的第三个渲染后端: 实体经其
    /// <c>Draw(IGraphicsDraw)</c> 把自己分解成基础图元, 本类把每个图元翻成一个 netDxf 实体.
    /// 这样 66 个 Entity 子类 (光学透镜 / 24 个光学标记 / 图框 / 各类标注) 无需各写一个
    /// type switch 分支就全部可导出 — 画布上看到什么, DXF 里就是什么.
    ///
    /// 坐标系: DXF 与 lcdb 同为模型 Y 向上, 无需翻转 (对比 SvgGraphicsDraw 要取负).
    ///
    /// 图层 / 颜色 / 线型由调用方 (<see cref="Database.SaveAsDxf"/>) 在每个实体绘制前经
    /// <see cref="BeginEntity"/> 注入; 实体 Draw 内部临时改 <see cref="CurrentColor"/> /
    /// <see cref="CurrentLineType"/> 的 (镀膜标记等) 会覆盖该默认值, 未改的沿用 —
    /// 从而 ByLayer 实体在 DXF 里仍是 ByLayer (改层色仍生效), 而非烧死 RGB.
    /// </summary>
    public sealed class DxfGraphicsDraw : IGraphicsDraw
    {
        private readonly List<netDxf.Entities.EntityObject> _entities = new List<netDxf.Entities.EntityObject>();

        /// <summary>本次绘制累积出的 netDxf 实体, 由调用方加入 DxfDocument.</summary>
        public IReadOnlyList<netDxf.Entities.EntityObject> Entities => _entities;

        // ---- 当前实体上下文 (BeginEntity 注入) ----
        private netDxf.Tables.Layer _layer = netDxf.Tables.Layer.Default;
        private netDxf.AciColor _defaultAci = netDxf.AciColor.ByLayer;
        private System.Drawing.Color _defaultSysColor;
        private lcdb.LineType _defaultLineType = lcdb.LineType.ByLayer;

        private System.Drawing.Color _currentColor;
        private lcdb.LineType _currentLineType = lcdb.LineType.ByLayer;

        /// <summary>
        /// 文字样式. 中文属性区/技术要求必须走 TrueType, 否则 AutoCAD 用 txt.shx 显示为问号.
        /// 由调用方注入 (默认宋体).
        /// </summary>
        public netDxf.Tables.TextStyle TextStyle { get; set; } = DefaultTextStyle;

        public RenderMode CurrentMode => RenderMode.Normal;

        public System.Drawing.Color CurrentColor
        {
            get => _currentColor;
            set => _currentColor = value;
        }

        public lcdb.LineType CurrentLineType
        {
            get => _currentLineType;
            set => _currentLineType = value;
        }

        /// <summary>
        /// 开始绘制一个 lcdb 实体: 设定其图层与"默认"颜色/线型.
        ///
        /// aciColor 保留 ByLayer/ByBlock 语义 (sysColor 只是给实体 Draw 读的解析值);
        /// 只要实体没在 Draw 里主动改 CurrentColor, 输出就用 aciColor.
        /// </summary>
        public void BeginEntity(netDxf.Tables.Layer layer, netDxf.AciColor aciColor,
                                System.Drawing.Color sysColor, lcdb.LineType lineType)
        {
            _layer = layer ?? netDxf.Tables.Layer.Default;
            _defaultAci = aciColor ?? netDxf.AciColor.ByLayer;
            _defaultSysColor = sysColor;
            _defaultLineType = lineType;

            _currentColor = sysColor;
            _currentLineType = lineType;
        }

        // -------- 公共属性套用 --------

        private T Add<T>(T e) where T : netDxf.Entities.EntityObject
        {
            e.Layer = _layer;
            // 实体 Draw 未覆盖颜色 → 沿用原始 ACI (可能是 ByLayer); 覆盖了 → 按 RGB 真彩色
            e.Color = _currentColor == _defaultSysColor
                ? _defaultAci
                : new netDxf.AciColor(_currentColor.R, _currentColor.G, _currentColor.B);
            e.Linetype = LinetypeOf(_currentLineType == _defaultLineType ? _defaultLineType : _currentLineType);
            e.Lineweight = netDxf.Lineweight.ByLayer;
            _entities.Add(e);
            return e;
        }

        // -------- 几何绘制 --------

        public void DrawPoint(Vector2 p)
            => Add(new netDxf.Entities.Point(new netDxf.Vector3(p.X, p.Y, 0)));

        public void DrawLine(Vector2 a, Vector2 b)
        {
            if (a == b) return;   // netDxf 允许零长线, 但对下游 CAD 无意义
            Add(new netDxf.Entities.Line(new netDxf.Vector2(a.X, a.Y), new netDxf.Vector2(b.X, b.Y)));
        }

        public void DrawLineDimension(Vector2 a, Vector2 b) => DrawLine(a, b);

        public void DrawXLine(Vector2 basePoint, Vector2 direction)
        {
            var d = direction.normalized;
            Add(new netDxf.Entities.XLine
            {
                Origin = new netDxf.Vector3(basePoint.X, basePoint.Y, 0),
                Direction = new netDxf.Vector3(d.X, d.Y, 0)
            });
        }

        public void DrawRay(Vector2 basePoint, Vector2 direction)
        {
            var d = direction.normalized;
            Add(new netDxf.Entities.Ray
            {
                Origin = new netDxf.Vector3(basePoint.X, basePoint.Y, 0),
                Direction = new netDxf.Vector3(d.X, d.Y, 0)
            });
        }

        public void DrawCircle(Vector2 center, double radius)
        {
            if (!(radius > 0)) return;   // netDxf 对 radius<=0 抛异常
            Add(new netDxf.Entities.Circle(new netDxf.Vector2(center.X, center.Y), radius));
        }

        public void DrawEllipse(Vector2 center, double radiusX, double radiusY)
        {
            if (!(radiusX > 0) || !(radiusY > 0)) return;

            // netDxf 的 MajorAxis/MinorAxis 是"全轴长"(内部处处 *0.5 当半轴用) → 半径要 ×2.
            // 且要求 major >= minor, 否则构造函数抛 — 竖椭圆交换两轴并转 90°.
            double major = radiusX * 2, minor = radiusY * 2, rotation = 0;
            if (minor > major)
            {
                (major, minor) = (minor, major);
                rotation = 90;
            }
            Add(new netDxf.Entities.Ellipse(new netDxf.Vector2(center.X, center.Y), major, minor)
            {
                Rotation = rotation
            });
        }

        public void DrawArc(Vector2 center, double radius, double startAngle, double endAngle)
        {
            if (!(radius > 0)) return;
            // lcdb 弧度 CCW → netDxf 度数 CCW (同向, 直接换算)
            Add(new netDxf.Entities.Arc(new netDxf.Vector2(center.X, center.Y), radius,
                                        startAngle * 180.0 / Math.PI, endAngle * 180.0 / Math.PI));
        }

        public void DrawRectangle(Vector2 position, double width, double height)
        {
            // position = 左下角 (与 SvgGraphicsDraw 同约定)
            var pts = new[]
            {
                new Vector2(position.X, position.Y),
                new Vector2(position.X + width, position.Y),
                new Vector2(position.X + width, position.Y + height),
                new Vector2(position.X, position.Y + height),
            };
            AddPolyline(pts, closed: true);
        }

        public void DrawTriangle(Vector2 v1, Vector2 v2, Vector2 v3)
            => AddSolidFill(new[] { v1, v2, v3 });

        public void DrawQuadrilateral(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4)
            => AddSolidFill(new[] { v1, v2, v3, v4 });

        public void DrawFilledPolygon(IReadOnlyList<Vector2> points)
        {
            if (points is null || points.Count < 3) return;
            AddSolidFill(points);
        }

        private netDxf.Entities.Polyline2D BuildPolyline(IReadOnlyList<Vector2> pts, bool closed)
        {
            var vertexes = new List<netDxf.Entities.Polyline2DVertex>(pts.Count);
            foreach (var p in pts)
                vertexes.Add(new netDxf.Entities.Polyline2DVertex(new netDxf.Vector2(p.X, p.Y)));
            return new netDxf.Entities.Polyline2D(vertexes, closed);
        }

        private void AddPolyline(IReadOnlyList<Vector2> pts, bool closed)
            => Add(BuildPolyline(pts, closed));

        /// <summary>实心多边形 → SOLID 图案填充 (箭头/实心符号). 边界随 Hatch 走, 不单独入库.</summary>
        private void AddSolidFill(IReadOnlyList<Vector2> pts)
        {
            var boundary = new netDxf.Entities.HatchBoundaryPath(
                new netDxf.Entities.EntityObject[] { BuildPolyline(pts, closed: true) });
            Add(new netDxf.Entities.Hatch(netDxf.Entities.HatchPattern.Solid,
                                          new[] { boundary },
                                          associative: false));
        }

        public Vector2 DrawText(Vector2 position, string text, double height, string font,
                                lcdb.TextAlignment textAlign, double angle)
        {
            if (string.IsNullOrEmpty(text) || !(height > 0)) return position;

            var t = new netDxf.Entities.Text(text, new netDxf.Vector2(position.X, position.Y), height, TextStyle)
            {
                Alignment = AlignmentOf(textAlign),
                Rotation = angle * 180.0 / Math.PI
            };
            Add(t);

            // 与 SvgGraphicsDraw 同口径的粗略步进宽度 (调用方用于连排文字)
            return position + new Vector2(text.Length * height * 0.6, 0);
        }

        public Vector2 DrawText(Vector3 position, string text, double height, string font,
                                lcdb.TextAlignment textAlign, double angle)
            => DrawText(new Vector2(position.X, position.Y), text, height, font, textAlign, angle);

        private static netDxf.Entities.TextAlignment AlignmentOf(lcdb.TextAlignment a)
        {
            switch (a)
            {
                case lcdb.TextAlignment.LeftBottom:   return netDxf.Entities.TextAlignment.BottomLeft;
                case lcdb.TextAlignment.LeftMiddle:   return netDxf.Entities.TextAlignment.MiddleLeft;
                case lcdb.TextAlignment.LeftTop:      return netDxf.Entities.TextAlignment.TopLeft;
                case lcdb.TextAlignment.CenterBottom: return netDxf.Entities.TextAlignment.BottomCenter;
                case lcdb.TextAlignment.CenterMiddle: return netDxf.Entities.TextAlignment.MiddleCenter;
                case lcdb.TextAlignment.CenterTop:    return netDxf.Entities.TextAlignment.TopCenter;
                case lcdb.TextAlignment.RightBottom:  return netDxf.Entities.TextAlignment.BottomRight;
                case lcdb.TextAlignment.RightMiddle:  return netDxf.Entities.TextAlignment.MiddleRight;
                case lcdb.TextAlignment.RightTop:     return netDxf.Entities.TextAlignment.TopRight;
                default:                              return netDxf.Entities.TextAlignment.BaselineLeft;
            }
        }

        // -------- 共享表对象 --------
        //
        // netDxf 的 Linetype.Continuous 等是每次 new 一个实例的属性 getter; 复用单例可避免
        // 同一份 DXF 里冒出多个同名线型定义.

        private static readonly netDxf.Tables.Linetype ContinuousLt = netDxf.Tables.Linetype.Continuous;
        private static readonly netDxf.Tables.Linetype DashedLt     = netDxf.Tables.Linetype.Dashed;
        private static readonly netDxf.Tables.Linetype DotLt        = netDxf.Tables.Linetype.Dot;
        private static readonly netDxf.Tables.Linetype DashDotLt    = netDxf.Tables.Linetype.DashDot;
        private static readonly netDxf.Tables.Linetype ByLayerLt    = netDxf.Tables.Linetype.ByLayer;
        private static readonly netDxf.Tables.Linetype ByBlockLt    = netDxf.Tables.Linetype.ByBlock;

        /// <summary>
        /// 双点画线 (GB/T 13323-2009 §2.1 光轴用). netDxf 无预定义 — 自建, 否则光轴在 DXF 里
        /// 退化成单点画线, 与中心线无法区分.
        /// </summary>
        private static readonly netDxf.Tables.Linetype DashDotDotLt = BuildDashDotDot();

        private static netDxf.Tables.Linetype BuildDashDotDot()
        {
            var segments = new List<netDxf.Tables.LinetypeSegment>
            {
                new netDxf.Tables.LinetypeSimpleSegment(1.0),
                new netDxf.Tables.LinetypeSimpleSegment(-0.25),
                new netDxf.Tables.LinetypeSimpleSegment(0.0),
                new netDxf.Tables.LinetypeSimpleSegment(-0.25),
                new netDxf.Tables.LinetypeSimpleSegment(0.0),
                new netDxf.Tables.LinetypeSimpleSegment(-0.25),
            };
            return new netDxf.Tables.Linetype("DASHDOTDOT", segments, "Dash dot dot __ . . __ . . __");
        }

        /// <summary>宋体 TrueType — 中文标题栏/技术要求在 AutoCAD 里不变问号的最低要求.</summary>
        private static readonly netDxf.Tables.TextStyle DefaultTextStyle =
            new netDxf.Tables.TextStyle("OtoCAD", "SimSun", netDxf.Tables.FontStyle.Regular);

        /// <summary>lcdb 线型 → netDxf 线型. 供本类与 <see cref="Database.SaveAsDxf"/> 共用.</summary>
        public static netDxf.Tables.Linetype LinetypeOf(lcdb.LineType lineType)
        {
            switch (lineType)
            {
                case lcdb.LineType.Solid:      return ContinuousLt;
                case lcdb.LineType.Dash:       return DashedLt;
                case lcdb.LineType.Dot:        return DotLt;
                case lcdb.LineType.DashDot:    return DashDotLt;
                case lcdb.LineType.DashDotDot: return DashDotDotLt;
                case lcdb.LineType.ByBlock:    return ByBlockLt;
                default:                       return ByLayerLt;
            }
        }
    }
}
