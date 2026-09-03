using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using lcdb;
using OtoCAD.Interface;
using OtoCAD.Interface.Events;

namespace OtoCAD.Interface.Examples
{
    /// <summary>
    /// 组合实体实现示例 - 光学透镜组件
    /// </summary>
    public class OpticalLensComponent : ICompositeEntity, IParametricEntity
    {
        private readonly List<Entity> _childEntities = new List<Entity>();
        private readonly Dictionary<string, object> _parameters = new Dictionary<string, object>();
        private ObjectId _id;
        private bool _needsRegeneration;

        #region ICompositeEntity 实现

        public ObjectId Id 
        { 
            get => _id; 
            set => _id = value; 
        }

        public IReadOnlyList<Entity> ChildEntities => _childEntities.AsReadOnly();

        public IDictionary<string, object> Parameters => _parameters;

        public bool NeedsRegeneration => _needsRegeneration;

        public event EventHandler<ChildEntityChangedEventArgs> ChildEntityChanged;
        public event EventHandler<ParameterChangedEventArgs> ParameterChanged;
        public event EventHandler<RegenerationEventArgs> BeforeRegeneration;
        public event EventHandler<RegenerationEventArgs> AfterRegeneration;

        public void AddChild(Entity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            _childEntities.Add(entity);
            entity.Source = EntitySource.OpticalElement;
            entity.ParentComponentId = this.Id;
            
            ChildEntityChanged?.Invoke(this, new ChildEntityChangedEventArgs(
                ChildEntityChangeType.Added, entity, _childEntities.Count - 1));
        }

        public bool RemoveChild(Entity entity)
        {
            int index = _childEntities.IndexOf(entity);
            if (index >= 0)
            {
                _childEntities.RemoveAt(index);
                ChildEntityChanged?.Invoke(this, new ChildEntityChangedEventArgs(
                    ChildEntityChangeType.Removed, entity, index));
                return true;
            }
            return false;
        }

        public void ClearChildren()
        {
            _childEntities.Clear();
            ChildEntityChanged?.Invoke(this, new ChildEntityChangedEventArgs(
                ChildEntityChangeType.Cleared, null));
        }

        public void RegenerateChildren()
        {
            var args = new RegenerationEventArgs("Parameter change");
            BeforeRegeneration?.Invoke(this, args);

            try
            {
                // 清除旧的子实体
                ClearChildren();

                // 根据参数生成新的子实体
                var diameter = GetParameter<double>("Diameter");
                var thickness = GetParameter<double>("Thickness");
                var curvature = GetParameter<double>("Curvature");

                // 创建透镜轮廓
                var outline = CreateLensOutline(diameter, thickness, curvature);
                AddChild(outline);

                // 创建光轴
                var axis = CreateOpticalAxis(diameter);
                AddChild(axis);

                // 创建焦点标记
                var focalPoint = CreateFocalPoint(diameter, curvature);
                AddChild(focalPoint);

                args.Success = true;
                args.GeneratedCount = _childEntities.Count;
                _needsRegeneration = false;
            }
            catch (Exception ex)
            {
                args.Success = false;
                args.ErrorMessage = ex.Message;
            }

            AfterRegeneration?.Invoke(this, args);
        }

        public void UpdateParameter(string parameterName, object value)
        {
            var oldValue = _parameters.ContainsKey(parameterName) ? _parameters[parameterName] : null;
            _parameters[parameterName] = value;
            _needsRegeneration = true;

            ParameterChanged?.Invoke(this, new ParameterChangedEventArgs(
                parameterName, oldValue, value, true));
        }

        public T GetParameter<T>(string parameterName)
        {
            if (_parameters.TryGetValue(parameterName, out var value))
            {
                return (T)value;
            }
            return default(T);
        }

        public bool ValidateParameters()
        {
            // 验证透镜参数
            var diameter = GetParameter<double>("Diameter");
            var thickness = GetParameter<double>("Thickness");
            
            return diameter > 0 && thickness > 0 && diameter > thickness;
        }

        #endregion

        #region IParametricEntity 实现

        public IReadOnlyList<ParameterDefinition> GetParameterDefinitions()
        {
            return new List<ParameterDefinition>
            {
                new ParameterDefinition
                {
                    Name = "Diameter",
                    DisplayName = "透镜直径",
                    ParameterType = typeof(double),
                    DefaultValue = 50.0,
                    Unit = "mm",
                    IsRequired = true
                },
                new ParameterDefinition
                {
                    Name = "Thickness",
                    DisplayName = "中心厚度",
                    ParameterType = typeof(double),
                    DefaultValue = 10.0,
                    Unit = "mm",
                    IsRequired = true
                },
                new ParameterDefinition
                {
                    Name = "Curvature",
                    DisplayName = "曲率半径",
                    ParameterType = typeof(double),
                    DefaultValue = 100.0,
                    Unit = "mm",
                    IsRequired = true
                },
                new ParameterDefinition
                {
                    Name = "Material",
                    DisplayName = "材料",
                    ParameterType = typeof(string),
                    DefaultValue = "BK7",
                    Category = "光学属性"
                }
            };
        }

        public object GetParameterValue(string parameterName)
        {
            return GetParameter<object>(parameterName);
        }

        public bool SetParameterValue(string parameterName, object value)
        {
            if (ValidateParameter(parameterName, value))
            {
                UpdateParameter(parameterName, value);
                return true;
            }
            return false;
        }

        public bool ValidateParameter(string parameterName, object value)
        {
            var constraint = GetParameterConstraint(parameterName);
            return constraint?.IsValid(value) ?? true;
        }

        public IParameterConstraint GetParameterConstraint(string parameterName)
        {
            switch (parameterName)
            {
                case "Diameter":
                case "Thickness":
                case "Curvature":
                    return new RangeConstraint { MinValue = 0.1, MaxValue = 1000 };
                case "Material":
                    return new EnumConstraint 
                    { 
                        AllowedValues = new List<object> { "BK7", "Fused Silica", "SF11", "LAK9" } 
                    };
                default:
                    return null;
            }
        }

        public void ApplyParameterChanges()
        {
            if (_needsRegeneration)
            {
                RegenerateChildren();
            }
        }

        public void ResetToDefaults()
        {
            foreach (var def in GetParameterDefinitions())
            {
                _parameters[def.Name] = def.DefaultValue;
            }
            _needsRegeneration = true;
        }

        public event EventHandler<ParameterChangeEventArgs> ParameterChanging;
        public event EventHandler<ParameterChangeEventArgs> ParameterChanged2;

        #endregion

        #region 辅助方法

        private Entity CreateLensOutline(double diameter, double thickness, double curvature)
        {
            // 创建透镜轮廓的示例代码
            // 实际实现会创建弧线和直线组成透镜形状
            return new Line();
        }

        private Entity CreateOpticalAxis(double diameter)
        {
            // 创建光轴线
            return new Line();
        }

        private Entity CreateFocalPoint(double diameter, double curvature)
        {
            // 创建焦点标记
            return new Circle();
        }

        #endregion
    }

    /// <summary>
    /// 实体生成器示例 - 光栅生成器
    /// </summary>
    public class GratingGenerator : IParametricGenerator
    {
        private readonly Dictionary<string, object> _parameters = new Dictionary<string, object>();

        public string GeneratorName => "Optical Grating Generator";
        public string Version => "1.0.0";

        public event EventHandler<GenerationEventArgs> BeforeGeneration;
        public event EventHandler<GenerationEventArgs> AfterGeneration;
        public event EventHandler<GenerationErrorEventArgs> GenerationError;

        public bool CanGenerate(GenerationContext context)
        {
            return context != null && 
                   context.Parameters.ContainsKey("LineCount") &&
                   context.Parameters.ContainsKey("Spacing");
        }

        public IEnumerable<Entity> Generate(GenerationContext context)
        {
            var startTime = DateTime.Now;
            var args = new GenerationEventArgs { Context = context };
            
            BeforeGeneration?.Invoke(this, args);

            var entities = new List<Entity>();

            try
            {
                int lineCount = Convert.ToInt32(context.Parameters["LineCount"]);
                double spacing = Convert.ToDouble(context.Parameters["Spacing"]);
                double width = Convert.ToDouble(context.Parameters.GetValueOrDefault("Width", 100.0));

                for (int i = 0; i < lineCount; i++)
                {
                    var line = new Line
                    {
                        StartPoint = new Point2d(0, i * spacing),
                        EndPoint = new Point2d(width, i * spacing),
                        Source = EntitySource.OpticalElement,
                        ParentComponentId = context.ParentId
                    };
                    entities.Add(line);
                }

                args.GeneratedEntities = entities;
                args.GenerationTime = DateTime.Now - startTime;
                AfterGeneration?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                var errorArgs = new GenerationErrorEventArgs
                {
                    Exception = ex,
                    ErrorMessage = $"生成光栅失败: {ex.Message}",
                    Context = context
                };
                GenerationError?.Invoke(this, errorArgs);
                
                if (!errorArgs.Handled)
                    throw;
            }

            return entities;
        }

        public IEnumerable<Entity> Preview(GenerationContext context)
        {
            // 生成预览（简化版）
            context.Options.PreviewOnly = true;
            return Generate(context).Take(5); // 只显示前5条线作为预览
        }

        public ValidationResult ValidateParameters(GenerationContext context)
        {
            var result = new ValidationResult();

            if (!context.Parameters.ContainsKey("LineCount"))
                result.AddError("缺少必需参数: LineCount");

            if (!context.Parameters.ContainsKey("Spacing"))
                result.AddError("缺少必需参数: Spacing");

            if (context.Parameters.TryGetValue("LineCount", out var lineCount))
            {
                int count = Convert.ToInt32(lineCount);
                if (count <= 0 || count > 1000)
                    result.AddError("LineCount 必须在 1-1000 之间");
            }

            if (context.Parameters.TryGetValue("Spacing", out var spacing))
            {
                double space = Convert.ToDouble(spacing);
                if (space <= 0)
                    result.AddError("Spacing 必须大于 0");
            }

            return result;
        }

        public int EstimateEntityCount(GenerationContext context)
        {
            if (context.Parameters.TryGetValue("LineCount", out var lineCount))
                return Convert.ToInt32(lineCount);
            return 0;
        }

        public IReadOnlyList<GeneratorParameterDefinition> GetParameterDefinitions()
        {
            return new List<GeneratorParameterDefinition>
            {
                new GeneratorParameterDefinition
                {
                    Name = "LineCount",
                    DisplayName = "线条数量",
                    ParameterType = typeof(int),
                    DefaultValue = 10,
                    IsRequired = true,
                    Constraint = new RangeConstraint { MinValue = 1, MaxValue = 1000 }
                },
                new GeneratorParameterDefinition
                {
                    Name = "Spacing",
                    DisplayName = "间距",
                    ParameterType = typeof(double),
                    DefaultValue = 1.0,
                    IsRequired = true,
                    Constraint = new RangeConstraint { MinValue = 0.01, MaxValue = 100 }
                },
                new GeneratorParameterDefinition
                {
                    Name = "Width",
                    DisplayName = "宽度",
                    ParameterType = typeof(double),
                    DefaultValue = 100.0,
                    IsRequired = false
                }
            };
        }

        public void SetParameter(string name, object value)
        {
            _parameters[name] = value;
        }

        public object GetParameter(string name)
        {
            return _parameters.GetValueOrDefault(name);
        }

        public void ResetParameters()
        {
            _parameters.Clear();
            foreach (var def in GetParameterDefinitions())
            {
                _parameters[def.Name] = def.DefaultValue;
            }
        }

        public void LoadTemplate(string templateName)
        {
            // 从模板加载参数
            // 实际实现会从文件或数据库加载
        }

        public void SaveTemplate(string templateName)
        {
            // 保存当前参数为模板
            // 实际实现会保存到文件或数据库
        }
    }

    /// <summary>
    /// 使用示例类
    /// </summary>
    public static class UsageExamples
    {
        /// <summary>
        /// 示例1: 创建和使用组合实体
        /// </summary>
        public static void CompositeEntityExample()
        {
            // 创建光学透镜组件
            var lens = new OpticalLensComponent();
            lens.Id = ObjectId.NewObjectId();

            // 设置参数
            lens.SetParameterValue("Diameter", 50.0);
            lens.SetParameterValue("Thickness", 10.0);
            lens.SetParameterValue("Curvature", 100.0);
            lens.SetParameterValue("Material", "BK7");

            // 订阅事件
            lens.ParameterChanged += (sender, e) =>
            {
                Console.WriteLine($"参数变化: {e.ParameterName} 从 {e.OldValue} 变为 {e.NewValue}");
            };

            lens.AfterRegeneration += (sender, e) =>
            {
                Console.WriteLine($"重新生成完成: 生成了 {e.GeneratedCount} 个实体");
            };

            // 生成子实体
            lens.RegenerateChildren();

            // 修改参数并重新生成
            lens.SetParameterValue("Diameter", 60.0);
            lens.ApplyParameterChanges();
        }

        /// <summary>
        /// 示例2: 使用实体生成器
        /// </summary>
        public static void EntityGeneratorExample()
        {
            // 创建生成器
            var generator = new GratingGenerator();
            
            // 创建生成上下文
            var context = new GenerationContext
            {
                ParentId = ObjectId.NewObjectId(),
                Parameters = new Dictionary<string, object>
                {
                    ["LineCount"] = 20,
                    ["Spacing"] = 2.5,
                    ["Width"] = 150.0
                },
                Options = new GenerationOptions
                {
                    Quality = QualityLevel.High,
                    MergeDuplicates = true
                }
            };

            // 验证参数
            var validation = generator.ValidateParameters(context);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                {
                    Console.WriteLine($"错误: {error}");
                }
                return;
            }

            // 预览生成
            var preview = generator.Preview(context);
            Console.WriteLine($"预览: 将生成 {generator.EstimateEntityCount(context)} 个实体");

            // 实际生成
            var entities = generator.Generate(context);
            foreach (var entity in entities)
            {
                Console.WriteLine($"生成实体: {entity.GetType().Name}");
            }
        }

        /// <summary>
        /// 示例3: 使用容器管理实体
        /// </summary>
        public static void EntityContainerExample()
        {
            // 创建索引容器
            var container = new IndexedEntityContainer(ObjectId.NewObjectId());

            // 订阅事件
            container.EntityAdded += (sender, e) =>
            {
                Console.WriteLine($"添加实体: {e.Entity.GetType().Name}");
            };

            // 添加实体
            container.Add(new Line());
            container.Add(new Circle());
            container.Add(new Arc());

            // 按类型查找
            var circles = container.GetByType<Circle>();
            Console.WriteLine($"找到 {circles.Count()} 个圆");

            // 使用索引访问
            var firstEntity = container[0];
            Console.WriteLine($"第一个实体: {firstEntity.GetType().Name}");

            // 交换位置
            container.Swap(0, 1);
        }

        /// <summary>
        /// 示例4: 序列化策略使用
        /// </summary>
        public static void SerializationExample()
        {
            // 创建序列化管理器
            var manager = new SerializationManager();

            // 创建要序列化的对象
            var lens = new OpticalLensComponent();
            lens.SetParameterValue("Diameter", 50.0);

            // JSON序列化
            using (var stream = new MemoryStream())
            {
                manager.Serialize(lens, stream, SerializationFormat.Json);
                stream.Position = 0;
                
                // 反序列化
                var restored = manager.Deserialize<OpticalLensComponent>(stream, SerializationFormat.Json);
                Console.WriteLine($"恢复的透镜直径: {restored.GetParameter<double>("Diameter")}");
            }
        }

        /// <summary>
        /// 示例5: 事件聚合器使用
        /// </summary>
        public static void EventAggregatorExample()
        {
            // 创建事件聚合器
            var aggregator = new EventAggregator();

            // 订阅事件
            aggregator.Subscribe<EntityChangeEventArgs>(e =>
            {
                Console.WriteLine($"实体变化: {e.ChangeType} - {e.Entity?.GetType().Name}");
            });

            // 发布事件
            var changeEvent = new EntityChangeEventArgs(EntityChangeType.Created, new Line());
            aggregator.Publish(changeEvent);
        }
    }
}