using System;
using System.Collections.Generic;
using LitMath;

namespace lcdb.SpatialIndex
{
    /// <summary>
    /// 空间索引接口，用于快速空间查询
    /// </summary>
    public interface ISpatialIndex<T> where T : class
    {
        /// <summary>
        /// 插入对象到空间索引
        /// </summary>
        /// <param name="item">要插入的对象</param>
        /// <param name="bounds">对象的边界框</param>
        void Insert(T item, Bounding bounds);
        
        /// <summary>
        /// 从空间索引中移除对象
        /// </summary>
        /// <param name="item">要移除的对象</param>
        /// <returns>是否成功移除</returns>
        bool Remove(T item);
        
        /// <summary>
        /// 更新对象在空间索引中的位置
        /// </summary>
        /// <param name="item">要更新的对象</param>
        /// <param name="newBounds">新的边界框</param>
        void Update(T item, Bounding newBounds);
        
        /// <summary>
        /// 查询与指定边界框相交的所有对象
        /// </summary>
        /// <param name="queryBounds">查询边界框</param>
        /// <returns>相交的对象列表</returns>
        List<T> Query(Bounding queryBounds);
        
        /// <summary>
        /// 查询包含指定点的所有对象
        /// </summary>
        /// <param name="point">查询点</param>
        /// <returns>包含该点的对象列表</returns>
        List<T> QueryPoint(Vector2 point);
        
        /// <summary>
        /// 查询与指定边界框相交的所有对象（带回调）
        /// </summary>
        /// <param name="queryBounds">查询边界框</param>
        /// <param name="callback">对每个找到的对象执行的回调</param>
        void Query(Bounding queryBounds, Action<T> callback);
        
        /// <summary>
        /// 清空空间索引
        /// </summary>
        void Clear();
        
        /// <summary>
        /// 获取索引中的对象数量
        /// </summary>
        int Count { get; }
        
        /// <summary>
        /// 获取索引的边界框
        /// </summary>
        Bounding Bounds { get; }
        
        /// <summary>
        /// 重建索引（用于批量更新后的优化）
        /// </summary>
        void Rebuild();
        
        /// <summary>
        /// 获取索引统计信息
        /// </summary>
        SpatialIndexStatistics GetStatistics();
    }
    
    /// <summary>
    /// 空间索引统计信息
    /// </summary>
    public class SpatialIndexStatistics
    {
        /// <summary>
        /// 总对象数
        /// </summary>
        public int TotalItems { get; set; }
        
        /// <summary>
        /// 节点总数
        /// </summary>
        public int NodeCount { get; set; }
        
        /// <summary>
        /// 树的深度
        /// </summary>
        public int TreeDepth { get; set; }
        
        /// <summary>
        /// 平均每个节点的对象数
        /// </summary>
        public double AverageItemsPerNode { get; set; }
        
        /// <summary>
        /// 最大节点对象数
        /// </summary>
        public int MaxItemsInNode { get; set; }
        
        /// <summary>
        /// 空节点数
        /// </summary>
        public int EmptyNodes { get; set; }
        
        public override string ToString()
        {
            return $"SpatialIndex[Items={TotalItems}, Nodes={NodeCount}, Depth={TreeDepth}, Avg={AverageItemsPerNode:F1}]";
        }
    }
}