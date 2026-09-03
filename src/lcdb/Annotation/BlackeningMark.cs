using System;
using System.Collections.Generic;
using LitMath;
using OtoCAD;

namespace lcdb.Annotation
{
    /// <summary>
    /// 涂墨标记实体 — <b>已废弃(2026-06-08)</b>,类保留仅供旧 .otocad 反序列化。
    ///
    /// 本类的方块+对角线符号系自创、无 GB/ISO 标准依据。涂墨/涂黑的标准符号是
    /// GB/T 13323-2009 表1 #17(—·— 点划线),由 <see cref="CoatingMark"/> +
    /// <see cref="CoatingType.Blackening"/> 实现并已人工核对 ✅(见镀膜双边核对页)。
    /// Ribbon「涂墨」按钮已改放 CoatingMark.Blackening;新代码不要使用本类。
    /// </summary>
    public class BlackeningMark : Entity, ISurfaceAttachable
    {
        public override string className => "BlackeningMark";

        public Vector2 Position { get; set; }
        public double Size { get; set; }

        /// <summary>
        /// 符号朝向(弧度)。0 = 默认竖直放置。
        /// 贴面放置时设为「外法线方向 - 90°」,使符号沿外法线竖立、坐在面外侧。
        /// 文字始终水平(字形不旋转,仅锚点随符号旋转)。
        /// </summary>
        public double Rotation { get; set; } = 0.0;
        public BlackeningPurpose Purpose { get; set; }
        /// <summary>覆盖率(%)</summary>
        public double Coverage { get; set; }
        public bool ShowText { get; set; }
        public BlackeningTextPosition TextPosition { get; set; }
        public string Text { get; set; }

        public BlackeningMark() : this(Vector2.Zero, 3.0) { }

        public BlackeningMark(Vector2 position, double size)
        {
            Position = position;
            Size = size;
            Purpose = BlackeningPurpose.StrayLightControl;
            Coverage = 100.0;
            ShowText = true;
            TextPosition = BlackeningTextPosition.Right;
            Text = "涂墨";
        }

        public override Bounding bounding
        {
            get
            {
                var halfSize = Size / 2;
                return new Bounding(
                    new Vector2(Position.X - halfSize, Position.Y - halfSize),
                    new Vector2(Position.X + halfSize, Position.Y + halfSize));
            }
        }

        /// <summary>绕 Position 旋转 Rotation 弧度 (Rotation == 0 时原样返回, 零开销)。</summary>
        private Vector2 R(Vector2 p) =>
            Rotation == 0.0 ? p : Vector2.RotateInRadian(p, Position, Rotation);

        public override void Draw(IGraphicsDraw gd)
        {
            // 绘制涂墨标记(矩形边框 + 对角线填充)
            // 先在「未旋转」坐标系算出四角, 再统一 R() 旋转 (Rotation = 0 时退化为原行为)。
            var halfSize = Size / 2;
            var p1 = R(new Vector2(Position.X - halfSize, Position.Y - halfSize));
            var p2 = R(new Vector2(Position.X + halfSize, Position.Y - halfSize));
            var p3 = R(new Vector2(Position.X + halfSize, Position.Y + halfSize));
            var p4 = R(new Vector2(Position.X - halfSize, Position.Y + halfSize));

            gd.DrawLine(p1, p2);
            gd.DrawLine(p2, p3);
            gd.DrawLine(p3, p4);
            gd.DrawLine(p4, p1);

            // 对角线填充
            gd.DrawLine(p1, p3);
            gd.DrawLine(p2, p4);

            if (ShowText && !string.IsNullOrEmpty(Text))
            {
                var textPos = Position;
                switch (TextPosition)
                {
                    case BlackeningTextPosition.Right:
                        textPos = new Vector2(Position.X + halfSize + 2, Position.Y);
                        break;
                    case BlackeningTextPosition.Left:
                        textPos = new Vector2(Position.X - halfSize - 2, Position.Y);
                        break;
                    case BlackeningTextPosition.Top:
                        textPos = new Vector2(Position.X, Position.Y + halfSize + 2);
                        break;
                    case BlackeningTextPosition.Bottom:
                        textPos = new Vector2(Position.X, Position.Y - halfSize - 2);
                        break;
                }

                // 文字锚点随符号旋转, 字形保持水平 (Text 实体不旋转)。
                textPos = R(textPos);

                var textEntity = new lcdb.Text
                {
                    Position = new Vector3(textPos.X, textPos.Y, 0),
                    Value = Text,
                    Height = 2.5,
                    color = this.color,
                    layer = this.layer
                };
                textEntity.Draw(gd);
            }
        }

        /// <summary>
        /// 贴面放置 (ISurfaceAttachable): 符号沿外法线竖立, 坐在面外侧。
        /// 沿外法线外移半个符号尺寸, 使符号坐在面外侧。BlackeningMark 直接绘制, 无缓存可清。
        /// </summary>
        public void AttachToSurface(Vector2 surfacePoint, Vector2 outwardNormal)
        {
            Rotation = Math.Atan2(outwardNormal.Y, outwardNormal.X) - Math.PI / 2.0;
            Position = surfacePoint + outwardNormal * (Size * 0.5);
        }

        public override void Translate(Vector2 translation)
        {
            Position += translation;
        }

        public override void Rotate(Vector2 rotCenter, double angle)
        {
            Position = Vector2.RotateInRadian(Position, rotCenter, angle);
            Rotation += angle;
        }

        public override void TransformBy(Matrix3 transform)
        {
            Position = transform * Position;
        }

        public override object Clone()
        {
            var clone = new BlackeningMark(Position, Size)
            {
                Rotation = this.Rotation,
                Purpose = this.Purpose,
                Coverage = this.Coverage,
                ShowText = this.ShowText,
                TextPosition = this.TextPosition,
                Text = this.Text,
                color = this.color,
                layerId = this.layerId
            };
            return clone;
        }

        protected override DBObject CreateInstance()
        {
            return new BlackeningMark(Vector2.Zero, 3.0);
        }

        public override List<GripPoint> GetGripPoints()
        {
            return new List<GripPoint>
            {
                new GripPoint(GripPointType.Center, Position)
            };
        }

        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            if (index == 0) Position = newPosition;
        }
    }

    /// <summary>涂墨目的</summary>
    public enum BlackeningPurpose
    {
        /// <summary>杂散光控制</summary>
        StrayLightControl,
        /// <summary>减少反射</summary>
        ReflectionReduction,
        /// <summary>边缘保护</summary>
        EdgeProtection,
        /// <summary>外观要求</summary>
        Cosmetic
    }

    /// <summary>涂墨文本位置</summary>
    public enum BlackeningTextPosition
    {
        /// <summary>右侧</summary>
        Right,
        /// <summary>左侧</summary>
        Left,
        /// <summary>顶部</summary>
        Top,
        /// <summary>底部</summary>
        Bottom
    }
}
