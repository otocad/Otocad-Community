using System.Text.Json;
using LitMath;
using lcdb.Annotation;

namespace lcdb.IO;

/// <summary>
/// 已删除标记类的反序列化迁移 (2026-06-04 代号审计定的 3 对合并, 2026-06-21 落码).
///
/// 旧 .otocad 里这三种 kind 的实体不再有对应类, 反序列化时按下表转换为替代标记,
/// 保留位置/缩放/文字偏移与可映射参数, 代号串由替代类自己的格式源重新生成
/// (旧 MarkText 是非 ISO 散文, 不迁移):
///
///   SurfaceImperfectionMark → SurfaceQualityMark      (ISO 10110-7, 5/N×A)
///   CenterDeviationMark     → CenteringToleranceMark  (ISO 10110-6, 4/σ(t))
///   MaterialDefectMark      → 按 DefectType 拆回专用标记:
///     Bubble/Inclusion/Combined → ISO10110_3Mark (1/N×A, Combined 模式恰为"气泡夹杂同标准")
///     Stria                     → ISO10110_4Mark (2/A;B, 旧 nm/cm 分级与 ISO 等级制不同构, 取默认等级)
///     StressBirefringence       → ISO10110_2Mark (0/A)
///
/// 不迁移 color(旧类经 GetMarkColor 强制配色, 属 T-2 反模式; 替代标记走 ByLayer)。
/// </summary>
internal static class LegacyMarkMigrations
{
    /// <summary>kind 是否为已删除的旧标记类名.</summary>
    internal static bool CanMigrate(string kind) =>
        kind is "SurfaceImperfectionMark" or "CenterDeviationMark" or "MaterialDefectMark";

    /// <summary>把旧 JSON 转换为替代标记实体. 不认识的 kind 返回 null.</summary>
    internal static Entity? Migrate(string kind, JsonElement root) => kind switch
    {
        "SurfaceImperfectionMark" => MigrateSurfaceImperfection(root),
        "CenterDeviationMark" => MigrateCenterDeviation(root),
        "MaterialDefectMark" => MigrateMaterialDefect(root),
        _ => null,
    };

    private static Entity MigrateSurfaceImperfection(JsonElement root)
    {
        var mark = new SurfaceQualityMark(
            GetVector2(root, "Position"),
            GetInt(root, "AllowedDefectCount", 3),
            GetDouble(root, "PointDefectDiameter", 0.025),
            scratchCount: 0,  // 旧类无线状缺陷数量字段, 不虚构
            scratchWidth: GetDouble(root, "LinearDefectWidth", 0.0));
        ApplyCommon(mark, root);
        return mark;
    }

    private static Entity MigrateCenterDeviation(JsonElement root)
    {
        var mark = new CenteringToleranceMark(
            GetVector2(root, "Position"),
            GetDouble(root, "DecentrationValue", 0.05),
            GetDouble(root, "TiltValue", 0.0));
        ApplyCommon(mark, root);
        return mark;
    }

    private static Entity MigrateMaterialDefect(JsonElement root)
    {
        var pos = GetVector2(root, "Position");
        // MaterialDefectType: 0=Bubble 1=Stria 2=StressBirefringence 3=Inclusion 4=Combined
        Entity mark = GetInt(root, "DefectType", 0) switch
        {
            1 => new ISO10110_4Mark { Position = pos },
            2 => new ISO10110_2Mark(pos, GetDouble(root, "StressBirefringence", 10.0)),
            _ => new ISO10110_3Mark(pos,
                     GetInt(root, "BubbleCount", 1),
                     GetDouble(root, "BubbleGrade", 0.16)),
        };
        ApplyCommon(mark, root);
        return mark;
    }

    /// <summary>共通呈现字段: Scale / ShowText / TextOffset / Rotation / IsVisible (替代类同名属性).</summary>
    private static void ApplyCommon(Entity mark, JsonElement root)
    {
        var t = mark.GetType();
        t.GetProperty("Scale")!.SetValue(mark, GetDouble(root, "Scale", 1.0));
        t.GetProperty("ShowText")!.SetValue(mark, GetBool(root, "ShowText", true));
        t.GetProperty("TextOffset")!.SetValue(mark, GetVector2(root, "TextOffset", new Vector2(0, -30)));
        t.GetProperty("Rotation")!.SetValue(mark, GetDouble(root, "Rotation", 0.0));
        t.GetProperty("IsVisible")!.SetValue(mark, GetBool(root, "IsVisible", true));
    }

    // ---- JsonElement 读取 (缺字段回退默认值; Vector2 兼容 [x,y] 与 {X,Y} 两种历史格式) ----

    private static double GetDouble(JsonElement root, string name, double fallback) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number ? el.GetDouble() : fallback;

    private static int GetInt(JsonElement root, string name, int fallback) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number ? el.GetInt32() : fallback;

    private static bool GetBool(JsonElement root, string name, bool fallback) =>
        root.TryGetProperty(name, out var el) && el.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? el.GetBoolean() : fallback;

    private static Vector2 GetVector2(JsonElement root, string name) => GetVector2(root, name, Vector2.Zero);

    private static Vector2 GetVector2(JsonElement root, string name, Vector2 fallback)
    {
        if (!root.TryGetProperty(name, out var el)) return fallback;
        if (el.ValueKind == JsonValueKind.Array && el.GetArrayLength() >= 2)
            return new Vector2(el[0].GetDouble(), el[1].GetDouble());
        if (el.ValueKind == JsonValueKind.Object &&
            el.TryGetProperty("X", out var x) && el.TryGetProperty("Y", out var y))
            return new Vector2(x.GetDouble(), y.GetDouble());
        return fallback;
    }
}
