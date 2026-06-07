using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using LitMath;

namespace lcdb.IO;

/// <summary>
/// LitMath.Vector2 序列化为 [x, y] 双元素数组.
/// 默认 System.Text.Json 反射会跟 .normalized 等 derived 属性套娃无限递归.
/// </summary>
public sealed class Vector2JsonConverter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return Vector2.Zero;

        // 数组形式 [x, y] (新格式)
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            reader.Read();
            double x = reader.GetDouble();
            reader.Read();
            double y = reader.GetDouble();
            reader.Read();  // EndArray
            return new Vector2(x, y);
        }

        // 兼容旧对象形式 {"X":..,"Y":..}
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            double xo = 0, yo = 0;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName) continue;
                var name = reader.GetString();
                reader.Read();
                if (string.Equals(name, "X", StringComparison.OrdinalIgnoreCase))      xo = reader.GetDouble();
                else if (string.Equals(name, "Y", StringComparison.OrdinalIgnoreCase)) yo = reader.GetDouble();
                else reader.Skip();
            }
            return new Vector2(xo, yo);
        }

        throw new JsonException($"Vector2 期望 StartArray 或 StartObject, 实际 {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteEndArray();
    }
}

/// <summary>LitMath.Vector3 序列化为 [x, y, z].</summary>
public sealed class Vector3JsonConverter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return new Vector3(0, 0, 0);

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            reader.Read();
            double x = reader.GetDouble();
            reader.Read();
            double y = reader.GetDouble();
            reader.Read();
            double z = reader.GetDouble();
            reader.Read();
            return new Vector3(x, y, z);
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            double xo = 0, yo = 0, zo = 0;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName) continue;
                var name = reader.GetString();
                reader.Read();
                if (string.Equals(name, "X", StringComparison.OrdinalIgnoreCase))      xo = reader.GetDouble();
                else if (string.Equals(name, "Y", StringComparison.OrdinalIgnoreCase)) yo = reader.GetDouble();
                else if (string.Equals(name, "Z", StringComparison.OrdinalIgnoreCase)) zo = reader.GetDouble();
                else reader.Skip();
            }
            return new Vector3(xo, yo, zo);
        }

        throw new JsonException($"Vector3 期望 StartArray 或 StartObject, 实际 {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteEndArray();
    }
}
