namespace lcdb
{
    /// <summary>
    /// 多行文本行间距样式
    /// </summary>
    public enum MTextLineSpacingStyle
    {
        /// <summary>
        /// 默认值（仅适用于段落选项）
        /// </summary>
        Default = 0,

        /// <summary>
        /// 至少（较高字符会覆盖）
        /// </summary>
        AtLeast = 1,

        /// <summary>
        /// 精确（较高字符不会覆盖）
        /// </summary>
        Exact = 2,

        /// <summary>
        /// 倍数（仅适用于段落选项）
        /// </summary>
        Multiple = 3
    }
}