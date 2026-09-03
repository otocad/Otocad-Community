using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using lcdb.Optic.Import;
using lcdb.Optic;

namespace lcdb.Tests
{
    /// <summary>
    /// P2: 本地 .zmx → 中性处方 (ZmxParser) → 同一 PrescriptionToLensBuilder → 实体.
    /// 夹具是消色差双胶合 (N-BK7 + F2), 验证胶合组识别 (CookeTriplet 未覆盖).
    /// </summary>
    [TestClass]
    public class ZmxImportTests
    {
        private const double Tol = 1e-2;

        private static string LoadZmxPath(string name)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "fixtures", "zmx", name);
            Assert.IsTrue(File.Exists(path), $"夹具缺失: {path}");
            return path;
        }

        [TestMethod]
        public void Build_AchromatDoublet_ProducesOneCementedLens()
        {
            var data = new ZmxParser().Parse(LoadZmxPath("achromat_doublet.zmx"));
            Assert.IsTrue(data.IsValid, "ZmxParser 应成功: " + string.Join("; ", data.Errors));

            var result = new PrescriptionToLensBuilder().Build(
                data, new PrescriptionToLensBuilder.Options { SourceLabel = "achromat_doublet.zmx" });

            Assert.AreEqual(1, result.Entities.Count, "应得 1 个元件");
            Assert.AreEqual(1, result.CementedCount, "应识别为胶合镜");
            Assert.AreEqual(0, result.SingleCount, "无单透镜");

            var d = result.Entities[0] as CementedLens;
            Assert.IsNotNull(d, "应为 CementedLens");

            // 几何: R1=+60, RContact=-40, R3=-120, T1=4, T2=2.5
            Assert.AreEqual(60.0, d.R1, Tol);
            Assert.AreEqual(-40.0, d.RContact, Tol);
            Assert.AreEqual(-120.0, d.R3, Tol);
            Assert.AreEqual(4.0, d.T1, Tol);
            Assert.AreEqual(2.5, d.T2, Tol);

            // 净口径取 DIAM (Semi-Diameter 10 → φ20); 机械外径取 MEMA (12.5 → φ25). 真实 Zemax 文件惯例.
            Assert.AreEqual(20.0, d.Diameter, Tol);
            // MEMA > DIAM → 有平肩 (land)
            Assert.IsTrue(d.MechanicalDiameter.HasValue, "机械外径应设(MEMA>DIAM)");
            Assert.AreEqual(25.0, d.MechanicalDiameter.Value, Tol);

            // 材料: 片1 N-BK7, 片2 F2
            Assert.AreEqual("N-BK7", d.Material1);
            Assert.AreEqual("F2", d.Material2);

            // provenance
            Assert.AreEqual("achromat_doublet.zmx", d.Extensions.Tags["source"]);
        }
    }
}
