using LitMath;
using lcdb.Colors;
using lcdb.Annotation;
using OtoCAD.OpticEntity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using OtoCAD;

namespace lcdb
{
    /// <summary>
    /// 实体来源类型枚举
    /// </summary>
    public enum EntitySource
    {
        /// <summary>
        /// 用户手动创建
        /// </summary>
        Manual = 0,
        /// <summary>
        /// 由光学元件生成
        /// </summary>
        OpticalElement = 1,
        /// <summary>
        /// 由光学框架生成
        /// </summary>
        OpticalFrame = 2,
        /// <summary>
        /// 由材料组件生成
        /// </summary>
        Material = 3,
        /// <summary>
        /// 由镀膜组件生成
        /// </summary>
        Coating = 4,
        /// <summary>
        /// 由其他组件生成
        /// </summary>
        OtherComponent = 5
    }

    /// <summary>
    /// 实体编辑模式枚举 - 标识实体属于Frame还是Drawing
    /// </summary>
    public enum EntityEditMode
    {
        /// <summary>
        /// 绘图模式实体 - 普通绘图内容
        /// </summary>
        Drawing = 0,
        /// <summary>
        /// 图框模式实体 - 图框内容
        /// </summary>
        Frame = 1
    }
    // JsonDerivedType attributes are not supported in .NET Framework 4.8
    // These would be needed for polymorphic JSON serialization in .NET 7+
    // For .NET Framework, we need to use custom converters or other approaches
    [Serializable]

    public abstract class Entity : DBObject
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className
        {
            get { return "Entity"; }
        }
        /// <summary>
        /// 所有者（支持新旧两种架构）
        /// </summary>
        [JsonIgnore]

        public object Owner { get; set; } // BaseElementBlock

        /// <summary>
        /// 关联刷新钩子 (会话级, 不序列化)。出图生成时由布局层注入: 闭包捕获锚定对象(透镜)+ 重算几何,
        /// 渲染前统一调用 → 锚定对象一变(改口径/厚度/半径/平移), 标记/标注的锚点跟随重derive。
        /// 返回是否发生变化 (实现内部应做变化门控, 无变化零开销, 避免每帧重建)。
        /// 线性标注另走 <see cref="LinearDimension.RefreshFromAnchors"/> 的数据绑定(支持夹点改锚)。
        /// </summary>
        [JsonIgnore]
        public System.Func<bool>? AssociativeRefresh { get; set; }
        /// <summary>
        /// 实体来源标记
        /// </summary>
        private EntitySource _source = EntitySource.Manual;

        [JsonInclude]
        public EntitySource Source
        {
            get { return _source; }
            set { _source = value; }
        }

        /// <summary>
        /// 实体编辑模式 - 标识实体属于Frame还是Drawing
        /// </summary>
        private EntityEditMode _editMode = EntityEditMode.Drawing;

        [JsonInclude]
        public EntityEditMode EditMode
        {
            get { return _editMode; }
            set { _editMode = value; }
        }

        /// <summary>
        /// 父组件ID（关联组件生成的实体）
        /// </summary>
        private ObjectId? _parentComponentId = null;

        public ObjectId? ParentComponentId
        {
            get { return _parentComponentId; }
            set { _parentComponentId = value; }
        }

        /// <summary>
        /// 是否为生成的实体（由BaseElementBlock生成）
        /// </summary>
        private bool _isGenerated = false;

        [JsonInclude]
        public bool IsGenerated
        {
            get { return _isGenerated; }
            set { _isGenerated = value; }
        }

        /// <summary>
        /// 是否锁定（防止重新生成时被覆盖）
        /// </summary>
        private bool _isLocked = false;

        [JsonInclude]
        public bool IsLocked
        {
            get { return _isLocked; }
            set { _isLocked = value; }
        }

        /// <summary>
        /// 父级BaseElementBlock的ID（用于关联生成实体与父级）
        /// </summary>
        private ObjectId _parentBlockId = ObjectId.Null;

        [JsonInclude]
        public ObjectId ParentBlockId
        {
            get { return _parentBlockId; }
            set { _parentBlockId = value; }
        }
        /// <summary>
        /// 边界框
        /// </summary>
        public abstract Bounding bounding
        {
            get;
        }

        /// <summary>
        /// 线型
        /// </summary>
        private LineType _linetype = LineType.ByLayer;

        public LineType lineType
        {
            get { return _linetype; }
            set { _linetype = value; }
        }
        /// <summary>
        /// 颜色
        /// </summary>
        private Color _color = Color.ByLayer;

        public Color color
        {
            get { return _color; }
            set { _color = value; }
        }

        /// <summary>
        /// 解析后的颜色 (lcdb 原生类型, 解析 ByBlock/ByLayer 引用为具体 RGB)。
        /// 优先使用此 API; <see cref="colorValue"/> 仅为 System.Drawing 桥接, 未来废弃。
        /// </summary>
        public Color resolvedColor
        {
            get
            {
                switch (_color.colorMethod)
                {
                    case ColorMethod.ByBlock:
                        if (this.parent is BlockReference blockRef)
                            return blockRef.resolvedColor;
                        return Color.FromRGB(_color.r, _color.g, _color.b);
                    case ColorMethod.ByLayer:
                        Database db = this.database;
                        if (db != null && db.layerTable.Has(this.layer))
                        {
                            Layer layer = db.layerTable[this.layer] as Layer;
                            return layer.color;
                        }
                        return Color.FromRGB(_color.r, _color.g, _color.b);
                    case ColorMethod.ByColor:
                    case ColorMethod.ByEntity:
                    case ColorMethod.None:
                    default:
                        return Color.FromRGB(_color.r, _color.g, _color.b);
                }
            }
        }

        public System.Drawing.Color colorValue
        {
            get
            {
                var c = this.resolvedColor;
                return System.Drawing.Color.FromArgb(c.r, c.g, c.b);
            }
        }
        /// <summary>
        /// 线宽
        /// </summary>
        private LineWeight _lineWeight = LineWeight.ByLayer;

        public LineWeight lineWeight
        {
            get { return _lineWeight; }
            set { _lineWeight = value; }
        }
        /// <summary>
        /// 图层
        /// </summary>
        private ObjectId _layerId = ObjectId.Null;

        public ObjectId layerId
        {
            get { return _layerId; }
            set { _layerId = value; }
        }

        public string layer
        {
            get
            {
                Database db = this.database;
                if (db != null 
                    && _layerId != ObjectId.Null
                    && db.layerTable.Has(_layerId))
                {
                    Layer layerRecord = db.GetObject(_layerId) as Layer;
                    if (layerRecord != null)
                    {
                        return layerRecord.name;
                    }
                }
                return "";
            }
            set
            {
                Database db = this.database;
                if (db != null
                    && db.layerTable.Has(value))
                {
                    _layerId = db.layerTable[value].id;
                }
            }
        }
        /// <summary>
        /// 绘制方法
        /// </summary>
        public virtual void Draw(IGraphicsDraw gd)
        {
        }
        /// <summary>
        /// 克隆方法
        /// </summary>
        public override object Clone()
        {
            Entity entity = base.Clone() as Entity;
            entity._color = _color;
            entity._layerId = _layerId;
            entity._source = _source;
            entity._parentComponentId = _parentComponentId;
            entity._isGenerated = _isGenerated;
            entity._isLocked = _isLocked;
            entity._parentBlockId = _parentBlockId;
            entity._lineWeight = _lineWeight;
            entity._linetype = _linetype;
            return entity;
        }
        /// <summary>
        /// 清空内部缓存 (例如 OpticalLens/CementedLens 的 _markEntities 派生几何缓存).
        /// 当外部修改了影响渲染的属性 (Diameter/Thickness/R1/R2/Material 等) 后,
        /// 调用方应触发本方法让下一次 Draw 重新 Generate.
        /// 默认 no-op (普通几何实体 Line/Circle/Arc 没有派生缓存).
        /// </summary>
        public virtual void InvalidateRenderCache() { }

        /// <summary>
        /// 平移
        /// </summary>
        public abstract void Translate(LitMath.Vector2 translation);
        /// <summary>
        /// 旋转
        /// </summary>
        public abstract void Rotate(LitMath.Vector2 center, double angle);
        /// <summary>
        /// Transform
        /// 注意: 不要在这里调用其他变换函数
        /// </summary>
        public abstract void TransformBy(LitMath.Matrix3 transform);
        /// <summary>
        /// 删除
        /// </summary>
        protected override void _Erase()
        {
            if (_parent != null)
            {
                Block block = _parent as Block;
                block.RemoveEntity(this);
            }
        }
        /// <summary>
        /// 捕捉点
        /// </summary>
        public virtual List<ObjectSnapPoint> GetSnapPoints()
        {
            return null;
        }
        /// <summary>
        /// 夺点
        /// </summary>
        public virtual List<GripPoint> GetGripPoints()
        {
            return null;
        }
        /// <summary>
        /// 设置夺点
        /// </summary>
        public virtual void SetGripPointAt(int index, GripPoint gripPoint, LitMath.Vector2 newPosition)
        {
        }

        public delegate void GripPointAtEvent(int index, GripPoint gripPoint, LitMath.Vector2 newPosition);

        public event GripPointAtEvent OnGripPointAtEvent;

        public virtual void SetGripPointAtFinished(int index, GripPoint gripPoint, LitMath.Vector2 newPosition)
        {
            if (OnGripPointAtEvent != null)
            {
                OnGripPointAtEvent.Invoke(index, gripPoint, newPosition);
            }
        }
        /// <summary>
        /// ͨ参数名
        /// </summary>
        public void NotifyModified()
        {
            // ͨ更新变更类型
            if (this.database != null)
            {
                this.database.NotifyObjectModified(this);
            }
        }
    }
    /// <summary>
    /// 统一接口，用于桥接 BaseElementBlock 和 CompositeEntity 两个架构
    /// </summary>
    public interface IComposableEntity
    {
        /// <summary>
        /// 获取子实体列表
        /// </summary>
        IReadOnlyList<Entity> ChildEntities { get; }
        /// <summary>
        /// 获取参数字典
        /// </summary>
        IReadOnlyDictionary<string, object> Parameters { get; }
        /// <summary>
        /// 获取或设置实体ID
        /// </summary>
        ObjectId Id { get; set; }
        /// <summary>
        /// 获取或设置是否激活
        /// </summary>
        bool IsActive { get; set; }
        /// <summary>
        /// 获取或设置是否可见
        /// </summary>
        bool IsVisible { get; set; }
        /// <summary>
        /// 更新子实体
        /// </summary>
        void Regenerate();
        /// <summary>
        /// 添加子实体
        /// </summary>
        /// <param name="entity">要添加的实体</param>
        void AddChild(Entity entity);
        /// <summary>
        /// 移除子实体
        /// </summary>
        /// <param name="id">要移除的实体ID</param>
        bool RemoveChild(ObjectId id);
        /// <summary>
        /// 清空所有子实体
        /// </summary>
        void ClearChildren();
        /// <summary>
        /// 获取参数值
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="key">参数名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>参数值</returns>
        T GetParameter<T>(string key, T defaultValue = default(T));
        /// <summary>
        /// 设置参数值
        /// </summary>
        /// <param name="key">参数名</param>
        /// <param name="value">参数值</param>
        void SetParameter(string key, object value);
        /// <summary>
        /// 验证参数
        /// </summary>
        /// <returns>参数是否有效</returns>
        bool ValidateParameters();
        /// <summary>
        /// 获取边界框
        /// </summary>
        Bounding GetBounding();
        /// <summary>
        /// 属性变更事件
        /// </summary>
        event EventHandler<CompositeEntityEventArgs> PropertyChanged;
        /// <summary>
        /// 子实体变更事件
        /// </summary>
        event EventHandler<ChildEntityChangedEventArgs> ChildEntityChanged;
    }
    /// <summary>
    /// 组合实体事件参数
    /// </summary>
    public class CompositeEntityEventArgs : EventArgs
    {
        /// <summary>
        /// 属性名称
        /// </summary>
        public string PropertyName { get; set; }
        /// <summary>
        /// 旧值
        /// </summary>
        public object OldValue { get; set; }
        /// <summary>
        /// 新值
        /// </summary>
        public object NewValue { get; set; }
        /// <summary>
        /// 变更类型
        /// </summary>
        public CompositeChangeType Type { get; set; }
        /// <summary>
        /// 受影响的实体
        /// </summary>
        public Entity AffectedEntity { get; set; }
        /// <summary>
        /// 构造函数
        /// </summary>
        public CompositeEntityEventArgs(string propertyName, object oldValue, object newValue, CompositeChangeType type = CompositeChangeType.PropertyChanged)
        {
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
            Type = type;
        }
    }
    /// <summary>
    /// 子实体变更事件参数
    /// </summary>
    public class ChildEntityChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 变更类型
        /// </summary>
        public ChildChangeType ChangeType { get; set; }
        /// <summary>
        /// 子实体
        /// </summary>
        public Entity ChildEntity { get; set; }
        /// <summary>
        /// （仅用于替换操作）
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// 构造函数
        /// </summary>
        public ChildEntityChangedEventArgs(ChildChangeType changeType, Entity childEntity, int index = -1)
        {
            ChangeType = changeType;
            ChildEntity = childEntity;
            Index = index;
        }
    }
    /// <summary>
    /// 组合实体变更类型枚举
    /// </summary>
    public enum CompositeChangeType
    {
        /// <summary>
        /// 属性变更
        /// </summary>
        PropertyChanged,
        /// <summary>
        /// 子实体添加
        /// </summary>
        ChildAdded,
        /// <summary>
        /// 子实体移除
        /// </summary>
        ChildRemoved,
        /// <summary>
        /// 参数变更
        /// </summary>
        ParameterUpdated,
        /// <summary>
        /// 状态变更
        /// </summary>
        Regenerated
    }
    /// <summary>
    /// 子实体变更类型
    /// </summary>
    public enum ChildChangeType
    {
        /// <summary>
        /// 添加
        /// </summary>
        Added,
        /// <summary>
        /// 移除
        /// </summary>
        Removed,
        /// <summary>
        /// 更新
        /// </summary>
        Cleared,
        /// <summary>
        /// 替换
        /// </summary>
        Replaced
    }
    /// <summary>
    /// 参数约束类
    /// </summary>
    public class ParameterConstraint
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 参数类型
        /// </summary>
        public Type DataType { get; set; }
        /// <summary>
        /// 最小值（可选）
        /// </summary>
        public object MinValue { get; set; }
        /// <summary>
        /// 最大值（可选）
        /// </summary>
        public object MaxValue { get; set; }
        /// <summary>
        /// 默认值
        /// </summary>
        public object DefaultValue { get; set; }
        /// <summary>
        /// 是否必须
        /// </summary>
        public bool IsRequired { get; set; }
        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }
        /// <summary>
        /// 自定义验证器
        /// </summary>
        public Func<object, bool> Validator { get; set; }
        /// <summary>
        /// 参数更新添加
        /// </summary>
        public object[] AllowedValues { get; set; }
        /// <summary>
        /// 是否必须
        /// </summary>
        public bool Validate(object value, out string error)
        {
            error = null;
            // 检查必须
            if (IsRequired && value == null)
            {
                error = ErrorMessage ?? $"{Name} 是必须的参数";
                return false;
            }
            // 如果值为null且不是必须的，直接通过
            if (value == null)
                return true;
            // 检查类型
            if (DataType != null && !DataType.IsAssignableFrom(value.GetType()))
            {
                error = $"{Name} 类型不匹配，期望 {DataType.Name}，实际 {value.GetType().Name}";
                return false;
            }
            // 检查允许的值列表
            if (AllowedValues != null && AllowedValues.Length > 0)
            {
                bool found = false;
                foreach (var allowed in AllowedValues)
                {
                    if (Equals(value, allowed))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    error = ErrorMessage ?? $"{Name} 的值必须是允许值之一";
                    return false;
                }
            }
            // 检查范围
            if (MinValue != null && Comparer.Default.Compare(value, MinValue) < 0)
            {
                error = ErrorMessage ?? $"{Name} 的值必须大于或等于 {MinValue}";
                return false;
            }
            if (MaxValue != null && Comparer.Default.Compare(value, MaxValue) > 0)
            {
                error = ErrorMessage ?? $"{Name} 的值必须小于或等于 {MaxValue}";
                return false;
            }
            // 自定义验证器
            if (Validator != null && !Validator(value))
            {
                error = ErrorMessage ?? $"{Name} 验证失败";
                return false;
            }
            return true;
        }
    }
}
