using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LitMath;
using lcdb;
using lcdb.Annotation;
using lcdb.Rendering;

namespace lcdb.Tests
{
    /// <summary>
    /// 有效孔径合规自检 — GB/T 13323-2009 4.3.5,记法 `Φe{D}`(圆)/ `长×宽`(方/椭圆)。
    ///
    /// 模型(skill optical-mark-compliance):有效孔径是"对零件的要求",归**面属性区 Φe 行**
    /// (参照表面质量/面型精度),不作独立 ⌀ 引线标记 → EffectiveApertureMark 为数据载体。
    ///   1. 记法串:直接断言 MarkText(确定性串)。
    ///   2. 预览资产:属性区整表 + 绿框高亮 Φe 行(非独立符号)。
    /// </summary>
    [TestClass]
    public class EffectiveApertureMarkComplianceTests
    {
        // ========== 1. 记法串 ==========

        [TestMethod]
        public void Circular_is_phi_e_diameter()               // Φe{D:F1}
        {
            Assert.AreEqual("Φe18.0", new EffectiveApertureMark(Vector2.Zero, 18.0).MarkText);
        }

        [TestMethod]
        public void Circular_with_symmetric_tolerance()        // Φe{D}±{t}
        {
            var m = new EffectiveApertureMark(Vector2.Zero, 18.0)
            {
                ToleranceUpper = 0.05,
                ToleranceLower = -0.05,
            };
            m.UpdateMarkText();
            Assert.AreEqual("Φe18.0±0.05", m.MarkText);
        }

        [TestMethod]
        public void Circular_with_asymmetric_tolerance()       // Φe{D}+{u}/-{l}
        {
            var m = new EffectiveApertureMark(Vector2.Zero, 62.0)
            {
                ToleranceUpper = 0.0,
                ToleranceLower = -0.5,
            };
            m.UpdateMarkText();
            Assert.AreEqual("Φe62.0+0.00/-0.50", m.MarkText);
        }

        [TestMethod]
        public void Rectangular_is_length_times_width()        // 长×宽
        {
            var m = new EffectiveApertureMark(Vector2.Zero, 20.0)
            {
                ShapeType = ApertureShapeType.Rectangular,
                SecondaryDimension = 10.0,
            };
            m.UpdateMarkText();
            Assert.AreEqual("20.0×10.0", m.MarkText);
        }

        [TestMethod]
        public void Uses_decimal_point_regardless_of_locale()  // 逗号小数区域下仍输出小数点
        {
            var prev = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture =
                    System.Globalization.CultureInfo.GetCultureInfo("de-DE");
                Assert.AreEqual("Φe18.0", new EffectiveApertureMark(Vector2.Zero, 18.0).MarkText);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = prev;
            }
        }

        // ========== 2. 模型形态: 数据载体, 不自绘 ==========

        [TestMethod]
        public void Is_data_carrier_not_independent_symbol()
        {
            var m = new EffectiveApertureMark(Vector2.Zero, 18.0);
            var svg = new SvgGraphicsDraw();
            m.Draw(svg);
            var xml = svg.BuildSvg(pixelSize: 100);
            Assert.IsFalse(xml.Contains("<line") || xml.Contains("<circle"),
                "数据载体不应自绘任何几何(独立 ⌀ 引线符号已撤)");
        }

        // ========== 3. 预览资产: 属性区整表 + 绿框高亮 Φe 行 ==========

        [TestMethod]
        public void Generate_effective_aperture_table_svg_into_docusaurus()
        {
            // 有效孔径进面属性区表格的 Φe 行(行标签已是 "Φe", 表格按数值渲 "Φ18.0")。
            var dir = OutDir();
            Directory.CreateDirectory(dir);

            var xml = BuildAttributeTableWithHighlight(18.0, 18.0);
            Assert.IsTrue(xml.StartsWith("<svg") && xml.Contains("Φe") && xml.Contains("Φ18.0")
                && xml.Contains("#008000"), "整表应含 Φe 行(值 Φ18.0)+ 绿色高亮框");
            File.WriteAllText(Path.Combine(dir, "effective-aperture-table.svg"), xml);
            Assert.IsTrue(File.Exists(Path.Combine(dir, "effective-aperture-table.svg")));
        }

        // RowLabels 顺序: R, Φe, 倒角, 表面, 3/, 4/, Surface quality, 6/ → Φe 在第 1 行(0 基).
        private const int ApertureRowIndex = 1;

        private static string BuildAttributeTableWithHighlight(double leftDia, double rightDia)
        {
            var table = new TechnicalRequirementTable(Vector2.Zero)
            {
                ShowTitle = false,
                ColumnWidth = 52,
            };
            table.LeftSurface.EffectiveAperture = leftDia;    // Φe 行 (行索引 1, 数值经 FormatAperture 渲 "Φ{D:F1}")
            table.RightSurface.EffectiveAperture = rightDia;

            var svg = new SvgGraphicsDraw();
            table.Draw(svg);
            DrawRowHighlight(svg, table, ApertureRowIndex, System.Drawing.Color.FromArgb(0, 128, 0));
            return svg.BuildSvg(pixelSize: 360, strokeWidth: 0.4);
        }

        private static void DrawRowHighlight(SvgGraphicsDraw svg, TechnicalRequirementTable t, int rowIndex, System.Drawing.Color color)
        {
            double titleH = t.ShowTitle ? t.TitleRowHeight : 0;
            double rowTop = t.Position.Y - (titleH + (rowIndex + 1) * t.RowHeight) * t.Scale;
            double rowH = t.RowHeight * t.Scale;
            double width = t.ColumnWidth * 3 * t.Scale;
            const double inset = 0.6;
            var prev = svg.CurrentColor;
            svg.CurrentColor = color;
            svg.DrawRectangle(new Vector2(t.Position.X + inset, rowTop - rowH + inset),
                width - 2 * inset, rowH - 2 * inset);
            svg.CurrentColor = prev;
        }

        private static string OutDir([CallerFilePath] string thisFile = "")
        {
            var repo = Directory.GetParent(thisFile)!.Parent!.Parent!.FullName;
            return Path.Combine(repo, "website-docusaurus", "static", "img", "marks");
        }
    }
}
