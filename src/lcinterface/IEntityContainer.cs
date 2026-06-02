using System;
using System.Collections.Generic;
using System.Linq;
using lcdb;

namespace OtoCAD.Interface
{
    /// <summary>
    /// 实体容器接口 - 定义管理多个实体的容器行为
    /// </summary>
    public interface IEntityContainer
    {
        /// <summary>
        /// 容器ID
        /// </summary>
        ObjectId ContainerId { get; }

        /// <summary>
        /// 包含的实体数量
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 是否为空
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// 添加实体
        /// </summary>
        void Add(Entity entity);

        /// <summary>
        /// 批量添加实体
        /// </summary>
        void AddRange(IEnumerable<Entity> entities);

        /// <summary>
        /// 移除实体
        /// </summary>
        bool Remove(Entity entity);

        /// <summary>
        /// 通过ID移除实体
        /// </summary>
        bool RemoveById(ObjectId id);

        /// <summary>
        /// 清空所有实体
        /// </summary>
        void Clear();

        /// <summary>
        /// 是否包含指定实体
        /// </summary>
        bool Contains(Entity entity);

        /// <summary>
        /// 通过ID查找实体
        /// </summary>
        Entity FindById(ObjectId id);

        /// <summary>
        /// 获取所有实体
        /// </summary>
        IEnumerable<Entity> GetAll();

        /// <summary>
        /// 按类型获取实体
        /// </summary>
        IEnumerable<T> GetByType<T>() where T : Entity;

        /// <summary>
        /// 按条件查找实体
        /// </summary>
        IEnumerable<Entity> Find(Predicate<Entity> predicate);

        /// <summary>
        /// 实体添加事件
        /// </summary>
        event EventHandler<EntityContainerEventArgs> EntityAdded;

        /// <summary>
        /// 实体移除事件
        /// </summary>
        event EventHandler<EntityContainerEventArgs> EntityRemoved;

        /// <summary>
        /// 容器清空事件
        /// </summary>
        event EventHandler ContainerCleared;
    }

    /// <summary>
    /// 分层实体容器接口
    /// </summary>
    public interface IHierarchicalContainer : IEntityContainer
    {
        /// <summary>
        /// 父容器
        /// </summary>
        IHierarchicalContainer Parent { get; set; }

        /// <summary>
        /// 子容器集合
        /// </summary>
        IReadOnlyList<IHierarchicalContainer> Children { get; }

        /// <summary>
        /// 添加子容器
        /// </summary>
        void AddChild(IHierarchicalContainer child);

        /// <summary>
        /// 移除子容器
        /// </summary>
        bool RemoveChild(IHierarchicalContainer child);

        /// <summary>
        /// 获取层级深度
        /// </summary>
        int GetDepth();

        /// <summary>
        /// 获取根容器
        /// </summary>
        IHierarchicalContainer GetRoot();

        /// <summary>
        /// 遍历所有后代容器
        /// </summary>
        IEnumerable<IHierarchicalContainer> GetDescendants();

        /// <summary>
        /// 遍历所有祖先容器
        /// </summary>
        IEnumerable<IHierarchicalContainer> GetAncestors();
    }

    /// <summary>
    /// 可索引实体容器接口
    /// </summary>
    public interface IIndexedContainer : IEntityContainer
    {
        /// <summary>
        /// 通过索引获取实体
        /// </summary>
        Entity this[int index] { get; set; }

        /// <summary>
        /// 插入实体到指定位置
        /// </summary>
        void Insert(int index, Entity entity);

        /// <summary>
        /// 移除指定位置的实体
        /// </summary>
        void RemoveAt(int index);

        /// <summary>
        /// 获取实体的索引
        /// </summary>
        int IndexOf(Entity entity);

        /// <summary>
        /// 交换两个实体的位置
        /// </summary>
        void Swap(int index1, int index2);

        /// <summary>
        /// 移动实体到指定位置
        /// </summary>
        void Move(int fromIndex, int toIndex);
    }

    /// <summary>
    /// 实体容器事件参数
    /// </summary>
    public class EntityContainerEventArgs : EventArgs
    {
        /// <summary>
        /// 相关实体
        /// </summary>
        public Entity Entity { get; set; }

        /// <summary>
        /// 实体索引（如果适用）
        /// </summary>
        public int Index { get; set; } = -1;

        /// <summary>
        /// 操作类型
        /// </summary>
        public ContainerOperation Operation { get; set; }

        public EntityContainerEventArgs(Entity entity, ContainerOperation operation)
        {
            Entity = entity;
            Operation = operation;
        }
    }

    /// <summary>
    /// 容器操作类型
    /// </summary>
    public enum ContainerOperation
    {
        /// <summary>
        /// 添加
        /// </summary>
        Add,

        /// <summary>
        /// 插入
        /// </summary>
        Insert,

        /// <summary>
        /// 移除
        /// </summary>
        Remove,

        /// <summary>
        /// 替换
        /// </summary>
        Replace,

        /// <summary>
        /// 移动
        /// </summary>
        Move,

        /// <summary>
        /// 清空
        /// </summary>
        Clear
    }

    /// <summary>
    /// 基础实体容器实现
    /// </summary>
    public class EntityContainer : IEntityContainer
    {
        private readonly List<Entity> _entities;
        private readonly Dictionary<ObjectId, Entity> _entityMap;

        public ObjectId ContainerId { get; private set; }
        public int Count => _entities.Count;
        public bool IsEmpty => _entities.Count == 0;

        public event EventHandler<EntityContainerEventArgs> EntityAdded;
        public event EventHandler<EntityContainerEventArgs> EntityRemoved;
        public event EventHandler ContainerCleared;

        public EntityContainer(ObjectId containerId)
        {
            ContainerId = containerId;
            _entities = new List<Entity>();
            _entityMap = new Dictionary<ObjectId, Entity>();
        }

        public void Add(Entity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            _entities.Add(entity);
            if (entity.id != ObjectId.Null)
            {
                _entityMap[entity.id] = entity;
            }
            
            EntityAdded?.Invoke(this, new EntityContainerEventArgs(entity, ContainerOperation.Add));
        }

        public void AddRange(IEnumerable<Entity> entities)
        {
            foreach (var entity in entities)
            {
                Add(entity);
            }
        }

        public bool Remove(Entity entity)
        {
            if (entity == null) return false;
            
            bool removed = _entities.Remove(entity);
            if (removed)
            {
                _entityMap.Remove(entity.id);
                EntityRemoved?.Invoke(this, new EntityContainerEventArgs(entity, ContainerOperation.Remove));
            }
            
            return removed;
        }

        public bool RemoveById(ObjectId id)
        {
            if (_entityMap.TryGetValue(id, out var entity))
            {
                return Remove(entity);
            }
            return false;
        }

        public void Clear()
        {
            _entities.Clear();
            _entityMap.Clear();
            ContainerCleared?.Invoke(this, EventArgs.Empty);
        }

        public bool Contains(Entity entity)
        {
            return _entities.Contains(entity);
        }

        public Entity FindById(ObjectId id)
        {
            return _entityMap.TryGetValue(id, out var entity) ? entity : null;
        }

        public IEnumerable<Entity> GetAll()
        {
            return _entities.AsReadOnly();
        }

        public IEnumerable<T> GetByType<T>() where T : Entity
        {
            return _entities.OfType<T>();
        }

        public IEnumerable<Entity> Find(Predicate<Entity> predicate)
        {
            return _entities.Where(e => predicate(e));
        }
    }

    /// <summary>
    /// 索引容器实现
    /// </summary>
    public class IndexedEntityContainer : EntityContainer, IIndexedContainer
    {
        private readonly List<Entity> _indexedEntities;

        public IndexedEntityContainer(ObjectId containerId) : base(containerId)
        {
            _indexedEntities = new List<Entity>();
        }

        public Entity this[int index]
        {
            get => _indexedEntities[index];
            set
            {
                var oldEntity = _indexedEntities[index];
                _indexedEntities[index] = value;
                
                Remove(oldEntity);
                Add(value);
            }
        }

        public void Insert(int index, Entity entity)
        {
            _indexedEntities.Insert(index, entity);
            Add(entity);
        }

        public void RemoveAt(int index)
        {
            var entity = _indexedEntities[index];
            _indexedEntities.RemoveAt(index);
            Remove(entity);
        }

        public int IndexOf(Entity entity)
        {
            return _indexedEntities.IndexOf(entity);
        }

        public void Swap(int index1, int index2)
        {
            var temp = _indexedEntities[index1];
            _indexedEntities[index1] = _indexedEntities[index2];
            _indexedEntities[index2] = temp;
        }

        public void Move(int fromIndex, int toIndex)
        {
            var entity = _indexedEntities[fromIndex];
            _indexedEntities.RemoveAt(fromIndex);
            _indexedEntities.Insert(toIndex, entity);
        }
    }
}