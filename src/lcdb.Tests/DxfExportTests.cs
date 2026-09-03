using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LitMath;
using lcdb;
using lcdb.Annotation;
using lcdb.Colors;
using lcdb.Optic;

namespace lcdb.Tests
{
    /// <summary>
    /// DXF 导出契约 — 真实往返 (Database.SaveAs(*.dxf) → netDxf.DxfDocument.Load).
    ///
    /// 背景: 旧 SaveAsDxf 是一条 type switch 只认 9 种基础几何, Arc 分支是空 TODO,
    /// 光学透镜 / 光学标记 / 图框 / 各类标注 全部静默丢弃 — 导出给厂方的是残图.
    /// 现走 <see cref="lcdb.Rendering.DxfGraphicsDraw"/> (IGraphicsDraw 后端), 实体经自己的
    /// Draw 分解成基础图元. 本组测试钉死"画布上有的, DXF 里也得有".
    /// </summary>
    [TestClass]
    public class DxfExportTests
    {
        private string _dir;

        [TestInitialize]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "otocad-dxf-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { }
        }

        /// <summary>建 db → 加实体 → 导出 → 读回.</summary>
        private netDxf.DxfDocument Roundtrip(params Entity[] entities)
            => Roundtrip(db => { foreach (var e in entities) db.AddEntity(e); });

        private netDxf.DxfDocument Roundtrip(Action<Database> populate)
        {
            var db = new Database();
            populate(db);

            string path = Path.Combine(_dir, "out.dxf");
            db.SaveAs(path);

            Assert.IsTrue(File.Exists(path), "DXF 文件未生成");
            var doc = netDxf.DxfDocument.Load(path);
            Assert.IsNotNull(doc, "导出的 DXF 无法被 netDxf 读回 — 文件结构损坏");
            return doc;
        }

        // ---------------------------------------------------------------
        // 1. 基础几何 — Arc 曾是空 TODO 分支
        // ---------------------------------------------------------------

        [TestMethod]
        public void Arc_survives_roundtrip_with_geometry_intact()
        {
            // 透镜曲面全是 Arc; 旧代码这一支只有注释, 一个都出不去.
            var arc = new Arc { center = new Vector2(5, -3), radius = 12.5, startAngle = 0, endAngle = Math.PI / 2 };

            var doc = Roundtrip(arc);

            var arcs = doc.Entities.Arcs.ToList();
            Assert.AreEqual(1, arcs.Count, "Arc 未导出");
            Assert.AreEqual(5, arcs[0].Center.X, 1e-6);
            Assert.AreEqual(-3, arcs[0].Center.Y, 1e-6);
            Assert.AreEqual(12.5, arcs[0].Radius, 1e-6);
            // lcdb 弧度 → DXF 度数, 同为逆时针
            Assert.AreEqual(0, arcs[0].StartAngle, 1e-6);
            Assert.AreEqual(90, arcs[0].EndAngle, 1e-6);
        }

        [TestMethod]
        public void Line_keeps_endpoints()
        {
            var doc = Roundtrip(new Line(new Vector2(0, 0), new Vector2(30, 40)));

            var lines = doc.Entities.Lines.ToList();
            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual(30, lines[0].EndPoint.X, 1e-6);
            Assert.AreEqual(40, lines[0].EndPoint.Y, 1e-6);
        }

        /// <summary>
        /// 旧代码把半径直接当 netDxf 的 MajorAxis/MinorAxis 传 — 那两个是"全轴长"(内部处处 *0.5),
        /// 于是导出的椭圆只有一半大; 且 netDxf 要求 major >= minor, 竖椭圆直接抛异常炸掉整份导出.
        /// </summary>
        [TestMethod]
        public void Tall_ellipse_exports_full_axis_lengths_without_throwing()
        {
            // radiusY > radiusX 的竖椭圆 — 旧路径在此抛 ArgumentOutOfRangeException
            var doc = Roundtrip(new Ellipse { center = Vector2.Zero, radiusX = 4, radiusY = 10 });

            var ellipses = doc.Entities.Ellipses.ToList();
            Assert.AreEqual(1, ellipses.Count, "Ellipse 未导出");
            Assert.AreEqual(20, ellipses[0].MajorAxis, 1e-6, "长轴应为半径×2 (netDxf 用全轴长)");
            Assert.AreEqual(8, ellipses[0].MinorAxis, 1e-6, "短轴应为半径×2");
            Assert.AreEqual(90, ellipses[0].Rotation, 1e-6, "竖椭圆应转 90° 而非交换语义");
        }

        [TestMethod]
        public void Wide_ellipse_keeps_axes_unrotated()
        {
            var doc = Roundtrip(new Ellipse { center = Vector2.Zero, radiusX = 10, radiusY = 4 });

            var e = doc.Entities.Ellipses.Single();
            Assert.AreEqual(20, e.MajorAxis, 1e-6);
            Assert.AreEqual(8, e.MinorAxis, 1e-6);
            Assert.AreEqual(0, e.Rotation, 1e-6);
        }

        // ---------------------------------------------------------------
        // 2. 复合实体 — 旧 type switch 全部静默丢弃
        // ---------------------------------------------------------------

        [TestMethod]
        public void OpticalLens_exports_real_geometry()
        {
            // 双凸透镜: 两个曲面 (Arc) + 边缘 (Line). 旧路径导出 0 个实体.
            var lens = new OpticalLens { Position = Vector2.Zero, Diameter = 25.4, Thickness = 6, R1 = 50, R2 = -50 };

            var doc = Roundtrip(lens);

            Assert.IsTrue(doc.Entities.Arcs.Any(), "透镜曲面 (Arc) 未出现在 DXF 中");
            Assert.IsTrue(doc.Entities.All.Count() >= 3,
                $"透镜应至少导出 2 个曲面 + 边缘, 实得 {doc.Entities.All.Count()} 个");

            // 几何落在合理范围内 (口径 25.4 → 半高 12.7), 证明不是空壳实体
            double maxRadius = doc.Entities.Arcs.Max(a => a.Radius);
            Assert.AreEqual(50, maxRadius, 1e-3, "曲面半径应等于 R1/R2");
        }

        [TestMethod]
        public void CoatingMark_exports_real_geometry()
        {
            // AR 镀膜 = 圆 + 十字. 旧路径导出 0 个实体.
            var mark = new CoatingMark(new Vector2(10, 10), CoatingType.AR, 8.0);

            var doc = Roundtrip(mark);

            int n = doc.Entities.All.Count();
            Assert.IsTrue(n > 0, "镀膜标记完全丢失");
            Assert.IsTrue(doc.Entities.Circles.Any() || doc.Entities.Arcs.Any(),
                "AR 镀膜符号的圆未导出");
        }

        [TestMethod]
        public void LinearDimension_exports_lines_and_text()
        {
            // 标注走 Draw 分解 (而非原生 DXF Dimension): 下游 CAD 不会用自己的 dimstyle 重绘,
            // GB 格式/文字取向所见即所得.
            var dim = new LinearDimension(new Vector2(0, 0), new Vector2(40, 0), 10.0, 0.0, new DimensionStyle());

            var doc = Roundtrip(dim);

            Assert.IsTrue(doc.Entities.Lines.Any(), "标注的尺寸线/尺寸界线未导出");
            Assert.IsTrue(doc.Entities.Texts.Any(), "标注文字未导出");
        }

        // ---------------------------------------------------------------
        // 3. 图层 / 颜色 / 线型语义
        // ---------------------------------------------------------------

        [TestMethod]
        public void Entity_layer_and_bylayer_color_are_preserved()
        {
            var doc = Roundtrip(db =>
            {
                db.layerTable.Add(new Layer("光轴") { color = Color.FromRGB(255, 0, 0) });
                var line = new Line(new Vector2(0, 0), new Vector2(10, 0)) { color = Color.ByLayer };
                db.AddEntity(line);
                line.layer = "光轴";
            });

            var l = doc.Entities.Lines.Single();
            Assert.AreEqual("光轴", l.Layer.Name, "实体图层归属丢失");
            Assert.IsTrue(l.Color.IsByLayer,
                "ByLayer 颜色被烧死成 RGB — 下游改层色将失效");
            Assert.AreEqual(255, l.Layer.Color.R, "层色未随图层表导出");
        }

        [TestMethod]
        public void Explicit_rgb_color_is_written_as_truecolor()
        {
            var line = new Line(new Vector2(0, 0), new Vector2(10, 0)) { color = Color.FromRGB(0, 128, 64) };

            var l = Roundtrip(line).Entities.Lines.Single();

            Assert.IsFalse(l.Color.IsByLayer);
            Assert.AreEqual(0, l.Color.R);
            Assert.AreEqual(128, l.Color.G);
            Assert.AreEqual(64, l.Color.B);
        }

        /// <summary>
        /// GB/T 13323-2009 §2.1: 光轴用双点画线, 中心线用单点画线 — 两者在图上必须可区分.
        /// netDxf 无预定义双点画线, 旧映射把 DashDotDot 退化成 DashDot, 光轴与中心线就分不清了.
        /// </summary>
        [TestMethod]
        public void Axis_dashdotdot_stays_distinct_from_centerline_dashdot()
        {
            var doc = Roundtrip(db =>
            {
                db.AddEntity(new Line(new Vector2(0, 0), new Vector2(10, 0)) { lineType = LineType.DashDotDot });
                db.AddEntity(new Line(new Vector2(0, 5), new Vector2(10, 5)) { lineType = LineType.DashDot });
            });

            var names = doc.Entities.Lines.Select(l => l.Linetype.Name).ToList();
            Assert.AreEqual(2, names.Distinct().Count(),
                $"双点画线与单点画线映射到了同一线型: [{string.Join(", ", names)}]");
        }

        [TestMethod]
        public void Polyline_bulge_stays_a_single_polyline()
        {
            // bulge = 圆弧段. 走 Draw 会被拆成散的线/弧, 原生映射保住一体性 (下游可编辑).
            var poly = new Polyline();
            poly.AddVertexAt(new Vector2(0, 0), 0.5);
            poly.AddVertexAt(new Vector2(10, 0));
            poly.AddVertexAt(new Vector2(10, 10));

            var doc = Roundtrip(poly);

            var polys = doc.Entities.Polylines2D.ToList();
            Assert.AreEqual(1, polys.Count, "Polyline 应导出为一条 Polyline2D 而非散件");
            Assert.AreEqual(3, polys[0].Vertexes.Count);
            Assert.AreEqual(0.5, polys[0].Vertexes[0].Bulge, 1e-9, "圆弧段 bulge 丢失");
        }

        [TestMethod]
        public void Drawing_frame_exports_border_and_title_block_text()
        {
            // 图框 = 交给厂方的图纸骨架 (边框 + 标题栏 + 属性区文字). 旧路径整个丢.
            var frame = new lcdb.DrawingFrame.GbLensDrawingFrame();

            var doc = Roundtrip(frame);

            Assert.IsTrue(doc.Entities.Lines.Any(), "图框边框/分格线未导出");
            Assert.IsTrue(doc.Entities.Texts.Any(), "标题栏/属性区文字未导出");
        }

        // ---------------------------------------------------------------
        // 4. 整图场景 — 覆盖率回归闸
        // ---------------------------------------------------------------

        [TestMethod]
        public void Full_lens_drawing_exports_every_layer_of_content()
        {
            // 一张真实光学零件图的最小组合: 透镜 + 镀膜标记 + 尺寸标注.
            // 旧路径的结果是 0 个实体 (三者都不在 type switch 里).
            var lens = new OpticalLens { Position = Vector2.Zero, Diameter = 25.4, Thickness = 6, R1 = 50, R2 = -50 };
            var mark = new CoatingMark(new Vector2(0, 20), CoatingType.AR, 8.0);
            var dim = new LinearDimension(new Vector2(-12.7, 0), new Vector2(12.7, 0), 20.0, 0.0, new DimensionStyle());

            var doc = Roundtrip(lens, mark, dim);

            int total = doc.Entities.All.Count();
            Assert.IsTrue(total >= 10,
                $"整图导出实体数过少 ({total}) — 疑似仍有实体类型被静默丢弃");
            Assert.IsTrue(doc.Entities.Arcs.Any(), "缺透镜曲面");
            Assert.IsTrue(doc.Entities.Texts.Any(), "缺标注文字");
        }
    }
}
