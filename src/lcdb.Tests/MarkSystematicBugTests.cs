using System;
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
    /// 光学标记"模板病"系统性探测器 — 自动扫描 <b>所有</b> IOpticalMark 实现, 钉死
    /// docs/sprint-artifacts/TECH-DEBT-annotation-mark-template-virus.md 诊断的 5 类共享根因。
    ///
    /// 设计目标:不再手测一个修一个。本类用反射发现每一个 Mark 类(新增的 mark 自动纳入),
    /// 对每类 bug 跑一遍, 失败时把"还带这个 bug 的全部 mark"一次性列出。
    ///
    /// 这些测试 <b>现在应为红</b> —— 每个失败信息就是待修清单;统一修复(建议 MarkBase 基类,
    /// 见 tech-debt 文档选项 B)后逐个转绿, 之后永久作为回归闸:谁再复制旧模板, 当场红。
    ///
    /// 探测手段说明:
    ///   T-1/T-3/T-4/T-5 = 行为断言(真实调用 TransformBy/夹点/Generate/bounding, 不靠文本匹配)。
    ///   T-2            = 结构断言(类自身声明了私有 GetMarkColor —— 该方法应被删除, 颜色交 this.color)。
    /// </summary>
    [TestClass]
    public class MarkSystematicBugTests
    {
        // CoatingMark 的按镀膜类型配色是 Epic 10 (WCAG AA) 的有意设计, 不算 T-2 硬覆盖。
        private static readonly HashSet<string> ColorPaletteAllowlist = new() { "CoatingMark" };

        // TechnicalRequirementTable 是技术要求表格(多行表), MarkText 非其显示载体, T-4 不适用。
        private static readonly HashSet<string> MarkTextNotDisplayMedium = new() { "TechnicalRequirementTable" };

        // ---------- 发现 ----------
        // 覆盖 lcdb.Annotation 下全部 Entity 派生标记 (IOpticalMark + ISurfaceAttachable 两族);
        // 各检查在所需属性缺失时自动跳过, 故宁可发现得广。新增 mark 自动纳入。
        private static List<Type> AllMarkTypes() =>
            typeof(AssemblyMark).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract
                         && t.Namespace == "lcdb.Annotation"
                         && typeof(Entity).IsAssignableFrom(t))
                .OrderBy(t => t.Name)
                .ToList();

        // ---------- 通用实例化(取参数最少的构造函数, 用合理缺省填参) ----------
        private static object TryInstantiate(Type t)
        {
            foreach (var ctor in t.GetConstructors().OrderBy(c => c.GetParameters().Length))
            {
                try
                {
                    var args = ctor.GetParameters().Select(p => DefaultArg(p.ParameterType)).ToArray();
                    return ctor.Invoke(args);
                }
                catch { /* 试下一个构造函数 */ }
            }
            return null;
        }

        private static object DefaultArg(Type t)
        {
            if (t == typeof(Vector2)) return new Vector2(0, 0);
            if (t == typeof(double)) return 10.0;
            if (t == typeof(float)) return 10f;
            if (t == typeof(int)) return 1;
            if (t == typeof(bool)) return true;
            if (t == typeof(string)) return "";
            if (t.IsEnum) return Enum.GetValues(t).GetValue(0);
            if (t.IsValueType) return Activator.CreateInstance(t);
            return null;
        }

        // ---------- 反射访问器 ----------
        private static PropertyInfo Prop(object o, string name) =>
            o.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

        private static bool TrySet(object o, string name, object value)
        {
            var p = Prop(o, name);
            if (p == null || !p.CanWrite) return false;
            try { p.SetValue(o, value); return true; } catch { return false; }
        }

        private static T Get<T>(object o, string name) => (T)Prop(o, name).GetValue(o);

        private static FieldInfo MarkEntitiesField(object o) =>
            o.GetType().GetField("_markEntities", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>清缓存后重新生成, 返回生成实体(无 Generate 的数据载体返回空)。</summary>
        private static List<Entity> Regenerate(object mark)
        {
            var fld = MarkEntitiesField(mark);
            if (fld != null) fld.SetValue(mark, new List<Entity>());
            var gen = mark.GetType().GetMethod("Generate", BindingFlags.NonPublic | BindingFlags.Instance)
                   ?? mark.GetType().GetMethod("Generate", BindingFlags.Public | BindingFlags.Instance);
            if (gen != null && gen.GetParameters().Length == 0)
            {
                try { gen.Invoke(mark, null); } catch { return new List<Entity>(); }
            }
            return fld?.GetValue(mark) as List<Entity> ?? new List<Entity>();
        }

        /// <summary>聚合实体的外接框尺寸(用于判断几何是否随 Scale 变化)。</summary>
        private static (double w, double h) GeomExtent(IEnumerable<Entity> ents)
        {
            double minX = double.PositiveInfinity, minY = double.PositiveInfinity;
            double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;
            bool any = false;
            foreach (var e in ents)
            {
                Bounding b;
                try { b = e.bounding; } catch { continue; }
                minX = Math.Min(minX, b.left); maxX = Math.Max(maxX, b.right);
                minY = Math.Min(minY, b.bottom); maxY = Math.Max(maxY, b.top);
                any = true;
            }
            return any ? (maxX - minX, maxY - minY) : (0, 0);
        }

        private static string Report(string bug, string fix, List<string> hits) =>
            $"\n[{bug}] {hits.Count} 个 mark 仍带此 bug:\n  " +
            string.Join("\n  ", hits) +
            $"\n统一修复: {fix}\n参考: docs/sprint-artifacts/TECH-DEBT-annotation-mark-template-virus.md\n";

        // ============================================================
        // 覆盖保障:所有 mark 必须能被通用实例化, 否则探测有盲区(必须可见, 不许静默跳过)。
        // ============================================================
        [TestMethod]
        public void All_marks_are_instantiable_for_scanning()
        {
            var types = AllMarkTypes();
            Assert.IsTrue(types.Count >= 20, $"只发现 {types.Count} 个 IOpticalMark, 预期 ≥20 — 发现逻辑可能失效");
            var dead = types.Where(t => TryInstantiate(t) == null).Select(t => t.Name).ToList();
            Assert.AreEqual(0, dead.Count,
                $"以下 mark 无法用通用构造函数实例化 → 探测盲区, 需在测试加专用构造:\n  {string.Join("\n  ", dead)}");
        }

        // ============================================================
        // T-1: TransformBy Scale 被平移项污染 (纯平移不得改变 Scale)
        // ============================================================
        [TestMethod]
        public void T1_TransformBy_translation_must_not_change_scale()
        {
            // 同时覆盖 Scale 和 Size 两种尺寸字段 (CoatingMark 等用 Size, 曾因只查 Scale 漏检,
            // 用户手测抓出"移动一次镀膜标记就爆大" — 盲区必须显式列举)
            var hits = new List<string>();
            var move = Matrix3.Translate(new Vector2(50, 50));   // 纯平移, 无缩放
            foreach (var t in AllMarkTypes())
            {
                var m = TryInstantiate(t);
                if (m == null) continue;
                foreach (var propName in new[] { "Scale", "Size" })
                {
                    var p = Prop(m, propName);
                    if (p == null || p.PropertyType != typeof(double)) continue;
                    if (!TrySet(m, propName, 2.0)) continue;
                    ((Entity)m).TransformBy(move);
                    double after = Get<double>(m, propName);
                    if (Math.Abs(after - 2.0) > 1e-6)
                        hits.Add($"{t.Name}: 纯平移后 {propName} 2 → {after:F3}");
                }
            }
            Assert.AreEqual(0, hits.Count,
                Report("T-1 TransformBy 尺寸字段被平移污染", "尺寸 *= 变换线性部分 (不用 new Vector2(尺寸,0) 直乘含平移的仿射矩阵)", hits));
        }

        // ============================================================
        // T-2: 私有 GetMarkColor 硬覆盖图层颜色 (结构: 该方法应被删除)
        // ============================================================
        [TestMethod]
        public void T2_no_mark_declares_hardcoded_GetMarkColor()
        {
            var hits = new List<string>();
            foreach (var t in AllMarkTypes())
            {
                if (ColorPaletteAllowlist.Contains(t.Name)) continue;
                var g = t.GetMethod("GetMarkColor",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (g != null) hits.Add($"{t.Name}: 声明了 {g.ReturnType.Name} GetMarkColor()");
            }
            Assert.AreEqual(0, hits.Count,
                Report("T-2 GetMarkColor 硬覆盖图层颜色", "删除 GetMarkColor, 实体颜色用 this.color (ByLayer/用户控制)", hits));
        }

        // ============================================================
        // T-3: SetGripPointAt FrameSize 没除 Scale (角点夹点往返不变)
        // ============================================================
        [TestMethod]
        public void T3_corner_grip_roundtrip_preserves_framesize()
        {
            var hits = new List<string>();
            foreach (var t in AllMarkTypes())
            {
                var m = TryInstantiate(t);
                if (m == null || Prop(m, "FrameSize") == null || Prop(m, "Scale") == null) continue;
                if (!TrySet(m, "Scale", 2.0)) continue;

                var grips = ((Entity)m).GetGripPoints();
                if (grips == null) continue;
                int idx = grips.FindIndex(g => g.type == GripPointType.Corner);
                if (idx < 0) continue;

                var fsBefore = Get<Vector2>(m, "FrameSize");
                // 把角点拖回它自己当前的位置 → FrameSize 不应变化。漏除 Scale 的会放大 Scale 倍。
                ((Entity)m).SetGripPointAt(idx, grips[idx], grips[idx].position);
                var fsAfter = Get<Vector2>(m, "FrameSize");

                if ((fsAfter - fsBefore).length > 1e-6)
                    hits.Add($"{t.Name}: Scale=2 角点原地往返 FrameSize {fsBefore} → {fsAfter}");
            }
            Assert.AreEqual(0, hits.Count,
                Report("T-3 SetGripPointAt 漏除 Scale", "FrameSize = new Vector2(|Δx|*2/Scale, |Δy|*2/Scale)", hits));
        }

        // ============================================================
        // T-4: GenerateText 不渲染 MarkText (用户编辑 MarkText 无效)
        // ============================================================
        [TestMethod]
        public void T4_rendered_text_reflects_MarkText()
        {
            const string Sentinel = "ZZ9182736";
            var hits = new List<string>();
            foreach (var t in AllMarkTypes())
            {
                if (MarkTextNotDisplayMedium.Contains(t.Name)) continue;
                var m = TryInstantiate(t);
                if (m == null || Prop(m, "MarkText") == null) continue;
                TrySet(m, "ShowText", true);
                if (!TrySet(m, "MarkText", Sentinel)) continue;

                var texts = Regenerate(m).OfType<Text>().ToList();
                if (texts.Count == 0) continue;   // 数据载体 / 无文字 — 不适用
                if (!texts.Any(x => x.Value != null && x.Value.Contains(Sentinel)))
                    hits.Add($"{t.Name}: 设 MarkText='{Sentinel}' 后渲染文字为 [{string.Join(" | ", texts.Select(x => x.Value))}]");
            }
            Assert.AreEqual(0, hits.Count,
                Report("T-4 显示不用 MarkText", "text.Value = MarkText (MarkText 作唯一格式源, 删 GetDetailedDescription)", hits));
        }

        // ============================================================
        // T-5: bounding 不随 Scale 变化 (几何缩放了, 选择/捕捉框没跟上)
        // ============================================================
        [TestMethod]
        public void T5_bounding_tracks_scale_when_geometry_does()
        {
            var hits = new List<string>();
            foreach (var t in AllMarkTypes())
            {
                var m = TryInstantiate(t);
                if (m == null || Prop(m, "Scale") == null) continue;

                if (!TrySet(m, "Scale", 1.0)) continue;
                var g1 = GeomExtent(Regenerate(m));
                Bounding b1; try { b1 = ((Entity)m).bounding; } catch { continue; }

                if (!TrySet(m, "Scale", 2.0)) continue;
                var g2 = GeomExtent(Regenerate(m));
                Bounding b2; try { b2 = ((Entity)m).bounding; } catch { continue; }

                bool geomGrew = (g2.w + g2.h) > (g1.w + g1.h) * 1.05;       // 几何确实随 Scale 放大
                bool boundingGrew = (b2.width + b2.height) > (b1.width + b1.height) + 1e-6;
                if (geomGrew && !boundingGrew)
                    hits.Add($"{t.Name}: 几何 {g1.w:F0}×{g1.h:F0}→{g2.w:F0}×{g2.h:F0} 放大, 但 bounding {b1.width:F0}×{b1.height:F0} 不变");
            }
            Assert.AreEqual(0, hits.Count,
                Report("T-5 bounding 忽略 Scale", "bounding 基于 Text.GetBoundingBox + Scale (不用 magic +100/+15)", hits));
        }
    }
}
