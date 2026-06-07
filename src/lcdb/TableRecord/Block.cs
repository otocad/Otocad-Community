using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Reflection;
using lcdb.SpatialIndex;

namespace lcdb
{
    public class Block : DBTableRecord
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className
        {
            get { return "Block"; }
        }

        private List<Entity> _items = new List<Entity>();
        private ISpatialIndex<Entity> _spatialIndex;
        private bool _useSpatialIndex = true;

        public string E { get; set; } = "E";
        public List<Entity> Entities => _items;
        
        /// <summary>
        /// 获取空间索引（延迟初始化）
        /// </summary>
        public ISpatialIndex<Entity> SpatialIndex
        {
            get
            {
                if (_spatialIndex == null && _useSpatialIndex)
                {
                    InitializeSpatialIndex();
                }
                return _spatialIndex;
            }
        }
        
        /// <summary>
        /// 是否启用空间索引
        /// </summary>
        public bool UseSpatialIndex
        {
            get => _useSpatialIndex;
            set
            {
                if (_useSpatialIndex != value)
                {
                    _useSpatialIndex = value;
                    if (value && _spatialIndex == null)
                    {
                        InitializeSpatialIndex();
                    }
                    else if (!value)
                    {
                        _spatialIndex = null;
                    }
                }
            }
        }
        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Block block = base.Clone() as Block;
            foreach (Entity item in _items)
            {
                Entity itemCopy = item.Clone() as Entity;
                block.AppendEntity(itemCopy);
            }

            return block;
        }

        protected override DBObject CreateInstance()
        {
            return new Block();
        }

        /// <summary>
        /// 添加图元
        /// </summary>
        public ObjectId AppendEntity(Entity entity)
        {
            if (entity.id != ObjectId.Null)
            {
                // Entity already has an ID, it's already added somewhere
                // Return the existing ID instead of throwing
                return entity.id;
            }

            var objId = _AppendEntity(entity);
            return objId;
        }

        private ObjectId _AppendEntity(Entity entity)
        {
            if (this.id == ObjectId.Null)
            {
                _items.Add(entity);
                entity.SetParent(this);
            }
            else
            {
                _items.Add(entity);
                entity.SetParent(this);
                this.database.IdentifyObject(entity);
            }
            
            // 更新空间索引
            if (_spatialIndex != null && entity.bounding.IsValid)
            {
                _spatialIndex.Insert(entity, entity.bounding);
            }

            return entity.id;
        }

        /// <summary>
        /// 删除图元
        /// </summary>
        public void RemoveEntity(Entity entity)
        {
            _items.Remove(entity);
            
            // 从空间索引中移除
            _spatialIndex?.Remove(entity);
        }
        /// <summary>
        /// 删除图元
        /// </summary>
        internal void RemoveEntityById(ObjectId id)
        {
            var entity = _items.FirstOrDefault(x => x.id == id);
            if (entity != null)
            {
                _items.Remove(entity);
                _spatialIndex?.Remove(entity);
            }
        }
        /// <summary>
        /// 清空图元
        /// </summary>
        internal void Clear()
        {
            _items.Clear();
            _spatialIndex?.Clear();
        }

        /// <summary>
        /// 写XML
        /// </summary>

        /// <summary>
        /// 读XML
        /// </summary>

        #region IDBObjectContainer
        public IEnumerator<Entity> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        #endregion
        
        /// <summary>
        /// 初始化空间索引
        /// </summary>
        private void InitializeSpatialIndex()
        {
            // 计算所有实体的边界
            Bounding bounds = Bounding.Empty;
            bool hasBounds = false;
            foreach (var entity in _items)
            {
                if (entity.bounding.IsValid)
                {
                    if (!hasBounds)
                    {
                        bounds = new Bounding(entity.bounding);
                        hasBounds = true;
                    }
                    else
                        bounds.Union(entity.bounding);
                }
            }
            
            // 如果没有实体或没有边界，使用默认边界
            if (!hasBounds)
            {
                bounds = new Bounding(new LitMath.Vector2(-10000, -10000), new LitMath.Vector2(10000, 10000));
            }
            else
            {
                // 稍微扩大边界
                var size = bounds.maxPoint - bounds.minPoint;
                bounds = new Bounding(
                    bounds.minPoint - size * 0.1,
                    bounds.maxPoint + size * 0.1);
            }
            
            // 创建四叉树
            _spatialIndex = new QuadTree<Entity>(bounds, 10, 8, true);
            
            // 插入所有实体
            foreach (var entity in _items)
            {
                if (entity.bounding.IsValid)
                {
                    _spatialIndex.Insert(entity, entity.bounding);
                }
            }
        }
        
        /// <summary>
        /// 使用空间索引查询与边界相交的实体
        /// </summary>
        public List<Entity> QueryEntities(Bounding queryBounds)
        {
            if (_spatialIndex != null && _useSpatialIndex)
            {
                return _spatialIndex.Query(queryBounds);
            }
            else
            {
                // 回退到线性搜索
                var results = new List<Entity>();
                foreach (var entity in _items)
                {
                    if (entity.bounding.IsValid && entity.bounding.IntersectWith(queryBounds))
                    {
                        results.Add(entity);
                    }
                }
                return results;
            }
        }
        
        /// <summary>
        /// 使用空间索引查询包含点的实体
        /// </summary>
        public List<Entity> QueryEntitiesAtPoint(LitMath.Vector2 point)
        {
            if (_spatialIndex != null && _useSpatialIndex)
            {
                return _spatialIndex.QueryPoint(point);
            }
            else
            {
                // 回退到线性搜索
                var results = new List<Entity>();
                foreach (var entity in _items)
                {
                    if (entity.bounding.IsValid && entity.bounding.Contains(point))
                    {
                        results.Add(entity);
                    }
                }
                return results;
            }
        }
        
        /// <summary>
        /// 更新实体在空间索引中的位置
        /// </summary>
        public void UpdateEntitySpatialIndex(Entity entity)
        {
            if (_spatialIndex != null && entity.bounding.IsValid)
            {
                _spatialIndex.Update(entity, entity.bounding);
            }
        }
        
        /// <summary>
        /// 重建空间索引
        /// </summary>
        public void RebuildSpatialIndex()
        {
            if (_useSpatialIndex)
            {
                _spatialIndex = null;
                InitializeSpatialIndex();
            }
        }
    }
}
