using System;
using System.Collections.Generic;
using System.Xml;
using LitMath;
using netDxf.Entities;
using lcdb;

namespace lcdb.NetDxfAdapter
{
    /// <summary>
    /// 文本实体适配器，将netDxf.Entities.Text包装为OtoCAD兼容实体
    /// </summary>
    public class TextEntityAdapter : NetDxfEntityAdapter<netDxf.Entities.Text>
    {
        public TextEntityAdapter(netDxf.Entities.Text netDxfText) : base(netDxfText)
        {
        }

        protected override DBObject CreateInstance()
        {
            return new TextEntityAdapter(new netDxf.Entities.Text());
        }

        public TextEntityAdapter(Vector2 position, string text, double height) 
            : base(new netDxf.Entities.Text(text, CoordinateConverter.ToNetDxfVector3(position), height))
        {
        }

        protected override IEntityStrategy<netDxf.Entities.Text> CreateStrategy()
        {
            return new TextEntityStrategy();
        }

        protected override NetDxfEntityAdapter<netDxf.Entities.Text> CreateClone(netDxf.Entities.Text clonedNetDxfEntity)
        {
            return new TextEntityAdapter(clonedNetDxfEntity);
        }

        #region 文本特定属性

        /// <summary>
        /// 文本内容
        /// </summary>
        public string Text
        {
            get => _netDxfEntity.Value;
            set
            {
                _netDxfEntity.Value = value;
                SyncFromNetDxf();
            }
        }

        /// <summary>
        /// 文本高度
        /// </summary>
        public double Height
        {
            get => _netDxfEntity.Height;
            set
            {
                _netDxfEntity.Height = value;
                SyncFromNetDxf();
            }
        }

        /// <summary>
        /// 文本位置
        /// </summary>
        public Vector2 Position
        {
            get => CoordinateConverter.ToLitMathVector2(_netDxfEntity.Position);
            set
            {
                _netDxfEntity.Position = CoordinateConverter.ToNetDxfVector3(value);
                SyncFromNetDxf();
            }
        }

        /// <summary>
        /// 文本旋转角度（弧度）
        /// </summary>
        public double Rotation
        {
            get => CoordinateConverter.DegreesToRadians(_netDxfEntity.Rotation);
            set
            {
                _netDxfEntity.Rotation = CoordinateConverter.RadiansToDegrees(value);
                SyncFromNetDxf();
            }
        }

        /// <summary>
        /// 文本样式
        /// </summary>
        public string TextStyle
        {
            get => _netDxfEntity.Style?.Name ?? "Standard";
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _netDxfEntity.Style = new netDxf.Tables.TextStyle(value);
                    SyncFromNetDxf();
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// 文本实体策略实现
    /// </summary>
    public class TextEntityStrategy : EntityStrategyBase<netDxf.Entities.Text>
    {
        public override List<GripPoint> GetGripPoints(netDxf.Entities.Text entity)
        {
            var gripPoints = new List<GripPoint>();
            
            // 文本只有一个夹点，位于文本的插入点
            var position = CoordinateConverter.ToLitMathVector2(entity.Position);
            gripPoints.Add(CreateGripPoint(position, GripPointType.Node));
            
            return gripPoints;
        }

        public override void SetGripPointAt(netDxf.Entities.Text entity, int index, Vector2 newPosition)
        {
            if (index == 0)
            {
                // 移动文本的插入点
                entity.Position = CoordinateConverter.ToNetDxfVector3(newPosition);
            }
        }

        public override void Draw(OtoCAD.IGraphicsDraw gd, netDxf.Entities.Text entity)
        {
            var position = CoordinateConverter.ToLitMathVector2(entity.Position);
            var rotation = CoordinateConverter.DegreesToRadians(entity.Rotation);
            
            // 使用OtoCAD的文本绘制功能
            gd.DrawText(position, entity.Value, entity.Height, 
                        entity.Style?.Name ?? "Standard", 
                        ConvertTextAlignment(entity.Alignment), 
                        rotation);
        }

        public override Bounding GetBounding(netDxf.Entities.Text entity)
        {
            var position = CoordinateConverter.ToLitMathVector2(entity.Position);
            var height = entity.Height;
            
            // 估算文本宽度（基于字符数和高度）
            var estimatedWidth = entity.Value.Length * height * 0.6;
            
            // 考虑文本旋转的边界计算
            var rotation = CoordinateConverter.DegreesToRadians(entity.Rotation);
            var cos = Math.Cos(rotation);
            var sin = Math.Sin(rotation);
            
            // 计算旋转后的边界框
            var corners = new Vector2[]
            {
                new Vector2(position.X, position.Y),
                new Vector2(position.X + estimatedWidth * cos, position.Y + estimatedWidth * sin),
                new Vector2(position.X + estimatedWidth * cos - height * sin, position.Y + estimatedWidth * sin + height * cos),
                new Vector2(position.X - height * sin, position.Y + height * cos)
            };
            
            double minX = corners[0].X, maxX = corners[0].X;
            double minY = corners[0].Y, maxY = corners[0].Y;
            
            foreach (var corner in corners)
            {
                minX = Math.Min(minX, corner.X);
                maxX = Math.Max(maxX, corner.X);
                minY = Math.Min(minY, corner.Y);
                maxY = Math.Max(maxY, corner.Y);
            }
            
            return new Bounding(minX, minY, maxX, maxY);
        }

        public override List<Vector2> GetSnapPoints(netDxf.Entities.Text entity)
        {
            var snapPoints = new List<Vector2>();
            
            // 文本的捕捉点包括插入点
            var position = CoordinateConverter.ToLitMathVector2(entity.Position);
            snapPoints.Add(position);
            
            return snapPoints;
        }

        protected override void XmlOutSpecific(XmlWriter xmlWriter, netDxf.Entities.Text entity)
        {
            xmlWriter.WriteElementString("text", entity.Value);
            xmlWriter.WriteElementString("height", entity.Height.ToString());
            xmlWriter.WriteElementString("rotation", entity.Rotation.ToString());
            xmlWriter.WriteElementString("alignment", entity.Alignment.ToString());
            
            var position = CoordinateConverter.ToLitMathVector2(entity.Position);
            xmlWriter.WriteStartElement("position");
            xmlWriter.WriteAttributeString("X", position.X.ToString());
            xmlWriter.WriteAttributeString("Y", position.Y.ToString());
            xmlWriter.WriteEndElement();
            
            if (entity.Style != null)
            {
                xmlWriter.WriteElementString("style", entity.Style.Name);
            }
        }

        protected override void XmlInSpecific(XmlReader xmlReader, netDxf.Entities.Text entity)
        {
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    switch (xmlReader.Name)
                    {
                        case "text":
                            entity.Value = xmlReader.ReadElementContentAsString();
                            break;
                        case "height":
                            entity.Height = xmlReader.ReadElementContentAsDouble();
                            break;
                        case "rotation":
                            entity.Rotation = xmlReader.ReadElementContentAsDouble();
                            break;
                        case "alignment":
                            if (Enum.TryParse<netDxf.Entities.TextAlignment>(xmlReader.ReadElementContentAsString(), out var alignment))
                            {
                                entity.Alignment = alignment;
                            }
                            break;
                        case "position":
                            var x = double.Parse(xmlReader.GetAttribute("X"));
                            var y = double.Parse(xmlReader.GetAttribute("Y"));
                            entity.Position = new netDxf.Vector3(x, y, 0);
                            break;
                        case "style":
                            var styleName = xmlReader.ReadElementContentAsString();
                            entity.Style = new netDxf.Tables.TextStyle(styleName);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 转换文本对齐方式
        /// </summary>
        private lcdb.TextAlignment ConvertTextAlignment(netDxf.Entities.TextAlignment netDxfAlignment)
        {
            switch (netDxfAlignment)
            {
                case netDxf.Entities.TextAlignment.TopLeft:
                    return lcdb.TextAlignment.LeftTop;
                case netDxf.Entities.TextAlignment.TopCenter:
                    return lcdb.TextAlignment.CenterTop;
                case netDxf.Entities.TextAlignment.TopRight:
                    return lcdb.TextAlignment.RightTop;
                case netDxf.Entities.TextAlignment.MiddleLeft:
                    return lcdb.TextAlignment.LeftMiddle;
                case netDxf.Entities.TextAlignment.MiddleCenter:
                    return lcdb.TextAlignment.CenterMiddle;
                case netDxf.Entities.TextAlignment.MiddleRight:
                    return lcdb.TextAlignment.RightMiddle;
                case netDxf.Entities.TextAlignment.BaselineLeft:
                    return lcdb.TextAlignment.LeftBottom;
                case netDxf.Entities.TextAlignment.BaselineCenter:
                    return lcdb.TextAlignment.CenterBottom;
                case netDxf.Entities.TextAlignment.BaselineRight:
                    return lcdb.TextAlignment.RightBottom;
                case netDxf.Entities.TextAlignment.BottomLeft:
                    return lcdb.TextAlignment.LeftBottom;
                case netDxf.Entities.TextAlignment.BottomCenter:
                    return lcdb.TextAlignment.CenterBottom;
                case netDxf.Entities.TextAlignment.BottomRight:
                    return lcdb.TextAlignment.RightBottom;
                default:
                    return lcdb.TextAlignment.LeftBottom;
            }
        }
    }
}