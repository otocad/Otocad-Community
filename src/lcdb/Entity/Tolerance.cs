using System;
using System.Collections.Generic;
using LitMath;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 公差符号帮助类
    /// </summary>
    public static class ToleranceSymbol
    {
        /// <summary>
        /// 几何符号字典
        /// </summary>
        public static readonly Dictionary<ToleranceGeometricSymbol, string> Symbols = new Dictionary<ToleranceGeometricSymbol, string>
        {
            { ToleranceGeometricSymbol.Position, "⌖" },
            { ToleranceGeometricSymbol.Concentricity, "◎" },
            { ToleranceGeometricSymbol.Symmetry, "⌯" },
            { ToleranceGeometricSymbol.Parallelism, "∥" },
            { ToleranceGeometricSymbol.Perpendicularity, "⊥" },
            { ToleranceGeometricSymbol.Angularity, "∠" },
            { ToleranceGeometricSymbol.Cylindricity, "⌭" },
            { ToleranceGeometricSymbol.Flatness, "⏥" },
            { ToleranceGeometricSymbol.Roundness, "○" },
            { ToleranceGeometricSymbol.Straightness, "⏤" },
            { ToleranceGeometricSymbol.ProfileSurface, "⌒" },
            { ToleranceGeometricSymbol.ProfileLine, "⌒" },
            { ToleranceGeometricSymbol.CircularRunout, "↗" },
            { ToleranceGeometricSymbol.TotalRunout, "↗↗" }
        };
    }

    /// <summary>
    /// 形位公差实体
    /// 原生OtoCAD实现，用于显示几何尺寸和公差(GD&T)符号
    /// </summary>
    [Serializable]
    public class Tolerance : Entity
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "Tolerance";

        #region 字段

        // 第一个公差条目
        private ToleranceEntry _entry1;
        
        // 第二个公差条目（可选）
        private ToleranceEntry _entry2;
        
        // 投影公差区域值
        private string _projectedToleranceZoneValue = string.Empty;
        
        // 是否显示投影公差区域符号
        private bool _showProjectedToleranceZoneSymbol = false;
        
        // 基准标识符
        private string _datumIdentifier = string.Empty;
        
        // 标注样式
        private DimensionStyle _style;
        
        // 文本高度
        private double _textHeight = 2.5;
        
        // 位置
        private Vector3 _position = new Vector3();
        
        // 旋转角度（弧度）
        private double _rotation = 0.0;

        #endregion

        #region 属性

        /// <summary>
        /// 第一个公差条目
        /// </summary>
        public ToleranceEntry Entry1
        {
            get { return _entry1; }
            set { _entry1 = value; }
        }

        /// <summary>
        /// 第二个公差条目
        /// </summary>
        public ToleranceEntry Entry2
        {
            get { return _entry2; }
            set { _entry2 = value; }
        }

        /// <summary>
        /// 投影公差区域值
        /// </summary>
        public string ProjectedToleranceZoneValue
        {
            get { return _projectedToleranceZoneValue; }
            set { _projectedToleranceZoneValue = value ?? string.Empty; }
        }

        /// <summary>
        /// 是否显示投影公差区域符号
        /// </summary>
        public bool ShowProjectedToleranceZoneSymbol
        {
            get { return _showProjectedToleranceZoneSymbol; }
            set { _showProjectedToleranceZoneSymbol = value; }
        }

        /// <summary>
        /// 基准标识符
        /// </summary>
        public string DatumIdentifier
        {
            get { return _datumIdentifier; }
            set { _datumIdentifier = value ?? string.Empty; }
        }

        /// <summary>
        /// 标注样式
        /// </summary>
        public DimensionStyle Style
        {
            get { return _style; }
            set { _style = value ?? DimensionStyle.Default; }
        }

        /// <summary>
        /// 文本高度
        /// </summary>
        public double TextHeight
        {
            get { return _textHeight; }
            set { _textHeight = Math.Max(0.1, value); }
        }

        /// <summary>
        /// 位置
        /// </summary>
        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; }
        }
        

        /// <summary>
        /// 旋转角度（弧度）
        /// </summary>
        public double Rotation
        {
            get { return _rotation; }
            set { _rotation = value; }
        }
        
        /// <summary>
        /// 公差文本（用于解析和设置第一个公差条目）
        /// </summary>
        public string Text
        {
            get 
            { 
                // 返回第一个公差条目的格式化文本
                if (_entry1 != null)
                {
                    return FormatToleranceText(_entry1);
                }
                return string.Empty;
            }
            set 
            { 
                // 解析文本并设置到第一个公差条目
                _entry1 = ParseToleranceText(value);
            }
        }


        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                // 估算公差框的大小
                double width = CalculateWidth();
                double height = CalculateHeight();
                
                Vector2 pos2d = new Vector2(_position.X, _position.Y);
                
                if (Math.Abs(_rotation) < 1e-10)
                {
                    return new Bounding(pos2d, pos2d + new Vector2(width, height));
                }
                else
                {
                    // 旋转后的边界框
                    Vector2[] corners = new Vector2[]
                    {
                        pos2d,
                        pos2d + Vector2.RotateInRadian(new Vector2(width, 0), Vector2.Zero, _rotation),
                        pos2d + Vector2.RotateInRadian(new Vector2(width, height), Vector2.Zero, _rotation),
                        pos2d + Vector2.RotateInRadian(new Vector2(0, height), Vector2.Zero, _rotation)
                    };
                    
                    double minX = double.MaxValue, minY = double.MaxValue;
                    double maxX = double.MinValue, maxY = double.MinValue;
                    
                    foreach (var corner in corners)
                    {
                        minX = Math.Min(minX, corner.X);
                        minY = Math.Min(minY, corner.Y);
                        maxX = Math.Max(maxX, corner.X);
                        maxY = Math.Max(maxY, corner.Y);
                    }
                    
                    return new Bounding(new Vector2(minX, minY), new Vector2(maxX, maxY));
                }
            }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建形位公差
        /// </summary>
        public Tolerance() : base()
        {
            _style = DimensionStyle.Default;
            _textHeight = _style?.TextHeight ?? 2.5;
        }

        /// <summary>
        /// 创建形位公差
        /// </summary>
        /// <param name="tolerance">公差条目</param>
        public Tolerance(ToleranceEntry tolerance) : base()
        {
            _entry1 = tolerance;
            _style = DimensionStyle.Default;
            _textHeight = _style?.TextHeight ?? 2.5;
        }

        /// <summary>
        /// 创建形位公差
        /// </summary>
        /// <param name="tolerance">公差条目</param>
        /// <param name="position">位置</param>
        public Tolerance(ToleranceEntry tolerance, Vector2 position) : base()
        {
            _entry1 = tolerance;
            _position = new Vector3(position.X, position.Y, 0);
            _style = DimensionStyle.Default;
            _textHeight = _style?.TextHeight ?? 2.5;
        }

        #endregion

        #region 方法

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            double cellHeight = _textHeight * 1.5;
            double cellPadding = _textHeight * 0.2;
            Vector2 currentPos = new Vector2(_position.X, _position.Y);
            
            // 应用旋转变换
            if (Math.Abs(_rotation) > 1e-10)
            {
                // TODO: 应用旋转变换到绘制操作
            }
            
            // 绘制第一个公差条目
            if (_entry1 != null)
            {
                currentPos.X += DrawToleranceEntry(gd, _entry1, currentPos, cellHeight, cellPadding);
            }
            
            // 绘制第二个公差条目
            if (_entry2 != null)
            {
                currentPos.X += cellPadding;
                currentPos.X += DrawToleranceEntry(gd, _entry2, currentPos, cellHeight, cellPadding);
            }
            
            // 绘制投影公差区域
            if (_showProjectedToleranceZoneSymbol || !string.IsNullOrEmpty(_projectedToleranceZoneValue))
            {
                currentPos.X += cellPadding;
                currentPos.X += DrawProjectedZone(gd, currentPos, cellHeight, cellPadding);
            }
            
            // 绘制基准标识符
            if (!string.IsNullOrEmpty(_datumIdentifier))
            {
                currentPos.X += cellPadding;
                DrawDatumIdentifier(gd, currentPos, cellHeight, cellPadding);
            }
        }

        /// <summary>
        /// 绘制公差条目
        /// </summary>
        private double DrawToleranceEntry(IGraphicsDraw gd, ToleranceEntry entry, Vector2 pos, double cellHeight, double padding)
        {
            double currentX = 0;
            
            // 绘制几何特征符号框
            if (entry.GeometricSymbol != ToleranceGeometricSymbol.None)
            {
                double symbolWidth = cellHeight;
                DrawCell(gd, pos + new Vector2(currentX, 0), symbolWidth, cellHeight);
                DrawGeometricSymbol(gd, entry.GeometricSymbol, 
                    pos + new Vector2(currentX + symbolWidth/2, cellHeight/2), cellHeight * 0.8);
                currentX += symbolWidth;
            }
            
            // 绘制公差值框
            if (!string.IsNullOrEmpty(entry.Tolerance1))
            {
                string toleranceText = entry.Tolerance1;
                double textWidth = EstimateTextWidth(toleranceText, _textHeight);
                double cellWidth = textWidth + padding * 2;
                
                DrawCell(gd, pos + new Vector2(currentX, 0), cellWidth, cellHeight);
                gd.DrawText(pos + new Vector2(currentX + cellWidth/2, cellHeight/2), 
                    toleranceText, _textHeight * 0.8, "", (lcdb.TextAlignment)TextAlignment.CenterMiddle, 0);
                currentX += cellWidth;
            }
            
            // 绘制材料条件框
            if (entry.MaterialCondition1 != ToleranceMaterialCondition.None)
            {
                double mcWidth = cellHeight * 0.8;
                DrawCell(gd, pos + new Vector2(currentX, 0), mcWidth, cellHeight);
                DrawMaterialCondition(gd, entry.MaterialCondition1,
                    pos + new Vector2(currentX + mcWidth/2, cellHeight/2), cellHeight * 0.7);
                currentX += mcWidth;
            }
            
            // 绘制基准参考
            for (int i = 0; i < 3; i++)
            {
                string datum = GetDatumReference(entry, i);
                ToleranceMaterialCondition mc = GetDatumMaterialCondition(entry, i);
                
                if (!string.IsNullOrEmpty(datum))
                {
                    // 基准字母
                    double datumWidth = EstimateTextWidth(datum, _textHeight) + padding * 2;
                    DrawCell(gd, pos + new Vector2(currentX, 0), datumWidth, cellHeight);
                    gd.DrawText(pos + new Vector2(currentX + datumWidth/2, cellHeight/2),
                        datum, _textHeight * 0.8, "", (lcdb.TextAlignment)TextAlignment.CenterMiddle, 0);
                    currentX += datumWidth;
                    
                    // 材料条件
                    if (mc != ToleranceMaterialCondition.None)
                    {
                        double mcWidth = cellHeight * 0.8;
                        DrawCell(gd, pos + new Vector2(currentX, 0), mcWidth, cellHeight);
                        DrawMaterialCondition(gd, mc,
                            pos + new Vector2(currentX + mcWidth/2, cellHeight/2), cellHeight * 0.7);
                        currentX += mcWidth;
                    }
                }
            }
            
            return currentX;
        }

        /// <summary>
        /// 绘制投影公差区域
        /// </summary>
        private double DrawProjectedZone(IGraphicsDraw gd, Vector2 pos, double cellHeight, double padding)
        {
            double width = 0;
            
            if (_showProjectedToleranceZoneSymbol)
            {
                // 绘制P符号
                double symbolWidth = cellHeight * 0.8;
                DrawCell(gd, pos + new Vector2(width, 0), symbolWidth, cellHeight);
                gd.DrawText(pos + new Vector2(width + symbolWidth/2, cellHeight/2),
                    "P", _textHeight * 0.8, "", (lcdb.TextAlignment)TextAlignment.CenterMiddle, 0);
                width += symbolWidth;
            }
            
            if (!string.IsNullOrEmpty(_projectedToleranceZoneValue))
            {
                double textWidth = EstimateTextWidth(_projectedToleranceZoneValue, _textHeight) + padding * 2;
                DrawCell(gd, pos + new Vector2(width, 0), textWidth, cellHeight);
                gd.DrawText(pos + new Vector2(width + textWidth/2, cellHeight/2),
                    _projectedToleranceZoneValue, _textHeight * 0.8, "", (lcdb.TextAlignment)TextAlignment.CenterMiddle, 0);
                width += textWidth;
            }
            
            return width;
        }

        /// <summary>
        /// 绘制基准标识符
        /// </summary>
        private void DrawDatumIdentifier(IGraphicsDraw gd, Vector2 pos, double cellHeight, double padding)
        {
            // 绘制基准框（带三角形）
            double width = EstimateTextWidth(_datumIdentifier, _textHeight) + padding * 2;
            
            // 绘制矩形框
            DrawCell(gd, pos, width, cellHeight);
            
            // 绘制左侧三角形
            Vector2 p1 = pos + new Vector2(0, 0);
            Vector2 p2 = pos + new Vector2(-cellHeight * 0.3, cellHeight * 0.5);
            Vector2 p3 = pos + new Vector2(0, cellHeight);
            gd.DrawLine(p1, p2);
            gd.DrawLine(p2, p3);
            
            // 绘制基准字母
            gd.DrawText(pos + new Vector2(width/2, cellHeight/2),
                _datumIdentifier, _textHeight * 0.8, "", (lcdb.TextAlignment)TextAlignment.CenterMiddle, 0);
        }

        /// <summary>
        /// 绘制单元格边框
        /// </summary>
        private void DrawCell(IGraphicsDraw gd, Vector2 pos, double width, double height)
        {
            gd.DrawLine(pos, pos + new Vector2(width, 0));
            gd.DrawLine(pos + new Vector2(width, 0), pos + new Vector2(width, height));
            gd.DrawLine(pos + new Vector2(width, height), pos + new Vector2(0, height));
            gd.DrawLine(pos + new Vector2(0, height), pos);
        }

        /// <summary>
        /// 绘制几何特征符号
        /// </summary>
        private void DrawGeometricSymbol(IGraphicsDraw gd, ToleranceGeometricSymbol symbol, Vector2 center, double size)
        {
            // 使用文本绘制符号（简化处理）
            string symbolText = GetGeometricSymbolText(symbol);
            if (!string.IsNullOrEmpty(symbolText))
            {
                gd.DrawText(center, symbolText, size, "", (lcdb.TextAlignment)TextAlignment.CenterMiddle, 0);
            }
        }

        /// <summary>
        /// 绘制材料条件符号
        /// </summary>
        private void DrawMaterialCondition(IGraphicsDraw gd, ToleranceMaterialCondition condition, Vector2 center, double size)
        {
            string conditionText = "";
            switch (condition)
            {
                case ToleranceMaterialCondition.Maximum:
                    conditionText = "Ⓜ";
                    break;
                case ToleranceMaterialCondition.Least:
                    conditionText = "Ⓛ";
                    break;
                case ToleranceMaterialCondition.Regardless:
                    conditionText = "Ⓢ";
                    break;
            }
            
            if (!string.IsNullOrEmpty(conditionText))
            {
                gd.DrawText(center, conditionText, size, "", (lcdb.TextAlignment)TextAlignment.CenterMiddle, 0);
            }
        }

        /// <summary>
        /// 获取几何符号文本
        /// </summary>
        private string GetGeometricSymbolText(ToleranceGeometricSymbol symbol)
        {
            switch (symbol)
            {
                case ToleranceGeometricSymbol.Position: return "⌖";
                case ToleranceGeometricSymbol.Concentricity: return "◎";
                case ToleranceGeometricSymbol.Symmetry: return "⌯";
                case ToleranceGeometricSymbol.Parallelism: return "∥";
                case ToleranceGeometricSymbol.Perpendicularity: return "⊥";
                case ToleranceGeometricSymbol.Angularity: return "∠";
                case ToleranceGeometricSymbol.Cylindricity: return "⌭";
                case ToleranceGeometricSymbol.Flatness: return "⏥";
                case ToleranceGeometricSymbol.Roundness: return "○";
                case ToleranceGeometricSymbol.Straightness: return "⏤";
                case ToleranceGeometricSymbol.ProfileSurface: return "⌒";
                case ToleranceGeometricSymbol.ProfileLine: return "⌒";
                case ToleranceGeometricSymbol.CircularRunout: return "↗";
                case ToleranceGeometricSymbol.TotalRunout: return "↗↗";
                default: return "";
            }
        }

        /// <summary>
        /// 计算宽度
        /// </summary>
        private double CalculateWidth()
        {
            double width = 0;
            double cellHeight = _textHeight * 1.5;
            double padding = _textHeight * 0.2;
            
            // 第一个公差条目宽度
            if (_entry1 != null)
            {
                width += CalculateEntryWidth(_entry1, cellHeight, padding);
            }
            
            // 第二个公差条目宽度
            if (_entry2 != null)
            {
                width += padding + CalculateEntryWidth(_entry2, cellHeight, padding);
            }
            
            // 投影公差区域宽度
            if (_showProjectedToleranceZoneSymbol || !string.IsNullOrEmpty(_projectedToleranceZoneValue))
            {
                width += padding;
                if (_showProjectedToleranceZoneSymbol)
                    width += cellHeight * 0.8;
                if (!string.IsNullOrEmpty(_projectedToleranceZoneValue))
                    width += EstimateTextWidth(_projectedToleranceZoneValue, _textHeight) + padding * 2;
            }
            
            // 基准标识符宽度
            if (!string.IsNullOrEmpty(_datumIdentifier))
            {
                width += padding + EstimateTextWidth(_datumIdentifier, _textHeight) + padding * 2;
            }
            
            return width;
        }

        /// <summary>
        /// 计算条目宽度
        /// </summary>
        private double CalculateEntryWidth(ToleranceEntry entry, double cellHeight, double padding)
        {
            double width = 0;
            
            if (entry.GeometricSymbol != ToleranceGeometricSymbol.None)
                width += cellHeight;
                
            if (!string.IsNullOrEmpty(entry.Tolerance1))
                width += EstimateTextWidth(entry.Tolerance1, _textHeight) + padding * 2;
                
            if (entry.MaterialCondition1 != ToleranceMaterialCondition.None)
                width += cellHeight * 0.8;
                
            // 基准参考
            for (int i = 0; i < 3; i++)
            {
                string datum = GetDatumReference(entry, i);
                if (!string.IsNullOrEmpty(datum))
                {
                    width += EstimateTextWidth(datum, _textHeight) + padding * 2;
                    if (GetDatumMaterialCondition(entry, i) != ToleranceMaterialCondition.None)
                        width += cellHeight * 0.8;
                }
            }
            
            return width;
        }

        /// <summary>
        /// 计算高度
        /// </summary>
        private double CalculateHeight()
        {
            // 公差框高度是文本高度的1.5倍
            return _textHeight * 1.5;
        }

        /// <summary>
        /// 估算文本宽度
        /// </summary>
        private double EstimateTextWidth(string text, double height)
        {
            return text.Length * height * 0.6;
        }

        /// <summary>
        /// 获取基准参考
        /// </summary>
        private string GetDatumReference(ToleranceEntry entry, int index)
        {
            switch (index)
            {
                case 0: return entry?.DatumReference1 ?? "";
                case 1: return entry?.DatumReference2 ?? "";
                case 2: return entry?.DatumReference3 ?? "";
                default: return "";
            }
        }

        /// <summary>
        /// 获取基准材料条件
        /// </summary>
        private ToleranceMaterialCondition GetDatumMaterialCondition(ToleranceEntry entry, int index)
        {
            switch (index)
            {
                case 0: return entry?.DatumMaterialCondition1 ?? ToleranceMaterialCondition.None;
                case 1: return entry?.DatumMaterialCondition2 ?? ToleranceMaterialCondition.None;
                case 2: return entry?.DatumMaterialCondition3 ?? ToleranceMaterialCondition.None;
                default: return ToleranceMaterialCondition.None;
            }
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new Tolerance();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Tolerance tolerance = base.Clone() as Tolerance;
            tolerance._entry1 = _entry1?.Clone() as ToleranceEntry;
            tolerance._entry2 = _entry2?.Clone() as ToleranceEntry;
            tolerance._projectedToleranceZoneValue = _projectedToleranceZoneValue;
            tolerance._showProjectedToleranceZoneSymbol = _showProjectedToleranceZoneSymbol;
            tolerance._datumIdentifier = _datumIdentifier;
            tolerance._style = _style;
            tolerance._textHeight = _textHeight;
            tolerance._position = _position;
            tolerance._rotation = _rotation;
            return tolerance;
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            _position = new Vector3(_position.X + translation.X, _position.Y + translation.Y, _position.Z);
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            Vector2 pos2d = new Vector2(_position.X, _position.Y);
            Vector2 rotated = Vector2.RotateInRadian(pos2d, center, angle);
            _position = new Vector3(rotated.X, rotated.Y, _position.Z);
            _rotation += angle;
        }

        /// <summary>
        /// 变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            Vector2 pos2d = new Vector2(_position.X, _position.Y);
            Vector2 transformed = transform * pos2d;
            _position = new Vector3(transformed.X, transformed.Y, _position.Z);
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            gripPoints.Add(new GripPoint(GripPointType.Center, new Vector2(_position.X, _position.Y)));
            return gripPoints;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            if (index == 0)
            {
                _position = new Vector3(newPosition.X, newPosition.Y, _position.Z);
            }
        }

        /// <summary>
        /// 获取捕捉点
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPoints = new List<ObjectSnapPoint>();
            
            double width = CalculateWidth();
            double height = CalculateHeight();
            Vector2 pos2d = new Vector2(_position.X, _position.Y);
            
            // 四个角点
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, pos2d));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, pos2d + new Vector2(width, 0)));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, pos2d + new Vector2(width, height)));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, pos2d + new Vector2(0, height)));
            
            // 中心点
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Center, pos2d + new Vector2(width/2, height/2)));
            
            // 考虑旋转
            if (Math.Abs(_rotation) > 1e-10)
            {
                for (int i = 0; i < snapPoints.Count; i++)
                {
                    Vector2 rotated = Vector2.RotateInRadian(snapPoints[i].position - pos2d, Vector2.Zero, _rotation) + pos2d;
                    snapPoints[i] = new ObjectSnapPoint(snapPoints[i].type, rotated);
                }
            }
            
            return snapPoints;
        }

        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 解析公差文本
        /// </summary>
        private ToleranceEntry ParseToleranceText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new ToleranceEntry();
                
            var entry = new ToleranceEntry();
            
            // 简单解析逻辑，实际应用中可能需要更复杂的解析
            // 格式示例: "⌖0.05ABC"
            if (text.Length > 0)
            {
                // 查找公差符号
                int symbolIndex = 0;
                foreach (var kvp in ToleranceSymbol.Symbols)
                {
                    if (text.StartsWith(kvp.Value))
                    {
                        entry.GeometricSymbol = kvp.Key;
                        symbolIndex = kvp.Value.Length;
                        break;
                    }
                }
                
                // 提取公差值和基准
                if (symbolIndex < text.Length)
                {
                    string remaining = text.Substring(symbolIndex);
                    // 简单分割：数值和基准
                    int letterIndex = -1;
                    for (int i = 0; i < remaining.Length; i++)
                    {
                        if (char.IsLetter(remaining[i]))
                        {
                            letterIndex = i;
                            break;
                        }
                    }
                    
                    if (letterIndex > 0)
                    {
                        entry.Tolerance1 = remaining.Substring(0, letterIndex);
                        entry.DatumReference1 = remaining.Substring(letterIndex);
                    }
                    else
                    {
                        entry.Tolerance1 = remaining;
                    }
                }
            }
            
            return entry;
        }
        
        /// <summary>
        /// 格式化公差文本
        /// </summary>
        private string FormatToleranceText(ToleranceEntry entry)
        {
            if (entry == null)
                return string.Empty;
                
            string result = string.Empty;
            
            // 添加几何符号
            if (ToleranceSymbol.Symbols.ContainsKey(entry.GeometricSymbol))
            {
                result += ToleranceSymbol.Symbols[entry.GeometricSymbol];
            }
            
            // 添加公差值
            if (!string.IsNullOrEmpty(entry.Tolerance1))
            {
                result += entry.Tolerance1;
            }
            
            // 添加基准参考
            if (!string.IsNullOrEmpty(entry.DatumReference1))
            {
                result += entry.DatumReference1;
            }
            
            return result;
        }
        
        #endregion
    }

    /// <summary>
    /// 公差条目
    /// </summary>
    [Serializable]
    public class ToleranceEntry : ICloneable
    {
        /// <summary>
        /// 几何特征符号
        /// </summary>
        public ToleranceGeometricSymbol GeometricSymbol { get; set; } = ToleranceGeometricSymbol.None;
        
        /// <summary>
        /// 第一个公差值
        /// </summary>
        public string Tolerance1 { get; set; } = string.Empty;
        
        /// <summary>
        /// 第二个公差值（可选）
        /// </summary>
        public string Tolerance2 { get; set; } = string.Empty;
        
        /// <summary>
        /// 第一个材料条件
        /// </summary>
        public ToleranceMaterialCondition MaterialCondition1 { get; set; } = ToleranceMaterialCondition.None;
        
        /// <summary>
        /// 第二个材料条件
        /// </summary>
        public ToleranceMaterialCondition MaterialCondition2 { get; set; } = ToleranceMaterialCondition.None;
        
        /// <summary>
        /// 基准参考A
        /// </summary>
        public string DatumReference1 { get; set; } = string.Empty;
        
        /// <summary>
        /// 基准参考B
        /// </summary>
        public string DatumReference2 { get; set; } = string.Empty;
        
        /// <summary>
        /// 基准参考C
        /// </summary>
        public string DatumReference3 { get; set; } = string.Empty;
        
        /// <summary>
        /// 基准材料条件1
        /// </summary>
        public ToleranceMaterialCondition DatumMaterialCondition1 { get; set; } = ToleranceMaterialCondition.None;
        
        /// <summary>
        /// 基准材料条件2
        /// </summary>
        public ToleranceMaterialCondition DatumMaterialCondition2 { get; set; } = ToleranceMaterialCondition.None;
        
        /// <summary>
        /// 基准材料条件3
        /// </summary>
        public ToleranceMaterialCondition DatumMaterialCondition3 { get; set; } = ToleranceMaterialCondition.None;

        /// <summary>
        /// 克隆
        /// </summary>
        public object Clone()
        {
            return new ToleranceEntry
            {
                GeometricSymbol = this.GeometricSymbol,
                Tolerance1 = this.Tolerance1,
                Tolerance2 = this.Tolerance2,
                MaterialCondition1 = this.MaterialCondition1,
                MaterialCondition2 = this.MaterialCondition2,
                DatumReference1 = this.DatumReference1,
                DatumReference2 = this.DatumReference2,
                DatumReference3 = this.DatumReference3,
                DatumMaterialCondition1 = this.DatumMaterialCondition1,
                DatumMaterialCondition2 = this.DatumMaterialCondition2,
                DatumMaterialCondition3 = this.DatumMaterialCondition3
            };
        }
    }

    /// <summary>
    /// 几何特征符号
    /// </summary>
    public enum ToleranceGeometricSymbol
    {
        /// <summary>
        /// 无
        /// </summary>
        None = 0,
        
        /// <summary>
        /// 位置度
        /// </summary>
        Position,
        
        /// <summary>
        /// 同心度/同轴度
        /// </summary>
        Concentricity,
        
        /// <summary>
        /// 对称度
        /// </summary>
        Symmetry,
        
        /// <summary>
        /// 平行度
        /// </summary>
        Parallelism,
        
        /// <summary>
        /// 垂直度
        /// </summary>
        Perpendicularity,
        
        /// <summary>
        /// 倾斜度
        /// </summary>
        Angularity,
        
        /// <summary>
        /// 圆柱度
        /// </summary>
        Cylindricity,
        
        /// <summary>
        /// 平面度
        /// </summary>
        Flatness,
        
        /// <summary>
        /// 圆度
        /// </summary>
        Roundness,
        
        /// <summary>
        /// 直线度
        /// </summary>
        Straightness,
        
        /// <summary>
        /// 面轮廓度
        /// </summary>
        ProfileSurface,
        
        /// <summary>
        /// 线轮廓度
        /// </summary>
        ProfileLine,
        
        /// <summary>
        /// 圆跳动
        /// </summary>
        CircularRunout,
        
        /// <summary>
        /// 全跳动
        /// </summary>
        TotalRunout
    }

    /// <summary>
    /// 材料条件
    /// </summary>
    public enum ToleranceMaterialCondition
    {
        /// <summary>
        /// 无
        /// </summary>
        None = 0,
        
        /// <summary>
        /// 最大实体条件(M)
        /// </summary>
        Maximum,
        
        /// <summary>
        /// 最小实体条件(L)
        /// </summary>
        Least,
        
        /// <summary>
        /// 与尺寸无关(S)
        /// </summary>
        Regardless
    }
}