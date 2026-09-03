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
    /// 表面质量(疵病)合规自检 — ISO 10110-7:2017 / MIL-PRF-13830.
    ///
    /// T 型(文字格式码), 双标准:
    ///   1. ISO 10110-7 §6: 个体表面/元件 `5/N×A`(N=数目, A=等级);组件用 `15/`;
    ///      可加 `;` 刮痕 / `;W` 宽度 / `;L` 长划痕 / `;C` 镀膜疵病。原文例 `5/1×0,25; W0,04`。
    ///   2. MIL-PRF-13830: scratch-dig `S-D`(如 60-40)。
    /// 框几何: 矩形框 + 框内代码文本。
    /// 注: `SurfaceImperfectionMark` 已合并入本类(代号审计)。
    /// </summary>
    [TestClass]
    public class SurfaceQualityMarkComplianceTests
    {
        private static List<Entity> Generate(SurfaceQualityMark m)
        {
            var gen = typeof(SurfaceQualityMark).GetMethod("Generate", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(gen, "未找到 SurfaceQualityMark.Generate()");
            gen!.Invoke(m, null);
            var fld = typeof(SurfaceQualityMark).GetField("_markEntities", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fld, "未找到 SurfaceQualityMark._markEntities");
            return (List<Entity>)fld!.GetValue(m)!;
        }

        // ========== 1. ISO 10110-7 文字格式码 5/N×A ==========

        [TestMethod]
        public void ISO_basic_N_times_A()                       // §4.2.1.1  5/Ng×Ag
        {
            var m = new SurfaceQualityMark(Vector2.Zero, defectCount: 5, defectSize: 0.16);
            Assert.AreEqual("5/5×0.16", m.MarkText);
        }

        [TestMethod]
        public void ISO_long_scratch_term_carries_letter_L()    // §4.2.1.4  ; L Nl×Al
        {
            // ISO 10110-7:2017 §4.2.1.4: 长划痕项必须写成 "; L Nl×Al"
            // (Nl=条数, Al=最大宽度 mm), 字母代号 L 不可省。
            var m = new SurfaceQualityMark(Vector2.Zero, 5, 0.16, scratchCount: 2, scratchWidth: 0.04);
            Assert.AreEqual("5/5×0.16; L2×0.04", m.MarkText);
        }

        [TestMethod]
        public void ISO_every_supplementary_segment_has_a_letter_designation()
        {
            // 反作弊护栏: ISO §4.2.1.2–4.2.1.5 规定每个分号补充段都必须以字母代号
            // (W 宽度 / C 镀膜 / L 长划痕 / E 崩边) 起头 —— 不存在裸 "; N×A" 段。
            // (旧实现曾产出非标的 "5/5×0.16; 2×0.04", 会被工厂判非标拒收。)
            var m = new SurfaceQualityMark(Vector2.Zero, 5, 0.16, scratchCount: 2, scratchWidth: 0.04);
            var segments = m.MarkText.Split(';');
            for (int i = 1; i < segments.Length; i++)            // segments[0] 是 "5/Ng×Ag" 主段
            {
                var seg = segments[i].TrimStart();
                Assert.IsTrue(seg.Length > 0 && char.IsLetter(seg[0]),
                    $"补充段必须带字母代号 (W/C/L/E), 实得裸段 '{seg}' (完整: {m.MarkText})");
            }
        }

        [TestMethod]
        public void ISO_grade_value_is_not_truncated()          // R5 系列 0.063 / 0.004 不得被截位
        {
            // Ag/Al 取 Renard R5 系列, 最小到 0.01 (等级) / 0.004 (长划痕宽度)。
            // 旧实现用 F2 会把 0.063→"0.06"、0.004→"0.00", 丢失等级。
            var m = new SurfaceQualityMark(Vector2.Zero, 1, 0.063, scratchCount: 1, scratchWidth: 0.004);
            Assert.AreEqual("5/1×0.063; L1×0.004", m.MarkText);
        }

        [TestMethod]
        public void ISO_uses_decimal_point_regardless_of_locale()
        {
            // 不变区域性: 即便系统区域用逗号作小数分隔, 代码串仍用小数点。
            var prev = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture =
                    System.Globalization.CultureInfo.GetCultureInfo("de-DE"); // 逗号小数
                var m = new SurfaceQualityMark(Vector2.Zero, 1, 0.25);
                Assert.AreEqual("5/1×0.25", m.MarkText);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = prev;
            }
        }

        [TestMethod]
        public void ISO_code_always_starts_with_5()             // §4.1 疵病代号恒为 "5/"
        {
            foreach (var (n, a) in new[] { (1, 0.25), (5, 0.16), (10, 0.40) })
            {
                var m = new SurfaceQualityMark(Vector2.Zero, n, a);
                StringAssert.StartsWith(m.MarkText, "5/", $"疵病码必须以 5/ 开头, 实得 {m.MarkText}");
            }
        }

        // ========== 2. MIL-PRF-13830 scratch-dig ==========
        // 来源说明: 仓库无 MIL-PRF-13830B 原文 PDF (primary source 缺失, 见合规文档已知缺口)。
        // 以下断言依据二手权威 (U. Arizona J.H. Burge 课件
        //   docs/需求/ISO/arizona/10-Specifying-optical-components.pdf p25):
        //   "scratch/dig … 60/40, scratch designation 60, dig designation 40", 常见 80/50, 60/40, 20/10。
        // 分隔符: 标准课件用 '/', 但 Edmund/Thorlabs 等厂商目录普遍写连字符 '60-40' —— 本类采连字符 (主流惯例)。

        [TestMethod]
        public void MIL_scratch_dig_format_is_scratch_dash_dig()    // S-D
        {
            foreach (var (s, d) in new[] { ("80", "50"), ("60", "40"), ("20", "10") })
            {
                var m = new SurfaceQualityMark(Vector2.Zero, s, d);
                Assert.AreEqual($"{s}-{d}", m.MarkText,
                    $"MIL scratch-dig 应为 'S-D', 实得 {m.MarkText}");
                Assert.AreEqual(SurfaceQualityStandard.MIL_PRF_13830, m.QualityStandard);
            }
        }

        [TestMethod]
        public void MIL_does_not_emit_ISO_5_slash_prefix()
        {
            // MIL 是独立北美规范, 不应带 ISO 的 "5/" 代号前缀。
            var m = new SurfaceQualityMark(Vector2.Zero, "60", "40");
            StringAssert.StartsWith(m.MarkText, "60", $"MIL 串不应含 ISO 前缀, 实得 {m.MarkText}");
            Assert.IsFalse(m.MarkText.Contains("5/"), $"MIL 串不得含 '5/', 实得 {m.MarkText}");
        }

        // ========== 3. 框几何 (矩形框 + 框内代码) ==========

        [TestMethod]
        public void Rectangle_frame_is_four_lines_with_code()
        {
            var m = new SurfaceQualityMark(Vector2.Zero, 5, 0.16) { ShowText = true };
            var e = Generate(m);
            Assert.AreEqual(4, e.OfType<Line>().Count(), "矩形框应为 4 条边");
            Assert.IsTrue(e.OfType<Text>().Any(t => t.Value == "5/5×0.16"), "框内应含 ISO 代码 5/5×0.16");
        }

        // ========== 4. 文档 SVG 资产 ==========

        [TestMethod]
        public void Generate_surface_quality_ISO_table_svg_into_docusaurus()
        {
            // ISO 疵病也不画方框: 5/N×A 进面属性区表格的 5/ 行。
            // 行标签已是 "5/", 故单元值只填 N×A 部分 (剥掉 MarkText 的 "5/" 前缀), 避免重复。
            var dir = OutDir();
            Directory.CreateDirectory(dir);

            string full = new SurfaceQualityMark(Vector2.Zero, 5, 0.16).MarkText; // "5/5×0.16"
            string cell = full.Substring("5/".Length);                            // "5×0.16"
            Assert.AreEqual("5×0.16", cell);

            var xml = BuildAttributeTableWithHighlight(cell, cell);
            Assert.IsTrue(xml.StartsWith("<svg") && xml.Contains("5×0.16") && xml.Contains("Surface quality")
                && xml.Contains("#008000"), "整表应含 Surface quality 行(值 5×0.16)+ 绿色高亮框");
            File.WriteAllText(Path.Combine(dir, "surface-quality-iso-table.svg"), xml);
            Assert.IsTrue(File.Exists(Path.Combine(dir, "surface-quality-iso-table.svg")));
        }

        [TestMethod]
        public void Generate_surface_quality_MIL_table_svg_into_docusaurus()
        {
            // 「使用美军标时」: 疵病不画独立方框, 而是把 scratch-dig 值写进面属性区表格的
            // 表面缺陷行 (代码 5/ 行)。值取自真实 SurfaceQualityMark(MIL 模式)的 MarkText,
            // 保持「格式码单一真值」: 表格单元 = 标记产出的串。
            var dir = OutDir();
            Directory.CreateDirectory(dir);

            string scratchDig = new SurfaceQualityMark(Vector2.Zero, "60", "40").MarkText; // "60-40"
            Assert.AreEqual("60-40", scratchDig);

            var xml = BuildAttributeTableWithHighlight(scratchDig, scratchDig);
            Assert.IsTrue(xml.StartsWith("<svg") && xml.Contains("60-40") && xml.Contains("Surface quality")
                && xml.Contains("#008000"), "整表应含 Surface quality 行(scratch-dig 60-40)+ 绿色高亮框");
            File.WriteAllText(Path.Combine(dir, "surface-quality-mil-table.svg"), xml);
            Assert.IsTrue(File.Exists(Path.Combine(dir, "surface-quality-mil-table.svg")));
        }

        // RowLabels 顺序: R, Φe, 倒角, 表面, 3/, 4/, Surface quality, 6/ → 表面质量在第 6 行(0 基).
        private const int SurfaceQualityRowIndex = 6;

        /// <summary>
        /// 整张面属性区表(所有行不变, 仅去掉"技术要求"抬头), 把 Surface quality 行左/右表面值填入,
        /// 并用绿色框高亮该行。值由调用方传入(来自 SurfaceQualityMark.MarkText), 保持格式码单一真值。
        /// </summary>
        private static string BuildAttributeTableWithHighlight(string leftVal, string rightVal)
        {
            var table = new TechnicalRequirementTable(Vector2.Zero)
            {
                ShowTitle = false,   // 去掉"技术要求"抬头
                ColumnWidth = 52,    // 容下 "Surface quality" 标签 + 值
            };
            table.LeftSurface.ISO10110_3_Value = leftVal;    // Surface quality 行 (历史命名 ISO10110_3_Value)
            table.RightSurface.ISO10110_3_Value = rightVal;

            var svg = new SvgGraphicsDraw();
            table.Draw(svg);                                              // 先画整表(黑)
            DrawRowHighlight(svg, table, SurfaceQualityRowIndex,
                System.Drawing.Color.FromArgb(0, 128, 0));               // 再叠绿框高亮
            return svg.BuildSvg(pixelSize: 360, strokeWidth: 0.4);
        }

        /// <summary>在表格指定数据行外圈叠一个彩色框(模型坐标), 用于高亮某一行。</summary>
        private static void DrawRowHighlight(SvgGraphicsDraw svg, TechnicalRequirementTable t, int rowIndex, System.Drawing.Color color)
        {
            double titleH = t.ShowTitle ? t.TitleRowHeight : 0;
            // 数据行 i 顶 Y = Position.Y - (抬头 + 列标题 + i 行) ；列标题占一个 RowHeight
            double rowTop = t.Position.Y - (titleH + (rowIndex + 1) * t.RowHeight) * t.Scale;
            double rowH = t.RowHeight * t.Scale;
            double width = t.ColumnWidth * 3 * t.Scale;
            const double inset = 0.6;
            var prev = svg.CurrentColor;
            svg.CurrentColor = color;
            // DrawRectangle(position = 左下角, width, height)
            svg.DrawRectangle(new Vector2(t.Position.X + inset, rowTop - rowH + inset),
                width - 2 * inset, rowH - 2 * inset);
            svg.CurrentColor = prev;
        }

        private static string OutDir([CallerFilePath] string thisFile = "")
        {
            var repo = Directory.GetParent(thisFile)!.Parent!.Parent!.FullName; // …/src/lcdb.Tests → src → repo
            return Path.Combine(repo, "website-docusaurus", "static", "img", "marks");
        }
    }
}
