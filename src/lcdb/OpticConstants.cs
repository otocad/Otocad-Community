namespace lcdb
{
    /// <summary>
    /// 光学设计相关常量
    /// </summary>
    public static class OpticConstants
    {
        /// <summary>
        /// 透镜相关常量
        /// </summary>
        public static class Lens
        {
            /// <summary>
            /// 默认中心线偏移量
            /// </summary>
            public const double DEFAULT_CENTER_LINE_OFFSET = 8.0;

            /// <summary>
            /// 粗糙度标记偏移量
            /// </summary>
            public const double ROUGHNESS_OFFSET = 5.0;

            /// <summary>
            /// 最小厚度
            /// </summary>
            public const double MIN_THICKNESS = 1e-10;
        }
    }
}