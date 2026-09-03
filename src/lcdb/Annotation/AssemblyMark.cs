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
    /// 装配标记实现
    /// 用于标注光学元件的装配要求和工艺信息
    /// </summary>
    public class AssemblyMark : Entity, IOpticalMark
    {
        public override string className => "AssemblyMark";

        #region IOpticalMark 接口属性?
        /// <summary>
        /// 标记类型
        /// </summary>
        public OpticalMarkType MarkType => OpticalMarkType.Assembly;

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
        public string MarkText { get; set; } = "装配-L1";

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

        #region 装配标记特定属性?
        /// <summary>
        /// 装配类型
        /// </summary>
        public AssemblyType AssemblyCategory { get; set; } = AssemblyType.Optical;

        /// <summary>
        /// 装配序号
        /// </summary>
        public string AssemblySequence { get; set; } = "L1";

        /// <summary>
        /// 装配精度等级
        /// </summary>
        public AssemblyPrecision Precision { get; set; } = AssemblyPrecision.High;

        /// <summary>
        /// 装配工艺要求
        /// </summary>
        public string ProcessRequirement { get; set; } = "光轴对齐,无应力装配";

        /// <summary>
        /// 装配工具
        /// </summary>
        public string AssemblyTools { get; set; } = "光学装配台,扭矩扳手";

        /// <summary>
        /// 装配扭矩（N·m）
        /// </summary>
        public double AssemblyTorque { get; set; } = 0.5;

        /// <summary>
        /// 装配间隙/过盈量（mm）
        /// </summary>
        public double AssemblyFit { get; set; } = 0.005;

        /// <summary>
        /// 装配方向要求
        /// </summary>
        public AssemblyDirection Direction { get; set; } = AssemblyDirection.Axial;

        /// <summary>
        /// 胶合剂类型
        /// </summary>
        public string AdhesiveType { get; set; } = "";

        /// <summary>
        /// 固化条件
        /// </summary>
        public string CuringCondition { get; set; } = "";

        /// <summary>
        /// 装配环境要求
        /// </summary>
        public string EnvironmentRequirement { get; set; } = "无尘环境,23±2℃";

        /// <summary>
        /// 质量控制要求
        /// </summary>
        public string QualityControl { get; set; } = "装配后检验光轴偏差";

        /// <summary>
        /// 装配状态
        /// </summary>
        public AssemblyStatus Status { get; set; } = AssemblyStatus.NotAssembled;

        /// <summary>
        /// 装配人员
        /// </summary>
        public string AssemblyWorker { get; set; } = "";

        /// <summary>
        /// 装配日期
        /// </summary>
        public DateTime AssemblyDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 标记框架大小
        /// </summary>
        public Vector2 FrameSize { get; set; } = new Vector2(50, 40);

        /// <summary>
        /// 装配标准
        /// </summary>
        public string AssemblyStandard { get; set; } = "企业标准";

        #endregion

        private List<Entity> _markEntities = new List<Entity>();

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                // 框矩形 ∪ 外部说明文字范围(文字在单侧, 不对称外扩 — 旧实现偏心且虚大)。
                double hw = FrameSize.X * 0.5 * Scale, hh = FrameSize.Y * 0.5 * Scale;
                double minX = Position.X - hw, maxX = Position.X + hw;
                double minY = Position.Y - hh, maxY = Position.Y + hh;
                if (ShowText)
                {
                    var t = Position + TextOffset;
                    const double textHalfW = 40, textHalfH = 7.5;
                    minX = Math.Min(minX, t.X - textHalfW); maxX = Math.Max(maxX, t.X + textHalfW);
                    minY = Math.Min(minY, t.Y - textHalfH); maxY = Math.Max(maxY, t.Y + textHalfH);
                }
                return new Bounding(new Vector2(minX, minY), new Vector2(maxX, maxY));
            }
        }

        #region 构造函数?
        /// <summary>
        /// 默认构造函数?        /// </summary>
        public AssemblyMark()
        {
            UpdateMarkText();
        }

        /// <summary>
        /// 带参数构造函数?        /// </summary>
        public AssemblyMark(Vector2 position, AssemblyType assemblyType, string sequence)
        {
            Position = position;
            AssemblyCategory = assemblyType;
            AssemblySequence = sequence;
            UpdateMarkText();
        }

        #endregion

        #region 核心方法

        /// <summary>
        /// 更新标记文本
        /// </summary>
        private void UpdateMarkText()
        {
            string assemblyDesc = GetAssemblyTypeDescription();
            MarkText = $"{assemblyDesc}-{AssemblySequence}";
        }

        /// <summary>
        /// 获取装配类型描述
        /// </summary>
        private string GetAssemblyTypeDescription()
        {
            switch (AssemblyCategory)
            {
                case AssemblyType.Optical: return "光学装配";
                case AssemblyType.Mechanical: return "机械装配";
                case AssemblyType.Bonding: return "胶合装配";
                case AssemblyType.Threading: return "螺纹装配";
                case AssemblyType.Pressing: return "压装";
                case AssemblyType.Welding: return "焊接";
                case AssemblyType.Clamping: return "夹装";
                case AssemblyType.Snap: return "卡装";
                default: return "装配";
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

        /// <summary>绕 Position 旋转 Rotation 度 (Rotation == 0 时原样返回)。文字锚点随框旋转, 字形保持水平。</summary>
        private Vector2 R(Vector2 p) =>
            Rotation == 0.0 ? p : Vector2.RotateInRadian(p, Position, Rotation * Math.PI / 180.0);

        /// <summary>
        /// 生成主标记符号 — 工厂惯例: 矩形框 + 结构化文字标签 (GB/T 4458.4 装配备注规范).
        /// 旧实现 (八角形 + 8 种装配子图标 + 序号 + 状态符号) 无 GB/ISO 标准依据, 已移除.
        /// </summary>
        private void GenerateMainSymbol()
        {
            var markColor = color;
            double halfWidth = FrameSize.X * 0.5 * Scale;
            double halfHeight = FrameSize.Y * 0.5 * Scale;

            GenerateRectangleFrame(halfWidth, halfHeight, markColor);
            GenerateAssemblyLabel(halfHeight, markColor);
        }

        /// <summary>
        /// 矩形框 (4 条 Line, 应用 Rotation).
        /// </summary>
        private void GenerateRectangleFrame(double halfWidth, double halfHeight, lcdb.Colors.Color color)
        {
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
                _markEntities.Add(new Line(pts[i], pts[(i + 1) % 4]) { color = color });
            }
        }

        /// <summary>
        /// 框内 2 行结构化文字: 第一行 "类型: 序号", 第二行 "状态".
        /// </summary>
        private void GenerateAssemblyLabel(double halfHeight, lcdb.Colors.Color color)
        {
            var p1 = R(new Vector2(Position.X, Position.Y + halfHeight * 0.35));
            var line1 = new Text
            {
                Value = MarkText,
                Position = new Vector3(p1.X, p1.Y, 0.0),
                Height = 4 * Scale,
                color = color,
                alignment = lcdb.TextAlignment.CenterMiddle,
            };
            _markEntities.Add(line1);

            var p2 = R(new Vector2(Position.X, Position.Y - halfHeight * 0.35));
            var line2 = new Text
            {
                Value = GetStatusDescription(),
                Position = new Vector3(p2.X, p2.Y, 0.0),
                Height = 3 * Scale,
                color = GetStatusColor(),
                alignment = lcdb.TextAlignment.CenterMiddle,
            };
            _markEntities.Add(line2);
        }

        /// <summary>
        /// 生成外部说明文本
        /// </summary>
        private void GenerateText()
        {
            var textPosition = R(Position + TextOffset);
            var text = new Text();
            text.Value = GetDetailedDescription();
            text.Position = new Vector3(textPosition.X, textPosition.Y, 0.0);
            text.Height = 5 * Scale;
            text.color = color;
            text.alignment = lcdb.TextAlignment.CenterMiddle;
            _markEntities.Add(text);
        }

        /// <summary>
        /// 获取详细描述
        /// </summary>
        private string GetDetailedDescription()
        {
            string assemblyDesc = GetAssemblyTypeDescription();
            string precisionDesc = GetPrecisionDescription();
            string statusDesc = GetStatusDescription();
            return $"{assemblyDesc}: {AssemblySequence}, {precisionDesc}精度 - {statusDesc}";
        }

        /// <summary>
        /// 获取精度描述
        /// </summary>
        private string GetPrecisionDescription()
        {
            switch (Precision)
            {
                case AssemblyPrecision.Standard: return "标准";
                case AssemblyPrecision.High: return "高";
                case AssemblyPrecision.Precision: return "精密";
                case AssemblyPrecision.UltraPrecision: return "超精密";
                default: return "标准";
            }
        }

        /// <summary>
        /// 获取状态描述?        /// </summary>
        private string GetStatusDescription()
        {
            switch (Status)
            {
                case AssemblyStatus.NotAssembled: return "未装配";
                case AssemblyStatus.InProgress: return "装配中";
                case AssemblyStatus.Assembled: return "已装配";
                case AssemblyStatus.Rework: return "重新装配";
                default: return "未装配";
            }
        }

        /// <summary>
        /// 获取状态颜?        /// </summary>
        private lcdb.Colors.Color GetStatusColor()
        {
            switch (Status)
            {
                case AssemblyStatus.Assembled:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Green);      // 已装?- 绿色
                case AssemblyStatus.InProgress:
                    // WCAG AA: Yellow (255,255,0) 白底 1.07:1 → 不可见; 改 #806000 (5.9:1) 深橄榄金
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.FromArgb(128, 96, 0));
                case AssemblyStatus.NotAssembled:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Gray);       // 未装配?- 灰色
                case AssemblyStatus.Rework:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Red);        // 重新装配 - 红色
                default:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Gray);
            }
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
        /// GDI+ 路径(WinForms 已封存): 旧八角形+子图标渲染与现役 Generate()(矩形框+文字)是两个不同符号,
        /// 已删避免"同一标记两种画法"; 空实现满足接口。
        /// </summary>
        public void Draw(Graphics g, float scale) { }
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
            if (string.IsNullOrEmpty(AssemblySequence))
                return false;

            if (string.IsNullOrEmpty(ProcessRequirement))
                return false;

            if (AssemblyTorque < 0)
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
                { "AssemblyCategory", AssemblyCategory },
                { "AssemblySequence", AssemblySequence },
                { "Precision", Precision },
                { "ProcessRequirement", ProcessRequirement },
                { "AssemblyTools", AssemblyTools },
                { "AssemblyTorque", AssemblyTorque },
                { "AssemblyFit", AssemblyFit },
                { "Direction", Direction },
                { "AdhesiveType", AdhesiveType },
                { "CuringCondition", CuringCondition },
                { "EnvironmentRequirement", EnvironmentRequirement },
                { "QualityControl", QualityControl },
                { "Status", Status },
                { "AssemblyWorker", AssemblyWorker },
                { "AssemblyDate", AssemblyDate },
                { "FrameSize", FrameSize },
                { "AssemblyStandard", AssemblyStandard }
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
            if (properties.ContainsKey("AssemblyCategory"))
                AssemblyCategory = (AssemblyType)properties["AssemblyCategory"];
            if (properties.ContainsKey("AssemblySequence"))
                AssemblySequence = (string)properties["AssemblySequence"];
            if (properties.ContainsKey("Precision"))
                Precision = (AssemblyPrecision)properties["Precision"];
            if (properties.ContainsKey("ProcessRequirement"))
                ProcessRequirement = (string)properties["ProcessRequirement"];
            if (properties.ContainsKey("AssemblyTools"))
                AssemblyTools = (string)properties["AssemblyTools"];
            if (properties.ContainsKey("AssemblyTorque"))
                AssemblyTorque = (double)properties["AssemblyTorque"];
            if (properties.ContainsKey("AssemblyFit"))
                AssemblyFit = (double)properties["AssemblyFit"];
            if (properties.ContainsKey("Direction"))
                Direction = (AssemblyDirection)properties["Direction"];
            if (properties.ContainsKey("AdhesiveType"))
                AdhesiveType = (string)properties["AdhesiveType"];
            if (properties.ContainsKey("CuringCondition"))
                CuringCondition = (string)properties["CuringCondition"];
            if (properties.ContainsKey("EnvironmentRequirement"))
                EnvironmentRequirement = (string)properties["EnvironmentRequirement"];
            if (properties.ContainsKey("QualityControl"))
                QualityControl = (string)properties["QualityControl"];
            if (properties.ContainsKey("Status"))
                Status = (AssemblyStatus)properties["Status"];
            if (properties.ContainsKey("AssemblyWorker"))
                AssemblyWorker = (string)properties["AssemblyWorker"];
            if (properties.ContainsKey("AssemblyDate"))
                AssemblyDate = (DateTime)properties["AssemblyDate"];
            if (properties.ContainsKey("FrameSize"))
                FrameSize = (Vector2)properties["FrameSize"];
            if (properties.ContainsKey("AssemblyStandard"))
                AssemblyStandard = (string)properties["AssemblyStandard"];

            UpdateMarkText();
        }

        /// <summary>
        /// 克隆标记
        /// </summary>
        IOpticalMark IOpticalMark.Clone()
        {
            return Clone() as AssemblyMark;
        }

        /// <summary>
        /// 获取标记描述
        /// </summary>
        public string GetDescription()
        {
            return $"装配标记 - {GetAssemblyTypeDescription()}: {AssemblySequence}, {GetPrecisionDescription()}精度, {GetStatusDescription()}";
        }

        #endregion

        #region Entity 重写方法

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new AssemblyMark();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            AssemblyMark mark = base.Clone() as AssemblyMark;
            mark.Position = Position;
            mark.Scale = Scale;
            mark.MarkText = MarkText;
            mark.ShowText = ShowText;
            mark.TextOffset = TextOffset;
            mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.AssemblyCategory = AssemblyCategory;
            mark.AssemblySequence = AssemblySequence;
            mark.Precision = Precision;
            mark.ProcessRequirement = ProcessRequirement;
            mark.AssemblyTools = AssemblyTools;
            mark.AssemblyTorque = AssemblyTorque;
            mark.AssemblyFit = AssemblyFit;
            mark.Direction = Direction;
            mark.AdhesiveType = AdhesiveType;
            mark.CuringCondition = CuringCondition;
            mark.EnvironmentRequirement = EnvironmentRequirement;
            mark.QualityControl = QualityControl;
            mark.Status = Status;
            mark.AssemblyWorker = AssemblyWorker;
            mark.AssemblyDate = AssemblyDate;
            mark.FrameSize = FrameSize;
            mark.AssemblyStandard = AssemblyStandard;
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
    /// 装配类型枚举
    /// </summary>
    public enum AssemblyType
    {
        /// <summary>
        /// 光学装配
        /// </summary>
        Optical = 0,
        
        /// <summary>
        /// 机械装配
        /// </summary>
        Mechanical = 1,
        
        /// <summary>
        /// 胶合装配
        /// </summary>
        Bonding = 2,
        
        /// <summary>
        /// 螺纹装配
        /// </summary>
        Threading = 3,
        
        /// <summary>
        /// 压装
        /// </summary>
        Pressing = 4,
        
        /// <summary>
        /// 焊接
        /// </summary>
        Welding = 5,
        
        /// <summary>
        /// 夹装
        /// </summary>
        Clamping = 6,
        
        /// <summary>
        /// 卡装
        /// </summary>
        Snap = 7
    }

    /// <summary>
    /// 装配精度枚举
    /// </summary>
    public enum AssemblyPrecision
    {
        /// <summary>
        /// 标准精度
        /// </summary>
        Standard = 0,
        
        /// <summary>
        /// 高精度?        /// </summary>
        High = 1,
        
        /// <summary>
        /// 精密装配
        /// </summary>
        Precision = 2,
        
        /// <summary>
        /// 超精密装?        /// </summary>
        UltraPrecision = 3
    }

    /// <summary>
    /// 装配方向枚举
    /// </summary>
    public enum AssemblyDirection
    {
        /// <summary>
        /// 轴向装配
        /// </summary>
        Axial = 0,
        
        /// <summary>
        /// 径向装配
        /// </summary>
        Radial = 1,
        
        /// <summary>
        /// 角向装配
        /// </summary>
        Angular = 2,
        
        /// <summary>
        /// 复合装配
        /// </summary>
        Combined = 3
    }

    /// <summary>
    /// 装配状态枚?    /// </summary>
    public enum AssemblyStatus
    {
        /// <summary>
        /// 未装配?        /// </summary>
        NotAssembled = 0,
        
        /// <summary>
        /// 装配?        /// </summary>
        InProgress = 1,
        
        /// <summary>
        /// 已装?        /// </summary>
        Assembled = 2,
        
        /// <summary>
        /// 需要重新装?        /// </summary>
        Rework = 3
    }
}