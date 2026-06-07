using System;
using System.Collections.Generic;
using System.Linq;
using LitMath;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// 块参照（块插入实体）
    /// </summary>
    public class BlockReference : Entity
    {
        #region 私有字段

        private ObjectId _blockId = ObjectId.Null;
        private Vector2 _position = new Vector2(0, 0);
        private Vector2 _scale = new Vector2(1, 1);
        private double _rotation = 0.0;
        private bool _uniformScale = true;

        #endregion

        #region 属�?

        /// <summary>
        /// 类名
        /// </summary>
        public override string className
        {
            get { return "BlockReference"; }
        }

        /// <summary>
        /// 块定义ID
        /// </summary>
        public ObjectId BlockId
        {
            get { return _blockId; }
            set { _blockId = value; }
        }

        /// <summary>
        /// 块定义名�?
        /// </summary>
        public string BlockName
        {
            get
            {
                if (_blockId == ObjectId.Null || database == null)
                    return "";

                var block = database.GetObject(_blockId) as Block;
                return block?.name ?? "";
            }
            set
            {
                if (database != null && database.blockTable.Has(value))
                {
                    _blockId = database.blockTable[value].id;
                }
            }
        }

        /// <summary>
        /// 插入位置
        /// </summary>
        public Vector2 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// 缩放比例
        /// </summary>
        public Vector2 Scale
        {
            get { return _scale; }
            set { _scale = value; }
        }

        /// <summary>
        /// 旋转角度（弧度）
        /// </summary>
        public double Rotation
        {
            get { return _rotation; }
            set { _rotation = value; }
        }

        /// <summary>
        /// 是否等比缩放
        /// </summary>
        public bool UniformScale
        {
            get { return _uniformScale; }
            set { _uniformScale = value; }
        }

        /// <summary>
        /// X方向缩放比例
        /// </summary>
        public double ScaleX
        {
            get { return _scale.X; }
            set 
            { 
                _scale = new Vector2(value, _uniformScale ? value : _scale.Y); 
            }
        }

        /// <summary>
        /// Y方向缩放比例
        /// </summary>
        public double ScaleY
        {
            get { return _scale.Y; }
            set 
            { 
                _scale = new Vector2(_uniformScale ? value : _scale.X, value); 
            }
        }

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                if (_blockId == ObjectId.Null || database == null)
                    return new Bounding(_position, 0, 0);

                var block = database.GetObject(_blockId) as Block;
                if (block == null || block.Entities.Count == 0)
                    return new Bounding(_position, 0, 0);

                // 计算块的边界�?
                var blockBounding = CalculateBlockBounding(block);
                
                // 应用变换
                var transformedBounding = TransformBounding(blockBounding);
                
                return transformedBounding;
            }
        }

        #endregion

        #region 构造函�?

        /// <summary>
        /// 默认构造函�?
        /// </summary>
        public BlockReference()
        {
            _position = new Vector2(0, 0);
            _scale = new Vector2(1, 1);
            _rotation = 0.0;
            _uniformScale = true;
        }

        /// <summary>
        /// 通过块名称构�?
        /// </summary>
        /// <param name="blockName">块名�?/param>
        /// <param name="position">插入位置</param>
        public BlockReference(string blockName, Vector2 position)
        {
            _position = position;
            _scale = new Vector2(1, 1);
            _rotation = 0.0;
            _uniformScale = true;
            BlockName = blockName;
        }

        /// <summary>
        /// 通过块ID构�?
        /// </summary>
        /// <param name="blockId">块ID</param>
        /// <param name="position">插入位置</param>
        public BlockReference(ObjectId blockId, Vector2 position)
        {
            _blockId = blockId;
            _position = position;
            _scale = new Vector2(1, 1);
            _rotation = 0.0;
            _uniformScale = true;
        }

        /// <summary>
        /// 完整参数构�?
        /// </summary>
        /// <param name="blockId">块ID</param>
        /// <param name="position">插入位置</param>
        /// <param name="scale">缩放比例</param>
        /// <param name="rotation">旋转角度</param>
        public BlockReference(ObjectId blockId, Vector2 position, Vector2 scale, double rotation)
        {
            _blockId = blockId;
            _position = position;
            _scale = scale;
            _rotation = rotation;
            _uniformScale = Math.Abs(scale.X - scale.Y) < 1e-10;
        }

        #endregion

        #region 必须实现的方�?

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            if (_blockId == ObjectId.Null || database == null)
                return;

            var block = database.GetObject(_blockId) as Block;
            if (block == null)
                return;

            // 计算变换矩阵
            var transform = GetTransformMatrix();

            // 绘制块中的所有实�?
            foreach (var entity in block.Entities)
            {
                DrawTransformedEntity(gd, entity, transform);
            }
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new BlockReference();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            BlockReference blkRef = base.Clone() as BlockReference;
            blkRef._blockId = _blockId;
            blkRef._position = _position;
            blkRef._scale = _scale;
            blkRef._rotation = _rotation;
            blkRef._uniformScale = _uniformScale;
            return blkRef;
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            _position += translation;
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            _position = Vector2.RotateInRadian(_position, center, angle);
            _rotation += angle;
        }

        /// <summary>
        /// 矩阵变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            _position = transform * _position;
            
            // 提取变换矩阵的缩放和旋转信息
            var scaleVector = new Vector2(
                Math.Sqrt(transform.m11 * transform.m11 + transform.m12 * transform.m12),
                Math.Sqrt(transform.m21 * transform.m21 + transform.m22 * transform.m22)
            );
            
            _scale = new Vector2(_scale.X * scaleVector.X, _scale.Y * scaleVector.Y);
            
            // 提取旋转角度
            double angle = Math.Atan2(transform.m21, transform.m11);
            _rotation += angle;
        }

        #endregion

        #region 交互功能

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            
            // 插入点夹�?
            gripPoints.Add(new GripPoint(GripPointType.Center, _position));
            
            // 如果有块定义，添加缩放控制点
            if (_blockId != ObjectId.Null && database != null)
            {
                var block = database.GetObject(_blockId) as Block;
                if (block != null)
                {
                    var bounding = CalculateBlockBounding(block);
                    var transform = GetTransformMatrix();
                    
                    // 添加四个角的缩放控制�?
                    var corners = new[]
                    {
                        transform * new Vector2(bounding.center.X - bounding.width/2, bounding.center.Y - bounding.height/2),
                        transform * new Vector2(bounding.center.X + bounding.width/2, bounding.center.Y - bounding.height/2),
                        transform * new Vector2(bounding.center.X + bounding.width/2, bounding.center.Y + bounding.height/2),
                        transform * new Vector2(bounding.center.X - bounding.width/2, bounding.center.Y + bounding.height/2)
                    };
                    
                    foreach (var corner in corners)
                    {
                        gripPoints.Add(new GripPoint(GripPointType.Quad, corner));
                    }
                }
            }
            
            return gripPoints;
        }

        /// <summary>
        /// 对象捕捉�?
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPoints = new List<ObjectSnapPoint>();
            
            // 插入�?
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Ins, _position));
            
            // 如果有块定义，添加块内实体的捕捉�?
            if (_blockId != ObjectId.Null && database != null)
            {
                var block = database.GetObject(_blockId) as Block;
                if (block != null)
                {
                    var transform = GetTransformMatrix();
                    
                    foreach (var entity in block.Entities)
                    {
                        var entitySnapPoints = entity.GetSnapPoints();
                        if (entitySnapPoints != null)
                        {
                            foreach (var snapPoint in entitySnapPoints)
                            {
                                var transformedPoint = transform * snapPoint.position;
                                snapPoints.Add(new ObjectSnapPoint(snapPoint.type, transformedPoint));
                            }
                        }
                    }
                }
            }
            
            return snapPoints;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            if (index == 0)
            {
                // 移动插入�?
                _position = newPosition;
            }
            else if (index >= 1 && index <= 4)
            {
                // 缩放操作
                var originalBounding = bounding;
                if (originalBounding.width > 0 && originalBounding.height > 0)
                {
                    var centerToNew = newPosition - originalBounding.center;
                    var centerToOld = gripPoint.position - originalBounding.center;
                    
                    if (Math.Abs(centerToOld.X) > 1e-10 && Math.Abs(centerToOld.Y) > 1e-10)
                    {
                        var scaleFactorX = Math.Abs(centerToNew.X / centerToOld.X);
                        var scaleFactorY = Math.Abs(centerToNew.Y / centerToOld.Y);
                        
                        if (_uniformScale)
                        {
                            var scaleFactor = Math.Max(scaleFactorX, scaleFactorY);
                            _scale = new Vector2(_scale.X * scaleFactor, _scale.Y * scaleFactor);
                        }
                        else
                        {
                            _scale = new Vector2(_scale.X * scaleFactorX, _scale.Y * scaleFactorY);
                        }
                    }
                }
            }
        }

        #endregion

        #region XML序列�?

        /// <summary>
        /// 写XML
        /// </summary>

        /// <summary>
        /// 读XML
        /// </summary>

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取变换矩阵
        /// </summary>
        /// <returns>变换矩阵</returns>
        public Matrix3 GetTransformMatrix()
        {
            // 按照缩放 -> 旋转 -> 平移的顺序构建变换矩�?
            var scaleMatrix = Matrix3.Scale(_scale);
            var rotationMatrix = Matrix3.RotateInRadian(_rotation);
            var translationMatrix = Matrix3.Translate(_position);
            
            return translationMatrix * rotationMatrix * scaleMatrix;
        }

        /// <summary>
        /// 计算块的边界�?
        /// </summary>
        /// <param name="block">块定�?/param>
        /// <returns>边界�?/returns>
        private Bounding CalculateBlockBounding(Block block)
        {
            if (block.Entities.Count == 0)
                return new Bounding(Vector2.Zero, 0, 0);

            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;

            foreach (var entity in block.Entities)
            {
                var entityBounding = entity.bounding;
                minX = Math.Min(minX, entityBounding.center.X - entityBounding.width / 2);
                maxX = Math.Max(maxX, entityBounding.center.X + entityBounding.width / 2);
                minY = Math.Min(minY, entityBounding.center.Y - entityBounding.height / 2);
                maxY = Math.Max(maxY, entityBounding.center.Y + entityBounding.height / 2);
            }

            return new Bounding(
                new Vector2((minX + maxX) / 2, (minY + maxY) / 2),
                maxX - minX,
                maxY - minY
            );
        }

        /// <summary>
        /// 变换边界�?
        /// </summary>
        /// <param name="originalBounding">原始边界�?/param>
        /// <returns>变换后的边界�?/returns>
        private Bounding TransformBounding(Bounding originalBounding)
        {
            var transform = GetTransformMatrix();
            
            // 变换四个角点
            var corners = new[]
            {
                new Vector2(originalBounding.center.X - originalBounding.width/2, originalBounding.center.Y - originalBounding.height/2),
                new Vector2(originalBounding.center.X + originalBounding.width/2, originalBounding.center.Y - originalBounding.height/2),
                new Vector2(originalBounding.center.X + originalBounding.width/2, originalBounding.center.Y + originalBounding.height/2),
                new Vector2(originalBounding.center.X - originalBounding.width/2, originalBounding.center.Y + originalBounding.height/2)
            };

            var transformedCorners = corners.Select(corner => transform * corner).ToArray();

            double minX = transformedCorners.Min(p => p.X);
            double maxX = transformedCorners.Max(p => p.X);
            double minY = transformedCorners.Min(p => p.Y);
            double maxY = transformedCorners.Max(p => p.Y);

            return new Bounding(
                new Vector2((minX + maxX) / 2, (minY + maxY) / 2),
                maxX - minX,
                maxY - minY
            );
        }

        /// <summary>
        /// 绘制变换后的实体
        /// </summary>
        /// <param name="gd">图形绘制接口</param>
        /// <param name="entity">实体</param>
        /// <param name="transform">变换矩阵</param>
        private void DrawTransformedEntity(IGraphicsDraw gd, Entity entity, Matrix3 transform)
        {
            // 创建实体副本并应用变�?
            var entityCopy = entity.Clone() as Entity;
            entityCopy.TransformBy(transform);
            
            // 应用块参照的样式属�?
            if (entityCopy.color.colorMethod == lcdb.Colors.ColorMethod.ByBlock)
            {
                entityCopy.color = this.color;
            }
            
            if (entityCopy.lineWeight == LineWeight.ByBlock)
            {
                entityCopy.lineWeight = this.lineWeight;
            }
            
            // 绘制变换后的实体
            entityCopy.Draw(gd);
        }

        /// <summary>
        /// 获取块定�?
        /// </summary>
        /// <returns>块定义，如果不存在返回null</returns>
        public Block GetBlock()
        {
            if (_blockId == ObjectId.Null || database == null)
                return null;
                
            return database.GetObject(_blockId) as Block;
        }

        /// <summary>
        /// 设置块定�?
        /// </summary>
        /// <param name="block">块定�?/param>
        public void SetBlock(Block block)
        {
            if (block != null)
            {
                _blockId = block.id;
            }
            else
            {
                _blockId = ObjectId.Null;
            }
        }

        /// <summary>
        /// 展开块参照（将块中的实体转换为独立实体）
        /// </summary>
        /// <returns>展开后的实体列表</returns>
        public List<Entity> Explode()
        {
            var explodedEntities = new List<Entity>();
            
            if (_blockId == ObjectId.Null || database == null)
                return explodedEntities;

            var block = database.GetObject(_blockId) as Block;
            if (block == null)
                return explodedEntities;

            var transform = GetTransformMatrix();

            foreach (var entity in block.Entities)
            {
                var entityCopy = entity.Clone() as Entity;
                entityCopy.TransformBy(transform);
                
                // 应用块参照的样式属�?
                if (entityCopy.color.colorMethod == lcdb.Colors.ColorMethod.ByBlock)
                {
                    entityCopy.color = this.color;
                }
                
                if (entityCopy.lineWeight == LineWeight.ByBlock)
                {
                    entityCopy.lineWeight = this.lineWeight;
                }
                
                if (entityCopy.layer == "0") // 如果�?图层，使用块参照的图�?
                {
                    entityCopy.layer = this.layer;
                }
                
                explodedEntities.Add(entityCopy);
            }

            return explodedEntities;
        }

        #endregion
    }
}
