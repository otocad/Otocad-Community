using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LitMath;
using lcdb;
using lcdb.Annotation;
using lcdb.Checklist;
using lcdb.DrawingFrame;
using lcdb.Optic;

namespace lcdb.Tests
{
    /// <summary>
    /// 出图清单求值引擎逐动词测试 + 真实默认 YAML 集成测试。
    /// 引擎是无头纯函数: 收实体列表 + 定义 → 逐项判定。
    /// </summary>
    [TestClass]
    public class ChecklistEvaluatorTests
    {
        // ---- helpers ----

        /// <summary>构造一个带任意 Owner 的占位实体 (模拟"关联生成物")。</summary>
        private static Circle Owned(object owner, double cx = 0, double cy = 0, double r = 1)
        {
            var c = new Circle(new Vector2(cx, cy), r);
            c.Owner = owner;
            return c;
        }

        private static ChecklistResult Run(string yaml, params Entity[] entities)
            => ChecklistEvaluator.Evaluate(ChecklistDefinition.Parse(yaml), entities);

        private static CheckStatus StatusOf(ChecklistResult r, string key)
            => r.Items.Single(i => i.Key == key).Status;

        /// <summary>触发 OpticalDrawingFrame 从 GB 字段播种属性区三列 (Columns 懒生成于 Generate)。</summary>
        private static void SeedColumns(OpticalDrawingFrame f)
        {
            var m = typeof(OpticalDrawingFrame).GetMethod("EnsureColumns", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, "未找到 EnsureColumns");
            m!.Invoke(f, null);
        }

        // ========== frame: 顶层声明 (using frame, 档案控制层第 1 期) ==========

        [TestMethod]
        public void Frame_declaration_checks_present_frame_kind()
        {
            const string yaml = "version: 1\nname: t\nframe: gb-optical\nitems: []";
            // 声明 gb-optical: 在场 OpticalDrawingFrame → Pass; ISO 框 → Fail; 无框 → Fail
            Assert.AreEqual(CheckStatus.Pass, StatusOf(Run(yaml, new OpticalDrawingFrame()), "frame-is"));
            Assert.AreEqual(CheckStatus.Fail, StatusOf(Run(yaml, new IsoLensDrawingFrame()), "frame-is"));
            Assert.AreEqual(CheckStatus.Fail, StatusOf(Run(yaml), "frame-is"));
        }

        [TestMethod]
        public void Frame_declaration_unknown_name_fails_and_empty_adds_nothing()
        {
            Assert.AreEqual(CheckStatus.Fail,
                StatusOf(Run("version: 1\nname: t\nframe: GB-my-1\nitems: []", new OpticalDrawingFrame()), "frame-is"));
            // 未声明 frame → 不产生隐式项 (兼容 v1)
            var r = Run("version: 1\nname: t\nitems: []", new OpticalDrawingFrame());
            Assert.IsFalse(r.Items.Any(i => i.Key == "frame-is"));
        }

        // ========== exists / count ==========

        [TestMethod]
        public void Exists_passes_when_present_fails_when_absent()
        {
            const string yaml = "version: 1\nname: t\nitems:\n  - key: f\n    label: 图框\n    check: exists\n    type: DrawingFrame";
            Assert.AreEqual(CheckStatus.Pass, StatusOf(Run(yaml, new OpticalDrawingFrame()), "f"));
            Assert.AreEqual(CheckStatus.Fail, StatusOf(Run(yaml, new Circle(Vector2.Zero, 1)), "f"));
        }

        [TestMethod]
        public void Count_respects_min()
        {
            const string yaml = "version: 1\nname: t\nitems:\n  - key: c\n    label: 圆\n    check: count\n    type: Circle\n    min: 3";
            Assert.AreEqual(CheckStatus.Fail, StatusOf(Run(yaml, new Circle(Vector2.Zero, 1), new Circle(Vector2.Zero, 1)), "c"));
            Assert.AreEqual(CheckStatus.Pass, StatusOf(Run(yaml,
                new Circle(Vector2.Zero, 1), new Circle(Vector2.Zero, 1), new Circle(Vector2.Zero, 1)), "c"));
        }

        [TestMethod]
        public void Exists_matches_derived_via_base_type()
        {
            // DrawingFrame 是基类, OpticalDrawingFrame 派生 → IsInstanceOfType 命中。
            const string yaml = "version: 1\nname: t\nitems:\n  - key: f\n    label: 图框\n    check: exists\n    type: DrawingFrame";
            Assert.AreEqual(CheckStatus.Pass, StatusOf(Run(yaml, new OpticalDrawingFrame()), "f"));
        }

        [TestMethod]
        public void Unknown_type_fails_loudly()
        {
            const string yaml = "version: 1\nname: t\nitems:\n  - key: x\n    label: x\n    check: exists\n    type: NoSuchType";
            var item = Run(yaml, new Circle(Vector2.Zero, 1)).Items.Single();
            Assert.AreEqual(CheckStatus.Fail, item.Status);
            StringAssert.Contains(item.Detail, "未知类型");
        }

        // ========== owned-count ==========

        [TestMethod]
        public void OwnedCount_per_lens_min()
        {
            const string yaml = "version: 1\nname: t\nitems:\n  - key: d\n    label: 标注\n    check: owned-count\n    min: 4";
            var lens = new OpticalLens();
            // 4 个关联生成物 → 通过
            var pass = Run(yaml, lens, Owned(lens), Owned(lens), Owned(lens), Owned(lens));
            Assert.AreEqual(CheckStatus.Pass, StatusOf(pass, "d"));
            // 仅 2 个 → 不足
            var fail = Run(yaml, lens, Owned(lens), Owned(lens));
            Assert.AreEqual(CheckStatus.Fail, StatusOf(fail, "d"));
        }

        [TestMethod]
        public void OwnedCount_not_applicable_without_lens()
        {
            const string yaml = "version: 1\nname: t\nitems:\n  - key: d\n    label: 标注\n    check: owned-count\n    min: 4";
            Assert.AreEqual(CheckStatus.NotApplicable, StatusOf(Run(yaml, new Circle(Vector2.Zero, 1)), "d"));
        }

        // ========== surface-annotation (aspheric) ==========

        [TestMethod]
        public void SurfaceAnnotation_aspheric_requires_mark()
        {
            const string yaml = "version: 1\nname: t\nitems:\n  - key: a\n    label: 非球面标注\n    check: surface-annotation\n    type: ISO10110_12Mark\n    surface: aspheric";

            // 球面透镜 → 无非球面 → N/A
            var spherical = new OpticalLens();
            Assert.AreEqual(CheckStatus.NotApplicable, StatusOf(Run(yaml, spherical), "a"));

            // 一个面非球面, 但无 ISO10110_12Mark → Fail
            var aspheric = new OpticalLens { FrontSurface = new AsphericSurface { Radius = 50, ConicConstant = -1 } };
            Assert.AreEqual(CheckStatus.Fail, StatusOf(Run(yaml, aspheric), "a"));

            // 加上标注 → Pass
            var mark = new ISO10110_12Mark();
            Assert.AreEqual(CheckStatus.Pass, StatusOf(Run(yaml, aspheric, mark), "a"));
        }

        // ========== effective-aperture (有效口径 Øe, 条件性 N/A) ==========

        private const string EaYaml =
            "version: 1\nname: t\nitems:\n  - key: ea\n    label: 有效口径\n    check: effective-aperture\n    prefixes: [\"Øe\"]";

        /// <summary>构造一个含指定面列 Øe 行数的 ISO 图框 (其余列空)。</summary>
        private static IsoLensDrawingFrame IsoZone(int oeRows)
        {
            var cols = new List<IsoSpecColumn>();
            for (int i = 0; i < oeRows; i++)
                cols.Add(new IsoSpecColumn { Title = $"SURFACE {i}", Rows = new List<string> { "R = ∞", $"Øe 62.00" } });
            return new IsoLensDrawingFrame { Columns = cols };
        }

        [TestMethod]
        public void EffectiveAperture_not_applicable_without_lens_or_when_mech_equals_clear()
        {
            // 无光学元件 → N/A
            Assert.AreEqual(CheckStatus.NotApplicable, StatusOf(Run(EaYaml, IsoZone(0)), "ea"));
            // 机械外径未设 (== 通光口径) → N/A (相同时不标, 不恒红)
            var plain = new OpticalLens();
            Assert.AreEqual(CheckStatus.NotApplicable, StatusOf(Run(EaYaml, plain, IsoZone(0)), "ea"));
        }

        [TestMethod]
        public void EffectiveAperture_required_when_mech_exceeds_clear()
        {
            var lens = new OpticalLens { Diameter = 25.4, MechanicalDiameter = 30.0 };  // 2 面 → 需 2 行 Øe

            // 缺 Øe 行 → Fail
            var fail = Run(EaYaml, lens, IsoZone(0));
            Assert.AreEqual(CheckStatus.Fail, StatusOf(fail, "ea"));
            // 两面各一 Øe 行 → Pass
            var pass = Run(EaYaml, lens, IsoZone(2));
            Assert.AreEqual(CheckStatus.Pass, StatusOf(pass, "ea"));
        }

        [TestMethod]
        public void EffectiveAperture_not_applicable_without_zone()
        {
            // 需标 Øe 但无属性区 → N/A (缺图框由 frame 项另报, 此项不重复报红)
            var lens = new OpticalLens { Diameter = 25.4, MechanicalDiameter = 30.0 };
            Assert.AreEqual(CheckStatus.NotApplicable, StatusOf(Run(EaYaml, lens), "ea"));
        }

        // ========== no-overlap ==========

        [TestMethod]
        public void NoOverlap_detects_intersecting_boundings()
        {
            const string yaml = "version: 1\nname: t\nitems:\n  - key: o\n    label: 重叠\n    check: no-overlap\n    among: [Circle]";

            // 两圆相距远 (半径 1, 圆心相距 10) → 不重叠
            var apart = Run(yaml, new Circle(new Vector2(0, 0), 1), new Circle(new Vector2(10, 0), 1));
            Assert.AreEqual(CheckStatus.Pass, StatusOf(apart, "o"));

            // 两圆同心 → 包围盒相交 → 重叠
            var overlap = Run(yaml, new Circle(new Vector2(0, 0), 2), new Circle(new Vector2(0, 0), 2));
            Assert.AreEqual(CheckStatus.Fail, StatusOf(overlap, "o"));
        }

        // ========== inside-frame ==========

        [TestMethod]
        public void InsideFrame_flags_entities_outside_frame()
        {
            const string yaml = "version: 1\nname: t\nitems:\n  - key: in\n    label: 出框\n    check: inside-frame";

            var frame = new OpticalDrawingFrame();
            var fb = frame.bounding;
            Assert.IsTrue(fb.IsValid, "图框需有有效包围盒");

            // 框内一个小圆 (置于图框中心)
            var inside = new Circle(fb.center, Math.Min(fb.width, fb.height) * 0.1);
            Assert.AreEqual(CheckStatus.Pass, StatusOf(Run(yaml, frame, inside), "in"));

            // 远在框外的圆
            var outside = new Circle(new Vector2(fb.right + 1000, fb.top + 1000), 1);
            Assert.AreEqual(CheckStatus.Fail, StatusOf(Run(yaml, frame, outside), "in"));
        }

        [TestMethod]
        public void InsideFrame_not_applicable_without_frame()
        {
            const string yaml = "version: 1\nname: t\nitems:\n  - key: in\n    label: 出框\n    check: inside-frame";
            Assert.AreEqual(CheckStatus.NotApplicable, StatusOf(Run(yaml, new Circle(Vector2.Zero, 1)), "in"));
        }

        // ========== zone-rows / zone-row-prefix ==========

        [TestMethod]
        public void ZoneRows_counts_column_rows()
        {
            const string yaml = "version: 1\nname: t\nitems:\n  - key: z\n    label: 材料行数\n    check: zone-rows\n    column: 1\n    min: 6";
            var frame = new OpticalDrawingFrame();
            SeedColumns(frame);
            Assert.AreEqual(CheckStatus.Pass, StatusOf(Run(yaml, frame), "z"));  // 材料列 6 行
        }

        [TestMethod]
        public void ZoneRowPrefix_finds_real_seeded_codes()
        {
            // 真实播种: 前表面列含面形 "3/3(0.5)" 与疵病 "5/3×0.1"; 材料列含 "n_d ..." "v_d ..."。
            const string frontYaml = "version: 1\nname: t\nitems:\n  - key: codes\n    label: 前表面代号\n    check: zone-row-prefix\n    column: 0\n    prefixes: [\"3/\", \"5/\"]";
            const string matYaml = "version: 1\nname: t\nitems:\n  - key: mat\n    label: 材料\n    check: zone-row-prefix\n    column: 1\n    prefixes: [\"n_d\", \"v_d\"]";

            var frame = new OpticalDrawingFrame();
            SeedColumns(frame);
            Assert.AreEqual(CheckStatus.Pass, StatusOf(Run(frontYaml, frame), "codes"));
            Assert.AreEqual(CheckStatus.Pass, StatusOf(Run(matYaml, frame), "mat"));
        }

        [TestMethod]
        public void ZoneMaterialCodes_finds_seeded_defect_codes_0_1_2()
        {
            // ISO 10110-10: 0/应力 1/气泡 2/不均匀 — OpticalDrawingFrame 材料列默认即以这三码起头。
            const string yaml = "version: 1\nname: t\nitems:\n  - key: mc\n    label: 材料代号\n    check: zone-row-prefix\n    column: 1\n    prefixes: [\"0/\", \"1/\", \"2/\"]";
            var frame = new OpticalDrawingFrame();
            SeedColumns(frame);
            Assert.AreEqual(CheckStatus.Pass, StatusOf(Run(yaml, frame), "mc"));
        }

        [TestMethod]
        public void ZoneRowPrefix_fails_on_missing_code()
        {
            const string yaml = "version: 1\nname: t\nitems:\n  - key: codes\n    label: 前表面代号\n    check: zone-row-prefix\n    column: 0\n    prefixes: [\"9/\"]";
            var frame = new OpticalDrawingFrame();
            SeedColumns(frame);
            var item = Run(yaml, frame).Items.Single();
            Assert.AreEqual(CheckStatus.Fail, item.Status);
            StringAssert.Contains(item.Detail, "9/");
        }

        // ========== 集成: 真实默认 YAML ==========

        private static string LoadShippedDefault()
        {
            var asm = typeof(ChecklistEvaluatorTests).Assembly;
            var name = asm.GetManifestResourceNames().Single(n => n.EndsWith("gb-singlet-default.yaml", StringComparison.Ordinal));
            using var s = asm.GetManifestResourceStream(name)!;
            using var r = new StreamReader(s);
            return r.ReadToEnd();
        }

        [TestMethod]
        public void Shipped_default_yaml_parses_and_every_verb_is_recognized()
        {
            var def = ChecklistDefinition.Parse(LoadShippedDefault());
            Assert.AreEqual("gb-singlet-default", def.Name);
            Assert.IsTrue(def.Items.Count >= 8, "默认清单应有 ≥8 项");

            // 用一个空场景跑一遍: 不该出现"未知动词/未知类型" (即文件与引擎契约一致)。
            var result = ChecklistEvaluator.Evaluate(def, Array.Empty<Entity>());
            foreach (var item in result.Items)
            {
                StringAssert.DoesNotMatch(item.Detail,
                    new System.Text.RegularExpressions.Regex("未知动词|未知类型"),
                    $"项 '{item.Key}' 的动词/类型无法识别: {item.Detail}");
            }
        }

        [TestMethod]
        public void Shipped_default_standard_singlet_scene_has_no_blocking_failures()
        {
            var def = ChecklistDefinition.Parse(LoadShippedDefault());

            // 构造一张"标准单透镜图": 图框 + 球面透镜 + 4 项自动标注 + AR 镀膜标记。
            var frame = new OpticalDrawingFrame();
            SeedColumns(frame);
            var fb = frame.bounding;
            var lens = new OpticalLens { Position = fb.center };

            var entities = new List<Entity> { frame, lens };
            // 自动标注 (Owner=lens), 放在框内中心附近
            for (int i = 0; i < 4; i++) entities.Add(Owned(lens, fb.center.X, fb.center.Y, 0.5));
            entities.Add(new CoatingMark(fb.center, CoatingType.AR, 8.0) { Owner = lens });

            var result = ChecklistEvaluator.Evaluate(def, entities);

            // 球面透镜 → 非球面项 N/A; 关键 Error 项 (图框/自动标注/材料/非球面) 不得阻塞交付。
            Assert.IsTrue(result.CanDeliver,
                "标准单透镜场景不应有 Error 级失败: " +
                string.Join("; ", result.Items.Where(i => i.IsBlocking).Select(i => $"{i.Key}={i.Detail}")));
            Assert.AreEqual(CheckStatus.NotApplicable, StatusOf(result, "aspheric-mark"));
        }

        [TestMethod]
        public void Shipped_default_aspheric_requires_data_block()
        {
            var def = ChecklistDefinition.Parse(LoadShippedDefault());

            // 非球面透镜 (前表面非球) 但无数据块 → aspheric-mark Fail。
            var lens = new OpticalLens { FrontSurface = new AsphericSurface { Radius = 50, ConicConstant = -1 } };
            Assert.AreEqual(CheckStatus.Fail,
                StatusOf(ChecklistEvaluator.Evaluate(def, new Entity[] { lens }), "aspheric-mark"));

            // 加 AsphericDataBlock → Pass。
            var blk = AsphericDataBlock.FromSurface((AsphericSurface)lens.FrontSurface, 12.0, "1", Vector2.Zero);
            Assert.AreEqual(CheckStatus.Pass,
                StatusOf(ChecklistEvaluator.Evaluate(def, new Entity[] { lens, blk }), "aspheric-mark"));
        }

        /// <summary>zone-row-prefix column:-1 列布局无关: 材料在第3列(双胶合)也命中 n_d/v_d。</summary>
        [TestMethod]
        public void ZoneRowPrefix_column_minus1_finds_material_in_any_column()
        {
            var bag = new lcdb.Drawing.DrawingPropertyBag();
            bag.GetOrAddCluster("surface-front", "前表面").Add("rf", "R", "R130 CX");
            bag.GetOrAddCluster("cement", "胶合面").Add("rc", "R", "R-95 CC");
            bag.GetOrAddCluster("surface-back", "后表面").Add("rb", "R", "R-340 CC");
            var mat = bag.GetOrAddCluster("material", "材料技术要求");
            mat.Add("g1_nd", "n_d", "n_d 1.5168");
            mat.Add("g1_vd", "v_d", "v_d 64.17");
            var frame = new GbLensDrawingFrame();
            frame.ApplyBag(bag);   // 材料在第 3 列

            const string anyCol = "version: 1\nname: t\nitems:\n  - key: zm\n    label: 材料\n    check: zone-row-prefix\n    column: -1\n    prefixes: [\"n_d\", \"v_d\"]\n    severity: error";
            Assert.AreEqual(CheckStatus.Pass, StatusOf(Run(anyCol, frame), "zm"), "材料在第3列, column:-1 应命中");

            const string col1 = "version: 1\nname: t\nitems:\n  - key: zm\n    label: 材料\n    check: zone-row-prefix\n    column: 1\n    prefixes: [\"n_d\", \"v_d\"]\n    severity: error";
            Assert.AreEqual(CheckStatus.Fail, StatusOf(Run(col1, frame), "zm"), "硬编 col1(胶合面)应失败 — 证明列无关修复必要");
        }

        /// <summary>P4: property 动词按键查图框属性包 (IPropertyExport) — 在场且非空=Pass, 缺失=Fail。</summary>
        [TestMethod]
        public void Property_verb_checks_bag_key()
        {
            var bag = new lcdb.Drawing.DrawingPropertyBag();
            bag.GetOrAddCluster("material", "材料").Add("material_name", "牌号", "BK7");
            var frame = new GbLensDrawingFrame();
            frame.ApplyBag(bag);

            const string yamlPass = "version: 1\nname: t\nitems:\n  - key: mat\n    label: 材料牌号\n    check: property\n    prop: material_name\n    severity: error";
            Assert.AreEqual(CheckStatus.Pass, StatusOf(Run(yamlPass, frame), "mat"), "material_name 在场应 Pass");

            const string yamlFail = "version: 1\nname: t\nitems:\n  - key: pn\n    label: 图号\n    check: property\n    prop: product_number\n    severity: error";
            Assert.AreEqual(CheckStatus.Fail, StatusOf(Run(yamlFail, frame), "pn"), "缺失键应 Fail");
        }
    }
}
