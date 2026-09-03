using System.Collections.Generic;
using LitMath;
using OtoCAD;

namespace lcdb.DrawingFrame;

/// <summary>
/// 工程图图框 — 单一 Entity (内置标题栏), 缓存生成 Line/Text 子元素.
///
/// 几何约定:
/// - <see cref="Origin"/> = 外框左下角
/// - 外框: Origin → Origin + (PaperWidth, PaperHeight)
/// - 内框: 距外框 MarginLeft/Right/Top/Bottom (左侧装订边通常更宽)
/// - 标题栏 (可选): 内框右下角的矩形, 含 8 字段文字
///
/// 设计原则 (对比老 WinForms 的 Frame:BaseElementBlock):
/// - 单实体, 子元素是渲染缓存 (不可单独选择), 简化序列化和操作
/// - InvalidateRenderCache 模式: 改属性后下次 Draw 重算
/// - 不内嵌 lock 状态 — 锁定走 Layer.IsLocked (架构更通用)
/// </summary>
public class DrawingFrame : Entity
{
    public override string className => "DrawingFrame";

    // -------- 几何 (mm) --------

    /// <summary>外框左下角 (放置点).</summary>
    public Vector2 Origin { get; set; } = Vector2.Zero;

    /// <summary>纸张宽 (mm). A4 横=297 / A4 纵=210.</summary>
    public double PaperWidth { get; set; } = 297;

    /// <summary>纸张高 (mm).</summary>
    public double PaperHeight { get; set; } = 210;

    /// <summary>装订边 (左, mm). 工程图标准约 25mm.</summary>
    public double MarginLeft { get; set; } = 25;
    public double MarginRight { get; set; } = 5;
    public double MarginTop { get; set; } = 5;
    public double MarginBottom { get; set; } = 5;

    // -------- 标题栏 (内框右下) --------

    public bool ShowTitleBlock { get; set; } = true;
    /// <summary>标题栏宽 (mm). 常见 180mm.</summary>
    public double TitleBlockWidth { get; set; } = 180;
    /// <summary>标题栏高 (mm). 常见 56mm.</summary>
    public double TitleBlockHeight { get; set; } = 56;

    // -------- 标题栏字段 (中文工程图常见 8 字段) --------

    public string CompanyName { get; set; } = "";
    public string ProjectName { get; set; } = "";
    public string DrawingNumber { get; set; } = "";
    public string DrawingTitle { get; set; } = "";
    public string Designer { get; set; } = "";
    public string DesignDate { get; set; } = "";
    public string Material { get; set; } = "";
    public string Scale { get; set; } = "1:1";

    // -------- 缓存 --------

    /// <summary>子类可 AppendExtra 追加自定义图元 (例如 OpticalDrawingFrame 的双表).</summary>
    protected List<Entity> _markEntities = new();

    /// <summary>
    /// 属性区可编辑值格的命中矩形, 与 <see cref="_markEntities"/> 同生命周期
    /// (同在 Generate 重建)。实现 <see cref="IPropertyZoneFrame"/> 的子类在 Generate
    /// 时填充, 供 <see cref="HitTestCellCore"/> 做点-in-矩形判定。
    /// </summary>
    protected readonly List<PropertyCellHit> _cellHits = new();

    public override Bounding bounding =>
        new Bounding(Origin, new Vector2(Origin.X + PaperWidth, Origin.Y + PaperHeight));

    public override void Draw(IGraphicsDraw gd)
    {
        if (_markEntities.Count == 0) Generate();
        foreach (var e in _markEntities) e.Draw(gd);
    }

    public override void InvalidateRenderCache() => _markEntities.Clear();

    /// <summary>
    /// 生成图框 + 标题栏的所有图元.
    /// 子类可重写, 调 base.Generate() 后追加自己的图元 (例如双表).
    /// </summary>
    protected virtual void Generate()
    {
        _markEntities.Clear();
        _cellHits.Clear();

        // 外框
        var x0 = Origin.X;
        var y0 = Origin.Y;
        var x1 = Origin.X + PaperWidth;
        var y1 = Origin.Y + PaperHeight;
        AddRect(x0, y0, x1, y1);

        // 内框
        var ix0 = x0 + MarginLeft;
        var iy0 = y0 + MarginBottom;
        var ix1 = x1 - MarginRight;
        var iy1 = y1 - MarginTop;
        AddRect(ix0, iy0, ix1, iy1);

        // 标题栏 (内框右下角)
        if (ShowTitleBlock)
        {
            var tbx0 = ix1 - TitleBlockWidth;
            var tby0 = iy0;
            var tbx1 = ix1;
            var tby1 = iy0 + TitleBlockHeight;
            AddRect(tbx0, tby0, tbx1, tby1);

            // 简化 4×2 网格: 第 1 行 标题/比例, 第 2 行 设计/日期, 第 3 行 材料/图号, 第 4 行 项目/公司
            // 标题栏内部分隔线
            var midY1 = tby0 + TitleBlockHeight * 0.25;
            var midY2 = tby0 + TitleBlockHeight * 0.50;
            var midY3 = tby0 + TitleBlockHeight * 0.75;
            var midX  = tbx0 + TitleBlockWidth  * 0.50;
            AddLine(tbx0, midY1, tbx1, midY1);
            AddLine(tbx0, midY2, tbx1, midY2);
            AddLine(tbx0, midY3, tbx1, midY3);
            AddLine(midX, tby0, midX, tby1);

            // 文字 (左下基线对齐, 字号 3.5mm 标准制图)
            var th = TitleBlockHeight * 0.25;
            var pad = 2.0;
            AddText(tbx0 + pad,          tby0 + th * 3 + pad, "项目: "   + ProjectName);
            AddText(midX  + pad,          tby0 + th * 3 + pad, "公司: "   + CompanyName);
            AddText(tbx0 + pad,          tby0 + th * 2 + pad, "材料: "   + Material);
            AddText(midX  + pad,          tby0 + th * 2 + pad, "图号: "   + DrawingNumber);
            AddText(tbx0 + pad,          tby0 + th * 1 + pad, "设计: "   + Designer);
            AddText(midX  + pad,          tby0 + th * 1 + pad, "日期: "   + DesignDate);
            AddText(tbx0 + pad,          tby0          + pad, "标题: "   + DrawingTitle);
            AddText(midX  + pad,          tby0          + pad, "比例: "   + Scale);

            // 标题栏每格可点编辑(命名字段)。仅实现 IPropertyZoneFrame 的子类(如 GB OpticalDrawingFrame)
            // 会被 UI 查询;ISO 不调 base.Generate、纯 DrawingFrame 不实现接口,记录均无害。
            double colW = TitleBlockWidth * 0.50;
            void TitleCell(double cx, double rowBot, string key) =>
                _cellHits.Add(new PropertyCellHit(TitleFieldLabel(key), key, new Rectangle2(new Vector2(cx, rowBot), colW, th)));
            TitleCell(tbx0, tby0 + th * 3, "ProjectName");   TitleCell(midX, tby0 + th * 3, "CompanyName");
            TitleCell(tbx0, tby0 + th * 2, "Material");      TitleCell(midX, tby0 + th * 2, "DrawingNumber");
            TitleCell(tbx0, tby0 + th * 1, "Designer");      TitleCell(midX, tby0 + th * 1, "DesignDate");
            TitleCell(tbx0, tby0,          "DrawingTitle");  TitleCell(midX, tby0,          "Scale");
        }
    }

    /// <summary>标题栏命名字段 → 显示标签。</summary>
    protected static string TitleFieldLabel(string key) => key switch
    {
        "ProjectName" => "项目", "CompanyName" => "公司", "Material" => "材料",
        "DrawingNumber" => "图号", "Designer" => "设计", "DesignDate" => "日期",
        "DrawingTitle" => "标题", "Scale" => "比例", _ => key
    };

    /// <summary>读标题栏命名字段(IPropertyZoneFrame 子类按 FieldKey 分派时复用)。</summary>
    protected string GetTitleField(string key) => key switch
    {
        "ProjectName" => ProjectName, "CompanyName" => CompanyName, "Material" => Material,
        "DrawingNumber" => DrawingNumber, "Designer" => Designer, "DesignDate" => DesignDate,
        "DrawingTitle" => DrawingTitle, "Scale" => Scale, _ => ""
    };

    /// <summary>写标题栏命名字段;命中已知键返回 true。</summary>
    protected bool SetTitleField(string key, string v)
    {
        switch (key)
        {
            case "ProjectName": ProjectName = v; return true;
            case "CompanyName": CompanyName = v; return true;
            case "Material": Material = v; return true;
            case "DrawingNumber": DrawingNumber = v; return true;
            case "Designer": Designer = v; return true;
            case "DesignDate": DesignDate = v; return true;
            case "DrawingTitle": DrawingTitle = v; return true;
            case "Scale": Scale = v; return true;
            default: return false;
        }
    }

    protected void AddRect(double x0, double y0, double x1, double y1)
    {
        _markEntities.Add(new Line(new Vector2(x0, y0), new Vector2(x1, y0)) { color = color });
        _markEntities.Add(new Line(new Vector2(x1, y0), new Vector2(x1, y1)) { color = color });
        _markEntities.Add(new Line(new Vector2(x1, y1), new Vector2(x0, y1)) { color = color });
        _markEntities.Add(new Line(new Vector2(x0, y1), new Vector2(x0, y0)) { color = color });
    }

    protected void AddLine(double x0, double y0, double x1, double y1)
    {
        _markEntities.Add(new Line(new Vector2(x0, y0), new Vector2(x1, y1)) { color = color });
    }

    protected void AddText(double x, double y, string text, double height = 3.5)
    {
        if (string.IsNullOrEmpty(text)) return;
        try
        {
            _markEntities.Add(new MText
            {
                Position = new Vector2(x, y),
                Value = text,
                Height = height,
                color = color,
            });
        }
        catch { /* MText 构造可能要求别的字段, 失败就跳过 */ }
    }

    // -------- 属性区命中 (IPropertyZoneFrame 子类复用) --------

    /// <summary>点-in-矩形命中已记录的可编辑值格 (惰性触发 Generate)。表外返回 null。</summary>
    protected PropertyCellHit? HitTestCellCore(Vector2 modelPoint)
    {
        if (_markEntities.Count == 0) Generate();
        foreach (var h in _cellHits)
            if (RectContains(h.Rect, modelPoint)) return h;
        return null;
    }

    /// <summary>矩形包含判定, 用 min/max 归一化以兼容负宽高。</summary>
    protected static bool RectContains(Rectangle2 r, Vector2 p)
    {
        double x0 = System.Math.Min(r.location.X, r.location.X + r.width);
        double x1 = System.Math.Max(r.location.X, r.location.X + r.width);
        double y0 = System.Math.Min(r.location.Y, r.location.Y + r.height);
        double y1 = System.Math.Max(r.location.Y, r.location.Y + r.height);
        return p.X >= x0 && p.X <= x1 && p.Y >= y0 && p.Y <= y1;
    }

    /// <summary>
    /// 三列(N 列)规格表渲染 — ISO/GB 共用。(x0,y0)=左下角,(x1,y1)=右上角;表头在顶行。
    /// 每行只画自描述的值字符串(行含义靠位置/内容);记录每个值格 + 每个区(列)的命中矩形。
    /// </summary>
    protected void DrawSpecColumns(double x0, double y0, double x1, double y1,
        List<IsoSpecColumn> columns, double rowHeight, double headerTextHeight, double cellTextHeight)
    {
        AddRect(x0, y0, x1, y1);
        int n = System.Math.Max(1, columns.Count);
        double colW = (x1 - x0) / n;
        double headBot = y1 - rowHeight;
        AddLine(x0, headBot, x1, headBot);

        for (int i = 0; i < columns.Count; i++)
        {
            double cl = x0 + i * colW;
            double cr = cl + colW;
            if (i > 0) AddLine(cl, y0, cl, y1);   // 列分隔线
            AddTextCentered(cl, cr, headBot + rowHeight * 0.25, columns[i].Title, headerTextHeight);

            var rows = columns[i].Rows;
            for (int r = 0; r < rows.Count; r++)
            {
                double rowTop = headBot - r * rowHeight;
                AddText(cl + 2.0, rowTop - rowHeight * 0.72, rows[r], cellTextHeight);
                _cellHits.Add(new PropertyCellHit(columns[i].Title, "", i, r,
                    new Rectangle2(new Vector2(cl, rowTop - rowHeight), colW, rowHeight)));
            }
            // 区(列)命中: rowIndex = -1, 矩形 = 整列。登记在该列数据格之后 → 点数据格优先, 点列头落区。
            double colBottom = headBot - rows.Count * rowHeight;
            _cellHits.Add(new PropertyCellHit(columns[i].Title, "", i, -1,
                new Rectangle2(new Vector2(cl, colBottom), colW, y1 - colBottom)));
        }
    }

    /// <summary>在 [xL, xR] 区间按字宽估算水平居中绘制文字。</summary>
    protected void AddTextCentered(double xL, double xR, double y, string text, double height)
    {
        if (string.IsNullOrEmpty(text)) return;
        double estW = text.Length * height * 0.58;
        AddText((xL + xR) * 0.5 - estW * 0.5, y, text, height);
    }

    /// <summary>取某列各行文本(只读)。IPropertyZoneFrame 子类复用。</summary>
    protected static IReadOnlyList<string> GetColumnsRows(List<IsoSpecColumn> columns, int colIndex)
        => (colIndex >= 0 && colIndex < columns.Count) ? columns[colIndex].Rows : new List<string>();

    /// <summary>读三列表某格文本(区命中 rowIndex&lt;0 → 列标题)。IPropertyZoneFrame 子类复用。</summary>
    protected static string GetColumnsCellText(List<IsoSpecColumn> columns, PropertyCellHit cell)
    {
        if (cell.ColIndex < 0 || cell.ColIndex >= columns.Count) return "";
        var col = columns[cell.ColIndex];
        if (cell.RowIndex < 0) return col.Title;
        return cell.RowIndex < col.Rows.Count ? col.Rows[cell.RowIndex] : "";
    }

    /// <summary>写三列表某格文本(区命中 rowIndex&lt;0 → 改列标题)+ 失效缓存。越界返回 false。</summary>
    protected bool SetColumnsCellText(List<IsoSpecColumn> columns, PropertyCellHit cell, string text)
    {
        if (cell.ColIndex < 0 || cell.ColIndex >= columns.Count) return false;
        var col = columns[cell.ColIndex];
        if (cell.RowIndex < 0) { col.Title = text ?? ""; InvalidateRenderCache(); return true; }
        if (cell.RowIndex >= col.Rows.Count) return false;
        col.Rows[cell.RowIndex] = text ?? "";
        InvalidateRenderCache();
        return true;
    }

    /// <summary>在某列指定下标插入一行(夹取到 [0,Count]),返回实际下标(列越界 -1)。IPropertyZoneFrame 子类复用。</summary>
    protected int InsertColumnsRow(List<IsoSpecColumn> columns, int colIndex, int rowIndex, string text)
    {
        if (colIndex < 0 || colIndex >= columns.Count) return -1;
        var rows = columns[colIndex].Rows;
        int idx = System.Math.Max(0, System.Math.Min(rowIndex, rows.Count));
        rows.Insert(idx, text ?? "");
        InvalidateRenderCache();
        return idx;
    }

    /// <summary>向某列末尾加一行,返回新行下标(列越界 -1)。IPropertyZoneFrame 子类复用。</summary>
    protected int AddColumnsRow(List<IsoSpecColumn> columns, int colIndex, string text)
    {
        if (colIndex < 0 || colIndex >= columns.Count) return -1;
        columns[colIndex].Rows.Add(text ?? "");
        InvalidateRenderCache();
        return columns[colIndex].Rows.Count - 1;
    }

    /// <summary>删除某列某行,越界返回 false。</summary>
    protected bool RemoveColumnsRow(List<IsoSpecColumn> columns, int colIndex, int rowIndex)
    {
        if (colIndex < 0 || colIndex >= columns.Count) return false;
        var rows = columns[colIndex].Rows;
        if (rowIndex < 0 || rowIndex >= rows.Count) return false;
        rows.RemoveAt(rowIndex);
        InvalidateRenderCache();
        return true;
    }

    /// <summary>按 (列,行) 在最新命中表里查命中(含矩形),用于增删后重选。无则 null。</summary>
    protected PropertyCellHit? FindCell(int colIndex, int rowIndex)
    {
        if (_markEntities.Count == 0) Generate();
        foreach (var h in _cellHits)
            if (h.ColIndex == colIndex && h.RowIndex == rowIndex) return h;
        return null;
    }

    // -------- Entity 必需 --------

    protected override DBObject CreateInstance() => new DrawingFrame();

    public override object Clone()
    {
        var c = (DrawingFrame)base.Clone();
        CopyTo(c);
        return c;
    }

    /// <summary>子类 Clone 用 — 把 base 字段拷到 c, 然后子类 append 自己的字段拷贝.</summary>
    protected void CopyTo(DrawingFrame c)
    {
        c.Origin = Origin;
        c.PaperWidth = PaperWidth;
        c.PaperHeight = PaperHeight;
        c.MarginLeft = MarginLeft;
        c.MarginRight = MarginRight;
        c.MarginTop = MarginTop;
        c.MarginBottom = MarginBottom;
        c.ShowTitleBlock = ShowTitleBlock;
        c.TitleBlockWidth = TitleBlockWidth;
        c.TitleBlockHeight = TitleBlockHeight;
        c.CompanyName = CompanyName;
        c.ProjectName = ProjectName;
        c.DrawingNumber = DrawingNumber;
        c.DrawingTitle = DrawingTitle;
        c.Designer = Designer;
        c.DesignDate = DesignDate;
        c.Material = Material;
        c.Scale = Scale;
        c._markEntities = new List<Entity>();
    }

    public override void Translate(Vector2 translation)
    {
        Origin += translation;
        _markEntities.Clear();
    }

    public override void Rotate(Vector2 center, double angle)
    {
        Origin = Vector2.RotateInRadian(Origin, center, angle);
        _markEntities.Clear();
    }

    public override void TransformBy(Matrix3 transform)
    {
        Origin = transform * Origin;
        _markEntities.Clear();
    }

    public override List<GripPoint> GetGripPoints() => new()
    {
        new(GripPointType.Corner, Origin),
        new(GripPointType.Corner, new Vector2(Origin.X + PaperWidth, Origin.Y + PaperHeight)),
    };

    public override List<ObjectSnapPoint> GetSnapPoints() => new()
    {
        new(ObjectSnapMode.End, Origin),
        new(ObjectSnapMode.End, new Vector2(Origin.X + PaperWidth, Origin.Y)),
        new(ObjectSnapMode.End, new Vector2(Origin.X, Origin.Y + PaperHeight)),
        new(ObjectSnapMode.End, new Vector2(Origin.X + PaperWidth, Origin.Y + PaperHeight)),
    };

    public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
    {
        if (index == 0)
        {
            // 左下角 grip → 整体平移
            Origin = newPosition;
        }
        else if (index == 1)
        {
            // 右上角 grip → 调整尺寸 (保持 Origin 不变)
            var w = newPosition.X - Origin.X;
            var h = newPosition.Y - Origin.Y;
            if (w > 50) PaperWidth = w;
            if (h > 50) PaperHeight = h;
        }
        _markEntities.Clear();
    }
}
