using System;

namespace lcdb.Transaction
{
    /// <summary>
    /// 事务管理器接口
    /// </summary>
    public interface ITransactionManager
    {
        /// <summary>
        /// 获取当前事务
        /// </summary>
        IEntityTransaction CurrentTransaction { get; }

        /// <summary>
        /// 开始一个新事务
        /// </summary>
        /// <param name="description">事务描述</param>
        /// <returns>事务对象</returns>
        IEntityTransaction BeginTransaction(string description = null);

        /// <summary>
        /// 开始一个嵌套事务
        /// </summary>
        /// <param name="description">事务描述</param>
        /// <returns>事务对象</returns>
        IEntityTransaction BeginNestedTransaction(string description = null);
    }
}