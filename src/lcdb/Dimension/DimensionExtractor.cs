using System;
using System.Collections.Generic;
using System.Linq;

namespace OtoCAD.Dimension
{
    /// <summary>
    /// 尺寸自动提取器
    /// </summary>
    public class DimensionExtractor
    {
        /// <summary>
        /// 从光学元件提取尺寸
        /// </summary>
        public List<DimensionSpecification> ExtractDimensions(object opticalElement)
        {
            var dimensions = new List<DimensionSpecification>();

            // 这里是简化实现，实际应该分析光学元件的几何特征
            // 提取直径
            dimensions.Add(new DimensionSpecification
            {
                Name = "直径",
                Type = DimensionType.Diameter,
                NominalValue = 25.4,
                Source = DimensionSource.Auto,
                Prefix = "Ø",
                Tolerance = RecommendTolerance(DimensionType.Diameter, 25.4)
            });

            // 提取厚度
            dimensions.Add(new DimensionSpecification
            {
                Name = "中心厚度",
                Type = DimensionType.Thickness,
                NominalValue = 5.0,
                Source = DimensionSource.Auto,
                Tolerance = RecommendTolerance(DimensionType.Thickness, 5.0)
            });

            // 提取曲率半径
            dimensions.Add(new DimensionSpecification
            {
                Name = "曲率半径1",
                Type = DimensionType.Radius,
                NominalValue = 50.0,
                Source = DimensionSource.Auto,
                Prefix = "R",
                Tolerance = RecommendTolerance(DimensionType.Radius, 50.0)
            });

            return dimensions;
        }

        /// <summary>
        /// 智能推荐公差
        /// </summary>
        public ToleranceSpecification RecommendTolerance(DimensionType type, double nominalValue)
        {
            var tolerance = new ToleranceSpecification
            {
                Type = ToleranceType.Symmetric
            };

            // 基于类型和数值的简化规则
            switch (type)
            {
                case DimensionType.Diameter:
                    if (nominalValue <= 10)
                        tolerance.Value = 0.02;
                    else if (nominalValue <= 50)
                        tolerance.Value = 0.05;
                    else
                        tolerance.Value = 0.1;
                    break;

                case DimensionType.Thickness:
                    if (nominalValue <= 5)
                        tolerance.Value = 0.01;
                    else if (nominalValue <= 20)
                        tolerance.Value = 0.02;
                    else
                        tolerance.Value = 0.05;
                    break;

                case DimensionType.Radius:
                    // 半径公差通常是百分比
                    tolerance.Value = nominalValue * 0.005; // 0.5%
                    break;

                case DimensionType.Angle:
                    tolerance.Value = 0.05; // 度
                    break;

                default:
                    tolerance.Value = 0.05;
                    break;
            }

            return tolerance;
        }
    }

    /// <summary>
    /// 公差优化器
    /// </summary>
    public class ToleranceOptimizer
    {
        /// <summary>
        /// 计算公差链累积
        /// </summary>
        public ToleranceChainResult CalculateStackUp(List<DimensionSpecification> chain)
        {
            double totalNominal = 0;
            double totalToleranceSquared = 0;
            double worstCaseTolerance = 0;

            foreach (var dim in chain)
            {
                totalNominal += dim.NominalValue;
                
                if (dim.Tolerance != null && dim.Tolerance.Type == ToleranceType.Symmetric)
                {
                    // 统计公差法（RSS）
                    totalToleranceSquared += Math.Pow(dim.Tolerance.Value, 2);
                    // 最坏情况
                    worstCaseTolerance += dim.Tolerance.Value;
                }
            }

            return new ToleranceChainResult
            {
                NominalValue = totalNominal,
                StatisticalTolerance = Math.Sqrt(totalToleranceSquared),
                WorstCaseTolerance = worstCaseTolerance
            };
        }

        /// <summary>
        /// 优化公差分配
        /// </summary>
        public List<DimensionSpecification> OptimizeAllocation(double targetTolerance, List<DimensionSpecification> chain)
        {
            int n = chain.Count;
            if (n == 0) return chain;

            // 均匀分配法
            double uniformTolerance = targetTolerance / Math.Sqrt(n);

            var optimized = new List<DimensionSpecification>();
            foreach (var dim in chain)
            {
                var newDim = dim.Clone();
                if (newDim.Tolerance == null)
                {
                    newDim.Tolerance = new ToleranceSpecification();
                }
                newDim.Tolerance.Type = ToleranceType.Symmetric;
                newDim.Tolerance.Value = uniformTolerance * GetDifficultyFactor(dim);
                optimized.Add(newDim);
            }

            return optimized;
        }

        /// <summary>
        /// 获取加工难度系数
        /// </summary>
        private double GetDifficultyFactor(DimensionSpecification dimension)
        {
            // 基于尺寸类型和大小的简化难度评估
            double factor = 1.0;

            switch (dimension.Type)
            {
                case DimensionType.Diameter:
                    factor = 1.0;
                    break;
                case DimensionType.Thickness:
                    factor = 1.2;
                    break;
                case DimensionType.Radius:
                    factor = 1.5;
                    break;
                case DimensionType.Angle:
                    factor = 1.3;
                    break;
            }

            // 小尺寸加工难度更大
            if (dimension.NominalValue < 10)
            {
                factor *= 1.5;
            }

            return factor;
        }
    }

    /// <summary>
    /// 公差链计算结果
    /// </summary>
    public class ToleranceChainResult
    {
        public double NominalValue { get; set; }
        public double StatisticalTolerance { get; set; }
        public double WorstCaseTolerance { get; set; }
    }
}