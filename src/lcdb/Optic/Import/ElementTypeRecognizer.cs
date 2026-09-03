using System;
using System.Collections.Generic;

namespace lcdb.Optic.Import
{
    /// <summary>
    /// 识别的光学元件类型
    /// </summary>
    public enum RecognizedElementType
    {
        /// <summary>未知类型</summary>
        Unknown = 0,

        // ========== 透镜类型 ==========

        /// <summary>双凸透镜 (R1 > 0, R2 < 0)</summary>
        BiconvexLens,

        /// <summary>双凹透镜 (R1 < 0, R2 > 0)</summary>
        BiconcaveLens,

        /// <summary>平凸透镜 (一面平, 一面凸)</summary>
        PlanoConvexLens,

        /// <summary>平凹透镜 (一面平, 一面凹)</summary>
        PlanoConcaveLens,

        /// <summary>正弯月透镜 (R1 > 0, R2 > 0, |R1| < |R2|)</summary>
        PositiveMeniscusLens,

        /// <summary>负弯月透镜 (R1 > 0, R2 > 0, |R1| > |R2|)</summary>
        NegativeMeniscusLens,

        // ========== 棱镜类型 ==========

        /// <summary>直角棱镜</summary>
        RightAnglePrism,

        /// <summary>等边棱镜</summary>
        EquilateralPrism,

        /// <summary>道威棱镜</summary>
        DovePrism,

        /// <summary>五角棱镜</summary>
        PentaPrism,

        /// <summary>楔形棱镜</summary>
        WedgePrism,

        // ========== 反射镜类型 ==========

        /// <summary>平面反射镜</summary>
        FlatMirror,

        /// <summary>球面凹反射镜</summary>
        SphericalConcaveMirror,

        /// <summary>球面凸反射镜</summary>
        SphericalConvexMirror,

        /// <summary>抛物面反射镜</summary>
        ParabolicMirror,

        // ========== 特殊元件 ==========

        /// <summary>平行平板/窗片</summary>
        ParallelPlate,

        /// <summary>滤光片</summary>
        Filter,

        /// <summary>分束镜</summary>
        BeamSplitter,

        /// <summary>非球面透镜</summary>
        AsphericLens
    }

    /// <summary>
    /// 元件识别结果
    /// </summary>
    public class ElementRecognitionResult
    {
        /// <summary>识别的元件类型</summary>
        public RecognizedElementType Type { get; set; }

        /// <summary>置信度 (0-1)</summary>
        public double Confidence { get; set; }

        /// <summary>类型描述（中文）</summary>
        public string TypeDescription { get; set; }

        /// <summary>类型描述（英文）</summary>
        public string TypeDescriptionEn { get; set; }

        /// <summary>提取的参数</summary>
        public Dictionary<string, object> ExtractedParameters { get; set; } = new Dictionary<string, object>();

        /// <summary>识别说明</summary>
        public string Notes { get; set; }
    }

    /// <summary>
    /// 光学元件类型识别器
    /// </summary>
    public class ElementTypeRecognizer
    {
        #region Constants

        private const double InfinityThreshold = 1e10;
        private const double ZeroThreshold = 1e-10;

        #endregion

        #region Public Methods

        /// <summary>
        /// 识别ZmxElement的类型
        /// </summary>
        public ElementRecognitionResult Recognize(ZmxElement element)
        {
            if (element == null)
            {
                return new ElementRecognitionResult
                {
                    Type = RecognizedElementType.Unknown,
                    Confidence = 0,
                    TypeDescription = "无效元件",
                    Notes = "元件数据为空"
                };
            }

            return Recognize(
                element.R1,
                element.R2,
                element.CenterThickness,
                element.Diameter,
                element.IsAspheric,
                element.GlassMaterial
            );
        }

        /// <summary>
        /// 根据表面参数识别元件类型
        /// </summary>
        /// <param name="R1">前表面曲率半径</param>
        /// <param name="R2">后表面曲率半径</param>
        /// <param name="thickness">中心厚度</param>
        /// <param name="diameter">直径</param>
        /// <param name="isAspheric">是否为非球面</param>
        /// <param name="material">材料</param>
        public ElementRecognitionResult Recognize(
            double R1, double R2,
            double thickness, double diameter,
            bool isAspheric = false,
            string material = null)
        {
            var result = new ElementRecognitionResult
            {
                ExtractedParameters =
                {
                    ["R1"] = R1,
                    ["R2"] = R2,
                    ["Thickness"] = thickness,
                    ["Diameter"] = diameter,
                    ["Material"] = material ?? "Unknown"
                }
            };

            // 检查是否为非球面
            if (isAspheric)
            {
                result.Type = RecognizedElementType.AsphericLens;
                result.Confidence = 0.95;
                result.TypeDescription = "非球面透镜";
                result.TypeDescriptionEn = "Aspheric Lens";
                result.Notes = "检测到非球面系数，识别为非球面透镜";
                return result;
            }

            // 分析曲率半径
            bool R1Flat = IsFlat(R1);
            bool R2Flat = IsFlat(R2);
            bool R1Positive = R1 > 0 && !R1Flat;
            bool R1Negative = R1 < 0 && !R1Flat;
            bool R2Positive = R2 > 0 && !R2Flat;
            bool R2Negative = R2 < 0 && !R2Flat;

            // 两面都是平面 -> 平行平板
            if (R1Flat && R2Flat)
            {
                result.Type = RecognizedElementType.ParallelPlate;
                result.Confidence = 0.98;
                result.TypeDescription = "平行平板";
                result.TypeDescriptionEn = "Parallel Plate";
                result.Notes = "两面均为平面";
                return result;
            }

            // 双凸透镜: R1 > 0, R2 < 0
            if (R1Positive && R2Negative)
            {
                result.Type = RecognizedElementType.BiconvexLens;
                result.Confidence = 0.95;
                result.TypeDescription = "双凸透镜";
                result.TypeDescriptionEn = "Biconvex Lens";
                result.Notes = $"R1={R1:F2}>0, R2={R2:F2}<0";
                return result;
            }

            // 双凹透镜: R1 < 0, R2 > 0
            if (R1Negative && R2Positive)
            {
                result.Type = RecognizedElementType.BiconcaveLens;
                result.Confidence = 0.95;
                result.TypeDescription = "双凹透镜";
                result.TypeDescriptionEn = "Biconcave Lens";
                result.Notes = $"R1={R1:F2}<0, R2={R2:F2}>0";
                return result;
            }

            // 平凸透镜
            if ((R1Flat && R2Negative) || (R1Positive && R2Flat))
            {
                result.Type = RecognizedElementType.PlanoConvexLens;
                result.Confidence = 0.95;
                result.TypeDescription = "平凸透镜";
                result.TypeDescriptionEn = "Plano-Convex Lens";
                result.Notes = R1Flat ? "前平后凸" : "前凸后平";
                return result;
            }

            // 平凹透镜
            if ((R1Flat && R2Positive) || (R1Negative && R2Flat))
            {
                result.Type = RecognizedElementType.PlanoConcaveLens;
                result.Confidence = 0.95;
                result.TypeDescription = "平凹透镜";
                result.TypeDescriptionEn = "Plano-Concave Lens";
                result.Notes = R1Flat ? "前平后凹" : "前凹后平";
                return result;
            }

            // 弯月透镜: R1和R2同号
            if ((R1Positive && R2Positive) || (R1Negative && R2Negative))
            {
                double absR1 = Math.Abs(R1);
                double absR2 = Math.Abs(R2);

                if (absR1 < absR2)
                {
                    result.Type = RecognizedElementType.PositiveMeniscusLens;
                    result.Confidence = 0.90;
                    result.TypeDescription = "正弯月透镜";
                    result.TypeDescriptionEn = "Positive Meniscus Lens";
                    result.Notes = $"|R1|={absR1:F2} < |R2|={absR2:F2}";
                }
                else
                {
                    result.Type = RecognizedElementType.NegativeMeniscusLens;
                    result.Confidence = 0.90;
                    result.TypeDescription = "负弯月透镜";
                    result.TypeDescriptionEn = "Negative Meniscus Lens";
                    result.Notes = $"|R1|={absR1:F2} >= |R2|={absR2:F2}";
                }
                return result;
            }

            // 未能识别
            result.Type = RecognizedElementType.Unknown;
            result.Confidence = 0.5;
            result.TypeDescription = "未知类型";
            result.TypeDescriptionEn = "Unknown";
            result.Notes = $"无法根据R1={R1:F2}, R2={R2:F2}确定类型";
            return result;
        }

        /// <summary>
        /// 识别反射镜类型
        /// </summary>
        public ElementRecognitionResult RecognizeMirror(double radius, double conic)
        {
            var result = new ElementRecognitionResult
            {
                ExtractedParameters =
                {
                    ["Radius"] = radius,
                    ["Conic"] = conic
                }
            };

            // 平面镜
            if (IsFlat(radius))
            {
                result.Type = RecognizedElementType.FlatMirror;
                result.Confidence = 0.98;
                result.TypeDescription = "平面反射镜";
                result.TypeDescriptionEn = "Flat Mirror";
                return result;
            }

            // 抛物面 (conic = -1)
            if (Math.Abs(conic + 1) < 0.01)
            {
                result.Type = RecognizedElementType.ParabolicMirror;
                result.Confidence = 0.95;
                result.TypeDescription = "抛物面反射镜";
                result.TypeDescriptionEn = "Parabolic Mirror";
                result.Notes = $"Conic={conic:F4} ≈ -1";
                return result;
            }

            // 球面反射镜
            if (radius > 0)
            {
                result.Type = RecognizedElementType.SphericalConvexMirror;
                result.TypeDescription = "球面凸反射镜";
                result.TypeDescriptionEn = "Spherical Convex Mirror";
            }
            else
            {
                result.Type = RecognizedElementType.SphericalConcaveMirror;
                result.TypeDescription = "球面凹反射镜";
                result.TypeDescriptionEn = "Spherical Concave Mirror";
            }
            result.Confidence = 0.90;
            return result;
        }

        /// <summary>
        /// 获取元件类型的中文描述
        /// </summary>
        public static string GetTypeDescription(RecognizedElementType type)
        {
            switch (type)
            {
                case RecognizedElementType.BiconvexLens: return "双凸透镜";
                case RecognizedElementType.BiconcaveLens: return "双凹透镜";
                case RecognizedElementType.PlanoConvexLens: return "平凸透镜";
                case RecognizedElementType.PlanoConcaveLens: return "平凹透镜";
                case RecognizedElementType.PositiveMeniscusLens: return "正弯月透镜";
                case RecognizedElementType.NegativeMeniscusLens: return "负弯月透镜";
                case RecognizedElementType.RightAnglePrism: return "直角棱镜";
                case RecognizedElementType.EquilateralPrism: return "等边棱镜";
                case RecognizedElementType.DovePrism: return "道威棱镜";
                case RecognizedElementType.PentaPrism: return "五角棱镜";
                case RecognizedElementType.WedgePrism: return "楔形棱镜";
                case RecognizedElementType.FlatMirror: return "平面反射镜";
                case RecognizedElementType.SphericalConcaveMirror: return "球面凹反射镜";
                case RecognizedElementType.SphericalConvexMirror: return "球面凸反射镜";
                case RecognizedElementType.ParabolicMirror: return "抛物面反射镜";
                case RecognizedElementType.ParallelPlate: return "平行平板";
                case RecognizedElementType.Filter: return "滤光片";
                case RecognizedElementType.BeamSplitter: return "分束镜";
                case RecognizedElementType.AsphericLens: return "非球面透镜";
                default: return "未知类型";
            }
        }

        /// <summary>
        /// 获取元件类型的英文描述
        /// </summary>
        public static string GetTypeDescriptionEn(RecognizedElementType type)
        {
            switch (type)
            {
                case RecognizedElementType.BiconvexLens: return "Biconvex Lens";
                case RecognizedElementType.BiconcaveLens: return "Biconcave Lens";
                case RecognizedElementType.PlanoConvexLens: return "Plano-Convex Lens";
                case RecognizedElementType.PlanoConcaveLens: return "Plano-Concave Lens";
                case RecognizedElementType.PositiveMeniscusLens: return "Positive Meniscus Lens";
                case RecognizedElementType.NegativeMeniscusLens: return "Negative Meniscus Lens";
                case RecognizedElementType.RightAnglePrism: return "Right Angle Prism";
                case RecognizedElementType.EquilateralPrism: return "Equilateral Prism";
                case RecognizedElementType.DovePrism: return "Dove Prism";
                case RecognizedElementType.PentaPrism: return "Penta Prism";
                case RecognizedElementType.WedgePrism: return "Wedge Prism";
                case RecognizedElementType.FlatMirror: return "Flat Mirror";
                case RecognizedElementType.SphericalConcaveMirror: return "Spherical Concave Mirror";
                case RecognizedElementType.SphericalConvexMirror: return "Spherical Convex Mirror";
                case RecognizedElementType.ParabolicMirror: return "Parabolic Mirror";
                case RecognizedElementType.ParallelPlate: return "Parallel Plate";
                case RecognizedElementType.Filter: return "Filter";
                case RecognizedElementType.BeamSplitter: return "Beam Splitter";
                case RecognizedElementType.AsphericLens: return "Aspheric Lens";
                default: return "Unknown";
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 判断曲率半径是否为平面 (无穷大或极大值)
        /// </summary>
        private bool IsFlat(double radius)
        {
            return double.IsInfinity(radius) ||
                   double.IsNaN(radius) ||
                   Math.Abs(radius) > InfinityThreshold;
        }

        #endregion
    }
}
