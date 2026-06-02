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
    /// ISO 10110-17 激光损伤阈值标记实?    /// 用于标注光学元件的激光损伤阈值参?    /// </summary>
    public class LaserDamageThresholdMark : Entity, IOpticalMark
    {
        public override string className => "LaserDamageThresholdMark";

        #region IOpticalMark 接口属性?
        /// <summary>
        /// 标记类型
        /// </summary>
        public OpticalMarkType MarkType => OpticalMarkType.ISO10110_17;

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
        public string MarkText { get; set; } = "17/5.0@1064nm-10ns";

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

        #region ISO 10110-17 特定属性?
        /// <summary>
        /// 激光损伤阈值（J/cm²?        /// </summary>
        public double DamageThreshold { get; set; } = 5.0;

        /// <summary>
        /// 激光波长（nm?        /// </summary>
        public double LaserWavelength { get; set; } = 1064;

        /// <summary>
        /// 脉冲宽度（ns、ps、fs等）
        /// </summary>
        public double PulseWidth { get; set; } = 10;

        /// <summary>
        /// 脉冲宽度单位
        /// </summary>
        public PulseWidthUnit PulseUnit { get; set; } = PulseWidthUnit.Nanoseconds;

        /// <summary>
        /// 重复频率（Hz?        /// </summary>
        public double RepetitionRate { get; set; } = 10;

        /// <summary>
        /// 测试标准
        /// </summary>
        public LaserDamageTestStandard TestStandard { get; set; } = LaserDamageTestStandard.ISO21254;

        /// <summary>
        /// 光束直径（μm?        /// </summary>
        public double BeamDiameter { get; set; } = 100;

        /// <summary>
        /// 测试温度（℃?        /// </summary>
        public double TestTemperature { get; set; } = 23;

        /// <summary>
        /// 环境湿度?RH?        /// </summary>
        public double RelativeHumidity { get; set; } = 45;

        /// <summary>
        /// 损伤类型
        /// </summary>
        public LaserDamageType DamageType { get; set; } = LaserDamageType.Threshold;

        /// <summary>
        /// 标记框架大小
        /// </summary>
        public Vector2 FrameSize { get; set; } = new Vector2(65, 35);

        /// <summary>
        /// 检测标准?        /// </summary>
        public string Standard { get; set; } = "ISO 10110-17";

        #endregion

        private List<Entity> _markEntities = new List<Entity>();

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                double totalWidth = FrameSize.X + (ShowText ? Math.Abs(TextOffset.X) + 150 : 0);
                double totalHeight = FrameSize.Y + (ShowText ? Math.Abs(TextOffset.Y) + 15 : 0);
                return new Bounding(Position, totalWidth, totalHeight);
            }
        }

        #region 构造函数?
        /// <summary>
        /// 默认构造函数?        /// </summary>
        public LaserDamageThresholdMark()
        {
            UpdateMarkText();
        }

        /// <summary>
        /// 带参数构造函数?        /// </summary>
        public LaserDamageThresholdMark(Vector2 position, double damageThreshold, double laserWavelength, double pulseWidth)
        {
            Position = position;
            DamageThreshold = damageThreshold;
            LaserWavelength = laserWavelength;
            PulseWidth = pulseWidth;
            UpdateMarkText();
        }

        #endregion

        #region 核心方法

        /// <summary>
        /// 更新标记文本
        /// </summary>
        private void UpdateMarkText()
        {
            string pulseStr = $"{PulseWidth}{GetPulseUnitString()}";
            MarkText = $"17/{DamageThreshold:F1}@{LaserWavelength:F0}nm-{pulseStr}";
        }

        /// <summary>
        /// 获取脉冲单位字符号?        /// </summary>
        private string GetPulseUnitString()
        {
            switch (PulseUnit)
            {
                case PulseWidthUnit.Femtoseconds: return "fs";
                case PulseWidthUnit.Picoseconds: return "ps";
                case PulseWidthUnit.Nanoseconds: return "ns";
                case PulseWidthUnit.Microseconds: return "μs";
                case PulseWidthUnit.Milliseconds: return "ms";
                case PulseWidthUnit.Seconds: return "s";
                case PulseWidthUnit.ContinuousWave: return "CW";
                default: return "ns";
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
        /// ISO 21254 激光损伤阈值 (LIDT): 矩形框 + "LIDT F@λ/τ" + 单位.
        /// 旧实现 (八边形 + 警告三角 + 散射光线装饰 + 散点参数) 装饰过多, 已简化.
        /// 格式: LIDT {F} J/cm² @{λ}nm, {τ}{unit}  (F=fluence, λ=波长, τ=脉宽)
        /// </summary>
        private void GenerateMainSymbol()
        {
            var markColor = GetMarkColor();
            double halfWidth = FrameSize.X * 0.5 * Scale;
            double halfHeight = FrameSize.Y * 0.5 * Scale;

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
                _markEntities.Add(new Line(pts[i], pts[(i + 1) % 4]) { color = markColor });
            }

            // 主行: "LIDT F J/cm²"
            string fluenceStr = DamageThreshold > 0 ? $"{DamageThreshold:G3}" : "—";
            _markEntities.Add(new Text
            {
                Value = $"LIDT {fluenceStr} J/cm²",
                Position = new Vector3(Position.X, Position.Y + halfHeight * 0.3, 0.0),
                Height = halfHeight * 0.4,
                color = markColor,
                alignment = lcdb.TextAlignment.CenterMiddle,
            });

            // 副行: "@λnm, τ ns/ps/fs"
            string pulseStr = $"{PulseWidth:G3}{GetPulseUnitString()}";
            _markEntities.Add(new Text
            {
                Value = $"@{LaserWavelength:F0}nm, {pulseStr}",
                Position = new Vector3(Position.X, Position.Y - halfHeight * 0.3, 0.0),
                Height = halfHeight * 0.3,
                color = markColor,
                alignment = lcdb.TextAlignment.CenterMiddle,
            });
        }

        /// <summary>
        /// 生成八边形框架?        /// </summary>
        private void GenerateOctagonFrame(double radius, lcdb.Colors.Color color)
        {
            var points = new List<Vector2>();

            // 计算八边形顶点
            for (int i = 0; i < 8; i++)
            {
                double angle = i * Math.PI / 4; // 45度间距
                double x = Position.X + radius * Math.Cos(angle);
                double y = Position.Y + radius * Math.Sin(angle);
                points.Add(new Vector2(x, y));
            }

            // 应用旋转
            if (Rotation != 0)
            {
                double radians = Rotation * Math.PI / 180;
                for (int i = 0; i < points.Count; i++)
                {
                    points[i] = Vector2.RotateInRadian(points[i], Position, radians);
                }
            }

            // 绘制八边形边
            for (int i = 0; i < 8; i++)
            {
                int next = (i + 1) % 8;
                _markEntities.Add(new Line(points[i], points[next]) { color = color });
            }
        }

        /// <summary>
        /// 生成激光符号?        /// </summary>
        private void GenerateLaserSymbol(lcdb.Colors.Color color)
        {
            double symbolSize = 10 * Scale;
            
            // 激光束符号 - 发散光线
            int rayCount = 8;
            for (int i = 0; i < rayCount; i++)
            {
                double angle = i * Math.PI * 2 / rayCount;
                double innerRadius = symbolSize * 0.2;
                double outerRadius = symbolSize * 0.6;
                
                var startPoint = new Vector2(
                    Position.X + innerRadius * Math.Cos(angle),
                    Position.Y + innerRadius * Math.Sin(angle)
                );
                var endPoint = new Vector2(
                    Position.X + outerRadius * Math.Cos(angle),
                    Position.Y + outerRadius * Math.Sin(angle)
                );
                
                var ray = new Line(startPoint, endPoint);
                ray.color = color;
                _markEntities.Add(ray);
            }

            // 中心点?- 表示激光源
            var laserSource = new Circle
            {
                center = Position,
                radius = symbolSize * 0.15,
                color = color
            };
            _markEntities.Add(laserSource);

            // 添加警告三角形
            GenerateWarningTriangle(symbolSize * 0.8, color);
        }

        /// <summary>
        /// 生成警告三角形?        /// </summary>
        private void GenerateWarningTriangle(double size, lcdb.Colors.Color color)
        {
            double height = size * 0.866; // sqrt(3)/2
            var trianglePoints = new[]
            {
                new Vector2(Position.X, Position.Y + height * 2/3),               // 顶点
                new Vector2(Position.X - size/2, Position.Y - height/3),         // 左下
                new Vector2(Position.X + size/2, Position.Y - height/3)          // 右下
            };

            // 应用旋转
            if (Rotation != 0)
            {
                double radians = Rotation * Math.PI / 180;
                for (int i = 0; i < 3; i++)
                {
                    trianglePoints[i] = Vector2.RotateInRadian(trianglePoints[i], Position, radians);
                }
            }

            // 绘制三角形边
            for (int i = 0; i < 3; i++)
            {
                int next = (i + 1) % 3;
                _markEntities.Add(new Line(trianglePoints[i], trianglePoints[next]) { color = color });
            }

            // 在三角形内添加感叹号
            var exclamationLine = new Line(
                new Vector2(Position.X, Position.Y + size * 0.1),
                new Vector2(Position.X, Position.Y + size * 0.3)
            );
            exclamationLine.color = color;
            _markEntities.Add(exclamationLine);

            var exclamationDot = new Circle
            {
                center = new Vector2(Position.X, Position.Y - size * 0.1),
                radius = size * 0.03,
                color = color
            };
            _markEntities.Add(exclamationDot);
        }

        /// <summary>
        /// 生成ISO标识
        /// </summary>
        private void GenerateISOIdentifier(lcdb.Colors.Color color)
        {
            var text = new Text();
            text.Value = "17";
            text.Position = new Vector3(Position.X - FrameSize.X * 0.35, Position.Y + FrameSize.Y * 0.3, 0.0);
            text.Height = 7 * Scale;
            text.color = color;
            text.alignment = lcdb.TextAlignment.CenterMiddle;
            _markEntities.Add(text);
        }

        /// <summary>
        /// 生成损伤阈值参数文本?        /// </summary>
        private void GenerateDamageParameters(lcdb.Colors.Color color)
        {
            // 损伤阈
            var thresholdText = new Text();
            thresholdText.Value = $"{DamageThreshold:F1} J/cm²";
            thresholdText.Position = new Vector3(Position.X + FrameSize.X * 0.1, Position.Y + FrameSize.Y * 0.25, 0.0);
            thresholdText.Height = 4 * Scale;
            thresholdText.color = color;
            thresholdText.alignment = lcdb.TextAlignment.CenterMiddle;
            _markEntities.Add(thresholdText);

            // 波长
            var wavelengthText = new Text();
            wavelengthText.Value = $"{LaserWavelength:F0}nm";
            wavelengthText.Position = new Vector3(Position.X + FrameSize.X * 0.1, Position.Y, 0.0);
            wavelengthText.Height = 4 * Scale;
            wavelengthText.color = color;
            wavelengthText.alignment = lcdb.TextAlignment.CenterMiddle;
            _markEntities.Add(wavelengthText);

            // 脉冲宽度
            var pulseText = new Text();
            pulseText.Value = $"{PulseWidth}{GetPulseUnitString()}";
            pulseText.Position = new Vector3(Position.X + FrameSize.X * 0.1, Position.Y - FrameSize.Y * 0.25, 0.0);
            pulseText.Height = 4 * Scale;
            pulseText.color = color;
            pulseText.alignment = lcdb.TextAlignment.CenterMiddle;
            _markEntities.Add(pulseText);

            // 重复频率（如果不是单次）
            if (RepetitionRate > 0 && PulseUnit != PulseWidthUnit.ContinuousWave)
            {
                var freqText = new Text();
                freqText.Value = $"{RepetitionRate:F0}Hz";
                freqText.Position = new Vector3(Position.X - FrameSize.X * 0.1, Position.Y - FrameSize.Y * 0.3, 0.0);
                freqText.Height = 3 * Scale;
                freqText.color = color;
                freqText.alignment = lcdb.TextAlignment.CenterMiddle;
                _markEntities.Add(freqText);
            }
        }

        /// <summary>
        /// ISO 21254: 框内即完整规范, 外部仅注 ISO 标准引用.
        /// </summary>
        private void GenerateText()
        {
            var textPosition = Position + TextOffset;
            _markEntities.Add(new Text
            {
                Value = "ISO 21254",
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
            string pulseStr = PulseUnit == PulseWidthUnit.ContinuousWave ? "CW" : $"{PulseWidth}{GetPulseUnitString()}";
            return $"激光损伤阈? {DamageThreshold:F1}J/cm² @{LaserWavelength:F0}nm, {pulseStr}";
        }

        /// <summary>
        /// 获取标记颜色
        /// </summary>
        private lcdb.Colors.Color GetMarkColor()
        {
            // 根据损伤阈值等级选择颜色
            if (DamageThreshold >= 10.0)
                return lcdb.Colors.Color.FromColor(System.Drawing.Color.Green);       // 高阈?- 绿色
            else if (DamageThreshold >= 5.0)
                return lcdb.Colors.Color.FromColor(System.Drawing.Color.Orange);      // 中等阈?- 橙色
            else if (DamageThreshold >= 1.0)
                return lcdb.Colors.Color.FromColor(System.Drawing.Color.Red);         // 低阈?- 红色
            else
                return lcdb.Colors.Color.FromColor(System.Drawing.Color.Purple);      // 极低阈?- 紫色
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
            DrawISO10110_17Symbol(g, position, scale * (float)Scale);
        }

        /// <summary>
        /// 绘制ISO 10110-17符号
        /// </summary>
        private void DrawISO10110_17Symbol(Graphics g, PointF position, float scale)
        {
            using (var pen = new Pen(GetMarkColor().ToDrawingColor(), 1.5f * scale))
            using (var brush = new SolidBrush(GetMarkColor().ToDrawingColor()))
            using (var font = new Font("Arial", 4 * scale, System.Drawing.FontStyle.Bold))
            {
                float radius = Math.Min((float)FrameSize.X, (float)FrameSize.Y) * 0.3f * scale;

                // 绘制八边形框架
                PointF[] octPoints = new PointF[8];
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * (float)Math.PI / 4;
                    octPoints[i] = new PointF(
                        position.X + radius * (float)Math.Cos(angle),
                        position.Y + radius * (float)Math.Sin(angle)
                    );
                }
                g.DrawPolygon(pen, octPoints);

                // 绘制激光符号
                float symbolSize = 10 * scale;
                
                // 发散光线
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * (float)Math.PI / 4;
                    float innerR = symbolSize * 0.2f;
                    float outerR = symbolSize * 0.6f;
                    
                    g.DrawLine(pen,
                        position.X + innerR * (float)Math.Cos(angle),
                        position.Y + innerR * (float)Math.Sin(angle),
                        position.X + outerR * (float)Math.Cos(angle),
                        position.Y + outerR * (float)Math.Sin(angle));
                }

                // 中心点
                g.DrawEllipse(pen, 
                    position.X - symbolSize * 0.15f, position.Y - symbolSize * 0.15f,
                    symbolSize * 0.3f, symbolSize * 0.3f);

                // 警告三角形
                float triSize = symbolSize * 0.8f;
                float triHeight = triSize * 0.866f;
                PointF[] triangle = new PointF[]
                {
                    new PointF(position.X, position.Y + triHeight * 2/3),
                    new PointF(position.X - triSize/2, position.Y - triHeight/3),
                    new PointF(position.X + triSize/2, position.Y - triHeight/3)
                };
                g.DrawPolygon(pen, triangle);

                // 绘制标识和参
                string identifier = "17";
                string parameters = $"{DamageThreshold:F1}J/cm²\n{LaserWavelength:F0}nm\n{PulseWidth}{GetPulseUnitString()}";
                
                g.DrawString(identifier, font, brush, position.X - radius * 1.2f, position.Y + radius * 0.8f);
                g.DrawString(parameters, font, brush, position.X + radius * 0.3f, position.Y,
                           new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
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
            // 验证损伤阈值范
            if (DamageThreshold < 0 || DamageThreshold > 1000.0)
                return false;

            // 验证激光波长范
            if (LaserWavelength < 100 || LaserWavelength > 10000)
                return false;

            // 验证脉冲宽度
            if (PulseWidth < 0 || (PulseUnit != PulseWidthUnit.ContinuousWave && PulseWidth == 0))
                return false;

            // 验证重复频率
            if (RepetitionRate < 0)
                return false;

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
                { "DamageThreshold", DamageThreshold },
                { "LaserWavelength", LaserWavelength },
                { "PulseWidth", PulseWidth },
                { "PulseUnit", PulseUnit },
                { "RepetitionRate", RepetitionRate },
                { "TestStandard", TestStandard },
                { "BeamDiameter", BeamDiameter },
                { "TestTemperature", TestTemperature },
                { "RelativeHumidity", RelativeHumidity },
                { "DamageType", DamageType },
                { "FrameSize", FrameSize },
                { "Standard", Standard }
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
            if (properties.ContainsKey("DamageThreshold"))
                DamageThreshold = (double)properties["DamageThreshold"];
            if (properties.ContainsKey("LaserWavelength"))
                LaserWavelength = (double)properties["LaserWavelength"];
            if (properties.ContainsKey("PulseWidth"))
                PulseWidth = (double)properties["PulseWidth"];
            if (properties.ContainsKey("PulseUnit"))
                PulseUnit = (PulseWidthUnit)properties["PulseUnit"];
            if (properties.ContainsKey("RepetitionRate"))
                RepetitionRate = (double)properties["RepetitionRate"];
            if (properties.ContainsKey("TestStandard"))
                TestStandard = (LaserDamageTestStandard)properties["TestStandard"];
            if (properties.ContainsKey("BeamDiameter"))
                BeamDiameter = (double)properties["BeamDiameter"];
            if (properties.ContainsKey("TestTemperature"))
                TestTemperature = (double)properties["TestTemperature"];
            if (properties.ContainsKey("RelativeHumidity"))
                RelativeHumidity = (double)properties["RelativeHumidity"];
            if (properties.ContainsKey("DamageType"))
                DamageType = (LaserDamageType)properties["DamageType"];
            if (properties.ContainsKey("FrameSize"))
                FrameSize = (Vector2)properties["FrameSize"];
            if (properties.ContainsKey("Standard"))
                Standard = (string)properties["Standard"];

            UpdateMarkText();
        }

        /// <summary>
        /// 克隆标记
        /// </summary>
        IOpticalMark IOpticalMark.Clone()
        {
            return Clone() as LaserDamageThresholdMark;
        }

        /// <summary>
        /// 获取标记描述
        /// </summary>
        public string GetDescription()
        {
            string pulseStr = PulseUnit == PulseWidthUnit.ContinuousWave ? "CW" : $"{PulseWidth}{GetPulseUnitString()}";
            return $"ISO 10110-17 激光损伤阈值标准?- {DamageThreshold:F1}J/cm² @{LaserWavelength:F0}nm, {pulseStr}";
        }

        #endregion

        #region Entity 重写方法

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new LaserDamageThresholdMark();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            LaserDamageThresholdMark mark = base.Clone() as LaserDamageThresholdMark;
            mark.Position = Position;
            mark.Scale = Scale;
            mark.MarkText = MarkText;
            mark.ShowText = ShowText;
            mark.TextOffset = TextOffset;
            mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.DamageThreshold = DamageThreshold;
            mark.LaserWavelength = LaserWavelength;
            mark.PulseWidth = PulseWidth;
            mark.PulseUnit = PulseUnit;
            mark.RepetitionRate = RepetitionRate;
            mark.TestStandard = TestStandard;
            mark.BeamDiameter = BeamDiameter;
            mark.TestTemperature = TestTemperature;
            mark.RelativeHumidity = RelativeHumidity;
            mark.DamageType = DamageType;
            mark.FrameSize = FrameSize;
            mark.Standard = Standard;
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
    /// 脉冲宽度单位枚举
    /// </summary>
    public enum PulseWidthUnit
    {
        /// <summary>
        /// 飞秒
        /// </summary>
        Femtoseconds = 0,
        
        /// <summary>
        /// 皮秒
        /// </summary>
        Picoseconds = 1,
        
        /// <summary>
        /// 纳秒
        /// </summary>
        Nanoseconds = 2,
        
        /// <summary>
        /// 微秒
        /// </summary>
        Microseconds = 3,
        
        /// <summary>
        /// 毫秒
        /// </summary>
        Milliseconds = 4,
        
        /// <summary>
        /// ?        /// </summary>
        Seconds = 5,
        
        /// <summary>
        /// 连续?        /// </summary>
        ContinuousWave = 6
    }

    /// <summary>
    /// 激光损伤测试标准枚?    /// </summary>
    public enum LaserDamageTestStandard
    {
        /// <summary>
        /// ISO 21254
        /// </summary>
        ISO21254 = 0,
        
        /// <summary>
        /// ASTM F1624
        /// </summary>
        ASTM_F1624 = 1,
        
        /// <summary>
        /// IEC 60825
        /// </summary>
        IEC60825 = 2,
        
        /// <summary>
        /// MIL-PRF-13830B
        /// </summary>
        MIL_PRF_13830B = 3
    }

    /// <summary>
    /// 激光损伤类型枚?    /// </summary>
    public enum LaserDamageType
    {
        /// <summary>
        /// 损伤阈?        /// </summary>
        Threshold = 0,
        
        /// <summary>
        /// 灾变损伤
        /// </summary>
        Catastrophic = 1,
        
        /// <summary>
        /// 功能损伤
        /// </summary>
        Functional = 2,
        
        /// <summary>
        /// 表面形变
        /// </summary>
        Morphological = 3
    }
}