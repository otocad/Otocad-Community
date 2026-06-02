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
public sealed class OpticalDrawingFrame : DrawingFrame
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

    /// <summary>面形精度 (ISO 10110-5) — 4/N(ΔN), 例: "4/3(0.5)".</summary>
    public string FormError { get; set; } = "4/3(0.5)";

    /// <summary>表面疵病 (ISO 10110-7) — 5/B, 例: "5/3×0.1".</summary>
    public string SurfaceImperf { get; set; } = "5/3×0.1";

    /// <summary>中心偏差 (ISO 10110-6) — 例: "C=3'".</summary>
    public string CenteringTol { get; set; } = "C=3'";

    /// <summary>样板精度 (ΔR) — 例: "ΔR=A".</summary>
    public string SampleGrade { get; set; } = "ΔR=A";

    /// <summary>表面结构 / 粗糙度 — 8/P3 形式.</summary>
    public string SurfaceTexture { get; set; } = "8/P3";

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

    /// <summary>字段名列宽 (mm).</summary>
    public double TableLabelWidth { get; set; } = 40.0;

    /// <summary>值列宽 (mm).</summary>
    public double TableValueWidth { get; set; } = 60.0;

    /// <summary>双表整体距标题栏左侧间距 (mm).</summary>
    public double TableGap { get; set; } = 4.0;

    // -------- 渲染 --------

    protected override void Generate()
    {
        base.Generate();   // 外框 + 内框 + 标题栏

        if (!ShowTitleBlock) return;

        // T6: 渲染期 override (不写回字段, 取消关联即恢复用户输入)
        var (linkedMat, linkedNd, linkedVd) = GetLinkedOverride();

        var x0 = Origin.X;
        var y0 = Origin.Y;
        var x1 = Origin.X + PaperWidth;
        var ix0 = x0 + MarginLeft;
        var ix1 = x1 - MarginRight;
        var iy0 = y0 + MarginBottom;
        var iy1 = y0 + PaperHeight - MarginTop;
        var tbx0 = ix1 - TitleBlockWidth;

        // 双表布局: 紧贴标题栏左侧 + 上下叠放
        double tableWidth = TableLabelWidth + TableValueWidth;
        double tableX1 = tbx0 - TableGap;
        double tableX0 = tableX1 - tableWidth;

        // 收集两表行 (跳过空 LaserDamage). 材料行用 linked override 优先.
        var matRows = new List<(string label, string value)>
        {
            ("玻璃牌号",  linkedMat ?? GlassMaterial),
            ("n_d",       linkedNd  ?? Nd),
            ("v_d",       linkedVd  ?? Vd),
            ("双折射",    Birefringence),
            ("气泡度",    BubbleClass),
            ("不均匀性",  Inhomogeneity),
        };
        var partRows = new List<(string label, string value)>
        {
            ("面形 4/N(ΔN)",  FormError),
            ("表面疵病 5/B",  SurfaceImperf),
            ("中心偏差",      CenteringTol),
            ("样板精度",      SampleGrade),
            ("表面结构",      SurfaceTexture),
        };
        if (!string.IsNullOrWhiteSpace(LaserDamage))
        {
            partRows.Add(("激光阈值", LaserDamage));
        }

        double matHeight  = matRows.Count  * TableRowHeight + TableRowHeight; // +1 for header
        double partHeight = partRows.Count * TableRowHeight + TableRowHeight;

        // 对零件的要求 (下) — 距标题栏底齐
        double partY0 = iy0;
        double partY1 = partY0 + partHeight;
        // 对材料的要求 (上)
        double matY0 = partY1 + TableGap;
        double matY1 = matY0 + matHeight;

        // 边界检查: 上侧不能超内框顶, 左侧不能超内框左
        if (matY1 > iy1)
        {
            // 两表合计高度超过可用空间 — 让用户在 status 看到, 但仍渲染 (截断到顶)
            System.Diagnostics.Debug.WriteLine(
                $"[OpticalDrawingFrame] 双表高度 {matY1 - partY0:F1}mm 超过可用空间 " +
                $"{iy1 - partY0:F1}mm, 表格可能溢出顶边");
        }
        if (tableX0 < ix0)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[OpticalDrawingFrame] 双表左缘 {tableX0:F1} 超过内框左缘 {ix0:F1}, " +
                "可能溢出装订边 — 考虑缩小 TableLabelWidth/TableValueWidth");
        }

        DrawTable(tableX0, partY0, tableWidth, "对零件的要求", partRows);
        DrawTable(tableX0, matY0, tableWidth, "对材料的要求", matRows);
    }

    private void DrawTable(double x0, double y0, double width, string title, List<(string label, string value)> rows)
    {
        double rh = TableRowHeight;
        double height = (rows.Count + 1) * rh; // +1 标题行
        double x1 = x0 + width;
        double y1 = y0 + height;

        // 外框
        AddRect(x0, y0, x1, y1);

        // 标题行下分隔
        double titleY = y1 - rh;
        AddLine(x0, titleY, x1, titleY);

        // 标题
        AddText(x0 + 2, titleY + rh * 0.25, title, height: rh * 0.55);

        // 中分隔 (Label | Value)
        double midX = x0 + TableLabelWidth;
        AddLine(midX, y0, midX, titleY);

        // 行 + 行间分隔
        for (int i = 0; i < rows.Count; i++)
        {
            double rowTopY = titleY - (i + 1) * rh;
            // 行底分隔 (除最后一行外用外框底边)
            if (i < rows.Count - 1)
            {
                AddLine(x0, rowTopY, x1, rowTopY);
            }
            // 字段名 (左)
            AddText(x0 + 2, rowTopY + rh * 0.25, rows[i].label, height: rh * 0.50);
            // 值 (右)
            AddText(midX + 2, rowTopY + rh * 0.25, rows[i].value, height: rh * 0.50);
        }
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
        return c;
    }
}
