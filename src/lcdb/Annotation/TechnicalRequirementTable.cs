using lcdb.Colors;
using System;
using System.Collections.Generic;
using System.Drawing;
using LitMath;
using lcdb;
using lcdb.Interfaces;
using OtoCAD;

namespace lcdb.Annotation
{
    /// <summary>
    /// GB/T 13323-2009 技术要求表格
    /// 三区域列表格式：左表面 | 材料技术要求 | 右表面
    /// </summary>
    public class TechnicalRequirementTable : Entity, IOpticalMark
    {
        public override string className => "TechnicalRequirementTable";

        #region IOpticalMark 接口属性
        public OpticalMarkType MarkType => OpticalMarkType.TechnicalRequirement;
        public Vector2 Position { get; set; } = new Vector2(0, 0);
        public double Scale { get; set; } = 1.0;
        public string MarkText { get; set; } = "技术要求";
        public bool ShowText { get; set; } = true;
        public Vector2 TextOffset { get; set; } = new Vector2(0, -30);
        public double Rotation { get; set; } = 0.0;
        public bool IsVisible { get; set; } = true;
        #endregion

        #region 表格数据
        /// <summary>
        /// 左表面数据
        /// </summary>
        public SurfaceRequirementData LeftSurface { get; set; } = new SurfaceRequirementData();

        /// <summary>
        /// 右表面数据
        /// </summary>
        public SurfaceRequirementData RightSurface { get; set; } = new SurfaceRequirementData();

        /// <summary>
        /// 材料数据
        /// </summary>
        public MaterialRequirementData Material { get; set; } = new MaterialRequirementData();

        /// <summary>
        /// 是否为胶合件（使用表3格式）
        /// </summary>
        public bool IsCementedElement { get; set; } = false;

        /// <summary>
        /// 胶合剂信息（胶合件时使用）
        /// </summary>
        public string AdhesiveInfo { get; set; } = "";
        #endregion

        #region 表格样式
        /// <summary>
        /// 列宽
        /// </summary>
        public double ColumnWidth { get; set; } = 40.0;

        /// <summary>
        /// 行高
        /// </summary>
        public double RowHeight { get; set; } = 8.0;

        /// <summary>
        /// 文字高度
        /// </summary>
        public double TextHeight { get; set; } = 3.5;

        /// <summary>
        /// 边框线宽
        /// </summary>
        public LineWeight BorderLineWeight { get; set; } = LineWeight.LineWeight035;

        /// <summary>
        /// 标题行高度
        /// </summary>
        public double TitleRowHeight { get; set; } = 10.0;

        /// <summary>
        /// 是否绘制顶部"技术要求"标题行(抬头)。默认 true;聚焦展示某些行时可关掉抬头。
        /// </summary>
        public bool ShowTitle { get; set; } = true;
        #endregion

        private List<Entity> _markEntities = new List<Entity>();

        #region 行定义
        /// <summary>
        /// 表格行类型定义
        /// ISO 10110绘图代码: Part 5→"3/", Part 6→"4/", Part 7→"5/", Part 8→"6/"
        /// </summary>
        private static readonly string[] RowLabels = new string[]
        {
            "R",        // 曲率半径
            "Φe",       // 有效口径
            "倒角",      // 倒角要求
            "表面",      // 表面要求
            "3/",              // ISO 10110-5 面形公差 (代码3/)
            "4/",              // ISO 10110-6 中心偏差 (代码4/)
            "Surface quality", // ISO 10110-7 表面缺陷 / MIL scratch-dig (描述代替代号 5/)
            "6/"               // ISO 10110-8 表面纹理 (代码6/)
        };

        /// <summary>
        /// 材料列行标签
        /// ISO 10110绘图代码: Part 2→"0/", Part 3→"1/", Part 4→"2/"
        /// </summary>
        private static readonly string[] MaterialRowLabels = new string[]
        {
            "n",        // 折射率
            "ν",        // 阿贝数
            "0/",       // ISO 10110-2 应力双折射 (代码0/)
            "1/",       // ISO 10110-3 气泡夹杂 (代码1/)
            "2/"        // ISO 10110-4 不均匀性条纹 (代码2/)
        };
        #endregion

        public override Bounding bounding
        {
            get
            {
                double totalWidth = ColumnWidth * 3 * Scale;
                double totalHeight = (TitleRowHeight + RowHeight * 8) * Scale;
                return new Bounding(Position, totalWidth, totalHeight);
            }
        }

        #region 构造函数
        public TechnicalRequirementTable()
        {
        }

        public TechnicalRequirementTable(Vector2 position)
        {
            Position = position;
        }
        #endregion

        #region 核心方法
        protected void Generate()
        {
            _markEntities.Clear();
            var markColor = lcdb.Colors.Color.FromColor(System.Drawing.Color.Black);

            double totalWidth = ColumnWidth * 3 * Scale;
            double currentY = Position.Y;

            // 1. 绘制标题行(抬头)
            if (ShowTitle)
                currentY = GenerateTitleRow(currentY, totalWidth, markColor);

            // 2. 绘制列标题
            currentY = GenerateColumnHeaders(currentY, markColor);

            // 3. 绘制数据行
            currentY = GenerateDataRows(currentY, markColor);

            // 4. 绘制外边框
            GenerateOuterBorder(totalWidth, Position.Y - currentY, markColor);
        }

        private double GenerateTitleRow(double startY, double totalWidth, lcdb.Colors.Color color)
        {
            double rowHeight = TitleRowHeight * Scale;
            double y = startY;

            // 标题背景边框
            _markEntities.Add(new Line(
                new Vector2(Position.X, y),
                new Vector2(Position.X + totalWidth, y)
            ) { color = color, lineWeight = BorderLineWeight });

            _markEntities.Add(new Line(
                new Vector2(Position.X, y - rowHeight),
                new Vector2(Position.X + totalWidth, y - rowHeight)
            ) { color = color, lineWeight = BorderLineWeight });

            // 标题文字
            var titleText = new Text();
            titleText.Value = "技术要求";
            titleText.Position = new Vector3(Position.X + totalWidth / 2, y - rowHeight / 2, 0);
            titleText.Height = TextHeight * 1.2 * Scale;
            titleText.color = color;
            titleText.alignment = TextAlignment.CenterMiddle;
            _markEntities.Add(titleText);

            return y - rowHeight;
        }

        private double GenerateColumnHeaders(double startY, lcdb.Colors.Color color)
        {
            double rowHeight = RowHeight * Scale;
            double colWidth = ColumnWidth * Scale;
            double y = startY;

            // 列标题: 左表面 | 材料技术要求 | 右表面
            string[] headers = { "左表面", "材料技术要求", "右表面" };

            for (int i = 0; i < 3; i++)
            {
                double x = Position.X + i * colWidth;

                // 列边框
                _markEntities.Add(new Line(
                    new Vector2(x, y),
                    new Vector2(x, y - rowHeight)
                ) { color = color, lineWeight = BorderLineWeight });

                // 列标题文字
                var headerText = new Text();
                headerText.Value = headers[i];
                headerText.Position = new Vector3(x + colWidth / 2, y - rowHeight / 2, 0);
                headerText.Height = TextHeight * Scale;
                headerText.color = color;
                headerText.alignment = TextAlignment.CenterMiddle;
                _markEntities.Add(headerText);
            }

            // 右边框
            _markEntities.Add(new Line(
                new Vector2(Position.X + colWidth * 3, y),
                new Vector2(Position.X + colWidth * 3, y - rowHeight)
            ) { color = color, lineWeight = BorderLineWeight });

            // 底部边框
            _markEntities.Add(new Line(
                new Vector2(Position.X, y - rowHeight),
                new Vector2(Position.X + colWidth * 3, y - rowHeight)
            ) { color = color, lineWeight = BorderLineWeight });

            return y - rowHeight;
        }

        private double GenerateDataRows(double startY, lcdb.Colors.Color color)
        {
            double rowHeight = RowHeight * Scale;
            double colWidth = ColumnWidth * Scale;
            double y = startY;

            // 数据行数量（取表面和材料行的最大值）
            int maxRows = Math.Max(RowLabels.Length, MaterialRowLabels.Length);

            for (int row = 0; row < maxRows; row++)
            {
                // 绘制行
                y = GenerateDataRow(y, rowHeight, colWidth, row, color);
            }

            return y;
        }

        private double GenerateDataRow(double y, double rowHeight, double colWidth, int rowIndex, lcdb.Colors.Color color)
        {
            // 左表面列
            string leftLabel = rowIndex < RowLabels.Length ? RowLabels[rowIndex] : "";
            string leftValue = GetLeftSurfaceValue(rowIndex);
            GenerateCell(Position.X, y, colWidth, rowHeight, leftLabel, leftValue, color);

            // 材料列
            string materialLabel = rowIndex < MaterialRowLabels.Length ? MaterialRowLabels[rowIndex] : "";
            string materialValue = GetMaterialValue(rowIndex);
            GenerateCell(Position.X + colWidth, y, colWidth, rowHeight, materialLabel, materialValue, color);

            // 右表面列
            string rightLabel = rowIndex < RowLabels.Length ? RowLabels[rowIndex] : "";
            string rightValue = GetRightSurfaceValue(rowIndex);
            GenerateCell(Position.X + colWidth * 2, y, colWidth, rowHeight, rightLabel, rightValue, color);

            // 右边框
            _markEntities.Add(new Line(
                new Vector2(Position.X + colWidth * 3, y),
                new Vector2(Position.X + colWidth * 3, y - rowHeight)
            ) { color = color, lineWeight = BorderLineWeight });

            // 底部边框
            _markEntities.Add(new Line(
                new Vector2(Position.X, y - rowHeight),
                new Vector2(Position.X + colWidth * 3, y - rowHeight)
            ) { color = color, lineWeight = BorderLineWeight });

            return y - rowHeight;
        }

        private void GenerateCell(double x, double y, double width, double height, string label, string value, lcdb.Colors.Color color)
        {
            // 左边框
            _markEntities.Add(new Line(
                new Vector2(x, y),
                new Vector2(x, y - height)
            ) { color = color, lineWeight = BorderLineWeight });

            // 标签文字（左对齐）
            if (!string.IsNullOrEmpty(label))
            {
                var labelText = new Text();
                labelText.Value = label;
                labelText.Position = new Vector3(x + 2 * Scale, y - height / 2, 0);
                labelText.Height = TextHeight * 0.9 * Scale;
                labelText.color = color;
                labelText.alignment = TextAlignment.LeftMiddle;
                _markEntities.Add(labelText);
            }

            // 数值文字（右对齐）
            if (!string.IsNullOrEmpty(value))
            {
                var valueText = new Text();
                valueText.Value = value;
                valueText.Position = new Vector3(x + width - 2 * Scale, y - height / 2, 0);
                valueText.Height = TextHeight * 0.9 * Scale;
                valueText.color = color;
                valueText.alignment = TextAlignment.RightMiddle;
                _markEntities.Add(valueText);
            }
        }

        private void GenerateOuterBorder(double width, double height, lcdb.Colors.Color color)
        {
            // 左边框
            _markEntities.Add(new Line(
                new Vector2(Position.X, Position.Y),
                new Vector2(Position.X, Position.Y - height)
            ) { color = color, lineWeight = BorderLineWeight });
        }

        private string GetLeftSurfaceValue(int rowIndex)
        {
            if (LeftSurface == null) return "";

            // 表面列的行顺序: R, Φe, 倒角, 表面, 3/(面形), 4/(中心), 5/(缺陷), 6/(纹理)
            switch (rowIndex)
            {
                case 0: return FormatRadius(LeftSurface.Radius);
                case 1: return FormatAperture(LeftSurface.EffectiveAperture);
                case 2: return LeftSurface.ChamferRequirement;
                case 3: return LeftSurface.SurfaceRequirement;
                case 4: return LeftSurface.ISO10110_5_Value; // 代码3/ → ISO 10110-5 面形公差
                case 5: return LeftSurface.ISO10110_6_Value; // 代码4/ → ISO 10110-6 中心偏差
                case 6: return LeftSurface.ISO10110_3_Value; // 代码5/ → ISO 10110-7 表面缺陷 (注:属性名历史遗留)
                case 7: return LeftSurface.ISO10110_4_Value; // 代码6/ → ISO 10110-8 表面纹理 (注:属性名历史遗留)
                default: return "";
            }
        }

        private string GetMaterialValue(int rowIndex)
        {
            if (Material == null) return "";

            // 材料列的行顺序: n, ν, 0/(应力), 1/(气泡), 2/(不均匀)
            switch (rowIndex)
            {
                case 0: return FormatRefractiveIndex(Material.RefractiveIndex, Material.RefractiveIndexTolerance);
                case 1: return FormatAbbeNumber(Material.AbbeNumber, Material.AbbeNumberTolerance);
                case 2: return Material.ISO10110_0_Value; // 代码0/ → ISO 10110-2 应力双折射
                case 3: return Material.ISO10110_1_Value; // 代码1/ → ISO 10110-3 气泡夹杂
                case 4: return Material.ISO10110_2_Value; // 代码2/ → ISO 10110-4 不均匀性条纹
                default: return "";
            }
        }

        private string GetRightSurfaceValue(int rowIndex)
        {
            if (RightSurface == null) return "";

            // 表面列的行顺序: R, Φe, 倒角, 表面, 3/(面形), 4/(中心), 5/(缺陷), 6/(纹理)
            switch (rowIndex)
            {
                case 0: return FormatRadius(RightSurface.Radius);
                case 1: return FormatAperture(RightSurface.EffectiveAperture);
                case 2: return RightSurface.ChamferRequirement;
                case 3: return RightSurface.SurfaceRequirement;
                case 4: return RightSurface.ISO10110_5_Value; // 代码3/ → ISO 10110-5 面形公差
                case 5: return RightSurface.ISO10110_6_Value; // 代码4/ → ISO 10110-6 中心偏差
                case 6: return RightSurface.ISO10110_3_Value; // 代码5/ → ISO 10110-7 表面缺陷 (注:属性名历史遗留)
                case 7: return RightSurface.ISO10110_4_Value; // 代码6/ → ISO 10110-8 表面纹理 (注:属性名历史遗留)
                default: return "";
            }
        }

        private string FormatRadius(double radius)
        {
            if (double.IsNaN(radius) || Math.Abs(radius) < 0.001) return "∞";
            return $"{radius:F2}";
        }

        private string FormatAperture(double aperture)
        {
            if (double.IsNaN(aperture) || aperture <= 0) return "";
            return $"Φ{aperture:F1}";
        }

        private string FormatRefractiveIndex(double n, double tolerance)
        {
            if (double.IsNaN(n) || n <= 0) return "";
            if (tolerance > 0)
                return $"{n:F5}±{tolerance:F5}";
            return $"{n:F5}";
        }

        private string FormatAbbeNumber(double v, double tolerance)
        {
            if (double.IsNaN(v) || v <= 0) return "";
            if (tolerance > 0)
                return $"{v:F1}±{tolerance:F1}";
            return $"{v:F1}";
        }
        #endregion

        #region 绘制方法
        public override void Draw(IGraphicsDraw gd)
        {
            if (_markEntities.Count == 0) Generate();
            foreach (var entity in _markEntities) entity.Draw(gd);
        }

#if WINDOWS
        public void Draw(Graphics g, float scale)
        {
            // GDI+ 绘制实现（预览用）
            using (var pen = new Pen(System.Drawing.Color.Black, 1.0f * scale))
            using (var brush = new SolidBrush(System.Drawing.Color.Black))
            using (var font = new Font("Arial", 8 * scale))
            {
                float width = (float)(ColumnWidth * 3 * Scale * scale);
                float height = (float)((TitleRowHeight + RowHeight * 8) * Scale * scale);
                var rect = new RectangleF((float)Position.X * scale, (float)Position.Y * scale - height, width, height);
                g.DrawRectangle(pen, Rectangle.Round(rect));
                g.DrawString(MarkText, font, brush, rect.X + width / 2, rect.Y + 5,
                    new StringFormat { Alignment = StringAlignment.Center });
            }
        }
#endif
        #endregion

        #region IOpticalMark 接口实现
        public RectangleF GetBounds()
        {
            var bound = bounding;
            return new RectangleF((float)bound.left, (float)bound.bottom, (float)bound.width, (float)bound.height);
        }

        public bool Validate()
        {
            return true;
        }

        public Dictionary<string, object> GetProperties()
        {
            return new Dictionary<string, object>
            {
                { "Position", Position },
                { "Scale", Scale },
                { "ColumnWidth", ColumnWidth },
                { "RowHeight", RowHeight },
                { "TextHeight", TextHeight },
                { "IsCementedElement", IsCementedElement }
            };
        }

        public void SetProperties(Dictionary<string, object> properties)
        {
            if (properties.ContainsKey("Position")) Position = (Vector2)properties["Position"];
            if (properties.ContainsKey("Scale")) Scale = (double)properties["Scale"];
            if (properties.ContainsKey("ColumnWidth")) ColumnWidth = (double)properties["ColumnWidth"];
            if (properties.ContainsKey("RowHeight")) RowHeight = (double)properties["RowHeight"];
            if (properties.ContainsKey("TextHeight")) TextHeight = (double)properties["TextHeight"];
            if (properties.ContainsKey("IsCementedElement")) IsCementedElement = (bool)properties["IsCementedElement"];
            _markEntities.Clear();
        }

        IOpticalMark IOpticalMark.Clone() => Clone() as TechnicalRequirementTable;

        public string GetDescription()
        {
            return $"技术要求表格 - GB/T 13323格式";
        }
        #endregion

        #region Entity 重写方法
        protected override DBObject CreateInstance() => new TechnicalRequirementTable();

        public override object Clone()
        {
            var table = base.Clone() as TechnicalRequirementTable;
            table.Position = Position;
            table.Scale = Scale;
            table.ColumnWidth = ColumnWidth;
            table.RowHeight = RowHeight;
            table.TextHeight = TextHeight;
            table.TitleRowHeight = TitleRowHeight;
            table.ShowTitle = ShowTitle;
            table.BorderLineWeight = BorderLineWeight;
            table.IsCementedElement = IsCementedElement;
            table.AdhesiveInfo = AdhesiveInfo;
            table.LeftSurface = LeftSurface?.Clone();
            table.RightSurface = RightSurface?.Clone();
            table.Material = Material?.Clone();
            table._markEntities = new List<Entity>();
            return table;
        }

        public override void Translate(Vector2 translation)
        {
            Position += translation;
            _markEntities.Clear();
        }

        public override void Rotate(Vector2 center, double angle)
        {
            Position = Vector2.RotateInRadian(Position, center, angle);
            Rotation += angle * 180 / Math.PI;
            _markEntities.Clear();
        }

        public override void TransformBy(Matrix3 transform)
        {
            Position = transform * Position;
            Vector2 scaleVector = new Vector2(Scale, 0);
            Scale = (transform * scaleVector).length;
            _markEntities.Clear();
        }

        public override List<GripPoint> GetGripPoints()
        {
            var gripPoints = new List<GripPoint>();
            gripPoints.Add(new GripPoint(GripPointType.Center, Position));

            double totalWidth = ColumnWidth * 3 * Scale;
            double totalHeight = (TitleRowHeight + RowHeight * 8) * Scale;
            gripPoints.Add(new GripPoint(GripPointType.Corner, Position + new Vector2(totalWidth, -totalHeight)));

            return gripPoints;
        }

        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            var snapPnts = new List<ObjectSnapPoint>();
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Corner, Position));

            double totalWidth = ColumnWidth * 3 * Scale;
            double totalHeight = (TitleRowHeight + RowHeight * 8) * Scale;
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Corner, Position + new Vector2(totalWidth, 0)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Corner, Position + new Vector2(0, -totalHeight)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Corner, Position + new Vector2(totalWidth, -totalHeight)));

            return snapPnts;
        }

        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            switch (index)
            {
                case 0:
                    Position = newPosition;
                    break;
                case 1:
                    // 调整表格大小
                    var delta = newPosition - Position;
                    if (delta.X > 0) ColumnWidth = delta.X / 3 / Scale;
                    break;
            }
            _markEntities.Clear();
        }
        #endregion
    }

    #region 数据结构

    /// <summary>
    /// 表面技术要求数据
    /// </summary>
    public class SurfaceRequirementData
    {
        /// <summary>曲率半径 (mm)</summary>
        public double Radius { get; set; } = double.NaN;

        /// <summary>有效口径 (mm)</summary>
        public double EffectiveAperture { get; set; } = double.NaN;

        /// <summary>倒角要求</summary>
        public string ChamferRequirement { get; set; } = "";

        /// <summary>表面要求</summary>
        public string SurfaceRequirement { get; set; } = "";

        /// <summary>ISO 10110-7 表面缺陷 (绘图代码5/)</summary>
        public string ISO10110_3_Value { get; set; } = "";  // 注:属性名历史遗留,实际用于代码5/

        /// <summary>ISO 10110-8 表面纹理 (绘图代码6/)</summary>
        public string ISO10110_4_Value { get; set; } = "";  // 注:属性名历史遗留,实际用于代码6/

        /// <summary>ISO 10110-5 面形公差 (绘图代码3/)</summary>
        public string ISO10110_5_Value { get; set; } = "";

        /// <summary>ISO 10110-6 中心偏差 (绘图代码4/)</summary>
        public string ISO10110_6_Value { get; set; } = "";

        public SurfaceRequirementData Clone()
        {
            return new SurfaceRequirementData
            {
                Radius = Radius,
                EffectiveAperture = EffectiveAperture,
                ChamferRequirement = ChamferRequirement,
                SurfaceRequirement = SurfaceRequirement,
                ISO10110_3_Value = ISO10110_3_Value,
                ISO10110_4_Value = ISO10110_4_Value,
                ISO10110_5_Value = ISO10110_5_Value,
                ISO10110_6_Value = ISO10110_6_Value
            };
        }
    }

    /// <summary>
    /// 材料技术要求数据
    /// </summary>
    public class MaterialRequirementData
    {
        /// <summary>玻璃牌号</summary>
        public string GlassGrade { get; set; } = "";

        /// <summary>折射率 ne</summary>
        public double RefractiveIndex { get; set; } = double.NaN;

        /// <summary>折射率公差</summary>
        public double RefractiveIndexTolerance { get; set; } = 0.0005;

        /// <summary>阿贝数 νe</summary>
        public double AbbeNumber { get; set; } = double.NaN;

        /// <summary>阿贝数公差</summary>
        public double AbbeNumberTolerance { get; set; } = 0.5;

        /// <summary>ISO 10110-2 应力双折射 (绘图代码0/)</summary>
        public string ISO10110_0_Value { get; set; } = "";

        /// <summary>ISO 10110-3 气泡和夹杂物 (绘图代码1/)</summary>
        public string ISO10110_1_Value { get; set; } = "";

        /// <summary>ISO 10110-4 不均匀性和条纹 (绘图代码2/)</summary>
        public string ISO10110_2_Value { get; set; } = "";

        public MaterialRequirementData Clone()
        {
            return new MaterialRequirementData
            {
                GlassGrade = GlassGrade,
                RefractiveIndex = RefractiveIndex,
                RefractiveIndexTolerance = RefractiveIndexTolerance,
                AbbeNumber = AbbeNumber,
                AbbeNumberTolerance = AbbeNumberTolerance,
                ISO10110_0_Value = ISO10110_0_Value,
                ISO10110_1_Value = ISO10110_1_Value,
                ISO10110_2_Value = ISO10110_2_Value
            };
        }
    }

    #endregion
}
