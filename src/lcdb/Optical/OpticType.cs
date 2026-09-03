using System;

namespace lcdb.Optical
{
    /// <summary>
    /// 光学类型枚举
    /// </summary>
    public enum OpticType
    {
        /// <summary>
        /// 无光学属性
        /// </summary>
        None = 0,
        
        /// <summary>
        /// 反射
        /// </summary>
        Reflective = 1,
        
        /// <summary>
        /// 折射
        /// </summary>
        Refractive = 2,
        
        /// <summary>
        /// 吸收
        /// </summary>
        Absorptive = 3,
        
        /// <summary>
        /// 散射
        /// </summary>
        Scattering = 4,
        
        /// <summary>
        /// 衍射
        /// </summary>
        Diffractive = 5
    }
}