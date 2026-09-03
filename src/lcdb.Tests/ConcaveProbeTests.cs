using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LitMath;
using lcdb;
using lcdb.Optic;

namespace lcdb.Tests
{
    /// <summary>
    /// 回归: 凹面镜净口径渲染 (用户反馈正透镜没事/凹面有问题).
    /// 净口径半径 > 面 |R| 时球面弧本会钳成半球够不到边线 → 缺口; 修复后绘制半口径钳到 min(halfD, |R|),
    /// 弧端点应正好达到该(钳后)净口径边. 这里断言每个面弧的 Y 跨度 = min(halfD, 各|R|).
    /// </summary>
    [TestClass]
    public class ConcaveProbeTests
    {
        private static double ArcMaxAbsY(Arc a, double cy)
        {
            double y1 = a.center.Y + a.radius * Math.Sin(a.startAngle);
            double y2 = a.center.Y + a.radius * Math.Sin(a.endAngle);
            return Math.Max(Math.Abs(y1 - cy), Math.Abs(y2 - cy));
        }

        private static void AssertArcsReachAperture(double r1, double r2, double dia)
        {
            var lens = new OpticalLens
            {
                Position = Vector2.Zero,
                Diameter = dia,
                Thickness = 3.0,
                FrontSurface = OpticalSurface.Sphere(r1),
                BackSurface = OpticalSurface.Sphere(r2),
            };
            double expected = Math.Min(dia * 0.5, Math.Min(Math.Abs(r1), Math.Abs(r2)));
            var arcs = lens.GetPierceableSubEntities().OfType<Arc>().ToList();
            Assert.IsTrue(arcs.Count >= 2, "应有前后两个球面弧");
            foreach (var a in arcs)
                Assert.AreEqual(expected, ArcMaxAbsY(a, 0), 0.02,
                    $"R1={r1} R2={r2} φ{dia}: 弧(R={a.radius:F1}) 未达净口径边 {expected:F3}");
        }

        [TestMethod]
        public void Mechanical_diameter_draws_flat_land()
        {
            // 净口径 φ20, 机械外径 φ25 → 表面只在 ±10 弯曲, 外侧到 ±12.5 画平肩 (land).
            var lens = new OpticalLens
            {
                Position = Vector2.Zero,
                Diameter = 20,
                MechanicalDiameter = 25,
                Thickness = 3.0,
                FrontSurface = OpticalSurface.Sphere(-30),
                BackSurface = OpticalSurface.Sphere(30),
            };
            const double net = 10, mech = 12.5;
            var subs = lens.GetPierceableSubEntities().ToList();

            foreach (var a in subs.OfType<Arc>())
                Assert.AreEqual(net, ArcMaxAbsY(a, 0), 0.02, "表面弧应只在净口径(±10)内");

            var lines = subs.OfType<Line>().ToList();
            bool od = lines.Any(l => Math.Abs(l.startPoint.Y - l.endPoint.Y) < 1e-6
                                     && Math.Abs(Math.Abs(l.startPoint.Y) - mech) < 0.02);
            Assert.IsTrue(od, "应有机械外径横边线 (±12.5)");

            bool land = lines.Any(l =>
                Math.Abs(l.startPoint.X - l.endPoint.X) < 1e-6
                && Math.Abs(Math.Min(Math.Abs(l.startPoint.Y), Math.Abs(l.endPoint.Y)) - net) < 0.02
                && Math.Abs(Math.Max(Math.Abs(l.startPoint.Y), Math.Abs(l.endPoint.Y)) - mech) < 0.02);
            Assert.IsTrue(land, "应有平肩竖线 (净口径→机械边)");
        }

        [TestMethod]
        public void Per_surface_aperture_each_surface_curves_to_own_net()
        {
            // 前面净口径12, 后面净口径8 → 各弯到自己的净口径; OD=max=12, 后面平肩 8→12.
            var lens = new OpticalLens
            {
                Position = Vector2.Zero,
                Diameter = 24,
                Thickness = 4.0,
                FrontSurface = new SphericalSurface { Radius = 50, SemiAperture = 12 },
                BackSurface = new SphericalSurface { Radius = -50, SemiAperture = 8 },
            };
            var subs = lens.GetPierceableSubEntities().ToList();
            var arcMax = subs.OfType<Arc>().Select(a => Math.Round(ArcMaxAbsY(a, 0), 2)).OrderBy(x => x).ToArray();
            CollectionAssert.AreEqual(new[] { 8.0, 12.0 }, arcMax, "前后表面应各弯到 8 / 12");

            bool backLand = subs.OfType<Line>().Any(l =>
                Math.Abs(l.startPoint.X - l.endPoint.X) < 1e-6
                && Math.Abs(Math.Min(Math.Abs(l.startPoint.Y), Math.Abs(l.endPoint.Y)) - 8) < 0.02
                && Math.Abs(Math.Max(Math.Abs(l.startPoint.Y), Math.Abs(l.endPoint.Y)) - 12) < 0.02);
            Assert.IsTrue(backLand, "后表面应有 8→12 平肩 (净口径小于 OD)");
        }

        [TestMethod]
        public void Glass_hatch_segments_lie_inside_polygon()
        {
            // 20×20 方形剖面, 45° 全剖面线 → 应有若干线段, 且端点都在方形内 (裁剪正确).
            var sq = new System.Collections.Generic.List<Vector2>
            {
                new(-10, 10), new(10, 10), new(10, -10), new(-10, -10),
            };
            var segs = OpticSurfaceGeometry.HatchSegments(sq, 3.0, Math.PI / 4);
            Assert.IsTrue(segs.Count >= 3, "应生成多条剖面线");
            foreach (var (a, b) in segs)
            {
                foreach (var p in new[] { a, b })
                {
                    Assert.IsTrue(p.X >= -10.01 && p.X <= 10.01 && p.Y >= -10.01 && p.Y <= 10.01,
                        $"剖面线端点 ({p.X:F2},{p.Y:F2}) 超出剖面");
                }
            }
        }

        [TestMethod]
        public void Glass_symbol_tiles_small_short_long_short_clusters_with_gaps()
        {
            // GB 光学玻璃符号: 小三线 短长短 簇按间距平铺 (多簇), 簇间留白; 端点都在剖面内.
            var sq = new System.Collections.Generic.List<Vector2>
            {
                new(-10, 10), new(10, 10), new(10, -10), new(-10, -10),
            };
            var symbol = OpticSurfaceGeometry.GlassPatternSegments(sq, GlassPattern.Symbol, 20.0, Math.PI / 4);
            Assert.IsTrue(symbol.Count >= 4, $"应平铺多个短长短小簇, 实得 {symbol.Count} 条线");
            foreach (var (a, b) in symbol)
                foreach (var p in new[] { a, b })
                    Assert.IsTrue(p.X >= -10.05 && p.X <= 10.05 && p.Y >= -10.05 && p.Y <= 10.05,
                        $"玻璃符号端点 ({p.X:F2},{p.Y:F2}) 超出剖面");
            // 线长应分长/短两档 (短 ≈ 0.5×长), 体现 短长短
            static double Len((Vector2 a, Vector2 b) s) =>
                Math.Sqrt((s.b.X - s.a.X) * (s.b.X - s.a.X) + (s.b.Y - s.a.Y) * (s.b.Y - s.a.Y));
            var lens = symbol.Select(s => Math.Round(Len(s), 2)).Distinct().OrderBy(x => x).ToArray();
            Assert.IsTrue(lens.Length >= 2 && lens.First() < lens.Last() * 0.75, "应有长短两档线 (短长短)");
            // 总墨量应明显少于机械件密铺 (簇间留白 → 大部分空白)
            var hatch = OpticSurfaceGeometry.GlassPatternSegments(sq, GlassPattern.Hatch, 20.0, Math.PI / 4);
            double symbolInk = symbol.Sum(Len), hatchInk = hatch.Sum(Len);
            Assert.IsTrue(symbolInk < hatchInk * 0.7, $"Symbol 墨量({symbolInk:F0}) 应明显少于 Hatch({hatchInk:F0}) — 留白");
            // None → 无线段
            Assert.AreEqual(0, OpticSurfaceGeometry.GlassPatternSegments(sq, GlassPattern.None, 20.0, Math.PI / 4).Count);
        }

        [TestMethod]
        public void Mechanical_diameter_clamped_to_net_aperture()
        {
            // 机械外径不得小于净口径: 设入更小值, 读出应被钳到 = 净口径.
            var lens = new OpticalLens { Diameter = 20, MechanicalDiameter = 12 };
            Assert.AreEqual(20.0, lens.MechanicalDiameter!.Value, 1e-9, "机械外径 < 净口径应钳到净口径");
            lens.MechanicalDiameter = 30;
            Assert.AreEqual(30.0, lens.MechanicalDiameter!.Value, 1e-9, "机械外径 > 净口径应原样保留");
            // 默认填充样式 = 三斜线符号
            Assert.AreEqual(GlassPattern.Symbol, new OpticalLens().Pattern);
        }

        [TestMethod]
        public void Surface_arcs_reach_clamped_aperture()
        {
            AssertArcsReachAperture(-50, 50, 25.4);   // 正常凹面 |R|>halfD → 达 12.7
            AssertArcsReachAperture(-10, 10, 25.4);   // 强凹面 |R|<halfD → 钳到 10, 弧达 10 (修复前只到 10 但边线在 12.7)
            AssertArcsReachAperture(50, -50, 25.4);   // 双凸正透镜 → 达 12.7
            AssertArcsReachAperture(-13, 13, 25.4);   // |R| 略大于 halfD → 12.7
        }
    }
}
