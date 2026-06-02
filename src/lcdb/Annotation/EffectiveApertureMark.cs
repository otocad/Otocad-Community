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
    /// 有效口径标记 (GB/T 13323-2009 4.3.5)
    /// 用于标注光学零件的有效孔径区域
    /// - 圆形: 使用"Φe"符号（如Φe18）
    /// - 方形: 标注"长×宽"
    /// - 椭圆形: 标注"长轴×短轴"
    /// </summary>
    public class EffectiveApertureMark : Entity, IOpticalMark
    {
        public override string className => "EffectiveApertureMark";

        #region IOpticalMark 接口属性

        /// <summary>
        /// 标记类型
        /// </summary>
        public OpticalMarkType MarkType => OpticalMarkType.ISO10110_6;

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
        public string MarkText { get; set; } = "Φe18";

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

        #region 有效口径特定属性

        /// <summary>
        /// 孔径形状类型
        /// </summary>
        public ApertureShapeType ShapeType { get; set; } = ApertureShapeType.Circular;

        /// <summary>
        /// 直径/主尺寸 (mm)
        /// </summary>
        public double Diameter { get; set; } = 18.0;

        /// <summary>
        /// 次尺寸 (mm) - 用于方形(宽度)或椭圆形(短轴)
        /// </summary>
        public double SecondaryDimension { get; set; } = 0.0;

        /// <summary>
        /// 公差上偏差
        /// </summary>
        public double ToleranceUpper { get; set; } = 0.0;

        /// <summary>
        /// 公差下偏差
        /// </summary>
        public double ToleranceLower { get; set; } = 0.0;

        /// <summary>
        /// 引线起点（从表面到标注）
        /// </summary>
        public Vector2 LeaderStart { get; set; } = new Vector2(0, 0);

        /// <summary>
        /// 是否显示引线
        /// </summary>
        public bool ShowLeader { get; set; } = true;

        /// <summary>
        /// 文本高度
        /// </summary>
        public double TextHeight { get; set; } = 3.5;

        #endregion

        private List<Entity> _markEntities = new List<Entity>();

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public EffectiveApertureMark()
        {
        }

        /// <summary>
        /// 带位置的构造函数
        /// </summary>
        public EffectiveApertureMark(Vector2 position, double diameter = 18.0)
        {
            Position = position;
            Diameter = diameter;
            UpdateMarkText();
        }

        /// <summary>
        /// 更新标记文本
        /// </summary>
        private void UpdateMarkText()
        {
            switch (ShapeType)
            {
                case ApertureShapeType.Circular:
                    MarkText = $"Φe{Diameter:F1}";
                    if (ToleranceUpper != 0 || ToleranceLower != 0)
                    {
                        if (ToleranceUpper == -ToleranceLower)
                            MarkText += $"±{Math.Abs(ToleranceUpper):F2}";
                        else
                            MarkText += $"+{ToleranceUpper:F2}/-{Math.Abs(ToleranceLower):F2}";
                    }
                    break;

                case ApertureShapeType.Rectangular:
                    MarkText = $"{Diameter:F1}×{SecondaryDimension:F1}";
                    break;

                case ApertureShapeType.Elliptical:
                    MarkText = $"{Diameter:F1}×{SecondaryDimension:F1}";
                    break;
            }
        }

        /// <summary>
        /// 外围边框 — 包括: 圆孔几何 + 双箭头标注 + 文字
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                double half = Diameter * 0.5 * Scale;
                double textWidth  = Math.Max(8, MarkText.Length * TextHeight * 0.6);
                double textHeight = TextHeight * 1.5;
                var textPos = Position + TextOffset;

                double minX = Math.Min(Math.Min(Position.X - half, LeaderStart.X), textPos.X - textWidth * 0.5) - 2;
                double maxX = Math.Max(Math.Max(Position.X + half, LeaderStart.X), textPos.X + textWidth * 0.5) + 2;
                double minY = Math.Min(Math.Min(Position.Y - half, LeaderStart.Y), textPos.Y - textHeight * 0.5) - 2;
                double maxY = Math.Max(Math.Max(Position.Y + half, LeaderStart.Y), textPos.Y + textHeight * 0.5) + 2;

                return new Bounding(new Vector2(minX, minY), new Vector2(maxX, maxY));
            }
        }

        /// <summary>
        /// 生成标记几何 — ISO 10110-2 有效孔径符号:
        /// - 圆 (孔径外径)
        /// - 水平双箭头 (φ 标注线, 跨越圆)
        /// - "Φe18.0" 文字 (Φe = effective diameter)
        /// </summary>
        private void Generate()
        {
            _markEntities.Clear();
            double half = Diameter * 0.5 * Scale;

            // 孔径圆
            _markEntities.Add(new Circle
            {
                center = Position,
                radius = half,
                color = this.color,
            });

            // 直径线 (穿过中心的水平线)
            _markEntities.Add(new Line(
                new Vector2(Position.X - half, Position.Y),
                new Vector2(Position.X + half, Position.Y)) { color = this.color });

            // 左箭头头部
            double ah = half * 0.15;
            _markEntities.Add(new Line(
                new Vector2(Position.X - half, Position.Y),
                new Vector2(Position.X - half + ah, Position.Y + ah * 0.6)) { color = this.color });
            _markEntities.Add(new Line(
                new Vector2(Position.X - half, Position.Y),
                new Vector2(Position.X - half + ah, Position.Y - ah * 0.6)) { color = this.color });

            // 右箭头头部
            _markEntities.Add(new Line(
                new Vector2(Position.X + half, Position.Y),
                new Vector2(Position.X + half - ah, Position.Y + ah * 0.6)) { color = this.color });
            _markEntities.Add(new Line(
                new Vector2(Position.X + half, Position.Y),
                new Vector2(Position.X + half - ah, Position.Y - ah * 0.6)) { color = this.color });

            // 引线 (可选)
            if (ShowLeader && LeaderStart != Position)
            {
                _markEntities.Add(new Line(LeaderStart, Position) { color = this.color });
            }

            // 文字标签
            if (ShowText)
            {
                var textPos = Position + TextOffset;
                _markEntities.Add(new Text
                {
                    Value = MarkText,
                    Position = new LitMath.Vector3(textPos.X, textPos.Y, 0.0),
                    Height = TextHeight * Scale,
                    color = this.color,
                    alignment = TextAlignment.CenterMiddle,
                });
            }
        }

        #region Entity 重写

        protected override DBObject CreateInstance() => new EffectiveApertureMark();

        public override void Draw(IGraphicsDraw gd)
        {
            if (!IsVisible) return;

            if (_markEntities.Count == 0)
                Generate();

            foreach (var entity in _markEntities)
            {
                entity.Draw(gd);
            }
        }

        public override void Translate(Vector2 translation)
        {
            Position += translation;
            LeaderStart += translation;
            _markEntities.Clear();
        }

        public override void Rotate(Vector2 basePoint, double angle)
        {
            Position = Vector2.RotateInRadian(Position, basePoint, angle);
            LeaderStart = Vector2.RotateInRadian(LeaderStart, basePoint, angle);
            Rotation += angle * 180.0 / Math.PI;
            _markEntities.Clear();
        }

        public override void TransformBy(Matrix3 matrix)
        {
            Position = matrix * Position;
            LeaderStart = matrix * LeaderStart;
            _markEntities.Clear();
        }

        public override List<GripPoint> GetGripPoints()
        {
            var gripPoints = new List<GripPoint>();
            gripPoints.Add(new GripPoint(GripPointType.Center, Position));
            gripPoints.Add(new GripPoint(GripPointType.End, LeaderStart));
            return gripPoints;
        }

        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            switch (index)
            {
                case 0:
                    Position = newPosition;
                    break;
                case 1:
                    LeaderStart = newPosition;
                    break;
            }
            _markEntities.Clear();
        }

        public override object Clone()
        {
            var clone = new EffectiveApertureMark();
            clone.Position = this.Position;
            clone.Scale = this.Scale;
            clone.MarkText = this.MarkText;
            clone.ShowText = this.ShowText;
            clone.TextOffset = this.TextOffset;
            clone.Rotation = this.Rotation;
            clone.IsVisible = this.IsVisible;
            clone.ShapeType = this.ShapeType;
            clone.Diameter = this.Diameter;
            clone.SecondaryDimension = this.SecondaryDimension;
            clone.ToleranceUpper = this.ToleranceUpper;
            clone.ToleranceLower = this.ToleranceLower;
            clone.LeaderStart = this.LeaderStart;
            clone.ShowLeader = this.ShowLeader;
            clone.TextHeight = this.TextHeight;
            clone.color = this.color;
            return clone;
        }

        #endregion

        #region IOpticalMark 接口方法

#if WINDOWS
        void IOpticalMark.Draw(Graphics g, float scale)
        {
            // GDI+ 绘制实现（用于预览等）
        }
#endif

        RectangleF IOpticalMark.GetBounds()
        {
            var b = bounding;
            return new RectangleF(
                (float)b.left, (float)b.bottom,
                (float)b.width, (float)b.height);
        }

        bool IOpticalMark.Validate()
        {
            return Diameter > 0 && TextHeight > 0;
        }

        Dictionary<string, object> IOpticalMark.GetProperties()
        {
            return new Dictionary<string, object>
            {
                { "ShapeType", ShapeType },
                { "Diameter", Diameter },
                { "SecondaryDimension", SecondaryDimension },
                { "ToleranceUpper", ToleranceUpper },
                { "ToleranceLower", ToleranceLower },
                { "ShowLeader", ShowLeader },
                { "TextHeight", TextHeight }
            };
        }

        void IOpticalMark.SetProperties(Dictionary<string, object> properties)
        {
            if (properties.TryGetValue("ShapeType", out var shapeType))
                ShapeType = (ApertureShapeType)shapeType;
            if (properties.TryGetValue("Diameter", out var diameter))
                Diameter = Convert.ToDouble(diameter);
            if (properties.TryGetValue("SecondaryDimension", out var secondary))
                SecondaryDimension = Convert.ToDouble(secondary);
            if (properties.TryGetValue("ToleranceUpper", out var upper))
                ToleranceUpper = Convert.ToDouble(upper);
            if (properties.TryGetValue("ToleranceLower", out var lower))
                ToleranceLower = Convert.ToDouble(lower);
            if (properties.TryGetValue("ShowLeader", out var showLeader))
                ShowLeader = Convert.ToBoolean(showLeader);
            if (properties.TryGetValue("TextHeight", out var textHeight))
                TextHeight = Convert.ToDouble(textHeight);

            UpdateMarkText();
            _markEntities.Clear();
        }

        IOpticalMark IOpticalMark.Clone()
        {
            return (IOpticalMark)Clone();
        }

        string IOpticalMark.GetDescription()
        {
            return $"有效口径标记: {MarkText}";
        }

        #endregion
    }

    /// <summary>
    /// 孔径形状类型
    /// </summary>
    public enum ApertureShapeType
    {
        /// <summary>
        /// 圆形 - 使用Φe符号
        /// </summary>
        Circular,

        /// <summary>
        /// 方形 - 标注长×宽
        /// </summary>
        Rectangular,

        /// <summary>
        /// 椭圆形 - 标注长轴×短轴
        /// </summary>
        Elliptical
    }
}
