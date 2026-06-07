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
    /// 精度·公差 + ISO 10110 剩余代号标记合规自检 —— T 型(文字格式码)。
    /// 对文字格式码标记, "对齐标准"的可证伪契约 = 渲染出的代号串符合标准格式(引用具体条目);
    /// 框几何各异(矩形/圆角矩形/带装饰), 仅冒烟(Generate 产出非空)。原文实锤:
    ///   面形   ISO 10110-5 §7   `3/...`(面形公差; Zernike 项 `3/i,j:T`)
    ///   对中   ISO 10110-6 §5.3 `4/...`(代号 4)
    ///   非球面 ISO 10110-12 §5.2 → 面形 `3/`(无专用 12/ 代号)
    ///   波前   ISO 10110-14 §5.2 `13/...`
    ///   激光   ISO 10110-17(ISO 10110-10 代号表) `6/...`
    /// </summary>
    [TestClass]
    public class PrecisionCodeMarksComplianceTests
    {
        private static List<Entity> Generate(object mark)
        {
            var t = mark.GetType();
            var gen = t.GetMethod("Generate", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(gen, $"未找到 {t.Name}.Generate()");
            gen!.Invoke(mark, null);
            var fld = t.GetField("_markEntities", BindingFlags.NonPublic | BindingFlags.Instance);
            return (List<Entity>)fld!.GetValue(mark)!;
        }

        private static void AssertSymbolDrawn(object mark)
        {
            var e = Generate(mark);
            Assert.IsTrue(e.Count > 0, $"{mark.GetType().Name}: Generate 应产出符号实体");
            Assert.IsTrue(e.OfType<Line>().Any(), $"{mark.GetType().Name}: 应含框线");
        }

        // ===== 面形 3/ (ISO 10110-5, Zernike 项) =====
        [TestMethod] public void SurfaceForm_3_slash()
        {
            var m = new SurfaceFormMark(Vector2.Zero, "3", 0.05);
            Assert.AreEqual("3/3:0.050", m.MarkText);
            AssertSymbolDrawn(m);
        }

        // ===== 对中 4/ (ISO 10110-6) =====
        [TestMethod] public void Centering_4_slash()
        {
            var m = new CenteringToleranceMark(Vector2.Zero, 0.05, 0.02);
            Assert.AreEqual("4/0.050,0.020", m.MarkText);
            StringAssert.StartsWith(m.MarkText, "4/", $"对中代号应 4/, 实得 {m.MarkText}");
            AssertSymbolDrawn(m);
        }

        // ===== 非球面 3/ (ISO 10110-12 → 面形, 无专用 12/) =====
        [TestMethod] public void Aspheric_3_slash_not_12()
        {
            var m = new ISO10110_12Mark(Vector2.Zero, 0.5);
            Assert.AreEqual("3/0.50", m.MarkText);
            Assert.IsFalse(m.MarkText.StartsWith("12/"), "非球面不应用部件号 12/");
            AssertSymbolDrawn(m);
        }

        // ===== 波前 13/ (ISO 10110-14 §5.2) =====
        [TestMethod] public void Wavefront_13_slash()
        {
            var m = new ISO10110_14Mark(Vector2.Zero, 0.25);
            StringAssert.StartsWith(m.MarkText, "13/", $"波前代号应 13/, 实得 {m.MarkText}");
            Assert.AreEqual("13/0.25λ RMS", m.MarkText);
            AssertSymbolDrawn(m);
        }

        // ===== 激光 6/ (ISO 10110-17, 代号表 6/) =====
        [TestMethod] public void LaserDamage_6_slash_not_17()
        {
            var m = new LaserDamageThresholdMark(Vector2.Zero, 5.0, 1064.0, 10.0);
            StringAssert.StartsWith(m.MarkText, "6/", $"激光损伤代号应 6/, 实得 {m.MarkText}");
            Assert.IsFalse(m.MarkText.StartsWith("17/"), "激光损伤不应用部件号 17/");
            AssertSymbolDrawn(m);
        }

        // ===== SVG 资产 =====
        [TestMethod] public void Generate_precision_code_svgs()
        {
            var dir = OutDir();
            Directory.CreateDirectory(dir);
            var samples = new (string name, Entity mark)[]
            {
                ("surface-form-3", new SurfaceFormMark(Vector2.Zero, "3", 0.05)),
                ("centering-4", new CenteringToleranceMark(Vector2.Zero, 0.05, 0.02)),
                ("aspheric-3", new ISO10110_12Mark(Vector2.Zero, 0.5)),
                ("wavefront-13", new ISO10110_14Mark(Vector2.Zero, 0.25)),
                ("laser-damage-6", new LaserDamageThresholdMark(Vector2.Zero, 5.0, 1064.0, 10.0)),
            };
            foreach (var (name, mark) in samples)
            {
                var svg = new SvgGraphicsDraw();
                mark.Draw(svg);
                var xml = svg.BuildSvg(pixelSize: 220);
                Assert.IsTrue(xml.StartsWith("<svg"), $"{name}: SVG 异常");
                File.WriteAllText(Path.Combine(dir, $"{name}.svg"), xml);
            }
            Assert.IsTrue(File.Exists(Path.Combine(dir, "wavefront-13.svg")));
        }

        private static string OutDir([CallerFilePath] string thisFile = "")
        {
            var repo = Directory.GetParent(thisFile)!.Parent!.Parent!.FullName;
            return Path.Combine(repo, "website-docusaurus", "static", "img", "marks");
        }
    }
}
