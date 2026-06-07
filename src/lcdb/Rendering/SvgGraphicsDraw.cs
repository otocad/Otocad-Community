using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LitMath;
using OtoCAD;

namespace lcdb.Rendering
{
    /// <summary>
    /// SVG 实现的 <see cref="IGraphicsDraw"/> — "单一真值"出图/文档渲染器.
    ///
    /// 与 SkiaGraphicsDraw 并列: 任何 lcdb 实体 (Line / Circle / Arc / CoatingMark / 各 Mark)
    /// 经其 <c>Draw(IGraphicsDraw)</c> 即可吐出 SVG, 无需为每个实体写 type switch, 也无需把
    /// 几何在前端 (HTML/JS) 重抄一遍 — 文档预览与实际出图共用同一份几何代码.
    ///
    /// 坐标系: 模型 Y 向上, SVG Y 向下 → 内部对每个 Y 取负 (svgY = -y), 文字保持正立 (不镜像).
    /// 纯字符串输出, 无 UI 依赖, 跨平台 (net10.0).
    /// </summary>
    public sealed class SvgGraphicsDraw : IGraphicsDraw
    {
        private readonly List<string> _elements = new List<string>();
        private System.Drawing.Color _currentColor = System.Drawing.Color.Black;
        private lcdb.LineType _currentLineType = lcdb.LineType.Solid;

        // 内容包围盒 (SVG 空间, 已 Y 翻转). 用于自动 viewBox.
        private double _minX = double.PositiveInfinity, _minY = double.PositiveInfinity;
        private double _maxX = double.NegativeInfinity, _maxY = double.NegativeInfinity;

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

        // -------- 坐标 / 格式化 --------

        private static double SvgY(double y) => -y;   // 模型 Y 向上 → SVG Y 向下

        private static string F(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);

        private static string Hex(System.Drawing.Color c) =>
            $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        private void Expand(double sx, double sy)
        {
            if (sx < _minX) _minX = sx;
            if (sy < _minY) _minY = sy;
            if (sx > _maxX) _maxX = sx;
            if (sy > _maxY) _maxY = sy;
        }

        /// <summary>GB/T 17450 / ISO 128 比例的 stroke-dasharray (模型单位); 实线返回 null.</summary>
        private string DashArray(double u)
        {
            switch (_currentLineType)
            {
                case lcdb.LineType.Dash:       return $"{F(8 * u)},{F(4 * u)}";
                case lcdb.LineType.Dot:        return $"{F(1.5 * u)},{F(3 * u)}";
                case lcdb.LineType.DashDot:    return $"{F(10 * u)},{F(3 * u)},{F(1.5 * u)},{F(3 * u)}";
                case lcdb.LineType.DashDotDot: return $"{F(10 * u)},{F(3 * u)},{F(1.5 * u)},{F(3 * u)},{F(1.5 * u)},{F(3 * u)}";
                default: return null;
            }
        }

        private string StrokeAttrs(double dashUnit)
        {
            var da = DashArray(dashUnit);
            var dash = da is null ? "" : $" stroke-dasharray=\"{da}\"";
            return $"stroke=\"{Hex(_currentColor)}\" fill=\"none\"{dash}";
        }

        private string FillAttrs() => $"fill=\"{Hex(_currentColor)}\" stroke=\"none\"";

        // -------- 几何绘制 --------

        public void DrawPoint(Vector2 p)
        {
            double x = p.X, y = SvgY(p.Y);
            Expand(x, y);
            // 点用一个小实心圆表示 (半径相对内容自适应在 BuildSvg 时无法预知, 用固定小值, BuildSvg 会整体缩放)
            _elements.Add($"<circle cx=\"{F(x)}\" cy=\"{F(y)}\" r=\"__PTR__\" {FillAttrs()}/>");
        }

        public void DrawLine(Vector2 a, Vector2 b)
        {
            double x1 = a.X, y1 = SvgY(a.Y), x2 = b.X, y2 = SvgY(b.Y);
            Expand(x1, y1); Expand(x2, y2);
            _elements.Add($"<line x1=\"{F(x1)}\" y1=\"{F(y1)}\" x2=\"{F(x2)}\" y2=\"{F(y2)}\" {StrokeAttrs(_dashUnit)} __SW__ stroke-linecap=\"round\"/>");
        }

        public void DrawLineDimension(Vector2 a, Vector2 b) => DrawLine(a, b);

        public void DrawXLine(Vector2 basePoint, Vector2 direction)
        {
            var d = direction.normalized;
            const double L = 100000;
            DrawLine(basePoint - d * L, basePoint + d * L);
        }

        public void DrawRay(Vector2 basePoint, Vector2 direction)
        {
            var d = direction.normalized;
            const double L = 100000;
            DrawLine(basePoint, basePoint + d * L);
        }

        public void DrawCircle(Vector2 center, double radius)
        {
            double cx = center.X, cy = SvgY(center.Y);
            Expand(cx - radius, cy - radius); Expand(cx + radius, cy + radius);
            _elements.Add($"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(radius)}\" {StrokeAttrs(_dashUnit)} __SW__/>");
        }

        public void DrawEllipse(Vector2 center, double radiusX, double radiusY)
        {
            double cx = center.X, cy = SvgY(center.Y);
            Expand(cx - radiusX, cy - radiusY); Expand(cx + radiusX, cy + radiusY);
            _elements.Add($"<ellipse cx=\"{F(cx)}\" cy=\"{F(cy)}\" rx=\"{F(radiusX)}\" ry=\"{F(radiusY)}\" {StrokeAttrs(_dashUnit)} __SW__/>");
        }

        public void DrawArc(Vector2 center, double radius, double startAngle, double endAngle)
        {
            // lcdb 约定逆时针. 用折线采样, 绕过 SVG arc sweep-flag 在 Y 翻转坐标下的歧义 — 视觉总正确.
            double sweep = endAngle - startAngle;
            while (sweep <= 0) sweep += 2 * Math.PI;
            int seg = Math.Max(8, (int)(sweep / (Math.PI / 36)));   // ~5° 一段
            var sb = new StringBuilder("<polyline points=\"");
            for (int i = 0; i <= seg; i++)
            {
                double a = startAngle + sweep * i / seg;
                double x = center.X + radius * Math.Cos(a);
                double y = SvgY(center.Y + radius * Math.Sin(a));
                Expand(x, y);
                if (i > 0) sb.Append(' ');
                sb.Append(F(x)).Append(',').Append(F(y));
            }
            sb.Append("\" ").Append(StrokeAttrs(_dashUnit)).Append(" __SW__/>");
            _elements.Add(sb.ToString());
        }

        public void DrawRectangle(Vector2 position, double width, double height)
        {
            // position = 左下角 (模型). SVG rect 用左上角 → y 取 SvgY(position.Y + height).
            double x = position.X, y = SvgY(position.Y + height);
            Expand(x, y); Expand(x + width, y + height);
            _elements.Add($"<rect x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(width)}\" height=\"{F(height)}\" {StrokeAttrs(_dashUnit)} __SW__/>");
        }

        public void DrawTriangle(Vector2 v1, Vector2 v2, Vector2 v3)
            => AddFilledPoly(new[] { v1, v2, v3 });

        public void DrawQuadrilateral(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4)
            => AddFilledPoly(new[] { v1, v2, v3, v4 });

        public void DrawFilledPolygon(System.Collections.Generic.IReadOnlyList<Vector2> points)
        {
            if (points is null || points.Count < 3) return;
            AddFilledPoly(points);
        }

        private void AddFilledPoly(System.Collections.Generic.IReadOnlyList<Vector2> pts)
        {
            var sb = new StringBuilder("<polygon points=\"");
            for (int i = 0; i < pts.Count; i++)
            {
                double x = pts[i].X, y = SvgY(pts[i].Y);
                Expand(x, y);
                if (i > 0) sb.Append(' ');
                sb.Append(F(x)).Append(',').Append(F(y));
            }
            sb.Append("\" ").Append(FillAttrs()).Append("/>");
            _elements.Add(sb.ToString());
        }

        public Vector2 DrawText(Vector2 position, string text, double height, string font, lcdb.TextAlignment textAlign, double angle)
        {
            if (string.IsNullOrEmpty(text)) return position;
            double x = position.X, y = SvgY(position.Y);
            // 文字始终正立: 不随 Y 翻转镜像. 角度 (模型 CCW) 在 Y 向下空间取负 → SVG rotate 顺时针为正.
            string anchor = AnchorOf(textAlign);
            string baseline = BaselineOf(textAlign);
            string rot = Math.Abs(angle) > 1e-9
                ? $" transform=\"rotate({F(-angle * 180.0 / Math.PI)} {F(x)} {F(y)})\""
                : "";
            // 粗略包围盒 (用于 viewBox): 宽度按 0.6*height/字 估算
            double w = text.Length * height * 0.6;
            Expand(x, y - height); Expand(x + w, y + height * 0.3);
            _elements.Add(
                $"<text x=\"{F(x)}\" y=\"{F(y)}\" font-size=\"{F(height)}\" " +
                $"fill=\"{Hex(_currentColor)}\" text-anchor=\"{anchor}\" dominant-baseline=\"{baseline}\"" +
                $"{rot}>{Escape(text)}</text>");
            return position + new Vector2(w, 0);
        }

        public Vector2 DrawText(Vector3 position, string text, double height, string font, lcdb.TextAlignment textAlign, double angle)
            => DrawText(new Vector2(position.X, position.Y), text, height, font, textAlign, angle);

        private static string AnchorOf(lcdb.TextAlignment a)
        {
            string s = a.ToString();
            if (s.Contains("Left")) return "start";
            if (s.Contains("Right")) return "end";
            return "middle";
        }

        private static string BaselineOf(lcdb.TextAlignment a)
        {
            string s = a.ToString();
            if (s.Contains("Top")) return "hanging";
            if (s.Contains("Bottom") || s.Contains("Base")) return "alphabetic";
            return "middle";
        }

        private static string Escape(string s) => s
            .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        // -------- 输出 --------

        // dash 单位与线宽在 BuildSvg 时按内容尺度确定; 绘制期先用占位符再回填.
        private double _dashUnit = 0.2;

        /// <summary>
        /// 组装最终 <c>&lt;svg&gt;</c> 字符串. viewBox 自动贴合内容 (留白 marginRatio),
        /// 线宽按内容最大边自适应 (≈ 渲染时 1.5px 视觉).
        /// </summary>
        /// <param name="pixelSize">SVG width/height 像素 (正方形适配; 默认 160).</param>
        /// <param name="marginRatio">留白占最大边比例 (默认 0.12).</param>
        /// <param name="strokeWidth">绝对线宽 (模型单位); &gt;0 时覆盖"按内容尺度自适应"的默认值。
        /// 符号类标记 (圆/十字, 内容小) 用默认即可; 表格类 (内容大、文字小) 应显式传细线宽
        /// (如 0.4), 否则自适应线宽会粗到把小字糊成一团。</param>
        public string BuildSvg(int pixelSize = 160, double marginRatio = 0.12, double strokeWidth = 0)
        {
            if (double.IsInfinity(_minX))   // 空内容
                return $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{pixelSize}\" height=\"{pixelSize}\" viewBox=\"0 0 1 1\"></svg>";

            double w = _maxX - _minX, h = _maxY - _minY;
            double dim = Math.Max(Math.Max(w, h), 1e-6);
            double margin = dim * marginRatio;
            double vbX = _minX - margin, vbY = _minY - margin;
            double vbW = w + 2 * margin, vbH = h + 2 * margin;

            double stroke = strokeWidth > 0 ? strokeWidth : dim * 0.012;   // 显式细线宽优先, 否则按内容尺度
            double ptR = dim * 0.02;

            // width/height 按内容长宽比, 长边=pixelSize → 杜绝正方形画布给宽/扁内容(如表格)留大白边.
            double aspect = vbW / vbH;
            int pxW = aspect >= 1 ? pixelSize : Math.Max(1, (int)Math.Round(pixelSize * aspect));
            int pxH = aspect >= 1 ? Math.Max(1, (int)Math.Round(pixelSize / aspect)) : pixelSize;

            var sb = new StringBuilder();
            sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{pxW}\" height=\"{pxH}\" ")
              .Append($"viewBox=\"{F(vbX)} {F(vbY)} {F(vbW)} {F(vbH)}\">");
            foreach (var el in _elements)
            {
                sb.Append(el
                    .Replace("__SW__", $"stroke-width=\"{F(stroke)}\"")
                    .Replace("__PTR__", F(ptR)));
            }
            sb.Append("</svg>");
            return sb.ToString();
        }
    }
}
