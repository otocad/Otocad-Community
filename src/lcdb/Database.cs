using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml;
using lcdb.IO;
using LitMath;
using netDxf;
using netDxf.Entities;
using netDxf.Units;
using lcdb.Colors;
using OtoCAD.OpticEntity;
using lcinterface.Interface;


namespace lcdb
{
    public class Database : IDatabase
    {
        /// <summary>
        /// 块表
        /// </summary>
        private BlockTable _blockTable = null;
     
        public BlockTable blockTable
        {
            get { return _blockTable; }
        }
        public static ObjectId BlockTableId
        {
            get { return new ObjectId(TableIds.BlockTableId); }
        }

        /// <summary>
        /// 图层表
        /// </summary>
        private LayerTable _layerTable = null;
        public static ObjectId LayerTableId
        {
            get { return new ObjectId(TableIds.LayerTableId); }
        }
        public LayerTable layerTable
        {
            get { return _layerTable; }
        }

        /// <summary>
        /// 文本样式表
        /// </summary>
        private TextStyleTable _textStyleTable = null;
        public static ObjectId TextStyleTableId
        {
            get { return new ObjectId(TableIds.TextStyleTableId); }
        }
        public TextStyleTable textStyleTable
        {
            get { return _textStyleTable; }
        }

        /// <summary>
        /// 形状样式表
        /// </summary>
        private ShapeStyleTable _shapeStyleTable = null;
        public static ObjectId ShapeStyleTableId
        {
            get { return new ObjectId(TableIds.ShapeStyleTableId); }
        }
        public ShapeStyleTable shapeStyleTable
        {
            get { return _shapeStyleTable; }
        }

        /// <summary>
        /// ID
        /// </summary>
        private Dictionary<ObjectId, DBObject> _dictId2Object = null;
        internal ObjectId currentMaxId
        {
            get
            {
                if (_dictId2Object == null || _dictId2Object.Count == 0)
                {
                    return ObjectId.Null;
                }
                else
                {
                    ObjectId id = ObjectId.Null;
                    foreach (KeyValuePair<ObjectId, DBObject> kvp in _dictId2Object)
                    {
                        if (kvp.Key.CompareTo(id) > 0)
                        {
                            id = kvp.Key;
                        }
                    }
                    return id;
                }
            }
        }

        private ObjectIdMgr _idMgr = null;

        /// <summary>
        /// 文件名
        /// </summary>
        private string _fileName = null;
        public string fileName
        {
            get { return _fileName; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public Database()
        {
            _dictId2Object = new Dictionary<ObjectId, DBObject>();
            _idMgr = new ObjectIdMgr(this);

            _blockTable = new BlockTable(this);
            Block modelSpace = new Block();
            modelSpace.name = "ModelSpace";
            _blockTable.Add(modelSpace);

            // 创建FrameBlock用于存放Frame的子实体
            Block frameBlock = new Block();
            frameBlock.name = "FrameBlock";
            _blockTable.Add(frameBlock);

            IdentifyDBTable(_blockTable);

            _layerTable = new LayerTable(this);
            IdentifyDBTable(_layerTable);
            
            // 创建默认的 0 层
            Layer layer0 = new Layer();
            layer0.name = "0";
            layer0.color = Color.FromRGB(255, 255, 255);  // 白色
            _layerTable.Add(layer0);

            _textStyleTable = new TextStyleTable(this);
            IdentifyDBTable(_textStyleTable);

            _shapeStyleTable = new ShapeStyleTable(this);
            IdentifyDBTable(_shapeStyleTable);
        }

        /// <summary>
        /// 通过ID获取数据库对象
        /// </summary>
        public DBObject GetObject(ObjectId oid)
        {
            if (_dictId2Object.ContainsKey(oid))
            {
                return _dictId2Object[oid];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 打开文件
        /// </summary>
        /// <param name="fileFullPath">文件路径</param>
        public void Open(string fileFullPath)
        {
            string ext = Path.GetExtension(fileFullPath).ToLower();

            if (ext == ".dxf")
            {
                OpenDxf(fileFullPath);
            }
            else
            {
                // 清空旧数据,加载干净
                ClearLayerTable();
                ClearBlockTable();

                // 路由: V5 zip (.otocad zip 容器) 优先, V4 JSON 兜底.
                if (lcdb.IO.OtocadPackageV5.IsV5File(fileFullPath))
                {
                    lcdb.IO.OtocadPackageV5.LoadInto(this, fileFullPath);
                }
                else if (lcdb.IO.OtocadFileFormatV4.IsV4File(fileFullPath))
                {
                    lcdb.IO.OtocadFileFormatV4.LoadInto(this, fileFullPath);
                }
                else
                {
                    throw new System.IO.InvalidDataException(
                        $"不支持的文件格式: {fileFullPath}. " +
                        "v0.1+ 仅支持 V4 单 JSON / V5 zip 容器 / DXF.");
                }
                _fileName = fileFullPath;
                _idMgr.reset();
            }
        }

        /// <summary>
        /// 保存文件
        /// </summary>
        public void Save()
        {
            if (_fileName != null && System.IO.File.Exists(_fileName))
            {
                JsonOut(_fileName);
            }
        }

        /// <summary>
        /// 另存为
        /// </summary>
        /// <param name="fileFullPath">文件路径</param>
        /// <param name="rename">是否重命名</param>
        public void SaveAs(string fileFullPath, bool rename = false)
        {
            string ext = Path.GetExtension(fileFullPath).ToLower();

            if (ext == ".dxf")
                SaveAsDxf(fileFullPath);
            else
                JsonOut(fileFullPath);

            if (rename)
            {
                _fileName = fileFullPath;
            }
        }

        internal void OpenDxf(string fileFullPath)
        {
            // this check is optional but recommended before loading a DXF file
            var dxfVersion = DxfDocument.CheckDxfFileVersion(fileFullPath);
            // netDxf is only compatible with AutoCad2000 and higher DXF versions
            if (dxfVersion < netDxf.Header.DxfVersion.AutoCad2000)
            {
                // 业务层不依赖 UI: 抛异常, 由 UI 层 catch 显示错误
                // 用 InvalidDataException (基础库, 跨平台); FileFormatException 在 System.IO.Packaging, 非 net10.0 内置
                throw new System.IO.InvalidDataException(
                    $"DXF 版本不兼容: 需要 AutoCAD 2000 (R15) 或更高, 当前文件 {dxfVersion}");
            }

            // load file
            DxfDocument dxfDoc = DxfDocument.Load(fileFullPath);

            //
            ClearLayerTable();
            ClearBlockTable();

            //foreach (var lay in dxfDoc.Layers)
            //{
            //    _layerTable.Add(new Layer { name = lay.Name, color = Color.FromRGB(lay.Color.R, lay.Color.G, lay.Color.B), lineWeight = LineWeight.ByLineWeightDefault, lineType = LineType.ByLineTypeDefault, description = lay.Description });
            //}

            Block modelSpace = new Block();
            modelSpace.name = "ModelSpace";
            
            // 开始批量操作
            BeginBatchOperation();


            foreach (var ent in dxfDoc.Entities.Arcs)
            {
                modelSpace.AppendEntity(new lcdb.Arc 
                { 
                    center = new LitMath.Vector2(ent.Center.X, ent.Center.Y),
                    radius = ent.Radius,
                    startAngle = ent.StartAngle * Math.PI / 180.0,  // Convert degrees to radians
                    endAngle = ent.EndAngle * Math.PI / 180.0,      // Convert degrees to radians
                    color = Color.FromRGB(ent.Color.R, ent.Color.G, ent.Color.B),
                    lineWeight = LineWeight.ByLineWeightDefault,
                    layer = ent.Layer.Name
                });
            }

            foreach (var ent in dxfDoc.Entities.Circles)
            {
                modelSpace.AppendEntity(new lcdb.Circle { center = new LitMath.Vector2(ent.Center.X, ent.Center.Y), radius = ent.Radius, color = Color.FromRGB(ent.Color.R, ent.Color.G, ent.Color.B), lineWeight = LineWeight.ByLineWeightDefault, layer = ent.Layer.Name });
            }

            foreach (var ent in dxfDoc.Entities.Ellipses)
            {
                modelSpace.AppendEntity(new lcdb.Ellipse { center = new LitMath.Vector2(ent.Center.X, ent.Center.Y), radiusX = ent.MajorAxis, radiusY = ent.MinorAxis, color = Color.FromRGB(ent.Color.R, ent.Color.G, ent.Color.B), lineWeight = LineWeight.ByLineWeightDefault, layer = ent.Layer.Name });
            }

            foreach (var ent in dxfDoc.Entities.Lines)
            {
                modelSpace.AppendEntity(new lcdb.Line { startPoint = new LitMath.Vector2(ent.StartPoint.X, ent.StartPoint.Y), endPoint = new LitMath.Vector2(ent.EndPoint.X, ent.EndPoint.Y), color = Color.FromRGB(ent.Color.R, ent.Color.G, ent.Color.B), lineWeight = LineWeight.ByLineWeightDefault, layer = ent.Layer.Name });
            }

            foreach (var ent in dxfDoc.Entities.Points)
            {
                modelSpace.AppendEntity(new lcdb.XPoint { endPoint = new LitMath.Vector2(ent.Position.X, ent.Position.Y), color = Color.FromRGB(ent.Color.R, ent.Color.G, ent.Color.B), lineWeight = LineWeight.ByLineWeightDefault, layer = ent.Layer.Name });
            }

            foreach (var ent in dxfDoc.Entities.Rays)
            {
                modelSpace.AppendEntity(new lcdb.Ray { basePoint = new LitMath.Vector2(ent.Origin.X, ent.Origin.Y), direction = new LitMath.Vector2(ent.Direction.X, ent.Direction.Y), color = Color.FromRGB(ent.Color.R, ent.Color.G, ent.Color.B), lineWeight = LineWeight.ByLineWeightDefault, layer = ent.Layer.Name });
            }

            foreach (var ent in dxfDoc.Entities.XLines)
            {
                modelSpace.AppendEntity(new lcdb.Xline { basePoint = new LitMath.Vector2(ent.Origin.X, ent.Origin.Y), direction = new LitMath.Vector2(ent.Direction.X, ent.Direction.Y), color = Color.FromRGB(ent.Color.R, ent.Color.G, ent.Color.B), lineWeight = LineWeight.ByLineWeightDefault, layer = ent.Layer.Name });
            }

            foreach (var ent in dxfDoc.Entities.Texts)
            {
                modelSpace.AppendEntity(new lcdb.Text { Position = new LitMath.Vector3(ent.Position.X, ent.Position.Y, 0.0), color = Color.FromRGB(ent.Color.R, ent.Color.G, ent.Color.B), lineWeight = LineWeight.ByLineWeightDefault, layer = ent.Layer.Name, Height = ent.Height, Value = ent.Value });
            }

            foreach (var ent in dxfDoc.Entities.Polylines2D)
            {
                var poly = new lcdb.Polyline { color = Color.FromRGB(ent.Color.R, ent.Color.G, ent.Color.B), lineWeight = LineWeight.ByLineWeightDefault, layer = ent.Layer.Name };

                for (int i = 0; i < ent.Vertexes.Count; ++i)
                {
                    poly.AddVertexAt(i, new LitMath.Vector2b(ent.Vertexes[i].Position.X, ent.Vertexes[i].Position.Y, ent.Vertexes[i].Bulge));
                }

                poly.closed = ent.IsClosed;

                modelSpace.AppendEntity(poly);
            }

            _blockTable.Add(modelSpace);

            // 结束批量操作，一次性触发所有事件
            EndBatchOperation();
            
            IdentifyDBTable(_blockTable);

            _fileName = fileFullPath;
            _idMgr.reset();
        }

        internal void SaveAsDxf(string fileFullPath)
        {
            // create a new document, by default it will create an AutoCad2000 DXF version
            DxfDocument doc = new DxfDocument();
            
            doc.DrawingVariables.InsUnits = DrawingUnits.Millimeters;
            

            // LAYERS
            foreach (Layer layer in _layerTable._items)
            {
                netDxf.Tables.Layer lay = new netDxf.Tables.Layer(layer.name);

                lay.Color = new netDxf.AciColor(layer.color.r, layer.color.g, layer.color.b);
                lay.Lineweight = netDxf.Lineweight.Default;
                lay.Linetype = netDxf.Tables.Linetype.Continuous;
                lay.Description = layer.description;
                
                doc.Layers.Add(lay);
            }

       

            // ENTITIES
            foreach (Block block in _blockTable._items)
            {

                foreach (lcdb.Entity entity in block.Entities)
                {
                    if (entity is Polyline)
                    {
                        var poly = (Polyline)entity;
                        string layername = string.IsNullOrEmpty(poly.layer) ? "0" : poly.layer;

                        //polyline
                        netDxf.Entities.Polyline2DVertex polyVertex;

                        List<netDxf.Entities.Polyline2DVertex> polyVertexes =
                            new List<netDxf.Entities.Polyline2DVertex>();


                        foreach (var vertice in poly.Vertices)
                        {
                            polyVertex = new netDxf.Entities.Polyline2DVertex(new netDxf.Vector2(vertice.X, vertice.Y));
                            polyVertex.Bulge = vertice.B;
                            polyVertexes.Add(polyVertex);
                        }

                        // an entity
                        netDxf.Entities.Polyline2D
                            dxfEntity = new netDxf.Entities.Polyline2D(polyVertexes, poly.closed);

                        dxfEntity.Color = new netDxf.AciColor(poly.color.r, poly.color.g, poly.color.b);
                        dxfEntity.Layer = doc.Layers.Where(o => o.Name == layername).SingleOrDefault();
                        dxfEntity.Lineweight = netDxf.Lineweight.Default;

                        // add your entities here
                        doc.Entities.Add(dxfEntity);
                    }
                    else if (entity is Circle)
                    {
                        var circle = (Circle)entity;
                        string layername = string.IsNullOrEmpty(circle.layer) ? "0" : circle.layer;

                        // an entity
                        netDxf.Entities.Circle dxfEntity =
                            new netDxf.Entities.Circle(new netDxf.Vector2(circle.center.X, circle.center.Y),
                                circle.radius);
                        dxfEntity.Color = new netDxf.AciColor(circle.color.r, circle.color.g, circle.color.b);
                        dxfEntity.Layer = doc.Layers.Where(o => o.Name == layername).SingleOrDefault();
                        dxfEntity.Lineweight = netDxf.Lineweight.Default;

                        // add your entities here
                        doc.Entities.Add(dxfEntity);
                    }
                    else if (entity is Ellipse)
                    {
                        var ellipse = (Ellipse)entity;
                        string layername = string.IsNullOrEmpty(ellipse.layer) ? "0" : ellipse.layer;

                        // an entity
                        netDxf.Entities.Ellipse dxfEntity = new netDxf.Entities.Ellipse(
                            new netDxf.Vector2(ellipse.center.X, ellipse.center.Y), ellipse.radiusX, ellipse.radiusY);
                        dxfEntity.Color = new netDxf.AciColor(ellipse.color.r, ellipse.color.g, ellipse.color.b);
                        dxfEntity.Layer = doc.Layers.Where(o => o.Name == layername).SingleOrDefault();
                        dxfEntity.Lineweight = netDxf.Lineweight.Default;

                        // add your entities here
                        doc.Entities.Add(dxfEntity);
                    }
                    else if (entity is Arc)
                    {
                        var arc = (Arc)entity;
                        string layername = string.IsNullOrEmpty(arc.layer) ? "0" : arc.layer;

                        // TODO: Convert Arc to netDxf.Entities.Arc before adding
                        // doc.Entities.Add(new netDxf.Entities.Arc(...));
                    }
                    else if (entity is Line)
                    {
                        var line = (Line)entity;
                        string layername = string.IsNullOrEmpty(line.layer) ? "0" : line.layer;

                        // an entity
                        netDxf.Entities.Line dxfEntity = new netDxf.Entities.Line(
                            new netDxf.Vector2(line.startPoint.X, line.startPoint.Y),
                            new netDxf.Vector2(line.endPoint.X, line.endPoint.Y));
                        dxfEntity.Color = new netDxf.AciColor(line.color.r, line.color.g, line.color.b);
                        dxfEntity.Linetype = LineTypeFromEntityLineType(line);
                        dxfEntity.Layer = doc.Layers.Where(o => o.Name == layername).SingleOrDefault();
                        dxfEntity.Lineweight = netDxf.Lineweight.Default;

                        // add your entities here
                        doc.Entities.Add(dxfEntity);
                    }
                    else if (entity is XPoint)
                    {
                        var point = (XPoint)entity;
                        string layername = string.IsNullOrEmpty(point.layer) ? "0" : point.layer;

                        // an entity
                        netDxf.Entities.Point dxfEntity = new netDxf.Entities.Point
                            { Position = new netDxf.Vector3(point.endPoint.X, point.endPoint.Y, 0) };
                        dxfEntity.Color = new netDxf.AciColor(point.color.r, point.color.g, point.color.b);
                        dxfEntity.Layer = doc.Layers.Where(o => o.Name == layername).SingleOrDefault();
                        dxfEntity.Lineweight = netDxf.Lineweight.Default;

                        // add your entities here
                        doc.Entities.Add(dxfEntity);
                    }
                    else if (entity is Ray)
                    {
                        var ray = (Ray)entity;
                        string layername = string.IsNullOrEmpty(ray.layer) ? "0" : ray.layer;

                        // an entity
                        netDxf.Entities.Ray dxfEntity = new netDxf.Entities.Ray
                        {
                            Origin = new netDxf.Vector3(ray.basePoint.X, ray.basePoint.Y, 0),
                            Direction = new netDxf.Vector3(ray.direction.X, ray.direction.Y, 0)
                        };
                        dxfEntity.Color = new netDxf.AciColor(ray.color.r, ray.color.g, ray.color.b);
                        dxfEntity.Layer = doc.Layers.Where(o => o.Name == layername).SingleOrDefault();
                        dxfEntity.Lineweight = netDxf.Lineweight.Default;

                        // add your entities here
                        doc.Entities.Add(dxfEntity);
                    }
                    else if (entity is Xline)
                    {
                        var xline = (Xline)entity;
                        string layername = string.IsNullOrEmpty(xline.layer) ? "0" : xline.layer;

                        // an entity
                        netDxf.Entities.XLine dxfEntity = new netDxf.Entities.XLine
                        {
                            Origin = new netDxf.Vector3(xline.basePoint.X, xline.basePoint.Y, 0),
                            Direction = new netDxf.Vector3(xline.direction.X, xline.direction.Y, 0)
                        };
                        dxfEntity.Color = new netDxf.AciColor(xline.color.r, xline.color.g, xline.color.b);
                        dxfEntity.Layer = doc.Layers.Where(o => o.Name == layername).SingleOrDefault();
                        dxfEntity.Lineweight = netDxf.Lineweight.Default;

                        // add your entities here
                        doc.Entities.Add(dxfEntity);
                    }
                    else if (entity is Text)
                    {
                        var text = (Text)entity;
                        string layername = string.IsNullOrEmpty(text.layer) ? "0" : text.layer;

                        // an entity
                        netDxf.Entities.Text dxfEntity = new netDxf.Entities.Text
                            { Position = new netDxf.Vector3(text.Position.X, text.Position.Y, 0) };
                        dxfEntity.Color = new netDxf.AciColor(text.color.r, text.color.g, text.color.b);
                        dxfEntity.Layer = doc.Layers.Where(o => o.Name == layername).SingleOrDefault();
                        dxfEntity.Lineweight = netDxf.Lineweight.Default;
                        dxfEntity.Value = text.Value;
                        dxfEntity.Height = text.Height;

                        // add your entities here
                        doc.Entities.Add(dxfEntity);
                    }
                    else if (entity is lcdb.LinearDimension)
                    {
                        var e = entity as lcdb.LinearDimension;
                        // TODO: Convert LinearDimension to DXF format
                        // For now, create a basic netDxf LinearDimension
                        var dxfDim = new netDxf.Entities.LinearDimension(
                            new netDxf.Vector2(e.firstReferencePoint.X, e.firstReferencePoint.Y),
                            new netDxf.Vector2(e.secondReferencePoint.X, e.secondReferencePoint.Y),
                            e.dimLinePosition.X,
                            e.rotation * 180.0 / Math.PI);
                        doc.Entities.Add(dxfDim);
                    }
                   


                }
            }

            // save to file
            doc.Save(fileFullPath);
        }

        /// <summary>
        /// 写XML文件
        /// </summary>
        /// <param name="xmlFileFullPath">XML文件全路径</param>

        /// <summary>
        /// 读XML文件
        /// </summary>

        /// <summary>
        /// 清空图层表
        /// </summary>
        public void ClearLayerTable()
        {
            List<Layer> allLayers = new List<Layer>();
            foreach (Layer layer in _layerTable._items)
            {
                allLayers.Add(layer);
            }
            _layerTable.Clear();

            foreach (Layer layer in allLayers)
            {
                layer.Erase();
            }
        }

        /// <summary>
        /// 清空块表
        /// </summary>
        public void ClearBlockTable()
        {
            Dictionary<Entity, Entity> allEnts = new Dictionary<Entity,Entity>();
            List<Block> allBlocks = new List<Block>();

            foreach (Block block in _blockTable._items)
            {
                foreach (lcdb.Entity entity in block.Entities)
                {
                    allEnts[entity] = entity;
                }
                block.Clear();
                allBlocks.Add(block);
            }
            _blockTable.Clear();

            foreach (KeyValuePair<Entity, Entity> kvp in allEnts)
            {
                kvp.Key.Erase();
            }

            foreach (Block block in allBlocks)
            {
                block.Erase();
            }
        }

        public LitMath.Rectangle2 GetBoundingBox()
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            var allPoints = PointCloudsFromBlockTable();

            foreach (var point in allPoints)
            {
                if (point.X < minX)
                    minX = point.X;

                if (point.X > maxX)
                    maxX = point.X;

                if (point.Y < minY)
                    minY = point.Y;

                if (point.Y > maxY)
                    maxY = point.Y;
            }

            if (allPoints.Count > 0)
                return new LitMath.Rectangle2(new LitMath.Vector2(minX, maxY), new LitMath.Vector2(maxX, minY));
            else
                return new LitMath.Rectangle2(new LitMath.Vector2(0, 0), new LitMath.Vector2(0, 0));
        }

        private List<LitMath.Vector2> PointCloudsFromBlockTable()
        {
            List<LitMath.Vector2> allPoints = new List<LitMath.Vector2>();

            foreach (Block block in _blockTable._items)
            {
                foreach (lcdb.Entity entity in block.Entities)
                {
                    if (entity is Polyline)
                    {
                        var poly = (Polyline)entity;

                        foreach (var vertice in poly.Vertices)
                        {
                            allPoints.Add(new LitMath.Vector2(vertice.X, vertice.Y));
                        }
                    }
                    else if (entity is Circle)
                    {
                        var circle = (Circle)entity;

                        // Four quadrants 
                        allPoints.Add(new LitMath.Vector2 { X = circle.center.X + circle.radius, Y = circle.center.Y });
                        allPoints.Add(new LitMath.Vector2 { X = circle.center.X - circle.radius, Y = circle.center.Y });
                        allPoints.Add(new LitMath.Vector2 { X = circle.center.X, Y = circle.center.Y + circle.radius });
                        allPoints.Add(new LitMath.Vector2 { X = circle.center.X, Y = circle.center.Y - circle.radius });
                    }
                    else if (entity is Arc)
                    {
                        var arc = (Arc)entity;
                        var bounding = arc.bounding;


                        allPoints.Add(new LitMath.Vector2 (bounding.left, bounding.bottom));
                        allPoints.Add(new LitMath.Vector2 (bounding.right, bounding.top));
                        //// Four quadrants 
                        //allPoints.Add(new LitMath.Vector2 { X = arc.center.X + arc.radius, Y = arc.center.Y });
                        //allPoints.Add(new LitMath.Vector2 { X = arc.center.X - arc.radius, Y = arc.center.Y });
                        //allPoints.Add(new LitMath.Vector2 { X = arc.center.X, Y = arc.center.Y + arc.radius });
                        //allPoints.Add(new LitMath.Vector2 { X = arc.center.X, Y = arc.center.Y - arc.radius });
                    }
                    else if (entity is Line)
                    {
                        var line = (Line)entity;

                        allPoints.Add(new LitMath.Vector2 { X = line.startPoint.X, Y = line.startPoint.Y });
                        allPoints.Add(new LitMath.Vector2 { X = line.endPoint.X, Y = line.endPoint.Y });
                    }
                }
            }

            return allPoints;
        }
        #region IdentityObject
        private void IdentifyDBTable(DBTable table)
        {
            MapSingleObject(table);
        }

        internal void IdentifyObject(DBObject obj)
        {
            IdentifyObjectSingle(obj);
            if (obj is Block)
            {
                Block block = obj as Block;
                foreach (lcdb.Entity entity in block.Entities)
                {
                    IdentifyObjectSingle(entity);
                    // 触发实体添加事件
                    OnObjectAdded(entity);
                }
            }
            else if (obj is Entity entity)
            {
                // 触发实体添加事件
                OnObjectAdded(entity);
            }
        }

        private void IdentifyObjectSingle(DBObject obj)
        {
            if (obj.id.isNull)
            {
                obj.SetId(_idMgr.NextId);
            }
            MapSingleObject(obj);
        }

        private netDxf.Tables.Linetype LineTypeFromEntityLineType(Entity entity)
        {
            if (entity.lineType == lcdb.LineType.ByBlock)
                return netDxf.Tables.Linetype.ByBlock;
            else if (entity.lineType == lcdb.LineType.Solid)
                return netDxf.Tables.Linetype.Continuous;
            else if (entity.lineType == lcdb.LineType.Dash)
                return netDxf.Tables.Linetype.Dashed;
            else if (entity.lineType == lcdb.LineType.Dot)
                return netDxf.Tables.Linetype.Dot;
            else if (entity.lineType == lcdb.LineType.DashDot)
                return netDxf.Tables.Linetype.DashDot;
            else if (entity.lineType == lcdb.LineType.DashDotDot)
                return netDxf.Tables.Linetype.DashDot;
            else if (entity.lineType == lcdb.LineType.Custom)
                return netDxf.Tables.Linetype.ByLayer;
            else
                return netDxf.Tables.Linetype.ByLayer;
        }
        #endregion

        #region MapObject
        private void MapObject(DBObject obj)
        {
            MapSingleObject(obj);
            if (obj is Block)
            {
                Block block = obj as Block;
                foreach (lcdb.Entity entity in block.Entities)
                {
                    MapSingleObject(entity);
                }
            }
        }

        internal void UnmapObject(DBObject obj)
        {
            UnmapSingleObject(obj);
            if (obj is Block)
            {
                Block block = obj as Block;
                foreach (lcdb.Entity entity in block.Entities)
                {
                    UnmapSingleObject(entity);
                }
            }
        }

        private void MapSingleObject(DBObject obj)
        {
            _dictId2Object[obj.id] = obj;
        }

        private void UnmapSingleObject(DBObject obj)
        {
            _dictId2Object.Remove(obj.id);
        }

        public void SingletChanged()
        {
           


        }
        
        #region IDatabase Implementation
        
        /// <summary>
        /// 当对象被添加到数据库时触发
        /// </summary>
        public event Action<object> objectAdded;

        /// <summary>
        /// 当数据库中的对象被修改时触发
        /// </summary>
        public event Action<object> objectModified;

        /// <summary>
        /// 当对象从数据库中被删除时触发
        /// </summary>
        public event Action<object> objectErased;
        
        #region Observer Pattern Implementation
        
        /// <summary>
        /// 注册的观察者列表（使用WeakReference避免内存泄漏）
        /// </summary>
        private readonly List<WeakReference> _observers = new List<WeakReference>();
        
        /// <summary>
        /// 批量更新管理
        /// </summary>
        private bool _isBatchUpdate = false;
        private Guid _currentBatchId = Guid.Empty;
        private readonly List<ObjectId> _batchAddedEntities = new List<ObjectId>();
        private readonly List<ObjectId> _batchModifiedEntities = new List<ObjectId>();
        private readonly List<ObjectId> _batchDeletedEntities = new List<ObjectId>();
        
        /// <summary>
        /// 注册观察者
        /// </summary>
        public void RegisterObserver(IDatabaseObserver observer)
        {
            if (observer == null) return;
            
            // 清理已失效的引用
            CleanupObservers();
            
            // 检查是否已注册
            foreach (var weakRef in _observers)
            {
                if (weakRef.IsAlive && weakRef.Target == observer)
                    return;
            }
            
            _observers.Add(new WeakReference(observer));
        }
        
        /// <summary>
        /// 注销观察者
        /// </summary>
        public void UnregisterObserver(IDatabaseObserver observer)
        {
            if (observer == null) return;
            
            _observers.RemoveAll(wr => !wr.IsAlive || wr.Target == observer);
        }
        
        /// <summary>
        /// 清理失效的观察者引用
        /// </summary>
        private void CleanupObservers()
        {
            _observers.RemoveAll(wr => !wr.IsAlive);
        }
        
        /// <summary>
        /// 开始批量更新
        /// </summary>
        public void BeginBatchUpdate()
        {
            if (_isBatchUpdate) return;
            
            _isBatchUpdate = true;
            _currentBatchId = Guid.NewGuid();
            _batchAddedEntities.Clear();
            _batchModifiedEntities.Clear();
            _batchDeletedEntities.Clear();
            
            NotifyObservers(obs => obs.OnBatchUpdateBegin(_currentBatchId));
        }
        
        /// <summary>
        /// 结束批量更新
        /// </summary>
        public void EndBatchUpdate()
        {
            if (!_isBatchUpdate) return;
            
            _isBatchUpdate = false;
            
            // 通知所有累积的变化
            if (_batchAddedEntities.Count > 0)
                NotifyObservers(obs => obs.OnEntitiesAdded(_batchAddedEntities));
            
            if (_batchModifiedEntities.Count > 0)
                NotifyObservers(obs => obs.OnEntitiesModified(_batchModifiedEntities));
            
            if (_batchDeletedEntities.Count > 0)
                NotifyObservers(obs => obs.OnEntitiesDeleted(_batchDeletedEntities));
            
            NotifyObservers(obs => obs.OnBatchUpdateEnd(_currentBatchId));
            
            _currentBatchId = Guid.Empty;
            _batchAddedEntities.Clear();
            _batchModifiedEntities.Clear();
            _batchDeletedEntities.Clear();
        }
        
        /// <summary>
        /// 通知所有观察者
        /// </summary>
        private void NotifyObservers(Action<IDatabaseObserver> action)
        {
            CleanupObservers();
            
            foreach (var weakRef in _observers.ToList())
            {
                if (weakRef.IsAlive)
                {
                    var observer = weakRef.Target as IDatabaseObserver;
                    if (observer != null)
                    {
                        try
                        {
                            action(observer);
                        }
                        catch (Exception ex)
                        {
                            // 记录错误但不中断其他观察者的通知
                            System.Diagnostics.Debug.WriteLine($"Observer notification failed: {ex.Message}");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 通知实体添加
        /// </summary>
        internal void NotifyEntityAdded(ObjectId entityId)
        {
            if (_isBatchUpdate)
            {
                _batchAddedEntities.Add(entityId);
            }
            else
            {
                NotifyObservers(obs => obs.OnEntitiesAdded(new[] { entityId }));
            }
        }
        
        /// <summary>
        /// 通知实体修改
        /// </summary>
        internal void NotifyEntityModified(ObjectId entityId)
        {
            if (_isBatchUpdate)
            {
                if (!_batchModifiedEntities.Contains(entityId))
                    _batchModifiedEntities.Add(entityId);
            }
            else
            {
                NotifyObservers(obs => obs.OnEntitiesModified(new[] { entityId }));
            }
        }
        
        /// <summary>
        /// 通知实体删除
        /// </summary>
        internal void NotifyEntityDeleted(ObjectId entityId)
        {
            if (_isBatchUpdate)
            {
                _batchDeletedEntities.Add(entityId);
                _batchAddedEntities.Remove(entityId);
                _batchModifiedEntities.Remove(entityId);
            }
            else
            {
                NotifyObservers(obs => obs.OnEntitiesDeleted(new[] { entityId }));
            }
        }
        
        /// <summary>
        /// 通知图层变化
        /// </summary>
        internal void NotifyLayerChanged(ObjectId layerId, LayerChangeType changeType)
        {
            NotifyObservers(obs => obs.OnLayerChanged(layerId, changeType));
        }
        
        #endregion
        
        #region 批量操作支持
        
        private bool _isBatchOperation = false;
        private List<object> _batchAddedObjects = new List<object>();
        
        /// <summary>
        /// 开始批量操作，暂停事件触发
        /// </summary>
        public void BeginBatchOperation()
        {
            _isBatchOperation = true;
            _batchAddedObjects.Clear();
        }
        
        /// <summary>
        /// 结束批量操作，延迟触发事件
        /// </summary>
        public void EndBatchOperation()
        {
            _isBatchOperation = false;
            
            // 批量操作完成后，暂时不触发事件
            // 文件加载完成后由调用方决定何时刷新UI
            // 这避免了大量实体逐个更新UI导致的卡顿
            
            // 清空批量对象列表
            _batchAddedObjects.Clear();
        }
        
        /// <summary>
        /// 手动触发批量操作的事件
        /// </summary>
        public void FlushBatchEvents()
        {
            // 如果需要，可以在适当的时机调用此方法触发所有事件
            // 目前文件加载后直接刷新画布即可，不需要逐个触发事件
        }
        
        #endregion
        
        /// <summary>
        /// 触发对象添加事件
        /// </summary>
        protected virtual void OnObjectAdded(object obj)
        {
            if (_isBatchOperation)
            {
                // 批量操作时，先收集对象
                _batchAddedObjects.Add(obj);
            }
            else
            {
                objectAdded?.Invoke(obj);
                
                // 同时通知观察者
                if (obj is Entity entity && entity.id != ObjectId.Null)
                {
                    NotifyEntityAdded(entity.id);
                }
            }
        }
        
        /// <summary>
        /// 触发对象修改事件
        /// </summary>
        protected virtual void OnObjectModified(object obj)
        {
            objectModified?.Invoke(obj);
            
            // 同时通知观察者
            if (obj is Entity entity && entity.id != ObjectId.Null)
            {
                NotifyEntityModified(entity.id);
            }
        }

        /// <summary>
        /// 通知对象已修改
        /// </summary>
        public void NotifyObjectModified(DBObject obj)
        {
            if (obj != null)
            {
                OnObjectModified(obj);
            }
        }
        
        /// <summary>
        /// 触发对象删除事件
        /// </summary>
        protected virtual void OnObjectErased(object obj)
        {
            objectErased?.Invoke(obj);
            
            // 同时通知观察者
            if (obj is Entity entity && entity.id != ObjectId.Null)
            {
                NotifyEntityDeleted(entity.id);
            }
        }
        
        #endregion
        
        #region Entity Management Methods
        
        /// <summary>
        /// 添加实体到模型空间
        /// </summary>
        /// <param name="entity">要添加的实体</param>
        /// <returns>实体的ObjectId</returns>
        public ObjectId AddEntity(Entity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
                
            // 获取模型空间
            var modelSpace = _blockTable["ModelSpace"] as Block;
            if (modelSpace == null)
                throw new InvalidOperationException("ModelSpace not found in database");
                
            // 添加实体到模型空间
            var id = modelSpace.AppendEntity(entity);
            
            // 触发添加事件
            OnObjectAdded(entity);
            
            return id;
        }
        
        /// <summary>
        /// 添加实体到指定块
        /// </summary>
        /// <param name="entity">要添加的实体</param>
        /// <param name="blockName">块名称</param>
        /// <returns>实体的ObjectId</returns>
        public ObjectId AddEntity(Entity entity, string blockName)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrEmpty(blockName))
                blockName = "ModelSpace";
                
            // 获取指定块
            var block = _blockTable[blockName] as Block;
            if (block == null)
                throw new InvalidOperationException($"Block '{blockName}' not found in database");
                
            // 添加实体到块
            var id = block.AppendEntity(entity);
            
            // 触发添加事件
            OnObjectAdded(entity);
            
            return id;
        }
        
        /// <summary>
        /// 从数据库中移除实体
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveEntity(ObjectId entityId)
        {
            if (entityId == ObjectId.Null)
                return false;
                
            // 获取实体
            var entity = GetObject(entityId) as Entity;
            if (entity == null)
                return false;
                
            // 查找包含该实体的块
            foreach (Block block in _blockTable)
            {
                if (block.Entities.Any(e => e.id == entityId))
                {
                    // 从块中移除实体
                    block.RemoveEntityById(entityId);
                    
                    // 从数据库映射中移除
                    UnmapSingleObject(entity);
                    
                    // 触发删除事件
                    OnObjectErased(entity);
                    
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 批量添加实体
        /// </summary>
        /// <param name="entities">实体集合</param>
        /// <returns>实体ID列表</returns>
        public List<ObjectId> AddEntities(IEnumerable<Entity> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
                
            var ids = new List<ObjectId>();
            
            // 开始批量更新
            BeginBatchUpdate();
            
            try
            {
                foreach (var entity in entities)
                {
                    var id = AddEntity(entity);
                    ids.Add(id);
                }
                
                // 提交批量更新
                EndBatchUpdate();
            }
            catch
            {
                // 发生错误时取消批量更新
                if (_isBatchUpdate)
                {
                    _batchAddedEntities.Clear();
                    _batchModifiedEntities.Clear();
                    _batchDeletedEntities.Clear();
                    _isBatchUpdate = false;
                    _currentBatchId = Guid.Empty;
                }
                throw;
            }
            
            return ids;
        }
        
        /// <summary>
        /// 批量移除实体
        /// </summary>
        /// <param name="entityIds">实体ID集合</param>
        /// <returns>成功移除的数量</returns>
        public int RemoveEntities(IEnumerable<ObjectId> entityIds)
        {
            if (entityIds == null)
                throw new ArgumentNullException(nameof(entityIds));
                
            int removedCount = 0;
            
            // 开始批量更新
            BeginBatchUpdate();
            
            try
            {
                foreach (var id in entityIds)
                {
                    if (RemoveEntity(id))
                        removedCount++;
                }
                
                // 提交批量更新
                EndBatchUpdate();
            }
            catch
            {
                // 发生错误时取消批量更新
                if (_isBatchUpdate)
                {
                    _batchAddedEntities.Clear();
                    _batchModifiedEntities.Clear();
                    _batchDeletedEntities.Clear();
                    _isBatchUpdate = false;
                    _currentBatchId = Guid.Empty;
                }
                throw;
            }
            
            return removedCount;
        }
        
        /// <summary>
        /// 根据ID获取实体
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <returns>实体对象，如果不存在则返回null</returns>
        public Entity GetEntity(ObjectId entityId)
        {
            if (entityId == ObjectId.Null)
                return null;
                
            var obj = GetObject(entityId);
            return obj as Entity;
        }
        
        /// <summary>
        /// 获取所有实体
        /// </summary>
        /// <param name="blockName">块名称，默认为模型空间</param>
        /// <returns>实体集合</returns>
        public IEnumerable<Entity> GetEntities(string blockName = "ModelSpace")
        {
            var block = _blockTable[blockName] as Block;
            if (block != null)
            {
                return block.Entities;
            }
            return Enumerable.Empty<Entity>();
        }
        
        /// <summary>
        /// 按类型获取实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="blockName">块名称，默认为模型空间</param>
        /// <returns>指定类型的实体集合</returns>
        public IEnumerable<T> GetEntitiesByType<T>(string blockName = "ModelSpace") where T : Entity
        {
            return GetEntities(blockName).OfType<T>();
        }
        
        #endregion
        
        #endregion
        /// <summary>
        /// 保存为 JSON 格式
        /// </summary>
        internal void JsonOut(string jsonFileFullPath)
        {
            try
            {
                // 按扩展名路由:
                //   .otocad → V5 zip 包 (OPC 模式, META/MODEL/STYLES 分段)
                //   *      → V4 单 JSON (polymorphic 双段)
                string ext = Path.GetExtension(jsonFileFullPath).ToLower();
                if (ext == ".otocad")
                    lcdb.IO.OtocadPackageV5.Save(this, jsonFileFullPath);
                else
                    lcdb.IO.OtocadFileFormatV4.Save(this, jsonFileFullPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存 文件失败: {ex.Message}", ex);
            }
        }

        #region Transaction Support
        
        private bool _inTransaction = false;
        
        /// <summary>
        /// 开始事务
        /// </summary>
        public void BeginTransaction()
        {
            _inTransaction = true;
        }
        
        /// <summary>
        /// 提交事务
        /// </summary>
        public void CommitTransaction()
        {
            _inTransaction = false;
            // 触发更新事件
            OnObjectModified(this);
        }
        
        /// <summary>
        /// 回滚事务
        /// </summary>
        public void RollbackTransaction()
        {
            _inTransaction = false;
            // 可以在这里添加回滚逻辑
        }
        
        /// <summary>
        /// 标记数据库已修改
        /// </summary>
        public void MarkAsModified()
        {
            OnObjectModified(this);
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// 获取模型空间
        /// </summary>
        public Block GetModelSpace()
        {
            return _blockTable["ModelSpace"] as Block;
        }

        /// <summary>
        /// 确保 ModelSpace + FrameBlock 存在 (ClearBlockTable 后用).
        /// </summary>
        public void EnsureModelSpace()
        {
            if (_blockTable["ModelSpace"] is not Block)
            {
                var ms = new Block { name = "ModelSpace" };
                _blockTable.Add(ms);
            }
            if (_blockTable["FrameBlock"] is not Block)
            {
                var fb = new Block { name = "FrameBlock" };
                _blockTable.Add(fb);
            }
        }
        
        /// <summary>
        /// 压缩表格（清理未使用的条目）
        /// </summary>
        public void CompactTables()
        {
            // 可以在这里添加表格压缩逻辑
            // 例如清理未使用的层、样式等
        }
        
        #endregion
    }
}
