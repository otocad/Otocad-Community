using lcdb.Colors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using LitMath;
using lcdb;
using lcdb.Interfaces;
using OtoCAD;

namespace lcdb.Annotation
{
    /// <summary>
    /// 光学表面结构(表面纹理)— <b>数据载体</b>(非独立标记)。
    ///
    /// GB/T 13323-2009 §附录C / ISO 10110-8 的 G/P 表示:粗糙面 G(ground, 用 Rq),
    /// 抛光面 P(polished, P1–P4 / Rq / PSD)。<b>无 slash 代号</b>(ISO 10110-8 不用 `N/`,
    /// `6/` 是激光损伤的代号 —— 见 ISO 10110-10 代号表;故本类绝不产 `6/`/`8/` 前缀)。
    ///
    /// 2026-06-08 模型对齐(skill <c>optical-mark-compliance</c>):表面纹理是"对零件的要求",
    /// 归 <b>面属性区表格行</b>(Surface texture 行,参照表面质量/面型精度),不作独立带框标记。
    /// 本类只保留参数 + <see cref="UpdateMarkText"/> 生成 <see cref="MarkText"/>(供属性区取值)
    /// 与序列化往返;<b>不再自绘几何,Draw 为空操作</b>。
    ///
    /// 与机械粗糙度 <see cref="SurfaceRoughnessMark"/>(GB/T 131 Ra V 形, 通用)并存、各管各:
    /// 光学表面结构走本类的 G/P, 一般机械粗糙度走 Ra V 形(后者仍是独立贴面符号)。
    /// </summary>
    public class SurfaceTextureMark : Entity, IOpticalMark
    {
        public override string className => "SurfaceTextureMark";

        #region IOpticalMark 接口属性
        public OpticalMarkType MarkType => OpticalMarkType.ISO10110_8;
        public Vector2 Position { get; set; } = new Vector2(0, 0);
        public double Scale { get; set; } = 1.0;
        public string MarkText { get; set; } = "P";
        public bool ShowText { get; set; } = true;
        public Vector2 TextOffset { get; set; } = new Vector2(0, -30);
        public double Rotation { get; set; } = 0.0;
        public bool IsVisible { get; set; } = true;
        #endregion

        #region ISO 10110-8 参数
        /// <summary>平均粗糙度 Ra (μm) — 朴素参数表示用</summary>
        public double RaValue { get; set; } = 0.8;
        /// <summary>最大高度差 Rz (μm)</summary>
        public double RzValue { get; set; } = 6.3;
        /// <summary>均方根粗糙度 Rq (μm), 0 = 不指定</summary>
        public double RqValue { get; set; } = 0.0;
        /// <summary>轮廓支承长度比 Rmr (%), 0 = 不指定</summary>
        public double RmrValue { get; set; } = 0.0;
        /// <summary>表面纹理类型</summary>
        public SurfaceTextureType TextureType { get; set; } = SurfaceTextureType.Polished;
        /// <summary>纹理方向</summary>
        public TextureDirection Direction { get; set; } = TextureDirection.Any;
        /// <summary>测量长度 (mm)</summary>
        public double MeasurementLength { get; set; } = 0.8;
        /// <summary>评定长度 (mm)</summary>
        public double EvaluationLength { get; set; } = 4.0;
        /// <summary>框架大小(历史字段,仅供旧文件序列化往返;已不渲染)</summary>
        public Vector2 FrameSize { get; set; } = new Vector2(60, 30);
        /// <summary>检测标准</summary>
        public string Standard { get; set; } = "ISO 10110-8";
        #endregion

        #region GB/T 13323 附录C 表面结构属性
        /// <summary>GB/T 13323 表面结构类型 (G/P/P1-P4)</summary>
        public GB13323SurfaceType GB13323Type { get; set; } = GB13323SurfaceType.Polished_P;

        /// <summary>是否使用 GB/T 13323 G/P 格式(否则朴素 Ra/Rz/Rq 参数串)</summary>
        public bool UseGB13323Format { get; set; } = true;   // 光学表面结构默认 G/P (GB13323 §附录C / ISO 10110-8)

        /// <summary>斜率取样长度 (mm) - GB/T 13323 附录C</summary>
        public double SlopeSampleLength { get; set; } = 0.002;

        /// <summary>微缺陷数量参数 (每10mm轮廓微缺陷数)</summary>
        public int MicrodefectCount { get; set; } = 1;
        #endregion

        public override Bounding bounding => new Bounding(Position, 20, 10);

        #region 构造函数
        public SurfaceTextureMark()
        {
            UpdateMarkText();
        }

        public SurfaceTextureMark(Vector2 position, double raValue, double rzValue)
        {
            Position = position;
            RaValue = raValue;
            RzValue = rzValue;
            UpdateMarkText();
        }
        #endregion

        #region 文字码生成(供属性区取值)
        /// <summary>
        /// 按当前格式生成表面结构代码串(不变区域性: 恒用小数点)。
        /// GB13323 G/P: "G/Rq2.000"、"P"、"P3/0.002/1/Rq0.002";朴素: "Ra0.8-Rz6.3"。
        /// 绝不带 `6/`/`8/` slash 代号(纹理无 slash 代号; 6/ 是激光损伤)。
        /// </summary>
        public void UpdateMarkText()
        {
            var inv = CultureInfo.InvariantCulture;
            if (UseGB13323Format)
            {
                string typeSymbol = GetGB13323TypeSymbol();
                string slopeText = SlopeSampleLength > 0 ? string.Format(inv, "/{0:F3}", SlopeSampleLength) : "";
                string defectText = MicrodefectCount > 0 ? string.Format(inv, "/{0}", MicrodefectCount) : "";
                string rqText = RqValue > 0 ? string.Format(inv, "/Rq{0:F3}", RqValue) : "";

                // G 类型格式: G/0.002/Rq2.000;P 类型格式: P 或 P2 或 P3/0.002/1/Rq0.002
                if (GB13323Type == GB13323SurfaceType.Rough_G)
                    MarkText = $"G{slopeText}{rqText}";
                else
                    MarkText = $"{typeSymbol}{slopeText}{defectText}{rqText}";
            }
            else
            {
                // 朴素参数表示(非 G/P): 仅 Ra/Rz/Rq, 不加任何 slash 代号(ISO 10110-8 无 `8/` 代号)。
                string raText = string.Format(inv, "Ra{0:F1}", RaValue);
                string rzText = RzValue > 0 ? string.Format(inv, "-Rz{0:F1}", RzValue) : "";
                string rqText = RqValue > 0 ? string.Format(inv, "-Rq{0:F1}", RqValue) : "";
                MarkText = $"{raText}{rzText}{rqText}";
            }
        }

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
        #endregion

        #region Entity 重写(数据载体: 不自绘,Draw 空操作)
        /// <summary>表面纹理已归面属性区 Surface texture 行,不再作独立标记自绘 → Draw 空操作。</summary>
        public override void Draw(IGraphicsDraw gd) { }

        protected override DBObject CreateInstance() => new SurfaceTextureMark();

        public override object Clone()
        {
            SurfaceTextureMark mark = base.Clone() as SurfaceTextureMark;
            mark.Position = Position; mark.Scale = Scale; mark.MarkText = MarkText;
            mark.ShowText = ShowText; mark.TextOffset = TextOffset; mark.Rotation = Rotation;
            mark.IsVisible = IsVisible;
            mark.RaValue = RaValue; mark.RzValue = RzValue; mark.RqValue = RqValue; mark.RmrValue = RmrValue;
            mark.TextureType = TextureType; mark.Direction = Direction;
            mark.MeasurementLength = MeasurementLength; mark.EvaluationLength = EvaluationLength;
            mark.FrameSize = FrameSize; mark.Standard = Standard;
            mark.GB13323Type = GB13323Type; mark.UseGB13323Format = UseGB13323Format;
            mark.SlopeSampleLength = SlopeSampleLength; mark.MicrodefectCount = MicrodefectCount;
            return mark;
        }

        public override void Translate(Vector2 translation) => Position += translation;

        public override void Rotate(Vector2 center, double angle)
        {
            Position = Vector2.RotateInRadian(Position, center, angle);
            Rotation += angle * 180 / Math.PI;
        }

        public override void TransformBy(Matrix3 transform)
        {
            Position = transform * Position;
            Vector2 scaleVector = new Vector2(Scale, 0);
            Scale = (transform * scaleVector - transform * new Vector2(0, 0)).length;
        }

        public override List<GripPoint> GetGripPoints()
            => new List<GripPoint> { new GripPoint(GripPointType.Center, Position) };

        public override List<ObjectSnapPoint> GetSnapPoints()
            => new List<ObjectSnapPoint> { new ObjectSnapPoint(ObjectSnapMode.Center, Position) };

        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            if (index == 0) Position = newPosition;
        }
        #endregion

        #region IOpticalMark 接口实现
#if WINDOWS
        /// <summary>GDI+ 路径(WinForms 已封存):数据载体不渲染,空实现满足接口。</summary>
        public void Draw(Graphics g, float scale) { }
#endif

        public RectangleF GetBounds()
        {
            var bound = bounding;
            return new RectangleF((float)bound.left, (float)bound.bottom, (float)bound.width, (float)bound.height);
        }

        public bool Validate()
        {
            if (RaValue < 0 || RaValue > 50.0) return false;
            if (RzValue < 0 || RzValue > 400.0) return false;
            if (RqValue < 0 || RqValue > 50.0) return false;
            if (RmrValue < 0 || RmrValue > 100.0) return false;
            return true;
        }

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
                { "GB13323Type", GB13323Type },
                { "UseGB13323Format", UseGB13323Format },
                { "SlopeSampleLength", SlopeSampleLength },
                { "MicrodefectCount", MicrodefectCount }
            };
        }

        public void SetProperties(Dictionary<string, object> properties)
        {
            if (properties.ContainsKey("Position")) Position = (Vector2)properties["Position"];
            if (properties.ContainsKey("Scale")) Scale = (double)properties["Scale"];
            if (properties.ContainsKey("MarkText")) MarkText = (string)properties["MarkText"];
            if (properties.ContainsKey("ShowText")) ShowText = (bool)properties["ShowText"];
            if (properties.ContainsKey("TextOffset")) TextOffset = (Vector2)properties["TextOffset"];
            if (properties.ContainsKey("Rotation")) Rotation = (double)properties["Rotation"];
            if (properties.ContainsKey("IsVisible")) IsVisible = (bool)properties["IsVisible"];
            if (properties.ContainsKey("RaValue")) RaValue = (double)properties["RaValue"];
            if (properties.ContainsKey("RzValue")) RzValue = (double)properties["RzValue"];
            if (properties.ContainsKey("RqValue")) RqValue = (double)properties["RqValue"];
            if (properties.ContainsKey("RmrValue")) RmrValue = (double)properties["RmrValue"];
            if (properties.ContainsKey("TextureType")) TextureType = (SurfaceTextureType)properties["TextureType"];
            if (properties.ContainsKey("Direction")) Direction = (TextureDirection)properties["Direction"];
            if (properties.ContainsKey("MeasurementLength")) MeasurementLength = (double)properties["MeasurementLength"];
            if (properties.ContainsKey("EvaluationLength")) EvaluationLength = (double)properties["EvaluationLength"];
            if (properties.ContainsKey("FrameSize")) FrameSize = (Vector2)properties["FrameSize"];
            if (properties.ContainsKey("Standard")) Standard = (string)properties["Standard"];
            if (properties.ContainsKey("GB13323Type")) GB13323Type = (GB13323SurfaceType)properties["GB13323Type"];
            if (properties.ContainsKey("UseGB13323Format")) UseGB13323Format = (bool)properties["UseGB13323Format"];
            if (properties.ContainsKey("SlopeSampleLength")) SlopeSampleLength = (double)properties["SlopeSampleLength"];
            if (properties.ContainsKey("MicrodefectCount")) MicrodefectCount = (int)properties["MicrodefectCount"];

            UpdateMarkText();
        }

        IOpticalMark IOpticalMark.Clone() => Clone() as SurfaceTextureMark;

        public string GetDescription()
        {
            return $"表面结构(纹理) - {MarkText} ({GetTextureTypeDescription()})";
        }
        #endregion
    }

    /// <summary>
    /// 表面纹理类型枚举
    /// </summary>
    public enum SurfaceTextureType
    {
        /// <summary>抛光</summary>
        Polished = 0,
        /// <summary>磨削</summary>
        Ground = 1,
        /// <summary>机加工</summary>
        Machined = 2,
        /// <summary>成形</summary>
        AsFormed = 3,
        /// <summary>标准</summary>
        Standard = 4
    }

    /// <summary>
    /// GB/T 13323 附录C 表面结构类型
    /// </summary>
    public enum GB13323SurfaceType
    {
        /// <summary>G - 粗糙表面(研磨)</summary>
        Rough_G = 0,
        /// <summary>P - 抛光表面(无微缺陷要求)</summary>
        Polished_P = 1,
        /// <summary>P1 - 抛光表面(微缺陷等级1: 80&lt;N&lt;400)</summary>
        Polished_P1 = 2,
        /// <summary>P2 - 抛光表面(微缺陷等级2: 16&lt;N&lt;80)</summary>
        Polished_P2 = 3,
        /// <summary>P3 - 抛光表面(微缺陷等级3: 3&lt;N&lt;16)</summary>
        Polished_P3 = 4,
        /// <summary>P4 - 抛光表面(微缺陷等级4: N&lt;3)</summary>
        Polished_P4 = 5
    }

    /// <summary>
    /// 纹理方向枚举(SurfaceRoughnessMark 复用此定义)
    /// </summary>
    public enum TextureDirection
    {
        /// <summary>任意方向</summary>
        Any = 0,
        /// <summary>平行</summary>
        Parallel = 1,
        /// <summary>垂直</summary>
        Perpendicular = 2,
        /// <summary>交叉</summary>
        Crossed = 3,
        /// <summary>放射状</summary>
        Radial = 4,
        /// <summary>同心圆</summary>
        Concentric = 5
    }
}
