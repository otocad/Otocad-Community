using System;
using System.Collections.Generic;

namespace lcdb.Transaction
{
    /// <summary>
    /// 事务操作类型
    /// </summary>
    public enum TransactionOperationType
    {
        Add,
        Remove,
        Modify,
        Move,
        Copy
    }

    /// <summary>
    /// 事务操作记录
    /// </summary>
    public class TransactionOperation
    {
        public TransactionOperationType Type { get; set; }
        public ObjectId EntityId { get; set; }
        public Entity Entity { get; set; }
        public Entity OriginalEntity { get; set; }  // 用于回滚
        public string BlockName { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// 事务状态
    /// </summary>
    public enum TransactionState
    {
        Active,
        Committed,
        RolledBack,
        Disposed
    }

    /// <summary>
    /// 事务事件参数
    /// </summary>
    public class TransactionEventArgs : EventArgs
    {
        public IEntityTransaction Transaction { get; }
        
        public TransactionEventArgs(IEntityTransaction transaction)
        {
            Transaction = transaction;
        }
    }

    /// <summary>
    /// 事务异常
    /// </summary>
    public class TransactionException : Exception
    {
        public TransactionException(string message) : base(message) { }
        public TransactionException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// 事务接口
    /// </summary>
    public interface IEntityTransaction : IDisposable
    {
        /// <summary>
        /// 事务ID
        /// </summary>
        Guid TransactionId { get; }
        
        /// <summary>
        /// 事务状态
        /// </summary>
        TransactionState State { get; }
        
        /// <summary>
        /// 是否支持嵌套事务
        /// </summary>
        bool SupportsNesting { get; }
        
        /// <summary>
        /// 父事务
        /// </summary>
        IEntityTransaction Parent { get; }

        /// <summary>
        /// 添加Entity
        /// </summary>
        ObjectId AddEntity(Entity entity, string blockName = "ModelSpace");
        
        /// <summary>
        /// 移除Entity
        /// </summary>
        bool RemoveEntity(ObjectId entityId, string blockName = "ModelSpace");
        
        /// <summary>
        /// 修改Entity
        /// </summary>
        bool ModifyEntity(ObjectId entityId, Entity newEntity, string blockName = "ModelSpace");
        
        /// <summary>
        /// 移动Entity到不同Block
        /// </summary>
        bool MoveEntity(ObjectId entityId, string fromBlock, string toBlock);
        
        /// <summary>
        /// 复制Entity
        /// </summary>
        ObjectId CopyEntity(ObjectId sourceId, string targetBlock = null);
        
        /// <summary>
        /// 提交事务
        /// </summary>
        void Commit();
        
        /// <summary>
        /// 回滚事务
        /// </summary>
        void Rollback();
        
        /// <summary>
        /// 创建保存点
        /// </summary>
        string CreateSavepoint(string name);
        
        /// <summary>
        /// 回滚到保存点
        /// </summary>
        void RollbackToSavepoint(string savepointName);
        
        /// <summary>
        /// 获取操作历史
        /// </summary>
        IReadOnlyList<TransactionOperation> GetOperationHistory();
        
        /// <summary>
        /// 事务提交前事件
        /// </summary>
        event EventHandler<TransactionEventArgs> BeforeCommit;
        
        /// <summary>
        /// 事务提交后事件
        /// </summary>
        event EventHandler<TransactionEventArgs> AfterCommit;
        
        /// <summary>
        /// 事务回滚后事件
        /// </summary>
        event EventHandler<TransactionEventArgs> AfterRollback;
    }

    /// <summary>
    /// 事务管理器接口
    /// </summary>
    public interface ITransactionManager
    {
        /// <summary>
        /// 当前活动事务
        /// </summary>
        IEntityTransaction CurrentTransaction { get; }
        
        /// <summary>
        /// 开始新事务
        /// </summary>
        IEntityTransaction BeginTransaction(string description = null);
        
        /// <summary>
        /// 开始嵌套事务
        /// </summary>
        IEntityTransaction BeginNestedTransaction(string description = null);
        
        /// <summary>
        /// 获取事务历史
        /// </summary>
        IReadOnlyList<TransactionOperation> GetTransactionHistory(int limit = 100);
    }
}