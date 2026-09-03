using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using lcdb.Optic.Import;
using lcdb.Optic;

namespace lcdb.Tests
{
    /// <summary>
    /// 契约层 (P1) 单测: Optic JSON → 中性处方 → OtoCAD 光学实体.
    /// 夹具 = OpticChat venv 跑 CookeTriplet().to_dict() (3 片空气间隔单透镜, SK16/F2/SK16).
    /// </summary>
    [TestClass]
    public class OpticImportTests
    {
        private const double Tol = 1e-3;

        private static string FixturePath(string name)
            => Path.Combine(AppContext.BaseDirectory, "fixtures", "optic", name);

        private static string LoadFixture(string name)
        {
            var path = FixturePath(name);
            Assert.IsTrue(File.Exists(path), $"夹具缺失: {path}");
            return File.ReadAllText(path);
        }

        [TestMethod]
        public void Deserialize_CookeTriplet_ParsesEightSurfaces()
        {
            var data = new OpticJsonDeserializer().Parse(LoadFixture("cooke_triplet.json"));

            Assert.IsTrue(data.IsValid, "解析应成功: " + string.Join("; ", data.Errors));
            Assert.AreEqual(8, data.Surfaces.Count, "CookeTriplet 应有 8 个面 (OBJ+6+IMG)");
            Assert.IsTrue(data.Wavelengths.Count >= 1, "应至少一个波长");

            // 物面/像面平面 (radius null → ∞)
            Assert.IsTrue(double.IsInfinity(data.Surfaces[0].Radius), "OBJ 面应为平面(∞)");
            Assert.IsTrue(double.IsInfinity(data.Surfaces[7].Radius), "IMG 面应为平面(∞)");

            // 玻璃面: 1,3,5 是 SK16/F2/SK16; 2,4,6 之后是空气
            Assert.IsTrue(data.Surfaces[1].HasMaterial, "面1 之后应为玻璃");
            Assert.IsFalse(data.Surfaces[2].HasMaterial, "面2 之后应为空气");
            Assert.AreEqual("SK16", data.Surfaces[1].Glass);
            Assert.AreEqual("F2", data.Surfaces[3].Glass);
        }

        [TestMethod]
        public void Build_CookeTriplet_ProducesThreeSingletLenses()
        {
            var data = new OpticJsonDeserializer().Parse(LoadFixture("cooke_triplet.json"));
            var result = new PrescriptionToLensBuilder().Build(
                data, new PrescriptionToLensBuilder.Options { SourceLabel = "test" });

            Assert.AreEqual(3, result.Entities.Count, "应得 3 个元件");
            Assert.AreEqual(3, result.SingleCount, "全部单透镜");
            Assert.AreEqual(0, result.CementedCount, "无胶合镜");
            Assert.IsTrue(result.Entities.All(e => e is OpticalLens), "全部应为 OpticalLens");

            var lenses = result.Entities.Cast<OpticalLens>().ToList();

            // 材料顺序
            CollectionAssert.AreEqual(
                new[] { "SK16", "F2", "SK16" },
                lenses.Select(l => l.MaterialName).ToArray());

            // 镜1: R1=+22.01359, R2=-435.76044, T=3.25896
            Assert.AreEqual(22.01359, lenses[0].R1, Tol);
            Assert.AreEqual(-435.76044, lenses[0].R2, Tol);
            Assert.AreEqual(3.25896, lenses[0].Thickness, Tol);

            // 镜2: R1=-22.21328, R2=+20.29192, T=0.99997
            Assert.AreEqual(-22.21328, lenses[1].R1, Tol);
            Assert.AreEqual(20.29192, lenses[1].R2, Tol);
            Assert.AreEqual(0.99997, lenses[1].Thickness, Tol);

            // 镜3: R1=+79.6836, R2=-18.39533, T=2.95208
            Assert.AreEqual(79.6836, lenses[2].R1, Tol);
            Assert.AreEqual(-18.39533, lenses[2].R2, Tol);
            Assert.AreEqual(2.95208, lenses[2].Thickness, Tol);

            // 沿光轴 X 递增排布
            Assert.IsTrue(lenses[0].Position.X < lenses[1].Position.X);
            Assert.IsTrue(lenses[1].Position.X < lenses[2].Position.X);

            // 缺通光半径 → 兜底直径 + 告警
            Assert.AreEqual(25.4, lenses[0].Diameter, Tol);
            Assert.IsTrue(result.Warnings.Any(w => w.Contains("通光半径")), "应有口径兜底告警");

            // provenance 写入
            Assert.AreEqual("test", lenses[0].Extensions.Tags["source"]);
        }

        [TestMethod]
        public void Build_CookeFromZemax_ProducesSameThreeSinglets()
        {
            // 夹具 = CookeTriplet 经 Optiland .zmx writer→reader 往返再 to_dict 的 Optic JSON.
            // 验证云端 ".zmx → Optiland → Optic JSON" 转换链产出的格式喂进 builder 与直接路径一致.
            var data = new OpticJsonDeserializer().Parse(LoadFixture("cooke_from_zemax.json"));
            Assert.IsTrue(data.IsValid, string.Join("; ", data.Errors));

            var result = new PrescriptionToLensBuilder().Build(data);
            Assert.AreEqual(3, result.SingleCount, "应得 3 片单透镜");
            Assert.AreEqual(0, result.CementedCount);

            var lenses = result.Entities.Cast<OpticalLens>().ToList();
            CollectionAssert.AreEqual(
                new[] { "SK16", "F2", "SK16" },
                lenses.Select(l => l.MaterialName).ToArray());
            Assert.AreEqual(22.01359, lenses[0].R1, Tol);
            Assert.AreEqual(-18.39533, lenses[2].R2, Tol);
        }

        [TestMethod]
        public void Parse_EmptyJson_ReportsErrorNotThrow()
        {
            var data = new OpticJsonDeserializer().Parse("");
            Assert.IsFalse(data.IsValid);
            Assert.IsTrue(data.Errors.Count > 0);
        }
    }
}
