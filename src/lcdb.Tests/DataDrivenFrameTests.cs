using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LitMath;
using lcdb;
using lcdb.Checklist;
using lcdb.Drawing;
using lcdb.DrawingFrame;
using lcdb.IO;
using lcdb.NetDxfAdapter;
using lcdb.Optic;

namespace lcdb.Tests
{
    /// <summary>
    /// 数据驱动图框 (模板系统 Phase 2a): 定义 JSON → 通用渲染器 → 属性区交互 → 内嵌序列化往返.
    ///
    /// 样例定义 (Origin=(0,0), A4 纵 210×297, margin 8 ⇒ 内框 x∈[8,202], y∈[8,289]):
    ///   notes 带 (h12) y∈[8,20]: NOTES 主区 x∈[8,150], 通用公差盒 x∈[150,202] (宽 52, 右端 12 让给投影符号 → 命中到 190)
    ///   title-grid (h16, 8 格) y∈[20,36]: cw=194/8=24.25 → 零件 [8,80.75] 制图 [80.75,105] 比例 [105,129.25]
    ///   spec-table (2 列 × 2 行, 行高 6) y∈[36,54]: 表头 [48,54], 行0 [42,48], 行1 [36,42]; colW=97 → 列0 [8,105] 列1 [105,202]
    ///   投影符号: 圆心 (194, 14), 大圆 r=1.76
    /// </summary>
    [TestClass]
    public class DataDrivenFrameTests
    {
        private const string SampleJson = """
        {
          "name": "GB-my-1", "title": "我厂光学件图框", "paper": "A4P",
          "border": { "margin": 8 },
          "bands": [
            { "kind": "notes", "height": 12,
              "fields": [ { "label": "NOTES", "key": "Notes" },
                          { "label": "通用公差", "key": "GeneralTol", "width": 52, "default": "DIM. IN mm" } ] },
            { "kind": "title-grid", "height": 16, "cols": 8,
              "cells": [ { "key": "ProjectPart", "label": "零件", "col": 0, "w": 3 },
                         { "key": "DrawnBy", "label": "制图", "col": 3, "w": 1 },
                         { "key": "DrawScale", "label": "比例", "col": 4, "w": 1, "default": "1:1" } ] },
            { "kind": "spec-table", "rowHeight": 6,
              "columns": [ { "title": "前表面", "seed": [ "3/3(0.5)", "5/3×0.1" ], "seedKeys": [ "form_front", "imperf_front" ] },
                           { "title": "材料",   "seed": [ "N-BK7", "n_d 1.5168" ], "seedKeys": [ "material_name", "n_d" ] } ] }
          ],
          "furniture": [ { "kind": "projection-symbol", "angle": "first", "at": "notes-right" } ],
          "style": { "cellTextHeight": 2.5 }
        }
        """;

        private static FrameDefinition Sample() => FrameDefinition.FromJson(SampleJson);
        private static DataDrivenFrame NewFrame() => new DataDrivenFrame(Sample()) { Origin = Vector2.Zero };

        /// <summary>记录型 IGraphicsDraw: 捕获线段 / 圆 / 文字, 供几何断言.</summary>
        private sealed class RecordingDraw : MockGraphicsDraw, OtoCAD.IGraphicsDraw
        {
            public readonly List<(Vector2 A, Vector2 B)> Lines = new();
            public readonly List<(Vector2 C, double R)> Circles = new();
            public readonly List<string> Texts = new();
            public new void DrawLine(Vector2 a, Vector2 b) => Lines.Add((a, b));
            public new void DrawCircle(Vector2 c, double r) => Circles.Add((c, r));
            public new Vector2 DrawText(Vector2 p, string text, double h, string font, TextAlignment al, double angle)
            { Texts.Add(text); return p; }
            public new Vector2 DrawText(Vector3 p, string text, double h, string font, TextAlignment al, double angle)
            { Texts.Add(text); return new Vector2(p.X, p.Y); }
        }

        private static RecordingDraw Render(DataDrivenFrame f)
        {
            var rec = new RecordingDraw();
            f.Draw(rec);
            return rec;
        }

        // ========== 定义: 解析 / 校验 / 纸张 ==========

        [TestMethod]
        public void FromJson_parses_design_sample_camelCase_case_insensitive()
        {
            var d = Sample();
            Assert.AreEqual("GB-my-1", d.Name);
            Assert.AreEqual("A4P", d.Paper);
            Assert.AreEqual(8, d.Border.Margin);
            Assert.AreEqual(3, d.Bands.Count);
            Assert.AreEqual(FrameDefinition.KindNotes, d.Bands[0].Kind);
            Assert.AreEqual(2, d.Bands[0].Fields.Count);
            Assert.AreEqual(52, d.Bands[0].Fields[1].Width);
            Assert.AreEqual(FrameDefinition.KindTitleGrid, d.Bands[1].Kind);
            Assert.AreEqual(3, d.Bands[1].Cells[0].W, "w → W 大小写不敏感");
            Assert.AreEqual(FrameDefinition.KindSpecTable, d.Bands[2].Kind);
            Assert.AreEqual(2, d.Bands[2].Columns[0].Seed.Count);
            Assert.AreEqual("form_front", d.Bands[2].Columns[0].SeedKeys[0]);
            Assert.AreEqual(1, d.Furniture.Count);
            Assert.AreEqual("first", d.Furniture[0].Angle);
            Assert.AreEqual(2.5, d.Style.CellTextHeight);
            Assert.AreEqual(0, d.Validate().Count, string.Join("; ", d.Validate()));
        }

        [TestMethod]
        public void ToJson_round_trips_and_emits_camelCase()
        {
            var d = Sample();
            string json = d.ToJson();
            StringAssert.Contains(json, "\"kind\"");
            StringAssert.Contains(json, "\"rowHeight\"");
            StringAssert.Contains(json, "\"seedKeys\"");
            var back = FrameDefinition.FromJson(json);
            Assert.AreEqual(d.Bands.Count, back.Bands.Count);
            Assert.AreEqual(d.Bands[1].Cells.Count, back.Bands[1].Cells.Count);
            Assert.AreEqual("GeneralTol", back.Bands[0].Fields[1].Key);
            CollectionAssert.AreEqual(
                d.NamedFields().Select(f => f.Key).ToList(),
                back.NamedFields().Select(f => f.Key).ToList());
        }

        [TestMethod]
        public void Validate_reports_structural_errors()
        {
            var bad = FrameDefinition.FromJson("""
            {
              "name": "", "paper": "B5",
              "bands": [
                { "kind": "footer" },
                { "kind": "notes", "fields": [ { "key": "Notes" }, { "key": "Notes", "width": 10 } ] },
                { "kind": "title-grid", "cols": 4, "cells": [ { "key": "A", "col": 3, "w": 2 } ] },
                { "kind": "spec-table", "columns": [] },
                { "kind": "spec-table", "columns": [ { "title": "X" } ] }
              ],
              "furniture": [ { "kind": "clock" }, { "kind": "general-roughness" } ]
            }
            """);
            var errors = bad.Validate();
            Assert.IsTrue(errors.Any(e => e.Contains("name")), "空 name");
            Assert.IsTrue(errors.Any(e => e.Contains("paper")), "非法纸张代码");
            Assert.IsTrue(errors.Any(e => e.Contains("未知带种类 'footer'")), "未知带");
            Assert.IsTrue(errors.Any(e => e.Contains("命名字段键重复: 'Notes'")), "重复键");
            Assert.IsTrue(errors.Any(e => e.Contains("col/w 越出")), "格越界");
            Assert.IsTrue(errors.Any(e => e.Contains("至少一列")), "空表");
            Assert.IsTrue(errors.Any(e => e.Contains("最多一个 spec-table")), "两张表");
            Assert.IsTrue(errors.Any(e => e.Contains("未知种类 'clock'")), "未知 furniture");
            Assert.IsTrue(errors.Any(e => e.Contains("general-roughness 须给 key")), "粗糙度缺键");
        }

        [TestMethod]
        public void TryParsePaper_codes()
        {
            Assert.IsTrue(DrawingFrameTemplates.TryParsePaper("A4P", out var w, out var h));
            Assert.AreEqual((210.0, 297.0), (w, h));
            Assert.IsTrue(DrawingFrameTemplates.TryParsePaper("A3L", out w, out h));
            Assert.AreEqual((420.0, 297.0), (w, h));
            Assert.IsTrue(DrawingFrameTemplates.TryParsePaper("a4l", out w, out h), "大小写不敏感");
            Assert.AreEqual((297.0, 210.0), (w, h));
            Assert.IsFalse(DrawingFrameTemplates.TryParsePaper("auto", out _, out _));
            Assert.IsFalse(DrawingFrameTemplates.TryParsePaper("A5P", out _, out _));
            Assert.IsFalse(DrawingFrameTemplates.TryParsePaper("", out _, out _));
            Assert.IsFalse(DrawingFrameTemplates.TryParsePaper(null, out _, out _));
        }

        [TestMethod]
        public void Constructor_applies_explicit_paper_and_auto_keeps_default()
        {
            var f = NewFrame();
            Assert.AreEqual(210, f.PaperWidth);
            Assert.AreEqual(297, f.PaperHeight);

            var d = Sample();
            d.Paper = "auto";
            var g = new DataDrivenFrame(d);
            Assert.AreEqual(297, g.PaperWidth, "auto: 沿用基类默认 A4 横, 由布局引擎定");
            Assert.AreEqual(210, g.PaperHeight);
        }

        // ========== 渲染: 带堆叠 + 命中 ==========

        [TestMethod]
        public void Bands_stack_bottom_up_and_bottom_block_height_sums()
        {
            var f = NewFrame();
            Assert.AreEqual(12, f.BandHeight(f.Definition.Bands[0]));
            Assert.AreEqual(16, f.BandHeight(f.Definition.Bands[1]));
            Assert.AreEqual(18, f.BandHeight(f.Definition.Bands[2]), "(2 行 + 表头) × 6");
            Assert.AreEqual(46, f.BottomBlockHeight());
        }

        [TestMethod]
        public void HitTestCell_notes_fields_and_projection_reserve()
        {
            IPropertyZoneFrame f = NewFrame();
            var notes = f.HitTestCell(new Vector2(30, 14));
            Assert.IsTrue(notes.HasValue);
            Assert.AreEqual("Notes", notes!.Value.FieldKey);
            Assert.AreEqual("", f.GetCellText(notes.Value), "未设值且无 default → 空");

            var tol = f.HitTestCell(new Vector2(170, 14));
            Assert.IsTrue(tol.HasValue);
            Assert.AreEqual("GeneralTol", tol!.Value.FieldKey);
            Assert.AreEqual("DIM. IN mm", f.GetCellText(tol.Value), "未设值 → 定义 default");

            Assert.IsFalse(f.HitTestCell(new Vector2(195, 14)).HasValue, "最右 12mm 让给投影符号, 不命中");
        }

        [TestMethod]
        public void HitTestCell_title_grid_cells_by_grid_units()
        {
            IPropertyZoneFrame f = NewFrame();
            var part = f.HitTestCell(new Vector2(30, 28));
            Assert.AreEqual("ProjectPart", part!.Value.FieldKey);
            Assert.AreEqual("零件", part.Value.Label);

            var drawn = f.HitTestCell(new Vector2(90, 28));
            Assert.AreEqual("DrawnBy", drawn!.Value.FieldKey);

            var scale = f.HitTestCell(new Vector2(115, 28));
            Assert.AreEqual("DrawScale", scale!.Value.FieldKey);
            Assert.AreEqual("1:1", f.GetCellText(scale.Value));

            Assert.IsFalse(f.HitTestCell(new Vector2(160, 28)).HasValue, "网格未定义的区域不命中");
        }

        [TestMethod]
        public void HitTestCell_spec_table_cells_rows_and_zone_header()
        {
            IPropertyZoneFrame f = NewFrame();
            var c00 = f.HitTestCell(new Vector2(20, 45));
            Assert.AreEqual((0, 0), (c00!.Value.ColIndex, c00.Value.RowIndex));
            Assert.AreEqual("3/3(0.5)", f.GetCellText(c00.Value));
            Assert.AreEqual("前表面", c00.Value.Zone);

            var c11 = f.HitTestCell(new Vector2(150, 39));
            Assert.AreEqual((1, 1), (c11!.Value.ColIndex, c11.Value.RowIndex));
            Assert.AreEqual("n_d 1.5168", f.GetCellText(c11.Value));

            var zone = f.HitTestCell(new Vector2(150, 51));
            Assert.AreEqual((1, -1), (zone!.Value.ColIndex, zone.Value.RowIndex), "点表头落区");
            Assert.AreEqual("材料", f.GetCellText(zone.Value));

            CollectionAssert.AreEqual(new[] { "N-BK7", "n_d 1.5168" }, f.GetZoneRows(1).ToArray());
        }

        [TestMethod]
        public void SetCellText_named_field_and_table_cell_persist_and_export()
        {
            var frame = NewFrame();
            IPropertyZoneFrame f = frame;
            var part = f.HitTestCell(new Vector2(30, 28))!.Value;
            f.SetCellText(part, "PCX-007");
            Assert.AreEqual("PCX-007", frame.FieldValues["ProjectPart"]);
            Assert.AreEqual("PCX-007", f.GetCellText(f.HitTestCell(new Vector2(30, 28))!.Value), "重渲后仍读到");

            var c00 = f.HitTestCell(new Vector2(20, 45))!.Value;
            f.SetCellText(c00, "3/5(1)");
            Assert.AreEqual("3/5(1)", frame.Columns[0].Rows[0]);

            var flat = frame.ExportFlat();
            Assert.AreEqual("PCX-007", flat["ProjectPart"]);
            Assert.AreEqual("1:1", flat["DrawScale"], "未设值命名字段导出 default");
            Assert.AreEqual("3/5(1)", flat["form_front"]);
            Assert.AreEqual("n_d 1.5168", flat["n_d"]);
        }

        [TestMethod]
        public void AddRow_InsertRow_RemoveRow_assign_custom_keys()
        {
            var frame = NewFrame();
            IPropertyZoneFrame f = frame;
            int idx = f.AddRow(0, "自定义 A");
            Assert.AreEqual(2, idx);
            Assert.AreEqual("custom_1", frame.Columns[0].RowKeys[2]);
            Assert.AreEqual("自定义 A", frame.ExportFlat()["custom_1"]);

            int ins = f.InsertRow(0, 0, "插到最前");
            Assert.AreEqual(0, ins);
            Assert.AreEqual("custom_2", frame.Columns[0].RowKeys[0]);
            Assert.AreEqual("form_front", frame.Columns[0].RowKeys[1], "原行键随行后移");

            Assert.IsTrue(f.RemoveRow(0, 0));
            Assert.AreEqual("form_front", frame.Columns[0].RowKeys[0]);
            Assert.IsFalse(frame.ExportFlat().ContainsKey("custom_2"));
            Assert.IsFalse(f.RemoveRow(5, 0), "列越界");

            Assert.IsTrue(f.GetCell(0, 2).HasValue, "增行后表格重生成, 新行可命中");
            Assert.AreEqual(3 * 6 + 6, frame.BandHeight(frame.Definition.Bands[2]), "3 行 + 表头");
        }

        // ========== furniture ==========

        [TestMethod]
        public void Projection_symbol_first_angle_cone_left_of_circles_third_angle_mirrored()
        {
            var first = NewFrame();
            var rec = Render(first);
            var circles = rec.Circles.Where(c => Math.Abs(c.C.Y - 14) < 1e-6).ToList();
            Assert.AreEqual(2, circles.Count, "两同心圆");
            Assert.IsTrue(circles.All(c => Math.Abs(c.C.X - 194) < 1e-6), "圆心 x = 内框右缘 - 8");
            CollectionAssert.AreEquivalent(new[] { 1.76, 0.88 }, circles.Select(c => c.R).ToArray());

            static List<(Vector2 A, Vector2 B)> SymbolVerticals(RecordingDraw r) => r.Lines
                .Where(l => Math.Abs(l.A.X - l.B.X) < 1e-9 && l.A.Y > 11 && l.A.Y < 17 && l.B.Y > 11 && l.B.Y < 17
                            && l.A.X > 180)
                .ToList();
            var v1 = SymbolVerticals(rec);
            Assert.AreEqual(2, v1.Count, "锥台两条竖边");
            Assert.IsTrue(v1.All(l => l.A.X < 194 - 1.76), "第一角: 锥台在圆左侧");

            var d = Sample();
            d.Furniture[0].Angle = "third";
            var third = new DataDrivenFrame(d);
            var v3 = SymbolVerticals(Render(third));
            Assert.AreEqual(2, v3.Count);
            Assert.IsTrue(v3.All(l => l.A.X > 194 + 1.76), "第三角: 镜像到圆右侧");
        }

        [TestMethod]
        public void General_roughness_furniture_is_editable_named_field()
        {
            var d = Sample();
            d.Furniture.Add(new FrameFurniture
            {
                Kind = FrameDefinition.FurnitureGeneralRoughness, At = "top-right", Key = "general_roughness", Default = "Ra 1.6",
            });
            Assert.AreEqual(0, d.Validate().Count);
            var frame = new DataDrivenFrame(d);
            IPropertyZoneFrame f = frame;
            // top-right: 文字左起 (x1-30, y1-8) = (172, 281); 命中矩形 30×8 从 (171,280)
            var hit = f.HitTestCell(new Vector2(185, 284));
            Assert.IsTrue(hit.HasValue, "其余 粗糙度可点编辑");
            Assert.AreEqual("general_roughness", hit!.Value.FieldKey);
            Assert.AreEqual("Ra 1.6", f.GetCellText(hit.Value));
            f.SetCellText(hit.Value, "Ra 3.2");
            var rec = Render(frame);
            Assert.IsTrue(rec.Texts.Contains("Ra 3.2"));
            Assert.IsTrue(rec.Texts.Contains("其余"));
        }

        [TestMethod]
        public void Shared_grid_edges_are_drawn_once()
        {
            var rec = Render(NewFrame());
            var seen = new HashSet<string>();
            foreach (var (a, b) in rec.Lines)
            {
                string k1 = $"{a.X:F3},{a.Y:F3}-{b.X:F3},{b.Y:F3}";
                string k2 = $"{b.X:F3},{b.Y:F3}-{a.X:F3},{a.Y:F3}";
                // 允许基类规格表与外框共边 (DrawSpecColumns 自画矩形); 标题栏格之间不得重线
                if (a.Y > 20 && a.Y < 36 && b.Y > 20 && b.Y < 36 && Math.Abs(a.X - b.X) < 1e-9)
                    Assert.IsTrue(seen.Add(k1) && !seen.Contains(k2), $"标题栏竖线重复: {k1}");
            }
        }

        // ========== 序列化 / 克隆 ==========

        [TestMethod]
        public void Definition_fields_and_columns_survive_otocad_v4_round_trip()
        {
            var path = Path.Combine(Path.GetTempPath(), $"ddf-{Guid.NewGuid():N}.otocad");
            var db = new Database();
            var frame = NewFrame();
            frame.FieldValues["ProjectPart"] = "PCX-007";
            ((IPropertyZoneFrame)frame).AddRow(0, "自定义行");
            db.AddEntity(frame);
            try
            {
                OtocadFileFormatV4.Save(db, path);
                var (db2, _) = OtocadFileFormatV4.Load(path);
                var f2 = db2.GetEntities().OfType<DataDrivenFrame>().SingleOrDefault();
                Assert.IsNotNull(f2, "DataDrivenFrame 应按 kind 往返");
                Assert.AreEqual("GB-my-1", f2!.Definition.Name);
                Assert.AreEqual(3, f2.Definition.Bands.Count, "定义内嵌随文档走");
                Assert.AreEqual(3, f2.Definition.Bands[1].Cells.Count);
                Assert.AreEqual(210, f2.PaperWidth);
                Assert.AreEqual("PCX-007", f2.FieldValues["ProjectPart"]);
                Assert.AreEqual(3, f2.Columns[0].Rows.Count);
                Assert.AreEqual("custom_1", f2.Columns[0].RowKeys[2]);
                Assert.AreEqual("自定义行", f2.Columns[0].Rows[2]);

                // 载回后无需本机预设文件即可渲染 + 交互
                IPropertyZoneFrame pz = f2;
                var part = pz.HitTestCell(new Vector2(30, 28));
                Assert.IsTrue(part.HasValue);
                Assert.AreEqual("PCX-007", pz.GetCellText(part!.Value));
            }
            finally { try { if (File.Exists(path)) File.Delete(path); } catch { } }
        }

        [TestMethod]
        public void Clone_is_deep()
        {
            var a = NewFrame();
            a.FieldValues["ProjectPart"] = "A";
            var b = (DataDrivenFrame)a.Clone();
            b.FieldValues["ProjectPart"] = "B";
            b.Definition.Name = "other";
            b.Columns[0].Rows[0] = "changed";
            ((IPropertyZoneFrame)b).AddRow(1, "x");

            Assert.AreEqual("A", a.FieldValues["ProjectPart"]);
            Assert.AreEqual("GB-my-1", a.Definition.Name);
            Assert.AreEqual("3/3(0.5)", a.Columns[0].Rows[0]);
            Assert.AreEqual(2, a.Columns[1].Rows.Count);
            Assert.AreEqual(210, b.PaperWidth);
            Assert.IsTrue(((IPropertyZoneFrame)b).HitTestCell(new Vector2(30, 28)).HasValue, "克隆体可独立渲染");
        }

        // ========== 属性包驱动 ==========

        [TestMethod]
        public void ApplyBag_fills_fields_notes_columns_and_edits_write_back()
        {
            var bag = new DrawingPropertyBag();
            bag.GetOrAddCluster("title", "标题栏").Add("ProjectPart", "零件", "X-1");
            var notes = bag.GetOrAddCluster("notes", "注释");
            notes.Add("note1", "", "注 1");
            notes.Add("note2", "", "注 2");
            bag.GetOrAddCluster("surface-front", "前表面").Add("form_front", "面形", "3/5(1)");
            bag.GetOrAddCluster("material", "材料").Add("material_name", "牌号", "H-K9L");

            var frame = NewFrame();
            frame.ApplyBag(bag);
            Assert.AreSame(bag, frame.Bag);
            Assert.AreEqual("X-1", frame.FieldValues["ProjectPart"]);
            Assert.AreEqual("注 1\n注 2", frame.FieldValues["Notes"], "notes 簇 → 第一个 notes 带首字段");
            Assert.AreEqual(2, frame.Columns.Count);
            Assert.AreEqual("前表面", frame.Columns[0].Title);
            Assert.AreEqual("form_front", frame.Columns[0].RowKeys[0]);
            Assert.AreEqual("H-K9L", frame.Columns[1].Rows[0]);

            // 1 行表 → 表高 12, y∈[36,48], 行0 y∈[36,42]
            IPropertyZoneFrame f = frame;
            var c00 = f.HitTestCell(new Vector2(20, 39))!.Value;
            Assert.AreEqual((0, 0), (c00.ColIndex, c00.RowIndex));
            f.SetCellText(c00, "3/6(1)");
            Assert.AreEqual("3/6(1)", bag.GetValue("form_front"), "改格回写包");

            f.SetCellText(f.HitTestCell(new Vector2(30, 28))!.Value, "X-2");
            Assert.AreEqual("X-2", bag.GetValue("ProjectPart"), "命名字段键即包键");

            int row = f.AddRow(0, "自定义");
            Assert.AreEqual(2, bag.Cluster("surface-front")!.Properties.Count, "增行同步进包");
            Assert.IsTrue(f.RemoveRow(0, row));
            Assert.AreEqual(1, bag.Cluster("surface-front")!.Properties.Count);

            frame.ApplyBag(bag);   // 一键重出: 包为真值, 改动保留
            Assert.AreEqual("3/6(1)", frame.Columns[0].Rows[0]);
            Assert.AreEqual("3/6(1)", frame.ExportFlat()["form_front"]);
        }

        [TestMethod]
        public void ApplyBag_resolves_bag_key_aliases_and_writes_back_to_matched_key()
        {
            var d = Sample();
            var part = d.Bands[1].Cells.Single(c => c.Key == "ProjectPart");
            part.BagKeys = new List<string> { "product_number", "DrawingTitle" };   // 包里只有第二个
            var frame = new DataDrivenFrame(d);

            var bag = new DrawingPropertyBag();
            bag.GetOrAddCluster("title", "标题栏").Add("DrawingTitle", "名称", "N-BK7 透镜");
            frame.ApplyBag(bag);
            Assert.AreEqual("N-BK7 透镜", frame.GetField("ProjectPart"), "定义键不在包里 → 按 bagKeys 顺序取第一个存在的");

            IPropertyZoneFrame f = frame;
            f.SetCellText(f.HitTestCell(new Vector2(30, 28))!.Value, "PCX-007 透镜");
            Assert.AreEqual("PCX-007 透镜", bag.GetValue("DrawingTitle"), "回写到命中的别名键");
            Assert.IsNull(bag.GetValue("ProjectPart"), "不凭空往包里塞定义键");

            // 定义键本身在包里 → 直接键优先于别名
            var bag2 = new DrawingPropertyBag();
            var t2 = bag2.GetOrAddCluster("title", "标题栏");
            t2.Add("DrawingTitle", "名称", "别名值");
            t2.Add("ProjectPart", "零件", "直接值");
            frame.ApplyBag(bag2);
            Assert.AreEqual("直接值", frame.GetField("ProjectPart"));
        }

        [TestMethod]
        public void Both_bag_driven_frames_implement_IPropertyBagFrame()
        {
            IPropertyBagFrame ddf = NewFrame();
            IPropertyBagFrame gb = new GbLensDrawingFrame();
            Assert.IsNull(ddf.Bag);
            Assert.IsNull(gb.Bag);
            var bag = GbLensSheet.BuildBag(new OpticalLens { Diameter = 25, Thickness = 4, R1 = 50, R2 = -50, MaterialName = "N-BK7" });
            ddf.ApplyBag(bag);
            gb.ApplyBag(bag);
            Assert.AreSame(bag, ddf.Bag);
            Assert.AreSame(bag, gb.Bag);
        }

        [TestMethod]
        public void Shipped_presets_title_cells_resolve_gb_sheet_bag_keys()
        {
            // 一键出图走 GbLensSheet.BuildBag (标题键 DrawingTitle/Designer/Scale…): 三个预设的标题格都应吃到
            var lens = new OpticalLens { Diameter = 25, Thickness = 4, R1 = 50, R2 = -50, MaterialName = "N-BK7" };
            foreach (var (res, json) in ShippedPresets())
            {
                var d = FrameDefinition.FromJson(json);
                var frame = new DataDrivenFrame(d);
                var bag = GbLensSheet.BuildBag(lens);
                bag.SetValue("Scale", "2:1");
                frame.ApplyBag(bag);
                string titleKey = d.Name == "gb-lens" ? "name" : "ProjectPart";
                string scaleKey = d.Name == "gb-lens" ? "scale" : "DrawScale";
                Assert.AreEqual("N-BK7 透镜", frame.GetField(titleKey), $"{d.Name}: 标题格应经 bagKeys 吃到 DrawingTitle");
                Assert.AreEqual("2:1", frame.GetField(scaleKey), $"{d.Name}: 比例格应吃到 Scale");
                Assert.AreEqual(3, frame.Columns.Count, $"{d.Name}: 属性区列 = 包的 材料/前表面/后表面 三簇");
            }
        }

        // ========== 出图清单 frame-is ==========

        [TestMethod]
        public void Checklist_frame_is_matches_embedded_definition_name()
        {
            static CheckStatus FrameIs(string frameName, Entity frame)
                => ChecklistEvaluator.Evaluate(
                        ChecklistDefinition.Parse($"version: 1\nname: t\nframe: {frameName}\nitems: []"), new[] { frame })
                    .Items.Single(i => i.Key == "frame-is").Status;

            var frame = NewFrame();
            Assert.AreEqual(CheckStatus.Pass, FrameIs("GB-my-1", frame));
            Assert.AreEqual(CheckStatus.Pass, FrameIs("gb-my-1", frame), "大小写不敏感");
            Assert.AreEqual(CheckStatus.Fail, FrameIs("GB-my-2", frame));
            Assert.AreEqual(CheckStatus.Fail, FrameIs("gb-optical", frame), "内置名不接受数据驱动图框冒充");
            Assert.AreEqual(CheckStatus.Fail, FrameIs("GB-my-1", new OpticalDrawingFrame()));
        }

        // ========== 出厂预设 (嵌入 SHIPPED Config/Frames/*.json, 改坏即红) ==========

        private static IEnumerable<(string Name, string Json)> ShippedPresets()
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var n in asm.GetManifestResourceNames())
            {
                if (!n.EndsWith(".json", StringComparison.Ordinal) || !n.Contains("Frames")) continue;
                using var s = asm.GetManifestResourceStream(n)!;
                using var r = new StreamReader(s);
                yield return (n, r.ReadToEnd());
            }
        }

        [TestMethod]
        public void Shipped_presets_parse_validate_render_and_export_all_keys()
        {
            var presets = ShippedPresets().ToList();
            Assert.AreEqual(3, presets.Count, "iso-lens / gb-lens / gb-my-1 三个预设应嵌入");
            var names = new List<string>();
            foreach (var (res, json) in presets)
            {
                var d = FrameDefinition.FromJson(json);
                var errors = d.Validate();
                Assert.AreEqual(0, errors.Count, $"{res}: {string.Join("; ", errors)}");
                names.Add(d.Name);

                var frame = new DataDrivenFrame(d);
                Assert.AreEqual(210, frame.PaperWidth, $"{d.Name}: 预设均 A4 纵");
                var rec = Render(frame);
                Assert.IsTrue(rec.Lines.Count >= 20, $"{d.Name}: 线段太少 ({rec.Lines.Count})");
                Assert.IsTrue(rec.Texts.Count >= 10, $"{d.Name}: 文字太少 ({rec.Texts.Count})");

                var flat = frame.ExportFlat();
                foreach (var nf in d.NamedFields())
                    Assert.IsTrue(flat.ContainsKey(nf.Key), $"{d.Name}: 命名字段 {nf.Key} 未导出");
                var table = d.FirstBand(FrameDefinition.KindSpecTable)!;
                foreach (var col in table.Columns)
                {
                    Assert.AreEqual(col.Seed.Count, col.SeedKeys.Count, $"{d.Name}/{col.Title}: seed 与 seedKeys 须等长");
                    foreach (var k in col.SeedKeys)
                        Assert.IsTrue(flat.ContainsKey(k), $"{d.Name}: 行键 {k} 未导出");
                }
                var allKeys = d.NamedFields().Select(f => f.Key).Concat(table.Columns.SelectMany(c => c.SeedKeys)).ToList();
                Assert.AreEqual(allKeys.Count, allKeys.Distinct().Count(), $"{d.Name}: 键跨区重复");

                IPropertyZoneFrame pz = frame;
                Assert.IsTrue(pz.GetCell(0, 0).HasValue, $"{d.Name}: 属性区首格可命中");
            }
            CollectionAssert.AreEquivalent(new[] { "iso-lens", "gb-lens", "GB-my-1" }, names);
        }
    }
}
