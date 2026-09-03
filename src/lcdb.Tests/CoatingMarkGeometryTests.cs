using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LitMath;
using lcdb;
using lcdb.Annotation;
using lcdb.Rendering;

namespace lcdb.Tests
{
    /// <summary>
    /// 镀膜标记**几何**合规自检 (GB/T 13323-2009 表1 符号形状).
    /// 镀膜符号主体是几何 (圆/十字/X/Y/横线), 常无文字 → OCR 无能为力, 必须断言 Generate() 产出的实体形状.
    /// 直接断言确定性几何, 不渲染、不 OCR, 跨平台. 见 memory: ocr-vs-geometry-verification.
    /// </summary>
    [TestClass]
    public class CoatingMarkGeometryTests
    {
        private const double MarkSize = 10.0;

        /// <summary>反射触发 Generate() 并取私有 _markEntities (Generate/字段非公开).</summary>
        private static List<Entity> Generate(CoatingType type)
        {
            var m = new CoatingMark { CoatingType = type, Size = MarkSize, Center = Vector2.Zero };
            var gen = typeof(CoatingMark).GetMethod("Generate", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(gen, "未找到 CoatingMark.Generate()");
            gen!.Invoke(m, null);
            var fld = typeof(CoatingMark).GetField("_markEntities", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fld, "未找到 CoatingMark._markEntities");
            return (List<Entity>)fld!.GetValue(m)!;
        }

        private static int CircleCount(List<Entity> e) => e.OfType<Circle>().Count();
        private static List<Line> LinesOf(List<Entity> e) => e.OfType<Line>().ToList();
        private static bool IsHorizontal(Line l) =>
            Math.Abs(l.startPoint.Y - l.endPoint.Y) < 1e-6 && Math.Abs(l.startPoint.X - l.endPoint.X) > 1e-6;
        private static bool IsVertical(Line l) =>
            Math.Abs(l.startPoint.X - l.endPoint.X) < 1e-6 && Math.Abs(l.startPoint.Y - l.endPoint.Y) > 1e-6;
        private static bool IsDiagonal(Line l) => !IsHorizontal(l) && !IsVertical(l);

        // GB/T 13323-2009 表1: 各镀膜类型的符号 = (外圆数, 线段数)
        [DataTestMethod]
        [DataRow(CoatingType.AR, 1, 2)]              // ⊕ 圆+完整十字
        [DataRow(CoatingType.Polarizing, 1, 2)]      // 圆+横+上半竖
        [DataRow(CoatingType.HR, 1, 3)]              // 圆+顶横线+尖(圆底): 闭合 ▽
        [DataRow(CoatingType.OuterReflective, 1, 2)] // 圆+尖(圆底,无横线): 两斜边
        [DataRow(CoatingType.BS, 1, 3)]              // 分束(色)膜: 外圆 + Y
        [DataRow(CoatingType.Filter, 1, 1)]          // ⊖ 圆+单横线
        [DataRow(CoatingType.Protective, 1, 2)]      // ⊜ 圆+双横线
        [DataRow(CoatingType.Conductive, 1, 4)]      // 圆 + 单波浪线 (4 段折线)
        [DataRow(CoatingType.Blackening, 0, 3)]      // --- 点划 (无圆, 划-点-划)
        public void CoatingMark_symbol_geometry_matrix(CoatingType type, int circles, int lines)
        {
            var e = Generate(type);
            Assert.AreEqual(circles, CircleCount(e), $"{type}: 外圆数量不符");
            Assert.AreEqual(lines, LinesOf(e).Count, $"{type}: 线段数量不符");
            foreach (var c in e.OfType<Circle>())
                Assert.AreEqual(MarkSize * 0.5, c.radius, 1e-6, $"{type}: 外圆半径应为 Size/2");
        }

        [TestMethod]
        public void AR_is_circle_plus_full_upright_cross()   // ⊕ 圆+完整十字: 竖线贯穿上下
        {
            var lines = LinesOf(Generate(CoatingType.AR));
            Assert.AreEqual(1, lines.Count(IsHorizontal), "AR 应含 1 条水平线");
            var verticals = lines.Where(IsVertical).ToList();
            Assert.AreEqual(1, verticals.Count, "AR 应含 1 条垂直线");
            // 完整十字: 竖线贯穿 -r..+r (与偏振膜的"上半竖"区分)
            var v = verticals[0];
            double minY = Math.Min(v.startPoint.Y, v.endPoint.Y);
            double maxY = Math.Max(v.startPoint.Y, v.endPoint.Y);
            Assert.AreEqual(-MarkSize * 0.5, minY, 1e-6, "AR 竖线应到底 (−r)");
            Assert.AreEqual(MarkSize * 0.5, maxY, 1e-6, "AR 竖线应到顶 (+r)");
        }

        [TestMethod]
        public void Polarizing_is_circle_plus_half_cross()   // 圆+横+上半竖 (非完整十字)
        {
            var lines = LinesOf(Generate(CoatingType.Polarizing));
            Assert.AreEqual(1, lines.Count(IsHorizontal), "偏振膜应含 1 条水平线");
            var verticals = lines.Where(IsVertical).ToList();
            Assert.AreEqual(1, verticals.Count, "偏振膜应含 1 条垂直线");
            // 上半竖: 从圆心 (0) 到顶 (+r), 不到底
            var v = verticals[0];
            double minY = Math.Min(v.startPoint.Y, v.endPoint.Y);
            double maxY = Math.Max(v.startPoint.Y, v.endPoint.Y);
            Assert.AreEqual(0.0, minY, 1e-6, "偏振膜竖线下端在圆心 (0), 不到底");
            Assert.AreEqual(MarkSize * 0.5, maxY, 1e-6, "偏振膜竖线上端在顶 (+r)");
        }

        [TestMethod]
        public void HR_is_top_hline_plus_up_apex_legs_to_arc_60deg()  // 内反: 顶横线 + 上尖(线上) + 两腿向下张到圆弧, 总60°
        {
            var e = Generate(CoatingType.HR);
            Assert.AreEqual(1, CircleCount(e), "HR 应 1 外圆");
            var lines = LinesOf(e);
            Assert.AreEqual(3, lines.Count, "顶横线 + 两腿 = 3 条");

            double r = MarkSize * 0.5, by = r / 3.0;
            // 1 条横线在上 1/3
            var hor = lines.Where(IsHorizontal).ToList();
            Assert.AreEqual(1, hor.Count, "应有 1 条顶横线");
            Assert.AreEqual(by, hor[0].startPoint.Y, 1e-6, "横线在上 1/3 (y=+r/3)");

            // 上尖(顶点)在线上中点 (0, +r/3), 两腿共享它
            var apex = new Vector2(0, by);
            var legs = lines.Where(IsDiagonal).ToList();
            Assert.AreEqual(2, legs.Count, "应有 2 条腿");
            foreach (var l in legs)
                Assert.IsTrue((l.startPoint - apex).length < 1e-6 || (l.endPoint - apex).length < 1e-6,
                    "两腿应交于线上中点的上尖 (0,+r/3)");

            // 腿端落在圆周(圆弧)上 + 在尖下方
            Vector2 Far(Line l) => (l.startPoint - apex).length < 1e-6 ? l.endPoint : l.startPoint;
            var f1 = Far(legs[0]);
            var f2 = Far(legs[1]);
            Assert.AreEqual(r, (f1 - Vector2.Zero).length, 1e-6, "左腿端落在圆周上");
            Assert.AreEqual(r, (f2 - Vector2.Zero).length, 1e-6, "右腿端落在圆周上");
            Assert.IsTrue(f1.Y < by && f2.Y < by, "腿向下 (端点低于上尖)");

            // 总张角 == 60° (单边 30°)
            var v1 = f1 - apex; var v2 = f2 - apex;
            double ang = Math.Acos((v1.X * v2.X + v1.Y * v2.Y) / (v1.length * v2.length)) * 180.0 / Math.PI;
            Assert.AreEqual(60.0, ang, 0.5, "尖角总张角应为 60° (单边 30°)");
        }

        [TestMethod]
        public void OuterReflective_is_apex_only_no_hline()  // 外反: 只有尖(圆底两斜边), 无横线
        {
            var e = Generate(CoatingType.OuterReflective);
            Assert.AreEqual(1, CircleCount(e), "外反射膜应 1 外圆");
            var lines = LinesOf(e);
            Assert.AreEqual(2, lines.Count, "只有两条斜边");
            Assert.AreEqual(0, lines.Count(IsHorizontal), "外反射膜无横线");
            Assert.IsTrue(lines.All(IsDiagonal), "两条斜边都斜");
            var apexY = lines.SelectMany(l => new[] { l.startPoint, l.endPoint }).Min(p => p.Y);
            Assert.AreEqual(-MarkSize * 0.5, apexY, 1e-6, "尖在圆底 (−r)");
            // 两斜边共享圆底那个尖点
            var apex = lines.SelectMany(l => new[] { l.startPoint, l.endPoint })
                            .OrderBy(p => p.Y).First();
            int sharing = lines.Count(l => (l.startPoint - apex).length < 1e-6 || (l.endPoint - apex).length < 1e-6);
            Assert.AreEqual(2, sharing, "两斜边交于同一圆底尖点");
        }

        [TestMethod]
        public void Conductive_is_circle_plus_single_wave()   // 导电膜: 外圆 + 一条波浪线
        {
            var e = Generate(CoatingType.Conductive);
            Assert.AreEqual(1, CircleCount(e), "导电膜有外圆");
            var lines = LinesOf(e);
            Assert.AreEqual(4, lines.Count, "单条波浪线 = 4 段折线");
            Assert.IsTrue(lines.All(IsDiagonal), "波浪折线每段都斜");
            // 单行波浪 → 端点聚成 1 个中心 Y 高度
            var yLevels = lines.Select(l => Math.Round((l.startPoint.Y + l.endPoint.Y) / 2.0, 3))
                               .Distinct().Count();
            Assert.AreEqual(1, yLevels, "应为单行波浪线");
        }

        [TestMethod]
        public void Blackening_is_dash_dot_dash_horizontal()  // --- 点划 (无外圆, 全水平)
        {
            var e = Generate(CoatingType.Blackening);
            Assert.AreEqual(0, CircleCount(e), "涂黑无外圆");
            var lines = LinesOf(e);
            Assert.AreEqual(3, lines.Count, "涂黑 = 划-点-划 三段");
            Assert.IsTrue(lines.All(IsHorizontal), "点划线全部水平");
        }

        [TestMethod]
        public void AR_circle_cross_and_label_share_mark_color()  // 出图保真: 圆/线/文字同色
        {
            // 经真实 SvgGraphicsDraw 管线渲染 AR (DarkGreen #006400, 含文字标签).
            // Circle.Draw / Text.Draw 修复前: 圆与文字会落到环境黑 #000000, 与十字不一致.
            var mark = new CoatingMark
            {
                CoatingType = CoatingType.AR, Size = 10, Center = Vector2.Zero, ShowText = true,
            };
            var svg = new SvgGraphicsDraw();
            mark.Draw(svg);
            var xml = svg.BuildSvg();

            StringAssert.Contains(xml, "#006400", "AR 应以 DarkGreen 渲染");
            Assert.IsFalse(xml.Contains("#000000"),
                "圆/文字落到了环境黑色 — Circle.Draw / Text.Draw 未遵循自身颜色 (出图保真 bug)");
        }
    }
}
