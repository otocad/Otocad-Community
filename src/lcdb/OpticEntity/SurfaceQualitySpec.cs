using System;
using System.ComponentModel;

namespace OtoCAD.OpticEntity
{
    /// <summary>
    /// 表面质量规格
    /// </summary>
    public class SurfaceQualitySpec
    {
        [Category("面形精度")]
        [DisplayName("面形精度")]
        [Description("光学表面的面形精度要求")]
        public string Accuracy { get; set; } = "λ/4";

        [Category("面形精度")]
        [DisplayName("光圈数")]
        [Description("干涉条纹的光圈数")]
        public string PowerFringes { get; set; } = "3";

        [Category("面形精度")]
        [DisplayName("局部光圈")]
        [Description("局部区域的光圈数")]
        public string IrregularityFringes { get; set; } = "1";

        [Category("表面缺陷")]
        [DisplayName("疵病等级")]
        [Description("表面疵病等级（划痕-麻点）")]
        public string ScratchDig { get; set; } = "60-40";

        [Category("表面缺陷")]
        [DisplayName("表面粗糙度")]
        [Description("表面粗糙度RMS值")]
        public string Roughness { get; set; } = "10Å";

        [Category("表面缺陷")]
        [DisplayName("边缘倒角")]
        [Description("边缘倒角要求")]
        public string EdgeBevel { get; set; } = "0.25mm × 45°";

        [Category("清洁度")]
        [DisplayName("清洁度等级")]
        [Description("表面清洁度要求")]
        public CleanlinessLevel Cleanliness { get; set; } = CleanlinessLevel.Standard;

        /// <summary>
        /// 验证疵病等级格式
        /// </summary>
        public bool ValidateScratchDig(out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(ScratchDig))
            {
                errorMessage = "疵病等级不能为空";
                return false;
            }

            // 标准格式: XX-YY，例如 60-40, 80-50, 20-10
            var pattern = @"^\d{1,3}-\d{1,3}$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(ScratchDig.Trim(), pattern))
            {
                errorMessage = "疵病等级格式错误，应为XX-YY格式（如60-40）";
                return false;
            }

            // 验证数值范围
            var parts = ScratchDig.Split('-');
            if (int.TryParse(parts[0], out int scratch) && int.TryParse(parts[1], out int dig))
            {
                if (scratch < 10 || scratch > 120 || dig < 5 || dig > 80)
                {
                    errorMessage = "疵病等级数值超出有效范围";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 验证面形精度格式
        /// </summary>
        public bool ValidateSurfaceAccuracy(out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(Accuracy))
            {
                errorMessage = "面形精度不能为空";
                return false;
            }

            // 支持的格式: λ/N, λ/N @ 632.8nm, N/N波长
            var patterns = new[]
            {
                @"^λ/\d+$",
                @"^λ/\d+\s*@\s*\d+(\.\d+)?\s*nm$",
                @"^\d+/\d+\s*波长$"
            };

            if (!Array.Exists(patterns, pattern => 
                System.Text.RegularExpressions.Regex.IsMatch(Accuracy.Trim(), pattern)))
            {
                errorMessage = "面形精度格式错误，支持格式：λ/4、λ/4 @ 632.8nm、1/4波长";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取表面质量等级
        /// </summary>
        public SurfaceQualityGrade GetQualityGrade()
        {
            // 根据疵病等级判断
            if (ScratchDig.StartsWith("10-") || ScratchDig.StartsWith("20-"))
                return SurfaceQualityGrade.LaserGrade;
            else if (ScratchDig.StartsWith("40-") || ScratchDig.StartsWith("60-"))
                return SurfaceQualityGrade.PrecisionGrade;
            else if (ScratchDig.StartsWith("80-"))
                return SurfaceQualityGrade.StandardGrade;
            else
                return SurfaceQualityGrade.CommercialGrade;
        }

        /// <summary>
        /// 从质量等级设置默认值
        /// </summary>
        public void SetFromGrade(SurfaceQualityGrade grade)
        {
            switch (grade)
            {
                case SurfaceQualityGrade.LaserGrade:
                    Accuracy = "λ/10";
                    PowerFringes = "0.5";
                    IrregularityFringes = "0.1";
                    ScratchDig = "10-5";
                    Roughness = "5Å";
                    Cleanliness = CleanlinessLevel.UltraClean;
                    break;

                case SurfaceQualityGrade.PrecisionGrade:
                    Accuracy = "λ/4";
                    PowerFringes = "1";
                    IrregularityFringes = "0.5";
                    ScratchDig = "40-20";
                    Roughness = "10Å";
                    Cleanliness = CleanlinessLevel.Clean;
                    break;

                case SurfaceQualityGrade.StandardGrade:
                    Accuracy = "λ/2";
                    PowerFringes = "3";
                    IrregularityFringes = "1";
                    ScratchDig = "60-40";
                    Roughness = "20Å";
                    Cleanliness = CleanlinessLevel.Standard;
                    break;

                case SurfaceQualityGrade.CommercialGrade:
                    Accuracy = "λ";
                    PowerFringes = "5";
                    IrregularityFringes = "2";
                    ScratchDig = "80-50";
                    Roughness = "50Å";
                    Cleanliness = CleanlinessLevel.Normal;
                    break;
            }
        }

        /// <summary>
        /// 克隆表面质量规格
        /// </summary>
        public SurfaceQualitySpec Clone()
        {
            return new SurfaceQualitySpec
            {
                Accuracy = this.Accuracy,
                PowerFringes = this.PowerFringes,
                IrregularityFringes = this.IrregularityFringes,
                ScratchDig = this.ScratchDig,
                Roughness = this.Roughness,
                EdgeBevel = this.EdgeBevel,
                Cleanliness = this.Cleanliness
            };
        }
    }

    /// <summary>
    /// 表面质量等级
    /// </summary>
    public enum SurfaceQualityGrade
    {
        /// <summary>
        /// 商业级
        /// </summary>
        [Description("商业级")]
        CommercialGrade,

        /// <summary>
        /// 标准级
        /// </summary>
        [Description("标准级")]
        StandardGrade,

        /// <summary>
        /// 精密级
        /// </summary>
        [Description("精密级")]
        PrecisionGrade,

        /// <summary>
        /// 激光级
        /// </summary>
        [Description("激光级")]
        LaserGrade
    }

    /// <summary>
    /// 清洁度等级
    /// </summary>
    public enum CleanlinessLevel
    {
        /// <summary>
        /// 普通
        /// </summary>
        [Description("普通")]
        Normal,

        /// <summary>
        /// 标准
        /// </summary>
        [Description("标准")]
        Standard,

        /// <summary>
        /// 洁净
        /// </summary>
        [Description("洁净")]
        Clean,

        /// <summary>
        /// 超洁净
        /// </summary>
        [Description("超洁净")]
        UltraClean
    }
}