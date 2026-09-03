using lcdb.Colors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using LitMath;
using lcdb;
using lcdb.Interfaces;
using OtoCAD;

namespace lcdb.Annotation
{
    /// <summary>
    /// ISO 10110-3 标注模式枚举
    /// </summary>
    public enum ISO10110_3MarkMode
    {
        /// <summary>
        /// 混合模式 - 气泡和夹杂使用相同标准
        /// 格式: 1/N×A
        /// </summary>
        Combined = 0,

        /// <summary>
        /// 分离模式 - 气泡和夹杂分别标注
        /// 气泡格式: 1/N×A
        /// 夹杂格式: 1'/C×D
        /// </summary>
        Separate = 1,

        /// <summary>
        /// 仅气泡模式
        /// 格式: 1/N×A
        /// </summary>
        BubbleOnly = 2,

        /// <summary>
        /// 仅夹杂模式
        /// 格式: 1'/C×D
        /// </summary>
        InclusionOnly = 3
    }

    /// <summary>
    /// 【非独立标记】气泡夹杂(代号 `1/`)是材料(玻璃体)缺陷, 属"对材料的要求",
    /// 应列入 <see cref="TechnicalRequirementTable"/> 的「材料技术要求」列(图纸下方表),
    /// 不作贴面标记;已移出 Ribbon/命令, 本类仅保留供旧 .otocad 反序列化。
    ///
    /// ISO 10110-3 气泡和夹杂物(代码 1/)
    /// 支持混合标注和分离标注两种模式
    /// 混合格式: 1/N×A (代码1/数量×最大直径)
    /// 分离气泡格式: 1/N×A (代码1/数量×最大直径)
    /// 分离夹杂格式: 1'/C×D (代码1'/数量×最大尺寸)
    /// </summary>
    public class ISO10110_3Mark : Entity, IOpticalMark
    {
        public override string className => "ISO10110_3Mark";

        #region IOpticalMark 接口属性
        public OpticalMarkType MarkType => OpticalMarkType.BubbleInclusion;
        public Vector2 Position { get; set; } = new Vector2(0, 0);
        public double Scale { get; set; } = 1.0;
        public string MarkText { get; set; } = "1/1×0.16";  // ISO 10110-3 气泡夹杂使用代码"1/"
        public bool ShowText { get; set; } = true;
        public Vector2 TextOffset { get; set; } = new Vector2(0, -30);
        public double Rotation { get; set; } = 0.0;
        public bool IsVisible { get; set; } = true;
        #endregion

        #region 标注模式
        /// <summary>
        /// 标注模式（混合/分离/仅气泡/仅夹杂）
        /// </summary>
        public ISO10110_3MarkMode MarkMode { get; set; } = ISO10110_3MarkMode.Combined;

        /// <summary>
        /// 是否区分气泡和夹杂物（兼容旧属性）
        /// </summary>
        public bool SeparateBubblesAndInclusions
        {
            get => MarkMode == ISO10110_3MarkMode.Separate;
            set => MarkMode = value ? ISO10110_3MarkMode.Separate : ISO10110_3MarkMode.Combined;
        }
        #endregion

        #region 气泡参数 (代码 1/)
        /// <summary>
        /// 气泡等级 (0-5)
        /// 等级越低要求越严格
        /// </summary>
        public int BubbleClass { get; set; } = 1;

        /// <summary>
        /// 气泡数量限制 N
        /// </summary>
        public int BubbleCount { get; set; } = 1;

        /// <summary>
        /// 最大单个气泡直径 A (mm)
        /// </summary>
        public double MaxBubbleDiameter { get; set; } = 0.16;

        /// <summary>
        /// 允许的气泡总面积 (mm²)
        /// </summary>
        public double TotalBubbleArea { get; set; } = 0.5;

        /// <summary>
        /// 检测体积 (cm³)
        /// </summary>
        public double InspectionVolume { get; set; } = 100.0;
        #endregion

        #region 夹杂参数 (代码 1'/)
        /// <summary>
        /// 夹杂物等级 (0-5)
        /// </summary>
        public int InclusionClass { get; set; } = 1;

        /// <summary>
        /// 夹杂物数量限制 C
        /// </summary>
        public int InclusionCount { get; set; } = 1;

        /// <summary>
        /// 最大夹杂物尺寸 D (mm)
        /// </summary>
        public double MaxInclusionSize { get; set; } = 0.1;

        /// <summary>
        /// 夹杂物总面积限制 (mm²)
        /// </summary>
        public double TotalInclusionArea { get; set; } = 0.3;
        #endregion

        #region 显示属性
        /// <summary>
        /// 标记框架大小
        /// </summary>
        public Vector2 FrameSize { get; set; } = new Vector2(60, 25);

        /// <summary>
        /// 检测标准
        /// </summary>
        public string Standard { get; set; } = "ISO 10110-3";
        #endregion

        private List<Entity> _markEntities = new List<Entity>();

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
        public ISO10110_3Mark()
        {
            UpdateMarkText();
        }

        /// <summary>
        /// 混合模式构造函数
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="bubbleCount">气泡/夹杂数量</param>
        /// <param name="maxDiameter">最大直径</param>
        public ISO10110_3Mark(Vector2 position, int bubbleCount, double maxDiameter)
        {
            Position = position;
            MarkMode = ISO10110_3MarkMode.Combined;
            BubbleCount = bubbleCount;
            MaxBubbleDiameter = maxDiameter;
            UpdateMarkText();
        }

        /// <summary>
        /// 分离模式构造函数
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="bubbleCount">气泡数量 N</param>
        /// <param name="maxBubbleDiameter">最大气泡直径 A (mm)</param>
        /// <param name="inclusionCount">夹杂数量 C</param>
        /// <param name="maxInclusionSize">最大夹杂尺寸 D (mm)</param>
        public ISO10110_3Mark(Vector2 position, int bubbleCount, double maxBubbleDiameter,
            int inclusionCount, double maxInclusionSize)
        {
            Position = position;
            MarkMode = ISO10110_3MarkMode.Separate;
            BubbleCount = bubbleCount;
            MaxBubbleDiameter = maxBubbleDiameter;
            InclusionCount = inclusionCount;
            MaxInclusionSize = maxInclusionSize;
            UpdateMarkText();
        }
        #endregion

        #region 核心方法
        /// <summary>
        /// 更新标记文本（根据当前模式）
        /// </summary>
        private void UpdateMarkText()
        {
            switch (MarkMode)
            {
                case ISO10110_3MarkMode.Combined:
                    UpdateCombinedMarkText();
                    break;
                case ISO10110_3MarkMode.Separate:
                    UpdateSeparateMarkText();
                    break;
                case ISO10110_3MarkMode.BubbleOnly:
                    UpdateBubbleOnlyMarkText();
                    break;
                case ISO10110_3MarkMode.InclusionOnly:
                    UpdateInclusionOnlyMarkText();
                    break;
            }
        }

        /// <summary>
        /// 更新混合模式标记文本
        /// 格式: 1/N×A
        /// </summary>
        private void UpdateCombinedMarkText()
        {
            MarkText = $"1/{BubbleCount}×{MaxBubbleDiameter:F2}";
        }

        /// <summary>
        /// 更新分离模式标记文本
        /// 格式: 1/N×A; 1'/C×D
        /// </summary>
        private void UpdateSeparateMarkText()
        {
            var sb = new StringBuilder();
            // 气泡部分
            sb.AppendFormat("1/{0}×{1:F2}", BubbleCount, MaxBubbleDiameter);
            // 夹杂部分
            sb.AppendFormat("; 1'/{0}×{1:F2}", InclusionCount, MaxInclusionSize);
            MarkText = sb.ToString();
        }

        /// <summary>
        /// 更新仅气泡模式标记文本
        /// 格式: 1/N×A
        /// </summary>
        private void UpdateBubbleOnlyMarkText()
        {
            MarkText = $"1/{BubbleCount}×{MaxBubbleDiameter:F2}";
        }

        /// <summary>
        /// 更新仅夹杂模式标记文本
        /// 格式: 1'/C×D
        /// </summary>
        private void UpdateInclusionOnlyMarkText()
        {
            MarkText = $"1'/{InclusionCount}×{MaxInclusionSize:F2}";
        }

        protected void Generate()
        {
            _markEntities.Clear();
            GenerateMainSymbol();
            if (ShowText) GenerateText();
        }

        /// <summary>
        /// ISO 10110-3 / GB/T 11297.3 气泡夹杂: 矩形框 + 居中代码 "1/N×A" (N=允许个数, A=最大直径 mm).
        /// 旧实现 (双线框 + 装饰气泡圆 + 上下分行) 不符合 ISO 10110-3 严格格式, 已移除.
        /// </summary>
        private void GenerateMainSymbol()
        {
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
                _markEntities.Add(new Line(pts[i], pts[(i + 1) % 4]) { color = color });
            }

            // 居中显示用户可编辑的 MarkText (ISO 10110-3 格式 "1/N×A")
            _markEntities.Add(new Text
            {
                Value = MarkText,
                Position = new Vector3(Position.X, Position.Y, 0.0),
                Height = halfHeight * 0.5,
                color = color,
                alignment = lcdb.TextAlignment.CenterMiddle,
            });
        }

        /// <summary>
        /// ISO 10110-3: 框内代码即完整规范, 外部仅注单位 (mm).
        /// </summary>
        private void GenerateText()
        {
            var textPosition = Position + TextOffset;
            _markEntities.Add(new Text
            {
                Value = "Ø mm",
                Position = new Vector3(textPosition.X, textPosition.Y, 0.0),
                Height = 4 * Scale,
                color = color,
                alignment = lcdb.TextAlignment.CenterMiddle,
            });
        }
        #endregion

        #region 绘制方法
        public override void Draw(IGraphicsDraw gd)
        {
            if (_markEntities.Count == 0) Generate();
            foreach (var entity in _markEntities) entity.Draw(gd);
        }

#if WINDOWS
        public void Draw(Graphics g, float scale)
        {
            var position = new PointF((float)Position.X, (float)Position.Y);
            using (var pen = new Pen(System.Drawing.Color.Brown, 1.5f * scale))
            using (var brush = new SolidBrush(System.Drawing.Color.Brown))
            using (var font = new Font("Arial", 8 * scale, System.Drawing.FontStyle.Bold))
            {
                float width = (float)FrameSize.X * scale;
                float height = (float)FrameSize.Y * scale;
                RectangleF rect = new RectangleF(position.X - width/2, position.Y - height/2, width, height);
                g.DrawRectangle(pen, Rectangle.Round(rect));
                g.DrawString(MarkText, font, brush, position.X, position.Y,
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }
        }
#endif
        #endregion

        #region IOpticalMark 接口实现
        public RectangleF GetBounds()
        {
            var bound = bounding;
            return new RectangleF((float)bound.left, (float)bound.bottom, (float)bound.width, (float)bound.height);
        }

        public bool Validate()
        {
            // 气泡参数验证
            if (BubbleClass < 0 || BubbleClass > 5) return false;
            if (BubbleCount < 0) return false;
            if (MaxBubbleDiameter < 0 || MaxBubbleDiameter > 10) return false;
            if (TotalBubbleArea < 0) return false;

            // 夹杂参数验证（分离模式或仅夹杂模式需要）
            if (MarkMode == ISO10110_3MarkMode.Separate || MarkMode == ISO10110_3MarkMode.InclusionOnly)
            {
                if (InclusionClass < 0 || InclusionClass > 5) return false;
                if (InclusionCount < 0) return false;
                if (MaxInclusionSize < 0 || MaxInclusionSize > 10) return false;
                if (TotalInclusionArea < 0) return false;
            }

            return true;
        }

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
                { "MarkMode", MarkMode },
                // 气泡参数
                { "BubbleClass", BubbleClass },
                { "BubbleCount", BubbleCount },
                { "MaxBubbleDiameter", MaxBubbleDiameter },
                { "TotalBubbleArea", TotalBubbleArea },
                { "InspectionVolume", InspectionVolume },
                // 夹杂参数
                { "InclusionClass", InclusionClass },
                { "InclusionCount", InclusionCount },
                { "MaxInclusionSize", MaxInclusionSize },
                { "TotalInclusionArea", TotalInclusionArea },
                // 通用参数
                { "FrameSize", FrameSize },
                { "Standard", Standard }
            };
        }

        public void SetProperties(Dictionary<string, object> properties)
        {
            if (properties.ContainsKey("Position")) Position = (Vector2)properties["Position"];
            if (properties.ContainsKey("Scale")) Scale = Convert.ToDouble(properties["Scale"]);
            if (properties.ContainsKey("ShowText")) ShowText = (bool)properties["ShowText"];
            if (properties.ContainsKey("TextOffset")) TextOffset = (Vector2)properties["TextOffset"];
            if (properties.ContainsKey("Rotation")) Rotation = Convert.ToDouble(properties["Rotation"]);
            if (properties.ContainsKey("IsVisible")) IsVisible = (bool)properties["IsVisible"];
            if (properties.ContainsKey("MarkMode")) MarkMode = (ISO10110_3MarkMode)properties["MarkMode"];
            // 气泡参数
            if (properties.ContainsKey("BubbleClass")) BubbleClass = Convert.ToInt32(properties["BubbleClass"]);
            if (properties.ContainsKey("BubbleCount")) BubbleCount = Convert.ToInt32(properties["BubbleCount"]);
            if (properties.ContainsKey("MaxBubbleDiameter")) MaxBubbleDiameter = Convert.ToDouble(properties["MaxBubbleDiameter"]);
            if (properties.ContainsKey("TotalBubbleArea")) TotalBubbleArea = Convert.ToDouble(properties["TotalBubbleArea"]);
            if (properties.ContainsKey("InspectionVolume")) InspectionVolume = Convert.ToDouble(properties["InspectionVolume"]);
            // 夹杂参数
            if (properties.ContainsKey("InclusionClass")) InclusionClass = Convert.ToInt32(properties["InclusionClass"]);
            if (properties.ContainsKey("InclusionCount")) InclusionCount = Convert.ToInt32(properties["InclusionCount"]);
            if (properties.ContainsKey("MaxInclusionSize")) MaxInclusionSize = Convert.ToDouble(properties["MaxInclusionSize"]);
            if (properties.ContainsKey("TotalInclusionArea")) TotalInclusionArea = Convert.ToDouble(properties["TotalInclusionArea"]);
            // 通用参数
            if (properties.ContainsKey("FrameSize")) FrameSize = (Vector2)properties["FrameSize"];

            UpdateMarkText();
            _markEntities.Clear();
        }

        IOpticalMark IOpticalMark.Clone() => Clone() as ISO10110_3Mark;

        public string GetDescription()
        {
            switch (MarkMode)
            {
                case ISO10110_3MarkMode.Separate:
                    return $"ISO 10110-3 气泡夹杂标记 - 气泡: {BubbleCount}×Ø{MaxBubbleDiameter}mm; 夹杂: {InclusionCount}×{MaxInclusionSize}mm";
                case ISO10110_3MarkMode.BubbleOnly:
                    return $"ISO 10110-3 气泡标记 - {BubbleCount}×Ø{MaxBubbleDiameter}mm";
                case ISO10110_3MarkMode.InclusionOnly:
                    return $"ISO 10110-3 夹杂标记 - {InclusionCount}×{MaxInclusionSize}mm";
                default:  // Combined
                    return $"ISO 10110-3 气泡夹杂标记 - {BubbleCount}×Ø{MaxBubbleDiameter}mm";
            }
        }
        #endregion

        #region Entity 重写方法
        protected override DBObject CreateInstance() => new ISO10110_3Mark();

        public override object Clone()
        {
            ISO10110_3Mark mark = base.Clone() as ISO10110_3Mark;
            mark.Position = Position;
            mark.Scale = Scale;
            mark.MarkText = MarkText;
            mark.ShowText = ShowText;
            mark.TextOffset = TextOffset;
            mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.MarkMode = MarkMode;
            // 气泡参数
            mark.BubbleClass = BubbleClass;
            mark.BubbleCount = BubbleCount;
            mark.MaxBubbleDiameter = MaxBubbleDiameter;
            mark.TotalBubbleArea = TotalBubbleArea;
            mark.InspectionVolume = InspectionVolume;
            // 夹杂参数
            mark.InclusionClass = InclusionClass;
            mark.InclusionCount = InclusionCount;
            mark.MaxInclusionSize = MaxInclusionSize;
            mark.TotalInclusionArea = TotalInclusionArea;
            // 通用参数
            mark.FrameSize = FrameSize;
            mark.Standard = Standard;
            mark._markEntities = new List<Entity>();
            return mark;
        }

        public override void Translate(Vector2 translation)
        {
            Position += translation;
            _markEntities.Clear();
        }

        public override void Rotate(Vector2 center, double angle)
        {
            Position = Vector2.RotateInRadian(Position, center, angle);
            Rotation += angle * 180 / Math.PI;
            _markEntities.Clear();
        }

        public override void TransformBy(Matrix3 transform)
        {
            Position = transform * Position;
            Vector2 scaleVector = new Vector2(Scale, 0);
            Scale = (transform * scaleVector - transform * new Vector2(0, 0)).length;
            _markEntities.Clear();
        }

        public override List<GripPoint> GetGripPoints()
        {
            var gripPoints = new List<GripPoint>();
            gripPoints.Add(new GripPoint(GripPointType.Center, Position));
            double halfWidth = FrameSize.X * 0.5 * Scale;
            double halfHeight = FrameSize.Y * 0.5 * Scale;
            gripPoints.Add(new GripPoint(GripPointType.Corner, Position + new Vector2(halfWidth, halfHeight)));
            if (ShowText) gripPoints.Add(new GripPoint(GripPointType.Center, Position + TextOffset));
            return gripPoints;
        }

        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            var snapPnts = new List<ObjectSnapPoint>();
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Center, Position));
            double halfWidth = FrameSize.X * 0.5 * Scale;
            double halfHeight = FrameSize.Y * 0.5 * Scale;
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Corner, Position + new Vector2(halfWidth, halfHeight)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Corner, Position + new Vector2(-halfWidth, halfHeight)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Corner, Position + new Vector2(halfWidth, -halfHeight)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Corner, Position + new Vector2(-halfWidth, -halfHeight)));
            return snapPnts;
        }

        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            switch (index)
            {
                case 0: Position = newPosition; break;
                case 1:
                    var delta = newPosition - Position;
                    FrameSize = new Vector2(Math.Abs(delta.X) * 2 / Scale, Math.Abs(delta.Y) * 2 / Scale);
                    break;
                case 2: if (ShowText) TextOffset = newPosition - Position; break;
            }
            _markEntities.Clear();
        }
        #endregion
    }
}
