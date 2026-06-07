namespace lcdb
{
    /// <summary>
    /// 填充图案样式
    /// </summary>
    public enum HatchStyle
    {
        /// <summary>
        /// 标准样式 - 填充"奇偶"区域
        /// </summary>
        Normal = 0,

        /// <summary>
        /// 外部样式 - 仅填充最外层区域
        /// </summary>
        Outer = 1,

        /// <summary>
        /// 忽略样式 - 填充整个区域
        /// </summary>
        Ignore = 2
    }
}