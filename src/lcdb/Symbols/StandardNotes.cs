using System;
using System.Collections.Generic;
using System.Linq;
using LitMath;
using lcdb;
using lcdb.Colors;
using lcdb.Standards;

namespace lcdb.Symbols
{
    /// <summary>
    /// 标准注释库
    /// 提供光学图纸的标准技术要求和注释模板
    /// </summary>
    public static class StandardNotes
    {
        #region 预定义注释模板

        /// <summary>
        /// 获取通用技术要求
        /// </summary>
        public static List<string> GetGeneralNotes(DrawingStandard standard = null)
        {
            var notes = new List<string>();

            if (standard is ISO10110Standard)
            {
                notes.Add("1. 除非另有说明，所有尺寸单位为毫米(mm)");
                notes.Add("2. 表面质量按ISO 10110-7标准");
                notes.Add("3. 公差按ISO 10110-5标准");
                notes.Add("4. 材料折射率按d光(587.6nm)测量");
                notes.Add("5. 环境试验按ISO 9022标准");
                notes.Add("6. 清洁度要求按MIL-PRF-13830B");
                notes.Add("7. 包装按光学元件包装标准");
            }
            else if (standard is GB13323Standard)
            {
                notes.Add("1. 除非另有说明，所有尺寸单位为毫米(mm)");
                notes.Add("2. 表面疵病按GB/T 2831执行");
                notes.Add("3. 面形偏差按GB/T 13323-2009标准");
                notes.Add("4. 材料牌号按GB/T 903标准");
                notes.Add("5. 检验方法按相关国标执行");
                notes.Add("6. 清洁度等级按GB/T 13324");
                notes.Add("7. 未注公差按IT11级");
            }
            else
            {
                // 默认注释
                notes.Add("1. 除非另有说明，所有尺寸单位为毫米(mm)");
                notes.Add("2. 未注公差按一般公差");
                notes.Add("3. 表面质量按相关标准");
                notes.Add("4. 材料特性见材料规格表");
                notes.Add("5. 检验按相关检验标准");
            }

            return notes;
        }

        /// <summary>
        /// 获取光学零件专用注释
        /// </summary>
        public static List<string> GetOpticalComponentNotes(ComponentType componentType)
        {
            var notes = new List<string>();

            switch (componentType)
            {
                case ComponentType.Lens:
                    notes.AddRange(GetLensNotes());
                    break;
                
                case ComponentType.Mirror:
                    notes.AddRange(GetMirrorNotes());
                    break;
                
                case ComponentType.Prism:
                    notes.AddRange(GetPrismNotes());
                    break;
                
                case ComponentType.Window:
                    notes.AddRange(GetWindowNotes());
                    break;
                
                case ComponentType.Filter:
                    notes.AddRange(GetFilterNotes());
                    break;
            }

            return notes;
        }

        /// <summary>
        /// 获取制造工艺注释
        /// </summary>
        public static List<string> GetManufacturingNotes(ManufacturingProcess process)
        {
            var notes = new List<string>();

            switch (process)
            {
                case ManufacturingProcess.Grinding:
                    notes.Add("• 粗磨后应力释放24小时");
                    notes.Add("• 精磨采用金刚石砂轮");
                    notes.Add("• 磨削液pH值控制在7-8");
                    break;
                
                case ManufacturingProcess.Polishing:
                    notes.Add("• 抛光采用沥青或聚氨酯抛光模");
                    notes.Add("• 抛光液：氧化铈悬浮液");
                    notes.Add("• 最终清洗：去离子水+乙醇");
                    break;
                
                case ManufacturingProcess.Coating:
                    notes.Add("• 镀膜前超声波清洗30分钟");
                    notes.Add("• 真空度优于5×10⁻⁵ Pa");
                    notes.Add("• 基片温度：200±10°C");
                    notes.Add("• 镀膜后100°C烘烤2小时");
                    break;
                
                case ManufacturingProcess.DiamondTurning:
                    notes.Add("• 金刚石刀具半径：1mm");
                    notes.Add("• 主轴转速：1000-3000 rpm");
                    notes.Add("• 进给速度：1-5 mm/min");
                    notes.Add("• 切削深度：1-5 μm");
                    break;
            }

            return notes;
        }

        /// <summary>
        /// 获取检验要求注释
        /// </summary>
        public static List<string> GetInspectionNotes(InspectionLevel level)
        {
            var notes = new List<string>();

            switch (level)
            {
                case InspectionLevel.Standard:
                    notes.Add("检验项目：");
                    notes.Add("□ 外形尺寸");
                    notes.Add("□ 表面质量");
                    notes.Add("□ 面形偏差");
                    notes.Add("□ 中心偏差");
                    break;
                
                case InspectionLevel.Precision:
                    notes.Add("精密检验项目：");
                    notes.Add("□ 干涉仪测量面形");
                    notes.Add("□ 轮廓仪测量粗糙度");
                    notes.Add("□ 三坐标测量尺寸");
                    notes.Add("□ 中心偏测量仪");
                    notes.Add("□ 分光光度计测透过率");
                    break;
                
                case InspectionLevel.Critical:
                    notes.Add("关键检验项目：");
                    notes.Add("□ 全口径干涉测量");
                    notes.Add("□ 亚表面损伤检测");
                    notes.Add("□ 应力双折射测量");
                    notes.Add("□ 激光损伤阈值测试");
                    notes.Add("□ 环境试验后复检");
                    break;
            }

            return notes;
        }

        #endregion

        #region 注释格式化和布局

        /// <summary>
        /// 创建格式化的技术要求文本实体
        /// </summary>
        public static List<Entity> CreateFormattedNotes(List<string> notes, Vector2 startPosition, NoteFormatting formatting = null)
        {
            var entities = new List<Entity>();
            
            if (formatting == null)
            {
                formatting = new NoteFormatting();
            }

            double currentY = startPosition.Y;

            // 添加标题
            if (!string.IsNullOrEmpty(formatting.Title))
            {
                var titleText = new Text
                {
                    Value = formatting.Title,
                    Position = new Vector3(startPosition.X, currentY, 0),
                    Height = formatting.TitleHeight,
                    alignment = TextAlignment.LeftMiddle,
                    color = Color.ByLayer
                };
                entities.Add(titleText);
                
                currentY -= formatting.TitleHeight + formatting.LineSpacing;

                // 添加下划线
                if (formatting.UnderlineTitle)
                {
                    var underline = new Line(
                        new Vector2(startPosition.X, currentY + formatting.LineSpacing / 2),
                        new Vector2(startPosition.X + formatting.Width, currentY + formatting.LineSpacing / 2)
                    );
                    underline.lineWeight = LineWeight.LineWeight025;
                    entities.Add(underline);
                    
                    currentY -= formatting.LineSpacing;
                }
            }

            // 添加注释内容
            foreach (var note in notes)
            {
                var noteText = new Text
                {
                    Value = note,
                    Position = new Vector3(startPosition.X + formatting.IndentSize, currentY, 0),
                    Height = formatting.TextHeight,
                    alignment = TextAlignment.LeftMiddle,
                    color = Color.ByLayer
                };
                entities.Add(noteText);
                
                currentY -= formatting.TextHeight + formatting.LineSpacing;
            }

            // 添加边框
            if (formatting.ShowBorder)
            {
                var borderHeight = startPosition.Y - currentY + formatting.LineSpacing;
                var border = new Polyline();
                border.closed = true;
                border.AddVertexAt(0, new Vector2(startPosition.X - 2, startPosition.Y + 2));
                border.AddVertexAt(1, new Vector2(startPosition.X + formatting.Width + 2, startPosition.Y + 2));
                border.AddVertexAt(2, new Vector2(startPosition.X + formatting.Width + 2, currentY - 2));
                border.AddVertexAt(3, new Vector2(startPosition.X - 2, currentY - 2));
                border.lineWeight = LineWeight.LineWeight025;
                border.lineType = LineType.Dash;
                entities.Add(border);
            }

            return entities;
        }

        /// <summary>
        /// 创建表格式注释
        /// </summary>
        public static List<Entity> CreateTableNotes(List<TableRow> rows, Vector2 position, double columnWidth = 30.0)
        {
            var entities = new List<Entity>();
            
            double rowHeight = 6.0;
            double textHeight = 2.5;
            double currentY = position.Y;
            
            // 绘制表格
            foreach (var row in rows)
            {
                double currentX = position.X;
                
                // 绘制单元格
                foreach (var cell in row.Cells)
                {
                    // 单元格边框
                    var cellBorder = new Polyline();
                    cellBorder.closed = true;
                    cellBorder.AddVertexAt(0, new Vector2(currentX, currentY));
                    cellBorder.AddVertexAt(1, new Vector2(currentX + columnWidth, currentY));
                    cellBorder.AddVertexAt(2, new Vector2(currentX + columnWidth, currentY - rowHeight));
                    cellBorder.AddVertexAt(3, new Vector2(currentX, currentY - rowHeight));
                    cellBorder.lineWeight = LineWeight.LineWeight025;
                    entities.Add(cellBorder);
                    
                    // 单元格文本
                    var cellText = new Text
                    {
                        Value = cell,
                        Position = new Vector3(currentX + columnWidth / 2, currentY - rowHeight / 2, 0),
                        Height = textHeight,
                        alignment = TextAlignment.CenterMiddle,
                        color = Color.ByLayer
                    };
                    entities.Add(cellText);
                    
                    currentX += columnWidth;
                }
                
                currentY -= rowHeight;
            }
            
            return entities;
        }

        /// <summary>
        /// 创建参数列表
        /// </summary>
        public static List<Entity> CreateParameterList(Dictionary<string, string> parameters, Vector2 position)
        {
            var entities = new List<Entity>();
            
            double labelWidth = 60.0;
            double valueWidth = 40.0;
            double rowHeight = 4.0;
            double textHeight = 2.5;
            double currentY = position.Y;
            
            foreach (var param in parameters)
            {
                // 参数名称
                var labelText = new Text
                {
                    Value = param.Key + ":",
                    Position = new Vector3(position.X, currentY, 0),
                    Height = textHeight,
                    alignment = TextAlignment.LeftMiddle,
                    color = Color.ByLayer
                };
                entities.Add(labelText);
                
                // 参数值
                var valueText = new Text
                {
                    Value = param.Value,
                    Position = new Vector3(position.X + labelWidth, currentY, 0),
                    Height = textHeight,
                    alignment = TextAlignment.LeftMiddle,
                    color = Color.ByLayer
                };
                entities.Add(valueText);
                
                // 分隔线
                if (currentY != position.Y) // 不在第一行
                {
                    var separator = new Line(
                        new Vector2(position.X, currentY + rowHeight / 2),
                        new Vector2(position.X + labelWidth + valueWidth, currentY + rowHeight / 2)
                    );
                    separator.lineWeight = LineWeight.LineWeight015;
                    separator.lineType = LineType.Dash;
                    entities.Add(separator);
                }
                
                currentY -= rowHeight;
            }
            
            return entities;
        }

        #endregion

        #region 专用注释内容

        private static List<string> GetLensNotes()
        {
            return new List<string>
            {
                "透镜专用要求：",
                "• 光学面不得有划痕、麻点、气泡",
                "• 中心厚度公差：±0.05mm",
                "• 边缘厚度最小值：1.0mm",
                "• 偏心差：<3'",
                "• 倒角：0.2×45°（防崩边）"
            };
        }

        private static List<string> GetMirrorNotes()
        {
            return new List<string>
            {
                "反射镜专用要求：",
                "• 反射面平面度：λ/10",
                "• 反射率：R>99%@设计波长",
                "• 基底材料：熔石英或微晶玻璃",
                "• 镀膜：增强铝膜或介质膜",
                "• 背面需做消光处理"
            };
        }

        private static List<string> GetPrismNotes()
        {
            return new List<string>
            {
                "棱镜专用要求：",
                "• 角度公差：±30\"",
                "• 角锥误差：<10\"",
                "• 表面平面度：λ/4",
                "• 边长公差：±0.1mm",
                "• 所有边缘倒角处理"
            };
        }

        private static List<string> GetWindowNotes()
        {
            return new List<string>
            {
                "窗口片专用要求：",
                "• 平行度：<30\"",
                "• 表面平面度：λ/4",
                "• 透过率：T>99%@设计波段",
                "• 双面增透镀膜",
                "• 楔角：<5'"
            };
        }

        private static List<string> GetFilterNotes()
        {
            return new List<string>
            {
                "滤光片专用要求：",
                "• 中心波长公差：±2nm",
                "• 半带宽公差：±5nm",
                "• 峰值透过率：>90%",
                "• 截止深度：OD4",
                "• 入射角度：0°±5°"
            };
        }

        #endregion

        #region 特殊标记和符号

        /// <summary>
        /// 创建修订标记
        /// </summary>
        public static List<Entity> CreateRevisionMark(Vector2 position, string revision, string date, string description)
        {
            var entities = new List<Entity>();
            
            // 修订符号（三角形）
            var triangle = new Polyline();
            triangle.closed = true;
            triangle.AddVertexAt(0, position);
            triangle.AddVertexAt(1, position + new Vector2(5, 0));
            triangle.AddVertexAt(2, position + new Vector2(2.5, 4));
            triangle.lineWeight = LineWeight.LineWeight035;
            entities.Add(triangle);
            
            // 修订号
            var revText = new Text
            {
                Value = revision,
                Position = new Vector3(position.X + 2.5, position.Y + 1.5, 0),
                Height = 2.0,
                alignment = TextAlignment.CenterMiddle,
                color = Color.ByLayer
            };
            entities.Add(revText);
            
            // 日期和说明
            var infoText = new Text
            {
                Value = $"{date} - {description}",
                Position = new Vector3(position.X + 8, position.Y + 2, 0),
                Height = 2.0,
                alignment = TextAlignment.LeftMiddle,
                color = Color.ByLayer
            };
            entities.Add(infoText);
            
            return entities;
        }

        /// <summary>
        /// 创建关键尺寸标记
        /// </summary>
        public static List<Entity> CreateCriticalDimensionMark(Vector2 position)
        {
            var entities = new List<Entity>();
            
            // 菱形符号
            var diamond = new Polyline();
            diamond.closed = true;
            diamond.AddVertexAt(0, position + new Vector2(0, -3));
            diamond.AddVertexAt(1, position + new Vector2(3, 0));
            diamond.AddVertexAt(2, position + new Vector2(0, 3));
            diamond.AddVertexAt(3, position + new Vector2(-3, 0));
            diamond.lineWeight = LineWeight.LineWeight050;
            diamond.color = Color.FromRGB(255, 0, 0);
            entities.Add(diamond);
            
            return entities;
        }

        /// <summary>
        /// 创建参考尺寸标记
        /// </summary>
        public static List<Entity> CreateReferenceDimensionMark(Vector2 position, string value)
        {
            var entities = new List<Entity>();
            
            // 括号
            var leftBracket = new Text
            {
                Value = "(",
                Position = new Vector3(position.X - 3, position.Y, 0),
                Height = 3.0,
                alignment = TextAlignment.CenterMiddle
            };
            entities.Add(leftBracket);
            
            var rightBracket = new Text
            {
                Value = ")",
                Position = new Vector3(position.X + 3, position.Y, 0),
                Height = 3.0,
                alignment = TextAlignment.CenterMiddle
            };
            entities.Add(rightBracket);
            
            // 数值
            var valueText = new Text
            {
                Value = value,
                Position = new Vector3(position.X, position.Y, 0),
                Height = 2.5,
                alignment = TextAlignment.CenterMiddle
            };
            entities.Add(valueText);
            
            return entities;
        }

        #endregion
    }

    #region 辅助类和枚举

    /// <summary>
    /// 注释格式化选项
    /// </summary>
    public class NoteFormatting
    {
        public string Title { get; set; } = "技术要求";
        public double TitleHeight { get; set; } = 3.5;
        public double TextHeight { get; set; } = 2.5;
        public double LineSpacing { get; set; } = 1.0;
        public double IndentSize { get; set; } = 0.0;
        public double Width { get; set; } = 100.0;
        public bool ShowBorder { get; set; } = false;
        public bool UnderlineTitle { get; set; } = true;
    }

    /// <summary>
    /// 表格行
    /// </summary>
    public class TableRow
    {
        public List<string> Cells { get; set; } = new List<string>();
        
        public TableRow(params string[] cells)
        {
            Cells.AddRange(cells);
        }
    }

    /// <summary>
    /// 零件类型
    /// </summary>
    public enum ComponentType
    {
        Lens,
        Mirror,
        Prism,
        Window,
        Filter,
        Beamsplitter,
        Polarizer,
        Waveplate
    }

    /// <summary>
    /// 制造工艺
    /// </summary>
    public enum ManufacturingProcess
    {
        Grinding,
        Polishing,
        Coating,
        DiamondTurning,
        Molding,
        Etching
    }

    /// <summary>
    /// 检验级别
    /// </summary>
    public enum InspectionLevel
    {
        Standard,
        Precision,
        Critical
    }

    #endregion
}