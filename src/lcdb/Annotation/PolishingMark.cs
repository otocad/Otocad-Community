using System;
using System.Collections.Generic;
using LitMath;
using OtoCAD;

namespace lcdb.Annotation
{
    /// <summary>
    /// 抛光标记(粗/标准/精/超精)— Entity 实体。
    /// ISO 抛光符号:三角形 + 内部水平线条(条数对应等级)。
    /// 替代旧 OtoCAD.Drawing.Elements.SpecialMarks.PolishingMark(非 Entity,无法渲染)。
    /// </summary>
    public class PolishingMark : Entity, ISurfaceAttachable
    {
        public override string className => "PolishingMark";

        public Vector2 Center { get; set; } = new Vector2(0, 0);
        public double Size { get; set; } = 10.0;

        /// <summary>
        /// 符号朝向(弧度)。0 = 三角形顶点朝下(默认竖直放置)。
        /// 贴面放置时设为「外法线方向 - 90°」,使符号沿外法线竖立、坐在面外侧。
        /// 文字始终水平(字形不旋转,仅锚点随符号旋转)。
        /// </summary>
        public double Rotation { get; set; } = 0.0;

        public PolishingGrade Grade { get; set; } = PolishingGrade.Standard;
        /// <summary>表面粗糙度 Ra (nm)</summary>
        public double RoughnessRa { get; set; } = 10.0;
        public PolishingMethod Method { get; set; } = PolishingMethod.Mechanical;
        public bool IsOpticalPolish { get; set; } = true;
        public bool ShowText { get; set; } = true;

        private List<Entity> _markEntities = new List<Entity>();

        public override Bounding bounding
        {
            get
            {
                double half = Size * 0.5;
                double textW = ShowText ? Size * 1.2 : 0;
                return new Bounding(
                    new Vector2(Center.X - half, Center.Y - Size * 2 / 3),
                    new Vector2(Center.X + half + textW, Center.Y + Size / 3));
            }
        }

        public PolishingMark() { }

        public PolishingMark(Vector2 center, double size = 10.0)
        {
            Center = center;
            Size = size;
        }

        /// <summary>
        /// 贴面放置 (ISurfaceAttachable): 符号沿外法线竖立, 坐在面外侧。
        /// 默认符号主体在 Center 上方, 顶点朝下; 旋转使其对齐外法线。
        /// </summary>
        public void AttachToSurface(Vector2 surfacePoint, Vector2 outwardNormal)
        {
            Rotation = Math.Atan2(outwardNormal.Y, outwardNormal.X) - Math.PI / 2.0;
            Center = surfacePoint + outwardNormal * (Size * 0.5);
            _markEntities.Clear();
        }

        /// <summary>绕 Center 旋转 Rotation 弧度 (Rotation == 0 时原样返回, 零开销)。</summary>
        private Vector2 R(Vector2 p) =>
            Rotation == 0.0 ? p : Vector2.RotateInRadian(p, Center, Rotation);

        protected void Generate()
        {
            _markEntities.Clear();

            // 先在「未旋转」坐标系算出所有点, 再统一 R() 旋转 (Rotation = 0 时退化为原行为)。
            double half = Size * 0.5;
            var apex   = R(new Vector2(Center.X,        Center.Y - Size * 2.0 / 3.0));
            var rightB = R(new Vector2(Center.X + half, Center.Y + Size / 3.0));
            var leftB  = R(new Vector2(Center.X - half, Center.Y + Size / 3.0));

            _markEntities.Add(new Line(leftB, apex)   { color = color });
            _markEntities.Add(new Line(apex, rightB)  { color = color });

            // 等级:在三角形内画 1/2/3 条水平线
            int lineCount = Grade switch
            {
                PolishingGrade.Rough     => 0,
                PolishingGrade.Standard  => 1,
                PolishingGrade.Fine      => 2,
                PolishingGrade.UltraFine => 3,
                _ => 1
            };

            for (int i = 0; i < lineCount; i++)
            {
                double y = Center.Y - Size / 3.0 + i * Size / 6.0;
                double w = Size / 3.0;
                _markEntities.Add(new Line(
                    R(new Vector2(Center.X - w, y)),
                    R(new Vector2(Center.X + w, y))) { color = color });
            }
        }

        public override void Draw(IGraphicsDraw gd)
        {
            if (_markEntities.Count == 0) Generate();
            foreach (var e in _markEntities) e.Draw(gd);

            if (ShowText)
            {
                // 文字锚点随符号旋转, 但字形保持水平 (角度参数仍传 0.0)。
                var raPos = R(new Vector2(Center.X + Size * 0.6, Center.Y - Size * 0.2));
                gd.DrawText(
                    raPos,
                    $"Ra{RoughnessRa}",
                    Size * 0.25,
                    "Standard",
                    TextAlignment.LeftMiddle,
                    0.0);
            }
        }

        protected override DBObject CreateInstance() => new PolishingMark();

        public override object Clone()
        {
            var c = base.Clone() as PolishingMark;
            c.Center = Center;
            c.Size = Size;
            c.Rotation = Rotation;
            c.Grade = Grade;
            c.RoughnessRa = RoughnessRa;
            c.Method = Method;
            c.IsOpticalPolish = IsOpticalPolish;
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

    /// <summary>抛光等级</summary>
    public enum PolishingGrade
    {
        /// <summary>粗抛光</summary>
        Rough,
        /// <summary>标准抛光</summary>
        Standard,
        /// <summary>精抛光</summary>
        Fine,
        /// <summary>超精抛光</summary>
        UltraFine
    }

    /// <summary>抛光方法</summary>
    public enum PolishingMethod
    {
        /// <summary>机械抛光</summary>
        Mechanical,
        /// <summary>化学机械抛光(CMP)</summary>
        ChemicalMechanical,
        /// <summary>磁流变抛光(MRF)</summary>
        Magnetorheological,
        /// <summary>离子束抛光(IBF)</summary>
        IonBeam
    }
}
