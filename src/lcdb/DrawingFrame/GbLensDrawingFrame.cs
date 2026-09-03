using System.Collections.Generic;
using LitMath;

namespace lcdb.DrawingFrame;

/// <summary>
/// GB 风格光学零件图框 (中文, 对标 docs/需求/图面样例-单透镜2.png).
///
/// 自底向上: GB/T 10609.1 标题栏 → 三列表(左表面 | 材料技术要求 | 右表面) → 注1/2/3 → 绘图区.
/// 右上角 "其余 ∇Ra". 与英文 ISO 的 <see cref="IsoLensDrawingFrame"/> 是并列的两套模板, 不互相套用.
/// 复用基类几何缓存 + 属性区交互 (DrawSpecColumns/_cellHits/HitTestCellCore/列读写助手).
/// </summary>
public sealed class GbLensDrawingFrame : DrawingFrame, IPropertyZoneFrame, IPropertyExport, IPropertyBagFrame
{
    public override string className => "GbLensDrawingFrame";

    // -------- 布局 (mm) --------
    public double BorderMargin { get; set; } = 8.0;
    public double TableRowHeight { get; set; } = 6.0;
    public double TitleBlockHeightGb { get; set; } = 40.0;
    public double CellTextHeight { get; set; } = 2.5;

    // -------- 三列表 (中文) --------
    public List<IsoSpecColumn> Columns { get; set; } = new();

    // -------- 注释 (注1/注2/注3) + 其余 --------
    public List<string> NoteLines { get; set; } = new();
    public string GeneralRoughness { get; set; } = "Ra 1";   // 其余 ∇ ...

    // -------- GB 标题栏字段 --------
    public string Unit { get; set; } = "(单位名称)";
    public string PartName { get; set; } = "透镜";
    public string DrawingNumberGb { get; set; } = "(图样代号)";
    public string Designer { get; set; } = "";
    public string StdCheck { get; set; } = "";   // 标准化
    public string Reviewer { get; set; } = "";   // 审核
    public string ProcessBy { get; set; } = "";  // 工艺
    public string Approver { get; set; } = "";   // 批准
    public string Stage { get; set; } = "";      // 阶段标记
    public string Weight { get; set; } = "";
    public string ScaleText { get; set; } = "1:1";
    public string SheetOf { get; set; } = "共 1 张   第 1 张";

    // -------- 渲染 --------
    protected override void Generate()
    {
        _markEntities.Clear();
        _cellHits.Clear();

        double x0 = Origin.X + BorderMargin;
        double y0 = Origin.Y + BorderMargin;
        double x1 = Origin.X + PaperWidth - BorderMargin;
        double y1 = Origin.Y + PaperHeight - BorderMargin;
        AddRect(x0, y0, x1, y1);

        // 自底向上
        double titleTop = y0 + TitleBlockHeightGb;
        int rowCount = 0;
        foreach (var c in Columns) rowCount = System.Math.Max(rowCount, c.Rows.Count);
        double tableTop = titleTop + (rowCount + 1) * TableRowHeight;

        DrawTitleBlock(x0, y0, x1, titleTop);
        // 三列表 (复用基类: 绘制 + 登记每格/每列命中 → 三区可点击编辑)
        DrawSpecColumns(x0, titleTop, x1, tableTop, Columns, TableRowHeight, 2.6, CellTextHeight);
        DrawNotes(x0 + 2, tableTop + 2);
        DrawGeneralRoughness(x1 - 30, y1 - 8);
    }

    // ── 注1/注2/注3 (表上方, 左对齐; 整块作为一个可编辑命名字段) ──
    private void DrawNotes(double x, double yBottom)
    {
        double y = yBottom + NoteLines.Count * 4.2;
        foreach (var line in NoteLines)
        {
            AddText(x, y, line, height: 2.4);
            y -= 4.2;
        }
        double h = System.Math.Max(4.2, NoteLines.Count * 4.2) + 2;
        _cellHits.Add(new PropertyCellHit("注释", "Notes", new Rectangle2(new Vector2(x - 2, yBottom - 1), 100, h)));
    }

    // ── 其余 ∇Ra (右上角, 可编辑) ──
    private void DrawGeneralRoughness(double x, double y)
    {
        AddText(x, y, "其余", height: 3.0);
        double s = 3.0, bx = x + 11;
        AddLine(bx, y + 1, bx + s * 0.5, y + 1 + s);
        AddLine(bx + s * 0.5, y + 1 + s, bx + s, y + 1);
        AddText(bx + s + 1.5, y + 1.5, GeneralRoughness, height: 2.4);
        _cellHits.Add(new PropertyCellHit("其余", "GeneralRoughness", new Rectangle2(new Vector2(x - 1, y - 1), 30, 8)));
    }

    // ── GB/T 10609.1 标题栏 (简化但结构对版; 关键格可点编辑) ──
    private void DrawTitleBlock(double x0, double y0, double x1, double y1)
    {
        AddRect(x0, y0, x1, y1);
        double w = x1 - x0, h = y1 - y0;
        double xMid = x0 + w * 0.62;   // 左(更改+签字) | 右(识别区)
        AddLine(xMid, y0, xMid, y1);

        // ---- 左块: 顶部更改记录表头 + 下方签字区 ----
        double rowH = h / 5.0;
        double headY = y1 - rowH;
        AddLine(x0, headY, xMid, headY);
        double[] frac = { 0.12, 0.12, 0.12, 0.28, 0.18, 0.18 };
        string[] hdr = { "标记", "处数", "分区", "更改文件号", "签名", "年月日" };
        double cx = x0;
        double lw = xMid - x0;
        for (int i = 0; i < frac.Length; i++)
        {
            double cw = lw * frac[i];
            if (i > 0) AddLine(cx, headY, cx, y1);
            AddTextCentered(cx, cx + cw, headY + rowH * 0.28, hdr[i], 2.0);
            cx += cw;
        }
        // 签字区 4 行: 设计/标准化/审核/工艺 — 标签列 | 签名格(可编辑) | 日期格
        string[] roles = { "设计", "标准化", "审核", "工艺" };
        string[] roleKeys = { "Designer", "StdCheck", "Reviewer", "ProcessBy" };
        string[] roleVals = { Designer, StdCheck, Reviewer, ProcessBy };
        double lblW = lw * 0.16, signW = lw * 0.20;
        for (int r = 0; r < 4; r++)
        {
            double top = headY - r * rowH;
            double bot = top - rowH;
            if (r > 0) AddLine(x0, top, xMid, top);
            AddText(x0 + 1.5, bot + rowH * 0.28, roles[r], height: 2.0);
            AddLine(x0 + lblW, bot, x0 + lblW, top);
            AddLine(x0 + lblW + signW, bot, x0 + lblW + signW, top);
            if (!string.IsNullOrEmpty(roleVals[r]))
                AddText(x0 + lblW + 1.5, bot + rowH * 0.28, roleVals[r], height: 2.0);
            _cellHits.Add(new PropertyCellHit(roles[r], roleKeys[r],
                new Rectangle2(new Vector2(x0 + lblW, bot), signW, rowH)));
        }

        // ---- 右块: 识别区 ----
        double rw = x1 - xMid;
        double rRow = h / 4.0;
        // 单位名称 (顶)
        double unitBot = y1 - rRow;
        AddLine(xMid, unitBot, x1, unitBot);
        AddTextCentered(xMid, x1, unitBot + rRow * 0.35, Unit, 2.8);
        _cellHits.Add(new PropertyCellHit("单位名称", "Unit", new Rectangle2(new Vector2(xMid, unitBot), rw, y1 - unitBot)));
        // 阶段标记 | 重量 | 比例
        double sBot = unitBot - rRow;
        AddLine(xMid, sBot, x1, sBot);
        double c1 = xMid + rw * 0.40, c2 = xMid + rw * 0.70;
        AddLine(c1, sBot, c1, unitBot);
        AddLine(c2, sBot, c2, unitBot);
        AddText(xMid + 1.5, sBot + rRow * 0.55, "阶段标记", 1.7);
        AddText(c1 + 1.5, sBot + rRow * 0.55, "重量", 1.7);
        AddText(c2 + 1.5, sBot + rRow * 0.55, "比例", 1.7);
        AddText(xMid + 1.5, sBot + rRow * 0.12, Stage, 2.2);
        AddText(c1 + 1.5, sBot + rRow * 0.12, Weight, 2.2);
        AddText(c2 + 1.5, sBot + rRow * 0.12, ScaleText, 2.4);
        _cellHits.Add(new PropertyCellHit("阶段标记", "Stage", new Rectangle2(new Vector2(xMid, sBot), c1 - xMid, rRow)));
        _cellHits.Add(new PropertyCellHit("重量", "Weight", new Rectangle2(new Vector2(c1, sBot), c2 - c1, rRow)));
        _cellHits.Add(new PropertyCellHit("比例", "ScaleText", new Rectangle2(new Vector2(c2, sBot), x1 - c2, rRow)));
        // 透镜 (图样名称, 大字居中)
        double nameBot = sBot - rRow;
        AddLine(xMid, nameBot, x1, nameBot);
        AddTextCentered(xMid, x1, nameBot + rRow * 0.32, PartName, 4.0);
        _cellHits.Add(new PropertyCellHit("图样名称", "PartName", new Rectangle2(new Vector2(xMid, nameBot), rw, sBot - nameBot)));
        // 图样代号 + 共X张
        AddTextCentered(xMid, x1, y0 + rRow * 0.55, DrawingNumberGb, 2.6);
        AddTextCentered(xMid, x1, y0 + rRow * 0.12, SheetOf, 1.9);
        _cellHits.Add(new PropertyCellHit("图样代号", "DrawingNumberGb", new Rectangle2(new Vector2(xMid, y0 + (nameBot - y0) * 0.4), rw, (nameBot - y0) * 0.6)));
        _cellHits.Add(new PropertyCellHit("张次", "SheetOf", new Rectangle2(new Vector2(xMid, y0), rw, (nameBot - y0) * 0.4)));
    }

    // -------- IPropertyZoneFrame (属性区交互编辑) --------

    public PropertyCellHit? HitTestCell(Vector2 modelPoint) => HitTestCellCore(modelPoint);

    public string GetCellText(PropertyCellHit cell)
        => string.IsNullOrEmpty(cell.FieldKey) ? GetColumnsCellText(Columns, cell) : GetField(cell.FieldKey);

    public void SetCellText(PropertyCellHit cell, string text)
    {
        text ??= "";
        if (string.IsNullOrEmpty(cell.FieldKey))
        {
            SetColumnsCellText(Columns, cell, text);
            // 回写文档级属性包 (单一真值源): 表格行 → 按行键
            if (_bag is not null && cell.ColIndex >= 0 && cell.ColIndex < Columns.Count)
            {
                var rk = Columns[cell.ColIndex].RowKeys;
                if (cell.RowIndex >= 0 && cell.RowIndex < rk.Count && !string.IsNullOrEmpty(rk[cell.RowIndex]))
                    _bag.SetValue(rk[cell.RowIndex], text);
            }
            return;
        }
        SetField(cell.FieldKey, text);
        // 回写文档级属性包: 标题栏命名字段 → 经 字段键→包键 反查
        if (_bag is not null && _fieldToBagKey.TryGetValue(cell.FieldKey, out var bagKey))
            _bag.SetValue(bagKey, text);
        InvalidateRenderCache();
    }

    private string GetField(string key) => key switch
    {
        "Unit" => Unit, "PartName" => PartName, "DrawingNumberGb" => DrawingNumberGb,
        "Designer" => Designer, "StdCheck" => StdCheck, "Reviewer" => Reviewer,
        "ProcessBy" => ProcessBy, "Approver" => Approver, "Stage" => Stage, "Weight" => Weight,
        "ScaleText" => ScaleText, "SheetOf" => SheetOf, "GeneralRoughness" => GeneralRoughness,
        "Notes" => string.Join("\n", NoteLines), _ => ""
    };

    private void SetField(string key, string v)
    {
        switch (key)
        {
            case "Unit": Unit = v; break;
            case "PartName": PartName = v; break;
            case "DrawingNumberGb": DrawingNumberGb = v; break;
            case "Designer": Designer = v; break;
            case "StdCheck": StdCheck = v; break;
            case "Reviewer": Reviewer = v; break;
            case "ProcessBy": ProcessBy = v; break;
            case "Approver": Approver = v; break;
            case "Stage": Stage = v; break;
            case "Weight": Weight = v; break;
            case "ScaleText": ScaleText = v; break;
            case "SheetOf": SheetOf = v; break;
            case "GeneralRoughness": GeneralRoughness = v; break;
            case "Notes": NoteLines = new List<string>(v.Split('\n')); break;
        }
    }

    // -------- 属性包驱动 (文档级单一真值源 → 填图框) --------

    /// <summary>绑定的文档级属性包 (SetCellText 回写到它, 保单一真值; 一键重出 re-ApplyBag 保留用户改动).</summary>
    private lcdb.Drawing.DrawingPropertyBag? _bag;

    /// <summary>暴露绑定的属性包, 供画布在装载图纸时采纳为文档级单一真值源 (db.DrawingProperties)。</summary>
    public lcdb.Drawing.DrawingPropertyBag? Bag => _bag;
    /// <summary>标题栏 字段键 → 包键 反查表 (ApplyBag 时建).</summary>
    private readonly Dictionary<string, string> _fieldToBagKey = new();

    /// <summary>由属性包 (lcdb.Drawing 单一真值源) 填充图框: title 簇→标题栏字段, notes 簇→注释, 其它簇→规格列(带行键). 列标题用簇 Label.</summary>
    public void ApplyBag(lcdb.Drawing.DrawingPropertyBag bag)
    {
        _bag = bag;
        _fieldToBagKey.Clear();
        Columns = new List<IsoSpecColumn>();
        foreach (var c in bag.Clusters)
        {
            if (c.Key == "title")
            {
                foreach (var p in c.Properties)
                {
                    var fk = MapTitleKey(p.Key);
                    _fieldToBagKey[fk] = p.Key;   // 字段键→包键, 供回写反查
                    SetField(fk, p.Value);
                }
            }
            else if (c.Key == "notes")
            {
                NoteLines = new List<string>();
                foreach (var p in c.Properties) NoteLines.Add(p.Value);
            }
            else // 规格区 → 列 (行值 + 行键)
            {
                var col = new IsoSpecColumn { Title = c.Label };
                foreach (var p in c.Properties) { col.Rows.Add(p.Value); col.RowKeys.Add(p.Key); }
                Columns.Add(col);
            }
        }
        InvalidateRenderCache();
    }

    /// <summary>扁平 裸键→值 (与 OpticalDrawingFrame.ExportFlat / 包 GetValue 同词汇, 非 Flatten 的 簇.键 复合键),
    /// 供出图清单 property 动词按键查 / AI 整体读取. 绑定文档包则取包(单一真值源); 否则按当前列行键拼.</summary>
    public IReadOnlyDictionary<string, string> ExportFlat()
    {
        var d = new Dictionary<string, string>();
        if (_bag is not null)
        {
            foreach (var p in _bag.AllProperties)
                if (!string.IsNullOrEmpty(p.Key)) d[p.Key] = p.Value;
            return d;
        }
        foreach (var col in Columns)
            for (int i = 0; i < col.Rows.Count && i < col.RowKeys.Count; i++)
                if (!string.IsNullOrEmpty(col.RowKeys[i])) d[col.RowKeys[i]] = col.Rows[i];
        return d;
    }

    /// <summary>属性包标题键 → 标题栏字段键 (SetField). 兼容语义键与 master lcdb.Drawing 既有键(DrawingTitle/DrawingNumber…).</summary>
    private static string MapTitleKey(string bagKey) => bagKey switch
    {
        "name" or "DrawingTitle" => "PartName",
        "product_number" or "drawing_number" or "DrawingNumber" => "DrawingNumberGb",
        "unit" => "Unit",
        "scale" or "Scale" => "ScaleText",
        "sheet" => "SheetOf",
        "designer" or "Designer" => "Designer",
        "std_check" => "StdCheck",
        "reviewer" => "Reviewer",
        "process" => "ProcessBy",
        "approver" => "Approver",
        "stage" => "Stage",
        "weight" => "Weight",
        "general_roughness" => "GeneralRoughness",
        _ => bagKey,   // 未知键 (如 Material/DesignDate) → SetField 默认忽略
    };

    // 增删行 = 增删一个"自定义小属性": 给新行配机器键(custom_N)、同步写文档级属性包(单一真值源)。
    // 这样用户在属性区加的任意行都是一等属性: 随 .otocad 序列化(P2)、可被清单按键校验(P4)。
    public int AddRow(int colIndex, string text = "")
    {
        int idx = AddColumnsRow(Columns, colIndex, text);
        if (idx < 0) return idx;
        var col = Columns[colIndex];
        while (col.RowKeys.Count <= idx) col.RowKeys.Add("");
        string key = UniqueCustomKey(col);
        col.RowKeys[idx] = key;
        BagClusterForColumn(colIndex)?.Add(key, "", text);
        return idx;
    }

    public int InsertRow(int colIndex, int rowIndex, string text)
    {
        int idx = InsertColumnsRow(Columns, colIndex, rowIndex, text);
        if (idx < 0) return idx;
        var col = Columns[colIndex];
        string key = UniqueCustomKey(col);
        if (idx <= col.RowKeys.Count) col.RowKeys.Insert(idx, key);
        else { while (col.RowKeys.Count < idx) col.RowKeys.Add(""); col.RowKeys.Add(key); }
        var cl = BagClusterForColumn(colIndex);
        if (cl is not null) cl.Properties.Insert(System.Math.Min(idx, cl.Properties.Count), new lcdb.Drawing.DrawingProperty(key, "", text));
        return idx;
    }

    public bool RemoveRow(int colIndex, int rowIndex)
    {
        if (colIndex < 0 || colIndex >= Columns.Count) return false;
        var col = Columns[colIndex];
        string? key = (rowIndex >= 0 && rowIndex < col.RowKeys.Count) ? col.RowKeys[rowIndex] : null;
        if (!RemoveColumnsRow(Columns, colIndex, rowIndex)) return false;
        if (rowIndex < col.RowKeys.Count) col.RowKeys.RemoveAt(rowIndex);
        if (!string.IsNullOrEmpty(key)) BagClusterForColumn(colIndex)?.Properties.RemoveAll(p => p.Key == key);
        return true;
    }

    public PropertyCellHit? GetCell(int colIndex, int rowIndex) => FindCell(colIndex, rowIndex);
    public IReadOnlyList<string> GetZoneRows(int colIndex) => GetColumnsRows(Columns, colIndex);

    /// <summary>列内唯一自定义键 custom_N (N = 现有 custom_ 最大序号 +1)。</summary>
    private static string UniqueCustomKey(IsoSpecColumn col)
    {
        int n = 0;
        foreach (var k in col.RowKeys)
            if (k != null && k.StartsWith("custom_") && int.TryParse(k.Substring(7), out var v) && v > n) n = v;
        return $"custom_{n + 1}";
    }

    /// <summary>第 colIndex 个规格列 ↔ 包中第 colIndex 个非 title/notes 簇 (ApplyBag 的反向)。</summary>
    private lcdb.Drawing.DrawingPropertyCluster? BagClusterForColumn(int colIndex)
    {
        if (_bag is null) return null;
        int i = 0;
        foreach (var c in _bag.Clusters)
        {
            if (c.Key == "title" || c.Key == "notes") continue;
            if (i == colIndex) return c;
            i++;
        }
        return null;
    }

    /// <summary>取某表格格的属性键 (命名字段/越界返回 null)。供属性面板"自定义键起名"。</summary>
    public string? GetCellKey(PropertyCellHit cell)
    {
        if (!string.IsNullOrEmpty(cell.FieldKey)) return null;
        if (cell.ColIndex >= 0 && cell.ColIndex < Columns.Count)
        {
            var rk = Columns[cell.ColIndex].RowKeys;
            if (cell.RowIndex >= 0 && cell.RowIndex < rk.Count) return rk[cell.RowIndex];
        }
        return null;
    }

    /// <summary>给某表格格改属性键 (重命名), 同步文档包对应属性键。空键/命名字段/越界忽略。</summary>
    public void SetCellKey(PropertyCellHit cell, string newKey)
    {
        if (string.IsNullOrWhiteSpace(newKey) || !string.IsNullOrEmpty(cell.FieldKey)) return;
        if (cell.ColIndex < 0 || cell.ColIndex >= Columns.Count) return;
        var rk = Columns[cell.ColIndex].RowKeys;
        if (cell.RowIndex < 0 || cell.RowIndex >= rk.Count) return;
        string old = rk[cell.RowIndex];
        if (old == newKey) return;
        rk[cell.RowIndex] = newKey;
        var cl = BagClusterForColumn(cell.ColIndex);
        if (cl is not null)
            foreach (var p in cl.Properties)
                if (p.Key == old) { p.Key = newKey; break; }
        InvalidateRenderCache();
    }

    protected override DBObject CreateInstance() => new GbLensDrawingFrame();

    public override object Clone()
    {
        var c = (GbLensDrawingFrame)base.Clone();
        c.BorderMargin = BorderMargin;
        c.TableRowHeight = TableRowHeight;
        c.TitleBlockHeightGb = TitleBlockHeightGb;
        c.CellTextHeight = CellTextHeight;
        c.Columns = new List<IsoSpecColumn>();
        foreach (var col in Columns)
            c.Columns.Add(new IsoSpecColumn { Title = col.Title, Rows = new List<string>(col.Rows), RowKeys = new List<string>(col.RowKeys) });
        c.NoteLines = new List<string>(NoteLines);
        c.GeneralRoughness = GeneralRoughness;
        c.Unit = Unit; c.PartName = PartName; c.DrawingNumberGb = DrawingNumberGb;
        c.Designer = Designer; c.StdCheck = StdCheck; c.Reviewer = Reviewer;
        c.ProcessBy = ProcessBy; c.Approver = Approver;
        c.Stage = Stage; c.Weight = Weight; c.ScaleText = ScaleText; c.SheetOf = SheetOf;
        return c;
    }
}
