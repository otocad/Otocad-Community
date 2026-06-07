using System;
using lcdb;

namespace lcdb
{
    /// <summary>
    /// 实体来源辅助类
    /// </summary>
    public static class EntitySourceHelper
    {
        /// <summary>
        /// 获取实体来源类型的友好名称
        /// </summary>
        /// <param name="source">实体来源类型</param>
        /// <returns>友好名称字符串</returns>
        public static string GetSourceTypeName(EntitySource source)
        {
            switch (source)
            {
                case EntitySource.Manual:
                    return "手动创建";
                case EntitySource.OpticalElement:
                    return "光学元件生成";
                case EntitySource.OpticalFrame:
                    return "光学框架生成";
                case EntitySource.Material:
                    return "材料属性生成";
                case EntitySource.Coating:
                    return "镀膜属性生成";
                case EntitySource.OtherComponent:
                    return "其他组件生成";
                default:
                    return "未知来源";
            }
        }

        /// <summary>
        /// 判断实体是否为生成的实体（非手动创建）
        /// </summary>
        /// <param name="entity">要检查的实体</param>
        /// <returns>如果是生成的实体返回true，否则返回false</returns>
        public static bool IsGeneratedEntity(Entity entity)
        {
            if (entity == null)
                return false;

            return entity.Source != EntitySource.Manual;
        }

        /// <summary>
        /// 判断实体是否来自特定的源类型
        /// </summary>
        /// <param name="entity">要检查的实体</param>
        /// <param name="source">源类型</param>
        /// <returns>如果实体来自指定源类型返回true，否则返回false</returns>
        public static bool IsFromSource(Entity entity, EntitySource source)
        {
            if (entity == null)
                return false;

            return entity.Source == source;
        }

        /// <summary>
        /// 标记实体为生成的实体
        /// </summary>
        /// <param name="entity">要标记的实体</param>
        /// <param name="source">实体来源类型</param>
        /// <param name="parentId">父组件ID</param>
        public static void MarkAsGenerated(Entity entity, EntitySource source, ObjectId parentId)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (source == EntitySource.Manual)
                throw new ArgumentException("不能将实体标记为手动创建类型", nameof(source));

            entity.Source = source;
            entity.ParentComponentId = parentId;
        }

        /// <summary>
        /// 清除实体的生成标记，恢复为手动创建状态
        /// </summary>
        /// <param name="entity">要清除标记的实体</param>
        public static void ClearGeneratedMark(Entity entity)
        {
            if (entity == null)
                return;

            entity.Source = EntitySource.Manual;
            entity.ParentComponentId = null;
        }

        /// <summary>
        /// 检查实体是否有有效的父组件引用
        /// </summary>
        /// <param name="entity">要检查的实体</param>
        /// <returns>如果有有效的父组件引用返回true，否则返回false</returns>
        public static bool HasValidParentComponent(Entity entity)
        {
            if (entity == null)
                return false;

            return entity.ParentComponentId.HasValue && 
                   !entity.ParentComponentId.Value.isNull &&
                   entity.Source != EntitySource.Manual;
        }

        /// <summary>
        /// 获取实体的父组件（如果存在）
        /// </summary>
        /// <param name="entity">实体</param>
        /// <param name="database">数据库</param>
        /// <returns>父组件对象，如果不存在返回null</returns>
        public static DBObject GetParentComponent(Entity entity, Database database)
        {
            if (!HasValidParentComponent(entity) || database == null)
                return null;

            try
            {
                return database.GetObject(entity.ParentComponentId.Value);
            }
            catch
            {
                return null;
            }
        }
    }
}