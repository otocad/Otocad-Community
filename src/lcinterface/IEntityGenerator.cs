using System;
using System.Collections.Generic;
using lcdb;

namespace OtoCAD.Interface
{
    /// <summary>
    /// 实体生成器接口 - 定义从参数生成实体的行为
    /// </summary>
    public interface IEntityGenerator
    {
        /// <summary>
        /// 生成器名称
        /// </summary>
        string GeneratorName { get; }

        /// <summary>
        /// 生成器版本
        /// </summary>
        string Version { get; }

        /// <summary>
        /// 是否可以生成
        /// </summary>
        bool CanGenerate(GenerationContext context);

        /// <summary>
        /// 生成实体
        /// </summary>
        IEnumerable<Entity> Generate(GenerationContext context);

        /// <summary>
        /// 预览生成结果
        /// </summary>
        IEnumerable<Entity> Preview(GenerationContext context);

        /// <summary>
        /// 验证生成参数
        /// </summary>
        ValidationResult ValidateParameters(GenerationContext context);

        /// <summary>
        /// 估算生成实体数量
        /// </summary>
        int EstimateEntityCount(GenerationContext context);

        /// <summary>
        /// 生成前事件
        /// </summary>
        event EventHandler<GenerationEventArgs> BeforeGeneration;

        /// <summary>
        /// 生成后事件
        /// </summary>
        event EventHandler<GenerationEventArgs> AfterGeneration;

        /// <summary>
        /// 生成错误事件
        /// </summary>
        event EventHandler<GenerationErrorEventArgs> GenerationError;
    }

    /// <summary>
    /// 参数化实体生成器
    /// </summary>
    public interface IParametricGenerator : IEntityGenerator
    {
        /// <summary>
        /// 获取参数定义
        /// </summary>
        IReadOnlyList<GeneratorParameterDefinition> GetParameterDefinitions();

        /// <summary>
        /// 设置参数值
        /// </summary>
        void SetParameter(string name, object value);

        /// <summary>
        /// 获取参数值
        /// </summary>
        object GetParameter(string name);

        /// <summary>
        /// 重置参数到默认值
        /// </summary>
        void ResetParameters();

        /// <summary>
        /// 从模板加载参数
        /// </summary>
        void LoadTemplate(string templateName);

        /// <summary>
        /// 保存当前参数为模板
        /// </summary>
        void SaveTemplate(string templateName);
    }

    /// <summary>
    /// 批量生成器接口
    /// </summary>
    public interface IBatchGenerator : IEntityGenerator
    {
        /// <summary>
        /// 批量生成
        /// </summary>
        IEnumerable<BatchGenerationResult> GenerateBatch(IEnumerable<GenerationContext> contexts);

        /// <summary>
        /// 支持的最大批量大小
        /// </summary>
        int MaxBatchSize { get; }

        /// <summary>
        /// 是否支持并行生成
        /// </summary>
        bool SupportsParallel { get; }

        /// <summary>
        /// 批量生成进度事件
        /// </summary>
        event EventHandler<BatchProgressEventArgs> BatchProgress;
    }

    /// <summary>
    /// 生成上下文
    /// </summary>
    public class GenerationContext
    {
        /// <summary>
        /// 父组件ID
        /// </summary>
        public ObjectId ParentId { get; set; }

        /// <summary>
        /// 生成参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; }

        /// <summary>
        /// 目标数据库
        /// </summary>
        public Database TargetDatabase { get; set; }

        /// <summary>
        /// 目标图层
        /// </summary>
        public ObjectId LayerId { get; set; }

        /// <summary>
        /// 变换矩阵
        /// </summary>
        public Matrix3d TransformMatrix { get; set; }

        /// <summary>
        /// 生成选项
        /// </summary>
        public GenerationOptions Options { get; set; }

        /// <summary>
        /// 用户数据
        /// </summary>
        public object UserData { get; set; }

        public GenerationContext()
        {
            Parameters = new Dictionary<string, object>();
            Options = new GenerationOptions();
            TransformMatrix = Matrix3d.Identity;
        }
    }

    /// <summary>
    /// 生成选项
    /// </summary>
    public class GenerationOptions
    {
        /// <summary>
        /// 是否生成预览
        /// </summary>
        public bool PreviewOnly { get; set; }

        /// <summary>
        /// 是否保留原始实体
        /// </summary>
        public bool KeepOriginal { get; set; }

        /// <summary>
        /// 是否合并重复
        /// </summary>
        public bool MergeDuplicates { get; set; }

        /// <summary>
        /// 是否应用样式
        /// </summary>
        public bool ApplyStyles { get; set; }

        /// <summary>
        /// 生成质量级别
        /// </summary>
        public QualityLevel Quality { get; set; } = QualityLevel.Normal;

        /// <summary>
        /// 超时时间（毫秒）
        /// </summary>
        public int TimeoutMs { get; set; } = 30000;
    }

    /// <summary>
    /// 质量级别
    /// </summary>
    public enum QualityLevel
    {
        /// <summary>
        /// 草稿质量
        /// </summary>
        Draft,

        /// <summary>
        /// 正常质量
        /// </summary>
        Normal,

        /// <summary>
        /// 高质量
        /// </summary>
        High,

        /// <summary>
        /// 最高质量
        /// </summary>
        Maximum
    }

    /// <summary>
    /// 生成器参数定义
    /// </summary>
    public class GeneratorParameterDefinition
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 参数类型
        /// </summary>
        public Type ParameterType { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// 是否必需
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 参数约束
        /// </summary>
        public IParameterConstraint Constraint { get; set; }

        /// <summary>
        /// 参数组
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int Order { get; set; }
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public List<string> Errors { get; set; }

        /// <summary>
        /// 警告消息
        /// </summary>
        public List<string> Warnings { get; set; }

        public ValidationResult()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
            IsValid = true;
        }

        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }

    /// <summary>
    /// 生成事件参数
    /// </summary>
    public class GenerationEventArgs : EventArgs
    {
        /// <summary>
        /// 生成上下文
        /// </summary>
        public GenerationContext Context { get; set; }

        /// <summary>
        /// 生成的实体
        /// </summary>
        public IEnumerable<Entity> GeneratedEntities { get; set; }

        /// <summary>
        /// 是否取消
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// 生成时间
        /// </summary>
        public TimeSpan GenerationTime { get; set; }
    }

    /// <summary>
    /// 生成错误事件参数
    /// </summary>
    public class GenerationErrorEventArgs : EventArgs
    {
        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 异常
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// 生成上下文
        /// </summary>
        public GenerationContext Context { get; set; }

        /// <summary>
        /// 是否已处理
        /// </summary>
        public bool Handled { get; set; }
    }

    /// <summary>
    /// 批量生成结果
    /// </summary>
    public class BatchGenerationResult
    {
        /// <summary>
        /// 上下文
        /// </summary>
        public GenerationContext Context { get; set; }

        /// <summary>
        /// 生成的实体
        /// </summary>
        public IEnumerable<Entity> Entities { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 批量进度事件参数
    /// </summary>
    public class BatchProgressEventArgs : EventArgs
    {
        /// <summary>
        /// 总数
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 已完成数
        /// </summary>
        public int Completed { get; set; }

        /// <summary>
        /// 当前项
        /// </summary>
        public GenerationContext CurrentItem { get; set; }

        /// <summary>
        /// 进度百分比
        /// </summary>
        public double ProgressPercentage => Total > 0 ? (double)Completed / Total * 100 : 0;
    }

    /// <summary>
    /// 生成器工厂
    /// </summary>
    public class GeneratorFactory
    {
        private static readonly Dictionary<string, IEntityGenerator> _generators = new Dictionary<string, IEntityGenerator>();

        /// <summary>
        /// 注册生成器
        /// </summary>
        public static void RegisterGenerator(string name, IEntityGenerator generator)
        {
            _generators[name] = generator;
        }

        /// <summary>
        /// 获取生成器
        /// </summary>
        public static IEntityGenerator GetGenerator(string name)
        {
            return _generators.TryGetValue(name, out var generator) ? generator : null;
        }

        /// <summary>
        /// 获取所有生成器名称
        /// </summary>
        public static IEnumerable<string> GetGeneratorNames()
        {
            return _generators.Keys;
        }

        /// <summary>
        /// 移除生成器
        /// </summary>
        public static bool RemoveGenerator(string name)
        {
            return _generators.Remove(name);
        }
    }
}