using System;
using System.Collections.Generic;
using System.Linq;
using LitMath;

namespace lcdb.SpatialIndex
{
    /// <summary>
    /// 四叉树实现，用于快速空间查询
    /// </summary>
    public class QuadTree<T> : ISpatialIndex<T> where T : class
    {
        private QuadTreeNode<T> _root;
        private readonly Dictionary<T, QuadTreeItem<T>> _itemsMap;
        private Bounding _bounds;
        
        /// <summary>
        /// 每个节点的最大对象数
        /// </summary>
        private readonly int _maxItemsPerNode;
        
        /// <summary>
        /// 最大树深度
        /// </summary>
        private readonly int _maxDepth;
        
        /// <summary>
        /// 动态扩展边界
        /// </summary>
        private readonly bool _dynamicBounds;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="bounds">初始边界</param>
        /// <param name="maxItemsPerNode">每个节点的最大对象数</param>
        /// <param name="maxDepth">最大深度</param>
        /// <param name="dynamicBounds">是否动态扩展边界</param>
        public QuadTree(Bounding bounds, int maxItemsPerNode = 10, int maxDepth = 8, bool dynamicBounds = true)
        {
            _bounds = bounds;
            _maxItemsPerNode = maxItemsPerNode;
            _maxDepth = maxDepth;
            _dynamicBounds = dynamicBounds;
            _itemsMap = new Dictionary<T, QuadTreeItem<T>>();
            _root = new QuadTreeNode<T>(_bounds, 0, _maxItemsPerNode, _maxDepth);
        }
        
        /// <summary>
        /// 无参构造函数（使用默认边界）
        /// </summary>
        public QuadTree() : this(new Bounding(new Vector2(-10000, -10000), new Vector2(10000, 10000)))
        {
        }
        
        public void Insert(T item, Bounding bounds)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
                
            // 检查是否已存在
            if (_itemsMap.ContainsKey(item))
            {
                Update(item, bounds);
                return;
            }
            
            // 如果边界超出根节点，扩展树
            if (_dynamicBounds && !_bounds.Contains(bounds))
            {
                ExpandBounds(bounds);
            }
            
            var treeItem = new QuadTreeItem<T>(item, bounds);
            _itemsMap[item] = treeItem;
            _root.Insert(treeItem);
        }
        
        public bool Remove(T item)
        {
            if (item == null || !_itemsMap.TryGetValue(item, out var treeItem))
                return false;
                
            _itemsMap.Remove(item);
            return _root.Remove(item);
        }
        
        public void Update(T item, Bounding newBounds)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
                
            // 移除旧的
            if (_itemsMap.TryGetValue(item, out var oldItem))
            {
                _root.Remove(item);
                _itemsMap.Remove(item);
            }
            
            // 插入新的
            Insert(item, newBounds);
        }
        
        public List<T> Query(Bounding queryBounds)
        {
            var results = new List<T>();
            _root.Query(queryBounds, results);
            
            // 去重（因为对象可能存在于多个节点）
            return results.Distinct().ToList();
        }
        
        public List<T> QueryPoint(Vector2 point)
        {
            var results = new List<T>();
            _root.QueryPoint(point, results);
            return results.Distinct().ToList();
        }
        
        public void Query(Bounding queryBounds, Action<T> callback)
        {
            var results = Query(queryBounds);
            foreach (var item in results)
            {
                callback(item);
            }
        }
        
        public void Clear()
        {
            _itemsMap.Clear();
            _root = new QuadTreeNode<T>(_bounds, 0, _maxItemsPerNode, _maxDepth);
        }
        
        public int Count => _itemsMap.Count;
        
        public Bounding Bounds => _bounds;
        
        public void Rebuild()
        {
            var items = _itemsMap.Values.ToList();
            Clear();
            
            // 重新计算边界
            if (_dynamicBounds && items.Count > 0)
            {
                _bounds = items[0].Bounds;
                foreach (var item in items.Skip(1))
                {
                    _bounds.Union(item.Bounds);
                }
                
                // 稍微扩大边界
                var size = _bounds.maxPoint - _bounds.minPoint;
                _bounds = new Bounding(
                    _bounds.minPoint - size * 0.1,
                    _bounds.maxPoint + size * 0.1);
            }
            
            _root = new QuadTreeNode<T>(_bounds, 0, _maxItemsPerNode, _maxDepth);
            
            // 重新插入所有对象
            foreach (var item in items)
            {
                _itemsMap[item.Item] = item;
                _root.Insert(item);
            }
        }
        
        public SpatialIndexStatistics GetStatistics()
        {
            var stats = new SpatialIndexStatistics
            {
                TotalItems = Count,
                NodeCount = 0,
                TreeDepth = 0,
                MaxItemsInNode = 0,
                EmptyNodes = 0
            };
            
            int maxDepth = 0;
            _root.CollectStatistics(stats, ref maxDepth);
            stats.TreeDepth = maxDepth + 1;
            
            if (stats.NodeCount > 0)
            {
                stats.AverageItemsPerNode = (double)stats.TotalItems / stats.NodeCount;
            }
            
            return stats;
        }
        
        /// <summary>
        /// 扩展边界以包含新的边界框
        /// </summary>
        private void ExpandBounds(Bounding newBounds)
        {
            // 计算需要包含的新边界
            var expandedBounds = new Bounding(_bounds);
            expandedBounds.Union(newBounds);
            
            // 稍微扩大以留出空间
            var size = expandedBounds.maxPoint - expandedBounds.minPoint;
            expandedBounds = new Bounding(
                expandedBounds.minPoint - size * 0.1,
                expandedBounds.maxPoint + size * 0.1);
            
            // 如果边界变化很大，重建整个树
            var oldSize = _bounds.maxPoint - _bounds.minPoint;
            var newSize = expandedBounds.maxPoint - expandedBounds.minPoint;
            
            if (newSize.X > oldSize.X * 2 || newSize.Y > oldSize.Y * 2)
            {
                _bounds = expandedBounds;
                Rebuild();
            }
            else
            {
                // 创建新的根节点，将旧根作为子节点
                _bounds = expandedBounds;
                var newRoot = new QuadTreeNode<T>(_bounds, 0, _maxItemsPerNode, _maxDepth);
                
                // 将所有现有对象重新插入
                var allItems = new List<T>();
                _root.GetAllItems(allItems);
                _root = newRoot;
                
                foreach (var item in allItems)
                {
                    if (_itemsMap.TryGetValue(item, out var treeItem))
                    {
                        _root.Insert(treeItem);
                    }
                }
            }
        }
        
        /// <summary>
        /// 获取用于调试的节点边界
        /// </summary>
        public List<Bounding> GetNodeBounds()
        {
            var bounds = new List<Bounding>();
            CollectNodeBounds(_root, bounds);
            return bounds;
        }
        
        private void CollectNodeBounds(QuadTreeNode<T> node, List<Bounding> bounds)
        {
            if (node == null)
                return;
                
            bounds.Add(node.Bounds);
            
            if (!node.IsLeaf)
            {
                // 递归收集子节点边界
                for (int i = 0; i < 4; i++)
                {
                    var child = node.GetType()
                        .GetField("_children", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.GetValue(node) as QuadTreeNode<T>[];
                    
                    if (child?[i] != null)
                    {
                        CollectNodeBounds(child[i], bounds);
                    }
                }
            }
        }
        
        /// <summary>
        /// 节点边界信息
        /// </summary>
        public class NodeBoundsInfo
        {
            public Bounding Bounds { get; set; }
            public bool IsLeaf { get; set; }
            public int ItemCount { get; set; }
            public int Depth { get; set; }
        }
        
        /// <summary>
        /// 获取所有节点的边界（用于可视化）
        /// </summary>
        public List<NodeBoundsInfo> GetNodeBoundsInfo()
        {
            var result = new List<NodeBoundsInfo>();
            if (_root != null)
            {
                CollectNodeBoundsWithInfo(_root, 0, result);
            }
            return result;
        }
        
        private void CollectNodeBoundsWithInfo(QuadTreeNode<T> node, int depth, List<NodeBoundsInfo> result)
        {
            if (node == null) return;
            
            result.Add(new NodeBoundsInfo
            {
                Bounds = node.Bounds,
                IsLeaf = node.IsLeaf,
                ItemCount = node.ItemCount,
                Depth = depth
            });
            
            if (!node.IsLeaf)
            {
                // Use reflection to access private children field
                var childrenField = node.GetType()
                    .GetField("_children", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var children = childrenField?.GetValue(node) as QuadTreeNode<T>[];
                
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        if (child != null)
                            CollectNodeBoundsWithInfo(child, depth + 1, result);
                    }
                }
            }
        }
    }
}