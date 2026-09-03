using System.Collections.Generic;
using lcdb;
using lcdb.Optic;
using OtoCAD.Avalonia.Commands;

namespace OtoCAD.Avalonia.Templating;

/// <summary>单透镜 (OpticalLens) 适配器 — 复用 IsoSingleLensSheet.Build.</summary>
public sealed class LensPartAdapter : IPartAdapter
{
    public bool CanHandle(Entity part) => part is OpticalLens;

    public List<Entity> BuildSheet(Entity part, SheetMeta meta)
    {
        var lens = (OpticalLens)part;
        var spec = IsoSingleLensSheet.SpecFromLens(lens);
        ApplyMeta(spec, meta);
        IsoSingleLensSheet.ApplyLayout(spec);   // 自动选纸 + scale-to-fit (覆盖 meta.Scale 标签)
        return IsoSingleLensSheet.Build(spec);
    }

    /// <summary>把 SheetMeta 覆盖到 Spec 标题栏 (空值保留 Spec/适配器的默认).</summary>
    private static void ApplyMeta(IsoSingleLensSheet.Spec s, SheetMeta m)
    {
        if (!string.IsNullOrEmpty(m.ProjectPart)) s.ProjectPart = m.ProjectPart;
        if (!string.IsNullOrEmpty(m.DrawnBy)) s.DrawnBy = m.DrawnBy;
        if (!string.IsNullOrEmpty(m.ApprovedBy)) s.ApprovedBy = m.ApprovedBy;
        if (!string.IsNullOrEmpty(m.DrawDate)) s.DrawDate = m.DrawDate;
        if (!string.IsNullOrEmpty(m.Scale)) s.DrawScale = m.Scale;
        if (!string.IsNullOrEmpty(m.Sheet)) s.Sheet = m.Sheet;
        if (!string.IsNullOrEmpty(m.Rev)) s.Rev = m.Rev;
        if (!string.IsNullOrEmpty(m.Notes)) s.Notes = m.Notes;
    }
}
