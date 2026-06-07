using System;

namespace OtoCAD.OpticEntity.FeatureControl
{
    /// <summary>
    /// 功能控制特性标记
    /// 用于标记支持功能控制的属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class FeatureControlAttribute : System.Attribute
    {
        /// <summary>
        /// 功能控制分组名称
        /// </summary>
        public string GroupName { get; set; } = "General";

        /// <summary>
        /// 默认启用状态
        /// </summary>
        public bool DefaultEnabled { get; set; } = true;

        /// <summary>
        /// 优先级，数值越大优先级越高
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// 功能描述
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 是否允许用户禁用此功能
        /// </summary>
        public bool AllowDisable { get; set; } = true;

        /// <summary>
        /// 构造函数
        /// </summary>
        public FeatureControlAttribute()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="groupName">分组名称</param>
        /// <param name="defaultEnabled">默认启用状态</param>
        public FeatureControlAttribute(string groupName, bool defaultEnabled = true)
        {
            GroupName = groupName;
            DefaultEnabled = defaultEnabled;
        }
    }
}