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
    /// ISO 10110-8 表面纹理标记实现
    /// 用于标注光学表面的粗糙度和纹理要?    /// </summary>
    public class SurfaceTextureMark : Entity, IOpticalMark, ISurfaceAttachable
    {
        public override string className => "SurfaceTextureMark";

        #region IOpticalMark 接口属性?
        /// <summary>
        /// 标记类型
        /// </summary>
        public OpticalMarkType MarkType => OpticalMarkType.ISO10110_8;

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
        public string MarkText { get; set; } = "8/Ra0.8-Rz6.3";

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

        #region ISO 10110-8 特定属性?
        /// <summary>
        /// 平均粗糙度Ra值（μm?        /// </summary>
        public double RaValue { get; set; } = 0.8;

        /// <summary>
        /// 最大高度差Rz值（μm?        /// </summary>
        public double RzValue { get; set; } = 6.3;

        /// <summary>
        /// 均方根粗糙度Rq值（μm?        /// </summary>
        public double RqValue { get; set; } = 0.0; // 0表示不指
        /// <summary>
        /// 轮廓支承长度比Rmr值（%?        /// </summary>
        public double RmrValue { get; set; } = 0.0; // 0表示不指
        /// <summary>
        /// 表面纹理类型
        /// </summary>
        public SurfaceTextureType TextureType { get; set; } = SurfaceTextureType.Polished;

        /// <summary>
        /// 纹理方向
        /// </summary>
        public TextureDirection Direction { get; set; } = TextureDirection.Any;

        /// <summary>
        /// 测量长度（mm?        /// </summary>
        public double MeasurementLength { get; set; } = 0.8;

        /// <summary>
        /// 评定长度（mm?        /// </summary>
        public double EvaluationLength { get; set; } = 4.0;

        /// <summary>
        /// 标记框架大小
        /// </summary>
        public Vector2 FrameSize { get; set; } = new Vector2(60, 30);

        /// <summary>
        /// 检测标准
        /// </summary>
        public string Standard { get; set; } = "ISO 10110-8";

        #endregion

        #region GB/T 13323 附录C 表面结构属性

        /// <summary>
        /// GB/T 13323 表面结构类型 (G/P/P1-P4)
        /// </summary>
        public GB13323SurfaceType GB13323Type { get; set; } = GB13323SurfaceType.Polished_P;

        /// <summary>
        /// 是否使用GB/T 13323格式（否则使用ISO 10110-8格式）
        /// </summary>
        public bool UseGB13323Format { get; set; } = false;

        /// <summary>
        /// 斜率取样长度 (mm) - GB/T 13323 附录C
        /// </summary>
        public double SlopeSampleLength { get; set; } = 0.002;

        /// <summary>
        /// 微缺陷数量参数 (每10mm轮廓微缺陷数)
        /// </summary>
        public int MicrodefectCount { get; set; } = 1;

        #endregion

        private List<Entity> _markEntities = new List<Entity>();

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                double totalWidth = FrameSize.X + (ShowText ? Math.Abs(TextOffset.X) + 120 : 0);
                double totalHeight = FrameSize.Y + (ShowText ? Math.Abs(TextOffset.Y) + 15 : 0);
                return new Bounding(Position, totalWidth, totalHeight);
            }
        }

        #region 构造函数?
        /// <summary>
        /// 默认构造函数?        /// </summary>
        public SurfaceTextureMark()
        {
            UpdateMarkText();
        }

        /// <summary>
        /// 带参数构造函数?        /// </summary>
        public SurfaceTextureMark(Vector2 position, double raValue, double rzValue)
        {
            Position = position;
            RaValue = raValue;
            RzValue = rzValue;
            UpdateMarkText();
        }

        #endregion

        #region 核心方法

        /// <summary>
        /// 更新标记文本
        /// </summary>
        private void UpdateMarkText()
        {
            if (UseGB13323Format)
            {
                // GB/T 13323 附录C 格式: P3/0.002/1/Rq0.002
                string typeSymbol = GetGB13323TypeSymbol();
                string slopeText = SlopeSampleLength > 0 ? $"/{SlopeSampleLength:F3}" : "";
                string defectText = MicrodefectCount > 0 ? $"/{MicrodefectCount}" : "";
                string rqText = RqValue > 0 ? $"/Rq{RqValue:F3}" : "";

                // G类型格式: G/5/Rq2
                // P类型格式: P 或 P2 或 P3/0.002/1/Rq0.002
                if (GB13323Type == GB13323SurfaceType.Rough_G)
                {
                    MarkText = $"G{slopeText}{rqText}";
                }
                else
                {
                    MarkText = $"{typeSymbol}{slopeText}{defectText}{rqText}";
                }
            }
            else
            {
                // ISO 10110-8 格式: 8/Ra0.8-Rz6.3
                string raText = $"Ra{RaValue:F1}";
                string rzText = RzValue > 0 ? $"-Rz{RzValue:F1}" : "";
                string rqText = RqValue > 0 ? $"-Rq{RqValue:F1}" : "";
                MarkText = $"8/{raText}{rzText}{rqText}";
            }
        }

        /// <summary>
        /// 获取GB/T 13323类型符号
        /// </summary>
        private string GetGB13323TypeSymbol()
        {
            switch (GB13323Type)
            {
                case GB13323SurfaceType.Rough_G: return "G";
                case GB13323SurfaceType.Polished_P: return "P";
                case GB13323SurfaceType.Polished_P1: return "P1";
                case GB13323SurfaceType.Polished_P2: return "P2";
                case GB13323SurfaceType.Polished_P3: return "P3";
                case GB13323SurfaceType.Polished_P4: return "P4";
                default: return "P";
            }
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
        /// GB/T 131-2006 / ISO 1302 表面纹理: 矩形框 + Ra/Rz 居中代码 + 纹理类型副标识.
        /// 旧实现 (圆角装饰 + 4 种纹理子图标 + 散点参数 + 中文详述) 不符合 GB/T 131, 已简化.
        /// 注: 此 mark 替代不了 SurfaceRoughnessMark (后者用 V 形符号), 本 mark 用矩形备注框 (适合 multi-Ra/Rz 表达).
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

            // Ra 主行 + (可选 Rz/Rq) 副行
            string mainLine = RaValue > 0 ? $"Ra {RaValue:G3}" : "Ra —";
            string subParts = "";
            if (RzValue > 0) subParts += $"Rz {RzValue:G3}";
            if (RqValue > 0) { if (subParts.Length > 0) subParts += " "; subParts += $"Rq {RqValue:G3}"; }

            _markEntities.Add(new Text
            {
                Value = mainLine,
                Position = new Vector3(Position.X, Position.Y + halfHeight * 0.3, 0.0),
                Height = halfHeight * 0.4,
                color = markColor,
                alignment = lcdb.TextAlignment.CenterMiddle,
            });

            if (subParts.Length > 0)
            {
                _markEntities.Add(new Text
                {
                    Value = subParts,
                    Position = new Vector3(Position.X, Position.Y - halfHeight * 0.3, 0.0),
                    Height = halfHeight * 0.3,
                    color = markColor,
                    alignment = lcdb.TextAlignment.CenterMiddle,
                });
            }
        }

        /// <summary>
        /// 生成圆角矩形框架
        /// </summary>
        private void GenerateRoundedFrame(double halfWidth, double halfHeight, lcdb.Colors.Color color)
        {
            // 外框
            var outerRect = new[]
            {
                new Vector2(Position.X - halfWidth, Position.Y - halfHeight),
                new Vector2(Position.X + halfWidth, Position.Y - halfHeight),
                new Vector2(Position.X + halfWidth, Position.Y + halfHeight),
                new Vector2(Position.X - halfWidth, Position.Y + halfHeight)
            };

            // 应用旋转
            if (Rotation != 0)
            {
                double radians = Rotation * Math.PI / 180;
                for (int i = 0; i < 4; i++)
                {
                    outerRect[i] = Vector2.RotateInRadian(outerRect[i], Position, radians);
                }
            }

            // 绘制框架
            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                _markEntities.Add(new Line(outerRect[i], outerRect[next]) { color = color });
            }

            // 添加圆角装饰
            double cornerRadius = 2 * Scale;
            for (int i = 0; i < 4; i++)
            {
                var corner = outerRect[i];
                var cornerCircle = new Circle
                {
                    center = corner,
                    radius = cornerRadius,
                    color = color
                };
                _markEntities.Add(cornerCircle);
            }
        }

        /// <summary>
        /// 生成表面纹理符号
        /// </summary>
        private void GenerateTextureSymbol(lcdb.Colors.Color color)
        {
            double symbolSize = 12 * Scale;
            
            // 根据纹理类型生成不同符号
            switch (TextureType)
            {
                case SurfaceTextureType.Polished:
                    GeneratePolishedSymbol(symbolSize, color);
                    break;
                case SurfaceTextureType.Ground:
                    GenerateGroundSymbol(symbolSize, color);
                    break;
                case SurfaceTextureType.Machined:
                    GenerateMachinedSymbol(symbolSize, color);
                    break;
                case SurfaceTextureType.AsFormed:
                    GenerateAsFormedSymbol(symbolSize, color);
                    break;
            }

            // 添加纹理方向指示
            if (Direction != TextureDirection.Any)
            {
                GenerateDirectionSymbol(symbolSize, color);
            }
        }

        /// <summary>
        /// 生成抛光符号
        /// </summary>
        private void GeneratePolishedSymbol(double size, lcdb.Colors.Color color)
        {
            // 光滑波浪线
            int points = 20;
            var wavePoints = new List<Vector2>();
            
            for (int i = 0; i <= points; i++)
            {
                double t = (double)i / points;
                double x = Position.X + (t - 0.5) * size;
                double y = Position.Y + Math.Sin(t * Math.PI * 6) * size * 0.1;
                wavePoints.Add(new Vector2(x, y));
            }

            for (int i = 0; i < wavePoints.Count - 1; i++)
            {
                _markEntities.Add(new Line(wavePoints[i], wavePoints[i + 1]) { color = color });
            }
        }

        /// <summary>
        /// 生成磨削符号
        /// </summary>
        private void GenerateGroundSymbol(double size, lcdb.Colors.Color color)
        {
            // 锯齿
            int teeth = 8;
            var sawPoints = new List<Vector2>();
            
            for (int i = 0; i <= teeth; i++)
            {
                double x1 = Position.X + (i * 2 - teeth) * size / teeth / 2;
                double x2 = Position.X + ((i * 2 + 1) - teeth) * size / teeth / 2;
                double y1 = Position.Y + size * 0.15;
                double y2 = Position.Y - size * 0.15;
                
                if (i < teeth)
                {
                    sawPoints.Add(new Vector2(x1, y1));
                    sawPoints.Add(new Vector2(x2, y2));
                }
            }

            for (int i = 0; i < sawPoints.Count - 1; i++)
            {
                _markEntities.Add(new Line(sawPoints[i], sawPoints[i + 1]) { color = color });
            }
        }

        /// <summary>
        /// 生成加工符号
        /// </summary>
        private void GenerateMachinedSymbol(double size, lcdb.Colors.Color color)
        {
            // 平行线条
            int lines = 5;
            double spacing = size / lines;
            
            for (int i = 0; i < lines; i++)
            {
                double x = Position.X + (i - lines / 2.0) * spacing;
                var line = new Line(
                    new Vector2(x, Position.Y - size * 0.2),
                    new Vector2(x, Position.Y + size * 0.2)
                );
                line.color = color;
                _markEntities.Add(line);
            }
        }

        /// <summary>
        /// 生成成形符号
        /// </summary>
        private void GenerateAsFormedSymbol(double size, lcdb.Colors.Color color)
        {
            // 交叉线条
            var cross1 = new Line(
                new Vector2(Position.X - size * 0.2, Position.Y - size * 0.2),
                new Vector2(Position.X + size * 0.2, Position.Y + size * 0.2)
            );
            var cross2 = new Line(
                new Vector2(Position.X - size * 0.2, Position.Y + size * 0.2),
                new Vector2(Position.X + size * 0.2, Position.Y - size * 0.2)
            );
            cross1.color = color;
            cross2.color = color;
            _markEntities.Add(cross1);
            _markEntities.Add(cross2);
        }

        /// <summary>
        /// 生成方向符号
        /// </summary>
        private void GenerateDirectionSymbol(double size, lcdb.Colors.Color color)
        {
            Vector2 arrowStart = new Vector2(Position.X + size * 0.4, Position.Y);
            Vector2 arrowEnd = arrowStart;

            switch (Direction)
            {
                case TextureDirection.Parallel:
                    arrowEnd = new Vector2(Position.X + size * 0.6, Position.Y);
                    break;
                case TextureDirection.Perpendicular:
                    arrowEnd = new Vector2(Position.X + size * 0.4, Position.Y + size * 0.2);
                    break;
                case TextureDirection.Crossed:
                    // 绘制交叉箭头
                    var arrow1 = new Line(arrowStart, new Vector2(Position.X + size * 0.55, Position.Y + size * 0.15));
                    var arrow2 = new Line(arrowStart, new Vector2(Position.X + size * 0.55, Position.Y - size * 0.15));
                    arrow1.color = color;
                    arrow2.color = color;
                    _markEntities.Add(arrow1);
                    _markEntities.Add(arrow2);
                    return;
                case TextureDirection.Radial:
                    // 绘制放射状符号
                    for (int i = 0; i < 8; i++)
                    {
                        double angle = i * Math.PI / 4;
                        var radialEnd = new Vector2(
                            arrowStart.X + size * 0.15 * Math.Cos(angle),
                            arrowStart.Y + size * 0.15 * Math.Sin(angle)
                        );
                        var radialLine = new Line(arrowStart, radialEnd);
                        radialLine.color = color;
                        _markEntities.Add(radialLine);
                    }
                    return;
            }

            if (arrowEnd != arrowStart)
            {
                var arrow = new Line(arrowStart, arrowEnd);
                arrow.color = color;
                _markEntities.Add(arrow);
            }
        }

        /// <summary>
        /// 生成ISO标识
        /// </summary>
        private void GenerateISOIdentifier(lcdb.Colors.Color color)
        {
            var text = new Text();
            text.Value = "8";
            text.Position = new Vector3(Position.X - FrameSize.X * 0.4, Position.Y + FrameSize.Y * 0.3, 0.0);
            text.Height = 4 * Scale;
            text.color = color;
            text.alignment = lcdb.TextAlignment.CenterMiddle;
            _markEntities.Add(text);
        }

        /// <summary>
        /// 生成粗糙度参数文本?        /// </summary>
        private void GenerateRoughnessParameters(lcdb.Colors.Color color)
        {
            // Ra
            var raText = new Text();
            raText.Value = $"Ra{RaValue:F1}";
            raText.Position = new Vector3(Position.X + FrameSize.X * 0.1, Position.Y + FrameSize.Y * 0.2, 0.0);
            raText.Height = 5 * Scale;
            raText.color = color;
            raText.alignment = lcdb.TextAlignment.CenterMiddle;
            _markEntities.Add(raText);

            // Rz值（如果指定
            if (RzValue > 0)
            {
                var rzText = new Text();
                rzText.Value = $"Rz{RzValue:F1}";
                rzText.Position = new Vector3(Position.X + FrameSize.X * 0.1, Position.Y, 0.0);
                rzText.Height = 5 * Scale;
                rzText.color = color;
                rzText.alignment = lcdb.TextAlignment.CenterMiddle;
                _markEntities.Add(rzText);
            }

            // Rq值（如果指定
            if (RqValue > 0)
            {
                var rqText = new Text();
                rqText.Value = $"Rq{RqValue:F1}";
                rqText.Position = new Vector3(Position.X + FrameSize.X * 0.1, Position.Y - FrameSize.Y * 0.2, 0.0);
                rqText.Height = 5 * Scale;
                rqText.color = color;
                rqText.alignment = lcdb.TextAlignment.CenterMiddle;
                _markEntities.Add(rqText);
            }
        }

        /// <summary>
        /// GB/T 131-2006: 框内代码即完整规范, 外部仅注单位 μm + 纹理类型简称.
        /// </summary>
        private void GenerateText()
        {
            var textPosition = Position + TextOffset;
            _markEntities.Add(new Text
            {
                Value = $"μm ({GetTextureTypeDescription()})",
                Position = new Vector3(textPosition.X, textPosition.Y, 0.0),
                Height = 4 * Scale,
                color = GetMarkColor(),
                alignment = lcdb.TextAlignment.CenterMiddle,
            });
        }

        /// <summary>
        /// 获取详细描述
        /// </summary>
        private string GetDetailedDescription()
        {
            string textureDesc = GetTextureTypeDescription();
            string roughnessDesc = $"Ra{RaValue:F1}μm";
            if (RzValue > 0) roughnessDesc += $", Rz{RzValue:F1}μm";
            if (RqValue > 0) roughnessDesc += $", Rq{RqValue:F1}μm";
            
            return $"表面纹理: {textureDesc} - {roughnessDesc}";
        }

        /// <summary>
        /// 获取纹理类型描述
        /// </summary>
        private string GetTextureTypeDescription()
        {
            switch (TextureType)
            {
                case SurfaceTextureType.Polished: return "抛光";
                case SurfaceTextureType.Ground: return "磨削";
                case SurfaceTextureType.Machined: return "机加工";
                case SurfaceTextureType.AsFormed: return "成形";
                default: return "标准";
            }
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
            // 根据粗糙度等级选择颜色
            if (RaValue <= 0.1)
                return lcdb.Colors.Color.FromColor(System.Drawing.Color.Purple);      // 超精度?- 紫色
            else if (RaValue <= 0.4)
                return lcdb.Colors.Color.FromColor(System.Drawing.Color.Blue);        // 精密 - 蓝色
            else if (RaValue <= 1.0)
                return lcdb.Colors.Color.FromColor(System.Drawing.Color.Green);       // 精细 - 绿色
            else if (RaValue <= 3.2)
                return lcdb.Colors.Color.FromColor(System.Drawing.Color.Orange);      // 中等 - 橙色
            else
                return lcdb.Colors.Color.FromColor(System.Drawing.Color.Red);         // 粗糙 - 红色
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
            DrawISO10110_8Symbol(g, position, scale * (float)Scale);
        }

        /// <summary>
        /// 绘制ISO 10110-8符号
        /// </summary>
        private void DrawISO10110_8Symbol(Graphics g, PointF position, float scale)
        {
            using (var pen = new Pen(GetMarkColor().ToDrawingColor(), 1.5f * scale))
            using (var brush = new SolidBrush(GetMarkColor().ToDrawingColor()))
            using (var font = new Font("Arial", 5 * scale, System.Drawing.FontStyle.Bold))
            {
                float width = (float)FrameSize.X * scale;
                float height = (float)FrameSize.Y * scale;

                // 绘制框架
                RectangleF rect = new RectangleF(position.X - width/2, position.Y - height/2, width, height);
                g.DrawRectangle(pen, Rectangle.Round(rect));

                // 绘制纹理符号
                DrawTexturePattern(g, pen, position, scale);

                // 绘制标识和参
                string identifier = "8";
                string parameters = GetParameterString();
                
                g.DrawString(identifier, font, brush, position.X - width*0.4f, position.Y + height*0.3f);
                g.DrawString(parameters, font, brush, position.X + width*0.1f, position.Y, 
                           new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }
        }

        /// <summary>
        /// 绘制纹理图案
        /// </summary>
        private void DrawTexturePattern(Graphics g, Pen pen, PointF position, float scale)
        {
            float size = 12 * scale;
            
            switch (TextureType)
            {
                case SurfaceTextureType.Polished:
                    // 光滑波浪线
                    for (int i = 0; i < 19; i++)
                    {
                        float t1 = (float)i / 19;
                        float t2 = (float)(i + 1) / 19;
                        float x1 = position.X + (t1 - 0.5f) * size;
                        float x2 = position.X + (t2 - 0.5f) * size;
                        float y1 = position.Y + (float)Math.Sin(t1 * Math.PI * 6) * size * 0.1f;
                        float y2 = position.Y + (float)Math.Sin(t2 * Math.PI * 6) * size * 0.1f;
                        g.DrawLine(pen, x1, y1, x2, y2);
                    }
                    break;
                    
                case SurfaceTextureType.Ground:
                    // 锯齿
                    for (int i = 0; i < 8; i++)
                    {
                        float x1 = position.X + (i * 2 - 8) * size / 16;
                        float x2 = position.X + ((i * 2 + 1) - 8) * size / 16;
                        float x3 = position.X + ((i * 2 + 2) - 8) * size / 16;
                        g.DrawLine(pen, x1, position.Y + size * 0.15f, x2, position.Y - size * 0.15f);
                        if (i < 7)
                            g.DrawLine(pen, x2, position.Y - size * 0.15f, x3, position.Y + size * 0.15f);
                    }
                    break;
                    
                case SurfaceTextureType.Machined:
                    // 平行线条
                    for (int i = 0; i < 5; i++)
                    {
                        float x = position.X + (i - 2) * size / 5;
                        g.DrawLine(pen, x, position.Y - size * 0.2f, x, position.Y + size * 0.2f);
                    }
                    break;
                    
                case SurfaceTextureType.AsFormed:
                    // 交叉线条
                    g.DrawLine(pen, position.X - size * 0.2f, position.Y - size * 0.2f, 
                              position.X + size * 0.2f, position.Y + size * 0.2f);
                    g.DrawLine(pen, position.X - size * 0.2f, position.Y + size * 0.2f, 
                              position.X + size * 0.2f, position.Y - size * 0.2f);
                    break;
            }
        }
#endif

        /// <summary>
        /// 获取参数字符号?        /// </summary>
        private string GetParameterString()
        {
            List<string> parameters = new List<string>();
            parameters.Add($"Ra{RaValue:F1}");
            if (RzValue > 0) parameters.Add($"Rz{RzValue:F1}");
            if (RqValue > 0) parameters.Add($"Rq{RqValue:F1}");
            return string.Join("\n", parameters);
        }

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
            // 验证Ra值范
            if (RaValue < 0 || RaValue > 50.0)
                return false;

            // 验证Rz值范
            if (RzValue < 0 || RzValue > 400.0)
                return false;

            // 验证Rq值范
            if (RqValue < 0 || RqValue > 50.0)
                return false;

            // 验证Rmr值范
            if (RmrValue < 0 || RmrValue > 100.0)
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
                { "RaValue", RaValue },
                { "RzValue", RzValue },
                { "RqValue", RqValue },
                { "RmrValue", RmrValue },
                { "TextureType", TextureType },
                { "Direction", Direction },
                { "MeasurementLength", MeasurementLength },
                { "EvaluationLength", EvaluationLength },
                { "FrameSize", FrameSize },
                { "Standard", Standard },
                // GB/T 13323 属性
                { "GB13323Type", GB13323Type },
                { "UseGB13323Format", UseGB13323Format },
                { "SlopeSampleLength", SlopeSampleLength },
                { "MicrodefectCount", MicrodefectCount }
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
            if (properties.ContainsKey("RaValue"))
                RaValue = (double)properties["RaValue"];
            if (properties.ContainsKey("RzValue"))
                RzValue = (double)properties["RzValue"];
            if (properties.ContainsKey("RqValue"))
                RqValue = (double)properties["RqValue"];
            if (properties.ContainsKey("RmrValue"))
                RmrValue = (double)properties["RmrValue"];
            if (properties.ContainsKey("TextureType"))
                TextureType = (SurfaceTextureType)properties["TextureType"];
            if (properties.ContainsKey("Direction"))
                Direction = (TextureDirection)properties["Direction"];
            if (properties.ContainsKey("MeasurementLength"))
                MeasurementLength = (double)properties["MeasurementLength"];
            if (properties.ContainsKey("EvaluationLength"))
                EvaluationLength = (double)properties["EvaluationLength"];
            if (properties.ContainsKey("FrameSize"))
                FrameSize = (Vector2)properties["FrameSize"];
            if (properties.ContainsKey("Standard"))
                Standard = (string)properties["Standard"];
            // GB/T 13323 属性
            if (properties.ContainsKey("GB13323Type"))
                GB13323Type = (GB13323SurfaceType)properties["GB13323Type"];
            if (properties.ContainsKey("UseGB13323Format"))
                UseGB13323Format = (bool)properties["UseGB13323Format"];
            if (properties.ContainsKey("SlopeSampleLength"))
                SlopeSampleLength = (double)properties["SlopeSampleLength"];
            if (properties.ContainsKey("MicrodefectCount"))
                MicrodefectCount = (int)properties["MicrodefectCount"];

            UpdateMarkText();
        }

        /// <summary>
        /// 克隆标记
        /// </summary>
        IOpticalMark IOpticalMark.Clone()
        {
            return Clone() as SurfaceTextureMark;
        }

        /// <summary>
        /// 获取标记描述
        /// </summary>
        public string GetDescription()
        {
            return $"ISO 10110-8 表面纹理标记 - Ra{RaValue:F1}μm" + (RzValue > 0 ? $", Rz{RzValue:F1}μm" : "") + $" ({GetTextureTypeDescription()})";
        }

        #endregion

        #region Entity 重写方法

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new SurfaceTextureMark();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            SurfaceTextureMark mark = base.Clone() as SurfaceTextureMark;
            mark.Position = Position;
            mark.Scale = Scale;
            mark.MarkText = MarkText;
            mark.ShowText = ShowText;
            mark.TextOffset = TextOffset;
            mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.RaValue = RaValue;
            mark.RzValue = RzValue;
            mark.RqValue = RqValue;
            mark.RmrValue = RmrValue;
            mark.TextureType = TextureType;
            mark.Direction = Direction;
            mark.MeasurementLength = MeasurementLength;
            mark.EvaluationLength = EvaluationLength;
            mark.FrameSize = FrameSize;
            mark.Standard = Standard;
            // GB/T 13323 属性
            mark.GB13323Type = GB13323Type;
            mark.UseGB13323Format = UseGB13323Format;
            mark.SlopeSampleLength = SlopeSampleLength;
            mark.MicrodefectCount = MicrodefectCount;
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
    /// 表面纹理类型枚举
    /// </summary>
    public enum SurfaceTextureType
    {
        /// <summary>
        /// 抛光
        /// </summary>
        Polished = 0,

        /// <summary>
        /// 磨削
        /// </summary>
        Ground = 1,

        /// <summary>
        /// 机加工
        /// </summary>
        Machined = 2,

        /// <summary>
        /// 成形
        /// </summary>
        AsFormed = 3,

        /// <summary>
        /// 标准
        /// </summary>
        Standard = 4
    }

    /// <summary>
    /// GB/T 13323 附录C 表面结构类型
    /// </summary>
    public enum GB13323SurfaceType
    {
        /// <summary>
        /// G - 粗糙表面（研磨）
        /// </summary>
        Rough_G = 0,

        /// <summary>
        /// P - 抛光表面（无微缺陷要求）
        /// </summary>
        Polished_P = 1,

        /// <summary>
        /// P1 - 抛光表面（微缺陷等级1: 80<N<400）
        /// </summary>
        Polished_P1 = 2,

        /// <summary>
        /// P2 - 抛光表面（微缺陷等级2: 16<N<80）
        /// </summary>
        Polished_P2 = 3,

        /// <summary>
        /// P3 - 抛光表面（微缺陷等级3: 3<N<16）
        /// </summary>
        Polished_P3 = 4,

        /// <summary>
        /// P4 - 抛光表面（微缺陷等级4: N<3）
        /// </summary>
        Polished_P4 = 5
    }

    /// <summary>
    /// 纹理方向枚举
    /// </summary>
    public enum TextureDirection
    {
        /// <summary>
        /// 任意方向
        /// </summary>
        Any = 0,
        
        /// <summary>
        /// 平行
        /// </summary>
        Parallel = 1,
        
        /// <summary>
        /// 垂直
        /// </summary>
        Perpendicular = 2,
        
        /// <summary>
        /// 交叉
        /// </summary>
        Crossed = 3,
        
        /// <summary>
        /// 放射?        /// </summary>
        Radial = 4,
        
        /// <summary>
        /// 同心?        /// </summary>
        Concentric = 5
    }
}