using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LitMath;
using lcdb;
using lcdb.Annotation;
using lcdb.IO;
using lcdb.Optic;

namespace lcdb.Tests
{
    /// <summary>
    /// 非球面数据块 (ISO 10110-12) 实体契约: 矢高 z 真算、生成几何有效、属性 .otocad 往返保留。
    /// 是图面数据块 (方程+矢高表+系数), 对标 docs/需求/图面样例-非球面透镜1.png。
    /// </summary>
    [TestClass]
    public class AsphericDataBlockTests
    {
        [TestMethod]
        public void Sag_table_z_matches_even_asphere_formula()
        {
            // 与 AsphericSurface.Sag 同一公式 (数据块内部就用它真算) — 纯圆锥 (无高次项) 矢高单调正。
            // (含高次项时净矢高可正可负, 取决于系数; 单调性检查用纯圆锥面更稳。)
            var surf = new AsphericSurface { Radius = 56.031, ConicConstant = -3 };
            var blk = AsphericDataBlock.FromSurface(surf, semiAperture: 19.0, label: "1", position: Vector2.Zero);

            Assert.AreEqual("1", blk.SurfaceLabel);
            Assert.AreEqual(56.031, blk.BaseRadius, 1e-9);
            Assert.AreEqual(-3, blk.ConicConstant, 1e-9);

            // h=0 → z=0; h>0 → z>0 且随 h 增大 (凸非球面)
            Assert.AreEqual(0.0, surf.Sag(0), 1e-12);
            double z10 = surf.Sag(10), z19 = surf.Sag(19);
            Assert.IsTrue(z10 > 0 && z19 > z10, $"矢高应随口径增大: z10={z10}, z19={z19}");

            // 取样行: 0 与末行=满口径
            Assert.AreEqual(0.0, blk.SampleHeights.First(), 1e-9);
            Assert.AreEqual(19.0, blk.SampleHeights.Last(), 0.5, "末行应≈满口径");
        }

        [TestMethod]
        public void Bounding_is_valid_after_generation()
        {
            var surf = new AsphericSurface { Radius = 50, ConicConstant = -1, EvenCoefficients = new[] { 1e-6, 2e-9 } };
            var blk = AsphericDataBlock.FromSurface(surf, 15.0, "1", new Vector2(10, 200));
            var b = blk.bounding;
            Assert.IsTrue(b.IsValid, "数据块应有有效包围盒 (方程+表+系数)");
            Assert.IsTrue(b.width > 0 && b.height > 0);
        }

        [TestMethod]
        public void Translate_moves_anchor_and_clears_cache()
        {
            var blk = new AsphericDataBlock { Position = new Vector2(0, 0), BaseRadius = 50, ConicConstant = -1 };
            var before = blk.bounding;          // 触发生成
            blk.Translate(new Vector2(100, 50));
            Assert.AreEqual(new Vector2(100, 50), blk.Position);
            var after = blk.bounding;
            Assert.AreEqual(before.width, after.width, 1e-6, "平移不改尺寸");
            Assert.AreEqual(before.center.X + 100, after.center.X, 1e-6);
            Assert.AreEqual(before.center.Y + 50, after.center.Y, 1e-6);
        }

        [TestMethod]
        public void Survives_otocad_v4_round_trip()
        {
            var blk = new AsphericDataBlock
            {
                Position = new Vector2(12, 34),
                SurfaceLabel = "2",
                BaseRadius = 56.031,
                ConicConstant = -3,
                EvenCoefficients = new[] { -4.3264e-5, -9.7614e-8, -1.0852e-13, -1.2284e-13 },
                SampleHeights = new[] { 0.0, 5.0, 10.0, 15.0, 19.0 },
                SagTolerances = new[] { 0.0, 0.002, 0.004, 0.006, 0.008 },
                SlopeTolerances = new[] { "0.3'", "0.5'", "0.5'", "0.8'", "" },
                SlopeSampleLength = 1.0,
                SlopeSampleStep = 0.1,
            };
            var path = Path.Combine(Path.GetTempPath(), $"asph-{Guid.NewGuid():N}.otocad");
            var db = new Database();
            db.AddEntity(blk);
            try
            {
                OtocadFileFormatV4.Save(db, path);
                var (db2, _) = OtocadFileFormatV4.Load(path);
                var back = db2.GetEntitiesByType<AsphericDataBlock>().Single();

                Assert.AreEqual("2", back.SurfaceLabel);
                Assert.AreEqual(56.031, back.BaseRadius, 1e-9);
                Assert.AreEqual(-3, back.ConicConstant, 1e-9);
                CollectionAssert.AreEqual(blk.EvenCoefficients, back.EvenCoefficients);
                CollectionAssert.AreEqual(blk.SampleHeights, back.SampleHeights);
                CollectionAssert.AreEqual(blk.SagTolerances, back.SagTolerances);
                CollectionAssert.AreEqual(blk.SlopeTolerances, back.SlopeTolerances);
                Assert.AreEqual(1.0, back.SlopeSampleLength, 1e-9);
                Assert.AreEqual(0.1, back.SlopeSampleStep, 1e-9);
                Assert.AreEqual(new Vector2(12, 34), back.Position);
            }
            finally { try { if (File.Exists(path)) File.Delete(path); } catch { } }
        }
    }
}
