using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using LitMath;

namespace lcdb.Symbols
{
    /// <summary>
    /// 光学标记符号库
    /// 提供ISO 10110和GB/T 13323-2009标准的光学标记符号绘制方法
    /// </summary>
    public static class OpticalMarkSymbols
    {
        #region 表面质量符号

        /// <summary>
        /// 绘制表面质量标记（疵病等级）
        /// 格式：60-40（划痕-麻点）
        /// </summary>
        public static void DrawSurfaceQualityMark(Graphics g, PointF position, string scratchGrade, string digGrade, float scale = 1.0f)
        {
            using (var font = new Font("Arial", 10 * scale))
            using (var brush = new SolidBrush(Color.Black))
            {
                string markText = $"{scratchGrade}-{digGrade}";
                
                // 绘制边框
                var rect = GetTextBounds(g, markText, font, position);
                rect.Inflate(5 * scale, 3 * scale);
                using (var pen = new Pen(Color.Black, 0.5f * scale))
                {
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                }
                
                // 绘制文字
                g.DrawString(markText, font, brush, position);
            }
        }

        /// <summary>
        /// 绘制表面等级标记（I、II、III级）
        /// </summary>
        public static void DrawSurfaceGradeMark(Graphics g, PointF position, string grade, float scale = 1.0f)
        {
            using (var font = new Font("Arial", 10 * scale, System.Drawing.FontStyle.Bold))
            using (var brush = new SolidBrush(Color.Black))
            {
                // 绘制圆形边框
                float diameter = 20 * scale;
                var rect = new RectangleF(position.X - diameter/2, position.Y - diameter/2, diameter, diameter);
                using (var pen = new Pen(Color.Black, 0.5f * scale))
                {
                    g.DrawEllipse(pen, rect);
                }
                
                // 绘制等级文字
                var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(grade, font, brush, new PointF(position.X, position.Y), format);
            }
        }

        #endregion

        #region 面形精度符号

        /// <summary>
        /// 绘制面形精度标记
        /// </summary>
        public static void DrawFormAccuracyMark(Graphics g, PointF position, string pvValue, string rmsValue, string wavelength, float scale = 1.0f)
        {
            using (var font = new Font("Arial", 9 * scale))
            using (var brush = new SolidBrush(Color.Black))
            {
                float lineHeight = 15 * scale;
                float y = position.Y;
                
                // 绘制PV值
                if (!string.IsNullOrEmpty(pvValue))
                {
                    g.DrawString($"PV: λ/{pvValue}", font, brush, position.X, y);
                    y += lineHeight;
                }
                
                // 绘制RMS值
                if (!string.IsNullOrEmpty(rmsValue))
                {
                    g.DrawString($"RMS: λ/{rmsValue}", font, brush, position.X, y);
                    y += lineHeight;
                }
                
                // 绘制测试波长
                if (!string.IsNullOrEmpty(wavelength))
                {
                    g.DrawString($"@{wavelength}nm", font, brush, position.X, y);
                }
                
                // 绘制边框
                float totalHeight = y - position.Y + lineHeight;
                var bounds = new RectangleF(position.X - 5 * scale, position.Y - 3 * scale, 
                                           80 * scale, totalHeight);
                using (var pen = new Pen(Color.Black, 0.5f * scale))
                {
                    pen.DashStyle = DashStyle.Dash;
                    g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
                }
            }
        }

        /// <summary>
        /// 绘制Power/Irregularity标记
        /// </summary>
        public static void DrawPowerIrregularityMark(Graphics g, PointF position, float power, float irregularity, float scale = 1.0f)
        {
            using (var font = new Font("Arial", 9 * scale))
            using (var brush = new SolidBrush(Color.Black))
            {
                string markText = $"{power:F1}/{irregularity:F2}";
                
                // 绘制符号框
                var rect = GetTextBounds(g, markText, font, position);
                rect.Inflate(8 * scale, 5 * scale);
                
                using (var pen = new Pen(Color.Black, 0.7f * scale))
                {
                    // 绘制双线框
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                    rect.Inflate(-2 * scale, -2 * scale);
                    pen.Width = 0.3f * scale;
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                }
                
                // 绘制文字
                g.DrawString(markText, font, brush, position);
            }
        }

        #endregion

        #region ISO 10110标准符号

        /// <summary>
        /// 绘制ISO 10110-5面形公差标记（Zernike系数）
        /// </summary>
        public static void DrawISO10110_5Mark(Graphics g, PointF position, string zernikeData, float scale = 1.0f)
        {
            using (var font = new Font("Courier New", 8 * scale))
            using (var brush = new SolidBrush(Color.Black))
            {
                // 绘制ISO标准框
                DrawISOFrame(g, position, 100 * scale, 40 * scale, scale);
                
                // 绘制标准号
                g.DrawString("ISO 10110-5", font, brush, position.X + 5 * scale, position.Y + 2 * scale);
                
                // 绘制Zernike数据
                g.DrawString(zernikeData, font, brush, position.X + 5 * scale, position.Y + 15 * scale);
            }
        }

        /// <summary>
        /// 绘制ISO 10110-6中心公差标记
        /// </summary>
        public static void DrawISO10110_6Mark(Graphics g, PointF position, float decentration, float tilt, float scale = 1.0f)
        {
            using (var font = new Font("Arial", 9 * scale))
            using (var brush = new SolidBrush(Color.Black))
            {
                // 绘制中心偏差符号（十字圆）
                float radius = 15 * scale;
                var center = new PointF(position.X, position.Y);
                
                using (var pen = new Pen(Color.Black, 0.5f * scale))
                {
                    // 圆
                    g.DrawEllipse(pen, center.X - radius, center.Y - radius, radius * 2, radius * 2);
                    
                    // 十字线
                    g.DrawLine(pen, center.X - radius, center.Y, center.X + radius, center.Y);
                    g.DrawLine(pen, center.X, center.Y - radius, center.X, center.Y + radius);
                }
                
                // 绘制数值
                string text = $"δ: {decentration:F3}mm\nθ: {tilt:F3}°";
                g.DrawString(text, font, brush, position.X + radius + 5 * scale, position.Y - 10 * scale);
            }
        }

        /// <summary>
        /// 绘制ISO 10110-7表面缺陷标记
        /// </summary>
        public static void DrawISO10110_7Mark(Graphics g, PointF position, int scratchNumber, float scratchWidth, int digNumber, float digDiameter, float scale = 1.0f)
        {
            using (var font = new Font("Arial", 9 * scale))
            using (var brush = new SolidBrush(Color.Black))
            {
                // 格式：5 × 0.01; 10 × 0.05
                string markText = $"{scratchNumber} × {scratchWidth:F3}; {digNumber} × {digDiameter:F3}";
                
                // 绘制ISO框架
                DrawISOFrame(g, position, 120 * scale, 25 * scale, scale);
                
                // 绘制文字
                g.DrawString("ISO 10110-7:", font, brush, position.X + 3 * scale, position.Y + 2 * scale);
                g.DrawString(markText, font, brush, position.X + 3 * scale, position.Y + 12 * scale);
            }
        }

        /// <summary>
        /// 绘制ISO 10110-17激光损伤阈值标记
        /// </summary>
        public static void DrawISO10110_17Mark(Graphics g, PointF position, float lidt, string wavelength, string pulseWidth, float scale = 1.0f)
        {
            using (var font = new Font("Arial", 9 * scale))
            using (var brush = new SolidBrush(Color.Black))
            {
                // 绘制激光警告符号
                DrawLaserWarningSymbol(g, position, 20 * scale);
                
                // 绘制LIDT值
                float textX = position.X + 25 * scale;
                g.DrawString($"LIDT: {lidt:F1} J/cm²", font, brush, textX, position.Y);
                g.DrawString($"λ: {wavelength}nm", font, brush, textX, position.Y + 12 * scale);
                g.DrawString($"τ: {pulseWidth}", font, brush, textX, position.Y + 24 * scale);
            }
        }

        #endregion

        #region 中心偏差和光学特征符号

        /// <summary>
        /// 绘制偏心标记
        /// </summary>
        public static void DrawDecenterMark(Graphics g, PointF position, float value, float scale = 1.0f)
        {
            using (var pen = new Pen(Color.Black, 0.5f * scale))
            using (var font = new Font("Arial", 9 * scale))
            using (var brush = new SolidBrush(Color.Black))
            {
                // 绘制偏心符号（两个偏移的圆）
                float radius = 10 * scale;
                float offset = 3 * scale;
                
                g.DrawEllipse(pen, position.X - radius, position.Y - radius, radius * 2, radius * 2);
                pen.DashStyle = DashStyle.Dash;
                g.DrawEllipse(pen, position.X - radius + offset, position.Y - radius, radius * 2, radius * 2);
                
                // 绘制偏心值
                g.DrawString($"δ={value:F3}mm", font, brush, position.X + radius + 5 * scale, position.Y);
            }
        }

        /// <summary>
        /// 绘制倾斜标记
        /// </summary>
        public static void DrawTiltMark(Graphics g, PointF position, float angle, float scale = 1.0f)
        {
            using (var pen = new Pen(Color.Black, 0.7f * scale))
            using (var font = new Font("Arial", 9 * scale))
            using (var brush = new SolidBrush(Color.Black))
            {
                // 绘制倾斜线
                float length = 20 * scale;
                double radians = angle * Math.PI / 180;
                
                g.DrawLine(pen, position.X - length, position.Y, position.X + length, position.Y);
                
                pen.DashStyle = DashStyle.Dash;
                float endX = position.X + length * (float)Math.Cos(radians);
                float endY = position.Y - length * (float)Math.Sin(radians);
                g.DrawLine(pen, position.X - length, position.Y, endX, endY);
                
                // 绘制角度弧
                var rect = new RectangleF(position.X - 10 * scale, position.Y - 10 * scale, 20 * scale, 20 * scale);
                pen.DashStyle = DashStyle.Solid;
                pen.Width = 0.3f * scale;
                g.DrawArc(pen, rect, 0, -angle);
                
                // 绘制角度值
                g.DrawString($"θ={angle:F1}°", font, brush, position.X + 25 * scale, position.Y);
            }
        }

        /// <summary>
        /// 绘制光轴标记
        /// </summary>
        public static void DrawOpticalAxisMark(Graphics g, PointF start, PointF end, float scale = 1.0f)
        {
            using (var pen = new Pen(Color.Orange, 0.5f * scale))
            {
                pen.DashStyle = DashStyle.DashDot;
                g.DrawLine(pen, start, end);
                
                // 绘制箭头
                DrawArrowHead(g, end, start, end, 8 * scale, Color.Orange, scale);
                
                // 绘制"OA"标记
                using (var font = new Font("Arial", 8 * scale, System.Drawing.FontStyle.Italic))
                using (var brush = new SolidBrush(Color.Orange))
                {
                    float midX = (start.X + end.X) / 2;
                    float midY = (start.Y + end.Y) / 2;
                    g.DrawString("OA", font, brush, midX + 5 * scale, midY - 10 * scale);
                }
            }
        }

        /// <summary>
        /// 绘制焦点标记
        /// </summary>
        public static void DrawFocalPointMark(Graphics g, PointF position, string label = "F", float scale = 1.0f)
        {
            using (var pen = new Pen(Color.Red, 0.7f * scale))
            using (var font = new Font("Arial", 10 * scale, System.Drawing.FontStyle.Bold))
            using (var brush = new SolidBrush(Color.Red))
            {
                // 绘制十字标记
                float size = 5 * scale;
                g.DrawLine(pen, position.X - size, position.Y, position.X + size, position.Y);
                g.DrawLine(pen, position.X, position.Y - size, position.X, position.Y + size);
                
                // 绘制圆圈
                pen.Width = 0.5f * scale;
                g.DrawEllipse(pen, position.X - size, position.Y - size, size * 2, size * 2);
                
                // 绘制标签
                g.DrawString(label, font, brush, position.X + size + 2 * scale, position.Y - size);
            }
        }

        #endregion

        #region 材料缺陷和特殊标记

        /// <summary>
        /// 绘制气泡标记
        /// </summary>
        public static void DrawBubbleMark(Graphics g, PointF position, string grade, float scale = 1.0f)
        {
            using (var pen = new Pen(Color.Black, 0.5f * scale))
            using (var font = new Font("Arial", 9 * scale))
            using (var brush = new SolidBrush(Color.Black))
            {
                // 绘制气泡符号（多个小圆）
                float[] sizes = { 3, 4, 2.5f };
                float[] offsetsX = { 0, 8, -5 };
                float[] offsetsY = { 0, 3, 4 };
                
                for (int i = 0; i < sizes.Length; i++)
                {
                    float radius = sizes[i] * scale;
                    float x = position.X + offsetsX[i] * scale;
                    float y = position.Y + offsetsY[i] * scale;
                    g.DrawEllipse(pen, x - radius, y - radius, radius * 2, radius * 2);
                }
                
                // 绘制等级
                g.DrawString($"气泡度: {grade}", font, brush, position.X + 15 * scale, position.Y);
            }
        }

        /// <summary>
        /// 绘制条纹标记
        /// </summary>
        public static void DrawStriaMark(Graphics g, PointF position, string grade, float scale = 1.0f)
        {
            using (var pen = new Pen(Color.Black, 0.3f * scale))
            using (var font = new Font("Arial", 9 * scale))
            using (var brush = new SolidBrush(Color.Black))
            {
                // 绘制波浪线表示条纹
                float amplitude = 3 * scale;
                float wavelength = 8 * scale;
                float length = 30 * scale;
                
                var points = new List<PointF>();
                for (float x = 0; x <= length; x += 2)
                {
                    float y = amplitude * (float)Math.Sin(x / wavelength * 2 * Math.PI);
                    points.Add(new PointF(position.X + x, position.Y + y));
                }
                
                if (points.Count > 1)
                {
                    g.DrawCurve(pen, points.ToArray());
                }
                
                // 绘制条纹度
                g.DrawString($"条纹度: {grade}", font, brush, position.X, position.Y + 10 * scale);
            }
        }

        /// <summary>
        /// 绘制应力双折射标记
        /// </summary>
        public static void DrawStressBirefringenceMark(Graphics g, PointF position, float value, float scale = 1.0f)
        {
            using (var pen = new Pen(Color.Black, 0.5f * scale))
            using (var font = new Font("Arial", 9 * scale))
            using (var brush = new SolidBrush(Color.Black))
            {
                // 绘制双折射符号（两个正交的箭头）
                float size = 15 * scale;
                
                // 水平箭头
                g.DrawLine(pen, position.X - size, position.Y, position.X + size, position.Y);
                DrawArrowHead(g, new PointF(position.X + size, position.Y), 
                            new PointF(position.X - size, position.Y), 
                            new PointF(position.X + size, position.Y), 
                            5 * scale, Color.Black, scale);
                
                // 垂直箭头
                g.DrawLine(pen, position.X, position.Y - size, position.X, position.Y + size);
                DrawArrowHead(g, new PointF(position.X, position.Y - size), 
                            new PointF(position.X, position.Y + size), 
                            new PointF(position.X, position.Y - size), 
                            5 * scale, Color.Black, scale);
                
                // 绘制数值
                g.DrawString($"Δn={value:E2}", font, brush, position.X + size + 5 * scale, position.Y);
            }
        }

        /// <summary>
        /// 绘制加工标记
        /// </summary>
        public static void DrawProcessingMark(Graphics g, PointF position, string processType, string direction, float scale = 1.0f)
        {
            using (var pen = new Pen(Color.Black, 0.5f * scale))
            using (var font = new Font("Arial", 9 * scale))
            using (var brush = new SolidBrush(Color.Black))
            {
                // 绘制加工方向箭头
                if (!string.IsNullOrEmpty(direction))
                {
                    float length = 25 * scale;
                    double angle = 0;
                    
                    switch (direction.ToUpper())
                    {
                        case "H": angle = 0; break;
                        case "V": angle = 90; break;
                        case "R": // 径向
                            DrawRadialArrows(g, position, 15 * scale, scale);
                            break;
                        case "C": // 周向
                            DrawCircularArrow(g, position, 15 * scale, scale);
                            break;
                        default:
                            if (double.TryParse(direction, out angle)) { }
                            break;
                    }
                    
                    if (direction != "R" && direction != "C")
                    {
                        double radians = angle * Math.PI / 180;
                        float endX = position.X + length * (float)Math.Cos(radians);
                        float endY = position.Y - length * (float)Math.Sin(radians);
                        g.DrawLine(pen, position.X, position.Y, endX, endY);
                        DrawArrowHead(g, new PointF(endX, endY), position, new PointF(endX, endY), 6 * scale, Color.Black, scale);
                    }
                }
                
                // 绘制加工类型
                g.DrawString(processType, font, brush, position.X + 20 * scale, position.Y + 10 * scale);
            }
        }

        /// <summary>
        /// 绘制检验标记
        /// </summary>
        public static void DrawInspectionMark(Graphics g, PointF position, string inspectionType, string result, float scale = 1.0f)
        {
            using (var pen = new Pen(Color.Blue, 0.7f * scale))
            using (var font = new Font("Arial", 9 * scale))
            using (var brush = new SolidBrush(Color.Blue))
            {
                // 绘制检验符号（勾选框）
                float size = 12 * scale;
                var rect = new RectangleF(position.X, position.Y, size, size);
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                
                if (result == "PASS" || result == "OK")
                {
                    // 绘制勾
                    pen.Width = 1.5f * scale;
                    g.DrawLine(pen, position.X + 2 * scale, position.Y + size/2, 
                             position.X + size/3, position.Y + size - 2 * scale);
                    g.DrawLine(pen, position.X + size/3, position.Y + size - 2 * scale,
                             position.X + size - 2 * scale, position.Y + 2 * scale);
                }
                else if (result == "FAIL" || result == "NG")
                {
                    // 绘制叉
                    pen.Width = 1.5f * scale;
                    g.DrawLine(pen, position.X + 2 * scale, position.Y + 2 * scale,
                             position.X + size - 2 * scale, position.Y + size - 2 * scale);
                    g.DrawLine(pen, position.X + size - 2 * scale, position.Y + 2 * scale,
                             position.X + 2 * scale, position.Y + size - 2 * scale);
                }
                
                // 绘制检验类型
                g.DrawString(inspectionType, font, brush, position.X + size + 5 * scale, position.Y);
            }
        }

        /// <summary>
        /// 绘制装配标记
        /// </summary>
        public static void DrawAssemblyMark(Graphics g, PointF position, string assemblyDirection, string referencePoint, float scale = 1.0f)
        {
            using (var pen = new Pen(Color.Green, 0.7f * scale))
            using (var font = new Font("Arial", 9 * scale, System.Drawing.FontStyle.Bold))
            using (var brush = new SolidBrush(Color.Green))
            {
                // 绘制装配箭头
                float length = 30 * scale;
                pen.Width = 1.5f * scale;
                
                switch (assemblyDirection.ToUpper())
                {
                    case "UP":
                        g.DrawLine(pen, position.X, position.Y + length, position.X, position.Y);
                        DrawArrowHead(g, position, new PointF(position.X, position.Y + length), position, 8 * scale, Color.Green, scale);
                        break;
                    case "DOWN":
                        g.DrawLine(pen, position.X, position.Y, position.X, position.Y + length);
                        DrawArrowHead(g, new PointF(position.X, position.Y + length), position, new PointF(position.X, position.Y + length), 8 * scale, Color.Green, scale);
                        break;
                    case "LEFT":
                        g.DrawLine(pen, position.X + length, position.Y, position.X, position.Y);
                        DrawArrowHead(g, position, new PointF(position.X + length, position.Y), position, 8 * scale, Color.Green, scale);
                        break;
                    case "RIGHT":
                        g.DrawLine(pen, position.X, position.Y, position.X + length, position.Y);
                        DrawArrowHead(g, new PointF(position.X + length, position.Y), position, new PointF(position.X + length, position.Y), 8 * scale, Color.Green, scale);
                        break;
                }
                
                // 绘制参考点标记
                if (!string.IsNullOrEmpty(referencePoint))
                {
                    // 绘制三角形定位符号
                    var triangle = new PointF[]
                    {
                        new PointF(position.X, position.Y - 8 * scale),
                        new PointF(position.X - 5 * scale, position.Y + 4 * scale),
                        new PointF(position.X + 5 * scale, position.Y + 4 * scale)
                    };
                    g.FillPolygon(brush, triangle);
                    
                    // 绘制参考点标签
                    g.DrawString(referencePoint, font, brush, position.X + 10 * scale, position.Y - 5 * scale);
                }
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取文字边界
        /// </summary>
        private static RectangleF GetTextBounds(Graphics g, string text, Font font, PointF position)
        {
            var size = g.MeasureString(text, font);
            return new RectangleF(position.X, position.Y, size.Width, size.Height);
        }

        /// <summary>
        /// 绘制ISO标准框架
        /// </summary>
        private static void DrawISOFrame(Graphics g, PointF position, float width, float height, float scale)
        {
            using (var pen = new Pen(Color.Black, 0.7f * scale))
            {
                var rect = new RectangleF(position.X, position.Y, width, height);
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                
                // 绘制标题分割线
                g.DrawLine(pen, rect.X, rect.Y + 12 * scale, rect.X + rect.Width, rect.Y + 12 * scale);
            }
        }

        /// <summary>
        /// 绘制箭头头部
        /// </summary>
        private static void DrawArrowHead(Graphics g, PointF tip, PointF start, PointF end, float size, Color color, float scale)
        {
            using (var pen = new Pen(color, 0.5f * scale))
            {
                float dx = end.X - start.X;
                float dy = end.Y - start.Y;
                float length = (float)Math.Sqrt(dx * dx + dy * dy);
                
                if (length > 0)
                {
                    dx /= length;
                    dy /= length;
                    
                    float perpX = -dy;
                    float perpY = dx;
                    
                    var leftWing = new PointF(tip.X - dx * size + perpX * size * 0.3f, 
                                             tip.Y - dy * size + perpY * size * 0.3f);
                    var rightWing = new PointF(tip.X - dx * size - perpX * size * 0.3f,
                                              tip.Y - dy * size - perpY * size * 0.3f);
                    
                    g.DrawLine(pen, tip, leftWing);
                    g.DrawLine(pen, tip, rightWing);
                }
            }
        }

        /// <summary>
        /// 绘制激光警告符号
        /// </summary>
        private static void DrawLaserWarningSymbol(Graphics g, PointF position, float size)
        {
            using (var pen = new Pen(Color.Red, 1.0f))
            using (var brush = new SolidBrush(Color.Yellow))
            {
                // 绘制三角形警告框
                var triangle = new PointF[]
                {
                    new PointF(position.X, position.Y - size * 0.8f),
                    new PointF(position.X - size * 0.7f, position.Y + size * 0.4f),
                    new PointF(position.X + size * 0.7f, position.Y + size * 0.4f)
                };
                
                g.FillPolygon(brush, triangle);
                g.DrawPolygon(pen, triangle);
                
                // 绘制激光符号
                using (var blackPen = new Pen(Color.Black, 0.8f))
                {
                    // 中心点
                    float centerX = position.X;
                    float centerY = position.Y - size * 0.1f;
                    g.FillEllipse(Brushes.Black, centerX - 2, centerY - 2, 4, 4);
                    
                    // 放射线
                    for (int i = 0; i < 6; i++)
                    {
                        double angle = i * 60 * Math.PI / 180;
                        float endX = centerX + size * 0.4f * (float)Math.Cos(angle);
                        float endY = centerY + size * 0.4f * (float)Math.Sin(angle);
                        g.DrawLine(blackPen, centerX, centerY, endX, endY);
                    }
                }
            }
        }

        /// <summary>
        /// 绘制径向箭头
        /// </summary>
        private static void DrawRadialArrows(Graphics g, PointF center, float radius, float scale)
        {
            using (var pen = new Pen(Color.Black, 0.5f * scale))
            {
                for (int i = 0; i < 4; i++)
                {
                    double angle = i * 90 * Math.PI / 180;
                    float endX = center.X + radius * (float)Math.Cos(angle);
                    float endY = center.Y + radius * (float)Math.Sin(angle);
                    
                    g.DrawLine(pen, center.X, center.Y, endX, endY);
                    DrawArrowHead(g, new PointF(endX, endY), center, new PointF(endX, endY), 4 * scale, Color.Black, scale);
                }
            }
        }

        /// <summary>
        /// 绘制圆形箭头
        /// </summary>
        private static void DrawCircularArrow(Graphics g, PointF center, float radius, float scale)
        {
            using (var pen = new Pen(Color.Black, 0.5f * scale))
            {
                // 绘制圆弧
                var rect = new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2);
                g.DrawArc(pen, rect, 0, 270);
                
                // 绘制箭头
                float endX = center.X;
                float endY = center.Y - radius;
                DrawArrowHead(g, new PointF(endX, endY), 
                            new PointF(endX - radius * 0.7f, endY - radius * 0.7f), 
                            new PointF(endX, endY), 5 * scale, Color.Black, scale);
            }
        }

        #endregion
    }
}