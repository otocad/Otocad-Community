using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using LitMath;

namespace lcdb.Optic;

/// <summary>
/// 光学表面 — OpticalLens / CementedLens 等所有光学元件的表面定义.
///
/// 子类:
/// - <see cref="SphericalSurface"/>  球面 (含 R=∞ 平面退化)
/// - <see cref="AsphericSurface"/>   非球面 (圆锥常数 k + 偶次项)
/// - 未来: FreeformSurface / CylindricalSurface / DiffractiveSurface ...
///
/// 子类必须可 JSON 多态序列化 (V4 storage). 添加新子类时:
/// 1. 子类加 [JsonDerivedType(typeof(X), "X")] 到本类 (见下 attribute)
/// 2. 实现 Sag() + Clone() + 渲染图元生成
///
/// 符号约定 (与 ISO 10110-1 / Zemax 一致): R&gt;0 中心在表面右侧 (光行进方向).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(SphericalSurface), "spherical")]
[JsonDerivedType(typeof(AsphericSurface),  "aspheric")]
public abstract class OpticalSurface
{
    /// <summary>
    /// 基础曲率半径 (带符号; R&gt;0 中心在右; R=∞ 平面).
    /// 非球面也有基础 Radius (= 顶点处近轴曲率半径).
    /// </summary>
    public double Radius { get; set; } = double.PositiveInfinity;

    /// <summary>
    /// 本表面净口径半径 (mm). &gt;0 时表面只弯曲到此半口径, 外侧到镜片机械外径画平肩;
    /// 0 = 未单独指定, 用镜片整体净口径 (Diameter/2). 来自 Zemax 逐面 DIAM (Semi-Diameter).
    /// </summary>
    public double SemiAperture { get; set; } = 0.0;

    /// <summary>
    /// 表面下垂量 sag(h): 顶点到 (半口径 h 处) 的 X 偏移 (带符号).
    /// 球面: ISO 10110-1 sag = sign(R) × (|R| - √(R²-h²))
    /// 非球面: 球面项 + 偶次项 (见 AsphericSurface)
    /// </summary>
    public abstract double Sag(double semiAperture);

    /// <summary>
    /// 把表面渲染为 0~N 个 Entity (Arc / Line / Polyline 等), 加到 dst 列表.
    /// apexX/apexY = 表面顶点的模型坐标 (Y 通常 = lens.Position.Y).
    /// </summary>
    public abstract void AppendRenderEntities(
        List<Entity> dst, double apexX, double apexY,
        double semiAperture, lcdb.Colors.Color color);

    /// <summary>深拷贝.</summary>
    public abstract OpticalSurface Clone();

    // -------- 工厂便利 --------

    /// <summary>用半径快速构 SphericalSurface (含 ∞ 平面).</summary>
    public static SphericalSurface Sphere(double radius) => new() { Radius = radius };

    /// <summary>平面 (R = +∞).</summary>
    public static SphericalSurface Flat() => new() { Radius = double.PositiveInfinity };
}
