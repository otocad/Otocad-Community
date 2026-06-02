using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace lcdb.IO;

/// <summary>
/// V4 文件格式 — 文档型/CAD 型 双段 JSON + polymorphic Entity 序列化.
///
/// 单文件 JSON, 顶层结构:
/// {
///   "format": "otocad",
///   "version": "4.0",
///   "document": { ... DocumentMeta ... },
///   "model":    { "entities": [ {"kind":"Line",...}, {"kind":"OpticalLens",...} ] }
/// }
///
/// 优点:
/// - Entity 子类一行 className 自动 round-trip (反射注册 + EntityJsonConverter)
/// - DocumentMeta 跟 entities 分段 → DXF 导出只取 model 段
/// - 老 V3 .otocad 文件仍可读 (Database.Open 路由)
/// - 加新字段不破坏旧文件 (System.Text.Json 默认忽略未知字段)
/// </summary>
public static class OtocadFileFormatV4
{
    public const string FormatTag = "otocad";
    public const string CurrentVersion = "4.0";

    /// <summary>给 V5 zip 容器复用 (跨格式共享 polymorphic + LitMath converters).</summary>
    internal static JsonSerializerOptions BuildOptionsExposed() => BuildOptions();

    private static JsonSerializerOptions BuildOptions()
    {
        var opts = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            MaxDepth = 128,
            // 允许 ±Infinity / NaN 字面量 (光学透镜 R=∞ 表平面)
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        };
        opts.Converters.Add(new EntityJsonConverter());
        opts.Converters.Add(new Vector2JsonConverter());
        opts.Converters.Add(new Vector3JsonConverter());
        return opts;
    }

    /// <summary>检测路径所指 JSON 文件是否 V4 格式 (顶层 format=="otocad" + version 以 "4." 开头).</summary>
    public static bool IsV4File(string path)
    {
        if (!File.Exists(path)) return false;
        try
        {
            using var stream = File.OpenRead(path);
            using var doc = JsonDocument.Parse(stream);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return false;
            if (!root.TryGetProperty("format", out var f) || f.GetString() != FormatTag) return false;
            if (!root.TryGetProperty("version", out var v)) return false;
            var ver = v.GetString();
            return ver is not null && ver.StartsWith("4.");
        }
        catch { return false; }
    }

    /// <summary>把 Database 写入 V4 文件.</summary>
    public static void Save(Database db, string path, DocumentMeta? meta = null)
    {
        meta ??= new DocumentMeta { Title = Path.GetFileNameWithoutExtension(path) };
        meta.Modified = DateTime.UtcNow;

        // 摘 Layer 表到 meta.Layers (用户可能改了图层后保存)
        meta.Layers = db.layerTable._items.OfType<Layer>().Select(LayerDto.FromLayer).ToList();

        var entities = db.GetEntities("ModelSpace").ToList();

        var doc = new V4FileRoot
        {
            Format = FormatTag,
            Version = CurrentVersion,
            Document = meta,
            Model = new ModelSection { Entities = entities },
        };

        using var stream = File.Create(path);
        JsonSerializer.Serialize(stream, doc, BuildOptions());
    }

    /// <summary>从 V4 文件读取 (Database, DocumentMeta). 失败抛 JsonException / IOException.</summary>
    public static (Database db, DocumentMeta meta) Load(string path)
    {
        var db = new Database();
        var meta = LoadInto(db, path);
        return (db, meta);
    }

    /// <summary>
    /// 把 V4 文件加载到指定 Database (in-place). 用于 Database.Open 路由
    /// (Clear 后已没有 ModelSpace, 要复用本 db 提前确保 ModelSpace 存在).
    /// </summary>
    public static DocumentMeta LoadInto(Database db, string path)
    {
        using var stream = File.OpenRead(path);
        var doc = JsonSerializer.Deserialize<V4FileRoot>(stream, BuildOptions())
                  ?? throw new InvalidDataException("V4 文件反序列化为 null");

        var meta = doc.Document ?? new DocumentMeta();

        // 确保 ModelSpace 存在 (Database.Open 前可能 ClearBlockTable 过)
        db.EnsureModelSpace();

        // 恢复 Layer 表 (跳过已存在的 "0" 默认层)
        if (meta.Layers is not null)
        {
            foreach (var ldto in meta.Layers)
            {
                if (string.IsNullOrEmpty(ldto.Name)) continue;
                if (db.layerTable[ldto.Name] is Layer) continue;  // 已存在 (默认 "0" 或之前加过)
                db.layerTable.Add(ldto.ToLayer());
            }
        }

        if (doc.Model?.Entities is not null)
        {
            foreach (var e in doc.Model.Entities.Where(x => x is not null))
            {
                try { db.AddEntity(e); }
                catch { /* 容错 */ }
            }
        }
        return meta;
    }

    // -------- 内部 DTO --------

    private sealed class V4FileRoot
    {
        [JsonPropertyName("format")]   public string Format { get; set; } = FormatTag;
        [JsonPropertyName("version")]  public string Version { get; set; } = CurrentVersion;
        [JsonPropertyName("document")] public DocumentMeta? Document { get; set; }
        [JsonPropertyName("model")]    public ModelSection? Model { get; set; }
    }

    private sealed class ModelSection
    {
        [JsonPropertyName("entities")] public List<Entity> Entities { get; set; } = new();
    }
}
