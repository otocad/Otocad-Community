using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OtoCAD.Interface;

namespace lcdb.Parameters
{
    /// <summary>
    /// 参数验证器 - 提供参数验证功能
    /// </summary>
    public class ParameterValidator
    {
        #region 字段

        private readonly Dictionary<string, List<IValidationRule>> _rules;
        private readonly Dictionary<string, ParameterDefinition> _definitions;
        private readonly List<string> _errors;
        private readonly List<string> _warnings;

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ParameterValidator()
        {
            _rules = new Dictionary<string, List<IValidationRule>>();
            _definitions = new Dictionary<string, ParameterDefinition>();
            _errors = new List<string>();
            _warnings = new List<string>();
        }

        #endregion

        #region 属性

        /// <summary>
        /// 获取错误列表
        /// </summary>
        public IReadOnlyList<string> Errors => _errors.AsReadOnly();

        /// <summary>
        /// 获取警告列表
        /// </summary>
        public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasErrors => _errors.Count > 0;

        /// <summary>
        /// 是否有警告
        /// </summary>
        public bool HasWarnings => _warnings.Count > 0;

        #endregion

        #region 公共方法

        /// <summary>
        /// 注册参数定义
        /// </summary>
        public void RegisterDefinition(ParameterDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            _definitions[definition.Name] = definition;

            // 根据定义创建默认规则
            CreateDefaultRules(definition);
        }

        /// <summary>
        /// 批量注册参数定义
        /// </summary>
        public void RegisterDefinitions(IEnumerable<ParameterDefinition> definitions)
        {
            foreach (var definition in definitions)
            {
                RegisterDefinition(definition);
            }
        }

        /// <summary>
        /// 添加验证规则
        /// </summary>
        public void AddRule(string parameterName, IValidationRule rule)
        {
            if (!_rules.ContainsKey(parameterName))
                _rules[parameterName] = new List<IValidationRule>();

            _rules[parameterName].Add(rule);
        }

        /// <summary>
        /// 验证单个参数
        /// </summary>
        public bool ValidateParameter(string parameterName, object value)
        {
            ClearErrors();

            // 检查参数定义
            if (_definitions.TryGetValue(parameterName, out var definition))
            {
                // 使用定义的验证
                if (!definition.Validate(value))
                {
                    _errors.Add($"参数 '{parameterName}' 验证失败");
                    return false;
                }
            }

            // 应用自定义规则
            if (_rules.TryGetValue(parameterName, out var rules))
            {
                foreach (var rule in rules)
                {
                    var result = rule.Validate(value);
                    if (!result.IsValid)
                    {
                        _errors.AddRange(result.Errors);
                    }
                    _warnings.AddRange(result.Warnings);
                }
            }

            return !HasErrors;
        }

        /// <summary>
        /// 验证所有参数
        /// </summary>
        public bool ValidateAll(IDictionary<string, object> parameters)
        {
            ClearErrors();
            bool allValid = true;

            // 验证必需参数
            foreach (var definition in _definitions.Values.Where(d => d.IsRequired))
            {
                if (!parameters.ContainsKey(definition.Name) || parameters[definition.Name] == null)
                {
                    _errors.Add($"必需参数 '{definition.Name}' 缺失");
                    allValid = false;
                }
            }

            // 验证每个参数
            foreach (var kvp in parameters)
            {
                if (!ValidateParameter(kvp.Key, kvp.Value))
                {
                    allValid = false;
                }
            }

            // 验证参数依赖关系
            ValidateDependencies(parameters);

            return allValid && !HasErrors;
        }

        /// <summary>
        /// 验证参数组
        /// </summary>
        public bool ValidateGroup(IDictionary<string, object> parameters, string groupName)
        {
            ClearErrors();

            var groupDefinitions = _definitions.Values
                .Where(d => d.Category == groupName);

            foreach (var definition in groupDefinitions)
            {
                if (parameters.TryGetValue(definition.Name, out var value))
                {
                    if (!ValidateParameter(definition.Name, value))
                    {
                        return false;
                    }
                }
                else if (definition.IsRequired)
                {
                    _errors.Add($"组 '{groupName}' 中的必需参数 '{definition.Name}' 缺失");
                    return false;
                }
            }

            return !HasErrors;
        }

        /// <summary>
        /// 清除错误和警告
        /// </summary>
        public void ClearErrors()
        {
            _errors.Clear();
            _warnings.Clear();
        }

        /// <summary>
        /// 获取验证报告
        /// </summary>
        public ValidationReport GetReport()
        {
            return new ValidationReport
            {
                IsValid = !HasErrors,
                Errors = new List<string>(_errors),
                Warnings = new List<string>(_warnings),
                Timestamp = DateTime.Now
            };
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 创建默认规则
        /// </summary>
        private void CreateDefaultRules(ParameterDefinition definition)
        {
            var rules = new List<IValidationRule>();

            // 类型规则
            rules.Add(new TypeValidationRule(definition.ParameterType));

            // 必需规则
            if (definition.IsRequired)
            {
                rules.Add(new RequiredValidationRule());
            }

            // 范围规则（如果有验证器）
            if (definition.Validator != null)
            {
                rules.Add(new CustomValidatorRule(definition.Validator, definition));
            }

            // 允许值规则
            if (definition.AllowedValues != null && definition.AllowedValues.Count > 0)
            {
                rules.Add(new AllowedValuesRule(definition.AllowedValues));
            }

            _rules[definition.Name] = rules;
        }

        /// <summary>
        /// 验证参数依赖关系
        /// </summary>
        private void ValidateDependencies(IDictionary<string, object> parameters)
        {
            foreach (var definition in _definitions.Values)
            {
                if (definition.DependsOn != null && definition.DependsOn.Count > 0)
                {
                    // 检查是否提供了当前参数
                    if (!parameters.ContainsKey(definition.Name))
                        continue;

                    // 检查所有依赖参数是否存在
                    foreach (var dependency in definition.DependsOn)
                    {
                        if (!parameters.ContainsKey(dependency))
                        {
                            _errors.Add($"参数 '{definition.Name}' 依赖于 '{dependency}'，但后者未提供");
                        }
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// 验证规则接口
    /// </summary>
    public interface IValidationRule
    {
        /// <summary>
        /// 验证值
        /// </summary>
        ValidationRuleResult Validate(object value);
    }

    /// <summary>
    /// 验证规则结果
    /// </summary>
    public class ValidationRuleResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }

        public ValidationRuleResult()
        {
            IsValid = true;
            Errors = new List<string>();
            Warnings = new List<string>();
        }

        public static ValidationRuleResult Success()
        {
            return new ValidationRuleResult { IsValid = true };
        }

        public static ValidationRuleResult Error(string message)
        {
            return new ValidationRuleResult
            {
                IsValid = false,
                Errors = new List<string> { message }
            };
        }

        public static ValidationRuleResult Warning(string message)
        {
            return new ValidationRuleResult
            {
                IsValid = true,
                Warnings = new List<string> { message }
            };
        }
    }

    /// <summary>
    /// 类型验证规则
    /// </summary>
    public class TypeValidationRule : IValidationRule
    {
        private readonly Type _expectedType;

        public TypeValidationRule(Type expectedType)
        {
            _expectedType = expectedType;
        }

        public ValidationRuleResult Validate(object value)
        {
            if (value == null)
                return ValidationRuleResult.Success();

            if (!_expectedType.IsAssignableFrom(value.GetType()))
            {
                try
                {
                    Convert.ChangeType(value, _expectedType);
                    return ValidationRuleResult.Success();
                }
                catch
                {
                    return ValidationRuleResult.Error(
                        $"值类型 '{value.GetType().Name}' 不能转换为 '{_expectedType.Name}'");
                }
            }

            return ValidationRuleResult.Success();
        }
    }

    /// <summary>
    /// 必需验证规则
    /// </summary>
    public class RequiredValidationRule : IValidationRule
    {
        public ValidationRuleResult Validate(object value)
        {
            if (value == null)
                return ValidationRuleResult.Error("值不能为空");

            if (value is string str && string.IsNullOrWhiteSpace(str))
                return ValidationRuleResult.Error("值不能为空字符串");

            return ValidationRuleResult.Success();
        }
    }

    /// <summary>
    /// 允许值验证规则
    /// </summary>
    public class AllowedValuesRule : IValidationRule
    {
        private readonly List<object> _allowedValues;

        public AllowedValuesRule(List<object> allowedValues)
        {
            _allowedValues = allowedValues;
        }

        public ValidationRuleResult Validate(object value)
        {
            if (value == null)
                return ValidationRuleResult.Success();

            if (!_allowedValues.Contains(value))
            {
                var allowedStr = string.Join(", ", _allowedValues);
                return ValidationRuleResult.Error(
                    $"值 '{value}' 不在允许的值列表中: [{allowedStr}]");
            }

            return ValidationRuleResult.Success();
        }
    }

    /// <summary>
    /// 自定义验证器规则
    /// </summary>
    public class CustomValidatorRule : IValidationRule
    {
        private readonly IParameterValidator _validator;
        private readonly ParameterDefinition _definition;

        public CustomValidatorRule(IParameterValidator validator, ParameterDefinition definition)
        {
            _validator = validator;
            _definition = definition;
        }

        public ValidationRuleResult Validate(object value)
        {
            if (_validator.Validate(value, _definition))
                return ValidationRuleResult.Success();

            return ValidationRuleResult.Error(_validator.GetErrorMessage());
        }
    }

    /// <summary>
    /// 验证报告
    /// </summary>
    public class ValidationReport
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误列表
        /// </summary>
        public List<string> Errors { get; set; }

        /// <summary>
        /// 警告列表
        /// </summary>
        public List<string> Warnings { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 获取摘要
        /// </summary>
        public string GetSummary()
        {
            if (IsValid)
            {
                return Warnings.Count > 0 
                    ? $"验证通过，但有 {Warnings.Count} 个警告" 
                    : "验证通过";
            }

            return $"验证失败: {Errors.Count} 个错误, {Warnings.Count} 个警告";
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"验证报告 - {Timestamp:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"状态: {(IsValid ? "通过" : "失败")}");

            if (Errors.Count > 0)
            {
                sb.AppendLine("错误:");
                foreach (var error in Errors)
                {
                    sb.AppendLine($"  - {error}");
                }
            }

            if (Warnings.Count > 0)
            {
                sb.AppendLine("警告:");
                foreach (var warning in Warnings)
                {
                    sb.AppendLine($"  - {warning}");
                }
            }

            return sb.ToString();
        }
    }
}