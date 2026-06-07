using System;
using System.ComponentModel;

namespace lcdb.Chamfer
{
    /// <summary>
    /// 倒角类型枚举
    /// </summary>
    public enum ChamferType
    {
        /// <summary>
        /// C型倒角（平面倒角）
        /// </summary>
        [Description("C型倒角")]
        CType,

        /// <summary>
        /// R型倒角（圆弧倒角）
        /// </summary>
        [Description("R型倒角")]
        RType,

        /// <summary>
        /// 特殊倒角
        /// </summary>
        [Description("特殊倒角")]
        Special,

        /// <summary>
        /// 双重倒角
        /// </summary>
        [Description("双重倒角")]
        Double,

        /// <summary>
        /// 微倒角
        /// </summary>
        [Description("微倒角")]
        Micro,

        /// <summary>
        /// 尖棱 - GB/T 13323 4.3.7 功能性尖棱，标记"0"
        /// 表示该边缘不允许倒角
        /// </summary>
        [Description("尖棱(0)")]
        SharpEdge,

        /// <summary>
        /// 斜面 - GB/T 13323 4.3.7 带角度公差的斜面
        /// 格式如 "45°±1°"
        /// </summary>
        [Description("斜面")]
        Bevel,

        /// <summary>
        /// 保护性倒角 - GB/T 13323 4.3.7 非功能性保护倒角
        /// 标注允许的最大最小宽度（如0.2-0.5）
        /// </summary>
        [Description("保护性倒角")]
        Protective,

        /// <summary>
        /// 内部边渡 - GB/T 13323 4.3.7 内部边渡形状
        /// 标注极限偏差，单个数值表示允许的最大宽度
        /// </summary>
        [Description("内部边渡")]
        InternalTransition
    }

    /// <summary>
    /// 倒角位置枚举
    /// </summary>
    public enum ChamferPosition
    {
        /// <summary>
        /// 前表面边缘
        /// </summary>
        [Description("前表面边缘")]
        FrontSurface,

        /// <summary>
        /// 后表面边缘
        /// </summary>
        [Description("后表面边缘")]
        BackSurface,

        /// <summary>
        /// 内孔边缘
        /// </summary>
        [Description("内孔边缘")]
        InnerHole,

        /// <summary>
        /// 外圆边缘
        /// </summary>
        [Description("外圆边缘")]
        OuterCircle,

        /// <summary>
        /// 其他位置
        /// </summary>
        [Description("其他位置")]
        Other
    }

    /// <summary>
    /// 表面处理类型
    /// </summary>
    public enum SurfaceFinish
    {
        /// <summary>
        /// 粗磨
        /// </summary>
        [Description("粗磨")]
        RoughGrind,

        /// <summary>
        /// 精磨
        /// </summary>
        [Description("精磨")]
        FineGrind,

        /// <summary>
        /// 抛光
        /// </summary>
        [Description("抛光")]
        Polish,

        /// <summary>
        /// 超光滑抛光
        /// </summary>
        [Description("超光滑抛光")]
        SuperPolish
    }

    /// <summary>
    /// 加工难度等级
    /// </summary>
    public enum ProcessingDifficulty
    {
        /// <summary>
        /// 简单
        /// </summary>
        [Description("简单")]
        Easy = 1,

        /// <summary>
        /// 中等
        /// </summary>
        [Description("中等")]
        Medium = 2,

        /// <summary>
        /// 困难
        /// </summary>
        [Description("困难")]
        Hard = 3,

        /// <summary>
        /// 极难
        /// </summary>
        [Description("极难")]
        VeryHard = 4
    }
}