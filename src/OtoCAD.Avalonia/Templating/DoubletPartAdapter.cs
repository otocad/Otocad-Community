using System.Collections.Generic;
using lcdb;
using lcdb.Optic;
using OtoCAD.Avalonia.Commands;

namespace OtoCAD.Avalonia.Templating;

/// <summary>双胶合 (CementedLens) 适配器 — 复用 IsoSingleLensSheet.BuildDoublet.</summary>
public sealed class DoubletPartAdapter : IPartAdapter
{
    public bool CanHandle(Entity part) => part is CementedLens;

    public List<Entity> BuildSheet(Entity part, SheetMeta meta)
    {
        var c = (CementedLens)part;
        string proj = string.IsNullOrEmpty(meta.ProjectPart)
            ? $"{c.Material1}+{c.Material2} Doublet"
            : meta.ProjectPart;
        string notes = string.IsNullOrEmpty(meta.Notes)
            ? "Cemented doublet; protect cement joint from solvents."
            : meta.Notes;
        return IsoSingleLensSheet.BuildDoublet(c, proj, meta.DrawnBy, meta.Scale, notes);
    }
}
