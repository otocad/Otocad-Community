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
    /// ISO 10110-5 面形公差标记实现
    /// 基于Zernike系数描述光学表面的面形公性    /// </summary>
    public class SurfaceFormMark : Entity, IOpticalMark, ISurfaceAttachable
    {
        public override string className => "SurfaceFormMark";

        #region IOpticalMark 接口属性?
        /// <summary>
        /// 标记类型
        /// </summary>
        public OpticalMarkType MarkType => OpticalMarkType.ISO10110_5;

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
        public string MarkText { get; set; } = "5/2,3:-0.05";

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

        #region ISO 10110-5 特定属性?
        /// <summary>
        /// Zernike项序号（性,3表示性级第3项）
        /// </summary>
        public string ZernikeIndex { get; set; } = "2,3";

        /// <summary>
        /// 公差值（λ为单位）
        /// </summary>
        public double ToleranceValue { get; set; } = -0.05;

        /// <summary>
        /// 波长（nm性        /// </summary>
        public double Wavelength { get; set; } = 632.8;

        /// <summary>
        /// 测试孔径直径（mm性        /// </summary>
        public double TestAperture { get; set; } = 50.0;

        /// <summary>
        /// 面形类型
        /// </summary>
        public ISO10110SurfaceFormType FormType { get; set; } = ISO10110SurfaceFormType.ZernikeCoefficient;

        /// <summary>
        /// 标记框架大小
        /// </summary>
        public Vector2 FrameSize { get; set; } = new Vector2(60, 25);

        /// <summary>
        /// 检测标准?        /// </summary>
        public string Standard { get; set; } = "ISO 10110-5";

        #endregion

        private List<Entity> _markEntities = new List<Entity>();

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                double totalWidth = FrameSize.X + (ShowText ? Math.Abs(TextOffset.X) + 80 : 0);
                double totalHeight = FrameSize.Y + (ShowText ? Math.Abs(TextOffset.Y) + 15 : 0);
                return new Bounding(Position, totalWidth, totalHeight);
            }
        }

        #region 构造函数?
        /// <summary>
        /// 默认构造函数?        /// </summary>
        public SurfaceFormMark()
        {
            UpdateMarkText();
        }

        /// <summary>
        /// 带参数构造函数?        /// </summary>
        public SurfaceFormMark(Vector2 position, string zernikeIndex, double toleranceValue)
        {
            Position = position;
            ZernikeIndex = zernikeIndex;
            ToleranceValue = toleranceValue;
            UpdateMarkText();
        }

        #endregion

        #region 核心方法

        /// <summary>
        /// 更新标记文本
        /// </summary>
        private void UpdateMarkText()
        {
            MarkText = $"5/{ZernikeIndex}:{ToleranceValue:F3}";
        }

        /// <summary>
        /// 生成标记图形
        /// </summary>
        protected void Generate()
        {
            _markEntities.Clear();

            // 生成主标记符号
            GenerateMainSymbol();

            // 生成文本
            if (ShowText)
            {
                GenerateText();
            }
        }

        /// <summary>
        /// ISO 10110-5 表面形状公差 (Zernike 项规范): 矩形框 + 居中代码 "5/index:tolerance".
        /// 旧实现 (双线框 + 分行 "5" / 参数) 不符合 ISO 严格格式, 已移除.
        /// 注: ISO 10110-5 第 8 章 Zernike 项格式: 5/i,j,...:T (i,j... 为 Zernike 索引, T 公差波数).
        /// </summary>
        private void GenerateMainSymbol()
        {
            var markColor = GetMarkColor();
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
                _markEntities.Add(new Line(pts[i], pts[(i + 1) % 4]) { color = markColor });
            }

            // ISO 10110-5 严格代码 "5/index:tolerance"
            string codeText = !string.IsNullOrEmpty(ZernikeIndex)
                ? $"5/{ZernikeIndex}:{ToleranceValue:G3}"
                : "5/—";

            _markEntities.Add(new Text
            {
                Value = codeText,
                Position = new Vector3(Position.X, Position.Y, 0.0),
                Height = halfHeight * 0.5,
                color = markColor,
                alignment = lcdb.TextAlignment.CenterMiddle,
            });
        }

        /// <summary>
        /// ISO 10110-5: 框内代码即完整规范, 外部只注测试条件 (λ + 孔径).
        /// </summary>
        private void GenerateText()
        {
            var textPosition = Position + TextOffset;
            _markEntities.Add(new Text
            {
                Value = $"λ={Wavelength:F1}nm, Φ={TestAperture:F0}mm",
                Position = new Vector3(textPosition.X, textPosition.Y, 0.0),
                Height = 4 * Scale,
                color = GetMarkColor(),
                alignment = lcdb.TextAlignment.CenterMiddle,
            });
        }

        /// <summary>
        /// 贴面放置: 代码框保持水平可读 (不旋转), 仅把框移到被测面外侧、用户悬停的那一侧。
        /// (ISO 标准的"引线连接"画法是另一个更大的特性, 本期先做"贴边摆放"。)
        /// </summary>
        public void AttachToSurface(Vector2 surfacePoint, Vector2 outwardNormal)
        {
            // 框保持水平: 不改 Rotation。沿外法线把锚点外移, 使框紧贴面外侧而不压在线上。
            // 偏移量取框高的 0.7 倍: 框半高为 FrameSize.Y*0.5, 再留约 0.2 倍框高的间隙, 使框近边离面一个小缝。
            Position = surfacePoint + outwardNormal * (FrameSize.Y * 0.7 * Scale);
            _markEntities.Clear();
        }

        /// <summary>
        /// 获取标记颜色
        /// </summary>
        private lcdb.Colors.Color GetMarkColor()
        {
            // 根据公差严格程度选择颜色
            if (Math.Abs(ToleranceValue) <= 0.02)
                return lcdb.Colors.Color.FromColor(System.Drawing.Color.Red);        // 高精度?- 红色
            else if (Math.Abs(ToleranceValue) <= 0.1)
                return lcdb.Colors.Color.FromColor(System.Drawing.Color.Orange);     // 中等精度 - 橙色
            else
                return lcdb.Colors.Color.FromColor(System.Drawing.Color.Blue);       // 一般精度?- 蓝色
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
            // 绘制ISO 10110-5标记符号
            DrawISO10110_5Symbol(g, position, scale * (float)Scale);
        }

        /// <summary>
        /// 绘制ISO 10110-5符号
        /// </summary>
        private void DrawISO10110_5Symbol(Graphics g, PointF position, float scale)
        {
            using (var pen = new Pen(System.Drawing.Color.Blue, 1.5f * scale))
            using (var brush = new SolidBrush(System.Drawing.Color.Blue))
            using (var font = new Font("Arial", 8 * scale, System.Drawing.FontStyle.Bold))
            {
                float width = (float)FrameSize.X * scale;
                float height = (float)FrameSize.Y * scale;

                // 绘制双线框架
                RectangleF outerRect = new RectangleF(position.X - width/2, position.Y - height/2, width, height);
                RectangleF innerRect = new RectangleF(position.X - width/2 + 2*scale, position.Y - height/2 + 2*scale, 
                                                     width - 4*scale, height - 4*scale);
                
                g.DrawRectangle(pen, Rectangle.Round(outerRect));
                g.DrawRectangle(pen, Rectangle.Round(innerRect));

                // 绘制标识和参数
                string displayText = $"5\n{ZernikeIndex}\n{ToleranceValue:F3}";
                g.DrawString(displayText, font, brush, position.X, position.Y, 
                           new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }
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
            if (string.IsNullOrEmpty(ZernikeIndex))
                return false;

            // 验证Zernike索引格式
            var parts = ZernikeIndex.Split(',');
            if (parts.Length != 2)
                return false;

            if (!int.TryParse(parts[0], out int n) || !int.TryParse(parts[1], out int m))
                return false;

            // 验证Zernike系数的有效性（n >= 0, |m| <= n, n-m为偶数）
            if (n < 0 || Math.Abs(m) > n || (n - Math.Abs(m)) % 2 != 0)
                return false;

            // 验证公差值范性            if (Math.Abs(ToleranceValue) > 10.0)
                return false;

            return true;
        }

        /// <summary>
        /// 获取标记属性?        /// </summary>
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
                { "ZernikeIndex", ZernikeIndex },
                { "ToleranceValue", ToleranceValue },
                { "Wavelength", Wavelength },
                { "TestAperture", TestAperture },
                { "FormType", FormType },
                { "FrameSize", FrameSize },
                { "Standard", Standard }
            };
        }

        /// <summary>
        /// 设置标记属性?        /// </summary>
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
            if (properties.ContainsKey("ZernikeIndex"))
                ZernikeIndex = (string)properties["ZernikeIndex"];
            if (properties.ContainsKey("ToleranceValue"))
                ToleranceValue = (double)properties["ToleranceValue"];
            if (properties.ContainsKey("Wavelength"))
                Wavelength = (double)properties["Wavelength"];
            if (properties.ContainsKey("TestAperture"))
                TestAperture = (double)properties["TestAperture"];
            if (properties.ContainsKey("FormType"))
                FormType = (ISO10110SurfaceFormType)properties["FormType"];
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
            return Clone() as SurfaceFormMark;
        }

        /// <summary>
        /// 获取标记描述
        /// </summary>
        public string GetDescription()
        {
            return $"ISO 10110-5 面形公差标记 - Zernike({ZernikeIndex}): {ToleranceValue:F3}λ @{Wavelength}nm";
        }

        #endregion

        #region Entity 重写方法

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new SurfaceFormMark();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            SurfaceFormMark mark = base.Clone() as SurfaceFormMark;
            mark.Position = Position;
            mark.Scale = Scale;
            mark.MarkText = MarkText;
            mark.ShowText = ShowText;
            mark.TextOffset = TextOffset;
            mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.ZernikeIndex = ZernikeIndex;
            mark.ToleranceValue = ToleranceValue;
            mark.Wavelength = Wavelength;
            mark.TestAperture = TestAperture;
            mark.FormType = FormType;
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
            Scale = (transform * scaleVector).length;
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
                    FrameSize = new Vector2(Math.Abs(delta.X) * 2, Math.Abs(delta.Y) * 2);
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
    /// ISO 10110 表面形状类型枚举
    /// </summary>
    public enum ISO10110SurfaceFormType
    {
        /// <summary>
        /// Zernike系数
        /// </summary>
        ZernikeCoefficient = 0,
        
        /// <summary>
        /// 功率/不规则度
        /// </summary>
        PowerIrregularity = 1,
        
        /// <summary>
        /// RMS波面误差
        /// </summary>
        RMSWavefront = 2,
        
        /// <summary>
        /// PV波面误差
        /// </summary>
        PVWavefront = 3
    }
}