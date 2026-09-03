namespace OtoCAD.Avalonia.Templating;

/// <summary>
/// 一张标准图纸的"非几何"元数据 (标题栏字段 + 注释). 几何/规格由零件实体 + 适配器推导.
/// Phase 3 起可由 Config/optical-templates.json 填充默认值.
/// </summary>
public sealed class SheetMeta
{
    public string ProjectPart = "";        // 空 = 适配器按零件自动取名
    public string DrawnBy = "OtoCAD";
    public string ApprovedBy = "—";
    public string DrawDate = "";            // 空 = 用今天
    public string Scale = "1:1";
    public string Sheet = "1 / 1";
    public string Rev = "A";
    public string Notes = "";               // 空 = 适配器给默认注释

    public static SheetMeta Default() => new();
}
