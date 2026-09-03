using lcdb.Colors;
using System;
using System.Collections.Generic;
using System.Drawing;
using LitMath;
using lcdb;
using lcdb.Interfaces;
using lcdb.Symbols;
using OtoCAD;

namespace lcdb.Annotation
{
    /// <summary>
    /// ISO 10110-6 中心公差标记实现
    /// 用于标注光学元件的偏心和倾斜公差
    /// </summary>
    public class CenteringToleranceMark : Entity, IOpticalMark
    {
        public override string className => "CenteringToleranceMark";

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
        public string MarkText { get; set; } = "4/0.05,0.02";

        /// <summary>
        /// 是否显示文本
        /// </summary>
        public bool ShowText { get; set; } = true;

        /// <summary>
        /// 文本偏移量?        /// </summary>
        public Vector2 TextOffset { get; set; } = new Vector2(0, -30);

        /// <summary>
        /// 标记旋转角度（度）?        /// </summary>
        public double Rotation { get; set; } = 0.0;

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible { get; set; } = true;

        #endregion

        #region ISO 10110-6 特定属性
        /// <summary>
        /// 偏心公差（mm?        /// </summary>
        public double DecentrationTolerance { get; set; } = 0.05;

        /// <summary>
        /// 倾斜公差（角分）
        /// </summary>
        public double TiltTolerance { get; set; } = 0.02;

        /// <summary>
        /// 参考面编号
        /// </summary>
        public string ReferenceSurface { get; set; } = "1";

        /// <summary>
        /// 测试波长（nm?        /// </summary>
        public double TestWavelength { get; set; } = 632.8;

        /// <summary>
        /// 中心公差类型
        /// </summary>
        public CenteringToleranceType ToleranceType { get; set; } = CenteringToleranceType.DecentrationAndTilt;

        /// <summary>
        /// 标记框架大小
        /// </summary>
        public Vector2 FrameSize { get; set; } = new Vector2(50, 30);

        /// <summary>
        /// 检测标准?        /// </summary>
        public string Standard { get; set; } = "ISO 10110-6";

        #endregion

        private List<Entity> _markEntities = new List<Entity>();

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                double w = Math.Max(8.0, MarkText.Length * 3.0 * Scale);
                double h = 6.0 * Scale;
                return new Bounding(Position, w, h);
            }
        }

        #region 构造函数
        /// <summary>
        /// 默认构造函数?        /// </summary>
        public CenteringToleranceMark()
        {
            UpdateMarkText();
        }

        /// <summary>
        /// 带参数构造函数?        /// </summary>
        public CenteringToleranceMark(Vector2 position, double decentrationTolerance, double tiltTolerance)
        {
            Position = position;
            DecentrationTolerance = decentrationTolerance;
            TiltTolerance = tiltTolerance;
            UpdateMarkText();
        }

        #endregion

        #region 核心方法

        /// <summary>
        /// 更新标记文本
        /// </summary>
        private void UpdateMarkText()
        {
            // ISO 10110-6 对中公差的指示代号是 "4/"(ISO 10110-10 代号表;非部件号 6)
            MarkText = $"4/{DecentrationTolerance:F3},{TiltTolerance:F3}";
        }

        /// <summary>
        /// 生成标记图形
        /// </summary>
        protected void Generate()
        {
            _markEntities.Clear();
            if (!ShowText) return;
            // 中心偏差按“技术要求表”理念只出 ISO 10110-6 指示代号文字 (4/偏心,倾斜),
            // 不画方框/十字/中心圆, 也不重复 e≤/t≤ 与中文长句。
            _markEntities.Add(new Text
            {
                Value = MarkText,
                Position = new Vector3(Position.X, Position.Y, 0.0),
                Height = 5 * Scale,
                color = color,
                alignment = lcdb.TextAlignment.CenterMiddle,
            });
        }

        #endregion

        
        #region 绘制方法重写

        /// <summary>
        /// 重写Entity的Draw方法以绘制光学标记
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            // 确保生成了图形元素
            if (_markEntities.Count == 0)
            {
                Generate();
            }

            // 绘制所有子实体
            foreach (var entity in _markEntities)
            {
                entity.Draw(gd);
            }
        }

        #endregion

#region IOpticalMark 接口实现

#if WINDOWS
        /// <summary>
        /// 绘制标记
        /// </summary>
        public void Draw(Graphics g, float scale)
        {
            var position = new PointF((float)Position.X, (float)Position.Y);
            DrawISO10110_6Symbol(g, position, scale * (float)Scale);
        }

        /// <summary>
        /// 绘制ISO 10110-6符号
        /// </summary>
        private void DrawISO10110_6Symbol(Graphics g, PointF position, float scale)
        {
            using (var pen = new Pen(color.ToDrawingColor(), 1.5f * scale))
            using (var brush = new SolidBrush(color.ToDrawingColor()))
            using (var font = new Font("Arial", 6 * scale, System.Drawing.FontStyle.Bold))
            {
                float width = (float)FrameSize.X * scale;
                float height = (float)FrameSize.Y * scale;

                // 绘制圆角矩形框架
                RectangleF rect = new RectangleF(position.X - width/2, position.Y - height/2, width, height);
                using (var path = CreateRoundedRectPath(rect, 3 * scale))
                {
                    g.DrawPath(pen, path);
                }

                // 绘制中心点
                float lineLen = width * 0.3f;
                g.DrawLine(pen, position.X - lineLen/2, position.Y, position.X + lineLen/2, position.Y);
                g.DrawLine(pen, position.X, position.Y - lineLen/2, position.X, position.Y + lineLen/2);

                // 绘制中心点
                float circleSize = 4 * scale;
                g.DrawEllipse(pen, position.X - circleSize/2, position.Y - circleSize/2, circleSize, circleSize);

                // 绘制标识和公差值
                string identifier = "6";
                string tolerances = $"e≤{DecentrationTolerance:F3}\nt≤{TiltTolerance:F3}'";
                
                g.DrawString(identifier, font, brush, position.X - width*0.35f, position.Y + height*0.25f);
                g.DrawString(tolerances, font, brush, position.X + width*0.15f, position.Y, 
                           new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }
        }

        /// <summary>
        /// 创建圆角矩形路径
        /// </summary>
        private System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectPath(RectangleF rect, float radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
#endif

        /// <summary>
        /// 获取标记边界框?        /// </summary>
        public RectangleF GetBounds()
        {
            var bound = bounding;
            return new RectangleF((float)bound.left, (float)bound.bottom, (float)bound.width, (float)bound.height);
        }

        /// <summary>
        /// 验证标记数据
        /// </summary>
        public bool Validate()
        {
            // 验证偏心公差范围
            if (DecentrationTolerance < 0 || DecentrationTolerance > 1.0)
                return false;

            // 验证倾斜公差范围
            if (TiltTolerance < 0 || TiltTolerance > 10.0)
                return false;

            // 验证参考面编号
            if (string.IsNullOrEmpty(ReferenceSurface))
                return false;

            return true;
        }

        /// <summary>
        /// 获取标记属性        /// </summary>
        public Dictionary<string, object> GetProperties()
        {
            return new Dictionary<string, object>
            {
                { "Position", Position },
                { "Scale", Scale },
                { "MarkText", MarkText },
                { "ShowText", ShowText },
                { "TextOffset", TextOffset },
                { "Rotation", Rotation },
                { "IsVisible", IsVisible },
                { "DecentrationTolerance", DecentrationTolerance },
                { "TiltTolerance", TiltTolerance },
                { "ReferenceSurface", ReferenceSurface },
                { "TestWavelength", TestWavelength },
                { "ToleranceType", ToleranceType },
                { "FrameSize", FrameSize },
                { "Standard", Standard }
            };
        }

        /// <summary>
        /// 设置标记属性        /// </summary>
        public void SetProperties(Dictionary<string, object> properties)
        {
            if (properties.ContainsKey("Position"))
                Position = (Vector2)properties["Position"];
            if (properties.ContainsKey("Scale"))
                Scale = (double)properties["Scale"];
            if (properties.ContainsKey("MarkText"))
                MarkText = (string)properties["MarkText"];
            if (properties.ContainsKey("ShowText"))
                ShowText = (bool)properties["ShowText"];
            if (properties.ContainsKey("TextOffset"))
                TextOffset = (Vector2)properties["TextOffset"];
            if (properties.ContainsKey("Rotation"))
                Rotation = (double)properties["Rotation"];
            if (properties.ContainsKey("IsVisible"))
                IsVisible = (bool)properties["IsVisible"];
            if (properties.ContainsKey("DecentrationTolerance"))
                DecentrationTolerance = (double)properties["DecentrationTolerance"];
            if (properties.ContainsKey("TiltTolerance"))
                TiltTolerance = (double)properties["TiltTolerance"];
            if (properties.ContainsKey("ReferenceSurface"))
                ReferenceSurface = (string)properties["ReferenceSurface"];
            if (properties.ContainsKey("TestWavelength"))
                TestWavelength = (double)properties["TestWavelength"];
            if (properties.ContainsKey("ToleranceType"))
                ToleranceType = (CenteringToleranceType)properties["ToleranceType"];
            if (properties.ContainsKey("FrameSize"))
                FrameSize = (Vector2)properties["FrameSize"];
            if (properties.ContainsKey("Standard"))
                Standard = (string)properties["Standard"];

            UpdateMarkText();
        }

        /// <summary>
        /// 克隆标记
        /// </summary>
        IOpticalMark IOpticalMark.Clone()
        {
            return Clone() as CenteringToleranceMark;
        }

        /// <summary>
        /// 获取标记描述
        /// </summary>
        public string GetDescription()
        {
            return $"ISO 10110-6 中心公差标记 - 偏心: ≤{DecentrationTolerance:F3}mm, 倾斜: ≤{TiltTolerance:F3}' (面{ReferenceSurface})";
        }

        #endregion

        #region Entity 重写方法

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new CenteringToleranceMark();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            CenteringToleranceMark mark = base.Clone() as CenteringToleranceMark;
            mark.Position = Position;
            mark.Scale = Scale;
            mark.MarkText = MarkText;
            mark.ShowText = ShowText;
            mark.TextOffset = TextOffset;
            mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.DecentrationTolerance = DecentrationTolerance;
            mark.TiltTolerance = TiltTolerance;
            mark.ReferenceSurface = ReferenceSurface;
            mark.TestWavelength = TestWavelength;
            mark.ToleranceType = ToleranceType;
            mark.FrameSize = FrameSize;
            mark.Standard = Standard;
            mark._markEntities = new List<Entity>();

            return mark;
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            Position += translation;
            _markEntities.Clear();
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            Position = Vector2.RotateInRadian(Position, center, angle);
            Rotation += angle * 180 / Math.PI;
            _markEntities.Clear();
        }

        /// <summary>
        /// Transform
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            Position = transform * Position;
            Vector2 scaleVector = new Vector2(Scale, 0);
            Scale = (transform * scaleVector - transform * new Vector2(0, 0)).length;
            _markEntities.Clear();
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            gripPoints.Add(new GripPoint(GripPointType.Center, Position));

            // 添加框架角点
            double halfWidth = FrameSize.X * 0.5 * Scale;
            double halfHeight = FrameSize.Y * 0.5 * Scale;
            gripPoints.Add(new GripPoint(GripPointType.Corner, Position + new Vector2(halfWidth, halfHeight)));

            if (ShowText)
            {
                var textPos = Position + TextOffset;
                gripPoints.Add(new GripPoint(GripPointType.Center, textPos));
            }

            return gripPoints;
        }

        /// <summary>
        /// 对象捕捉点?        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPnts = new List<ObjectSnapPoint>();

            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Center, Position));

            double halfWidth = FrameSize.X * 0.5 * Scale;
            double halfHeight = FrameSize.Y * 0.5 * Scale;
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Corner, Position + new Vector2(halfWidth, halfHeight)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Corner, Position + new Vector2(-halfWidth, halfHeight)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Corner, Position + new Vector2(halfWidth, -halfHeight)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Corner, Position + new Vector2(-halfWidth, -halfHeight)));

            return snapPnts;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            switch (index)
            {
                case 0: // 中心点
                    Position = newPosition;
                    break;
                case 1: // 框架大小调整点
                    var delta = newPosition - Position;
                    FrameSize = new Vector2(Math.Abs(delta.X) * 2 / Scale, Math.Abs(delta.Y) * 2 / Scale);
                    break;
                case 2: // 文本位置点
                    if (ShowText)
                    {
                        TextOffset = newPosition - Position;
                    }
                    break;
            }
            _markEntities.Clear();
        }

        #endregion
    }

    /// <summary>
    /// 中心公差类型枚举
    /// </summary>
    public enum CenteringToleranceType
    {
        /// <summary>
        /// 偏心和倾斜
        /// </summary>
        DecentrationAndTilt = 0,
        
        /// <summary>
        /// 仅偏?        /// </summary>
        DecentrationOnly = 1,
        
        /// <summary>
        /// 仅倾斜
        /// </summary>
        TiltOnly = 2,
        
        /// <summary>
        /// 圆跳?        /// </summary>
        CircularRunout = 3
    }
}