namespace lcdb.Annotation
{
    /// <summary>
    /// 镀膜类型枚举
    /// 符合 GB/T 13323-2009 光学制图标准
    /// </summary>
    public enum CoatingType
    {
        /// <summary>
        /// 减反射膜/增透膜 (AR) - 圆圈内十字加90°标注
        /// GB/T 13323-2009 表1
        /// </summary>
        AR = 0,

        /// <summary>
        /// 内反射膜/高反膜 (HR) - 圆圈内向上三角
        /// GB/T 13323-2009 表1
        /// </summary>
        HR = 1,

        /// <summary>
        /// 分束膜/分光膜 (BS) - 圆圈内Y形
        /// GB/T 13323-2009 表1
        /// </summary>
        BS = 2,

        /// <summary>
        /// 滤光膜 - 圆圈内横线
        /// GB/T 13323-2009 表1
        /// </summary>
        Filter = 3,

        /// <summary>
        /// 保护膜 - 圆圈内双横线
        /// GB/T 13323-2009 表1
        /// </summary>
        Protective = 4,

        /// <summary>
        /// 其他/自定义
        /// </summary>
        Other = 5,

        /// <summary>
        /// 外反射膜 - 圆圈内向下三角
        /// GB/T 13323-2009 表1
        /// </summary>
        OuterReflective = 6,

        /// <summary>
        /// 导电膜 - 圆圈内波浪线
        /// GB/T 13323-2009 表1
        /// </summary>
        Conductive = 7,

        /// <summary>
        /// 偏振膜 - 圆圈内十字
        /// GB/T 13323-2009 表1
        /// </summary>
        Polarizing = 8,

        /// <summary>
        /// 涂黑 - 粗点划线
        /// GB/T 13323-2009 表1
        /// </summary>
        Blackening = 9,

        /// <summary>
        /// 部分反射膜 (PR)
        /// </summary>
        PR = 10,

        /// <summary>
        /// 宽带增透膜 (BBAR)
        /// </summary>
        BBAR = 11,

        /// <summary>
        /// 自定义
        /// </summary>
        Custom = 12
    }

    /// <summary>
    /// 镀膜标记形状
    /// 注意: GB/T 13323-2009标准中，镀膜符号统一使用圆圈+内部符号的形式
    /// 此枚举保留用于兼容旧版本，新代码应使用CoatingType直接决定符号形状
    /// </summary>
    public enum CoatingMarkShape
    {
        /// <summary>
        /// 标准符号 - 根据CoatingType自动选择GB/T 13323符号
        /// </summary>
        Standard = 0,

        /// <summary>
        /// 三角形 (兼容旧版)
        /// </summary>
        Triangle = 1,

        /// <summary>
        /// 圆形 (兼容旧版)
        /// </summary>
        Circle = 2,

        /// <summary>
        /// 正方形 (兼容旧版)
        /// </summary>
        Square = 3,

        /// <summary>
        /// 菱形 (兼容旧版)
        /// </summary>
        Diamond = 4
    }

    /// <summary>
    /// 镀膜文本位置
    /// </summary>
    public enum CoatingTextPosition
    {
        /// <summary>
        /// 右侧
        /// </summary>
        Right = 0,
        
        /// <summary>
        /// 左侧
        /// </summary>
        Left = 1,
        
        /// <summary>
        /// 上方
        /// </summary>
        Top = 2,
        
        /// <summary>
        /// 下方
        /// </summary>
        Bottom = 3,
        
        /// <summary>
        /// 右上
        /// </summary>
        TopRight = 4,
        
        /// <summary>
        /// 左上
        /// </summary>
        TopLeft = 5,
        
        /// <summary>
        /// 右下
        /// </summary>
        BottomRight = 6,
        
        /// <summary>
        /// 左下
        /// </summary>
        BottomLeft = 7
    }
}