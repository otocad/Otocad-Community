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
    /// 面型精度标记合规自检 (ISO 10110-5 / GB/T 11297.5-2011).
    ///
    /// 与镀膜(纯几何)不同, 面型精度的合规分两面, 用对应工具各自验证:
    ///   1. **文字格式码** 3/A(B/C) —— 确定性字符串, 用格式断言 (比 OCR 更准).
    ///   2. **框几何** 虚线框 —— 实体级几何断言 (4 条 Dash 线 + 框内代码 + 框外 λ).
    /// 见 memory: ocr-vs-geometry-verification; 双边对照页 dev/form-accuracy-compliance.
    /// </summary>
    [TestClass]
    public class FormAccuracyMarkComplianceTests
    {
        // ---- 反射取 Generate() 产出的 _markEntities (与 CoatingMark 同模式) ----
        private static List<Entity> Generate(FormAccuracyMark m)
        {
            var gen = typeof(FormAccuracyMark).GetMethod("Generate", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(gen, "未找到 FormAccuracyMark.Generate()");
            gen!.Invoke(m, null);
            var fld = typeof(FormAccuracyMark).GetField("_markEntities", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fld, "未找到 FormAccuracyMark._markEntities");
            return (List<Entity>)fld!.GetValue(m)!;
        }

        private static void RefreshText(FormAccuracyMark m)
        {
            var upd = typeof(FormAccuracyMark).GetMethod("UpdateMarkText", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(upd, "未找到 FormAccuracyMark.UpdateMarkText()");
            upd!.Invoke(m, null);
        }

        // ========== 1. 文字格式码 (ISO 10110-5: 3/A(B/C)) ==========

        [TestMethod]
        public void ISO_code_power_and_irregularity()          // 3/A(B)
        {
            var m = new FormAccuracyMark(Vector2.Zero, powerFringes: 4, irregularityFringes: 1);
            Assert.AreEqual("3/4(1)", m.MarkText);
        }

        [TestMethod]
        public void ISO_code_with_rotational_symmetry()        // 3/A(B/C)
        {
            var m = new FormAccuracyMark(Vector2.Zero, powerFringes: 3, irregularityFringes: 1, rotationalSymmetry: 2);
            Assert.AreEqual("3/3(1/2)", m.MarkText);
        }

        [TestMethod]
        public void ISO_code_irregularity_only()               // 3/-(B)
        {
            var m = new FormAccuracyMark { IrregularityOnly = true, IrregularityFringes = 1 };
            RefreshText(m);
            Assert.AreEqual("3/-(1)", m.MarkText);
        }

        [TestMethod]
        public void ISO_code_with_evaluation_diameter()        // 3/A(B) Ø
        {
            var m = new FormAccuracyMark { PowerFringes = 4, IrregularityFringes = 1, EvaluationDiameter = 30 };
            RefreshText(m);
            Assert.AreEqual("3/4(1) Ø30.0", m.MarkText);
        }

        [TestMethod]
        public void ISO_code_always_starts_with_form_code_3()  // ISO 10110-5 面形代号恒为 "3/"
        {
            foreach (var (p, b) in new[] { (1.0, 0.5), (10.0, 2.0), (0.5, 0.2) })
            {
                var m = new FormAccuracyMark(Vector2.Zero, p, b);
                StringAssert.StartsWith(m.MarkText, "3/", $"面形码必须以 3/ 开头, 实得 {m.MarkText}");
            }
        }

        // ========== 2. 框几何 (虚线框 + 框内代码 + 框外 λ) ==========

        [TestMethod]
        public void DashedBox_is_four_dashed_lines()
        {
            var m = new FormAccuracyMark { Position = Vector2.Zero, Scale = 1, MarkStyle = FormAccuracyMarkStyle.DashedBox };
            var e = Generate(m);
            var dashed = e.OfType<Line>().Where(l => l.lineType == LineType.Dash).ToList();
            Assert.AreEqual(4, dashed.Count, "DashedBox 应为 4 条虚线边");
        }

        [TestMethod]
        public void Frame_contains_iso_code_and_wavelength_text()
        {
            var m = new FormAccuracyMark(Vector2.Zero, powerFringes: 4, irregularityFringes: 1) { ShowText = true };
            var texts = Generate(m).OfType<Text>().Select(t => t.Value).ToList();
            Assert.IsTrue(texts.Any(v => v == "3/4(1)"), "框内应有 ISO 代码 3/4(1)");
            Assert.IsTrue(texts.Any(v => v != null && v.Contains("λ=")), "框外应有测试波长 λ= 注记");
        }

        // ========== 3. 文档 SVG 资产 (真实实体经 SvgGraphicsDraw) ==========

        [TestMethod]
        public void Generate_form_accuracy_svgs_into_docusaurus()
        {
            var dir = OutDir();
            Directory.CreateDirectory(dir);

            // 代表性示例: 标准 3/A(B) 与 仅不规则度 3/-(B)
            var samples = new (string name, FormAccuracyMark mark)[]
            {
                ("form-accuracy-3-4-1", new FormAccuracyMark(Vector2.Zero, 4, 1)),
                ("form-accuracy-irregularity-only", BuildIrregularityOnly()),
            };

            foreach (var (name, mark) in samples)
            {
                var svg = new SvgGraphicsDraw();
                mark.Draw(svg);
                var xml = svg.BuildSvg(pixelSize: 220);
                Assert.IsTrue(xml.StartsWith("<svg") && xml.Contains("<line"), $"{name}: SVG 异常");
                File.WriteAllText(Path.Combine(dir, $"{name}.svg"), xml);
            }
            Assert.IsTrue(File.Exists(Path.Combine(dir, "form-accuracy-3-4-1.svg")));
        }

        private static FormAccuracyMark BuildIrregularityOnly()
        {
            var m = new FormAccuracyMark { IrregularityOnly = true, IrregularityFringes = 1 };
            var upd = typeof(FormAccuracyMark).GetMethod("UpdateMarkText", BindingFlags.NonPublic | BindingFlags.Instance);
            upd!.Invoke(m, null);
            return m;
        }

        private static string OutDir([CallerFilePath] string thisFile = "")
        {
            var repo = Directory.GetParent(thisFile)!.Parent!.Parent!.FullName; // …/src/lcdb.Tests → src → repo
            return Path.Combine(repo, "website-docusaurus", "static", "img", "marks");
        }
    }
}
