using lcdb;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using lcdb;

namespace OtoCAD.OpticEntity
{
    /// <summary>
    /// 光学实体基类
    /// </summary>
    public abstract class OpticalEntity : lcdb.Entity
    {
        #region 光学材料属性
        
        /// <summary>
        /// 材料名称
        /// </summary>
        [Category("材料")]
        [DisplayName("材料名称")]
        [Description("光学材料名称，如BK7、熔融石英等")]
        public string Material { get; set; } = "BK7";
        
        /// <summary>
        /// 折射率（nd）
        /// </summary>
        [Category("材料")]
        [DisplayName("折射率")]
        [Description("d光（587.6nm）折射率")]
        public double RefractiveIndex { get; set; } = 1.51680;
        
        /// <summary>
        /// 阿贝数（Vd）
        /// </summary>
        [Category("材料")]
        [DisplayName("阿贝数")]
        [Description("色散特性参数")]
        public double AbbeNumber { get; set; } = 64.17;
        
        #endregion
        
        #region 表面质量属性
        
        /// <summary>
        /// 表面质量等级（ISO 10110-7）
        /// </summary>
        [Category("表面质量")]
        [DisplayName("表面疵病")]
        [Description("按ISO 10110-7标准，格式：5/2×0.16")]
        public string SurfaceQuality { get; set; } = "5/2×0.16";
        
        /// <summary>
        /// 表面光圈数
        /// </summary>
        [Category("表面质量")]
        [DisplayName("光圈数")]
        [Description("表面形状偏差，单位：光圈")]
        public double SurfaceFigure { get; set; } = 3.0;
        
        /// <summary>
        /// 表面不规则度
        /// </summary>
        [Category("表面质量")]
        [DisplayName("局部光圈")]
        [Description("表面不规则度，单位：光圈")]
        public double SurfaceIrregularity { get; set; } = 0.5;
        
        #endregion
        
        #region 公差属性
        
        /// <summary>
        /// 中心厚度公差
        /// </summary>
        [Category("公差")]
        [DisplayName("厚度公差")]
        [Description("中心厚度公差，单位：mm")]
        public double CenterThicknessTolerance { get; set; } = 0.1;
        
        /// <summary>
        /// 直径公差
        /// </summary>
        [Category("公差")]
        [DisplayName("直径公差")]
        [Description("直径公差，单位：mm")]
        public double DiameterTolerance { get; set; } = 0.1;
        
        /// <summary>
        /// 中心偏差
        /// </summary>
        [Category("公差")]
        [DisplayName("中心偏差")]
        [Description("光轴偏心，单位：mm")]
        public double Decentration { get; set; } = 0.05;
        
        /// <summary>
        /// 楔角
        /// </summary>
        [Category("公差")]
        [DisplayName("楔角")]
        [Description("表面倾斜，单位：角分")]
        public double Wedge { get; set; } = 3.0;
        
        #endregion
        
        #region 镀膜属性
        
        /// <summary>
        /// 镀膜类型
        /// </summary>
        [Category("镀膜")]
        [DisplayName("镀膜类型")]
        [Description("镀膜类型：AR(增透)、HR(高反)、BS(分束)等")]
        public string CoatingType { get; set; } = "None";
        
        /// <summary>
        /// 镀膜规格
        /// </summary>
        [Category("镀膜")]
        [DisplayName("镀膜规格")]
        [Description("镀膜规格描述，如：AR@400-700nm,R<0.5%")]
        public string CoatingSpecification { get; set; } = "";
        
        /// <summary>
        /// 镀膜面
        /// </summary>
        [Category("镀膜")]
        [DisplayName("镀膜面")]
        [Description("镀膜应用面：S1、S2、Both")]
        public string CoatingSurface { get; set; } = "None";
        
        #endregion
        
        #region 标识属性
        
        /// <summary>
        /// 零件编号
        /// </summary>
        [Category("标识")]
        [DisplayName("零件编号")]
        [Description("光学零件编号")]
        public string PartNumber { get; set; } = "";
        
        /// <summary>
        /// 制造商
        /// </summary>
        [Category("标识")]
        [DisplayName("制造商")]
        [Description("制造商信息")]
        public string Manufacturer { get; set; } = "";
        
        /// <summary>
        /// 备注
        /// </summary>
        [Category("标识")]
        [DisplayName("备注")]
        [Description("其他说明信息")]
        public string Remarks { get; set; } = "";
        
        #endregion
        
        #region 抽象方法
        
        /// <summary>
        /// 计算光学元件的体积
        /// </summary>
        /// <returns>体积（立方毫米）</returns>
        public abstract double CalculateVolume();
        
        /// <summary>
        /// 计算光学元件的质量
        /// </summary>
        /// <returns>质量（克）</returns>
        public virtual double CalculateMass()
        {
            // 默认使用BK7的密度：2.51 g/cm³
            double density = GetMaterialDensity();
            double volumeInCm3 = CalculateVolume() / 1000.0; // mm³ to cm³
            return volumeInCm3 * density;
        }
        
        /// <summary>
        /// 获取材料密度
        /// </summary>
        /// <returns>密度（g/cm³）</returns>
        protected virtual double GetMaterialDensity()
        {
            // 常见光学材料密度表
            switch (Material?.ToUpper())
            {
                case "BK7":
                case "N-BK7":
                    return 2.51;
                case "FUSED SILICA":
                case "FS":
                case "熔融石英":
                    return 2.20;
                case "SF11":
                case "N-SF11":
                    return 3.98;
                case "F2":
                case "N-F2":
                    return 3.60;
                case "K9":
                    return 2.52;
                case "ZF7":
                    return 3.16;
                default:
                    return 2.51; // 默认BK7密度
            }
        }
        
        /// <summary>
        /// 验证光学参数
        /// </summary>
        /// <returns>验证结果，包含是否有效和错误信息</returns>
        public virtual (bool IsValid, List<string> Errors) ValidateOpticalParameters()
        {
            var errors = new List<string>();
            
            // 验证材料属性
            if (RefractiveIndex < 1.0 || RefractiveIndex > 3.0)
            {
                errors.Add($"折射率 {RefractiveIndex} 超出合理范围 (1.0-3.0)");
            }
            
            if (AbbeNumber < 10 || AbbeNumber > 100)
            {
                errors.Add($"阿贝数 {AbbeNumber} 超出合理范围 (10-100)");
            }
            
            // 验证公差
            if (CenterThicknessTolerance < 0)
            {
                errors.Add("厚度公差不能为负值");
            }
            
            if (DiameterTolerance < 0)
            {
                errors.Add("直径公差不能为负值");
            }
            
            return (errors.Count == 0, errors);
        }
        
        /// <summary>
        /// 获取制图标准格式的标注信息
        /// </summary>
        /// <param name="standard">制图标准：ISO10110、GB等</param>
        /// <returns>标注信息列表</returns>
        public virtual List<string> GetDrawingAnnotations(string standard)
        {
            var annotations = new List<string>();
            
            switch (standard)
            {
                case "ISO10110":
                    // ISO 10110标准标注
                    annotations.Add($"Material: {Material}");
                    annotations.Add($"nd = {RefractiveIndex:F5}");
                    annotations.Add($"νd = {AbbeNumber:F1}");
                    if (!string.IsNullOrEmpty(SurfaceQuality))
                    {
                        annotations.Add($"Surface quality: {SurfaceQuality}");
                    }
                    if (SurfaceFigure > 0)
                    {
                        annotations.Add($"Surface figure: {SurfaceFigure:F1} fringes");
                    }
                    break;
                    
                case "GB":
                case "GB/T":
                    // GB标准标注
                    annotations.Add($"材料：{Material}");
                    annotations.Add($"折射率：{RefractiveIndex:F5}");
                    annotations.Add($"阿贝数：{AbbeNumber:F1}");
                    if (!string.IsNullOrEmpty(SurfaceQuality))
                    {
                        annotations.Add($"表面疵病：{SurfaceQuality}");
                    }
                    if (SurfaceFigure > 0)
                    {
                        annotations.Add($"光圈数：N={SurfaceFigure:F1}");
                    }
                    if (SurfaceIrregularity > 0)
                    {
                        annotations.Add($"局部光圈：ΔN={SurfaceIrregularity:F1}");
                    }
                    break;
            }
            
            // 添加镀膜信息
            if (!string.IsNullOrEmpty(CoatingType) && CoatingType != "None")
            {
                annotations.Add($"{CoatingType}: {CoatingSpecification}");
            }
            
            return annotations;
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 从材料库获取材料属性
        /// </summary>
        /// <param name="materialName">材料名称</param>
        public virtual void LoadMaterialProperties(string materialName)
        {
            Material = materialName;
            // TODO: 从材料数据库加载折射率和阿贝数
            // 这里暂时使用硬编码的值
            switch (materialName?.ToUpper())
            {
                case "BK7":
                case "N-BK7":
                    RefractiveIndex = 1.51680;
                    AbbeNumber = 64.17;
                    break;
                case "FUSED SILICA":
                case "FS":
                    RefractiveIndex = 1.45846;
                    AbbeNumber = 67.82;
                    break;
                case "SF11":
                case "N-SF11":
                    RefractiveIndex = 1.78472;
                    AbbeNumber = 25.76;
                    break;
                default:
                    // 保持当前值
                    break;
            }
        }
        
        #endregion
    }
}