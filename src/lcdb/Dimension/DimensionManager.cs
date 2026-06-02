using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using LitMath;

namespace OtoCAD.Dimension
{
    /// <summary>
    /// 尺寸标注管理器（单例模式）
    /// </summary>
    public class DimensionManager
    {
        #region 单例模式

        private static DimensionManager _instance;
        private static readonly object _lock = new object();

        public static DimensionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DimensionManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private DimensionManager()
        {
            Initialize();
        }

        #endregion

        #region 字段

        private List<DimensionSpecification> _dimensions;
        private List<DimensionTemplate> _templates;
        private Dictionary<string, List<DimensionSpecification>> _componentDimensions;
        private DimensionExtractor _extractor;
        private ToleranceOptimizer _optimizer;
        private string _dataPath;

        #endregion

        #region 属性

        /// <summary>
        /// 所有尺寸规格
        /// </summary>
        public List<DimensionSpecification> Dimensions
        {
            get { return _dimensions; }
        }

        /// <summary>
        /// 所有尺寸模板
        /// </summary>
        public List<DimensionTemplate> Templates
        {
            get { return _templates; }
        }

        /// <summary>
        /// 按组件ID分组的尺寸
        /// </summary>
        public Dictionary<string, List<DimensionSpecification>> ComponentDimensions
        {
            get { return _componentDimensions; }
        }

        #endregion

        #region 初始化

        private void Initialize()
        {
            _dimensions = new List<DimensionSpecification>();
            _templates = new List<DimensionTemplate>();
            _componentDimensions = new Dictionary<string, List<DimensionSpecification>>();
            _extractor = new DimensionExtractor();
            _optimizer = new ToleranceOptimizer();

            // 设置数据路径
            _dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OtoCAD", "Dimension");
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
            _templates.AddRange(DimensionTemplate.CreateStandardTemplates());
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
                    var userTemplates = JsonSerializer.Deserialize<List<DimensionTemplate>>(json);
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

            // 加载尺寸数据
            var dimensionsFile = Path.Combine(_dataPath, "Dimensions.json");
            if (File.Exists(dimensionsFile))
            {
                try
                {
                    var json = File.ReadAllText(dimensionsFile);
                    _dimensions = JsonSerializer.Deserialize<List<DimensionSpecification>>(json) ?? new List<DimensionSpecification>();
                    
                    // 重建组件索引
                    RebuildComponentIndex();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"加载尺寸数据失败: {ex.Message}");
                }
            }
        }

        #endregion

        #region 尺寸管理

        /// <summary>
        /// 添加尺寸
        /// </summary>
        public void AddDimension(DimensionSpecification dimension)
        {
            if (dimension == null) return;

            _dimensions.Add(dimension);

            // 更新组件索引
            if (!string.IsNullOrEmpty(dimension.ComponentId))
            {
                if (!_componentDimensions.ContainsKey(dimension.ComponentId))
                {
                    _componentDimensions[dimension.ComponentId] = new List<DimensionSpecification>();
                }
                _componentDimensions[dimension.ComponentId].Add(dimension);
            }

            SaveData();
        }

        /// <summary>
        /// 更新尺寸
        /// </summary>
        public void UpdateDimension(DimensionSpecification dimension)
        {
            if (dimension == null) return;

            var existing = _dimensions.FirstOrDefault(d => d.Id == dimension.Id);
            if (existing != null)
            {
                var index = _dimensions.IndexOf(existing);
                _dimensions[index] = dimension;

                // 更新组件索引
                RebuildComponentIndex();

                SaveData();
            }
        }

        /// <summary>
        /// 删除尺寸
        /// </summary>
        public void RemoveDimension(Guid dimensionId)
        {
            var dimension = _dimensions.FirstOrDefault(d => d.Id == dimensionId);
            if (dimension != null)
            {
                _dimensions.Remove(dimension);

                // 更新组件索引
                if (!string.IsNullOrEmpty(dimension.ComponentId) && _componentDimensions.ContainsKey(dimension.ComponentId))
                {
                    _componentDimensions[dimension.ComponentId].Remove(dimension);
                    if (_componentDimensions[dimension.ComponentId].Count == 0)
                    {
                        _componentDimensions.Remove(dimension.ComponentId);
                    }
                }

                SaveData();
            }
        }

        /// <summary>
        /// 获取尺寸
        /// </summary>
        public DimensionSpecification GetDimension(Guid dimensionId)
        {
            return _dimensions.FirstOrDefault(d => d.Id == dimensionId);
        }

        /// <summary>
        /// 获取组件的所有尺寸
        /// </summary>
        public List<DimensionSpecification> GetComponentDimensions(string componentId)
        {
            if (string.IsNullOrEmpty(componentId)) return new List<DimensionSpecification>();

            return _componentDimensions.ContainsKey(componentId) 
                ? new List<DimensionSpecification>(_componentDimensions[componentId])
                : new List<DimensionSpecification>();
        }

        /// <summary>
        /// 批量添加尺寸
        /// </summary>
        public void AddDimensionBatch(List<DimensionSpecification> dimensions)
        {
            if (dimensions == null || dimensions.Count == 0) return;

            _dimensions.AddRange(dimensions);
            RebuildComponentIndex();
            SaveData();
        }

        #endregion

        #region 自动提取功能

        /// <summary>
        /// 从光学元件自动提取尺寸
        /// </summary>
        public List<DimensionSpecification> AutoExtractDimensions(object opticalElement)
        {
            return _extractor.ExtractDimensions(opticalElement);
        }

        /// <summary>
        /// 智能推荐公差
        /// </summary>
        public ToleranceSpecification RecommendTolerance(DimensionType type, double nominalValue)
        {
            return _extractor.RecommendTolerance(type, nominalValue);
        }

        #endregion

        #region 公差优化

        /// <summary>
        /// 计算公差链累积
        /// </summary>
        public ToleranceChainResult CalculateToleranceChain(List<DimensionSpecification> chain)
        {
            return _optimizer.CalculateStackUp(chain);
        }

        /// <summary>
        /// 优化公差分配
        /// </summary>
        public List<DimensionSpecification> OptimizeToleranceAllocation(double targetTolerance, List<DimensionSpecification> chain)
        {
            return _optimizer.OptimizeAllocation(targetTolerance, chain);
        }

        #endregion

        #region 模板管理

        /// <summary>
        /// 添加模板
        /// </summary>
        public void AddTemplate(DimensionTemplate template)
        {
            if (template == null) return;

            template.IsSystem = false;
            _templates.Add(template);
            SaveData();
        }

        /// <summary>
        /// 获取模板
        /// </summary>
        public DimensionTemplate GetTemplate(Guid templateId)
        {
            return _templates.FirstOrDefault(t => t.Id == templateId);
        }

        /// <summary>
        /// 获取模板（按名称）
        /// </summary>
        public DimensionTemplate GetTemplateByName(string name)
        {
            return _templates.FirstOrDefault(t => t.Name == name);
        }

        /// <summary>
        /// 应用模板
        /// </summary>
        public List<DimensionSpecification> ApplyTemplate(DimensionTemplate template, string componentId)
        {
            if (template == null) return new List<DimensionSpecification>();

            var dimensions = template.GenerateDimensions(componentId);
            AddDimensionBatch(dimensions);
            return dimensions;
        }

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量设置公差等级
        /// </summary>
        public void SetToleranceGradeBatch(List<Guid> dimensionIds, ToleranceGrade grade)
        {
            foreach (var id in dimensionIds)
            {
                var dimension = GetDimension(id);
                if (dimension != null && dimension.Tolerance != null)
                {
                    dimension.Tolerance.Grade = grade;
                    // 根据等级重新计算公差值
                    var recommended = RecommendTolerance(dimension.Type, dimension.NominalValue);
                    dimension.Tolerance.Value = recommended.Value;
                    dimension.ModifiedDate = DateTime.Now;
                }
            }
            SaveData();
        }

        /// <summary>
        /// 批量锁定/解锁
        /// </summary>
        public void SetLockedBatch(List<Guid> dimensionIds, bool locked)
        {
            foreach (var id in dimensionIds)
            {
                var dimension = GetDimension(id);
                if (dimension != null)
                {
                    dimension.SetLocked(locked);
                }
            }
            SaveData();
        }

        /// <summary>
        /// 更新所有自动尺寸
        /// </summary>
        public void UpdateAllAutoDimensions(object opticalElement)
        {
            var autoDimensions = _dimensions.Where(d => d.Source == DimensionSource.Auto && !d.IsLocked).ToList();
            var newDimensions = AutoExtractDimensions(opticalElement);

            foreach (var autoDim in autoDimensions)
            {
                var newDim = newDimensions.FirstOrDefault(d => d.Name == autoDim.Name && d.Type == autoDim.Type);
                if (newDim != null)
                {
                    autoDim.NominalValue = newDim.NominalValue;
                    autoDim.ModifiedDate = DateTime.Now;
                }
            }

            SaveData();
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

                // 保存尺寸数据
                var dimensionsJson = JsonSerializer.Serialize(_dimensions, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(_dataPath, "Dimensions.json"), dimensionsJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存数据失败: {ex.Message}");
            }
        }

        private void RebuildComponentIndex()
        {
            _componentDimensions.Clear();
            foreach (var dimension in _dimensions)
            {
                if (!string.IsNullOrEmpty(dimension.ComponentId))
                {
                    if (!_componentDimensions.ContainsKey(dimension.ComponentId))
                    {
                        _componentDimensions[dimension.ComponentId] = new List<DimensionSpecification>();
                    }
                    _componentDimensions[dimension.ComponentId].Add(dimension);
                }
            }
        }

        #endregion

        #region 导入导出

        /// <summary>
        /// 导出尺寸列表
        /// </summary>
        public string ExportDimensions(List<Guid> dimensionIds = null)
        {
            var toExport = dimensionIds != null
                ? _dimensions.Where(d => dimensionIds.Contains(d.Id)).ToList()
                : _dimensions;

            return JsonSerializer.Serialize(toExport, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// 导入尺寸数据
        /// </summary>
        public int ImportDimensions(string json, bool merge = true)
        {
            try
            {
                var imported = JsonSerializer.Deserialize<List<DimensionSpecification>>(json);
                if (imported == null || imported.Count == 0) return 0;

                if (!merge)
                {
                    _dimensions.Clear();
                    _componentDimensions.Clear();
                }

                // 生成新的ID避免冲突
                foreach (var dimension in imported)
                {
                    dimension.Id = Guid.NewGuid();
                    dimension.CreatedDate = DateTime.Now;
                    dimension.ModifiedDate = DateTime.Now;
                }

                _dimensions.AddRange(imported);
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
}