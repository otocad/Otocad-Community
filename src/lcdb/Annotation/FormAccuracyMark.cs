using lcdb.Colors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using LitMath;
using lcdb;
using lcdb.Interfaces;
using lcdb.Symbols;
using OtoCAD;

namespace lcdb.Annotation
{
    /// <summary>
    /// 面形精度标准枚举
    /// </summary>
    public enum FormAccuracyStandard
    {
        /// <summary>
        /// ISO 10110-5 面形精度标准
        /// 格式: 3/A(B/C) - 代码3/功率(不规则度/旋转对称偏差)
        /// </summary>
        ISO_10110_5 = 0,

        /// <summary>
        /// 传统 PV/RMS 格式
        /// 格式: PV: λ/N, RMS: λ/M
        /// </summary>
        PV_RMS_Format = 1
    }

    /// <summary>
    /// 面形精度标记实现
    /// 支持 ISO 10110-5 和传统 PV/RMS 双标准
    /// ISO格式: 3/A(B/C) - 代码3/功率条纹数(不规则度/旋转对称偏差)
    /// 传统格式: PV: λ/N, RMS: λ/M
    /// </summary>
    public class FormAccuracyMark : Entity, IOpticalMark, ISurfaceAttachable
    {
        public override string className => "FormAccuracyMark";

        #region IOpticalMark 接口属性
        /// <summary>
        /// 标记类型
        /// </summary>
        public OpticalMarkType MarkType => OpticalMarkType.FormAccuracy;

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
        public string MarkText { get; set; } = "3/4(1)";

        /// <summary>
        /// 是否显示文本
        /// </summary>
        public bool ShowText { get; set; } = true;

        /// <summary>
        /// 文本偏移量
        /// </summary>
        public Vector2 TextOffset { get; set; } = new Vector2(0, -30);

        /// <summary>
        /// 标记旋转角度（度）
        /// </summary>
        public double Rotation { get; set; } = 0.0;

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible { get; set; } = true;

        #endregion

        #region 标准选择
        /// <summary>
        /// 面形精度标准（ISO或传统PV/RMS）
        /// </summary>
        public FormAccuracyStandard FormStandard { get; set; } = FormAccuracyStandard.ISO_10110_5;

        #endregion

        #region ISO 10110-5 参数
        /// <summary>
        /// 功率/球差 A (条纹数)
        /// ISO 10110-5 格式中的第一个参数
        /// </summary>
        public double PowerFringes { get; set; } = 4.0;

        /// <summary>
        /// 不规则度 B (条纹数)
        /// ISO 10110-5 格式中括号内的第一个参数
        /// </summary>
        public double IrregularityFringes { get; set; } = 1.0;

        /// <summary>
        /// 旋转对称偏差 C (条纹数，可选)
        /// ISO 10110-5 格式中括号内的第二个参数
        /// </summary>
        public double RotationalSymmetryDeviation { get; set; } = 0.0;

        /// <summary>
        /// 是否包含旋转对称偏差
        /// </summary>
        public bool HasRotationalSymmetry { get; set; } = false;

        /// <summary>
        /// 评估区域直径 (mm，可选)
        /// </summary>
        public double EvaluationDiameter { get; set; } = 0.0;

        /// <summary>
        /// 是否仅指定不规则度（无功率要求）
        /// 格式: 3/-(B)
        /// </summary>
        public bool IrregularityOnly { get; set; } = false;

        #endregion

        #region 传统 PV/RMS 参数
        /// <summary>
        /// PV值（Peak-to-Valley，波峰到波谷值，λ的分母）
        /// </summary>
        public string PVValue { get; set; } = "4";

        /// <summary>
        /// RMS值（Root Mean Square，均方根值，λ的分母）
        /// </summary>
        public string RMSValue { get; set; } = "20";

        /// <summary>
        /// 面形精度类型（传统格式使用）
        /// </summary>
        public FormAccuracyType AccuracyType { get; set; } = FormAccuracyType.PV_RMS;

        #endregion

        #region 通用属性
        /// <summary>
        /// 测试波长（纳米）
        /// </summary>
        public string Wavelength { get; set; } = "632.8";

        /// <summary>
        /// 测试方法
        /// </summary>
        public string TestMethod { get; set; } = "Fizeau干涉仪";

        /// <summary>
        /// 标记样式
        /// </summary>
        public FormAccuracyMarkStyle MarkStyle { get; set; } = FormAccuracyMarkStyle.DashedBox;

        /// <summary>
        /// 框架大小
        /// </summary>
        public Vector2 FrameSize { get; set; } = new Vector2(60, 30);

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
                double totalHeight = FrameSize.Y + (ShowText ? Math.Abs(TextOffset.Y) + 20 : 0);
                return new Bounding(Position, totalWidth, totalHeight);
            }
        }

        #region 构造函数
        /// <summary>
        /// 默认构造函数（默认ISO标准）
        /// </summary>
        public FormAccuracyMark()
        {
            FormStandard = FormAccuracyStandard.ISO_10110_5;
            UpdateMarkText();
        }

        /// <summary>
        /// 传统PV/RMS格式构造函数
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="pvValue">PV值（λ的分母）</param>
        /// <param name="rmsValue">RMS值（λ的分母）</param>
        /// <param name="wavelength">测试波长(nm)</param>
        public FormAccuracyMark(Vector2 position, string pvValue, string rmsValue = "", string wavelength = "632.8")
        {
            Position = position;
            FormStandard = FormAccuracyStandard.PV_RMS_Format;
            PVValue = pvValue;
            RMSValue = rmsValue;
            Wavelength = wavelength;
            UpdateMarkText();
        }

        /// <summary>
        /// ISO 10110-5 格式构造函数
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="powerFringes">功率/球差 A (条纹数)</param>
        /// <param name="irregularityFringes">不规则度 B (条纹数)</param>
        /// <param name="rotationalSymmetry">旋转对称偏差 C (条纹数，0表示不指定)</param>
        /// <param name="wavelength">测试波长(nm)</param>
        public FormAccuracyMark(Vector2 position, double powerFringes, double irregularityFringes,
            double rotationalSymmetry = 0.0, string wavelength = "632.8")
        {
            Position = position;
            FormStandard = FormAccuracyStandard.ISO_10110_5;
            PowerFringes = powerFringes;
            IrregularityFringes = irregularityFringes;
            RotationalSymmetryDeviation = rotationalSymmetry;
            HasRotationalSymmetry = rotationalSymmetry > 0;
            Wavelength = wavelength;
            UpdateMarkText();
        }

        #endregion

        #region 核心方法

        /// <summary>
        /// 更新标记文本（根据当前标准）
        /// </summary>
        private void UpdateMarkText()
        {
            if (FormStandard == FormAccuracyStandard.ISO_10110_5)
            {
                UpdateISOMarkText();
            }
            else
            {
                UpdatePVRMSMarkText();
            }
        }

        /// <summary>
        /// 更新ISO 10110-5格式标记文本
        /// 格式: 3/A(B) 或 3/A(B/C) 或 3/-(B)
        /// </summary>
        private void UpdateISOMarkText()
        {
            var sb = new StringBuilder();
            sb.Append("3/");  // ISO 10110-5 代码前缀

            // 功率部分
            if (IrregularityOnly)
            {
                sb.Append("-");  // 无功率要求
            }
            else
            {
                // 格式化功率值（整数或一位小数）
                if (PowerFringes == Math.Floor(PowerFringes))
                    sb.Append(((int)PowerFringes).ToString());
                else
                    sb.AppendFormat("{0:F1}", PowerFringes);
            }

            // 不规则度部分
            sb.Append("(");
            if (IrregularityFringes == Math.Floor(IrregularityFringes))
                sb.Append(((int)IrregularityFringes).ToString());
            else
                sb.AppendFormat("{0:F1}", IrregularityFringes);

            // 旋转对称偏差部分（可选）
            if (HasRotationalSymmetry && RotationalSymmetryDeviation > 0)
            {
                sb.Append("/");
                if (RotationalSymmetryDeviation == Math.Floor(RotationalSymmetryDeviation))
                    sb.Append(((int)RotationalSymmetryDeviation).ToString());
                else
                    sb.AppendFormat("{0:F1}", RotationalSymmetryDeviation);
            }

            sb.Append(")");

            // 评估区域直径（可选）
            if (EvaluationDiameter > 0)
            {
                sb.AppendFormat(" Ø{0:F1}", EvaluationDiameter);
            }

            MarkText = sb.ToString();
        }

        /// <summary>
        /// 更新传统PV/RMS格式标记文本
        /// </summary>
        private void UpdatePVRMSMarkText()
        {
            switch (AccuracyType)
            {
                case FormAccuracyType.PV_Only:
                    MarkText = $"PV: λ/{PVValue}";
                    break;
                case FormAccuracyType.RMS_Only:
                    MarkText = $"RMS: λ/{RMSValue}";
                    break;
                case FormAccuracyType.PV_RMS:
                    if (!string.IsNullOrEmpty(PVValue) && !string.IsNullOrEmpty(RMSValue))
                        MarkText = $"PV: λ/{PVValue}\nRMS: λ/{RMSValue}";
                    else if (!string.IsNullOrEmpty(PVValue))
                        MarkText = $"PV: λ/{PVValue}";
                    else if (!string.IsNullOrEmpty(RMSValue))
                        MarkText = $"RMS: λ/{RMSValue}";
                    break;
                case FormAccuracyType.PowerIrregularity:
                    MarkText = $"{PVValue}/{RMSValue}";
                    break;
                default:
                    MarkText = $"λ/{PVValue}";
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
        /// 生成主标记符号?        /// </summary>
        private void GenerateMainSymbol()
        {
            var markColor = GetMarkColor();
            double halfWidth = FrameSize.X * 0.5 * Scale;
            double halfHeight = FrameSize.Y * 0.5 * Scale;

            switch (MarkStyle)
            {
                case FormAccuracyMarkStyle.DashedBox:
                    GenerateDashedBoxSymbol(halfWidth, halfHeight, markColor);
                    break;
                case FormAccuracyMarkStyle.DoubleBox:
                    GenerateDoubleBoxSymbol(halfWidth, halfHeight, markColor);
                    break;
                case FormAccuracyMarkStyle.WaveSymbol:
                    GenerateWaveSymbol(halfWidth, halfHeight, markColor);
                    break;
            }

            // 在符号内部添加面形精度文本
            GenerateInnerText(markColor);
        }

        /// <summary>
        /// 生成虚线框符号?        /// </summary>
        private void GenerateDashedBoxSymbol(double halfWidth, double halfHeight, lcdb.Colors.Color color)
        {
            var p1 = new Vector2(Position.X - halfWidth, Position.Y - halfHeight);
            var p2 = new Vector2(Position.X + halfWidth, Position.Y - halfHeight);
            var p3 = new Vector2(Position.X + halfWidth, Position.Y + halfHeight);
            var p4 = new Vector2(Position.X - halfWidth, Position.Y + halfHeight);

            // 应用旋转
            if (Rotation != 0)
            {
                double radians = Rotation * Math.PI / 180;
                p1 = Vector2.RotateInRadian(p1, Position, radians);
                p2 = Vector2.RotateInRadian(p2, Position, radians);
                p3 = Vector2.RotateInRadian(p3, Position, radians);
                p4 = Vector2.RotateInRadian(p4, Position, radians);
            }

            // 创建虚线（通过设置线型
            var line1 = new Line(p1, p2) { color = color, lineType = LineType.Dash };
            var line2 = new Line(p2, p3) { color = color, lineType = LineType.Dash };
            var line3 = new Line(p3, p4) { color = color, lineType = LineType.Dash };
            var line4 = new Line(p4, p1) { color = color, lineType = LineType.Dash };

            _markEntities.Add(line1);
            _markEntities.Add(line2);
            _markEntities.Add(line3);
            _markEntities.Add(line4);
        }

        /// <summary>
        /// 生成双线框符号?        /// </summary>
        private void GenerateDoubleBoxSymbol(double halfWidth, double halfHeight, lcdb.Colors.Color color)
        {
            // 外框
            GenerateRectangle(halfWidth, halfHeight, color, 0);
            
            // 内框
            double innerOffset = 3 * Scale;
            GenerateRectangle(halfWidth - innerOffset, halfHeight - innerOffset, color, 0);
        }

        /// <summary>
        /// 生成波浪符号
        /// </summary>
        private void GenerateWaveSymbol(double halfWidth, double halfHeight, lcdb.Colors.Color color)
        {
            // 生成基础
            GenerateRectangle(halfWidth, halfHeight, color, 0);

            // 在框的顶部添加波浪线
            int waveCount = 3;
            double waveWidth = 2 * halfWidth / waveCount;
            double waveHeight = 3 * Scale;
            
            for (int i = 0; i < waveCount; i++)
            {
                double startX = Position.X - halfWidth + i * waveWidth;
                double endX = startX + waveWidth;
                double y = Position.Y + halfHeight;

                // 创建简单的波浪形状
                var wavePoints = new List<Vector2>();
                for (double x = startX; x <= endX; x += waveWidth / 10)
                {
                    double waveY = y + waveHeight * Math.Sin((x - startX) / waveWidth * 2 * Math.PI);
                    wavePoints.Add(new Vector2(x, waveY));
                }

                // 连接波浪线
                for (int j = 0; j < wavePoints.Count - 1; j++)
                {
                    _markEntities.Add(new Line(wavePoints[j], wavePoints[j + 1]) { color = color });
                }
            }
        }

        /// <summary>
        /// 生成矩形辅助方法
        /// </summary>
        private void GenerateRectangle(double halfWidth, double halfHeight, lcdb.Colors.Color color, double lineOffset)
        {
            var p1 = new Vector2(Position.X - halfWidth, Position.Y - halfHeight);
            var p2 = new Vector2(Position.X + halfWidth, Position.Y - halfHeight);
            var p3 = new Vector2(Position.X + halfWidth, Position.Y + halfHeight);
            var p4 = new Vector2(Position.X - halfWidth, Position.Y + halfHeight);

            if (Rotation != 0)
            {
                double radians = Rotation * Math.PI / 180;
                p1 = Vector2.RotateInRadian(p1, Position, radians);
                p2 = Vector2.RotateInRadian(p2, Position, radians);
                p3 = Vector2.RotateInRadian(p3, Position, radians);
                p4 = Vector2.RotateInRadian(p4, Position, radians);
            }

            _markEntities.Add(new Line(p1, p2) { color = color });
            _markEntities.Add(new Line(p2, p3) { color = color });
            _markEntities.Add(new Line(p3, p4) { color = color });
            _markEntities.Add(new Line(p4, p1) { color = color });
        }

        /// <summary>
        /// 生成符号内部文本
        /// </summary>
        private void GenerateInnerText(lcdb.Colors.Color color)
        {
            string[] lines = MarkText.Split('\n');
            double lineHeight = 4 * Scale;
            double startY = Position.Y + (lines.Length - 1) * lineHeight * 0.5;

            for (int i = 0; i < lines.Length; i++)
            {
                var text = new Text();
                text.Value = lines[i];
                text.Position = new Vector3(Position.X, startY - i * lineHeight, 0.0);
                text.Height = 3 * Scale;
                text.color = color;
                text.alignment = lcdb.TextAlignment.CenterMiddle;
                _markEntities.Add(text);
            }
        }

        /// <summary>
        /// 生成外部说明文本.
        /// ISO 模式 (GB/T 11297.5-2011 / ISO 10110-5): 框内 "3/A(B/C)" 即为完整规范, 外部仅注 "λ=632.8nm" 测试条件.
        /// PV/RMS 模式: 保留 PV/RMS 详细注释 (非 ISO, 用于客户传统格式)
        /// </summary>
        private void GenerateText()
        {
            var textPosition = Position + TextOffset;
            var lines = new List<string>();

            if (FormStandard == FormAccuracyStandard.ISO_10110_5)
            {
                lines.Add($"λ={Wavelength}nm");
            }
            else
            {
                lines.Add(MarkText.Replace('\n', ' '));
                lines.Add($"λ={Wavelength}nm");
            }

            double lineHeight = 6 * Scale;
            for (int i = 0; i < lines.Count; i++)
            {
                var text = new Text();
                text.Value = lines[i];
                text.Position = new Vector3(textPosition.X, textPosition.Y - i * lineHeight, 0.0);
                text.Height = 4 * Scale;
                text.color = GetMarkColor();
                text.alignment = lcdb.TextAlignment.CenterMiddle;
                _markEntities.Add(text);
            }
        }

        /// <summary>
        /// 贴面放置: 代码框保持水平可读 (不旋转), 仅把框移到被测面外侧、用户悬停的那一侧。
        /// (ISO 标准的"引线连接"画法是另一个更大的特性, 本期先做"贴边摆放"。)
        /// </summary>
        public void AttachToSurface(Vector2 surfacePoint, Vector2 outwardNormal)
        {
            // 框保持水平: 不改 Rotation。沿外法线把锚点外移, 使框紧贴面外侧而不压在线上。
            // 偏移量取框高的 0.7 倍: 框半高为 FrameSize.Y*0.5, 再留约 0.2 倍框高的间隙, 使框近边离面一个小缝。
            Position = surfacePoint + outwardNormal * (FrameSize.Y * 0.7 * Scale);
            _markEntities.Clear();
        }

        /// <summary>
        /// 获取标记颜色
        /// </summary>
        private lcdb.Colors.Color GetMarkColor()
        {
            // 根据精度等级选择颜色
            if (double.TryParse(PVValue, out double pvValue))
            {
                if (pvValue >= 10)
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Green);      // 高精度?- 绿色
                else if (pvValue >= 4)
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Blue);       // 中等精度 - 蓝色
                else if (pvValue >= 2)
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Orange);     // 较高精度 - 橙色
                else
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Red);        // 极高精度 - 红色
            }
            return lcdb.Colors.Color.FromColor(System.Drawing.Color.Purple);             // 默认 - 紫色
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
            OpticalMarkSymbols.DrawFormAccuracyMark(g, position, PVValue, RMSValue, Wavelength, scale * (float)Scale);
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
            if (FormStandard == FormAccuracyStandard.ISO_10110_5)
            {
                return ValidateISO();
            }
            else
            {
                return ValidatePVRMS();
            }
        }

        /// <summary>
        /// 验证ISO 10110-5格式数据
        /// </summary>
        private bool ValidateISO()
        {
            // 功率必须>=0（除非仅指定不规则度）
            if (!IrregularityOnly && PowerFringes < 0)
                return false;

            // 不规则度必须>=0
            if (IrregularityFringes < 0)
                return false;

            // 旋转对称偏差必须>=0
            if (RotationalSymmetryDeviation < 0)
                return false;

            // 评估直径必须>=0
            if (EvaluationDiameter < 0)
                return false;

            // 验证波长
            if (!string.IsNullOrEmpty(Wavelength))
            {
                if (!double.TryParse(Wavelength, out double wl) || wl <= 0 || wl > 10000)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 验证传统PV/RMS格式数据
        /// </summary>
        private bool ValidatePVRMS()
        {
            // 验证PV
            if (!string.IsNullOrEmpty(PVValue))
            {
                if (!double.TryParse(PVValue, out double pv) || pv <= 0)
                    return false;
            }

            // 验证RMS
            if (!string.IsNullOrEmpty(RMSValue))
            {
                if (!double.TryParse(RMSValue, out double rms) || rms <= 0)
                    return false;
            }

            // 验证波长
            if (!string.IsNullOrEmpty(Wavelength))
            {
                if (!double.TryParse(Wavelength, out double wl) || wl <= 0 || wl > 10000)
                    return false;
            }

            // 至少要有PV或RMS中的一个
            return !string.IsNullOrEmpty(PVValue) || !string.IsNullOrEmpty(RMSValue);
        }

        /// <summary>
        /// 获取标记属性
        /// </summary>
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
                { "FormStandard", FormStandard },
                // ISO 10110-5 参数
                { "PowerFringes", PowerFringes },
                { "IrregularityFringes", IrregularityFringes },
                { "RotationalSymmetryDeviation", RotationalSymmetryDeviation },
                { "HasRotationalSymmetry", HasRotationalSymmetry },
                { "EvaluationDiameter", EvaluationDiameter },
                { "IrregularityOnly", IrregularityOnly },
                // 传统 PV/RMS 参数
                { "PVValue", PVValue },
                { "RMSValue", RMSValue },
                { "AccuracyType", AccuracyType },
                // 通用参数
                { "Wavelength", Wavelength },
                { "TestMethod", TestMethod },
                { "MarkStyle", MarkStyle },
                { "FrameSize", FrameSize }
            };
        }

        /// <summary>
        /// 设置标记属性
        /// </summary>
        public void SetProperties(Dictionary<string, object> properties)
        {
            if (properties.ContainsKey("Position"))
                Position = (Vector2)properties["Position"];
            if (properties.ContainsKey("Scale"))
                Scale = Convert.ToDouble(properties["Scale"]);
            if (properties.ContainsKey("MarkText"))
                MarkText = (string)properties["MarkText"];
            if (properties.ContainsKey("ShowText"))
                ShowText = (bool)properties["ShowText"];
            if (properties.ContainsKey("TextOffset"))
                TextOffset = (Vector2)properties["TextOffset"];
            if (properties.ContainsKey("Rotation"))
                Rotation = Convert.ToDouble(properties["Rotation"]);
            if (properties.ContainsKey("IsVisible"))
                IsVisible = (bool)properties["IsVisible"];
            if (properties.ContainsKey("FormStandard"))
                FormStandard = (FormAccuracyStandard)properties["FormStandard"];
            // ISO 10110-5 参数
            if (properties.ContainsKey("PowerFringes"))
                PowerFringes = Convert.ToDouble(properties["PowerFringes"]);
            if (properties.ContainsKey("IrregularityFringes"))
                IrregularityFringes = Convert.ToDouble(properties["IrregularityFringes"]);
            if (properties.ContainsKey("RotationalSymmetryDeviation"))
                RotationalSymmetryDeviation = Convert.ToDouble(properties["RotationalSymmetryDeviation"]);
            if (properties.ContainsKey("HasRotationalSymmetry"))
                HasRotationalSymmetry = (bool)properties["HasRotationalSymmetry"];
            if (properties.ContainsKey("EvaluationDiameter"))
                EvaluationDiameter = Convert.ToDouble(properties["EvaluationDiameter"]);
            if (properties.ContainsKey("IrregularityOnly"))
                IrregularityOnly = (bool)properties["IrregularityOnly"];
            // 传统 PV/RMS 参数
            if (properties.ContainsKey("PVValue"))
                PVValue = (string)properties["PVValue"];
            if (properties.ContainsKey("RMSValue"))
                RMSValue = (string)properties["RMSValue"];
            if (properties.ContainsKey("AccuracyType"))
                AccuracyType = (FormAccuracyType)properties["AccuracyType"];
            // 通用参数
            if (properties.ContainsKey("Wavelength"))
                Wavelength = (string)properties["Wavelength"];
            if (properties.ContainsKey("TestMethod"))
                TestMethod = (string)properties["TestMethod"];
            if (properties.ContainsKey("MarkStyle"))
                MarkStyle = (FormAccuracyMarkStyle)properties["MarkStyle"];
            if (properties.ContainsKey("FrameSize"))
                FrameSize = (Vector2)properties["FrameSize"];

            UpdateMarkText();
            _markEntities.Clear();
        }

        /// <summary>
        /// 克隆标记
        /// </summary>
        IOpticalMark IOpticalMark.Clone()
        {
            return Clone() as FormAccuracyMark;
        }

        /// <summary>
        /// 获取标记描述
        /// </summary>
        public string GetDescription()
        {
            if (FormStandard == FormAccuracyStandard.ISO_10110_5)
            {
                return $"面形精度标记 - {MarkText} (ISO 10110-5, @{Wavelength}nm)";
            }
            else
            {
                return $"面形精度标记 - {MarkText.Replace('\n', ' ')} (PV/RMS, @{Wavelength}nm)";
            }
        }

        #endregion

        #region Entity 重写方法

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new FormAccuracyMark();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            FormAccuracyMark mark = base.Clone() as FormAccuracyMark;
            mark.Position = Position;
            mark.Scale = Scale;
            mark.MarkText = MarkText;
            mark.ShowText = ShowText;
            mark.TextOffset = TextOffset;
            mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.FormStandard = FormStandard;
            // ISO 10110-5 参数
            mark.PowerFringes = PowerFringes;
            mark.IrregularityFringes = IrregularityFringes;
            mark.RotationalSymmetryDeviation = RotationalSymmetryDeviation;
            mark.HasRotationalSymmetry = HasRotationalSymmetry;
            mark.EvaluationDiameter = EvaluationDiameter;
            mark.IrregularityOnly = IrregularityOnly;
            // 传统 PV/RMS 参数
            mark.PVValue = PVValue;
            mark.RMSValue = RMSValue;
            mark.AccuracyType = AccuracyType;
            // 通用参数
            mark.Wavelength = Wavelength;
            mark.TestMethod = TestMethod;
            mark.MarkStyle = MarkStyle;
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
                case 0:
                    Position = newPosition;
                    break;
                case 1:
                    var delta = newPosition - Position;
                    FrameSize = new Vector2(Math.Abs(delta.X) * 2, Math.Abs(delta.Y) * 2);
                    break;
                case 2:
                    if (ShowText)
                    {
                        TextOffset = newPosition - Position;
                    }
                    break;
            }
            // 清除缓存，下次绘制时重新生成
            _markEntities.Clear();
        }

        #endregion
    }

    /// <summary>
    /// 面形精度类型枚举
    /// </summary>
    public enum FormAccuracyType
    {
        /// <summary>
        /// 仅PV?        /// </summary>
        PV_Only = 0,
        
        /// <summary>
        /// 仅RMS?        /// </summary>
        RMS_Only = 1,
        
        /// <summary>
        /// PV和RMS?        /// </summary>
        PV_RMS = 2,
        
        /// <summary>
        /// Power/Irregularity
        /// </summary>
        PowerIrregularity = 3
    }

    /// <summary>
    /// 面形精度标记样式枚举
    /// </summary>
    public enum FormAccuracyMarkStyle
    {
        /// <summary>
        /// 虚线?        /// </summary>
        DashedBox = 0,
        
        /// <summary>
        /// 双线?        /// </summary>
        DoubleBox = 1,
        
        /// <summary>
        /// 波浪符号
        /// </summary>
        WaveSymbol = 2
    }
}