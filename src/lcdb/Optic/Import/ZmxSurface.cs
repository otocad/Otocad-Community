using System;
using System.Collections.Generic;

namespace lcdb.Optic.Import
{
    /// <summary>
    /// Zemax表面数据模型
    /// </summary>
    public class ZmxSurface
    {
        #region Properties

        /// <summary>
        /// 表面序号
        /// </summary>
        public int SurfaceNumber { get; set; }

        /// <summary>
        /// 表面类型 (STANDARD, EVENASPH, BINARY2, COORDBRK, etc.)
        /// </summary>
        public string Type { get; set; } = "STANDARD";

        /// <summary>
        /// 曲率半径 (mm)
        /// </summary>
        public double Radius { get; set; } = double.PositiveInfinity;

        /// <summary>
        /// 曲率 (1/mm) - 从CURV字段读取
        /// </summary>
        public double Curvature { get; set; }

        /// <summary>
        /// 厚度/到下一面距离 (mm)
        /// </summary>
        public double Thickness { get; set; }

        /// <summary>
        /// 玻璃材料代号
        /// </summary>
        public string Glass { get; set; }

        /// <summary>
        /// 净口径(光学)半径 (mm) — Zemax <c>DIAM</c> (Semi-Diameter): 球面弯曲到此半口径.
        /// </summary>
        public double SemiDiameter { get; set; }

        /// <summary>
        /// 机械半口径 (mm) — Zemax <c>MEMA</c> (Mechanical Semi-Diameter): 玻璃物理边缘.
        /// &gt; <see cref="SemiDiameter"/> 时, 净口径外到机械边是一圈平肩 (land). null = 未指定.
        /// 注: Optiland 不解析 MEMA, 故仅本地 .zmx 路有此值; 云端 Optic JSON 无.
        /// </summary>
        public double? MechanicalSemiDiameter { get; set; }

        /// <summary>
        /// 圆形通光裁剪半径 (mm) — Zemax <c>CLAP</c> / Optiland RadialAperture.r_max. 可选, 通常 ≤ DIAM.
        /// 云端 Optic JSON 里这是唯一的口径信息 (DIAM/MEMA 不进 to_dict).
        /// </summary>
        public double? ClearSemiDiameter { get; set; }

        /// <summary>
        /// 通光半口径 = DIAM 与 CLAP 取较小 (有谁取谁); 用于光学件通光直径.
        /// 本地 .zmx 主用 DIAM; 云端 (无 DIAM) 退 CLAP.
        /// </summary>
        public double NetSemiDiameter
        {
            get
            {
                double net = SemiDiameter > 1e-9 ? SemiDiameter : 0.0;
                if (ClearSemiDiameter.HasValue && ClearSemiDiameter.Value > 1e-9)
                    net = net > 1e-9 ? System.Math.Min(net, ClearSemiDiameter.Value) : ClearSemiDiameter.Value;
                return net;
            }
        }

        /// <summary>
        /// 圆锥常数 (conic constant, k)
        /// </summary>
        public double Conic { get; set; }

        /// <summary>
        /// 非球面系数 [A4, A6, A8, A10, A12, A14, A16, A18]
        /// </summary>
        public double[] AsphericCoefficients { get; set; } = new double[8];

        /// <summary>
        /// 该面之后材料的折射率 (Optic JSON IdealMaterial.index; 无玻璃牌号时承载).
        /// null = 未提供 (走 <see cref="Glass"/> 牌号查 GlassLibrary). 约 1.0 视为空气.
        /// </summary>
        public double? RefractiveIndexPost { get; set; }

        /// <summary>该面之后材料的阿贝数 (可选; 多数 IdealMaterial 无此值).</summary>
        public double? AbbeNumberPost { get; set; }

        /// <summary>
        /// 表面注释/名称
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// 是否为孔径光阑 (Stop)
        /// </summary>
        public bool IsStop { get; set; }

        /// <summary>
        /// 扩展参数
        /// </summary>
        public Dictionary<string, object> ExtraParameters { get; set; } = new Dictionary<string, object>();

        #endregion

        #region Calculated Properties

        /// <summary>
        /// 是否为平面 (R = ∞)
        /// </summary>
        public bool IsFlat => double.IsInfinity(Radius) || Math.Abs(Curvature) < 1e-10;

        /// <summary>
        /// 是否为凸面 (曲率中心在表面后方)
        /// </summary>
        public bool IsConvex => Radius > 0 && !IsFlat;

        /// <summary>
        /// 是否为凹面 (曲率中心在表面前方)
        /// </summary>
        public bool IsConcave => Radius < 0;

        /// <summary>
        /// 是否有材料 (非空气间隔)
        /// </summary>
        public bool HasMaterial =>
            (RefractiveIndexPost.HasValue && RefractiveIndexPost.Value > 1.0 + 1e-6) ||
            (!string.IsNullOrWhiteSpace(Glass) &&
             Glass.ToUpper() != "AIR" &&
             Glass != "—");

        /// <summary>
        /// 是否为非球面
        /// </summary>
        public bool IsAspheric => Type == "EVENASPH" || Type == "BINARY2" ||
                                  Math.Abs(Conic) > 1e-10 ||
                                  HasNonZeroAsphericCoefficients();

        /// <summary>
        /// 全口径 (直径)
        /// </summary>
        public double Diameter => SemiDiameter * 2;

        #endregion

        #region Methods

        /// <summary>
        /// 检查是否有非零非球面系数
        /// </summary>
        private bool HasNonZeroAsphericCoefficients()
        {
            if (AsphericCoefficients == null) return false;
            foreach (var coef in AsphericCoefficients)
            {
                if (Math.Abs(coef) > 1e-15) return true;
            }
            return false;
        }

        /// <summary>
        /// 计算给定半径处的矢高 (sag)
        /// </summary>
        /// <param name="r">径向距离 (mm)</param>
        /// <returns>矢高值 (mm)</returns>
        public double CalculateSag(double r)
        {
            if (IsFlat) return 0;

            double c = Curvature;
            double k = Conic;
            double rsq = r * r;

            // 标准圆锥面矢高公式
            double sqrtTerm = 1 - (1 + k) * c * c * rsq;
            if (sqrtTerm < 0) sqrtTerm = 0; // 避免负数开方

            double sag = c * rsq / (1 + Math.Sqrt(sqrtTerm));

            // 添加非球面高次项
            if (AsphericCoefficients != null)
            {
                for (int i = 0; i < AsphericCoefficients.Length; i++)
                {
                    // A4*r^4 + A6*r^6 + A8*r^8 + ...
                    sag += AsphericCoefficients[i] * Math.Pow(r, 2 * (i + 2));
                }
            }

            return sag;
        }

        /// <summary>
        /// 计算边缘矢高
        /// </summary>
        public double EdgeSag => CalculateSag(SemiDiameter);

        /// <summary>
        /// 克隆表面数据
        /// </summary>
        public ZmxSurface Clone()
        {
            return new ZmxSurface
            {
                SurfaceNumber = this.SurfaceNumber,
                Type = this.Type,
                Radius = this.Radius,
                Curvature = this.Curvature,
                Thickness = this.Thickness,
                Glass = this.Glass,
                SemiDiameter = this.SemiDiameter,
                Conic = this.Conic,
                AsphericCoefficients = (double[])this.AsphericCoefficients?.Clone(),
                Comment = this.Comment,
                IsStop = this.IsStop,
                ExtraParameters = new Dictionary<string, object>(this.ExtraParameters)
            };
        }

        public override string ToString()
        {
            return $"Surface {SurfaceNumber}: R={Radius:F3}, T={Thickness:F3}, Glass={Glass ?? "AIR"}, D={Diameter:F3}";
        }

        #endregion
    }
}
