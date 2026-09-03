using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using lcdb.Annotation;

namespace lcdb.Standards
{
    /// <summary>
    /// 出图惯例服务(静态,AppSettings 级)——范本: Avalonia 端 DimensionStandardService。
    ///
    /// 放在 lcdb(而非 UI 层)是因为各 Mark / 属性区(均在 lcdb)渲染时要读 <see cref="Active"/>:
    /// 自身有 override 用 override,否则取 Active 的对应项。UI 层只负责选择/编辑后调用本服务。
    ///
    /// 本片(A1)实现:内置预设 + Active/SetActive + Derive/Save(内存)。
    /// 文件持久化(用户库 %AppData%/OtoCAD/standards/*.json)与 .otocad 文档内嵌见后续片。
    /// </summary>
    public static class DrawingConventionService
    {
        private static readonly List<DrawingConvention> _builtIns = CreateBuiltIns();
        private static readonly List<DrawingConvention> _custom = new();
        private static DrawingConvention _active = _builtIns[0];

        /// <summary>内置预设(只读):GB-ISO / MIL-ANSI / JIS / DIN。</summary>
        public static IReadOnlyList<DrawingConvention> BuiltIns => _builtIns;

        /// <summary>用户保存的自定义惯例。</summary>
        public static IReadOnlyList<DrawingConvention> CustomLibrary => _custom;

        /// <summary>当前文档使用的惯例(默认 GB-ISO)。</summary>
        public static DrawingConvention Active => _active;

        /// <summary>全部可选名(内置 + 自定义),供下拉列举。</summary>
        public static IEnumerable<string> AllNames => _builtIns.Concat(_custom).Select(s => s.Name);

        /// <summary>按名查找(先内置后自定义);无则 null。</summary>
        public static DrawingConvention? Find(string? name)
            => name is null ? null : _builtIns.Concat(_custom).FirstOrDefault(s => s.Name == name);

        /// <summary>按名设为当前;找不到返回 false(不改 Active)。</summary>
        public static bool SetActive(string name)
        {
            var s = Find(name);
            if (s is null) return false;
            _active = s;
            return true;
        }

        /// <summary>直接设当前(用于文档内嵌惯例加载)。</summary>
        public static void SetActive(DrawingConvention convention) => _active = convention;

        /// <summary>
        /// 采纳 .otocad 文档内嵌的惯例为当前:同名(内置/已知自定义)用既有实例;
        /// 未知名则作为自定义入内存库并设为当前(不落盘——用户可另存为)。null 忽略。
        /// </summary>
        public static void AdoptFromDocument(DrawingConvention? convention)
        {
            if (convention is null) return;
            var existing = Find(convention.Name);
            if (existing is not null) { _active = existing; return; }
            convention.IsBuiltIn = false;
            _custom.Add(convention);
            _active = convention;
        }

        /// <summary>复制基准 → 可改的自定义副本,命名 "基准名-修改"。</summary>
        public static DrawingConvention Derive(DrawingConvention baseConvention)
        {
            var c = baseConvention.Clone();
            c.BaseName = baseConvention.Name;
            c.IsBuiltIn = false;
            c.Name = baseConvention.Name + "-修改";
            return c;
        }

        /// <summary>存入自定义库(同名替换)并落盘。</summary>
        public static void Save(DrawingConvention convention)
        {
            convention.IsBuiltIn = false;
            _custom.RemoveAll(s => s.Name == convention.Name);
            _custom.Add(convention);
            TryWriteFile(convention);
        }

        /// <summary>(测试/重置用)清空内存自定义库并把 Active 复位到首个内置(不动磁盘)。</summary>
        public static void ResetCustom()
        {
            _custom.Clear();
            _active = _builtIns[0];
        }

        // -------- 文件持久化(用户库 %AppData%/OtoCAD/standards/*.json) --------

        /// <summary>覆盖存储目录(测试用);null 则用默认 %AppData%/OtoCAD/standards。</summary>
        public static string? StoreDirOverride { get; set; }

        private static readonly JsonSerializerOptions _json = new JsonSerializerOptions { WriteIndented = true };

        private static string StoreDir
        {
            get
            {
                var dir = StoreDirOverride ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "OtoCAD", "standards");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        /// <summary>从磁盘加载全部自定义标准到内存库(清空后重载)。失败静默。</summary>
        public static void Load()
        {
            try
            {
                _custom.Clear();
                foreach (var file in Directory.EnumerateFiles(StoreDir, "*.json"))
                {
                    try
                    {
                        var std = JsonSerializer.Deserialize<DrawingConvention>(File.ReadAllText(file), _json);
                        if (std is not null && !string.IsNullOrWhiteSpace(std.Name))
                        {
                            std.IsBuiltIn = false;
                            _custom.RemoveAll(s => s.Name == std.Name);
                            _custom.Add(std);
                        }
                    }
                    catch { /* 跳过坏文件 */ }
                }
            }
            catch { /* 目录不可用静默 */ }
        }

        /// <summary>把内存自定义库全部写盘。</summary>
        public static void Persist()
        {
            foreach (var s in _custom) TryWriteFile(s);
        }

        private static void TryWriteFile(DrawingConvention convention)
        {
            try
            {
                File.WriteAllText(
                    Path.Combine(StoreDir, SafeFileName(convention.Name) + ".json"),
                    JsonSerializer.Serialize(convention, _json));
            }
            catch { /* 写失败静默 */ }
        }

        private static string SafeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "unnamed" : name;
        }

        private static List<DrawingConvention> CreateBuiltIns() => new List<DrawingConvention>
        {
            new DrawingConvention
            {
                Name = "GB-ISO 10110", IsBuiltIn = true,
                SurfaceQuality = SurfaceQualityStandard.ISO_10110_7,
                FormAccuracy = FormAccuracyStandard.ISO_10110_5,
                Roughness = RoughnessStandard.GB,
                LaserDamage = LaserDamageTestStandard.ISO21254,
                Units = DrawingUnits.Millimeter,
                DimensionStandardKey = "GB-Optical",
                DrawingStandardName = "GB",
            },
            new DrawingConvention
            {
                Name = "MIL-ANSI", IsBuiltIn = true,
                SurfaceQuality = SurfaceQualityStandard.MIL_PRF_13830,
                FormAccuracy = FormAccuracyStandard.PV_RMS_Format,
                Roughness = RoughnessStandard.ANSI,
                LaserDamage = LaserDamageTestStandard.MIL_PRF_13830B,
                Units = DrawingUnits.Millimeter,
                DimensionStandardKey = "Standard",
                DrawingStandardName = "ISO",
                FrameTemplateKey = "iso-lens",
            },
            new DrawingConvention
            {
                Name = "JIS", IsBuiltIn = true,
                SurfaceQuality = SurfaceQualityStandard.ISO_10110_7,
                FormAccuracy = FormAccuracyStandard.ISO_10110_5,
                Roughness = RoughnessStandard.JIS,
                LaserDamage = LaserDamageTestStandard.ISO21254,
                Units = DrawingUnits.Millimeter,
                DimensionStandardKey = "ISO-25",
                DrawingStandardName = "ISO",
                FrameTemplateKey = "iso-lens",
            },
            new DrawingConvention
            {
                Name = "DIN", IsBuiltIn = true,
                SurfaceQuality = SurfaceQualityStandard.ISO_10110_7,
                FormAccuracy = FormAccuracyStandard.ISO_10110_5,
                Roughness = RoughnessStandard.DIN,
                LaserDamage = LaserDamageTestStandard.ISO21254,
                Units = DrawingUnits.Millimeter,
                DimensionStandardKey = "ISO-25",
                DrawingStandardName = "ISO",
                FrameTemplateKey = "iso-lens",
            },
        };
    }
}
