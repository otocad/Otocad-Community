using System;
using System.Collections.Generic;
using System.Linq;

namespace lcdb
{
    /// <summary>
    /// 文本样式表
    /// </summary>
    public class TextStyleTable : DBTable
    {
        #region Private Fields

        private Dictionary<string, TextStyle> _predefinedStyles;

        #endregion

        #region Constructors

        /// <summary>
        /// 构造函数
        /// </summary>
        public TextStyleTable(Database database)
            : base(database, Database.TextStyleTableId)
        {
            InitializePredefinedStyles();
            AddDefaultStyles();
        }

        #endregion

        #region Properties

        /// <summary>
        /// 类名
        /// </summary>
        public override string className
        {
            get { return "TextStyleTable"; }
        }

        /// <summary>
        /// 当前文本样式
        /// </summary>
        public TextStyle Current { get; set; }

        /// <summary>
        /// 预定义样式字典
        /// </summary>
        public IReadOnlyDictionary<string, TextStyle> PredefinedStyles
        {
            get { return _predefinedStyles; }
        }

        #endregion

        #region Initialization Methods

        /// <summary>
        /// 初始化预定义样式
        /// </summary>
        private void InitializePredefinedStyles()
        {
            _predefinedStyles = new Dictionary<string, TextStyle>
            {
                { TextStyle.DefaultName, TextStyle.CreateDefault() },
                { TextStyle.ISOStandardName, TextStyle.CreateISOStandard() },
                { TextStyle.GBStandardName, TextStyle.CreateGBStandard() }
            };

            // 添加更多ISO标准样式
            _predefinedStyles.Add("ISO-Title", CreateISOTitleStyle());
            _predefinedStyles.Add("ISO-Dimension", CreateISODimensionStyle());
            _predefinedStyles.Add("ISO-Annotation", CreateISOAnnotationStyle());

            // 添加更多GB国标样式
            _predefinedStyles.Add("GB-Title", CreateGBTitleStyle());
            _predefinedStyles.Add("GB-Dimension", CreateGBDimensionStyle());
            _predefinedStyles.Add("GB-Annotation", CreateGBAnnotationStyle());
        }

        /// <summary>
        /// 添加默认样式到表中
        /// </summary>
        private void AddDefaultStyles()
        {
            // 添加默认样式
            TextStyle defaultStyle = TextStyle.CreateDefault();
            Add(defaultStyle);
            Current = defaultStyle;
        }

        #endregion

        #region ISO Standard Styles

        /// <summary>
        /// 创建ISO标题样式
        /// </summary>
        private TextStyle CreateISOTitleStyle()
        {
            return new TextStyle("ISO-Title", "Arial", FontStyle.Bold)
            {
                Height = 5.0,
                WidthFactor = 1.0,
                ObliqueAngle = 0.0
            };
        }

        /// <summary>
        /// 创建ISO标注样式
        /// </summary>
        private TextStyle CreateISODimensionStyle()
        {
            return new TextStyle("ISO-Dimension", "Arial", FontStyle.Regular)
            {
                Height = 2.0,
                WidthFactor = 1.0,
                ObliqueAngle = 0.0
            };
        }

        /// <summary>
        /// 创建ISO注释样式
        /// </summary>
        private TextStyle CreateISOAnnotationStyle()
        {
            return new TextStyle("ISO-Annotation", "Arial", FontStyle.Italic)
            {
                Height = 1.8,
                WidthFactor = 1.0,
                ObliqueAngle = 0.0
            };
        }

        #endregion

        #region GB Standard Styles

        /// <summary>
        /// 创建GB标题样式
        /// </summary>
        private TextStyle CreateGBTitleStyle()
        {
            return new TextStyle("GB-Title", "SimHei", FontStyle.Bold)
            {
                Height = 7.0,
                WidthFactor = 0.8,
                ObliqueAngle = 0.0
            };
        }

        /// <summary>
        /// 创建GB标注样式
        /// </summary>
        private TextStyle CreateGBDimensionStyle()
        {
            return new TextStyle("GB-Dimension", "SimSun", FontStyle.Regular)
            {
                Height = 2.5,
                WidthFactor = 0.8,
                ObliqueAngle = 0.0
            };
        }

        /// <summary>
        /// 创建GB注释样式
        /// </summary>
        private TextStyle CreateGBAnnotationStyle()
        {
            return new TextStyle("GB-Annotation", "KaiTi", FontStyle.Regular)
            {
                Height = 2.0,
                WidthFactor = 0.8,
                ObliqueAngle = 15.0  // 楷体通常有轻微倾斜
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 添加文本样式
        /// </summary>
        /// <param name="textStyle">文本样式</param>
        /// <returns>是否添加成功</returns>
        public bool Add(TextStyle textStyle)
        {
            if (textStyle == null)
                return false;

            if (Has(textStyle.name))
                return false;

            ObjectId id = base.Add(textStyle);
            return id != ObjectId.Null;
        }

        /// <summary>
        /// 删除文本样式
        /// </summary>
        /// <param name="name">样式名称</param>
        /// <returns>是否删除成功</returns>
        public bool Remove(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            // 不能删除默认样式
            if (name == TextStyle.DefaultName)
                return false;

            // 不能删除当前样式
            if (Current != null && Current.name == name)
                return false;

            TextStyle textStyle = this[name] as TextStyle;
            if (textStyle == null)
                return false;

            try
            {
                base.Remove(textStyle);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取所有ISO标准样式
        /// </summary>
        /// <returns>ISO标准样式列表</returns>
        public List<TextStyle> GetISOStandardStyles()
        {
            return _predefinedStyles.Values
                .Where(style => style.name.StartsWith("ISO-"))
                .ToList();
        }

        /// <summary>
        /// 获取所有GB国标样式
        /// </summary>
        /// <returns>GB国标样式列表</returns>
        public List<TextStyle> GetGBStandardStyles()
        {
            return _predefinedStyles.Values
                .Where(style => style.name.StartsWith("GB-"))
                .ToList();
        }

        /// <summary>
        /// 应用标准样式集合
        /// </summary>
        /// <param name="standardType">标准类型</param>
        public void ApplyStandardStyles(StandardType standardType)
        {
            List<TextStyle> stylesToAdd = new List<TextStyle>();

            switch (standardType)
            {
                case StandardType.ISO:
                    stylesToAdd = GetISOStandardStyles();
                    break;
                case StandardType.GB:
                    stylesToAdd = GetGBStandardStyles();
                    break;
                case StandardType.All:
                    stylesToAdd = _predefinedStyles.Values.ToList();
                    break;
            }

            foreach (var style in stylesToAdd)
            {
                if (!Has(style.name))
                {
                    Add((TextStyle)style.Clone());
                }
            }
        }

        /// <summary>
        /// 创建自定义样式
        /// </summary>
        /// <param name="name">样式名称</param>
        /// <param name="baseStyle">基础样式</param>
        /// <returns>新创建的样式</returns>
        public TextStyle CreateCustomStyle(string name, TextStyle baseStyle = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (Has(name))
                throw new ArgumentException($"样式 '{name}' 已存在");

            TextStyle newStyle;
            if (baseStyle != null)
            {
                newStyle = (TextStyle)baseStyle.Clone();
                newStyle.name = name;
            }
            else
            {
                newStyle = new TextStyle(name, "arial.ttf");
            }

            Add(newStyle);
            return newStyle;
        }

        /// <summary>
        /// 设置当前样式
        /// </summary>
        /// <param name="name">样式名称</param>
        /// <returns>是否设置成功</returns>
        public bool SetCurrent(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            TextStyle style = this[name] as TextStyle;
            if (style == null)
                return false;

            Current = style;
            return true;
        }

        /// <summary>
        /// 获取样式使用统计
        /// </summary>
        /// <returns>样式使用统计字典</returns>
        public Dictionary<string, int> GetStyleUsageStatistics()
        {
            // 这里需要扫描数据库中的所有文本实体来统计样式使用情况
            // 目前返回空字典，实际实现时需要遍历所有Text实体
            return new Dictionary<string, int>();
        }

        #endregion

        #region Override Methods

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new TextStyleTable(database);
        }

        #endregion
    }

    /// <summary>
    /// 标准类型枚举
    /// </summary>
    public enum StandardType
    {
        /// <summary>
        /// ISO标准
        /// </summary>
        ISO,
        /// <summary>
        /// GB国标
        /// </summary>
        GB,
        /// <summary>
        /// 所有标准
        /// </summary>
        All
    }

    /// <summary>
    /// 表记录事件参数
    /// </summary>
    public class TableRecordEventArgs : EventArgs
    {
        public DBTableRecord Record { get; }

        public TableRecordEventArgs(DBTableRecord record)
        {
            Record = record;
        }
    }
}