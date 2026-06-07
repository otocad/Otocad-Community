using System.Collections.Generic;
using LitMath;

namespace lcdb.DrawingFrame;

/// <summary>ISO 规格表的一列: 列标题 + 若干行已格式化文本.</summary>
public sealed class IsoSpecColumn
{
    public string Title { get; set; } = "";
    public List<string> Rows { get; set; } = new();
}

/// <summary>
/// ISO 10110 单透镜零件图框 (竖版, optiland #458 / lutzerb 原型样例风格).
///
/// 与 GB 风格的 <see cref="OpticalDrawingFrame"/> 不同, 本图框底部是 ISO 10110 三列表:
/// <c>SURFACE 1 (FRONT) | MATERIAL | SURFACE 2 (REAR)</c>, 表下接标题栏行 (7 格) 和 NOTES 行.
/// 上部留给透镜视图 + 尺寸标注 (由生成器另行添加).
///
/// 几何: <see cref="DrawingFrame.Origin"/> = 纸张左下角, PaperWidth/Height = A4 竖版 210×297.
/// 主图框边距 <see cref="BorderMargin"/>; 底部块自下而上: NOTES → 标题栏 → 三列表.
/// 不调用 base.Generate (避开基类 4×2 标题栏), 自绘边框 + 底部块.
/// </summary>
public sealed class IsoLensDrawingFrame : DrawingFrame, IPropertyZoneFrame
{
    public override string className => "IsoLensDrawingFrame";

    // -------- 布局参数 (mm) --------

    /// <summary>主图框距纸张边的边距.</summary>
    public double BorderMargin { get; set; } = 8.0;
    /// <summary>三列表每行高.</summary>
    public double TableRowHeight { get; set; } = 6.0;
    /// <summary>标题栏行高.</summary>
    public double TitleRowHeight { get; set; } = 16.0;
    /// <summary>NOTES 行高.</summary>
    public double NotesRowHeight { get; set; } = 12.0;
    /// <summary>正文字高.</summary>
    public double CellTextHeight { get; set; } = 2.5;

    // -------- 规格表内容 (N 列, 每格一行字符串, 已格式化) --------
    // 单透镜 = 3 列 (SURFACE1|MATERIAL|SURFACE2); 双胶合 = 4 列 (3 面 + 材料).

    public List<IsoSpecColumn> Columns { get; set; } = new();

    // -------- 标题栏 7 格 (值 + 标签) --------

    public string ProjectPart { get; set; } = "";
    public string DrawnBy { get; set; } = "";
    public string ApprovedBy { get; set; } = "—";
    public string DrawDate { get; set; } = "";
    public string DrawScale { get; set; } = "1:1";
    public string Sheet { get; set; } = "1 / 1";
    public string Rev { get; set; } = "A";

    // -------- NOTES --------

    public string Notes { get; set; } = "";
    public string GeneralTol { get; set; } = "DIM. IN mm\nGENERAL TOL. PER ISO 10110-11";

    // -------- 渲染 --------

    protected override void Generate()
    {
        _markEntities.Clear();
        _cellHits.Clear();

        double x0 = Origin.X + BorderMargin;
        double y0 = Origin.Y + BorderMargin;
        double x1 = Origin.X + PaperWidth - BorderMargin;
        double y1 = Origin.Y + PaperHeight - BorderMargin;

        // 主图框
        AddRect(x0, y0, x1, y1);

        // 自下而上: NOTES → 标题栏 → 规格表
        double notesTop = y0 + NotesRowHeight;
        double titleTop = notesTop + TitleRowHeight;
        int rowCount = 0;
        foreach (var col in Columns) rowCount = System.Math.Max(rowCount, col.Rows.Count);
        double tableH = (rowCount + 1) * TableRowHeight;   // +1 表头
        double tableTop = titleTop + tableH;

        DrawNotesRow(x0, y0, x1, notesTop);
        DrawTitleRow(x0, notesTop, x1, titleTop);
        DrawSpecTable(x0, titleTop, x1, tableTop);
    }

    // ── NOTES 行 ──
    private void DrawNotesRow(double x0, double y0, double x1, double y1)
    {
        AddRect(x0, y0, x1, y1);
        double rightX = x1 - 52;          // 右侧公差说明区
        AddLine(rightX, y0, rightX, y1);

        double pad = 2.0;
        AddText(x0 + pad, y1 - 4.0, "NOTES", height: 2.2);
        if (!string.IsNullOrWhiteSpace(Notes))
            AddText(x0 + pad, y0 + 2.5, Notes, height: CellTextHeight);

        // 右侧: 通用公差说明 (多行)
        double ty = y1 - 3.5;
        foreach (var line in GeneralTol.Split('\n'))
        {
            AddText(rightX + pad, ty, line, height: 2.0);
            ty -= 3.0;
        }

        // NOTES / 通用公差 可点编辑(命名字段)
        _cellHits.Add(new PropertyCellHit("NOTES", "Notes", new Rectangle2(new Vector2(x0, y0), rightX - x0, y1 - y0)));
        _cellHits.Add(new PropertyCellHit("通用公差", "GeneralTol", new Rectangle2(new Vector2(rightX, y0), (x1 - 12) - rightX, y1 - y0)));

        // 第一角投影识别符号 (NOTES 行右端) — 属图框 furniture, 随图框锁定
        AddProjectionSymbol(x1 - 8, (y0 + y1) * 0.5, 1.76);   // 80% 尺寸
    }

    /// <summary>
    /// 第一角投影识别符号 (GB/ISO 第一视角): 左侧锥台(梯形, 小头朝左) + 右侧两同心圆,
    /// 锥台大端朝向圆. cx,cy = 同心圆圆心.
    /// </summary>
    private void AddProjectionSymbol(double cx, double cy, double s)
    {
        _markEntities.Add(new Circle(new Vector2(cx, cy), s) { color = color });
        _markEntities.Add(new Circle(new Vector2(cx, cy), s * 0.5) { color = color });
        double xBig = cx - s * 1.3;       // 大端 (朝向圆)
        double xSmall = xBig - s * 1.6;   // 小端 (远左)
        AddLine(xBig, cy - s, xBig, cy + s);
        AddLine(xSmall, cy - s * 0.5, xSmall, cy + s * 0.5);
        AddLine(xSmall, cy - s * 0.5, xBig, cy - s);
        AddLine(xSmall, cy + s * 0.5, xBig, cy + s);
        AddLine(xSmall - s * 0.4, cy, cx + s + s * 0.4, cy);
    }

    // ── 标题栏 7 格 ──
    private void DrawTitleRow(double x0, double y0, double x1, double y1)
    {
        AddRect(x0, y0, x1, y1);
        double w = x1 - x0;
        // 7 格相对宽度
        double[] frac = { 0.30, 0.16, 0.13, 0.16, 0.10, 0.08, 0.07 };
        string[] vals = { ProjectPart, DrawnBy, ApprovedBy, DrawDate, DrawScale, Sheet, Rev };
        string[] labels = { "PROJECT  /  PART NUMBER", "DRAWN BY", "APPROVED", "DATE", "SCALE", "SHEET", "REV" };
        string[] keys = { "ProjectPart", "DrawnBy", "ApprovedBy", "DrawDate", "DrawScale", "Sheet", "Rev" };
        string[] zh = { "项目/图号", "绘图", "审核", "日期", "比例", "张次", "版本" };

        double cx = x0;
        double valY = y0 + (y1 - y0) * 0.45;
        double labY = y0 + 1.5;
        for (int i = 0; i < frac.Length; i++)
        {
            double cw = w * frac[i];
            if (i > 0) AddLine(cx, y0, cx, y1);
            AddText(cx + 2.0, valY, vals[i], height: i == 0 ? 3.2 : 2.8);
            AddText(cx + 2.0, labY, labels[i], height: 1.7);
            // 标题栏每格可点编辑(命名字段)
            _cellHits.Add(new PropertyCellHit(zh[i], keys[i], new Rectangle2(new Vector2(cx, y0), cw, y1 - y0)));
            cx += cw;
        }
    }

    // ── N 列规格表 (复用基类共享渲染 + 命中记录) ──
    private void DrawSpecTable(double x0, double y0, double x1, double y1)
        => DrawSpecColumns(x0, y0, x1, y1, Columns, TableRowHeight, 2.5, CellTextHeight);

    // -------- IPropertyZoneFrame (属性区交互编辑) --------

    public PropertyCellHit? HitTestCell(Vector2 modelPoint) => HitTestCellCore(modelPoint);
    public string GetCellText(PropertyCellHit cell)
        => string.IsNullOrEmpty(cell.FieldKey) ? GetColumnsCellText(Columns, cell) : GetField(cell.FieldKey);
    public void SetCellText(PropertyCellHit cell, string text)
    {
        if (string.IsNullOrEmpty(cell.FieldKey)) { SetColumnsCellText(Columns, cell, text); return; }
        SetField(cell.FieldKey, text ?? "");
        InvalidateRenderCache();
    }

    private string GetField(string key) => key switch
    {
        "ProjectPart" => ProjectPart, "DrawnBy" => DrawnBy, "ApprovedBy" => ApprovedBy,
        "DrawDate" => DrawDate, "DrawScale" => DrawScale, "Sheet" => Sheet, "Rev" => Rev,
        "Notes" => Notes, "GeneralTol" => GeneralTol, _ => ""
    };

    private void SetField(string key, string v)
    {
        switch (key)
        {
            case "ProjectPart": ProjectPart = v; break;
            case "DrawnBy": DrawnBy = v; break;
            case "ApprovedBy": ApprovedBy = v; break;
            case "DrawDate": DrawDate = v; break;
            case "DrawScale": DrawScale = v; break;
            case "Sheet": Sheet = v; break;
            case "Rev": Rev = v; break;
            case "Notes": Notes = v; break;
            case "GeneralTol": GeneralTol = v; break;
        }
    }
    public int AddRow(int colIndex, string text = "") => AddColumnsRow(Columns, colIndex, text);
    public int InsertRow(int colIndex, int rowIndex, string text) => InsertColumnsRow(Columns, colIndex, rowIndex, text);
    public bool RemoveRow(int colIndex, int rowIndex) => RemoveColumnsRow(Columns, colIndex, rowIndex);
    public PropertyCellHit? GetCell(int colIndex, int rowIndex) => FindCell(colIndex, rowIndex);
    public System.Collections.Generic.IReadOnlyList<string> GetZoneRows(int colIndex) => GetColumnsRows(Columns, colIndex);

    // -------- Entity 覆写 --------

    protected override DBObject CreateInstance() => new IsoLensDrawingFrame();

    public override object Clone()
    {
        var c = (IsoLensDrawingFrame)base.Clone();
        c.BorderMargin = BorderMargin;
        c.TableRowHeight = TableRowHeight;
        c.TitleRowHeight = TitleRowHeight;
        c.NotesRowHeight = NotesRowHeight;
        c.CellTextHeight = CellTextHeight;
        c.Columns = new List<IsoSpecColumn>(Columns.Count);
        foreach (var col in Columns)
            c.Columns.Add(new IsoSpecColumn { Title = col.Title, Rows = new List<string>(col.Rows) });
        c.ProjectPart = ProjectPart;
        c.DrawnBy = DrawnBy;
        c.ApprovedBy = ApprovedBy;
        c.DrawDate = DrawDate;
        c.DrawScale = DrawScale;
        c.Sheet = Sheet;
        c.Rev = Rev;
        c.Notes = Notes;
        c.GeneralTol = GeneralTol;
        return c;
    }
}
