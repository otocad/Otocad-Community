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
    /// ISO 10110-12 非球面表面标记 —— 面形公差按 ISO 10110-5 用代号 `3/A`(A=sag 偏差)。
    /// 注: 非球面无专用 slash 代号(ISO 10110-10 代号表), 其面形公差走 `3/`(ISO 10110-5:2007 §7;
    /// Part 12 NOTE 2 确认)。斜率/类型属非球面方程描述, 不进 `3/` 公差码, 已从本标记移除。
    /// 方程系数(ConicConstant/AsphericCoefficients/ClearAperture)保留作曲面描述。
    /// </summary>
    public class ISO10110_12Mark : Entity, IOpticalMark
    {
        public override string className => "ISO10110_12Mark";

        #region IOpticalMark 接口属性
        public OpticalMarkType MarkType => OpticalMarkType.Aspheric;
        public Vector2 Position { get; set; } = new Vector2(0, 0);
        public double Scale { get; set; } = 1.0;
        public string MarkText { get; set; } = "3/0.50";
        public bool ShowText { get; set; } = true;
        public Vector2 TextOffset { get; set; } = new Vector2(0, -30);
        public double Rotation { get; set; } = 0.0;
        public bool IsVisible { get; set; } = true;
        #endregion

        #region ISO 10110-12 特定属性
        /// <summary>
        /// 面形(sag)偏差公差 (μm) —— ISO 10110-5 `3/A` 中的 A.
        /// </summary>
        public double FormDeviation { get; set; } = 0.5;

        /// <summary>
        /// 有效口径 (mm)
        /// </summary>
        public double ClearAperture { get; set; } = 50.0;

        /// <summary>
        /// 非球面系数K (圆锥常数)
        /// </summary>
        public double ConicConstant { get; set; } = -1.0;

        /// <summary>
        /// 高阶非球面系数 A4, A6, A8, A10
        /// </summary>
        public double[] AsphericCoefficients { get; set; } = new double[4];

        /// <summary>
        /// 测量方法
        /// </summary>
        public AsphericMeasurementMethod MeasurementMethod { get; set; } = AsphericMeasurementMethod.Interferometric;

        /// <summary>
        /// 标记框架大小
        /// </summary>
        public Vector2 FrameSize { get; set; } = new Vector2(65, 30);

        /// <summary>
        /// 检测标准
        /// </summary>
        public string Standard { get; set; } = "ISO 10110-12";
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
        public ISO10110_12Mark()
        {
            UpdateMarkText();
        }

        public ISO10110_12Mark(Vector2 position, double formDeviation)
        {
            Position = position;
            FormDeviation = formDeviation;
            UpdateMarkText();
        }
        #endregion

        #region 核心方法
        private void UpdateMarkText()
        {
            // ISO 10110-5 面形公差代号 "3/A"(A=sag 偏差;非球面无专用 12/ 代号, 斜率/类型不入此码)
            MarkText = FormDeviation > 0 ? $"3/{FormDeviation:F2}" : "3/—";
        }

        protected void Generate()
        {
            _markEntities.Clear();
            GenerateMainSymbol();
            if (ShowText) GenerateText();
        }

        /// <summary>
        /// ISO 10110-12 非球面面形公差: 矩形框 + 居中代码 "3/A" (A=sag 偏差 μm, ISO 10110-5).
        /// 非球面无专用 slash 代号, 面形公差走 `3/`(非旧实现的 `12/`); 斜率/类型属曲面方程描述, 不入公差码.
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

            // 居中显示用户可编辑的 MarkText (ISO 代码 "3/A", A=sag 偏差 μm)
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
        /// ISO 10110-12: 框内代码即完整规范, 外部仅注单位 (μm).
        /// </summary>
        private void GenerateText()
        {
            var textPosition = Position + TextOffset;
            _markEntities.Add(new Text
            {
                Value = "μm",
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
            using (var pen = new Pen(System.Drawing.Color.Purple, 1.5f * scale))
            using (var brush = new SolidBrush(System.Drawing.Color.Purple))
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
            if (FormDeviation < 0 || FormDeviation > 100) return false;
            if (ClearAperture <= 0) return false;
            return true;
        }

        public Dictionary<string, object> GetProperties()
        {
            return new Dictionary<string, object>
            {
                { "Position", Position }, { "Scale", Scale }, { "MarkText", MarkText },
                { "ShowText", ShowText }, { "TextOffset", TextOffset }, { "Rotation", Rotation },
                { "IsVisible", IsVisible },
                { "FormDeviation", FormDeviation },
                { "ClearAperture", ClearAperture }, { "ConicConstant", ConicConstant },
                { "MeasurementMethod", MeasurementMethod }, { "Standard", Standard }
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
            if (properties.ContainsKey("FormDeviation")) FormDeviation = (double)properties["FormDeviation"];
            if (properties.ContainsKey("ClearAperture")) ClearAperture = (double)properties["ClearAperture"];
            if (properties.ContainsKey("ConicConstant")) ConicConstant = (double)properties["ConicConstant"];
            if (properties.ContainsKey("MeasurementMethod")) MeasurementMethod = (AsphericMeasurementMethod)properties["MeasurementMethod"];
            UpdateMarkText();
            _markEntities.Clear();   // 属性改了须清缓存, 否则 Draw 仍渲旧帧 (对齐 ISO10110_3Mark)
        }

        IOpticalMark IOpticalMark.Clone() => Clone() as ISO10110_12Mark;

        public string GetDescription()
        {
            return $"ISO 10110-12 非球面标记 - 面形(sag)偏差: {FormDeviation}μm (3/)";
        }
        #endregion

        #region Entity 重写方法
        protected override DBObject CreateInstance() => new ISO10110_12Mark();

        public override object Clone()
        {
            ISO10110_12Mark mark = base.Clone() as ISO10110_12Mark;
            mark.Position = Position;
            mark.Scale = Scale;
            mark.MarkText = MarkText;
            mark.ShowText = ShowText;
            mark.TextOffset = TextOffset;
            mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.FormDeviation = FormDeviation;
            mark.ClearAperture = ClearAperture;
            mark.ConicConstant = ConicConstant;
            mark.AsphericCoefficients = (double[])AsphericCoefficients.Clone();
            mark.MeasurementMethod = MeasurementMethod;
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
    /// 非球面测量方法枚举
    /// </summary>
    public enum AsphericMeasurementMethod
    {
        /// <summary>干涉测量</summary>
        Interferometric = 0,
        /// <summary>轮廓仪测量</summary>
        Profilometry = 1,
        /// <summary>CGH补偿测量</summary>
        CGHCompensated = 2,
        /// <summary>拼接干涉测量</summary>
        StitchingInterferometry = 3
    }
}
