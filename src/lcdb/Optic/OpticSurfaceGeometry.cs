using System;
using LitMath;

namespace lcdb.Optic;

/// <summary>玻璃剖面填充样式 (可在属性面板选择).</summary>
public enum GlassPattern
{
    /// <summary>无 — 仅描边轮廓.</summary>
    None,
    /// <summary>小三线 短长短 符号 (GB 光学玻璃, 默认) — 剖面内一小处画 3 条短长短斜线, 大部分留白.</summary>
    Symbol,
    /// <summary>均匀全剖面斜线 (机械件式) — 等长 45° 斜线铺满整个剖面.</summary>
    Hatch,
}

/// <summary>
/// 光学表面几何 helper — 提供 sag 计算 + 球面 Arc / 平面 Line 生成.
/// 跨 OpticalLens / CementedLens / OpticalAssembly 复用, 集中 ISO 10110-1 符号约定.
///
/// 符号约定 (与 Zemax 一致):
///   R &gt; 0 ⇔ 曲率中心在表面 *右侧* (光行进方向)
///   R = ∞ ⇔ 平面
///
/// 统一公式:
///   centerX = apexX + R     (R 带符号; R&gt;0 中心在右, R&lt;0 中心在左)
///   sag(R, h) = sign(R) × (|R| - √(R² - h²))   (R&gt;0 sag&gt;0; R&lt;0 sag&lt;0)
///   edgeX = apexX + sag    (统一所有前/后表面)
/// </summary>
public static class OpticSurfaceGeometry
{
    /// <summary>玻璃剖面线颜色 (蓝灰细线, 区别于零件轮廓黑).</summary>
    public static readonly lcdb.Colors.Color GlassHatch = lcdb.Colors.Color.FromRGB(0x6E, 0x93, 0xB3);

    /// <summary>
    /// 把镜片绘制半口径钳到各球面物理可达的最大半口径 (= min |R|).
    /// 球面在半径 |R| 处达半球, 净口径不可能超过 |R|; 否则弧 (半球) 够不到净口径边线 → 缺口.
    /// 平面 (R=∞) 不限制. 返回 ≤ 入参 halfD 的值.
    /// </summary>
    public static double LimitSemiAperture(double halfD, params OpticalSurface[] surfaces)
    {
        double lim = halfD;
        if (surfaces != null)
        {
            foreach (var s in surfaces)
            {
                if (s is null) continue;
                double r = Math.Abs(s.Radius);
                if (!double.IsInfinity(r) && !double.IsNaN(r) && r > 1e-9 && r < lim)
                    lim = r;
            }
        }
        return lim;
    }

    /// <summary>
    /// 单个表面的绘制净口径半径: 优先用面自带 <see cref="OpticalSurface.SemiAperture"/>(&gt;0),
    /// 否则用 fallback (镜片整体净口径); 再钳到该面物理可达上限 |R| (不超过半球).
    /// </summary>
    public static double DrawnSemiAperture(OpticalSurface surface, double fallbackHalf)
    {
        double req = (surface != null && surface.SemiAperture > 1e-9) ? surface.SemiAperture : fallbackHalf;
        double r = surface == null ? double.PositiveInfinity : Math.Abs(surface.Radius);
        if (!double.IsInfinity(r) && !double.IsNaN(r) && r > 1e-9 && r < req) req = r;
        return req;
    }

    /// <summary>
    /// 在闭合多边形内生成平铺剖面线 (沿 angleRad、间距 spacing 的平行线, even-odd 裁剪到多边形内).
    /// <paramref name="shortRatio"/> &lt; 1 时偶数序号线缩短并居中 → 短长短长… 交替 (GB 透明材料剖面线);
    /// = 1 时全为整长 (机械件式均匀剖面线). 返回线段端点对.
    /// </summary>
    public static System.Collections.Generic.List<(Vector2 a, Vector2 b)> HatchSegments(
        System.Collections.Generic.IReadOnlyList<Vector2> poly, double spacing, double angleRad, double shortRatio = 1.0)
    {
        var result = new System.Collections.Generic.List<(Vector2, Vector2)>();
        if (poly == null || poly.Count < 3 || spacing <= 1e-6) return result;

        double ux = Math.Cos(angleRad), uy = Math.Sin(angleRad);   // 线方向
        double nx = -uy, ny = ux;                                  // 法向 (单位)
        double nMin = double.MaxValue, nMax = double.MinValue;
        foreach (var p in poly)
        {
            double d = p.X * nx + p.Y * ny;
            if (d < nMin) nMin = d;
            if (d > nMax) nMax = d;
        }

        var hits = new System.Collections.Generic.List<double>();
        int lineIdx = -1;
        for (double t = nMin + spacing * 0.5; t < nMax; t += spacing)
        {
            lineIdx++;
            hits.Clear();
            for (int i = 0; i < poly.Count; i++)
            {
                var a = poly[i];
                var b = poly[(i + 1) % poly.Count];
                double da = a.X * nx + a.Y * ny - t;
                double db = b.X * nx + b.Y * ny - t;
                if (Math.Abs(da - db) < 1e-12) continue;     // 平行
                double s = da / (da - db);
                if (s < 0.0 || s > 1.0) continue;
                double px = a.X + s * (b.X - a.X);
                double py = a.Y + s * (b.Y - a.Y);
                hits.Add(px * ux + py * uy);                 // 沿线方向参数
            }
            if (hits.Count < 2) continue;
            hits.Sort();
            bool shorten = shortRatio < 0.999 && (lineIdx % 2 == 0);   // 偶数线 = 短
            for (int k = 0; k + 1 < hits.Count; k += 2)
            {
                double lo = hits[k], hi = hits[k + 1];
                if (shorten)
                {
                    double mid = (lo + hi) * 0.5, h = (hi - lo) * 0.5 * shortRatio;
                    lo = mid - h; hi = mid + h;
                }
                double bx = t * nx, by = t * ny;             // 该线上 param=0 的基点
                result.Add((
                    new Vector2(bx + lo * ux, by + lo * uy),
                    new Vector2(bx + hi * ux, by + hi * uy)));
            }
        }
        return result;
    }

    /// <summary>
    /// GB 光学玻璃材料符号: 小三线 短长短 簇按间距**平铺**整个剖面 —— 一簇(3 条短长短斜线)、空一段、再一簇,
    /// 簇之间留白. 每条裁剪到剖面多边形内. 与机械件密铺剖面线不同, 簇稀疏分布作材料标识.
    /// </summary>
    /// <param name="longLen">每簇中间长线长度 (两侧短线 = 一半)</param>
    /// <param name="lineGap">簇内相邻线间距 (沿法向)</param>
    /// <param name="clusterGap">簇与簇之间的法向留白</param>
    /// <param name="colGap">沿线方向相邻簇列间距</param>
    public static System.Collections.Generic.List<(Vector2 a, Vector2 b)> GlassSymbolSegments(
        System.Collections.Generic.IReadOnlyList<Vector2> poly,
        double longLen, double lineGap, double clusterGap, double colGap, double angleRad)
    {
        var result = new System.Collections.Generic.List<(Vector2, Vector2)>();
        if (poly == null || poly.Count < 3 || longLen <= 1e-6 || colGap <= 1e-6) return result;

        double ux = Math.Cos(angleRad), uy = Math.Sin(angleRad);   // 线方向
        double nx = -uy, ny = ux;                                  // 法向 (排布方向)
        double uMin = double.MaxValue, uMax = double.MinValue, nMin = double.MaxValue, nMax = double.MinValue;
        foreach (var p in poly)
        {
            double du = p.X * ux + p.Y * uy, dn = p.X * nx + p.Y * ny;
            if (du < uMin) uMin = du; if (du > uMax) uMax = du;
            if (dn < nMin) nMin = dn; if (dn > nMax) nMax = dn;
        }

        double nPeriod = 2 * lineGap + clusterGap;                 // 一簇(3线跨 2*lineGap) + 留白
        double[] factor = { 0.5, 1.0, 0.5 };                       // 短长短
        for (double ub = uMin + colGap * 0.5; ub <= uMax; ub += colGap)
            for (double nb = nMin + lineGap; nb < nMax; nb += nPeriod)
                for (int i = 0; i < 3; i++)
                {
                    double nn = nb + i * lineGap;
                    double half = 0.5 * longLen * factor[i];
                    double mx = ub * ux + nn * nx, my = ub * uy + nn * ny;   // (u=ub, n=nn) 处中点
                    var a = new Vector2(mx - half * ux, my - half * uy);
                    var b = new Vector2(mx + half * ux, my + half * uy);
                    var clipped = ClipSegmentToPolygon(a, b, poly);
                    if (clipped.HasValue) result.Add(clipped.Value);
                }
        return result;
    }

    /// <summary>把线段 p0→p1 裁剪到多边形内部 (even-odd); 返回含中点的内部子段, 全在外则 null.</summary>
    private static (Vector2 a, Vector2 b)? ClipSegmentToPolygon(
        Vector2 p0, Vector2 p1, System.Collections.Generic.IReadOnlyList<Vector2> poly)
    {
        double dx = p1.X - p0.X, dy = p1.Y - p0.Y;
        var ts = new System.Collections.Generic.List<double> { 0.0, 1.0 };
        for (int i = 0; i < poly.Count; i++)
        {
            var e0 = poly[i];
            var e1 = poly[(i + 1) % poly.Count];
            double ex = e1.X - e0.X, ey = e1.Y - e0.Y;
            double denom = dx * ey - dy * ex;
            if (Math.Abs(denom) < 1e-12) continue;
            double t = ((e0.X - p0.X) * ey - (e0.Y - p0.Y) * ex) / denom;   // 段上参数
            double s = ((e0.X - p0.X) * dy - (e0.Y - p0.Y) * dx) / denom;   // 边上参数
            if (t > 1e-9 && t < 1.0 - 1e-9 && s >= -1e-9 && s <= 1.0 + 1e-9) ts.Add(t);
        }
        ts.Sort();
        for (int i = 0; i + 1 < ts.Count; i++)
        {
            double tm = 0.5 * (ts[i] + ts[i + 1]);
            var m = new Vector2(p0.X + tm * dx, p0.Y + tm * dy);
            if (PointInPolygon(m, poly))
                return (new Vector2(p0.X + ts[i] * dx, p0.Y + ts[i] * dy),
                        new Vector2(p0.X + ts[i + 1] * dx, p0.Y + ts[i + 1] * dy));
        }
        return null;
    }

    /// <summary>射线法点在多边形内判定.</summary>
    private static bool PointInPolygon(Vector2 p, System.Collections.Generic.IReadOnlyList<Vector2> poly)
    {
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            var a = poly[i];
            var b = poly[j];
            if (((a.Y > p.Y) != (b.Y > p.Y)) &&
                (p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X))
                inside = !inside;
        }
        return inside;
    }

    /// <summary>
    /// 按 <paramref name="pattern"/> 生成玻璃剖面线段: Symbol = 小三线 短长短 符号 (大部分留白, GB 光学玻璃);
    /// Hatch = 均匀全剖面斜线 (机械件式, 铺满); None = 空. diameter 用于自适应尺寸/间距.
    /// </summary>
    public static System.Collections.Generic.List<(Vector2 a, Vector2 b)> GlassPatternSegments(
        System.Collections.Generic.IReadOnlyList<Vector2> poly, GlassPattern pattern, double diameter, double angleRad)
    {
        switch (pattern)
        {
            case GlassPattern.Hatch:
                return HatchSegments(poly, Math.Max(1.2, diameter / 18.0), angleRad);
            case GlassPattern.Symbol:
                // 小三线 短长短 簇按间距平铺整个剖面, 簇间留白.
                double longLen = Math.Max(2.0, diameter * 0.12);   // 每簇长线 ≈ 0.12×口径
                double lineGap = Math.Max(0.6, longLen * 0.32);    // 簇内线距
                double clusterGap = Math.Max(1.6, longLen * 1.2);  // 簇间法向留白
                double colGap = Math.Max(2.6, longLen * 1.7);      // 列间距
                return GlassSymbolSegments(poly, longLen, lineGap, clusterGap, colGap, angleRad);
            default:
                return new System.Collections.Generic.List<(Vector2, Vector2)>();
        }
    }

    /// <summary>球面 sag = r - sqrt(r²-h²); R 带符号 → sag 带符号; R=∞ → sag=0.</summary>
    public static double ComputeSag(double radius, double semiAperture)
    {
        if (double.IsInfinity(radius) || double.IsNaN(radius)) return 0;
        double r = Math.Abs(radius);
        double h = Math.Abs(semiAperture);
        if (r < h) h = r;  // 防 sqrt 负数 (无效物理)
        double mag = r - Math.Sqrt(r * r - h * h);
        return Math.Sign(radius) * mag;
    }

    /// <summary>
    /// 表面图元 — 球面返 Arc, 平面 (R=∞) 返 Line.
    /// apexY = 表面顶点的 Y (沿光轴, 通常 = Position.Y)
    /// </summary>
    /// <returns>null 时上层应跳过 (semiAperture &lt;= 0)</returns>
    public static Entity? MakeSurface(
        double apexX, double apexY, double radius, double semiAperture,
        lcdb.Colors.Color color)
    {
        if (semiAperture <= 0) return null;

        // 平面 (∞ 或绝对值过大)
        if (double.IsInfinity(radius) || Math.Abs(radius) > 1e6)
        {
            return new Line(
                new Vector2(apexX, apexY - semiAperture),
                new Vector2(apexX, apexY + semiAperture)) { color = color };
        }

        // 球面 → Arc
        double r = Math.Abs(radius);
        double centerX = apexX + radius;
        var center = new Vector2(centerX, apexY);

        double sinHalf = Math.Min(1.0, semiAperture / r);
        double halfSpan = Math.Asin(sinHalf);

        // 顶点相对中心的方向 (R>0 顶点在中心左侧 → angle≈π; R<0 顶点在右 → angle≈0)
        double apexAngle = Math.Atan2(apexY - center.Y, apexX - center.X);
        double startAngle = apexAngle - halfSpan;
        double endAngle   = apexAngle + halfSpan;

        return new Arc(center, r, startAngle, endAngle) { color = color };
    }
}
