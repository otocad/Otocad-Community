using System;
using System.Collections.Generic;
using System.Linq;
using lcdb.Colors;
using lcdb.Annotation;

namespace lcdb.Standards
{
    /// <summary>
    /// 绘图标准基类
    /// </summary>
    public abstract class DrawingStandard
    {
        #region Properties

        /// <summary>
        /// 标准名称
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// 标准描述
        /// </summary>
        public string Description { get; protected set; }

        /// <summary>
        /// 标准版本
        /// </summary>
        public string Version { get; protected set; }

        /// <summary>
        /// 线型样式字典
        /// </summary>
        public Dictionary<string, LineStyle> LineStyles { get; protected set; }

        /// <summary>
        /// 颜色方案字典
        /// </summary>
        public Dictionary<string, ColorScheme> ColorSchemes { get; protected set; }

        /// <summary>
        /// 文本样式字典
        /// </summary>
        public Dictionary<string, TextStyle> TextStyles { get; protected set; }

        /// <summary>
        /// 镀膜标记样式字典
        /// </summary>
        public Dictionary<string, CoatingMarkStyle> CoatingMarkStyles { get; protected set; }

        /// <summary>
        /// 标准图层配置
        /// </summary>
        public Dictionary<string, StandardLayer> StandardLayers { get; protected set; }

        #endregion

        #region Constructor

        protected DrawingStandard(string name, string description, string version)
        {
            Name = name;
            Description = description;
            Version = version;

            LineStyles = new Dictionary<string, LineStyle>();
            ColorSchemes = new Dictionary<string, ColorScheme>();
            TextStyles = new Dictionary<string, TextStyle>();
            CoatingMarkStyles = new Dictionary<string, CoatingMarkStyle>();
            StandardLayers = new Dictionary<string, StandardLayer>();

            InitializeStandard();
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// 初始化标准配置
        /// </summary>
        protected abstract void InitializeStandard();

        #endregion

        #region Public Methods

        /// <summary>
        /// 应用标准到数据库
        /// </summary>
        /// <param name="database">目标数据库</param>
        /// <param name="overwriteExisting">是否覆盖已存在的项</param>
        public virtual void ApplyToDatabase(Database database, bool overwriteExisting = false)
        {
            // 应用文本样式
            foreach (var textStyle in TextStyles.Values)
            {
                if (!database.textStyleTable.Has(textStyle.name) || overwriteExisting)
                {
                    if (overwriteExisting && database.textStyleTable.Has(textStyle.name))
                    {
                        database.textStyleTable.Remove(textStyle.name);
                    }
                    database.textStyleTable.Add((TextStyle)textStyle.Clone());
                }
            }

            // 应用图层标准
            foreach (var standardLayer in StandardLayers.Values)
            {
                if (!database.layerTable.Has(standardLayer.Name) || overwriteExisting)
                {
                    if (overwriteExisting && database.layerTable.Has(standardLayer.Name))
                    {
                        var existingLayer = database.layerTable[standardLayer.Name];
                        if (existingLayer != null)
                            database.layerTable.Remove(existingLayer);
                    }

                    var layer = new Layer(standardLayer.Name)
                    {
                        color = standardLayer.Color,
                        lineWeight = standardLayer.LineWeight,
                        lineType = standardLayer.LineType
                    };
                    database.layerTable.Add(layer);
                }
            }
        }

        /// <summary>
        /// 获取指定用途的线型样式
        /// </summary>
        /// <param name="usage">用途</param>
        /// <returns>线型样式</returns>
        public LineStyle GetLineStyle(string usage)
        {
            return LineStyles.ContainsKey(usage) ? LineStyles[usage] : null;
        }

        /// <summary>
        /// 获取指定用途的颜色方案
        /// </summary>
        /// <param name="usage">用途</param>
        /// <returns>颜色方案</returns>
        public ColorScheme GetColorScheme(string usage)
        {
            return ColorSchemes.ContainsKey(usage) ? ColorSchemes[usage] : null;
        }

        /// <summary>
        /// 获取指定用途的文本样式
        /// </summary>
        /// <param name="usage">用途</param>
        /// <returns>文本样式</returns>
        public TextStyle GetTextStyle(string usage)
        {
            return TextStyles.ContainsKey(usage) ? TextStyles[usage] : null;
        }

        /// <summary>
        /// 获取指定类型的镀膜标记样式
        /// </summary>
        /// <param name="coatingType">镀膜类型</param>
        /// <returns>镀膜标记样式</returns>
        public CoatingMarkStyle GetCoatingMarkStyle(CoatingType coatingType)
        {
            string key = coatingType.ToString();
            return CoatingMarkStyles.ContainsKey(key) ? CoatingMarkStyles[key] : null;
        }

        /// <summary>
        /// 验证标准完整性
        /// </summary>
        /// <returns>验证结果</returns>
        public virtual StandardValidationResult Validate()
        {
            var result = new StandardValidationResult();

            // 验证必要的样式是否存在
            string[] requiredLineStyles = { "Outline", "Hidden", "Center", "Dimension" };
            string[] requiredColorSchemes = { "Default", "Highlight", "Dimension" };
            string[] requiredTextStyles = { "Standard", "Title", "Dimension" };

            foreach (var required in requiredLineStyles)
            {
                if (!LineStyles.ContainsKey(required))
                {
                    result.Errors.Add($"缺少必需的线型样式: {required}");
                }
            }

            foreach (var required in requiredColorSchemes)
            {
                if (!ColorSchemes.ContainsKey(required))
                {
                    result.Errors.Add($"缺少必需的颜色方案: {required}");
                }
            }

            foreach (var required in requiredTextStyles)
            {
                if (!TextStyles.ContainsKey(required))
                {
                    result.Errors.Add($"缺少必需的文本样式: {required}");
                }
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 添加线型样式
        /// </summary>
        protected void AddLineStyle(string usage, LineType lineType, LineWeight lineWeight, Color color)
        {
            LineStyles[usage] = new LineStyle(usage, lineType, lineWeight, color);
        }

        /// <summary>
        /// 添加颜色方案
        /// </summary>
        protected void AddColorScheme(string usage, Color primary, Color secondary = default, Color accent = default)
        {
            ColorSchemes[usage] = new ColorScheme(usage, primary, secondary, accent);
        }

        /// <summary>
        /// 添加文本样式
        /// </summary>
        protected void AddTextStyle(string usage, TextStyle textStyle)
        {
            var cloned = (TextStyle)textStyle.Clone();
            cloned.name = $"{Name}-{usage}";
            TextStyles[usage] = cloned;
        }

        /// <summary>
        /// 添加镀膜标记样式
        /// </summary>
        protected void AddCoatingMarkStyle(CoatingType coatingType, CoatingMarkShape shape, Color color, double size = 2.0)
        {
            CoatingMarkStyles[coatingType.ToString()] = new CoatingMarkStyle(coatingType, shape, color, size);
        }

        /// <summary>
        /// 添加标准图层
        /// </summary>
        protected void AddStandardLayer(string name, Color color, LineWeight lineWeight, LineType lineType, string description = "")
        {
            StandardLayers[name] = new StandardLayer(name, color, lineWeight, lineType, description);
        }

        #endregion
    }

    /// <summary>
    /// 标准验证结果
    /// </summary>
    public class StandardValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// 标准图层定义
    /// </summary>
    public class StandardLayer
    {
        public string Name { get; set; }
        public Color Color { get; set; }
        public LineWeight LineWeight { get; set; }
        public LineType LineType { get; set; }
        public string Description { get; set; }

        public StandardLayer(string name, Color color, LineWeight lineWeight, LineType lineType, string description = "")
        {
            Name = name;
            Color = color;
            LineWeight = lineWeight;
            LineType = lineType;
            Description = description;
        }
    }
}