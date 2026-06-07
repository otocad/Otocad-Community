using System;
using System.Collections.Generic;
using lcdb.Colors;
using lcdb.Annotation;

namespace lcdb.Standards
{
    /// <summary>
    /// GB/T 13323-2009 光学制图标准
    /// </summary>
    public class GB13323Standard : DrawingStandard
    {
        #region Constructor

        public GB13323Standard() : base("GB/T 13323", "GB/T 13323-2009 光学制图", "2009")
        {
        }

        #endregion

        #region Override Methods

        protected override void InitializeStandard()
        {
            InitializeLineStyles();
            InitializeColorSchemes();
            InitializeTextStyles();
            InitializeCoatingMarkStyles();
            InitializeStandardLayers();
            InitializeOpticalNotations();
        }

        #endregion

        #region Private Initialization Methods

        private void InitializeLineStyles()
        {
            // GB/T 13323标准线型
            AddLineStyle("粗实线", LineType.Solid, LineWeight.LineWeight070, Color.FromRGB(0, 0, 0));
            AddLineStyle("中粗实线", LineType.Solid, LineWeight.LineWeight050, Color.FromRGB(0, 0, 0));
            AddLineStyle("细实线", LineType.Solid, LineWeight.LineWeight025, Color.FromRGB(0, 0, 0));
            AddLineStyle("细虚线", LineType.Dash, LineWeight.LineWeight025, Color.FromRGB(128, 128, 128));
            AddLineStyle("细点划线", LineType.DashDot, LineWeight.LineWeight025, Color.FromRGB(0, 128, 0));
            AddLineStyle("细双点划线", LineType.DashDotDot, LineWeight.LineWeight025, Color.FromRGB(128, 0, 128));
            AddLineStyle("波浪线", LineType.Custom, LineWeight.LineWeight025, Color.FromRGB(0, 0, 255));
            AddLineStyle("双折线", LineType.Custom, LineWeight.LineWeight025, Color.FromRGB(255, 0, 0));

            // 光学专用线型
            AddLineStyle("光轴线", LineType.DashDot, LineWeight.LineWeight025, Color.FromRGB(255, 128, 0));
            AddLineStyle("光学表面", LineType.Solid, LineWeight.LineWeight050, Color.FromRGB(0, 0, 128));
            AddLineStyle("机械表面", LineType.Solid, LineWeight.LineWeight035, Color.FromRGB(64, 64, 64));
            AddLineStyle("镀膜边界", LineType.Solid, LineWeight.LineWeight025, Color.FromRGB(0, 255, 0));
            AddLineStyle("有效孔径", LineType.Dash, LineWeight.LineWeight025, Color.FromRGB(255, 0, 255));
        }

        private void InitializeColorSchemes()
        {
            // GB/T 13323光学制图颜色方案
            var gbOpticalScheme = new ColorScheme("GB-Optical", Color.FromRGB(0, 0, 0))
            {
                SecondaryColor = Color.FromRGB(128, 128, 128),
                AccentColor = Color.FromRGB(255, 0, 0),
                BackgroundColor = Color.FromRGB(255, 255, 255),
                TextColor = Color.FromRGB(0, 0, 0),
                Description = "GB/T 13323光学制图标准配色"
            };

            // 添加国标光学专用颜色
            gbOpticalScheme.AddExtendedColor("光学面", Color.FromRGB(0, 0, 255));
            gbOpticalScheme.AddExtendedColor("非光学面", Color.FromRGB(128, 128, 128));
            gbOpticalScheme.AddExtendedColor("增透膜", Color.FromRGB(0, 255, 0));
            gbOpticalScheme.AddExtendedColor("高反膜", Color.FromRGB(255, 0, 0));
            gbOpticalScheme.AddExtendedColor("分光膜", Color.FromRGB(255, 255, 0));
            gbOpticalScheme.AddExtendedColor("光轴", Color.FromRGB(255, 128, 0));
            gbOpticalScheme.AddExtendedColor("有效通光孔径", Color.FromRGB(255, 0, 255));
            gbOpticalScheme.AddExtendedColor("尺寸标注", Color.FromRGB(255, 0, 0));

            ColorSchemes["GB-Optical"] = gbOpticalScheme;
            ColorSchemes["Default"] = gbOpticalScheme;

            // GB打印颜色方案
            var gbPrintScheme = new ColorScheme("GB-Print", Color.FromRGB(0, 0, 0))
            {
                SecondaryColor = Color.FromRGB(0, 0, 0),
                AccentColor = Color.FromRGB(0, 0, 0),
                BackgroundColor = Color.FromRGB(255, 255, 255),
                TextColor = Color.FromRGB(0, 0, 0),
                Description = "GB/T 13323打印颜色方案"
            };
            ColorSchemes["Print"] = gbPrintScheme;
        }

        private void InitializeTextStyles()
        {
            // GB/T 13323文字样式
            AddTextStyle("标题", new TextStyle("GB-Title", "仿宋", FontStyle.Bold)
            {
                Height = 7.0,
                WidthFactor = 1.0
            });

            AddTextStyle("图号", new TextStyle("GB-DrawingNo", "仿宋", FontStyle.Bold)
            {
                Height = 5.0,
                WidthFactor = 1.0
            });

            AddTextStyle("正文", new TextStyle("GB-Normal", "仿宋", FontStyle.Regular)
            {
                Height = 3.5,
                WidthFactor = 1.0
            });

            AddTextStyle("尺寸", new TextStyle("GB-Dimension", "仿宋", FontStyle.Regular)
            {
                Height = 3.0,
                WidthFactor = 0.8
            });

            AddTextStyle("公差", new TextStyle("GB-Tolerance", "仿宋", FontStyle.Regular)
            {
                Height = 2.5,
                WidthFactor = 0.7
            });

            AddTextStyle("表面质量", new TextStyle("GB-Surface", "仿宋", FontStyle.Regular)
            {
                Height = 2.5,
                WidthFactor = 0.8
            });

            AddTextStyle("技术要求", new TextStyle("GB-TechReq", "仿宋", FontStyle.Regular)
            {
                Height = 3.0,
                WidthFactor = 1.0
            });

            AddTextStyle("材料标注", new TextStyle("GB-Material", "仿宋", FontStyle.Bold)
            {
                Height = 3.5,
                WidthFactor = 1.0
            });
        }

        private void InitializeCoatingMarkStyles()
        {
            // GB/T 13323镀膜标记
            AddCoatingMarkStyle(CoatingType.AR, CoatingMarkShape.Circle, Color.FromRGB(0, 255, 0), 3.0);
            AddCoatingMarkStyle(CoatingType.HR, CoatingMarkShape.Square, Color.FromRGB(255, 0, 0), 3.0);
            AddCoatingMarkStyle(CoatingType.BS, CoatingMarkShape.Triangle, Color.FromRGB(255, 255, 0), 3.0);
            AddCoatingMarkStyle(CoatingType.Filter, CoatingMarkShape.Diamond, Color.FromRGB(255, 0, 255), 3.0);
            AddCoatingMarkStyle(CoatingType.Protective, CoatingMarkShape.Square, Color.FromRGB(128, 128, 128), 3.0);
            AddCoatingMarkStyle(CoatingType.Other, CoatingMarkShape.Circle, Color.FromRGB(128, 0, 255), 3.0);

            // 国标特殊镀膜标记
            var protectiveCoating = new CoatingMarkStyle(CoatingType.Custom, CoatingMarkShape.Circle, Color.FromRGB(128, 128, 128))
            {
                Name = "保护膜",
                MarkSize = 3.0,
                TextSize = 2.5,
                ShowText = true,
                Description = "保护性镀膜标记"
            };
            CoatingMarkStyles["Protective"] = protectiveCoating;
        }

        private void InitializeStandardLayers()
        {
            // GB/T 13323标准图层
            AddStandardLayer("0", Color.FromRGB(255, 255, 255), LineWeight.LineWeight025, LineType.Solid, "默认图层");
            AddStandardLayer("光学元件", Color.FromRGB(0, 0, 0), LineWeight.LineWeight050, LineType.Solid, "光学元件轮廓");
            AddStandardLayer("光轴", Color.FromRGB(255, 128, 0), LineWeight.LineWeight025, LineType.DashDot, "光轴线");
            AddStandardLayer("尺寸", Color.FromRGB(255, 0, 0), LineWeight.LineWeight020, LineType.Solid, "尺寸标注");
            AddStandardLayer("公差", Color.FromRGB(0, 0, 255), LineWeight.LineWeight020, LineType.Solid, "公差标注");
            AddStandardLayer("表面质量", Color.FromRGB(0, 128, 0), LineWeight.LineWeight020, LineType.Solid, "表面质量标注");
            AddStandardLayer("镀膜", Color.FromRGB(0, 255, 0), LineWeight.LineWeight025, LineType.Solid, "镀膜标记");
            AddStandardLayer("文字", Color.FromRGB(0, 0, 0), LineWeight.LineWeight020, LineType.Solid, "文字说明");
            AddStandardLayer("中心线", Color.FromRGB(0, 128, 0), LineWeight.LineWeight025, LineType.DashDot, "中心线");
            AddStandardLayer("辅助线", Color.FromRGB(192, 192, 192), LineWeight.LineWeight015, LineType.Dot, "辅助线");
            AddStandardLayer("剖面", Color.FromRGB(255, 0, 0), LineWeight.LineWeight070, LineType.Solid, "剖面线");
            AddStandardLayer("机械零件", Color.FromRGB(64, 64, 64), LineWeight.LineWeight035, LineType.Solid, "机械零件");
        }

        private void InitializeOpticalNotations()
        {
            // GB/T 13323光学标记将在专门的类中实现
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 获取线宽
        /// </summary>
        public double GetLineWidth(string usage)
        {
            switch (usage)
            {
                case "OuterFrame":
                case "外框":
                    return 0.7;
                case "InnerFrame":
                case "内框":
                    return 0.35;
                case "Division":
                case "分割线":
                    return 0.25;
                case "Text":
                case "文字":
                    return 0.25;
                case "Dimension":
                case "尺寸":
                    return 0.25;
                case "OpticalElement":
                case "光学元件":
                    return 0.5;
                case "OpticalAxis":
                case "光轴":
                    return 0.25;
                default:
                    return 0.35;
            }
        }

        /// <summary>
        /// 获取文字高度
        /// </summary>
        public double GetTextHeight(string usage)
        {
            switch (usage)
            {
                case "Title":
                case "标题":
                    return 7.0;
                case "Subtitle":
                case "副标题":
                    return 5.0;
                case "Normal":
                case "正文":
                    return 3.5;
                case "Small":
                case "小字":
                    return 2.5;
                case "Dimension":
                case "尺寸":
                    return 3.0;
                case "Tolerance":
                case "公差":
                    return 2.5;
                case "SurfaceQuality":
                case "表面质量":
                    return 2.5;
                default:
                    return 3.5;
            }
        }

        /// <summary>
        /// 生成表面粗糙度标记
        /// </summary>
        public string GenerateSurfaceRoughnessNotation(double ra)
        {
            // GB/T 13323表面粗糙度标记
            return $"Ra{ra}";
        }

        /// <summary>
        /// 生成表面疵病标记
        /// </summary>
        public string GenerateSurfaceDefectNotation(string grade, int number)
        {
            // GB/T 13323表面疵病标记
            return $"{grade}-{number}";
        }

        /// <summary>
        /// 生成面形偏差标记
        /// </summary>
        public string GenerateFormDeviationNotation(string type, double value, string unit = "λ")
        {
            // GB/T 13323面形偏差标记
            return $"{type}: {value}{unit}";
        }

        /// <summary>
        /// 生成中心偏差标记
        /// </summary>
        public string GenerateCenterDeviationNotation(double radial, double angular)
        {
            // GB/T 13323中心偏差标记
            return $"径向: {radial}mm, 角度: {angular}'";
        }

        /// <summary>
        /// 获取GB图纸折叠方式
        /// </summary>
        public GBFoldingMethod GetFoldingMethod(string paperSize)
        {
            switch (paperSize.ToUpper())
            {
                case "A0":
                    return GBFoldingMethod.A0Folding;
                case "A1":
                    return GBFoldingMethod.A1Folding;
                case "A2":
                    return GBFoldingMethod.A2Folding;
                case "A3":
                    return GBFoldingMethod.A3Folding;
                default:
                    return GBFoldingMethod.None;
            }
        }

        #endregion
    }

    /// <summary>
    /// GB图纸折叠方式
    /// </summary>
    public enum GBFoldingMethod
    {
        None,           // 不折叠
        A0Folding,      // A0折叠方式
        A1Folding,      // A1折叠方式
        A2Folding,      // A2折叠方式
        A3Folding       // A3折叠方式
    }

    /// <summary>
    /// GB光学表面类型
    /// </summary>
    public enum GBOpticalSurfaceType
    {
        球面,
        非球面,
        柱面,
        环面,
        自由曲面,
        平面
    }

    /// <summary>
    /// GB公差等级
    /// </summary>
    public enum GBToleranceGrade
    {
        精密级,
        一般级,
        粗糙级
    }
}