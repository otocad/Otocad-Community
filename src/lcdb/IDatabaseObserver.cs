using System;
using System.Collections.Generic;

namespace lcdb
{
    /// <summary>
    /// 数据库观察者接口
    /// 实现该接口的类可以监听数据库变化事件
    /// </summary>
    public interface IDatabaseObserver
    {
        /// <summary>
        /// 实体添加通知
        /// </summary>
        /// <param name="entityIds">添加的实体ID列表</param>
        void OnEntitiesAdded(IEnumerable<ObjectId> entityIds);
        
        /// <summary>
        /// 实体修改通知
        /// </summary>
        /// <param name="entityIds">修改的实体ID列表</param>
        void OnEntitiesModified(IEnumerable<ObjectId> entityIds);
        
        /// <summary>
        /// 实体删除通知
        /// </summary>
        /// <param name="entityIds">删除的实体ID列表</param>
        void OnEntitiesDeleted(IEnumerable<ObjectId> entityIds);
        
        /// <summary>
        /// 图层变化通知
        /// </summary>
        /// <param name="layerId">变化的图层ID</param>
        /// <param name="changeType">变化类型</param>
        void OnLayerChanged(ObjectId layerId, LayerChangeType changeType);
        
        /// <summary>
        /// 数据库事务开始
        /// </summary>
        void OnTransactionBegin();
        
        /// <summary>
        /// 数据库事务提交
        /// </summary>
        void OnTransactionCommit();
        
        /// <summary>
        /// 数据库事务回滚
        /// </summary>
        void OnTransactionRollback();
        
        /// <summary>
        /// 批量更新开始
        /// </summary>
        /// <param name="updateId">批量更新标识</param>
        void OnBatchUpdateBegin(Guid updateId);
        
        /// <summary>
        /// 批量更新结束
        /// </summary>
        /// <param name="updateId">批量更新标识</param>
        void OnBatchUpdateEnd(Guid updateId);
    }
    
    /// <summary>
    /// 图层变化类型
    /// </summary>
    public enum LayerChangeType
    {
        /// <summary>
        /// 添加图层
        /// </summary>
        Added,
        
        /// <summary>
        /// 修改图层属性
        /// </summary>
        Modified,
        
        /// <summary>
        /// 删除图层
        /// </summary>
        Deleted,
        
        /// <summary>
        /// 图层可见性改变
        /// </summary>
        VisibilityChanged,
        
        /// <summary>
        /// 图层锁定状态改变
        /// </summary>
        LockChanged
    }
    
    /// <summary>
    /// 数据库变化事件参数
    /// </summary>
    public class DatabaseChangeEventArgs : EventArgs
    {
        public ChangeType Type { get; set; }
        public IEnumerable<ObjectId> AffectedIds { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
        
        public DatabaseChangeEventArgs()
        {
            Timestamp = DateTime.Now;
            AffectedIds = new List<ObjectId>();
        }
    }
    
    /// <summary>
    /// 变化类型
    /// </summary>
    public enum ChangeType
    {
        EntityAdded,
        EntityModified,
        EntityDeleted,
        LayerChanged,
        BlockChanged,
        StyleChanged
    }
}