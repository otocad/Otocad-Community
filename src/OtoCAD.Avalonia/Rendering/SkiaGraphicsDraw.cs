using LitMath;
using OtoCAD;
using SkiaSharp;
using System;
using System.Drawing;

namespace OtoCAD.Avalonia.Rendering;

/// <summary>
/// SkiaSharp 实现的 IGraphicsDraw — POC-02 的核心策略.
///
/// 通过实现 lcinterface 的 IGraphicsDraw 接口, **所有 lcdb 实体的 Draw 方法
/// (Line / Circle / Arc / CoatingMark / 其它 27+ Mark) 自动获得 SkiaSharp 渲染能力**,
/// 无需为每个实体类型写 type switch.
///
/// 这是 POC-02 验证 "业务层可复用" 的最强证据:
/// 现有 Entity.Draw(IGraphicsDraw) 方法零修改即可在 Avalonia 中使用.
/// </summary>
public sealed class SkiaGraphicsDraw : IGraphicsDraw, System.IDisposable
{
    private readonly SKCanvas _canvas;
    private readonly SKPaint _paint;
    private readonly float _modelScale;  // 用于保持 1px 线宽

    /// <summary>
    /// 进程级 CJK 字体缓存. SkiaSharp 在 Windows 上 SKTypeface.FromFamilyName
    /// 拿不到字体时返回的 typeface 不含 CJK 字形, 导致"文本"→豆腐块.
    /// 通过 SKFontManager.MatchCharacter('文') 强制找一个含 CJK 字形的 typeface,
    /// 即使用户系统未安装"微软雅黑"也能 fallback 到任意 CJK 兼容字体.
    /// </summary>
    private static readonly SKTypeface _cjkTypeface = ResolveCjkTypeface();

    private static SKTypeface ResolveCjkTypeface()
    {
        var fm = SKFontManager.Default;
        // 1) 优先按家族名找常见 Windows / macOS / Linux CJK 字体
        foreach (var name in new[]
        {
            "Microsoft YaHei", "Microsoft YaHei UI", "微软雅黑",
            "SimHei", "黑体", "SimSun", "宋体",
            "Microsoft JhengHei", "PingFang SC", "Noto Sans CJK SC",
            "Source Han Sans SC", "WenQuanYi Micro Hei"
        })
        {
            var tf = fm.MatchFamily(name);
            if (tf is not null && tf.ContainsGlyph('文')) return tf;
        }
        // 2) 兜底: 按字符查找任意支持 CJK 的字体
        var match = fm.MatchCharacter('文');
        return match ?? SKTypeface.Default;
    }

    public SkiaGraphicsDraw(SKCanvas canvas, float modelScale, SKColor defaultColor)
    {
        _canvas = canvas;
        _modelScale = Math.Max(modelScale, 0.0001f);
        _paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.0f / _modelScale,
            Color = defaultColor,
        };
    }

    /// <summary>
    /// 当前颜色 (SkiaSharp 原生类型) — 画布 overlay / 默认色用. 供 CadCanvas 直接设 SKColor.
    /// </summary>
    public SKColor CurrentSkColor
    {
        get => _paint.Color;
        set => _paint.Color = value;
    }

    /// <summary>
    /// 当前颜色 (System.Drawing.Color) — 实现 IGraphicsDraw 扩展约定, 让 Entity.Draw 能按自身
    /// 显式颜色覆盖. 与 <see cref="CurrentSkColor"/> 共享同一 _paint.Color.
    /// </summary>
    public System.Drawing.Color CurrentColor
    {
        get
        {
            var c = _paint.Color;
            return System.Drawing.Color.FromArgb(c.Red, c.Green, c.Blue);
        }
        set => _paint.Color = new SKColor(value.R, value.G, value.B);
    }

    /// <summary>
    /// POC-02 默认 Normal 模式 — Monochrome 不在 POC 范围.
    /// </summary>
    public RenderMode CurrentMode => RenderMode.Normal;

    // -------- LineType (GB/T 13323-2009 双点画线 / 单点画线) --------

    private lcdb.LineType _currentLineType = lcdb.LineType.Solid;

    /// <summary>
    /// 当前线型. 设值会即时重建 _paint.PathEffect.
    /// 模型尺度补偿: dash 周期长度按 1/modelScale 缩放, 确保不同 zoom 下视觉一致.
    /// </summary>
    public lcdb.LineType CurrentLineType
    {
        get => _currentLineType;
        set
        {
            if (_currentLineType == value) return;
            _currentLineType = value;
            ApplyLineTypeToPaint();
        }
    }

    private void ApplyLineTypeToPaint()
    {
        var oldEffect = _paint.PathEffect;
        _paint.PathEffect = BuildPathEffect(_currentLineType, _modelScale);
        oldEffect?.Dispose();
    }

    /// <summary>
    /// 按 GB/T 17450 / ISO 128 大致比例构造 dash 图案.
    /// 单位为模型坐标, 通过 1/modelScale 把"像素感"转回模型尺寸.
    /// 图案值 (像素): 单位长度 ≈ 6px (DashDot 长划), 1.5px (短点), 间隙 3px.
    /// </summary>
    private static SKPathEffect? BuildPathEffect(lcdb.LineType lineType, float modelScale)
    {
        if (modelScale <= 0) modelScale = 1f;
        float u = 1f / modelScale;   // 像素→模型 缩放
        switch (lineType)
        {
            case lcdb.LineType.Dash:
                return SKPathEffect.CreateDash(new[] { 8f * u, 4f * u }, 0);
            case lcdb.LineType.Dot:
                return SKPathEffect.CreateDash(new[] { 1.5f * u, 3f * u }, 0);
            case lcdb.LineType.DashDot:
                // 长划 · 间隙 · 点 · 间隙
                return SKPathEffect.CreateDash(new[] { 10f * u, 3f * u, 1.5f * u, 3f * u }, 0);
            case lcdb.LineType.DashDotDot:
                // GB/T 13323-2009 §2.1: 光轴双点画线 — 长划 · 点 · 点
                return SKPathEffect.CreateDash(new[] { 10f * u, 3f * u, 1.5f * u, 3f * u, 1.5f * u, 3f * u }, 0);
            case lcdb.LineType.Solid:
            case lcdb.LineType.ByLayer:
            case lcdb.LineType.ByBlock:
            case lcdb.LineType.Custom:
            default:
                return null;  // 实线 / 未定义 — 清空 path effect
        }
    }

    // -------- 几何绘制 --------

    public void DrawFilledPolygon(System.Collections.Generic.IReadOnlyList<Vector2> points)
    {
        if (points is null || points.Count < 3) return;
        using var path = new SKPath();
        path.MoveTo((float)points[0].X, (float)points[0].Y);
        for (int i = 1; i < points.Count; i++)
            path.LineTo((float)points[i].X, (float)points[i].Y);
        path.Close();
        using var fill = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = _paint.Color,
            IsAntialias = true,
        };
        _canvas.DrawPath(path, fill);
    }

    public void DrawPoint(Vector2 endPoint)
    {
        var r = 2.0f / _modelScale;
        var fillPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = _paint.Color, IsAntialias = true };
        _canvas.DrawCircle((float)endPoint.X, (float)endPoint.Y, r, fillPaint);
        fillPaint.Dispose();
    }

    public void DrawLine(Vector2 startPoint, Vector2 endPoint)
        => _canvas.DrawLine(
            (float)startPoint.X, (float)startPoint.Y,
            (float)endPoint.X, (float)endPoint.Y,
            _paint);

    public void DrawLineDimension(Vector2 startPoint, Vector2 endPoint)
        => DrawLine(startPoint, endPoint);

    public void DrawXLine(Vector2 basePoint, Vector2 direction)
    {
        // 无限直线 — POC 用一个足够长的有限线段近似
        var dir = direction.normalized;
        const double L = 100000;
        var p1 = basePoint - dir * L;
        var p2 = basePoint + dir * L;
        DrawLine(p1, p2);
    }

    public void DrawRay(Vector2 basePoint, Vector2 direction)
    {
        var dir = direction.normalized;
        const double L = 100000;
        DrawLine(basePoint, basePoint + dir * L);
    }

    public void DrawCircle(Vector2 center, double radius)
        => _canvas.DrawCircle(
            (float)center.X, (float)center.Y, (float)radius, _paint);

    public void DrawEllipse(Vector2 center, double radiusX, double radiusY)
    {
        var oval = new SKRect(
            (float)(center.X - radiusX), (float)(center.Y - radiusY),
            (float)(center.X + radiusX), (float)(center.Y + radiusY));
        _canvas.DrawOval(oval, _paint);
    }

    public void DrawArc(Vector2 center, double radius, double startAngle, double endAngle)
    {
        // lcdb / IGraphicsDraw 注释: 逆时针 (CCW). SkiaSharp DrawArc 用 sweep 角度, 正数 = 顺时针 (在 Y 向下的屏幕坐标系)
        // 我们在 CadCanvas 中已经做了 Y 翻转 (Scale(s, -s)), 因此屏幕坐标系上 Y 向上 ≡ 数学坐标系
        // 此时 SkiaSharp 的 "顺时针" 在视觉上变成数学逆时针 — 与 lcdb 一致
        var rect = new SKRect(
            (float)(center.X - radius), (float)(center.Y - radius),
            (float)(center.X + radius), (float)(center.Y + radius));

        // SkiaSharp 角度单位是度, 0° 在 +X 正方向
        var startDeg = (float)(startAngle * 180.0 / Math.PI);
        var endDeg = (float)(endAngle * 180.0 / Math.PI);
        var sweepDeg = endDeg - startDeg;
        if (sweepDeg <= 0) sweepDeg += 360f;

        using var path = new SKPath();
        path.AddArc(rect, startDeg, sweepDeg);
        _canvas.DrawPath(path, _paint);
    }

    public void DrawRectangle(Vector2 position, double width, double height)
    {
        var rect = new SKRect(
            (float)position.X, (float)position.Y,
            (float)(position.X + width), (float)(position.Y + height));
        _canvas.DrawRect(rect, _paint);
    }

    public void DrawTriangle(Vector2 v1, Vector2 v2, Vector2 v3)
    {
        using var path = new SKPath();
        path.MoveTo((float)v1.X, (float)v1.Y);
        path.LineTo((float)v2.X, (float)v2.Y);
        path.LineTo((float)v3.X, (float)v3.Y);
        path.Close();
        // 实体三角形 — 接口语义是填充
        using var fill = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = _paint.Color,
            IsAntialias = true
        };
        _canvas.DrawPath(path, fill);
    }

    public void DrawQuadrilateral(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4)
    {
        using var path = new SKPath();
        path.MoveTo((float)v1.X, (float)v1.Y);
        path.LineTo((float)v2.X, (float)v2.Y);
        path.LineTo((float)v3.X, (float)v3.Y);
        path.LineTo((float)v4.X, (float)v4.Y);
        path.Close();
        using var fill = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = _paint.Color,
            IsAntialias = true
        };
        _canvas.DrawPath(path, fill);
    }

    public Vector2 DrawText(Vector2 position, string text, double height, string font, lcdb.TextAlignment textAlign, double angle)
    {
        if (string.IsNullOrEmpty(text)) return position;

        // 字体解析: 用户传 font 名 → 用其匹配, 失败/为空 → 用进程级 CJK 缓存
        SKTypeface? typeface = null;
        if (!string.IsNullOrEmpty(font))
        {
            typeface = SKFontManager.Default.MatchFamily(font);
            // 用户指定的字体若不含 CJK, 但文本含中文 → 用 CJK 缓存覆盖
            if (typeface is null || (HasNonAscii(text) && !typeface.ContainsGlyph('文')))
                typeface = _cjkTypeface;
        }
        typeface ??= _cjkTypeface;

        using var textPaint = new SKPaint
        {
            Color = _paint.Color,
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            TextSize = (float)height,
            Typeface = typeface,
        };

        var x = (float)position.X;
        var y = (float)position.Y;

        // CadCanvas 用了 Y 翻转 (Scale(s, -s)), 文字会上下镜像 — 局部反翻转
        _canvas.Save();
        _canvas.Translate(x, y);
        _canvas.Scale(1, -1);
        if (Math.Abs(angle) > 1e-9)
            _canvas.RotateRadians(-(float)angle);
        _canvas.DrawText(text, 0, 0, textPaint);
        _canvas.Restore();

        // 返回近似的文本占用宽度终点 (POC 不强求精确)
        var width = textPaint.MeasureText(text);
        return position + new Vector2(width, 0);
    }

    public Vector2 DrawText(Vector3 position, string text, double height, string font, lcdb.TextAlignment textAlign, double angle)
        => DrawText(new Vector2(position.X, position.Y), text, height, font, textAlign, angle);

    private static bool HasNonAscii(string s)
    {
        foreach (var ch in s) if (ch > 127) return true;
        return false;
    }

    public void Dispose()
    {
        _paint.PathEffect?.Dispose();
        _paint.Dispose();
    }
}
