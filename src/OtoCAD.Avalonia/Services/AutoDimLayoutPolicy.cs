namespace OtoCAD.Avalonia.Services;

/// <summary>
/// 自动尺寸标注的布局策略 — 控制 R/d/φ 标注的相对位置、引线长度、kink 方向等.
/// 与 <see cref="lcdb.DimensionStyle"/> 互补:
///   - DimensionStyle 管"视觉属性" (字高/箭头/精度)
///   - AutoDimLayoutPolicy 管"放在哪里"的策略 (kinked 还是直线、text 推到哪里、d/φ 离镜片多远)
///
/// 当前阶段直接在 <see cref="DimensionStandardService"/> 提供单一 <see cref="Default"/> 实例;
/// 后续 Step 3 会聚合进 DimensionStandard, 允许 GB/ISO/Company/User 各自定义自己的布局.
/// </summary>
public class AutoDimLayoutPolicy
{
    // ── R 标注 (RadialDimension) ──────────────────────────

    /// <summary>R 标注径向引线最小长度 (mm).</summary>
    public double RLeaderArmMin { get; set; } = 8.0;

    /// <summary>R 标注径向引线长度系数 (× TextHeight). 实际 = max(Min, factor × TextHeight).</summary>
    public double RLeaderArmFactor { get; set; } = 2.5;

    /// <summary>R 文字水平外推系数 (× halfD). 决定 kink 后文字距引线终点多远.</summary>
    public double RTextKickFactor { get; set; } = 0.5;

    /// <summary>R 文字水平外推最小系数 (× TextHeight). 取 max(kickFactor×halfD, kickMin×textHeight).</summary>
    public double RTextKickMinFactor { get; set; } = 4.0;

    // ── d (中心厚) / φ (直径) 标注 ────────────────────────

    /// <summary>d 尺寸线距镜片上沿的偏移系数 (× halfD). 用于 中心厚/边厚 标注.</summary>
    public double DAboveLensFactor { get; set; } = 0.55;

    /// <summary>d 尺寸线最小偏移量 (mm). 自动标注的 d 用 halfD + 本值, 始终在边缘之上 (大镜片不落进内部).</summary>
    public double DAboveLensMin { get; set; } = 14.0;

    /// <summary>φ 尺寸线距镜片右沿的偏移系数 (× halfT).</summary>
    public double PhiSideOfLensFactor { get; set; } = 1.35;

    /// <summary>φ 尺寸线最小偏移量 (mm).</summary>
    public double PhiSideOfLensMin { get; set; } = 18.0;

    // ── 双胶合 T1/T2 错峰 ────────────────────────────────

    /// <summary>双胶合 T2 相对 T1 的额外抬高系数 (× halfD).</summary>
    public double CementedT2StackFactor { get; set; } = 0.28;

    /// <summary>双胶合 T2 抬高的最小量 (mm).</summary>
    public double CementedT2StackMin { get; set; } = 3.0;

    /// <summary>内置默认 — 与目前 GB 光学约定一致.</summary>
    public static AutoDimLayoutPolicy Default => new AutoDimLayoutPolicy();

    public AutoDimLayoutPolicy Clone() => (AutoDimLayoutPolicy)MemberwiseClone();
}
