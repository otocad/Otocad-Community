using System.Collections.Generic;

namespace lcdb.Optic;

/// <summary>球面表面 (含 R=∞ 平面退化). 复用 OpticSurfaceGeometry 实现.</summary>
public sealed class SphericalSurface : OpticalSurface
{
    public override double Sag(double semiAperture)
        => OpticSurfaceGeometry.ComputeSag(Radius, semiAperture);

    public override void AppendRenderEntities(
        List<Entity> dst, double apexX, double apexY,
        double semiAperture, lcdb.Colors.Color color)
    {
        var ent = OpticSurfaceGeometry.MakeSurface(apexX, apexY, Radius, semiAperture, color);
        if (ent is not null) dst.Add(ent);
    }

    public override OpticalSurface Clone() => new SphericalSurface { Radius = Radius, SemiAperture = SemiAperture };
}
