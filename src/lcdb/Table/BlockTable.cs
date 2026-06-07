using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Xml;

namespace lcdb
{
    public class BlockTable : DBTable
    {
        /// <summary>
        /// 类名
        /// </summary>
        [JsonIgnore]
        public override string className
        {
            get { return "BlockTable"; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        internal BlockTable(Database db)
            : base(db, Database.BlockTableId)
        {
        }

        /// <summary>
        /// 读XML
        /// </summary>

        internal IEnumerable<object> GetNames()
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// 获取当前块（模型空间）
        /// </summary>
        public Block CurrentBlock
        {
            get { 
                // 尝试使用标准名称"*Model_Space"
                var block = this["*Model_Space"] as Block;
                if (block == null)
                {
                    // 如果找不到，尝试旧名称"ModelSpace"
                    block = this["ModelSpace"] as Block;
                }
                return block;
            }
        }
    }
}
