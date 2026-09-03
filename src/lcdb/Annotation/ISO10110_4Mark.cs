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
    /// 【非独立标记】不均匀性/条纹(代号 `2/`)是材料(玻璃体)缺陷, 属"对材料的要求",
    /// 应列入 <see cref="TechnicalRequirementTable"/> 的「材料技术要求」列(图纸下方表),
    /// 不作贴面标记;已移出 Ribbon/命令, 本类仅保留供旧 .otocad 反序列化。
    ///
    /// ISO 10110-4 不均匀性和条纹(代码 2/)
    /// 用于标注光学材料的折射率不均匀性和条纹要求
    /// </summary>
    public class ISO10110_4Mark : Entity, IOpticalMark
    {
        public override string className => "ISO10110_4Mark";

        #region IOpticalMark 接口属性
        public OpticalMarkType MarkType => OpticalMarkType.Inhomogeneity;
        public Vector2 Position { get; set; } = new Vector2(0, 0);
        public double Scale { get; set; } = 1.0;
        public string MarkText { get; set; } = "2/2;A";  // ISO 10110-4 不均匀性/条纹的指示代号是 "2/"(非部件号 4)
        public bool ShowText { get; set; } = true;
        public Vector2 TextOffset { get; set; } = new Vector2(0, -30);
        public double Rotation { get; set; } = 0.0;
        public bool IsVisible { get; set; } = true;
        #endregion

        #region ISO 10110-4 特定属性
        /// <summary>
        /// 不均匀性等级 (0-5)
        /// </summary>
        public int InhomogeneityClass { get; set; } = 2;

        /// <summary>
        /// 条纹等级 (A, B, C 或数字)
        /// </summary>
        public string StriaeClass { get; set; } = "A";

        /// <summary>
        /// 折射率变化量 (×10^-6)
        /// </summary>
        public double RefractiveIndexVariation { get; set; } = 2.0;

        /// <summary>
        /// 测量孔径 (mm)
        /// </summary>
        public double MeasurementAperture { get; set; } = 50.0;

        /// <summary>
        /// 条纹观察方向
        /// </summary>
        public string StriaeDirection { get; set; } = "沿光轴";

        /// <summary>
        /// 测量波长 (nm)
        /// </summary>
        public double MeasurementWavelength { get; set; } = 632.8;

        /// <summary>
        /// 标记框架大小
        /// </summary>
        public Vector2 FrameSize { get; set; } = new Vector2(55, 25);

        /// <summary>
        /// 检测标准
        /// </summary>
        public string Standard { get; set; } = "ISO 10110-4";
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
        public ISO10110_4Mark()
        {
            UpdateMarkText();
        }

        public ISO10110_4Mark(Vector2 position, int inhomogeneityClass, string striaeClass)
        {
            Position = position;
            InhomogeneityClass = inhomogeneityClass;
            StriaeClass = striaeClass;
            UpdateMarkText();
        }
        #endregion

        #region 核心方法
        private void UpdateMarkText()
        {
            // ISO 10110-4 不均匀性和条纹的指示代号是 "2/"(ISO 10110-10 代号表;非部件号 4)
            MarkText = $"2/{InhomogeneityClass};{StriaeClass}";
        }

        protected void Generate()
        {
            _markEntities.Clear();
            GenerateMainSymbol();
            if (ShowText) GenerateText();
        }

        /// <summary>
        /// ISO 10110-4 / GB/T 11297.4 不均匀性/条纹: 矩形框 + 居中代码 "2/A;B" (A=均匀性等级, B=条纹等级).
        /// 旧实现 (双线框 + 装饰波浪 + 上下分行) 不符合 ISO 10110-4 严格格式, 已移除.
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

            // 居中显示用户可编辑的 MarkText (ISO 10110-4 格式 "2/A;B")
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
        /// ISO 10110-4: 框内代码即完整规范, 外部无需补充.
        /// </summary>
        private void GenerateText()
        {
            // 框内代码已表达完整规范, 不输出冗余外部说明
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
            using (var pen = new Pen(System.Drawing.Color.Teal, 1.5f * scale))
            using (var brush = new SolidBrush(System.Drawing.Color.Teal))
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
            if (InhomogeneityClass < 0 || InhomogeneityClass > 5) return false;
            if (string.IsNullOrEmpty(StriaeClass)) return false;
            return true;
        }

        public Dictionary<string, object> GetProperties()
        {
            return new Dictionary<string, object>
            {
                { "Position", Position }, { "Scale", Scale }, { "MarkText", MarkText },
                { "ShowText", ShowText }, { "TextOffset", TextOffset }, { "Rotation", Rotation },
                { "IsVisible", IsVisible }, { "InhomogeneityClass", InhomogeneityClass },
                { "StriaeClass", StriaeClass }, { "RefractiveIndexVariation", RefractiveIndexVariation },
                { "MeasurementAperture", MeasurementAperture }, { "StriaeDirection", StriaeDirection },
                { "MeasurementWavelength", MeasurementWavelength }, { "Standard", Standard }
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
            if (properties.ContainsKey("InhomogeneityClass")) InhomogeneityClass = (int)properties["InhomogeneityClass"];
            if (properties.ContainsKey("StriaeClass")) StriaeClass = (string)properties["StriaeClass"];
            if (properties.ContainsKey("RefractiveIndexVariation")) RefractiveIndexVariation = (double)properties["RefractiveIndexVariation"];
            if (properties.ContainsKey("MeasurementAperture")) MeasurementAperture = (double)properties["MeasurementAperture"];
            if (properties.ContainsKey("StriaeDirection")) StriaeDirection = (string)properties["StriaeDirection"];
            if (properties.ContainsKey("MeasurementWavelength")) MeasurementWavelength = (double)properties["MeasurementWavelength"];
            UpdateMarkText();
            _markEntities.Clear();   // 属性改了须清缓存, 否则 Draw 仍渲旧帧 (对齐 ISO10110_3Mark)
        }

        IOpticalMark IOpticalMark.Clone() => Clone() as ISO10110_4Mark;

        public string GetDescription()
        {
            return $"ISO 10110-4 不均匀性标记 - 等级{InhomogeneityClass}, 条纹{StriaeClass}";
        }
        #endregion

        #region Entity 重写方法
        protected override DBObject CreateInstance() => new ISO10110_4Mark();

        public override object Clone()
        {
            ISO10110_4Mark mark = base.Clone() as ISO10110_4Mark;
            mark.Position = Position;
            mark.Scale = Scale;
            mark.MarkText = MarkText;
            mark.ShowText = ShowText;
            mark.TextOffset = TextOffset;
            mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.InhomogeneityClass = InhomogeneityClass;
            mark.StriaeClass = StriaeClass;
            mark.RefractiveIndexVariation = RefractiveIndexVariation;
            mark.MeasurementAperture = MeasurementAperture;
            mark.StriaeDirection = StriaeDirection;
            mark.MeasurementWavelength = MeasurementWavelength;
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
