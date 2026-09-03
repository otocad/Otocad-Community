using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using lcdb;
using OtoCAD.OpticEntity;

namespace OtoCAD.OpticEntity.FeatureControl
{
    /// <summary>
    /// 功能控制属性基类
    /// 所有具体的功能控制类都应继承此基类
    /// </summary>
    public abstract class FeatureControlProperty : INotifyPropertyChanged
    {
        #region 私有字段
        private bool _isEnabled = true;
        private int _priority = 0;
        private string _name = "";
        private string _description = "";
        #endregion

        #region 公共属性

        /// <summary>
        /// 功能是否启用
        /// </summary>
        [Category("Control")]
        [DisplayName("启用")]
        [Description("是否启用此功能")]
        public virtual bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                    OnEnabledChanged();
                }
            }
        }

        /// <summary>
        /// 功能优先级
        /// </summary>
        [Category("Control")]
        [DisplayName("优先级")]
        [Description("功能执行优先级，数值越大优先级越高")]
        public virtual int Priority
        {
            get => _priority;
            set
            {
                if (_priority != value)
                {
                    _priority = value;
                    OnPropertyChanged(nameof(Priority));
                }
            }
        }

        /// <summary>
        /// 功能名称
        /// </summary>
        [Category("Control")]
        [DisplayName("名称")]
        [Description("功能名称")]
        public virtual string Name
        {
            get => string.IsNullOrEmpty(_name) ? GetType().Name : _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        /// <summary>
        /// 功能描述
        /// </summary>
        [Category("Control")]
        [DisplayName("描述")]
        [Description("功能详细描述")]
        public virtual string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        /// <summary>
        /// 最后应用时间
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public DateTime LastAppliedTime { get; protected set; } = DateTime.MinValue;

        /// <summary>
        /// 是否需要重新应用
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public bool NeedsReapply { get; protected set; } = true;

        #endregion

        #region 事件

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 功能启用状态变更事件
        /// </summary>
        public event EventHandler<bool> EnabledChanged;

        /// <summary>
        /// 功能应用前事件
        /// </summary>
        public event EventHandler<FeatureApplyEventArgs> BeforeApply;

        /// <summary>
        /// 功能应用后事件
        /// </summary>
        public event EventHandler<FeatureApplyEventArgs> AfterApply;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        protected FeatureControlProperty()
        {
            InitializeDefaults();
        }

        /// <summary>
        /// 初始化默认值
        /// </summary>
        protected virtual void InitializeDefaults()
        {
            // 从特性中读取默认值
            var attributes = GetType().GetCustomAttributes(typeof(FeatureControlAttribute), false);
            if (attributes.Length > 0)
            {
                var attribute = (FeatureControlAttribute)attributes[0];
                IsEnabled = attribute.DefaultEnabled;
                Priority = attribute.Priority;
                Description = attribute.Description;
            }
        }

        #endregion

        #region 抽象方法

        /// <summary>
        /// 应用功能到指定元素
        /// </summary>
        /// <param name="element">目标元素</param>
        public abstract void Apply(BaseElementBlock element);

        /// <summary>
        /// 重置功能设置
        /// </summary>
        /// <param name="element">目标元素</param>
        public abstract void Reset(BaseElementBlock element);

        /// <summary>
        /// 验证功能设置是否有效
        /// </summary>
        /// <returns>验证结果</returns>
        public abstract bool Validate();

        #endregion

        #region 虚方法

        /// <summary>
        /// 功能启用状态变更时调用
        /// </summary>
        protected virtual void OnEnabledChanged()
        {
            NeedsReapply = true;
            EnabledChanged?.Invoke(this, IsEnabled);
        }

        /// <summary>
        /// 应用功能的安全包装
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <returns>是否成功应用</returns>
        public virtual bool TryApply(BaseElementBlock element)
        {
            if (!IsEnabled || element == null)
                return false;

            try
            {
                var args = new FeatureApplyEventArgs(element, this);
                BeforeApply?.Invoke(this, args);

                if (args.Cancel)
                    return false;

                if (!Validate())
                    return false;

                Apply(element);

                LastAppliedTime = DateTime.Now;
                NeedsReapply = false;

                AfterApply?.Invoke(this, args);
                return true;
            }
            catch (Exception ex)
            {
                // 记录错误日志
                System.Diagnostics.Debug.WriteLine($"Feature {Name} apply failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 重置功能的安全包装
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <returns>是否成功重置</returns>
        public virtual bool TryReset(BaseElementBlock element)
        {
            if (element == null)
                return false;

            try
            {
                Reset(element);
                LastAppliedTime = DateTime.MinValue;
                NeedsReapply = true;
                return true;
            }
            catch (Exception ex)
            {
                // 记录错误日志
                System.Diagnostics.Debug.WriteLine($"Feature {Name} reset failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 克隆功能控制属性
        /// </summary>
        /// <returns>克隆的对象</returns>
        public virtual FeatureControlProperty Clone()
        {
            var clone = (FeatureControlProperty)MemberwiseClone();
            clone.LastAppliedTime = DateTime.MinValue;
            clone.NeedsReapply = true;
            return clone;
        }

        #endregion

        #region 受保护方法

        /// <summary>
        /// 触发属性变更事件
        /// </summary>
        /// <param name="propertyName">属性名</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            // 属性变更时标记需要重新应用
            if (propertyName != nameof(LastAppliedTime) && propertyName != nameof(NeedsReapply))
            {
                NeedsReapply = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// 功能应用事件参数
    /// </summary>
    public class FeatureApplyEventArgs : EventArgs
    {
        /// <summary>
        /// 目标元素
        /// </summary>
        public BaseElementBlock Element { get; }

        /// <summary>
        /// 功能控制属性
        /// </summary>
        public FeatureControlProperty Feature { get; }

        /// <summary>
        /// 是否取消应用
        /// </summary>
        public bool Cancel { get; set; } = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <param name="feature">功能控制属性</param>
        public FeatureApplyEventArgs(BaseElementBlock element, FeatureControlProperty feature)
        {
            Element = element;
            Feature = feature;
        }
    }
}