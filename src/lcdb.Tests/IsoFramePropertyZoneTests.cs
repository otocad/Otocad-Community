using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LitMath;
using lcdb;
using lcdb.DrawingFrame;
using lcdb.IO;

namespace lcdb.Tests
{
    /// <summary>
    /// 模板图框属性区交互编辑 — Slice 1 (ISO `IsoLensDrawingFrame` 三列式) lcdb 层单测。
    ///
    /// 受控布局(Origin=(0,0), Paper 100×120, BorderMargin=8, TableRowHeight=6,
    /// NotesRowHeight=12, TitleRowHeight=16, 3 列 × 3 行):
    ///   x0=8 x1=92 → colW=(92-8)/3=28;列 0/1/2 左缘 = 8/36/64。
    ///   notesTop=20 titleTop=36 tableTop=36+(3+1)*6=60 headBot=60-6=54。
    ///   行 r 值格 y∈[54-(r+1)*6, 54-r*6];行 0/1/2 = [48,54]/[42,48]/[36,42]。
    ///   ⇒ (列1,行1) 中心 = (36+14, 45) = (50,45)。
    /// </summary>
    [TestClass]
    public class IsoFramePropertyZoneTests
    {
        private static IsoLensDrawingFrame NewFrame()
        {
            var f = new IsoLensDrawingFrame
            {
                Origin = Vector2.Zero,
                PaperWidth = 100,
                PaperHeight = 120,
                BorderMargin = 8,
                TableRowHeight = 6,
                NotesRowHeight = 12,
                TitleRowHeight = 16,
                ShowTitleBlock = false,
            };
            f.Columns = new List<IsoSpecColumn>
            {
                new IsoSpecColumn { Title = "SURFACE 1", Rows = new List<string> { "R=10", "Φe=8", "3/ 5" } },
                new IsoSpecColumn { Title = "MATERIAL",  Rows = new List<string> { "N-BK7", "n=1.5168", "ν=64" } },
                new IsoSpecColumn { Title = "SURFACE 2", Rows = new List<string> { "R=-10", "Φe=8", "3/ 5" } },
            };
            return f;
        }

        [TestMethod]
        public void HitTestCell_returns_correct_column_and_row()
        {
            IPropertyZoneFrame f = NewFrame();
            var hit = f.HitTestCell(new Vector2(50, 45));   // 列1 行1
            Assert.IsTrue(hit.HasValue, "应命中 (列1,行1)");
            Assert.AreEqual(1, hit.Value.ColIndex);
            Assert.AreEqual(1, hit.Value.RowIndex);
            Assert.AreEqual("MATERIAL", hit.Value.Zone);
            Assert.AreEqual("n=1.5168", f.GetCellText(hit.Value));
        }

        [TestMethod]
        public void HitTestCell_other_cells()
        {
            IPropertyZoneFrame f = NewFrame();
            // 列0 行0 中心 (8+14, 51) = (22,51)
            var c00 = f.HitTestCell(new Vector2(22, 51));
            Assert.IsTrue(c00.HasValue);
            Assert.AreEqual(0, c00.Value.ColIndex);
            Assert.AreEqual(0, c00.Value.RowIndex);
            Assert.AreEqual("R=10", f.GetCellText(c00.Value));
            // 列2 行2 中心 (64+14, 39) = (78,39)
            var c22 = f.HitTestCell(new Vector2(78, 39));
            Assert.IsTrue(c22.HasValue);
            Assert.AreEqual(2, c22.Value.ColIndex);
            Assert.AreEqual(2, c22.Value.RowIndex);
        }

        [TestMethod]
        public void HitTestCell_on_column_header_selects_zone()
        {
            var frame = NewFrame();
            IPropertyZoneFrame f = frame;
            // 列头行 y∈[54,60],列1 中心 x=50 ⇒ (50,57) 命中"区"(列头,非数据格)
            var hit = f.HitTestCell(new Vector2(50, 57));
            Assert.IsTrue(hit.HasValue, "应命中列头(区)");
            Assert.AreEqual(1, hit.Value.ColIndex);
            Assert.IsTrue(hit.Value.RowIndex < 0, "区命中 RowIndex 应 < 0");
            Assert.AreEqual("MATERIAL", f.GetCellText(hit.Value), "区命中读到的应是列标题");
            // 重命名该区
            f.SetCellText(hit.Value, "GLASS");
            Assert.AreEqual("GLASS", frame.Columns[1].Title);
        }

        [TestMethod]
        public void Data_cell_takes_precedence_over_zone_on_data_rows()
        {
            IPropertyZoneFrame f = NewFrame();
            // 数据行点击仍命中具体格(非区), 即使整列矩形也覆盖该点
            var hit = f.HitTestCell(new Vector2(50, 45));
            Assert.IsTrue(hit.HasValue);
            Assert.AreEqual(1, hit.Value.RowIndex, "数据行应命中格(row=1)而非区(-1)");
        }

        [TestMethod]
        public void HitTestCell_outside_returns_null()
        {
            IPropertyZoneFrame f = NewFrame();
            Assert.IsNull(f.HitTestCell(new Vector2(500, 500)), "表外应返回 null");
            Assert.IsNull(f.HitTestCell(new Vector2(50, 5)), "标题/Notes 下方非值格应返回 null");
        }

        [TestMethod]
        public void SetCellText_writes_back_and_rehits()
        {
            var frame = NewFrame();
            IPropertyZoneFrame f = frame;
            var hit = f.HitTestCell(new Vector2(50, 45))!.Value;
            f.SetCellText(hit, "n=1.6000");
            Assert.AreEqual("n=1.6000", frame.Columns[1].Rows[1], "应写回底层 Columns");
            // 失效缓存后重命中仍正确, 取到新值
            var hit2 = f.HitTestCell(new Vector2(50, 45));
            Assert.IsTrue(hit2.HasValue);
            Assert.AreEqual("n=1.6000", f.GetCellText(hit2.Value));
        }

        [TestMethod]
        public void AddRow_appends_editable_row()
        {
            var frame = NewFrame();
            IPropertyZoneFrame f = frame;
            int before = frame.Columns[1].Rows.Count;       // 材料列 3 行
            int idx = f.AddRow(1, "new");
            Assert.AreEqual(before, idx, "追加在末尾 → 下标=原行数");
            Assert.AreEqual(before + 1, frame.Columns[1].Rows.Count);
            Assert.AreEqual("new", frame.Columns[1].Rows[idx]);
            // 新行可命中(GetCell 取到含矩形的命中,供增后重选)
            var nh = f.GetCell(1, idx);
            Assert.IsTrue(nh.HasValue);
            Assert.AreEqual(1, nh.Value.ColIndex);
            Assert.AreEqual(idx, nh.Value.RowIndex);
        }

        [TestMethod]
        public void InsertRow_places_at_index_and_clamps()
        {
            var frame = NewFrame();
            IPropertyZoneFrame f = frame;
            // 列0(前表面)默认 {R=10, Φe=8, 3/ 5}:在下标1插入"倒角"应落在 R 与 Φe 之间
            int at = f.InsertRow(0, 1, "倒角 C0.3");
            Assert.AreEqual(1, at);
            CollectionAssert.AreEqual(
                new[] { "R=10", "倒角 C0.3", "Φe=8", "3/ 5" },
                frame.Columns[0].Rows.ToArray());
            Assert.AreEqual(frame.Columns[0].Rows.Count, f.InsertRow(0, 999, "尾"), "越界夹取到末尾");
            Assert.AreEqual("尾", frame.Columns[0].Rows[^1]);
            Assert.AreEqual(-1, f.InsertRow(9, 0, "x"), "列越界 -1");
        }

        [TestMethod]
        public void RemoveRow_deletes_and_guards_bounds()
        {
            var frame = NewFrame();
            IPropertyZoneFrame f = frame;
            int before = frame.Columns[0].Rows.Count;
            Assert.IsTrue(f.RemoveRow(0, 0));
            Assert.AreEqual(before - 1, frame.Columns[0].Rows.Count);
            Assert.IsFalse(f.RemoveRow(0, 999), "越界删除返回 false");
            Assert.IsFalse(f.RemoveRow(9, 0), "列越界返回 false");
        }

        [TestMethod]
        public void Edited_columns_round_trip_through_otocad()
        {
            var frame = NewFrame();
            ((IPropertyZoneFrame)frame).SetCellText(
                new PropertyCellHit("MATERIAL", "", 1, 0, default), "H-K9L");
            // 直接改 Columns 也行, 这里验证 Set 后往返
            Assert.AreEqual("H-K9L", frame.Columns[1].Rows[0]);

            var path = Path.Combine(Path.GetTempPath(), $"iso-frame-{Guid.NewGuid():N}.otocad");
            var db = new Database();
            db.AddEntity(frame);
            try
            {
                OtocadFileFormatV4.Save(db, path);
                var (db2, _) = OtocadFileFormatV4.Load(path);
                var back = db2.GetEntitiesByType<IsoLensDrawingFrame>().Single();
                Assert.AreEqual(3, back.Columns.Count);
                Assert.AreEqual("H-K9L", back.Columns[1].Rows[0]);
                Assert.AreEqual("SURFACE 2", back.Columns[2].Title);
                CollectionAssert.AreEqual(
                    new[] { "R=-10", "Φe=8", "3/ 5" }, back.Columns[2].Rows.ToArray());
            }
            finally { try { if (File.Exists(path)) File.Delete(path); } catch { } }
        }
    }
}
