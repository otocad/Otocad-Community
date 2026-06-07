using System;
using System.Collections.Generic;
using lcdb.Colors;
using lcdb.Annotation;

namespace lcdb.Standards
{
    /// <summary>
    /// ISO 10110 光学零件和系统图纸标准
    /// </summary>
    public class ISO10110Standard : DrawingStandard
    {
        #region Constructor

        public ISO10110Standard() : base("ISO 10110", "ISO 10110 光学零件和系统图纸标准", "2019")
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
            InitializeOpticalSymbols();
            InitializeSurfaceQualityNotations();
        }

        #endregion

        #region Private Initialization Methods

        private void InitializeLineStyles()
        {
            // ISO 10110标准线型
            AddLineStyle("OpticalAxis", LineType.DashDot, LineWeight.LineWeight025, Color.FromRGB(255, 128, 0));
            AddLineStyle("LensOutline", LineType.Solid, LineWeight.LineWeight050, Color.FromRGB(0, 0, 0));
            AddLineStyle("MountingOutline", LineType.Solid, LineWeight.LineWeight035, Color.FromRGB(64, 64, 64));
            AddLineStyle("RayPath", LineType.Dash, LineWeight.LineWeight020, Color.FromRGB(0, 128, 255));
            AddLineStyle("CoatingBoundary", LineType.Solid, LineWeight.LineWeight025, Color.FromRGB(0, 255, 0));
            AddLineStyle("ClearAperture", LineType.Dash, LineWeight.LineWeight025, Color.FromRGB(255, 0, 0));
            AddLineStyle("MechanicalReference", LineType.DashDotDot, LineWeight.LineWeight020, Color.FromRGB(128, 0, 128));
            
            // 特殊光学表面线型
            var asphericIndicator = new LineStyle("AsphericIndicator", LineType.Custom, LineWeight.LineWeight025, Color.FromRGB(0, 0, 255))
            {
                DashPattern = new double[] { 10, 2, 2, 2 },
                Description = "非球面指示线"
            };
            LineStyles["AsphericIndicator"] = asphericIndicator;
        }

        private void InitializeColorSchemes()
        {
            // ISO 10110光学图纸颜色方案
            var opticalScheme = new ColorScheme("Optical", Color.FromRGB(0, 0, 0))
            {
                SecondaryColor = Color.FromRGB(128, 128, 128),
                AccentColor = Color.FromRGB(255, 128, 0),
                BackgroundColor = Color.FromRGB(255, 255, 255),
                TextColor = Color.FromRGB(0, 0, 0),
                Description = "ISO 10110光学元件标准配色"
            };

            // 添加光学专用颜色
            opticalScheme.AddExtendedColor("OpticalSurface", Color.FromRGB(0, 128, 255));
            opticalScheme.AddExtendedColor("MechanicalSurface", Color.FromRGB(128, 128, 128));
            opticalScheme.AddExtendedColor("CoatingAR", Color.FromRGB(0, 255, 0));
            opticalScheme.AddExtendedColor("CoatingHR", Color.FromRGB(255, 0, 0));
            opticalScheme.AddExtendedColor("CoatingPartial", Color.FromRGB(255, 255, 0));
            opticalScheme.AddExtendedColor("OpticalAxis", Color.FromRGB(255, 128, 0));
            opticalScheme.AddExtendedColor("ClearAperture", Color.FromRGB(255, 0, 255));

            ColorSchemes["Optical"] = opticalScheme;
            ColorSchemes["Default"] = opticalScheme;
        }

        private void InitializeTextStyles()
        {
            // ISO 10110文本样式
            AddTextStyle("SurfaceQuality", new TextStyle("ISO10110-Surface", "Arial", FontStyle.Regular)
            {
                Height = 2.5,
                WidthFactor = 0.8
            });

            AddTextStyle("ToleranceNotation", new TextStyle("ISO10110-Tolerance", "Arial", FontStyle.Regular)
            {
                Height = 2.0,
                WidthFactor = 0.7
            });

            AddTextStyle("MaterialSpec", new TextStyle("ISO10110-Material", "Arial", FontStyle.Bold)
            {
                Height = 3.0,
                WidthFactor = 1.0
            });

            AddTextStyle("OpticalData", new TextStyle("ISO10110-OpticalData", "Arial", FontStyle.Regular)
            {
                Height = 2.5,
                WidthFactor = 0.9
            });
        }

        private void InitializeCoatingMarkStyles()
        {
            // ISO 10110-7 镀膜标记
            AddCoatingMarkStyle(CoatingType.AR, CoatingMarkShape.Circle, Color.FromRGB(0, 255, 0), 2.5);
            AddCoatingMarkStyle(CoatingType.HR, CoatingMarkShape.Square, Color.FromRGB(255, 0, 0), 2.5);
            AddCoatingMarkStyle(CoatingType.BS, CoatingMarkShape.Triangle, Color.FromRGB(255, 255, 0), 2.5);
            AddCoatingMarkStyle(CoatingType.Filter, CoatingMarkShape.Diamond, Color.FromRGB(255, 0, 255), 2.5);
            AddCoatingMarkStyle(CoatingType.Protective, CoatingMarkShape.Circle, Color.FromRGB(128, 128, 128), 2.5);
            AddCoatingMarkStyle(CoatingType.Other, CoatingMarkShape.Square, Color.FromRGB(128, 0, 255), 2.5);
        }

        private void InitializeStandardLayers()
        {
            // ISO 10110标准图层
            AddStandardLayer("0", Color.FromRGB(255, 255, 255), LineWeight.LineWeight025, LineType.Solid, "默认图层");
            AddStandardLayer("OpticalElements", Color.FromRGB(0, 0, 0), LineWeight.LineWeight050, LineType.Solid, "光学元件");
            AddStandardLayer("OpticalAxis", Color.FromRGB(255, 128, 0), LineWeight.LineWeight025, LineType.DashDot, "光轴");
            AddStandardLayer("Dimensions", Color.FromRGB(255, 0, 0), LineWeight.LineWeight020, LineType.Solid, "尺寸标注");
            AddStandardLayer("SurfaceQuality", Color.FromRGB(0, 128, 0), LineWeight.LineWeight020, LineType.Solid, "表面质量");
            AddStandardLayer("Tolerances", Color.FromRGB(0, 0, 255), LineWeight.LineWeight020, LineType.Solid, "公差标注");
            AddStandardLayer("Coatings", Color.FromRGB(0, 255, 0), LineWeight.LineWeight025, LineType.Solid, "镀膜标记");
            AddStandardLayer("ClearAperture", Color.FromRGB(255, 0, 255), LineWeight.LineWeight025, LineType.Dash, "通光孔径");
            AddStandardLayer("MountingFeatures", Color.FromRGB(128, 128, 128), LineWeight.LineWeight035, LineType.Solid, "安装特征");
            AddStandardLayer("TechnicalNotes", Color.FromRGB(0, 0, 0), LineWeight.LineWeight020, LineType.Solid, "技术说明");
        }

        private void InitializeOpticalSymbols()
        {
            // ISO 10110光学符号将在OpticalSymbols.cs中实现
        }

        private void InitializeSurfaceQualityNotations()
        {
            // ISO 10110表面质量标记将在专门的类中实现
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
                    return 0.7;
                case "InnerFrame":
                    return 0.35;
                case "Division":
                    return 0.25;
                case "Text":
                    return 0.25;
                case "Dimension":
                    return 0.25;
                case "OpticalElement":
                    return 0.5;
                case "OpticalAxis":
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
                    return 5.0;
                case "Subtitle":
                    return 3.5;
                case "Normal":
                    return 2.5;
                case "Small":
                    return 2.0;
                case "Dimension":
                    return 2.5;
                case "Tolerance":
                    return 2.0;
                case "SurfaceQuality":
                    return 2.5;
                default:
                    return 2.5;
            }
        }

        /// <summary>
        /// 生成表面质量标记
        /// </summary>
        public string GenerateSurfaceQualityNotation(int scratchNumber, int digNumber)
        {
            // ISO 10110-7 表面缺陷标记
            return $"{scratchNumber}/{digNumber}";
        }

        /// <summary>
        /// 生成形状公差标记
        /// </summary>
        public string GenerateFormToleranceNotation(string type, double value, string unit = "λ")
        {
            // ISO 10110-5 形状公差标记
            return $"{type} {value}{unit}";
        }

        /// <summary>
        /// 生成中心偏差标记
        /// </summary>
        public string GenerateCenteringNotation(double decenter, double tilt)
        {
            // ISO 10110-6 中心偏差标记
            return $"C {decenter}; {tilt}";
        }

        #endregion
    }

    /// <summary>
    /// ISO 10110 表面类型
    /// </summary>
    public enum ISO10110SurfaceType
    {
        Spherical,      // 球面
        Aspherical,     // 非球面
        Cylindrical,    // 柱面
        Toroidal,       // 环面
        Freeform,       // 自由曲面
        Flat            // 平面
    }

    /// <summary>
    /// ISO 10110 公差类型
    /// </summary>
    public enum ISO10110ToleranceType
    {
        FormDeviation,      // 形状偏差
        Irregularity,       // 不规则度
        CenteringError,     // 中心误差
        SurfaceTexture,     // 表面结构
        SurfaceImperfection // 表面缺陷
    }
}