using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using OtoCAD.Interface;

namespace lcdb.Parameters
{
    /// <summary>
    /// 参数定义类 - 定义参数的元数据和约束
    /// </summary>
    public class ParameterDefinition : OtoCAD.Interface.ParameterDefinition
    {
        #region 扩展属性

        /// <summary>
        /// 参数分组索引
        /// </summary>
        public int GroupIndex { get; set; }

        /// <summary>
        /// 参数显示顺序
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// 是否高级参数
        /// </summary>
        public bool IsAdvanced { get; set; }

        /// <summary>
        /// 是否隐藏
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// 依赖的参数名称
        /// </summary>
        public List<string> DependsOn { get; set; }

        /// <summary>
        /// 参数验证器
        /// </summary>
        public IParameterValidator Validator { get; set; }

        /// <summary>
        /// 值转换器
        /// </summary>
        public IValueConverter ValueConverter { get; set; }

        /// <summary>
        /// 允许的值列表（用于枚举类型）
        /// </summary>
        public List<object> AllowedValues { get; set; }

        /// <summary>
        /// 参数提示
        /// </summary>
        public string Tooltip { get; set; }

        /// <summary>
        /// 参数帮助URL
        /// </summary>
        public string HelpUrl { get; set; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ParameterDefinition()
        {
            DependsOn = new List<string>();
            AllowedValues = new List<object>();
            DisplayOrder = 0;
            GroupIndex = 0;
            IsAdvanced = false;
            IsHidden = false;
        }

        /// <summary>
        /// 创建参数定义
        /// </summary>
        public static ParameterDefinition Create(
            string name, 
            Type type, 
            object defaultValue,
            string displayName = null,
            string category = null,
            bool isRequired = false)
        {
            return new ParameterDefinition
            {
                Name = name,
                ParameterType = type,
                DefaultValue = defaultValue,
                DisplayName = displayName ?? name,
                Category = category ?? "General",
                IsRequired = isRequired
            };
        }

        #endregion

        #region 方法

        /// <summary>
        /// 验证参数值
        /// </summary>
        public bool Validate(object value)
        {
            // 类型检查
            if (value != null && !ParameterType.IsAssignableFrom(value.GetType()))
            {
                try
                {
                    // 尝试类型转换
                    value = Convert.ChangeType(value, ParameterType);
                }
                catch
                {
                    return false;
                }
            }

            // 必需参数检查
            if (IsRequired && value == null)
                return false;

            // 只读参数不能修改
            if (IsReadOnly)
                return false;

            // 使用验证器
            if (Validator != null)
                return Validator.Validate(value, this);

            // 检查允许值列表
            if (AllowedValues != null && AllowedValues.Count > 0)
            {
                if (!AllowedValues.Contains(value))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 转换参数值
        /// </summary>
        public object ConvertValue(object value)
        {
            if (ValueConverter != null)
                return ValueConverter.Convert(value, ParameterType);

            if (value == null)
                return DefaultValue;

            if (ParameterType.IsAssignableFrom(value.GetType()))
                return value;

            try
            {
                return Convert.ChangeType(value, ParameterType);
            }
            catch
            {
                return DefaultValue;
            }
        }

        /// <summary>
        /// 克隆参数定义
        /// </summary>
        public ParameterDefinition Clone()
        {
            return new ParameterDefinition
            {
                Name = this.Name,
                DisplayName = this.DisplayName,
                ParameterType = this.ParameterType,
                DefaultValue = this.DefaultValue,
                Description = this.Description,
                Category = this.Category,
                IsReadOnly = this.IsReadOnly,
                IsRequired = this.IsRequired,
                Unit = this.Unit,
                GroupIndex = this.GroupIndex,
                DisplayOrder = this.DisplayOrder,
                IsAdvanced = this.IsAdvanced,
                IsHidden = this.IsHidden,
                DependsOn = new List<string>(this.DependsOn),
                Validator = this.Validator,
                ValueConverter = this.ValueConverter,
                AllowedValues = this.AllowedValues != null ? new List<object>(this.AllowedValues) : null,
                Tooltip = this.Tooltip,
                HelpUrl = this.HelpUrl
            };
        }

        /// <summary>
        /// 获取显示文本
        /// </summary>
        public string GetDisplayText(object value)
        {
            if (value == null)
                return "N/A";

            // 添加单位
            if (!string.IsNullOrEmpty(Unit))
                return $"{value} {Unit}";

            return value.ToString();
        }

        #endregion

        #region 静态工厂方法

        /// <summary>
        /// 创建数值参数
        /// </summary>
        public static ParameterDefinition CreateNumeric(
            string name,
            double defaultValue,
            double? min = null,
            double? max = null,
            string unit = null)
        {
            var def = new ParameterDefinition
            {
                Name = name,
                DisplayName = name,
                ParameterType = typeof(double),
                DefaultValue = defaultValue,
                Unit = unit
            };

            if (min.HasValue || max.HasValue)
            {
                def.Validator = new RangeValidator { Min = min, Max = max };
            }

            return def;
        }

        /// <summary>
        /// 创建整数参数
        /// </summary>
        public static ParameterDefinition CreateInteger(
            string name,
            int defaultValue,
            int? min = null,
            int? max = null)
        {
            var def = new ParameterDefinition
            {
                Name = name,
                DisplayName = name,
                ParameterType = typeof(int),
                DefaultValue = defaultValue
            };

            if (min.HasValue || max.HasValue)
            {
                def.Validator = new RangeValidator { Min = min, Max = max };
            }

            return def;
        }

        /// <summary>
        /// 创建布尔参数
        /// </summary>
        public static ParameterDefinition CreateBoolean(
            string name,
            bool defaultValue,
            string displayName = null)
        {
            return new ParameterDefinition
            {
                Name = name,
                DisplayName = displayName ?? name,
                ParameterType = typeof(bool),
                DefaultValue = defaultValue
            };
        }

        /// <summary>
        /// 创建枚举参数
        /// </summary>
        public static ParameterDefinition CreateEnum<T>(
            string name,
            T defaultValue,
            string displayName = null) where T : Enum
        {
            var def = new ParameterDefinition
            {
                Name = name,
                DisplayName = displayName ?? name,
                ParameterType = typeof(T),
                DefaultValue = defaultValue,
                AllowedValues = new List<object>()
            };

            // 添加所有枚举值
            foreach (var value in Enum.GetValues(typeof(T)))
            {
                def.AllowedValues.Add(value);
            }

            return def;
        }

        /// <summary>
        /// 创建字符串参数
        /// </summary>
        public static ParameterDefinition CreateString(
            string name,
            string defaultValue,
            int? maxLength = null,
            string pattern = null)
        {
            var def = new ParameterDefinition
            {
                Name = name,
                DisplayName = name,
                ParameterType = typeof(string),
                DefaultValue = defaultValue
            };

            if (maxLength.HasValue || !string.IsNullOrEmpty(pattern))
            {
                def.Validator = new StringValidator 
                { 
                    MaxLength = maxLength, 
                    Pattern = pattern 
                };
            }

            return def;
        }

        /// <summary>
        /// 创建选择列表参数
        /// </summary>
        public static ParameterDefinition CreateChoice(
            string name,
            object defaultValue,
            params object[] choices)
        {
            return new ParameterDefinition
            {
                Name = name,
                DisplayName = name,
                ParameterType = defaultValue?.GetType() ?? typeof(object),
                DefaultValue = defaultValue,
                AllowedValues = new List<object>(choices)
            };
        }

        #endregion
    }

    /// <summary>
    /// 参数验证器接口
    /// </summary>
    public interface IParameterValidator
    {
        /// <summary>
        /// 验证参数值
        /// </summary>
        bool Validate(object value, ParameterDefinition definition);

        /// <summary>
        /// 获取验证错误信息
        /// </summary>
        string GetErrorMessage();
    }

    /// <summary>
    /// 值转换器接口
    /// </summary>
    public interface IValueConverter
    {
        /// <summary>
        /// 转换值
        /// </summary>
        object Convert(object value, Type targetType);

        /// <summary>
        /// 反向转换
        /// </summary>
        object ConvertBack(object value, Type targetType);
    }

    /// <summary>
    /// 范围验证器
    /// </summary>
    public class RangeValidator : IParameterValidator
    {
        public double? Min { get; set; }
        public double? Max { get; set; }
        public bool IncludeMin { get; set; } = true;
        public bool IncludeMax { get; set; } = true;

        private string _lastError;

        public bool Validate(object value, ParameterDefinition definition)
        {
            if (value == null)
                return !definition.IsRequired;

            double numValue;
            try
            {
                numValue = Convert.ToDouble(value);
            }
            catch
            {
                _lastError = "值必须是数字";
                return false;
            }

            if (Min.HasValue)
            {
                if (IncludeMin && numValue < Min.Value)
                {
                    _lastError = $"值必须大于或等于 {Min.Value}";
                    return false;
                }
                if (!IncludeMin && numValue <= Min.Value)
                {
                    _lastError = $"值必须大于 {Min.Value}";
                    return false;
                }
            }

            if (Max.HasValue)
            {
                if (IncludeMax && numValue > Max.Value)
                {
                    _lastError = $"值必须小于或等于 {Max.Value}";
                    return false;
                }
                if (!IncludeMax && numValue >= Max.Value)
                {
                    _lastError = $"值必须小于 {Max.Value}";
                    return false;
                }
            }

            return true;
        }

        public string GetErrorMessage() => _lastError;
    }

    /// <summary>
    /// 字符串验证器
    /// </summary>
    public class StringValidator : IParameterValidator
    {
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public string Pattern { get; set; }

        private string _lastError;

        public bool Validate(object value, ParameterDefinition definition)
        {
            if (value == null)
                return !definition.IsRequired;

            var str = value.ToString();

            if (MinLength.HasValue && str.Length < MinLength.Value)
            {
                _lastError = $"字符串长度必须至少为 {MinLength.Value}";
                return false;
            }

            if (MaxLength.HasValue && str.Length > MaxLength.Value)
            {
                _lastError = $"字符串长度不能超过 {MaxLength.Value}";
                return false;
            }

            if (!string.IsNullOrEmpty(Pattern))
            {
                var regex = new System.Text.RegularExpressions.Regex(Pattern);
                if (!regex.IsMatch(str))
                {
                    _lastError = $"字符串格式不正确";
                    return false;
                }
            }

            return true;
        }

        public string GetErrorMessage() => _lastError;
    }
}