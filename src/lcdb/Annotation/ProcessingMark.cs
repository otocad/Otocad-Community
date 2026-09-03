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
    /// 加工标记实现
    /// 用于标注光学元件的加工要求和工艺信息
    /// </summary>
    public class ProcessingMark : Entity, IOpticalMark
    {
        public override string className => "ProcessingMark";

        #region IOpticalMark 接口属性
        /// <summary>
        /// 标记类型
        /// </summary>
        public OpticalMarkType MarkType => OpticalMarkType.Processing;

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
        public string MarkText { get; set; } = "抛光-P3";

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

        #region 加工标记特定属性
        /// <summary>
        /// 加工类型
        /// </summary>
        public ProcessingType ProcessType { get; set; } = ProcessingType.Polishing;

        /// <summary>
        /// 加工等级
        /// </summary>
        public string ProcessGrade { get; set; } = "P3";

        /// <summary>
        /// 工艺要求
        /// </summary>
        public string ProcessRequirement { get; set; } = "精抛光";

        /// <summary>
        /// 加工精度
        /// </summary>
        public ProcessingPrecision Precision { get; set; } = ProcessingPrecision.High;

        /// <summary>
        /// 表面处理
        /// </summary>
        public SurfaceTreatment Treatment { get; set; } = SurfaceTreatment.Polished;

        /// <summary>
        /// 特殊要求
        /// </summary>
        public string SpecialRequirement { get; set; } = "";

        /// <summary>
        /// 加工设备
        /// </summary>
        public string Equipment { get; set; } = "光学抛光机";

        /// <summary>
        /// 工艺参数
        /// </summary>
        public string ProcessParameters { get; set; } = "转速:300rpm";

        /// <summary>
        /// 质量等级
        /// </summary>
        public QualityGrade Quality { get; set; } = QualityGrade.Precision;

        /// <summary>
        /// 标记框架大小
        /// </summary>
        public Vector2 FrameSize { get; set; } = new Vector2(45, 35);

        /// <summary>
        /// 工艺标准
        /// </summary>
        public string ProcessStandard { get; set; } = "GB/T 1185-2006";

        #endregion

        private List<Entity> _markEntities = new List<Entity>();

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                // 框矩形 ∪ 外部说明文字范围(文字在 Position+TextOffset 单侧, 不能对称外扩——
                // 旧实现把 |TextOffset| 对称加在两侧, 选择框偏心且虚大)。
                double hw = FrameSize.X * 0.5 * Scale, hh = FrameSize.Y * 0.5 * Scale;
                double minX = Position.X - hw, maxX = Position.X + hw;
                double minY = Position.Y - hh, maxY = Position.Y + hh;
                if (ShowText)
                {
                    var t = Position + TextOffset;                 // 说明文字锚点 (CenterMiddle)
                    const double textHalfW = 40, textHalfH = 7.5;  // 估算文字半宽/半高
                    minX = Math.Min(minX, t.X - textHalfW); maxX = Math.Max(maxX, t.X + textHalfW);
                    minY = Math.Min(minY, t.Y - textHalfH); maxY = Math.Max(maxY, t.Y + textHalfH);
                }
                return new Bounding(new Vector2(minX, minY), new Vector2(maxX, maxY));
            }
        }

        #region 构造函数
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ProcessingMark()
        {
            UpdateMarkText();
        }

        /// <summary>
        /// 带参数构造函数
        /// </summary>
        public ProcessingMark(Vector2 position, ProcessingType processType, string grade)
        {
            Position = position;
            ProcessType = processType;
            ProcessGrade = grade;
            UpdateMarkText();
        }

        #endregion

        #region 核心方法

        /// <summary>
        /// 更新标记文本
        /// </summary>
        private void UpdateMarkText()
        {
            string processDesc = GetProcessTypeDescription();
            MarkText = $"{processDesc}-{ProcessGrade}";
        }

        /// <summary>
        /// 获取加工类型描述
        /// </summary>
        private string GetProcessTypeDescription()
        {
            switch (ProcessType)
            {
                case ProcessingType.Grinding: return "磨削";
                case ProcessingType.Polishing: return "抛光";
                case ProcessingType.Machining: return "加工";
                case ProcessingType.Coating: return "镀膜";
                case ProcessingType.Cutting: return "切割";
                case ProcessingType.Drilling: return "钻孔";
                case ProcessingType.Edging: return "修边";
                case ProcessingType.Assembly: return "装配";
                default: return "加工";
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
        /// 生成主标记符号 — 工厂惯例: 矩形框 + 结构化文字标签 (GB/T 4458.4 工艺备注规范).
        /// 旧实现 (五角形 + 子工艺图标 + 星级) 无 GB/ISO 标准依据, 已移除.
        /// </summary>
        private void GenerateMainSymbol()
        {
            var markColor = color;
            double halfWidth = FrameSize.X * 0.5 * Scale;
            double halfHeight = FrameSize.Y * 0.5 * Scale;

            GenerateRectangleFrame(halfWidth, halfHeight, markColor);
            GenerateProcessLabel(halfHeight, markColor);
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
        /// 框内 2 行结构化文字: 第一行 "工艺: 等级", 第二行 "质量".
        /// </summary>
        private void GenerateProcessLabel(double halfHeight, lcdb.Colors.Color color)
        {
            var line1 = new Text
            {
                Value = MarkText,
                Position = new Vector3(Position.X, Position.Y + halfHeight * 0.35, 0.0),
                Height = 4 * Scale,
                color = color,
                alignment = lcdb.TextAlignment.CenterMiddle,
            };
            _markEntities.Add(line1);

            var line2 = new Text
            {
                Value = GetQualityDescription(),
                Position = new Vector3(Position.X, Position.Y - halfHeight * 0.35, 0.0),
                Height = 3 * Scale,
                color = color,
                alignment = lcdb.TextAlignment.CenterMiddle,
            };
            _markEntities.Add(line2);
        }

        /// <summary>
        /// 生成外部说明文本
        /// </summary>
        private void GenerateText()
        {
            var textPosition = Position + TextOffset;
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
            string processDesc = GetProcessTypeDescription();
            string qualityDesc = GetQualityDescription();
            return $"{processDesc}工艺: {ProcessGrade}级, {qualityDesc}质量";
        }

        /// <summary>
        /// 获取质量描述
        /// </summary>
        private string GetQualityDescription()
        {
            switch (Quality)
            {
                case QualityGrade.Standard: return "标准";
                case QualityGrade.Precision: return "精密";
                case QualityGrade.HighPrecision: return "高精度";
                case QualityGrade.UltraPrecision: return "超精度";
                default: return "标准";
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
        /// GDI+ 路径(WinForms 已封存): 旧五角形+星级渲染与现役 Generate()(矩形框+文字)
        /// 是两个不同符号, 已删避免"同一标记两种画法"; 空实现满足接口。
        /// </summary>
        public void Draw(Graphics g, float scale) { }
#endif

        /// <summary>
        /// 获取标记边界框
        /// </summary>
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
            if (string.IsNullOrEmpty(ProcessGrade))
                return false;

            if (string.IsNullOrEmpty(ProcessRequirement))
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
                { "ProcessType", ProcessType },
                { "ProcessGrade", ProcessGrade },
                { "ProcessRequirement", ProcessRequirement },
                { "Precision", Precision },
                { "Treatment", Treatment },
                { "SpecialRequirement", SpecialRequirement },
                { "Equipment", Equipment },
                { "ProcessParameters", ProcessParameters },
                { "Quality", Quality },
                { "FrameSize", FrameSize },
                { "ProcessStandard", ProcessStandard }
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
            if (properties.ContainsKey("ProcessType"))
                ProcessType = (ProcessingType)properties["ProcessType"];
            if (properties.ContainsKey("ProcessGrade"))
                ProcessGrade = (string)properties["ProcessGrade"];
            if (properties.ContainsKey("ProcessRequirement"))
                ProcessRequirement = (string)properties["ProcessRequirement"];
            if (properties.ContainsKey("Precision"))
                Precision = (ProcessingPrecision)properties["Precision"];
            if (properties.ContainsKey("Treatment"))
                Treatment = (SurfaceTreatment)properties["Treatment"];
            if (properties.ContainsKey("SpecialRequirement"))
                SpecialRequirement = (string)properties["SpecialRequirement"];
            if (properties.ContainsKey("Equipment"))
                Equipment = (string)properties["Equipment"];
            if (properties.ContainsKey("ProcessParameters"))
                ProcessParameters = (string)properties["ProcessParameters"];
            if (properties.ContainsKey("Quality"))
                Quality = (QualityGrade)properties["Quality"];
            if (properties.ContainsKey("FrameSize"))
                FrameSize = (Vector2)properties["FrameSize"];
            if (properties.ContainsKey("ProcessStandard"))
                ProcessStandard = (string)properties["ProcessStandard"];

            UpdateMarkText();
        }

        /// <summary>
        /// 克隆标记
        /// </summary>
        IOpticalMark IOpticalMark.Clone()
        {
            return Clone() as ProcessingMark;
        }

        /// <summary>
        /// 获取标记描述
        /// </summary>
        public string GetDescription()
        {
            return $"加工标记 - {GetProcessTypeDescription()}: {ProcessGrade}级, {GetQualityDescription()}质量";
        }

        #endregion

        #region Entity 重写方法

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new ProcessingMark();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            ProcessingMark mark = base.Clone() as ProcessingMark;
            mark.Position = Position;
            mark.Scale = Scale;
            mark.MarkText = MarkText;
            mark.ShowText = ShowText;
            mark.TextOffset = TextOffset;
            mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.ProcessType = ProcessType;
            mark.ProcessGrade = ProcessGrade;
            mark.ProcessRequirement = ProcessRequirement;
            mark.Precision = Precision;
            mark.Treatment = Treatment;
            mark.SpecialRequirement = SpecialRequirement;
            mark.Equipment = Equipment;
            mark.ProcessParameters = ProcessParameters;
            mark.Quality = Quality;
            mark.FrameSize = FrameSize;
            mark.ProcessStandard = ProcessStandard;
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
        /// 对象捕捉点
        /// </summary>
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
    /// 加工类型枚举
    /// </summary>
    public enum ProcessingType
    {
        /// <summary>
        /// 磨削
        /// </summary>
        Grinding = 0,
        
        /// <summary>
        /// 抛光
        /// </summary>
        Polishing = 1,
        
        /// <summary>
        /// 机械加工
        /// </summary>
        Machining = 2,
        
        /// <summary>
        /// 镀膜
        /// </summary>
        Coating = 3,
        
        /// <summary>
        /// 切割
        /// </summary>
        Cutting = 4,
        
        /// <summary>
        /// 钻孔
        /// </summary>
        Drilling = 5,
        
        /// <summary>
        /// 修边
        /// </summary>
        Edging = 6,
        
        /// <summary>
        /// 装配
        /// </summary>
        Assembly = 7
    }

    /// <summary>
    /// 加工精度枚举
    /// </summary>
    public enum ProcessingPrecision
    {
        /// <summary>
        /// 标准精度
        /// </summary>
        Standard = 0,
        
        /// <summary>
        /// 高精密
        /// </summary>
        High = 1,
        
        /// <summary>
        /// 精密加工
        /// </summary>
        Precision = 2,
        
        /// <summary>
        /// 超精密加工
        /// </summary>
        UltraPrecision = 3
    }

    /// <summary>
    /// 表面处理枚举
    /// </summary>
    public enum SurfaceTreatment
    {
        /// <summary>
        /// 抛光
        /// </summary>
        Polished = 0,
        
        /// <summary>
        /// 磨砂
        /// </summary>
        Sandblasted = 1,
        
        /// <summary>
        /// 镀膜
        /// </summary>
        Coated = 2,
        
        /// <summary>
        /// 喷砂
        /// </summary>
        Sandblasted2 = 3,
        
        /// <summary>
        /// 阳极氧化
        /// </summary>
        Anodized = 4
    }

    /// <summary>
    /// 质量等级枚举
    /// </summary>
    public enum QualityGrade
    {
        /// <summary>
        /// 标准
        /// </summary>
        Standard = 0,
        
        /// <summary>
        /// 精密
        /// </summary>
        Precision = 1,
        
        /// <summary>
        /// 高精密
        /// </summary>
        HighPrecision = 2,
        
        /// <summary>
        /// 超精度
        /// </summary>
        UltraPrecision = 3
    }
}