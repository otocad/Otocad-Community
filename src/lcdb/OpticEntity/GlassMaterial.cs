using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace OtoCAD.OpticEntity
{
    /// <summary>
    /// 玻璃材料类，用于存储光学玻璃的物理和光学属性
    /// </summary>
    public class GlassMaterial
    {
        /// <summary>
        /// 玻璃代码（唯一标识符）
        /// </summary>
        [Category("基本信息")]
        [DisplayName("玻璃代码")]
        [Description("玻璃的唯一标识符")]
        public string Code { get; set; }

        /// <summary>
        /// 玻璃名称
        /// </summary>
        [Category("基本信息")]
        [DisplayName("玻璃名称")]
        [Description("玻璃的名称")]
        public string Name { get; set; }

        /// <summary>
        /// 制造商
        /// </summary>
        [Category("基本信息")]
        [DisplayName("制造商")]
        [Description("玻璃制造商")]
        public string Manufacturer { get; set; }

        /// <summary>
        /// 玻璃类型
        /// </summary>
        [Category("基本信息")]
        [DisplayName("玻璃类型")]
        [Description("玻璃的类型分类")]
        public GlassType Type { get; set; }

        #region 光学特性

        /// <summary>
        /// d光（587.56nm）折射率
        /// </summary>
        [Category("光学特性")]
        [DisplayName("nd")]
        [Description("d光（587.56nm）的折射率")]
        public double Nd { get; set; }

        /// <summary>
        /// F光（486.13nm）折射率
        /// </summary>
        [Category("光学特性")]
        [DisplayName("nF")]
        [Description("F光（486.13nm）的折射率")]
        public double NF { get; set; }

        /// <summary>
        /// C光（656.27nm）折射率
        /// </summary>
        [Category("光学特性")]
        [DisplayName("nC")]
        [Description("C光（656.27nm）的折射率")]
        public double NC { get; set; }

        /// <summary>
        /// 阿贝数
        /// </summary>
        [Category("光学特性")]
        [DisplayName("阿贝数")]
        [Description("色散系数")]
        [JsonIgnore]
        public double AbbeNumber => (Nd - 1) / (NF - NC);

        /// <summary>
        /// 部分色散
        /// </summary>
        [Category("光学特性")]
        [DisplayName("部分色散")]
        [Description("PgF值")]
        public double PartialDispersion { get; set; }

        #endregion

        #region 物理特性

        /// <summary>
        /// 密度 (g/cm³)
        /// </summary>
        [Category("物理特性")]
        [DisplayName("密度")]
        [Description("玻璃密度 (g/cm³)")]
        public double Density { get; set; }

        /// <summary>
        /// 热膨胀系数 (10^-6/K)
        /// </summary>
        [Category("物理特性")]
        [DisplayName("热膨胀系数")]
        [Description("线性热膨胀系数 (10^-6/K)")]
        public double ThermalExpansion { get; set; }

        /// <summary>
        /// 软化温度 (°C)
        /// </summary>
        [Category("物理特性")]
        [DisplayName("软化温度")]
        [Description("玻璃软化温度 (°C)")]
        public double SofteningTemperature { get; set; }

        /// <summary>
        /// 杨氏模量 (GPa)
        /// </summary>
        [Category("物理特性")]
        [DisplayName("杨氏模量")]
        [Description("弹性模量 (GPa)")]
        public double YoungsModulus { get; set; }

        /// <summary>
        /// 泊松比
        /// </summary>
        [Category("物理特性")]
        [DisplayName("泊松比")]
        [Description("横向应变与纵向应变之比")]
        public double PoissonRatio { get; set; }

        #endregion

        #region 化学特性

        /// <summary>
        /// 耐水性等级
        /// </summary>
        [Category("化学特性")]
        [DisplayName("耐水性")]
        [Description("耐水性等级 (1-5)")]
        public int WaterResistance { get; set; }

        /// <summary>
        /// 耐酸性等级
        /// </summary>
        [Category("化学特性")]
        [DisplayName("耐酸性")]
        [Description("耐酸性等级 (1-5)")]
        public int AcidResistance { get; set; }

        /// <summary>
        /// 耐碱性等级
        /// </summary>
        [Category("化学特性")]
        [DisplayName("耐碱性")]
        [Description("耐碱性等级 (1-5)")]
        public int AlkaliResistance { get; set; }

        #endregion

        #region 其他属性

        /// <summary>
        /// 备注
        /// </summary>
        [Category("其他")]
        [DisplayName("备注")]
        [Description("其他说明信息")]
        public string Remarks { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Category("其他")]
        [DisplayName("创建时间")]
        [Description("材料数据创建时间")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Category("其他")]
        [DisplayName("更新时间")]
        [Description("材料数据最后更新时间")]
        public DateTime UpdateTime { get; set; }

        #endregion

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public GlassMaterial()
        {
            CreateTime = DateTime.Now;
            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public GlassMaterial(string code, string name, string manufacturer)
        {
            Code = code;
            Name = name;
            Manufacturer = manufacturer;
            CreateTime = DateTime.Now;
            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 克隆方法
        /// </summary>
        public GlassMaterial Clone()
        {
            return new GlassMaterial
            {
                Code = this.Code,
                Name = this.Name,
                Manufacturer = this.Manufacturer,
                Type = this.Type,
                Nd = this.Nd,
                NF = this.NF,
                NC = this.NC,
                PartialDispersion = this.PartialDispersion,
                Density = this.Density,
                ThermalExpansion = this.ThermalExpansion,
                SofteningTemperature = this.SofteningTemperature,
                YoungsModulus = this.YoungsModulus,
                PoissonRatio = this.PoissonRatio,
                WaterResistance = this.WaterResistance,
                AcidResistance = this.AcidResistance,
                AlkaliResistance = this.AlkaliResistance,
                Remarks = this.Remarks,
                CreateTime = this.CreateTime,
                UpdateTime = DateTime.Now
            };
        }

        /// <summary>
        /// 重写ToString方法
        /// </summary>
        public override string ToString()
        {
            return $"{Code} - {Name} ({Manufacturer})";
        }
    }

    /// <summary>
    /// 玻璃类型枚举
    /// </summary>
    public enum GlassType
    {
        [Description("冕牌玻璃")]
        Crown,
        
        [Description("火石玻璃")]
        Flint,
        
        [Description("重冕玻璃")]
        DenseCrown,
        
        [Description("重火石玻璃")]
        DenseFlint,
        
        [Description("特种玻璃")]
        Special,
        
        [Description("超低色散玻璃")]
        ED,
        
        [Description("低色散玻璃")]
        LD,
        
        [Description("其他")]
        Other
    }
}