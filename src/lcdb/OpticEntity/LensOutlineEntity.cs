using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using lcdb;
using OtoCAD.OpticEntity.Generators;
using LitMath;

namespace OtoCAD.OpticEntity
{
    /// <summary>
    /// 透镜轮廓组合实体
    /// 将多个基础Entity组合成一个可整体选中的轮廓实体
    /// </summary>
    [Serializable]
    public class LensOutlineEntity : Entity, IComposableEntity, ISerializable
    {
        #region 私有字段
        private List<Entity> _childEntities = new List<Entity>();  // 直接存储实体而不是ID
        private Dictionary<string, object> _parameters = new Dictionary<string, object>();
        private bool _isHighlighted = false;
        private Vector2 _currentPosition = new Vector2(0, 0);  // 记录当前位置，用于计算位移
        #endregion

        #region 属性

        /// <summary>
        /// 所属的Element
        /// </summary>
        public Element ParentElement { get; set; }

        /// <summary>
        /// 子实体列表
        /// </summary>
        public IReadOnlyList<Entity> Children => _childEntities.AsReadOnly();

        /// <summary>
        /// 是否作为整体选中
        /// </summary>
        public bool SelectAsGroup { get; set; } = true;

        /// <summary>
        /// 轮廓类型
        /// </summary>
        public string OutlineType { get; set; } = "LensOutline";

        #endregion

        #region IComposableEntity 实现

        /// <summary>
        /// 获取子实体列表
        /// </summary>
        public IReadOnlyList<Entity> ChildEntities => _childEntities.AsReadOnly();

        /// <summary>
        /// 获取参数字典
        /// </summary>
        public IReadOnlyDictionary<string, object> Parameters => _parameters;

        /// <summary>
        /// 获取或设置实体ID
        /// </summary>
        public ObjectId Id
        {
            get { return this.id; }
            set { /* id is read-only in DBObject */ }
        }

        /// <summary>
        /// 获取或设置是否激活
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 获取或设置是否可见
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// 更新子实体
        /// </summary>
        public void Regenerate()
        {
            // 如果有父Element，重新生成轮廓
            if (ParentElement != null)
            {
                _childEntities.Clear();
                var newOutlines = ElementOutlineGenerator.GenerateOutline(ParentElement);
                foreach (var outline in newOutlines)
                {
                    AddChild(outline);
                }
            }
        }

        /// <summary>
        /// 添加子实体
        /// </summary>
        public void AddChild(Entity entity)
        {
            if (entity != null && !_childEntities.Contains(entity))
            {
                _childEntities.Add(entity);
                entity.layerId = this.layerId;
                entity.color = this.color;

                OnChildEntityChanged(new ChildEntityChangedEventArgs(ChildChangeType.Added, entity));
            }
        }

        /// <summary>
        /// 移除子实体
        /// </summary>
        public bool RemoveChild(ObjectId id)
        {
            var entity = _childEntities.FirstOrDefault(e => e.id == id);
            if (entity != null)
            {
                _childEntities.Remove(entity);
                OnChildEntityChanged(new ChildEntityChangedEventArgs(ChildChangeType.Removed, entity));
                return true;
            }
            return false;
        }

        /// <summary>
        /// 清空所有子实体
        /// </summary>
        public void ClearChildren()
        {
            _childEntities.Clear();
            OnChildEntityChanged(new ChildEntityChangedEventArgs(ChildChangeType.Cleared, null));
        }

        /// <summary>
        /// 获取参数值
        /// </summary>
        public T GetParameter<T>(string key, T defaultValue = default(T))
        {
            if (_parameters.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 设置参数值
        /// </summary>
        public void SetParameter(string key, object value)
        {
            var oldValue = _parameters.ContainsKey(key) ? _parameters[key] : null;
            _parameters[key] = value;

            OnPropertyChanged(new CompositeEntityEventArgs(key, oldValue, value, CompositeChangeType.PropertyChanged));
        }

        /// <summary>
        /// 验证参数
        /// </summary>
        public bool ValidateParameters()
        {
            // 验证必要的参数
            if (ParentElement == null && _childEntities.Count == 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 获取边界框
        /// </summary>
        public Bounding GetBounding()
        {
            return bounding;
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public LensOutlineEntity()
        {
        }

        /// <summary>
        /// 从子实体创建
        /// </summary>
        public LensOutlineEntity(IEnumerable<Entity> children)
        {
            if (children != null)
            {
                foreach (var child in children)
                {
                    AddChild(child);
                }
            }
        }

        /// <summary>
        /// 序列化构造函数
        /// </summary>
        protected LensOutlineEntity(SerializationInfo info, StreamingContext context)
        {
            try
            {
                ParentElement = info.GetValue("ParentElement", typeof(Element)) as Element;
                SelectAsGroup = info.GetBoolean("SelectAsGroup");
                OutlineType = info.GetString("OutlineType");
                IsActive = info.GetBoolean("IsActive");
                IsVisible = info.GetBoolean("IsVisible");
                _parameters = (Dictionary<string, object>)info.GetValue("Parameters", typeof(Dictionary<string, object>));

                // 反序列化子实体
                var childTypes = (Type[])info.GetValue("ChildEntityTypes", typeof(Type[]));
                var childEntities = (Entity[])info.GetValue("ChildEntities", typeof(Entity[]));

                _childEntities = new List<Entity>();
                if (childEntities != null)
                {
                    _childEntities.AddRange(childEntities);
                }
            }
            catch (Exception ex)
            {
                // 处理反序列化错误
                System.Diagnostics.Debug.WriteLine($"反序列化LensOutlineEntity出错: {ex.Message}");
                _childEntities = new List<Entity>();
                _parameters = new Dictionary<string, object>();
            }
        }

        #endregion

        #region Entity抽象方法实现

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            var gripPoints = new List<GripPoint>();

            // 夹点是Entity的原点位置（考虑整体偏移）
            // 对于LensOutlineEntity，这对应于Surface1的顶点位置
            double gripX = _currentPosition.X;
            double gripY = _currentPosition.Y;

            // 如果有父Element，使用其位置
            if (ParentElement != null)
            {
                gripX = ParentElement.OriginalX;
                gripY = ParentElement.OriginalY;
                // 同步更新当前位置
                _currentPosition = new Vector2(gripX, gripY);
            }

            gripPoints.Add(new GripPoint(GripPointType.Center, new Vector2(gripX, gripY)));

            return gripPoints;
        }

        /// <summary>
        /// 设置夹点位置
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            if (index == 0)
            {
                // 计算基于当前位置的位移
                var translation = newPosition - _currentPosition;

                if (translation.length > 1e-10)
                {
                    // 平移所有子实体
                    Translate(translation);

                    // 更新当前位置记录
                    _currentPosition = newPosition;

                    // 如果有父Element，同步更新其位置
                    if (ParentElement != null)
                    {
                        ParentElement.OriginalX = newPosition.X;
                        ParentElement.OriginalY = newPosition.Y;
                    }
                }
            }
        }

        /// <summary>
        /// 获取捕捉点
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            var snapPoints = new List<ObjectSnapPoint>();

            if (_childEntities == null || _childEntities.Count == 0)
                return snapPoints;

            // 收集所有关键端点
            foreach (var entity in _childEntities)
            {
                if (entity is Line line)
                {
                    // 线的端点
                    snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, line.startPoint));
                    snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, line.endPoint));
                    // 线的中点
                    var midPoint = new Vector2(
                        (line.startPoint.X + line.endPoint.X) / 2,
                        (line.startPoint.Y + line.endPoint.Y) / 2
                    );
                    snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, midPoint));
                }
                else if (entity is Arc arc)
                {
                    // Arc的中心点
                    snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Center, arc.center));

                    // Arc的起点和终点
                    var startPoint = new Vector2(
                        arc.center.X + arc.radius * Math.Cos(arc.startAngle),
                        arc.center.Y + arc.radius * Math.Sin(arc.startAngle)
                    );
                    var endPoint = new Vector2(
                        arc.center.X + arc.radius * Math.Cos(arc.endAngle),
                        arc.center.Y + arc.radius * Math.Sin(arc.endAngle)
                    );
                    snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, startPoint));
                    snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, endPoint));

                    // Arc的象限点（0, 90, 180, 270度）
                    for (int i = 0; i < 4; i++)
                    {
                        double angle = i * Math.PI / 2;
                        // 检查角度是否在Arc的范围内
                        if (IsAngleInArcRange(angle, arc.startAngle, arc.endAngle))
                        {
                            var quadPoint = new Vector2(
                                arc.center.X + arc.radius * Math.Cos(angle),
                                arc.center.Y + arc.radius * Math.Sin(angle)
                            );
                            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Quad, quadPoint));
                        }
                    }
                }
            }

            // 去重（基于位置的近似相等）
            var uniqueSnapPoints = new List<ObjectSnapPoint>();
            foreach (var point in snapPoints)
            {
                bool isDuplicate = false;
                foreach (var existing in uniqueSnapPoints)
                {
                    if (Math.Abs(point.position.X - existing.position.X) < 1e-6 &&
                        Math.Abs(point.position.Y - existing.position.Y) < 1e-6)
                    {
                        isDuplicate = true;
                        break;
                    }
                }
                if (!isDuplicate)
                {
                    uniqueSnapPoints.Add(point);
                }
            }

            return uniqueSnapPoints;
        }

        /// <summary>
        /// 检查角度是否在Arc的角度范围内
        /// </summary>
        private bool IsAngleInArcRange(double angle, double startAngle, double endAngle)
        {
            // 规范化角度到[0, 2π]
            while (angle < 0) angle += 2 * Math.PI;
            while (angle >= 2 * Math.PI) angle -= 2 * Math.PI;
            while (startAngle < 0) startAngle += 2 * Math.PI;
            while (startAngle >= 2 * Math.PI) startAngle -= 2 * Math.PI;
            while (endAngle < 0) endAngle += 2 * Math.PI;
            while (endAngle >= 2 * Math.PI) endAngle -= 2 * Math.PI;

            if (startAngle <= endAngle)
            {
                return angle >= startAngle && angle <= endAngle;
            }
            else
            {
                // Arc跨越0度
                return angle >= startAngle || angle <= endAngle;
            }
        }

        /// <summary>
        /// 获取边界框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                if (_childEntities == null || _childEntities.Count == 0)
                {
                    return new Bounding();
                }

                // 计算所有子实体的边界框
                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue;
                bool hasValidBounds = false;

                foreach (var entity in _childEntities)
                {
                    if (entity != null)
                    {
                        var entityBounds = entity.bounding;
                        if (entityBounds.IsValid)
                        {
                            hasValidBounds = true;
                            minX = Math.Min(minX, entityBounds.left);
                            minY = Math.Min(minY, entityBounds.bottom);
                            maxX = Math.Max(maxX, entityBounds.right);
                            maxY = Math.Max(maxY, entityBounds.top);
                        }
                    }
                }

                if (hasValidBounds)
                {
                    return new Bounding(
                        new Vector2(minX, minY),
                        new Vector2(maxX, maxY)
                    );
                }
                return new Bounding();
            }
        }

        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            // 平移所有子实体
            foreach (var entity in _childEntities)
            {
                entity?.Translate(translation);
            }
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            foreach (var entity in _childEntities)
            {
                entity?.Rotate(center, angle);
            }
        }

        /// <summary>
        /// 变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            foreach (var entity in _childEntities)
            {
                entity?.TransformBy(transform);
            }
        }

        #endregion

        #region DBObject抽象方法实现

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new LensOutlineEntity();
        }

        #endregion

        #region 绘图相关

        /// <summary>
        /// 绘制实体
        /// </summary>
        public override void Draw(IGraphicsDraw graphics)
        {
            if (!IsVisible || _childEntities == null)
                return;

            // 绘制所有子实体
            foreach (var entity in _childEntities)
            {
                if (entity != null)
                {
                    entity.Draw(graphics);
                }
            }
        }

        #endregion

        #region 选择和高亮

        /// <summary>
        /// 高亮显示
        /// </summary>
        public void Highlight()
        {
            _isHighlighted = true;
            // 可以在这里添加高亮逻辑
        }

        /// <summary>
        /// 取消高亮
        /// </summary>
        public void Unhighlight()
        {
            _isHighlighted = false;
            // 可以在这里添加取消高亮逻辑
        }

        #endregion

        #region 克隆

        /// <summary>
        /// 克隆实体
        /// </summary>
        public override object Clone()
        {
            var clone = new LensOutlineEntity
            {
                // 创建一个新的Element副本，避免共享引用
                ParentElement = this.ParentElement != null ? new Element
                {
                    OriginalX = this.ParentElement.OriginalX,
                    OriginalY = this.ParentElement.OriginalY,
                    // 复制其他必要属性
                } : null,
                SelectAsGroup = this.SelectAsGroup,
                OutlineType = this.OutlineType,
                IsActive = this.IsActive,
                IsVisible = this.IsVisible,
                layerId = this.layerId,
                color = this.color,
                lineWeight = this.lineWeight,
                _currentPosition = this._currentPosition  // 复制当前位置
            };

            // 复制参数
            foreach (var kvp in _parameters)
            {
                clone._parameters[kvp.Key] = kvp.Value;
            }

            // 克隆子实体
            foreach (var entity in _childEntities)
            {
                if (entity != null)
                {
                    var childClone = entity.Clone() as Entity;
                    if (childClone != null)
                    {
                        clone._childEntities.Add(childClone);
                    }
                }
            }

            return clone;
        }

        #endregion

        #region 序列化

        /// <summary>
        /// 序列化数据
        /// </summary>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // 序列化基类属性
            info.AddValue("Id", this.id.ToString());
            info.AddValue("LayerId", this.layerId.ToString());
            info.AddValue("Color", this.color.ToArgb());
            info.AddValue("LineWeight", this.lineWeight);

            // 序列化特有属性
            info.AddValue("ParentElement", ParentElement);
            info.AddValue("SelectAsGroup", SelectAsGroup);
            info.AddValue("OutlineType", OutlineType);
            info.AddValue("IsActive", IsActive);
            info.AddValue("IsVisible", IsVisible);
            info.AddValue("Parameters", _parameters);

            // 序列化子实体（直接序列化Entity对象）
            if (_childEntities != null && _childEntities.Count > 0)
            {
                var childTypes = _childEntities.Select(e => e?.GetType()).ToArray();
                info.AddValue("ChildEntityTypes", childTypes);
                info.AddValue("ChildEntities", _childEntities.ToArray());
            }
            else
            {
                info.AddValue("ChildEntityTypes", new Type[0]);
                info.AddValue("ChildEntities", new Entity[0]);
            }
        }

        #endregion

        #region DTO方法

        /// <summary>
        /// 转换为DTO
        /// </summary>
        public LensOutlineEntityDto ToDto()
        {
            return new LensOutlineEntityDto
            {
                Id = this.id.ToString(),
                SelectAsGroup = this.SelectAsGroup,
                OutlineType = this.OutlineType,
                IsActive = this.IsActive,
                IsVisible = this.IsVisible,
                LayerId = this.layerId.ToString(),
                Color = this.color.ToArgb(),
                Parameters = new Dictionary<string, object>(_parameters),
                ChildEntities = _childEntities.Select(e => SerializeChildEntityToDto(e)).ToList()
            };
        }

        /// <summary>
        /// 从DTO创建
        /// </summary>
        public static LensOutlineEntity FromDto(LensOutlineEntityDto dto, Database database)
        {
            var entity = new LensOutlineEntity
            {
                SelectAsGroup = dto.SelectAsGroup,
                OutlineType = dto.OutlineType,
                IsActive = dto.IsActive,
                IsVisible = dto.IsVisible
            };

            // 恢复ID
            if (Guid.TryParse(dto.Id, out var id))
            {
                // id is read-only, cannot set it directly
            }

            // 恢复图层
            // 注意：由于ObjectId构造函数是internal的，我们无法直接设置layerId
            // 这需要在实际使用时通过数据库或其他方式设置

            // 恢复颜色
            if (dto.Color != 0)
            {
                // 从整数值创建Color
                entity.color = lcdb.Colors.Color.FromRGB((byte)(dto.Color >> 16), (byte)(dto.Color >> 8), (byte)dto.Color);
            }

            // 恢复参数
            if (dto.Parameters != null)
            {
                foreach (var kvp in dto.Parameters)
                {
                    entity._parameters[kvp.Key] = kvp.Value;
                }
            }

            // 恢复子实体
            if (dto.ChildEntities != null)
            {
                foreach (var childDto in dto.ChildEntities)
                {
                    var childEntity = DeserializeChildEntityFromDto(childDto, database);
                    if (childEntity != null)
                    {
                        entity._childEntities.Add(childEntity);
                    }
                }
            }

            return entity;
        }

        /// <summary>
        /// 序列化子实体为DTO
        /// </summary>
        private static object SerializeChildEntityToDto(Entity entity)
        {
            if (entity == null)
                return null;

            // 根据实体类型返回不同的DTO
            switch (entity)
            {
                case Line line:
                    return new
                    {
                        Type = "Line",
                        StartPoint = new { x = line.startPoint.X, y = line.startPoint.Y },
                        EndPoint = new { x = line.endPoint.X, y = line.endPoint.Y },
                        LineType = line.lineType.ToString()
                    };

                case Arc arc:
                    return new
                    {
                        Type = "Arc",
                        Center = new { x = arc.center.X, y = arc.center.Y },
                        Radius = arc.radius,
                        StartAngle = arc.startAngle,
                        EndAngle = arc.endAngle
                    };

                default:
                    return new
                    {
                        Type = entity.GetType().Name,
                        Data = entity.ToString()
                    };
            }
        }

        /// <summary>
        /// 从DTO反序列化子实体
        /// </summary>
        private static Entity DeserializeChildEntityFromDto(object dto, Database database)
        {
            if (dto == null)
                return null;

            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(dto);
                using (var doc = System.Text.Json.JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    var type = root.GetProperty("Type").GetString();

                    switch (type)
                    {
                        case "Line":
                            var startPoint = root.GetProperty("StartPoint");
                            var endPoint = root.GetProperty("EndPoint");
                            var line = new Line(
                                new Vector2(startPoint.GetProperty("x").GetDouble(), startPoint.GetProperty("y").GetDouble()),
                                new Vector2(endPoint.GetProperty("x").GetDouble(), endPoint.GetProperty("y").GetDouble())
                            );
                            if (root.TryGetProperty("LineType", out var lineTypeElement))
                            {
                                if (Enum.TryParse<LineType>(lineTypeElement.GetString(), out var lineType))
                                {
                                    line.lineType = lineType;
                                }
                            }
                            return line;

                        case "Arc":
                            var center = root.GetProperty("Center");
                            return new Arc
                            {
                                center = new Vector2(center.GetProperty("x").GetDouble(), center.GetProperty("y").GetDouble()),
                                radius = root.GetProperty("Radius").GetDouble(),
                                startAngle = root.GetProperty("StartAngle").GetDouble(),
                                endAngle = root.GetProperty("EndAngle").GetDouble()
                            };

                        default:
                            // 未知类型，返回null
                            return null;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region 事件

        public event EventHandler<CompositeEntityEventArgs> PropertyChanged;
        public event EventHandler<ChildEntityChangedEventArgs> ChildEntityChanged;

        protected virtual void OnPropertyChanged(CompositeEntityEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected virtual void OnChildEntityChanged(ChildEntityChangedEventArgs e)
        {
            ChildEntityChanged?.Invoke(this, e);
        }

        #endregion
    }

    #region DTO类

    /// <summary>
    /// LensOutlineEntity的DTO
    /// </summary>
    public class LensOutlineEntityDto
    {
        public string Id { get; set; }
        public bool SelectAsGroup { get; set; }
        public string OutlineType { get; set; }
        public bool IsActive { get; set; }
        public bool IsVisible { get; set; }
        public string LayerId { get; set; }
        public int Color { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public List<object> ChildEntities { get; set; }
    }


    #endregion
}