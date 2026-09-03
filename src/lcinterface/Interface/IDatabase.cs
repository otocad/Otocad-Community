using System;

namespace lcinterface.Interface
{
    /// <summary>
    /// 数据库接口，定义数据库对象变化事件
    /// </summary>
    public interface IDatabase
    {
        /// <summary>
        /// 当对象被添加到数据库时触发
        /// </summary>
        event Action<object> objectAdded;

        /// <summary>
        /// 当数据库中的对象被修改时触发
        /// </summary>
        event Action<object> objectModified;

        /// <summary>
        /// 当对象从数据库中被删除时触发
        /// </summary>
        event Action<object> objectErased;
    }
}