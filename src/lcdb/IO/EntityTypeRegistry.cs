using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace lcdb.IO;

/// <summary>
/// V4 polymorphic 序列化的 Entity 子类注册表.
///
/// 设计: 不依赖手填 [JsonDerivedType] (70+ 个 Entity 子类会漏 + 易出错).
/// 启动时反射扫描 lcdb 程序集, 自动收集所有 `: Entity` 子类, 用其 className
/// 作为 polymorphic discriminator.
///
/// className 来源: 各 Entity 子类的 `public override string className` 属性
/// (lcdb 现有约定, 例如 "Line" / "OpticalLens" / "CoatingMark").
/// </summary>
public static class EntityTypeRegistry
{
    private static readonly Lazy<Dictionary<string, Type>> _byName = new(BuildRegistry);
    private static readonly Lazy<Dictionary<Type, string>> _byType = new(BuildReverseLookup);

    /// <summary>按 className 字符串查 Entity 子类型. 找不到返回 null.</summary>
    public static Type? FindByName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return _byName.Value.TryGetValue(name, out var t) ? t : null;
    }

    /// <summary>按 Entity 子类型查 className. 未注册子类返回 t.Name.</summary>
    public static string GetName(Type t) =>
        _byType.Value.TryGetValue(t, out var n) ? n : t.Name;

    /// <summary>所有已注册 className (用于诊断/单测).</summary>
    public static IEnumerable<string> AllNames => _byName.Value.Keys;

    /// <summary>所有已注册 Entity 子类型.</summary>
    public static IEnumerable<Type> AllTypes => _byName.Value.Values;

    private static Dictionary<string, Type> BuildRegistry()
    {
        var dict = new Dictionary<string, Type>(StringComparer.Ordinal);
        var lcdbAssembly = typeof(Entity).Assembly;

        // 扫 lcdb 程序集所有 public/non-abstract `: Entity` 子类
        foreach (var t in lcdbAssembly.GetTypes())
        {
            if (t.IsAbstract) continue;
            if (!typeof(Entity).IsAssignableFrom(t)) continue;
            if (t == typeof(Entity)) continue;

            var name = TryGetClassName(t);
            if (string.IsNullOrEmpty(name)) name = t.Name;

            // 同名冲突: 第一个先到先得 (后续诊断会显示)
            if (!dict.ContainsKey(name))
            {
                dict[name] = t;
            }
        }

        return dict;
    }

    private static Dictionary<Type, string> BuildReverseLookup()
    {
        var rev = new Dictionary<Type, string>();
        foreach (var kv in _byName.Value)
        {
            if (!rev.ContainsKey(kv.Value))
                rev[kv.Value] = kv.Key;
        }
        return rev;
    }

    /// <summary>
    /// 尝试取 Entity 子类的 className 静态值: 用无参构造实例化后读 className 属性.
    /// 若实例化失败 (无 public 无参构造) → 退回 Type.Name.
    /// </summary>
    private static string? TryGetClassName(Type t)
    {
        try
        {
            var ctor = t.GetConstructor(Type.EmptyTypes);
            if (ctor is null) return null;
            var instance = ctor.Invoke(null) as Entity;
            return instance?.className;
        }
        catch { return null; }
    }
}
