#nullable enable
using System.IO;
using System.Text.Json;

namespace lcdb.DrawingFrame;

/// <summary>
/// 数据驱动图框的定义 — 纯数据 (模板系统 Phase 2a, 设计稿 docs/开发文档/图框数据驱动设计.md).
///
/// 整张图框 = 边框 + 自底向上堆叠的水平带 (<c>notes</c> / <c>title-grid</c> / <c>spec-table</c>)
/// + 绝对定位 furniture (投影符号 / 其余粗糙度). 由 <see cref="DataDrivenFrame"/> 通用渲染;
/// 定义随实体内嵌进 .otocad — 换机器打开不依赖本机 Config/Frames 预设文件.
///
/// JSON 键 camelCase (Config/Frames/*.json 与内嵌同构), 解析大小写不敏感、允许注释与尾逗号.
/// 只描述"纸上可见"的结构; 产生/验收它的规则 (出图档案/清单) 不在此.
/// </summary>
public sealed class FrameDefinition
{
    public const string KindNotes = "notes";
    public const string KindTitleGrid = "title-grid";
    public const string KindSpecTable = "spec-table";
    public const string FurnitureProjectionSymbol = "projection-symbol";
    public const string FurnitureGeneralRoughness = "general-roughness";
    public const string PaperAuto = "auto";

    /// <summary>图框名 (出图档案 <c>frame:</c> 声明比对用; 大小写不敏感).</summary>
    public string Name { get; set; } = "";

    /// <summary>显示标题.</summary>
    public string Title { get; set; } = "";

    /// <summary>国标依据 / 说明 (JSON 无注释, 写这里).</summary>
    public string Note { get; set; } = "";

    /// <summary>"auto" = 由布局引擎定纸; 或显式 A4P / A4L / A3L … (A0–A4 × P 纵 / L 横).</summary>
    public string Paper { get; set; } = PaperAuto;

    public FrameBorder Border { get; set; } = new();

    /// <summary>水平带, 自底向上: 第 0 个在最下.</summary>
    public List<FrameBand> Bands { get; set; } = new();

    public List<FrameFurniture> Furniture { get; set; } = new();

    public FrameStyle Style { get; set; } = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static FrameDefinition FromJson(string json)
        => JsonSerializer.Deserialize<FrameDefinition>(json, JsonOpts)
           ?? throw new JsonException("图框定义 JSON 为空");

    public static FrameDefinition Load(string path) => FromJson(File.ReadAllText(path));

    public string ToJson() => JsonSerializer.Serialize(this, JsonOpts);

    /// <summary>深拷贝 (定义是纯数据, JSON 往返即可).</summary>
    public FrameDefinition Clone() => FromJson(ToJson());

    /// <summary>第一个某类带 (无则 null).</summary>
    public FrameBand? FirstBand(string kind) => Bands.FirstOrDefault(b => b.Kind == kind);

    /// <summary>
    /// 所有命名字段 (notes 字段 + title-grid 带键的格 + 带键的 furniture), 按出现顺序.
    /// 这就是"命名字段集"的唯一真值表: 渲染器认得的键 = 定义里声明的键, 不另设映射.
    /// </summary>
    public IEnumerable<FrameNamedField> NamedFields()
    {
        foreach (var b in Bands)
        {
            if (b.Kind == KindNotes)
            {
                foreach (var f in b.Fields) yield return new FrameNamedField(f.Key, f.Label, f.Default, f.BagKeys);
            }
            else if (b.Kind == KindTitleGrid)
            {
                foreach (var c in b.Cells)
                    if (!string.IsNullOrEmpty(c.Key)) yield return new FrameNamedField(c.Key, c.Label, c.Default, c.BagKeys);
            }
        }
        foreach (var f in Furniture)
            if (!string.IsNullOrEmpty(f.Key)) yield return new FrameNamedField(f.Key, f.Label, f.Default, f.BagKeys);
    }

    /// <summary>结构校验, 返回错误列表 (空 = 合法). 不做国标合规判断 — 那是出图清单的事.</summary>
    public List<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name)) errors.Add("name 不能为空");
        if (!string.Equals(Paper, PaperAuto, StringComparison.OrdinalIgnoreCase)
            && !DrawingFrameTemplates.TryParsePaper(Paper, out _, out _))
            errors.Add($"paper '{Paper}' 无法识别 (auto 或 A0–A4 + P/L, 如 A4P)");
        if (Border.Margin < 0) errors.Add("border.margin 不能为负");
        if (Bands.Count == 0) errors.Add("至少需要一个带 (bands)");

        int specTables = 0;
        for (int i = 0; i < Bands.Count; i++)
        {
            var b = Bands[i];
            string where = $"bands[{i}]";
            switch (b.Kind)
            {
                case KindNotes:
                    if (b.Fields.Count == 0) errors.Add($"{where} notes 至少一个字段");
                    for (int k = 0; k < b.Fields.Count; k++)
                    {
                        if (string.IsNullOrWhiteSpace(b.Fields[k].Key)) errors.Add($"{where}.fields[{k}] key 不能为空");
                        if (k > 0 && b.Fields[k].Width <= 0) errors.Add($"{where}.fields[{k}] 非首字段须给 width (从右侧起排)");
                    }
                    break;

                case KindTitleGrid:
                {
                    if (b.Cells.Count == 0) errors.Add($"{where} title-grid 至少一个格");
                    int rows = Math.Max(1, b.Rows);
                    int cols = b.EffectiveCols();
                    for (int k = 0; k < b.Cells.Count; k++)
                    {
                        var c = b.Cells[k];
                        if (c.Col < 0 || c.W < 1 || c.Col + c.W > cols) errors.Add($"{where}.cells[{k}] col/w 越出 cols={cols}");
                        if (c.Row < 0 || c.H < 1 || c.Row + c.H > rows) errors.Add($"{where}.cells[{k}] row/h 越出 rows={rows}");
                    }
                    break;
                }

                case KindSpecTable:
                    specTables++;
                    if (b.Columns.Count == 0) errors.Add($"{where} spec-table 至少一列");
                    for (int k = 0; k < b.Columns.Count; k++)
                        if (string.IsNullOrWhiteSpace(b.Columns[k].Title)) errors.Add($"{where}.columns[{k}] title 不能为空");
                    break;

                default:
                    errors.Add($"{where} 未知带种类 '{b.Kind}' (可用: notes / title-grid / spec-table)");
                    break;
            }
        }
        if (specTables > 1) errors.Add("最多一个 spec-table 带 (属性区列/行寻址按单表)");

        for (int i = 0; i < Furniture.Count; i++)
        {
            var f = Furniture[i];
            switch (f.Kind)
            {
                case FurnitureProjectionSymbol:
                    if (f.Angle != "first" && f.Angle != "third") errors.Add($"furniture[{i}] angle 须为 first / third");
                    break;
                case FurnitureGeneralRoughness:
                    if (string.IsNullOrWhiteSpace(f.Key)) errors.Add($"furniture[{i}] general-roughness 须给 key");
                    break;
                default:
                    errors.Add($"furniture[{i}] 未知种类 '{f.Kind}' (可用: projection-symbol / general-roughness)");
                    break;
            }
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var nf in NamedFields())
            if (!seen.Add(nf.Key)) errors.Add($"命名字段键重复: '{nf.Key}'");
        return errors;
    }
}

/// <summary>命名字段 (键 / 显示标签 / 默认值 / 属性包别名键).</summary>
public readonly record struct FrameNamedField(string Key, string Label, string Default, List<string> BagKeys);

public sealed class FrameBorder
{
    /// <summary>图框边距 (mm, 四边等距).</summary>
    public double Margin { get; set; } = 8;
}

public sealed class FrameStyle
{
    public double CellTextHeight { get; set; } = 2.5;
    public double HeaderTextHeight { get; set; } = 2.5;
    public double LabelTextHeight { get; set; } = 1.7;
    public double ValueTextHeight { get; set; } = 2.8;
    public double LineWeight { get; set; } = 0.25;
}

public sealed class FrameBand
{
    /// <summary>notes / title-grid / spec-table.</summary>
    public string Kind { get; set; } = "";

    /// <summary>带总高 (mm). notes 默认 12; title-grid 默认 rows × rowHeight; spec-table 忽略 (按行数算).</summary>
    public double Height { get; set; }

    /// <summary>行高: spec-table 默认 6; title-grid 未给 height 时用 (默认 16).</summary>
    public double RowHeight { get; set; }

    /// <summary>title-grid 行数 (行 0 在顶).</summary>
    public int Rows { get; set; } = 1;

    /// <summary>title-grid 横向网格单位数 (0 = 取格的 max(col+w)).</summary>
    public int Cols { get; set; }

    /// <summary>notes: 是否画带的边框盒与字段分隔线 (GB 风格注释无盒).</summary>
    public bool Box { get; set; } = true;

    /// <summary>notes 字段.</summary>
    public List<FrameField> Fields { get; set; } = new();

    /// <summary>title-grid 格.</summary>
    public List<FrameCell> Cells { get; set; } = new();

    /// <summary>spec-table 列.</summary>
    public List<FrameColumn> Columns { get; set; } = new();

    /// <summary>title-grid 实际网格单位数.</summary>
    public int EffectiveCols()
    {
        if (Cols > 0) return Cols;
        int max = 0;
        foreach (var c in Cells) max = Math.Max(max, c.Col + c.W);
        return Math.Max(1, max);
    }
}

/// <summary>notes 带字段: 首字段占剩余宽度; 其余按 width 从右侧起排 (盒).</summary>
public sealed class FrameField
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public double Width { get; set; }
    public string Default { get; set; } = "";
    /// <summary>属性包 title 簇里的别名键 (定义键在包里没有时按序取第一个存在的; 回写到命中的键).</summary>
    public List<string> BagKeys { get; set; } = new();
}

/// <summary>title-grid 格 (网格单位定位). key 为空 = 纯标签格 (表头 / 角色名), 不可编辑.</summary>
public sealed class FrameCell
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public int Col { get; set; }
    public int W { get; set; } = 1;
    public int Row { get; set; }
    public int H { get; set; } = 1;
    public string Default { get; set; } = "";

    /// <summary>值字高覆盖 (0 = style.valueTextHeight; 纯标签格 0 = 2.0).</summary>
    public double TextHeight { get; set; }

    /// <summary>"left" (默认) / "center".</summary>
    public string Align { get; set; } = "left";

    /// <summary>属性包 title 簇里的别名键 (同 <see cref="FrameField.BagKeys"/>).</summary>
    public List<string> BagKeys { get; set; } = new();
}

public sealed class FrameColumn
{
    public string Title { get; set; } = "";

    /// <summary>新建图框时播种的行 (自描述值串, 代号内嵌, 同现状).</summary>
    public List<string> Seed { get; set; } = new();

    /// <summary>与 seed 对齐的行键 (供属性包 / 清单按键寻址; 可短 / 可空).</summary>
    public List<string> SeedKeys { get; set; } = new();
}

public sealed class FrameFurniture
{
    /// <summary>projection-symbol / general-roughness.</summary>
    public string Kind { get; set; } = "";

    /// <summary>projection-symbol: first / third.</summary>
    public string Angle { get; set; } = "first";

    /// <summary>锚点: notes-right / top-right / top-left / bottom-right / bottom-left / custom (x,y 相对图框内左下角).</summary>
    public string At { get; set; } = "notes-right";

    public double X { get; set; }
    public double Y { get; set; }

    /// <summary>尺寸 (投影符号 = 大圆半径, 默认 1.76).</summary>
    public double Size { get; set; }

    /// <summary>general-roughness: 值的命名字段键.</summary>
    public string Key { get; set; } = "";

    public string Label { get; set; } = "";
    public string Default { get; set; } = "";

    /// <summary>属性包 title 簇里的别名键 (同 <see cref="FrameField.BagKeys"/>).</summary>
    public List<string> BagKeys { get; set; } = new();
}
