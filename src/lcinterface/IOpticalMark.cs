using System;
using System.Collections.Generic;
using System.Drawing;
using LitMath;

namespace lcdb.Interfaces
{
    /// <summary>
    /// 光学标记接口
    /// 定义所有光学标记的基本行为
    /// </summary>
    public interface IOpticalMark
    {
        /// <summary>
        /// 标记类型
        /// </summary>
        OpticalMarkType MarkType { get; }

        /// <summary>
        /// 标记位置
        /// </summary>
        Vector2 Position { get; set; }

        /// <summary>
        /// 标记大小/比例
        /// </summary>
        double Scale { get; set; }

        /// <summary>
        /// 标记文本内容
        /// </summary>
        string MarkText { get; set; }

        /// <summary>
        /// 是否显示文本
        /// </summary>
        bool ShowText { get; set; }

        /// <summary>
        /// 文本偏移量
        /// </summary>
        Vector2 TextOffset { get; set; }

        /// <summary>
        /// 标记旋转角度（度）
        /// </summary>
        double Rotation { get; set; }

        /// <summary>
        /// 是否可见
        /// </summary>
        bool IsVisible { get; set; }

#if WINDOWS
        /// <summary>
        /// 绘制标记 (WinForms GDI+ 路径, 跨平台用 Entity.Draw(IGraphicsDraw))
        /// </summary>
        void Draw(Graphics g, float scale);
#endif

        /// <summary>
        /// 获取标记边界框
        /// </summary>
        RectangleF GetBounds();

        /// <summary>
        /// 验证标记数据
        /// </summary>
        bool Validate();

        /// <summary>
        /// 获取标记属性
        /// </summary>
        Dictionary<string, object> GetProperties();

        /// <summary>
        /// 设置标记属性
        /// </summary>
        void SetProperties(Dictionary<string, object> properties);

        /// <summary>
        /// 克隆标记
        /// </summary>
        IOpticalMark Clone();

        /// <summary>
        /// 获取标记描述
        /// </summary>
        string GetDescription();
    }

    /// <summary>
    /// 光学标记类型枚举
    /// </summary>
    public enum OpticalMarkType
    {
        // 表面质量标记
        SurfaceQuality,         // 表面质量（疵病等级）
        SurfaceGrade,          // 表面等级（I、II、III）
        
        // 面形精度标记
        FormAccuracy,          // 面形精度（PV/RMS）
        PowerIrregularity,     // Power/Irregularity
        
        // 中心偏差标记
        CenterDeviation,       // 中心偏差
        Decentration,         // 偏心
        Tilt,                 // 倾斜
        
        // 光学特征标记
        OpticalAxis,          // 光轴
        FocalPoint,           // 焦点
        
        // ISO 10110标准标记
        ISO10110_5,           // 面形公差
        ISO10110_6,           // 中心公差
        ISO10110_7,           // 表面缺陷
        ISO10110_8,           // 表面纹理
        ISO10110_17,          // 激光损伤阈值

        // ISO 10110 新增标记 (2025-11)
        ISO10110_2,           // 应力双折射 (新)
        ISO10110_3,           // 气泡和夹杂物 (新)
        ISO10110_4,           // 不均匀性和条纹 (新)
        ISO10110_12,          // 非球面表面 (新)
        ISO10110_14,          // 波前变形公差 (新)

        // 材料缺陷标记
        Bubble,               // 气泡
        BubbleInclusion,      // 气泡和夹杂物 (新)
        Stria,                // 条纹
        Inhomogeneity,        // 不均匀性 (新)
        StressBirefringence,  // 应力双折射

        // 非球面和波前标记 (新)
        Aspheric,             // 非球面表面
        Wavefront,            // 波前变形
        
        // 特殊标记
        Processing,           // 加工标记
        Inspection,           // 检验标记
        Assembly,             // 装配标记
        
        // 镀膜标记（已存在）
        Coating,              // 镀膜
        
        // 粗糙度标记（已存在）
        Roughness,           // 粗糙度

        // 技术要求表格 (2025-12)
        TechnicalRequirement // GB/T 13323 技术要求表格
    }

    // 注意：以下接口（IOpticalMarkManager, IOpticalMarkValidator, IOpticalMarkStyle, IOpticalMarkTemplate）
    // 已删除，因为没有实现类。如需要，请在实际需要时再定义。
}