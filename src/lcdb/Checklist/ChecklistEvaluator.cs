using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using lcdb.DrawingFrame;
using lcdb.Optic;

namespace lcdb.Checklist;

/// <summary>
/// 出图清单求值引擎 — 给一份 <see cref="ChecklistDefinition"/> 和一张图纸的实体列表,
/// 逐项判定并产出 <see cref="ChecklistResult"/>。
///
/// 无头、纯函数: 只收实体列表与定义, 不碰文件/UI/命令系统 (分层下层)。
/// 同一引擎服务三处: 打磨期回归断言、清单面板、未来用户侧 lint / CI。
///
/// 每个动词 (check) 是一个小方法, 哑执行 YAML 声明的参数;
/// 标准知识 (代号前缀、要素阈值) 在 YAML, 引擎不内嵌国标语义。
/// </summary>
public static class ChecklistEvaluator
{
    public static ChecklistResult Evaluate(ChecklistDefinition def, IReadOnlyList<Entity> entities)
    {
        if (def is null) throw new ArgumentNullException(nameof(def));
        entities ??= Array.Empty<Entity>();

        var results = new List<ChecklistItemResult>(def.Items.Count + 1);

        // 顶层 frame: 声明 ("using frame GB-my-1") → 隐式 error 项: 在场图框必须是声明的那个。
        // 这是 draw-list 作为控制层的第一块: 声明一次, 校验消费 (生成端将来读同一声明)。
        if (!string.IsNullOrWhiteSpace(def.Frame))
        {
            var (st, detail) = CheckFrameIs(entities, def.Frame.Trim());
            results.Add(new ChecklistItemResult
            {
                Key = "frame-is",
                Label = $"图框 = {def.Frame.Trim()}",
                Status = st,
                Severity = CheckSeverity.Error,
                Detail = detail,
                Regen = "",
            });
        }

        foreach (var item in def.Items)
            results.Add(EvaluateItem(item, entities));

        return new ChecklistResult { Name = def.Name, Items = results };
    }

    /// <summary>
    /// 内置 (硬编码) 图框注册表: 档案里的 frame 名 → 图框类型。
    /// 数据驱动图框 (<see cref="DrawingFrame.DataDrivenFrame"/>) 不在此表: 其名是内嵌定义的
    /// <c>Definition.Name</c>, 随文档走, frame-is 直接比对 (不依赖本机预设文件)。
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Type> FrameRegistry =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["gb-optical"] = typeof(DrawingFrame.OpticalDrawingFrame),
            ["iso-lens"]   = typeof(DrawingFrame.IsoLensDrawingFrame),
        };

    /// <summary>
    /// frame-is: 在场图框 (DrawingFrame) 必须与声明名一致 —
    /// 内置名按类型比对; 其它名按数据驱动图框内嵌定义名 (大小写不敏感) 比对。
    /// </summary>
    private static (CheckStatus, string) CheckFrameIs(IReadOnlyList<Entity> entities, string frameName)
    {
        var frame = entities.FirstOrDefault(e => e is DrawingFrame.DrawingFrame);
        bool builtin = FrameRegistry.TryGetValue(frameName, out var expected);
        if (frame is null)
            return (CheckStatus.Fail, $"无图框 (声明需要 {frameName})");

        if (builtin)
            return expected!.IsInstanceOfType(frame)
                ? (CheckStatus.Pass, frameName)
                : (CheckStatus.Fail, $"在场图框是 {frame.GetType().Name}, 声明需要 {frameName} ({expected.Name})");

        if (frame is DrawingFrame.DataDrivenFrame ddf)
            return string.Equals(ddf.Definition.Name, frameName, StringComparison.OrdinalIgnoreCase)
                ? (CheckStatus.Pass, frameName)
                : (CheckStatus.Fail, $"在场图框定义是 '{ddf.Definition.Name}', 声明需要 '{frameName}'");

        return (CheckStatus.Fail,
            $"未知图框 '{frameName}' (内置: {string.Join(" ", FrameRegistry.Keys)}; 或数据驱动图框的定义名, 在场图框是 {frame.GetType().Name})");
    }

    /// <summary>
    /// property: 图框属性包(单一真值源 IPropertyExport)中某键存在且非空。
    /// prop 可给多个候选键 (| 或 , 分隔, 任一命中即过) — 兼容不同图框的键词汇 (如 material_name|Material)。
    /// </summary>
    private static (CheckStatus, string) CheckProperty(IReadOnlyList<Entity> entities, string keys)
    {
        if (string.IsNullOrWhiteSpace(keys)) return (CheckStatus.Fail, "规则未指定 prop 键");
        var src = entities.OfType<DrawingFrame.IPropertyExport>().FirstOrDefault();
        if (src is null) return (CheckStatus.Fail, "无属性图框");
        var flat = src.ExportFlat();
        foreach (var raw in keys.Split('|', ','))
        {
            var k = raw.Trim();
            if (k.Length > 0 && flat.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                return (CheckStatus.Pass, $"{k} = {v}");
        }
        return (CheckStatus.Fail, $"属性 [{keys}] 缺失/为空");
    }

    private static ChecklistItemResult EvaluateItem(ChecklistItemDef item, IReadOnlyList<Entity> entities)
    {
        (CheckStatus status, string detail) = item.Check?.Trim().ToLowerInvariant() switch
        {
            "exists"            => CheckCount(entities, item.Type, min: 1),
            "count"             => CheckCount(entities, item.Type, item.Min),
            "owned-count"       => CheckOwnedCount(entities, item.Min),
            "surface-annotation"=> CheckSurfaceAnnotation(entities, item.Type, item.Surface),
            "effective-aperture"=> CheckEffectiveAperture(entities, item.Prefixes),
            "no-overlap"        => CheckNoOverlap(entities, item.Among),
            "inside-frame"      => CheckInsideFrame(entities),
            "zone-rows"         => CheckZoneRows(entities, item.Column, item.Min),
            "zone-row-prefix"   => CheckZoneRowPrefix(entities, item.Column, item.Prefixes),
            "property"          => CheckProperty(entities, item.Prop),
            _                   => (CheckStatus.Fail, $"未知动词 '{item.Check}'"),
        };

        return new ChecklistItemResult
        {
            Key = item.Key,
            Label = item.Label,
            Status = status,
            Severity = item.SeverityEnum,
            Detail = detail,
            Regen = item.Regen ?? "",
        };
    }

    // ---------------- 动词 ----------------

    /// <summary>exists / count: 场景中匹配 type 的实体数 ≥ min。</summary>
    private static (CheckStatus, string) CheckCount(IReadOnlyList<Entity> entities, string typeName, int min)
    {
        var t = ResolveType(typeName);
        if (t is null) return (CheckStatus.Fail, $"未知类型 '{typeName}'");
        int n = entities.Count(e => t.IsInstanceOfType(e));
        return n >= min
            ? (CheckStatus.Pass, $"{n} 个")
            : (CheckStatus.Fail, $"需 {min} 个, 实有 {n}");
    }

    /// <summary>owned-count: 每个光学元件 (有光学面的实体) 的关联生成物 (Owner==它) ≥ min。无元件 → N/A。</summary>
    private static (CheckStatus, string) CheckOwnedCount(IReadOnlyList<Entity> entities, int min)
    {
        var lenses = entities.Where(IsOpticalElement).ToList();
        if (lenses.Count == 0) return (CheckStatus.NotApplicable, "无光学元件");

        var deficient = new List<int>();
        foreach (var lens in lenses)
        {
            int owned = entities.Count(e => ReferenceEquals(e.Owner, lens));
            if (owned < min) deficient.Add(owned);
        }
        return deficient.Count == 0
            ? (CheckStatus.Pass, $"{lenses.Count} 个元件均 ≥ {min} 标注")
            : (CheckStatus.Fail, $"{deficient.Count}/{lenses.Count} 个元件标注不足 (需 {min})");
    }

    /// <summary>
    /// surface-annotation: 跨所有元件统计匹配 surface 过滤 (aspheric/spherical) 的面数 Na;
    /// 场景中匹配 type 的标注数须 ≥ Na。Na=0 → N/A。
    /// (诚实的 count-based: 无法把标注绑定到具体面, 故按"够不够数"判, 见设计文档未决项①)
    /// </summary>
    private static (CheckStatus, string) CheckSurfaceAnnotation(IReadOnlyList<Entity> entities, string typeName, string surfaceFilter)
    {
        var t = ResolveType(typeName);
        if (t is null) return (CheckStatus.Fail, $"未知类型 '{typeName}'");

        int na = entities.Where(IsOpticalElement)
                         .SelectMany(GetSurfaces)
                         .Count(s => MatchesSurfaceFilter(s, surfaceFilter));
        if (na == 0) return (CheckStatus.NotApplicable, $"无{SurfaceFilterLabel(surfaceFilter)}面");

        int marks = entities.Count(e => t.IsInstanceOfType(e));
        return marks >= na
            ? (CheckStatus.Pass, $"{SurfaceFilterLabel(surfaceFilter)}面 {na} 个, 标注 {marks} 个")
            : (CheckStatus.Fail, $"{SurfaceFilterLabel(surfaceFilter)}面 {na} 个, 标注 {marks} 个 (缺 {na - marks})");
    }

    /// <summary>
    /// effective-aperture: 有效口径 Øe 标注。用户规则 "Øe == 机械口径时可不标" (设计文档未决项 #2)。
    /// 仅当存在 "机械外径 &gt; 通光口径" 的元件时才要求标 Øe → 否则 N/A (不恒红)。
    /// 诚实 count-based (无法把行绑定到具体面, 同 surface-annotation): 需标 Øe 的面数 = 这些元件的面数;
    /// 属性区里以 Øe 前缀开头的行数须 ≥ 之。prefixes 默认 ["Øe"], 可由 YAML 覆盖。
    /// </summary>
    private static (CheckStatus, string) CheckEffectiveAperture(IReadOnlyList<Entity> entities, List<string> prefixes)
    {
        var lenses = entities.Where(IsOpticalElement).ToList();
        if (lenses.Count == 0) return (CheckStatus.NotApplicable, "无光学元件");

        int need = lenses.Where(RequiresEffectiveAperture).SelectMany(GetSurfaces).Count();
        if (need == 0) return (CheckStatus.NotApplicable, "无机械外径>通光口径的元件 (Øe 可不标)");

        var zone = entities.OfType<IPropertyZoneFrame>().FirstOrDefault();
        if (zone is null) return (CheckStatus.NotApplicable, "无属性区 (图框项另报)");

        var ps = (prefixes is { Count: > 0 }) ? prefixes : new List<string> { "Øe" };
        int found = CountZoneRowsWithAnyPrefix(zone, ps);
        return found >= need
            ? (CheckStatus.Pass, $"需标 Øe 面 {need} 个, 属性区 Øe 行 {found} 行")
            : (CheckStatus.Fail, $"需标 Øe 面 {need} 个, 属性区 Øe 行 {found} 行 (缺 {need - found})");
    }

    /// <summary>该元件是否需标有效口径: 机械外径明确 &gt; 通光口径 (反射取 MechanicalDiameter / Diameter, 适配 OpticalLens / CementedLens)。</summary>
    private static bool RequiresEffectiveAperture(Entity e)
    {
        var t = e.GetType();
        var mech = t.GetProperty("MechanicalDiameter")?.GetValue(e);
        var dia = t.GetProperty("Diameter")?.GetValue(e);
        return mech is double m && dia is double d && m > d + 1e-6;
    }

    /// <summary>统计属性区所有列里以任一 prefix 开头的行数 (列数未知, 扫 0..7; 越界列返回空表)。</summary>
    private static int CountZoneRowsWithAnyPrefix(IPropertyZoneFrame zone, List<string> prefixes)
    {
        int n = 0;
        for (int col = 0; col < 8; col++)
            foreach (var r in zone.GetZoneRows(col))
                if (prefixes.Any(p => !string.IsNullOrEmpty(p)
                    && (r ?? "").TrimStart().StartsWith(p, StringComparison.Ordinal)))
                    n++;
        return n;
    }

    /// <summary>no-overlap: among 列出的类型间, 任意两实体包围盒不相交。severity 通常 warning。</summary>
    private static (CheckStatus, string) CheckNoOverlap(IReadOnlyList<Entity> entities, List<string> amongNames)
    {
        var types = (amongNames ?? new List<string>()).Select(ResolveType).Where(t => t != null).ToList();
        if (types.Count == 0) return (CheckStatus.Fail, "among 未指定有效类型");

        var targets = entities
            .Where(e => types.Any(t => t!.IsInstanceOfType(e)))
            .Select(e => (e, b: e.bounding))
            .Where(x => x.b.IsValid)
            .ToList();

        int overlaps = 0;
        for (int i = 0; i < targets.Count; i++)
            for (int j = i + 1; j < targets.Count; j++)
                if (targets[i].b.IntersectWith(targets[j].b)) overlaps++;

        return overlaps == 0
            ? (CheckStatus.Pass, $"{targets.Count} 个标注无重叠")
            : (CheckStatus.Fail, $"{overlaps} 处标注重叠");
    }

    /// <summary>inside-frame: 找到图框 (DrawingFrame), 每个非图框实体的包围盒须被图框包围盒包含。无图框 → N/A。</summary>
    private static (CheckStatus, string) CheckInsideFrame(IReadOnlyList<Entity> entities)
    {
        var frame = entities.FirstOrDefault(e => e is DrawingFrame.DrawingFrame);
        if (frame is null) return (CheckStatus.NotApplicable, "无图框");

        var frameBox = frame.bounding;
        if (!frameBox.IsValid) return (CheckStatus.NotApplicable, "图框无有效包围盒");

        int outside = 0;
        foreach (var e in entities)
        {
            if (ReferenceEquals(e, frame)) continue;
            var b = e.bounding;
            if (!b.IsValid) continue;
            if (!frameBox.Contains(b)) outside++;
        }
        return outside == 0
            ? (CheckStatus.Pass, "内容均在图框内")
            : (CheckStatus.Fail, $"{outside} 个实体出框");
    }

    /// <summary>zone-rows: 属性区某列行数 ≥ min。无图框/列越界 → N/A。</summary>
    private static (CheckStatus, string) CheckZoneRows(IReadOnlyList<Entity> entities, int col, int min)
    {
        var zone = entities.OfType<IPropertyZoneFrame>().FirstOrDefault();
        if (zone is null) return (CheckStatus.NotApplicable, "无属性区图框");
        var rows = zone.GetZoneRows(col);
        return rows.Count >= min
            ? (CheckStatus.Pass, $"第 {col} 列 {rows.Count} 行")
            : (CheckStatus.Fail, $"第 {col} 列需 {min} 行, 实有 {rows.Count}");
    }

    /// <summary>zone-row-prefix: 属性区某列内, 每个 prefix 至少一行以其开头。无图框 → N/A。</summary>
    private static (CheckStatus, string) CheckZoneRowPrefix(IReadOnlyList<Entity> entities, int col, List<string> prefixes)
    {
        var zone = entities.OfType<IPropertyZoneFrame>().FirstOrDefault();
        if (zone is null) return (CheckStatus.NotApplicable, "无属性区图框");
        var ps = (prefixes ?? new List<string>()).Where(p => !string.IsNullOrEmpty(p)).ToList();
        if (ps.Count == 0) return (CheckStatus.Pass, "无需检查");

        // col < 0 = 列布局无关: 任一列含全部前缀即过 (单透镜材料在 col1 / 双胶合在 col3 都能命中)。
        if (col < 0)
        {
            for (int c = 0; c < 8; c++)
            {
                var r = zone.GetZoneRows(c);
                if (r.Count == 0) continue;
                if (ps.All(p => r.Any(x => (x ?? "").TrimStart().StartsWith(p, StringComparison.Ordinal))))
                    return (CheckStatus.Pass, $"第 {c} 列代号齐全");
            }
            return (CheckStatus.Fail, $"任一列均缺代号: {string.Join(" ", ps)}");
        }

        var rows = zone.GetZoneRows(col);
        var missing = ps
            .Where(p => !rows.Any(r => (r ?? "").TrimStart().StartsWith(p, StringComparison.Ordinal)))
            .ToList();
        return missing.Count == 0
            ? (CheckStatus.Pass, $"第 {col} 列代号齐全")
            : (CheckStatus.Fail, $"第 {col} 列缺代号: {string.Join(" ", missing)}");
    }

    // ---------------- 辅助 ----------------

    /// <summary>光学元件 = 拥有 ≥1 个 OpticalSurface 属性的实体 (OpticalLens / CementedLens / 未来 …)。</summary>
    private static bool IsOpticalElement(Entity e) => GetSurfaces(e).Any();

    /// <summary>反射取实体上所有 OpticalSurface 类型属性值 (不硬编码 Front/Back/Contact, 自适应任意元件)。</summary>
    private static IEnumerable<OpticalSurface> GetSurfaces(Entity e)
    {
        foreach (var p in e.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!typeof(OpticalSurface).IsAssignableFrom(p.PropertyType)) continue;
            if (p.GetIndexParameters().Length != 0) continue;
            if (p.GetValue(e) is OpticalSurface s) yield return s;
        }
    }

    private static bool MatchesSurfaceFilter(OpticalSurface s, string filter) =>
        filter?.Trim().ToLowerInvariant() switch
        {
            "aspheric"  => s is AsphericSurface,
            "spherical" => s is SphericalSurface,
            _           => true,
        };

    private static string SurfaceFilterLabel(string filter) =>
        filter?.Trim().ToLowerInvariant() switch
        {
            "aspheric"  => "非球",
            "spherical" => "球",
            _           => "",
        };

    // ---- 类型名解析: 类名 / className / 抽象基类 / 接口, 全在 lcdb 程序集内 ----
    private static readonly Lazy<Dictionary<string, Type>> _typeIndex = new(BuildTypeIndex);

    /// <summary>把 YAML 里的类型名解析为 Type。支持具体类、抽象基类 (DimensionBase)、接口 (IOpticalMark)。找不到返回 null。</summary>
    public static Type? ResolveType(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return _typeIndex.Value.TryGetValue(name.Trim(), out var t) ? t : null;
    }

    private static Dictionary<string, Type> BuildTypeIndex()
    {
        // 先收 className (具体 Entity 子类), 再补 Type.Name (含抽象基类与接口) — 不覆盖已有键。
        var dict = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var name in lcdb.IO.EntityTypeRegistry.AllNames)
        {
            var t = lcdb.IO.EntityTypeRegistry.FindByName(name);
            if (t != null && !dict.ContainsKey(name)) dict[name] = t;
        }
        foreach (var t in typeof(Entity).Assembly.GetTypes())
        {
            if (!dict.ContainsKey(t.Name)) dict[t.Name] = t;
        }
        return dict;
    }
}
