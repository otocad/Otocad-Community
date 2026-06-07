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
    /// ISO 10110 材料缺陷(应力 0/ / 气泡 1/ / 不均匀 2/)合规自检。
    ///
    /// ⚠️ 材料缺陷是**玻璃体性质**, 属"对材料的要求", 按 GB/T 13323-2009 列入图纸下方的
    /// **材料要求表**(<see cref="TechnicalRequirementTable"/> 的「材料技术要求」列), **不是独立贴面标记**。
    /// 故核实对象是"表里的材料行代号", 而非独立标记的框几何。
    /// 原文代号: ISO 10110-2 §4.3 `0/A`、-3 §4.3 `1/N×A`、-4 §code=2 `2/A;B`。
    /// `ISO10110_2/3/4Mark` 类仅保留作数据/反序列化, 已移出 Ribbon。
    /// </summary>
    [TestClass]
    public class Iso10110SeriesComplianceTests
    {
        // ===== 材料要求表: 材料行代号 = 0/ 1/ 2/ (真正归宿) =====
        [TestMethod]
        public void MaterialTable_row_labels_are_0_1_2()
        {
            var fld = typeof(TechnicalRequirementTable)
                .GetField("MaterialRowLabels", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(fld, "未找到 TechnicalRequirementTable.MaterialRowLabels");
            var labels = (string[])fld!.GetValue(null)!;
            // 材料行 = 折射率 n、阿贝数 ν + 三个材料缺陷代号 0/(应力) 1/(气泡) 2/(不均匀)
            foreach (var code in new[] { "0/", "1/", "2/" })
                CollectionAssert.Contains(labels, code, $"材料要求表材料行应含缺陷代号 {code}");
        }

        [TestMethod]
        public void MaterialTable_renders_material_codes()
        {
            var table = new TechnicalRequirementTable(Vector2.Zero);
            var gen = typeof(TechnicalRequirementTable).GetMethod("Generate", BindingFlags.NonPublic | BindingFlags.Instance);
            gen!.Invoke(table, null);
            var fld = typeof(TechnicalRequirementTable).GetField("_markEntities", BindingFlags.NonPublic | BindingFlags.Instance)
                      ?? typeof(TechnicalRequirementTable).GetField("markEntities", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fld, "未找到表的实体缓存字段");
            var texts = ((IEnumerable<Entity>)fld!.GetValue(table)!).OfType<Text>().Select(t => t.Value).ToList();
            foreach (var code in new[] { "0/", "1/", "2/" })
                Assert.IsTrue(texts.Any(v => v != null && v.Contains(code)), $"材料要求表应渲出材料代号 {code}");
        }

        // ===== 代号格式(类作数据/序列化仍须正确) =====
        [TestMethod] public void Stress_format_0_slash_A()
            => Assert.AreEqual("0/10", new ISO10110_2Mark(Vector2.Zero, 10.0).MarkText);

        [TestMethod] public void Bubbles_format_1_slash_NxA()
            => Assert.AreEqual("1/1×0.16", new ISO10110_3Mark(Vector2.Zero, 1, 0.16).MarkText);

        [TestMethod] public void Inhomogeneity_format_2_slash_A_B()
            => Assert.AreEqual("2/2;A", new ISO10110_4Mark(Vector2.Zero, 2, "A").MarkText);

        // ===== SVG: 材料要求表(表是材料缺陷的真实呈现) =====
        [TestMethod]
        public void Generate_material_table_svg()
        {
            var dir = OutDir();
            Directory.CreateDirectory(dir);
            var table = new TechnicalRequirementTable(Vector2.Zero);
            var svg = new SvgGraphicsDraw();
            table.Draw(svg);
            var xml = svg.BuildSvg(pixelSize: 320, strokeWidth: 0.4);   // 细线: 表很大, 自适应线宽会粗到糊住小字
            Assert.IsTrue(xml.StartsWith("<svg") && xml.Contains("<line"), "SVG 异常");
            File.WriteAllText(Path.Combine(dir, "material-requirement-table.svg"), xml);
            Assert.IsTrue(File.Exists(Path.Combine(dir, "material-requirement-table.svg")));
        }

        private static string OutDir([CallerFilePath] string thisFile = "")
        {
            var repo = Directory.GetParent(thisFile)!.Parent!.Parent!.FullName;
            return Path.Combine(repo, "website-docusaurus", "static", "img", "marks");
        }
    }
}
