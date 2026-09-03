using System;

namespace lcdb.Annotation
{
    /// <summary>
    /// 加工标记样式
    /// </summary>
    public enum ProcessingMarkStyle
    {
        /// <summary>
        /// 标准样式
        /// </summary>
        Standard,
        
        /// <summary>
        /// 简化样式
        /// </summary>
        Simplified,
        
        /// <summary>
        /// 详细样式
        /// </summary>
        Detailed
    }

    /// <summary>
    /// 检验标记样式
    /// </summary>
    public enum InspectionMarkStyle
    {
        /// <summary>
        /// 标准样式
        /// </summary>
        Standard,
        
        /// <summary>
        /// 简化样式
        /// </summary>
        Simplified,
        
        /// <summary>
        /// 详细样式
        /// </summary>
        Detailed
    }

    /// <summary>
    /// 装配标记样式
    /// </summary>
    public enum AssemblyMarkStyle
    {
        /// <summary>
        /// 标准样式
        /// </summary>
        Standard,
        
        /// <summary>
        /// 简化样式
        /// </summary>
        Simplified,
        
        /// <summary>
        /// 详细样式
        /// </summary>
        Detailed
    }

    /// <summary>
    /// ISO 10110-5 表面形状误差标记样式
    /// </summary>
    public enum ISO10110_5MarkStyle
    {
        /// <summary>
        /// 标准样式
        /// </summary>
        Standard,
        
        /// <summary>
        /// 带公差样式
        /// </summary>
        WithTolerance,
        
        /// <summary>
        /// 带测试条件样式
        /// </summary>
        WithTestCondition
    }

    /// <summary>
    /// ISO 10110-6 中心偏差标记样式
    /// </summary>
    public enum ISO10110_6MarkStyle
    {
        /// <summary>
        /// 标准样式
        /// </summary>
        Standard,
        
        /// <summary>
        /// 带公差样式
        /// </summary>
        WithTolerance,
        
        /// <summary>
        /// 带测量方法样式
        /// </summary>
        WithMeasurementMethod
    }

    /// <summary>
    /// ISO 10110-7 表面缺陷标记样式
    /// </summary>
    public enum ISO10110_7MarkStyle
    {
        /// <summary>
        /// 标准样式
        /// </summary>
        Standard,
        
        /// <summary>
        /// 详细样式
        /// </summary>
        Detailed,
        
        /// <summary>
        /// 简化样式
        /// </summary>
        Simplified
    }

    /// <summary>
    /// ISO 10110-8 表面纹理标记样式
    /// </summary>
    public enum ISO10110_8MarkStyle
    {
        /// <summary>
        /// 标准样式
        /// </summary>
        Standard,
        
        /// <summary>
        /// 带纹理参数样式
        /// </summary>
        WithTextureParameters,
        
        /// <summary>
        /// 带方向样式
        /// </summary>
        WithDirection
    }

    /// <summary>
    /// ISO 10110-17 激光损伤阈值标记样式
    /// </summary>
    public enum ISO10110_17MarkStyle
    {
        /// <summary>
        /// 标准样式
        /// </summary>
        Standard,
        
        /// <summary>
        /// 带测试条件样式
        /// </summary>
        WithTestConditions,
        
        /// <summary>
        /// 带波长样式
        /// </summary>
        WithWavelength
    }
}