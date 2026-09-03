using System;
using System.Collections.Generic;
using System.Xml;
using LitMath;
using netDxf.Entities;
using lcdb;

namespace lcdb.NetDxfAdapter
{
    /// <summary>
    /// 直线实体适配器
    /// </summary>
    public class LineEntityAdapter : NetDxfEntityAdapter<netDxf.Entities.Line>
    {
        public LineEntityAdapter(netDxf.Entities.Line netDxfLine) : base(netDxfLine)
        {
        }

        protected override DBObject CreateInstance()
        {
            return new LineEntityAdapter(new netDxf.Entities.Line());
        }

        public LineEntityAdapter(Vector2 startPoint, Vector2 endPoint) 
            : base(new netDxf.Entities.Line(CoordinateConverter.ToNetDxfVector3(startPoint), 
                                            CoordinateConverter.ToNetDxfVector3(endPoint)))
        {
        }

        protected override IEntityStrategy<netDxf.Entities.Line> CreateStrategy()
        {
            return new LineEntityStrategy();
        }

        protected override NetDxfEntityAdapter<netDxf.Entities.Line> CreateClone(netDxf.Entities.Line clonedNetDxfEntity)
        {
            return new LineEntityAdapter(clonedNetDxfEntity);
        }

        #region 直线特定属性

        /// <summary>
        /// 起点
        /// </summary>
        public Vector2 StartPoint
        {
            get => CoordinateConverter.ToLitMathVector2(_netDxfEntity.StartPoint);
            set
            {
                _netDxfEntity.StartPoint = CoordinateConverter.ToNetDxfVector3(value);
                SyncFromNetDxf();
            }
        }

        /// <summary>
        /// 终点
        /// </summary>
        public Vector2 EndPoint
        {
            get => CoordinateConverter.ToLitMathVector2(_netDxfEntity.EndPoint);
            set
            {
                _netDxfEntity.EndPoint = CoordinateConverter.ToNetDxfVector3(value);
                SyncFromNetDxf();
            }
        }

        /// <summary>
        /// 直线长度
        /// </summary>
        public double Length
        {
            get 
            {
                var start = StartPoint;
                var end = EndPoint;
                var dx = end.X - start.X;
                var dy = end.Y - start.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }
        }

        /// <summary>
        /// 中点
        /// </summary>
        public Vector2 MidPoint
        {
            get
            {
                var start = StartPoint;
                var end = EndPoint;
                return new Vector2((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            }
        }

        #endregion
    }

    /// <summary>
    /// 直线实体策略实现
    /// </summary>
    public class LineEntityStrategy : EntityStrategyBase<netDxf.Entities.Line>
    {
        public override List<GripPoint> GetGripPoints(netDxf.Entities.Line entity)
        {
            var gripPoints = new List<GripPoint>();
            var startPoint = CoordinateConverter.ToLitMathVector2(entity.StartPoint);
            var endPoint = CoordinateConverter.ToLitMathVector2(entity.EndPoint);
            
            // 起点和终点
            gripPoints.Add(CreateGripPoint(startPoint, GripPointType.End));
            gripPoints.Add(CreateGripPoint(endPoint, GripPointType.End));
            
            // 中点
            var midPoint = new Vector2((startPoint.X + endPoint.X) / 2, (startPoint.Y + endPoint.Y) / 2);
            gripPoints.Add(CreateGripPoint(midPoint, GripPointType.Mid));
            
            return gripPoints;
        }

        public override void SetGripPointAt(netDxf.Entities.Line entity, int index, Vector2 newPosition)
        {
            var startPoint = CoordinateConverter.ToLitMathVector2(entity.StartPoint);
            var endPoint = CoordinateConverter.ToLitMathVector2(entity.EndPoint);
            
            switch (index)
            {
                case 0: // 起点
                    entity.StartPoint = CoordinateConverter.ToNetDxfVector3(newPosition);
                    break;
                case 1: // 终点
                    entity.EndPoint = CoordinateConverter.ToNetDxfVector3(newPosition);
                    break;
                case 2: // 中点 - 整体移动
                    var currentMidPoint = new Vector2((startPoint.X + endPoint.X) / 2, (startPoint.Y + endPoint.Y) / 2);
                    var offset = new Vector2(newPosition.X - currentMidPoint.X, newPosition.Y - currentMidPoint.Y);
                    entity.StartPoint = CoordinateConverter.ToNetDxfVector3(new Vector2(startPoint.X + offset.X, startPoint.Y + offset.Y));
                    entity.EndPoint = CoordinateConverter.ToNetDxfVector3(new Vector2(endPoint.X + offset.X, endPoint.Y + offset.Y));
                    break;
            }
        }

        public override void Draw(OtoCAD.IGraphicsDraw gd, netDxf.Entities.Line entity)
        {
            var startPoint = CoordinateConverter.ToLitMathVector2(entity.StartPoint);
            var endPoint = CoordinateConverter.ToLitMathVector2(entity.EndPoint);
            gd.DrawLine(startPoint, endPoint);
        }

        public override Bounding GetBounding(netDxf.Entities.Line entity)
        {
            var startPoint = CoordinateConverter.ToLitMathVector2(entity.StartPoint);
            var endPoint = CoordinateConverter.ToLitMathVector2(entity.EndPoint);
            
            return new Bounding(
                Math.Min(startPoint.X, endPoint.X), Math.Min(startPoint.Y, endPoint.Y),
                Math.Max(startPoint.X, endPoint.X), Math.Max(startPoint.Y, endPoint.Y)
            );
        }

        public override List<Vector2> GetSnapPoints(netDxf.Entities.Line entity)
        {
            var snapPoints = new List<Vector2>();
            var startPoint = CoordinateConverter.ToLitMathVector2(entity.StartPoint);
            var endPoint = CoordinateConverter.ToLitMathVector2(entity.EndPoint);
            
            // 端点
            snapPoints.Add(startPoint);
            snapPoints.Add(endPoint);
            
            // 中点
            var midPoint = new Vector2((startPoint.X + endPoint.X) / 2, (startPoint.Y + endPoint.Y) / 2);
            snapPoints.Add(midPoint);
            
            return snapPoints;
        }

        protected override void XmlOutSpecific(XmlWriter xmlWriter, netDxf.Entities.Line entity)
        {
            var startPoint = CoordinateConverter.ToLitMathVector2(entity.StartPoint);
            var endPoint = CoordinateConverter.ToLitMathVector2(entity.EndPoint);
            
            xmlWriter.WriteStartElement("startPoint");
            xmlWriter.WriteAttributeString("X", startPoint.X.ToString());
            xmlWriter.WriteAttributeString("Y", startPoint.Y.ToString());
            xmlWriter.WriteEndElement();
            
            xmlWriter.WriteStartElement("endPoint");
            xmlWriter.WriteAttributeString("X", endPoint.X.ToString());
            xmlWriter.WriteAttributeString("Y", endPoint.Y.ToString());
            xmlWriter.WriteEndElement();
        }

        protected override void XmlInSpecific(XmlReader xmlReader, netDxf.Entities.Line entity)
        {
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    switch (xmlReader.Name)
                    {
                        case "startPoint":
                            var startX = double.Parse(xmlReader.GetAttribute("X"));
                            var startY = double.Parse(xmlReader.GetAttribute("Y"));
                            entity.StartPoint = new netDxf.Vector3(startX, startY, 0.0);
                            break;
                        case "endPoint":
                            var endX = double.Parse(xmlReader.GetAttribute("X"));
                            var endY = double.Parse(xmlReader.GetAttribute("Y"));
                            entity.EndPoint = new netDxf.Vector3(endX, endY, 0.0);
                            break;
                    }
                }
            }
        }
    }
}