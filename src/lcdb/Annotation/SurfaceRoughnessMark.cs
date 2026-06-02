using System;
using System.Collections.Generic;
using LitMath;
using OtoCAD;

namespace lcdb.Annotation
{
    /// <summary>
    /// 表面粗糙度标记(Ra/Rq/Rz 多参数)— Entity 实体。
    /// 与既有 lcdb.Annotation.Roughness 区别:Roughness 是 GB 简单粗糙度三角形;
    /// 本类支持 Ra/Rq/Rz 多参数 + 加工方法 + ISO/DIN/JIS/GB/ANSI 多标准 + 纹理方向。
    /// 替代旧 OtoCAD.Drawing.Elements.SpecialMarks.SurfaceRoughnessMark。
    /// </summary>
    public class SurfaceRoughnessMark : Entity, ISurfaceAttachable
    {
        public override string className => "SurfaceRoughnessMark";

        public Vector2 Center { get; set; } = new Vector2(0, 0);
        public double Size { get; set; } = 10.0;

        /// <summary>
        /// 符号朝向(弧度)。0 = V 顶点朝下(默认竖直放置)。
        /// 贴面放置时设为「该面外法线方向 - 90°」,使 V 顶点落在面上、符号轴沿外法线
        /// (GB/T 131 标准:符号尖端指向被测表面)。
        /// 文字始终水平(Text 实体不旋转,仅锚点随符号一起旋转)。
        /// </summary>
        public double Rotation { get; set; } = 0.0;

        /// <summary>Ra 算术平均粗糙度(nm)</summary>
        public double Ra { get; set; } = 0.8;
        /// <summary>Rq 均方根粗糙度(nm)</summary>
        public double Rq { get; set; } = 1.0;
        /// <summary>Rz 十点平均粗糙度(nm)</summary>
        public double Rz { get; set; } = 3.2;

        public MachiningMethod Method { get; set; } = MachiningMethod.Grinding;
        public RoughnessStandard Standard { get; set; } = RoughnessStandard.ISO;

        /// <summary>取样长度(mm)</summary>
        public double SamplingLength { get; set; } = 0.8;
        /// <summary>评定长度(mm)</summary>
        public double EvaluationLength { get; set; } = 4.0;
        public TextureDirection Direction { get; set; } = TextureDirection.Parallel;

        public bool ShowText { get; set; } = true;

        private List<Entity> _markEntities = new List<Entity>();

        /// <summary>
        /// 是否需要去除材料 (GB/T 131-2006 4.1.2):
        ///   true  → 基本 V 形 + 水平延长线 (∇ 形, 最常用, 表示"去除材料")
        ///   false → V 形 + 内接圆 (⌀V, 表示"不允许去除材料")
        /// 若 null/Any → 仅 V 形 (任意方法)
        /// </summary>
        public MaterialRemoval Removal { get; set; } = MaterialRemoval.Remove;

        public override Bounding bounding
        {
            get
            {
                double half = Size * 0.5;
                // 水平延长线 + Ra 文字 (在延长线上方) 占右上空间; 加工方法 在 V 上方
                double textW = ShowText ? Size * 2.5 : Size * 1.2;
                double topY  = Center.Y + Size * 2.0 / 3.0 + (ShowText ? Size * 0.5 : 0);
                var lo = new Vector2(Center.X - half, Center.Y - Size * 2 / 3);
                var hi = new Vector2(Center.X + half + textW, topY);
                if (Rotation == 0.0)
                    return new Bounding(lo, hi);

                // 已旋转: 取 4 个角点旋转后的轴对齐包围盒
                var c1 = Vector2.RotateInRadian(lo, Center, Rotation);
                var c2 = Vector2.RotateInRadian(hi, Center, Rotation);
                var c3 = Vector2.RotateInRadian(new Vector2(lo.X, hi.Y), Center, Rotation);
                var c4 = Vector2.RotateInRadian(new Vector2(hi.X, lo.Y), Center, Rotation);
                double minX = Math.Min(Math.Min(c1.X, c2.X), Math.Min(c3.X, c4.X));
                double maxX = Math.Max(Math.Max(c1.X, c2.X), Math.Max(c3.X, c4.X));
                double minY = Math.Min(Math.Min(c1.Y, c2.Y), Math.Min(c3.Y, c4.Y));
                double maxY = Math.Max(Math.Max(c1.Y, c2.Y), Math.Max(c3.Y, c4.Y));
                return new Bounding(new Vector2(minX, minY), new Vector2(maxX, maxY));
            }
        }

        public SurfaceRoughnessMark() { }

        public SurfaceRoughnessMark(Vector2 center, double size = 10.0)
        {
            Center = center;
            Size = size;
        }

        /// <summary>
        /// GB/T 131-2006 表面粗糙度符号 — 完整布局:
        ///
        ///   方法
        ///   ───────────  ← 水平延长线 (∇ 形, position b 上方)
        ///   \    /     position a: Ra值 (右侧, 与延长线齐高)
        ///    \  /
        ///     V    Ra 0.8
        ///
        /// 符号几何 (符合 GB/T 131-2006 4.1):
        ///   - V 形两边等长, 短边 60°, 长边 30°(右边长是左边的 2 倍)
        ///   - 顶点向下 (V 底部代表"被测表面")
        /// </summary>
        /// <summary>
        /// 贴面放置 (ISurfaceAttachable): V 顶点落在面上的 surfacePoint, 符号轴沿外法线 outwardNormal 竖立。
        /// 默认符号轴 (顶点→开口) 指向 +Y(90°); 旋转使其对齐外法线。
        /// 顶点在 Center 下方 2/3·Size 处, 故 Center 沿外法线外移, 让顶点正好落在面上。
        /// </summary>
        public void AttachToSurface(Vector2 surfacePoint, Vector2 outwardNormal)
        {
            Rotation = Math.Atan2(outwardNormal.Y, outwardNormal.X) - Math.PI / 2.0;
            Center = surfacePoint + outwardNormal * (Size * 2.0 / 3.0);
            _markEntities.Clear();
        }

        /// <summary>绕 Center 旋转 Rotation 弧度 (Rotation == 0 时原样返回, 零开销)。</summary>
        private Vector2 R(Vector2 p) =>
            Rotation == 0.0 ? p : Vector2.RotateInRadian(p, Center, Rotation);

        protected void Generate()
        {
            _markEntities.Clear();

            // GB/T 131-2006 4.1: V 形, 短边在左 60°, 长边在右 30°(2:1 比例)
            // 顶点在下, 表示被测表面
            // 先在「未旋转」坐标系算出所有点, 最后统一绕 Center 旋转 Rotation,
            // 使贴面放置时 V 顶点落在面上、符号轴沿外法线 (Rotation = 0 时退化为原行为)。
            double half = Size * 0.5;
            double barY = Center.Y + Size * 2.0 / 3.0;  // 长边右端点 / 延长线的 Y
            var apex   = R(new Vector2(Center.X,            Center.Y - Size * 2.0 / 3.0));
            var leftB  = R(new Vector2(Center.X - half * 0.7, Center.Y + Size / 3.0));
            var rightB = R(new Vector2(Center.X + half,     barY));  // 长边右

            _markEntities.Add(new Line(leftB, apex)  { color = color });
            _markEntities.Add(new Line(apex, rightB) { color = color });

            // GB/T 131-2006 4.1.2: "去除材料" (∇) — 水平延长线从右上端点水平向右伸出
            if (Removal == MaterialRemoval.Remove)
            {
                var barEnd = R(new Vector2(Center.X + Size * 1.8, barY));
                _markEntities.Add(new Line(rightB, barEnd) { color = color });
            }
            // GB/T 131-2006 4.1.3: "不去除材料" (⌀V) — V 形内接圆 (绕 Center 旋转后圆心不变)
            else if (Removal == MaterialRemoval.NotRemove)
            {
                _markEntities.Add(new Circle
                {
                    center = new Vector2(Center.X, Center.Y),
                    radius = Size * 0.25,
                    color = color
                });
            }
            // MaterialRemoval.Any → 不加修饰, 表示任意加工方法

            if (ShowText)
            {
                // GB/T 131-2006 Figure 1 / 5.3 - 参数值 (Ra) 写在水平延长线 *上方*
                // 文字锚点随符号旋转, 但字形保持水平 (Text 实体不旋转), 符合"文字水平书写"要求。
                var raPos = R(new Vector2(Center.X + Size * 0.5, barY + Size * 0.25));
                _markEntities.Add(new Text
                {
                    Value = FormatRoughnessValue("Ra", Ra),
                    Position = new Vector3(raPos.X, raPos.Y, 0.0),
                    Height = Size * 0.28,
                    color = color,
                    alignment = TextAlignment.LeftMiddle,
                });

                // GB/T 131-2006 Figure 1 - Position b 加工方法, 写在 V 形上方 (左侧)
                // 仅当 Remove 模式 + 非默认方法 (避免每张图都重复打"磨")
                if (Removal == MaterialRemoval.Remove && Method != MachiningMethod.Grinding)
                {
                    var mPos = R(new Vector2(Center.X - Size * 0.3, barY + Size * 0.25));
                    _markEntities.Add(new Text
                    {
                        Value = GetMethodLabel(Method),
                        Position = new Vector3(mPos.X, mPos.Y, 0.0),
                        Height = Size * 0.22,
                        color = color,
                        alignment = TextAlignment.LeftMiddle,
                    });
                }
            }
        }

        /// <summary>
        /// GB/T 131-2006 5.3: 参数值格式. 单位 μm. 值为 0 时显示 "—" (未指定).
        /// </summary>
        private static string FormatRoughnessValue(string param, double valueUm)
        {
            if (valueUm <= 0) return $"{param} —";
            // 0.001-100 μm 范围内, 保留 2 位有效数字
            return $"{param} {valueUm:G2}";
        }

        private static string GetMethodLabel(MachiningMethod m)
        {
            switch (m)
            {
                case MachiningMethod.Turning:   return "车";
                case MachiningMethod.Grinding:  return "磨";
                case MachiningMethod.Polishing: return "抛";
                case MachiningMethod.Lapping:   return "研";
                case MachiningMethod.EDM:       return "电火花";
                case MachiningMethod.Milling:   return "铣";
                default: return "";
            }
        }

        public override void Draw(IGraphicsDraw gd)
        {
            if (_markEntities.Count == 0) Generate();
            foreach (var e in _markEntities) e.Draw(gd);
        }

        protected override DBObject CreateInstance() => new SurfaceRoughnessMark();

        public override object Clone()
        {
            var c = base.Clone() as SurfaceRoughnessMark;
            c.Center = Center;
            c.Size = Size;
            c.Rotation = Rotation;
            c.Ra = Ra; c.Rq = Rq; c.Rz = Rz;
            c.Method = Method;
            c.Standard = Standard;
            c.SamplingLength = SamplingLength;
            c.EvaluationLength = EvaluationLength;
            c.Direction = Direction;
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
            Rotation += angle;  // 符号本身也随之转向, 保持与被测面的相对朝向
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

    /// <summary>加工方法(SurfaceRoughnessMark 使用)</summary>
    public enum MachiningMethod
    {
        /// <summary>车削</summary>
        Turning,
        /// <summary>磨削</summary>
        Grinding,
        /// <summary>抛光</summary>
        Polishing,
        /// <summary>研磨</summary>
        Lapping,
        /// <summary>电火花</summary>
        EDM,
        /// <summary>铣削</summary>
        Milling
    }

    /// <summary>
    /// GB/T 131-2006 4.1 材料去除属性 (决定 V 形符号的修饰):
    ///   Any        — 仅 V (任意加工方法), 4.1.1
    ///   Remove     — V + 水平延长线 (∇, 去除材料), 4.1.2 [最常用]
    ///   NotRemove  — V + 内接圆 (⌀V, 不允许去除材料), 4.1.3
    /// </summary>
    public enum MaterialRemoval
    {
        Any,
        Remove,
        NotRemove,
    }

    /// <summary>粗糙度标准</summary>
    public enum RoughnessStandard
    {
        /// <summary>ISO</summary>
        ISO,
        /// <summary>DIN</summary>
        DIN,
        /// <summary>JIS</summary>
        JIS,
        /// <summary>GB</summary>
        GB,
        /// <summary>ANSI</summary>
        ANSI
    }

    // 注:TextureDirection 复用 lcdb.Annotation.SurfaceTextureMark 中既有定义
    // (Any/Parallel/Perpendicular/Crossed/Radial/Concentric),避免命名空间重复。
}
