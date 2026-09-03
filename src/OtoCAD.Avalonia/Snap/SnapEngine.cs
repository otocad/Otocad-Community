using System;
using System.Collections.Generic;
using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Snap;

/// <summary>
/// Phase 1E: 对象捕捉 (Snap) 引擎.
/// 给定屏幕点 + 实体列表, 在容差内找最近的特征点 (端点/中点/圆心/交点/最近点).
///
/// 调用方 (CadCanvas) 在画图命令期间应优先用 snapped 点而非原始光标点.
/// </summary>
public sealed class SnapEngine
{
    /// <summary>启用的 snap 类型 (位掩码).</summary>
    public SnapType Enabled { get; set; } = SnapType.Endpoint | SnapType.Midpoint | SnapType.Center;

    /// <summary>
    /// 在模型 tolerance 范围内查最近的 snap 点. 找不到返回 null.
    /// overrideTypes 非 null 时用它替代 Enabled (供命令临时切换 snap 模式, e.g. Mark cmd → Nearest only).
    /// </summary>
    public SnapResult? FindSnap(Vector2 cursor, IReadOnlyList<Entity> entities, double tolerance, SnapType? overrideTypes = null)
    {
        var activeTypes = overrideTypes ?? Enabled;
        SnapResult? best = null;
        double bestDistSq = tolerance * tolerance;

        // Nearest 捕捉的统一更新 (复合实体下钻时复用): 取 src 边上离 cursor 最近的点, 更近则更新 best。
        void TryNearest(Entity src)
        {
            if (NearestPointOn(src, cursor) is not { } np) return;
            var dx = np.X - cursor.X;
            var dy = np.Y - cursor.Y;
            var d2 = dx * dx + dy * dy;
            if (d2 < bestDistSq)
            {
                bestDistSq = d2;
                best = new SnapResult(np, SnapType.Nearest, src);
            }
        }

        foreach (var e in entities)
        {
            // 离散 candidate 点
            foreach (var (pt, type) in EnumerateCandidates(e))
            {
                if ((activeTypes & type) == 0) continue;
                var dx = pt.X - cursor.X;
                var dy = pt.Y - cursor.Y;
                var d2 = dx * dx + dy * dy;
                if (d2 < bestDistSq)
                {
                    bestDistSq = d2;
                    best = new SnapResult(pt, type, e);
                }
            }
            // 连续: nearest-on-curve (Mark cmd 偏好)
            if ((activeTypes & SnapType.Nearest) != 0)
            {
                TryNearest(e);
                // 复合实体 (透镜/图框等) 自身无 nearest-on-curve, 需下钻到内部弧/线,
                // 否则贴面标记捕捉不到透镜曲面 (用户点弧边没反应)。
                if (e is IPierceable pierceable)
                    foreach (var sub in pierceable.GetPierceableSubEntities())
                        TryNearest(sub);
            }
        }
        return best;
    }

    /// <summary>给 CadCanvas.PickEntityAt 复用 (走相同的最近点算法).</summary>
    public static Vector2? NearestPointOnPublic(Entity e, Vector2 cursor) => NearestPointOn(e, cursor);

    /// <summary>
    /// 计算实体边上离 cursor 最近的点. 不支持的实体返回 null.
    /// </summary>
    private static Vector2? NearestPointOn(Entity e, Vector2 cursor)
    {
        switch (e)
        {
            case Line line:
                return NearestOnSegment(cursor, line.startPoint, line.endPoint);
            case Circle c:
                {
                    var dx = cursor.X - c.center.X;
                    var dy = cursor.Y - c.center.Y;
                    var d = Math.Sqrt(dx * dx + dy * dy);
                    if (d < 1e-9) return null;
                    return new Vector2(c.center.X + dx / d * c.radius, c.center.Y + dy / d * c.radius);
                }
            case Arc a:
                {
                    var dx = cursor.X - a.center.X;
                    var dy = cursor.Y - a.center.Y;
                    var d = Math.Sqrt(dx * dx + dy * dy);
                    if (d < 1e-9) return null;
                    var angle = Math.Atan2(dy, dx);
                    if (!AngleInArc(angle, a.startAngle, a.endAngle)) return null;
                    return new Vector2(a.center.X + dx / d * a.radius, a.center.Y + dy / d * a.radius);
                }
            case Polyline pl:
                {
                    Vector2? best = null;
                    double bestSq = double.MaxValue;
                    for (int i = 0; i + 1 < pl.NumberOfVertices; i++)
                    {
                        var aPt = new Vector2(pl.Vertices[i].X, pl.Vertices[i].Y);
                        var bPt = new Vector2(pl.Vertices[i + 1].X, pl.Vertices[i + 1].Y);
                        var p = NearestOnSegment(cursor, aPt, bPt);
                        var dx = p.X - cursor.X; var dy = p.Y - cursor.Y;
                        var d2 = dx * dx + dy * dy;
                        if (d2 < bestSq) { bestSq = d2; best = p; }
                    }
                    return best;
                }
            default:
                return null;
        }
    }

    private static Vector2 NearestOnSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var dx = b.X - a.X; var dy = b.Y - a.Y;
        var lenSq = dx * dx + dy * dy;
        if (lenSq < 1e-12) return a;
        var t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSq;
        t = Math.Max(0, Math.Min(1, t));
        return new Vector2(a.X + t * dx, a.Y + t * dy);
    }

    private static bool AngleInArc(double angle, double startAngle, double endAngle)
    {
        double Norm(double x) { while (x < 0) x += 2 * Math.PI; while (x >= 2 * Math.PI) x -= 2 * Math.PI; return x; }
        double na = Norm(angle), ns = Norm(startAngle), ne = Norm(endAngle);
        if (ns <= ne) return na >= ns && na <= ne;
        return na >= ns || na <= ne;
    }

    private static IEnumerable<(Vector2 Point, SnapType Type)> EnumerateCandidates(Entity e)
    {
        switch (e)
        {
            case Line line:
                yield return (line.startPoint, SnapType.Endpoint);
                yield return (line.endPoint, SnapType.Endpoint);
                yield return (new Vector2((line.startPoint.X + line.endPoint.X) / 2,
                                          (line.startPoint.Y + line.endPoint.Y) / 2), SnapType.Midpoint);
                break;

            case Circle c:
                yield return (c.center, SnapType.Center);
                // 4 个象限点作为 endpoint 候选
                yield return (new Vector2(c.center.X + c.radius, c.center.Y), SnapType.Endpoint);
                yield return (new Vector2(c.center.X - c.radius, c.center.Y), SnapType.Endpoint);
                yield return (new Vector2(c.center.X, c.center.Y + c.radius), SnapType.Endpoint);
                yield return (new Vector2(c.center.X, c.center.Y - c.radius), SnapType.Endpoint);
                break;

            case Arc a:
                yield return (a.center, SnapType.Center);
                var s = new Vector2(a.center.X + a.radius * Math.Cos(a.startAngle),
                                     a.center.Y + a.radius * Math.Sin(a.startAngle));
                var ee = new Vector2(a.center.X + a.radius * Math.Cos(a.endAngle),
                                      a.center.Y + a.radius * Math.Sin(a.endAngle));
                yield return (s, SnapType.Endpoint);
                yield return (ee, SnapType.Endpoint);
                // 圆弧中点
                var mid = (a.startAngle + a.endAngle) / 2;
                yield return (new Vector2(a.center.X + a.radius * Math.Cos(mid),
                                           a.center.Y + a.radius * Math.Sin(mid)), SnapType.Midpoint);
                break;

            case Polyline pl:
                for (int i = 0; i < pl.NumberOfVertices; i++)
                {
                    var v = pl.Vertices[i];
                    yield return (new Vector2(v.X, v.Y), SnapType.Endpoint);
                    if (i + 1 < pl.NumberOfVertices)
                    {
                        var n = pl.Vertices[i + 1];
                        yield return (new Vector2((v.X + n.X) / 2, (v.Y + n.Y) / 2), SnapType.Midpoint);
                    }
                }
                break;

            case lcdb.Point p:
                yield return (p.position, SnapType.Endpoint);
                break;

            default:
                // 其它实体 (透镜/双胶合/标注…) 走通用 GetSnapPoints — 透镜的顶点/口径端/剖面角点等都成捕捉目标。
                System.Collections.Generic.List<lcdb.ObjectSnapPoint> sps;
                try { sps = e.GetSnapPoints() ?? new System.Collections.Generic.List<lcdb.ObjectSnapPoint>(); }
                catch { sps = new System.Collections.Generic.List<lcdb.ObjectSnapPoint>(); }
                foreach (var sp in sps)
                    yield return (sp.position, MapSnapMode(sp.type));
                break;
        }
    }

    private static SnapType MapSnapMode(lcdb.ObjectSnapMode m) => m switch
    {
        lcdb.ObjectSnapMode.Center => SnapType.Center,
        lcdb.ObjectSnapMode.Mid => SnapType.Midpoint,
        _ => SnapType.Endpoint,
    };
}

[Flags]
public enum SnapType
{
    None = 0,
    Endpoint = 1,     // 端点 (绿方块)
    Midpoint = 2,     // 中点 (黄三角)
    Center = 4,       // 圆心/弧心 (蓝圆)
    Intersection = 8, // 交点 (红 X) — 未实现
    Nearest = 16      // 实体边上的最近点 (紫菱形) — 用于标记/注释
}

public sealed record SnapResult(Vector2 Point, SnapType Type, Entity Source);
