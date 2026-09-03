using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LitMath;
using lcdb;
using lcdb.Annotation;
using lcdb.IO;

namespace lcdb.Tests
{
    /// <summary>
    /// 已删除标记类的反序列化迁移 (3 对合并落码, 2026-06-21):
    /// 旧 .otocad 中 SurfaceImperfectionMark / CenterDeviationMark / MaterialDefectMark
    /// 反序列化时应转换为替代标记 —— 不丢实体、位置与可映射参数保留、
    /// 代号串由替代类格式源生成 (非旧散文 MarkText)。
    /// </summary>
    [TestClass]
    public class LegacyMarkMigrationTests
    {
        // 与 OtocadFileFormatV4.BuildOptions 相同的 converter 组合 (全 public, 无需 internals)
        private static JsonSerializerOptions Options()
        {
            var opts = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            };
            opts.Converters.Add(new EntityJsonConverter());
            opts.Converters.Add(new Vector2JsonConverter());
            opts.Converters.Add(new Vector3JsonConverter());
            return opts;
        }

        private static Entity? Deserialize(string json) =>
            JsonSerializer.Deserialize<Entity>(json, Options());

        [TestMethod]
        public void SurfaceImperfection_migrates_to_SurfaceQuality()
        {
            var e = Deserialize("""
                { "kind": "SurfaceImperfectionMark", "Position": [12.5, -3.0], "Scale": 2.0,
                  "AllowedDefectCount": 4, "PointDefectDiameter": 0.1, "LinearDefectWidth": 0.063,
                  "ShowText": true, "TextOffset": [5.0, -20.0], "MarkText": "5/0.1x0.063;0.025" }
                """);
            var m = e as SurfaceQualityMark;
            Assert.IsNotNull(m, "应迁移为 SurfaceQualityMark");
            Assert.IsTrue((m!.Position - new Vector2(12.5, -3.0)).length < 1e-9, "位置保留");
            Assert.AreEqual(2.0, m.Scale, 1e-9, "Scale 保留");
            Assert.AreEqual(4, m.DefectCount, "缺陷数量映射");
            Assert.AreEqual(0.1, m.DefectSize, 1e-9, "缺陷尺寸映射");
            Assert.IsTrue((m.TextOffset - new Vector2(5, -20)).length < 1e-9, "TextOffset 保留");
            StringAssert.StartsWith(m.MarkText, "5/", "代号由替代类格式源生成 (ISO 10110-7)");
        }

        [TestMethod]
        public void CenterDeviation_migrates_to_CenteringTolerance()
        {
            var e = Deserialize("""
                { "kind": "CenterDeviationMark", "Position": [-8.0, 40.0],
                  "DecentrationValue": 0.08, "TiltValue": 0.03, "MarkText": "δ?.05mm" }
                """);
            var m = e as CenteringToleranceMark;
            Assert.IsNotNull(m, "应迁移为 CenteringToleranceMark");
            Assert.IsTrue((m!.Position - new Vector2(-8, 40)).length < 1e-9, "位置保留");
            Assert.AreEqual(0.08, m.DecentrationTolerance, 1e-9, "偏心公差映射");
            Assert.AreEqual(0.03, m.TiltTolerance, 1e-9, "倾斜公差映射");
            StringAssert.StartsWith(m.MarkText, "4/", "代号由替代类格式源生成 (ISO 10110-6), 不带旧散文");
        }

        [TestMethod]
        public void MaterialDefect_bubble_migrates_to_ISO3()
        {
            var e = Deserialize("""
                { "kind": "MaterialDefectMark", "Position": [0.0, 5.0], "DefectType": 0,
                  "BubbleCount": 3, "BubbleGrade": 0.05, "MarkText": "气泡:≤3?Φ0.05mm" }
                """);
            var m = e as ISO10110_3Mark;
            Assert.IsNotNull(m, "Bubble 应迁移为 ISO10110_3Mark");
            Assert.AreEqual(3, m!.BubbleCount, "气泡数量映射");
            Assert.AreEqual(0.05, m.MaxBubbleDiameter, 1e-9, "气泡尺寸映射");
            StringAssert.StartsWith(m.MarkText, "1/", "代号 1/ (ISO 10110-3), 不带旧 '?' 散文");
        }

        [TestMethod]
        public void MaterialDefect_stress_migrates_to_ISO2_and_stria_to_ISO4()
        {
            var stress = Deserialize("""
                { "kind": "MaterialDefectMark", "Position": [1.0, 1.0], "DefectType": 2, "StressBirefringence": 8.0 }
                """);
            Assert.IsInstanceOfType(stress, typeof(ISO10110_2Mark), "应力应迁移为 ISO10110_2Mark");
            Assert.AreEqual(8.0, ((ISO10110_2Mark)stress!).BirefringenceValue, 1e-9, "应力值映射");

            var stria = Deserialize("""
                { "kind": "MaterialDefectMark", "Position": [2.0, 2.0], "DefectType": 1, "StriaGrade": 2.0 }
                """);
            Assert.IsInstanceOfType(stria, typeof(ISO10110_4Mark), "条纹应迁移为 ISO10110_4Mark");
        }

        [TestMethod]
        public void Legacy_classes_are_gone_but_unknown_kinds_still_drop_silently()
        {
            foreach (var name in new[] { "SurfaceImperfectionMark", "CenterDeviationMark", "MaterialDefectMark" })
                Assert.IsNull(EntityTypeRegistry.FindByName(name), $"{name} 类应已删除, 不在注册表");

            Assert.IsNull(Deserialize("""{ "kind": "TotallyUnknownMark", "Position": [0,0] }"""),
                "真正未知的 kind 仍走原有的跳过路径");
        }
    }
}
