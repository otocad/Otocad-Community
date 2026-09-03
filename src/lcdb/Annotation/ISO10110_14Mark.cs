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
    /// ISO 10110-14 波前变形公差标记
    /// 用于标注光学系统或元件的波前变形要求
    /// </summary>
    public class ISO10110_14Mark : Entity, IOpticalMark
    {
        public override string className => "ISO10110_14Mark";

        #region IOpticalMark 接口属性
        public OpticalMarkType MarkType => OpticalMarkType.Wavefront;
        public Vector2 Position { get; set; } = new Vector2(0, 0);
        public double Scale { get; set; } = 1.0;
        public string MarkText { get; set; } = "13/0.25λ RMS";
        public bool ShowText { get; set; } = true;
        public Vector2 TextOffset { get; set; } = new Vector2(0, -30);
        public double Rotation { get; set; } = 0.0;
        public bool IsVisible { get; set; } = true;
        #endregion

        #region ISO 10110-14 特定属性
        /// <summary>
        /// 波前误差类型
        /// </summary>
        public WavefrontErrorType ErrorType { get; set; } = WavefrontErrorType.RMS;

        /// <summary>
        /// 波前误差值 (λ为单位)
        /// </summary>
        public double WavefrontError { get; set; } = 0.25;

        /// <summary>
        /// 参考波长 (nm)
        /// </summary>
        public double ReferenceWavelength { get; set; } = 632.8;

        /// <summary>
        /// 测试孔径 (mm)
        /// </summary>
        public double TestAperture { get; set; } = 50.0;

        /// <summary>
        /// 是否包含功率项
        /// </summary>
        public bool IncludePower { get; set; } = false;

        /// <summary>
        /// 是否包含倾斜项
        /// </summary>
        public bool IncludeTilt { get; set; } = false;

        /// <summary>
        /// Zernike项排除列表 (如 "Z1,Z2,Z3")
        /// </summary>
        public string ExcludedZernikeTerms { get; set; } = "";

        /// <summary>
        /// 测试条件 (如温度、气压)
        /// </summary>
        public string TestConditions { get; set; } = "20°C, 101.3kPa";

        /// <summary>
        /// 标记框架大小
        /// </summary>
        public Vector2 FrameSize { get; set; } = new Vector2(70, 30);

        /// <summary>
        /// 检测标准
        /// </summary>
        public string Standard { get; set; } = "ISO 10110-14";
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
        public ISO10110_14Mark()
        {
            UpdateMarkText();
        }

        public ISO10110_14Mark(Vector2 position, double wavefrontError, WavefrontErrorType errorType = WavefrontErrorType.RMS)
        {
            Position = position;
            WavefrontError = wavefrontError;
            ErrorType = errorType;
            UpdateMarkText();
        }
        #endregion

        #region 核心方法
        private void UpdateMarkText()
        {
            string typeStr = ErrorType == WavefrontErrorType.RMS ? "RMS" : "P-V";
            MarkText = $"13/{WavefrontError:F2}λ {typeStr}";
        }

        protected void Generate()
        {
            _markEntities.Clear();
            GenerateMainSymbol();
            if (ShowText) GenerateText();
        }

        /// <summary>
        /// ISO 10110-14 / GB/T 11297.14 波前: 矩形框 + 居中代码 "13/A" (A=RMS λ 波数).
        /// 注: ISO 10110-14 部分号在标准中为 "13" (not 14, 部分号 ≠ 文档序号), 这里保持 ISO 实际编码.
        /// 旧实现 (双线框 + 装饰波浪 + 上下分行 14 / 公差) 不符合标准, 已移除.
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

            // 居中显示用户可编辑的 MarkText (ISO 10110-14 格式 "13/A", A=RMS 波数)
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
        /// ISO 10110-14: 框内代码即完整规范, 外部注 RMS/P-V 类型 + 参考波长.
        /// </summary>
        private void GenerateText()
        {
            var textPosition = Position + TextOffset;
            string typeStr = ErrorType == WavefrontErrorType.RMS ? "RMS" : "P-V";
            _markEntities.Add(new Text
            {
                Value = $"{typeStr} λ={ReferenceWavelength:F1}nm",
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
            using (var pen = new Pen(System.Drawing.Color.Green, 1.5f * scale))
            using (var brush = new SolidBrush(System.Drawing.Color.Green))
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
            if (WavefrontError < 0 || WavefrontError > 10) return false;
            if (ReferenceWavelength <= 0) return false;
            if (TestAperture <= 0) return false;
            return true;
        }

        public Dictionary<string, object> GetProperties()
        {
            return new Dictionary<string, object>
            {
                { "Position", Position }, { "Scale", Scale }, { "MarkText", MarkText },
                { "ShowText", ShowText }, { "TextOffset", TextOffset }, { "Rotation", Rotation },
                { "IsVisible", IsVisible }, { "ErrorType", ErrorType },
                { "WavefrontError", WavefrontError }, { "ReferenceWavelength", ReferenceWavelength },
                { "TestAperture", TestAperture }, { "IncludePower", IncludePower },
                { "IncludeTilt", IncludeTilt }, { "ExcludedZernikeTerms", ExcludedZernikeTerms },
                { "TestConditions", TestConditions }, { "Standard", Standard }
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
            if (properties.ContainsKey("ErrorType")) ErrorType = (WavefrontErrorType)properties["ErrorType"];
            if (properties.ContainsKey("WavefrontError")) WavefrontError = (double)properties["WavefrontError"];
            if (properties.ContainsKey("ReferenceWavelength")) ReferenceWavelength = (double)properties["ReferenceWavelength"];
            if (properties.ContainsKey("TestAperture")) TestAperture = (double)properties["TestAperture"];
            if (properties.ContainsKey("IncludePower")) IncludePower = (bool)properties["IncludePower"];
            if (properties.ContainsKey("IncludeTilt")) IncludeTilt = (bool)properties["IncludeTilt"];
            if (properties.ContainsKey("ExcludedZernikeTerms")) ExcludedZernikeTerms = (string)properties["ExcludedZernikeTerms"];
            if (properties.ContainsKey("TestConditions")) TestConditions = (string)properties["TestConditions"];
            UpdateMarkText();
            _markEntities.Clear();   // 属性改了须清缓存, 否则 Draw 仍渲旧帧 (对齐 ISO10110_3Mark)
        }

        IOpticalMark IOpticalMark.Clone() => Clone() as ISO10110_14Mark;

        public string GetDescription()
        {
            string typeStr = ErrorType == WavefrontErrorType.RMS ? "RMS" : "P-V";
            return $"ISO 10110-14 波前公差标记 - {WavefrontError:F2}λ {typeStr} @{ReferenceWavelength}nm";
        }
        #endregion

        #region Entity 重写方法
        protected override DBObject CreateInstance() => new ISO10110_14Mark();

        public override object Clone()
        {
            ISO10110_14Mark mark = base.Clone() as ISO10110_14Mark;
            mark.Position = Position;
            mark.Scale = Scale;
            mark.MarkText = MarkText;
            mark.ShowText = ShowText;
            mark.TextOffset = TextOffset;
            mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.ErrorType = ErrorType;
            mark.WavefrontError = WavefrontError;
            mark.ReferenceWavelength = ReferenceWavelength;
            mark.TestAperture = TestAperture;
            mark.IncludePower = IncludePower;
            mark.IncludeTilt = IncludeTilt;
            mark.ExcludedZernikeTerms = ExcludedZernikeTerms;
            mark.TestConditions = TestConditions;
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
    /// 波前误差类型枚举
    /// </summary>
    public enum WavefrontErrorType
    {
        /// <summary>RMS波前误差</summary>
        RMS = 0,
        /// <summary>峰谷值波前误差</summary>
        PeakToValley = 1
    }
}
