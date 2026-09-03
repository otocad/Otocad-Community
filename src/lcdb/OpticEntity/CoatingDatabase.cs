using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using lcdb.Annotation;

namespace OtoCAD.OpticEntity
{
    /// <summary>
    /// 镀膜规格
    /// </summary>
    public class CoatingSpec
    {
        /// <summary>
        /// 镀膜名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 镀膜描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 设计波长 (nm)
        /// </summary>
        public double DesignWavelength { get; set; }

        /// <summary>
        /// 工作波长范围 (nm)
        /// </summary>
        public double MinWavelength { get; set; }
        public double MaxWavelength { get; set; }

        /// <summary>
        /// 反射率 (%)
        /// </summary>
        public double Reflectance { get; set; }

        /// <summary>
        /// 透射率 (%)
        /// </summary>
        public double Transmittance { get; set; }

        /// <summary>
        /// 入射角范围 (度)
        /// </summary>
        public double MaxIncidentAngle { get; set; }

        /// <summary>
        /// 基底材料
        /// </summary>
        public List<string> SubstrateMaterials { get; set; } = new List<string>();

        /// <summary>
        /// 镀膜层数
        /// </summary>
        public int LayerCount { get; set; }

        /// <summary>
        /// 镀膜厚度 (nm)
        /// </summary>
        public double Thickness { get; set; }

        /// <summary>
        /// 激光损伤阈值 (J/cm²)
        /// </summary>
        public double LaserDamageThreshold { get; set; }

        /// <summary>
        /// 环境稳定性
        /// </summary>
        public string EnvironmentalStability { get; set; }

        /// <summary>
        /// 成本等级
        /// </summary>
        public CostLevel CostLevel { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public CoatingSpec()
        {
            SubstrateMaterials = new List<string>();
            EnvironmentalStability = "标准";
            CostLevel = CostLevel.Medium;
        }
    }

    /// <summary>
    /// 成本等级枚举
    /// </summary>
    public enum CostLevel
    {
        Low,
        Medium,
        High,
        VeryHigh
    }

    /// <summary>
    /// 镀膜数据库
    /// 提供镀膜规格库、颜色映射和符号库
    /// </summary>
    public static class CoatingDatabase
    {
        #region 镀膜规格库

        /// <summary>
        /// 镀膜规格库
        /// </summary>
        public static readonly Dictionary<CoatingType, CoatingSpec> CoatingSpecs = new Dictionary<CoatingType, CoatingSpec>
        {
            {
                CoatingType.AR,
                new CoatingSpec
                {
                    Name = "增透膜",
                    Description = "减少表面反射，提高透射率",
                    DesignWavelength = 587.56,
                    MinWavelength = 400,
                    MaxWavelength = 700,
                    Reflectance = 0.5,
                    Transmittance = 99.5,
                    MaxIncidentAngle = 30,
                    SubstrateMaterials = { "BK7", "SF10", "SF11", "熔融石英" },
                    LayerCount = 2,
                    Thickness = 125,
                    LaserDamageThreshold = 10,
                    EnvironmentalStability = "优秀",
                    CostLevel = CostLevel.Medium
                }
            },
            {
                CoatingType.HR,
                new CoatingSpec
                {
                    Name = "高反膜",
                    Description = "高反射率镀膜，用于反射镜",
                    DesignWavelength = 587.56,
                    MinWavelength = 400,
                    MaxWavelength = 700,
                    Reflectance = 99.5,
                    Transmittance = 0.5,
                    MaxIncidentAngle = 45,
                    SubstrateMaterials = { "BK7", "SF10", "SF11", "熔融石英", "金属" },
                    LayerCount = 7,
                    Thickness = 875,
                    LaserDamageThreshold = 5,
                    EnvironmentalStability = "良好",
                    CostLevel = CostLevel.High
                }
            },
            {
                CoatingType.PR,
                new CoatingSpec
                {
                    Name = "部分反射膜",
                    Description = "部分反射部分透射，用于分束镜",
                    DesignWavelength = 587.56,
                    MinWavelength = 400,
                    MaxWavelength = 700,
                    Reflectance = 50,
                    Transmittance = 50,
                    MaxIncidentAngle = 45,
                    SubstrateMaterials = { "BK7", "SF10", "熔融石英" },
                    LayerCount = 3,
                    Thickness = 250,
                    LaserDamageThreshold = 8,
                    EnvironmentalStability = "良好",
                    CostLevel = CostLevel.High
                }
            },
            {
                CoatingType.BBAR,
                new CoatingSpec
                {
                    Name = "宽带增透膜",
                    Description = "宽波段增透镀膜，覆盖可见光全波段",
                    DesignWavelength = 550,
                    MinWavelength = 400,
                    MaxWavelength = 700,
                    Reflectance = 0.5,
                    Transmittance = 99.5,
                    MaxIncidentAngle = 30,
                    SubstrateMaterials = { "BK7", "SF10", "SF11", "熔融石英" },
                    LayerCount = 4,
                    Thickness = 350,
                    LaserDamageThreshold = 12,
                    EnvironmentalStability = "优秀",
                    CostLevel = CostLevel.VeryHigh
                }
            },
            {
                CoatingType.Custom,
                new CoatingSpec
                {
                    Name = "自定义镀膜",
                    Description = "根据特定需求定制的镀膜",
                    DesignWavelength = 587.56,
                    MinWavelength = 350,
                    MaxWavelength = 2500,
                    Reflectance = 1,
                    Transmittance = 99,
                    MaxIncidentAngle = 60,
                    SubstrateMaterials = { "各种材料" },
                    LayerCount = 1,
                    Thickness = 100,
                    LaserDamageThreshold = 1,
                    EnvironmentalStability = "待定",
                    CostLevel = CostLevel.VeryHigh
                }
            }
        };

        #endregion

        #region 镀膜颜色映射

        /// <summary>
        /// 镀膜颜色映射
        /// </summary>
        public static readonly Dictionary<CoatingType, Color> CoatingColors = new Dictionary<CoatingType, Color>
        {
            { CoatingType.AR, Color.Green },
            { CoatingType.HR, Color.Red },
            { CoatingType.PR, Color.Blue },
            { CoatingType.BBAR, Color.Purple },
            { CoatingType.Custom, Color.Yellow }
        };

        /// <summary>
        /// 镀膜颜色映射（OtoCAD颜色）
        /// </summary>
        public static readonly Dictionary<CoatingType, lcdb.Colors.Color> CoatingColorsOtoCAD = new Dictionary<CoatingType, lcdb.Colors.Color>
        {
            { CoatingType.AR, lcdb.Colors.Color.FromColor(Color.Green) },
            { CoatingType.HR, lcdb.Colors.Color.FromColor(Color.Red) },
            { CoatingType.PR, lcdb.Colors.Color.FromColor(Color.Blue) },
            { CoatingType.BBAR, lcdb.Colors.Color.FromColor(Color.Purple) },
            { CoatingType.Custom, lcdb.Colors.Color.FromColor(Color.Yellow) }
        };

        #endregion

        #region 镀膜符号库

        /// <summary>
        /// 镀膜符号库
        /// </summary>
        public static readonly Dictionary<CoatingType, string> CoatingSymbols = new Dictionary<CoatingType, string>
        {
            { CoatingType.AR, "AR" },
            { CoatingType.HR, "HR" },
            { CoatingType.PR, "PR" },
            { CoatingType.BBAR, "BBAR" },
            { CoatingType.Custom, "C" }
        };

        /// <summary>
        /// 镀膜详细符号库
        /// </summary>
        public static readonly Dictionary<CoatingType, string> CoatingDetailedSymbols = new Dictionary<CoatingType, string>
        {
            { CoatingType.AR, "Anti-Reflection" },
            { CoatingType.HR, "High Reflection" },
            { CoatingType.PR, "Partial Reflection" },
            { CoatingType.BBAR, "Broadband AR" },
            { CoatingType.Custom, "Custom" }
        };

        /// <summary>
        /// 镀膜中文名称
        /// </summary>
        public static readonly Dictionary<CoatingType, string> CoatingChineseNames = new Dictionary<CoatingType, string>
        {
            { CoatingType.AR, "增透膜" },
            { CoatingType.HR, "高反膜" },
            { CoatingType.PR, "部分反射膜" },
            { CoatingType.BBAR, "宽带增透膜" },
            { CoatingType.Custom, "自定义镀膜" }
        };

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取镀膜规格
        /// </summary>
        /// <param name="coatingType">镀膜类型</param>
        /// <returns>镀膜规格</returns>
        public static CoatingSpec GetCoatingSpec(CoatingType coatingType)
        {
            return CoatingSpecs.ContainsKey(coatingType) ? CoatingSpecs[coatingType] : CoatingSpecs[CoatingType.Custom];
        }

        /// <summary>
        /// 获取镀膜颜色
        /// </summary>
        /// <param name="coatingType">镀膜类型</param>
        /// <returns>镀膜颜色</returns>
        public static Color GetCoatingColor(CoatingType coatingType)
        {
            return CoatingColors.ContainsKey(coatingType) ? CoatingColors[coatingType] : CoatingColors[CoatingType.Custom];
        }

        /// <summary>
        /// 获取镀膜颜色（OtoCAD）
        /// </summary>
        /// <param name="coatingType">镀膜类型</param>
        /// <returns>镀膜颜色</returns>
        public static lcdb.Colors.Color GetCoatingColorOtoCAD(CoatingType coatingType)
        {
            return CoatingColorsOtoCAD.ContainsKey(coatingType) ? CoatingColorsOtoCAD[coatingType] : CoatingColorsOtoCAD[CoatingType.Custom];
        }

        /// <summary>
        /// 获取镀膜符号
        /// </summary>
        /// <param name="coatingType">镀膜类型</param>
        /// <returns>镀膜符号</returns>
        public static string GetCoatingSymbol(CoatingType coatingType)
        {
            return CoatingSymbols.ContainsKey(coatingType) ? CoatingSymbols[coatingType] : CoatingSymbols[CoatingType.Custom];
        }

        /// <summary>
        /// 获取镀膜详细符号
        /// </summary>
        /// <param name="coatingType">镀膜类型</param>
        /// <returns>镀膜详细符号</returns>
        public static string GetCoatingDetailedSymbol(CoatingType coatingType)
        {
            return CoatingDetailedSymbols.ContainsKey(coatingType) ? CoatingDetailedSymbols[coatingType] : CoatingDetailedSymbols[CoatingType.Custom];
        }

        /// <summary>
        /// 获取镀膜中文名称
        /// </summary>
        /// <param name="coatingType">镀膜类型</param>
        /// <returns>镀膜中文名称</returns>
        public static string GetCoatingChineseName(CoatingType coatingType)
        {
            return CoatingChineseNames.ContainsKey(coatingType) ? CoatingChineseNames[coatingType] : CoatingChineseNames[CoatingType.Custom];
        }

        /// <summary>
        /// 获取所有镀膜类型
        /// </summary>
        /// <returns>镀膜类型列表</returns>
        public static List<CoatingType> GetAllCoatingTypes()
        {
            return new List<CoatingType>(CoatingSpecs.Keys);
        }

        /// <summary>
        /// 检查镀膜是否适用于基底材料
        /// </summary>
        /// <param name="coatingType">镀膜类型</param>
        /// <param name="substrateMaterial">基底材料</param>
        /// <returns>是否适用</returns>
        public static bool IsCoatingCompatible(CoatingType coatingType, string substrateMaterial)
        {
            var spec = GetCoatingSpec(coatingType);
            return spec.SubstrateMaterials.Any(m => string.Equals(m, substrateMaterial, StringComparison.OrdinalIgnoreCase)) || 
                   spec.SubstrateMaterials.Any(m => string.Equals(m, "各种材料", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 获取适用的镀膜类型
        /// </summary>
        /// <param name="substrateMaterial">基底材料</param>
        /// <returns>适用的镀膜类型列表</returns>
        public static List<CoatingType> GetCompatibleCoatings(string substrateMaterial)
        {
            var compatibleCoatings = new List<CoatingType>();
            
            foreach (var kvp in CoatingSpecs)
            {
                if (IsCoatingCompatible(kvp.Key, substrateMaterial))
                {
                    compatibleCoatings.Add(kvp.Key);
                }
            }

            return compatibleCoatings;
        }

        /// <summary>
        /// 按成本等级过滤镀膜
        /// </summary>
        /// <param name="maxCostLevel">最大成本等级</param>
        /// <returns>符合成本要求的镀膜类型列表</returns>
        public static List<CoatingType> FilterByCostLevel(CostLevel maxCostLevel)
        {
            var filteredCoatings = new List<CoatingType>();
            
            foreach (var kvp in CoatingSpecs)
            {
                if (kvp.Value.CostLevel <= maxCostLevel)
                {
                    filteredCoatings.Add(kvp.Key);
                }
            }

            return filteredCoatings;
        }

        /// <summary>
        /// 按波长范围过滤镀膜
        /// </summary>
        /// <param name="wavelength">目标波长</param>
        /// <returns>适用的镀膜类型列表</returns>
        public static List<CoatingType> FilterByWavelength(double wavelength)
        {
            var filteredCoatings = new List<CoatingType>();
            
            foreach (var kvp in CoatingSpecs)
            {
                if (wavelength >= kvp.Value.MinWavelength && wavelength <= kvp.Value.MaxWavelength)
                {
                    filteredCoatings.Add(kvp.Key);
                }
            }

            return filteredCoatings;
        }

        /// <summary>
        /// 按入射角过滤镀膜
        /// </summary>
        /// <param name="incidentAngle">入射角度</param>
        /// <returns>适用的镀膜类型列表</returns>
        public static List<CoatingType> FilterByIncidentAngle(double incidentAngle)
        {
            var filteredCoatings = new List<CoatingType>();
            
            foreach (var kvp in CoatingSpecs)
            {
                if (Math.Abs(incidentAngle) <= kvp.Value.MaxIncidentAngle)
                {
                    filteredCoatings.Add(kvp.Key);
                }
            }

            return filteredCoatings;
        }

        /// <summary>
        /// 获取镀膜建议
        /// </summary>
        /// <param name="substrateMaterial">基底材料</param>
        /// <param name="wavelength">工作波长</param>
        /// <param name="incidentAngle">入射角</param>
        /// <param name="maxCostLevel">最大成本等级</param>
        /// <returns>建议的镀膜类型列表</returns>
        public static List<CoatingType> GetCoatingRecommendations(string substrateMaterial, double wavelength, double incidentAngle, CostLevel maxCostLevel)
        {
            var compatibleCoatings = GetCompatibleCoatings(substrateMaterial);
            var wavelengthFiltered = FilterByWavelength(wavelength);
            var angleFiltered = FilterByIncidentAngle(incidentAngle);
            var costFiltered = FilterByCostLevel(maxCostLevel);

            // 取交集
            var recommendations = compatibleCoatings
                .Intersect(wavelengthFiltered)
                .Intersect(angleFiltered)
                .Intersect(costFiltered)
                .ToList();

            return recommendations;
        }

        #endregion
    }
}