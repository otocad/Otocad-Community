using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace OtoCAD.OpticEntity
{
    /// <summary>
    /// 镀膜规格
    /// </summary>
    public class CoatingSpecification
    {
        [Category("基本参数")]
        [DisplayName("镀膜类型")]
        [Description("镀膜的类型")]
        public string Type { get; set; } = "AR";

        [Category("基本参数")]
        [DisplayName("镀膜材料")]
        [Description("镀膜使用的材料")]
        public string Material { get; set; } = "MgF2";

        [Category("基本参数")]
        [DisplayName("镀膜表面")]
        [Description("镀膜应用的表面")]
        public CoatingSurface Surface { get; set; } = CoatingSurface.Both;

        [Category("光学特性")]
        [DisplayName("中心波长")]
        [Description("镀膜设计的中心波长(nm)")]
        public double CenterWavelength { get; set; } = 550;

        [Category("光学特性")]
        [DisplayName("波长范围")]
        [Description("镀膜的工作波长范围")]
        public string WavelengthRange { get; set; } = "400-700nm";

        [Category("光学特性")]
        [DisplayName("反射率")]
        [Description("镀膜的平均反射率")]
        public double Reflectance { get; set; } = 0.5;

        [Category("光学特性")]
        [DisplayName("透过率")]
        [Description("镀膜的平均透过率")]
        public double Transmittance { get; set; } = 99.5;

        [Category("物理特性")]
        [DisplayName("膜层数")]
        [Description("镀膜的层数")]
        public int LayerCount { get; set; } = 1;

        [Category("物理特性")]
        [DisplayName("总厚度")]
        [Description("镀膜的总厚度(nm)")]
        public double TotalThickness { get; set; } = 138;

        [Category("环境耐受性")]
        [DisplayName("耐温范围")]
        [Description("镀膜的工作温度范围")]
        public string TemperatureRange { get; set; } = "-40°C ~ +85°C";

        [Category("环境耐受性")]
        [DisplayName("耐湿度")]
        [Description("镀膜的湿度耐受性")]
        public string HumidityResistance { get; set; } = "95% RH @ 40°C";

        [Category("环境耐受性")]
        [DisplayName("耐磨性")]
        [Description("镀膜的耐磨性等级")]
        public DurabilityGrade Durability { get; set; } = DurabilityGrade.Moderate;

        [Category("环境耐受性")]
        [DisplayName("激光损伤阈值")]
        [Description("镀膜的激光损伤阈值(J/cm²)")]
        public double LaserDamageThreshold { get; set; } = 10;

        /// <summary>
        /// 获取镀膜类型的详细描述
        /// </summary>
        public string GetCoatingDescription()
        {
            switch (Type)
            {
                case "AR":
                    return "增透膜";
                case "HR":
                    return "高反膜";
                case "BS":
                    return "分光膜";
                case "PR":
                    return "部分反射膜";
                case "Filter":
                    return "滤光膜";
                case "Polarizing":
                    return "偏振膜";
                case "ITO":
                    return "导电膜";
                default:
                    return "自定义膜";
            }
        }

        /// <summary>
        /// 验证镀膜参数
        /// </summary>
        public bool ValidateParameters(out List<string> errors)
        {
            errors = new List<string>();

            // 验证波长
            if (CenterWavelength < 200 || CenterWavelength > 2000)
            {
                errors.Add("中心波长应在200-2000nm范围内");
            }

            // 验证反射率和透过率
            if (Reflectance < 0 || Reflectance > 100)
            {
                errors.Add("反射率应在0-100%范围内");
            }

            if (Transmittance < 0 || Transmittance > 100)
            {
                errors.Add("透过率应在0-100%范围内");
            }

            // AR膜的特殊验证
            if (Type == "AR" && Reflectance > 5)
            {
                errors.Add("增透膜的反射率通常应小于5%");
            }

            // HR膜的特殊验证
            if (Type == "HR" && Reflectance < 95)
            {
                errors.Add("高反膜的反射率通常应大于95%");
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// 根据镀膜类型设置默认参数
        /// </summary>
        public void SetDefaultsForType(string coatingType)
        {
            Type = coatingType;

            switch (coatingType)
            {
                case "AR": // 增透膜
                    Material = "MgF2";
                    LayerCount = 1;
                    Reflectance = 0.5;
                    Transmittance = 99.5;
                    WavelengthRange = "400-700nm";
                    CenterWavelength = 550;
                    break;

                case "HR": // 高反膜
                    Material = "Dielectric";
                    LayerCount = 20;
                    Reflectance = 99.5;
                    Transmittance = 0.5;
                    WavelengthRange = "450-650nm";
                    CenterWavelength = 550;
                    break;

                case "BS": // 分光膜
                    Material = "Dielectric";
                    LayerCount = 10;
                    Reflectance = 50;
                    Transmittance = 50;
                    WavelengthRange = "400-700nm";
                    CenterWavelength = 550;
                    break;

                case "Filter": // 滤光膜
                    Material = "Dielectric";
                    LayerCount = 30;
                    Reflectance = 1;
                    Transmittance = 90;
                    WavelengthRange = "530-570nm";
                    CenterWavelength = 550;
                    break;

                case "Polarizing": // 偏振膜
                    Material = "Wire Grid";
                    LayerCount = 1;
                    Reflectance = 10;
                    Transmittance = 85;
                    WavelengthRange = "400-700nm";
                    CenterWavelength = 550;
                    break;
            }
        }

        /// <summary>
        /// 克隆镀膜规格
        /// </summary>
        public CoatingSpecification Clone()
        {
            return new CoatingSpecification
            {
                Type = this.Type,
                Material = this.Material,
                Surface = this.Surface,
                CenterWavelength = this.CenterWavelength,
                WavelengthRange = this.WavelengthRange,
                Reflectance = this.Reflectance,
                Transmittance = this.Transmittance,
                LayerCount = this.LayerCount,
                TotalThickness = this.TotalThickness,
                TemperatureRange = this.TemperatureRange,
                HumidityResistance = this.HumidityResistance,
                Durability = this.Durability,
                LaserDamageThreshold = this.LaserDamageThreshold
            };
        }
    }

    /// <summary>
    /// 镀膜表面
    /// </summary>
    public enum CoatingSurface
    {
        /// <summary>
        /// 仅第一面
        /// </summary>
        [Description("仅第一面")]
        FirstSurface,

        /// <summary>
        /// 仅第二面
        /// </summary>
        [Description("仅第二面")]
        SecondSurface,

        /// <summary>
        /// 双面
        /// </summary>
        [Description("双面")]
        Both,

        /// <summary>
        /// 无镀膜
        /// </summary>
        [Description("无镀膜")]
        None
    }

    /// <summary>
    /// 耐久性等级
    /// </summary>
    public enum DurabilityGrade
    {
        /// <summary>
        /// 低
        /// </summary>
        [Description("低")]
        Low,

        /// <summary>
        /// 中等
        /// </summary>
        [Description("中等")]
        Moderate,

        /// <summary>
        /// 高
        /// </summary>
        [Description("高")]
        High,

        /// <summary>
        /// 军标
        /// </summary>
        [Description("军标")]
        MilSpec
    }
}