using System;
using System.Collections.Generic;
using System.Linq;


namespace lcdb.Transaction
{
    /// <summary>
    /// Entity事务实现
    /// </summary>
    public class EntityTransaction : IEntityTransaction
    {
        private readonly Database _database;
        private readonly List<TransactionOperation> _operations;
        private readonly Dictionary<string, List<TransactionOperation>> _savepoints;
        private readonly object _lockObject = new object();
        
        public Guid TransactionId { get; }
        public TransactionState State { get; private set; }
        public bool SupportsNesting => true;
        public IEntityTransaction Parent { get; }
        
        public event EventHandler<TransactionEventArgs> BeforeCommit;
        public event EventHandler<TransactionEventArgs> AfterCommit;
        public event EventHandler<TransactionEventArgs> AfterRollback;

        public EntityTransaction(Database database, IEntityTransaction parent = null)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _operations = new List<TransactionOperation>();
            _savepoints = new Dictionary<string, List<TransactionOperation>>();
            
            TransactionId = Guid.NewGuid();
            State = TransactionState.Active;
            Parent = parent;
        }

        public ObjectId AddEntity(Entity entity, string blockName = "ModelSpace")
        {
            EnsureTransactionActive();
            
            lock (_lockObject)
            {
                var operation = new TransactionOperation
                {
                    Type = TransactionOperationType.Add,
                    Entity = entity,
                    BlockName = blockName,
                    Timestamp = DateTime.Now,
                    Description = $"Add {entity.GetType().Name} to {blockName}"
                };
                
                _operations.Add(operation);
                
                // 立即分配实际ID以便后续清除操作
                var block = GetOrCreateBlock(blockName);
                var actualId = block.AppendEntity(entity);
                operation.EntityId = actualId;
                
                return actualId;
            }
        }

        public bool RemoveEntity(ObjectId entityId, string blockName = "ModelSpace")
        {
            EnsureTransactionActive();
            
            lock (_lockObject)
            {
                var block = _database.blockTable[blockName] as Block;
                var entity = FindEntityInBlock(block, entityId);
                
                if (entity == null) return false;

                var operation = new TransactionOperation
                {
                    Type = TransactionOperationType.Remove,
                    EntityId = entityId,
                    OriginalEntity = CloneEntity(entity),
                    BlockName = blockName,
                    Timestamp = DateTime.Now,
                    Description = $"Remove {entity.GetType().Name} from {blockName}"
                };
                
                _operations.Add(operation);
                return true;
            }
        }

        public bool ModifyEntity(ObjectId entityId, Entity newEntity, string blockName = "ModelSpace")
        {
            EnsureTransactionActive();
            
            lock (_lockObject)
            {
                var block = _database.blockTable[blockName] as Block;
                var originalEntity = FindEntityInBlock(block, entityId);
                
                if (originalEntity == null) return false;

                var operation = new TransactionOperation
                {
                    Type = TransactionOperationType.Modify,
                    EntityId = entityId,
                    Entity = CloneEntity(newEntity),
                    OriginalEntity = CloneEntity(originalEntity),
                    BlockName = blockName,
                    Timestamp = DateTime.Now,
                    Description = $"Modify {originalEntity.GetType().Name} in {blockName}"
                };
                
                _operations.Add(operation);
                return true;
            }
        }

        public bool MoveEntity(ObjectId entityId, string fromBlock, string toBlock)
        {
            EnsureTransactionActive();
            
            lock (_lockObject)
            {
                var sourceBlock = _database.blockTable[fromBlock] as Block;
                var entity = FindEntityInBlock(sourceBlock, entityId);
                
                if (entity == null) return false;

                var operation = new TransactionOperation
                {
                    Type = TransactionOperationType.Move,
                    EntityId = entityId,
                    Entity = CloneEntity(entity),
                    BlockName = $"{fromBlock}→{toBlock}",
                    Timestamp = DateTime.Now,
                    Description = $"Move {entity.GetType().Name} from {fromBlock} to {toBlock}"
                };
                
                _operations.Add(operation);
                return true;
            }
        }

        public ObjectId CopyEntity(ObjectId sourceId, string targetBlock = null)
        {
            EnsureTransactionActive();
            
            lock (_lockObject)
            {
                Entity sourceEntity = null;
                string sourceBlockName = null;
                
                // 查找源实体
                foreach (var item in _database.blockTable._items)
                {
                    var block = item as Block;
                    sourceEntity = FindEntityInBlock(block, sourceId);
                    if (sourceEntity != null)
                    {
                        sourceBlockName = block.name;
                        break;
                    }
                }
                
                if (sourceEntity == null) return ObjectId.Null;

                targetBlock = targetBlock ?? sourceBlockName;
                var clonedEntity = CloneEntity(sourceEntity);
                
                return AddEntity(clonedEntity, targetBlock);
            }
        }

        public string CreateSavepoint(string name)
        {
            EnsureTransactionActive();
            
            lock (_lockObject)
            {
                var savepointName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}";
                _savepoints[savepointName] = new List<TransactionOperation>(_operations);
                return savepointName;
            }
        }

        public void RollbackToSavepoint(string savepointName)
        {
            EnsureTransactionActive();
            
            lock (_lockObject)
            {
                if (_savepoints.TryGetValue(savepointName, out var savepointOperations))
                {
                    var operationsToRollback = _operations.Skip(savepointOperations.Count).ToList();
                    
                    foreach (var operation in operationsToRollback.AsEnumerable().Reverse())
                    {
                        RollbackOperation(operation);
                    }
                    
                    _operations.RemoveRange(savepointOperations.Count, 
                        _operations.Count - savepointOperations.Count);
                }
            }
        }

        public void Commit()
        {
            EnsureTransactionActive();
            
            lock (_lockObject)
            {
                try
                {
                    BeforeCommit?.Invoke(this, new TransactionEventArgs(this));
                    
                    foreach (var operation in _operations)
                    {
                        ExecuteOperation(operation);
                    }
                    
                    State = TransactionState.Committed;
                    AfterCommit?.Invoke(this, new TransactionEventArgs(this));
                }
                catch (Exception ex)
                {
                    Rollback();
                    throw new TransactionException($"Transaction commit failed: {ex.Message}", ex);
                }
            }
        }

        public void Rollback()
        {
            if (State == TransactionState.RolledBack || State == TransactionState.Disposed)
                return;
            
            lock (_lockObject)
            {
                try
                {
                    foreach (var operation in _operations.AsEnumerable().Reverse())
                    {
                        RollbackOperation(operation);
                    }
                    
                    State = TransactionState.RolledBack;
                    AfterRollback?.Invoke(this, new TransactionEventArgs(this));
                }
                catch (Exception ex)
                {
                    throw new TransactionException($"Transaction rollback failed: {ex.Message}", ex);
                }
            }
        }

        public IReadOnlyList<TransactionOperation> GetOperationHistory()
        {
            lock (_lockObject)
            {
                return _operations.AsReadOnly();
            }
        }

        private void ExecuteOperation(TransactionOperation operation)
        {
            var block = GetOrCreateBlock(operation.BlockName);
            
            switch (operation.Type)
            {
                case TransactionOperationType.Add:
                    // Entity已经在AddEntity时添加到块中，这里无需重复添加
                    break;
                    
                case TransactionOperationType.Remove:
                    block.RemoveEntityById(operation.EntityId);
                    break;
                    
                case TransactionOperationType.Modify:
                    var existingEntity = FindEntityInBlock(block, operation.EntityId);
                    if (existingEntity != null)
                    {
                        CopyEntityProperties(operation.Entity, existingEntity);
                    }
                    break;
                    
                case TransactionOperationType.Move:
                    var parts = operation.BlockName.Split('→');
                    var sourceBlock = _database.blockTable[parts[0]] as Block;
                    var targetBlock = GetOrCreateBlock(parts[1]);
                    
                    var entity = FindEntityInBlock(sourceBlock, operation.EntityId);
                    if (entity != null)
                    {
                        sourceBlock.RemoveEntityById(operation.EntityId);
                        operation.EntityId = targetBlock.AppendEntity(entity);
                    }
                    break;
            }
        }

        private void RollbackOperation(TransactionOperation operation)
        {
            var block = GetOrCreateBlock(operation.BlockName);
            
            switch (operation.Type)
            {
                case TransactionOperationType.Add:
                    if (!operation.EntityId.isNull)
                    {
                        block.RemoveEntityById(operation.EntityId);
                    }
                    break;
                    
                case TransactionOperationType.Remove:
                    if (operation.OriginalEntity != null)
                    {
                        block.AppendEntity(operation.OriginalEntity);
                    }
                    break;
                    
                case TransactionOperationType.Modify:
                    var entity = FindEntityInBlock(block, operation.EntityId);
                    if (entity != null && operation.OriginalEntity != null)
                    {
                        CopyEntityProperties(operation.OriginalEntity, entity);
                    }
                    break;
            }
        }

        private Block GetOrCreateBlock(string blockName)
        {
            var block = _database.blockTable[blockName] as Block;
            if (block == null)
            {
                block = new Block();
                block.name = blockName;
                _database.blockTable.Add(block);
            }
            return block;
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

        private Entity CloneEntity(Entity entity)
        {
            if (entity == null) return null;
            
            try
            {
                return entity.Clone() as Entity;
            }
            catch
            {
                return entity;
            }
        }

        private void CopyEntityProperties(Entity source, Entity target)
        {
            if (source == null || target == null) return;
            
            try
            {
                target.color = source.color;
                target.lineType = source.lineType;
            }
            catch
            {
                // 忽略属性复制错误
            }
        }

        private void EnsureTransactionActive()
        {
            if (State != TransactionState.Active)
            {
                throw new InvalidOperationException($"Transaction is not active. Current state: {State}");
            }
        }

        public void Dispose()
        {
            if (State == TransactionState.Active)
            {
                Rollback();
            }
            
            State = TransactionState.Disposed;
            _operations.Clear();
            _savepoints.Clear();
        }
    }
}