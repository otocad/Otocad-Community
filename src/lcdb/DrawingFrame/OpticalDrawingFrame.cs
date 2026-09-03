using System.Collections.Generic;
using LitMath;

namespace lcdb.DrawingFrame;

/// <summary>
/// 光学零件图框 (GB/T 13323-2009 §1.2 / §2 合规).
///
/// 在基础 <see cref="DrawingFrame"/> (标题栏 / 外框 / 内框) 之上额外渲染两张表:
/// - **对材料的要求** (Material Requirements) — 玻璃牌号 / n_d / v_d / 双折射 / 气泡 / 不均匀性
/// - **对零件的要求** (Part Requirements)    — 面形 / 表面疵病 / 中心偏差 / 样板精度 / 表面结构
///
/// 这两张表是工厂收图判断 "图纸合规" 的硬要求 — 不带表的图 = 不能投产.
///
/// 表格布局:
/// 在标题栏左侧 (内框区域, 标题栏宽度之外) 上下叠放两个表.
/// 每个表 6 行 2 列 (字段名 | 值), 默认值为 GB/T 13323-2009 §3 推荐值.
/// </summary>
public sealed class OpticalDrawingFrame : DrawingFrame, IPropertyZoneFrame, IPropertyExport
{
    public override string className => "OpticalDrawingFrame";

    // -------- 对材料的要求 --------

    /// <summary>玻璃牌号 (例如 N-BK7 / F2 / H-K9L).</summary>
    public string GlassMaterial { get; set; } = "N-BK7";

    /// <summary>折射率 n_d (含允差). 显示如 "1.5168 ±0.0005".</summary>
    public string Nd { get; set; } = "1.5168 ±0.0005";

    /// <summary>阿贝数 v_d (含允差). 显示如 "64.17 ±0.5%".</summary>
    public string Vd { get; set; } = "64.17 ±0.5%";

    /// <summary>双折射 (GB/T 7962.1) — 应力双折射 OPD/cm, 例: "0/20".</summary>
    public string Birefringence { get; set; } = "0/20";

    /// <summary>气泡度 (GB/T 7962.2) — 1/N×A, 例: "1/3×0.16".</summary>
    public string BubbleClass { get; set; } = "1/3×0.16";

    /// <summary>不均匀性 + 条纹 (GB/T 7962.4) — 2/Cls;StripeClass, 例: "2/2;2".</summary>
    public string Inhomogeneity { get; set; } = "2/2;2";

    // -------- 对零件的要求 --------

    /// <summary>面形精度 (ISO 10110-5) — 3/N(ΔN), 例: "3/3(0.5)"。注: 面形是 3/, 4/ 是中心偏差。</summary>
    public string FormError { get; set; } = "3/3(0.5)";

    /// <summary>表面疵病 (ISO 10110-7) — 5/B, 例: "5/3×0.1".</summary>
    public string SurfaceImperf { get; set; } = "5/3×0.1";

    /// <summary>中心偏差 (ISO 10110-6) — 例: "C=3'".</summary>
    public string CenteringTol { get; set; } = "C=3'";

    /// <summary>样板精度 (ΔR) — 例: "ΔR=A".</summary>
    public string SampleGrade { get; set; } = "ΔR=A";

    /// <summary>表面结构(纹理)— G/P 形式(ISO 10110-8 / GB13323 附录C, 无 slash 代号; 旧值 "8/P3" 的 8/ 系凭空).</summary>
    public string SurfaceTexture { get; set; } = "P3";

    /// <summary>(选) 激光损伤阈值 — 6/Hth, J/cm². 留空则不显示该行.</summary>
    public string LaserDamage { get; set; } = "";

    // -------- 关联透镜 (T6: 自动填表) --------

    /// <summary>
    /// 关联到该图框的透镜 (OpticalLens / CementedLens). 关联后双表的材料行
    /// **在渲染时** 用透镜数据替代 — 但**不会修改持久化字段** (GlassMaterial /
    /// Nd / Vd). 取消关联后字段回到用户原值, 不会丢失.
    ///
    /// 注: 用 Entity 引用而非 ID — V4 JSON 反序列化时 LinkedLens 会丢 (P0
    /// 不持久化关联), 用户重选即可.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public Entity? LinkedLens { get; set; }

    /// <summary>
    /// 从 LinkedLens 提取玻璃材料/n/v 作为渲染时的 *override* — 不写回字段.
    /// 返回 (材料名, n_d, v_d), 任一为 null 表示"用持久化字段值".
    /// </summary>
    private (string? Material, string? Nd, string? Vd) GetLinkedOverride()
    {
        switch (LinkedLens)
        {
            case lcdb.Optic.OpticalLens ol:
                return (
                    ol.MaterialName,
                    ol.RefractiveIndex.ToString("F4", System.Globalization.CultureInfo.InvariantCulture),
                    ol.AbbeNumber.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            case lcdb.Optic.CementedLens cl:
                return (
                    $"{cl.Material1} + {cl.Material2}",
                    $"{cl.Nd1:F4} / {cl.Nd2:F4}",
                    $"{cl.Vd1:F2} / {cl.Vd2:F2}");
            default:
                return (null, null, null);
        }
    }

    // -------- 表格布局参数 --------

    /// <summary>每行高 (mm).</summary>
    public double TableRowHeight { get; set; } = 6.0;

    /// <summary>(旧两表布局遗留, 三列式不再使用; 保留供旧文件反序列化.)</summary>
    public double TableLabelWidth { get; set; } = 40.0;
    /// <summary>(旧两表布局遗留, 三列式不再使用.)</summary>
    public double TableValueWidth { get; set; } = 60.0;

    /// <summary>三列表距标题栏左侧间距 (mm).</summary>
    public double TableGap { get; set; } = 4.0;

    // -------- 属性区: 三列式 (前表面 | 材料 | 后表面, 镜像 ISO) --------

    /// <summary>
    /// 属性区三列(前表面 / 材料 / 后表面)。每行是自描述的值字符串(GB 代号内嵌)。
    /// 为空时由 <see cref="EnsureColumns"/> 从下方 11 个 GB 字段播种(也吃 LinkedLens 材料 override)。
    /// </summary>
    public List<IsoSpecColumn> Columns { get; set; } = new();

    /// <summary>列为空时(新建 / 旧文件无 Columns)从 GB 字段播种三列。已有则不动。</summary>
    private void EnsureColumns()
    {
        if (Columns.Count > 0) return;
        var (lm, ln, lv) = GetLinkedOverride();
        var part = new List<string> { FormError, SurfaceImperf, CenteringTol, SampleGrade, SurfaceTexture };
        if (!string.IsNullOrWhiteSpace(LaserDamage)) part.Add(LaserDamage);
        Columns = new List<IsoSpecColumn>
        {
            new IsoSpecColumn { Title = "前表面", Rows = new List<string>(part) },
            new IsoSpecColumn { Title = "材料", Rows = new List<string>
            {
                lm ?? GlassMaterial,
                "n_d " + (ln ?? Nd),
                "v_d " + (lv ?? Vd),
                Birefringence,
                BubbleClass,
                Inhomogeneity,
            } },
            new IsoSpecColumn { Title = "后表面", Rows = new List<string>(part) },
        };
    }

    // -------- IPropertyZoneFrame (属性区交互编辑, 与 ISO 同路径) --------

    public PropertyCellHit? HitTestCell(Vector2 modelPoint) => HitTestCellCore(modelPoint);
    public string GetCellText(PropertyCellHit cell)
        => string.IsNullOrEmpty(cell.FieldKey) ? GetColumnsCellText(Columns, cell) : GetTitleField(cell.FieldKey);
    public void SetCellText(PropertyCellHit cell, string text)
    {
        if (string.IsNullOrEmpty(cell.FieldKey)) { SetColumnsCellText(Columns, cell, text); return; }
        if (SetTitleField(cell.FieldKey, text ?? "")) InvalidateRenderCache();
    }
    public int AddRow(int colIndex, string text = "") => AddColumnsRow(Columns, colIndex, text);
    public int InsertRow(int colIndex, int rowIndex, string text) => InsertColumnsRow(Columns, colIndex, rowIndex, text);
    public bool RemoveRow(int colIndex, int rowIndex) => RemoveColumnsRow(Columns, colIndex, rowIndex);
    public PropertyCellHit? GetCell(int colIndex, int rowIndex) => FindCell(colIndex, rowIndex);
    public System.Collections.Generic.IReadOnlyList<string> GetZoneRows(int colIndex) => GetColumnsRows(Columns, colIndex);

    // -------- 属性包驱动 (落地分期 P1: 属性驱动渲染) --------

    /// <summary>
    /// 用属性包填本图框 —— 属性驱动渲染入口。标题栏簇 (key="title") 填命名字段;
    /// 三列属性区按 surface-front | material | surface-back 三簇重建 (行=各簇属性 Value)。
    /// 调用后 Columns 非空 → <see cref="EnsureColumns"/> 不再播种, 内容全来自包。
    /// 未调此法的旧用法照旧走默认播种 (不破坏现有渲染)。
    /// </summary>
    public void ApplyBag(lcdb.Drawing.DrawingPropertyBag bag)
    {
        if (bag is null) return;

        var title = bag.Cluster("title");
        if (title != null)
            foreach (var p in title.Properties) SetTitleField(p.Key, p.Value);

        Columns = new List<IsoSpecColumn>
        {
            ColumnFromCluster(bag, "surface-front", "前表面"),
            ColumnFromCluster(bag, "material", "材料"),
            ColumnFromCluster(bag, "surface-back", "后表面"),
        };
        InvalidateRenderCache();
    }

    private static IsoSpecColumn ColumnFromCluster(lcdb.Drawing.DrawingPropertyBag bag, string key, string fallbackLabel)
    {
        var c = bag.Cluster(key);
        var col = new IsoSpecColumn { Title = c?.Label ?? fallbackLabel };
        if (c != null) foreach (var p in c.Properties) col.Rows.Add(p.Value);
        return col;
    }

    /// <summary>图框暴露扁平 key→value (标题栏命名字段)。product_number 即 DrawingNumber。供清单按键查 / 重出回填。</summary>
    public System.Collections.Generic.IReadOnlyDictionary<string, string> ExportFlat()
    {
        var d = new Dictionary<string, string>();
        foreach (var k in new[] { "DrawingNumber", "DrawingTitle", "Material", "Designer", "DesignDate", "Scale", "CompanyName", "ProjectName" })
            d[k] = GetTitleField(k);
        return d;
    }

    // -------- 渲染 --------

    protected override void Generate()
    {
        base.Generate();   // 外框 + 内框 + 标题栏 (并清 _markEntities + _cellHits)

        if (!ShowTitleBlock) return;
        EnsureColumns();

        double ix0 = Origin.X + MarginLeft;
        double ix1 = Origin.X + PaperWidth - MarginRight;
        double iy0 = Origin.Y + MarginBottom;
        double tbx0 = ix1 - TitleBlockWidth;

        // 三列表: 内框左 → 标题栏左, 底部对齐内框底
        double tableX0 = ix0;
        double tableX1 = tbx0 - TableGap;
        if (tableX1 <= tableX0) tableX1 = tableX0 + 60;   // 退化保护

        int rowCount = 0;
        foreach (var c in Columns) rowCount = System.Math.Max(rowCount, c.Rows.Count);
        double tableY0 = iy0;
        double tableY1 = tableY0 + (rowCount + 1) * TableRowHeight;

        DrawSpecColumns(tableX0, tableY0, tableX1, tableY1, Columns, TableRowHeight, 2.5, 2.2);
    }

    // -------- Entity 覆写 --------

    protected override DBObject CreateInstance() => new OpticalDrawingFrame();

    public override object Clone()
    {
        var c = (OpticalDrawingFrame)base.Clone();
        c.GlassMaterial = GlassMaterial;
        c.Nd = Nd;
        c.Vd = Vd;
        c.Birefringence = Birefringence;
        c.BubbleClass = BubbleClass;
        c.Inhomogeneity = Inhomogeneity;
        c.FormError = FormError;
        c.SurfaceImperf = SurfaceImperf;
        c.CenteringTol = CenteringTol;
        c.SampleGrade = SampleGrade;
        c.SurfaceTexture = SurfaceTexture;
        c.LaserDamage = LaserDamage;
        c.TableRowHeight = TableRowHeight;
        c.TableLabelWidth = TableLabelWidth;
        c.TableValueWidth = TableValueWidth;
        c.TableGap = TableGap;
        c.Columns = new List<IsoSpecColumn>(Columns.Count);
        foreach (var col in Columns)
            c.Columns.Add(new IsoSpecColumn { Title = col.Title, Rows = new List<string>(col.Rows) });
        return c;
    }
}
