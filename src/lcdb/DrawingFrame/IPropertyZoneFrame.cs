using LitMath;

namespace lcdb.DrawingFrame
{
    /// <summary>
    /// 模板图框底部"属性区"(规格表)中被命中的一个可编辑值格。
    /// 由 <see cref="IPropertyZoneFrame.HitTestCell"/> 返回,作为读写该格文本的句柄。
    /// 这是瞬态命中结果,不参与序列化。
    /// </summary>
    public readonly struct PropertyCellHit
    {
        /// <summary>区显示名(ISO 列标题 / GB "对材料的要求"·"对零件的要求")。</summary>
        public string Zone { get; }

        /// <summary>行标签(显示用):GB 为字段名;ISO 行无独立标签时为空。</summary>
        public string Label { get; }

        /// <summary>区下标:ISO = 第几列;GB = 0 材料表 / 1 零件表。</summary>
        public int ColIndex { get; }

        /// <summary>区内行下标。</summary>
        public int RowIndex { get; }

        /// <summary>命中矩形(模型坐标,值格区域)。供命中判定与高亮渲染。</summary>
        public Rectangle2 Rect { get; }

        /// <summary>
        /// 命名字段键:非空 = 命中的是标题栏/NOTES 等**强类型命名字段**(按 key 读写),
        /// 此时 Col/Row 无意义(-1);为空 = 规格表格(按 Zone/Col/Row 读写)。
        /// </summary>
        public string FieldKey { get; }

        /// <summary>规格表格(列/行)命中。</summary>
        public PropertyCellHit(string zone, string label, int colIndex, int rowIndex, Rectangle2 rect)
        {
            Zone = zone;
            Label = label;
            ColIndex = colIndex;
            RowIndex = rowIndex;
            Rect = rect;
            FieldKey = "";
        }

        /// <summary>命名字段(标题栏/NOTES 等)命中。</summary>
        public PropertyCellHit(string label, string fieldKey, Rectangle2 rect)
        {
            Zone = label;
            Label = label;
            ColIndex = -1;
            RowIndex = -1;
            Rect = rect;
            FieldKey = fieldKey;
        }
    }

    /// <summary>
    /// 模板图框的"属性区可交互编辑"契约:把点击下钻到具体的值格,并读写该格文本。
    /// 由 <see cref="IsoLensDrawingFrame"/>(ISO,列/行)与 <see cref="OpticalDrawingFrame"/>
    /// (GB,材料/零件双表)实现,供 UI 层统一驱动(点格→属性面板编辑+悬停高亮)。
    /// </summary>
    public interface IPropertyZoneFrame
    {
        /// <summary>点-in-矩形命中:返回被点中的值格;表外返回 null。</summary>
        PropertyCellHit? HitTestCell(Vector2 modelPoint);

        /// <summary>读该格当前文本(取持久化值,不含渲染期 override)。</summary>
        string GetCellText(PropertyCellHit cell);

        /// <summary>写该格文本并失效渲染缓存(下次重绘生效)。越界忽略。</summary>
        void SetCellText(PropertyCellHit cell, string text);

        /// <summary>向某区(列)末尾加一指标行(默认空)。返回新行下标;列越界返回 -1。</summary>
        int AddRow(int colIndex, string text = "");

        /// <summary>在某区(列)指定下标插入一行(自动排序用)。返回实际插入下标(夹取到 [0,Count]);列越界返回 -1。</summary>
        int InsertRow(int colIndex, int rowIndex, string text);

        /// <summary>删除某区某行。越界返回 false。</summary>
        bool RemoveRow(int colIndex, int rowIndex);

        /// <summary>取某区/行当前命中(含最新矩形),用于增删后重新选中。无则 null。</summary>
        PropertyCellHit? GetCell(int colIndex, int rowIndex);

        /// <summary>取某区(列)当前各行文本——供属性面板列出"可加指标"并判断哪些已加。</summary>
        System.Collections.Generic.IReadOnlyList<string> GetZoneRows(int colIndex);
    }
}
