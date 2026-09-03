using System;
using System.Collections.Generic;
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
    /// 面形精度合规自检 — ISO 10110-5 / GB/T 11297.5-2011,代号 `3/A(B/C)`。
    ///
    /// 2026-06-08 模型对齐(skill optical-mark-compliance):面形精度是"对零件的要求",
    /// 归**面属性区表格行**(参照表面质量),不再作独立带框标记 → FormAccuracyMark 降为
    /// 数据载体(参数 + UpdateMarkText 生成 MarkText,不自绘)。故本测试:
    ///   1. 文字格式码:直接断言 MarkText(确定性串),不再断言"框 4 条 Dash 线"(旧作弊测试已删)。
    ///   2. 预览资产:照表面质量范式,生成**属性区整表 + 绿框高亮 3/ 行**的 SVG(非单符号方框)。
    ///
    /// ⚠️ 待人工核国标:3/A(B/C) 的 C 字段语义(现"旋转对称偏差",ISO 中通常为旋转对称残差/RMS)。
    /// </summary>
    [TestClass]
    public class FormAccuracyMarkComplianceTests
    {
        // ========== 1. ISO 10110-5 文字格式码 3/A(B/C) ==========

        [TestMethod]
        public void ISO_basic_power_irregularity()             // 3/A(B)
        {
            Assert.AreEqual("3/4(1)", new FormAccuracyMark(Vector2.Zero, 4.0, 1.0).MarkText);
        }

        [TestMethod]
        public void ISO_with_rotational_symmetry_C()           // 3/A(B/C)
        {
            Assert.AreEqual("3/3(1/2)", new FormAccuracyMark(Vector2.Zero, 3.0, 1.0, 2.0).MarkText);
        }

        [TestMethod]
        public void ISO_irregularity_only_uses_dash_for_power() // 3/-(B)
        {
            var m = new FormAccuracyMark();
            m.SetProperties(new Dictionary<string, object>
            {
                { "IrregularityOnly", true },
                { "IrregularityFringes", 1.0 },
            });
            Assert.AreEqual("3/-(1)", m.MarkText);
        }

        [TestMethod]
        public void ISO_evaluation_diameter_suffix()           // 3/A(B) Øxx
        {
            var m = new FormAccuracyMark();
            m.SetProperties(new Dictionary<string, object>
            {
                { "PowerFringes", 4.0 },
                { "IrregularityFringes", 1.0 },
                { "EvaluationDiameter", 30.0 },
            });
            Assert.AreEqual("3/4(1) Ø30.0", m.MarkText);
        }

        [TestMethod]
        public void ISO_code_always_starts_with_3()           // 面形代号恒为 "3/"
        {
            foreach (var (a, b) in new[] { (1.0, 0.5), (4.0, 1.0), (10.0, 2.0) })
            {
                var code = new FormAccuracyMark(Vector2.Zero, a, b).MarkText;
                StringAssert.StartsWith(code, "3/", $"面形码必须以 3/ 开头, 实得 {code}");
            }
        }

        [TestMethod]
        public void Uses_decimal_point_regardless_of_locale()  // 逗号小数区域下仍输出小数点
        {
            var prev = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture =
                    System.Globalization.CultureInfo.GetCultureInfo("de-DE");
                var m = new FormAccuracyMark();
                m.SetProperties(new Dictionary<string, object>
                {
                    { "PowerFringes", 4.0 }, { "IrregularityFringes", 1.0 }, { "EvaluationDiameter", 30.0 },
                });
                Assert.AreEqual("3/4(1) Ø30.0", m.MarkText);   // 小数点而非逗号
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
            // 面形精度归属性区表格行, 不再作独立带框标记 → Draw 空操作(不产任何几何)。
            var m = new FormAccuracyMark(Vector2.Zero, 4.0, 1.0);
            var svg = new SvgGraphicsDraw();
            m.Draw(svg);
            var xml = svg.BuildSvg(pixelSize: 100);
            Assert.IsFalse(xml.Contains("<line"), "数据载体不应自绘任何线(独立框已撤)");
        }

        // ========== 3. 预览资产: 属性区整表 + 绿框高亮 3/ 行 ==========

        [TestMethod]
        public void Generate_form_accuracy_ISO_table_svg_into_docusaurus()
        {
            // 面形精度不画方框: 3/A(B/C) 进面属性区表格的 3/ 行。
            // 行标签已是 "3/", 故单元值只填 A(B/C) 部分(剥掉 MarkText 的 "3/" 前缀), 避免重复。
            var dir = OutDir();
            Directory.CreateDirectory(dir);

            string full = new FormAccuracyMark(Vector2.Zero, 4.0, 1.0).MarkText; // "3/4(1)"
            string cell = full.Substring("3/".Length);                          // "4(1)"
            Assert.AreEqual("4(1)", cell);

            var xml = BuildAttributeTableWithHighlight(cell, cell);
            Assert.IsTrue(xml.StartsWith("<svg") && xml.Contains("4(1)") && xml.Contains("3/")
                && xml.Contains("#008000"), "整表应含 3/ 面形行(值 4(1))+ 绿色高亮框");
            File.WriteAllText(Path.Combine(dir, "form-accuracy-iso-table.svg"), xml);
            Assert.IsTrue(File.Exists(Path.Combine(dir, "form-accuracy-iso-table.svg")));
        }

        // RowLabels 顺序: R, Φe, 倒角, 表面, 3/, 4/, Surface quality, 6/ → 面形(3/)在第 4 行(0 基).
        private const int FormAccuracyRowIndex = 4;

        /// <summary>
        /// 整张面属性区表(去"技术要求"抬头),把 3/ 面形行左/右表面值填入,绿框高亮该行。
        /// 值由调用方传入(来自 FormAccuracyMark.MarkText 剥前缀),保持格式码单一真值。
        /// </summary>
        private static string BuildAttributeTableWithHighlight(string leftVal, string rightVal)
        {
            var table = new TechnicalRequirementTable(Vector2.Zero)
            {
                ShowTitle = false,
                ColumnWidth = 52,
            };
            table.LeftSurface.ISO10110_5_Value = leftVal;     // 3/ 面形行 (TechnicalRequirementTable 行索引 4)
            table.RightSurface.ISO10110_5_Value = rightVal;

            var svg = new SvgGraphicsDraw();
            table.Draw(svg);
            DrawRowHighlight(svg, table, FormAccuracyRowIndex, System.Drawing.Color.FromArgb(0, 128, 0));
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
