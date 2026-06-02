using lcdb.Colors;
using System;
using System.Collections.Generic;
using System.Drawing;
using LitMath;
using lcdb;
using lcdb.Interfaces;
using lcdb.Symbols;
using OtoCAD;

namespace lcdb.Annotation
{
    /// <summary>
    /// 材料缺陷标记实现
    /// 用于标注光学材料的内部缺陷，包括气泡、条纹、应力双折射?    /// </summary>
    public class MaterialDefectMark : Entity, IOpticalMark
    {
        public override string className => "MaterialDefectMark";

        #region IOpticalMark 接口属性?
        /// <summary>
        /// 标记类型
        /// </summary>
        public OpticalMarkType MarkType => OpticalMarkType.Bubble; // 根据缺陷类型动态返
        /// <summary>
        /// 标记位置
        /// </summary>
        public Vector2 Position { get; set; } = new Vector2(0, 0);

        /// <summary>
        /// 标记大小/比例
        /// </summary>
        public double Scale { get; set; } = 1.0;

        /// <summary>
        /// 标记文本内容
        /// </summary>
        public string MarkText { get; set; } = "气泡:??Φ0.05mm";

        /// <summary>
        /// 是否显示文本
        /// </summary>
        public bool ShowText { get; set; } = true;

        /// <summary>
        /// 文本偏移量?        /// </summary>
        public Vector2 TextOffset { get; set; } = new Vector2(0, -30);

        /// <summary>
        /// 标记旋转角度（度）?        /// </summary>
        public double Rotation { get; set; } = 0.0;

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible { get; set; } = true;

        #endregion

        #region 材料缺陷特定属性?
        /// <summary>
        /// 缺陷类型
        /// </summary>
        public MaterialDefectType DefectType { get; set; } = MaterialDefectType.Bubble;

        /// <summary>
        /// 气泡等级（直径，mm?        /// </summary>
        public double BubbleGrade { get; set; } = 0.05;

        /// <summary>
        /// 气泡数量限制
        /// </summary>
        public int BubbleCount { get; set; } = 3;

        /// <summary>
        /// 条纹等级（ΔN×厚度，nm/cm?        /// </summary>
        public double StriaGrade { get; set; } = 2.0;

        /// <summary>
        /// 应力双折射值（nm/cm?        /// </summary>
        public double StressBirefringence { get; set; } = 5.0;

        /// <summary>
        /// 包裹体等级?        /// </summary>
        public InclusionGrade InclusionLevel { get; set; } = InclusionGrade.Grade3;

        /// <summary>
        /// 检测孔径（mm?        /// </summary>
        public double InspectionAperture { get; set; } = 25.0;

        /// <summary>
        /// 检测波长（nm?        /// </summary>
        public double InspectionWavelength { get; set; } = 589.3;

        /// <summary>
        /// 检测标准?        /// </summary>
        public string TestStandard { get; set; } = "GB/T 1185-2006";

        /// <summary>
        /// 标记框架大小
        /// </summary>
        public Vector2 FrameSize { get; set; } = new Vector2(50, 40);

        /// <summary>
        /// 严重性等级?        /// </summary>
        public DefectSeverity Severity { get; set; } = DefectSeverity.Minor;

        #endregion

        private List<Entity> _markEntities = new List<Entity>();

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                double totalWidth = FrameSize.X + (ShowText ? Math.Abs(TextOffset.X) + 100 : 0);
                double totalHeight = FrameSize.Y + (ShowText ? Math.Abs(TextOffset.Y) + 15 : 0);
                return new Bounding(Position, totalWidth, totalHeight);
            }
        }

        #region 构造函数
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public MaterialDefectMark()
        {
            UpdateMarkText();
        }

        /// <summary>
        /// 带参数构造函数
        /// </summary>
        public MaterialDefectMark(Vector2 position, MaterialDefectType defectType)
        {
            Position = position;
            DefectType = defectType;
            UpdateMarkText();
        }

        #endregion

        #region 核心方法

        /// <summary>
        /// 更新标记文本
        /// </summary>
        private void UpdateMarkText()
        {
            switch (DefectType)
            {
                case MaterialDefectType.Bubble:
                    MarkText = $"气泡:≤{BubbleCount}?Φ{BubbleGrade:F2}mm";
                    break;
                case MaterialDefectType.Stria:
                    MarkText = $"条纹:≤{StriaGrade:F1}nm/cm";
                    break;
                case MaterialDefectType.StressBirefringence:
                    MarkText = $"应力:≤{StressBirefringence:F1}nm/cm";
                    break;
                case MaterialDefectType.Inclusion:
                    MarkText = $"包裹体:{InclusionLevel}级";
                    break;
                case MaterialDefectType.Combined:
                    MarkText = $"综合缺陷:{Severity}";
                    break;
            }
        }

        /// <summary>
        /// 生成标记图形
        /// </summary>
        protected void Generate()
        {
            _markEntities.Clear();

            // 生成主标记符号
            GenerateMainSymbol();

            // 生成文本
            if (ShowText)
            {
                GenerateText();
            }
        }

        /// <summary>
        /// 生成主标记符号 — 矩形框 + 结构化 ISO 缺陷代码标签.
        /// 旧实现 (按 DefectType 切换 5 种框架 + 5 种缺陷子图标 + 参数缩写) 不符合 ISO 10110-3/4 严格格式, 已移除.
        /// 严格 ISO 标记应使用专用的 ISO10110_3Mark (气泡) / ISO10110_4Mark (条纹). 此 mark 作工厂通用缺陷备注.
        /// </summary>
        private void GenerateMainSymbol()
        {
            var markColor = GetMarkColor();
            double halfWidth = FrameSize.X * 0.5 * Scale;
            double halfHeight = FrameSize.Y * 0.5 * Scale;

            GenerateRectangleFrame(halfWidth, halfHeight, markColor);
            GenerateDefectLabel(halfHeight, markColor);
        }

        /// <summary>
        /// 矩形框 (4 条 Line, 应用 Rotation).
        /// </summary>
        private void GenerateRectangleFrame(double halfWidth, double halfHeight, lcdb.Colors.Color color)
        {
            var pts = new[]
            {
                new Vector2(Position.X - halfWidth, Position.Y - halfHeight),
                new Vector2(Position.X + halfWidth, Position.Y - halfHeight),
                new Vector2(Position.X + halfWidth, Position.Y + halfHeight),
                new Vector2(Position.X - halfWidth, Position.Y + halfHeight),
            };
            if (Rotation != 0)
            {
                double rad = Rotation * Math.PI / 180;
                for (int i = 0; i < 4; i++) pts[i] = Vector2.RotateInRadian(pts[i], Position, rad);
            }
            for (int i = 0; i < 4; i++)
            {
                _markEntities.Add(new Line(pts[i], pts[(i + 1) % 4]) { color = color });
            }
        }

        /// <summary>
        /// 框内 2 行结构化文字: 第一行 ISO 缺陷代码, 第二行 严重程度.
        /// </summary>
        private void GenerateDefectLabel(double halfHeight, lcdb.Colors.Color color)
        {
            var line1 = new Text
            {
                Value = GetIsoDefectCode(),
                Position = new Vector3(Position.X, Position.Y + halfHeight * 0.35, 0.0),
                Height = 4 * Scale,
                color = color,
                alignment = lcdb.TextAlignment.CenterMiddle,
            };
            _markEntities.Add(line1);

            var line2 = new Text
            {
                Value = Severity.ToString(),
                Position = new Vector3(Position.X, Position.Y - halfHeight * 0.35, 0.0),
                Height = 3 * Scale,
                color = color,
                alignment = lcdb.TextAlignment.CenterMiddle,
            };
            _markEntities.Add(line2);
        }

        /// <summary>
        /// ISO 10110 缺陷代码 (1/N×A 气泡, 2/A 条纹, 0/A 应力, etc.). 默认值 0 时返回 "—".
        /// </summary>
        private string GetIsoDefectCode()
        {
            switch (DefectType)
            {
                case MaterialDefectType.Bubble:
                    return BubbleCount > 0 ? $"1/{BubbleCount}×{BubbleGrade:F2}" : "1/—";
                case MaterialDefectType.Stria:
                    return StriaGrade > 0 ? $"2/{StriaGrade:F1}" : "2/—";
                case MaterialDefectType.StressBirefringence:
                    return StressBirefringence > 0 ? $"0/{StressBirefringence:F1}" : "0/—";
                case MaterialDefectType.Inclusion:
                    return $"1/{InclusionLevel}";
                case MaterialDefectType.Combined:
                    return "0+1+2";
                default:
                    return "—";
            }
        }

        /// <summary>
        /// 框内 ISO 代码即完整规范, 外部仅注单位 (与 ISO 10110-3/4/0 一致).
        /// 详细 Chinese 描述移到 PropertyManager / Tooltip, 不渲染.
        /// </summary>
        private void GenerateText()
        {
            var textPosition = Position + TextOffset;
            string unit = DefectType switch
            {
                MaterialDefectType.Bubble              => "Ø mm",
                MaterialDefectType.Stria               => "nm/cm",
                MaterialDefectType.StressBirefringence => "nm/cm",
                MaterialDefectType.Inclusion           => "级",
                _                                      => "",
            };
            if (string.IsNullOrEmpty(unit)) return;
            _markEntities.Add(new Text
            {
                Value = unit,
                Position = new Vector3(textPosition.X, textPosition.Y, 0.0),
                Height = 4 * Scale,
                color = GetMarkColor(),
                alignment = lcdb.TextAlignment.CenterMiddle,
            });
        }

        /// <summary>
        /// 获取详细描述
        /// </summary>
        private string GetDetailedDescription()
        {
            switch (DefectType)
            {
                case MaterialDefectType.Bubble:
                    return $"气泡缺陷: ≤{BubbleCount}? 直径≤Φ{BubbleGrade:F2}mm";
                case MaterialDefectType.Stria:
                    return $"条纹缺陷: ≤{StriaGrade:F1}nm/cm ({TestStandard})";
                case MaterialDefectType.StressBirefringence:
                    return $"应力双折? ≤{StressBirefringence:F1}nm/cm @{InspectionWavelength:F1}nm";
                case MaterialDefectType.Inclusion:
                    return $"包裹体缺? {InclusionLevel}?(Φ{InspectionAperture:F0}mm检查?";
                default:
                    return $"材料缺陷: {Severity} ({TestStandard})";
            }
        }

        /// <summary>
        /// <summary>
        /// 获取标记颜色 — Story 10-2 WCAG AA 合规 (白底对比度 ≥ 4.5:1):
        ///   Critical → Crimson (220,20,60) 5.9:1  (替 Red 4.0:1)
        ///   Major    → SaddleBrown (139,69,19) 7.1:1  (替 Orange 2.5:1)
        ///   Minor    → #806000 深橄榄金 5.9:1  (替 Yellow 1.07:1 — 看不见!)
        ///   Negligible → DarkGreen (0,100,0) 7.5:1  (替 Green 3.7:1)
        ///   默认       → MediumBlue (0,0,205)
        /// </summary>
        private lcdb.Colors.Color GetMarkColor()
        {
            switch (Severity)
            {
                case DefectSeverity.Critical:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Crimson);
                case DefectSeverity.Major:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.SaddleBrown);
                case DefectSeverity.Minor:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.FromArgb(128, 96, 0));
                case DefectSeverity.Negligible:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.DarkGreen);
                default:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.MediumBlue);
            }
        }

        #endregion

        
        #region 绘制方法重写

        /// <summary>
        /// 重写Entity的Draw方法以绘制光学标记
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            // 确保生成了图形元素
            if (_markEntities.Count == 0)
            {
                Generate();
            }

            // 绘制所有子实体
            foreach (var entity in _markEntities)
            {
                entity.Draw(gd);
            }
        }

        #endregion

#region IOpticalMark 接口实现

#if WINDOWS
        /// <summary>
        /// 绘制标记
        /// </summary>
        public void Draw(Graphics g, float scale)
        {
            var position = new PointF((float)Position.X, (float)Position.Y);
            DrawMaterialDefectSymbol(g, position, scale * (float)Scale);
        }

        /// <summary>
        /// 绘制材料缺陷符号
        /// </summary>
        private void DrawMaterialDefectSymbol(Graphics g, PointF position, float scale)
        {
            using (var pen = new Pen(GetMarkColor().ToDrawingColor(), 1.5f * scale))
            using (var brush = new SolidBrush(GetMarkColor().ToDrawingColor()))
            using (var font = new Font("Arial", 6 * scale, System.Drawing.FontStyle.Bold))
            {
                float width = (float)FrameSize.X * scale;
                float height = (float)FrameSize.Y * scale;

                // 根据缺陷类型绘制不同框架
                switch (DefectType)
                {
                    case MaterialDefectType.Bubble:
                        float radius = Math.Min(width, height) * 0.4f;
                        g.DrawEllipse(pen, position.X - radius, position.Y - radius, radius * 2, radius * 2);
                        break;
                    case MaterialDefectType.Stria:
                        DrawWaveFrame(g, pen, position, width, height);
                        break;
                    default:
                        RectangleF rect = new RectangleF(position.X - width/2, position.Y - height/2, width, height);
                        g.DrawRectangle(pen, Rectangle.Round(rect));
                        break;
                }

                // 绘制缺陷符号
                DrawDefectPattern(g, pen, position, scale);

                // 绘制 ISO 缺陷代码 (与 Generate() 中 GetIsoDefectCode 一致)
                string paramStr = GetIsoDefectCode();
                g.DrawString(paramStr, font, brush, position.X, position.Y + height * 0.3f,
                           new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }
        }

        /// <summary>
        /// 绘制波浪框架
        /// </summary>
        private void DrawWaveFrame(Graphics g, Pen pen, PointF position, float width, float height)
        {
            int points = 20;
            PointF[] topWave = new PointF[points + 1];
            PointF[] bottomWave = new PointF[points + 1];
            
            for (int i = 0; i <= points; i++)
            {
                float t = (float)i / points;
                float x = position.X + (t - 0.5f) * width;
                topWave[i] = new PointF(x, position.Y + height/2 + (float)Math.Sin(t * Math.PI * 4) * height * 0.1f);
                bottomWave[i] = new PointF(x, position.Y - height/2 - (float)Math.Sin(t * Math.PI * 4) * height * 0.1f);
            }
            
            // 绘制波浪线
            for (int i = 0; i < points; i++)
            {
                g.DrawLine(pen, topWave[i], topWave[i + 1]);
                g.DrawLine(pen, bottomWave[i], bottomWave[i + 1]);
            }
            
            // 绘制侧边
            g.DrawLine(pen, topWave[0], bottomWave[0]);
            g.DrawLine(pen, topWave[points], bottomWave[points]);
        }

        /// <summary>
        /// 绘制缺陷图案
        /// </summary>
        private void DrawDefectPattern(Graphics g, Pen pen, PointF position, float scale)
        {
            float symbolSize = 8 * scale;
            
            switch (DefectType)
            {
                case MaterialDefectType.Bubble:
                    for (int i = 0; i < Math.Min(BubbleCount, 3); i++)
                    {
                        float x = position.X + (i - 1) * symbolSize * 0.3f;
                        float y = position.Y + (float)Math.Sin(i) * symbolSize * 0.2f;
                        float r = (float)BubbleGrade * 20; // 放大显示
                        g.DrawEllipse(pen, x - r, y - r, r * 2, r * 2);
                    }
                    break;
                    
                case MaterialDefectType.Stria:
                    for (int i = 0; i < 3; i++)
                    {
                        float y = position.Y + (i - 1) * symbolSize * 0.3f;
                        for (int j = 0; j < 10; j++)
                        {
                            float t1 = (float)j / 10;
                            float t2 = (float)(j + 1) / 10;
                            float x1 = position.X + (t1 - 0.5f) * symbolSize;
                            float x2 = position.X + (t2 - 0.5f) * symbolSize;
                            float dy1 = (float)Math.Sin(t1 * Math.PI * 6) * symbolSize * 0.1f;
                            float dy2 = (float)Math.Sin(t2 * Math.PI * 6) * symbolSize * 0.1f;
                            g.DrawLine(pen, x1, y + dy1, x2, y + dy2);
                        }
                    }
                    break;
                    
                case MaterialDefectType.StressBirefringence:
                    // 绘制应力箭头
                    g.DrawLine(pen, position.X - symbolSize * 0.4f, position.Y, position.X - symbolSize * 0.1f, position.Y);
                    g.DrawLine(pen, position.X + symbolSize * 0.4f, position.Y, position.X + symbolSize * 0.1f, position.Y);
                    g.DrawLine(pen, position.X, position.Y - symbolSize * 0.4f, position.X, position.Y - symbolSize * 0.1f);
                    g.DrawLine(pen, position.X, position.Y + symbolSize * 0.4f, position.X, position.Y + symbolSize * 0.1f);
                    break;
            }
        }
#endif

        /// <summary>
        /// 获取标记边界框?        /// </summary>
        public RectangleF GetBounds()
        {
            var bound = bounding;
            return new RectangleF((float)bound.left, (float)bound.bottom, (float)bound.width, (float)bound.height);
        }

        /// <summary>
        /// 验证标记数据
        /// </summary>
        public bool Validate()
        {
            // 验证气泡参数
            if (DefectType == MaterialDefectType.Bubble)
            {
                if (BubbleGrade < 0 || BubbleGrade > 1.0)
                    return false;
                if (BubbleCount < 0 || BubbleCount > 100)
                    return false;
            }

            // 验证条纹参数
            if (DefectType == MaterialDefectType.Stria)
            {
                if (StriaGrade < 0 || StriaGrade > 100.0)
                    return false;
            }

            // 验证应力双折射参
            if (DefectType == MaterialDefectType.StressBirefringence)
            {
                if (StressBirefringence < 0 || StressBirefringence > 1000.0)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 获取标记属性?        /// </summary>
        public Dictionary<string, object> GetProperties()
        {
            return new Dictionary<string, object>
            {
                { "Position", Position },
                { "Scale", Scale },
                { "MarkText", MarkText },
                { "ShowText", ShowText },
                { "TextOffset", TextOffset },
                { "Rotation", Rotation },
                { "IsVisible", IsVisible },
                { "DefectType", DefectType },
                { "BubbleGrade", BubbleGrade },
                { "BubbleCount", BubbleCount },
                { "StriaGrade", StriaGrade },
                { "StressBirefringence", StressBirefringence },
                { "InclusionLevel", InclusionLevel },
                { "InspectionAperture", InspectionAperture },
                { "InspectionWavelength", InspectionWavelength },
                { "TestStandard", TestStandard },
                { "FrameSize", FrameSize },
                { "Severity", Severity }
            };
        }

        /// <summary>
        /// 设置标记属性?        /// </summary>
        public void SetProperties(Dictionary<string, object> properties)
        {
            if (properties.ContainsKey("Position"))
                Position = (Vector2)properties["Position"];
            if (properties.ContainsKey("Scale"))
                Scale = (double)properties["Scale"];
            if (properties.ContainsKey("MarkText"))
                MarkText = (string)properties["MarkText"];
            if (properties.ContainsKey("ShowText"))
                ShowText = (bool)properties["ShowText"];
            if (properties.ContainsKey("TextOffset"))
                TextOffset = (Vector2)properties["TextOffset"];
            if (properties.ContainsKey("Rotation"))
                Rotation = (double)properties["Rotation"];
            if (properties.ContainsKey("IsVisible"))
                IsVisible = (bool)properties["IsVisible"];
            if (properties.ContainsKey("DefectType"))
                DefectType = (MaterialDefectType)properties["DefectType"];
            if (properties.ContainsKey("BubbleGrade"))
                BubbleGrade = (double)properties["BubbleGrade"];
            if (properties.ContainsKey("BubbleCount"))
                BubbleCount = (int)properties["BubbleCount"];
            if (properties.ContainsKey("StriaGrade"))
                StriaGrade = (double)properties["StriaGrade"];
            if (properties.ContainsKey("StressBirefringence"))
                StressBirefringence = (double)properties["StressBirefringence"];
            if (properties.ContainsKey("InclusionLevel"))
                InclusionLevel = (InclusionGrade)properties["InclusionLevel"];
            if (properties.ContainsKey("InspectionAperture"))
                InspectionAperture = (double)properties["InspectionAperture"];
            if (properties.ContainsKey("InspectionWavelength"))
                InspectionWavelength = (double)properties["InspectionWavelength"];
            if (properties.ContainsKey("TestStandard"))
                TestStandard = (string)properties["TestStandard"];
            if (properties.ContainsKey("FrameSize"))
                FrameSize = (Vector2)properties["FrameSize"];
            if (properties.ContainsKey("Severity"))
                Severity = (DefectSeverity)properties["Severity"];

            UpdateMarkText();
        }

        /// <summary>
        /// 克隆标记
        /// </summary>
        IOpticalMark IOpticalMark.Clone()
        {
            return Clone() as MaterialDefectMark;
        }

        /// <summary>
        /// 获取标记描述
        /// </summary>
        public string GetDescription()
        {
            return $"材料缺陷标记 - {GetDefectTypeDescription()}: {GetDetailedDescription()}";
        }

        /// <summary>
        /// 获取缺陷类型描述
        /// </summary>
        private string GetDefectTypeDescription()
        {
            switch (DefectType)
            {
                case MaterialDefectType.Bubble: return "气泡";
                case MaterialDefectType.Stria: return "条纹";
                case MaterialDefectType.StressBirefringence: return "应力双折射";
                case MaterialDefectType.Inclusion: return "包裹体";
                case MaterialDefectType.Combined: return "综合缺陷";
                default: return "材料缺陷";
            }
        }

        #endregion

        #region Entity 重写方法

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new MaterialDefectMark();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            MaterialDefectMark mark = base.Clone() as MaterialDefectMark;
            mark.Position = Position;
            mark.Scale = Scale;
            mark.MarkText = MarkText;
            mark.ShowText = ShowText;
            mark.TextOffset = TextOffset;
            mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.DefectType = DefectType;
            mark.BubbleGrade = BubbleGrade;
            mark.BubbleCount = BubbleCount;
            mark.StriaGrade = StriaGrade;
            mark.StressBirefringence = StressBirefringence;
            mark.InclusionLevel = InclusionLevel;
            mark.InspectionAperture = InspectionAperture;
            mark.InspectionWavelength = InspectionWavelength;
            mark.TestStandard = TestStandard;
            mark.FrameSize = FrameSize;
            mark.Severity = Severity;
            mark._markEntities = new List<Entity>();

            return mark;
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            Position += translation;
            _markEntities.Clear();
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            Position = Vector2.RotateInRadian(Position, center, angle);
            Rotation += angle * 180 / Math.PI;
            _markEntities.Clear();
        }

        /// <summary>
        /// Transform
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            Position = transform * Position;
            Vector2 scaleVector = new Vector2(Scale, 0);
            Scale = (transform * scaleVector).length;
            _markEntities.Clear();
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            gripPoints.Add(new GripPoint(GripPointType.Center, Position));

            // 添加框架角点
            double halfWidth = FrameSize.X * 0.5 * Scale;
            double halfHeight = FrameSize.Y * 0.5 * Scale;
            gripPoints.Add(new GripPoint(GripPointType.Corner, Position + new Vector2(halfWidth, halfHeight)));

            if (ShowText)
            {
                var textPos = Position + TextOffset;
                gripPoints.Add(new GripPoint(GripPointType.Center, textPos));
            }

            return gripPoints;
        }

        /// <summary>
        /// 对象捕捉点?        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPnts = new List<ObjectSnapPoint>();
            
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Center, Position));
            
            double halfWidth = FrameSize.X * 0.5 * Scale;
            double halfHeight = FrameSize.Y * 0.5 * Scale;
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Corner, Position + new Vector2(halfWidth, halfHeight)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Corner, Position + new Vector2(-halfWidth, halfHeight)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Corner, Position + new Vector2(halfWidth, -halfHeight)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Corner, Position + new Vector2(-halfWidth, -halfHeight)));
            
            return snapPnts;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            switch (index)
            {
                case 0: // 中心点
                    Position = newPosition;
                    break;
                case 1: // 框架大小调整点
                    var delta = newPosition - Position;
                    FrameSize = new Vector2(Math.Abs(delta.X) * 2, Math.Abs(delta.Y) * 2);
                    break;
                case 2: // 文本位置点
                    if (ShowText)
                    {
                        TextOffset = newPosition - Position;
                    }
                    break;
            }
            _markEntities.Clear();
        }

        #endregion
    }

    /// <summary>
    /// 材料缺陷类型枚举
    /// </summary>
    public enum MaterialDefectType
    {
        /// <summary>
        /// 气泡
        /// </summary>
        Bubble = 0,
        
        /// <summary>
        /// 条纹
        /// </summary>
        Stria = 1,
        
        /// <summary>
        /// 应力双折?        /// </summary>
        StressBirefringence = 2,
        
        /// <summary>
        /// 包裹体?        /// </summary>
        Inclusion = 3,
        
        /// <summary>
        /// 组合缺陷
        /// </summary>
        Combined = 4
    }

    /// <summary>
    /// 包裹体等级枚?    /// </summary>
    public enum InclusionGrade
    {
        /// <summary>
        /// 1级（最严格?        /// </summary>
        Grade1 = 1,
        
        /// <summary>
        /// 2?        /// </summary>
        Grade2 = 2,
        
        /// <summary>
        /// 3?        /// </summary>
        Grade3 = 3,
        
        /// <summary>
        /// 4?        /// </summary>
        Grade4 = 4,
        
        /// <summary>
        /// 5级（最宽松?        /// </summary>
        Grade5 = 5
    }

    /// <summary>
    /// 缺陷严重性等级枚?    /// </summary>
    public enum DefectSeverity
    {
        /// <summary>
        /// 可忽?        /// </summary>
        Negligible = 0,
        
        /// <summary>
        /// 轻微
        /// </summary>
        Minor = 1,
        
        /// <summary>
        /// 重大
        /// </summary>
        Major = 2,
        
        /// <summary>
        /// 严重
        /// </summary>
        Critical = 3
    }

    /// <summary>
    /// 缺陷类型枚举
    /// </summary>
    public enum DefectType
    {
        /// <summary>
        /// 气泡
        /// </summary>
        Bubble = 0,
        
        /// <summary>
        /// 条纹
        /// </summary>
        Striae = 1,
        
        /// <summary>
        /// 应力双折射
        /// </summary>
        StressBirefringence = 2,
        
        /// <summary>
        /// 包裹体
        /// </summary>
        Inclusion = 3,
        
        /// <summary>
        /// 其他缺陷
        /// </summary>
        Other = 4
    }

}
