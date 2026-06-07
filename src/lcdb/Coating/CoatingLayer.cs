using System;
using System.ComponentModel;

namespace lcdb.Coating
{
    /// <summary>
    /// 镀膜层
    /// </summary>
    public class CoatingLayer
    {
        /// <summary>
        /// 层索引（从基底开始计数）
        /// </summary>
        [Description("层号")]
        public int Index { get; set; }

        /// <summary>
        /// 材料名称
        /// </summary>
        [Description("材料")]
        public string Material { get; set; } = "SiO2";

        /// <summary>
        /// 物理厚度（纳米）
        /// </summary>
        [Description("厚度(nm)")]
        public double PhysicalThickness { get; set; }

        /// <summary>
        /// 光学厚度（QWOT）
        /// </summary>
        [Description("光学厚度")]
        public double OpticalThickness { get; set; }

        /// <summary>
        /// 折射率（在设计波长处）
        /// </summary>
        [Description("折射率")]
        public double RefractiveIndex { get; set; } = 1.46;

        /// <summary>
        /// 吸收系数
        /// </summary>
        [Description("吸收系数")]
        public double AbsorptionCoefficient { get; set; } = 0;

        /// <summary>
        /// 应力（MPa）
        /// </summary>
        [Description("应力")]
        public double Stress { get; set; }

        /// <summary>
        /// 密度（g/cm³）
        /// </summary>
        [Description("密度")]
        public double Density { get; set; }

        /// <summary>
        /// 沉积速率（nm/s）
        /// </summary>
        [Description("沉积速率")]
        public double DepositionRate { get; set; } = 0.5;

        /// <summary>
        /// 温度（°C）
        /// </summary>
        [Description("温度")]
        public double Temperature { get; set; } = 150;

        /// <summary>
        /// 备注
        /// </summary>
        [Description("备注")]
        public string Remarks { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public CoatingLayer()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="index">层索引</param>
        /// <param name="material">材料</param>
        /// <param name="thickness">厚度</param>
        /// <param name="refractiveIndex">折射率</param>
        public CoatingLayer(int index, string material, double thickness, double refractiveIndex)
        {
            Index = index;
            Material = material;
            PhysicalThickness = thickness;
            RefractiveIndex = refractiveIndex;
            CalculateOpticalThickness(550); // 默认550nm
        }

        /// <summary>
        /// 计算光学厚度
        /// </summary>
        /// <param name="wavelength">设计波长（nm）</param>
        public void CalculateOpticalThickness(double wavelength)
        {
            OpticalThickness = (RefractiveIndex * PhysicalThickness) / (wavelength / 4.0);
        }

        /// <summary>
        /// 根据光学厚度计算物理厚度
        /// </summary>
        /// <param name="wavelength">设计波长（nm）</param>
        public void CalculatePhysicalThickness(double wavelength)
        {
            PhysicalThickness = (OpticalThickness * wavelength / 4.0) / RefractiveIndex;
        }

        /// <summary>
        /// 克隆
        /// </summary>
        public CoatingLayer Clone()
        {
            return new CoatingLayer
            {
                Index = this.Index,
                Material = this.Material,
                PhysicalThickness = this.PhysicalThickness,
                OpticalThickness = this.OpticalThickness,
                RefractiveIndex = this.RefractiveIndex,
                AbsorptionCoefficient = this.AbsorptionCoefficient,
                Stress = this.Stress,
                Density = this.Density,
                DepositionRate = this.DepositionRate,
                Temperature = this.Temperature,
                Remarks = this.Remarks
            };
        }

        /// <summary>
        /// 验证层参数
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = null;

            if (PhysicalThickness <= 0)
            {
                errorMessage = $"第{Index}层厚度必须大于0";
                return false;
            }

            if (PhysicalThickness > 10000)
            {
                errorMessage = $"第{Index}层厚度不能超过10μm";
                return false;
            }

            if (RefractiveIndex < 1.0 || RefractiveIndex > 4.0)
            {
                errorMessage = $"第{Index}层折射率应在1.0-4.0之间";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Material))
            {
                errorMessage = $"第{Index}层材料不能为空";
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return $"Layer {Index}: {Material} ({PhysicalThickness:F1}nm, n={RefractiveIndex:F3})";
        }
    }
}