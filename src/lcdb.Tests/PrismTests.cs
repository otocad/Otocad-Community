using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LitMath;
using lcdb;
using lcdb.Colors;
using lcdb.Optic;
using lcdb.Rendering;

namespace lcdb.Tests
{
    /// <summary>
    /// 棱镜实体渲染契约 — 剖面几何 / 玻璃填充 / DXF 出图.
    /// 支撑"直角棱镜标准图纸"出图路径 (GbSheetScene.BuildPrismFromPart)。
    /// </summary>
    [TestClass]
    public class PrismTests
    {
        private static string Svg(Prism p)
        {
            var gd = new SvgGraphicsDraw();
            p.Draw(gd);
            return gd.BuildSvg();
        }

        // ---------------- 剖面几何 ----------------

        [TestMethod]
        public void RightAngle_outline_is_three_edges_at_documented_corners()
        {
            // lcdb.Prism 的几何约定 (出图装配器 AppendScaledPrismView 按此放标注锚点):
            // A=直角顶(左下) B=底边右端 C=竖边上端
            var p = new Prism { Position = Vector2.Zero, Width = 20, Height = 20 };

            var snaps = p.GetSnapPoints();
            Assert.AreEqual(4, snaps.Count, "捕捉点数变了 — 出图标注锚点索引会错位");
            Assert.AreEqual(-10, snaps[1].position.X, 1e-9);  // A 直角顶
            Assert.AreEqual(-10, snaps[1].position.Y, 1e-9);
            Assert.AreEqual(10, snaps[2].position.X, 1e-9);   // B 底边右端
            Assert.AreEqual(-10, snaps[2].position.Y, 1e-9);
            Assert.AreEqual(-10, snaps[3].position.X, 1e-9);  // C 竖边上端
            Assert.AreEqual(10, snaps[3].position.Y, 1e-9);
        }

        // ---------------- 玻璃填充 ----------------

        [TestMethod]
        public void Fill_color_emits_filled_polygon()
        {
            // 光学零件图的玻璃剖面要填充 (与 OpticalLens.FillColor 同约定), 否则棱镜图是空心线框
            var p = new Prism { Width = 20, Height = 20, FillColor = Color.FromRGB(0xCD, 0xE4, 0xF3) };

            var svg = Svg(p);

            Assert.IsTrue(svg.Contains("<polygon"), "玻璃填充未渲染");
            Assert.IsTrue(svg.Contains("#CDE4F3"), $"填充色不对: {svg}");
        }

        [TestMethod]
        public void No_fill_color_stays_outline_only()
        {
            // 默认不填充 — 保证既有用法 (画布上直接画棱镜) 不被改变
            var svg = Svg(new Prism { Width = 20, Height = 20 });

            Assert.IsFalse(svg.Contains("<polygon"), "未设 FillColor 却画了填充");
            Assert.IsTrue(svg.Contains("<line"), "轮廓线丢失");
        }

        [TestMethod]
        public void Rotated_fill_follows_the_outline()
        {
            // 填充多边形与轮廓必须同步旋转, 否则转过的棱镜会出现"底色与轮廓错位"
            var p = new Prism
            {
                Position = Vector2.Zero, Width = 20, Height = 20, Rotation = 90,
                FillColor = Color.FromRGB(0xCD, 0xE4, 0xF3),
            };

            var svg = Svg(p);

            Assert.IsTrue(svg.Contains("<polygon"), "旋转后填充丢失");
            // 转 90°: 直角顶 (-10,-10) → (10,-10). SVG Y 取负 → (10,10)
            Assert.IsTrue(svg.Contains("10,10"), $"填充未跟随旋转: {svg}");
        }

        [TestMethod]
        public void Clone_carries_fill_color()
        {
            var p = new Prism { FillColor = Color.FromRGB(1, 2, 3) };
            var c = (Prism)p.Clone();

            Assert.IsTrue(c.FillColor.HasValue, "Clone 丢了 FillColor — 出图克隆件会变空心");
            Assert.AreEqual(3, c.FillColor!.Value.b);
        }

        // ---------------- DXF 出图 ----------------

        [TestMethod]
        public void Prism_exports_to_dxf_as_real_geometry()
        {
            // 棱镜图要能交给厂方 — 走 DxfGraphicsDraw 的全实体覆盖路径
            var db = new Database();
            db.AddEntity(new Prism
            {
                Width = 25.4, Height = 25.4,
                FillColor = Color.FromRGB(0xCD, 0xE4, 0xF3),
            });

            string dir = Path.Combine(Path.GetTempPath(), "otocad-prism-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string path = Path.Combine(dir, "prism.dxf");
                db.SaveAs(path);
                var doc = netDxf.DxfDocument.Load(path);

                Assert.AreEqual(3, doc.Entities.Lines.Count(), "棱镜三条边未全部导出");
                Assert.AreEqual(1, doc.Entities.Hatches.Count(), "玻璃填充未导出为 SOLID 填充");
            }
            finally
            {
                try { Directory.Delete(dir, recursive: true); } catch { }
            }
        }
    }
}
