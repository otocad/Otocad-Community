using System;
using System.Collections.Generic;
using System.Linq;
using lcdb.Colors;
using lcdb.Annotation;

namespace lcdb.Standards
{
    /// <summary>
    /// 绘图标准管理器
    /// </summary>
    public static class DrawingStandardManager
    {
        #region Private Fields

        private static Dictionary<string, DrawingStandard> _standards;
        private static Dictionary<string, StandardTemplate> _templates;

        #endregion

        #region Static Constructor

        static DrawingStandardManager()
        {
            Initialize();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// 可用的绘图标准
        /// </summary>
        public static IReadOnlyDictionary<string, DrawingStandard> AvailableStandards
        {
            get { return _standards; }
        }

        /// <summary>
        /// 可用的标准模板
        /// </summary>
        public static IReadOnlyDictionary<string, StandardTemplate> AvailableTemplates
        {
            get { return _templates; }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化标准管理器
        /// </summary>
        private static void Initialize()
        {
            _standards = new Dictionary<string, DrawingStandard>();
            _templates = new Dictionary<string, StandardTemplate>();

            LoadBuiltInStandards();
            CreateStandardTemplates();
        }

        /// <summary>
        /// 加载内置标准
        /// </summary>
        private static void LoadBuiltInStandards()
        {
            // 加载ISO标准
            var isoStandard = new ISODrawingStandard();
            _standards["ISO"] = isoStandard;

            // 加载GB国标
            var gbStandard = new GBDrawingStandard();
            _standards["GB"] = gbStandard;
        }

        /// <summary>
        /// 创建标准模板
        /// </summary>
        private static void CreateStandardTemplates()
        {
            // ISO技术绘图模板
            _templates["ISO-Technical"] = new StandardTemplate
            {
                Name = "ISO-Technical",
                DisplayName = "ISO技术绘图",
                Description = "符合ISO标准的技术绘图模板",
                Standard = _standards["ISO"],
                PaperFormat = ISOPaperFormat.A3,
                DefaultTextStyle = "Standard",
                DefaultLineStyle = "Outline",
                DefaultColorScheme = "Basic",
                RecommendedLayers = new[] { "Outline", "Hidden", "Center", "Dimension", "Text" }
            };

            // ISO光学设计模板
            _templates["ISO-Optical"] = new StandardTemplate
            {
                Name = "ISO-Optical",
                DisplayName = "ISO光学设计",
                Description = "符合ISO 10110标准的光学设计模板",
                Standard = _standards["ISO"],
                PaperFormat = ISOPaperFormat.A3,
                DefaultTextStyle = "Standard",
                DefaultLineStyle = "Outline",
                DefaultColorScheme = "Basic",
                RecommendedLayers = new[] { "LensOutline", "OpticalAxis", "CoatingMark", "Dimension", "Text" }
            };

            // GB机械绘图模板
            _templates["GB-Mechanical"] = new StandardTemplate
            {
                Name = "GB-Mechanical",
                DisplayName = "GB机械绘图",
                Description = "符合GB国标的机械绘图模板",
                Standard = _standards["GB"],
                PaperFormat = GBPaperFormat.A3,
                DefaultTextStyle = "Standard",
                DefaultLineStyle = "Outline",
                DefaultColorScheme = "Basic",
                RecommendedLayers = new[] { "Outline", "Hidden", "Center", "Dimension", "Text", "Thread", "Tolerance" }
            };

            // GB光学设计模板
            _templates["GB-Optical"] = new StandardTemplate
            {
                Name = "GB-Optical",
                DisplayName = "GB光学设计",
                Description = "符合GB国标的光学设计模板",
                Standard = _standards["GB"],
                PaperFormat = GBPaperFormat.A3,
                DefaultTextStyle = "Standard",
                DefaultLineStyle = "Outline",
                DefaultColorScheme = "Basic",
                RecommendedLayers = new[] { "LensOutline", "OpticalAxis", "CoatingMark", "Dimension", "Text", "TechnicalReq" }
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 获取绘图标准
        /// </summary>
        /// <param name="standardName">标准名称</param>
        /// <returns>绘图标准，如果不存在则返回null</returns>
        public static DrawingStandard GetStandard(string standardName)
        {
            return _standards.ContainsKey(standardName) ? _standards[standardName] : null;
        }

        /// <summary>
        /// 获取标准模板
        /// </summary>
        /// <param name="templateName">模板名称</param>
        /// <returns>标准模板，如果不存在则返回null</returns>
        public static StandardTemplate GetTemplate(string templateName)
        {
            return _templates.ContainsKey(templateName) ? _templates[templateName] : null;
        }

        /// <summary>
        /// 应用标准到数据库
        /// </summary>
        /// <param name="database">目标数据库</param>
        /// <param name="standardName">标准名称</param>
        /// <param name="overwriteExisting">是否覆盖已存在的项</param>
        /// <returns>是否应用成功</returns>
        public static bool ApplyStandardToDatabase(Database database, string standardName, bool overwriteExisting = false)
        {
            var standard = GetStandard(standardName);
            if (standard == null) return false;

            try
            {
                standard.ApplyToDatabase(database, overwriteExisting);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 应用模板到数据库
        /// </summary>
        /// <param name="database">目标数据库</param>
        /// <param name="templateName">模板名称</param>
        /// <param name="overwriteExisting">是否覆盖已存在的项</param>
        /// <returns>应用结果</returns>
        public static TemplateApplicationResult ApplyTemplateToDatabase(Database database, string templateName, bool overwriteExisting = false)
        {
            var template = GetTemplate(templateName);
            if (template == null)
            {
                return new TemplateApplicationResult
                {
                    Success = false,
                    ErrorMessage = $"找不到模板: {templateName}"
                };
            }

            try
            {
                // 应用标准
                template.Standard.ApplyToDatabase(database, overwriteExisting);

                // 设置默认样式
                if (!string.IsNullOrEmpty(template.DefaultTextStyle))
                {
                    database.textStyleTable.SetCurrent(template.Standard.Name + "-" + template.DefaultTextStyle);
                }

                // 创建推荐图层
                if (template.RecommendedLayers != null)
                {
                    foreach (var layerName in template.RecommendedLayers)
                    {
                        if (template.Standard.StandardLayers.ContainsKey(layerName))
                        {
                            var standardLayer = template.Standard.StandardLayers[layerName];
                            if (!database.layerTable.Has(layerName) || overwriteExisting)
                            {
                                if (overwriteExisting && database.layerTable.Has(layerName))
                                {
                                    var existingLayer = database.layerTable[layerName];
                                    if (existingLayer != null)
                                        database.layerTable.Remove(existingLayer);
                                }

                                var layer = new Layer(layerName)
                                {
                                    color = standardLayer.Color,
                                    lineWeight = standardLayer.LineWeight,
                                    lineType = standardLayer.LineType
                                };
                                database.layerTable.Add(layer);
                            }
                        }
                    }
                }

                return new TemplateApplicationResult
                {
                    Success = true,
                    Template = template,
                    AppliedStandard = template.Standard.Name
                };
            }
            catch (Exception ex)
            {
                return new TemplateApplicationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 验证数据库是否符合指定标准
        /// </summary>
        /// <param name="database">数据库</param>
        /// <param name="standardName">标准名称</param>
        /// <returns>验证结果</returns>
        public static DatabaseStandardValidationResult ValidateDatabase(Database database, string standardName)
        {
            var standard = GetStandard(standardName);
            if (standard == null)
            {
                return new DatabaseStandardValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { $"未找到标准: {standardName}" }
                };
            }

            var result = new DatabaseStandardValidationResult();

            // 验证文本样式
            foreach (var requiredStyle in standard.TextStyles.Keys)
            {
                var styleName = standard.Name + "-" + requiredStyle;
                if (!database.textStyleTable.Has(styleName))
                {
                    result.Warnings.Add($"缺少推荐的文本样式: {styleName}");
                }
            }

            // 验证图层
            foreach (var requiredLayer in standard.StandardLayers.Keys)
            {
                if (!database.layerTable.Has(requiredLayer))
                {
                    result.Warnings.Add($"缺少推荐的图层: {requiredLayer}");
                }
            }

            // 验证标准本身
            var standardValidation = standard.Validate();
            result.Errors.AddRange(standardValidation.Errors);
            result.Warnings.AddRange(standardValidation.Warnings);

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// 获取推荐的线型样式
        /// </summary>
        /// <param name="standardName">标准名称</param>
        /// <param name="usage">用途</param>
        /// <returns>推荐的线型样式</returns>
        public static LineStyle GetRecommendedLineStyle(string standardName, string usage)
        {
            var standard = GetStandard(standardName);
            return standard?.GetLineStyle(usage);
        }

        /// <summary>
        /// 获取推荐的颜色方案
        /// </summary>
        /// <param name="standardName">标准名称</param>
        /// <param name="usage">用途</param>
        /// <returns>推荐的颜色方案</returns>
        public static ColorScheme GetRecommendedColorScheme(string standardName, string usage)
        {
            var standard = GetStandard(standardName);
            return standard?.GetColorScheme(usage);
        }

        /// <summary>
        /// 获取推荐的镀膜标记样式
        /// </summary>
        /// <param name="standardName">标准名称</param>
        /// <param name="coatingType">镀膜类型</param>
        /// <returns>推荐的镀膜标记样式</returns>
        public static CoatingMarkStyle GetRecommendedCoatingMarkStyle(string standardName, CoatingType coatingType)
        {
            var standard = GetStandard(standardName);
            return standard?.GetCoatingMarkStyle(coatingType);
        }

        /// <summary>
        /// 注册自定义标准
        /// </summary>
        /// <param name="standard">自定义标准</param>
        /// <returns>是否注册成功</returns>
        public static bool RegisterCustomStandard(DrawingStandard standard)
        {
            if (standard == null || string.IsNullOrEmpty(standard.Name))
                return false;

            if (_standards.ContainsKey(standard.Name))
                return false;

            _standards[standard.Name] = standard;
            return true;
        }

        /// <summary>
        /// 注册自定义模板
        /// </summary>
        /// <param name="template">自定义模板</param>
        /// <returns>是否注册成功</returns>
        public static bool RegisterCustomTemplate(StandardTemplate template)
        {
            if (template == null || string.IsNullOrEmpty(template.Name))
                return false;

            if (_templates.ContainsKey(template.Name))
                return false;

            _templates[template.Name] = template;
            return true;
        }

        /// <summary>
        /// 获取标准列表信息
        /// </summary>
        /// <returns>标准信息列表</returns>
        public static List<StandardInfo> GetStandardList()
        {
            return _standards.Values.Select(s => new StandardInfo
            {
                Name = s.Name,
                Description = s.Description,
                Version = s.Version,
                LineStyleCount = s.LineStyles.Count,
                ColorSchemeCount = s.ColorSchemes.Count,
                TextStyleCount = s.TextStyles.Count,
                CoatingMarkStyleCount = s.CoatingMarkStyles.Count,
                StandardLayerCount = s.StandardLayers.Count
            }).ToList();
        }

        /// <summary>
        /// 获取模板列表信息
        /// </summary>
        /// <returns>模板信息列表</returns>
        public static List<TemplateInfo> GetTemplateList()
        {
            return _templates.Values.Select(t => new TemplateInfo
            {
                Name = t.Name,
                DisplayName = t.DisplayName,
                Description = t.Description,
                StandardName = t.Standard.Name,
                RecommendedLayerCount = t.RecommendedLayers?.Length ?? 0
            }).ToList();
        }

        #endregion
    }

    /// <summary>
    /// 标准模板定义
    /// </summary>
    public class StandardTemplate
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public DrawingStandard Standard { get; set; }
        public object PaperFormat { get; set; }  // ISOPaperFormat 或 GBPaperFormat
        public string DefaultTextStyle { get; set; }
        public string DefaultLineStyle { get; set; }
        public string DefaultColorScheme { get; set; }
        public string[] RecommendedLayers { get; set; }
    }

    /// <summary>
    /// 模板应用结果
    /// </summary>
    public class TemplateApplicationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public StandardTemplate Template { get; set; }
        public string AppliedStandard { get; set; }
    }

    /// <summary>
    /// 数据库标准验证结果
    /// </summary>
    public class DatabaseStandardValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// 标准信息
    /// </summary>
    public class StandardInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public int LineStyleCount { get; set; }
        public int ColorSchemeCount { get; set; }
        public int TextStyleCount { get; set; }
        public int CoatingMarkStyleCount { get; set; }
        public int StandardLayerCount { get; set; }
    }

    /// <summary>
    /// 模板信息
    /// </summary>
    public class TemplateInfo
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string StandardName { get; set; }
        public int RecommendedLayerCount { get; set; }
    }
}