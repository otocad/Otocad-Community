using System;
using System.ComponentModel;

namespace OtoCAD.OpticEntity
{
    /// <summary>
    /// 透镜公差规格
    /// </summary>
    public class LensTolerances
    {
        [Category("尺寸公差")]
        [DisplayName("直径公差")]
        [Description("透镜直径的制造公差")]
        public string DiameterTolerance { get; set; } = "±0.1";

        [Category("尺寸公差")]
        [DisplayName("厚度公差")]
        [Description("中心厚度的制造公差")]
        public string ThicknessTolerance { get; set; } = "±0.1";

        [Category("尺寸公差")]
        [DisplayName("边缘厚度公差")]
        [Description("边缘厚度的制造公差")]
        public string EdgeThicknessTolerance { get; set; } = "±0.2";

        [Category("形位公差")]
        [DisplayName("中心偏差")]
        [Description("光学中心相对于机械中心的偏差")]
        public string Decentration { get; set; } = "< 0.05mm";

        [Category("形位公差")]
        [DisplayName("楔角")]
        [Description("两光学表面的楔形误差")]
        public string Wedge { get; set; } = "< 3'";

        [Category("光学公差")]
        [DisplayName("焦距公差")]
        [Description("有效焦距的公差")]
        public string FocalLengthTolerance { get; set; } = "±2%";

        [Category("光学公差")]
        [DisplayName("曲率半径公差")]
        [Description("表面曲率半径的公差")]
        public string RadiusTolerance { get; set; } = "±0.5%";

        [Category("材料公差")]
        [DisplayName("折射率公差")]
        [Description("玻璃材料折射率的公差")]
        public string RefractiveIndexTolerance { get; set; } = "±0.0005";

        [Category("材料公差")]
        [DisplayName("阿贝数公差")]
        [Description("玻璃材料阿贝数的公差")]
        public string AbbeNumberTolerance { get; set; } = "±0.8%";

        /// <summary>
        /// 验证公差值的格式
        /// </summary>
        public bool ValidateTolerances(out string errorMessage)
        {
            errorMessage = null;

            // 验证各种公差格式
            if (!IsValidToleranceFormat(DiameterTolerance))
            {
                errorMessage = "直径公差格式无效";
                return false;
            }

            if (!IsValidToleranceFormat(ThicknessTolerance))
            {
                errorMessage = "厚度公差格式无效";
                return false;
            }

            if (!IsValidPercentageFormat(FocalLengthTolerance))
            {
                errorMessage = "焦距公差格式无效";
                return false;
            }

            return true;
        }

        private bool IsValidToleranceFormat(string tolerance)
        {
            if (string.IsNullOrWhiteSpace(tolerance))
                return false;

            // 支持的格式: ±0.1, +0.1/-0.2, 0.1±0.05
            var patterns = new[]
            {
                @"^[+-]?\d+(\.\d+)?$",
                @"^[+-]\d+(\.\d+)?/[+-]\d+(\.\d+)?$",
                @"^\d+(\.\d+)?\s*[+-]\s*\d+(\.\d+)?$"
            };

            return Array.Exists(patterns, pattern => 
                System.Text.RegularExpressions.Regex.IsMatch(tolerance.Trim(), pattern));
        }

        private bool IsValidPercentageFormat(string tolerance)
        {
            if (string.IsNullOrWhiteSpace(tolerance))
                return false;

            // 支持的格式: ±2%, +1%/-2%
            var patterns = new[]
            {
                @"^[+-]?\d+(\.\d+)?%$",
                @"^[+-]\d+(\.\d+)?%/[+-]\d+(\.\d+)?%$"
            };

            return Array.Exists(patterns, pattern => 
                System.Text.RegularExpressions.Regex.IsMatch(tolerance.Trim(), pattern));
        }

        /// <summary>
        /// 获取公差等级
        /// </summary>
        public ToleranceGrade GetToleranceGrade()
        {
            // 根据公差值判断等级
            if (DiameterTolerance.IndexOf("0.01", StringComparison.Ordinal) >= 0 || ThicknessTolerance.IndexOf("0.01", StringComparison.Ordinal) >= 0)
                return ToleranceGrade.Precision;
            else if (DiameterTolerance.IndexOf("0.05", StringComparison.Ordinal) >= 0 || ThicknessTolerance.IndexOf("0.05", StringComparison.Ordinal) >= 0)
                return ToleranceGrade.High;
            else if (DiameterTolerance.IndexOf("0.1", StringComparison.Ordinal) >= 0 || ThicknessTolerance.IndexOf("0.1", StringComparison.Ordinal) >= 0)
                return ToleranceGrade.Standard;
            else
                return ToleranceGrade.Commercial;
        }

        /// <summary>
        /// 从公差等级设置默认值
        /// </summary>
        public void SetFromGrade(ToleranceGrade grade)
        {
            switch (grade)
            {
                case ToleranceGrade.Precision:
                    DiameterTolerance = "±0.01";
                    ThicknessTolerance = "±0.01";
                    EdgeThicknessTolerance = "±0.02";
                    Decentration = "< 0.01mm";
                    Wedge = "< 30\"";
                    FocalLengthTolerance = "±0.5%";
                    RadiusTolerance = "±0.1%";
                    RefractiveIndexTolerance = "±0.0001";
                    AbbeNumberTolerance = "±0.3%";
                    break;

                case ToleranceGrade.High:
                    DiameterTolerance = "±0.05";
                    ThicknessTolerance = "±0.05";
                    EdgeThicknessTolerance = "±0.1";
                    Decentration = "< 0.03mm";
                    Wedge = "< 1'";
                    FocalLengthTolerance = "±1%";
                    RadiusTolerance = "±0.25%";
                    RefractiveIndexTolerance = "±0.0003";
                    AbbeNumberTolerance = "±0.5%";
                    break;

                case ToleranceGrade.Standard:
                    DiameterTolerance = "±0.1";
                    ThicknessTolerance = "±0.1";
                    EdgeThicknessTolerance = "±0.2";
                    Decentration = "< 0.05mm";
                    Wedge = "< 3'";
                    FocalLengthTolerance = "±2%";
                    RadiusTolerance = "±0.5%";
                    RefractiveIndexTolerance = "±0.0005";
                    AbbeNumberTolerance = "±0.8%";
                    break;

                case ToleranceGrade.Commercial:
                    DiameterTolerance = "±0.25";
                    ThicknessTolerance = "±0.2";
                    EdgeThicknessTolerance = "±0.5";
                    Decentration = "< 0.1mm";
                    Wedge = "< 5'";
                    FocalLengthTolerance = "±5%";
                    RadiusTolerance = "±1%";
                    RefractiveIndexTolerance = "±0.001";
                    AbbeNumberTolerance = "±1%";
                    break;
            }
        }

        /// <summary>
        /// 克隆公差规格
        /// </summary>
        public LensTolerances Clone()
        {
            return new LensTolerances
            {
                DiameterTolerance = this.DiameterTolerance,
                ThicknessTolerance = this.ThicknessTolerance,
                EdgeThicknessTolerance = this.EdgeThicknessTolerance,
                Decentration = this.Decentration,
                Wedge = this.Wedge,
                FocalLengthTolerance = this.FocalLengthTolerance,
                RadiusTolerance = this.RadiusTolerance,
                RefractiveIndexTolerance = this.RefractiveIndexTolerance,
                AbbeNumberTolerance = this.AbbeNumberTolerance
            };
        }
    }

    /// <summary>
    /// 公差等级
    /// </summary>
    public enum ToleranceGrade
    {
        /// <summary>
        /// 商业级
        /// </summary>
        [Description("商业级")]
        Commercial,

        /// <summary>
        /// 标准级
        /// </summary>
        [Description("标准级")]
        Standard,

        /// <summary>
        /// 高精度
        /// </summary>
        [Description("高精度")]
        High,

        /// <summary>
        /// 精密级
        /// </summary>
        [Description("精密级")]
        Precision
    }
}