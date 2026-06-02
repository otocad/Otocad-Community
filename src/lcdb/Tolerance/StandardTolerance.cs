using System;
using System.Collections.Generic;

namespace lcdb
{
    public enum FitType
    {
        ClearanceFit,      // 间隙配合
        TransitionFit,     // 过渡配合
        InterferenceFit    // 过盈配合
    }

    public enum FitSystem
    {
        HoleBasis,         // 基孔制
        ShaftBasis         // 基轴制
    }

    public class StandardTolerance
    {
        private static readonly Dictionary<string, FitType> StandardFits = new Dictionary<string, FitType>
        {
            // 间隙配合
            { "H11/c11", FitType.ClearanceFit },
            { "H9/d9", FitType.ClearanceFit },
            { "H8/f7", FitType.ClearanceFit },
            { "H7/f6", FitType.ClearanceFit },
            { "H7/g6", FitType.ClearanceFit },
            { "H7/h6", FitType.ClearanceFit },
            
            // 过渡配合
            { "H7/js6", FitType.TransitionFit },
            { "H7/k6", FitType.TransitionFit },
            { "H7/m6", FitType.TransitionFit },
            { "H7/n6", FitType.TransitionFit },
            
            // 过盈配合
            { "H7/p6", FitType.InterferenceFit },
            { "H7/r6", FitType.InterferenceFit },
            { "H7/s6", FitType.InterferenceFit }
        };

        public static List<string> GetStandardFitTypes()
        {
            return new List<string>(StandardFits.Keys);
        }

        public static FitType GetFitType(string fitDesignation)
        {
            if (StandardFits.ContainsKey(fitDesignation))
            {
                return StandardFits[fitDesignation];
            }
            return FitType.ClearanceFit;
        }

        public static void ApplyStandardFit(BasicTolerance holeTolerance, BasicTolerance shaftTolerance, 
            string fitDesignation, double nominalSize)
        {
            var parts = fitDesignation.Split('/');
            if (parts.Length != 2)
                return;

            var holeDesignation = parts[0];
            var shaftDesignation = parts[1];

            // 应用孔公差
            ApplyDesignation(holeTolerance, holeDesignation, nominalSize, true);
            
            // 应用轴公差
            ApplyDesignation(shaftTolerance, shaftDesignation, nominalSize, false);
        }

        private static void ApplyDesignation(BasicTolerance tolerance, string designation, 
            double nominalSize, bool isHole)
        {
            if (string.IsNullOrEmpty(designation) || designation.Length < 2)
                return;

            char letter = char.ToUpper(designation[0]);
            string gradeStr = designation.Substring(1);
            
            int gradeNum;
            if (!int.TryParse(gradeStr, out gradeNum))
                return;

            ToleranceGrade grade = (ToleranceGrade)gradeNum;
            
            // 获取基本公差值
            var table = new ToleranceTable();
            var standardTolerance = table.GetStandardTolerance(nominalSize, grade);
            
            if (standardTolerance == null)
                return;

            double toleranceValue = standardTolerance.ToleranceValue;
            double basicDeviation = GetBasicDeviation(letter, nominalSize, isHole);

            if (isHole)
            {
                if (letter == 'H')
                {
                    // 基孔制
                    tolerance.LowerDeviation = 0;
                    tolerance.UpperDeviation = toleranceValue;
                }
                else
                {
                    tolerance.LowerDeviation = basicDeviation;
                    tolerance.UpperDeviation = basicDeviation + toleranceValue;
                }
            }
            else
            {
                if (letter == 'H')
                {
                    // 基轴制
                    tolerance.UpperDeviation = 0;
                    tolerance.LowerDeviation = -toleranceValue;
                }
                else
                {
                    tolerance.UpperDeviation = -basicDeviation;
                    tolerance.LowerDeviation = -basicDeviation - toleranceValue;
                }
            }

            tolerance.NominalSize = nominalSize;
            tolerance.ToleranceGrade = grade;
        }

        private static double GetBasicDeviation(char letter, double nominalSize, bool isHole)
        {
            // 简化的基本偏差表（实际应用中应该使用完整表格）
            var deviationTable = new Dictionary<char, Func<double, double>>
            {
                { 'C', (size) => size <= 30 ? 0.120 : 0.140 },
                { 'D', (size) => size <= 30 ? 0.065 : 0.080 },
                { 'E', (size) => size <= 30 ? 0.040 : 0.050 },
                { 'F', (size) => size <= 30 ? 0.020 : 0.025 },
                { 'G', (size) => size <= 30 ? 0.006 : 0.008 },
                { 'H', (size) => 0.0 },
                { 'J', (size) => GetJDeviation(size) },
                { 'K', (size) => size <= 30 ? -0.002 : -0.003 },
                { 'M', (size) => size <= 30 ? -0.008 : -0.012 },
                { 'N', (size) => size <= 30 ? -0.016 : -0.020 },
                { 'P', (size) => size <= 30 ? -0.024 : -0.032 },
                { 'R', (size) => size <= 30 ? -0.034 : -0.041 },
                { 'S', (size) => size <= 30 ? -0.048 : -0.059 }
            };

            char upperLetter = char.ToUpper(letter);
            if (deviationTable.ContainsKey(upperLetter))
            {
                return deviationTable[upperLetter](nominalSize);
            }

            return 0.0;
        }

        private static double GetJDeviation(double nominalSize)
        {
            // JS的基本偏差为±IT/2
            return 0.0;
        }

        public static List<string> GetCommonTolerances()
        {
            return new List<string>
            {
                "H7", "H8", "H9", "H11",
                "h6", "h7", "h9", "h11",
                "f6", "f7", "g6", "g7",
                "js6", "js7", "k6", "m6",
                "n6", "p6", "r6", "s6"
            };
        }

        public static string GetToleranceDescription(string designation)
        {
            var descriptions = new Dictionary<string, string>
            {
                { "H7", "基孔制公差，IT7级" },
                { "H8", "基孔制公差，IT8级" },
                { "h6", "基轴制公差，IT6级" },
                { "h7", "基轴制公差，IT7级" },
                { "f6", "间隙配合轴公差，IT6级" },
                { "f7", "间隙配合轴公差，IT7级" },
                { "g6", "小间隙配合轴公差，IT6级" },
                { "js6", "过渡配合轴公差，IT6级" },
                { "k6", "过渡配合轴公差，IT6级" },
                { "m6", "过渡配合轴公差，IT6级" },
                { "n6", "过渡配合轴公差，IT6级" },
                { "p6", "过盈配合轴公差，IT6级" },
                { "r6", "过盈配合轴公差，IT6级" },
                { "s6", "过盈配合轴公差，IT6级" }
            };

            if (descriptions.ContainsKey(designation))
            {
                return descriptions[designation];
            }

            return "标准公差";
        }

        public static bool ValidateToleranceDesignation(string designation)
        {
            if (string.IsNullOrEmpty(designation) || designation.Length < 2)
                return false;

            char letter = designation[0];
            string gradeStr = designation.Substring(1);

            // 验证字母
            if (!char.IsLetter(letter))
                return false;

            // 验证数字
            int grade;
            if (!int.TryParse(gradeStr, out grade))
                return false;

            // 验证等级范围
            if (grade < 1 || grade > 18)
                return false;

            return true;
        }
    }
}