using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LitMath;
using lcdb;
using lcdb.Annotation;
using lcdb.Rendering;

namespace lcdb.Tests
{
    /// <summary>
    /// 金样(Golden Sample)直出测试 — 三个**已人工核对 ✅** 标记的基准渲染锁定:
    ///   1. 镀膜(GB/T 13323-2009 表1 #10–18,9 符号,✅ 2026-06-04)
    ///   2. 粗糙度(GB/T 131-2006 §4.1,去除/不去除/任意 3 模式,✅ 2026-06-05/06-07)
    ///   3. 表面质量(ISO 10110-7:2017 §4.2.1,属性区表格 ISO+MIL,✅ 2026-06-06·ISO 侧)
    ///
    /// 语义:人工核对通过那一刻的渲染 = 金样。渲染走与出图同一份几何代码(单一真值),
    /// 几何确定性 → 逐字节稳定;**任何漂移(代码改动导致渲染变化)即测试失败**,
    /// 防止"核对过的符号被后续改动悄悄画歪"。
    ///
    /// 有意改动(重新核对后)更新金样:置环境变量 `OTOCAD_UPDATE_GOLDEN=1` 重跑,
    /// 或删除 `website-docusaurus/static/golden/` 下对应文件后重跑(自举重建)。
    /// 汇总页:`/golden/golden-samples.html`(docs 站静态服务,自包含内联 SVG)。
    /// </summary>
    [TestClass]
    public class GoldenSampleTests
    {
        private sealed record Sample(string Group, string Name, string Caption, string Svg);

        // ---- 渲染三个 ✅ 标记的全部金样(构造与各合规测试/文档生成器同源) ----
        private static List<Sample> RenderAll()
        {
            var samples = new List<Sample>();

            // 1. 镀膜: 9 个 GB/T 13323-2009 表1 符号(同 CoatingMarkSvgDocGenerator 构造)
            var coatingTypes = new (CoatingType type, string gb, string name)[]
            {
                (CoatingType.AR, "#18", "减反射膜 ⊕"), (CoatingType.Polarizing, "#16", "偏振膜"),
                (CoatingType.HR, "#10", "内反射膜"), (CoatingType.OuterReflective, "#11", "外反射膜"),
                (CoatingType.BS, "#12", "分束(色)膜"), (CoatingType.Filter, "#13", "滤光膜 ⊖"),
                (CoatingType.Protective, "#14", "保护膜 ⊜"), (CoatingType.Conductive, "#15", "导电膜"),
                (CoatingType.Blackening, "#17", "涂黑 —·—"),
            };
            foreach (var (type, gb, name) in coatingTypes)
            {
                var mark = new CoatingMark { CoatingType = type, Size = 10, Center = Vector2.Zero, ShowText = false };
                var svg = new SvgGraphicsDraw();
                mark.Draw(svg);
                samples.Add(new Sample("coating", $"coating-{type}", $"{gb} {name}", svg.BuildSvg(pixelSize: 160)));
            }

            // 2. 粗糙度: 3 种材料去除模式(同 RoughnessMarkComplianceTests 构造)
            foreach (var (removal, name) in new[]
            {
                (MaterialRemoval.Remove, "去除材料 ∇(V + 封口横杠)"),
                (MaterialRemoval.NotRemove, "不去除材料 ⌀V(V + 内接圆)"),
                (MaterialRemoval.Any, "任意方法(V + 标注横线)"),
            })
            {
                var mark = new SurfaceRoughnessMark(Vector2.Zero, 10.0) { Removal = removal, ShowText = true };
                var svg = new SvgGraphicsDraw();
                mark.Draw(svg);
                samples.Add(new Sample("roughness", $"roughness-{removal}", name, svg.BuildSvg(pixelSize: 200)));
            }

            // 3. 表面质量: 属性区表格 ISO + MIL(同 SurfaceQualityMarkComplianceTests 构造)
            samples.Add(new Sample("surface-quality", "surface-quality-iso-table",
                "ISO 10110-7:`5/5×0.16` → Surface quality 行(绿框)", BuildQualityTable("5×0.16")));
            samples.Add(new Sample("surface-quality", "surface-quality-mil-table",
                "MIL-PRF-13830:`60-40` → 同一行(绿框)", BuildQualityTable("60-40")));

            return samples;
        }

        private static string BuildQualityTable(string cellValue)
        {
            var table = new TechnicalRequirementTable(Vector2.Zero) { ShowTitle = false, ColumnWidth = 52 };
            table.LeftSurface.ISO10110_3_Value = cellValue;   // Surface quality 行 (历史命名)
            table.RightSurface.ISO10110_3_Value = cellValue;

            var svg = new SvgGraphicsDraw();
            table.Draw(svg);
            // 绿框高亮 Surface quality 行 (行索引 6, 同合规测试)
            const int rowIndex = 6;
            double rowTop = table.Position.Y - (rowIndex + 1) * table.RowHeight * table.Scale;
            double rowH = table.RowHeight * table.Scale;
            double width = table.ColumnWidth * 3 * table.Scale;
            const double inset = 0.6;
            var prev = svg.CurrentColor;
            svg.CurrentColor = System.Drawing.Color.FromArgb(0, 128, 0);
            svg.DrawRectangle(new Vector2(table.Position.X + inset, rowTop - rowH + inset),
                width - 2 * inset, rowH - 2 * inset);
            svg.CurrentColor = prev;
            return svg.BuildSvg(pixelSize: 360, strokeWidth: 0.4);
        }

        // ========== 1. 金样锁定: 渲染与已提交基准逐字节一致, 漂移即失败 ==========

        [TestMethod]
        public void Golden_svgs_match_locked_baseline()
        {
            var dir = GoldenDir();
            Directory.CreateDirectory(dir);
            bool update = Environment.GetEnvironmentVariable("OTOCAD_UPDATE_GOLDEN") == "1";
            var drifted = new List<string>();

            foreach (var s in RenderAll())
            {
                Assert.IsTrue(s.Svg.StartsWith("<svg"), $"{s.Name}: 非法 SVG");
                var path = Path.Combine(dir, s.Name + ".svg");
                if (!File.Exists(path) || update)
                {
                    File.WriteAllText(path, s.Svg);   // 自举建立 / 显式更新金样
                    continue;
                }
                var golden = Norm(File.ReadAllText(path));
                if (golden != Norm(s.Svg)) drifted.Add(s.Name);
            }

            Assert.AreEqual(0, drifted.Count,
                "金样漂移(已人工核对的渲染被改动): " + string.Join(", ", drifted) +
                "。若是重新核对后的有意改动, 置 OTOCAD_UPDATE_GOLDEN=1 重跑更新基准; 否则这是回归, 修代码。");
        }

        private static string Norm(string s) => s.Replace("\r\n", "\n").TrimEnd();

        // ========== 2. 汇总 HTML(自包含内联 SVG, 确定性输出) ==========

        [TestMethod]
        public void Aggregate_golden_samples_html()
        {
            var dir = GoldenDir();
            Directory.CreateDirectory(dir);
            var samples = RenderAll();

            var groups = new (string key, string title, string standard, string verified)[]
            {
                ("coating", "镀膜", "GB/T 13323-2009 表1 #10–18", "✅ 2026-06-04 用户对照原图逐符号核对"),
                ("roughness", "表面粗糙度", "GB/T 131-2006 §4.1 / Figure 1", "✅ 2026-06-05 逐版 ASCII 核对 + 06-07 封口横杠修正"),
                ("surface-quality", "表面质量(疵病)", "ISO 10110-7:2017 §4.2.1(+MIL 二手源)", "✅ 2026-06-06 逐行对照原文(ISO 侧)"),
            };

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html>\n<html lang=\"zh\">\n<head>\n<meta charset=\"utf-8\"/>\n");
            sb.Append("<title>OtoCAD 金样 · 已人工核对标记基准渲染</title>\n<style>\n");
            sb.Append("body{font-family:system-ui,'Microsoft YaHei',sans-serif;margin:24px auto;max-width:1080px;color:#222}\n");
            sb.Append("h1{font-size:1.5em}h2{border-bottom:2px solid #008000;padding-bottom:4px;margin-top:36px}\n");
            sb.Append(".meta{color:#555;font-size:.9em;margin:4px 0 12px}\n");
            sb.Append(".grid{display:flex;flex-wrap:wrap;gap:18px;align-items:flex-end}\n");
            sb.Append("figure{margin:0;text-align:center;border:1px solid #ddd;border-radius:6px;padding:10px;background:#fff}\n");
            sb.Append("figcaption{font-size:.85em;color:#333;margin-top:6px;max-width:240px}\n");
            sb.Append(".note{background:#f6f8fa;border-left:4px solid #008000;padding:8px 12px;font-size:.9em}\n");
            sb.Append("</style>\n</head>\n<body>\n");
            sb.Append("<h1>OtoCAD 金样(Golden Samples)— 已人工核对 ✅ 标记基准渲染</h1>\n");
            sb.Append("<p class=\"note\">人工核对通过那一刻的渲染即金样。本页由 <code>GoldenSampleTests</code> 直出(与出图同一份几何代码,单一真值);" +
                "对应 SVG 逐字节锁定在 <code>static/golden/</code>,<b>任何漂移即测试失败</b>。有意更新:<code>OTOCAD_UPDATE_GOLDEN=1</code> 重跑。</p>\n");

            foreach (var g in groups)
            {
                var items = samples.Where(s => s.Group == g.key).ToList();
                sb.Append($"<h2>{g.title}</h2>\n<p class=\"meta\">标准:{g.standard} ｜ 核对:{g.verified} ｜ 样本 {items.Count} 个</p>\n<div class=\"grid\">\n");
                foreach (var s in items)
                {
                    sb.Append("<figure>");
                    sb.Append(s.Svg);
                    sb.Append($"<figcaption>{s.Caption}</figcaption></figure>\n");
                }
                sb.Append("</div>\n");
            }
            sb.Append("</body>\n</html>\n");

            var html = sb.ToString();
            // 守卫: 三组齐 + 14 个样本(9 镀膜 + 3 粗糙度 + 2 表面质量)全内联
            Assert.IsTrue(html.Contains("<h2>镀膜</h2>") && html.Contains("<h2>表面粗糙度</h2>") && html.Contains("<h2>表面质量(疵病)</h2>"),
                "汇总页应含三个 ✅ 标记分节");
            int svgCount = html.Split(new[] { "<svg" }, StringSplitOptions.None).Length - 1;
            Assert.AreEqual(14, svgCount, "应内联 14 个金样 SVG(9 镀膜 + 3 粗糙度 + 2 表面质量)");

            File.WriteAllText(Path.Combine(dir, "golden-samples.html"), html);
            Assert.IsTrue(File.Exists(Path.Combine(dir, "golden-samples.html")));
        }

        private static string GoldenDir([CallerFilePath] string thisFile = "")
        {
            var repo = Directory.GetParent(thisFile)!.Parent!.Parent!.FullName; // …/src/lcdb.Tests → src → repo
            return Path.Combine(repo, "website-docusaurus", "static", "golden");
        }
    }
}
