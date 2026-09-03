using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Xml;

namespace lcdb
{
    public class LayerTable : DBTable
    {
        /// <summary>
        /// 类名
        /// </summary>
        [JsonIgnore]
        public override string className
        {
            get { return "LayerTable"; }
        }

        /// <summary>
        /// 
        /// </summary>
        private ObjectId _layerZeroId = ObjectId.Null;
        public ObjectId layerZeroId
        {
            get { return _layerZeroId; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        internal LayerTable(Database db)
            : base(db, Database.LayerTableId)
        {
            Layer layerZero = new Layer("0");
            this.Add(layerZero);

            _layerZeroId = layerZero.id;
        }

        /// <summary>
        /// 读XML
        /// </summary>
        
        /// <summary>
        /// 获取所有层
        /// </summary>
        public IEnumerable<Layer> GetAll()
        {
            foreach (var item in this)
            {
                if (item is Layer layer)
                {
                    yield return layer;
                }
            }
        }
    }
}
