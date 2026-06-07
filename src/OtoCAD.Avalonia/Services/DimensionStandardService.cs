using lcdb;

namespace OtoCAD.Avalonia.Services;

/// <summary>
/// Avalonia 端的标注标准选择服务。
/// 目前先服务自动标注，后续其他尺寸命令可复用同一入口。
/// </summary>
public static class DimensionStandardService
{
    public const string Standard = "Standard";
    public const string Iso25 = "ISO-25";
    public const string Gb = "GB";
    public const string GbOptical = "GB-Optical";
    public const string Company = "Company";

    public static string CurrentKey { get; private set; } = GbOptical;

    public static void SetCurrent(string? key)
    {
        CurrentKey = NormalizeKey(key);
    }

    public static string NormalizeKey(string? key) => key switch
    {
        Iso25 => Iso25,
        Gb => Gb,
        GbOptical => GbOptical,
        Company => Company,
        Standard => Standard,
        _ => GbOptical,
    };

    public static string GetDisplayName(string? key) => NormalizeKey(key) switch
    {
        Standard => "标准",
        Iso25 => "ISO-25",
        Gb => "GB",
        GbOptical => "GB 光学",
        Company => "公司标准",
        _ => "GB 光学",
    };

    public static DimensionStyle CreateStyle() => CreateStyle(CurrentKey);

    /// <summary>按指定标准 key 生成 DimensionStyle (供逐标注覆盖 / 实体级混标).</summary>
    public static DimensionStyle CreateStyle(string? key)
    {
        var style = NormalizeKey(key) switch
        {
            Standard => DimensionStyle.Default.Clone(),
            Iso25 => DimensionStyle.Iso25.Clone(),
            Gb => DimensionStyle.GB.Clone(),
            Company => CreateCompanyStyle(),
            _ => DimensionStyle.GBOptical.Clone(),
        };
        style.Name = NormalizeKey(key);   // 记标准 key 于 style.Name, 供属性面板回显当前标准
        return style;
    }

    /// <summary>从已有标注的 style.Name 反解标准 key (回显用); 无法识别回落文档默认.</summary>
    public static string KeyFromStyle(DimensionStyle? style) => NormalizeKey(style?.Name);

    /// <summary>全部标准 key (供属性面板下拉列举), 顺序与 Ribbon 标准▾ 一致.</summary>
    public static readonly string[] AllKeys = { Standard, Iso25, Gb, GbOptical, Company };

    /// <summary>
    /// 当前标准对应的自动标注布局策略.
    /// Step 2 阶段所有标准共用一份 <see cref="AutoDimLayoutPolicy.Default"/>;
    /// Step 3 起会按 CurrentKey 返回不同 policy (例如 ISO 用 kinked, GB-Optical 用 jogged 等).
    /// </summary>
    public static AutoDimLayoutPolicy CreateLayoutPolicy()
    {
        return AutoDimLayoutPolicy.Default;
    }

    private static DimensionStyle CreateCompanyStyle()
    {
        var style = DimensionStyle.GBOptical.Clone();
        style.Name = Company;
        style.TextHeight = 2.8;
        style.ArrowSize = 2.0;
        style.ExtensionLineExtend = 1.2;
        style.ExtensionLineOffset = 0.4;
        style.DimensionLineGap = 1.0;
        style.DecimalFormat = "F2";
        return style;
    }
}
