using lcdb.Colors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using LitMath;
using lcdb;
using lcdb.Interfaces;
using lcdb.Symbols;
using OtoCAD;

namespace lcdb.Annotation
{
    /// <summary>
    /// 表面质量标准枚举
    /// </summary>
    public enum SurfaceQualityStandard
    {
        /// <summary>
        /// ISO 10110-7 表面缺陷标准
        /// 格式: 5/N×A; C×B; L
        /// </summary>
        ISO_10110_7 = 0,

        /// <summary>
        /// MIL-PRF-13830 美军光学规范
        /// 格式: Scratch-Dig (如 60-40)
        /// </summary>
        MIL_PRF_13830 = 1
    }

    /// <summary>
    /// 表面质量标记实现
    /// 支持 ISO 10110-7 和 MIL-PRF-13830 双标准
    /// ISO格式: 5/N×A; C×B; L (表面缺陷数×尺寸; 刮痕数×宽度; 长刮痕)
    /// MIL格式: Scratch-Dig (划痕-麻点)
    /// </summary>
    public class SurfaceQualityMark : Entity, IOpticalMark, ISurfaceAttachable
    {
        public override string className => "SurfaceQualityMark";

        #region IOpticalMark 接口属性
        /// <summary>
        /// 标记类型
        /// </summary>
        public OpticalMarkType MarkType => OpticalMarkType.SurfaceQuality;

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
        public string MarkText { get; set; } = "5/5×0.16";

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

        #region 质量标准选择
        /// <summary>
        /// 表面质量标准（ISO或MIL）
        /// </summary>
        public SurfaceQualityStandard QualityStandard { get; set; } = SurfaceQualityStandard.ISO_10110_7;

        #endregion

        #region MIL-PRF-13830 参数
        /// <summary>
        /// 划痕等级（MIL格式，如60）
        /// </summary>
        public string ScratchGrade { get; set; } = "60";

        /// <summary>
        /// 麻点等级（MIL格式，如40）
        /// </summary>
        public string DigGrade { get; set; } = "40";

        #endregion

        #region ISO 10110-7 参数
        /// <summary>
        /// 表面缺陷数量 N（ISO格式）
        /// </summary>
        public int DefectCount { get; set; } = 5;

        /// <summary>
        /// 表面缺陷最大尺寸 A (mm)（ISO格式）
        /// </summary>
        public double DefectSize { get; set; } = 0.16;

        /// <summary>
        /// 刮痕数量 C（ISO格式）
        /// </summary>
        public int ScratchCount { get; set; } = 0;

        /// <summary>
        /// 刮痕最大宽度 B (mm)（ISO格式）
        /// </summary>
        public double ScratchWidth { get; set; } = 0.0;

        /// <summary>
        /// 长刮痕标记 L（ISO格式）
        /// </summary>
        public bool HasLongScratch { get; set; } = false;

        /// <summary>
        /// 刮痕总长度限制 (mm)（ISO格式，可选）
        /// </summary>
        public double TotalScratchLength { get; set; } = 0.0;

        #endregion

        #region 表面质量通用属性
        /// <summary>
        /// 检测标准描述
        /// </summary>
        public string Standard { get; set; } = "ISO 10110-7";

        /// <summary>
        /// 标记形状样式
        /// </summary>
        public SurfaceQualityMarkStyle MarkStyle { get; set; } = SurfaceQualityMarkStyle.Rectangle;

        /// <summary>
        /// 框架大小
        /// </summary>
        public Vector2 FrameSize { get; set; } = new Vector2(40, 20);

        #endregion

        private List<Entity> _markEntities = new List<Entity>();

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                double totalWidth = FrameSize.X + (ShowText ? Math.Abs(TextOffset.X) + 50 : 0);
                double totalHeight = FrameSize.Y + (ShowText ? Math.Abs(TextOffset.Y) + 15 : 0);
                return new Bounding(Position, totalWidth, totalHeight);
            }
        }

        #region 构造函数
        /// <summary>
        /// 默认构造函数（默认ISO标准）
        /// </summary>
        public SurfaceQualityMark()
        {
            QualityStandard = SurfaceQualityStandard.ISO_10110_7;
            UpdateMarkText();
        }

        /// <summary>
        /// MIL标准构造函数
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="scratchGrade">划痕等级</param>
        /// <param name="digGrade">麻点等级</param>
        public SurfaceQualityMark(Vector2 position, string scratchGrade, string digGrade)
        {
            Position = position;
            QualityStandard = SurfaceQualityStandard.MIL_PRF_13830;
            ScratchGrade = scratchGrade;
            DigGrade = digGrade;
            Standard = "MIL-PRF-13830";
            UpdateMarkText();
        }

        /// <summary>
        /// ISO 10110-7 标准构造函数
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="defectCount">缺陷数量 N</param>
        /// <param name="defectSize">缺陷尺寸 A (mm)</param>
        /// <param name="scratchCount">刮痕数量 C</param>
        /// <param name="scratchWidth">刮痕宽度 B (mm)</param>
        /// <param name="hasLongScratch">是否有长刮痕 L</param>
        public SurfaceQualityMark(Vector2 position, int defectCount, double defectSize,
            int scratchCount = 0, double scratchWidth = 0.0, bool hasLongScratch = false)
        {
            Position = position;
            QualityStandard = SurfaceQualityStandard.ISO_10110_7;
            DefectCount = defectCount;
            DefectSize = defectSize;
            ScratchCount = scratchCount;
            ScratchWidth = scratchWidth;
            HasLongScratch = hasLongScratch;
            Standard = "ISO 10110-7";
            UpdateMarkText();
        }

        #endregion

        #region 核心方法

        /// <summary>
        /// 更新标记文本（根据当前标准）
        /// </summary>
        private void UpdateMarkText()
        {
            if (QualityStandard == SurfaceQualityStandard.MIL_PRF_13830)
            {
                UpdateMILMarkText();
            }
            else
            {
                UpdateISOMarkText();
            }
        }

        /// <summary>
        /// 更新MIL格式标记文本
        /// </summary>
        private void UpdateMILMarkText()
        {
            if (!string.IsNullOrEmpty(ScratchGrade) && !string.IsNullOrEmpty(DigGrade))
            {
                MarkText = $"{ScratchGrade}-{DigGrade}";
            }
        }

        /// <summary>
        /// 更新ISO 10110-7格式标记文本
        /// 格式: 5/N×A; C×B; L
        /// </summary>
        private void UpdateISOMarkText()
        {
            var sb = new StringBuilder();
            sb.Append("5/");  // ISO 10110-7 代码前缀

            // 主缺陷部分: N×A
            sb.AppendFormat("{0}×{1:F2}", DefectCount, DefectSize);

            // 刮痕部分: C×B (如有)
            if (ScratchCount > 0 && ScratchWidth > 0)
            {
                sb.AppendFormat("; {0}×{1:F2}", ScratchCount, ScratchWidth);
            }

            // 长刮痕标记: L (如有)
            if (HasLongScratch)
            {
                sb.Append("; L");
                if (TotalScratchLength > 0)
                {
                    sb.AppendFormat("{0:F1}", TotalScratchLength);
                }
            }

            MarkText = sb.ToString();
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
        /// 生成主标记符号?        /// </summary>
        private void GenerateMainSymbol()
        {
            var markColor = GetMarkColor();
            double halfWidth = FrameSize.X * 0.5 * Scale;
            double halfHeight = FrameSize.Y * 0.5 * Scale;

            switch (MarkStyle)
            {
                case SurfaceQualityMarkStyle.Rectangle:
                    GenerateRectangleSymbol(halfWidth, halfHeight, markColor);
                    break;
                case SurfaceQualityMarkStyle.Circle:
                    GenerateCircleSymbol(Math.Max(halfWidth, halfHeight), markColor);
                    break;
                case SurfaceQualityMarkStyle.Diamond:
                    GenerateDiamondSymbol(halfWidth, halfHeight, markColor);
                    break;
            }

            // 在符号内部添加表面质量文本
            GenerateInnerText(markColor);
        }

        /// <summary>
        /// 生成矩形符号
        /// </summary>
        private void GenerateRectangleSymbol(double halfWidth, double halfHeight, lcdb.Colors.Color color)
        {
            var p1 = new Vector2(Position.X - halfWidth, Position.Y - halfHeight);
            var p2 = new Vector2(Position.X + halfWidth, Position.Y - halfHeight);
            var p3 = new Vector2(Position.X + halfWidth, Position.Y + halfHeight);
            var p4 = new Vector2(Position.X - halfWidth, Position.Y + halfHeight);

            // 应用旋转
            if (Rotation != 0)
            {
                double radians = Rotation * Math.PI / 180;
                p1 = Vector2.RotateInRadian(p1, Position, radians);
                p2 = Vector2.RotateInRadian(p2, Position, radians);
                p3 = Vector2.RotateInRadian(p3, Position, radians);
                p4 = Vector2.RotateInRadian(p4, Position, radians);
            }

            _markEntities.Add(new Line(p1, p2) { color = color });
            _markEntities.Add(new Line(p2, p3) { color = color });
            _markEntities.Add(new Line(p3, p4) { color = color });
            _markEntities.Add(new Line(p4, p1) { color = color });
        }

        /// <summary>
        /// 生成圆形符号
        /// </summary>
        private void GenerateCircleSymbol(double radius, lcdb.Colors.Color color)
        {
            var circle = new Circle
            {
                center = Position,
                radius = radius,
                color = color
            };
            _markEntities.Add(circle);
        }

        /// <summary>
        /// 生成菱形符号
        /// </summary>
        private void GenerateDiamondSymbol(double halfWidth, double halfHeight, lcdb.Colors.Color color)
        {
            var p1 = new Vector2(Position.X, Position.Y + halfHeight);      // 上
            var p2 = new Vector2(Position.X + halfWidth, Position.Y);       // 右
            var p3 = new Vector2(Position.X, Position.Y - halfHeight);      // 下
            var p4 = new Vector2(Position.X - halfWidth, Position.Y);       // 左
            // 应用旋转
            if (Rotation != 0)
            {
                double radians = Rotation * Math.PI / 180;
                p1 = Vector2.RotateInRadian(p1, Position, radians);
                p2 = Vector2.RotateInRadian(p2, Position, radians);
                p3 = Vector2.RotateInRadian(p3, Position, radians);
                p4 = Vector2.RotateInRadian(p4, Position, radians);
            }

            _markEntities.Add(new Line(p1, p2) { color = color });
            _markEntities.Add(new Line(p2, p3) { color = color });
            _markEntities.Add(new Line(p3, p4) { color = color });
            _markEntities.Add(new Line(p4, p1) { color = color });
        }

        /// <summary>
        /// 生成符号内部文本
        /// </summary>
        private void GenerateInnerText(lcdb.Colors.Color color)
        {
            var text = new Text();
            text.Value = MarkText;
            text.Position = new Vector3(Position.X, Position.Y, 0.0);
            text.Height = 4 * Scale;
            text.color = color;
            text.alignment = lcdb.TextAlignment.CenterMiddle;
            _markEntities.Add(text);
        }

        /// <summary>
        /// MIL-PRF-13830 划痕-麻点 (S-D): 框内 "S-D" 即完整规范, 外部仅注 "S/D" 单位说明.
        /// </summary>
        private void GenerateText()
        {
            var textPosition = Position + TextOffset;
            _markEntities.Add(new Text
            {
                Value = "Scratch-Dig",
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
            // 根据表面质量等级选择颜色
            if (int.TryParse(ScratchGrade, out int scratchValue))
            {
                if (scratchValue <= 20)
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Green);      // 高质量?- 绿色
                else if (scratchValue <= 60)
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Orange);     // 中等质量 - 橙色
                else
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Red);        // 低质量?- 红色
            }
            return lcdb.Colors.Color.FromColor(System.Drawing.Color.Blue);               // 默认 - 蓝色
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
            OpticalMarkSymbols.DrawSurfaceQualityMark(g, position, ScratchGrade, DigGrade, scale * (float)Scale);
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
            if (QualityStandard == SurfaceQualityStandard.MIL_PRF_13830)
            {
                return ValidateMIL();
            }
            else
            {
                return ValidateISO();
            }
        }

        /// <summary>
        /// 验证MIL格式数据
        /// </summary>
        private bool ValidateMIL()
        {
            if (string.IsNullOrEmpty(ScratchGrade) || string.IsNullOrEmpty(DigGrade))
                return false;

            // 验证划痕等级格式
            if (!int.TryParse(ScratchGrade, out int scratch) || scratch < 0 || scratch > 999)
                return false;

            // 验证麻点等级格式
            if (!int.TryParse(DigGrade, out int dig) || dig < 0 || dig > 999)
                return false;

            return true;
        }

        /// <summary>
        /// 验证ISO 10110-7格式数据
        /// </summary>
        private bool ValidateISO()
        {
            // 缺陷数量必须>=0
            if (DefectCount < 0)
                return false;

            // 缺陷尺寸必须>=0
            if (DefectSize < 0)
                return false;

            // 刮痕数量必须>=0
            if (ScratchCount < 0)
                return false;

            // 刮痕宽度必须>=0
            if (ScratchWidth < 0)
                return false;

            // 如果有刮痕数量，必须有刮痕宽度
            if (ScratchCount > 0 && ScratchWidth <= 0)
                return false;

            // 总长度必须>=0
            if (TotalScratchLength < 0)
                return false;

            return true;
        }

        /// <summary>
        /// 获取标记属性
        /// </summary>
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
                { "QualityStandard", QualityStandard },
                // MIL参数
                { "ScratchGrade", ScratchGrade },
                { "DigGrade", DigGrade },
                // ISO参数
                { "DefectCount", DefectCount },
                { "DefectSize", DefectSize },
                { "ScratchCount", ScratchCount },
                { "ScratchWidth", ScratchWidth },
                { "HasLongScratch", HasLongScratch },
                { "TotalScratchLength", TotalScratchLength },
                // 通用参数
                { "Standard", Standard },
                { "MarkStyle", MarkStyle },
                { "FrameSize", FrameSize }
            };
        }

        /// <summary>
        /// 设置标记属性
        /// </summary>
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
            if (properties.ContainsKey("QualityStandard"))
                QualityStandard = (SurfaceQualityStandard)properties["QualityStandard"];
            // MIL参数
            if (properties.ContainsKey("ScratchGrade"))
                ScratchGrade = (string)properties["ScratchGrade"];
            if (properties.ContainsKey("DigGrade"))
                DigGrade = (string)properties["DigGrade"];
            // ISO参数
            if (properties.ContainsKey("DefectCount"))
                DefectCount = Convert.ToInt32(properties["DefectCount"]);
            if (properties.ContainsKey("DefectSize"))
                DefectSize = Convert.ToDouble(properties["DefectSize"]);
            if (properties.ContainsKey("ScratchCount"))
                ScratchCount = Convert.ToInt32(properties["ScratchCount"]);
            if (properties.ContainsKey("ScratchWidth"))
                ScratchWidth = Convert.ToDouble(properties["ScratchWidth"]);
            if (properties.ContainsKey("HasLongScratch"))
                HasLongScratch = (bool)properties["HasLongScratch"];
            if (properties.ContainsKey("TotalScratchLength"))
                TotalScratchLength = Convert.ToDouble(properties["TotalScratchLength"]);
            // 通用参数
            if (properties.ContainsKey("Standard"))
                Standard = (string)properties["Standard"];
            if (properties.ContainsKey("MarkStyle"))
                MarkStyle = (SurfaceQualityMarkStyle)properties["MarkStyle"];
            if (properties.ContainsKey("FrameSize"))
                FrameSize = (Vector2)properties["FrameSize"];

            UpdateMarkText();
            _markEntities.Clear();
        }

        /// <summary>
        /// 克隆标记
        /// </summary>
        IOpticalMark IOpticalMark.Clone()
        {
            return Clone() as SurfaceQualityMark;
        }

        /// <summary>
        /// 获取标记描述
        /// </summary>
        public string GetDescription()
        {
            if (QualityStandard == SurfaceQualityStandard.MIL_PRF_13830)
            {
                return $"表面质量标记 - 划痕: {ScratchGrade}, 麻点: {DigGrade} (MIL-PRF-13830)";
            }
            else
            {
                return $"表面质量标记 - {MarkText} (ISO 10110-7)";
            }
        }

        #endregion

        #region Entity 重写方法

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new SurfaceQualityMark();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            SurfaceQualityMark mark = base.Clone() as SurfaceQualityMark;
            mark.Position = Position;
            mark.Scale = Scale;
            mark.MarkText = MarkText;
            mark.ShowText = ShowText;
            mark.TextOffset = TextOffset;
            mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.QualityStandard = QualityStandard;
            // MIL参数
            mark.ScratchGrade = ScratchGrade;
            mark.DigGrade = DigGrade;
            // ISO参数
            mark.DefectCount = DefectCount;
            mark.DefectSize = DefectSize;
            mark.ScratchCount = ScratchCount;
            mark.ScratchWidth = ScratchWidth;
            mark.HasLongScratch = HasLongScratch;
            mark.TotalScratchLength = TotalScratchLength;
            // 通用参数
            mark.Standard = Standard;
            mark.MarkStyle = MarkStyle;
            mark.FrameSize = FrameSize;
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
            // 根据变换矩阵调整大小
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
            
            // 中心捕捉点
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Center, Position));
            
            // 框架的四个角点
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
    /// 表面质量标记样式枚举
    /// </summary>
    public enum SurfaceQualityMarkStyle
    {
        /// <summary>
        /// 矩形性        /// </summary>
        Rectangle = 0,
        
        /// <summary>
        /// 圆形性        /// </summary>
        Circle = 1,
        
        /// <summary>
        /// 菱形性        /// </summary>
        Diamond = 2
    }
}