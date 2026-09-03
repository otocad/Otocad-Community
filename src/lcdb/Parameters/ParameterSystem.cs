using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using OtoCAD.Interface;
using OtoCAD.Interface.Events;

namespace lcdb.Parameters
{
    /// <summary>
    /// 参数系统 - 管理参数定义、验证和通知
    /// </summary>
    public class ParameterSystem : INotifyPropertyChanged
    {
        #region 字段

        private readonly Dictionary<string, ParameterDefinition> _definitions;
        private readonly Dictionary<string, object> _values;
        private readonly ParameterValidator _validator;
        private readonly List<IParameterObserver> _observers;
        private bool _suppressNotifications;

        #endregion

        #region 事件

        /// <summary>
        /// 参数变化前事件
        /// </summary>
        public event EventHandler<ParameterChangeEventArgs> ParameterChanging;

        /// <summary>
        /// 参数变化后事件
        /// </summary>
        public event EventHandler<ParameterChangeEventArgs> ParameterChanged;

        /// <summary>
        /// 批量参数变化事件
        /// </summary>
        public event EventHandler<BatchParameterChangeEventArgs> BatchParametersChanged;

        /// <summary>
        /// 属性变化事件（INotifyPropertyChanged）
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ParameterSystem()
        {
            _definitions = new Dictionary<string, ParameterDefinition>();
            _values = new Dictionary<string, object>();
            _validator = new ParameterValidator();
            _observers = new List<IParameterObserver>();
            _suppressNotifications = false;
        }

        #endregion

        #region 属性

        /// <summary>
        /// 获取所有参数定义
        /// </summary>
        public IReadOnlyDictionary<string, ParameterDefinition> Definitions => _definitions;

        /// <summary>
        /// 获取所有参数值
        /// </summary>
        public IReadOnlyDictionary<string, object> Values => _values;

        /// <summary>
        /// 获取验证器
        /// </summary>
        public ParameterValidator Validator => _validator;

        /// <summary>
        /// 参数索引器
        /// </summary>
        public object this[string parameterName]
        {
            get => GetValue(parameterName);
            set => SetValue(parameterName, value);
        }

        #endregion

        #region 参数定义管理

        /// <summary>
        /// 注册参数定义
        /// </summary>
        public void RegisterParameter(ParameterDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            _definitions[definition.Name] = definition;
            _validator.RegisterDefinition(definition);

            // 设置默认值
            if (!_values.ContainsKey(definition.Name))
            {
                _values[definition.Name] = definition.DefaultValue;
            }
        }

        /// <summary>
        /// 批量注册参数定义
        /// </summary>
        public void RegisterParameters(IEnumerable<ParameterDefinition> definitions)
        {
            foreach (var definition in definitions)
            {
                RegisterParameter(definition);
            }
        }

        /// <summary>
        /// 移除参数定义
        /// </summary>
        public bool RemoveParameter(string parameterName)
        {
            if (_definitions.Remove(parameterName))
            {
                _values.Remove(parameterName);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取参数定义
        /// </summary>
        public ParameterDefinition GetDefinition(string parameterName)
        {
            return _definitions.TryGetValue(parameterName, out var definition) ? definition : null;
        }

        #endregion

        #region 参数值管理

        /// <summary>
        /// 获取参数值
        /// </summary>
        public object GetValue(string parameterName)
        {
            return _values.TryGetValue(parameterName, out var value) ? value : null;
        }

        /// <summary>
        /// 获取类型化参数值
        /// </summary>
        public T GetValue<T>(string parameterName)
        {
            var value = GetValue(parameterName);
            if (value is T typedValue)
                return typedValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// 设置参数值
        /// </summary>
        public bool SetValue(string parameterName, object value)
        {
            // 获取旧值
            var oldValue = GetValue(parameterName);

            // 检查值是否相同
            if (Equals(oldValue, value))
                return true;

            // 触发变化前事件
            var changingArgs = new ParameterChangeEventArgs(parameterName, oldValue, value);
            OnParameterChanging(changingArgs);

            if (changingArgs.Cancel)
                return false;

            // 验证新值
            if (!_validator.ValidateParameter(parameterName, value))
                return false;

            // 获取定义并转换值
            if (_definitions.TryGetValue(parameterName, out var definition))
            {
                value = definition.ConvertValue(value);
            }

            // 更新值
            _values[parameterName] = value;

            // 触发变化后事件
            if (!_suppressNotifications)
            {
                var changedArgs = new ParameterChangeEventArgs(parameterName, oldValue, value);
                OnParameterChanged(changedArgs);
                OnPropertyChanged(parameterName);
            }

            // 通知观察者
            NotifyObservers(parameterName, oldValue, value);

            return true;
        }

        /// <summary>
        /// 批量设置参数值
        /// </summary>
        public bool SetValues(IDictionary<string, object> values)
        {
            if (values == null || values.Count == 0)
                return true;

            // 验证所有参数
            if (!_validator.ValidateAll(values))
                return false;

            var changes = new List<ParameterChange>();
            _suppressNotifications = true;

            try
            {
                foreach (var kvp in values)
                {
                    var oldValue = GetValue(kvp.Key);
                    if (SetValue(kvp.Key, kvp.Value))
                    {
                        changes.Add(new ParameterChange
                        {
                            ParameterName = kvp.Key,
                            OldValue = oldValue,
                            NewValue = kvp.Value
                        });
                    }
                }
            }
            finally
            {
                _suppressNotifications = false;
            }

            // 触发批量变化事件
            if (changes.Count > 0)
            {
                OnBatchParametersChanged(new BatchParameterChangeEventArgs(changes));
            }

            return true;
        }

        /// <summary>
        /// 重置参数到默认值
        /// </summary>
        public void ResetToDefaults()
        {
            var changes = new List<ParameterChange>();

            _suppressNotifications = true;
            try
            {
                foreach (var definition in _definitions.Values)
                {
                    var oldValue = GetValue(definition.Name);
                    if (!Equals(oldValue, definition.DefaultValue))
                    {
                        _values[definition.Name] = definition.DefaultValue;
                        changes.Add(new ParameterChange
                        {
                            ParameterName = definition.Name,
                            OldValue = oldValue,
                            NewValue = definition.DefaultValue
                        });
                    }
                }
            }
            finally
            {
                _suppressNotifications = false;
            }

            // 触发批量变化事件
            if (changes.Count > 0)
            {
                OnBatchParametersChanged(new BatchParameterChangeEventArgs(changes));
            }
        }

        /// <summary>
        /// 清除所有参数值
        /// </summary>
        public void Clear()
        {
            _values.Clear();
            OnBatchParametersChanged(new BatchParameterChangeEventArgs(new List<ParameterChange>()));
        }

        #endregion

        #region 参数类型系统

        /// <summary>
        /// 创建类型化参数代理
        /// </summary>
        public TypedParameter<T> CreateTypedParameter<T>(string name, T defaultValue)
        {
            var definition = ParameterDefinition.Create(name, typeof(T), defaultValue);
            RegisterParameter(definition);
            return new TypedParameter<T>(this, name);
        }

        /// <summary>
        /// 获取参数分组
        /// </summary>
        public IEnumerable<IGrouping<string, ParameterDefinition>> GetParameterGroups()
        {
            return _definitions.Values.GroupBy(d => d.Category ?? "General");
        }

        /// <summary>
        /// 获取分类参数
        /// </summary>
        public IEnumerable<ParameterDefinition> GetParametersByCategory(string category)
        {
            return _definitions.Values.Where(d => d.Category == category);
        }

        /// <summary>
        /// 导出参数到字典
        /// </summary>
        public Dictionary<string, object> ExportToDictionary()
        {
            return new Dictionary<string, object>(_values);
        }

        /// <summary>
        /// 从字典导入参数
        /// </summary>
        public void ImportFromDictionary(IDictionary<string, object> dictionary)
        {
            SetValues(dictionary);
        }

        #endregion

        #region 观察者模式

        /// <summary>
        /// 添加观察者
        /// </summary>
        public void AddObserver(IParameterObserver observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }

        /// <summary>
        /// 移除观察者
        /// </summary>
        public void RemoveObserver(IParameterObserver observer)
        {
            _observers.Remove(observer);
        }

        /// <summary>
        /// 通知观察者
        /// </summary>
        private void NotifyObservers(string parameterName, object oldValue, object newValue)
        {
            foreach (var observer in _observers)
            {
                observer.OnParameterChanged(parameterName, oldValue, newValue);
            }
        }

        #endregion

        #region 事件触发

        /// <summary>
        /// 触发参数变化前事件
        /// </summary>
        protected virtual void OnParameterChanging(ParameterChangeEventArgs e)
        {
            ParameterChanging?.Invoke(this, e);
        }

        /// <summary>
        /// 触发参数变化后事件
        /// </summary>
        protected virtual void OnParameterChanged(ParameterChangeEventArgs e)
        {
            ParameterChanged?.Invoke(this, e);
        }

        /// <summary>
        /// 触发批量参数变化事件
        /// </summary>
        protected virtual void OnBatchParametersChanged(BatchParameterChangeEventArgs e)
        {
            BatchParametersChanged?.Invoke(this, e);
        }

        /// <summary>
        /// 触发属性变化事件
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// 类型化参数代理
    /// </summary>
    public class TypedParameter<T>
    {
        private readonly ParameterSystem _system;
        private readonly string _name;

        public TypedParameter(ParameterSystem system, string name)
        {
            _system = system;
            _name = name;
        }

        /// <summary>
        /// 获取或设置值
        /// </summary>
        public T Value
        {
            get => _system.GetValue<T>(_name);
            set => _system.SetValue(_name, value);
        }

        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// 获取定义
        /// </summary>
        public ParameterDefinition Definition => _system.GetDefinition(_name);

        /// <summary>
        /// 隐式转换为值
        /// </summary>
        public static implicit operator T(TypedParameter<T> parameter)
        {
            return parameter.Value;
        }
    }

    /// <summary>
    /// 参数观察者接口
    /// </summary>
    public interface IParameterObserver
    {
        /// <summary>
        /// 参数变化时调用
        /// </summary>
        void OnParameterChanged(string parameterName, object oldValue, object newValue);
    }

    /// <summary>
    /// 参数变化信息
    /// </summary>
    public class ParameterChange
    {
        public string ParameterName { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
    }

    /// <summary>
    /// 批量参数变化事件参数
    /// </summary>
    public class BatchParameterChangeEventArgs : EventArgs
    {
        public IReadOnlyList<ParameterChange> Changes { get; }

        public BatchParameterChangeEventArgs(List<ParameterChange> changes)
        {
            Changes = changes.AsReadOnly();
        }
    }

    /// <summary>
    /// 参数类型注册表
    /// </summary>
    public static class ParameterTypeRegistry
    {
        private static readonly Dictionary<Type, IParameterTypeHandler> _handlers;

        static ParameterTypeRegistry()
        {
            _handlers = new Dictionary<Type, IParameterTypeHandler>();
            RegisterDefaultHandlers();
        }

        /// <summary>
        /// 注册类型处理器
        /// </summary>
        public static void RegisterHandler(Type type, IParameterTypeHandler handler)
        {
            _handlers[type] = handler;
        }

        /// <summary>
        /// 获取类型处理器
        /// </summary>
        public static IParameterTypeHandler GetHandler(Type type)
        {
            return _handlers.TryGetValue(type, out var handler) ? handler : null;
        }

        /// <summary>
        /// 注册默认处理器
        /// </summary>
        private static void RegisterDefaultHandlers()
        {
            RegisterHandler(typeof(int), new IntegerTypeHandler());
            RegisterHandler(typeof(double), new DoubleTypeHandler());
            RegisterHandler(typeof(bool), new BooleanTypeHandler());
            RegisterHandler(typeof(string), new StringTypeHandler());
            RegisterHandler(typeof(DateTime), new DateTimeTypeHandler());
        }
    }

    /// <summary>
    /// 参数类型处理器接口
    /// </summary>
    public interface IParameterTypeHandler
    {
        /// <summary>
        /// 验证值
        /// </summary>
        bool Validate(object value);

        /// <summary>
        /// 转换值
        /// </summary>
        object Convert(object value);

        /// <summary>
        /// 格式化显示
        /// </summary>
        string Format(object value);

        /// <summary>
        /// 解析字符串
        /// </summary>
        object Parse(string text);
    }

    /// <summary>
    /// 整数类型处理器
    /// </summary>
    public class IntegerTypeHandler : IParameterTypeHandler
    {
        public bool Validate(object value)
        {
            return value is int || (value != null && int.TryParse(value.ToString(), out _));
        }

        public object Convert(object value)
        {
            if (value is int)
                return value;
            return int.TryParse(value?.ToString(), out var result) ? result : 0;
        }

        public string Format(object value)
        {
            return value?.ToString() ?? "0";
        }

        public object Parse(string text)
        {
            return int.TryParse(text, out var result) ? result : 0;
        }
    }

    /// <summary>
    /// 双精度类型处理器
    /// </summary>
    public class DoubleTypeHandler : IParameterTypeHandler
    {
        public bool Validate(object value)
        {
            return value is double || (value != null && double.TryParse(value.ToString(), out _));
        }

        public object Convert(object value)
        {
            if (value is double)
                return value;
            return double.TryParse(value?.ToString(), out var result) ? result : 0.0;
        }

        public string Format(object value)
        {
            return value is double d ? d.ToString("F2") : "0.00";
        }

        public object Parse(string text)
        {
            return double.TryParse(text, out var result) ? result : 0.0;
        }
    }

    /// <summary>
    /// 布尔类型处理器
    /// </summary>
    public class BooleanTypeHandler : IParameterTypeHandler
    {
        public bool Validate(object value)
        {
            return value is bool || (value != null && bool.TryParse(value.ToString(), out _));
        }

        public object Convert(object value)
        {
            if (value is bool)
                return value;
            return bool.TryParse(value?.ToString(), out var result) && result;
        }

        public string Format(object value)
        {
            return value is bool b ? (b ? "是" : "否") : "否";
        }

        public object Parse(string text)
        {
            return bool.TryParse(text, out var result) && result;
        }
    }

    /// <summary>
    /// 字符串类型处理器
    /// </summary>
    public class StringTypeHandler : IParameterTypeHandler
    {
        public bool Validate(object value)
        {
            return true; // 所有值都可以转换为字符串
        }

        public object Convert(object value)
        {
            return value?.ToString() ?? string.Empty;
        }

        public string Format(object value)
        {
            return value?.ToString() ?? string.Empty;
        }

        public object Parse(string text)
        {
            return text ?? string.Empty;
        }
    }

    /// <summary>
    /// 日期时间类型处理器
    /// </summary>
    public class DateTimeTypeHandler : IParameterTypeHandler
    {
        public bool Validate(object value)
        {
            return value is DateTime || (value != null && DateTime.TryParse(value.ToString(), out _));
        }

        public object Convert(object value)
        {
            if (value is DateTime)
                return value;
            return DateTime.TryParse(value?.ToString(), out var result) ? result : DateTime.MinValue;
        }

        public string Format(object value)
        {
            return value is DateTime dt ? dt.ToString("yyyy-MM-dd HH:mm:ss") : "";
        }

        public object Parse(string text)
        {
            return DateTime.TryParse(text, out var result) ? result : DateTime.MinValue;
        }
    }
}