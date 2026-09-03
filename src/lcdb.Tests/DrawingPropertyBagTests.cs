using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using lcdb;
using lcdb.Drawing;
using lcdb.DrawingFrame;
using lcdb.IO;
using lcdb.Optic;

namespace lcdb.Tests
{
    /// <summary>
    /// 文档级属性包 P1: 属性模型 + 数据流。证明 GbLensSheet 先建包 → 由包填图框 →
    /// 图框内容来自包 (属性驱动渲染), 图框暴露扁平 key→value。
    /// </summary>
    [TestClass]
    public class DrawingPropertyBagTests
    {
        [TestMethod]
        public void Bag_clusters_and_flat_access()
        {
            var bag = new DrawingPropertyBag();
            var mat = bag.GetOrAddCluster("material", "材料");
            mat.Add("n_d", "折射率", "n_d 1.5168");
            mat.Add("v_d", "阿贝数", "v_d 64.17");
            bag.GetOrAddCluster("title", "标题栏").Add("DrawingNumber", "图号", "PCX-001");

            // 同簇复用 (不重复建)
            Assert.AreSame(mat, bag.GetOrAddCluster("material", "材料"));
            // 按键取/写值
            Assert.AreEqual("PCX-001", bag.GetValue("DrawingNumber"));
            Assert.IsTrue(bag.SetValue("DrawingNumber", "PCX-002"));
            Assert.AreEqual("PCX-002", bag.GetValue("DrawingNumber"));
            Assert.IsFalse(bag.SetValue("no_such_key", "x"));
            // 扁平 簇.键 → 值
            var flat = bag.Flatten();
            Assert.AreEqual("n_d 1.5168", flat["material.n_d"]);
            Assert.AreEqual("PCX-002", flat["title.DrawingNumber"]);
        }

        [TestMethod]
        public void GbLensSheet_builds_bag_from_lens()
        {
            var lens = new OpticalLens { MaterialName = "N-BK7", RefractiveIndex = 1.5168, AbbeNumber = 64.17 };
            var bag = GbLensSheet.BuildBag(lens);

            // 四簇: 标题栏 + 材料 + 前/后表面
            CollectionAssert.AreEquivalent(
                new[] { "title", "material", "surface-front", "surface-back" },
                bag.Clusters.Select(c => c.Key).ToArray());

            Assert.AreEqual("n_d 1.5168", bag.Cluster("material")!.Properties.Single(p => p.Key == "n_d").Value);
            Assert.AreEqual("N-BK7", bag.GetValue("Material"));
            // GB 默认代号在前表面区
            var front = bag.Cluster("surface-front")!;
            Assert.IsTrue(front.Properties.Single(p => p.Key == "form_error").Value.StartsWith("3/"));
            Assert.IsTrue(front.Properties.Single(p => p.Key == "surface_imperf").Value.StartsWith("5/"));
        }

        [TestMethod]
        public void Bag_drives_frame_rendering_content()
        {
            var lens = new OpticalLens { MaterialName = "H-K9L", RefractiveIndex = 1.5163, AbbeNumber = 64.06 };
            var bag = GbLensSheet.BuildBag(lens);
            // 用户填图号 (P3 场景的键)
            bag.SetValue("DrawingNumber", "ASP-123");

            var frame = DrawingFrameTemplates.CreateOptical(
                DrawingFrameTemplates.PaperSize.A4, DrawingFrameTemplates.Orientation.Portrait);
            frame.ApplyBag(bag);

            IPropertyZoneFrame zone = frame;
            // 属性区三列内容来自包 (属性驱动)
            var matRows = zone.GetZoneRows(1);
            Assert.IsTrue(matRows.Any(r => r == "n_d 1.5163"), "材料列应来自包");
            var frontRows = zone.GetZoneRows(0);
            Assert.IsTrue(frontRows.Any(r => r.StartsWith("3/")) && frontRows.Any(r => r.StartsWith("5/")));

            // 图框暴露扁平 key→value: 用户填的图号在场
            Assert.AreEqual("ASP-123", frame.ExportFlat()["DrawingNumber"]);
        }

        [TestMethod]
        public void BuildFrame_one_shot_is_property_driven()
        {
            var lens = new OpticalLens { MaterialName = "N-SF6", RefractiveIndex = 1.8052, AbbeNumber = 25.43 };
            var frame = GbLensSheet.BuildFrame(lens);
            IPropertyZoneFrame zone = frame;
            Assert.IsTrue(zone.GetZoneRows(1).Any(r => r == "v_d 25.43"), "一键建框内容应来自包");
            Assert.AreEqual("N-SF6", frame.ExportFlat()["Material"]);
        }

        /// <summary>P3: 改格回写文档级包 + 一键重出(re-ApplyBag)保留用户改动。</summary>
        [TestMethod]
        public void GbFrame_edit_writes_back_to_bag_and_survives_reapply()
        {
            var bag = new DrawingPropertyBag();
            bag.GetOrAddCluster("title", "标题栏").Add("DrawingNumber", "图号", "OLD-1");
            bag.GetOrAddCluster("surface-front", "左表面").Add("radius_left", "R", "R60.44CC");

            var frame = new GbLensDrawingFrame();
            frame.ApplyBag(bag);
            IPropertyZoneFrame pz = frame;

            // 改表格行 (左表面列=0, radius_left 行=0) → 回写包
            pz.SetCellText(new PropertyCellHit("左表面", "", 0, 0, default), "R99.9CX");
            Assert.AreEqual("R99.9CX", bag.GetValue("radius_left"), "表格改格应回写文档包");

            // 改标题栏命名字段 (DrawingNumberGb 字段 → DrawingNumber 包键) → 回写包
            pz.SetCellText(new PropertyCellHit("图样代号", "DrawingNumberGb", default), "NEW-9");
            Assert.AreEqual("NEW-9", bag.GetValue("DrawingNumber"), "标题栏改格应回写文档包");

            // 一键重出: 从(已改的)包重建图框 → 用户改动存活
            frame.ApplyBag(bag);
            var flat = frame.ExportFlat();
            Assert.AreEqual("R99.9CX", flat["radius_left"], "重出后表格改动应保留");
            Assert.AreEqual("NEW-9", frame.DrawingNumberGb, "重出后图号应保留");
        }

        /// <summary>P5: 属性区加/删行 = 增删一个带机器键的自定义小属性, 同步到文档级包 (单一真值源)。</summary>
        [TestMethod]
        public void GbFrame_add_remove_row_syncs_custom_property_in_bag()
        {
            var bag = new DrawingPropertyBag();
            bag.GetOrAddCluster("surface-front", "左表面").Add("radius_left", "R", "R60.44CC");
            var frame = new GbLensDrawingFrame();
            frame.ApplyBag(bag);
            IPropertyZoneFrame pz = frame;

            int row = pz.AddRow(0, "自定义: 镀膜 X");
            Assert.IsTrue(row >= 0, "加行应成功");
            var front = bag.Cluster("surface-front")!;
            Assert.AreEqual(2, front.Properties.Count, "包簇应多一个属性");
            var added = front.Properties[row];
            Assert.AreEqual("自定义: 镀膜 X", added.Value);
            StringAssert.StartsWith(added.Key, "custom_", "新行应配机器键 custom_N");
            Assert.AreEqual("自定义: 镀膜 X", frame.ExportFlat()[added.Key], "ExportFlat 应含自定义键");

            frame.ApplyBag(bag);   // 一键重出
            Assert.AreEqual("自定义: 镀膜 X", frame.ExportFlat()[added.Key], "重出后自定义行应保留");

            Assert.IsTrue(pz.RemoveRow(0, row), "删行应成功");
            Assert.AreEqual(1, bag.Cluster("surface-front")!.Properties.Count, "删除后包簇应少一个");
            Assert.IsFalse(frame.ExportFlat().ContainsKey(added.Key), "删除后扁平视图不应再含该键");
        }

        /// <summary>P2: 文档级属性包随 .otocad 序列化往返 (db.DrawingProperties → 文件 → 回灌)。</summary>
        [TestMethod]
        public void DrawingProperties_survive_otocad_v4_round_trip()
        {
            var path = Path.Combine(Path.GetTempPath(), $"props-{Guid.NewGuid():N}.otocad");
            var db = new Database();
            db.DrawingProperties.GetOrAddCluster("title", "标题栏").Add("DrawingNumber", "图号", "PCX-007");
            db.DrawingProperties.GetOrAddCluster("surface-front", "左表面").Add("radius_left", "R", "R60.44CC");
            try
            {
                OtocadFileFormatV4.Save(db, path);
                var (db2, _) = OtocadFileFormatV4.Load(path);

                Assert.AreEqual("PCX-007", db2.DrawingProperties.GetValue("DrawingNumber"), "图号应往返保留");
                Assert.AreEqual("R60.44CC", db2.DrawingProperties.GetValue("radius_left"), "前表面半径应往返保留");
                Assert.AreEqual(2, db2.DrawingProperties.Clusters.Count, "两个簇应都在");
            }
            finally { try { if (File.Exists(path)) File.Delete(path); } catch { } }
        }
    }
}
