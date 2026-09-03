using System;
using System.Collections.Generic;
using System.Globalization;
using LitMath;
using lcdb;          // Entity
using lcdb.Optic;

namespace lcdb.Optic.Import
{
    /// <summary>
    /// 中性处方 (<see cref="ZmxSystemData"/>) → OtoCAD 光学实体 (<see cref="OpticalLens"/> / <see cref="CementedLens"/>).
    ///
    /// 供两条导入路共用 (ZmxParser 解析的 .zmx, OpticJsonDeserializer 解析的 Optic JSON), 故置于 lcdb.
    ///
    /// 分组规则 (材料惯例: 面的 material_post = 该面之后的材料):
    /// - 连续"之后是玻璃"的面构成一个元件; 第一个"之后是空气"的面是该元件后表面.
    /// - 1 片玻璃 → OpticalLens; 2 片(共享胶合面) → CementedLens; ≥3 片 → 暂不支持(告警跳过, fail loud).
    ///
    /// 符号/几何与 lcdb.Optic 实体一致 (ISO 10110-1: R&gt;0 中心在右). 沿光轴 (X) 按累计厚度定位.
    /// </summary>
    public sealed class PrescriptionToLensBuilder
    {
        public sealed class Options
        {
            /// <summary>无通光半径信息时的兜底直径 (mm). 见开发计划 §8.3 缺口 2.</summary>
            public double DefaultDiameter { get; set; } = 25.4;

            /// <summary>放置原点 (第一个面顶点的模型坐标). 沿 +X 排布.</summary>
            public Vector2 Origin { get; set; } = Vector2.Zero;

            /// <summary>来源标签 (写入实体 Extensions.Tags["source"], 如 "optic.chat" 或 .zmx 路径).</summary>
            public string SourceLabel { get; set; }
        }

        public sealed class Result
        {
            public List<Entity> Entities { get; } = new List<Entity>();
            public List<string> Warnings { get; } = new List<string>();
            public int SingleCount { get; set; }
            public int CementedCount { get; set; }
            public int SkippedCount { get; set; }
        }

        public Result Build(ZmxSystemData data, Options options = null)
        {
            options = options ?? new Options();
            var result = new Result();

            if (data == null || data.Surfaces == null || data.Surfaces.Count < 3)
            {
                result.Warnings.Add("表面不足, 无法构建元件 (至少 OBJ + 1 元件 + IMG)");
                return result;
            }

            var surfaces = data.Surfaces;
            int n = surfaces.Count;

            // 累计轴向位置: z[k] = 第 k 面顶点沿光轴的绝对位置 (z[0]=0, 逐面累加 thickness)
            var z = new double[n];
            for (int k = 1; k < n; k++)
            {
                double t = surfaces[k - 1].Thickness;
                if (double.IsInfinity(t) || double.IsNaN(t)) t = 0; // 物面 ∞ 间隔不参与排布
                z[k] = z[k - 1] + t;
            }

            bool diameterFallbackWarned = false;

            // 扫描 [1 .. n-2]: 跳过 OBJ(0) 与 IMG(n-1)
            int i = 1;
            while (i < n - 1)
            {
                if (!surfaces[i].HasMaterial) { i++; continue; }

                // 找后表面 j: 第一个"之后是空气"的面
                int j = i + 1;
                while (j < n && surfaces[j].HasMaterial) j++;

                if (j >= n)
                {
                    result.Warnings.Add($"面 {i} 起的玻璃组未找到收尾空气面, 跳过");
                    break;
                }

                int pieces = j - i; // 玻璃片数

                if (pieces == 1)
                {
                    var lens = BuildSingle(surfaces[i], surfaces[j], z[i], options, result, ref diameterFallbackWarned);
                    SetProvenance(lens.Extensions = lens.Extensions ?? new OpticalExtensions(), options, i);
                    result.Entities.Add(lens);
                    result.SingleCount++;
                }
                else if (pieces == 2)
                {
                    var lens = BuildCemented(surfaces[i], surfaces[i + 1], surfaces[j], z[i], options, result, ref diameterFallbackWarned);
                    SetProvenance(lens.Extensions = lens.Extensions ?? new OpticalExtensions(), options, i);
                    result.Entities.Add(lens);
                    result.CementedCount++;
                }
                else
                {
                    result.Warnings.Add($"面 {i}~{j}: {pieces} 片胶合组暂不支持 (仅单片/双胶合), 已跳过");
                    result.SkippedCount++;
                }

                i = j + 1;
            }

            if (result.Entities.Count == 0)
                result.Warnings.Add("未构建任何光学元件 (无折射玻璃组?)");

            return result;
        }

        private OpticalLens BuildSingle(
            ZmxSurface front, ZmxSurface back, double frontZ,
            Options options, Result result, ref bool diamWarned)
        {
            double thickness = SafeThickness(front.Thickness);
            double diameter = ResolveDiameter(options, result, ref diamWarned, front, back);

            var lens = new OpticalLens
            {
                FrontSurface = ToSurface(front),
                BackSurface = ToSurface(back),
                Thickness = thickness,
                Diameter = diameter,
                Position = new Vector2(options.Origin.X + frontZ + thickness * 0.5, options.Origin.Y),
            };
            ApplyMaterial(front, name => lens.ApplyMaterial(name),
                (nd, vd) => { lens.MaterialName = FormatIndexName(nd); lens.RefractiveIndex = nd; if (vd.HasValue) lens.AbbeNumber = vd.Value; });
            lens.MechanicalDiameter = MechanicalDiameterOf(diameter, front, back);
            return lens;
        }

        private CementedLens BuildCemented(
            ZmxSurface front, ZmxSurface contact, ZmxSurface back, double frontZ,
            Options options, Result result, ref bool diamWarned)
        {
            double t1 = SafeThickness(front.Thickness);
            double t2 = SafeThickness(contact.Thickness);
            double diameter = ResolveDiameter(options, result, ref diamWarned, front, contact, back);

            var lens = new CementedLens
            {
                FrontSurface = ToSurface(front),
                ContactSurface = ToSurface(contact),
                BackSurface = ToSurface(back),
                T1 = t1,
                T2 = t2,
                Diameter = diameter,
                Position = new Vector2(options.Origin.X + frontZ + (t1 + t2) * 0.5, options.Origin.Y),
            };
            ApplyMaterial(front, name => lens.ApplyMaterial1(name),
                (nd, vd) => { lens.Material1 = FormatIndexName(nd); lens.Nd1 = nd; if (vd.HasValue) lens.Vd1 = vd.Value; });
            ApplyMaterial(contact, name => lens.ApplyMaterial2(name),
                (nd, vd) => { lens.Material2 = FormatIndexName(nd); lens.Nd2 = nd; if (vd.HasValue) lens.Vd2 = vd.Value; });
            lens.MechanicalDiameter = MechanicalDiameterOf(diameter, front, contact, back);
            return lens;
        }

        /// <summary>机械外径 = 各面 MEMA(机械半口径) 最大 ×2; 仅当 &gt; 净口径时返回(有平肩 land), 否则 null.</summary>
        private static double? MechanicalDiameterOf(double netDiameter, params ZmxSurface[] surfs)
        {
            double mechSemi = 0;
            foreach (var s in surfs)
                if (s.MechanicalSemiDiameter.HasValue && s.MechanicalSemiDiameter.Value > mechSemi)
                    mechSemi = s.MechanicalSemiDiameter.Value;
            double mech = mechSemi * 2.0;
            return mech > netDiameter + 1e-6 ? mech : (double?)null;
        }

        /// <summary>把玻璃面(其 material_post = 该元件玻璃)的材料应用到实体: 优先牌号, 退而求折射率.</summary>
        private static void ApplyMaterial(ZmxSurface glassSurface, Action<string> applyByName, Action<double, double?> applyByIndex)
        {
            if (!string.IsNullOrWhiteSpace(glassSurface.Glass))
                applyByName(glassSurface.Glass);
            else if (glassSurface.RefractiveIndexPost.HasValue)
                applyByIndex(glassSurface.RefractiveIndexPost.Value, glassSurface.AbbeNumberPost);
            // 两者皆无: 保持实体默认材料 (理论上 HasMaterial 已保证至少其一)
        }

        private static string FormatIndexName(double nd)
            => "n=" + nd.ToString("F4", CultureInfo.InvariantCulture);

        private static double SafeThickness(double t)
            => (double.IsInfinity(t) || double.IsNaN(t) || t <= 0) ? 0.0 : t;

        /// <summary>取元件各面最大通光直径; 无有效口径时用兜底直径 + 一次性告警.</summary>
        private static double ResolveDiameter(Options options, Result result, ref bool warned, params ZmxSurface[] surfs)
        {
            // 通光直径 = 各面净口径(优先 clear aperture, 退机械半口径)的最大值 ×2, 使镜片轮廓包住所有面.
            double semi = 0;
            foreach (var s in surfs)
                if (s.NetSemiDiameter > semi) semi = s.NetSemiDiameter;

            if (semi > 1e-9) return semi * 2.0;

            if (!warned)
            {
                result.Warnings.Add($"处方缺通光半径 (静态 to_dict 常无 semi_aperture), 用兜底直径 {options.DefaultDiameter}mm; 精确口径需 P3 端点追迹注入");
                warned = true;
            }
            return options.DefaultDiameter;
        }

        /// <summary>ZmxSurface → OpticalSurface: ∞→平面; 有圆锥/高次项→非球面; 否则球面. 带逐面净口径 (DIAM).</summary>
        private static OpticalSurface ToSurface(ZmxSurface s)
        {
            OpticalSurface surf;
            if (double.IsInfinity(s.Radius) || double.IsNaN(s.Radius))
            {
                surf = OpticalSurface.Flat();
            }
            else
            {
                double[] coeffs = TrimTrailingZeros(s.AsphericCoefficients);
                bool aspheric = Math.Abs(s.Conic) > 1e-12 || coeffs.Length > 0;
                surf = aspheric
                    ? new AsphericSurface { Radius = s.Radius, ConicConstant = s.Conic, EvenCoefficients = coeffs }
                    : OpticalSurface.Sphere(s.Radius);
            }
            // 逐面净口径 (DIAM, 与 CLAP 取小): 该面只弯曲到此, 外侧平肩到镜片机械外径.
            if (s.NetSemiDiameter > 1e-9) surf.SemiAperture = s.NetSemiDiameter;
            return surf;
        }

        private static double[] TrimTrailingZeros(double[] src)
        {
            if (src == null) return Array.Empty<double>();
            int last = -1;
            for (int i = 0; i < src.Length; i++)
                if (Math.Abs(src[i]) > 1e-20) last = i;
            if (last < 0) return Array.Empty<double>();
            var dst = new double[last + 1];
            Array.Copy(src, dst, last + 1);
            return dst;
        }

        private static void SetProvenance(OpticalExtensions ext, Options options, int frontSurfaceIndex)
        {
            if (!string.IsNullOrEmpty(options.SourceLabel))
                ext.Tags["source"] = options.SourceLabel;
            ext.Tags["surfaceIndex"] = frontSurfaceIndex.ToString(CultureInfo.InvariantCulture);
        }
    }
}
