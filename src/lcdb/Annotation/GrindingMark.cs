using System;
using System.Collections.Generic;
using LitMath;
using OtoCAD;

namespace lcdb.Annotation
{
    /// <summary>
    /// 研磨标记(粗磨/精磨/超精磨/研磨)— Entity 实体,通过 Generate() 产生
    /// Circle + Text 子图元,经 IGraphicsDraw 渲染。
    ///
    /// 该实体替代旧 OtoCAD.Drawing.Elements.SpecialMarks.GrindingMark(非 Entity,
    /// 无法接入 presenter 渲染管线),迁入 lcdb.Annotation 后与 ProcessingMark
    /// / Roughness / CoatingMark 共用同一渲染、选择、序列化路径。
    /// </summary>
    public class GrindingMark : Entity, ISurfaceAttachable
    {
        public override string className => "GrindingMark";

        /// <summary>标记中心位置</summary>
        public Vector2 Center { get; set; } = new Vector2(0, 0);

        /// <summary>标记大小(模型单位)</summary>
        public double Size { get; set; } = 10.0;

        /// <summary>
        /// 符号朝向(弧度)。0 = 默认竖直放置。
        /// 砂轮符号为圆形(完全对称),旋转后视觉相同,故 Generate 不包 R();
        /// 仍保留 Rotation 字段以便 Clone/Rotate 与贴面放置维护一致状态。
        /// </summary>
        public double Rotation { get; set; } = 0.0;

        /// <summary>研磨类型</summary>
        public GrindingType Type { get; set; } = GrindingType.Fine;

        /// <summary>磨料粒度(目)</summary>
        public int GritSize { get; set; } = 600;

        /// <summary>研磨深度(μm)</summary>
        public double Depth { get; set; } = 50.0;

        /// <summary>研磨速度(mm/min)</summary>
        public double Speed { get; set; } = 100.0;

        /// <summary>是否显示粒度文字</summary>
        public bool ShowText { get; set; } = true;

        private List<Entity> _markEntities = new List<Entity>();

        public override Bounding bounding
        {
            get
            {
                double half = Size * 0.5;
                double textPad = ShowText ? Size * 0.5 : 0;
                return new Bounding(
                    new Vector2(Center.X - half, Center.Y - half - textPad),
                    new Vector2(Center.X + half, Center.Y + half));
            }
        }

        public GrindingMark() { }

        public GrindingMark(Vector2 center, double size = 10.0)
        {
            Center = center;
            Size = size;
        }

        /// <summary>
        /// 贴面放置 (ISurfaceAttachable): 符号坐在面外侧, 沿外法线外移半个符号尺寸。
        /// 圆形完全对称, Rotation 不影响圆的视觉, 主要价值是把符号摆到面外侧。
        /// </summary>
        public void AttachToSurface(Vector2 surfacePoint, Vector2 outwardNormal)
        {
            Rotation = Math.Atan2(outwardNormal.Y, outwardNormal.X) - Math.PI / 2.0;
            Center = surfacePoint + outwardNormal * (Size * 0.5);
            _markEntities.Clear();
        }

        protected void Generate()
        {
            _markEntities.Clear();

            double outerR = Size * 0.5;
            double innerR = outerR * 0.3;

            // 外圆 + 中心圆 (砂轮简化图)
            _markEntities.Add(new Circle { center = Center, radius = outerR, color = color });
            _markEntities.Add(new Circle { center = Center, radius = innerR, color = color });

            if (ShowText)
            {
                // 中心 "G" (Grinding) — 工厂惯例标识
                _markEntities.Add(new Text
                {
                    Value = "G",
                    Position = new Vector3(Center.X, Center.Y, 0.0),
                    Height = innerR * 1.3,
                    color = color,
                    alignment = TextAlignment.CenterMiddle,
                });

                // 下方: 粒度 "#600"
                if (GritSize > 0)
                {
                    _markEntities.Add(new Text
                    {
                        Value = $"#{GritSize}",
                        Position = new Vector3(Center.X, Center.Y - outerR - Size * 0.15, 0.0),
                        Height = Size * 0.22,
                        color = color,
                        alignment = TextAlignment.CenterMiddle,
                    });
                }
            }
        }

        public override void Draw(IGraphicsDraw gd)
        {
            if (_markEntities.Count == 0) Generate();
            foreach (var e in _markEntities) e.Draw(gd);
        }

        protected override DBObject CreateInstance() => new GrindingMark();

        public override object Clone()
        {
            var c = base.Clone() as GrindingMark;
            c.Center = Center;
            c.Size = Size;
            c.Rotation = Rotation;
            c.Type = Type;
            c.GritSize = GritSize;
            c.Depth = Depth;
            c.Speed = Speed;
            c.ShowText = ShowText;
            c._markEntities = new List<Entity>();
            return c;
        }

        public override void Translate(Vector2 translation)
        {
            Center += translation;
            _markEntities.Clear();
        }

        public override void Rotate(Vector2 rotCenter, double angle)
        {
            Center = Vector2.RotateInRadian(Center, rotCenter, angle);
            Rotation += angle;
            _markEntities.Clear();
        }

        public override void TransformBy(Matrix3 transform)
        {
            var sizeVec = new Vector2(Size, 0);
            Center = transform * Center;
            Size = (transform * sizeVec - transform * Vector2.Zero).length;
            _markEntities.Clear();
        }

        public override List<GripPoint> GetGripPoints()
        {
            var grips = new List<GripPoint>();
            grips.Add(new GripPoint(GripPointType.Center, Center));
            return grips;
        }

        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            if (index == 0)
            {
                Center = newPosition;
                _markEntities.Clear();
            }
        }
    }

    /// <summary>研磨类型</summary>
    public enum GrindingType
    {
        /// <summary>粗磨</summary>
        Rough,
        /// <summary>精磨</summary>
        Fine,
        /// <summary>超精磨</summary>
        UltraFine,
        /// <summary>研磨</summary>
        Lapping
    }
}
