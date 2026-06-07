using System;
using System.Collections.Generic;
using LitMath;
using OtoCAD;

namespace lcdb.Annotation
{
    /// <summary>
    /// 金刚石车削标记(SPDT / FTS / STS / 飞刀)— Entity 实体。
    /// 通过 Generate() 产生 Line 子图元(钻石轮廓),通过 IGraphicsDraw 渲染。
    /// 替代旧 OtoCAD.Drawing.Elements.SpecialMarks.DiamondTurningMark。
    /// </summary>
    public class DiamondTurningMark : Entity, ISurfaceAttachable
    {
        public override string className => "DiamondTurningMark";

        public Vector2 Center { get; set; } = new Vector2(0, 0);
        public double Size { get; set; } = 10.0;

        /// <summary>
        /// 符号朝向(弧度)。0 = 默认竖直放置。
        /// 菱形为 90° 对称, 旋转后视觉相同, 故 Generate 不包 R();
        /// 仍保留 Rotation 字段以便 Clone/Rotate 与贴面放置维护一致状态。
        /// </summary>
        public double Rotation { get; set; } = 0.0;

        public DiamondTurningType Type { get; set; } = DiamondTurningType.SinglePoint;
        public double ToolRadius { get; set; } = 1.0;
        public double FeedRate { get; set; } = 5.0;
        public int SpindleSpeed { get; set; } = 2000;
        public double CuttingDepth { get; set; } = 5.0;

        /// <summary>表面粗糙度 Ra (nm)</summary>
        public double SurfaceRoughness { get; set; } = 5.0;

        public bool ShowText { get; set; } = true;

        private List<Entity> _markEntities = new List<Entity>();

        public override Bounding bounding
        {
            get
            {
                double halfX = Math.Max(Size / 3.0, Size * 0.5);  // 文字宽 ~Size*0.5 (Ra 标签)
                double halfY = Size * 0.5;
                double textPad = ShowText ? Size * 0.4 : 0;  // 上下文字行
                return new Bounding(
                    new Vector2(Center.X - halfX, Center.Y - halfY - textPad),
                    new Vector2(Center.X + halfX, Center.Y + halfY + textPad));
            }
        }

        public DiamondTurningMark() { }

        public DiamondTurningMark(Vector2 center, double size = 10.0)
        {
            Center = center;
            Size = size;
        }

        /// <summary>
        /// 贴面放置 (ISurfaceAttachable): 符号坐在面外侧, 沿外法线外移半个符号尺寸。
        /// 菱形 90° 对称, Rotation 不影响菱形的视觉, 主要价值是把符号摆到面外侧。
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

            double halfY = Size * 0.5;
            double halfX = Size / 3.0;

            // 钻石形状(上/右/下/左 4 点连线)
            var top    = new Vector2(Center.X,         Center.Y + halfY);
            var right  = new Vector2(Center.X + halfX, Center.Y);
            var bottom = new Vector2(Center.X,         Center.Y - halfY);
            var left   = new Vector2(Center.X - halfX, Center.Y);

            _markEntities.Add(new Line(top, right)    { color = color });
            _markEntities.Add(new Line(right, bottom) { color = color });
            _markEntities.Add(new Line(bottom, left)  { color = color });
            _markEntities.Add(new Line(left, top)     { color = color });

            if (ShowText)
            {
                // 菱形内 "D" (Diamond Turning), 居中 (工厂惯例标识)
                _markEntities.Add(new Text
                {
                    Value = "D",
                    Position = new Vector3(Center.X, Center.Y, 0.0),
                    Height = Size * 0.3,
                    color = color,
                    alignment = TextAlignment.CenterMiddle,
                });

                // 上方: 工艺类型 (SPDT/FTS/STS/FC)
                string typeText = Type switch
                {
                    DiamondTurningType.SinglePoint   => "SPDT",
                    DiamondTurningType.FastToolServo => "FTS",
                    DiamondTurningType.SlowToolServo => "STS",
                    DiamondTurningType.FlyingCutter  => "FC",
                    _ => "SPDT"
                };
                _markEntities.Add(new Text
                {
                    Value = typeText,
                    Position = new Vector3(Center.X, Center.Y + Size * 0.65, 0.0),
                    Height = Size * 0.2,
                    color = color,
                    alignment = TextAlignment.CenterMiddle,
                });

                // 下方: Ra
                if (SurfaceRoughness > 0)
                {
                    _markEntities.Add(new Text
                    {
                        Value = $"Ra {SurfaceRoughness:G3}nm",
                        Position = new Vector3(Center.X, Center.Y - Size * 0.65, 0.0),
                        Height = Size * 0.2,
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

        protected override DBObject CreateInstance() => new DiamondTurningMark();

        public override object Clone()
        {
            var c = base.Clone() as DiamondTurningMark;
            c.Center = Center;
            c.Size = Size;
            c.Rotation = Rotation;
            c.Type = Type;
            c.ToolRadius = ToolRadius;
            c.FeedRate = FeedRate;
            c.SpindleSpeed = SpindleSpeed;
            c.CuttingDepth = CuttingDepth;
            c.SurfaceRoughness = SurfaceRoughness;
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
            return new List<GripPoint>
            {
                new GripPoint(GripPointType.Center, Center)
            };
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

    /// <summary>金刚石车削类型</summary>
    public enum DiamondTurningType
    {
        /// <summary>单点金刚石车削(SPDT)</summary>
        SinglePoint,
        /// <summary>快速刀具伺服(FTS)</summary>
        FastToolServo,
        /// <summary>慢速刀具伺服(STS)</summary>
        SlowToolServo,
        /// <summary>飞刀切削</summary>
        FlyingCutter
    }
}
