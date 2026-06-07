using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using lcdb;
using lcdb.Annotation;
using lcdb.IO;
using lcdb.Standards;

namespace lcdb.Tests
{
    /// <summary>
    /// 出图标准系统 A1 — DrawingConvention(出图惯例)+ DrawingConventionService lcdb 地基。
    /// 服务是静态全局态,每测前 ResetCustom 复位。
    /// </summary>
    [TestClass]
    public class DrawingConventionServiceTests
    {
        private string _tmpDir = "";

        [TestInitialize]
        public void Reset()
        {
            DrawingConventionService.ResetCustom();
            // 隔离磁盘: 每测用独立临时目录, 不碰真实 %AppData%
            _tmpDir = Path.Combine(Path.GetTempPath(), "otocad-std-" + Guid.NewGuid().ToString("N"));
            DrawingConventionService.StoreDirOverride = _tmpDir;
        }

        [TestCleanup]
        public void Cleanup()
        {
            DrawingConventionService.StoreDirOverride = null;
            try { if (Directory.Exists(_tmpDir)) Directory.Delete(_tmpDir, true); } catch { }
        }

        [TestMethod]
        public void BuiltIns_present_and_default_active_is_gb_iso()
        {
            var names = DrawingConventionService.BuiltIns.Select(s => s.Name).ToList();
            CollectionAssert.AreEquivalent(
                new[] { "GB-ISO 10110", "MIL-ANSI", "JIS", "DIN" }, names);
            Assert.IsTrue(DrawingConventionService.BuiltIns.All(s => s.IsBuiltIn));
            Assert.AreEqual("GB-ISO 10110", DrawingConventionService.Active.Name, "默认 Active = GB-ISO");
            Assert.AreEqual(SurfaceQualityStandard.ISO_10110_7, DrawingConventionService.Active.SurfaceQuality);
        }

        [TestMethod]
        public void Convention_resolves_its_low_level_drawing_standard()
        {
            // 分层组合: 顶层惯例 → 引用底层样式 profile(线型/颜色/图层)
            var gbIso = DrawingConventionService.Find("GB-ISO 10110")!;
            Assert.AreEqual("GB", gbIso.DrawingStandardName);
            var profile = gbIso.ResolveStandard();
            Assert.IsNotNull(profile, "应解析到底层 DrawingStandard profile");
            Assert.AreEqual("GB", profile!.Name);

            var mil = DrawingConventionService.Find("MIL-ANSI")!;
            Assert.AreEqual("ISO", mil.ResolveStandard()!.Name);
        }

        [TestMethod]
        public void SetActive_by_name_switches_conventions()
        {
            Assert.IsTrue(DrawingConventionService.SetActive("MIL-ANSI"));
            Assert.AreEqual("MIL-ANSI", DrawingConventionService.Active.Name);
            Assert.AreEqual(SurfaceQualityStandard.MIL_PRF_13830, DrawingConventionService.Active.SurfaceQuality);
            Assert.AreEqual(FormAccuracyStandard.PV_RMS_Format, DrawingConventionService.Active.FormAccuracy);
        }

        [TestMethod]
        public void SetActive_unknown_returns_false_keeps_active()
        {
            var before = DrawingConventionService.Active.Name;
            Assert.IsFalse(DrawingConventionService.SetActive("不存在"));
            Assert.AreEqual(before, DrawingConventionService.Active.Name);
        }

        [TestMethod]
        public void Derive_creates_editable_copy_without_touching_base()
        {
            var baseStd = DrawingConventionService.Find("GB-ISO 10110")!;
            var derived = DrawingConventionService.Derive(baseStd);

            Assert.AreEqual("GB-ISO 10110-修改", derived.Name);
            Assert.AreEqual("GB-ISO 10110", derived.BaseName);
            Assert.IsFalse(derived.IsBuiltIn);
            Assert.AreEqual(baseStd.SurfaceQuality, derived.SurfaceQuality);

            // 改派生不影响基准
            derived.SurfaceQuality = SurfaceQualityStandard.MIL_PRF_13830;
            Assert.AreEqual(SurfaceQualityStandard.ISO_10110_7, baseStd.SurfaceQuality);
        }

        [TestMethod]
        public void Save_adds_to_custom_library_and_is_findable()
        {
            var derived = DrawingConventionService.Derive(DrawingConventionService.Find("GB-ISO 10110")!);
            derived.Name = "我的厂标";
            derived.Roughness = RoughnessStandard.JIS;
            DrawingConventionService.Save(derived);

            Assert.IsTrue(DrawingConventionService.CustomLibrary.Any(s => s.Name == "我的厂标"));
            Assert.IsTrue(DrawingConventionService.AllNames.Contains("我的厂标"));
            Assert.IsTrue(DrawingConventionService.SetActive("我的厂标"));
            Assert.AreEqual(RoughnessStandard.JIS, DrawingConventionService.Active.Roughness);
        }

        [TestMethod]
        public void Save_and_Load_round_trip_through_disk()
        {
            var std = DrawingConventionService.Derive(DrawingConventionService.Find("MIL-ANSI")!);
            std.Name = "厂标A";
            std.Roughness = RoughnessStandard.JIS;
            std.Units = DrawingUnits.Inch;
            DrawingConventionService.Save(std);            // 落盘(临时目录,见 TestInitialize)
            Assert.IsTrue(File.Exists(Path.Combine(_tmpDir, "厂标A.json")));

            DrawingConventionService.ResetCustom();         // 清内存
            Assert.IsFalse(DrawingConventionService.AllNames.Contains("厂标A"));

            DrawingConventionService.Load();                // 从盘重载
            var back = DrawingConventionService.Find("厂标A");
            Assert.IsNotNull(back);
            Assert.AreEqual(RoughnessStandard.JIS, back!.Roughness);
            Assert.AreEqual(DrawingUnits.Inch, back.Units);
            Assert.IsFalse(back.IsBuiltIn);
        }

        [TestMethod]
        public void Document_embeds_and_restores_active_convention()
        {
            var std = DrawingConventionService.Derive(DrawingConventionService.Find("GB-ISO 10110")!);
            std.Name = "文档标准X";
            std.SurfaceQuality = SurfaceQualityStandard.MIL_PRF_13830;
            DrawingConventionService.Save(std);
            Assert.IsTrue(DrawingConventionService.SetActive("文档标准X"));

            var path = Path.Combine(_tmpDir, "doc.otocad");
            OtocadFileFormatV4.Save(new Database(), path);   // 内嵌 Active = 文档标准X

            DrawingConventionService.ResetCustom();           // Active→GB-ISO, 自定义清空
            Assert.AreEqual("GB-ISO 10110", DrawingConventionService.Active.Name);

            OtocadFileFormatV4.Load(path);                    // 采纳文档内嵌
            Assert.AreEqual("文档标准X", DrawingConventionService.Active.Name);
            Assert.AreEqual(SurfaceQualityStandard.MIL_PRF_13830, DrawingConventionService.Active.SurfaceQuality);
            Assert.IsTrue(DrawingConventionService.AllNames.Contains("文档标准X"), "自定义随文档进来应入库");
        }

        [TestMethod]
        public void Save_same_name_replaces()
        {
            var a = DrawingConventionService.Derive(DrawingConventionService.Find("GB-ISO 10110")!);
            a.Name = "X"; a.Units = DrawingUnits.Millimeter;
            DrawingConventionService.Save(a);
            var b = DrawingConventionService.Derive(DrawingConventionService.Find("MIL-ANSI")!);
            b.Name = "X"; b.Units = DrawingUnits.Inch;
            DrawingConventionService.Save(b);

            var matches = DrawingConventionService.CustomLibrary.Where(s => s.Name == "X").ToList();
            Assert.AreEqual(1, matches.Count, "同名应替换而非重复");
            Assert.AreEqual(DrawingUnits.Inch, matches[0].Units);
        }
    }
}
