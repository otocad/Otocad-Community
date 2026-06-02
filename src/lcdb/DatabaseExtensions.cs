using System;
using System.Collections.Generic;
using System.Linq;
using lcdb;
using lcdb;
using lcdb.SpatialIndex;

namespace lcdb.Extensions
{
    /// <summary>
    /// 数据库扩展 - 支持组合实体
    /// </summary>
    public static class DatabaseExtensions
    {
        #region 字段

        /// <summary>
        /// 组合实体索引（每个数据库一个索引）
        /// </summary>
        private static readonly Dictionary<Database, CompositeEntityIndex> _compositeIndexes = new Dictionary<Database, CompositeEntityIndex>();

        /// <summary>
        /// 事务管理器（每个数据库一个）
        /// </summary>
        private static readonly Dictionary<Database, TransactionManager> _transactionManagers = new Dictionary<Database, TransactionManager>();

        #endregion

        #region 组合实体索引管理

        /// <summary>
        /// 获取或创建组合实体索引
        /// </summary>
        public static CompositeEntityIndex GetCompositeEntityIndex(this Database database)
        {
            if (!_compositeIndexes.TryGetValue(database, out var index))
            {
                index = new CompositeEntityIndex(database);
                _compositeIndexes[database] = index;
            }
            return index;
        }

        /// <summary>
        /// 清除组合实体索引
        /// </summary>
        public static void ClearCompositeEntityIndex(this Database database)
        {
            if (_compositeIndexes.ContainsKey(database))
            {
                _compositeIndexes[database].Clear();
                _compositeIndexes.Remove(database);
            }
        }

        #endregion

        #region 添加组合实体

        /// <summary>
        /// 添加组合实体到数据库（简化版）
        /// </summary>
        public static ObjectId AddCompositeEntitySimple(this Database database, CompositeEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            
            // 确保实体有ID
            if (entity.id == ObjectId.Null)
            {
                entity.id = database.AllocateId();
            }
            
            // 准备保存（将子实体添加到数据库）
            entity.PrepareForSave(database);
            
            // 添加组合实体本身
            database.AddEntity(entity);
            
            return entity.id;
        }
        
        /// <summary>
        /// 加载组合实体（自动恢复子实体）
        /// </summary>
        public static CompositeEntity LoadCompositeEntity(this Database database, ObjectId id)
        {
            var entity = database.GetEntity(id) as CompositeEntity;
            if (entity != null)
            {
                // 恢复子实体引用
                entity.RestoreChildEntities(database);
            }
            return entity;
        }
        
        /// <summary>
        /// 添加组合实体到数据库
        /// </summary>
        public static ObjectId AddCompositeEntity(this Database database, CompositeEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var transaction = database.GetTransaction();
            ObjectId id = ObjectId.Null;

            try
            {
                transaction.Start();

                // 添加主实体
                id = database.AddEntity(entity);
                entity.Id = id;

                // 添加到索引
                var index = database.GetCompositeEntityIndex();
                index.Add(entity);

                // 如果需要生成子实体
                if (entity.NeedsRegeneration)
                {
                    entity.RegenerateChildren();
                }

                // 添加所有子实体到数据库
                foreach (var child in entity.ChildEntities)
                {
                    var childId = database.AddEntity(child);
                    entity.ChildEntityIds.Add(childId);
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Abort();
                throw new InvalidOperationException($"添加组合实体失败: {ex.Message}", ex);
            }

            return id;
        }

        /// <summary>
        /// 批量添加组合实体
        /// </summary>
        public static List<ObjectId> AddCompositeEntities(this Database database, IEnumerable<CompositeEntity> entities)
        {
            var ids = new List<ObjectId>();
            var transaction = database.GetTransaction();

            try
            {
                transaction.Start();

                foreach (var entity in entities)
                {
                    var id = database.AddCompositeEntity(entity);
                    ids.Add(id);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Abort();
                throw;
            }

            return ids;
        }

        #endregion

        #region 获取组合实体

        /// <summary>
        /// 获取所有组合实体
        /// </summary>
        public static IEnumerable<CompositeEntity> GetCompositeEntities(this Database database)
        {
            var index = database.GetCompositeEntityIndex();
            return index.GetAll();
        }

        /// <summary>
        /// 根据ID获取组合实体
        /// </summary>
        public static CompositeEntity GetCompositeEntity(this Database database, ObjectId id)
        {
            var index = database.GetCompositeEntityIndex();
            return index.Get(id);
        }

        /// <summary>
        /// 按类型获取组合实体
        /// </summary>
        public static IEnumerable<T> GetCompositeEntitiesByType<T>(this Database database) where T : CompositeEntity
        {
            var index = database.GetCompositeEntityIndex();
            return index.GetByType<T>();
        }

        /// <summary>
        /// 查找组合实体
        /// </summary>
        public static IEnumerable<CompositeEntity> FindCompositeEntities(
            this Database database, 
            Predicate<CompositeEntity> predicate)
        {
            var index = database.GetCompositeEntityIndex();
            return index.Find(predicate);
        }

        /// <summary>
        /// 获取包含指定子实体的组合实体
        /// </summary>
        public static CompositeEntity GetParentCompositeEntity(this Database database, Entity childEntity)
        {
            if (childEntity?.ParentComponentId == null)
                return null;

            return database.GetCompositeEntity(childEntity.ParentComponentId.Value);
        }

        #endregion

        #region 更新和删除

        /// <summary>
        /// 更新组合实体
        /// </summary>
        public static void UpdateCompositeEntity(this Database database, CompositeEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var transaction = database.GetTransaction();

            try
            {
                transaction.Start();

                // 更新索引
                var index = database.GetCompositeEntityIndex();
                index.Update(entity);

                // 如果需要重新生成
                if (entity.NeedsRegeneration)
                {
                    // 删除旧的子实体
                    foreach (var childId in entity.ChildEntityIds)
                    {
                        database.RemoveEntity(childId);
                    }
                    entity.ChildEntityIds.Clear();

                    // 重新生成
                    entity.RegenerateChildren();

                    // 添加新的子实体
                    foreach (var child in entity.ChildEntities)
                    {
                        var childId = database.AddEntity(child);
                        entity.ChildEntityIds.Add(childId);
                    }
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Abort();
                throw;
            }
        }

        /// <summary>
        /// 删除组合实体
        /// </summary>
        public static bool RemoveCompositeEntity(this Database database, ObjectId id)
        {
            var entity = database.GetCompositeEntity(id);
            if (entity == null)
                return false;

            var transaction = database.GetTransaction();

            try
            {
                transaction.Start();

                // 删除所有子实体
                foreach (var childId in entity.ChildEntityIds)
                {
                    database.RemoveEntity(childId);
                }

                // 从索引中移除
                var index = database.GetCompositeEntityIndex();
                index.Remove(id);

                // 删除主实体
                database.RemoveEntity(id);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Abort();
                throw;
            }
        }

        #endregion

        #region 查询优化

        /// <summary>
        /// 在指定区域内查找组合实体
        /// </summary>
        public static IEnumerable<CompositeEntity> GetCompositeEntitiesInRegion(
            this Database database, 
            Bounding region)
        {
            var index = database.GetCompositeEntityIndex();
            return index.GetInRegion(region);
        }

        /// <summary>
        /// 获取组合实体统计信息
        /// </summary>
        public static CompositeEntityStatistics GetCompositeEntityStatistics(this Database database)
        {
            var index = database.GetCompositeEntityIndex();
            var entities = index.GetAll();

            return new CompositeEntityStatistics
            {
                TotalCount = entities.Count(),
                TypeCounts = entities.GroupBy(e => e.GetType().Name)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TotalChildEntities = entities.Sum(e => e.ChildCount),
                AverageChildCount = entities.Any() ? entities.Average(e => e.ChildCount) : 0
            };
        }

        #endregion

        #region 事务支持

        /// <summary>
        /// 获取事务管理器
        /// </summary>
        public static TransactionManager GetTransaction(this Database database)
        {
            if (!_transactionManagers.TryGetValue(database, out var manager))
            {
                manager = new TransactionManager(database);
                _transactionManagers[database] = manager;
            }
            return manager;
        }

        /// <summary>
        /// 在事务中执行操作
        /// </summary>
        public static void ExecuteInTransaction(this Database database, Action<Database> action)
        {
            var transaction = database.GetTransaction();

            try
            {
                transaction.Start();
                action(database);
                transaction.Commit();
            }
            catch
            {
                transaction.Abort();
                throw;
            }
        }

        /// <summary>
        /// 在事务中执行操作并返回结果
        /// </summary>
        public static T ExecuteInTransaction<T>(this Database database, Func<Database, T> func)
        {
            var transaction = database.GetTransaction();

            try
            {
                transaction.Start();
                var result = func(database);
                transaction.Commit();
                return result;
            }
            catch
            {
                transaction.Abort();
                throw;
            }
        }

        #endregion
    }

    /// <summary>
    /// 组合实体索引
    /// </summary>
    public class CompositeEntityIndex
    {
        private readonly Database _database;
        private readonly Dictionary<ObjectId, CompositeEntity> _entities;
        private readonly Dictionary<Type, List<CompositeEntity>> _typeIndex;
        private readonly QuadTree<CompositeEntity> _spatialIndex;

        public CompositeEntityIndex(Database database)
        {
            _database = database;
            _entities = new Dictionary<ObjectId, CompositeEntity>();
            _typeIndex = new Dictionary<Type, List<CompositeEntity>>();
            
            // 创建空间索引（假设图纸范围）
            var bounds = new Bounding(
                new LitMath.Vector2(-10000, -10000),
                new LitMath.Vector2(10000, 10000));
            _spatialIndex = new QuadTree<CompositeEntity>(bounds, 4);

            // 初始化索引
            BuildIndex();
        }

        /// <summary>
        /// 构建索引
        /// </summary>
        private void BuildIndex()
        {
            // 扫描数据库中的所有实体，找出组合实体
            var blockTable = _database.blockTable;
            if (blockTable == null)
                return;

            foreach (var block in blockTable)
            {
                foreach (var entityId in block.EntityIds)
                {
                    var entity = _database.GetObject(entityId) as CompositeEntity;
                    if (entity != null)
                    {
                        Add(entity);
                    }
                }
            }
        }

        /// <summary>
        /// 添加到索引
        /// </summary>
        public void Add(CompositeEntity entity)
        {
            if (entity == null || entity.Id == ObjectId.Null)
                return;

            // 添加到ID索引
            _entities[entity.Id] = entity;

            // 添加到类型索引
            var type = entity.GetType();
            if (!_typeIndex.ContainsKey(type))
                _typeIndex[type] = new List<CompositeEntity>();
            _typeIndex[type].Add(entity);

            // 添加到空间索引
            var bounds = entity.bounding;
            if (bounds != null)
            {
                _spatialIndex.Insert(entity, bounds);
            }
        }

        /// <summary>
        /// 从索引中移除
        /// </summary>
        public void Remove(ObjectId id)
        {
            if (_entities.TryGetValue(id, out var entity))
            {
                _entities.Remove(id);

                // 从类型索引中移除
                var type = entity.GetType();
                if (_typeIndex.TryGetValue(type, out var list))
                {
                    list.Remove(entity);
                }

                // 从空间索引中移除
                _spatialIndex.Remove(entity);
            }
        }

        /// <summary>
        /// 更新索引
        /// </summary>
        public void Update(CompositeEntity entity)
        {
            Remove(entity.Id);
            Add(entity);
        }

        /// <summary>
        /// 获取实体
        /// </summary>
        public CompositeEntity Get(ObjectId id)
        {
            return _entities.TryGetValue(id, out var entity) ? entity : null;
        }

        /// <summary>
        /// 获取所有实体
        /// </summary>
        public IEnumerable<CompositeEntity> GetAll()
        {
            return _entities.Values;
        }

        /// <summary>
        /// 按类型获取
        /// </summary>
        public IEnumerable<T> GetByType<T>() where T : CompositeEntity
        {
            if (_typeIndex.TryGetValue(typeof(T), out var list))
            {
                return list.Cast<T>();
            }
            return Enumerable.Empty<T>();
        }

        /// <summary>
        /// 查找实体
        /// </summary>
        public IEnumerable<CompositeEntity> Find(Predicate<CompositeEntity> predicate)
        {
            return _entities.Values.Where(e => predicate(e));
        }

        /// <summary>
        /// 在区域内查找
        /// </summary>
        public IEnumerable<CompositeEntity> GetInRegion(Bounding region)
        {
            return _spatialIndex.Query(region);
        }

        /// <summary>
        /// 清空索引
        /// </summary>
        public void Clear()
        {
            _entities.Clear();
            _typeIndex.Clear();
            _spatialIndex.Clear();
        }
    }

    /// <summary>
    /// 组合实体统计信息
    /// </summary>
    public class CompositeEntityStatistics
    {
        /// <summary>
        /// 总数量
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 各类型数量
        /// </summary>
        public Dictionary<string, int> TypeCounts { get; set; }

        /// <summary>
        /// 子实体总数
        /// </summary>
        public int TotalChildEntities { get; set; }

        /// <summary>
        /// 平均子实体数
        /// </summary>
        public double AverageChildCount { get; set; }

        public CompositeEntityStatistics()
        {
            TypeCounts = new Dictionary<string, int>();
        }

        /// <summary>
        /// 获取摘要
        /// </summary>
        public string GetSummary()
        {
            var summary = $"组合实体统计:\n";
            summary += $"  总数: {TotalCount}\n";
            summary += $"  子实体总数: {TotalChildEntities}\n";
            summary += $"  平均子实体数: {AverageChildCount:F2}\n";
            
            if (TypeCounts.Count > 0)
            {
                summary += "  类型分布:\n";
                foreach (var kvp in TypeCounts.OrderByDescending(x => x.Value))
                {
                    summary += $"    {kvp.Key}: {kvp.Value}\n";
                }
            }

            return summary;
        }
    }

    /// <summary>
    /// 事务管理器
    /// </summary>
    public class TransactionManager
    {
        private readonly Database _database;
        private readonly Stack<Transaction> _transactions;
        private Transaction _currentTransaction;

        public TransactionManager(Database database)
        {
            _database = database;
            _transactions = new Stack<Transaction>();
        }

        /// <summary>
        /// 开始事务
        /// </summary>
        public Transaction Start()
        {
            var transaction = new Transaction(_database);
            _transactions.Push(transaction);
            _currentTransaction = transaction;
            transaction.Begin();
            return transaction;
        }

        /// <summary>
        /// 提交当前事务
        /// </summary>
        public void Commit()
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Commit();
                _transactions.Pop();
                _currentTransaction = _transactions.Count > 0 ? _transactions.Peek() : null;
            }
        }

        /// <summary>
        /// 回滚当前事务
        /// </summary>
        public void Abort()
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Rollback();
                _transactions.Pop();
                _currentTransaction = _transactions.Count > 0 ? _transactions.Peek() : null;
            }
        }

        /// <summary>
        /// 获取当前事务
        /// </summary>
        public Transaction Current => _currentTransaction;

        /// <summary>
        /// 是否在事务中
        /// </summary>
        public bool InTransaction => _currentTransaction != null;
    }

    /// <summary>
    /// 事务
    /// </summary>
    public class Transaction : IDisposable
    {
        private readonly Database _database;
        private readonly List<IUndoableOperation> _operations;
        private bool _committed;
        private bool _disposed;

        public Transaction(Database database)
        {
            _database = database;
            _operations = new List<IUndoableOperation>();
            _committed = false;
            _disposed = false;
        }

        /// <summary>
        /// 开始事务
        /// </summary>
        public void Begin()
        {
            // 记录开始状态
        }

        /// <summary>
        /// 添加操作
        /// </summary>
        public void AddOperation(IUndoableOperation operation)
        {
            _operations.Add(operation);
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public void Commit()
        {
            if (_committed)
                return;

            // 执行所有操作
            foreach (var operation in _operations)
            {
                operation.Execute();
            }

            _committed = true;
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public void Rollback()
        {
            if (_committed)
                return;

            // 撤销所有操作（反向顺序）
            for (int i = _operations.Count - 1; i >= 0; i--)
            {
                _operations[i].Undo();
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                if (!_committed)
                {
                    Rollback();
                }
                _operations.Clear();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 可撤销操作接口
    /// </summary>
    public interface IUndoableOperation
    {
        /// <summary>
        /// 执行操作
        /// </summary>
        void Execute();

        /// <summary>
        /// 撤销操作
        /// </summary>
        void Undo();
    }
}