using System;
using System.Collections.Generic;
using lcdb.Colors;
using lcdb.Annotation;

namespace lcdb.Standards
{
    /// <summary>
    /// GB国标绘图标准实现
    /// </summary>
    public class GBDrawingStandard : DrawingStandard
    {
        #region Constructor

        public GBDrawingStandard() : base("GB", "GB国家标准技术制图", "2023")
        {
        }

        #endregion

        #region Override Methods

        /// <summary>
        /// 初始化GB国标配置
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
        /// 初始化GB线型样式
        /// </summary>
        private void InitializeLineStyles()
        {
            // GB/T 4457.4标准线型
            AddLineStyle("Outline", LineType.Solid, LineWeight.LineWeight050, Color.FromRGB(0, 0, 0));
            AddLineStyle("Hidden", LineType.Dash, LineWeight.LineWeight025, Color.FromRGB(128, 128, 128));
            AddLineStyle("Center", LineType.DashDot, LineWeight.LineWeight025, Color.FromRGB(0, 128, 0));
            AddLineStyle("Dimension", LineType.Solid, LineWeight.LineWeight020, Color.FromRGB(255, 0, 0));
            AddLineStyle("Section", LineType.Solid, LineWeight.LineWeight080, Color.FromRGB(255, 0, 0));
            AddLineStyle("Leader", LineType.Solid, LineWeight.LineWeight020, Color.FromRGB(0, 0, 255));
            AddLineStyle("Construction", LineType.Dot, LineWeight.LineWeight015, Color.FromRGB(192, 192, 192));
            AddLineStyle("Boundary", LineType.DashDotDot, LineWeight.LineWeight040, Color.FromRGB(128, 0, 128));

            // GB特殊线型
            var phantomStyle = new LineStyle("Phantom", LineType.Custom, LineWeight.LineWeight025, Color.FromRGB(255, 128, 0))
            {
                DashPattern = new double[] { 20, 5, 4, 5, 4, 5 },
                Description = "GB虚实线"
            };
            LineStyles["Phantom"] = phantomStyle;

            var cuttingPlaneStyle = new LineStyle("CuttingPlane", LineType.Custom, LineWeight.LineWeight080, Color.FromRGB(255, 0, 0))
            {
                DashPattern = new double[] { 20, 2, 4, 2 },
                Description = "GB剖切线"
            };
            LineStyles["CuttingPlane"] = cuttingPlaneStyle;

            var symmetryStyle = new LineStyle("Symmetry", LineType.Custom, LineWeight.LineWeight025, Color.FromRGB(0, 128, 0))
            {
                DashPattern = new double[] { 15, 2, 15, 2, 3, 2 },
                Description = "GB对称线"
            };
            LineStyles["Symmetry"] = symmetryStyle;
        }

        /// <summary>
        /// 初始化GB颜色方案
        /// </summary>
        private void InitializeColorSchemes()
        {
            // GB/T国标颜色方案
            var gbBasic = new ColorScheme("GB-Basic", Color.FromRGB(0, 0, 0))
            {
                SecondaryColor = Color.FromRGB(128, 128, 128),
                AccentColor = Color.FromRGB(255, 0, 0),
                BackgroundColor = Color.FromRGB(255, 255, 255),
                TextColor = Color.FromRGB(0, 0, 0),
                HighlightColor = Color.FromRGB(255, 255, 0),
                GridColor = Color.FromRGB(192, 192, 192),
                SelectionColor = Color.FromRGB(0, 0, 255),
                Description = "GB基础颜色方案",
                Usage = ColorUsage.Technical
            };
            
            // 添加GB技术图纸专用颜色
            gbBasic.AddExtendedColor("OutlineThick", Color.FromRGB(0, 0, 0));      // 粗实线
            gbBasic.AddExtendedColor("OutlineThin", Color.FromRGB(64, 64, 64));    // 细实线
            gbBasic.AddExtendedColor("HiddenLine", Color.FromRGB(128, 128, 128));  // 虚线
            gbBasic.AddExtendedColor("CenterLine", Color.FromRGB(0, 128, 0));      // 中心线
            gbBasic.AddExtendedColor("DimensionLine", Color.FromRGB(255, 0, 0));   // 尺寸线
            gbBasic.AddExtendedColor("SectionLine", Color.FromRGB(255, 0, 0));     // 剖面线
            gbBasic.AddExtendedColor("LeaderLine", Color.FromRGB(0, 0, 255));      // 引出线
            gbBasic.AddExtendedColor("ConstructionLine", Color.FromRGB(192, 192, 192)); // 辅助线
            gbBasic.AddExtendedColor("SymmetryLine", Color.FromRGB(0, 128, 0));    // 对称线

            ColorSchemes["Basic"] = gbBasic;

            // GB机械制图颜色方案
            var gbMechanical = new ColorScheme("GB-Mechanical", Color.FromRGB(0, 0, 0))
            {
                SecondaryColor = Color.FromRGB(128, 128, 128),
                AccentColor = Color.FromRGB(255, 0, 0),
                BackgroundColor = Color.FromRGB(255, 255, 255),
                TextColor = Color.FromRGB(0, 0, 0),
                HighlightColor = Color.FromRGB(255, 255, 128),
                GridColor = Color.FromRGB(200, 200, 200),
                SelectionColor = Color.FromRGB(0, 128, 255),
                Description = "GB机械制图颜色方案",
                Usage = ColorUsage.Technical
            };

            // 机械制图专用颜色
            gbMechanical.AddExtendedColor("ThreadLine", Color.FromRGB(128, 64, 0));    // 螺纹线
            gbMechanical.AddExtendedColor("BreakLine", Color.FromRGB(255, 128, 0));    // 断裂线
            gbMechanical.AddExtendedColor("MaterialHatch", Color.FromRGB(64, 64, 64)); // 材料剖面线
            gbMechanical.AddExtendedColor("ToleranceLine", Color.FromRGB(255, 0, 128)); // 公差线

            ColorSchemes["Mechanical"] = gbMechanical;

            // GB打印颜色方案
            var gbPrint = new ColorScheme("GB-Print", Color.FromRGB(0, 0, 0))
            {
                SecondaryColor = Color.FromRGB(0, 0, 0),
                AccentColor = Color.FromRGB(0, 0, 0),
                BackgroundColor = Color.FromRGB(255, 255, 255),
                TextColor = Color.FromRGB(0, 0, 0),
                HighlightColor = Color.FromRGB(128, 128, 128),
                GridColor = Color.FromRGB(192, 192, 192),
                SelectionColor = Color.FromRGB(128, 128, 128),
                Description = "GB打印颜色方案（单色）",
                Usage = ColorUsage.Print
            };
            ColorSchemes["Print"] = gbPrint;
        }

        /// <summary>
        /// 初始化GB文本样式
        /// </summary>
        private void InitializeTextStyles()
        {
            // GB/T 14691标准文字
            AddTextStyle("Standard", TextStyleManager.CreateGBStandard());
            AddTextStyle("Title", TextStyleManager.CreateGBTitle());
            AddTextStyle("Subtitle", TextStyleManager.CreateGBSubtitle());
            AddTextStyle("Dimension", TextStyleManager.CreateGBDimension());
            AddTextStyle("Annotation", TextStyleManager.CreateGBAnnotation());
            AddTextStyle("Section", TextStyleManager.CreateGBSection());
            AddTextStyle("Detail", TextStyleManager.CreateGBDetail());
            AddTextStyle("DrawingNumber", TextStyleManager.CreateGBDrawingNumber());
            AddTextStyle("Revision", TextStyleManager.CreateGBRevision());
            AddTextStyle("Notes", TextStyleManager.CreateGBNotes());

            // 特殊用途文本样式
            var microTextStyle = new TextStyle("GB-Micro", "SimSun", FontStyle.Regular)
            {
                Height = 1.0,
                WidthFactor = 0.8,
                ObliqueAngle = 0.0
            };
            TextStyles["Micro"] = microTextStyle;

            var titleBlockStyle = new TextStyle("GB-TitleBlock", "SimHei", FontStyle.Bold)
            {
                Height = 4.0,
                WidthFactor = 0.8,
                ObliqueAngle = 0.0
            };
            TextStyles["TitleBlock"] = titleBlockStyle;

            // 技术要求文本样式
            var technicalStyle = new TextStyle("GB-Technical", "KaiTi", FontStyle.Regular)
            {
                Height = 2.5,
                WidthFactor = 0.8,
                ObliqueAngle = 15.0
            };
            TextStyles["Technical"] = technicalStyle;

            // 材料标注文本样式
            var materialStyle = new TextStyle("GB-Material", "SimSun", FontStyle.Bold)
            {
                Height = 3.0,
                WidthFactor = 0.8,
                ObliqueAngle = 0.0
            };
            TextStyles["Material"] = materialStyle;
        }

        /// <summary>
        /// 初始化GB镀膜标记样式
        /// </summary>
        private void InitializeCoatingMarkStyles()
        {
            // GB光学元件图纸标准
            var gbStyles = CoatingMarkStyle.CreateGBStandardStyles();
            foreach (var kvp in gbStyles)
            {
                CoatingMarkStyles[kvp.Key.ToString()] = kvp.Value;
            }

            // 添加GB特殊镀膜标记
            var multiLayerStyle = new CoatingMarkStyle(CoatingType.Custom, CoatingMarkShape.Square, Color.FromRGB(0, 128, 128))
            {
                Name = "GB-MultiLayer",
                MarkSize = 3.5,
                TextSize = 2.5,
                TextStyleName = "GB-Standard",
                ShowText = true,
                FillMark = true,
                FillOpacity = 0.2,
                Description = "GB多层镀膜标记",
                Standard = CoatingStandard.GB
            };
            CoatingMarkStyles["MultiLayer"] = multiLayerStyle;

            var protectiveStyle = new CoatingMarkStyle(CoatingType.Custom, CoatingMarkShape.Diamond, Color.FromRGB(255, 165, 0))
            {
                Name = "GB-Protective",
                MarkSize = 3.0,
                TextSize = 2.0,
                TextStyleName = "GB-Standard",
                ShowText = true,
                Description = "GB保护膜标记",
                Standard = CoatingStandard.GB
            };
            CoatingMarkStyles["Protective"] = protectiveStyle;

            // 偏振膜标记
            var polarizerStyle = new CoatingMarkStyle(CoatingType.Custom, CoatingMarkShape.Triangle, Color.FromRGB(128, 0, 255))
            {
                Name = "GB-Polarizer",
                MarkSize = 2.8,
                TextSize = 2.0,
                TextStyleName = "GB-Standard",
                ShowText = true,
                Description = "GB偏振膜标记",
                Standard = CoatingStandard.GB
            };
            CoatingMarkStyles["Polarizer"] = polarizerStyle;
        }

        /// <summary>
        /// 初始化GB标准图层
        /// </summary>
        private void InitializeStandardLayers()
        {
            // GB标准图层定义
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
            AddStandardLayer("Symmetry", Color.FromRGB(0, 128, 0), LineWeight.LineWeight025, LineType.DashDot, "对称线图层");

            // 机械制图专用图层
            AddStandardLayer("Thread", Color.FromRGB(128, 64, 0), LineWeight.LineWeight020, LineType.Solid, "螺纹图层");
            AddStandardLayer("Break", Color.FromRGB(255, 128, 0), LineWeight.LineWeight030, LineType.Custom, "断裂线图层");
            AddStandardLayer("Tolerance", Color.FromRGB(255, 0, 128), LineWeight.LineWeight020, LineType.Solid, "公差图层");

            // 光学专用图层
            AddStandardLayer("OpticalAxis", Color.FromRGB(255, 128, 0), LineWeight.LineWeight025, LineType.DashDot, "光轴图层");
            AddStandardLayer("LensOutline", Color.FromRGB(0, 0, 128), LineWeight.LineWeight050, LineType.Solid, "透镜轮廓图层");
            AddStandardLayer("CoatingMark", Color.FromRGB(0, 255, 0), LineWeight.LineWeight025, LineType.Solid, "镀膜标记图层");
            AddStandardLayer("OpticalSurface", Color.FromRGB(0, 128, 255), LineWeight.LineWeight030, LineType.Solid, "光学面图层");
            AddStandardLayer("TechnicalReq", Color.FromRGB(128, 128, 0), LineWeight.LineWeight020, LineType.Solid, "技术要求图层");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 获取GB线宽等级
        /// </summary>
        /// <param name="level">等级（1-8）</param>
        /// <returns>对应的线宽</returns>
        public static LineWeight GetGBLineWeight(int level)
        {
            // GB/T 4457.4标准线宽
            switch (level)
            {
                case 1: return LineWeight.LineWeight015;
                case 2: return LineWeight.LineWeight020;
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
        /// 获取GB标准文字高度
        /// </summary>
        /// <param name="type">文字类型</param>
        /// <returns>文字高度（毫米）</returns>
        public static double GetGBTextHeight(GBTextType type)
        {
            // GB/T 14691标准文字高度
            switch (type)
            {
                case GBTextType.Micro:
                    return 1.0;
                case GBTextType.Small:
                    return 2.0;
                case GBTextType.Standard:
                    return 3.5;
                case GBTextType.Medium:
                    return 5.0;
                case GBTextType.Large:
                    return 7.0;
                case GBTextType.Title:
                    return 10.0;
                case GBTextType.MainTitle:
                    return 14.0;
                default:
                    return 3.5;
            }
        }

        /// <summary>
        /// 创建GB标准图框
        /// </summary>
        /// <param name="format">图纸格式</param>
        /// <returns>图框尺寸信息</returns>
        public static GBDrawingFormat GetGBDrawingFormat(GBPaperFormat format)
        {
            // GB/T 14689标准图纸格式
            switch (format)
            {
                case GBPaperFormat.A4:
                    return new GBDrawingFormat(210, 297, 25, 10, 8, 55);
                case GBPaperFormat.A3:
                    return new GBDrawingFormat(297, 420, 25, 10, 8, 55);
                case GBPaperFormat.A2:
                    return new GBDrawingFormat(420, 594, 25, 10, 8, 55);
                case GBPaperFormat.A1:
                    return new GBDrawingFormat(594, 841, 25, 10, 8, 55);
                case GBPaperFormat.A0:
                    return new GBDrawingFormat(841, 1189, 25, 10, 8, 55);
                default:
                    return new GBDrawingFormat(210, 297, 25, 10, 8, 55);
            }
        }

        /// <summary>
        /// 获取GB材料剖面线类型
        /// </summary>
        /// <param name="material">材料类型</param>
        /// <returns>剖面线样式</returns>
        public static LineStyle GetGBMaterialHatchStyle(GBMaterialType material)
        {
            switch (material)
            {
                case GBMaterialType.Steel:
                    return new LineStyle("Steel-Hatch", LineType.Solid, LineWeight.LineWeight015, Color.FromRGB(0, 0, 0))
                    {
                        Description = "钢材剖面线"
                    };
                case GBMaterialType.Cast:
                    return new LineStyle("Cast-Hatch", LineType.Custom, LineWeight.LineWeight015, Color.FromRGB(0, 0, 0))
                    {
                        DashPattern = new double[] { 2, 1 },
                        Description = "铸铁剖面线"
                    };
                case GBMaterialType.Aluminum:
                    return new LineStyle("Aluminum-Hatch", LineType.Custom, LineWeight.LineWeight015, Color.FromRGB(0, 0, 0))
                    {
                        DashPattern = new double[] { 3, 2, 1, 2 },
                        Description = "铝材剖面线"
                    };
                case GBMaterialType.Plastic:
                    return new LineStyle("Plastic-Hatch", LineType.Dot, LineWeight.LineWeight015, Color.FromRGB(0, 0, 0))
                    {
                        Description = "塑料剖面线"
                    };
                default:
                    return new LineStyle("General-Hatch", LineType.Solid, LineWeight.LineWeight015, Color.FromRGB(0, 0, 0))
                    {
                        Description = "通用剖面线"
                    };
            }
        }

        /// <summary>
        /// 验证是否符合GB标准
        /// </summary>
        /// <returns>验证结果</returns>
        public override StandardValidationResult Validate()
        {
            var result = base.Validate();

            // 额外的GB标准验证
            if (!LineStyles.ContainsKey("Outline"))
            {
                result.Errors.Add("缺少GB必需的轮廓线样式");
            }

            if (!ColorSchemes.ContainsKey("Basic"))
            {
                result.Errors.Add("缺少GB基础颜色方案");
            }

            // 验证线宽是否符合GB标准
            foreach (var style in LineStyles.Values)
            {
                var lineWidth = (int)style.LineWeight;
                if (!IsValidGBLineWeight(lineWidth))
                {
                    result.Warnings.Add($"线型 {style.Name} 的线宽 {lineWidth} 不符合GB标准推荐值");
                }
            }

            // 验证中文字体
            foreach (var textStyle in TextStyles.Values)
            {
                if (!IsValidGBFont(textStyle.FontFamilyName))
                {
                    result.Warnings.Add($"文本样式 {textStyle.name} 使用的字体可能不符合GB标准");
                }
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// 检查是否为有效的GB线宽
        /// </summary>
        private bool IsValidGBLineWeight(int weight)
        {
            int[] gbWeights = { 15, 20, 25, 35, 50, 70, 100, 140, 200 };
            return Array.IndexOf(gbWeights, weight) >= 0;
        }

        /// <summary>
        /// 检查是否为有效的GB字体
        /// </summary>
        private bool IsValidGBFont(string fontName)
        {
            string[] gbFonts = { "SimSun", "SimHei", "KaiTi", "FangSong", "Microsoft YaHei", "宋体", "黑体", "楷体", "仿宋" };
            return Array.IndexOf(gbFonts, fontName) >= 0;
        }

        #endregion
    }

    /// <summary>
    /// GB文字类型枚举
    /// </summary>
    public enum GBTextType
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
    /// GB纸张格式枚举
    /// </summary>
    public enum GBPaperFormat
    {
        A4,
        A3,
        A2,
        A1,
        A0
    }

    /// <summary>
    /// GB材料类型枚举
    /// </summary>
    public enum GBMaterialType
    {
        Steel,      // 钢材
        Cast,       // 铸铁
        Aluminum,   // 铝材
        Copper,     // 铜材
        Plastic,    // 塑料
        Rubber,     // 橡胶
        Wood,       // 木材
        Glass,      // 玻璃
        Ceramic     // 陶瓷
    }

    /// <summary>
    /// GB图纸格式信息
    /// </summary>
    public class GBDrawingFormat
    {
        public double Width { get; set; }              // 宽度（毫米）
        public double Height { get; set; }             // 高度（毫米）
        public double MarginLeft { get; set; }         // 左边距
        public double MarginRight { get; set; }        // 右边距
        public double MarginTop { get; set; }          // 上边距
        public double MarginBottom { get; set; }       // 下边距
        public double TitleBlockWidth { get; set; }    // 标题栏宽度
        public double TitleBlockHeight { get; set; }   // 标题栏高度

        public GBDrawingFormat(double width, double height, double marginLeft, 
                              double marginRight, double marginBottom, double titleBlockWidth)
        {
            Width = width;
            Height = height;
            MarginLeft = marginLeft;
            MarginRight = marginRight;
            MarginTop = marginLeft;
            MarginBottom = marginBottom;
            TitleBlockWidth = titleBlockWidth;
            TitleBlockHeight = marginBottom;
        }
    }
}