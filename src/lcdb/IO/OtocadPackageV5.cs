using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace lcdb.IO;

/// <summary>
/// V5 zip 容器格式 (OPC 模式) — .otocad 文件实际是 zip, 内含:
///   META.json     文档元数据 (DocumentMeta)
///   MODEL.json    CAD 实体 (与 V4 polymorphic 格式一致)
///   STYLES.json   图层/样式 (Layers, 单独可编辑)
///   THUMBNAIL.png (可选) 文件预览缩略图
///   manifest.json 包内文件列表 + schema 版本
///   external/    (可选) 内嵌外部资料 (Zemax .zmx 等)
///
/// 优点:
/// - 单文件分发但内部分层 (DXF 导出只解压 MODEL.json)
/// - 缩略图随文件 (操作系统文件浏览器预览)
/// - 内嵌外部资料防失链 (Zemax 设计文件可随包传)
/// - 兼容 V4 单 JSON (旧 .otocad 文件继续可读)
/// </summary>
public static class OtocadPackageV5
{
    public const string SchemaVersion = "5.0";

    private const string EntryMeta = "META.json";
    private const string EntryModel = "MODEL.json";
    private const string EntryStyles = "STYLES.json";
    private const string EntryManifest = "manifest.json";
    private const string EntryThumbnail = "THUMBNAIL.png";

    private static JsonSerializerOptions BuildOptions() => OtocadFileFormatV4.BuildOptionsExposed();

    /// <summary>检测文件是否 V5 zip 包 (zip 签名 + 含 manifest.json).</summary>
    public static bool IsV5File(string path)
    {
        if (!File.Exists(path)) return false;
        // zip 签名: PK\x03\x04
        try
        {
            using var fs = File.OpenRead(path);
            int b1 = fs.ReadByte();
            int b2 = fs.ReadByte();
            if (b1 != 'P' || b2 != 'K') return false;
            // 进一步验证: 含 manifest.json
            fs.Seek(0, SeekOrigin.Begin);
            using var zip = new ZipArchive(fs, ZipArchiveMode.Read);
            return zip.Entries.Any(e => string.Equals(e.FullName, EntryManifest, StringComparison.OrdinalIgnoreCase));
        }
        catch { return false; }
    }

    /// <summary>把 Database 打包为 V5 zip.</summary>
    /// <param name="thumbnailPng">可选 PNG 字节数据 (空则不嵌入缩略图)</param>
    public static void Save(Database db, string path, DocumentMeta? meta = null, byte[]? thumbnailPng = null)
    {
        meta ??= new DocumentMeta { Title = Path.GetFileNameWithoutExtension(path) };
        meta.Modified = DateTime.UtcNow;
        meta.Layers = db.layerTable._items.OfType<Layer>().Select(LayerDto.FromLayer).ToList();

        var entities = db.GetEntities("ModelSpace").ToList();
        var options = BuildOptions();

        // 临时文件 + 移动, 避免半写状态
        var tmp = path + ".tmp";
        try
        {
            using (var fs = File.Create(tmp))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                // META.json — 元数据 (不含 Layers, Layers 拆到 STYLES.json)
                var metaForPackage = CloneMetaWithoutLayers(meta);
                WriteJsonEntry(zip, EntryMeta, metaForPackage, options);

                // MODEL.json — 实体 (与 V4 model 段相同)
                var modelSection = new V5ModelSection { Entities = entities };
                WriteJsonEntry(zip, EntryModel, modelSection, options);

                // STYLES.json — 单独的样式 (Layers)
                var styles = new V5StylesSection { Layers = meta.Layers };
                WriteJsonEntry(zip, EntryStyles, styles, options);

                // THUMBNAIL.png — 可选
                if (thumbnailPng is { Length: > 0 })
                {
                    var thumbEntry = zip.CreateEntry(EntryThumbnail, CompressionLevel.Fastest);
                    using var ts = thumbEntry.Open();
                    ts.Write(thumbnailPng, 0, thumbnailPng.Length);
                }

                // manifest.json — 文件清单
                var manifest = new V5Manifest
                {
                    SchemaVersion = SchemaVersion,
                    Created = DateTime.UtcNow,
                    Files = new List<string> { EntryMeta, EntryModel, EntryStyles, EntryManifest }
                        .Concat(thumbnailPng is { Length: > 0 } ? new[] { EntryThumbnail } : Array.Empty<string>())
                        .ToList(),
                };
                WriteJsonEntry(zip, EntryManifest, manifest, options);
            }

            // 原子替换
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }
        catch
        {
            if (File.Exists(tmp)) File.Delete(tmp);
            throw;
        }
    }

    /// <summary>把 V5 zip 加载到指定 Database (in-place). 返回 DocumentMeta.</summary>
    public static DocumentMeta LoadInto(Database db, string path)
    {
        using var fs = File.OpenRead(path);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Read);
        var options = BuildOptions();

        // 读 META
        DocumentMeta? meta = null;
        var metaEntry = zip.GetEntry(EntryMeta);
        if (metaEntry is not null)
        {
            using var ms = metaEntry.Open();
            meta = JsonSerializer.Deserialize<DocumentMeta>(ms, options);
        }
        meta ??= new DocumentMeta();

        // 读 STYLES (Layers)
        var stylesEntry = zip.GetEntry(EntryStyles);
        if (stylesEntry is not null)
        {
            using var ss = stylesEntry.Open();
            var styles = JsonSerializer.Deserialize<V5StylesSection>(ss, options);
            if (styles?.Layers is not null) meta.Layers = styles.Layers;
        }

        db.EnsureModelSpace();

        // 恢复 Layer 表
        foreach (var ldto in meta.Layers ?? new List<LayerDto>())
        {
            if (string.IsNullOrEmpty(ldto.Name)) continue;
            if (db.layerTable[ldto.Name] is Layer) continue;
            db.layerTable.Add(ldto.ToLayer());
        }

        // 读 MODEL — 实体
        var modelEntry = zip.GetEntry(EntryModel);
        if (modelEntry is not null)
        {
            using var ms = modelEntry.Open();
            var model = JsonSerializer.Deserialize<V5ModelSection>(ms, options);
            if (model?.Entities is not null)
            {
                foreach (var e in model.Entities.Where(x => x is not null))
                {
                    try { db.AddEntity(e); }
                    catch { }
                }
            }
        }

        return meta;
    }

    /// <summary>仅提取 MODEL.json 段 (DXF 导出场景: 只需 CAD 不要 META).</summary>
    public static List<Entity>? ExtractModelOnly(string path)
    {
        using var fs = File.OpenRead(path);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Read);
        var modelEntry = zip.GetEntry(EntryModel);
        if (modelEntry is null) return null;
        using var ms = modelEntry.Open();
        var model = JsonSerializer.Deserialize<V5ModelSection>(ms, BuildOptions());
        return model?.Entities;
    }

    /// <summary>提取缩略图字节 (没有返 null).</summary>
    public static byte[]? ExtractThumbnail(string path)
    {
        using var fs = File.OpenRead(path);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Read);
        var thumb = zip.GetEntry(EntryThumbnail);
        if (thumb is null) return null;
        using var ts = thumb.Open();
        using var ms = new MemoryStream();
        ts.CopyTo(ms);
        return ms.ToArray();
    }

    private static void WriteJsonEntry<T>(ZipArchive zip, string name, T obj, JsonSerializerOptions options)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        JsonSerializer.Serialize(writer, obj, options);
    }

    private static DocumentMeta CloneMetaWithoutLayers(DocumentMeta src) => new()
    {
        Title = src.Title,
        Created = src.Created,
        Modified = src.Modified,
        Author = src.Author,
        Client = src.Client,
        ProjectId = src.ProjectId,
        Units = src.Units,
        Revision = src.Revision,
        ExternalRefs = new Dictionary<string, List<string>>(src.ExternalRefs),
        Tags = new Dictionary<string, string>(src.Tags),
        Layers = new List<LayerDto>(),  // 显式空 (STYLES.json 单独存)
    };

    // -------- 内部 DTO --------

    private sealed class V5Manifest
    {
        [JsonPropertyName("schema_version")] public string SchemaVersion { get; set; } = "5.0";
        [JsonPropertyName("created")]        public DateTime Created { get; set; }
        [JsonPropertyName("files")]          public List<string> Files { get; set; } = new();
    }

    private sealed class V5ModelSection
    {
        [JsonPropertyName("entities")] public List<Entity> Entities { get; set; } = new();
    }

    private sealed class V5StylesSection
    {
        [JsonPropertyName("layers")] public List<LayerDto> Layers { get; set; } = new();
    }
}
