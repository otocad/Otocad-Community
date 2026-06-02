namespace OtoCAD.OpticEntity
{
    /// <summary>
    /// 单透镜类型枚举，根据左右表面直径的关系定义
    /// </summary>
    public enum SingleLensType
    {
        /// <summary>
        /// 等径透镜 - 左右表面直径相等
        /// </summary>
        EqualDiameter = 0,

        /// <summary>
        /// 右大左小透镜 - 右侧表面直径大于左侧
        /// </summary>
        RightLarger = 1,

        /// <summary>
        /// 左大右小透镜 - 左侧表面直径大于右侧
        /// </summary>
        LeftLarger = -1
    }
}