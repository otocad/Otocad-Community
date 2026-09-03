using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LitMath;
using lcdb;
using lcdb.Annotation;

namespace lcdb.Tests
{
    /// <summary>
    /// 光学标记几何回归测试 — 钉死 2026-06-07 审计修复的四个缺陷:
    ///   1. PolishingMark 等级线超出三角形(固定半宽 vs 三角形渐窄)。
    ///   2. OpticalAxisMark 旋转两次(Rotate 既转 EndPosition 又累加 Rotation, 再被 GetCalculatedEndPosition 应用)。
    ///   3. CenteringToleranceMark 框内代号画成部件号 "6"(应为 ISO 10110-6 指示代号 "4/")。
    ///   4. ISO10110_4/12/14Mark SetProperties 改属性后不清缓存 → Draw 渲旧帧。
    /// </summary>
    [TestClass]
    public class MarkGeometryRegressionTests
    {
        // ---- 反射 helpers (Generate / _markEntities 均为非公开) ----
        private static List<Entity> Generate(object mark)
        {
            var t = mark.GetType();
            var gen = t.GetMethod("Generate", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(gen, $"{t.Name}: 未找到 Generate()");
            gen!.Invoke(mark, null);
            return MarkEntities(mark);
        }

        private static List<Entity> MarkEntities(object mark)
        {
            var fld = mark.GetType().GetField("_markEntities", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fld, $"{mark.GetType().Name}: 未找到 _markEntities");
            return (List<Entity>)fld!.GetValue(mark)!;
        }

        // ========== 1. PolishingMark: 等级线必须落在三角形内 ==========
        [TestMethod]
        public void Polishing_grade_lines_stay_inside_triangle()
        {
            const double S = 10.0;
            var m = new PolishingMark(Vector2.Zero, S) { Grade = PolishingGrade.UltraFine };
            var hatch = Generate(m).OfType<Line>()
                .Where(l => Math.Abs(l.startPoint.Y - l.endPoint.Y) < 1e-9)   // 水平线 = 等级线
                .ToList();
            Assert.AreEqual(3, hatch.Count, "超精抛光应有 3 条等级线");

            double apexY = -S * 2.0 / 3.0;   // 顶点在下
            foreach (var l in hatch)
            {
                double y = l.startPoint.Y;
                double w = Math.Max(Math.Abs(l.startPoint.X), Math.Abs(l.endPoint.X));
                double triHalf = 0.5 * (y - apexY);   // 三角形在该高度的半宽
                Assert.IsTrue(w <= triHalf + 1e-9,
                    $"等级线在 y={y:F3} 半宽 {w:F3} 超出三角形半宽 {triHalf:F3}");
            }
        }

        // ========== 2. OpticalAxisMark: 旋转只应用一次 ==========
        [TestMethod]
        public void OpticalAxis_rotate_is_applied_exactly_once()
        {
            var m = new OpticalAxisMark();   // Position=(0,0), EndPosition=(100,0)
            var expected = Vector2.RotateInRadian(new Vector2(100, 0), Vector2.Zero, Math.PI / 2);
            m.Rotate(Vector2.Zero, Math.PI / 2);

            // 取最长的线 = 光轴线; 远离起点的端点应为单次旋转后的终点
            var axis = Generate(m).OfType<Line>().OrderByDescending(l => (l.endPoint - l.startPoint).length).First();
            var far = (axis.startPoint - Vector2.Zero).length > (axis.endPoint - Vector2.Zero).length
                ? axis.startPoint : axis.endPoint;
            Assert.IsTrue((far - expected).length < 1e-6,
                $"光轴终点 {far} 应为单次旋转结果 {expected}(双重旋转 bug 会偏离)");
        }

        // ========== 3. CenteringToleranceMark: 只剩 ISO 代号文字, 无方框/十字/重复公差 ==========
        [TestMethod]
        public void CenteringTolerance_renders_minimal_code_text_only()
        {
            var ents = Generate(new CenteringToleranceMark(Vector2.Zero, 0.05, 0.02));
            Assert.AreEqual(1, ents.Count, "中心偏差应只有 1 个指示文字实体(无方框/十字/重复 e≤/t≤)");
            Assert.AreEqual(0, ents.OfType<Line>().Count(), "不应有方框/十字线");
            Assert.AreEqual(0, ents.OfType<Circle>().Count(), "不应有中心圆");
            var v = ((Text)ents[0]).Value;
            Assert.AreNotEqual("6", v, "不应把 ISO 部件号 '6' 当代号");
            Assert.IsTrue(v.StartsWith("4/"), $"指示文字应以 ISO 10110-6 代号 '4/' 开头, 实际 '{v}'");
        }

        // ========== 4. ISO10110_4/12/14: SetProperties 后清缓存 ==========
        [TestMethod]
        public void Iso10110_SetProperties_clears_render_cache()
        {
            foreach (object mark in new object[] { new ISO10110_4Mark(), new ISO10110_12Mark(), new ISO10110_14Mark() })
            {
                Generate(mark);   // 填充缓存
                Assert.IsTrue(MarkEntities(mark).Count > 0, $"{mark.GetType().Name}: Generate 应先填充缓存");

                var sp = mark.GetType().GetMethod("SetProperties", BindingFlags.Public | BindingFlags.Instance);
                Assert.IsNotNull(sp, $"{mark.GetType().Name}: 未找到 SetProperties");
                sp!.Invoke(mark, new object[] { new Dictionary<string, object> { { "Scale", 2.0 } } });

                Assert.AreEqual(0, MarkEntities(mark).Count,
                    $"{mark.GetType().Name}: SetProperties 后应清空缓存, 否则 Draw 渲旧帧");
            }
        }

        // ========== 5. 框型标记: 文字随框旋转 ==========
        [TestMethod]
        public void FrameMarks_text_rotates_with_frame()
        {
            foreach (object mark in new object[]
            {
                new AssemblyMark   { Position = Vector2.Zero, Rotation = 90 },
                new InspectionMark { Position = Vector2.Zero, Rotation = 90 },
            })
            {
                var texts = Generate(mark).OfType<Text>().ToList();
                Assert.IsTrue(texts.Count > 0, $"{mark.GetType().Name}: 应有文字");
                // 原始锚点都在 x=Position.X(=0) 轴上; 旋转 90° 后应全部离开 x=0
                foreach (var t in texts)
                    Assert.IsTrue(Math.Abs(t.Position.X) > 1e-6,
                        $"{mark.GetType().Name}: 文字 '{t.Value}' 未随 90° 旋转(仍在 x=0)");
            }
        }

        // ========== 6. EffectiveApertureMark: 已降为数据载体(归属性区 Φe 行), 不自绘 ==========
        [TestMethod]
        public void EffectiveAperture_is_data_carrier_no_geometry()
        {
            var m = new EffectiveApertureMark(Vector2.Zero, 18.0);
            var svg = new lcdb.Rendering.SvgGraphicsDraw();
            m.Draw(svg);
            Assert.IsFalse(svg.BuildSvg(pixelSize: 100).Contains("<line"),
                "有效孔径已归属性区 Φe 行, 数据载体不应自绘任何线");
        }

        // ========== 7. FocalPointMark: 文字栈随 Rotation 旋转 ==========
        [TestMethod]
        public void FocalPoint_text_stack_rotates()
        {
            var m = new FocalPointMark { Position = Vector2.Zero, Rotation = 90, ShowBeam = false };
            var texts = Generate(m).OfType<Text>().ToList();
            Assert.IsTrue(texts.Count >= 2, "焦点标记应有多行文字");
            // 旋转前整列共享 X(=TextOffset.X); 旋转 90° 后各行 X 应散开
            double xspread = texts.Max(t => t.Position.X) - texts.Min(t => t.Position.X);
            Assert.IsTrue(xspread > 1e-6, "旋转后文字栈各行 X 应散开(旧码全相同, Rotation 空操作)");
        }

        // ========== 8. 工厂惯例符号(— 类, 无专用国标): 结构一致性防回归 ==========
        // 只验内部一致性(同心/闭合/位置关系), 不发明标准要求(否则又成作弊测试)。

        [TestMethod]
        public void Grinding_is_two_concentric_circles()
        {
            const double S = 10.0;
            var circles = Generate(new GrindingMark(Vector2.Zero, S)).OfType<Circle>()
                .OrderByDescending(c => c.radius).ToList();
            Assert.AreEqual(2, circles.Count, "研磨应为内外两圆(砂轮简化图)");
            Assert.IsTrue((circles[0].center - circles[1].center).length < 1e-9, "两圆应同心");
            Assert.AreEqual(S * 0.5, circles[0].radius, 1e-9, "外圆半径 = Size/2");
            Assert.IsTrue(circles[1].radius < circles[0].radius, "内圆应小于外圆");
        }

        [TestMethod]
        public void DiamondTurning_rhombus_closes()
        {
            const double S = 10.0;
            var lines = Generate(new DiamondTurningMark(Vector2.Zero, S)).OfType<Line>().ToList();
            Assert.AreEqual(4, lines.Count, "菱形应为 4 条边");
            // 闭合: 每个顶点恰被 2 条边共享
            var pts = lines.SelectMany(l => new[] { l.startPoint, l.endPoint }).ToList();
            foreach (var p in pts)
                Assert.AreEqual(2, pts.Count(q => (q - p).length < 1e-9), $"顶点 {p} 应被两条边共享(闭合)");
            // 上下顶点 ±Size/2, 左右顶点 ±Size/3(关于 Center 对称)
            Assert.AreEqual(S * 0.5, pts.Max(p => p.Y), 1e-9, "上顶点 Y = +Size/2");
            Assert.AreEqual(-S * 0.5, pts.Min(p => p.Y), 1e-9, "下顶点 Y = -Size/2");
            Assert.AreEqual(S / 3.0, pts.Max(p => p.X), 1e-9, "右顶点 X = +Size/3");
        }

        // ========== 9. 工艺备注框(检验/加工/装配, 工厂惯例矩形框+文字) ==========

        [TestMethod]
        public void ProcessFlag_frames_close_with_labels_inside()
        {
            foreach (object mark in new object[]
            {
                new ProcessingMark { Position = Vector2.Zero },
                new InspectionMark { Position = Vector2.Zero },
                new AssemblyMark   { Position = Vector2.Zero },
            })
            {
                var ents = Generate(mark);
                var lines = ents.OfType<Line>().ToList();
                Assert.AreEqual(4, lines.Count, $"{mark.GetType().Name}: 矩形框应为 4 条边");
                // 闭合: 每个顶点恰被 2 条边共享
                var pts = lines.SelectMany(l => new[] { l.startPoint, l.endPoint }).ToList();
                foreach (var p in pts)
                    Assert.AreEqual(2, pts.Count(q => (q - p).length < 1e-9),
                        $"{mark.GetType().Name}: 顶点 {p} 应被两条边共享(闭合)");
                // 框内 2 行标签(锚点在框矩形内)
                double maxX = pts.Max(p => p.X), minX = pts.Min(p => p.X);
                double maxY = pts.Max(p => p.Y), minY = pts.Min(p => p.Y);
                var inner = ents.OfType<Text>().Where(t =>
                    t.Position.X >= minX && t.Position.X <= maxX &&
                    t.Position.Y >= minY && t.Position.Y <= maxY).ToList();
                Assert.IsTrue(inner.Count >= 2, $"{mark.GetType().Name}: 框内应有 ≥2 行标签");
            }
        }

        [TestMethod]
        public void ProcessFlag_bounding_is_not_symmetrically_inflated()
        {
            // 旧 bug: bounding 把 |TextOffset|+80 对称加在两侧 → 选择框偏心且虚大。
            // 修复后: 框 ∪ 文字范围。默认文字在下方(TextOffset=(0,-30)) → 顶边 = 框顶, 不再虚高。
            var m = new ProcessingMark { Position = Vector2.Zero };
            var b = m.bounding;
            Assert.AreEqual(m.FrameSize.Y * 0.5, b.top, 1e-9, "顶边应为框顶(文字在下方, 不应对称外扩)");
            Assert.IsTrue(b.bottom < -m.FrameSize.Y * 0.5, "底边应被外部说明文字向下扩展");
        }

        [TestMethod]
        public void Sandblasting_trapezoid_closes_with_surface_line_below()
        {
            const double S = 10.0;
            var lines = Generate(new SandblastingMark(Vector2.Zero, S) { ShowText = false })
                .OfType<Line>().ToList();
            Assert.AreEqual(5, lines.Count, "喷砂 = 梯形喷嘴 4 边 + 表面线");
            // 表面线 = 最低的那条水平线, 应在梯形(底边 y=0)下方
            var surface = lines.Where(l => Math.Abs(l.startPoint.Y - l.endPoint.Y) < 1e-9)
                               .OrderBy(l => l.startPoint.Y).First();
            Assert.IsTrue(surface.startPoint.Y < 0, "表面线应在喷嘴下方(向下喷射)");
            // 梯形 4 边闭合: 去掉表面线后每顶点恰被 2 条边共享
            var trap = lines.Where(l => !ReferenceEquals(l, surface)).ToList();
            var pts = trap.SelectMany(l => new[] { l.startPoint, l.endPoint }).ToList();
            foreach (var p in pts)
                Assert.AreEqual(2, pts.Count(q => (q - p).length < 1e-9), $"梯形顶点 {p} 应被两条边共享(闭合)");
        }
    }
}
