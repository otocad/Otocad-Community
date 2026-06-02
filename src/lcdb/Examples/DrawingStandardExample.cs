using System;
using System.Collections.Generic;
using lcdb;
using lcdb.Standards;
using lcdb.Annotation;
using lcdb.Colors;
using LitMath;

namespace OtoCAD.Examples
{
    /// <summary>
    /// 绘图标准使用示例
    /// </summary>
    public class DrawingStandardExample
    {
        /// <summary>
        /// 基本标准应用示例
        /// </summary>
        public static void BasicStandardExample()
        {
            // 创建数据库
            Database database = new Database();

            // 应用ISO标准
            bool success = DrawingStandardManager.ApplyStandardToDatabase(database, "ISO", true);
            if (success)
            {
                Console.WriteLine("ISO标准应用成功");

                // 使用ISO标准的线型样式绘制图形
                var outlineStyle = DrawingStandardManager.GetRecommendedLineStyle("ISO", "Outline");
                var hiddenStyle = DrawingStandardManager.GetRecommendedLineStyle("ISO", "Hidden");
                var centerStyle = DrawingStandardManager.GetRecommendedLineStyle("ISO", "Center");

                // 创建实体并应用样式
                var line1 = new Line(new Vector2(0, 0), new Vector2(100, 0));
                outlineStyle?.ApplyToEntity(line1);
                (database.blockTable["ModelSpace"] as Block).AppendEntity(line1);

                var line2 = new Line(new Vector2(0, 10), new Vector2(100, 10));
                hiddenStyle?.ApplyToEntity(line2);
                (database.blockTable["ModelSpace"] as Block).AppendEntity(line2);

                var line3 = new Line(new Vector2(50, -10), new Vector2(50, 20));
                centerStyle?.ApplyToEntity(line3);
                (database.blockTable["ModelSpace"] as Block).AppendEntity(line3);
            }
        }

        /// <summary>
        /// 模板应用示例
        /// </summary>
        public static void TemplateApplicationExample()
        {
            Database database = new Database();

            // 应用ISO技术绘图模板
            var result = DrawingStandardManager.ApplyTemplateToDatabase(database, "ISO-Technical", true);
            if (result.Success)
            {
                Console.WriteLine($"模板 {result.Template.DisplayName} 应用成功");
                Console.WriteLine($"使用标准: {result.AppliedStandard}");

                // 验证数据库是否符合标准
                var validation = DrawingStandardManager.ValidateDatabase(database, "ISO");
                if (validation.IsValid)
                {
                    Console.WriteLine("数据库符合ISO标准");
                }
                else
                {
                    Console.WriteLine("数据库不完全符合ISO标准：");
                    foreach (var error in validation.Errors)
                    {
                        Console.WriteLine($"  错误: {error}");
                    }
                    foreach (var warning in validation.Warnings)
                    {
                        Console.WriteLine($"  警告: {warning}");
                    }
                }
            }
        }

        /// <summary>
        /// 光学设计绘图示例
        /// </summary>
        public static void OpticalDesignExample()
        {
            Database database = new Database();

            // 应用ISO光学设计模板
            var result = DrawingStandardManager.ApplyTemplateToDatabase(database, "ISO-Optical", true);
            if (result.Success)
            {
                // 创建透镜轮廓
                var lensOutline = new Circle
                {
                    center = new Vector2(0, 0),
                    radius = 25.0
                };

                // 应用透镜轮廓样式
                var lensLayer = database.layerTable["LensOutline"] as Layer;
                if (lensLayer != null)
                {
                    lensOutline.color = lensLayer.color;
                    lensOutline.lineWeight = lensLayer.lineWeight;
                    lensOutline.lineType = lensLayer.lineType;
                }

                (database.blockTable["ModelSpace"] as Block).AppendEntity(lensOutline);

                // 添加光轴
                var opticalAxis = new Line(new Vector2(-30, 0), new Vector2(30, 0));
                var axisStyle = DrawingStandardManager.GetRecommendedLineStyle("ISO", "Center");
                axisStyle?.ApplyToEntity(opticalAxis);
                (database.blockTable["ModelSpace"] as Block).AppendEntity(opticalAxis);

                // 添加镀膜标记
                var arCoatingStyle = DrawingStandardManager.GetRecommendedCoatingMarkStyle("ISO", CoatingType.AR);
                if (arCoatingStyle != null)
                {
                    var coatingMark = arCoatingStyle.CreateMarkInstance(new Vector2(20, 20));
                    (database.blockTable["ModelSpace"] as Block).AppendEntity(coatingMark);
                }

                // 添加尺寸标注
                var dimensionText = new Text
                {
                    Value = "Ø50±0.1",
                    Position = new Vector3(0, -35, 0),
                    TextStyle = "ISO-Dimension",
                    alignment = lcdb.TextAlignment.CenterMiddle
                };

                var dimensionStyle = DrawingStandardManager.GetRecommendedLineStyle("ISO", "Dimension");
                dimensionStyle?.ApplyToEntity(dimensionText);
                (database.blockTable["ModelSpace"] as Block).AppendEntity(dimensionText);
            }
        }

        /// <summary>
        /// GB机械绘图示例
        /// </summary>
        public static void GBMechanicalExample()
        {
            Database database = new Database();

            // 应用GB机械绘图模板
            var result = DrawingStandardManager.ApplyTemplateToDatabase(database, "GB-Mechanical", true);
            if (result.Success)
            {
                // 创建机械零件轮廓
                var partOutline = new List<Line>
                {
                    new Line(new Vector2(0, 0), new Vector2(50, 0)),
                    new Line(new Vector2(50, 0), new Vector2(50, 30)),
                    new Line(new Vector2(50, 30), new Vector2(0, 30)),
                    new Line(new Vector2(0, 30), new Vector2(0, 0))
                };

                var outlineStyle = DrawingStandardManager.GetRecommendedLineStyle("GB", "Outline");
                foreach (var line in partOutline)
                {
                    outlineStyle?.ApplyToEntity(line);
                    (database.blockTable["ModelSpace"] as Block).AppendEntity(line);
                }

                // 添加隐藏线
                var hiddenLine = new Line(new Vector2(25, 0), new Vector2(25, 30));
                var hiddenStyle = DrawingStandardManager.GetRecommendedLineStyle("GB", "Hidden");
                hiddenStyle?.ApplyToEntity(hiddenLine);
                (database.blockTable["ModelSpace"] as Block).AppendEntity(hiddenLine);

                // 添加中心线
                var centerLine = new Line(new Vector2(-10, 15), new Vector2(60, 15));
                var centerStyle = DrawingStandardManager.GetRecommendedLineStyle("GB", "Center");
                centerStyle?.ApplyToEntity(centerLine);
                (database.blockTable["ModelSpace"] as Block).AppendEntity(centerLine);

                // 添加尺寸标注
                var dimensionText = new Text
                {
                    Value = "50",
                    Position = new Vector3(25, -10, 0),
                    TextStyle = "GB-Dimension",
                    alignment = lcdb.TextAlignment.CenterMiddle
                };
                (database.blockTable["ModelSpace"] as Block).AppendEntity(dimensionText);

                // 添加技术要求
                var technicalNote = new Text
                {
                    Value = "材料：45号钢\n表面粗糙度：Ra3.2",
                    Position = new Vector3(70, 20, 0),
                    TextStyle = "GB-Technical",
                    alignment = lcdb.TextAlignment.LeftTop
                };
                (database.blockTable["ModelSpace"] as Block).AppendEntity(technicalNote);
            }
        }

        /// <summary>
        /// 颜色方案使用示例
        /// </summary>
        public static void ColorSchemeExample()
        {
            Database database = new Database();

            // 获取ISO标准
            var isoStandard = DrawingStandardManager.GetStandard("ISO");
            if (isoStandard != null)
            {
                // 获取技术绘图颜色方案
                var colorScheme = isoStandard.GetColorScheme("Basic");
                if (colorScheme != null)
                {
                    Console.WriteLine($"使用颜色方案: {colorScheme.Name}");

                    // 创建不同用途的图形元素
                    var outline = new Line(new Vector2(0, 0), new Vector2(100, 0))
                    {
                        color = colorScheme.PrimaryColor
                    };

                    var hiddenLine = new Line(new Vector2(0, 10), new Vector2(100, 10))
                    {
                        color = colorScheme.GetExtendedColor("HiddenLine")
                    };

                    var centerLine = new Line(new Vector2(50, -10), new Vector2(50, 20))
                    {
                        color = colorScheme.GetExtendedColor("CenterLine")
                    };

                    var dimensionLine = new Line(new Vector2(10, -5), new Vector2(90, -5))
                    {
                        color = colorScheme.GetExtendedColor("DimensionLine")
                    };

                    (database.blockTable["ModelSpace"] as Block).AppendEntity(outline);
                    (database.blockTable["ModelSpace"] as Block).AppendEntity(hiddenLine);
                    (database.blockTable["ModelSpace"] as Block).AppendEntity(centerLine);
                    (database.blockTable["ModelSpace"] as Block).AppendEntity(dimensionLine);
                }
            }
        }

        /// <summary>
        /// 自定义标准创建示例
        /// </summary>
        public static void CustomStandardExample()
        {
            // 创建自定义标准
            var customStandard = new CustomDrawingStandard("Company", "公司绘图标准", "1.0");

            // 注册自定义标准
            bool registered = DrawingStandardManager.RegisterCustomStandard(customStandard);
            if (registered)
            {
                Console.WriteLine("自定义标准注册成功");

                // 创建自定义模板
                var customTemplate = new StandardTemplate
                {
                    Name = "Company-Standard",
                    DisplayName = "公司标准模板",
                    Description = "公司内部使用的标准绘图模板",
                    Standard = customStandard,
                    PaperFormat = ISOPaperFormat.A3,
                    DefaultTextStyle = "Standard",
                    DefaultLineStyle = "Outline",
                    DefaultColorScheme = "Basic",
                    RecommendedLayers = new[] { "Outline", "Dimension", "Text", "Notes" }
                };

                bool templateRegistered = DrawingStandardManager.RegisterCustomTemplate(customTemplate);
                if (templateRegistered)
                {
                    Console.WriteLine("自定义模板注册成功");

                    // 使用自定义模板
                    Database database = new Database();
                    var result = DrawingStandardManager.ApplyTemplateToDatabase(database, "Company-Standard", true);
                    if (result.Success)
                    {
                        Console.WriteLine("自定义模板应用成功");
                    }
                }
            }
        }

        /// <summary>
        /// 标准比较示例
        /// </summary>
        public static void StandardComparisonExample()
        {
            // 获取所有可用标准
            var standards = DrawingStandardManager.GetStandardList();
            Console.WriteLine("可用的绘图标准：");
            foreach (var standard in standards)
            {
                Console.WriteLine($"- {standard.Name}: {standard.Description}");
                Console.WriteLine($"  版本: {standard.Version}");
                Console.WriteLine($"  线型样式: {standard.LineStyleCount}");
                Console.WriteLine($"  颜色方案: {standard.ColorSchemeCount}");
                Console.WriteLine($"  文本样式: {standard.TextStyleCount}");
                Console.WriteLine($"  镀膜标记样式: {standard.CoatingMarkStyleCount}");
                Console.WriteLine($"  标准图层: {standard.StandardLayerCount}");
                Console.WriteLine();
            }

            // 获取所有可用模板
            var templates = DrawingStandardManager.GetTemplateList();
            Console.WriteLine("可用的标准模板：");
            foreach (var template in templates)
            {
                Console.WriteLine($"- {template.DisplayName} ({template.Name})");
                Console.WriteLine($"  描述: {template.Description}");
                Console.WriteLine($"  基于标准: {template.StandardName}");
                Console.WriteLine($"  推荐图层数: {template.RecommendedLayerCount}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 批量样式应用示例
        /// </summary>
        public static void BatchStyleApplicationExample()
        {
            Database database = new Database();

            // 应用ISO标准
            DrawingStandardManager.ApplyStandardToDatabase(database, "ISO", true);

            // 创建多个图形实体
            var entities = new List<Entity>
            {
                new Line(new Vector2(0, 0), new Vector2(100, 0)),     // 轮廓线
                new Line(new Vector2(0, 10), new Vector2(100, 10)),   // 隐藏线
                new Line(new Vector2(50, -10), new Vector2(50, 20)),  // 中心线
                new Circle { center = new Vector2(25, 5), radius = 5 }, // 圆形
                new Text { Value = "示例文本", Position = new Vector3(0, -20, 0) }
            };

            // 批量应用样式
            var styleUsages = new[] { "Outline", "Hidden", "Center", "Outline", "Standard" };
            for (int i = 0; i < entities.Count && i < styleUsages.Length; i++)
            {
                var lineStyle = DrawingStandardManager.GetRecommendedLineStyle("ISO", styleUsages[i]);
                lineStyle?.ApplyToEntity(entities[i]);

                (database.blockTable["ModelSpace"] as Block).AppendEntity(entities[i]);
            }

            Console.WriteLine($"批量应用样式完成，共处理 {entities.Count} 个实体");
        }
    }

    /// <summary>
    /// 自定义绘图标准示例
    /// </summary>
    public class CustomDrawingStandard : DrawingStandard
    {
        public CustomDrawingStandard(string name, string description, string version)
            : base(name, description, version)
        {
        }

        protected override void InitializeStandard()
        {
            // 自定义线型样式
            AddLineStyle("Outline", LineType.Solid, LineWeight.LineWeight050, Color.FromRGB(0, 0, 0));
            AddLineStyle("Dimension", LineType.Solid, LineWeight.LineWeight025, Color.FromRGB(255, 0, 0));

            // 自定义颜色方案
            AddColorScheme("Basic", Color.FromRGB(0, 0, 0), Color.FromRGB(128, 128, 128), Color.FromRGB(255, 0, 0));

            // 自定义文本样式
            var standardTextStyle = new TextStyle("Company-Standard", "Arial", FontStyle.Regular)
            {
                Height = 3.0,
                WidthFactor = 1.0
            };
            AddTextStyle("Standard", standardTextStyle);

            // 自定义图层
            AddStandardLayer("Outline", Color.FromRGB(0, 0, 0), LineWeight.LineWeight050, LineType.Solid, "轮廓线");
            AddStandardLayer("Dimension", Color.FromRGB(255, 0, 0), LineWeight.LineWeight025, LineType.Solid, "尺寸线");
            AddStandardLayer("Text", Color.FromRGB(0, 0, 0), LineWeight.LineWeight020, LineType.Solid, "文字");
            AddStandardLayer("Notes", Color.FromRGB(0, 128, 0), LineWeight.LineWeight020, LineType.Solid, "注释");
        }
    }
}