using System.Collections.Generic;
using System.Linq;

namespace lcdb.Optic;

/// <summary>
/// 常用光学玻璃静态库 (Schott / CDGM 常用牌号).
/// 用户也可选 "Custom" 自填 n/V.
///
/// n_d = 587.6 nm (氦黄 d 线) 处折射率
/// V_d = (n_d - 1) / (n_F - n_C), 阿贝数 (色散)
/// </summary>
public static class GlassLibrary
{
    public record Glass(string Name, double Nd, double Vd, string Vendor = "Schott");

    public static readonly IReadOnlyList<Glass> All = new[]
    {
        // ===== Schott (主流冠/火石玻璃, ~14 种) =====
        new Glass("N-BK7",   1.5168, 64.17),   // 最常用冠玻璃
        new Glass("BK7",     1.5168, 64.17),   // 兼容旧记法
        new Glass("N-K5",    1.5224, 59.48),
        new Glass("N-KF9",   1.5230, 51.50),
        new Glass("N-BAK1",  1.5725, 57.55),
        new Glass("N-BAK4",  1.5688, 56.13),
        new Glass("N-PSK53A",1.6180, 63.39),
        new Glass("F2",      1.6200, 36.37),   // 火石玻璃 (双胶合常用)
        new Glass("F5",      1.6034, 38.03),
        new Glass("N-SF2",   1.6477, 33.85),
        new Glass("N-SF5",   1.6727, 32.25),
        new Glass("N-SF8",   1.6889, 31.18),
        new Glass("N-SF10",  1.7283, 28.53),
        new Glass("N-SF11",  1.7847, 25.76),   // 高折射高色散
        new Glass("N-SF66",  1.9229, 20.88),   // 超高折射
        new Glass("N-LAK8",  1.7130, 53.83),
        new Glass("N-LAK22", 1.6510, 55.89),   // 高折射低色散
        new Glass("N-LASF44",1.8042, 39.58),
        new Glass("N-LASF46A",1.9037,31.32),
        new Glass("N-FK51A", 1.4866, 84.47),   // 萤石替代 (ED 玻璃)

        // ===== CDGM 成都光明 (国产替代, ~7 种) =====
        new Glass("H-K9L",   1.5168, 64.17, "CDGM"),    // N-BK7 国产对应
        new Glass("H-K50",   1.5187, 60.41, "CDGM"),
        new Glass("H-ZF6",   1.7552, 27.51, "CDGM"),
        new Glass("H-ZF52",  1.8467, 23.79, "CDGM"),    // 高折火石
        new Glass("H-LAK51",1.6968, 55.46,  "CDGM"),
        new Glass("H-LAF50A",1.7725, 49.62,  "CDGM"),
        new Glass("H-QK1",   1.4760, 65.86,  "CDGM"),   // 冕牌

        // ===== Ohara (日本, ~5 种) =====
        new Glass("S-BSL7",  1.5168, 64.20, "Ohara"),
        new Glass("S-TIH53", 1.8467, 23.79, "Ohara"),
        new Glass("S-LAH64", 1.7880, 47.49, "Ohara"),
        new Glass("S-FPL51", 1.4970, 81.61, "Ohara"),
        new Glass("S-NPH1",  1.8081, 22.76, "Ohara"),

        // ===== Hoya (日本, ~5 种) =====
        new Glass("BSC7",    1.5168, 64.17, "Hoya"),
        new Glass("F8",      1.5953, 39.19, "Hoya"),
        new Glass("FF5",     1.5927, 35.45, "Hoya"),
        new Glass("LAC14",   1.6968, 55.46, "Hoya"),
        new Glass("FCD1",    1.4970, 81.61, "Hoya"),    // 萤石

        // ===== 特种 (非氧化物 / 单晶) =====
        new Glass("Fused Silica", 1.4585, 67.82, "—"),  // 熔融石英
        new Glass("Sapphire",     1.7682, 72.20, "—"),  // 蓝宝石
        new Glass("CaF2",         1.4338, 95.10, "—"),  // 氟化钙
        new Glass("ZnSe",         2.4028, 12.78, "—"),  // 红外
    };

    /// <summary>按 Name (不区分大小写) 查; 找不到返回 null.</summary>
    public static Glass? Find(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return All.FirstOrDefault(g =>
            string.Equals(g.Name, name, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>默认材料 (N-BK7).</summary>
    public static Glass Default => All[0];
}
