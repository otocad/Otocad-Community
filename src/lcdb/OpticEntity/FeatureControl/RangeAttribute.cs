using System;

namespace OtoCAD.OpticEntity.FeatureControl
{
    /// <summary>
    /// 范围特性，用于标记数值属性的有效范围
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class RangeAttribute : System.Attribute
    {
        /// <summary>
        /// 最小值
        /// </summary>
        public object Minimum { get; }

        /// <summary>
        /// 最大值
        /// </summary>
        public object Maximum { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="minimum">最小值</param>
        /// <param name="maximum">最大值</param>
        public RangeAttribute(object minimum, object maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }
    }
}