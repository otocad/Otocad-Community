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
    /// ISO 10110-7 表面缺陷标记实现
    /// 用于标注光学表面的缺陷限制，包括点状缺陷和线状缺性    /// </summary>
    public class SurfaceImperfectionMark : Entity, IOpticalMark, ISurfaceAttachable
    {
        public override string className => "SurfaceImperfectionMark";

        #region IOpticalMark 接口属性?
        /// <summary>
        /// 标记类型
        /// </summary>
        public OpticalMarkType MarkType => OpticalMarkType.ISO10110_7;

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
        public string MarkText { get; set; } = "7/0.1x0.063;0.025";

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

        #region ISO 10110-7 特定属性?
        /// <summary>
        /// 线状缺陷长度限制（mm性        /// </summary>
        public double LinearDefectLength { get; set; } = 0.1;

        /// <summary>
        /// 线状缺陷宽度限制（mm性        /// </summary>
        public double LinearDefectWidth { get; set; } = 0.063;

        /// <summary>
        /// 点状缺陷直径限制（mm性        /// </summary>
        public double PointDefectDiameter { get; set; } = 0.025;

        /// <summary>
        /// 缺陷等级
        /// </summary>
        public SurfaceDefectGrade DefectGrade { get; set; } = SurfaceDefectGrade.Grade3;

        /// <summary>
        /// 允许缺陷数量
        /// </summary>
        public int AllowedDefectCount { get; set; } = 3;

        /// <summary>
        /// 检测孔径（mm性        /// </summary>
        public double InspectionAperture { get; set; } = 50.0;

        /// <summary>
        /// 缺陷类型
        /// </summary>
        public ISO10110DefectType DefectType { get; set; } = ISO10110DefectType.Combined;

        /// <summary>
        /// 标记框架大小
        /// </summary>
        public Vector2 FrameSize { get; set; } = new Vector2(55, 35);

        /// <summary>
        /// 检测标准?        /// </summary>
        public string Standard { get; set; } = "ISO 10110-7";

        #endregion

        private List<Entity> _markEntities = new List<Entity>();

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                double totalWidth = FrameSize.X + (ShowText ? Math.Abs(TextOffset.X) + 120 : 0);
                double totalHeight = FrameSize.Y + (ShowText ? Math.Abs(TextOffset.Y) + 15 : 0);
                return new Bounding(Position, totalWidth, totalHeight);
            }
        }

        #region 构造函数?
        /// <summary>
        /// 默认构造函数?        /// </summary>
        public SurfaceImperfectionMark()
        {
            UpdateMarkText();
        }

        /// <summary>
        /// 带参数构造函数?        /// </summary>
        public SurfaceImperfectionMark(Vector2 position, double linearLength, double linearWidth, double pointDiameter)
        {
            Position = position;
            LinearDefectLength = linearLength;
            LinearDefectWidth = linearWidth;
            PointDefectDiameter = pointDiameter;
            UpdateMarkText();
        }

        #endregion

        #region 核心方法

        /// <summary>
        /// 更新标记文本
        /// </summary>
        private void UpdateMarkText()
        {
            MarkText = $"7/{LinearDefectLength:F3}x{LinearDefectWidth:F3};{PointDefectDiameter:F3}";
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
        /// ISO 10110-7 表面缺陷: 矩形框 + 居中代码 "5/N×A; LN×A".
        /// 旧实现 (六边形框 + 装饰缺陷符号 + 散点数量标记 + 多行参数) 不符合 ISO, 已移除.
        /// 注: ISO 10110-7 符号代码是 "5" (不是 7; 7 是部分号).
        /// 格式: 5/N×A — N 个数, A 长度/直径; 多种缺陷用 "; " 分隔.
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

            // ISO 10110-7 严格代码:
            //   线状缺陷 5/{N}×{长 × 宽}; 点状缺陷 L{N}×{直径}  (合并表达)
            string parts = "";
            if (LinearDefectLength > 0)
            {
                parts += $"5/{AllowedDefectCount}×{LinearDefectLength:G3}";
                if (LinearDefectWidth > 0) parts += $"×{LinearDefectWidth:G3}";
            }
            if (PointDefectDiameter > 0)
            {
                if (parts.Length > 0) parts += "; ";
                parts += $"L{AllowedDefectCount}×{PointDefectDiameter:G3}";
            }
            if (string.IsNullOrEmpty(parts)) parts = "5/—";

            _markEntities.Add(new Text
            {
                Value = parts,
                Position = new Vector3(Position.X, Position.Y, 0.0),
                Height = halfHeight * 0.5,
                color = markColor,
                alignment = lcdb.TextAlignment.CenterMiddle,
            });
        }

        /// <summary>
        /// 生成六边形框架?        /// </summary>
        private void GenerateHexagonFrame(double radius, lcdb.Colors.Color color)
        {
            var points = new List<Vector2>();

            // 计算六边形顶点
            for (int i = 0; i < 6; i++)
            {
                double angle = i * Math.PI / 3; // 60度间隔
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

            // 绘制六边形边
            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                _markEntities.Add(new Line(points[i], points[next]) { color = color });
            }
        }

        /// <summary>
        /// 生成缺陷符号
        /// </summary>
        private void GenerateDefectSymbol(lcdb.Colors.Color color)
        {
            double symbolSize = 8 * Scale;
            
            // 线状缺陷符号 - 短线
            var linearDefect = new Line(
                new Vector2(Position.X - symbolSize, Position.Y + symbolSize * 0.3),
                new Vector2(Position.X + symbolSize, Position.Y + symbolSize * 0.3)
            );
            linearDefect.color = color;
            _markEntities.Add(linearDefect);

            // 点状缺陷符号 - 小圆
            var pointDefect = new Circle
            {
                center = new Vector2(Position.X, Position.Y - symbolSize * 0.3),
                radius = symbolSize * 0.15,
                color = color
            };
            _markEntities.Add(pointDefect);

            // 添加缺陷计数标记
            if (AllowedDefectCount > 0)
            {
                for (int i = 0; i < Math.Min(AllowedDefectCount, 5); i++)
                {
                    double angle = i * Math.PI * 2 / AllowedDefectCount;
                    double x = Position.X + symbolSize * 0.5 * Math.Cos(angle);
                    double y = Position.Y + symbolSize * 0.5 * Math.Sin(angle);
                    
                    var countMark = new Circle
                    {
                        center = new Vector2(x, y),
                        radius = 1 * Scale,
                        color = color
                    };
                    _markEntities.Add(countMark);
                }
            }
        }

        /// <summary>
        /// 生成ISO标识
        /// </summary>
        private void GenerateISOIdentifier(lcdb.Colors.Color color)
        {
            var text = new Text();
            text.Value = "7";
            text.Position = new Vector3(Position.X - FrameSize.X * 0.35, Position.Y + FrameSize.Y * 0.3, 0.0);
            text.Height = 4 * Scale;
            text.color = color;
            text.alignment = lcdb.TextAlignment.CenterMiddle;
            _markEntities.Add(text);
        }

        /// <summary>
        /// 生成缺陷参数文本
        /// </summary>
        private void GenerateDefectParameters(lcdb.Colors.Color color)
        {
            // 线状缺陷参数
            var linearText = new Text();
            linearText.Value = $"L:{LinearDefectLength:F3}×{LinearDefectWidth:F3}";
            linearText.Position = new Vector3(Position.X + FrameSize.X * 0.1, Position.Y + FrameSize.Y * 0.2, 0.0);
            linearText.Height = 4 * Scale;
            linearText.color = color;
            linearText.alignment = lcdb.TextAlignment.CenterMiddle;
            _markEntities.Add(linearText);

            // 点状缺陷参数
            var pointText = new Text();
            pointText.Value = $"P:Φ{PointDefectDiameter:F3}";
            pointText.Position = new Vector3(Position.X + FrameSize.X * 0.1, Position.Y - FrameSize.Y * 0.2, 0.0);
            pointText.Height = 4 * Scale;
            pointText.color = color;
            pointText.alignment = lcdb.TextAlignment.CenterMiddle;
            _markEntities.Add(pointText);

            // 缺陷数量
            if (AllowedDefectCount > 0)
            {
                var countText = new Text();
                countText.Value = $"n≤{AllowedDefectCount}";
                countText.Position = new Vector3(Position.X - FrameSize.X * 0.1, Position.Y - FrameSize.Y * 0.3, 0.0);
                countText.Height = 4 * Scale;
                countText.color = color;
                countText.alignment = lcdb.TextAlignment.CenterMiddle;
                _markEntities.Add(countText);
            }
        }

        /// <summary>
        /// ISO 10110-7: 框内代码即完整规范, 外部仅注单位 mm.
        /// </summary>
        private void GenerateText()
        {
            var textPosition = Position + TextOffset;
            _markEntities.Add(new Text
            {
                Value = "mm",
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
            return $"表面缺陷: 线状{LinearDefectLength:F3}×{LinearDefectWidth:F3}mm, 点状Φ{PointDefectDiameter:F3}mm (≤{AllowedDefectCount}性";
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
            // 根据缺陷等级选择颜色
            switch (DefectGrade)
            {
                case SurfaceDefectGrade.Grade1:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Red);        // 严格 - 红色
                case SurfaceDefectGrade.Grade2:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Orange);     // 中等 - 橙色
                case SurfaceDefectGrade.Grade3:
                    // WCAG AA: Yellow (255,255,0) 白底 1.07:1 → 不可见; 改 #806000 (5.9:1) 深橄榄金
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.FromArgb(128, 96, 0));
                case SurfaceDefectGrade.Grade4:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Green);      // 宽松 - 绿色
                default:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Blue);       // 默认 - 蓝色
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
            DrawISO10110_7Symbol(g, position, scale * (float)Scale);
        }

        /// <summary>
        /// 绘制ISO 10110-7符号
        /// </summary>
        private void DrawISO10110_7Symbol(Graphics g, PointF position, float scale)
        {
            using (var pen = new Pen(GetMarkColor().ToDrawingColor(), 1.5f * scale))
            using (var brush = new SolidBrush(GetMarkColor().ToDrawingColor()))
            using (var font = new Font("Arial", 5 * scale, System.Drawing.FontStyle.Bold))
            {
                float radius = Math.Min((float)FrameSize.X, (float)FrameSize.Y) * 0.4f * scale;

                // 绘制六边形框架
                PointF[] hexPoints = new PointF[6];
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * (float)Math.PI / 3;
                    hexPoints[i] = new PointF(
                        position.X + radius * (float)Math.Cos(angle),
                        position.Y + radius * (float)Math.Sin(angle)
                    );
                }
                g.DrawPolygon(pen, hexPoints);

                // 绘制缺陷符号
                float symbolSize = 8 * scale;
                
                // 线状缺陷
                g.DrawLine(pen, 
                    position.X - symbolSize, position.Y + symbolSize * 0.3f,
                    position.X + symbolSize, position.Y + symbolSize * 0.3f);
                
                // 点状缺陷
                g.DrawEllipse(pen, 
                    position.X - symbolSize * 0.15f, position.Y - symbolSize * 0.3f - symbolSize * 0.15f,
                    symbolSize * 0.3f, symbolSize * 0.3f);

                // 绘制标识和参数
                string identifier = "7";
                string parameters = $"L:{LinearDefectLength:F3}×{LinearDefectWidth:F3}\nP:Φ{PointDefectDiameter:F3}\nn≤{AllowedDefectCount}";
                
                g.DrawString(identifier, font, brush, position.X - radius * 0.7f, position.Y + radius * 0.6f);
                g.DrawString(parameters, font, brush, position.X + radius * 0.2f, position.Y, 
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
            // 验证线状缺陷参数
            if (LinearDefectLength < 0 || LinearDefectLength > 10.0)
                return false;

            if (LinearDefectWidth < 0 || LinearDefectWidth > 1.0)
                return false;

            // 验证点状缺陷参数
            if (PointDefectDiameter < 0 || PointDefectDiameter > 1.0)
                return false;

            // 验证缺陷数量
            if (AllowedDefectCount < 0 || AllowedDefectCount > 100)
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
                { "LinearDefectLength", LinearDefectLength },
                { "LinearDefectWidth", LinearDefectWidth },
                { "PointDefectDiameter", PointDefectDiameter },
                { "DefectGrade", DefectGrade },
                { "AllowedDefectCount", AllowedDefectCount },
                { "InspectionAperture", InspectionAperture },
                { "DefectType", DefectType },
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
            if (properties.ContainsKey("LinearDefectLength"))
                LinearDefectLength = (double)properties["LinearDefectLength"];
            if (properties.ContainsKey("LinearDefectWidth"))
                LinearDefectWidth = (double)properties["LinearDefectWidth"];
            if (properties.ContainsKey("PointDefectDiameter"))
                PointDefectDiameter = (double)properties["PointDefectDiameter"];
            if (properties.ContainsKey("DefectGrade"))
                DefectGrade = (SurfaceDefectGrade)properties["DefectGrade"];
            if (properties.ContainsKey("AllowedDefectCount"))
                AllowedDefectCount = (int)properties["AllowedDefectCount"];
            if (properties.ContainsKey("InspectionAperture"))
                InspectionAperture = (double)properties["InspectionAperture"];
            if (properties.ContainsKey("DefectType"))
                DefectType = (ISO10110DefectType)properties["DefectType"];
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
            return Clone() as SurfaceImperfectionMark;
        }

        /// <summary>
        /// 获取标记描述
        /// </summary>
        public string GetDescription()
        {
            return $"ISO 10110-7 表面缺陷标记 - 线状: {LinearDefectLength:F3}×{LinearDefectWidth:F3}mm, 点状: Φ{PointDefectDiameter:F3}mm (≤{AllowedDefectCount}性";
        }

        #endregion

        #region Entity 重写方法

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new SurfaceImperfectionMark();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            SurfaceImperfectionMark mark = base.Clone() as SurfaceImperfectionMark;
            mark.Position = Position;
            mark.Scale = Scale;
            mark.MarkText = MarkText;
            mark.ShowText = ShowText;
            mark.TextOffset = TextOffset;
            mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.LinearDefectLength = LinearDefectLength;
            mark.LinearDefectWidth = LinearDefectWidth;
            mark.PointDefectDiameter = PointDefectDiameter;
            mark.DefectGrade = DefectGrade;
            mark.AllowedDefectCount = AllowedDefectCount;
            mark.InspectionAperture = InspectionAperture;
            mark.DefectType = DefectType;
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
    /// 表面缺陷等级枚举
    /// </summary>
    public enum SurfaceDefectGrade
    {
        /// <summary>
        /// 等级1 - 最严格
        /// </summary>
        Grade1 = 1,
        
        /// <summary>
        /// 等级2 - 严格
        /// </summary>
        Grade2 = 2,
        
        /// <summary>
        /// 等级3 - 中等
        /// </summary>
        Grade3 = 3,
        
        /// <summary>
        /// 等级4 - 宽松
        /// </summary>
        Grade4 = 4,
        
        /// <summary>
        /// 等级5 - 最宽松
        /// </summary>
        Grade5 = 5
    }

    /// <summary>
    /// ISO 10110 缺陷类型枚举
    /// </summary>
    public enum ISO10110DefectType
    {
        /// <summary>
        /// 线状缺陷
        /// </summary>
        Linear = 0,
        
        /// <summary>
        /// 点状缺陷
        /// </summary>
        Point = 1,
        
        /// <summary>
        /// 组合缺陷
        /// </summary>
        Combined = 2,
        
        /// <summary>
        /// 区域缺陷
        /// </summary>
        Area = 3
    }
}