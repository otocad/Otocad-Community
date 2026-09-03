using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using LitMath;
using lcdb;
using lcdb.Interfaces;
using OtoCAD;

namespace lcdb.Annotation
{
    /// <summary>
    /// 面形精度标准枚举
    /// </summary>
    public enum FormAccuracyStandard
    {
        /// <summary>ISO 10110-5 面形精度标准. 格式: 3/A(B/C)</summary>
        ISO_10110_5 = 0,
        /// <summary>传统 PV/RMS 格式. 格式: PV: λ/N, RMS: λ/M</summary>
        PV_RMS_Format = 1
    }

    /// <summary>
    /// 面形精度 — <b>数据载体</b>(非独立标记)。
    ///
    /// 2026-06-08 模型对齐(skill <c>optical-mark-compliance</c>):面形精度是"对零件的要求",
    /// 归 <b>面属性区表格行</b>(参照表面质量),不再作独立贴面带框标记。本类只保留参数 +
    /// <see cref="UpdateMarkText"/> 生成 <see cref="MarkText"/>(供属性区取值)与序列化往返
    /// (旧 .otocad 兼容);<b>不再自绘几何,Draw 为空操作</b>。
    ///
    /// ⚠️ 待人工核国标:3/A(B/C) 的 C 字段语义 —— 现 <see cref="RotationalSymmetryDeviation"/>
    /// 注释为"旋转对称偏差",ISO 10110-5 / GB/T 11297.5 中 C 通常为"旋转对称残差/RMS",
    /// 核对原文后再修正字段名/生成逻辑。详见双边核对页 form-accuracy-compliance。
    /// </summary>
    public class FormAccuracyMark : Entity, IOpticalMark
    {
        public override string className => "FormAccuracyMark";

        #region IOpticalMark 接口属性
        public OpticalMarkType MarkType => OpticalMarkType.FormAccuracy;
        public Vector2 Position { get; set; } = new Vector2(0, 0);
        public double Scale { get; set; } = 1.0;
        public string MarkText { get; set; } = "3/4(1)";
        public bool ShowText { get; set; } = true;
        public Vector2 TextOffset { get; set; } = new Vector2(0, -30);
        public double Rotation { get; set; } = 0.0;
        public bool IsVisible { get; set; } = true;
        #endregion

        #region 标准选择
        public FormAccuracyStandard FormStandard { get; set; } = FormAccuracyStandard.ISO_10110_5;
        #endregion

        #region ISO 10110-5 参数
        /// <summary>功率/球差 A (条纹数)</summary>
        public double PowerFringes { get; set; } = 4.0;
        /// <summary>不规则度 B (条纹数)</summary>
        public double IrregularityFringes { get; set; } = 1.0;
        /// <summary>C (条纹数,可选)。⚠️ 现注释为"旋转对称偏差",待核国标(ISO 10110-5 中 C 通常为旋转对称残差/RMS)。</summary>
        public double RotationalSymmetryDeviation { get; set; } = 0.0;
        /// <summary>是否包含 C 项</summary>
        public bool HasRotationalSymmetry { get; set; } = false;
        /// <summary>评估区域直径 (mm,可选)</summary>
        public double EvaluationDiameter { get; set; } = 0.0;
        /// <summary>是否仅指定不规则度(无功率要求)。格式: 3/-(B)</summary>
        public bool IrregularityOnly { get; set; } = false;
        #endregion

        #region 传统 PV/RMS 参数
        public string PVValue { get; set; } = "4";
        public string RMSValue { get; set; } = "20";
        public FormAccuracyType AccuracyType { get; set; } = FormAccuracyType.PV_RMS;
        #endregion

        #region 通用属性
        /// <summary>测试波长(纳米)</summary>
        public string Wavelength { get; set; } = "632.8";
        /// <summary>测试方法</summary>
        public string TestMethod { get; set; } = "Fizeau干涉仪";
        /// <summary>标记样式(历史字段,仅供旧文件序列化往返;已不渲染)</summary>
        public FormAccuracyMarkStyle MarkStyle { get; set; } = FormAccuracyMarkStyle.DashedBox;
        /// <summary>框架大小(历史字段,供 bounding/序列化;已不渲染框)</summary>
        public Vector2 FrameSize { get; set; } = new Vector2(60, 30);
        #endregion

        public override Bounding bounding => new Bounding(Position, FrameSize.X, FrameSize.Y);

        #region 构造函数
        public FormAccuracyMark()
        {
            FormStandard = FormAccuracyStandard.ISO_10110_5;
            UpdateMarkText();
        }

        /// <summary>传统 PV/RMS 格式构造函数</summary>
        public FormAccuracyMark(Vector2 position, string pvValue, string rmsValue = "", string wavelength = "632.8")
        {
            Position = position;
            FormStandard = FormAccuracyStandard.PV_RMS_Format;
            PVValue = pvValue;
            RMSValue = rmsValue;
            Wavelength = wavelength;
            UpdateMarkText();
        }

        /// <summary>ISO 10110-5 格式构造函数</summary>
        public FormAccuracyMark(Vector2 position, double powerFringes, double irregularityFringes,
            double rotationalSymmetry = 0.0, string wavelength = "632.8")
        {
            Position = position;
            FormStandard = FormAccuracyStandard.ISO_10110_5;
            PowerFringes = powerFringes;
            IrregularityFringes = irregularityFringes;
            RotationalSymmetryDeviation = rotationalSymmetry;
            HasRotationalSymmetry = rotationalSymmetry > 0;
            Wavelength = wavelength;
            UpdateMarkText();
        }
        #endregion

        #region 文字码生成(供属性区取值)
        /// <summary>更新标记文本(按当前标准)</summary>
        private void UpdateMarkText()
        {
            if (FormStandard == FormAccuracyStandard.ISO_10110_5)
                UpdateISOMarkText();
            else
                UpdatePVRMSMarkText();
        }

        /// <summary>ISO 10110-5: 3/A(B) 或 3/A(B/C) 或 3/-(B)</summary>
        private void UpdateISOMarkText()
        {
            var sb = new StringBuilder();
            sb.Append("3/");                       // ISO 10110-5 代号前缀
            if (IrregularityOnly) sb.Append("-");  // 无功率要求
            else AppendNum(sb, PowerFringes);
            sb.Append("(");
            AppendNum(sb, IrregularityFringes);
            if (HasRotationalSymmetry && RotationalSymmetryDeviation > 0)
            {
                sb.Append("/");
                AppendNum(sb, RotationalSymmetryDeviation);
            }
            sb.Append(")");
            if (EvaluationDiameter > 0)
                sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, " Ø{0:F1}", EvaluationDiameter);
            MarkText = sb.ToString();
        }

        // 不变区域性: 即便系统区域用逗号作小数分隔, 代号串仍用小数点(对齐表面质量的做法)。
        private static void AppendNum(StringBuilder sb, double v)
        {
            if (v == Math.Floor(v)) sb.Append(((int)v).ToString(System.Globalization.CultureInfo.InvariantCulture));
            else sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:F1}", v);
        }

        /// <summary>传统 PV/RMS(非 ISO,客户传统格式)</summary>
        private void UpdatePVRMSMarkText()
        {
            switch (AccuracyType)
            {
                case FormAccuracyType.PV_Only: MarkText = $"PV: λ/{PVValue}"; break;
                case FormAccuracyType.RMS_Only: MarkText = $"RMS: λ/{RMSValue}"; break;
                case FormAccuracyType.PV_RMS:
                    if (!string.IsNullOrEmpty(PVValue) && !string.IsNullOrEmpty(RMSValue))
                        MarkText = $"PV: λ/{PVValue}\nRMS: λ/{RMSValue}";
                    else if (!string.IsNullOrEmpty(PVValue)) MarkText = $"PV: λ/{PVValue}";
                    else if (!string.IsNullOrEmpty(RMSValue)) MarkText = $"RMS: λ/{RMSValue}";
                    break;
                case FormAccuracyType.PowerIrregularity: MarkText = $"{PVValue}/{RMSValue}"; break;
                default: MarkText = $"λ/{PVValue}"; break;
            }
        }
        #endregion

        #region Entity 重写(数据载体: 不自绘,Draw 空操作)
        /// <summary>面形精度已归面属性区表格行,不再作独立标记自绘 → Draw 空操作。</summary>
        public override void Draw(IGraphicsDraw gd) { }

        protected override DBObject CreateInstance() => new FormAccuracyMark();

        public override object Clone()
        {
            FormAccuracyMark mark = base.Clone() as FormAccuracyMark;
            mark.Position = Position; mark.Scale = Scale; mark.MarkText = MarkText;
            mark.ShowText = ShowText; mark.TextOffset = TextOffset; mark.Rotation = Rotation;
            mark.IsVisible = IsVisible; mark.FormStandard = FormStandard;
            mark.PowerFringes = PowerFringes; mark.IrregularityFringes = IrregularityFringes;
            mark.RotationalSymmetryDeviation = RotationalSymmetryDeviation;
            mark.HasRotationalSymmetry = HasRotationalSymmetry;
            mark.EvaluationDiameter = EvaluationDiameter; mark.IrregularityOnly = IrregularityOnly;
            mark.PVValue = PVValue; mark.RMSValue = RMSValue; mark.AccuracyType = AccuracyType;
            mark.Wavelength = Wavelength; mark.TestMethod = TestMethod;
            mark.MarkStyle = MarkStyle; mark.FrameSize = FrameSize;
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
            => FormStandard == FormAccuracyStandard.ISO_10110_5 ? ValidateISO() : ValidatePVRMS();

        private bool ValidateISO()
        {
            if (!IrregularityOnly && PowerFringes < 0) return false;
            if (IrregularityFringes < 0) return false;
            if (RotationalSymmetryDeviation < 0) return false;
            if (EvaluationDiameter < 0) return false;
            if (!string.IsNullOrEmpty(Wavelength))
                if (!double.TryParse(Wavelength, out double wl) || wl <= 0 || wl > 10000) return false;
            return true;
        }

        private bool ValidatePVRMS()
        {
            if (!string.IsNullOrEmpty(PVValue))
                if (!double.TryParse(PVValue, out double pv) || pv <= 0) return false;
            if (!string.IsNullOrEmpty(RMSValue))
                if (!double.TryParse(RMSValue, out double rms) || rms <= 0) return false;
            if (!string.IsNullOrEmpty(Wavelength))
                if (!double.TryParse(Wavelength, out double wl) || wl <= 0 || wl > 10000) return false;
            return !string.IsNullOrEmpty(PVValue) || !string.IsNullOrEmpty(RMSValue);
        }

        public Dictionary<string, object> GetProperties() => new Dictionary<string, object>
        {
            { "Position", Position }, { "Scale", Scale }, { "MarkText", MarkText },
            { "ShowText", ShowText }, { "TextOffset", TextOffset }, { "Rotation", Rotation },
            { "IsVisible", IsVisible }, { "FormStandard", FormStandard },
            { "PowerFringes", PowerFringes }, { "IrregularityFringes", IrregularityFringes },
            { "RotationalSymmetryDeviation", RotationalSymmetryDeviation },
            { "HasRotationalSymmetry", HasRotationalSymmetry },
            { "EvaluationDiameter", EvaluationDiameter }, { "IrregularityOnly", IrregularityOnly },
            { "PVValue", PVValue }, { "RMSValue", RMSValue }, { "AccuracyType", AccuracyType },
            { "Wavelength", Wavelength }, { "TestMethod", TestMethod },
            { "MarkStyle", MarkStyle }, { "FrameSize", FrameSize }
        };

        public void SetProperties(Dictionary<string, object> properties)
        {
            if (properties.ContainsKey("Position")) Position = (Vector2)properties["Position"];
            if (properties.ContainsKey("Scale")) Scale = Convert.ToDouble(properties["Scale"]);
            if (properties.ContainsKey("MarkText")) MarkText = (string)properties["MarkText"];
            if (properties.ContainsKey("ShowText")) ShowText = (bool)properties["ShowText"];
            if (properties.ContainsKey("TextOffset")) TextOffset = (Vector2)properties["TextOffset"];
            if (properties.ContainsKey("Rotation")) Rotation = Convert.ToDouble(properties["Rotation"]);
            if (properties.ContainsKey("IsVisible")) IsVisible = (bool)properties["IsVisible"];
            if (properties.ContainsKey("FormStandard")) FormStandard = (FormAccuracyStandard)properties["FormStandard"];
            if (properties.ContainsKey("PowerFringes")) PowerFringes = Convert.ToDouble(properties["PowerFringes"]);
            if (properties.ContainsKey("IrregularityFringes")) IrregularityFringes = Convert.ToDouble(properties["IrregularityFringes"]);
            if (properties.ContainsKey("RotationalSymmetryDeviation")) RotationalSymmetryDeviation = Convert.ToDouble(properties["RotationalSymmetryDeviation"]);
            if (properties.ContainsKey("HasRotationalSymmetry")) HasRotationalSymmetry = (bool)properties["HasRotationalSymmetry"];
            if (properties.ContainsKey("EvaluationDiameter")) EvaluationDiameter = Convert.ToDouble(properties["EvaluationDiameter"]);
            if (properties.ContainsKey("IrregularityOnly")) IrregularityOnly = (bool)properties["IrregularityOnly"];
            if (properties.ContainsKey("PVValue")) PVValue = (string)properties["PVValue"];
            if (properties.ContainsKey("RMSValue")) RMSValue = (string)properties["RMSValue"];
            if (properties.ContainsKey("AccuracyType")) AccuracyType = (FormAccuracyType)properties["AccuracyType"];
            if (properties.ContainsKey("Wavelength")) Wavelength = (string)properties["Wavelength"];
            if (properties.ContainsKey("TestMethod")) TestMethod = (string)properties["TestMethod"];
            if (properties.ContainsKey("MarkStyle")) MarkStyle = (FormAccuracyMarkStyle)properties["MarkStyle"];
            if (properties.ContainsKey("FrameSize")) FrameSize = (Vector2)properties["FrameSize"];
            UpdateMarkText();
        }

        IOpticalMark IOpticalMark.Clone() => Clone() as FormAccuracyMark;

        public string GetDescription()
            => FormStandard == FormAccuracyStandard.ISO_10110_5
                ? $"面形精度 - {MarkText} (ISO 10110-5, @{Wavelength}nm)"
                : $"面形精度 - {MarkText.Replace('\n', ' ')} (PV/RMS, @{Wavelength}nm)";
        #endregion
    }

    /// <summary>面形精度类型枚举(传统 PV/RMS)</summary>
    public enum FormAccuracyType
    {
        PV_Only = 0,
        RMS_Only = 1,
        PV_RMS = 2,
        PowerIrregularity = 3
    }

    /// <summary>面形精度标记样式枚举(历史字段,已不渲染)</summary>
    public enum FormAccuracyMarkStyle
    {
        DashedBox = 0,
        DoubleBox = 1,
        WaveSymbol = 2
    }
}
