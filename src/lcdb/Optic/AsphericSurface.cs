using System;
using System.Collections.Generic;
using LitMath;

namespace lcdb.Optic;

/// <summary>
/// 标准非球面 (Even Asphere) — Zemax / Code V / OSLO 通用形式.
///
/// 矢高公式 (sag z, h 为半径处):
///
///     z(h) = c·h² / (1 + √(1 - (1+k)·c²·h²))  +  Σ a_i · h^(2i+4)
///                                                  i=0..N
///
/// 其中:
///   c = 1/R   (近轴曲率, R 为基础曲率半径 = 顶点处球面半径)
///   k = ConicConstant  (圆锥常数: -1=抛物面, &lt;-1=双曲面, =0=球面, &gt;0=椭球, 在 (-1,0) 之间也为椭球)
///   a_i = EvenCoefficients[i]  (a4, a6, a8, ...)  非球面系数, 单位 mm^-(2i+3)
///
/// 当 k=0 且 EvenCoefficients 空时退化为球面.
/// </summary>
public sealed class AsphericSurface : OpticalSurface
{
    /// <summary>圆锥常数 k. 默认 0 (球面).</summary>
    public double ConicConstant { get; set; } = 0.0;

    /// <summary>偶次项系数 [a4, a6, a8, ...]. 默认为空 (无高次项).</summary>
    public double[] EvenCoefficients { get; set; } = Array.Empty<double>();

    public override double Sag(double semiAperture)
    {
        if (double.IsInfinity(Radius) || double.IsNaN(Radius)) return 0;

        double R = Radius;
        double c = 1.0 / R;
        double h = Math.Abs(semiAperture);
        double hh = h * h;

        // 球面/圆锥项: c·h² / (1 + √(1 - (1+k)·c²·h²))
        double denomInside = 1.0 - (1.0 + ConicConstant) * c * c * hh;
        if (denomInside < 0) denomInside = 0;  // 防 sqrt 负数 (无效区域)
        double z = c * hh / (1.0 + Math.Sqrt(denomInside));

        // 偶次项: Σ a_i · h^(2i+4) (i=0 → h^4, i=1 → h^6, ...)
        for (int i = 0; i < EvenCoefficients.Length; i++)
        {
            z += EvenCoefficients[i] * Math.Pow(h, 2 * i + 4);
        }

        return z;
    }

    public override void AppendRenderEntities(
        List<Entity> dst, double apexX, double apexY,
        double semiAperture, lcdb.Colors.Color color)
    {
        if (semiAperture <= 0) return;

        // 离散为 N 段 polyline (从 -h 到 +h, 顶点在 h=0)
        const int Segments = 48;
        Vector2? prev = null;
        for (int i = -Segments; i <= Segments; i++)
        {
            double h = semiAperture * (double)i / Segments;
            double z = Sag(Math.Abs(h)) * Math.Sign(Math.Abs(h) > 0 ? 1 : 1);
            // 注: Sag 已带符号 (近轴 c=1/R, R>0 → z>0 即顶点向 +X 偏移)
            var pt = new Vector2(apexX + Sag(Math.Abs(h)), apexY + h);
            if (prev is { } p) dst.Add(new Line(p, pt) { color = color });
            prev = pt;
        }
    }

    public override OpticalSurface Clone() => new AsphericSurface
    {
        Radius = Radius,
        SemiAperture = SemiAperture,
        ConicConstant = ConicConstant,
        EvenCoefficients = (double[])EvenCoefficients.Clone(),
    };

    /// <summary>把现有 SphericalSurface 转 AsphericSurface (基础半径相同, k=0 无高次项).</summary>
    public static AsphericSurface FromSphere(SphericalSurface s) => new()
    {
        Radius = s.Radius,
        ConicConstant = 0,
        EvenCoefficients = Array.Empty<double>(),
    };

    /// <summary>抛物面: k=-1.</summary>
    public static AsphericSurface Parabola(double radius) => new()
    {
        Radius = radius,
        ConicConstant = -1.0,
    };
}
