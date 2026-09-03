using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace lcdb.IO;

/// <summary>
/// V4 polymorphic Entity 序列化器.
///
/// 写: 在 Entity 对象 JSON 顶部插 "kind" 字段 (= className), 然后按子类型默认序列化.
/// 读: 先读 "kind", 通过 EntityTypeRegistry 找到子类型, 用 System.Text.Json
///     默认反序列化为该子类型实例.
///
/// 这样所有 lcdb 已有 + 未来加的 Entity 子类自动 round-trip, 无需手写 [JsonDerivedType].
/// </summary>
public sealed class EntityJsonConverter : JsonConverter<Entity>
{
    public const string DiscriminatorProperty = "kind";

    /// <summary>
    /// 只对 abstract `Entity` 基类本身生效, 不对具体子类 (OpticalLens 等) 生效.
    /// 反序列化具体子类时走默认反射, 其内部的 List&lt;Entity&gt; / Entity 字段
    /// 还会路由回本 converter — 多态正确, 不递归.
    /// </summary>
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(Entity);

    public override Entity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Entity 反序列化期望 StartObject, 实际 {reader.TokenType}");

        // 把整个 object 先 parse 成 JsonDocument (因为我们要先看 "kind" 再决定 target type)
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty(DiscriminatorProperty, out var kindEl) ||
            kindEl.ValueKind != JsonValueKind.String)
        {
            // 缺 kind: 兼容老格式, 直接返 null (调用方应跳过或诊断)
            return null;
        }

        var kind = kindEl.GetString();
        var targetType = EntityTypeRegistry.FindByName(kind);
        if (targetType is null)
        {
            // 已删除的旧标记类 (3 对合并废弃) → 转换为替代标记, 不丢实体
            if (kind is not null && LegacyMarkMigrations.CanMigrate(kind))
                return LegacyMarkMigrations.Migrate(kind, root);

            System.Diagnostics.Debug.WriteLine($"[EntityJsonConverter] 未知 kind: {kind}");
            return null;
        }

        // 反序列化为具体子类. CanConvert 只对 abstract Entity 生效, 所以
        // 子类本身走默认反射, 子类内部的 List<Entity>/Entity 字段再路由回本 converter.
        // 不需要 remove self (CanConvert 已隔离).
        var rawJson = root.GetRawText();
        try
        {
            return (Entity?)JsonSerializer.Deserialize(rawJson, targetType, options);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EntityJsonConverter] 反序列化 {kind} 失败: {ex.Message}");
            System.Console.WriteLine($"[EntityJsonConverter] 反序列化 {kind} 失败: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options)
    {
        if (value is null) { writer.WriteNullValue(); return; }

        // 用具体子类型重新序列化 — CanConvert 不对子类生效, 不会递归.
        var rawJson = JsonSerializer.SerializeToUtf8Bytes(value, value.GetType(), options);
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        // 重写: 加 kind 在最前, 然后原属性
        writer.WriteStartObject();
        writer.WriteString(DiscriminatorProperty, EntityTypeRegistry.GetName(value.GetType()));
        foreach (var prop in root.EnumerateObject())
        {
            if (string.Equals(prop.Name, DiscriminatorProperty, StringComparison.Ordinal)) continue;
            prop.WriteTo(writer);
        }
        writer.WriteEndObject();
    }
}
