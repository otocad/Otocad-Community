using System;
using System.Collections.Generic;
using LitMath;
using OtoCAD;

namespace lcdb.Annotation
{
    /// <summary>
    /// 喷砂标记(轻/标准/重/精密)— Entity 实体。
    /// 简化符号:梯形喷嘴 + 水平表面线 + 参数文本。
    /// 替代旧 OtoCAD.Drawing.Elements.SpecialMarks.SandblastingMark。
    /// </summary>
    public class SandblastingMark : Entity, ISurfaceAttachable
    {
        public override string className => "SandblastingMark";

        public Vector2 Center { get; set; } = new Vector2(0, 0);
        public double Size { get; set; } = 10.0;

        /// <summary>
        /// 符号朝向(弧度)。0 = 喷嘴向下喷射(默认竖直放置)。
        /// 与 <see cref="Angle"/>(工艺喷砂角度, 度)无关:Rotation 是符号整体在图纸上的摆放朝向。
        /// 贴面放置时设为「外法线方向 - 90°」,使符号沿外法线竖立、坐在面外侧。
        /// 文字始终水平(字形不旋转,仅锚点随符号旋转)。
        /// </summary>
        public double Rotation { get; set; } = 0.0;

        public SandblastingType Type { get; set; } = SandblastingType.Standard;
        public AbrasiveType Abrasive { get; set; } = AbrasiveType.AluminumOxide;
        /// <summary>磨料粒度(目)</summary>
        public int GritSize { get; set; } = 180;
        /// <summary>喷砂压力(MPa)</summary>
        public double Pressure { get; set; } = 0.3;
        /// <summary>喷砂距离(mm)</summary>
        public double Distance { get; set; } = 150;
        /// <summary>喷砂角度(度)</summary>
        public double Angle { get; set; } = 90;

        public bool ShowText { get; set; } = true;

        private List<Entity> _markEntities = new List<Entity>();

        public override Bounding bounding
        {
            get
            {
                double half = Size * 0.5;
                double textPadBelow = ShowText ? Size * 0.8 : 0;
                return new Bounding(
                    new Vector2(Center.X - half * 1.5, Center.Y - half - textPadBelow),
                    new Vector2(Center.X + half * 1.5, Center.Y + half * 0.7));
            }
        }

        public SandblastingMark() { }

        public SandblastingMark(Vector2 center, double size = 10.0)
        {
            Center = center;
            Size = size;
        }

        /// <summary>
        /// 贴面放置 (ISurfaceAttachable): 符号沿外法线竖立, 坐在面外侧。
        /// 默认喷嘴主体在 Center 上方、向下喷射; 旋转使其对齐外法线。
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
            // 梯形喷嘴 (上宽下窄, 向下喷射)
            var topLeft  = R(new Vector2(Center.X - half / 2,    Center.Y + half * 0.5));
            var topRight = R(new Vector2(Center.X + half / 2,    Center.Y + half * 0.5));
            var botRight = R(new Vector2(Center.X + half / 3,    Center.Y));
            var botLeft  = R(new Vector2(Center.X - half / 3,    Center.Y));

            _markEntities.Add(new Line(topLeft, topRight)  { color = color });
            _markEntities.Add(new Line(topRight, botRight) { color = color });
            _markEntities.Add(new Line(botRight, botLeft)  { color = color });
            _markEntities.Add(new Line(botLeft, topLeft)   { color = color });

            // 表面线
            _markEntities.Add(new Line(
                R(new Vector2(Center.X - half, Center.Y - half * 0.6)),
                R(new Vector2(Center.X + half, Center.Y - half * 0.6))) { color = color });

            if (ShowText)
            {
                // 文字锚点随符号旋转, 但字形保持水平 (Text 实体不旋转)。
                // 喷嘴内 "S" (Sandblast) — 工厂惯例标识
                var sPos = R(new Vector2(Center.X, Center.Y + half * 0.25));
                _markEntities.Add(new Text
                {
                    Value = "S",
                    Position = new Vector3(sPos.X, sPos.Y, 0.0),
                    Height = half * 0.4,
                    color = color,
                    alignment = TextAlignment.CenterMiddle,
                });

                // 右侧: 磨料 + 粒度 (合并一行避免散乱)
                var abPos = R(new Vector2(Center.X, Center.Y - half * 0.9));
                _markEntities.Add(new Text
                {
                    Value = $"{GetAbrasiveCode()} #{GritSize}",
                    Position = new Vector3(abPos.X, abPos.Y, 0.0),
                    Height = Size * 0.2,
                    color = color,
                    alignment = TextAlignment.CenterMiddle,
                });

                // 下方: 压力 (P{Pressure}MPa)
                if (Pressure > 0)
                {
                    var pPos = R(new Vector2(Center.X, Center.Y - half * 1.2));
                    _markEntities.Add(new Text
                    {
                        Value = $"P {Pressure:G3}MPa",
                        Position = new Vector3(pPos.X, pPos.Y, 0.0),
                        Height = Size * 0.18,
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

        private string GetAbrasiveCode() => Abrasive switch
        {
            AbrasiveType.AluminumOxide  => "Al2O3",
            AbrasiveType.SiliconCarbide => "SiC",
            AbrasiveType.GlassBeads     => "GB",
            AbrasiveType.SteelShot      => "SS",
            AbrasiveType.WalnutShell    => "WS",
            _ => "AB"
        };

        protected override DBObject CreateInstance() => new SandblastingMark();

        public override object Clone()
        {
            var c = base.Clone() as SandblastingMark;
            c.Center = Center;
            c.Size = Size;
            c.Rotation = Rotation;
            c.Type = Type;
            c.Abrasive = Abrasive;
            c.GritSize = GritSize;
            c.Pressure = Pressure;
            c.Distance = Distance;
            c.Angle = Angle;
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

    /// <summary>喷砂类型</summary>
    public enum SandblastingType
    {
        /// <summary>轻度喷砂</summary>
        Light,
        /// <summary>标准喷砂</summary>
        Standard,
        /// <summary>重度喷砂</summary>
        Heavy,
        /// <summary>精密喷砂</summary>
        Precision
    }

    /// <summary>磨料类型</summary>
    public enum AbrasiveType
    {
        /// <summary>氧化铝</summary>
        AluminumOxide,
        /// <summary>碳化硅</summary>
        SiliconCarbide,
        /// <summary>玻璃珠</summary>
        GlassBeads,
        /// <summary>钢丸</summary>
        SteelShot,
        /// <summary>核桃壳</summary>
        WalnutShell
    }
}
