using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LitMath;
using lcdb;
using lcdb.Annotation;
using lcdb.DrawingFrame;
using lcdb.Rendering;

namespace lcdb.Tests
{
    /// <summary>
    /// 属性区整表合规自检 — 三区(前表面 | 材料 | 后表面)行序与代号。
    ///
    /// 验收契约(verification-workflow):三区行序/代号正确,逐行引用各标记格式。
    /// 代号权威 = ISO 10110-10 代号表(0/应力 1/气泡 2/不均匀 3/面形 4/对中 5/疵病 6/激光);
    /// 疵病/纹理行按用户先例用描述标签(Surface quality / Surface texture),
    /// 纹理**无 slash 代号**(G/P)、6/ 是激光的代号不得用作纹理行。
    ///
    /// 两条渲染路径都查:
    ///   1. TechnicalRequirementTable(独立可放置整表, 也是各双边核对页预览的载体)。
    ///   2. OpticalDrawingFrame(GB 模板图框属性区, 由 GB 字段播种)的行代号。
    /// </summary>
    [TestClass]
    public class TechnicalRequirementTableComplianceTests
    {
        // ========== 1. 行序与标签(钉死, 防把代号行挪位/改回错代号) ==========

        [TestMethod]
        public void Surface_row_labels_in_standard_order()
        {
            var labels = GetPrivateLabels("RowLabels");
            CollectionAssert.AreEqual(
                new[] { "R", "Φe", "倒角", "表面", "3/", "4/", "Surface quality", "Surface texture" },
                labels,
                "表面列行序/标签不符(注: 疵病/纹理用描述标签; 纹理无 slash 代号, 6/ 是激光)");
        }

        [TestMethod]
        public void Material_row_labels_in_standard_order()
        {
            var labels = GetPrivateLabels("MaterialRowLabels");
            CollectionAssert.AreEqual(
                new[] { "n", "ν", "0/", "1/", "2/" },
                labels,
                "材料列行序/标签不符(ISO 10110-10: 0/应力 1/气泡 2/不均匀)");
        }

        private static string[] GetPrivateLabels(string fieldName)
        {
            var fld = typeof(TechnicalRequirementTable).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(fld, $"未找到 TechnicalRequirementTable.{fieldName}");
            return (string[])fld!.GetValue(null)!;
        }

        // ========== 2. 值 → 行映射(逐行引用各标记格式, 单一真值) ==========

        [TestMethod]
        public void Surface_values_render_into_their_rows()
        {
            var table = new TechnicalRequirementTable(Vector2.Zero) { ShowTitle = false, ColumnWidth = 52 };
            table.LeftSurface.Radius = 100.0;
            table.LeftSurface.EffectiveAperture = 18.0;       // → Φe 行 (Φ18.0)
            table.LeftSurface.ChamferRequirement = "0.3×45°";
            table.LeftSurface.SurfaceRequirement = "抛光";
            table.LeftSurface.ISO10110_5_Value = "4(1)";       // → 3/ 行 (面形)
            table.LeftSurface.ISO10110_6_Value = "3'";         // → 4/ 行 (对中)
            table.LeftSurface.ISO10110_3_Value = "5×0.16";     // → Surface quality 行 (疵病, 历史属性名)
            table.LeftSurface.ISO10110_4_Value = "P3/Rq0.002"; // → Surface texture 行 (纹理, 历史属性名)

            var svg = new SvgGraphicsDraw();
            table.Draw(svg);
            var xml = svg.BuildSvg(pixelSize: 360, strokeWidth: 0.4);

            foreach (var expect in new[] { "Φ18.0", "0.3×45°", "4(1)", "3'", "5×0.16", "P3/Rq0.002" })
                Assert.IsTrue(xml.Contains(expect), $"整表应渲出值 '{expect}'");
            Assert.IsFalse(xml.Contains(">6/<"), "不得出现 6/ 行标签(6/ 是激光的代号, 非纹理)");
        }

        // ========== 3. GB 模板图框属性区(出图真路径)的行代号 ==========

        [TestMethod]
        public void Gb_frame_part_rows_carry_correct_codes()
        {
            var frame = new OpticalDrawingFrame { Origin = Vector2.Zero };
            IPropertyZoneFrame f = frame;
            f.HitTestCell(new Vector2(66.5, 38));   // 触发 Generate → EnsureColumns

            var front = f.GetZoneRows(0);           // 前表面列 = { FormError, SurfaceImperf, CenteringTol, SampleGrade, SurfaceTexture }
            StringAssert.StartsWith(front[0], "3/", $"面形行应 3/(非 4/), 实得 {front[0]}");
            StringAssert.StartsWith(front[1], "5/", $"疵病行应 5/, 实得 {front[1]}");
            Assert.IsFalse(front.Any(r => r.StartsWith("8/")), "纹理行不得带凭空 8/ 前缀");
            Assert.IsTrue(front.Any(r => r.StartsWith("G") || r.StartsWith("P")),
                $"纹理行应为 G/P 代码, 实得 [{string.Join(", ", front)}]");
        }

        // ========== 4. 文档 SVG 资产(整表, 双列值填齐) ==========

        [TestMethod]
        public void Generate_full_table_svg_into_docusaurus()
        {
            var dir = OutDir();
            Directory.CreateDirectory(dir);

            var table = new TechnicalRequirementTable(Vector2.Zero) { ShowTitle = false, ColumnWidth = 52 };
            foreach (var s in new[] { table.LeftSurface, table.RightSurface })
            {
                s.Radius = 100.0;
                s.EffectiveAperture = 18.0;
                s.ISO10110_5_Value = "4(1)";
                s.ISO10110_6_Value = "3'";
                s.ISO10110_3_Value = "5×0.16";
                s.ISO10110_4_Value = "P3";
            }

            var svg = new SvgGraphicsDraw();
            table.Draw(svg);
            var xml = svg.BuildSvg(pixelSize: 360, strokeWidth: 0.4);
            Assert.IsTrue(xml.StartsWith("<svg") && xml.Contains("Surface quality") && xml.Contains("Surface texture"),
                "整表 SVG 应含描述标签行");
            File.WriteAllText(Path.Combine(dir, "technical-requirement-table.svg"), xml);
            Assert.IsTrue(File.Exists(Path.Combine(dir, "technical-requirement-table.svg")));
        }

        private static string OutDir([CallerFilePath] string thisFile = "")
        {
            var repo = Directory.GetParent(thisFile)!.Parent!.Parent!.FullName;
            return Path.Combine(repo, "website-docusaurus", "static", "img", "marks");
        }
    }
}
