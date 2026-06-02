using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OtoCAD.OpticEntity
{
    /// <summary>
    /// 玻璃库版本控制系统
    /// </summary>
    public class GlassVersionControl
    {
        private readonly string _versionDirectory;
        private readonly GlassDatabase _database;
        private static GlassVersionControl _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static GlassVersionControl Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new GlassVersionControl(GlassDatabase.Instance);
                        }
                    }
                }
                return _instance;
            }
        }

        private GlassVersionControl(GlassDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            
            // 初始化版本目录
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "OtoCAD", "GlassVersions");
            _versionDirectory = appFolder;
            
            if (!Directory.Exists(_versionDirectory))
            {
                Directory.CreateDirectory(_versionDirectory);
            }
        }

        /// <summary>
        /// 创建新版本
        /// </summary>
        public GlassVersion CreateVersion(string description, string author = null)
        {
            var version = new GlassVersion
            {
                Id = Guid.NewGuid().ToString(),
                VersionNumber = GenerateVersionNumber(),
                CreateTime = DateTime.Now,
                Description = description,
                Author = author ?? Environment.UserName,
                MaterialCount = _database.Count
            };

            // 保存当前数据库状态
            var materials = _database.GetAllMaterials().ToList();
            SaveVersionData(version, materials);

            // 保存版本信息
            SaveVersionInfo(version);

            return version;
        }

        /// <summary>
        /// 获取所有版本
        /// </summary>
        public List<GlassVersion> GetVersions()
        {
            var versions = new List<GlassVersion>();
            var versionFiles = Directory.GetFiles(_versionDirectory, "*.version");

            foreach (var file in versionFiles)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var version = JsonSerializer.Deserialize<GlassVersion>(json);
                    if (version != null)
                    {
                        versions.Add(version);
                    }
                }
                catch (Exception ex)
                {
                    // 记录错误但继续处理其他版本
                    System.Diagnostics.Debug.WriteLine($"Failed to load version from {file}: {ex.Message}");
                }
            }

            return versions.OrderByDescending(v => v.CreateTime).ToList();
        }

        /// <summary>
        /// 恢复到指定版本
        /// </summary>
        public bool RestoreVersion(string versionId, bool createBackup = true)
        {
            var version = GetVersion(versionId);
            if (version == null)
                return false;

            try
            {
                // 创建当前状态的备份
                if (createBackup)
                {
                    CreateVersion($"自动备份 - 恢复到版本 {version.VersionNumber} 之前");
                }

                // 加载版本数据
                var materials = LoadVersionData(version);
                
                // 清空当前数据库
                foreach (var material in _database.GetAllMaterials().ToList())
                {
                    _database.RemoveMaterial(material.Code);
                }

                // 导入版本数据
                foreach (var material in materials)
                {
                    _database.AddOrUpdateMaterial(material);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to restore version {versionId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 比较两个版本
        /// </summary>
        public VersionComparison CompareVersions(string versionId1, string versionId2)
        {
            var version1 = GetVersion(versionId1);
            var version2 = GetVersion(versionId2);

            if (version1 == null || version2 == null)
                return null;

            var materials1 = LoadVersionData(version1);
            var materials2 = LoadVersionData(version2);

            var comparison = new VersionComparison
            {
                Version1 = version1,
                Version2 = version2
            };

            // 构建材料字典
            var dict1 = materials1.ToDictionary(m => m.Code);
            var dict2 = materials2.ToDictionary(m => m.Code);

            // 查找新增的材料
            foreach (var material in materials2)
            {
                if (!dict1.ContainsKey(material.Code))
                {
                    comparison.AddedMaterials.Add(material);
                }
            }

            // 查找删除的材料
            foreach (var material in materials1)
            {
                if (!dict2.ContainsKey(material.Code))
                {
                    comparison.RemovedMaterials.Add(material);
                }
            }

            // 查找修改的材料
            foreach (var material2 in materials2)
            {
                if (dict1.TryGetValue(material2.Code, out var material1))
                {
                    var changes = CompareMaterials(material1, material2);
                    if (changes.Count > 0)
                    {
                        comparison.ModifiedMaterials.Add(new MaterialModification
                        {
                            Original = material1,
                            Modified = material2,
                            Changes = changes
                        });
                    }
                }
            }

            return comparison;
        }

        /// <summary>
        /// 删除版本
        /// </summary>
        public bool DeleteVersion(string versionId)
        {
            var version = GetVersion(versionId);
            if (version == null)
                return false;

            try
            {
                // 删除版本信息文件
                var versionFile = Path.Combine(_versionDirectory, $"{versionId}.version");
                if (File.Exists(versionFile))
                    File.Delete(versionFile);

                // 删除版本数据文件
                var dataFile = Path.Combine(_versionDirectory, $"{versionId}.data");
                if (File.Exists(dataFile))
                    File.Delete(dataFile);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 导出版本
        /// </summary>
        public bool ExportVersion(string versionId, string exportPath)
        {
            var version = GetVersion(versionId);
            if (version == null)
                return false;

            try
            {
                var materials = LoadVersionData(version);
                
                var exportData = new GlassVersionExport
                {
                    Version = version,
                    Materials = materials,
                    ExportTime = DateTime.Now
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                string json = JsonSerializer.Serialize(exportData, options);
                File.WriteAllText(exportPath, json);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 导入版本
        /// </summary>
        public GlassVersion ImportVersion(string importPath)
        {
            try
            {
                var json = File.ReadAllText(importPath);
                var options = new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter() }
                };

                var exportData = JsonSerializer.Deserialize<GlassVersionExport>(json, options);
                if (exportData == null)
                    return null;

                // 创建新版本
                var version = new GlassVersion
                {
                    Id = Guid.NewGuid().ToString(),
                    VersionNumber = GenerateVersionNumber(),
                    CreateTime = DateTime.Now,
                    Description = $"导入自: {Path.GetFileName(importPath)} (原版本: {exportData.Version.VersionNumber})",
                    Author = Environment.UserName,
                    MaterialCount = exportData.Materials.Count,
                    OriginalVersion = exportData.Version
                };

                // 保存版本数据
                SaveVersionData(version, exportData.Materials);
                SaveVersionInfo(version);

                return version;
            }
            catch
            {
                return null;
            }
        }

        #region 私有方法

        private GlassVersion GetVersion(string versionId)
        {
            var versionFile = Path.Combine(_versionDirectory, $"{versionId}.version");
            if (!File.Exists(versionFile))
                return null;

            try
            {
                var json = File.ReadAllText(versionFile);
                return JsonSerializer.Deserialize<GlassVersion>(json);
            }
            catch
            {
                return null;
            }
        }

        private void SaveVersionInfo(GlassVersion version)
        {
            var versionFile = Path.Combine(_versionDirectory, $"{version.Id}.version");
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(version, options);
            File.WriteAllText(versionFile, json);
        }

        private void SaveVersionData(GlassVersion version, List<GlassMaterial> materials)
        {
            var dataFile = Path.Combine(_versionDirectory, $"{version.Id}.data");
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };
            string json = JsonSerializer.Serialize(materials, options);
            File.WriteAllText(dataFile, json);
        }

        private List<GlassMaterial> LoadVersionData(GlassVersion version)
        {
            var dataFile = Path.Combine(_versionDirectory, $"{version.Id}.data");
            if (!File.Exists(dataFile))
                return new List<GlassMaterial>();

            try
            {
                var json = File.ReadAllText(dataFile);
                var options = new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter() }
                };
                return JsonSerializer.Deserialize<List<GlassMaterial>>(json, options) ?? new List<GlassMaterial>();
            }
            catch
            {
                return new List<GlassMaterial>();
            }
        }

        private string GenerateVersionNumber()
        {
            var versions = GetVersions();
            if (!versions.Any())
                return "1.0.0";

            // 获取最新版本号并递增
            var latestVersion = versions.First().VersionNumber;
            var parts = latestVersion.Split('.');
            
            if (parts.Length == 3 && 
                int.TryParse(parts[0], out int major) &&
                int.TryParse(parts[1], out int minor) &&
                int.TryParse(parts[2], out int patch))
            {
                return $"{major}.{minor}.{patch + 1}";
            }

            return "1.0.0";
        }

        private List<string> CompareMaterials(GlassMaterial material1, GlassMaterial material2)
        {
            var changes = new List<string>();

            if (material1.Name != material2.Name)
                changes.Add($"名称: {material1.Name} → {material2.Name}");
            
            if (Math.Abs(material1.Nd - material2.Nd) > 0.000001)
                changes.Add($"nd: {material1.Nd:F6} → {material2.Nd:F6}");
            
            if (Math.Abs(material1.NF - material2.NF) > 0.000001)
                changes.Add($"nF: {material1.NF:F6} → {material2.NF:F6}");
            
            if (Math.Abs(material1.NC - material2.NC) > 0.000001)
                changes.Add($"nC: {material1.NC:F6} → {material2.NC:F6}");
            
            if (Math.Abs(material1.Density - material2.Density) > 0.01)
                changes.Add($"密度: {material1.Density:F2} → {material2.Density:F2}");

            // 可以添加更多属性比较...

            return changes;
        }

        #endregion
    }

    /// <summary>
    /// 玻璃库版本信息
    /// </summary>
    public class GlassVersion
    {
        public string Id { get; set; }
        public string VersionNumber { get; set; }
        public DateTime CreateTime { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public int MaterialCount { get; set; }
        
        [JsonIgnore]
        public GlassVersion OriginalVersion { get; set; }
    }

    /// <summary>
    /// 版本比较结果
    /// </summary>
    public class VersionComparison
    {
        public GlassVersion Version1 { get; set; }
        public GlassVersion Version2 { get; set; }
        public List<GlassMaterial> AddedMaterials { get; set; }
        public List<GlassMaterial> RemovedMaterials { get; set; }
        public List<MaterialModification> ModifiedMaterials { get; set; }

        public VersionComparison()
        {
            AddedMaterials = new List<GlassMaterial>();
            RemovedMaterials = new List<GlassMaterial>();
            ModifiedMaterials = new List<MaterialModification>();
        }
    }

    /// <summary>
    /// 材料修改信息
    /// </summary>
    public class MaterialModification
    {
        public GlassMaterial Original { get; set; }
        public GlassMaterial Modified { get; set; }
        public List<string> Changes { get; set; }
    }

    /// <summary>
    /// 版本导出数据
    /// </summary>
    public class GlassVersionExport
    {
        public GlassVersion Version { get; set; }
        public List<GlassMaterial> Materials { get; set; }
        public DateTime ExportTime { get; set; }
    }
}