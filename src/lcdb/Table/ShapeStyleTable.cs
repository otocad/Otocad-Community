using System;
using System.Collections.Generic;

namespace lcdb
{
    /// <summary>
    /// 形状样式表
    /// </summary>
    public class ShapeStyleTable : DBTable
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className
        {
            get { return "ShapeStyleTable"; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        internal ShapeStyleTable(Database db)
            : base(db, Database.ShapeStyleTableId)
        {
            // 添加默认的线型形状样式
            ShapeStyle defaultStyle = ShapeStyle.CreateLinetypeShapeStyle();
            this.Add(defaultStyle);
        }

        /// <summary>
        /// 通过形状名称查找包含该形状的样式
        /// </summary>
        public ShapeStyle FindStyleContainingShape(string shapeName)
        {
            if (string.IsNullOrEmpty(shapeName))
                return null;

            foreach (DBTableRecord record in _items)
            {
                ShapeStyle style = record as ShapeStyle;
                if (style != null && style.ContainsShapeName(shapeName))
                {
                    return style;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取所有可用的形状名称
        /// </summary>
        public IEnumerable<string> GetAllShapeNames()
        {
            HashSet<string> allNames = new HashSet<string>();

            foreach (DBTableRecord record in _items)
            {
                ShapeStyle style = record as ShapeStyle;
                if (style != null)
                {
                    foreach (string name in style.GetShapeNames())
                    {
                        allNames.Add(name);
                    }
                }
            }

            return allNames;
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            throw new NotImplementedException("ShapeStyleTable cannot be cloned");
        }
    }
}