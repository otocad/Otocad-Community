#nullable enable
using System.Text.Json.Serialization;
using LitMath;

namespace lcdb.DrawingFrame;

/// <summary>
/// 通用图框渲染器 — 一个类替代 N 个硬编码图框类 (模板系统 Phase 2a).
///
/// 按 <see cref="Definition"/> 自底向上画带 (notes / title-grid / spec-table) + furniture,
/// 登记每个可编辑格的命中矩形 — 属性区交互与既有图框同一契约 (<see cref="IPropertyZoneFrame"/>),
/// UI 层不需要认识本类.
///
/// 状态三件套全 public → 随实体内嵌进 .otocad: <see cref="Definition"/> (结构) /
/// <see cref="FieldValues"/> (命名字段值) / <see cref="Columns"/> (属性区运行期内容).
/// 与 <see cref="GbLensDrawingFrame"/> 一样可绑定文档级属性包 (<see cref="ApplyBag"/>):
/// 改格回写包, 一键重出 re-ApplyBag 保留用户改动.
/// </summary>
public sealed class DataDrivenFrame : DrawingFrame, IPropertyZoneFrame, IPropertyExport, IPropertyBagFrame
{
    public override string className => "DataDrivenFrame";

    /// <summary>图框定义 (纯数据). 换定义即换图框; 显式纸张见 <see cref="ApplyPaper"/>.</summary>
    public FrameDefinition Definition { get; set; } = new();

    /// <summary>命名字段值, 键 = 定义里的 key (标题栏格 / NOTES / 其余 …). 未设值的格显示定义的 default.</summary>
    public Dictionary<string, string> FieldValues { get; set; } = new();

    /// <summary>spec-table 带的运行期内容 (由定义 seed 播种后可编辑, 或由属性包覆盖).</summary>
    public List<IsoSpecColumn> Columns { get; set; } = new();

    public DataDrivenFrame() { }

    /// <summary>按定义建图框; 定义给显式纸张 (A4P …) 即应用纸张尺寸.</summary>
    public DataDrivenFrame(FrameDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ApplyPaper();
    }

    /// <summary>定义 paper 为显式代码 (A4P / A3L …) 时设置纸张宽高; "auto" 不动 (由布局引擎定). 返回是否应用.</summary>
    public bool ApplyPaper()
    {
        if (!DrawingFrameTemplates.TryParsePaper(Definition.Paper, out var w, out var h)) return false;
        PaperWidth = w;
        PaperHeight = h;
        InvalidateRenderCache();
        return true;
    }

    // ==================== 渲染 ====================

    /// <summary>已画线段 (相邻格共边只画一次, 免得 DXF 导出双线). 渲染期.</summary>
    private readonly HashSet<(long, long, long, long)> _drawnSegments = new();

    /// <summary>各带矩形 (渲染期, 供 furniture 锚定).</summary>
    private readonly List<(FrameBand Band, double Bottom, double Top)> _bandRects = new();

    protected override void Generate()
    {
        _markEntities.Clear();
        _cellHits.Clear();
        _drawnSegments.Clear();
        _bandRects.Clear();
        EnsureColumns();

        var d = Definition;
        double m = d.Border.Margin;
        double x0 = Origin.X + m, y0 = Origin.Y + m;
        double x1 = Origin.X + PaperWidth - m, y1 = Origin.Y + PaperHeight - m;
        AddRectOnce(x0, y0, x1, y1);

        double yb = y0;
        foreach (var band in d.Bands)
        {
            double yt = yb + BandHeight(band);
            switch (band.Kind)
            {
                case FrameDefinition.KindNotes: DrawNotesBand(band, x0, yb, x1, yt); break;
                case FrameDefinition.KindTitleGrid: DrawTitleGrid(band, x0, yb, x1, yt); break;
                case FrameDefinition.KindSpecTable: DrawSpecTable(band, x0, yb, x1, yt); break;
            }
            _bandRects.Add((band, yb, yt));
            yb = yt;
        }

        foreach (var f in d.Furniture) DrawFurniture(f, x0, y0, x1, y1);
    }

    /// <summary>带高: notes = height (默认 12); title-grid = height 或 rows × rowHeight (默认 16); spec-table = (最多行数 + 1 表头) × rowHeight (默认 6).</summary>
    public double BandHeight(FrameBand band) => band.Kind switch
    {
        FrameDefinition.KindNotes => band.Height > 0 ? band.Height : 12.0,
        FrameDefinition.KindTitleGrid => band.Height > 0
            ? band.Height
            : Math.Max(1, band.Rows) * (band.RowHeight > 0 ? band.RowHeight : 16.0),
        FrameDefinition.KindSpecTable => (MaxRows() + 1) * SpecRowHeight(band),
        _ => 0,
    };

    /// <summary>底部块总高 (全部带之和), 供布局引擎给图区留位.</summary>
    public double BottomBlockHeight()
    {
        EnsureColumns();
        double h = 0;
        foreach (var b in Definition.Bands) h += BandHeight(b);
        return h;
    }

    private static double SpecRowHeight(FrameBand band) => band.RowHeight > 0 ? band.RowHeight : 6.0;

    private int MaxRows()
    {
        EnsureColumns();
        int n = 0;
        foreach (var c in Columns) n = Math.Max(n, c.Rows.Count);
        return n;
    }

    /// <summary>Columns 为空时从 spec-table 定义播种 (seed 行 + 行键). 已有内容不动.</summary>
    public void EnsureColumns()
    {
        if (Columns.Count > 0) return;
        var band = Definition.FirstBand(FrameDefinition.KindSpecTable);
        if (band is null) return;
        foreach (var cd in band.Columns)
        {
            var col = new IsoSpecColumn { Title = cd.Title, Rows = new List<string>(cd.Seed) };
            for (int i = 0; i < col.Rows.Count; i++)
                col.RowKeys.Add(i < cd.SeedKeys.Count ? cd.SeedKeys[i] : "");
            Columns.Add(col);
        }
    }

    // ── notes 带: 首字段占剩余宽; 其余按 width 自右向左排盒 ──
    private void DrawNotesBand(FrameBand band, double x0, double yb, double x1, double yt)
    {
        if (band.Box) AddRectOnce(x0, yb, x1, yt);
        bool projAtRight = Definition.Furniture.Any(f =>
            f.Kind == FrameDefinition.FurnitureProjectionSymbol && f.At == "notes-right");

        double right = x1;
        for (int i = band.Fields.Count - 1; i >= 1; i--)
        {
            var f = band.Fields[i];
            if (f.Width <= 0) continue;
            double left = right - f.Width;
            if (band.Box) AddLineOnce(left, yb, left, yt);
            // 最右盒内若放投影符号, 命中矩形让出右端 12mm (与 ISO 图框一致)
            double hitRight = (right >= x1 - 1e-9 && projAtRight) ? right - 12 : right;
            DrawNotesField(f, left, right, yb, yt, hitRight);
            right = left;
        }
        if (band.Fields.Count > 0)
        {
            double hitRight = (right >= x1 - 1e-9 && projAtRight) ? right - 12 : right;
            DrawNotesField(band.Fields[0], x0, right, yb, yt, hitRight);
        }
    }

    private void DrawNotesField(FrameField f, double left, double right, double yb, double yt, double hitRight)
    {
        const double pad = 2.0;
        double ty = yt - 3.5;
        if (!string.IsNullOrEmpty(f.Label))
        {
            AddText(left + pad, yt - 4.0, f.Label, 2.2);
            ty -= 3.5;
        }
        double h = Definition.Style.CellTextHeight;
        foreach (var line in ValueOf(f.Key, f.Default).Split('\n'))
        {
            AddText(left + pad, ty, line, h);
            ty -= Math.Max(3.0, h * 1.2);
        }
        _cellHits.Add(new PropertyCellHit(string.IsNullOrEmpty(f.Label) ? f.Key : f.Label, f.Key,
            new Rectangle2(new Vector2(left, yb), hitRight - left, yt - yb)));
    }

    // ── title-grid 带: rows × cols 网格, 格按 (col,w,row,h) 定位, 行 0 在顶 ──
    private void DrawTitleGrid(FrameBand band, double x0, double yb, double x1, double yt)
    {
        AddRectOnce(x0, yb, x1, yt);
        int cols = band.EffectiveCols();
        int rows = Math.Max(1, band.Rows);
        double cw = (x1 - x0) / cols, rh = (yt - yb) / rows;
        var st = Definition.Style;

        foreach (var c in band.Cells)
        {
            double cx0 = x0 + c.Col * cw, cx1 = cx0 + c.W * cw;
            double ctop = yt - c.Row * rh, cbot = ctop - c.H * rh;
            AddRectOnce(cx0, cbot, cx1, ctop);
            double ch = ctop - cbot;

            if (string.IsNullOrEmpty(c.Key))
            {
                // 纯标签格 (表头 / 角色名): 竖向居中, 不可编辑
                if (string.IsNullOrEmpty(c.Label)) continue;
                double lh = c.TextHeight > 0 ? c.TextHeight : 2.0;
                if (c.Align == "center") AddTextCentered(cx0, cx1, cbot + ch * 0.5 - lh * 0.5, c.Label, lh);
                else AddText(cx0 + 1.5, cbot + ch * 0.5 - lh * 0.5, c.Label, lh);
                continue;
            }

            string v = ValueOf(c.Key, c.Default);
            double vh = c.TextHeight > 0 ? c.TextHeight : st.ValueTextHeight;
            double vy;
            if (!string.IsNullOrEmpty(c.Label))
            {
                AddText(cx0 + 2.0, cbot + 1.5, c.Label, st.LabelTextHeight);
                vy = cbot + ch * 0.45;
            }
            else vy = cbot + ch * 0.5 - vh * 0.5;
            if (c.Align == "center") AddTextCentered(cx0, cx1, vy, v, vh);
            else AddText(cx0 + 2.0, vy, v, vh);

            _cellHits.Add(new PropertyCellHit(string.IsNullOrEmpty(c.Label) ? c.Key : c.Label, c.Key,
                new Rectangle2(new Vector2(cx0, cbot), cx1 - cx0, ch)));
        }
    }

    // ── spec-table 带: 复用基类 N 列规格表 (绘制 + 每格 / 每列命中) ──
    private void DrawSpecTable(FrameBand band, double x0, double yb, double x1, double yt)
        => DrawSpecColumns(x0, yb, x1, yt, Columns, SpecRowHeight(band),
            Definition.Style.HeaderTextHeight, Definition.Style.CellTextHeight);

    // ── furniture ──
    private void DrawFurniture(FrameFurniture f, double x0, double y0, double x1, double y1)
    {
        switch (f.Kind)
        {
            case FrameDefinition.FurnitureProjectionSymbol:
            {
                var (cx, cy) = SymbolAnchor(f, x0, y0, x1, y1);
                AddProjectionSymbol(cx, cy, f.Size > 0 ? f.Size : 1.76, thirdAngle: f.Angle == "third");
                break;
            }
            case FrameDefinition.FurnitureGeneralRoughness:
            {
                // 文字块左起点: top-right 距右 30 (与 GB 图框一致); 其余锚点按符号锚 -2 起
                (double x, double y) = f.At switch
                {
                    "custom" => (x0 + f.X, y0 + f.Y),
                    "top-left" => (x0 + 2, y1 - 8),
                    "bottom-left" => (x0 + 2, y0 + 8),
                    "bottom-right" => (x1 - 30, y0 + 8),
                    _ => (x1 - 30, y1 - 8),
                };
                AddGeneralRoughness(f, x, y);
                break;
            }
        }
    }

    /// <summary>符号中心锚点. notes-right = 第一个 notes 带右端 (距右 8) 中线; 无 notes 带退到 bottom-right.</summary>
    private (double, double) SymbolAnchor(FrameFurniture f, double x0, double y0, double x1, double y1)
    {
        switch (f.At)
        {
            case "custom": return (x0 + f.X, y0 + f.Y);
            case "top-right": return (x1 - 8, y1 - 8);
            case "top-left": return (x0 + 8, y1 - 8);
            case "bottom-left": return (x0 + 8, y0 + 8);
            case "bottom-right": return (x1 - 8, y0 + 8);
            default:
                foreach (var (band, bottom, top) in _bandRects)
                    if (band.Kind == FrameDefinition.KindNotes) return (x1 - 8, (bottom + top) * 0.5);
                return (x1 - 8, y0 + 8);
        }
    }

    /// <summary>
    /// 投影识别符号 (ISO 128-30 / GB/T 14692): 锥台侧视 (小端远离圆, 大端朝向圆) + 两同心圆 (端视).
    /// 第一角: 锥台在圆左侧; 第三角: 镜像, 锥台在圆右侧. (cx,cy) = 同心圆圆心, s = 大圆半径.
    /// </summary>
    private void AddProjectionSymbol(double cx, double cy, double s, bool thirdAngle)
    {
        _markEntities.Add(new Circle(new Vector2(cx, cy), s) { color = color });
        _markEntities.Add(new Circle(new Vector2(cx, cy), s * 0.5) { color = color });
        double dir = thirdAngle ? 1 : -1;
        double xBig = cx + dir * s * 1.3;        // 大端 (朝向圆)
        double xSmall = xBig + dir * s * 1.6;    // 小端 (远端)
        AddLine(xBig, cy - s, xBig, cy + s);
        AddLine(xSmall, cy - s * 0.5, xSmall, cy + s * 0.5);
        AddLine(xSmall, cy - s * 0.5, xBig, cy - s);
        AddLine(xSmall, cy + s * 0.5, xBig, cy + s);
        double axA = xSmall + dir * s * 0.4, axB = cx - dir * (s + s * 0.4);
        AddLine(Math.Min(axA, axB), cy, Math.Max(axA, axB), cy);
    }

    /// <summary>"其余" + 粗糙度基本符号 (GB/T 131-2006 §4.1: 两条不等长直线与表面成 60°, 右长) + 值 (命名字段, 可编辑).</summary>
    private void AddGeneralRoughness(FrameFurniture f, double x, double y)
    {
        string label = string.IsNullOrEmpty(f.Label) ? "其余" : f.Label;
        AddText(x, y, label, 3.0);
        double s = 3.0;
        double ax = x + 11 + s * 0.5, ay = y + 1;               // V 形顶点
        AddLine(ax, ay, ax - s * 0.5, ay + s * 0.866);           // 左短边 (60°)
        AddLine(ax, ay, ax + s * 0.75, ay + s * 1.299);          // 右长边 (60°)
        AddText(x + 11 + s + 1.5, y + 1.5, ValueOf(f.Key, f.Default), 2.4);
        _cellHits.Add(new PropertyCellHit(label, f.Key, new Rectangle2(new Vector2(x - 1, y - 1), 30, 8)));
    }

    /// <summary>命名字段显示值: 已设值 → 值; 否则定义给的 default.</summary>
    private string ValueOf(string key, string? @default)
        => (!string.IsNullOrEmpty(key) && FieldValues.TryGetValue(key, out var v)) ? v : (@default ?? "");

    private void AddRectOnce(double x0, double y0, double x1, double y1)
    {
        AddLineOnce(x0, y0, x1, y0);
        AddLineOnce(x1, y0, x1, y1);
        AddLineOnce(x1, y1, x0, y1);
        AddLineOnce(x0, y1, x0, y0);
    }

    /// <summary>同一线段 (端点按 1µm 量化, 无向) 只画一次.</summary>
    private void AddLineOnce(double x0, double y0, double x1, double y1)
    {
        static long Q(double v) => (long)Math.Round(v * 1000.0);
        var a = (Q(x0), Q(y0));
        var b = (Q(x1), Q(y1));
        var key = a.CompareTo(b) <= 0 ? (a.Item1, a.Item2, b.Item1, b.Item2) : (b.Item1, b.Item2, a.Item1, a.Item2);
        if (!_drawnSegments.Add(key)) return;
        AddLine(x0, y0, x1, y1);
    }

    // ==================== IPropertyZoneFrame (属性区交互编辑) ====================

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
        FieldValues[cell.FieldKey] = text;
        // 回写包: 优先 ApplyBag 时解析到的包键 (别名), 否则字段键即包键; 包里没有该键则不写
        _bag?.SetValue(_fieldToBagKey.TryGetValue(cell.FieldKey, out var bagKey) ? bagKey : cell.FieldKey, text);
        InvalidateRenderCache();
    }

    /// <summary>读命名字段: 已设值 → 值; 否则定义的 default; 未声明的键 → "".</summary>
    public string GetField(string key)
    {
        if (FieldValues.TryGetValue(key, out var v)) return v;
        foreach (var nf in Definition.NamedFields())
            if (nf.Key == key) return nf.Default;
        return "";
    }

    // 增删行 = 增删一个"自定义小属性": 新行配机器键 custom_N, 同步文档级属性包 (与 GbLensDrawingFrame 同约定).
    public int AddRow(int colIndex, string text = "")
    {
        EnsureColumns();
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
        EnsureColumns();
        int idx = InsertColumnsRow(Columns, colIndex, rowIndex, text);
        if (idx < 0) return idx;
        var col = Columns[colIndex];
        string key = UniqueCustomKey(col);
        if (idx <= col.RowKeys.Count) col.RowKeys.Insert(idx, key);
        else { while (col.RowKeys.Count < idx) col.RowKeys.Add(""); col.RowKeys.Add(key); }
        var cl = BagClusterForColumn(colIndex);
        if (cl is not null)
            cl.Properties.Insert(Math.Min(idx, cl.Properties.Count), new lcdb.Drawing.DrawingProperty(key, "", text));
        return idx;
    }

    public bool RemoveRow(int colIndex, int rowIndex)
    {
        EnsureColumns();
        if (colIndex < 0 || colIndex >= Columns.Count) return false;
        var col = Columns[colIndex];
        string? key = (rowIndex >= 0 && rowIndex < col.RowKeys.Count) ? col.RowKeys[rowIndex] : null;
        if (!RemoveColumnsRow(Columns, colIndex, rowIndex)) return false;
        if (rowIndex < col.RowKeys.Count) col.RowKeys.RemoveAt(rowIndex);
        if (!string.IsNullOrEmpty(key)) BagClusterForColumn(colIndex)?.Properties.RemoveAll(p => p.Key == key);
        return true;
    }

    public PropertyCellHit? GetCell(int colIndex, int rowIndex) => FindCell(colIndex, rowIndex);

    public IReadOnlyList<string> GetZoneRows(int colIndex)
    {
        EnsureColumns();
        return GetColumnsRows(Columns, colIndex);
    }

    /// <summary>列内唯一自定义键 custom_N (N = 现有 custom_ 最大序号 + 1).</summary>
    private static string UniqueCustomKey(IsoSpecColumn col)
    {
        int n = 0;
        foreach (var k in col.RowKeys)
            if (k != null && k.StartsWith("custom_") && int.TryParse(k.Substring(7), out var v) && v > n) n = v;
        return $"custom_{n + 1}";
    }

    // ==================== 属性包驱动 (文档级单一真值源 → 填图框) ====================

    private lcdb.Drawing.DrawingPropertyBag? _bag;

    /// <summary>命名字段键 → 实际命中的包键 (ApplyBag 时按 bagKeys 别名解析; 回写用).</summary>
    private readonly Dictionary<string, string> _fieldToBagKey = new();

    /// <summary>绑定的文档级属性包 (画布装载图纸时采纳为 db.DrawingProperties). 瞬态, 不序列化.</summary>
    [JsonIgnore]
    public lcdb.Drawing.DrawingPropertyBag? Bag => _bag;

    /// <summary>
    /// 由属性包填充: title 簇 → 命名字段 (键即键; 定义可给 bagKeys 别名, 用于吃不同生成器的标题键词汇),
    /// notes 簇 → 第一个 notes 带首字段 (多行), 其它簇 → 规格列 (列标题 = 簇 Label, 行值 + 行键).
    /// 之后改格回写到包 (别名字段回写到命中的包键).
    /// </summary>
    public void ApplyBag(lcdb.Drawing.DrawingPropertyBag bag)
    {
        _bag = bag ?? throw new ArgumentNullException(nameof(bag));
        _fieldToBagKey.Clear();
        Columns = new List<IsoSpecColumn>();
        foreach (var c in bag.Clusters)
        {
            if (c.Key == "title")
            {
                var title = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var p in c.Properties) { FieldValues[p.Key] = p.Value; title[p.Key] = p.Value; }
                // 别名解析: 定义键在包里没有 → 按 bagKeys 顺序取第一个存在的包键
                foreach (var nf in Definition.NamedFields())
                {
                    if (title.ContainsKey(nf.Key)) { _fieldToBagKey[nf.Key] = nf.Key; continue; }
                    foreach (var alias in nf.BagKeys)
                        if (title.TryGetValue(alias, out var v))
                        {
                            FieldValues[nf.Key] = v;
                            _fieldToBagKey[nf.Key] = alias;
                            break;
                        }
                }
            }
            else if (c.Key == "notes")
            {
                FieldValues[NotesKey()] = string.Join("\n", c.Properties.Select(p => p.Value));
            }
            else
            {
                var col = new IsoSpecColumn { Title = c.Label };
                foreach (var p in c.Properties) { col.Rows.Add(p.Value); col.RowKeys.Add(p.Key); }
                Columns.Add(col);
            }
        }
        InvalidateRenderCache();
    }

    /// <summary>notes 簇落到的命名字段键: 第一个 notes 带的首字段键 (无则 "Notes").</summary>
    private string NotesKey()
    {
        var b = Definition.FirstBand(FrameDefinition.KindNotes);
        return (b is not null && b.Fields.Count > 0 && !string.IsNullOrEmpty(b.Fields[0].Key)) ? b.Fields[0].Key : "Notes";
    }

    /// <summary>扁平 裸键→值: 绑定文档包则取包 (单一真值源); 否则 命名字段 + 列行键.</summary>
    public IReadOnlyDictionary<string, string> ExportFlat()
    {
        var d = new Dictionary<string, string>();
        if (_bag is not null)
        {
            foreach (var p in _bag.AllProperties)
                if (!string.IsNullOrEmpty(p.Key)) d[p.Key] = p.Value;
            return d;
        }
        foreach (var nf in Definition.NamedFields()) d[nf.Key] = GetField(nf.Key);
        EnsureColumns();
        foreach (var col in Columns)
            for (int i = 0; i < col.Rows.Count && i < col.RowKeys.Count; i++)
                if (!string.IsNullOrEmpty(col.RowKeys[i])) d[col.RowKeys[i]] = col.Rows[i];
        return d;
    }

    /// <summary>第 colIndex 个规格列 ↔ 包中第 colIndex 个非 title/notes 簇 (ApplyBag 的反向).</summary>
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

    // ==================== Entity 覆写 ====================

    protected override DBObject CreateInstance() => new DataDrivenFrame();

    public override object Clone()
    {
        EnsureColumns();   // 克隆带走"有效内容": 未渲染过的图框也先按定义播种
        var c = (DataDrivenFrame)base.Clone();
        c.Definition = Definition.Clone();
        c.FieldValues = new Dictionary<string, string>(FieldValues);
        c.Columns = new List<IsoSpecColumn>(Columns.Count);
        foreach (var col in Columns)
            c.Columns.Add(new IsoSpecColumn
            {
                Title = col.Title,
                Rows = new List<string>(col.Rows),
                RowKeys = new List<string>(col.RowKeys),
            });
        return c;
    }
}
