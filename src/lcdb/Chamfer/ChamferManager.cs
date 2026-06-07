using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;

namespace lcdb.Chamfer
{
    /// <summary>
    /// 倒角管理器（单例模式）
    /// </summary>
    public class ChamferManager
    {
        #region 单例模式

        private static ChamferManager _instance;
        private static readonly object _lock = new object();

        public static ChamferManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ChamferManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private ChamferManager()
        {
            Initialize();
        }

        #endregion

        #region 字段

        private List<ChamferSpecification> _chamfers;
        private List<ChamferTemplate> _templates;
        private Dictionary<string, List<ChamferSpecification>> _componentChamfers;
        private string _dataPath;

        #endregion

        #region 属性

        /// <summary>
        /// 所有倒角规格
        /// </summary>
        public List<ChamferSpecification> Chamfers
        {
            get { return _chamfers; }
        }

        /// <summary>
        /// 所有倒角模板
        /// </summary>
        public List<ChamferTemplate> Templates
        {
            get { return _templates; }
        }

        /// <summary>
        /// 按组件ID分组的倒角
        /// </summary>
        public Dictionary<string, List<ChamferSpecification>> ComponentChamfers
        {
            get { return _componentChamfers; }
        }

        #endregion

        #region 初始化

        private void Initialize()
        {
            _chamfers = new List<ChamferSpecification>();
            _templates = new List<ChamferTemplate>();
            _componentChamfers = new Dictionary<string, List<ChamferSpecification>>();

            // 设置数据路径
            _dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OtoCAD", "Chamfer");
            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
            }

            // 加载标准模板
            LoadStandardTemplates();

            // 加载用户数据
            LoadUserData();
        }

        private void LoadStandardTemplates()
        {
            _templates.AddRange(ChamferTemplate.CreateStandardTemplates());
        }

        private void LoadUserData()
        {
            // 加载用户自定义模板
            var userTemplatesFile = Path.Combine(_dataPath, "UserTemplates.json");
            if (File.Exists(userTemplatesFile))
            {
                try
                {
                    var json = File.ReadAllText(userTemplatesFile);
                    var userTemplates = JsonSerializer.Deserialize<List<ChamferTemplate>>(json);
                    if (userTemplates != null)
                    {
                        _templates.AddRange(userTemplates.Where(t => !t.IsSystem));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"加载用户模板失败: {ex.Message}");
                }
            }

            // 加载倒角数据
            var chamfersFile = Path.Combine(_dataPath, "Chamfers.json");
            if (File.Exists(chamfersFile))
            {
                try
                {
                    var json = File.ReadAllText(chamfersFile);
                    _chamfers = JsonSerializer.Deserialize<List<ChamferSpecification>>(json) ?? new List<ChamferSpecification>();
                    
                    // 重建组件索引
                    RebuildComponentIndex();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"加载倒角数据失败: {ex.Message}");
                }
            }
        }

        #endregion

        #region 倒角管理

        /// <summary>
        /// 添加倒角
        /// </summary>
        public void AddChamfer(ChamferSpecification chamfer)
        {
            if (chamfer == null) return;

            _chamfers.Add(chamfer);

            // 更新组件索引
            if (!string.IsNullOrEmpty(chamfer.ComponentId))
            {
                if (!_componentChamfers.ContainsKey(chamfer.ComponentId))
                {
                    _componentChamfers[chamfer.ComponentId] = new List<ChamferSpecification>();
                }
                _componentChamfers[chamfer.ComponentId].Add(chamfer);
            }

            SaveData();
        }

        /// <summary>
        /// 更新倒角
        /// </summary>
        public void UpdateChamfer(ChamferSpecification chamfer)
        {
            if (chamfer == null) return;

            var existing = _chamfers.FirstOrDefault(c => c.Id == chamfer.Id);
            if (existing != null)
            {
                var index = _chamfers.IndexOf(existing);
                _chamfers[index] = chamfer;

                // 更新组件索引
                RebuildComponentIndex();

                SaveData();
            }
        }

        /// <summary>
        /// 删除倒角
        /// </summary>
        public void RemoveChamfer(Guid chamferId)
        {
            var chamfer = _chamfers.FirstOrDefault(c => c.Id == chamferId);
            if (chamfer != null)
            {
                _chamfers.Remove(chamfer);

                // 更新组件索引
                if (!string.IsNullOrEmpty(chamfer.ComponentId) && _componentChamfers.ContainsKey(chamfer.ComponentId))
                {
                    _componentChamfers[chamfer.ComponentId].Remove(chamfer);
                    if (_componentChamfers[chamfer.ComponentId].Count == 0)
                    {
                        _componentChamfers.Remove(chamfer.ComponentId);
                    }
                }

                SaveData();
            }
        }

        /// <summary>
        /// 获取倒角
        /// </summary>
        public ChamferSpecification GetChamfer(Guid chamferId)
        {
            return _chamfers.FirstOrDefault(c => c.Id == chamferId);
        }

        /// <summary>
        /// 获取组件的所有倒角
        /// </summary>
        public List<ChamferSpecification> GetComponentChamfers(string componentId)
        {
            if (string.IsNullOrEmpty(componentId)) return new List<ChamferSpecification>();

            return _componentChamfers.ContainsKey(componentId) 
                ? new List<ChamferSpecification>(_componentChamfers[componentId])
                : new List<ChamferSpecification>();
        }

        /// <summary>
        /// 获取边缘的倒角
        /// </summary>
        public ChamferSpecification GetEdgeChamfer(string edgeId)
        {
            if (string.IsNullOrEmpty(edgeId)) return null;

            return _chamfers.FirstOrDefault(c => c.EdgeId == edgeId);
        }

        /// <summary>
        /// 批量添加倒角
        /// </summary>
        public void AddChamferBatch(List<ChamferSpecification> chamfers)
        {
            if (chamfers == null || chamfers.Count == 0) return;

            _chamfers.AddRange(chamfers);
            RebuildComponentIndex();
            SaveData();
        }

        /// <summary>
        /// 清除所有倒角
        /// </summary>
        public void ClearAllChamfers()
        {
            _chamfers.Clear();
            _componentChamfers.Clear();
            SaveData();
        }

        #endregion

        #region 模板管理

        /// <summary>
        /// 添加模板
        /// </summary>
        public void AddTemplate(ChamferTemplate template)
        {
            if (template == null) return;

            template.IsSystem = false;
            _templates.Add(template);
            SaveData();
        }

        /// <summary>
        /// 更新模板
        /// </summary>
        public void UpdateTemplate(ChamferTemplate template)
        {
            if (template == null || template.IsSystem) return;

            var existing = _templates.FirstOrDefault(t => t.Id == template.Id && !t.IsSystem);
            if (existing != null)
            {
                var index = _templates.IndexOf(existing);
                _templates[index] = template;
                SaveData();
            }
        }

        /// <summary>
        /// 删除模板
        /// </summary>
        public void RemoveTemplate(Guid templateId)
        {
            var template = _templates.FirstOrDefault(t => t.Id == templateId && !t.IsSystem);
            if (template != null)
            {
                _templates.Remove(template);
                SaveData();
            }
        }

        /// <summary>
        /// 获取模板
        /// </summary>
        public ChamferTemplate GetTemplate(Guid templateId)
        {
            return _templates.FirstOrDefault(t => t.Id == templateId);
        }

        /// <summary>
        /// 根据名称获取模板
        /// </summary>
        public ChamferTemplate GetTemplateByName(string name)
        {
            return _templates.FirstOrDefault(t => t.Name == name);
        }

        /// <summary>
        /// 从规格创建模板
        /// </summary>
        public ChamferTemplate SaveAsTemplate(ChamferSpecification spec, string templateName)
        {
            if (spec == null || string.IsNullOrWhiteSpace(templateName)) return null;

            var template = new ChamferTemplate
            {
                Name = templateName,
                Description = spec.Remarks,
                Type = spec.Type,
                Position = spec.Position,
                Size = spec.Size,
                Angle = spec.Angle,
                Depth = spec.Depth,
                Finish = spec.Finish,
                Difficulty = spec.Difficulty,
                ToleranceGrade = spec.ToleranceGrade,
                Roughness = spec.Roughness,
                RequiresProtection = spec.RequiresProtection,
                ProcessingNotes = spec.ProcessingNotes,
                InspectionRequirements = spec.InspectionRequirements,
                IsStandard = spec.IsStandard,
                IsSystem = false,
                Category = "用户自定义"
            };

            AddTemplate(template);
            return template;
        }

        /// <summary>
        /// 获取按类别分组的模板
        /// </summary>
        public Dictionary<string, List<ChamferTemplate>> GetTemplatesByCategory()
        {
            return _templates.GroupBy(t => t.Category ?? "其他")
                            .ToDictionary(g => g.Key, g => g.ToList());
        }

        #endregion

        #region 统计分析

        /// <summary>
        /// 获取倒角统计信息
        /// </summary>
        public ChamferStatistics GetStatistics()
        {
            var stats = new ChamferStatistics
            {
                TotalCount = _chamfers.Count,
                TypeDistribution = _chamfers.GroupBy(c => c.Type)
                                           .ToDictionary(g => g.Key, g => g.Count()),
                PositionDistribution = _chamfers.GroupBy(c => c.Position)
                                               .ToDictionary(g => g.Key, g => g.Count()),
                FinishDistribution = _chamfers.GroupBy(c => c.Finish)
                                             .ToDictionary(g => g.Key, g => g.Count()),
                TotalCost = _chamfers.Sum(c => c.ProcessingCost),
                TotalProcessingTime = _chamfers.Sum(c => c.ProcessingTime),
                AverageSize = _chamfers.Any() ? _chamfers.Average(c => c.Size) : 0,
                ComponentCount = _componentChamfers.Count
            };

            return stats;
        }

        /// <summary>
        /// 获取最常用的模板
        /// </summary>
        public List<ChamferTemplate> GetMostUsedTemplates(int count = 5)
        {
            return _templates.OrderByDescending(t => t.UsageCount)
                           .Take(count)
                           .ToList();
        }

        #endregion

        #region 数据持久化

        private void SaveData()
        {
            try
            {
                // 保存用户模板
                var userTemplates = _templates.Where(t => !t.IsSystem).ToList();
                var templatesJson = JsonSerializer.Serialize(userTemplates, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(_dataPath, "UserTemplates.json"), templatesJson);

                // 保存倒角数据
                var chamfersJson = JsonSerializer.Serialize(_chamfers, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(_dataPath, "Chamfers.json"), chamfersJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存数据失败: {ex.Message}");
            }
        }

        private void RebuildComponentIndex()
        {
            _componentChamfers.Clear();
            foreach (var chamfer in _chamfers)
            {
                if (!string.IsNullOrEmpty(chamfer.ComponentId))
                {
                    if (!_componentChamfers.ContainsKey(chamfer.ComponentId))
                    {
                        _componentChamfers[chamfer.ComponentId] = new List<ChamferSpecification>();
                    }
                    _componentChamfers[chamfer.ComponentId].Add(chamfer);
                }
            }
        }

        #endregion

        #region 导入导出

        /// <summary>
        /// 导出倒角数据
        /// </summary>
        public string ExportChamfers(List<Guid> chamferIds = null)
        {
            var toExport = chamferIds != null
                ? _chamfers.Where(c => chamferIds.Contains(c.Id)).ToList()
                : _chamfers;

            return JsonSerializer.Serialize(toExport, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// 导入倒角数据
        /// </summary>
        public int ImportChamfers(string json, bool merge = true)
        {
            try
            {
                var imported = JsonSerializer.Deserialize<List<ChamferSpecification>>(json);
                if (imported == null || imported.Count == 0) return 0;

                if (!merge)
                {
                    _chamfers.Clear();
                    _componentChamfers.Clear();
                }

                // 生成新的ID避免冲突
                foreach (var chamfer in imported)
                {
                    chamfer.Id = Guid.NewGuid();
                    chamfer.CreatedDate = DateTime.Now;
                    chamfer.ModifiedDate = DateTime.Now;
                }

                _chamfers.AddRange(imported);
                RebuildComponentIndex();
                SaveData();

                return imported.Count;
            }
            catch
            {
                return 0;
            }
        }

        #endregion
    }

    /// <summary>
    /// 倒角统计信息
    /// </summary>
    public class ChamferStatistics
    {
        public int TotalCount { get; set; }
        public Dictionary<ChamferType, int> TypeDistribution { get; set; }
        public Dictionary<ChamferPosition, int> PositionDistribution { get; set; }
        public Dictionary<SurfaceFinish, int> FinishDistribution { get; set; }
        public double TotalCost { get; set; }
        public double TotalProcessingTime { get; set; }
        public double AverageSize { get; set; }
        public int ComponentCount { get; set; }
    }
}