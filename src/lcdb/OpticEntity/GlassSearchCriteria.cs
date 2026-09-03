using System;
using System.Collections.Generic;
using System.Linq;

namespace OtoCAD.OpticEntity
{
    /// <summary>
    /// 玻璃材料搜索条件
    /// </summary>
    public class GlassSearchCriteria
    {
        /// <summary>
        /// 关键字（搜索名称、代码、备注）
        /// </summary>
        public string Keyword { get; set; }

        /// <summary>
        /// 制造商列表
        /// </summary>
        public List<string> Manufacturers { get; set; }

        /// <summary>
        /// 玻璃类型列表
        /// </summary>
        public List<GlassType> Types { get; set; }

        /// <summary>
        /// 折射率范围
        /// </summary>
        public Range<double> RefractiveIndexRange { get; set; }

        /// <summary>
        /// 阿贝数范围
        /// </summary>
        public Range<double> AbbeNumberRange { get; set; }

        /// <summary>
        /// 密度范围
        /// </summary>
        public Range<double> DensityRange { get; set; }

        /// <summary>
        /// 部分色散范围
        /// </summary>
        public Range<double> PartialDispersionRange { get; set; }

        /// <summary>
        /// 创建时间范围
        /// </summary>
        public Range<DateTime> CreateTimeRange { get; set; }

        /// <summary>
        /// 更新时间范围
        /// </summary>
        public Range<DateTime> UpdateTimeRange { get; set; }

        /// <summary>
        /// 是否包含已删除的材料
        /// </summary>
        public bool IncludeDeleted { get; set; }

        public GlassSearchCriteria()
        {
            Manufacturers = new List<string>();
            Types = new List<GlassType>();
        }

        /// <summary>
        /// 检查材料是否匹配搜索条件
        /// </summary>
        public bool Matches(GlassMaterial material)
        {
            if (material == null) return false;

            // 关键字搜索
            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                var keyword = Keyword.ToLower();
                bool keywordMatch = 
                    (material.Code?.ToLower().IndexOf(keyword, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (material.Name?.ToLower().IndexOf(keyword, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (material.Remarks?.ToLower().IndexOf(keyword, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
                
                if (!keywordMatch) return false;
            }

            // 制造商筛选
            if (Manufacturers != null && Manufacturers.Count > 0)
            {
                if (!Manufacturers.Contains(material.Manufacturer))
                    return false;
            }

            // 类型筛选
            if (Types != null && Types.Count > 0)
            {
                if (!Types.Contains(material.Type))
                    return false;
            }

            // 折射率范围
            if (RefractiveIndexRange != null)
            {
                if (!RefractiveIndexRange.Contains(material.Nd))
                    return false;
            }

            // 阿贝数范围
            if (AbbeNumberRange != null)
            {
                if (!AbbeNumberRange.Contains(material.AbbeNumber))
                    return false;
            }

            // 密度范围
            if (DensityRange != null)
            {
                if (!DensityRange.Contains(material.Density))
                    return false;
            }

            // 部分色散范围
            if (PartialDispersionRange != null)
            {
                if (!PartialDispersionRange.Contains(material.PartialDispersion))
                    return false;
            }

            // 创建时间范围
            if (CreateTimeRange != null)
            {
                if (!CreateTimeRange.Contains(material.CreateTime))
                    return false;
            }

            // 更新时间范围
            if (UpdateTimeRange != null)
            {
                if (!UpdateTimeRange.Contains(material.UpdateTime))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 范围类
    /// </summary>
    public class Range<T> where T : IComparable<T>
    {
        public T Min { get; set; }
        public T Max { get; set; }

        public Range() { }

        public Range(T min, T max)
        {
            Min = min;
            Max = max;
        }

        public bool Contains(T value)
        {
            if (value == null) return false;
            
            bool aboveMin = Min == null || value.CompareTo(Min) >= 0;
            bool belowMax = Max == null || value.CompareTo(Max) <= 0;
            
            return aboveMin && belowMax;
        }
    }

    /// <summary>
    /// 玻璃材料高级搜索器
    /// </summary>
    public class GlassAdvancedSearch
    {
        private readonly GlassDatabase _database;

        public GlassAdvancedSearch(GlassDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>
        /// 执行高级搜索
        /// </summary>
        public List<GlassMaterial> Search(GlassSearchCriteria criteria)
        {
            if (criteria == null)
                return _database.GetAllMaterials().ToList();

            return _database.GetAllMaterials()
                .Where(m => criteria.Matches(m))
                .OrderBy(m => m.Code)
                .ToList();
        }

        /// <summary>
        /// 查找相似材料（基于光学特性）
        /// </summary>
        public List<GlassMaterial> FindSimilar(GlassMaterial reference, double ndTolerance = 0.001, double abbeTolerance = 1.0)
        {
            if (reference == null) return new List<GlassMaterial>();

            var criteria = new GlassSearchCriteria
            {
                RefractiveIndexRange = new Range<double>(
                    reference.Nd - ndTolerance,
                    reference.Nd + ndTolerance),
                AbbeNumberRange = new Range<double>(
                    reference.AbbeNumber - abbeTolerance,
                    reference.AbbeNumber + abbeTolerance)
            };

            return Search(criteria)
                .Where(m => m.Code != reference.Code) // 排除自身
                .ToList();
        }

        /// <summary>
        /// 查找替代材料
        /// </summary>
        public List<GlassMaterial> FindSubstitutes(GlassMaterial original, SubstituteSearchOptions options = null)
        {
            if (original == null) return new List<GlassMaterial>();

            options = options ?? new SubstituteSearchOptions();

            // 构建搜索条件
            var criteria = new GlassSearchCriteria
            {
                RefractiveIndexRange = new Range<double>(
                    original.Nd - options.NdTolerance,
                    original.Nd + options.NdTolerance),
                AbbeNumberRange = new Range<double>(
                    original.AbbeNumber - options.AbbeTolerance,
                    original.AbbeNumber + options.AbbeTolerance)
            };

            // 如果指定了密度范围
            if (options.ConsiderDensity)
            {
                criteria.DensityRange = new Range<double>(
                    original.Density * (1 - options.DensityTolerancePercent / 100),
                    original.Density * (1 + options.DensityTolerancePercent / 100));
            }

            // 如果限定制造商
            if (options.PreferredManufacturers != null && options.PreferredManufacturers.Count > 0)
            {
                criteria.Manufacturers = options.PreferredManufacturers;
            }

            var results = Search(criteria)
                .Where(m => m.Code != original.Code) // 排除自身
                .ToList();

            // 按匹配度排序
            return results.OrderBy(m => CalculateMatchScore(original, m, options)).ToList();
        }

        /// <summary>
        /// 计算匹配分数（越小越匹配）
        /// </summary>
        private double CalculateMatchScore(GlassMaterial original, GlassMaterial candidate, SubstituteSearchOptions options)
        {
            double score = 0;

            // 折射率差异权重最高
            score += Math.Abs(original.Nd - candidate.Nd) * options.NdWeight;

            // 阿贝数差异
            score += Math.Abs(original.AbbeNumber - candidate.AbbeNumber) * options.AbbeWeight;

            // 密度差异
            if (options.ConsiderDensity)
            {
                score += Math.Abs(original.Density - candidate.Density) * options.DensityWeight;
            }

            // 部分色散差异
            if (options.ConsiderPartialDispersion)
            {
                score += Math.Abs(original.PartialDispersion - candidate.PartialDispersion) * options.PartialDispersionWeight;
            }

            return score;
        }
    }

    /// <summary>
    /// 替代材料搜索选项
    /// </summary>
    public class SubstituteSearchOptions
    {
        public double NdTolerance { get; set; } = 0.001;
        public double AbbeTolerance { get; set; } = 1.0;
        public bool ConsiderDensity { get; set; } = true;
        public double DensityTolerancePercent { get; set; } = 10.0;
        public bool ConsiderPartialDispersion { get; set; } = true;
        public List<string> PreferredManufacturers { get; set; }
        
        // 匹配权重
        public double NdWeight { get; set; } = 100.0;
        public double AbbeWeight { get; set; } = 10.0;
        public double DensityWeight { get; set; } = 1.0;
        public double PartialDispersionWeight { get; set; } = 5.0;

        public SubstituteSearchOptions()
        {
            PreferredManufacturers = new List<string>();
        }
    }
}