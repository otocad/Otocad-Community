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
    /// 焦点标记实现
    /// 用于标注光学系统的焦点位置，包括前焦点、后焦点和有效焦点等
    /// </summary>
    public class FocalPointMark : Entity, IOpticalMark
    {
        public override string className => "FocalPointMark";

        #region IOpticalMark 接口属性
        /// <summary>
        /// 标记类型
        /// </summary>
        public OpticalMarkType MarkType => OpticalMarkType.FocalPoint;

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
        public string MarkText { get; set; } = "F";

        /// <summary>
        /// 是否显示文本
        /// </summary>
        public bool ShowText { get; set; } = true;

        /// <summary>
        /// 文本偏移量
        /// </summary>
        public Vector2 TextOffset { get; set; } = new Vector2(8, -8);

        /// <summary>
        /// 标记旋转角度（度）
        /// </summary>
        public double Rotation { get; set; } = 0.0;

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible { get; set; } = true;

        #endregion

        #region 焦点特定属性
        /// <summary>
        /// 焦点类型
        /// </summary>
        public FocalPointType FocalType { get; set; } = FocalPointType.ObjectFocus;

        /// <summary>
        /// 焦距值（毫米）
        /// </summary>
        public double FocalLength { get; set; } = 100.0;

        /// <summary>
        /// 有效焦距（EFL）
        /// </summary>
        public double EffectiveFocalLength { get; set; } = 100.0;

        /// <summary>
        /// 后焦距（BFL）
        /// </summary>
        public double BackFocalLength { get; set; } = 95.0;

        /// <summary>
        /// 前焦距（FFL）
        /// </summary>
        public double FrontFocalLength { get; set; } = 95.0;

        /// <summary>
        /// 焦点标记样式
        /// </summary>
        public FocalPointStyle PointStyle { get; set; } = FocalPointStyle.CrossCircle;

        /// <summary>
        /// 符号大小
        /// </summary>
        public double SymbolSize { get; set; } = 10.0;

        /// <summary>
        /// 焦点标签
        /// </summary>
        public string FocalLabel { get; set; } = "物方焦点";

        /// <summary>
        /// 波长信息（用于色焦点）
        /// </summary>
        public string Wavelength { get; set; } = "";

        /// <summary>
        /// 数值孔径（NA）
        /// </summary>
        public double NumericalAperture { get; set; } = 0.0;

        /// <summary>
        /// 光束质量因子（M²）
        /// </summary>
        public double BeamQuality { get; set; } = 1.0;

        /// <summary>
        /// 是否显示光束
        /// </summary>
        public bool ShowBeam { get; set; } = false;

        /// <summary>
        /// 光束锥角（半角，度）
        /// </summary>
        public double BeamConeAngle { get; set; } = 5.0;

        #endregion

        private List<Entity> _markEntities = new List<Entity>();

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                if (_markEntities.Count == 0) Generate();
                if (_markEntities.Count == 0) return new Bounding(Position, 0, 0);
                double minX = double.PositiveInfinity, minY = double.PositiveInfinity;
                double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;
                foreach (var e in _markEntities)
                {
                    var eb = e.bounding;
                    minX = Math.Min(minX, eb.left);  maxX = Math.Max(maxX, eb.right);
                    minY = Math.Min(minY, eb.bottom); maxY = Math.Max(maxY, eb.top);
                }
                var c = new Vector2((minX + maxX) * 0.5, (minY + maxY) * 0.5);
                return new Bounding(c, maxX - minX, maxY - minY);
            }
        }

        #region 构造函数
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public FocalPointMark()
        {
            UpdateMarkText();
        }

        /// <summary>
        /// 带参数构造函数
        /// </summary>
        public FocalPointMark(Vector2 position, FocalPointType focalType = FocalPointType.ObjectFocus, double focalLength = 100.0)
        {
            Position = position;
            FocalType = focalType;
            FocalLength = focalLength;
            EffectiveFocalLength = focalLength;
            UpdateMarkText();
        }

        #endregion

        #region 核心方法

        /// <summary>
        /// 更新标记文本
        /// </summary>
        private void UpdateMarkText()
        {
            switch (FocalType)
            {
                case FocalPointType.ObjectFocus:
                    MarkText = "F";
                    FocalLabel = "物方焦点";
                    break;
                case FocalPointType.ImageFocus:
                    MarkText = "F'";
                    FocalLabel = "像方焦点";
                    break;
                case FocalPointType.PrincipalFocus:
                    MarkText = "Fp";
                    FocalLabel = "主焦点";
                    break;
                case FocalPointType.EffectiveFocus:
                    MarkText = "Fe";
                    FocalLabel = "有效焦点";
                    break;
                case FocalPointType.BackFocus:
                    MarkText = "Fb";
                    FocalLabel = "后焦点";
                    break;
                case FocalPointType.FrontFocus:
                    MarkText = "Ff";
                    FocalLabel = "前焦点";
                    break;
                case FocalPointType.ChromaticFocus:
                    MarkText = $"F-{Wavelength}";
                    FocalLabel = $"色焦点({Wavelength}nm)";
                    break;
                case FocalPointType.AstigmaticFocus:
                    MarkText = "Fa";
                    FocalLabel = "像散焦点";
                    break;
                case FocalPointType.VirtualFocus:
                    MarkText = "Fv";
                    FocalLabel = "虚焦点";
                    break;
                default:
                    MarkText = "F";
                    FocalLabel = "焦点";
                    break;
            }
        }

        /// <summary>
        /// 生成标记图形
        /// </summary>
        protected void Generate()
        {
            _markEntities.Clear();

            // 生成主焦点符号
            GenerateMainSymbol();

            // 生成光束（如果需要）
            if (ShowBeam)
            {
                GenerateBeam();
            }

            // 生成文本
            if (ShowText)
            {
                GenerateText();
            }
        }

        /// <summary>绕 Position 旋转 Rotation 度 (Rotation == 0 时原样返回)。对称符号(圆/同心环)旋转不变,
        /// 仅十字/方/菱/像散框/光束/文字锚点需旋转。文字字形保持水平。</summary>
        private Vector2 R(Vector2 p) =>
            Rotation == 0.0 ? p : Vector2.RotateInRadian(p, Position, Rotation * Math.PI / 180.0);

        /// <summary>
        /// 生成主焦点符号
        /// </summary>
        private void GenerateMainSymbol()
        {
            var focalColor = GetFocalColor();
            double size = SymbolSize * Scale;

            switch (PointStyle)
            {
                case FocalPointStyle.CrossCircle:
                    GenerateCrossCircleSymbol(size, focalColor);
                    break;
                case FocalPointStyle.Cross:
                    GenerateCrossSymbol(size, focalColor);
                    break;
                case FocalPointStyle.Circle:
                    GenerateCircleSymbol(size, focalColor);
                    break;
                case FocalPointStyle.FilledCircle:
                    GenerateFilledCircleSymbol(size, focalColor);
                    break;
                case FocalPointStyle.Square:
                    GenerateSquareSymbol(size, focalColor);
                    break;
                case FocalPointStyle.Diamond:
                    GenerateDiamondSymbol(size, focalColor);
                    break;
                case FocalPointStyle.Target:
                    GenerateTargetSymbol(size, focalColor);
                    break;
            }

            // 特殊标记（根据焦点类型）
            GenerateSpecialMarks(size, focalColor);
        }

        /// <summary>
        /// 生成十字圆符号
        /// </summary>
        private void GenerateCrossCircleSymbol(double size, lcdb.Colors.Color color)
        {
            // 外圆
            var circle = new Circle
            {
                center = Position,
                radius = size * 0.5,
                color = color
            };
            _markEntities.Add(circle);

            // 十字线
            GenerateCrossSymbol(size * 0.8, color);
        }

        /// <summary>
        /// 生成十字符号
        /// </summary>
        private void GenerateCrossSymbol(double size, lcdb.Colors.Color color)
        {
            double halfSize = size * 0.5;

            var horizontalLine = new Line(
                R(new Vector2(Position.X - halfSize, Position.Y)),
                R(new Vector2(Position.X + halfSize, Position.Y)))
            { color = color };

            var verticalLine = new Line(
                R(new Vector2(Position.X, Position.Y - halfSize)),
                R(new Vector2(Position.X, Position.Y + halfSize)))
            { color = color };

            _markEntities.Add(horizontalLine);
            _markEntities.Add(verticalLine);
        }

        /// <summary>
        /// 生成圆形符号
        /// </summary>
        private void GenerateCircleSymbol(double size, lcdb.Colors.Color color)
        {
            var circle = new Circle
            {
                center = Position,
                radius = size * 0.5,
                color = color
            };
            _markEntities.Add(circle);
        }

        /// <summary>
        /// 生成填充圆符号
        /// </summary>
        private void GenerateFilledCircleSymbol(double size, lcdb.Colors.Color color)
        {
            // 创建实心圆 - 简化实现，使用多个同心圆
            int rings = 5;
            for (int i = 0; i < rings; i++)
            {
                var circle = new Circle
                {
                    center = Position,
                    radius = size * 0.5 * (1 - (double)i / rings),
                    color = color
                };
                _markEntities.Add(circle);
            }
        }

        /// <summary>
        /// 生成正方形符号
        /// </summary>
        private void GenerateSquareSymbol(double size, lcdb.Colors.Color color)
        {
            double halfSize = size * 0.5;

            var p1 = R(new Vector2(Position.X - halfSize, Position.Y - halfSize));
            var p2 = R(new Vector2(Position.X + halfSize, Position.Y - halfSize));
            var p3 = R(new Vector2(Position.X + halfSize, Position.Y + halfSize));
            var p4 = R(new Vector2(Position.X - halfSize, Position.Y + halfSize));

            _markEntities.Add(new Line(p1, p2) { color = color });
            _markEntities.Add(new Line(p2, p3) { color = color });
            _markEntities.Add(new Line(p3, p4) { color = color });
            _markEntities.Add(new Line(p4, p1) { color = color });
        }

        /// <summary>
        /// 生成菱形符号
        /// </summary>
        private void GenerateDiamondSymbol(double size, lcdb.Colors.Color color)
        {
            double halfSize = size * 0.5;

            var p1 = R(new Vector2(Position.X, Position.Y + halfSize));      // 上
            var p2 = R(new Vector2(Position.X + halfSize, Position.Y));      // 右
            var p3 = R(new Vector2(Position.X, Position.Y - halfSize));      // 下
            var p4 = R(new Vector2(Position.X - halfSize, Position.Y));      // 左
            _markEntities.Add(new Line(p1, p2) { color = color });
            _markEntities.Add(new Line(p2, p3) { color = color });
            _markEntities.Add(new Line(p3, p4) { color = color });
            _markEntities.Add(new Line(p4, p1) { color = color });
        }

        /// <summary>
        /// 生成靶标符号
        /// </summary>
        private void GenerateTargetSymbol(double size, lcdb.Colors.Color color)
        {
            // 多层同心
            double[] radii = { 0.5, 0.35, 0.2 };
            
            foreach (double ratio in radii)
            {
                var circle = new Circle
                {
                    center = Position,
                    radius = size * ratio,
                    color = color
                };
                _markEntities.Add(circle);
            }

            // 中心十字
            GenerateCrossSymbol(size * 0.3, color);
        }

        /// <summary>
        /// 生成特殊标记
        /// </summary>
        private void GenerateSpecialMarks(double size, lcdb.Colors.Color color)
        {
            switch (FocalType)
            {
                case FocalPointType.VirtualFocus:
                    // 虚焦点：使用虚线圆
                    var virtualCircle = new Circle
                    {
                        center = Position,
                        radius = size * 0.6,
                        color = color,
                        lineType = LineType.Dash
                    };
                    _markEntities.Add(virtualCircle);
                    break;

                case FocalPointType.AstigmaticFocus:
                    // 像散焦点：添加椭圆标记
                    GenerateEllipticalMark(size, color);
                    break;

                case FocalPointType.ChromaticFocus:
                    // 色焦点：添加彩色环
                    GenerateColorRing(size, color);
                    break;
            }
        }

        /// <summary>
        /// 生成椭圆标记（像散焦点）
        /// </summary>
        private void GenerateEllipticalMark(double size, lcdb.Colors.Color color)
        {
            // 简化实现：用矩形表示椭圆的两个主方向
            double halfWidth = size * 0.3;
            double halfHeight = size * 0.6;

            // 水平椭圆 (X 向 halfHeight=0.6 比 Y 向 halfWidth=0.3 宽, 故为横椭圆)
            var p1 = R(new Vector2(Position.X - halfHeight, Position.Y - halfWidth));
            var p2 = R(new Vector2(Position.X + halfHeight, Position.Y - halfWidth));
            var p3 = R(new Vector2(Position.X + halfHeight, Position.Y + halfWidth));
            var p4 = R(new Vector2(Position.X - halfHeight, Position.Y + halfWidth));

            var ellipse1 = new Line(p1, p2) { color = color, lineType = LineType.Dash };
            var ellipse2 = new Line(p2, p3) { color = color, lineType = LineType.Dash };
            var ellipse3 = new Line(p3, p4) { color = color, lineType = LineType.Dash };
            var ellipse4 = new Line(p4, p1) { color = color, lineType = LineType.Dash };

            _markEntities.Add(ellipse1);
            _markEntities.Add(ellipse2);
            _markEntities.Add(ellipse3);
            _markEntities.Add(ellipse4);
        }

        /// <summary>
        /// 生成彩色环标记（色焦点）
        /// </summary>
        private void GenerateColorRing(double size, lcdb.Colors.Color color)
        {
            // 三层彩色环表示色差
            System.Drawing.Color[] colors = { System.Drawing.Color.Red, System.Drawing.Color.Green, System.Drawing.Color.Blue };
            double[] radii = { 0.6, 0.5, 0.4 };

            for (int i = 0; i < colors.Length; i++)
            {
                var coloredCircle = new Circle
                {
                    center = Position,
                    radius = size * radii[i],
                    color = lcdb.Colors.Color.FromColor(colors[i])
                };
                _markEntities.Add(coloredCircle);
            }
        }

        /// <summary>
        /// 生成光束
        /// </summary>
        private void GenerateBeam()
        {
            if (BeamConeAngle <= 0) return;

            var beamColor = GetFocalColor();
            double halfAngle = BeamConeAngle * Math.PI / 180;
            double beamLength = FocalLength * 0.3; // 可视化长度
            
            // 光束的两条边界线
            var topPoint = R(Position + new Vector2(
                beamLength * Math.Cos(-halfAngle),
                beamLength * Math.Sin(-halfAngle)));

            var bottomPoint = R(Position + new Vector2(
                beamLength * Math.Cos(halfAngle),
                beamLength * Math.Sin(halfAngle)));

            var topBeamLine = new Line(Position, topPoint) 
            { 
                color = beamColor, 
                lineType = LineType.Dot 
            };
            
            var bottomBeamLine = new Line(Position, bottomPoint) 
            { 
                color = beamColor, 
                lineType = LineType.Dot 
            };

            _markEntities.Add(topBeamLine);
            _markEntities.Add(bottomBeamLine);

            // 数值孔径标注
            if (NumericalAperture > 0)
            {
                var naText = new Text();
                naText.Value = $"NA={NumericalAperture:F3}";
                var naPos = R(new Vector2(Position.X + beamLength + 5, Position.Y));
                naText.Position = new Vector3(naPos.X, naPos.Y, 0.0);
                naText.Height = 3 * Scale;
                naText.color = beamColor;
                naText.alignment = lcdb.TextAlignment.CenterMiddle;
                _markEntities.Add(naText);
            }
        }

        /// <summary>
        /// 生成文本
        /// </summary>
        private void GenerateText()
        {
            var textPosition = Position + TextOffset;
            var focalColor = GetFocalColor();
            // 文字锚点随 Rotation 旋转(绕 Position), 字形保持水平; dy 为该行相对首行的纵偏移。
            Vector3 TP(double dy) { var p = R(new Vector2(textPosition.X, textPosition.Y + dy)); return new Vector3(p.X, p.Y, 0.0); }

            // 主标记文本
            var mainText = new Text();
            mainText.Value = MarkText;
            mainText.Position = TP(0);
            mainText.Height = 6 * Scale;
            mainText.color = focalColor;
            mainText.alignment = lcdb.TextAlignment.CenterMiddle;
            _markEntities.Add(mainText);

            // 焦点标签
            if (!string.IsNullOrEmpty(FocalLabel))
            {
                var labelText = new Text();
                labelText.Value = FocalLabel;
                labelText.Position = TP(-15 * Scale);
                labelText.Height = 4 * Scale;
                labelText.color = focalColor;
                labelText.alignment = lcdb.TextAlignment.CenterMiddle;
                _markEntities.Add(labelText);
            }

            // 焦距信息
            var focalLengthText = new Text();
            focalLengthText.Value = $"f = {EffectiveFocalLength:F1}mm";
            focalLengthText.Position = TP(-30 * Scale);
            focalLengthText.Height = 3 * Scale;
            focalLengthText.color = focalColor;
            focalLengthText.alignment = lcdb.TextAlignment.CenterMiddle;
            _markEntities.Add(focalLengthText);

            // 光束质量因子（如果不为1）
            if (BeamQuality != 1.0)
            {
                var beamQualityText = new Text();
                beamQualityText.Value = $"M² = {BeamQuality:F2}";
                beamQualityText.Position = TP(-45 * Scale);
                beamQualityText.Height = 3 * Scale;
                beamQualityText.color = focalColor;
                beamQualityText.alignment = lcdb.TextAlignment.CenterMiddle;
                _markEntities.Add(beamQualityText);
            }
        }

        /// <summary>
        /// 获取焦点颜色
        /// </summary>
        private lcdb.Colors.Color GetFocalColor()
        {
            switch (FocalType)
            {
                case FocalPointType.ObjectFocus:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Blue);
                case FocalPointType.ImageFocus:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Red);
                case FocalPointType.PrincipalFocus:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Green);
                case FocalPointType.EffectiveFocus:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Purple);
                case FocalPointType.BackFocus:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Orange);
                case FocalPointType.FrontFocus:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Cyan);
                case FocalPointType.ChromaticFocus:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Magenta);
                case FocalPointType.AstigmaticFocus:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Brown);
                case FocalPointType.VirtualFocus:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Gray);
                default:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Red);
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
            OpticalMarkSymbols.DrawFocalPointMark(g, position, MarkText, scale * (float)Scale);
        }
#endif

        /// <summary>
        /// 获取标记边界框
        /// </summary>
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
            if (FocalLength <= 0 || FocalLength > 10000)
                return false;

            if (EffectiveFocalLength <= 0 || EffectiveFocalLength > 10000)
                return false;

            if (NumericalAperture < 0 || NumericalAperture > 1.0)
                return false;

            if (BeamQuality < 1.0 || BeamQuality > 10.0)
                return false;

            if (BeamConeAngle < 0 || BeamConeAngle > 90)
                return false;

            return true;
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
                { "FocalType", FocalType },
                { "FocalLength", FocalLength },
                { "EffectiveFocalLength", EffectiveFocalLength },
                { "BackFocalLength", BackFocalLength },
                { "FrontFocalLength", FrontFocalLength },
                { "PointStyle", PointStyle },
                { "SymbolSize", SymbolSize },
                { "FocalLabel", FocalLabel },
                { "Wavelength", Wavelength },
                { "NumericalAperture", NumericalAperture },
                { "BeamQuality", BeamQuality },
                { "ShowBeam", ShowBeam },
                { "BeamConeAngle", BeamConeAngle }
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
            if (properties.ContainsKey("FocalType"))
                FocalType = (FocalPointType)properties["FocalType"];
            if (properties.ContainsKey("FocalLength"))
                FocalLength = (double)properties["FocalLength"];
            if (properties.ContainsKey("EffectiveFocalLength"))
                EffectiveFocalLength = (double)properties["EffectiveFocalLength"];
            if (properties.ContainsKey("BackFocalLength"))
                BackFocalLength = (double)properties["BackFocalLength"];
            if (properties.ContainsKey("FrontFocalLength"))
                FrontFocalLength = (double)properties["FrontFocalLength"];
            if (properties.ContainsKey("PointStyle"))
                PointStyle = (FocalPointStyle)properties["PointStyle"];
            if (properties.ContainsKey("SymbolSize"))
                SymbolSize = (double)properties["SymbolSize"];
            if (properties.ContainsKey("FocalLabel"))
                FocalLabel = (string)properties["FocalLabel"];
            if (properties.ContainsKey("Wavelength"))
                Wavelength = (string)properties["Wavelength"];
            if (properties.ContainsKey("NumericalAperture"))
                NumericalAperture = (double)properties["NumericalAperture"];
            if (properties.ContainsKey("BeamQuality"))
                BeamQuality = (double)properties["BeamQuality"];
            if (properties.ContainsKey("ShowBeam"))
                ShowBeam = (bool)properties["ShowBeam"];
            if (properties.ContainsKey("BeamConeAngle"))
                BeamConeAngle = (double)properties["BeamConeAngle"];

            UpdateMarkText();
        }

        /// <summary>
        /// 克隆标记
        /// </summary>
        IOpticalMark IOpticalMark.Clone()
        {
            return Clone() as FocalPointMark;
        }

        /// <summary>
        /// 获取标记描述
        /// </summary>
        public string GetDescription()
        {
            return $"焦点标记 - {FocalLabel} ({MarkText}) - EFL: {EffectiveFocalLength:F1}mm";
        }

        #endregion

        #region Entity 重写方法

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new FocalPointMark();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            FocalPointMark mark = base.Clone() as FocalPointMark;
            mark.Position = Position;
            mark.Scale = Scale;
            mark.MarkText = MarkText;
            mark.ShowText = ShowText;
            mark.TextOffset = TextOffset;
            mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.FocalType = FocalType;
            mark.FocalLength = FocalLength;
            mark.EffectiveFocalLength = EffectiveFocalLength;
            mark.BackFocalLength = BackFocalLength;
            mark.FrontFocalLength = FrontFocalLength;
            mark.PointStyle = PointStyle;
            mark.SymbolSize = SymbolSize;
            mark.FocalLabel = FocalLabel;
            mark.Wavelength = Wavelength;
            mark.NumericalAperture = NumericalAperture;
            mark.BeamQuality = BeamQuality;
            mark.ShowBeam = ShowBeam;
            mark.BeamConeAngle = BeamConeAngle;
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
            Scale = (transform * scaleVector - transform * new Vector2(0, 0)).length;
            _markEntities.Clear();
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            gripPoints.Add(new GripPoint(GripPointType.Center, Position));

            double size = SymbolSize * Scale;
            gripPoints.Add(new GripPoint(GripPointType.Quad, Position + new Vector2(size, 0)));

            if (ShowText)
            {
                var textPos = Position + TextOffset;
                gripPoints.Add(new GripPoint(GripPointType.Center, textPos));
            }

            return gripPoints;
        }

        /// <summary>
        /// 对象捕捉点
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPnts = new List<ObjectSnapPoint>();
            
            // 中心捕捉点
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Center, Position));
            
            // 四个象限点
            double size = SymbolSize * Scale * 0.5;
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Quad, Position + new Vector2(size, 0)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Quad, Position + new Vector2(0, size)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Quad, Position + new Vector2(-size, 0)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Quad, Position + new Vector2(0, -size)));
            
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
                case 1: // 大小调整点
                    SymbolSize = (newPosition - Position).length;
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
    /// 焦点类型枚举
    /// </summary>
    public enum FocalPointType
    {
        /// <summary>
        /// 物方焦点
        /// </summary>
        ObjectFocus = 0,
        
        /// <summary>
        /// 像方焦点
        /// </summary>
        ImageFocus = 1,
        
        /// <summary>
        /// 主焦点
        /// </summary>
        PrincipalFocus = 2,
        
        /// <summary>
        /// 有效焦点
        /// </summary>
        EffectiveFocus = 3,
        
        /// <summary>
        /// 后焦点
        /// </summary>
        BackFocus = 4,
        
        /// <summary>
        /// 前焦点
        /// </summary>
        FrontFocus = 5,
        
        /// <summary>
        /// 色焦点
        /// </summary>
        ChromaticFocus = 6,
        
        /// <summary>
        /// 像散焦点
        /// </summary>
        AstigmaticFocus = 7,
        
        /// <summary>
        /// 虚焦点
        /// </summary>
        VirtualFocus = 8
    }

    /// <summary>
    /// 焦点样式枚举
    /// </summary>
    public enum FocalPointStyle
    {
        /// <summary>
        /// 十字圆符号
        /// </summary>
        CrossCircle = 0,
        
        /// <summary>
        /// 十字符号
        /// </summary>
        Cross = 1,
        
        /// <summary>
        /// 圆形符号
        /// </summary>
        Circle = 2,
        
        /// <summary>
        /// 填充圆符号
        /// </summary>
        FilledCircle = 3,
        
        /// <summary>
        /// 正方形符号
        /// </summary>
        Square = 4,
        
        /// <summary>
        /// 菱形符号
        /// </summary>
        Diamond = 5,
        
        /// <summary>
        /// 靶标符号
        /// </summary>
        Target = 6
    }
}