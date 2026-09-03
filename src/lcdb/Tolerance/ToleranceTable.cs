using System;
using System.Collections.Generic;
using System.Linq;

namespace lcdb
{
    public class ToleranceTable
    {
        private static readonly Dictionary<ToleranceGrade, Dictionary<double, double>> StandardTolerances = 
            new Dictionary<ToleranceGrade, Dictionary<double, double>>();

        static ToleranceTable()
        {
            InitializeToleranceTables();
        }

        private static void InitializeToleranceTables()
        {
            // IT6 公差值 (单位：mm)
            StandardTolerances[ToleranceGrade.IT6] = new Dictionary<double, double>
            {
                { 3, 0.006 },
                { 6, 0.008 },
                { 10, 0.009 },
                { 18, 0.011 },
                { 30, 0.013 },
                { 50, 0.016 },
                { 80, 0.019 },
                { 120, 0.022 },
                { 180, 0.025 },
                { 250, 0.029 },
                { 315, 0.032 },
                { 400, 0.036 },
                { 500, 0.040 }
            };

            // IT7 公差值
            StandardTolerances[ToleranceGrade.IT7] = new Dictionary<double, double>
            {
                { 3, 0.010 },
                { 6, 0.012 },
                { 10, 0.015 },
                { 18, 0.018 },
                { 30, 0.021 },
                { 50, 0.025 },
                { 80, 0.030 },
                { 120, 0.035 },
                { 180, 0.040 },
                { 250, 0.046 },
                { 315, 0.052 },
                { 400, 0.057 },
                { 500, 0.063 }
            };

            // IT8 公差值
            StandardTolerances[ToleranceGrade.IT8] = new Dictionary<double, double>
            {
                { 3, 0.014 },
                { 6, 0.018 },
                { 10, 0.022 },
                { 18, 0.027 },
                { 30, 0.033 },
                { 50, 0.039 },
                { 80, 0.046 },
                { 120, 0.054 },
                { 180, 0.063 },
                { 250, 0.072 },
                { 315, 0.081 },
                { 400, 0.089 },
                { 500, 0.097 }
            };

            // IT9 公差值
            StandardTolerances[ToleranceGrade.IT9] = new Dictionary<double, double>
            {
                { 3, 0.025 },
                { 6, 0.030 },
                { 10, 0.036 },
                { 18, 0.043 },
                { 30, 0.052 },
                { 50, 0.062 },
                { 80, 0.074 },
                { 120, 0.087 },
                { 180, 0.100 },
                { 250, 0.115 },
                { 315, 0.130 },
                { 400, 0.140 },
                { 500, 0.155 }
            };

            // IT10 公差值
            StandardTolerances[ToleranceGrade.IT10] = new Dictionary<double, double>
            {
                { 3, 0.040 },
                { 6, 0.048 },
                { 10, 0.058 },
                { 18, 0.070 },
                { 30, 0.084 },
                { 50, 0.100 },
                { 80, 0.120 },
                { 120, 0.140 },
                { 180, 0.160 },
                { 250, 0.185 },
                { 315, 0.210 },
                { 400, 0.230 },
                { 500, 0.250 }
            };

            // IT11 公差值
            StandardTolerances[ToleranceGrade.IT11] = new Dictionary<double, double>
            {
                { 3, 0.060 },
                { 6, 0.075 },
                { 10, 0.090 },
                { 18, 0.110 },
                { 30, 0.130 },
                { 50, 0.160 },
                { 80, 0.190 },
                { 120, 0.220 },
                { 180, 0.250 },
                { 250, 0.290 },
                { 315, 0.320 },
                { 400, 0.360 },
                { 500, 0.400 }
            };
        }

        public BasicTolerance GetStandardTolerance(double nominalSize, ToleranceGrade grade)
        {
            if (!StandardTolerances.ContainsKey(grade))
                return null;

            var table = StandardTolerances[grade];
            double toleranceValue = GetToleranceValue(nominalSize, table);

            if (toleranceValue > 0)
            {
                return new BasicTolerance(nominalSize, toleranceValue);
            }

            return null;
        }

        private double GetToleranceValue(double nominalSize, Dictionary<double, double> table)
        {
            // 找到适用的尺寸范围
            var sortedSizes = table.Keys.OrderBy(k => k).ToList();
            
            for (int i = 0; i < sortedSizes.Count; i++)
            {
                if (nominalSize <= sortedSizes[i])
                {
                    return table[sortedSizes[i]];
                }
            }

            // 如果超出最大范围，返回最大范围的公差值
            return table[sortedSizes.Last()];
        }

        public List<ToleranceGrade> GetAvailableGrades()
        {
            return StandardTolerances.Keys.OrderBy(g => (int)g).ToList();
        }

        public double GetMinNominalSize()
        {
            return 0.0;
        }

        public double GetMaxNominalSize()
        {
            return 500.0;
        }

        public BasicTolerance GetFitTolerance(double nominalSize, string fitType)
        {
            // 常用配合公差
            // H7/h6, H7/g6, H7/f7 等
            switch (fitType.ToUpper())
            {
                case "H7":  // 基孔制
                    var tolerance = GetStandardTolerance(nominalSize, ToleranceGrade.IT7);
                    if (tolerance != null)
                    {
                        tolerance.LowerDeviation = 0;
                        tolerance.UpperDeviation = tolerance.ToleranceValue;
                    }
                    return tolerance;

                case "H6":
                    tolerance = GetStandardTolerance(nominalSize, ToleranceGrade.IT6);
                    if (tolerance != null)
                    {
                        tolerance.LowerDeviation = 0;
                        tolerance.UpperDeviation = tolerance.ToleranceValue;
                    }
                    return tolerance;

                case "G6":  // 间隙配合
                    tolerance = GetStandardTolerance(nominalSize, ToleranceGrade.IT6);
                    if (tolerance != null)
                    {
                        // G6 的基本偏差根据尺寸范围确定
                        double basicDeviation = GetBasicDeviation(nominalSize, 'G');
                        tolerance.LowerDeviation = -basicDeviation - tolerance.ToleranceValue;
                        tolerance.UpperDeviation = -basicDeviation;
                    }
                    return tolerance;

                case "F7":  // 间隙配合
                    tolerance = GetStandardTolerance(nominalSize, ToleranceGrade.IT7);
                    if (tolerance != null)
                    {
                        double basicDeviation = GetBasicDeviation(nominalSize, 'F');
                        tolerance.LowerDeviation = -basicDeviation - tolerance.ToleranceValue;
                        tolerance.UpperDeviation = -basicDeviation;
                    }
                    return tolerance;

                default:
                    return null;
            }
        }

        private double GetBasicDeviation(double nominalSize, char deviationLetter)
        {
            // 简化的基本偏差计算
            // 实际应用中应该使用完整的基本偏差表
            switch (deviationLetter)
            {
                case 'F':
                    if (nominalSize <= 10) return 0.006;
                    if (nominalSize <= 18) return 0.010;
                    if (nominalSize <= 30) return 0.013;
                    if (nominalSize <= 50) return 0.016;
                    return 0.020;

                case 'G':
                    if (nominalSize <= 10) return 0.004;
                    if (nominalSize <= 18) return 0.006;
                    if (nominalSize <= 30) return 0.007;
                    if (nominalSize <= 50) return 0.009;
                    return 0.010;

                case 'H':
                    return 0.0;  // 基孔制，基本偏差为0

                default:
                    return 0.0;
            }
        }
    }
}