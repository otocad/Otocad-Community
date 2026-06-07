using netDxf;
using lcdb;
using lcdb.Transaction;
using OtoCAD.OpticEntity.FeatureControl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace OtoCAD.OpticEntity
{
    public delegate void DataUpdateEvent();
    public interface IEditableProperty
    {
        event DataUpdateEvent OnDataUpdate;
        void GenEntity();
    }

    /// <summary>
    /// 子组件基础类
    /// </summary>
    public abstract class BaseElementBlock : IEditableProperty
    {

        [Browsable(false)]
        public virtual string BlockName { get; } = "ModelSpace";

        public ObjectId id { get; set; } = ObjectId.Null;

        public bool BlockSelect { get; set; } = false;

        [Category("Draw")]
        [DisplayName("绘图位置X")]
        public double OriginalX { get; set; } = 100.0;
        [Category("Draw")]
        [DisplayName("绘图位置Y")]
        public double OriginalY { get; set; } = 150.0;


        [Category("操作")]
        [DisplayName("激活")]
        public bool Active { get; set; } = true;

        #region 功能控制属性

        /// <summary>
        /// 功能控制字典
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public Dictionary<string, FeatureControlProperty> FeatureControls { get; } = new Dictionary<string, FeatureControlProperty>();

        /// <summary>
        /// 渲染功能控制
        /// </summary>
        [FeatureControl("Rendering", true, Priority = 100, Description = "控制元素的渲染效果和显示属性")]
        [Category("Feature Control")]
        [DisplayName("渲染控制")]
        [Description("控制元素的显示效果，包括轮廓、填充、透明度等属性")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public RenderingFeature RenderingControl { get; set; } = new RenderingFeature();

        /// <summary>
        /// 标注功能控制
        /// </summary>
        [FeatureControl("Annotation", false, Priority = 50, Description = "控制元素的自动标注功能")]
        [Category("Feature Control")]
        [DisplayName("标注控制")]
        [Description("控制元素的自动标注功能，包括尺寸标注、标签和坐标显示")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public AnnotationFeature AnnotationControl { get; set; } = new AnnotationFeature();

        /// <summary>
        /// 功能控制启用状态变更事件
        /// </summary>
        public event EventHandler<FeatureControlChangedEventArgs> FeatureControlChanged;

        #endregion


        [Browsable(false)]
        [JsonIgnore]
        // 生成绘图Entity 需要添加到这里
        protected Database database { get; set; } = null;

        [Browsable(false)]
        [JsonIgnore]
        protected ITransactionManager transactionManager { get; set; }

        public virtual void SetDataBase(Database database)
        {
            this.database = database;
            this.transactionManager = new TransactionManager(database);
        }

        public event DataUpdateEvent OnDataUpdate;

        [Browsable(false)]
        public List<ObjectId> ChildIdList { get; } = new List<ObjectId>();
        
        /// <summary>
        /// 存储所有生成的实体ID
        /// </summary>
        [Browsable(false)]
        public List<ObjectId> GeneratedEntityIds { get; } = new List<ObjectId>();
        
        /// <summary>
        /// 生成计数器，用于调试
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public int GenerationCount { get; private set; } = 0;
        
        [Browsable(false)]
        [JsonIgnore]
        private List<Entity> _pendingEntities;

        protected BaseElementBlock(Database database)
        {
            this.database = database;
            this.transactionManager = new TransactionManager(database);
            InitializeFeatureControls();
        }
        protected BaseElementBlock()
        {
            InitializeFeatureControls();
        }

        /// <summary>
        /// 初始化功能控制
        /// </summary>
        private void InitializeFeatureControls()
        {
            // 注册功能控制属性
            RegisterFeatureControl("Rendering", RenderingControl);
            RegisterFeatureControl("Annotation", AnnotationControl);

            // 订阅功能控制变更事件
            RenderingControl.EnabledChanged += OnFeatureControlEnabledChanged;
            AnnotationControl.EnabledChanged += OnFeatureControlEnabledChanged;
        }

        /// <summary>
        /// 注册功能控制
        /// </summary>
        /// <param name="name">功能名称</param>
        /// <param name="feature">功能控制实例</param>
        protected virtual void RegisterFeatureControl(string name, FeatureControlProperty feature)
        {
            if (feature != null && !FeatureControls.ContainsKey(name))
            {
                FeatureControls[name] = feature;
            }
        }

        /// <summary>
        /// 功能控制启用状态变更处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="enabled">是否启用</param>
        private void OnFeatureControlEnabledChanged(object sender, bool enabled)
        {
            if (sender is FeatureControlProperty feature)
            {
                var args = new FeatureControlChangedEventArgs(feature.Name, enabled);
                FeatureControlChanged?.Invoke(this, args);
                
                // 触发数据更新
                DataUpdate();
            }
        }
        protected Block SafeBlock()
        {
            var blockName = BlockName;

            var blc = database.blockTable[blockName] as Block;
            if (blc != null)
            {
                // 使用现有的块
                return blc;
            }

            // 如果找不到指定的块，尝试使用CurrentBlock
            blc = database.blockTable.CurrentBlock;
            if (blc == null)
            {
                // 如果还是没有，创建一个新的
                blc = new Block();
                blc.name = blockName;
                database.blockTable.Add(blc);
            }
            return blc;
        }
        protected void SafeClearEntity()
        {
            Block blc = database.blockTable[BlockName] as Block;
            if (blc != null)
            {
                // 只清除未锁定的实体
                var toRemove = new List<ObjectId>();
                foreach (var id in ChildIdList ?? new List<ObjectId>())
                {
                    var entity = database.GetObject(id) as Entity;
                    if (entity != null && !entity.IsLocked)
                    {
                        toRemove.Add(id);
                    }
                }
                toRemove.ForEach(x => blc.RemoveEntityById(x));
                toRemove.ForEach(x => ChildIdList?.Remove(x));
            }
        }

        #region 锁定功能

        /// <summary>
        /// 锁定指定的子实体
        /// </summary>
        public void LockChildEntity(ObjectId entityId)
        {
            if (database == null) return;
            
            var entity = database.GetObject(entityId) as Entity;
            if (entity != null && entity.ParentBlockId == this.id)
            {
                entity.IsLocked = true;
            }
        }

        /// <summary>
        /// 解锁指定的子实体
        /// </summary>
        public void UnlockChildEntity(ObjectId entityId)
        {
            if (database == null) return;
            
            var entity = database.GetObject(entityId) as Entity;
            if (entity != null && entity.ParentBlockId == this.id)
            {
                entity.IsLocked = false;
            }
        }

        /// <summary>
        /// 锁定所有子实体
        /// </summary>
        public void LockAll()
        {
            if (database == null || ChildIdList == null) return;
            
            foreach (var id in ChildIdList)
            {
                var entity = database.GetObject(id) as Entity;
                if (entity != null && entity.ParentBlockId == this.id)
                {
                    entity.IsLocked = true;
                }
            }
        }

        /// <summary>
        /// 解锁所有子实体
        /// </summary>
        public void UnlockAll()
        {
            if (database == null || ChildIdList == null) return;
            
            foreach (var id in ChildIdList)
            {
                var entity = database.GetObject(id) as Entity;
                if (entity != null && entity.ParentBlockId == this.id)
                {
                    entity.IsLocked = false;
                }
            }
        }

        /// <summary>
        /// 智能锁定 - 根据实体类型或特征自动锁定
        /// </summary>
        public void SmartLock(Func<Entity, bool> lockPredicate)
        {
            if (database == null || ChildIdList == null || lockPredicate == null) return;
            
            foreach (var id in ChildIdList)
            {
                var entity = database.GetObject(id) as Entity;
                if (entity != null && entity.ParentBlockId == this.id)
                {
                    entity.IsLocked = lockPredicate(entity);
                }
            }
        }

        /// <summary>
        /// 获取所有锁定的子实体
        /// </summary>
        public List<Entity> GetLockedChildren()
        {
            var lockedEntities = new List<Entity>();
            if (database == null || ChildIdList == null) return lockedEntities;
            
            foreach (var id in ChildIdList)
            {
                var entity = database.GetObject(id) as Entity;
                if (entity != null && entity.IsLocked && entity.ParentBlockId == this.id)
                {
                    lockedEntities.Add(entity);
                }
            }
            return lockedEntities;
        }

        /// <summary>
        /// 获取所有未锁定的子实体
        /// </summary>
        public List<Entity> GetUnlockedChildren()
        {
            var unlockedEntities = new List<Entity>();
            if (database == null || ChildIdList == null) return unlockedEntities;
            
            foreach (var id in ChildIdList)
            {
                var entity = database.GetObject(id) as Entity;
                if (entity != null && !entity.IsLocked && entity.ParentBlockId == this.id)
                {
                    unlockedEntities.Add(entity);
                }
            }
            return unlockedEntities;
        }

        #endregion

        /// <summary>
        /// 使用事务生成Entity
        /// </summary>
        public virtual void GenEntityWithTransaction()
        {
            if (database == null) return;

            using (var transaction = transactionManager.BeginTransaction($"Generate {GetType().Name}"))
            {
                try
                {
                    var savepoint = transaction.CreateSavepoint("BeforeGenerate");
                    
                    SafeClearEntityWithTransaction(transaction);
                    
                    if (!Active)
                    {
                        transaction.Commit();
                        return;
                    }

                    // 清空待处理Entity列表
                    _pendingEntities?.Clear();
                    
                    GenerateEntitiesWithTransaction(transaction);
                    
                    // 应用功能控制
                    ApplyFeatureControls();
                    
                    transaction.Commit();
                    
                    // 事务提交后，从待处理Entity中获取最终ID并更新ChildIdList
                    UpdateChildIdListFromPendingEntities();
                    
                    DataUpdate();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _pendingEntities?.Clear(); // 清空待处理列表
                    throw new InvalidOperationException($"Failed to generate entities: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 传统的GenEntity方法，保持向后兼容
        /// </summary>
        public virtual void GenEntity()
        {
            // 增加生成计数器
            GenerationCount++;
            
            // 清理旧的子实体（需要ChildIdList中的ID信息）
            ClearOldChildEntities();
            
            // 在清理实体后才清空ChildIdList
            // 注意：必须在ClearOldChildEntities之后，否则无法找到要清理的实体
            ChildIdList.Clear();
            
            if (transactionManager != null)
            {
                    GenEntityWithTransaction();
            }
            else
            {
                if (database == null)
                    return;

                if (!Active)
                    return;

                GenerateEntitiesLegacy();
                
                // 应用功能控制
                ApplyFeatureControls();
            }
            
        }

        /// <summary>
        /// 应用所有启用的功能控制
        /// </summary>
        protected virtual void ApplyFeatureControls()
        {
            if (database == null)
                return;

            // 按优先级排序并应用功能控制
            var enabledFeatures = FeatureControls.Values
                .Where(f => f.IsEnabled)
                .OrderByDescending(f => f.Priority)
                .ToList();

            foreach (var feature in enabledFeatures)
            {
                try
                {
                    feature.TryApply(this);
                }
                catch (Exception ex)
                {
                    // 记录错误但继续处理其他功能
                    System.Diagnostics.Debug.WriteLine($"Failed to apply feature {feature.Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 重置所有功能控制
        /// </summary>
        public virtual void ResetAllFeatureControls()
        {
            foreach (var feature in FeatureControls.Values)
            {
                try
                {
                    feature.TryReset(this);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to reset feature {feature.Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取指定名称的功能控制
        /// </summary>
        /// <typeparam name="T">功能控制类型</typeparam>
        /// <param name="name">功能名称</param>
        /// <returns>功能控制实例</returns>
        public virtual T GetFeatureControl<T>(string name) where T : FeatureControlProperty
        {
            return FeatureControls.TryGetValue(name, out var feature) ? feature as T : null;
        }

        /// <summary>
        /// 启用或禁用指定功能控制
        /// </summary>
        /// <param name="name">功能名称</param>
        /// <param name="enabled">是否启用</param>
        public virtual void SetFeatureControlEnabled(string name, bool enabled)
        {
            if (FeatureControls.TryGetValue(name, out var feature))
            {
                feature.IsEnabled = enabled;
            }
        }

        /// <summary>
        /// 子类实现具体的Entity生成逻辑
        /// </summary>
        protected abstract void GenerateEntitiesWithTransaction(IEntityTransaction transaction);

        /// <summary>
        /// 子类实现传统的Entity生成逻辑（向后兼容）
        /// </summary>
        protected virtual void GenerateEntitiesLegacy()
        {
            // 默认实现为空，子类可以重写
        }

        /// <summary>
        /// 标记生成的实体
        /// </summary>
        protected void MarkGeneratedEntity(Entity entity)
        {
            if (entity != null)
            {
                entity.IsGenerated = true;
                entity.ParentBlockId = this.id;
                // 根据BaseElementBlock的具体类型设置Source和EditMode
                if (this is Frame)
                {
                    entity.Source = EntitySource.OpticalFrame;
                    // Frame生成的实体属于Frame SuperLayer
                    entity.EditMode = EntityEditMode.Frame;
                }
                else if (this is Element)
                {
                    entity.Source = EntitySource.OpticalElement;
                    // 光学元件生成的实体属于Drawing SuperLayer
                    entity.EditMode = EntityEditMode.Drawing;
                }
                else
                {
                    entity.Source = EntitySource.Manual;
                    entity.EditMode = EntityEditMode.Drawing;
                }
            }
        }

        /// <summary>
        /// 批量标记生成的实体
        /// </summary>
        protected void MarkGeneratedEntities(IEnumerable<Entity> entities)
        {
            if (entities == null) return;
            
            foreach (var entity in entities)
            {
                MarkGeneratedEntity(entity);
            }
        }

        /// <summary>
        /// 使用事务清除Entity
        /// </summary>
        protected virtual void SafeClearEntityWithTransaction(IEntityTransaction transaction)
        {
            // SafeClearEntityWithTransaction - 开始处理
            
            if (ChildIdList.Count > 0)
            {
                var savepoint = transaction.CreateSavepoint("BeforeClear");
                
                try
                {
                    // 验证ChildIdList中的ID是否有效
                    var block = database.blockTable[BlockName] as Block;
                    // 验证Block中的Entity数量
                    
                    foreach (var childId in ChildIdList)
                    {
                        var entity = FindEntityInBlock(block, childId);
                        if (entity != null)
                        {
                            // Entity类型已验证
                        }
                    }
                    
                    // 记录删除操作到事务中
                    var removeCount = 0;
                    foreach (var childId in ChildIdList.ToList())
                    {
                        var removed = transaction.RemoveEntity(childId, BlockName);
                        if (removed)
                        {
                            removeCount++;
                        }
                    }
                    
                    // 删除操作已记录到事务中
                    
                    ChildIdList.Clear();
                    // ChildIdList已清空
                }
                catch (Exception ex)
                {
                    // SafeClearEntityWithTransaction异常已捕获
                    transaction.RollbackToSavepoint(savepoint);
                    throw new InvalidOperationException($"Failed to clear entities: {ex.Message}", ex);
                }
            }
            else
            {
                // SafeClearEntityWithTransaction - ChildIdList为空，无需清除
            }
        }
        
        private Entity FindEntityInBlock(Block block, ObjectId entityId)
        {
            if (block == null) return null;
            
            foreach (var item in block)
            {
                if (item is Entity entity && entity.id == entityId)
                {
                    return entity;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 清理旧的子实体
        /// </summary>
        protected virtual void ClearOldChildEntities()
        {
            if (database == null || ChildIdList.Count == 0)
                return;
            
            var block = database.blockTable[BlockName] as Block;
            if (block == null)
                return;
            
            var entitiesToRemove = new List<ObjectId>();
            
            // 使用ChildIdList代替GeneratedEntityIds
            foreach (var entityId in ChildIdList)
            {
                var entity = database.GetObject(entityId) as Entity;
                if (entity != null)
                {
                    entitiesToRemove.Add(entityId);
                }
            }
            
            // 从数据库中移除实体
            foreach (var entityId in entitiesToRemove)
            {
                var entity = database.GetObject(entityId) as Entity;
                if (entity != null)
                {
                    entity.Erase();
                }
            }
            
            // ChildIdList将在调用方清理
        }
        
        /// <summary>
        /// 删除所有旧的生成实体（已废弃，保留以兼容）
        /// </summary>
        [Obsolete("请使用ClearOldChildEntities代替")]
        protected virtual void RemoveOldGeneratedEntities()
        {
            // 转发到新方法
            ClearOldChildEntities();
            
            // 清空废弃的GeneratedEntityIds列表
            GeneratedEntityIds.Clear();
        }
        
        /// <summary>
        /// 查找所有生成的实体
        /// </summary>
        /// <returns>生成的实体列表</returns>
        public virtual List<Entity> FindGeneratedEntities()
        {
            var result = new List<Entity>();
            
            if (database == null)
                return result;
            
            var block = database.blockTable[BlockName] as Block;
            if (block == null)
                return result;
            
            foreach (var item in block)
            {
                var entity = item as Entity;
                if (entity != null && 
                    IsGeneratedEntity(entity) &&
                    entity.ParentComponentId == this.id)
                {
                    result.Add(entity);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 记录生成的实体ID（已废弃，保留以兼容）
        /// </summary>
        /// <param name="entity">生成的实体</param>
        [Obsolete("不再需要调用此方法，实体ID应直接添加到ChildIdList")]
        protected virtual void RecordGeneratedEntity(Entity entity)
        {
            // 不再使用GeneratedEntityIds
            // 实体应该直接添加到ChildIdList
            if (entity != null && !entity.id.isNull && !ChildIdList.Contains(entity.id))
            {
                ChildIdList.Add(entity.id);
            }
        }
        
        /// <summary>
        /// 判断实体是否为生成的实体
        /// </summary>
        private bool IsGeneratedEntity(Entity entity)
        {
            return entity != null && entity.Source != EntitySource.Manual;
        }
        
        /// <summary>
        /// 事务提交后，从待处理Entity中获取最终ID并更新ChildIdList
        /// </summary>
        private void UpdateChildIdListFromPendingEntities()
        {
            if (_pendingEntities == null || _pendingEntities.Count == 0)
            {
                // UpdateChildIdListFromPendingEntities - 没有待处理Entity
                return;
            }
            
            // UpdateChildIdListFromPendingEntities - 开始处理Entity
            
            foreach (var entity in _pendingEntities)
            {
                // Entity类型和ID已记录
                
                if (!entity.id.isNull)
                {
                    ChildIdList.Add(entity.id);
                    // 添加最终ID到ChildIdList
                }
                else
                {
                    // 警告：Entity ID为空，跳过
                }
            }
            
            // ChildIdList更新完成
            _pendingEntities.Clear();
        }

        /// <summary>
        /// 使用事务添加Entity
        /// </summary>
        protected virtual void AppendEntityWithTransaction(IEntityTransaction transaction, Entity entity)
        {
            if (entity == null) return;

            // AppendEntityWithTransaction - 准备添加Entity
            
            var tempId = transaction.AddEntity(entity, BlockName);
            // transaction.AddEntity已执行
            
            // 不立即添加到ChildIdList，而是记录Entity引用，等待事务提交后获取最终ID
            // 临时存储Entity以便后续获取最终ID
            if (_pendingEntities == null)
                _pendingEntities = new List<Entity>();
            _pendingEntities.Add(entity);
            
            // Entity添加到待处理列表
            
            // 验证Entity是否真的被添加到Block中
            var block = database.blockTable[BlockName] as Block;
            var entityCount = block?.Entities.Count ?? 0;
            // Block中的Entity数量已统计
        }

        /// <summary>
        /// 传统的AppendEntity方法，保持向后兼容
        /// </summary>
        public void AppendEntity(Entity entity)
        {

            if (transactionManager?.CurrentTransaction != null)
            {
                AppendEntityWithTransaction(transactionManager.CurrentTransaction, entity);
            }
            else
            {
                SafeBlock().AppendEntity(entity);
                ChildIdList.Add(entity.id);
            }
        }

        /// <summary>
        /// 批量操作支持
        /// </summary>
        public virtual void BatchUpdate(Action<IEntityTransaction> updateAction)
        {
            if (database == null) return;

            using (var transaction = transactionManager.BeginTransaction($"Batch update {GetType().Name}"))
            {
                try
                {
                    updateAction(transaction);
                    transaction.Commit();
                    DataUpdate();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new InvalidOperationException($"Batch update failed: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 获取操作历史
        /// </summary>
        public virtual IReadOnlyList<TransactionOperation> GetOperationHistory()
        {
            return transactionManager?.GetTransactionHistory() ?? new List<TransactionOperation>().AsReadOnly();
        }

        internal void DataUpdate()
        {
            OnDataUpdate?.Invoke();
        }
    }

    /// <summary>
    /// 功能控制变更事件参数
    /// </summary>
    public class FeatureControlChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 功能名称
        /// </summary>
        public string FeatureName { get; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// 变更时间
        /// </summary>
        public DateTime ChangedTime { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="featureName">功能名称</param>
        /// <param name="enabled">是否启用</param>
        public FeatureControlChangedEventArgs(string featureName, bool enabled)
        {
            FeatureName = featureName;
            Enabled = enabled;
            ChangedTime = DateTime.Now;
        }
    }
}
