using System;
using System.Collections.Generic;
using lcdb.Colors;
using lcdb.Annotation;

namespace lcdb.Standards
{
    /// <summary>
    /// ISO绘图标准实现
    /// </summary>
    public class ISODrawingStandard : DrawingStandard
    {
        #region Constructor

        public ISODrawingStandard() : base("ISO", "ISO技术制图标准", "2023")
        {
        }

        #endregion

        #region Override Methods

        /// <summary>
        /// 初始化ISO标准配置
        /// </summary>
        protected override void InitializeStandard()
        {
            InitializeLineStyles();
            InitializeColorSchemes();
            InitializeTextStyles();
            InitializeCoatingMarkStyles();
            InitializeStandardLayers();
        }

        #endregion

        #region Private Initialization Methods

        /// <summary>
        /// 初始化ISO线型样式
        /// </summary>
        private void InitializeLineStyles()
        {
            // ISO 128-20标准线型
            AddLineStyle("Outline", LineType.Solid, LineWeight.LineWeight050, Color.FromRGB(0, 0, 0));
            AddLineStyle("Hidden", LineType.Dash, LineWeight.LineWeight025, Color.FromRGB(128, 128, 128));
            AddLineStyle("Center", LineType.DashDot, LineWeight.LineWeight025, Color.FromRGB(0, 128, 0));
            AddLineStyle("Dimension", LineType.Solid, LineWeight.LineWeight020, Color.FromRGB(255, 0, 0));
            AddLineStyle("Section", LineType.Solid, LineWeight.LineWeight080, Color.FromRGB(255, 0, 0));
            AddLineStyle("Leader", LineType.Solid, LineWeight.LineWeight020, Color.FromRGB(0, 0, 255));
            AddLineStyle("Construction", LineType.Dot, LineWeight.LineWeight015, Color.FromRGB(192, 192, 192));
            AddLineStyle("Boundary", LineType.DashDotDot, LineWeight.LineWeight040, Color.FromRGB(128, 0, 128));

            // 特殊线型
            var phantomStyle = new LineStyle("Phantom", LineType.Custom, LineWeight.LineWeight025, Color.FromRGB(255, 128, 0))
            {
                DashPattern = new double[] { 24, 6, 6, 6, 6, 6 },
                Description = "ISO虚实线"
            };
            LineStyles["Phantom"] = phantomStyle;

            var cuttingPlaneStyle = new LineStyle("CuttingPlane", LineType.Custom, LineWeight.LineWeight080, Color.FromRGB(255, 0, 0))
            {
                DashPattern = new double[] { 24, 3, 6, 3 },
                Description = "ISO剖切线"
            };
            LineStyles["CuttingPlane"] = cuttingPlaneStyle;
        }

        /// <summary>
        /// 初始化ISO颜色方案
        /// </summary>
        private void InitializeColorSchemes()
        {
            // ISO 5807标准颜色方案
            var isoBasic = new ColorScheme("ISO-Basic", Color.FromRGB(0, 0, 0))
            {
                SecondaryColor = Color.FromRGB(128, 128, 128),
                AccentColor = Color.FromRGB(255, 0, 0),
                BackgroundColor = Color.FromRGB(255, 255, 255),
                TextColor = Color.FromRGB(0, 0, 0),
                HighlightColor = Color.FromRGB(255, 255, 0),
                GridColor = Color.FromRGB(192, 192, 192),
                SelectionColor = Color.FromRGB(0, 0, 255),
                Description = "ISO基础颜色方案",
                Usage = ColorUsage.Technical
            };
            
            // 添加ISO技术图纸专用颜色
            isoBasic.AddExtendedColor("OutlineThick", Color.FromRGB(0, 0, 0));      // 粗实线
            isoBasic.AddExtendedColor("OutlineThin", Color.FromRGB(64, 64, 64));    // 细实线
            isoBasic.AddExtendedColor("HiddenLine", Color.FromRGB(128, 128, 128));  // 虚线
            isoBasic.AddExtendedColor("CenterLine", Color.FromRGB(0, 128, 0));      // 中心线
            isoBasic.AddExtendedColor("DimensionLine", Color.FromRGB(255, 0, 0));   // 尺寸线
            isoBasic.AddExtendedColor("SectionLine", Color.FromRGB(255, 0, 0));     // 剖面线
            isoBasic.AddExtendedColor("LeaderLine", Color.FromRGB(0, 0, 255));      // 引出线
            isoBasic.AddExtendedColor("ConstructionLine", Color.FromRGB(192, 192, 192)); // 辅助线

            ColorSchemes["Basic"] = isoBasic;

            // ISO打印颜色方案
            var isoPrint = new ColorScheme("ISO-Print", Color.FromRGB(0, 0, 0))
            {
                SecondaryColor = Color.FromRGB(0, 0, 0),
                AccentColor = Color.FromRGB(0, 0, 0),
                BackgroundColor = Color.FromRGB(255, 255, 255),
                TextColor = Color.FromRGB(0, 0, 0),
                HighlightColor = Color.FromRGB(128, 128, 128),
                GridColor = Color.FromRGB(192, 192, 192),
                SelectionColor = Color.FromRGB(128, 128, 128),
                Description = "ISO打印颜色方案（单色）",
                Usage = ColorUsage.Print
            };
            ColorSchemes["Print"] = isoPrint;
        }

        /// <summary>
        /// 初始化ISO文本样式
        /// </summary>
        private void InitializeTextStyles()
        {
            // ISO 3098标准文字
            AddTextStyle("Standard", TextStyleManager.CreateISOStandard());
            AddTextStyle("Title", TextStyleManager.CreateISOTitle());
            AddTextStyle("Subtitle", TextStyleManager.CreateISOSubtitle());
            AddTextStyle("Dimension", TextStyleManager.CreateISODimension());
            AddTextStyle("Annotation", TextStyleManager.CreateISOAnnotation());
            AddTextStyle("Section", TextStyleManager.CreateISOSection());
            AddTextStyle("Detail", TextStyleManager.CreateISODetail());
            AddTextStyle("DrawingNumber", TextStyleManager.CreateISODrawingNumber());
            AddTextStyle("Revision", TextStyleManager.CreateISORevision());
            AddTextStyle("Notes", TextStyleManager.CreateISONotes());

            // 特殊用途文本样式
            var microTextStyle = new TextStyle("ISO-Micro", "Arial", FontStyle.Regular)
            {
                Height = 1.0,
                WidthFactor = 0.8,
                ObliqueAngle = 0.0
            };
            TextStyles["Micro"] = microTextStyle;

            var titleBlockStyle = new TextStyle("ISO-TitleBlock", "Arial", FontStyle.Bold)
            {
                Height = 3.5,
                WidthFactor = 1.0,
                ObliqueAngle = 0.0
            };
            TextStyles["TitleBlock"] = titleBlockStyle;
        }

        /// <summary>
        /// 初始化ISO镀膜标记样式
        /// </summary>
        private void InitializeCoatingMarkStyles()
        {
            // ISO 10110光学元件图纸标准
            var isoStyles = CoatingMarkStyle.CreateISOStandardStyles();
            foreach (var kvp in isoStyles)
            {
                CoatingMarkStyles[kvp.Key.ToString()] = kvp.Value;
            }

            // 添加ISO特殊镀膜标记
            var multiLayerStyle = new CoatingMarkStyle(CoatingType.Custom, CoatingMarkShape.Square, Color.FromRGB(0, 128, 128))
            {
                Name = "ISO-MultiLayer",
                MarkSize = 3.0,
                TextSize = 2.0,
                ShowText = true,
                FillMark = true,
                FillOpacity = 0.2,
                Description = "ISO多层镀膜标记",
                Standard = CoatingStandard.ISO
            };
            CoatingMarkStyles["MultiLayer"] = multiLayerStyle;

            var gradientStyle = new CoatingMarkStyle(CoatingType.Custom, CoatingMarkShape.Diamond, Color.FromRGB(255, 165, 0))
            {
                Name = "ISO-Gradient",
                MarkSize = 2.5,
                TextSize = 1.8,
                ShowText = true,
                Description = "ISO渐变镀膜标记",
                Standard = CoatingStandard.ISO
            };
            CoatingMarkStyles["Gradient"] = gradientStyle;
        }

        /// <summary>
        /// 初始化ISO标准图层
        /// </summary>
        private void InitializeStandardLayers()
        {
            // ISO标准图层定义
            AddStandardLayer("0", Color.FromRGB(255, 255, 255), LineWeight.LineWeight025, LineType.Solid, "默认图层");
            AddStandardLayer("Outline", Color.FromRGB(0, 0, 0), LineWeight.LineWeight050, LineType.Solid, "轮廓线图层");
            AddStandardLayer("Hidden", Color.FromRGB(128, 128, 128), LineWeight.LineWeight025, LineType.Dash, "隐藏线图层");
            AddStandardLayer("Center", Color.FromRGB(0, 128, 0), LineWeight.LineWeight025, LineType.DashDot, "中心线图层");
            AddStandardLayer("Dimension", Color.FromRGB(255, 0, 0), LineWeight.LineWeight020, LineType.Solid, "尺寸标注图层");
            AddStandardLayer("Text", Color.FromRGB(0, 0, 0), LineWeight.LineWeight020, LineType.Solid, "文字图层");
            AddStandardLayer("Hatch", Color.FromRGB(0, 0, 0), LineWeight.LineWeight015, LineType.Solid, "填充图层");
            AddStandardLayer("Construction", Color.FromRGB(192, 192, 192), LineWeight.LineWeight015, LineType.Dot, "辅助线图层");
            AddStandardLayer("Section", Color.FromRGB(255, 0, 0), LineWeight.LineWeight080, LineType.Solid, "剖面线图层");
            AddStandardLayer("Leader", Color.FromRGB(0, 0, 255), LineWeight.LineWeight020, LineType.Solid, "引出线图层");

            // 光学专用图层
            AddStandardLayer("OpticalAxis", Color.FromRGB(255, 128, 0), LineWeight.LineWeight025, LineType.DashDot, "光轴图层");
            AddStandardLayer("LensOutline", Color.FromRGB(0, 0, 128), LineWeight.LineWeight050, LineType.Solid, "透镜轮廓图层");
            AddStandardLayer("CoatingMark", Color.FromRGB(0, 255, 0), LineWeight.LineWeight025, LineType.Solid, "镀膜标记图层");
            AddStandardLayer("OpticalSurface", Color.FromRGB(0, 128, 255), LineWeight.LineWeight030, LineType.Solid, "光学面图层");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 获取ISO线宽等级
        /// </summary>
        /// <param name="level">等级（1-8）</param>
        /// <returns>对应的线宽</returns>
        public static LineWeight GetISOLineWeight(int level)
        {
            // ISO 128-23标准线宽
            switch (level)
            {
                case 1: return LineWeight.LineWeight013;
                case 2: return LineWeight.LineWeight018;
                case 3: return LineWeight.LineWeight025;
                case 4: return LineWeight.LineWeight035;
                case 5: return LineWeight.LineWeight050;
                case 6: return LineWeight.LineWeight070;
                case 7: return LineWeight.LineWeight100;
                case 8: return LineWeight.LineWeight140;
                default: return LineWeight.LineWeight025;
            }
        }

        /// <summary>
        /// 获取ISO标准文字高度
        /// </summary>
        /// <param name="type">文字类型</param>
        /// <returns>文字高度（毫米）</returns>
        public static double GetISOTextHeight(ISOTextType type)
        {
            // ISO 3098标准文字高度
            switch (type)
            {
                case ISOTextType.Micro:
                    return 1.0;
                case ISOTextType.Small:
                    return 1.8;
                case ISOTextType.Standard:
                    return 2.5;
                case ISOTextType.Medium:
                    return 3.5;
                case ISOTextType.Large:
                    return 5.0;
                case ISOTextType.Title:
                    return 7.0;
                case ISOTextType.MainTitle:
                    return 10.0;
                default:
                    return 2.5;
            }
        }

        /// <summary>
        /// 创建ISO标准图框
        /// </summary>
        /// <param name="format">图纸格式</param>
        /// <returns>图框尺寸信息</returns>
        public static ISODrawingFormat GetISODrawingFormat(ISOPaperFormat format)
        {
            // ISO 5457标准图纸格式
            switch (format)
            {
                case ISOPaperFormat.A4:
                    return new ISODrawingFormat(210, 297, 20, 10, 5);
                case ISOPaperFormat.A3:
                    return new ISODrawingFormat(297, 420, 20, 10, 5);
                case ISOPaperFormat.A2:
                    return new ISODrawingFormat(420, 594, 20, 10, 5);
                case ISOPaperFormat.A1:
                    return new ISODrawingFormat(594, 841, 20, 10, 5);
                case ISOPaperFormat.A0:
                    return new ISODrawingFormat(841, 1189, 20, 10, 5);
                default:
                    return new ISODrawingFormat(210, 297, 20, 10, 5);
            }
        }

        /// <summary>
        /// 验证是否符合ISO标准
        /// </summary>
        /// <returns>验证结果</returns>
        public override StandardValidationResult Validate()
        {
            var result = base.Validate();

            // 额外的ISO标准验证
            if (!LineStyles.ContainsKey("Outline"))
            {
                result.Errors.Add("缺少ISO必需的轮廓线样式");
            }

            if (!ColorSchemes.ContainsKey("Basic"))
            {
                result.Errors.Add("缺少ISO基础颜色方案");
            }

            // 验证线宽是否符合ISO标准
            foreach (var style in LineStyles.Values)
            {
                var lineWidth = (int)style.LineWeight;
                if (!IsValidISOLineWeight(lineWidth))
                {
                    result.Warnings.Add($"线型 {style.Name} 的线宽 {lineWidth} 不符合ISO标准推荐值");
                }
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// 检查是否为有效的ISO线宽
        /// </summary>
        private bool IsValidISOLineWeight(int weight)
        {
            int[] isoWeights = { 13, 18, 25, 35, 50, 70, 100, 140, 200 };
            return Array.IndexOf(isoWeights, weight) >= 0;
        }

        #endregion
    }

    /// <summary>
    /// ISO文字类型枚举
    /// </summary>
    public enum ISOTextType
    {
        Micro = 1,
        Small = 2,
        Standard = 3,
        Medium = 4,
        Large = 5,
        Title = 6,
        MainTitle = 7
    }

    /// <summary>
    /// ISO纸张格式枚举
    /// </summary>
    public enum ISOPaperFormat
    {
        A4,
        A3,
        A2,
        A1,
        A0
    }

    /// <summary>
    /// ISO图纸格式信息
    /// </summary>
    public class ISODrawingFormat
    {
        public double Width { get; set; }          // 宽度（毫米）
        public double Height { get; set; }         // 高度（毫米）
        public double MarginLeft { get; set; }     // 左边距
        public double MarginRight { get; set; }    // 右边距
        public double MarginTop { get; set; }      // 上边距
        public double MarginBottom { get; set; }   // 下边距
        public double TitleBlockHeight { get; set; } // 标题栏高度

        public ISODrawingFormat(double width, double height, double marginLeft, 
                               double marginRight, double titleBlockHeight)
        {
            Width = width;
            Height = height;
            MarginLeft = marginLeft;
            MarginRight = marginRight;
            MarginTop = marginLeft;
            MarginBottom = marginLeft;
            TitleBlockHeight = titleBlockHeight;
        }
    }
}