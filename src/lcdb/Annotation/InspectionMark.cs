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
    /// 检验标记实现    /// 用于标注光学元件的检验要求和质量控制信息
    /// </summary>
    public class InspectionMark : Entity, IOpticalMark
    {
        public override string className => "InspectionMark";

        #region IOpticalMark 接口属性
        /// <summary>
        /// 标记类型
        /// </summary>
        public OpticalMarkType MarkType => OpticalMarkType.Inspection;

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
        public string MarkText { get; set; } = "检查-A级";

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

        #region 检验标记特定属性
        /// <summary>
        /// 检验类型        /// </summary>
        public InspectionType InspectionCategory { get; set; } = InspectionType.Dimensional;

        /// <summary>
        /// 检验等级        /// </summary>
        public InspectionGrade Grade { get; set; } = InspectionGrade.GradeA;

        /// <summary>
        /// 检验标准        /// </summary>
        public string InspectionStandard { get; set; } = "GB/T 1185-2006";

        /// <summary>
        /// 检验项目        /// </summary>
        public string InspectionItems { get; set; } = "外观,尺寸,性能";

        /// <summary>
        /// 抽样比例        /// </summary>
        public double SamplingRate { get; set; } = 100.0;

        /// <summary>
        /// 检验设备        /// </summary>
        public string Equipment { get; set; } = "光学检测仪";

        /// <summary>
        /// 检验环境要求        /// </summary>
        public string EnvironmentRequirement { get; set; } = "23±2℃, 50±10%RH";

        /// <summary>
        /// 合格准则
        /// </summary>
        public string AcceptanceCriteria { get; set; } = "符合技术要求";

        /// <summary>
        /// 检验频率        /// </summary>
        public InspectionFrequency Frequency { get; set; } = InspectionFrequency.PerBatch;

        /// <summary>
        /// 记录要求
        /// </summary>
        public string RecordRequirement { get; set; } = "详细记录检验结果";

        /// <summary>
        /// 检验结果状?        /// </summary>
        public InspectionResult Result { get; set; } = InspectionResult.Pending;

        /// <summary>
        /// 检验员信息
        /// </summary>
        public string Inspector { get; set; } = "";

        /// <summary>
        /// 检验日?        /// </summary>
        public DateTime InspectionDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 标记框架大小
        /// </summary>
        public Vector2 FrameSize { get; set; } = new Vector2(50, 40);

        #endregion

        private List<Entity> _markEntities = new List<Entity>();

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                double totalWidth = FrameSize.X + (ShowText ? Math.Abs(TextOffset.X) + 80 : 0);
                double totalHeight = FrameSize.Y + (ShowText ? Math.Abs(TextOffset.Y) + 15 : 0);
                return new Bounding(Position, totalWidth, totalHeight);
            }
        }

        #region 构造函数
        /// <summary>
        /// 默认构造函数        /// </summary>
        public InspectionMark()
        {
            UpdateMarkText();
        }

        /// <summary>
        /// 带参数构造函数        /// </summary>
        public InspectionMark(Vector2 position, InspectionType inspectionType, InspectionGrade grade)
        {
            Position = position;
            InspectionCategory = inspectionType;
            Grade = grade;
            UpdateMarkText();
        }

        #endregion

        #region 核心方法

        /// <summary>
        /// 更新标记文本
        /// </summary>
        private void UpdateMarkText()
        {
            string inspectionDesc = GetInspectionTypeDescription();
            string gradeDesc = GetGradeDescription();
            MarkText = $"{inspectionDesc}-{gradeDesc}";
        }

        /// <summary>
        /// 获取检验类型描述?        /// </summary>
        private string GetInspectionTypeDescription()
        {
            switch (InspectionCategory)
            {
                case InspectionType.Dimensional: return "尺寸检查";
                case InspectionType.Visual: return "外观检查";
                case InspectionType.Optical: return "光学检查";
                case InspectionType.Surface: return "表面检查";
                case InspectionType.Mechanical: return "机械检查";
                case InspectionType.Functional: return "功能检查";
                case InspectionType.Environmental: return "环境检查";
                case InspectionType.Comprehensive: return "综合检查";
                default: return "检查";
            }
        }

        /// <summary>
        /// 获取等级描述
        /// </summary>
        private string GetGradeDescription()
        {
            switch (Grade)
            {
                case InspectionGrade.GradeA: return "A";
                case InspectionGrade.GradeB: return "B";
                case InspectionGrade.GradeC: return "C";
                case InspectionGrade.Special: return "特检";
                default: return "A";
            }
        }

        /// <summary>
        /// 生成标记图形
        /// </summary>
        protected void Generate()
        {
            _markEntities.Clear();
            GenerateMainSymbol();
            if (ShowText)
            {
                GenerateText();
            }
        }

        /// <summary>
        /// 生成主标记符号 — 工厂惯例: 矩形框 + 结构化文字标签 (GB/T 4458.4 检验备注规范).
        /// 旧实现 (菱形 + 8 种工种子图标 + 等级 + 结果指示) 无 GB/ISO 标准依据, 已移除.
        /// </summary>
        private void GenerateMainSymbol()
        {
            var markColor = GetMarkColor();
            double halfWidth = FrameSize.X * 0.5 * Scale;
            double halfHeight = FrameSize.Y * 0.5 * Scale;

            GenerateRectangleFrame(halfWidth, halfHeight, markColor);
            GenerateInspectionLabel(halfHeight, markColor);
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
        /// 框内 2 行结构化文字: 第一行 "类型: 等级", 第二行 "结果".
        /// </summary>
        private void GenerateInspectionLabel(double halfHeight, lcdb.Colors.Color color)
        {
            var line1 = new Text
            {
                Value = $"{GetInspectionTypeDescription()}: {GetGradeDescription()}",
                Position = new Vector3(Position.X, Position.Y + halfHeight * 0.35, 0.0),
                Height = 4 * Scale,
                color = color,
                alignment = lcdb.TextAlignment.CenterMiddle,
            };
            _markEntities.Add(line1);

            var line2 = new Text
            {
                Value = GetResultDescription(),
                Position = new Vector3(Position.X, Position.Y - halfHeight * 0.35, 0.0),
                Height = 3 * Scale,
                color = GetResultColor(),
                alignment = lcdb.TextAlignment.CenterMiddle,
            };
            _markEntities.Add(line2);
        }

        /// <summary>
        /// 生成外部说明文本
        /// </summary>
        private void GenerateText()
        {
            var textPosition = Position + TextOffset;
            var text = new Text();
            text.Value = GetDetailedDescription();
            text.Position = new Vector3(textPosition.X, textPosition.Y, 0.0);
            text.Height = 5 * Scale;
            text.color = GetMarkColor();
            text.alignment = lcdb.TextAlignment.CenterMiddle;
            _markEntities.Add(text);
        }

        /// <summary>
        /// 获取详细描述
        /// </summary>
        private string GetDetailedDescription()
        {
            string inspectionDesc = GetInspectionTypeDescription();
            string gradeDesc = GetGradeDescription();
            string resultDesc = GetResultDescription();
            return $"{inspectionDesc}: {gradeDesc}级检查 - {resultDesc}";
        }

        /// <summary>
        /// 获取结果描述
        /// </summary>
        private string GetResultDescription()
        {
            switch (Result)
            {
                case InspectionResult.Passed: return "合格";
                case InspectionResult.Failed: return "不合格";
                case InspectionResult.Pending: return "待检";
                case InspectionResult.Rework: return "返工";
                default: return "待检";
            }
        }

        /// <summary>
        /// 获取标记颜色
        /// </summary>
        private lcdb.Colors.Color GetMarkColor()
        {
            // 根据检验等级选择颜色
            switch (Grade)
            {
                case InspectionGrade.Special:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Purple);     // 特检 - 紫色
                case InspectionGrade.GradeA:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Blue);       // A级 - 蓝色
                case InspectionGrade.GradeB:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Green);      // B级 - 绿色
                case InspectionGrade.GradeC:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Orange);     // C级 - 橙色
                default:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Blue);
            }
        }

        /// <summary>
        /// 获取结果颜色
        /// </summary>
        private lcdb.Colors.Color GetResultColor()
        {
            switch (Result)
            {
                case InspectionResult.Passed:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Green);      // 合格 - 绿色
                case InspectionResult.Failed:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Red);        // 不合格 - 红色
                case InspectionResult.Pending:
                    // WCAG AA: Yellow (255,255,0) 白底 1.07:1 → 不可见; 改 #806000 (5.9:1) 深橄榄金
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.FromArgb(128, 96, 0));
                case InspectionResult.Rework:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Orange);     // 返工 - 橙色
                default:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Gray);
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
            DrawInspectionSymbol(g, position, scale * (float)Scale);
        }

        /// <summary>
        /// 绘制检验符号        /// </summary>
        private void DrawInspectionSymbol(Graphics g, PointF position, float scale)
        {
            using (var pen = new Pen(GetMarkColor().ToDrawingColor(), 1.5f * scale))
            using (var brush = new SolidBrush(GetMarkColor().ToDrawingColor()))
            using (var font = new Font("Arial", 6 * scale, System.Drawing.FontStyle.Bold))
            {
                float halfWidth = (float)FrameSize.X * 0.5f * scale;
                float halfHeight = (float)FrameSize.Y * 0.5f * scale;

                // 绘制菱形框架
                PointF[] diamondPoints = new PointF[]
                {
                    new PointF(position.X, position.Y + halfHeight),
                    new PointF(position.X + halfWidth, position.Y),
                    new PointF(position.X, position.Y - halfHeight),
                    new PointF(position.X - halfWidth, position.Y)
                };
                g.DrawPolygon(pen, diamondPoints);

                // 绘制内部十字线
                g.DrawLine(pen, position.X - halfWidth * 0.6f, position.Y,
                          position.X + halfWidth * 0.6f, position.Y);
                g.DrawLine(pen, position.X, position.Y - halfHeight * 0.6f,
                          position.X, position.Y + halfHeight * 0.6f);

                // 绘制检验符号（简化）
                DrawSimpleInspectionSymbol(g, pen, position, scale);

                // 绘制等级标识
                string gradeText = GetGradeDescription();
                g.DrawString(gradeText, font, brush, position.X, position.Y + halfHeight * 0.7f,
                           new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

                // 绘制结果指示                DrawResultIndicator(g, position, halfWidth, halfHeight, scale);
            }
        }

        /// <summary>
        /// 绘制简化的检验符号        /// </summary>
        private void DrawSimpleInspectionSymbol(Graphics g, Pen pen, PointF position, float scale)
        {
            float symbolSize = 8 * scale;
            
            switch (InspectionCategory)
            {
                case InspectionType.Dimensional:
                    // 卡尺符号
                    g.DrawLine(pen, position.X - symbolSize * 0.3f, position.Y - symbolSize * 0.2f,
                              position.X - symbolSize * 0.3f, position.Y + symbolSize * 0.2f);
                    g.DrawLine(pen, position.X + symbolSize * 0.3f, position.Y - symbolSize * 0.2f,
                              position.X + symbolSize * 0.3f, position.Y + symbolSize * 0.2f);
                    g.DrawLine(pen, position.X - symbolSize * 0.2f, position.Y,
                              position.X + symbolSize * 0.2f, position.Y);
                    break;
                    
                case InspectionType.Visual:
                    // 眼睛符号
                    g.DrawEllipse(pen, position.X - symbolSize * 0.2f, position.Y - symbolSize * 0.2f,
                                 symbolSize * 0.4f, symbolSize * 0.4f);
                    g.DrawEllipse(pen, position.X - symbolSize * 0.1f, position.Y - symbolSize * 0.1f,
                                 symbolSize * 0.2f, symbolSize * 0.2f);
                    break;
                    
                case InspectionType.Optical:
                    // 透镜符号
                    g.DrawLine(pen, position.X - symbolSize * 0.2f, position.Y - symbolSize * 0.3f,
                              position.X - symbolSize * 0.1f, position.Y + symbolSize * 0.3f);
                    g.DrawLine(pen, position.X + symbolSize * 0.1f, position.Y - symbolSize * 0.3f,
                              position.X + symbolSize * 0.2f, position.Y + symbolSize * 0.3f);
                    g.DrawLine(pen, position.X - symbolSize * 0.4f, position.Y,
                              position.X + symbolSize * 0.4f, position.Y);
                    break;
                    
                default:
                    // 通用检验符号（勾号）
                    g.DrawLine(pen, position.X - symbolSize * 0.1f, position.Y,
                              position.X, position.Y - symbolSize * 0.1f);
                    g.DrawLine(pen, position.X, position.Y - symbolSize * 0.1f,
                              position.X + symbolSize * 0.2f, position.Y + symbolSize * 0.1f);
                    break;
            }
        }

        /// <summary>
        /// 绘制结果指示        /// </summary>
        private void DrawResultIndicator(Graphics g, PointF position, float halfWidth, float halfHeight, float scale)
        {
            using (var resultPen = new Pen(GetResultColor().ToDrawingColor(), 2.0f * scale))
            {
                float indicatorX = position.X + halfWidth * 0.7f;
                float indicatorY = position.Y + halfHeight * 0.7f;
                float indicatorSize = 3 * scale;

                switch (Result)
                {
                    case InspectionResult.Passed:
                        // 绿色勾号
                        g.DrawLine(resultPen, indicatorX - indicatorSize * 0.3f, indicatorY,
                                  indicatorX, indicatorY - indicatorSize * 0.3f);
                        g.DrawLine(resultPen, indicatorX, indicatorY - indicatorSize * 0.3f,
                                  indicatorX + indicatorSize * 0.3f, indicatorY + indicatorSize * 0.2f);
                        break;
                        
                    case InspectionResult.Failed:
                        // 红色叉号
                        g.DrawLine(resultPen, indicatorX - indicatorSize * 0.2f, indicatorY - indicatorSize * 0.2f,
                                  indicatorX + indicatorSize * 0.2f, indicatorY + indicatorSize * 0.2f);
                        g.DrawLine(resultPen, indicatorX - indicatorSize * 0.2f, indicatorY + indicatorSize * 0.2f,
                                  indicatorX + indicatorSize * 0.2f, indicatorY - indicatorSize * 0.2f);
                        break;
                        
                    case InspectionResult.Pending:
                        // 黄色圆圈
                        g.DrawEllipse(resultPen, indicatorX - indicatorSize * 0.2f, indicatorY - indicatorSize * 0.2f,
                                     indicatorSize * 0.4f, indicatorSize * 0.4f);
                        break;
                }
            }
        }
#endif

        /// <summary>
        /// 获取标记边界框        /// </summary>
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
            if (string.IsNullOrEmpty(InspectionStandard))
                return false;

            if (string.IsNullOrEmpty(InspectionItems))
                return false;

            if (SamplingRate < 0 || SamplingRate > 100)
                return false;

            return true;
        }

        /// <summary>
        /// 获取标记属性        /// </summary>
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
                { "InspectionCategory", InspectionCategory },
                { "Grade", Grade },
                { "InspectionStandard", InspectionStandard },
                { "InspectionItems", InspectionItems },
                { "SamplingRate", SamplingRate },
                { "Equipment", Equipment },
                { "EnvironmentRequirement", EnvironmentRequirement },
                { "AcceptanceCriteria", AcceptanceCriteria },
                { "Frequency", Frequency },
                { "RecordRequirement", RecordRequirement },
                { "Result", Result },
                { "Inspector", Inspector },
                { "InspectionDate", InspectionDate },
                { "FrameSize", FrameSize }
            };
        }

        /// <summary>
        /// 设置标记属性        /// </summary>
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
            if (properties.ContainsKey("InspectionCategory"))
                InspectionCategory = (InspectionType)properties["InspectionCategory"];
            if (properties.ContainsKey("Grade"))
                Grade = (InspectionGrade)properties["Grade"];
            if (properties.ContainsKey("InspectionStandard"))
                InspectionStandard = (string)properties["InspectionStandard"];
            if (properties.ContainsKey("InspectionItems"))
                InspectionItems = (string)properties["InspectionItems"];
            if (properties.ContainsKey("SamplingRate"))
                SamplingRate = (double)properties["SamplingRate"];
            if (properties.ContainsKey("Equipment"))
                Equipment = (string)properties["Equipment"];
            if (properties.ContainsKey("EnvironmentRequirement"))
                EnvironmentRequirement = (string)properties["EnvironmentRequirement"];
            if (properties.ContainsKey("AcceptanceCriteria"))
                AcceptanceCriteria = (string)properties["AcceptanceCriteria"];
            if (properties.ContainsKey("Frequency"))
                Frequency = (InspectionFrequency)properties["Frequency"];
            if (properties.ContainsKey("RecordRequirement"))
                RecordRequirement = (string)properties["RecordRequirement"];
            if (properties.ContainsKey("Result"))
                Result = (InspectionResult)properties["Result"];
            if (properties.ContainsKey("Inspector"))
                Inspector = (string)properties["Inspector"];
            if (properties.ContainsKey("InspectionDate"))
                InspectionDate = (DateTime)properties["InspectionDate"];
            if (properties.ContainsKey("FrameSize"))
                FrameSize = (Vector2)properties["FrameSize"];

            UpdateMarkText();
        }

        /// <summary>
        /// 克隆标记
        /// </summary>
        IOpticalMark IOpticalMark.Clone()
        {
            return Clone() as InspectionMark;
        }

        /// <summary>
        /// 获取标记描述
        /// </summary>
        public string GetDescription()
        {
            return $"检验标记 - {GetInspectionTypeDescription()}: {GetGradeDescription()}级 {GetResultDescription()}";
        }

        #endregion

        #region Entity 重写方法

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new InspectionMark();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            InspectionMark mark = base.Clone() as InspectionMark;
            mark.Position = Position;
            mark.Scale = Scale;
            mark.MarkText = MarkText;
            mark.ShowText = ShowText;
            mark.TextOffset = TextOffset;
            mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.InspectionCategory = InspectionCategory;
            mark.Grade = Grade;
            mark.InspectionStandard = InspectionStandard;
            mark.InspectionItems = InspectionItems;
            mark.SamplingRate = SamplingRate;
            mark.Equipment = Equipment;
            mark.EnvironmentRequirement = EnvironmentRequirement;
            mark.AcceptanceCriteria = AcceptanceCriteria;
            mark.Frequency = Frequency;
            mark.RecordRequirement = RecordRequirement;
            mark.Result = Result;
            mark.Inspector = Inspector;
            mark.InspectionDate = InspectionDate;
            mark.FrameSize = FrameSize;
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
        /// 对象捕捉点        /// </summary>
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
    /// 检验类型枚举    /// </summary>
    public enum InspectionType
    {
        /// <summary>
        /// 尺寸检查        /// </summary>
        Dimensional = 0,
        
        /// <summary>
        /// 外观检查        /// </summary>
        Visual = 1,
        
        /// <summary>
        /// 光学检查        /// </summary>
        Optical = 2,
        
        /// <summary>
        /// 表面检查        /// </summary>
        Surface = 3,
        
        /// <summary>
        /// 机械检查        /// </summary>
        Mechanical = 4,
        
        /// <summary>
        /// 功能检查        /// </summary>
        Functional = 5,
        
        /// <summary>
        /// 环境检查        /// </summary>
        Environmental = 6,
        
        /// <summary>
        /// 综合检查        /// </summary>
        Comprehensive = 7
    }

    /// <summary>
    /// 检验等级枚举    /// </summary>
    public enum InspectionGrade
    {
        /// <summary>
        /// A级（最严格?        /// </summary>
        GradeA = 0,
        
        /// <summary>
        /// B级        /// </summary>
        GradeB = 1,
        
        /// <summary>
        /// C级        /// </summary>
        GradeC = 2,
        
        /// <summary>
        /// 特殊检查        /// </summary>
        Special = 3
    }

    /// <summary>
    /// 检验频次枚举    /// </summary>
    public enum InspectionFrequency
    {
        /// <summary>
        /// 每批次        /// </summary>
        PerBatch = 0,
        
        /// <summary>
        /// 每个
        /// </summary>
        PerPiece = 1,
        
        /// <summary>
        /// 抽样
        /// </summary>
        Sampling = 2,
        
        /// <summary>
        /// 定期
        /// </summary>
        Periodic = 3,
        
        /// <summary>
        /// 首检
        /// </summary>
        FirstPiece = 4,
        
        /// <summary>
        /// 末检
        /// </summary>
        LastPiece = 5
    }

    /// <summary>
    /// 检验结果枚举    /// </summary>
    public enum InspectionResult
    {
        /// <summary>
        /// 待检查        /// </summary>
        Pending = 0,
        
        /// <summary>
        /// 合格
        /// </summary>
        Passed = 1,
        
        /// <summary>
        /// 不合格        /// </summary>
        Failed = 2,
        
        /// <summary>
        /// 返工
        /// </summary>
        Rework = 3
    }
}