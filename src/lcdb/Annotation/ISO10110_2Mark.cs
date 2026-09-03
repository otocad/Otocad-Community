using lcdb.Colors;
using System;
using System.Collections.Generic;
using System.Drawing;
using LitMath;
using lcdb;
using lcdb.Interfaces;
using OtoCAD;

namespace lcdb.Annotation
{
    /// <summary>
    /// 【非独立标记】应力双折射(代号 `0/`)是材料(玻璃体)缺陷, 属"对材料的要求",
    /// 应列入 <see cref="TechnicalRequirementTable"/> 的「材料技术要求」列(图纸下方表),
    /// 不作贴面标记;已移出 Ribbon/命令, 本类仅保留供旧 .otocad 反序列化。
    ///
    /// ISO 10110-2 应力双折射(代码 0/)
    /// 用于标注光学材料的应力双折射要求
    /// </summary>
    public class ISO10110_2Mark : Entity, IOpticalMark
    {
        public override string className => "ISO10110_2Mark";

        #region IOpticalMark 接口属性
        public OpticalMarkType MarkType => OpticalMarkType.StressBirefringence;
        public Vector2 Position { get; set; } = new Vector2(0, 0);
        public double Scale { get; set; } = 1.0;
        public string MarkText { get; set; } = "0/10";  // ISO 10110-2 应力双折射使用代码"0/"
        public bool ShowText { get; set; } = true;
        public Vector2 TextOffset { get; set; } = new Vector2(0, -30);
        public double Rotation { get; set; } = 0.0;
        public bool IsVisible { get; set; } = true;
        #endregion

        #region ISO 10110-2 特定属性
        /// <summary>
        /// 应力双折射等级 (nm/cm)
        /// </summary>
        public double BirefringenceValue { get; set; } = 10.0;

        /// <summary>
        /// 测量波长 (nm)
        /// </summary>
        public double MeasurementWavelength { get; set; } = 632.8;

        /// <summary>
        /// 光程差单位 (nm/cm)
        /// </summary>
        public BirefringenceUnit Unit { get; set; } = BirefringenceUnit.NmPerCm;

        /// <summary>
        /// 测量方向
        /// </summary>
        public string MeasurementDirection { get; set; } = "沿光轴";

        /// <summary>
        /// 允许的最大局部值
        /// </summary>
        public double MaxLocalValue { get; set; } = 15.0;

        /// <summary>
        /// 测试温度 (°C)
        /// </summary>
        public double TestTemperature { get; set; } = 20.0;

        /// <summary>
        /// 标记框架大小
        /// </summary>
        public Vector2 FrameSize { get; set; } = new Vector2(55, 25);

        /// <summary>
        /// 检测标准
        /// </summary>
        public string Standard { get; set; } = "ISO 10110-2";
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
        public ISO10110_2Mark()
        {
            UpdateMarkText();
        }

        public ISO10110_2Mark(Vector2 position, double birefringenceValue)
        {
            Position = position;
            BirefringenceValue = birefringenceValue;
            UpdateMarkText();
        }
        #endregion

        #region 核心方法
        private void UpdateMarkText()
        {
            // ISO 10110-2 应力双折射使用代码"0/"
            MarkText = $"0/{BirefringenceValue:F0}";
        }

        protected void Generate()
        {
            _markEntities.Clear();
            GenerateMainSymbol();
            if (ShowText) GenerateText();
        }

        /// <summary>
        /// ISO 10110-2 / GB/T 11297.2 应力双折射: 矩形框 + 居中代码 "0/A" (A 单位 nm/cm).
        /// 旧实现 (双线框 + ⊥ 箭头 + 分开 0/数值) 不符合 ISO 10110-2 标准规范, 已移除.
        /// </summary>
        private void GenerateMainSymbol()
        {
            double halfWidth = FrameSize.X * 0.5 * Scale;
            double halfHeight = FrameSize.Y * 0.5 * Scale;

            // 单矩形框
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

            // 居中显示用户可编辑的 MarkText (ISO 代码 "0/A")
            _markEntities.Add(new Text
            {
                Value = MarkText,
                Position = new Vector3(Position.X, Position.Y, 0.0),
                Height = halfHeight * 0.6,  // 框高 60%, 接近 ISO 推荐 0.5h 字号
                color = color,
                alignment = lcdb.TextAlignment.CenterMiddle,
            });
        }

        /// <summary>
        /// ISO 10110-2: 框内代码 "0/A" 即完整规范, 外部仅注 nm/cm 单位 (避免值含混).
        /// </summary>
        private void GenerateText()
        {
            var textPosition = Position + TextOffset;
            string unitStr = Unit == BirefringenceUnit.NmPerCm ? "nm/cm" : "nm";
            _markEntities.Add(new Text
            {
                Value = unitStr,
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
            using (var pen = new Pen(System.Drawing.Color.Magenta, 1.5f * scale))
            using (var brush = new SolidBrush(System.Drawing.Color.Magenta))
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
            if (BirefringenceValue < 0 || BirefringenceValue > 1000) return false;
            if (MeasurementWavelength <= 0) return false;
            return true;
        }

        public Dictionary<string, object> GetProperties()
        {
            return new Dictionary<string, object>
            {
                { "Position", Position }, { "Scale", Scale }, { "MarkText", MarkText },
                { "ShowText", ShowText }, { "TextOffset", TextOffset }, { "Rotation", Rotation },
                { "IsVisible", IsVisible }, { "BirefringenceValue", BirefringenceValue },
                { "MeasurementWavelength", MeasurementWavelength }, { "Unit", Unit },
                { "MeasurementDirection", MeasurementDirection }, { "MaxLocalValue", MaxLocalValue },
                { "TestTemperature", TestTemperature }, { "Standard", Standard }
            };
        }

        public void SetProperties(Dictionary<string, object> properties)
        {
            if (properties.ContainsKey("Position")) Position = (Vector2)properties["Position"];
            if (properties.ContainsKey("Scale")) Scale = (double)properties["Scale"];
            if (properties.ContainsKey("ShowText")) ShowText = (bool)properties["ShowText"];
            if (properties.ContainsKey("TextOffset")) TextOffset = (Vector2)properties["TextOffset"];
            if (properties.ContainsKey("Rotation")) Rotation = (double)properties["Rotation"];
            if (properties.ContainsKey("IsVisible")) IsVisible = (bool)properties["IsVisible"];
            if (properties.ContainsKey("BirefringenceValue")) BirefringenceValue = (double)properties["BirefringenceValue"];
            if (properties.ContainsKey("MeasurementWavelength")) MeasurementWavelength = (double)properties["MeasurementWavelength"];
            if (properties.ContainsKey("Unit")) Unit = (BirefringenceUnit)properties["Unit"];
            if (properties.ContainsKey("MeasurementDirection")) MeasurementDirection = (string)properties["MeasurementDirection"];
            if (properties.ContainsKey("MaxLocalValue")) MaxLocalValue = (double)properties["MaxLocalValue"];
            if (properties.ContainsKey("TestTemperature")) TestTemperature = (double)properties["TestTemperature"];
            UpdateMarkText();
        }

        IOpticalMark IOpticalMark.Clone() => Clone() as ISO10110_2Mark;

        public string GetDescription()
        {
            string unitStr = Unit == BirefringenceUnit.NmPerCm ? "nm/cm" : "nm";
            return $"ISO 10110-2 应力双折射标记 - {BirefringenceValue:F0}{unitStr}";
        }
        #endregion

        #region Entity 重写方法
        protected override DBObject CreateInstance() => new ISO10110_2Mark();

        public override object Clone()
        {
            ISO10110_2Mark mark = base.Clone() as ISO10110_2Mark;
            mark.Position = Position;
            mark.Scale = Scale;
            mark.MarkText = MarkText;
            mark.ShowText = ShowText;
            mark.TextOffset = TextOffset;
            mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.BirefringenceValue = BirefringenceValue;
            mark.MeasurementWavelength = MeasurementWavelength;
            mark.Unit = Unit;
            mark.MeasurementDirection = MeasurementDirection;
            mark.MaxLocalValue = MaxLocalValue;
            mark.TestTemperature = TestTemperature;
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

    /// <summary>
    /// 双折射单位枚举
    /// </summary>
    public enum BirefringenceUnit
    {
        /// <summary>nm/cm (标准单位)</summary>
        NmPerCm = 0,
        /// <summary>nm (总光程差)</summary>
        Nm = 1
    }
}
