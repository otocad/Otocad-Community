using lcdb.Colors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using LitMath;
using lcdb;
using lcdb.Interfaces;
using OtoCAD;

namespace lcdb.Annotation
{
    /// <summary>
    /// 有效孔径 (GB/T 13323-2009 4.3.5) — <b>数据载体</b>(非独立标记)。
    ///
    /// 2026-06-08 模型对齐(skill <c>optical-mark-compliance</c>):有效孔径是"对零件的要求",
    /// 归 <b>面属性区表格行</b>(Φe 行,参照表面质量/面型精度),不再作独立 ⌀ 引线标记。
    /// 本类只保留参数 + <see cref="UpdateMarkText"/> 生成 <see cref="MarkText"/>(供属性区取值)
    /// 与序列化往返(旧 .otocad 兼容);<b>不再自绘几何,Draw 为空操作</b>。
    ///
    /// 记法(GB/T 13323-2009 4.3.5,待人工核原文):
    /// - 圆形: "Φe18.0"(可带公差 ±/+x/-y)
    /// - 方形: "长×宽"
    /// - 椭圆形: "长轴×短轴"
    /// </summary>
    public class EffectiveApertureMark : Entity, IOpticalMark
    {
        public override string className => "EffectiveApertureMark";

        #region IOpticalMark 接口属性
        public OpticalMarkType MarkType => OpticalMarkType.ISO10110_6;
        public Vector2 Position { get; set; } = new Vector2(0, 0);
        public double Scale { get; set; } = 1.0;
        public string MarkText { get; set; } = "Φe18";
        public bool ShowText { get; set; } = true;
        public Vector2 TextOffset { get; set; } = new Vector2(0, -30);
        public double Rotation { get; set; } = 0.0;
        public bool IsVisible { get; set; } = true;
        #endregion

        #region 有效孔径参数
        /// <summary>孔径形状类型</summary>
        public ApertureShapeType ShapeType { get; set; } = ApertureShapeType.Circular;

        /// <summary>直径/主尺寸 (mm)</summary>
        public double Diameter { get; set; } = 18.0;

        /// <summary>次尺寸 (mm) - 方形(宽度)/椭圆形(短轴)</summary>
        public double SecondaryDimension { get; set; } = 0.0;

        /// <summary>公差上偏差</summary>
        public double ToleranceUpper { get; set; } = 0.0;

        /// <summary>公差下偏差</summary>
        public double ToleranceLower { get; set; } = 0.0;

        /// <summary>引线起点(历史字段,仅供旧文件序列化往返;已不渲染)</summary>
        public Vector2 LeaderStart { get; set; } = new Vector2(0, 0);

        /// <summary>是否显示引线(历史字段;已不渲染)</summary>
        public bool ShowLeader { get; set; } = true;

        /// <summary>文本高度(历史字段;已不渲染)</summary>
        public double TextHeight { get; set; } = 3.5;
        #endregion

        public EffectiveApertureMark()
        {
        }

        public EffectiveApertureMark(Vector2 position, double diameter = 18.0)
        {
            Position = position;
            Diameter = diameter;
            UpdateMarkText();
        }

        /// <summary>
        /// 按形状/公差生成记法串(不变区域性: 恒用小数点)。
        /// </summary>
        public void UpdateMarkText()
        {
            var inv = CultureInfo.InvariantCulture;
            switch (ShapeType)
            {
                case ApertureShapeType.Circular:
                    MarkText = string.Format(inv, "Φe{0:F1}", Diameter);
                    if (ToleranceUpper != 0 || ToleranceLower != 0)
                    {
                        if (ToleranceUpper == -ToleranceLower)
                            MarkText += string.Format(inv, "±{0:F2}", Math.Abs(ToleranceUpper));
                        else
                            MarkText += string.Format(inv, "+{0:F2}/-{1:F2}", ToleranceUpper, Math.Abs(ToleranceLower));
                    }
                    break;

                case ApertureShapeType.Rectangular:
                case ApertureShapeType.Elliptical:
                    MarkText = string.Format(inv, "{0:F1}×{1:F1}", Diameter, SecondaryDimension);
                    break;
            }
        }

        public override Bounding bounding => new Bounding(Position, 20, 10);

        #region Entity 重写(数据载体: 不自绘,Draw 空操作)
        /// <summary>有效孔径已归面属性区 Φe 行,不再作独立标记自绘 → Draw 空操作。</summary>
        public override void Draw(IGraphicsDraw gd) { }

        protected override DBObject CreateInstance() => new EffectiveApertureMark();

        public override void Translate(Vector2 translation)
        {
            Position += translation;
            LeaderStart += translation;
        }

        public override void Rotate(Vector2 basePoint, double angle)
        {
            Position = Vector2.RotateInRadian(Position, basePoint, angle);
            LeaderStart = Vector2.RotateInRadian(LeaderStart, basePoint, angle);
            Rotation += angle * 180.0 / Math.PI;
        }

        public override void TransformBy(Matrix3 matrix)
        {
            Position = matrix * Position;
            LeaderStart = matrix * LeaderStart;
        }

        public override List<GripPoint> GetGripPoints()
            => new List<GripPoint> { new GripPoint(GripPointType.Center, Position) };

        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            if (index == 0) Position = newPosition;
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
            // 数据载体不渲染(GDI+ 路径属已封存 WinForms 端)
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
        }

        IOpticalMark IOpticalMark.Clone()
        {
            return (IOpticalMark)Clone();
        }

        string IOpticalMark.GetDescription()
        {
            return $"有效孔径: {MarkText}";
        }

        #endregion
    }

    /// <summary>
    /// 孔径形状类型
    /// </summary>
    public enum ApertureShapeType
    {
        /// <summary>圆形 - 使用Φe符号</summary>
        Circular,

        /// <summary>方形 - 标注长×宽</summary>
        Rectangular,

        /// <summary>椭圆形 - 标注长轴×短轴</summary>
        Elliptical
    }
}
