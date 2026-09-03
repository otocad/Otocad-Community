using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LitMath;
using lcdb;
using lcdb.NetDxfAdapter;

namespace lcdb.Tests
{
    /// <summary>
    /// 公差标法: 上下偏差不等时应"摞起来" (上偏差一行、下偏差一行, 小字号), 对称时用 ±, 而非行内 MTEXT 码.
    /// 用记录型 IGraphicsDraw 捕获 DrawStacked 的每段 DrawText (文本 + 字高) 来断言.
    /// </summary>
    [TestClass]
    public class ToleranceStackTests
    {
        private sealed class RecordingDraw : MockGraphicsDraw, OtoCAD.IGraphicsDraw
        {
            public readonly List<(string text, double h)> Texts = new();

            public new Vector2 DrawText(Vector2 position, string text, double height, string font, TextAlignment a, double angle)
            {
                Texts.Add((text, height));
                return position + new Vector2(text.Length * height * 0.55, 0);
            }
        }

        [TestMethod]
        public void Bilateral_deviations_are_stacked_upper_and_lower()
        {
            var rec = new RecordingDraw();
            var tol = new BasicTolerance(20.0, 0.10, -0.05);   // 上 +0.10, 下 -0.05
            ToleranceAnnotation.DrawStacked(rec, Vector2.Zero, "", tol, 2.5, "Arial");

            Assert.AreEqual(3, rec.Texts.Count, "应为 公称 + 上偏差 + 下偏差 三段");
            Assert.AreEqual("20.00", rec.Texts[0].text);
            Assert.AreEqual(1, rec.Texts.Count(t => t.text.StartsWith("+")), "应有一行上偏差(+)");
            Assert.AreEqual(1, rec.Texts.Count(t => t.text.StartsWith("-")), "应有一行下偏差(-)");
            // 偏差字号应小于公称 (摞起来的小字)
            foreach (var t in rec.Texts.Where(t => t.text.StartsWith("+") || t.text.StartsWith("-")))
                Assert.IsTrue(t.h < 2.5 - 1e-6 && t.h > 1.0, $"偏差字高 {t.h} 应小于公称字高 2.5");
            // 不应出现 MTEXT 堆叠码
            Assert.IsFalse(rec.Texts.Any(t => t.text.Contains("\\S") || t.text.Contains("^") || t.text.Contains("{")),
                "不应输出 MTEXT 堆叠码");
        }

        [TestMethod]
        public void Symmetric_deviation_uses_plus_minus_inline()
        {
            var rec = new RecordingDraw();
            var tol = new BasicTolerance(20.0, 0.05, -0.05);   // 对称
            ToleranceAnnotation.DrawStacked(rec, Vector2.Zero, "", tol, 2.5, "Arial");

            Assert.AreEqual(2, rec.Texts.Count, "对称公差应为 公称 + ±dev 两段");
            Assert.IsTrue(rec.Texts[1].text.Contains("±"), "对称应用 ± 行内表示");
        }

        [TestMethod]
        public void Zero_tolerance_draws_only_nominal()
        {
            var rec = new RecordingDraw();
            var tol = new BasicTolerance(20.0, 0.0, 0.0);
            ToleranceAnnotation.DrawStacked(rec, Vector2.Zero, "", tol, 2.5, "Arial");

            Assert.AreEqual(1, rec.Texts.Count, "无公差只画公称");
            Assert.AreEqual("20.00", rec.Texts[0].text);
        }

        [TestMethod]
        public void Text_renders_mtext_stack_code_as_stacked_deviations()
        {
            // 标注文本含 MTEXT 堆叠码 (自动标注非对称公差) → 应拆成 前缀 + 上偏差小字 + 下偏差小字
            var rec = new RecordingDraw();
            Text.DrawPossiblyStacked(rec, Vector2.Zero, "Ø = 20.00\\S+0.10^-0.05;", 2.5, "Arial", lcdb.TextAlignment.LeftBottom, 0);

            Assert.AreEqual(3, rec.Texts.Count, "应为 前缀 + 上 + 下 三段");
            Assert.AreEqual("Ø = 20.00", rec.Texts[0].text);
            Assert.IsTrue(rec.Texts.Any(t => t.text == "+0.10" && t.h < 2.5), "上偏差小字");
            Assert.IsTrue(rec.Texts.Any(t => t.text == "-0.05" && t.h < 2.5), "下偏差小字");
            Assert.IsFalse(rec.Texts.Any(t => t.text.Contains("\\S") || t.text.Contains("^")), "不应残留堆叠码");
        }

        [TestMethod]
        public void Text_without_stack_code_draws_plain()
        {
            var rec = new RecordingDraw();
            Text.DrawPossiblyStacked(rec, Vector2.Zero, "Ø = 20.00±0.05", 2.5, "Arial", lcdb.TextAlignment.LeftBottom, 0);

            Assert.AreEqual(1, rec.Texts.Count, "普通文本(含±)整段绘制");
            Assert.AreEqual("Ø = 20.00±0.05", rec.Texts[0].text);
        }
    }
}
