using System;
using System.Collections.Generic;
using System.Xml;
using LitMath;
using netDxf.Entities;
using lcdb;

namespace lcdb.NetDxfAdapter
{
    /// <summary>
    /// 圆形实体适配器
    /// </summary>
    public class CircleEntityAdapter : NetDxfEntityAdapter<netDxf.Entities.Circle>
    {
        public CircleEntityAdapter() : base()
        {
        }
        public CircleEntityAdapter(netDxf.Entities.Circle netDxfCircle) : base(netDxfCircle)
        {
        }

        public CircleEntityAdapter(Vector2 center, double radius) 
            : base(new netDxf.Entities.Circle(CoordinateConverter.ToNetDxfVector2(center), radius))
        {
        }

        protected override IEntityStrategy<netDxf.Entities.Circle> CreateStrategy()
        {
            return new CircleEntityStrategy();
        }

        protected override NetDxfEntityAdapter<netDxf.Entities.Circle> CreateClone(netDxf.Entities.Circle clonedNetDxfEntity)
        {
            return new CircleEntityAdapter(clonedNetDxfEntity);
        }

        protected override DBObject CreateInstance()
        {
            return new CircleEntityAdapter();
        }

        #region 圆形特定属性

            /// <summary>
            /// 圆心
            /// </summary>
        public Vector2 Center
        {
            get => CoordinateConverter.ToLitMathVector2(_netDxfEntity.Center);
            set
            {
                _netDxfEntity.Center = CoordinateConverter.ToNetDxfVector3(value);
                SyncFromNetDxf();
            }
        }

        /// <summary>
        /// 半径
        /// </summary>
        public double Radius
        {
            get => _netDxfEntity.Radius;
            set
            {
                _netDxfEntity.Radius = value;
                SyncFromNetDxf();
            }
        }

        #endregion
    }

    /// <summary>
    /// 圆形实体策略实现
    /// </summary>
    public class CircleEntityStrategy : EntityStrategyBase<netDxf.Entities.Circle>
    {
        public override List<GripPoint> GetGripPoints(netDxf.Entities.Circle entity)
        {
            var gripPoints = new List<GripPoint>();
            var center = CoordinateConverter.ToLitMathVector2(entity.Center);
            var radius = entity.Radius;
            
            // 中心点
            gripPoints.Add(CreateGripPoint( center, GripPointType.Center));
            
            // 四象限点
            gripPoints.Add(CreateGripPoint(new Vector2(center.X + radius, center.Y), GripPointType.Quad));
            gripPoints.Add(CreateGripPoint(new Vector2(center.X, center.Y + radius), GripPointType.Quad));
            gripPoints.Add(CreateGripPoint(new Vector2(center.X - radius, center.Y), GripPointType.Quad));
            gripPoints.Add(CreateGripPoint(new Vector2(center.X, center.Y - radius), GripPointType.Quad));
            
            return gripPoints;
        }

        public override void SetGripPointAt(netDxf.Entities.Circle entity, int index, Vector2 newPosition)
        {
            var center = CoordinateConverter.ToLitMathVector2(entity.Center);
            
            if (index == 0)
            {
                // 移动圆心
                entity.Center = CoordinateConverter.ToNetDxfVector3(newPosition);
            }
            else if (index >= 1 && index <= 4)
            {
                // 调整半径
                var newRadius = Distance(center, newPosition);
                entity.Radius = newRadius;
            }
        }

        public override void Draw(OtoCAD.IGraphicsDraw gd, netDxf.Entities.Circle entity)
        {
            var center = CoordinateConverter.ToLitMathVector2(entity.Center);
            gd.DrawCircle(center, entity.Radius);
        }

        public override Bounding GetBounding(netDxf.Entities.Circle entity)
        {
            var center = CoordinateConverter.ToLitMathVector2(entity.Center);
            var radius = entity.Radius;
            
            return new Bounding(
                center.X - radius, center.Y - radius,
                center.X + radius, center.Y + radius
            );
        }

        public override List<Vector2> GetSnapPoints(netDxf.Entities.Circle entity)
        {
            var snapPoints = new List<Vector2>();
            var center = CoordinateConverter.ToLitMathVector2(entity.Center);
            var radius = entity.Radius;
            
            // 中心点
            snapPoints.Add(center);
            
            // 四象限点
            snapPoints.Add(new Vector2(center.X + radius, center.Y));
            snapPoints.Add(new Vector2(center.X, center.Y + radius));
            snapPoints.Add(new Vector2(center.X - radius, center.Y));
            snapPoints.Add(new Vector2(center.X, center.Y - radius));
            
            return snapPoints;
        }

        protected override void XmlOutSpecific(XmlWriter xmlWriter, netDxf.Entities.Circle entity)
        {
            var center = CoordinateConverter.ToLitMathVector2(entity.Center);
            xmlWriter.WriteStartElement("center");
            xmlWriter.WriteAttributeString("X", center.X.ToString());
            xmlWriter.WriteAttributeString("Y", center.Y.ToString());
            xmlWriter.WriteEndElement();
            
            xmlWriter.WriteElementString("radius", entity.Radius.ToString());
        }

        protected override void XmlInSpecific(XmlReader xmlReader, netDxf.Entities.Circle entity)
        {
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    switch (xmlReader.Name)
                    {
                        case "center":
                            var x = double.Parse(xmlReader.GetAttribute("X"));
                            var y = double.Parse(xmlReader.GetAttribute("Y"));
                            entity.Center = new netDxf.Vector3(x, y, 0.0);
                            break;
                        case "radius":
                            entity.Radius = xmlReader.ReadElementContentAsDouble();
                            break;
                    }
                }
            }
        }
    }
}