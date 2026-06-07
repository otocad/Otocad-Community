using System;
using System.ComponentModel;

namespace lcdb.Coating
{
    /// <summary>
    /// 镀膜类型枚举
    /// </summary>
    public enum CoatingType
    {
        /// <summary>
        /// 增透膜
        /// </summary>
        [Description("增透膜")]
        AR,

        /// <summary>
        /// 高反膜
        /// </summary>
        [Description("高反膜")]
        HR,

        /// <summary>
        /// 分光膜
        /// </summary>
        [Description("分光膜")]
        BS,

        /// <summary>
        /// 滤光膜
        /// </summary>
        [Description("滤光膜")]
        Filter,

        /// <summary>
        /// 金属膜
        /// </summary>
        [Description("金属膜")]
        Metal,

        /// <summary>
        /// 自定义
        /// </summary>
        [Description("自定义")]
        Custom,

        /// <summary>
        /// 偏振膜
        /// </summary>
        [Description("偏振膜")]
        Polarizer,

        /// <summary>
        /// 导电膜 (ITO等)
        /// </summary>
        [Description("导电膜")]
        Conductive,

        /// <summary>
        /// 宽带增透膜
        /// </summary>
        [Description("宽带增透膜")]
        BBAR,

        /// <summary>
        /// 部分反射膜
        /// </summary>
        [Description("部分反射膜")]
        PartialReflector
    }

    /// <summary>
    /// 偏振态类型
    /// </summary>
    public enum PolarizationType
    {
        /// <summary>
        /// 非偏振
        /// </summary>
        [Description("非偏振")]
        Unpolarized,

        /// <summary>
        /// S偏振
        /// </summary>
        [Description("S偏振")]
        S,

        /// <summary>
        /// P偏振
        /// </summary>
        [Description("P偏振")]
        P,

        /// <summary>
        /// 混合偏振
        /// </summary>
        [Description("混合偏振")]
        Both
    }

    /// <summary>
    /// 镀膜工艺类型
    /// </summary>
    public enum CoatingProcessType
    {
        /// <summary>
        /// 电子束蒸发
        /// </summary>
        [Description("电子束蒸发")]
        EBeam,

        /// <summary>
        /// 离子辅助沉积
        /// </summary>
        [Description("离子辅助沉积")]
        IAD,

        /// <summary>
        /// 磁控溅射
        /// </summary>
        [Description("磁控溅射")]
        Magnetron,

        /// <summary>
        /// 离子束溅射
        /// </summary>
        [Description("离子束溅射")]
        IBS,

        /// <summary>
        /// 原子层沉积
        /// </summary>
        [Description("原子层沉积")]
        ALD
    }

    /// <summary>
    /// 监控方式
    /// </summary>
    public enum MonitoringType
    {
        /// <summary>
        /// 光学监控
        /// </summary>
        [Description("光学监控")]
        Optical,

        /// <summary>
        /// 石英晶振监控
        /// </summary>
        [Description("石英晶振监控")]
        Quartz,

        /// <summary>
        /// 时间监控
        /// </summary>
        [Description("时间监控")]
        Time
    }
}