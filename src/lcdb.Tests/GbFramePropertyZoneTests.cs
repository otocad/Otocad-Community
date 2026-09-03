using System;
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
    /// 模板图框属性区交互编辑 — Slice 3(GB `OpticalDrawingFrame` 三列式,镜像 ISO)。
    ///
    /// 默认布局(Origin=(0,0), A4 横 297×210, MarginLeft=25/Right=5/Bottom=5,
    /// TitleBlockWidth=180, TableGap=4, TableRowHeight=6):
    ///   ix0=25 ix1=292 tbx0=112 → 表 x∈[25,108],colW=(108-25)/3≈27.667;
    ///   列 前表面/材料/后表面 左缘 = 25 / 52.667 / 80.333。
    ///   rowCount=6(材料6行),tableY1=5+7*6=47,headBot=41。
    ///   行 r 值格 y∈[41-(r+1)*6, 41-r*6];行 0=[35,41]。
    ///   ⇒ 材料(列1)行0 中心 ≈ (66.5, 38);前表面(列0)行0 ≈ (38.83, 38);列1 表头 ≈ (66.5, 44)。
    /// </summary>
    [TestClass]
    public class GbFramePropertyZoneTests
    {
        private static OpticalDrawingFrame NewFrame() => new OpticalDrawingFrame { Origin = Vector2.Zero };

        [TestMethod]
        public void Default_columns_mirror_iso_three_zones()
        {
            var frame = NewFrame();
            ((IPropertyZoneFrame)frame).HitTestCell(new Vector2(66.5, 38));  // 触发 Generate → EnsureColumns
            Assert.AreEqual(3, frame.Columns.Count);
            CollectionAssert.AreEqual(
                new[] { "前表面", "材料", "后表面" },
                frame.Columns.Select(c => c.Title).ToArray());
            Assert.AreEqual("N-BK7", frame.Columns[1].Rows[0], "材料列首行=玻璃牌号");
        }

        [TestMethod]
        public void HitTestCell_material_and_surface_cells()
        {
            IPropertyZoneFrame f = NewFrame();
            var mat = f.HitTestCell(new Vector2(66.5, 38));   // 材料列 行0
            Assert.IsTrue(mat.HasValue);
            Assert.AreEqual(1, mat.Value.ColIndex);
            Assert.AreEqual(0, mat.Value.RowIndex);
            Assert.AreEqual("N-BK7", f.GetCellText(mat.Value));

            var front = f.HitTestCell(new Vector2(38.83, 38)); // 前表面列 行0 = FormError 默认 "3/3(0.5)"(面形=3/, 非 4/)
            Assert.IsTrue(front.HasValue);
            Assert.AreEqual(0, front.Value.ColIndex);
            Assert.AreEqual(0, front.Value.RowIndex);
            Assert.AreEqual("3/3(0.5)", f.GetCellText(front.Value));
        }

        [TestMethod]
        public void HitTestCell_on_header_selects_zone()
        {
            IPropertyZoneFrame f = NewFrame();
            var hit = f.HitTestCell(new Vector2(66.5, 44));   // 材料列 表头
            Assert.IsTrue(hit.HasValue);
            Assert.AreEqual(1, hit.Value.ColIndex);
            Assert.IsTrue(hit.Value.RowIndex < 0);
            Assert.AreEqual("材料", f.GetCellText(hit.Value));
        }

        [TestMethod]
        public void HitTestCell_outside_returns_null()
        {
            IPropertyZoneFrame f = NewFrame();
            Assert.IsNull(f.HitTestCell(new Vector2(66.5, 100)), "表上方应 null");
            Assert.IsNull(f.HitTestCell(new Vector2(250, 200)), "标题栏上方空白应 null");
        }

        [TestMethod]
        public void HitTestCell_on_titleblock_field_is_named_field()
        {
            // GB 标题栏(基类 4×2): x∈[112,292] y∈[5,61], th=14, midX=202;
            // 左列上行(y∈[47,61])= 项目/ProjectName。点 (150,54) 命中它。
            IPropertyZoneFrame f = NewFrame();
            var hit = f.HitTestCell(new Vector2(150, 54));
            Assert.IsTrue(hit.HasValue, "应命中标题栏字段");
            Assert.AreEqual("ProjectName", hit.Value.FieldKey);
            f.SetCellText(hit.Value, "OBJ-77");
            Assert.AreEqual("OBJ-77", f.GetCellText(hit.Value));
        }

        [TestMethod]
        public void SetCellText_writes_back_and_round_trips()
        {
            var frame = NewFrame();
            IPropertyZoneFrame f = frame;
            var mat = f.HitTestCell(new Vector2(66.5, 38))!.Value;
            f.SetCellText(mat, "H-K9L");
            Assert.AreEqual("H-K9L", frame.Columns[1].Rows[0]);

            var path = Path.Combine(Path.GetTempPath(), $"gb-frame-{Guid.NewGuid():N}.otocad");
            var db = new Database();
            db.AddEntity(frame);
            try
            {
                OtocadFileFormatV4.Save(db, path);
                var (db2, _) = OtocadFileFormatV4.Load(path);
                var back = db2.GetEntitiesByType<OpticalDrawingFrame>().Single();
                Assert.AreEqual(3, back.Columns.Count);
                Assert.AreEqual("H-K9L", back.Columns[1].Rows[0]);
                CollectionAssert.AreEqual(
                    new[] { "前表面", "材料", "后表面" }, back.Columns.Select(c => c.Title).ToArray());
            }
            finally { try { if (File.Exists(path)) File.Delete(path); } catch { } }
        }
    }
}
