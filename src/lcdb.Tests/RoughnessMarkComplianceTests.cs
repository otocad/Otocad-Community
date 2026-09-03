using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LitMath;
using lcdb;
using lcdb.Annotation;
using lcdb.Rendering;

namespace lcdb.Tests
{
    /// <summary>
    /// 表面粗糙度标记合规自检 (GB/T 131-2006).
    ///
    /// 双面验证 (各用对的工具):
    ///   1. **几何**: V 形 (短左/长右 2:1, 顶点朝下) + 材料去除修饰
    ///      (Remove=∇ 加水平延长线 / NotRemove=⌀V 加内接圆 / Any=仅 V).
    ///   2. **文字格式**: 参数值 "Ra {值}" (μm, 2 位有效数字), 0 → "Ra —"; 加工方法字.
    /// 见 memory: ocr-vs-geometry-verification; 双边对照页 dev/roughness-compliance.
    /// </summary>
    [TestClass]
    public class RoughnessMarkComplianceTests
    {
        private const double S = 10.0;

        private static List<Entity> Generate(SurfaceRoughnessMark m)
        {
            var gen = typeof(SurfaceRoughnessMark).GetMethod("Generate", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(gen, "未找到 SurfaceRoughnessMark.Generate()");
            gen!.Invoke(m, null);
            var fld = typeof(SurfaceRoughnessMark).GetField("_markEntities", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fld, "未找到 SurfaceRoughnessMark._markEntities");
            return (List<Entity>)fld!.GetValue(m)!;
        }

        private static SurfaceRoughnessMark Mark(MaterialRemoval removal, bool showText = false) =>
            new SurfaceRoughnessMark(Vector2.Zero, S) { Removal = removal, ShowText = showText };

        // ========== 1. 几何: V 形 + 材料去除修饰 ==========

        [DataTestMethod]
        // 完整符号 = V 两边 + 标注横线(固定, 3 种修饰符都有), 再叠加修饰符:
        [DataRow(MaterialRemoval.Remove, 4, 0)]     // 去除材料: V(2) + 标注横线(1) + 封口横杠(1)
        [DataRow(MaterialRemoval.NotRemove, 3, 1)]  // 不去除: V(2) + 标注横线(1) + 内接圆
        [DataRow(MaterialRemoval.Any, 3, 0)]        // 任意: V(2) + 标注横线(1)
        public void Removal_modifier_geometry(MaterialRemoval removal, int lines, int circles)
        {
            var e = Generate(Mark(removal));
            Assert.AreEqual(lines, e.OfType<Line>().Count(), $"{removal}: 线段数不符");
            Assert.AreEqual(circles, e.OfType<Circle>().Count(), $"{removal}: 圆数不符");
            if (circles == 1)
                Assert.AreEqual(S * 0.25, e.OfType<Circle>().First().radius, 1e-6, "内接圆半径应为 Size*0.25");
        }

        [TestMethod]
        public void V_apex_points_down_and_right_edge_is_longer()  // GB/T 131 4.1: 顶点朝下, 长边在右 (2:1)
        {
            var e = Generate(Mark(MaterialRemoval.Any));   // 基本符号: V 两边 + 标注横线
            var lines = e.OfType<Line>().ToList();
            Assert.AreEqual(3, lines.Count, "任意(APA)完整符号应为 V 两边 + 标注横线 = 3 条线");

            // 顶点 = 全局最低点, 且为两条 V 边的公共端点 (标注横线不经过顶点)
            var pts = lines.SelectMany(l => new[] { l.startPoint, l.endPoint }).ToList();
            var apex = pts.OrderBy(p => p.Y).First();
            var legs = lines.Where(l => Near(l.startPoint, apex) || Near(l.endPoint, apex)).ToList();
            Assert.AreEqual(2, legs.Count, "两条 V 边应交于同一最低顶点");
            Assert.AreEqual(0.0, apex.X, 1e-6, "顶点应在中心正下方");

            double len(Line l) => (l.endPoint - l.startPoint).length;
            double longEdge = legs.Max(len), shortEdge = legs.Min(len);
            Assert.IsTrue(longEdge > shortEdge * 1.2, "长边(右)应明显长于短边(左)");
        }

        [TestMethod]
        public void Remove_closing_bar_is_flush_with_short_leg_top()  // GB/T 131 4.1.2: 封口横杠与左(短)边顶平齐, 低于标注横线
        {
            var lines = Generate(Mark(MaterialRemoval.Remove)).OfType<Line>().ToList();
            bool Horiz(Line l) => Math.Abs(l.startPoint.Y - l.endPoint.Y) < 1e-6;
            var horiz = lines.Where(Horiz).OrderBy(l => l.startPoint.Y).ToList();
            Assert.AreEqual(2, horiz.Count, "去除材料应有 2 条水平线 (封口横杠 + 标注横线)");

            double closingY = horiz[0].startPoint.Y;     // 低 = 封口横杠
            double annotationY = horiz[1].startPoint.Y;  // 高 = 标注横线
            Assert.IsTrue(closingY < annotationY - 1e-6, "封口横杠应低于标注横线(不可共线)");
            Assert.AreEqual(S / 3.0, closingY, 1e-6, "封口横杠应与短(左)边顶平齐 (Size/3)");
            Assert.AreEqual(S * 2.0 / 3.0, annotationY, 1e-6, "标注横线应在长(右)边顶 (Size*2/3)");
        }

        // ========== 2. 文字格式: Ra 值 + 加工方法 ==========

        [TestMethod]
        public void Ra_value_format()                  // "Ra {2 位有效数字}"
        {
            Assert.IsTrue(TextValues(Mark(MaterialRemoval.Remove, showText: true)).Any(v => v == "Ra 0.8"),
                "默认 Ra=0.8 应显示 'Ra 0.8'");
        }

        [TestMethod]
        public void Ra_zero_shows_dash()               // 未指定 → "Ra —"
        {
            var m = Mark(MaterialRemoval.Remove, showText: true);
            m.Ra = 0;
            Assert.IsTrue(TextValues(m).Any(v => v == "Ra —"), "Ra=0 应显示 'Ra —'");
        }

        [TestMethod]
        public void Machining_method_label_only_when_non_default()
        {
            // 默认磨削 (Grinding): 不重复打加工方法
            var grind = Mark(MaterialRemoval.Remove, showText: true);
            Assert.IsFalse(TextValues(grind).Any(v => v == "磨"), "默认磨削不应出现方法字");

            // 车削: 应出现 "车"
            var turn = Mark(MaterialRemoval.Remove, showText: true);
            turn.Method = MachiningMethod.Turning;
            Assert.IsTrue(TextValues(turn).Any(v => v == "车"), "车削应标 '车'");
        }

        // ========== 3. 文档 SVG ==========

        [TestMethod]
        public void Generate_roughness_svgs_into_docusaurus()
        {
            var dir = OutDir();
            Directory.CreateDirectory(dir);

            var samples = new (string name, SurfaceRoughnessMark mark)[]
            {
                ("roughness-remove", Mark(MaterialRemoval.Remove, showText: true)),
                ("roughness-notremove", Mark(MaterialRemoval.NotRemove, showText: true)),
            };
            foreach (var (name, mark) in samples)
            {
                var svg = new SvgGraphicsDraw();
                mark.Draw(svg);
                var xml = svg.BuildSvg(pixelSize: 200);
                Assert.IsTrue(xml.StartsWith("<svg") && xml.Contains("<line"), $"{name}: SVG 异常");
                File.WriteAllText(Path.Combine(dir, $"{name}.svg"), xml);
            }
            Assert.IsTrue(File.Exists(Path.Combine(dir, "roughness-remove.svg")));
        }

        // ---- helpers ----
        private static bool Near(Vector2 a, Vector2 b) => (a - b).length < 1e-6;
        private static List<string> TextValues(SurfaceRoughnessMark m) =>
            Generate(m).OfType<Text>().Select(t => t.Value).ToList();

        private static string OutDir([CallerFilePath] string thisFile = "")
        {
            var repo = Directory.GetParent(thisFile)!.Parent!.Parent!.FullName;
            return Path.Combine(repo, "website-docusaurus", "static", "img", "marks");
        }
    }
}
