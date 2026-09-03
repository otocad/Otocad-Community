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
    /// 表面纹理(光学表面结构)合规自检 — ISO 10110-8 / GB/T 13323-2009 附录C 的 G/P 代码。
    ///
    /// 关键代号事实(代号审计已核, ISO 10110-10 代号表):**纹理无 slash 代号** —— G(磨砂面,Rq)
    /// / P(抛光面,P1–P4),`6/` 是激光损伤的代号、`8/` 系凭空 → 反作弊护栏断言绝不带 slash 前缀。
    ///
    /// 模型(skill optical-mark-compliance):纹理是"对零件的要求",归**面属性区 Surface texture 行**,
    /// 不作独立带框标记 → SurfaceTextureMark 为数据载体。与机械粗糙度(GB/T 131 Ra V 形)并存各管各。
    /// </summary>
    [TestClass]
    public class SurfaceTextureMarkComplianceTests
    {
        // ========== 1. G/P 代码串 (GB13323 附录C / ISO 10110-8) ==========

        [TestMethod]
        public void Polished_plain_is_P()                       // 无微缺陷要求的抛光面 = "P"
        {
            var m = new SurfaceTextureMark
            {
                GB13323Type = GB13323SurfaceType.Polished_P,
                SlopeSampleLength = 0,
                MicrodefectCount = 0,
                RqValue = 0,
            };
            m.UpdateMarkText();
            Assert.AreEqual("P", m.MarkText);
        }

        [TestMethod]
        public void Polished_P3_full_form()                     // P3/斜率/微缺陷/Rq
        {
            var m = new SurfaceTextureMark
            {
                GB13323Type = GB13323SurfaceType.Polished_P3,
                SlopeSampleLength = 0.002,
                MicrodefectCount = 1,
                RqValue = 0.002,
            };
            m.UpdateMarkText();
            Assert.AreEqual("P3/0.002/1/Rq0.002", m.MarkText);
        }

        [TestMethod]
        public void Ground_with_Rq()                            // 磨砂面 G + Rq
        {
            var m = new SurfaceTextureMark
            {
                GB13323Type = GB13323SurfaceType.Rough_G,
                SlopeSampleLength = 0,
                RqValue = 2.0,
            };
            m.UpdateMarkText();
            Assert.AreEqual("G/Rq2.000", m.MarkText);
        }

        [TestMethod]
        public void Texture_code_never_carries_slash_prefix()   // 反作弊: 不得有 6/ (激光) 或凭空 8/
        {
            foreach (var t in new[]
            {
                GB13323SurfaceType.Rough_G, GB13323SurfaceType.Polished_P,
                GB13323SurfaceType.Polished_P1, GB13323SurfaceType.Polished_P4,
            })
            {
                var m = new SurfaceTextureMark { GB13323Type = t };
                m.UpdateMarkText();
                Assert.IsFalse(m.MarkText.StartsWith("6/") || m.MarkText.StartsWith("8/"),
                    $"纹理无 slash 代号(6/=激光, 8/=凭空), 实得 {m.MarkText}");
                Assert.IsTrue(m.MarkText.StartsWith("G") || m.MarkText.StartsWith("P"),
                    $"纹理代码应以 G/P 开头, 实得 {m.MarkText}");
            }
        }

        [TestMethod]
        public void Uses_decimal_point_regardless_of_locale()   // 逗号小数区域下仍输出小数点
        {
            var prev = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture =
                    System.Globalization.CultureInfo.GetCultureInfo("de-DE");
                var m = new SurfaceTextureMark
                {
                    GB13323Type = GB13323SurfaceType.Polished_P3,
                    SlopeSampleLength = 0.002,
                    MicrodefectCount = 1,
                    RqValue = 0.002,
                };
                m.UpdateMarkText();
                Assert.AreEqual("P3/0.002/1/Rq0.002", m.MarkText);
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
            var m = new SurfaceTextureMark();
            var svg = new SvgGraphicsDraw();
            m.Draw(svg);
            var xml = svg.BuildSvg(pixelSize: 100);
            Assert.IsFalse(xml.Contains("<line"), "数据载体不应自绘任何线(独立带框标记已撤)");
        }

        // ========== 3. 预览资产: 属性区整表 + 绿框高亮 Surface texture 行 ==========

        [TestMethod]
        public void Generate_surface_texture_table_svg_into_docusaurus()
        {
            // 纹理进面属性区 Surface texture 行(描述标签, 非 6/ —— 6/ 是激光的代号)。
            // 单元值 = 真实标记 MarkText(G/P 自带类型符号, 不剥前缀)。
            var dir = OutDir();
            Directory.CreateDirectory(dir);

            var mark = new SurfaceTextureMark
            {
                GB13323Type = GB13323SurfaceType.Polished_P3,
                SlopeSampleLength = 0,
                MicrodefectCount = 0,
                RqValue = 0.002,
            };
            mark.UpdateMarkText();
            Assert.AreEqual("P3/Rq0.002", mark.MarkText);

            var xml = BuildAttributeTableWithHighlight(mark.MarkText, mark.MarkText);
            Assert.IsTrue(xml.StartsWith("<svg") && xml.Contains("Surface texture") && xml.Contains("P3/Rq0.002")
                && xml.Contains("#008000"), "整表应含 Surface texture 行(值 P3/Rq0.002)+ 绿色高亮框");
            Assert.IsFalse(xml.Contains(">6/<"), "纹理行标签不得是 6/(激光的代号)");
            File.WriteAllText(Path.Combine(dir, "surface-texture-table.svg"), xml);
            Assert.IsTrue(File.Exists(Path.Combine(dir, "surface-texture-table.svg")));
        }

        // RowLabels 顺序: R, Φe, 倒角, 表面, 3/, 4/, Surface quality, Surface texture → 纹理在第 7 行(0 基).
        private const int TextureRowIndex = 7;

        private static string BuildAttributeTableWithHighlight(string leftVal, string rightVal)
        {
            var table = new TechnicalRequirementTable(Vector2.Zero)
            {
                ShowTitle = false,
                ColumnWidth = 52,
            };
            table.LeftSurface.ISO10110_4_Value = leftVal;    // Surface texture 行 (历史命名 ISO10110_4_Value)
            table.RightSurface.ISO10110_4_Value = rightVal;

            var svg = new SvgGraphicsDraw();
            table.Draw(svg);
            DrawRowHighlight(svg, table, TextureRowIndex, System.Drawing.Color.FromArgb(0, 128, 0));
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
