using System;
using System.Collections.Generic;
using LitMath;
using netDxf.Entities;
using lcdb;

namespace lcdb.NetDxfAdapter
{
    /// <summary>
    /// NetDxf实体适配器基类，将netDxf实体包装为OtoCAD兼容实体
    /// </summary>
    /// <typeparam name="T">netDxf实体类型</typeparam>
    public abstract class NetDxfEntityAdapter<T> : lcdb.Entity where T : EntityObject
    {
        protected T _netDxfEntity;
        private IEntityStrategy<T> _strategy;

        protected NetDxfEntityAdapter()
        {
            // 默认构造函数用于反序列化
        }

        protected NetDxfEntityAdapter(T netDxfEntity)
        {
            _netDxfEntity = netDxfEntity ?? throw new ArgumentNullException(nameof(netDxfEntity));
            _strategy = CreateStrategy();
            
            // 同步基础属性
            SyncFromNetDxf();
        }

        /// <summary>
        /// 创建实体特定的策略对象
        /// </summary>
        protected abstract IEntityStrategy<T> CreateStrategy();

        /// <summary>
        /// 获取底层netDxf实体
        /// </summary>
        public T NetDxfEntity => _netDxfEntity;

        /// <summary>
        /// 从netDxf实体同步属性到OtoCAD实体
        /// </summary>
        protected virtual void SyncFromNetDxf()
        {
            if (_netDxfEntity.Color != null)
            {
                // 转换颜色
                this.color = CoordinateConverter.ToOtoCADColor(_netDxfEntity.Color);
            }
            
            if (_netDxfEntity.Layer != null)
            {
                this.layer = _netDxfEntity.Layer.Name;
            }
        }

        /// <summary>
        /// 同步OtoCAD实体属性到netDxf实体
        /// </summary>
        protected virtual void SyncToNetDxf()
        {
            _netDxfEntity.Color = CoordinateConverter.ToNetDxfColor(this.color);
            
            if (!string.IsNullOrEmpty(this.layer))
            {
                // 需要从图层表获取图层对象
                _netDxfEntity.Layer = new netDxf.Tables.Layer(this.layer);
            }
        }

        #region Entity抽象方法实现

        public override List<GripPoint> GetGripPoints()
        {
            return _strategy.GetGripPoints(_netDxfEntity);
        }

        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            _strategy.SetGripPointAt(_netDxfEntity, index, newPosition);
            SyncFromNetDxf();
        }

        public override void Draw(OtoCAD.IGraphicsDraw gd)
        {
            _strategy.Draw(gd, _netDxfEntity);
        }

        public override Bounding bounding
        {
            get { return _strategy.GetBounding(_netDxfEntity); }
        }

        public override void Translate(Vector2 translation)
        {
            _strategy.Translate(_netDxfEntity, translation);
            SyncFromNetDxf();
        }

        public override void Rotate(Vector2 center, double angle)
        {
            _strategy.Rotate(_netDxfEntity, center, angle);
            SyncFromNetDxf();
        }

        public override void TransformBy(Matrix3 transform)
        {
            _strategy.TransformBy(_netDxfEntity, transform);
            SyncFromNetDxf();
        }

        public virtual void Mirror(Vector2 p1, Vector2 p2)
        {
            _strategy.Mirror(_netDxfEntity, p1, p2);
            SyncFromNetDxf();
        }

        public virtual void Scale(Vector2 center, double scale)
        {
            _strategy.Scale(_netDxfEntity, center, scale);
            SyncFromNetDxf();
        }

        public override object Clone()
        {
            T clonedNetDxfEntity = (T)_netDxfEntity.Clone();
            return CreateClone(clonedNetDxfEntity);
        }

        protected abstract NetDxfEntityAdapter<T> CreateClone(T clonedNetDxfEntity);

        #endregion

        #region 序列化支持

        [System.Obsolete("XML serialization is deprecated, use JSON format", false)]
        public virtual void XmlOut(System.Xml.XmlWriter xmlWriter)
        {
            // 保存netDxf实体的DXF表示
            xmlWriter.WriteStartElement("netDxfEntity");
            xmlWriter.WriteAttributeString("type", typeof(T).Name);
            
            // 这里可以序列化netDxf实体的关键属性
            _strategy.XmlOut(xmlWriter, _netDxfEntity);
            
            xmlWriter.WriteEndElement();
        }

        [System.Obsolete("XML serialization is deprecated, use JSON format", false)]
        public virtual void XmlIn(System.Xml.XmlReader xmlReader)
        {
            // 反序列化netDxf实体
            if (xmlReader.ReadToFollowing("netDxfEntity"))
            {
                _strategy.XmlIn(xmlReader, _netDxfEntity);
            }
            
            // 同步属性
            SyncFromNetDxf();
        }

        #endregion
    }
}