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
    /// 光轴标记实现
    /// 用于标注光学系统的光轴，显示光线传播方向和光学元件的对齐基准
    /// </summary>
    public class OpticalAxisMark : Entity, IOpticalMark
    {
        public override string className => "OpticalAxisMark";

        #region IOpticalMark 接口属性?
        /// <summary>
        /// 标记类型
        /// </summary>
        public OpticalMarkType MarkType => OpticalMarkType.OpticalAxis;

        /// <summary>
        /// 标记位置（起点）
        /// </summary>
        public Vector2 Position { get; set; } = new Vector2(0, 0);

        /// <summary>
        /// 标记大小/比例
        /// </summary>
        public double Scale { get; set; } = 1.0;

        /// <summary>
        /// 标记文本内容
        /// </summary>
        public string MarkText { get; set; } = "OA";

        /// <summary>
        /// 是否显示文本
        /// </summary>
        public bool ShowText { get; set; } = true;

        /// <summary>
        /// 文本偏移量?        /// </summary>
        public Vector2 TextOffset { get; set; } = new Vector2(5, -10);

        /// <summary>
        /// 标记旋转角度（度）?        /// </summary>
        public double Rotation { get; set; } = 0.0;

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible { get; set; } = true;

        #endregion

        #region 光轴特定属性?
        /// <summary>
        /// 光轴终点位置
        /// </summary>
        public Vector2 EndPosition { get; set; } = new Vector2(100, 0);

        /// <summary>
        /// 光轴长度（如果使用长度而不是终点）
        /// </summary>
        public double AxisLength { get; set; } = 100.0;

        /// <summary>
        /// 光轴方向角度（度）?        /// </summary>
        public double Direction { get; set; } = 0.0;

        /// <summary>
        /// 光轴类型
        /// </summary>
        public OpticalAxisType AxisType { get; set; } = OpticalAxisType.MainAxis;

        /// <summary>
        /// 光轴样式
        /// </summary>
        public OpticalAxisStyle AxisStyle { get; set; } = OpticalAxisStyle.DashDotLine;

        /// <summary>
        /// 是否显示箭头
        /// </summary>
        public bool ShowArrow { get; set; } = true;

        /// <summary>
        /// 箭头大小
        /// </summary>
        public double ArrowSize { get; set; } = 8.0;

        /// <summary>
        /// 光传播方?        /// </summary>
        public PropagationDirection PropagationDir { get; set; } = PropagationDirection.Forward;

        /// <summary>
        /// 波长信息（用于色散光轴）
        /// </summary>
        public string Wavelength { get; set; } = "";

        /// <summary>
        /// 光轴标签
        /// </summary>
        public string AxisLabel { get; set; } = "主光轴";

        #endregion

        private List<Entity> _markEntities = new List<Entity>();

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                var startPos = Position;
                var endPos = GetCalculatedEndPosition();
                
                double minX = Math.Min(startPos.X, endPos.X);
                double maxX = Math.Max(startPos.X, endPos.X);
                double minY = Math.Min(startPos.Y, endPos.Y);
                double maxY = Math.Max(startPos.Y, endPos.Y);

                // 考虑文本和箭头的额外空间
                double margin = ArrowSize + 20;
                
                if (ShowText)
                {
                    var textPos = GetTextPosition();
                    minX = Math.Min(minX, textPos.X - 50);
                    maxX = Math.Max(maxX, textPos.X + 50);
                    minY = Math.Min(minY, textPos.Y - 10);
                    maxY = Math.Max(maxY, textPos.Y + 10);
                }

                return new Bounding(
                    new Vector2((minX + maxX) / 2, (minY + maxY) / 2),
                    maxX - minX + margin,
                    maxY - minY + margin);
            }
        }

        #region 构造函数?
        /// <summary>
        /// 默认构造函数?        /// </summary>
        public OpticalAxisMark()
        {
            UpdateAxisGeometry();
        }

        /// <summary>
        /// 带参数构造函数?        /// </summary>
        public OpticalAxisMark(Vector2 startPosition, Vector2 endPosition, OpticalAxisType axisType = OpticalAxisType.MainAxis)
        {
            Position = startPosition;
            EndPosition = endPosition;
            AxisType = axisType;
            UpdateAxisGeometry();
            UpdateMarkText();
        }

        /// <summary>
        /// 使用长度和角度的构造函数?        /// </summary>
        public OpticalAxisMark(Vector2 startPosition, double length, double direction, OpticalAxisType axisType = OpticalAxisType.MainAxis)
        {
            Position = startPosition;
            AxisLength = length;
            Direction = direction;
            AxisType = axisType;
            CalculateEndPosition();
            UpdateMarkText();
        }

        #endregion

        #region 核心方法

        /// <summary>
        /// 更新标记文本
        /// </summary>
        private void UpdateMarkText()
        {
            switch (AxisType)
            {
                case OpticalAxisType.MainAxis:
                    MarkText = "OA";
                    break;
                case OpticalAxisType.SecondaryAxis:
                    MarkText = "SA";
                    break;
                case OpticalAxisType.MechanicalAxis:
                    MarkText = "MA";
                    break;
                case OpticalAxisType.ReferenceAxis:
                    MarkText = "RA";
                    break;
                case OpticalAxisType.DispersionAxis:
                    MarkText = $"DA-{Wavelength}";
                    break;
                default:
                    MarkText = "OA";
                    break;
            }
        }

        /// <summary>
        /// 更新光轴几何信息
        /// </summary>
        private void UpdateAxisGeometry()
        {
            var direction = EndPosition - Position;
            AxisLength = direction.length;
            Direction = Math.Atan2(direction.Y, direction.X) * 180 / Math.PI;
        }

        /// <summary>
        /// 计算终点位置
        /// </summary>
        private void CalculateEndPosition()
        {
            double radians = Direction * Math.PI / 180;
            EndPosition = Position + new Vector2(
                AxisLength * Math.Cos(radians),
                AxisLength * Math.Sin(radians));
        }

        /// <summary>
        /// 获取计算的终点位置（考虑旋转?        /// </summary>
        private Vector2 GetCalculatedEndPosition()
        {
            if (Rotation == 0)
                return EndPosition;

            double rotRadians = Rotation * Math.PI / 180;
            var direction = EndPosition - Position;
            var rotatedDirection = new Vector2(
                direction.X * Math.Cos(rotRadians) - direction.Y * Math.Sin(rotRadians),
                direction.X * Math.Sin(rotRadians) + direction.Y * Math.Cos(rotRadians));
            
            return Position + rotatedDirection;
        }

        /// <summary>
        /// 生成标记图形
        /// </summary>
        protected void Generate()
        {
            _markEntities.Clear();

            // 生成主光轴线
            GenerateAxisLine();

            // 生成箭头
            if (ShowArrow)
            {
                GenerateArrowHead();
            }

            // 生成文本
            if (ShowText)
            {
                GenerateText();
            }

            // 生成特殊标记（根据轴类型
            GenerateSpecialMarks();
        }

        /// <summary>
        /// 生成光轴线?        /// </summary>
        private void GenerateAxisLine()
        {
            var startPos = Position;
            var endPos = GetCalculatedEndPosition();
            var axisColor = GetAxisColor();

            var axisLine = new Line(startPos, endPos) { color = axisColor };
            
            // 设置线型
            switch (AxisStyle)
            {
                case OpticalAxisStyle.SolidLine:
                    axisLine.lineType = LineType.Solid;
                    break;
                case OpticalAxisStyle.DashLine:
                    axisLine.lineType = LineType.Dash;
                    break;
                case OpticalAxisStyle.DashDotLine:
                    axisLine.lineType = LineType.DashDot;
                    break;
                case OpticalAxisStyle.DotLine:
                    axisLine.lineType = LineType.Dot;
                    break;
            }

            _markEntities.Add(axisLine);
        }

        /// <summary>
        /// 生成箭头
        /// </summary>
        private void GenerateArrowHead()
        {
            var endPos = GetCalculatedEndPosition();
            var direction = (endPos - Position);
            direction.Normalize();
            var axisColor = GetAxisColor();
            
            // 根据传播方向确定箭头位置
            Vector2 arrowPos;
            Vector2 arrowDir;
            
            switch (PropagationDir)
            {
                case PropagationDirection.Forward:
                    arrowPos = endPos;
                    arrowDir = direction;
                    break;
                case PropagationDirection.Backward:
                    arrowPos = Position;
                    arrowDir = -direction;
                    break;
                case PropagationDirection.Bidirectional:
                    // 绘制两个箭头
                    GenerateArrowAtPosition(endPos, direction, axisColor);
                    GenerateArrowAtPosition(Position, -direction, axisColor);
                    return;
                default:
                    arrowPos = endPos;
                    arrowDir = direction;
                    break;
            }
            
            GenerateArrowAtPosition(arrowPos, arrowDir, axisColor);
        }

        /// <summary>
        /// 在指定位置生成箭?        /// </summary>
        private void GenerateArrowAtPosition(Vector2 tipPosition, Vector2 direction, lcdb.Colors.Color color)
        {
            double arrowSize = ArrowSize * Scale;
            // arrowAngle = 0.4 弧度，约23度，暂时未使用但保留用于角度调整
            
            var perpendicular = new Vector2(-direction.Y, direction.X);
            
            var leftWing = tipPosition - direction * arrowSize + perpendicular * arrowSize * 0.5;
            var rightWing = tipPosition - direction * arrowSize - perpendicular * arrowSize * 0.5;
            
            _markEntities.Add(new Line(tipPosition, leftWing) { color = color });
            _markEntities.Add(new Line(tipPosition, rightWing) { color = color });
        }

        /// <summary>
        /// 生成特殊标记
        /// </summary>
        private void GenerateSpecialMarks()
        {
            var axisColor = GetAxisColor();
            var midPoint = (Position + GetCalculatedEndPosition()) * 0.5;
            
            switch (AxisType)
            {
                case OpticalAxisType.MainAxis:
                    // 主光轴：在中点添加小
                    GenerateCircleMark(midPoint, 3 * Scale, axisColor);
                    break;
                    
                case OpticalAxisType.SecondaryAxis:
                    // 次光轴：在中点添加小正方
                    GenerateSquareMark(midPoint, 4 * Scale, axisColor);
                    break;
                    
                case OpticalAxisType.MechanicalAxis:
                    // 机械轴：添加双线标记
                    GenerateDoubleLineMark(midPoint, axisColor);
                    break;
                    
                case OpticalAxisType.ReferenceAxis:
                    // 参考轴：添加三角形标记
                    GenerateTriangleMark(midPoint, 5 * Scale, axisColor);
                    break;
                    
                case OpticalAxisType.DispersionAxis:
                    // 色散轴：添加波浪线
                    GenerateWaveMark(midPoint, axisColor);
                    break;
            }
        }

        /// <summary>
        /// 生成圆形标记
        /// </summary>
        private void GenerateCircleMark(Vector2 center, double radius, lcdb.Colors.Color color)
        {
            var circle = new Circle
            {
                center = center,
                radius = radius,
                color = color
            };
            _markEntities.Add(circle);
        }

        /// <summary>
        /// 生成正方形标准?        /// </summary>
        private void GenerateSquareMark(Vector2 center, double size, lcdb.Colors.Color color)
        {
            double halfSize = size * 0.5;
            
            var p1 = new Vector2(center.X - halfSize, center.Y - halfSize);
            var p2 = new Vector2(center.X + halfSize, center.Y - halfSize);
            var p3 = new Vector2(center.X + halfSize, center.Y + halfSize);
            var p4 = new Vector2(center.X - halfSize, center.Y + halfSize);

            _markEntities.Add(new Line(p1, p2) { color = color });
            _markEntities.Add(new Line(p2, p3) { color = color });
            _markEntities.Add(new Line(p3, p4) { color = color });
            _markEntities.Add(new Line(p4, p1) { color = color });
        }

        /// <summary>
        /// 生成双线标记
        /// </summary>
        private void GenerateDoubleLineMark(Vector2 center, lcdb.Colors.Color color)
        {
            var direction = (GetCalculatedEndPosition() - Position);
            direction.Normalize();
            var perpendicular = new Vector2(-direction.Y, direction.X);
            double offset = 2 * Scale;
            
            var line1Start = center + perpendicular * offset - direction * 5 * Scale;
            var line1End = center + perpendicular * offset + direction * 5 * Scale;
            var line2Start = center - perpendicular * offset - direction * 5 * Scale;
            var line2End = center - perpendicular * offset + direction * 5 * Scale;
            
            _markEntities.Add(new Line(line1Start, line1End) { color = color });
            _markEntities.Add(new Line(line2Start, line2End) { color = color });
        }

        /// <summary>
        /// 生成三角形标准?        /// </summary>
        private void GenerateTriangleMark(Vector2 center, double size, lcdb.Colors.Color color)
        {
            var direction = (GetCalculatedEndPosition() - Position);
            direction.Normalize();
            var perpendicular = new Vector2(-direction.Y, direction.X);
            
            var p1 = center + direction * size;
            var p2 = center - direction * size * 0.5 + perpendicular * size * 0.8;
            var p3 = center - direction * size * 0.5 - perpendicular * size * 0.8;

            _markEntities.Add(new Line(p1, p2) { color = color });
            _markEntities.Add(new Line(p2, p3) { color = color });
            _markEntities.Add(new Line(p3, p1) { color = color });
        }

        /// <summary>
        /// 生成波浪标记
        /// </summary>
        private void GenerateWaveMark(Vector2 center, lcdb.Colors.Color color)
        {
            var direction = (GetCalculatedEndPosition() - Position);
            direction.Normalize();
            var perpendicular = new Vector2(-direction.Y, direction.X);
            
            double waveLength = 20 * Scale;
            double waveAmplitude = 3 * Scale;
            int segments = 20;
            
            var wavePoints = new List<Vector2>();
            
            for (int i = 0; i <= segments; i++)
            {
                double t = (double)i / segments;
                double x = (t - 0.5) * waveLength;
                double y = waveAmplitude * Math.Sin(t * 4 * Math.PI);
                
                var point = center + direction * x + perpendicular * y;
                wavePoints.Add(point);
            }
            
            for (int i = 0; i < wavePoints.Count - 1; i++)
            {
                _markEntities.Add(new Line(wavePoints[i], wavePoints[i + 1]) { color = color });
            }
        }

        /// <summary>
        /// 生成文本
        /// </summary>
        private void GenerateText()
        {
            var textPosition = GetTextPosition();
            var axisColor = GetAxisColor();

            // 主标记文本
            var mainText = new Text();
            mainText.Value = MarkText;
            mainText.Position = new Vector3(textPosition.X, textPosition.Y, 0.0);
            mainText.Height = 5 * Scale;
            mainText.color = axisColor;
            mainText.alignment = lcdb.TextAlignment.CenterMiddle;
            _markEntities.Add(mainText);

            // 附加说明文本
            if (!string.IsNullOrEmpty(AxisLabel))
            {
                var labelText = new Text();
                labelText.Value = AxisLabel;
                labelText.Position = new Vector3(textPosition.X, textPosition.Y - 15 * Scale, 0.0);
                labelText.Height = 4 * Scale;
                labelText.color = axisColor;
                labelText.alignment = lcdb.TextAlignment.CenterMiddle;
                _markEntities.Add(labelText);
            }

            // 波长信息（用于色散轴
            if (AxisType == OpticalAxisType.DispersionAxis && !string.IsNullOrEmpty(Wavelength))
            {
                var wavelengthText = new Text();
                wavelengthText.Value = $"λ = {Wavelength}nm";
                wavelengthText.Position = new Vector3(textPosition.X, textPosition.Y - 30 * Scale, 0.0);
                wavelengthText.Height = 3 * Scale;
                wavelengthText.color = axisColor;
                wavelengthText.alignment = lcdb.TextAlignment.CenterMiddle;
                _markEntities.Add(wavelengthText);
            }
        }

        /// <summary>
        /// 获取文本位置
        /// </summary>
        private Vector2 GetTextPosition()
        {
            var midPoint = (Position + GetCalculatedEndPosition()) * 0.5;
            return midPoint + TextOffset;
        }

        /// <summary>
        /// 获取光轴颜色
        /// </summary>
        private lcdb.Colors.Color GetAxisColor()
        {
            switch (AxisType)
            {
                case OpticalAxisType.MainAxis:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Orange);
                case OpticalAxisType.SecondaryAxis:
                    // WCAG AA: Yellow (255,255,0) 白底 1.07:1 → 不可见; 改 #806000 (5.9:1) 深橄榄金
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.FromArgb(128, 96, 0));
                case OpticalAxisType.MechanicalAxis:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Gray);
                case OpticalAxisType.ReferenceAxis:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Green);
                case OpticalAxisType.DispersionAxis:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Purple);
                default:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Orange);
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
            var startPos = new PointF((float)Position.X, (float)Position.Y);
            var endPos = new PointF((float)GetCalculatedEndPosition().X, (float)GetCalculatedEndPosition().Y);
            OpticalMarkSymbols.DrawOpticalAxisMark(g, startPos, endPos, scale * (float)Scale);
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
            if (AxisLength <= 0)
                return false;

            if (Position == GetCalculatedEndPosition())
                return false;

            if (ArrowSize < 0 || ArrowSize > 50)
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
                { "EndPosition", EndPosition },
                { "AxisLength", AxisLength },
                { "Direction", Direction },
                { "AxisType", AxisType },
                { "AxisStyle", AxisStyle },
                { "ShowArrow", ShowArrow },
                { "ArrowSize", ArrowSize },
                { "PropagationDir", PropagationDir },
                { "Wavelength", Wavelength },
                { "AxisLabel", AxisLabel }
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
            if (properties.ContainsKey("EndPosition"))
                EndPosition = (Vector2)properties["EndPosition"];
            if (properties.ContainsKey("AxisLength"))
                AxisLength = (double)properties["AxisLength"];
            if (properties.ContainsKey("Direction"))
                Direction = (double)properties["Direction"];
            if (properties.ContainsKey("AxisType"))
                AxisType = (OpticalAxisType)properties["AxisType"];
            if (properties.ContainsKey("AxisStyle"))
                AxisStyle = (OpticalAxisStyle)properties["AxisStyle"];
            if (properties.ContainsKey("ShowArrow"))
                ShowArrow = (bool)properties["ShowArrow"];
            if (properties.ContainsKey("ArrowSize"))
                ArrowSize = (double)properties["ArrowSize"];
            if (properties.ContainsKey("PropagationDir"))
                PropagationDir = (PropagationDirection)properties["PropagationDir"];
            if (properties.ContainsKey("Wavelength"))
                Wavelength = (string)properties["Wavelength"];
            if (properties.ContainsKey("AxisLabel"))
                AxisLabel = (string)properties["AxisLabel"];

            UpdateMarkText();
        }

        /// <summary>
        /// 克隆标记
        /// </summary>
        IOpticalMark IOpticalMark.Clone()
        {
            return Clone() as OpticalAxisMark;
        }

        /// <summary>
        /// 获取标记描述
        /// </summary>
        public string GetDescription()
        {
            return $"光轴标记 - {AxisLabel} ({MarkText}) - 长度: {AxisLength:F1}mm";
        }

        #endregion

        #region Entity 重写方法

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new OpticalAxisMark();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            OpticalAxisMark mark = base.Clone() as OpticalAxisMark;
            mark.Position = Position;
            mark.Scale = Scale;
            mark.MarkText = MarkText;
            mark.ShowText = ShowText;
            mark.TextOffset = TextOffset;
            mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.EndPosition = EndPosition;
            mark.AxisLength = AxisLength;
            mark.Direction = Direction;
            mark.AxisType = AxisType;
            mark.AxisStyle = AxisStyle;
            mark.ShowArrow = ShowArrow;
            mark.ArrowSize = ArrowSize;
            mark.PropagationDir = PropagationDir;
            mark.Wavelength = Wavelength;
            mark.AxisLabel = AxisLabel;
            mark._markEntities = new List<Entity>();

            return mark;
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            Position += translation;
            EndPosition += translation;
            _markEntities.Clear();
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            Position = Vector2.RotateInRadian(Position, center, angle);
            EndPosition = Vector2.RotateInRadian(EndPosition, center, angle);
            Direction += angle * 180 / Math.PI;
            Rotation += angle * 180 / Math.PI;
            _markEntities.Clear();
        }

        /// <summary>
        /// Transform
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            Position = transform * Position;
            EndPosition = transform * EndPosition;
            Vector2 scaleVector = new Vector2(Scale, 0);
            Scale = (transform * scaleVector).length;
            UpdateAxisGeometry();
            _markEntities.Clear();
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            
            // 起点
            gripPoints.Add(new GripPoint(GripPointType.End, Position));
            
            // 终点
            gripPoints.Add(new GripPoint(GripPointType.End, GetCalculatedEndPosition()));
            
            // 中点
            var midPoint = (Position + GetCalculatedEndPosition()) * 0.5;
            gripPoints.Add(new GripPoint(GripPointType.Mid, midPoint));

            if (ShowText)
            {
                var textPos = GetTextPosition();
                gripPoints.Add(new GripPoint(GripPointType.Center, textPos));
            }

            return gripPoints;
        }

        /// <summary>
        /// 对象捕捉点?        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPnts = new List<ObjectSnapPoint>();
            
            // 端点
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.End, Position));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.End, GetCalculatedEndPosition()));
            
            // 中点
            var midPoint = (Position + GetCalculatedEndPosition()) * 0.5;
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, midPoint));
            
            return snapPnts;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            switch (index)
            {
                case 0: // 起点
                    Position = newPosition;
                    UpdateAxisGeometry();
                    break;
                case 1: // 终点
                    EndPosition = newPosition;
                    UpdateAxisGeometry();
                    break;
                case 2: // 中点 - 整体移动
                    var midPoint = (Position + GetCalculatedEndPosition()) * 0.5;
                    var offset = newPosition - midPoint;
                    Position += offset;
                    EndPosition += offset;
                    break;
                case 3: // 文本位置点
                    if (ShowText)
                    {
                        var currentMidPoint = (Position + GetCalculatedEndPosition()) * 0.5;
                        TextOffset = newPosition - currentMidPoint;
                    }
                    break;
            }
            _markEntities.Clear();
        }

        #endregion
    }

    /// <summary>
    /// 光轴类型枚举
    /// </summary>
    public enum OpticalAxisType
    {
        /// <summary>
        /// 主光?        /// </summary>
        MainAxis = 0,
        
        /// <summary>
        /// 次光?        /// </summary>
        SecondaryAxis = 1,
        
        /// <summary>
        /// 机械?        /// </summary>
        MechanicalAxis = 2,
        
        /// <summary>
        /// 参考轴
        /// </summary>
        ReferenceAxis = 3,
        
        /// <summary>
        /// 色散?        /// </summary>
        DispersionAxis = 4
    }

    /// <summary>
    /// 光轴样式枚举
    /// </summary>
    public enum OpticalAxisStyle
    {
        /// <summary>
        /// 实线
        /// </summary>
        SolidLine = 0,
        
        /// <summary>
        /// 虚线
        /// </summary>
        DashLine = 1,
        
        /// <summary>
        /// 点划?        /// </summary>
        DashDotLine = 2,
        
        /// <summary>
        /// 点线
        /// </summary>
        DotLine = 3
    }

    /// <summary>
    /// 传播方向枚举
    /// </summary>
    public enum PropagationDirection
    {
        /// <summary>
        /// 正向
        /// </summary>
        Forward = 0,
        
        /// <summary>
        /// 反向
        /// </summary>
        Backward = 1,
        
        /// <summary>
        /// 双向
        /// </summary>
        Bidirectional = 2
    }
}