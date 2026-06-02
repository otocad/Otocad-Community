using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace lcdb.Coating
{
    /// <summary>
    /// 镀膜管理器（单例模式）
    /// </summary>
    [Obsolete("准备删除，不需要的功能")]
    public class CoatingManager
    {
        #region 单例模式

        private static CoatingManager _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static CoatingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new CoatingManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private CoatingManager()
        {
            Initialize();
        }

        #endregion

        #region 属性

        /// <summary>
        /// 镀膜列表
        /// </summary>
        public List<CoatingSpecification> Coatings { get; private set; } = new List<CoatingSpecification>();

        /// <summary>
        /// 模板列表
        /// </summary>
        public List<CoatingTemplate> Templates { get; private set; } = new List<CoatingTemplate>();

        /// <summary>
        /// 材料库
        /// </summary>
        public List<CoatingMaterial> Materials { get; private set; } = new List<CoatingMaterial>();

        /// <summary>
        /// 当前选中的镀膜
        /// </summary>
        public CoatingSpecification CurrentCoating { get; set; }

        /// <summary>
        /// 数据库引用
        /// </summary>
        public lcdb.Database Database { get; set; }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化管理器
        /// </summary>
        private void Initialize()
        {
            // 加载标准模板
            LoadStandardTemplates();
            
            // 加载材料库
            LoadMaterials();
            
            // 加载用户模板
            LoadUserTemplates();
        }

        /// <summary>
        /// 加载标准模板
        /// </summary>
        private void LoadStandardTemplates()
        {
            Templates.AddRange(CoatingTemplate.CreateStandardTemplates());
        }

        /// <summary>
        /// 加载材料库
        /// </summary>
        private void LoadMaterials()
        {
            // 添加常用光学材料
            Materials.Add(new CoatingMaterial { Name = "MgF2", RefractiveIndex = 1.38, Category = "氟化物" });
            Materials.Add(new CoatingMaterial { Name = "SiO2", RefractiveIndex = 1.46, Category = "氧化物" });
            Materials.Add(new CoatingMaterial { Name = "Al2O3", RefractiveIndex = 1.62, Category = "氧化物" });
            Materials.Add(new CoatingMaterial { Name = "HfO2", RefractiveIndex = 1.95, Category = "氧化物" });
            Materials.Add(new CoatingMaterial { Name = "Ta2O5", RefractiveIndex = 2.15, Category = "氧化物" });
            Materials.Add(new CoatingMaterial { Name = "TiO2", RefractiveIndex = 2.35, Category = "氧化物" });
            Materials.Add(new CoatingMaterial { Name = "Nb2O5", RefractiveIndex = 2.25, Category = "氧化物" });
            Materials.Add(new CoatingMaterial { Name = "ZrO2", RefractiveIndex = 2.10, Category = "氧化物" });
            Materials.Add(new CoatingMaterial { Name = "CeO2", RefractiveIndex = 2.20, Category = "氧化物" });
            Materials.Add(new CoatingMaterial { Name = "Y2O3", RefractiveIndex = 1.80, Category = "氧化物" });
            Materials.Add(new CoatingMaterial { Name = "LaF3", RefractiveIndex = 1.59, Category = "氟化物" });
            Materials.Add(new CoatingMaterial { Name = "CaF2", RefractiveIndex = 1.43, Category = "氟化物" });
            Materials.Add(new CoatingMaterial { Name = "BaF2", RefractiveIndex = 1.47, Category = "氟化物" });
            Materials.Add(new CoatingMaterial { Name = "AlF3", RefractiveIndex = 1.35, Category = "氟化物" });
            Materials.Add(new CoatingMaterial { Name = "Si", RefractiveIndex = 3.50, Category = "半导体" });
            Materials.Add(new CoatingMaterial { Name = "Ge", RefractiveIndex = 4.00, Category = "半导体" });
            Materials.Add(new CoatingMaterial { Name = "ZnS", RefractiveIndex = 2.30, Category = "硫化物" });
            Materials.Add(new CoatingMaterial { Name = "ZnSe", RefractiveIndex = 2.50, Category = "硒化物" });
        }

        /// <summary>
        /// 加载用户模板
        /// </summary>
        private void LoadUserTemplates()
        {
            // TODO: 从文件或数据库加载用户保存的模板
        }

        #endregion

        #region 镀膜管理

        /// <summary>
        /// 添加镀膜
        /// </summary>
        public void AddCoating(CoatingSpecification coating)
        {
            if (coating != null && !Coatings.Any(c => c.Id == coating.Id))
            {
                Coatings.Add(coating);
                CurrentCoating = coating;
            }
        }

        /// <summary>
        /// 删除镀膜
        /// </summary>
        public bool RemoveCoating(string coatingId)
        {
            var coating = Coatings.FirstOrDefault(c => c.Id == coatingId);
            if (coating != null)
            {
                Coatings.Remove(coating);
                if (CurrentCoating?.Id == coatingId)
                {
                    CurrentCoating = Coatings.FirstOrDefault();
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取镀膜
        /// </summary>
        public CoatingSpecification GetCoating(string coatingId)
        {
            return Coatings.FirstOrDefault(c => c.Id == coatingId);
        }

        /// <summary>
        /// 获取表面的镀膜
        /// </summary>
        public List<CoatingSpecification> GetCoatingsForSurface(string surfaceId)
        {
            return Coatings.Where(c => c.SurfaceId == surfaceId).ToList();
        }

        /// <summary>
        /// 获取元件的镀膜
        /// </summary>
        public List<CoatingSpecification> GetCoatingsForComponent(string componentId)
        {
            return Coatings.Where(c => c.ComponentId == componentId).ToList();
        }

        /// <summary>
        /// 更新镀膜
        /// </summary>
        public void UpdateCoating(CoatingSpecification coating)
        {
            var existing = Coatings.FirstOrDefault(c => c.Id == coating.Id);
            if (existing != null)
            {
                var index = Coatings.IndexOf(existing);
                Coatings[index] = coating;
                coating.UpdatedAt = DateTime.Now;
            }
        }

        /// <summary>
        /// 清空所有镀膜
        /// </summary>
        public void ClearCoatings()
        {
            Coatings.Clear();
            CurrentCoating = null;
        }

        #endregion

        #region 模板管理

        /// <summary>
        /// 添加模板
        /// </summary>
        public void AddTemplate(CoatingTemplate template)
        {
            if (template != null && !Templates.Any(t => t.Id == template.Id))
            {
                template.IsSystemTemplate = false;
                Templates.Add(template);
                SaveUserTemplates();
            }
        }

        /// <summary>
        /// 删除模板
        /// </summary>
        public bool RemoveTemplate(string templateId)
        {
            var template = Templates.FirstOrDefault(t => t.Id == templateId);
            if (template != null && !template.IsSystemTemplate)
            {
                Templates.Remove(template);
                SaveUserTemplates();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取模板
        /// </summary>
        public CoatingTemplate GetTemplate(string templateId)
        {
            return Templates.FirstOrDefault(t => t.Id == templateId);
        }

        /// <summary>
        /// 获取模板（按名称）
        /// </summary>
        public CoatingTemplate GetTemplateByName(string name)
        {
            return Templates.FirstOrDefault(t => t.Name == name);
        }

        /// <summary>
        /// 获取分类模板
        /// </summary>
        public List<CoatingTemplate> GetTemplatesByCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
                return Templates;
            return Templates.Where(t => t.Category == category).ToList();
        }

        /// <summary>
        /// 获取类型模板
        /// </summary>
        public List<CoatingTemplate> GetTemplatesByType(CoatingType type)
        {
            return Templates.Where(t => t.Type == type).ToList();
        }

        /// <summary>
        /// 保存当前镀膜为模板
        /// </summary>
        public CoatingTemplate SaveAsTemplate(CoatingSpecification coating, string templateName)
        {
            if (coating == null) return null;

            var template = new CoatingTemplate
            {
                Name = templateName,
                Description = coating.Description,
                Type = coating.Type,
                Category = "用户",
                WavelengthStart = coating.WavelengthStart,
                WavelengthEnd = coating.WavelengthEnd,
                DesignWavelength = coating.DesignWavelength,
                IncidentAngle = coating.IncidentAngle,
                Polarization = coating.Polarization,
                AverageReflectanceTarget = coating.AverageReflectanceTarget,
                MaximumReflectanceTarget = coating.MaximumReflectanceTarget,
                AverageTransmittanceTarget = coating.AverageTransmittanceTarget,
                MinimumTransmittanceTarget = coating.MinimumTransmittanceTarget,
                ProcessType = coating.ProcessType,
                SubstrateTemperature = coating.SubstrateTemperature,
                DepositionRate = coating.DepositionRate,
                Pressure = coating.Pressure,
                Author = Environment.UserName,
                IsSystemTemplate = false
            };

            // 复制膜层
            foreach (var layer in coating.Layers)
            {
                template.Layers.Add(layer.Clone());
            }

            AddTemplate(template);
            return template;
        }

        /// <summary>
        /// 保存用户模板
        /// </summary>
        private void SaveUserTemplates()
        {
            // TODO: 保存用户模板到文件或数据库
        }

        #endregion

        #region 应用和验证

        /// <summary>
        /// 应用模板到镀膜
        /// </summary>
        public void ApplyTemplate(CoatingSpecification coating, CoatingTemplate template)
        {
            if (coating != null && template != null)
            {
                coating.LoadFromTemplate(template);
                template.UsageCount++;
            }
        }

        /// <summary>
        /// 批量应用镀膜
        /// </summary>
        public void ApplyCoatingToSurfaces(CoatingSpecification coating, List<string> surfaceIds)
        {
            foreach (var surfaceId in surfaceIds)
            {
                var newCoating = coating.Clone();
                newCoating.SurfaceId = surfaceId;
                AddCoating(newCoating);
            }
        }

        /// <summary>
        /// 验证所有镀膜
        /// </summary>
        public Dictionary<string, List<string>> ValidateAllCoatings()
        {
            var results = new Dictionary<string, List<string>>();
            
            foreach (var coating in Coatings)
            {
                coating.Validate(out List<string> errors);
                if (errors.Count > 0)
                {
                    results[coating.Id] = errors;
                }
            }
            
            return results;
        }

        /// <summary>
        /// 检测冲突
        /// </summary>
        public List<CoatingConflict> DetectConflicts()
        {
            var conflicts = new List<CoatingConflict>();
            
            // 检查同一表面多个镀膜
            var surfaceGroups = Coatings.GroupBy(c => c.SurfaceId);
            foreach (var group in surfaceGroups.Where(g => g.Count() > 1))
            {
                conflicts.Add(new CoatingConflict
                {
                    Type = ConflictType.MultiplCoatings,
                    SurfaceId = group.Key,
                    Message = $"表面{group.Key}有{group.Count()}个镀膜定义"
                });
            }
            
            // 检查材料兼容性
            foreach (var coating in Coatings)
            {
                for (int i = 0; i < coating.Layers.Count - 1; i++)
                {
                    if (!CheckMaterialCompatibility(coating.Layers[i].Material, coating.Layers[i + 1].Material))
                    {
                        conflicts.Add(new CoatingConflict
                        {
                            Type = ConflictType.MaterialIncompatibility,
                            CoatingId = coating.Id,
                            Message = $"第{i + 1}层和第{i + 2}层材料不兼容"
                        });
                    }
                }
            }
            
            return conflicts;
        }

        /// <summary>
        /// 检查材料兼容性
        /// </summary>
        private bool CheckMaterialCompatibility(string material1, string material2)
        {
            // 简化的兼容性检查
            // 实际应用中需要更详细的材料兼容性数据库
            
            // 氟化物和氧化物之间需要过渡层
            bool mat1IsFluoride = material1.Contains("F");
            bool mat2IsFluoride = material2.Contains("F");
            bool mat1IsOxide = material1.Contains("O");
            bool mat2IsOxide = material2.Contains("O");
            
            if ((mat1IsFluoride && mat2IsOxide) || (mat1IsOxide && mat2IsFluoride))
            {
                // 氟化物和氧化物直接接触可能有兼容性问题
                // 但这里简化处理，允许常见组合
                if ((material1 == "MgF2" && material2 == "SiO2") ||
                    (material2 == "MgF2" && material1 == "SiO2") ||
                    (material1 == "MgF2" && material2 == "TiO2") ||
                    (material2 == "MgF2" && material1 == "TiO2"))
                {
                    return true; // 这些是常见的可兼容组合
                }
            }
            
            return true; // 默认兼容
        }

        #endregion

        #region 成本计算

        /// <summary>
        /// 计算总成本
        /// </summary>
        public decimal CalculateTotalCost()
        {
            decimal total = 0;
            foreach (var coating in Coatings)
            {
                coating.CalculateCost();
                total += coating.TotalCost;
            }
            return total;
        }

        /// <summary>
        /// 按表面分组计算成本
        /// </summary>
        public Dictionary<string, decimal> CalculateCostBySurface()
        {
            var costs = new Dictionary<string, decimal>();
            
            foreach (var coating in Coatings)
            {
                coating.CalculateCost();
                if (!costs.ContainsKey(coating.SurfaceId))
                {
                    costs[coating.SurfaceId] = 0;
                }
                costs[coating.SurfaceId] += coating.TotalCost;
            }
            
            return costs;
        }

        #endregion

        #region 导入导出

        /// <summary>
        /// 导出到JSON
        /// </summary>
        public string ExportToJson()
        {
            // TODO: 实现JSON导出
            return "";
        }

        /// <summary>
        /// 从JSON导入
        /// </summary>
        public void ImportFromJson(string json)
        {
            // TODO: 实现JSON导入
        }

        /// <summary>
        /// 导出规格书
        /// </summary>
        public string GenerateSpecificationDocument(CoatingSpecification coating)
        {
            // TODO: 生成详细的镀膜规格文档
            return "";
        }

        #endregion
    }

    /// <summary>
    /// 镀膜材料
    /// </summary>
    public class CoatingMaterial
    {
        public string Name { get; set; }
        public double RefractiveIndex { get; set; }
        public string Category { get; set; }
        public double Density { get; set; }
        public double MeltingPoint { get; set; }
        public double EvaporationTemperature { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// 镀膜冲突
    /// </summary>
    public class CoatingConflict
    {
        public ConflictType Type { get; set; }
        public string CoatingId { get; set; }
        public string SurfaceId { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// 冲突类型
    /// </summary>
    public enum ConflictType
    {
        MultiplCoatings,
        MaterialIncompatibility,
        StressOverload,
        ThicknessExceeded
    }
}
