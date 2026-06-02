using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OtoCAD.OpticEntity
{
    /// <summary>
    /// 玻璃材料数据库管理类
    /// </summary>
    public class GlassDatabase
    {
        private static GlassDatabase _instance;
        private static readonly object _lock = new object();
        private Dictionary<string, GlassMaterial> _materials;
        private string _databasePath;

        /// <summary>
        /// 获取玻璃数据库单例实例
        /// </summary>
        public static GlassDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new GlassDatabase();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private GlassDatabase()
        {
            _materials = new Dictionary<string, GlassMaterial>();
            InitializeDatabasePath();
            LoadDatabase();
        }

        /// <summary>
        /// 初始化数据库路径
        /// </summary>
        private void InitializeDatabasePath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "OtoCAD");
            
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            _databasePath = Path.Combine(appFolder, "glass_database.json");
        }

        /// <summary>
        /// 获取所有玻璃材料
        /// </summary>
        public IEnumerable<GlassMaterial> GetAllMaterials()
        {
            return _materials.Values.OrderBy(m => m.Code);
        }

        /// <summary>
        /// 根据代码获取玻璃材料
        /// </summary>
        public GlassMaterial GetMaterial(string code)
        {
            return _materials.TryGetValue(code, out var material) ? material : null;
        }

        /// <summary>
        /// 添加或更新玻璃材料
        /// </summary>
        public bool AddOrUpdateMaterial(GlassMaterial material)
        {
            if (material == null || string.IsNullOrWhiteSpace(material.Code))
                return false;

            material.UpdateTime = DateTime.Now;
            _materials[material.Code] = material;
            SaveDatabase();
            return true;
        }

        /// <summary>
        /// 删除玻璃材料
        /// </summary>
        public bool RemoveMaterial(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            bool removed = _materials.Remove(code);
            if (removed)
            {
                SaveDatabase();
            }
            return removed;
        }

        /// <summary>
        /// 按制造商查找材料
        /// </summary>
        public IEnumerable<GlassMaterial> FindByManufacturer(string manufacturer)
        {
            return _materials.Values
                .Where(m => m.Manufacturer?.Equals(manufacturer, StringComparison.OrdinalIgnoreCase) ?? false)
                .OrderBy(m => m.Code);
        }

        /// <summary>
        /// 按玻璃类型查找材料
        /// </summary>
        public IEnumerable<GlassMaterial> FindByType(GlassType type)
        {
            return _materials.Values
                .Where(m => m.Type == type)
                .OrderBy(m => m.Code);
        }

        /// <summary>
        /// 按折射率范围查找材料
        /// </summary>
        public IEnumerable<GlassMaterial> FindByRefractiveIndex(double minNd, double maxNd)
        {
            return _materials.Values
                .Where(m => m.Nd >= minNd && m.Nd <= maxNd)
                .OrderBy(m => m.Nd);
        }

        /// <summary>
        /// 按阿贝数范围查找材料
        /// </summary>
        public IEnumerable<GlassMaterial> FindByAbbeNumber(double minAbbe, double maxAbbe)
        {
            return _materials.Values
                .Where(m => m.AbbeNumber >= minAbbe && m.AbbeNumber <= maxAbbe)
                .OrderBy(m => m.AbbeNumber);
        }

        /// <summary>
        /// 搜索材料（按代码或名称）
        /// </summary>
        public IEnumerable<GlassMaterial> Search(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return GetAllMaterials();

            keyword = keyword.ToLower();
            return _materials.Values
                .Where(m => (m.Code?.ToLower().IndexOf(keyword, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                           (m.Name?.ToLower().IndexOf(keyword, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0)
                .OrderBy(m => m.Code);
        }

        /// <summary>
        /// 保存数据库到文件
        /// </summary>
        private void SaveDatabase()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                string json = JsonSerializer.Serialize(_materials.Values.ToList(), options);
                File.WriteAllText(_databasePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"保存玻璃数据库失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从文件加载数据库
        /// </summary>
        private void LoadDatabase()
        {
            try
            {
                if (!File.Exists(_databasePath))
                {
                    InitializeDefaultMaterials();
                    SaveDatabase();
                    return;
                }

                var options = new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter() }
                };

                string json = File.ReadAllText(_databasePath);
                var materials = JsonSerializer.Deserialize<List<GlassMaterial>>(json, options);
                
                _materials.Clear();
                if (materials != null)
                {
                    foreach (var material in materials)
                    {
                        _materials[material.Code] = material;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"加载玻璃数据库失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 初始化默认材料数据
        /// </summary>
        private void InitializeDefaultMaterials()
        {
            // 添加一些常用的光学玻璃材料作为默认数据
            var defaultMaterials = new List<GlassMaterial>
            {
                new GlassMaterial("BK7", "BK7", "Schott")
                {
                    Type = GlassType.Crown,
                    Nd = 1.5168,
                    NF = 1.5224,
                    NC = 1.5143,
                    PartialDispersion = 0.3081,
                    Density = 2.51,
                    ThermalExpansion = 7.1,
                    SofteningTemperature = 719,
                    YoungsModulus = 81,
                    PoissonRatio = 0.206,
                    WaterResistance = 1,
                    AcidResistance = 1,
                    AlkaliResistance = 2
                },
                new GlassMaterial("SF6", "SF6", "Schott")
                {
                    Type = GlassType.Flint,
                    Nd = 1.8052,
                    NF = 1.8283,
                    NC = 1.7956,
                    PartialDispersion = 0.2551,
                    Density = 3.37,
                    ThermalExpansion = 8.2,
                    SofteningTemperature = 654,
                    YoungsModulus = 72,
                    PoissonRatio = 0.252,
                    WaterResistance = 1,
                    AcidResistance = 1,
                    AlkaliResistance = 4
                },
                new GlassMaterial("H-K9L", "H-K9L", "CDGM")
                {
                    Type = GlassType.Crown,
                    Nd = 1.5168,
                    NF = 1.5224,
                    NC = 1.5143,
                    PartialDispersion = 0.3081,
                    Density = 2.52,
                    ThermalExpansion = 7.4,
                    SofteningTemperature = 557,
                    YoungsModulus = 82.3,
                    PoissonRatio = 0.208,
                    WaterResistance = 1,
                    AcidResistance = 1,
                    AlkaliResistance = 2
                },
                new GlassMaterial("H-ZF2", "H-ZF2", "CDGM")
                {
                    Type = GlassType.DenseFlint,
                    Nd = 1.6727,
                    NF = 1.6869,
                    NC = 1.6674,
                    PartialDispersion = 0.2951,
                    Density = 3.17,
                    ThermalExpansion = 8.6,
                    SofteningTemperature = 436,
                    YoungsModulus = 60.6,
                    PoissonRatio = 0.224,
                    WaterResistance = 3,
                    AcidResistance = 3,
                    AlkaliResistance = 2
                }
            };

            foreach (var material in defaultMaterials)
            {
                _materials[material.Code] = material;
            }
        }

        /// <summary>
        /// 导出数据库到文件
        /// </summary>
        public void ExportToFile(string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                string json = JsonSerializer.Serialize(_materials.Values.ToList(), options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"导出玻璃数据库失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从文件导入数据库
        /// </summary>
        public void ImportFromFile(string filePath, bool merge = true)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("文件不存在", filePath);

                var options = new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter() }
                };

                string json = File.ReadAllText(filePath);
                var materials = JsonSerializer.Deserialize<List<GlassMaterial>>(json, options);

                if (!merge)
                {
                    _materials.Clear();
                }

                if (materials != null)
                {
                    foreach (var material in materials)
                    {
                        _materials[material.Code] = material;
                    }
                }

                SaveDatabase();
            }
            catch (Exception ex)
            {
                throw new Exception($"导入玻璃数据库失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取制造商列表
        /// </summary>
        public IEnumerable<string> GetManufacturers()
        {
            return _materials.Values
                .Select(m => m.Manufacturer)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct()
                .OrderBy(m => m);
        }

        /// <summary>
        /// 获取材料数量
        /// </summary>
        public int Count => _materials.Count;
    }
}