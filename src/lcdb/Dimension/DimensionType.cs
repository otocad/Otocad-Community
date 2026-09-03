using System;
using System.ComponentModel;

namespace OtoCAD.Dimension
{
    /// <summary>
    /// 尺寸标注类型枚举
    /// </summary>
    public enum DimensionType
    {
        /// <summary>
        /// 直径尺寸
        /// </summary>
        [Description("直径")]
        Diameter,

        /// <summary>
        /// 厚度尺寸
        /// </summary>
        [Description("厚度")]
        Thickness,

        /// <summary>
        /// 半径尺寸
        /// </summary>
        [Description("半径")]
        Radius,

        /// <summary>
        /// 角度尺寸
        /// </summary>
        [Description("角度")]
        Angle,

        /// <summary>
        /// 线性尺寸
        /// </summary>
        [Description("线性")]
        Linear,

        /// <summary>
        /// 链式尺寸
        /// </summary>
        [Description("链式")]
        Chain,

        /// <summary>
        /// 参考尺寸
        /// </summary>
        [Description("参考")]
        Reference,

        /// <summary>
        /// 坐标尺寸
        /// </summary>
        [Description("坐标")]
        Ordinate,

        /// <summary>
        /// 弧长尺寸
        /// </summary>
        [Description("弧长")]
        ArcLength,

        /// <summary>
        /// 倒角尺寸
        /// </summary>
        [Description("倒角")]
        Chamfer
    }

    /// <summary>
    /// 公差类型枚举
    /// </summary>
    public enum ToleranceType
    {
        /// <summary>
        /// 对称公差 (±)
        /// </summary>
        [Description("对称公差")]
        Symmetric,

        /// <summary>
        /// 不对称公差 (+/-)
        /// </summary>
        [Description("不对称公差")]
        Asymmetric,

        /// <summary>
        /// 极限偏差
        /// </summary>
        [Description("极限偏差")]
        Limit,

        /// <summary>
        /// 配合公差 (H7/g6)
        /// </summary>
        [Description("配合公差")]
        Fit,

        /// <summary>
        /// 无公差
        /// </summary>
        [Description("无公差")]
        None
    }

    /// <summary>
    /// 尺寸来源类型
    /// </summary>
    public enum DimensionSource
    {
        /// <summary>
        /// 自动生成
        /// </summary>
        [Description("自动生成")]
        Auto,

        /// <summary>
        /// 手动创建
        /// </summary>
        [Description("手动创建")]
        Manual,

        /// <summary>
        /// 导入
        /// </summary>
        [Description("导入")]
        Import
    }

    /// <summary>
    /// 公差等级枚举
    /// </summary>
    public enum ToleranceGrade
    {
        [Description("IT5 (精密)")]
        IT5,

        [Description("IT6 (较高)")]
        IT6,

        [Description("IT7 (一般)")]
        IT7,

        [Description("IT8 (较低)")]
        IT8,

        [Description("IT9")]
        IT9,

        [Description("IT10")]
        IT10,

        [Description("IT11")]
        IT11,

        [Description("IT12")]
        IT12,

        [Description("自定义")]
        Custom
    }

    /// <summary>
    /// 链式标注类型
    /// </summary>
    public enum ChainDimensionType
    {
        /// <summary>
        /// 连续链式
        /// </summary>
        [Description("连续链式")]
        Continuous,

        /// <summary>
        /// 基准链式
        /// </summary>
        [Description("基准链式")]
        Baseline,

        /// <summary>
        /// 坐标链式
        /// </summary>
        [Description("坐标链式")]
        Coordinate
    }

    /// <summary>
    /// 测量方法枚举
    /// </summary>
    public enum MeasurementMethod
    {
        [Description("千分尺")]
        Micrometer,

        [Description("游标卡尺")]
        Caliper,

        [Description("高度规")]
        HeightGauge,

        [Description("三坐标测量")]
        CMM,

        [Description("光学测量")]
        Optical,

        [Description("其他")]
        Other
    }
}