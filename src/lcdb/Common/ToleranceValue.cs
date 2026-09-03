using System;
using System.Globalization;

namespace lcdb.Common;

/// <summary>
/// GB/T 13323-2009 §1.1 三类值标识:
/// - <see cref="Nominal"/>   公称值 (普通几何尺寸)
/// - <see cref="Actual"/>    实际值 (带 +/- 偏差, 工厂检验依据)
/// - <see cref="Reference"/> 参考值 (括号包裹, 不作检验, 仅供加工参考)
/// </summary>
public enum ValueKind
{
    /// <summary>公称值, 显示 "60.00"</summary>
    Nominal,
    /// <summary>实际值, 显示 "60.00 +0.10/-0.05"</summary>
    Actual,
    /// <summary>参考值, 显示 "(60.00)"</summary>
    Reference,
}

/// <summary>
/// GB/T 13323-2009 §1.1 通用值 — 数值 + 公差 + 显示类型.
///
/// 设计原则:
/// - 简单不可变值对象 (POCO + setter, 便于绑定/序列化, 但语义上代表 immutable 值)
/// - 默认 Nominal (公称) — 兼容老代码 "纯数值"
/// - <see cref="ToString"/> 按 <see cref="Kind"/> 切换三种显示, 直接喂给
///   <see cref="DimensionBase.userText"/> 即可正确显示
/// </summary>
public sealed class ToleranceValue
{
    /// <summary>名义值 (mm 或其它原始单位).</summary>
    public double Value { get; set; }

    /// <summary>上偏差 (+方向), 仅 <see cref="ValueKind.Actual"/> 时有意义. 默认 0.</summary>
    public double Plus { get; set; }

    /// <summary>下偏差 (绝对值; 实际渲染为 -). 默认 0.</summary>
    public double Minus { get; set; }

    /// <summary>显示类型.</summary>
    public ValueKind Kind { get; set; } = ValueKind.Nominal;

    /// <summary>"平面" 标记 (R 用 — sag/曲率为零, 不画半径). 优先于其它格式化逻辑.</summary>
    public bool IsPlanar { get; set; }

    /// <summary>无参构造 — System.Text.Json 反序列化 + UI 数据绑定需要.</summary>
    public ToleranceValue() { }

    public ToleranceValue(double value, ValueKind kind = ValueKind.Nominal)
    {
        Value = value;
        Kind = kind;
    }

    public ToleranceValue(double value, double plus, double minus)
    {
        Value = value;
        Plus = plus;
        Minus = Math.Abs(minus);
        Kind = ValueKind.Actual;
    }

    public override string ToString()
    {
        if (IsPlanar) return "R∞";  // R∞
        switch (Kind)
        {
            case ValueKind.Reference:
                return $"({Value.ToString("F2", CultureInfo.InvariantCulture)})";
            case ValueKind.Actual when Plus != 0 || Minus != 0:
                // 对称 (±) 显示
                if (Math.Abs(Plus - Minus) < 1e-9)
                {
                    return $"{Value.ToString("F2", CultureInfo.InvariantCulture)}±{Plus.ToString("F2", CultureInfo.InvariantCulture)}";
                }
                return $"{Value.ToString("F2", CultureInfo.InvariantCulture)} +{Plus.ToString("F2", CultureInfo.InvariantCulture)}/-{Minus.ToString("F2", CultureInfo.InvariantCulture)}";
            case ValueKind.Actual:  // Actual but zero deviations — 等同 Nominal 显示
            case ValueKind.Nominal:
            default:
                return Value.ToString("F2", CultureInfo.InvariantCulture);
        }
    }

    public ToleranceValue Clone() => new()
    {
        Value = Value,
        Plus = Plus,
        Minus = Minus,
        Kind = Kind,
        IsPlanar = IsPlanar,
    };
}
