using System;
using System.Collections.Generic;
using lcdb;

namespace OtoCAD.Interface.Events
{
    #region 委托定义

    /// <summary>
    /// 实体变化处理委托
    /// </summary>
    public delegate void EntityChangeHandler(object sender, EntityChangeEventArgs e);

    /// <summary>
    /// 实体验证委托
    /// </summary>
    public delegate bool EntityValidationHandler(Entity entity, ValidationContext context);

    /// <summary>
    /// 参数变化处理委托
    /// </summary>
    public delegate void ParameterChangeHandler(object sender, ParameterChangeEventArgs e);

    /// <summary>
    /// 生成进度报告委托
    /// </summary>
    public delegate void GenerationProgressHandler(object sender, GenerationProgressEventArgs e);

    /// <summary>
    /// 容器操作委托
    /// </summary>
    public delegate void ContainerOperationHandler(IEntityContainer container, ContainerOperationEventArgs e);

    /// <summary>
    /// 序列化过滤委托
    /// </summary>
    public delegate bool SerializationFilterHandler(object item, SerializationContext context);

    /// <summary>
    /// 错误处理委托
    /// </summary>
    public delegate void ErrorHandler(object sender, ErrorEventArgs e);

    /// <summary>
    /// 异步操作完成委托
    /// </summary>
    public delegate void AsyncOperationCompletedHandler(object sender, AsyncOperationCompletedEventArgs e);

    #endregion

    #region 事件参数类

    /// <summary>
    /// 实体变化事件参数
    /// </summary>
    public class EntityChangeEventArgs : EventArgs
    {
        /// <summary>
        /// 变化类型
        /// </summary>
        public EntityChangeType ChangeType { get; set; }

        /// <summary>
        /// 受影响的实体
        /// </summary>
        public Entity Entity { get; set; }

        /// <summary>
        /// 旧值（如果适用）
        /// </summary>
        public object OldValue { get; set; }

        /// <summary>
        /// 新值（如果适用）
        /// </summary>
        public object NewValue { get; set; }

        /// <summary>
        /// 属性名称（如果是属性变化）
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// 是否可以取消
        /// </summary>
        public bool CanCancel { get; set; }

        /// <summary>
        /// 是否取消操作
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// 取消原因
        /// </summary>
        public string CancelReason { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        public EntityChangeEventArgs(EntityChangeType changeType, Entity entity)
        {
            ChangeType = changeType;
            Entity = entity;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 实体变化类型
    /// </summary>
    public enum EntityChangeType
    {
        /// <summary>
        /// 创建
        /// </summary>
        Created,

        /// <summary>
        /// 修改
        /// </summary>
        Modified,

        /// <summary>
        /// 删除
        /// </summary>
        Deleted,

        /// <summary>
        /// 移动
        /// </summary>
        Moved,

        /// <summary>
        /// 旋转
        /// </summary>
        Rotated,

        /// <summary>
        /// 缩放
        /// </summary>
        Scaled,

        /// <summary>
        /// 属性变化
        /// </summary>
        PropertyChanged,

        /// <summary>
        /// 样式变化
        /// </summary>
        StyleChanged,

        /// <summary>
        /// 可见性变化
        /// </summary>
        VisibilityChanged
    }

    /// <summary>
    /// 验证上下文
    /// </summary>
    public class ValidationContext
    {
        /// <summary>
        /// 验证规则
        /// </summary>
        public List<IValidationRule> Rules { get; set; }

        /// <summary>
        /// 验证级别
        /// </summary>
        public ValidationLevel Level { get; set; }

        /// <summary>
        /// 是否抛出异常
        /// </summary>
        public bool ThrowOnError { get; set; }

        /// <summary>
        /// 验证结果
        /// </summary>
        public ValidationResult Result { get; set; }

        /// <summary>
        /// 上下文数据
        /// </summary>
        public Dictionary<string, object> Data { get; set; }

        public ValidationContext()
        {
            Rules = new List<IValidationRule>();
            Data = new Dictionary<string, object>();
            Result = new ValidationResult();
        }
    }

    /// <summary>
    /// 验证级别
    /// </summary>
    public enum ValidationLevel
    {
        /// <summary>
        /// 无验证
        /// </summary>
        None,

        /// <summary>
        /// 基础验证
        /// </summary>
        Basic,

        /// <summary>
        /// 标准验证
        /// </summary>
        Standard,

        /// <summary>
        /// 严格验证
        /// </summary>
        Strict,

        /// <summary>
        /// 完全验证
        /// </summary>
        Full
    }

    /// <summary>
    /// 验证规则接口
    /// </summary>
    public interface IValidationRule
    {
        /// <summary>
        /// 规则名称
        /// </summary>
        string RuleName { get; }

        /// <summary>
        /// 验证
        /// </summary>
        bool Validate(object target, ValidationContext context);

        /// <summary>
        /// 获取错误消息
        /// </summary>
        string GetErrorMessage();
    }

    /// <summary>
    /// 生成进度事件参数
    /// </summary>
    public class GenerationProgressEventArgs : EventArgs
    {
        /// <summary>
        /// 当前步骤
        /// </summary>
        public int CurrentStep { get; set; }

        /// <summary>
        /// 总步骤数
        /// </summary>
        public int TotalSteps { get; set; }

        /// <summary>
        /// 进度百分比
        /// </summary>
        public double ProgressPercentage { get; set; }

        /// <summary>
        /// 当前操作描述
        /// </summary>
        public string CurrentOperation { get; set; }

        /// <summary>
        /// 已生成的实体数
        /// </summary>
        public int GeneratedCount { get; set; }

        /// <summary>
        /// 预计剩余时间
        /// </summary>
        public TimeSpan EstimatedTimeRemaining { get; set; }

        /// <summary>
        /// 是否可以取消
        /// </summary>
        public bool CanCancel { get; set; }

        /// <summary>
        /// 请求取消
        /// </summary>
        public bool CancelRequested { get; set; }
    }

    /// <summary>
    /// 容器操作事件参数
    /// </summary>
    public class ContainerOperationEventArgs : EventArgs
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public ContainerOperationType OperationType { get; set; }

        /// <summary>
        /// 影响的实体
        /// </summary>
        public IEnumerable<Entity> AffectedEntities { get; set; }

        /// <summary>
        /// 操作前的状态
        /// </summary>
        public object StateBefore { get; set; }

        /// <summary>
        /// 操作后的状态
        /// </summary>
        public object StateAfter { get; set; }

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
    /// 容器操作类型
    /// </summary>
    public enum ContainerOperationType
    {
        /// <summary>
        /// 批量添加
        /// </summary>
        BulkAdd,

        /// <summary>
        /// 批量移除
        /// </summary>
        BulkRemove,

        /// <summary>
        /// 排序
        /// </summary>
        Sort,

        /// <summary>
        /// 过滤
        /// </summary>
        Filter,

        /// <summary>
        /// 重组
        /// </summary>
        Reorganize,

        /// <summary>
        /// 合并
        /// </summary>
        Merge,

        /// <summary>
        /// 分割
        /// </summary>
        Split
    }

    /// <summary>
    /// 错误事件参数
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        /// <summary>
        /// 错误级别
        /// </summary>
        public ErrorLevel Level { get; set; }

        /// <summary>
        /// 错误代码
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 异常对象
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// 错误源
        /// </summary>
        public object Source { get; set; }

        /// <summary>
        /// 是否已处理
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// 建议的操作
        /// </summary>
        public string SuggestedAction { get; set; }

        /// <summary>
        /// 发生时间
        /// </summary>
        public DateTime OccurredAt { get; set; }

        public ErrorEventArgs(string message, Exception exception = null)
        {
            Message = message;
            Exception = exception;
            OccurredAt = DateTime.Now;
        }
    }

    /// <summary>
    /// 错误级别
    /// </summary>
    public enum ErrorLevel
    {
        /// <summary>
        /// 信息
        /// </summary>
        Information,

        /// <summary>
        /// 警告
        /// </summary>
        Warning,

        /// <summary>
        /// 错误
        /// </summary>
        Error,

        /// <summary>
        /// 严重错误
        /// </summary>
        Critical,

        /// <summary>
        /// 致命错误
        /// </summary>
        Fatal
    }

    /// <summary>
    /// 异步操作完成事件参数
    /// </summary>
    public class AsyncOperationCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// 操作ID
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// 操作名称
        /// </summary>
        public string OperationName { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 结果
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// 错误（如果有）
        /// </summary>
        public Exception Error { get; set; }

        /// <summary>
        /// 是否被取消
        /// </summary>
        public bool Cancelled { get; set; }

        /// <summary>
        /// 执行时间
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// 用户状态
        /// </summary>
        public object UserState { get; set; }
    }

    #endregion

    #region 事件管理器

    /// <summary>
    /// 全局事件管理器
    /// </summary>
    public static class GlobalEventManager
    {
        /// <summary>
        /// 全局实体变化事件
        /// </summary>
        public static event EntityChangeHandler EntityChanged;

        /// <summary>
        /// 全局参数变化事件
        /// </summary>
        public static event ParameterChangeHandler ParameterChanged;

        /// <summary>
        /// 全局错误事件
        /// </summary>
        public static event ErrorHandler ErrorOccurred;

        /// <summary>
        /// 触发实体变化事件
        /// </summary>
        public static void RaiseEntityChanged(object sender, EntityChangeEventArgs e)
        {
            EntityChanged?.Invoke(sender, e);
        }

        /// <summary>
        /// 触发参数变化事件
        /// </summary>
        public static void RaiseParameterChanged(object sender, ParameterChangeEventArgs e)
        {
            ParameterChanged?.Invoke(sender, e);
        }

        /// <summary>
        /// 触发错误事件
        /// </summary>
        public static void RaiseError(object sender, ErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(sender, e);
        }
    }

    #endregion

    #region 事件聚合器

    /// <summary>
    /// 事件聚合器接口
    /// </summary>
    public interface IEventAggregator
    {
        /// <summary>
        /// 发布事件
        /// </summary>
        void Publish<TEvent>(TEvent eventToPublish) where TEvent : class;

        /// <summary>
        /// 订阅事件
        /// </summary>
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;

        /// <summary>
        /// 取消订阅
        /// </summary>
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
    }

    /// <summary>
    /// 简单事件聚合器实现
    /// </summary>
    public class EventAggregator : IEventAggregator
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();
        private readonly object _lock = new object();

        public void Publish<TEvent>(TEvent eventToPublish) where TEvent : class
        {
            List<Delegate> handlers;
            lock (_lock)
            {
                if (!_handlers.TryGetValue(typeof(TEvent), out handlers))
                    return;
                handlers = handlers.ToList();
            }

            foreach (var handler in handlers)
            {
                (handler as Action<TEvent>)?.Invoke(eventToPublish);
            }
        }

        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            lock (_lock)
            {
                if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))
                {
                    handlers = new List<Delegate>();
                    _handlers[typeof(TEvent)] = handlers;
                }
                handlers.Add(handler);
            }
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            lock (_lock)
            {
                if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
                {
                    handlers.Remove(handler);
                    if (handlers.Count == 0)
                        _handlers.Remove(typeof(TEvent));
                }
            }
        }
    }

    #endregion
}