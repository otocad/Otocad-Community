using System;
using System.Collections.Generic;
using LitMath;

namespace lcdb.SpatialIndex
{
    /// <summary>
    /// 四叉树节点
    /// </summary>
    internal class QuadTreeNode<T> where T : class
    {
        /// <summary>
        /// 节点的边界
        /// </summary>
        public Bounding Bounds { get; private set; }
        
        /// <summary>
        /// 节点深度
        /// </summary>
        public int Depth { get; private set; }
        
        /// <summary>
        /// 子节点（西北、东北、西南、东南）
        /// </summary>
        private QuadTreeNode<T>[] _children;
        
        /// <summary>
        /// 节点中的对象列表
        /// </summary>
        private List<QuadTreeItem<T>> _items;
        
        /// <summary>
        /// 最大对象数量（超过此数量将分裂）
        /// </summary>
        private readonly int _maxItems;
        
        /// <summary>
        /// 最大深度
        /// </summary>
        private readonly int _maxDepth;
        
        /// <summary>
        /// 是否是叶子节点
        /// </summary>
        public bool IsLeaf => _children == null;
        
        /// <summary>
        /// 节点中的对象数量
        /// </summary>
        public int ItemCount => _items?.Count ?? 0;
        
        public QuadTreeNode(Bounding bounds, int depth, int maxItems, int maxDepth)
        {
            Bounds = bounds;
            Depth = depth;
            _maxItems = maxItems;
            _maxDepth = maxDepth;
            _items = new List<QuadTreeItem<T>>();
        }
        
        /// <summary>
        /// 插入对象
        /// </summary>
        public void Insert(QuadTreeItem<T> item)
        {
            // 如果对象边界不与节点相交，不插入
            if (!Bounds.IntersectWith(item.Bounds))
                return;
                
            // 如果是叶子节点
            if (IsLeaf)
            {
                _items.Add(item);
                
                // 检查是否需要分裂
                if (_items.Count > _maxItems && Depth < _maxDepth)
                {
                    Subdivide();
                }
            }
            else
            {
                // 插入到子节点
                InsertIntoChildren(item);
            }
        }
        
        /// <summary>
        /// 移除对象
        /// </summary>
        public bool Remove(T item)
        {
            if (IsLeaf)
            {
                return _items.RemoveAll(i => ReferenceEquals(i.Item, item)) > 0;
            }
            else
            {
                bool removed = false;
                foreach (var child in _children)
                {
                    if (child != null && child.Remove(item))
                    {
                        removed = true;
                    }
                }
                
                // 检查是否可以合并子节点
                TryMerge();
                return removed;
            }
        }
        
        /// <summary>
        /// 查询与边界相交的对象
        /// </summary>
        public void Query(Bounding queryBounds, List<T> results)
        {
            // 如果查询边界不与节点相交，直接返回
            if (!Bounds.IntersectWith(queryBounds))
                return;
                
            if (IsLeaf)
            {
                // 检查每个对象
                foreach (var item in _items)
                {
                    if (item.Bounds.IntersectWith(queryBounds))
                    {
                        results.Add(item.Item);
                    }
                }
            }
            else
            {
                // 递归查询子节点
                foreach (var child in _children)
                {
                    child?.Query(queryBounds, results);
                }
            }
        }
        
        /// <summary>
        /// 查询包含点的对象
        /// </summary>
        public void QueryPoint(Vector2 point, List<T> results)
        {
            // 如果点不在节点内，直接返回
            if (!Bounds.Contains(point))
                return;
                
            if (IsLeaf)
            {
                foreach (var item in _items)
                {
                    if (item.Bounds.Contains(point))
                    {
                        results.Add(item.Item);
                    }
                }
            }
            else
            {
                // 找到包含该点的子节点
                int index = GetChildIndex(point);
                _children[index]?.QueryPoint(point, results);
            }
        }
        
        /// <summary>
        /// 分裂节点
        /// </summary>
        private void Subdivide()
        {
            var center = Bounds.center;
            var halfSize = (Bounds.maxPoint - Bounds.minPoint) * 0.5;
            
            _children = new QuadTreeNode<T>[4];
            
            // 创建四个子节点
            // 0: 西北 (左上)
            _children[0] = new QuadTreeNode<T>(
                new Bounding(
                    new Vector2(Bounds.minPoint.X, center.Y),
                    new Vector2(center.X, Bounds.maxPoint.Y)),
                Depth + 1, _maxItems, _maxDepth);
                
            // 1: 东北 (右上)
            _children[1] = new QuadTreeNode<T>(
                new Bounding(
                    center,
                    Bounds.maxPoint),
                Depth + 1, _maxItems, _maxDepth);
                
            // 2: 西南 (左下)
            _children[2] = new QuadTreeNode<T>(
                new Bounding(
                    Bounds.minPoint,
                    center),
                Depth + 1, _maxItems, _maxDepth);
                
            // 3: 东南 (右下)
            _children[3] = new QuadTreeNode<T>(
                new Bounding(
                    new Vector2(center.X, Bounds.minPoint.Y),
                    new Vector2(Bounds.maxPoint.X, center.Y)),
                Depth + 1, _maxItems, _maxDepth);
            
            // 将现有对象重新插入子节点
            var items = _items;
            _items = null;
            
            foreach (var item in items)
            {
                InsertIntoChildren(item);
            }
        }
        
        /// <summary>
        /// 插入对象到子节点
        /// </summary>
        private void InsertIntoChildren(QuadTreeItem<T> item)
        {
            // 对象可能跨越多个子节点
            foreach (var child in _children)
            {
                if (child != null && child.Bounds.IntersectWith(item.Bounds))
                {
                    child.Insert(item);
                }
            }
        }
        
        /// <summary>
        /// 获取包含指定点的子节点索引
        /// </summary>
        private int GetChildIndex(Vector2 point)
        {
            var center = Bounds.center;
            
            if (point.X < center.X)
            {
                return point.Y < center.Y ? 2 : 0; // 西南 : 西北
            }
            else
            {
                return point.Y < center.Y ? 3 : 1; // 东南 : 东北
            }
        }
        
        /// <summary>
        /// 尝试合并子节点
        /// </summary>
        private void TryMerge()
        {
            if (IsLeaf)
                return;
                
            // 计算所有子节点的对象总数
            int totalItems = 0;
            foreach (var child in _children)
            {
                if (child != null)
                {
                    if (!child.IsLeaf)
                        return; // 如果有非叶子节点，不能合并
                    totalItems += child.ItemCount;
                }
            }
            
            // 如果总数小于等于最大数量，合并
            if (totalItems <= _maxItems)
            {
                _items = new List<QuadTreeItem<T>>(totalItems);
                
                foreach (var child in _children)
                {
                    if (child != null && child._items != null)
                    {
                        _items.AddRange(child._items);
                    }
                }
                
                _children = null;
            }
        }
        
        /// <summary>
        /// 获取所有对象
        /// </summary>
        public void GetAllItems(List<T> results)
        {
            if (IsLeaf)
            {
                foreach (var item in _items)
                {
                    results.Add(item.Item);
                }
            }
            else
            {
                foreach (var child in _children)
                {
                    child?.GetAllItems(results);
                }
            }
        }
        
        /// <summary>
        /// 收集统计信息
        /// </summary>
        public void CollectStatistics(SpatialIndexStatistics stats, ref int maxDepth)
        {
            stats.NodeCount++;
            maxDepth = Math.Max(maxDepth, Depth);
            
            if (IsLeaf)
            {
                if (_items.Count == 0)
                    stats.EmptyNodes++;
                else
                {
                    stats.MaxItemsInNode = Math.Max(stats.MaxItemsInNode, _items.Count);
                }
            }
            else
            {
                foreach (var child in _children)
                {
                    child?.CollectStatistics(stats, ref maxDepth);
                }
            }
        }
    }
    
    /// <summary>
    /// 四叉树项（包含对象和其边界）
    /// </summary>
    internal class QuadTreeItem<T> where T : class
    {
        public T Item { get; set; }
        public Bounding Bounds { get; set; }
        
        public QuadTreeItem(T item, Bounding bounds)
        {
            Item = item;
            Bounds = bounds;
        }
    }
}