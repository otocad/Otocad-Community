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
    /// 【已废弃 2026-06-04】已合并入 <see cref="CenteringToleranceMark"/>(同 ISO 10110-6 对中, 代号 4/)。
    /// 已移出 Ribbon/命令;仅保留类供旧 .otocad 反序列化, 勿新增引用。
    ///
    /// 中心偏差标记实现
    /// 用于标注光学元件的中心偏差、偏心和倾斜等几何公?    /// </summary>
    public class CenterDeviationMark : Entity, IOpticalMark
    {
        public override string className => "CenterDeviationMark";

        #region IOpticalMark 接口属性?
        /// <summary>
        /// 标记类型
        /// </summary>
        public OpticalMarkType MarkType => OpticalMarkType.CenterDeviation;

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
        public string MarkText { get; set; } = "δ?.05mm";

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

        #region 中心偏差特定属性?
        /// <summary>
        /// 偏心值（毫米?        /// </summary>
        public double DecentrationValue { get; set; } = 0.05;

        /// <summary>
        /// 倾斜值（度或弧度?        /// </summary>
        public double TiltValue { get; set; } = 0.0;

        /// <summary>
        /// 倾斜单位
        /// </summary>
        public AngleUnit TiltUnit { get; set; } = AngleUnit.Degree;

        /// <summary>
        /// 中心偏差类型
        /// </summary>
        public CenterDeviationType DeviationType { get; set; } = CenterDeviationType.Decentration;

        /// <summary>
        /// 公差等级
        /// </summary>
        public string ToleranceGrade { get; set; } = "IT7";

        /// <summary>
        /// 测试方法
        /// </summary>
        public string TestMethod { get; set; } = "光学自准直仪";

        /// <summary>
        /// 标记样式
        /// </summary>
        public CenterDeviationMarkStyle MarkStyle { get; set; } = CenterDeviationMarkStyle.CrossCircle;

        /// <summary>
        /// 符号半径
        /// </summary>
        public double SymbolRadius { get; set; } = 15.0;

        #endregion

        private List<Entity> _markEntities = new List<Entity>();

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                double totalWidth = SymbolRadius * 2 + (ShowText ? Math.Abs(TextOffset.X) + 80 : 0);
                double totalHeight = SymbolRadius * 2 + (ShowText ? Math.Abs(TextOffset.Y) + 20 : 0);
                return new Bounding(Position, totalWidth, totalHeight);
            }
        }

        #region 构造函数?
        /// <summary>
        /// 默认构造函数?        /// </summary>
        public CenterDeviationMark()
        {
            UpdateMarkText();
        }

        /// <summary>
        /// 带参数构造函数?        /// </summary>
        public CenterDeviationMark(Vector2 position, double decentrationValue, double tiltValue = 0.0)
        {
            Position = position;
            DecentrationValue = decentrationValue;
            TiltValue = tiltValue;
            
            if (tiltValue > 0)
            {
                DeviationType = CenterDeviationType.DecentrationAndTilt;
            }
            
            UpdateMarkText();
        }

        #endregion

        #region 核心方法

        /// <summary>
        /// 更新标记文本.
        /// ISO 10110-6 规范代码: 中心标识 = "4" (不是 "6"; 6 是部分号, 4 是符号代码).
        /// 严格 ISO 格式: "4/σ(τ')" — σ μm decentering, τ arc-min tilt.
        /// </summary>
        private void UpdateMarkText()
        {
            switch (DeviationType)
            {
                case CenterDeviationType.Decentration:
                    MarkText = $"4/{DecentrationValue * 1000:G3}";  // mm → μm
                    break;
                case CenterDeviationType.Tilt:
                    MarkText = $"4/—({FormatTiltArcMin(TiltValue)})";
                    break;
                case CenterDeviationType.DecentrationAndTilt:
                    MarkText = $"4/{DecentrationValue * 1000:G3}({FormatTiltArcMin(TiltValue)})";
                    break;
                case CenterDeviationType.ISO10110_6:
                    MarkText = $"4/{DecentrationValue * 1000:G3}({FormatTiltArcMin(TiltValue)})";
                    break;
                default:
                    MarkText = $"4/{DecentrationValue * 1000:G3}";
                    break;
            }
        }

        /// <summary>
        /// 倾斜值统一转为 arc-min ('). 接收 deg 或 rad 输入, 输出 "N'" 字符串.
        /// </summary>
        private string FormatTiltArcMin(double tilt)
        {
            if (tilt <= 0) return "—";
            double arcMin = TiltUnit == AngleUnit.Degree ? tilt * 60.0 : tilt * (180.0 / Math.PI) * 60.0;
            return $"{arcMin:G3}'";
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
        /// 生成主标记符号 — ISO 10110-6 矩形框 + 居中代码 "4/σ(τ')".
        /// 旧实现 (CrossCircle/OffsetCircles/TiltArrow 装饰图标 + 中文 δ/θ 标注) 不符合 ISO, 已统一为矩形框格式.
        /// 其他 MarkStyle 已废弃, 保留 enum 仅为反序列化兼容.
        /// </summary>
        private void GenerateMainSymbol()
        {
            var markColor = GetMarkColor();
            double radius = SymbolRadius * Scale;
            double halfWidth = radius * 1.4;
            double halfHeight = radius * 0.55;

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

            _markEntities.Add(new Text
            {
                Value = MarkText,
                Position = new Vector3(Position.X, Position.Y, 0.0),
                Height = halfHeight * 0.7,
                color = markColor,
                alignment = lcdb.TextAlignment.CenterMiddle,
            });
        }

        /// <summary>
        /// 生成十字圆符号
        /// </summary>
        private void GenerateCrossCircleSymbol(double radius, lcdb.Colors.Color color)
        {
            // 绘制圆
            var circle = new Circle
            {
                center = Position,
                radius = radius,
                color = color
            };
            _markEntities.Add(circle);

            // 绘制十字线
            var horizontalLine = new Line(
                new Vector2(Position.X - radius, Position.Y),
                new Vector2(Position.X + radius, Position.Y))
            { color = color };
            
            var verticalLine = new Line(
                new Vector2(Position.X, Position.Y - radius),
                new Vector2(Position.X, Position.Y + radius))
            { color = color };

            _markEntities.Add(horizontalLine);
            _markEntities.Add(verticalLine);

            // 如果有偏心值，在圆心附近添加偏心指
            if (DecentrationValue > 0)
            {
                double offsetDistance = radius * 0.3;
                var offsetCenter = new Vector2(Position.X + offsetDistance, Position.Y);
                var offsetCircle = new Circle
                {
                    center = offsetCenter,
                    radius = radius * 0.2,
                    color = color
                };
                _markEntities.Add(offsetCircle);

                // 连接线
                var connectionLine = new Line(Position, offsetCenter) { color = color };
                _markEntities.Add(connectionLine);
            }
        }

        /// <summary>
        /// 生成偏移圆符号
        /// </summary>
        private void GenerateOffsetCirclesSymbol(double radius, lcdb.Colors.Color color)
        {
            // 主圆
            var mainCircle = new Circle
            {
                center = Position,
                radius = radius,
                color = color
            };
            _markEntities.Add(mainCircle);

            // 偏移圆（虚线
            double offset = radius * 0.3;
            var offsetCircle = new Circle
            {
                center = new Vector2(Position.X + offset, Position.Y),
                radius = radius,
                color = color,
                lineType = LineType.Dash
            };
            _markEntities.Add(offsetCircle);
        }

        /// <summary>
        /// 生成倾斜箭头符号
        /// </summary>
        private void GenerateTiltArrowSymbol(double radius, lcdb.Colors.Color color)
        {
            // 基准线（水平）
            var baseLine = new Line(
                new Vector2(Position.X - radius, Position.Y),
                new Vector2(Position.X + radius, Position.Y))
            { color = color };
            _markEntities.Add(baseLine);

            // 倾斜线
            double tiltAngleRad = TiltUnit == AngleUnit.Degree ? 
                TiltValue * Math.PI / 180 : TiltValue;
            
            var tiltEndX = Position.X + radius * Math.Cos(tiltAngleRad);
            var tiltEndY = Position.Y + radius * Math.Sin(tiltAngleRad);
            
            var tiltLine = new Line(
                new Vector2(Position.X - radius, Position.Y),
                new Vector2(tiltEndX, tiltEndY))
            { color = color, lineType = LineType.Dash };
            _markEntities.Add(tiltLine);

            // 角度
            GenerateAngleArc(radius * 0.5, tiltAngleRad, color);

            // 箭头
            GenerateArrowHead(new Vector2(tiltEndX, tiltEndY), tiltAngleRad, color);
        }

        /// <summary>
        /// 生成ISO 10110标准符号
        /// </summary>
        private void GenerateISO10110Symbol(double radius, lcdb.Colors.Color color)
        {
            // 绘制ISO标准框
            double frameWidth = radius * 2.5;
            double frameHeight = radius * 1.2;
            GenerateISOFrame(frameWidth, frameHeight, color);

            // 在框内添加ISO标准文本
            var isoText = new Text();
            isoText.Value = "ISO 10110-6";
            isoText.Position = new Vector3(Position.X, Position.Y + frameHeight * 0.3, 0.0);
            isoText.Height = 3 * Scale;
            isoText.color = color;
            isoText.alignment = lcdb.TextAlignment.CenterMiddle;
            _markEntities.Add(isoText);

            // 偏差数量
            var valueText = new Text();
            valueText.Value = $"6/{DecentrationValue:F3}/{TiltValue:F3}";
            valueText.Position = new Vector3(Position.X, Position.Y - frameHeight * 0.2, 0.0);
            valueText.Height = 4 * Scale;
            valueText.color = color;
            valueText.alignment = lcdb.TextAlignment.CenterMiddle;
            _markEntities.Add(valueText);
        }

        /// <summary>
        /// 生成数值文本
        /// </summary>
        private void GenerateValueText(lcdb.Colors.Color color)
        {
            var valueText = new Text();
            valueText.Value = MarkText.Split('\n')[0]; // 只取第一
            valueText.Position = new Vector3(Position.X, Position.Y - SymbolRadius * Scale - 5, 0.0);
            valueText.Height = 4 * Scale;
            valueText.color = color;
            valueText.alignment = lcdb.TextAlignment.CenterMiddle;
            _markEntities.Add(valueText);
        }

        /// <summary>
        /// 生成角度线
        /// </summary>
        private void GenerateAngleArc(double arcRadius, double angle, lcdb.Colors.Color color)
        {
            // 简化实现：用多段直线近似弧
            int segments = 10;
            double angleStep = angle / segments;
            
            for (int i = 0; i < segments; i++)
            {
                double angle1 = i * angleStep;
                double angle2 = (i + 1) * angleStep;
                
                var p1 = new Vector2(
                    Position.X + arcRadius * Math.Cos(angle1),
                    Position.Y + arcRadius * Math.Sin(angle1));
                var p2 = new Vector2(
                    Position.X + arcRadius * Math.Cos(angle2),
                    Position.Y + arcRadius * Math.Sin(angle2));
                
                var arcSegment = new Line(p1, p2) { color = color };
                _markEntities.Add(arcSegment);
            }
        }

        /// <summary>
        /// 生成箭头头部
        /// </summary>
        private void GenerateArrowHead(Vector2 tip, double angle, lcdb.Colors.Color color)
        {
            double arrowSize = 5 * Scale;
            double arrowAngle = 0.3; // ?7
            
            var leftWing = new Vector2(
                tip.X - arrowSize * Math.Cos(angle - arrowAngle),
                tip.Y - arrowSize * Math.Sin(angle - arrowAngle));
            
            var rightWing = new Vector2(
                tip.X - arrowSize * Math.Cos(angle + arrowAngle),
                tip.Y - arrowSize * Math.Sin(angle + arrowAngle));
            
            _markEntities.Add(new Line(tip, leftWing) { color = color });
            _markEntities.Add(new Line(tip, rightWing) { color = color });
        }

        /// <summary>
        /// 生成ISO标准框
        /// </summary>
        private void GenerateISOFrame(double width, double height, lcdb.Colors.Color color)
        {
            double halfWidth = width * 0.5;
            double halfHeight = height * 0.5;

            var p1 = new Vector2(Position.X - halfWidth, Position.Y - halfHeight);
            var p2 = new Vector2(Position.X + halfWidth, Position.Y - halfHeight);
            var p3 = new Vector2(Position.X + halfWidth, Position.Y + halfHeight);
            var p4 = new Vector2(Position.X - halfWidth, Position.Y + halfHeight);

            _markEntities.Add(new Line(p1, p2) { color = color });
            _markEntities.Add(new Line(p2, p3) { color = color });
            _markEntities.Add(new Line(p3, p4) { color = color });
            _markEntities.Add(new Line(p4, p1) { color = color });

            // 添加分割线
            var dividerLine = new Line(
                new Vector2(Position.X - halfWidth, Position.Y),
                new Vector2(Position.X + halfWidth, Position.Y))
            { color = color };
            _markEntities.Add(dividerLine);
        }

        /// <summary>
        /// 外部说明: ISO 10110-6 框内代码即完整规范, 外部仅注单位 (μm decentering, ' arc-min tilt).
        /// </summary>
        private void GenerateText()
        {
            var textPosition = Position + TextOffset;
            _markEntities.Add(new Text
            {
                Value = "μm; ' arc-min",
                Position = new Vector3(textPosition.X, textPosition.Y, 0.0),
                Height = 4 * Scale,
                color = GetMarkColor(),
                alignment = lcdb.TextAlignment.CenterMiddle,
            });
        }

        /// <summary>
        /// 获取标记颜色
        /// </summary>
        private lcdb.Colors.Color GetMarkColor()
        {
            // 根据偏差值选择颜色
            if (DecentrationValue <= 0.01)
                return lcdb.Colors.Color.FromColor(System.Drawing.Color.Green);      // 高精度?- 绿色
            else if (DecentrationValue <= 0.05)
                return lcdb.Colors.Color.FromColor(System.Drawing.Color.Blue);       // 中等精度 - 蓝色
            else if (DecentrationValue <= 0.1)
                return lcdb.Colors.Color.FromColor(System.Drawing.Color.Orange);     // 较低精度 - 橙色
            else
                return lcdb.Colors.Color.FromColor(System.Drawing.Color.Red);        // 低精度?- 红色
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

            switch (DeviationType)
            {
                case CenterDeviationType.Decentration:
                    OpticalMarkSymbols.DrawDecenterMark(g, position, (float)DecentrationValue, scale * (float)Scale);
                    break;
                case CenterDeviationType.Tilt:
                    float tiltAngle = TiltUnit == AngleUnit.Degree ? (float)TiltValue : (float)(TiltValue * 180 / Math.PI);
                    OpticalMarkSymbols.DrawTiltMark(g, position, tiltAngle, scale * (float)Scale);
                    break;
                case CenterDeviationType.DecentrationAndTilt:
                    OpticalMarkSymbols.DrawISO10110_6Mark(g, position, (float)DecentrationValue, (float)TiltValue, scale * (float)Scale);
                    break;
                case CenterDeviationType.ISO10110_6:
                    OpticalMarkSymbols.DrawISO10110_6Mark(g, position, (float)DecentrationValue, (float)TiltValue, scale * (float)Scale);
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
            if (DecentrationValue < 0 || DecentrationValue > 10.0)
                return false;

            if (TiltValue < 0)
                return false;

            if (TiltUnit == AngleUnit.Degree && TiltValue > 90)
                return false;

            if (TiltUnit == AngleUnit.Radian && TiltValue > Math.PI / 2)
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
                { "DecentrationValue", DecentrationValue },
                { "TiltValue", TiltValue },
                { "TiltUnit", TiltUnit },
                { "DeviationType", DeviationType },
                { "ToleranceGrade", ToleranceGrade },
                { "TestMethod", TestMethod },
                { "MarkStyle", MarkStyle },
                { "SymbolRadius", SymbolRadius }
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
            if (properties.ContainsKey("DecentrationValue"))
                DecentrationValue = (double)properties["DecentrationValue"];
            if (properties.ContainsKey("TiltValue"))
                TiltValue = (double)properties["TiltValue"];
            if (properties.ContainsKey("TiltUnit"))
                TiltUnit = (AngleUnit)properties["TiltUnit"];
            if (properties.ContainsKey("DeviationType"))
                DeviationType = (CenterDeviationType)properties["DeviationType"];
            if (properties.ContainsKey("ToleranceGrade"))
                ToleranceGrade = (string)properties["ToleranceGrade"];
            if (properties.ContainsKey("TestMethod"))
                TestMethod = (string)properties["TestMethod"];
            if (properties.ContainsKey("MarkStyle"))
                MarkStyle = (CenterDeviationMarkStyle)properties["MarkStyle"];
            if (properties.ContainsKey("SymbolRadius"))
                SymbolRadius = (double)properties["SymbolRadius"];

            UpdateMarkText();
        }

        /// <summary>
        /// 克隆标记
        /// </summary>
        IOpticalMark IOpticalMark.Clone()
        {
            return Clone() as CenterDeviationMark;
        }

        /// <summary>
        /// 获取标记描述
        /// </summary>
        public string GetDescription()
        {
            return $"中心偏差标记 - {MarkText.Replace('\n', ' ')} (等级: {ToleranceGrade})";
        }

        #endregion

        #region Entity 重写方法

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new CenterDeviationMark();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            CenterDeviationMark mark = base.Clone() as CenterDeviationMark;
            mark.Position = Position;
            mark.Scale = Scale;
            mark.MarkText = MarkText;
            mark.ShowText = ShowText;
            mark.TextOffset = TextOffset;
            mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.DecentrationValue = DecentrationValue;
            mark.TiltValue = TiltValue;
            mark.TiltUnit = TiltUnit;
            mark.DeviationType = DeviationType;
            mark.ToleranceGrade = ToleranceGrade;
            mark.TestMethod = TestMethod;
            mark.MarkStyle = MarkStyle;
            mark.SymbolRadius = SymbolRadius;
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

            double radius = SymbolRadius * Scale;
            gripPoints.Add(new GripPoint(GripPointType.Quad, Position + new Vector2(radius, 0)));

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

            double radius = SymbolRadius * Scale;
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Quad, Position + new Vector2(radius, 0)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Quad, Position + new Vector2(0, radius)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Quad, Position + new Vector2(-radius, 0)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Quad, Position + new Vector2(0, -radius)));

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
                    SymbolRadius = (newPosition - Position).length;
                    break;
                case 2:
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
    /// 中心偏差类型枚举
    /// </summary>
    public enum CenterDeviationType
    {
        /// <summary>
        /// 仅偏?        /// </summary>
        Decentration = 0,
        
        /// <summary>
        /// 仅倾斜
        /// </summary>
        Tilt = 1,
        
        /// <summary>
        /// 偏心和倾斜
        /// </summary>
        DecentrationAndTilt = 2,
        
        /// <summary>
        /// ISO 10110-6标准
        /// </summary>
        ISO10110_6 = 3
    }

    /// <summary>
    /// 中心偏差标记样式枚举
    /// </summary>
    public enum CenterDeviationMarkStyle
    {
        /// <summary>
        /// 十字圆符号?        /// </summary>
        CrossCircle = 0,
        
        /// <summary>
        /// 偏移圆符号?        /// </summary>
        OffsetCircles = 1,
        
        /// <summary>
        /// 倾斜箭头符号
        /// </summary>
        TiltArrow = 2,
        
        /// <summary>
        /// ISO 10110标准符号
        /// </summary>
        ISO10110 = 3
    }

    /// <summary>
    /// 角度单位枚举
    /// </summary>
    public enum AngleUnit
    {
        /// <summary>
        /// ?        /// </summary>
        Degree = 0,
        
        /// <summary>
        /// 弧度
        /// </summary>
        Radian = 1
    }
}