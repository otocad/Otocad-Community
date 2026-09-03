using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using LitMath;

namespace OtoCAD.Dimension
{
    /// <summary>
    /// 尺寸标注规格定义
    /// </summary>
    public class DimensionSpecification
    {
        #region 属性

        /// <summary>
        /// 唯一标识
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 尺寸名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 尺寸类型
        /// </summary>
        public DimensionType Type { get; set; }

        /// <summary>
        /// 标称值
        /// </summary>
        public double NominalValue { get; set; }

        /// <summary>
        /// 实测值
        /// </summary>
        public double? ActualValue { get; set; }

        /// <summary>
        /// 公差设置
        /// </summary>
        public ToleranceSpecification Tolerance { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// 来源
        /// </summary>
        public DimensionSource Source { get; set; }

        /// <summary>
        /// 是否锁定
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// 关联的特征ID列表
        /// </summary>
        public List<string> BindingFeatureIds { get; set; }

        /// <summary>
        /// 显示设置
        /// </summary>
        public DimensionDisplaySettings DisplaySettings { get; set; }

        /// <summary>
        /// 测量要求
        /// </summary>
        public MeasurementRequirements MeasurementReq { get; set; }

        /// <summary>
        /// 前缀
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// 后缀
        /// </summary>
        public string Suffix { get; set; }

        /// <summary>
        /// 替代文本
        /// </summary>
        public string AlternativeText { get; set; }

        /// <summary>
        /// 标注位置
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// 关联的组件ID
        /// </summary>
        public string ComponentId { get; set; }

        /// <summary>
        /// 父尺寸ID（用于链式标注）
        /// </summary>
        public Guid? ParentDimensionId { get; set; }

        /// <summary>
        /// 子尺寸ID列表（用于链式标注）
        /// </summary>
        public List<Guid> ChildDimensionIds { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; }

        #endregion

        #region 构造函数

        public DimensionSpecification()
        {
            Id = Guid.NewGuid();
            Name = "新尺寸";
            Type = DimensionType.Linear;
            Unit = "mm";
            Source = DimensionSource.Manual;
            IsLocked = false;
            BindingFeatureIds = new List<string>();
            DisplaySettings = new DimensionDisplaySettings();
            MeasurementReq = new MeasurementRequirements();
            Tolerance = new ToleranceSpecification();
            ChildDimensionIds = new List<Guid>();
            CreatedDate = DateTime.Now;
            ModifiedDate = DateTime.Now;
        }

        #endregion

        #region 方法

        /// <summary>
        /// 验证尺寸规格
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
            {
                errors.Add("尺寸名称不能为空");
            }

            if (NominalValue <= 0 && Type != DimensionType.Angle)
            {
                errors.Add("尺寸值必须大于0");
            }

            // 验证公差
            if (Tolerance != null && Tolerance.Type != ToleranceType.None)
            {
                if (Tolerance.Type == ToleranceType.Symmetric && Tolerance.Value <= 0)
                {
                    errors.Add("对称公差值必须大于0");
                }
                else if (Tolerance.Type == ToleranceType.Asymmetric)
                {
                    if (Tolerance.UpperDeviation < Tolerance.LowerDeviation)
                    {
                        errors.Add("上偏差必须大于等于下偏差");
                    }
                }
            }

            // 验证链式标注
            if (Type == DimensionType.Chain && ChildDimensionIds.Count < 2)
            {
                errors.Add("链式标注至少需要2个子尺寸");
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// 计算极限尺寸
        /// </summary>
        public (double max, double min) CalculateLimits()
        {
            if (Tolerance == null || Tolerance.Type == ToleranceType.None)
            {
                return (NominalValue, NominalValue);
            }

            double maxValue = NominalValue;
            double minValue = NominalValue;

            switch (Tolerance.Type)
            {
                case ToleranceType.Symmetric:
                    maxValue = NominalValue + Tolerance.Value;
                    minValue = NominalValue - Tolerance.Value;
                    break;

                case ToleranceType.Asymmetric:
                    maxValue = NominalValue + Tolerance.UpperDeviation;
                    minValue = NominalValue + Tolerance.LowerDeviation;
                    break;

                case ToleranceType.Limit:
                    maxValue = Tolerance.UpperLimit;
                    minValue = Tolerance.LowerLimit;
                    break;
            }

            return (maxValue, minValue);
        }

        /// <summary>
        /// 生成标注文本
        /// </summary>
        public string GenerateAnnotationText()
        {
            if (!string.IsNullOrEmpty(AlternativeText))
            {
                return AlternativeText;
            }

            string text = "";

            // 添加前缀
            if (!string.IsNullOrEmpty(Prefix))
            {
                text += Prefix;
            }
            else
            {
                // 根据类型添加默认前缀
                switch (Type)
                {
                    case DimensionType.Diameter:
                        text += "Ø";
                        break;
                    case DimensionType.Radius:
                        text += "R";
                        break;
                }
            }

            // 添加数值
            string format = DisplaySettings?.DecimalPlaces > 0 
                ? $"F{DisplaySettings.DecimalPlaces}" 
                : "F2";
            text += NominalValue.ToString(format);

            // 添加公差
            if (Tolerance != null && Tolerance.Type != ToleranceType.None)
            {
                text += Tolerance.GenerateToleranceText();
            }

            // 添加单位
            if (DisplaySettings?.ShowUnit == true)
            {
                text += Unit;
            }

            // 添加后缀
            if (!string.IsNullOrEmpty(Suffix))
            {
                text += Suffix;
            }

            return text;
        }

        /// <summary>
        /// 检查实测值是否合格
        /// </summary>
        public bool CheckActualValue()
        {
            if (!ActualValue.HasValue)
            {
                return false;
            }

            var (max, min) = CalculateLimits();
            return ActualValue.Value >= min && ActualValue.Value <= max;
        }

        /// <summary>
        /// 更新实测值
        /// </summary>
        public void UpdateActualValue(double value)
        {
            ActualValue = value;
            ModifiedDate = DateTime.Now;
        }

        /// <summary>
        /// 锁定/解锁尺寸
        /// </summary>
        public void SetLocked(bool locked)
        {
            IsLocked = locked;
            ModifiedDate = DateTime.Now;
        }

        /// <summary>
        /// 克隆尺寸规格
        /// </summary>
        public DimensionSpecification Clone()
        {
            return new DimensionSpecification
            {
                Id = Guid.NewGuid(),
                Name = this.Name + "_副本",
                Type = this.Type,
                NominalValue = this.NominalValue,
                ActualValue = this.ActualValue,
                Tolerance = this.Tolerance?.Clone(),
                Unit = this.Unit,
                Source = DimensionSource.Manual,
                IsLocked = false,
                DisplaySettings = this.DisplaySettings?.Clone(),
                MeasurementReq = this.MeasurementReq?.Clone(),
                Prefix = this.Prefix,
                Suffix = this.Suffix,
                Position = this.Position,
                ComponentId = this.ComponentId,
                Remarks = this.Remarks,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };
        }

        #endregion
    }

    /// <summary>
    /// 公差规格
    /// </summary>
    public class ToleranceSpecification
    {
        public ToleranceType Type { get; set; }
        public double Value { get; set; }  // 对称公差值
        public double UpperDeviation { get; set; }  // 上偏差
        public double LowerDeviation { get; set; }  // 下偏差
        public double UpperLimit { get; set; }  // 上极限
        public double LowerLimit { get; set; }  // 下极限
        public ToleranceGrade? Grade { get; set; }  // IT等级
        public string Deviation { get; set; }  // 基本偏差 (H, g, etc.)

        public ToleranceSpecification()
        {
            Type = ToleranceType.None;
        }

        public string GenerateToleranceText()
        {
            switch (Type)
            {
                case ToleranceType.Symmetric:
                    return $"±{Value}";

                case ToleranceType.Asymmetric:
                    string upper = UpperDeviation >= 0 ? $"+{UpperDeviation}" : UpperDeviation.ToString();
                    string lower = LowerDeviation >= 0 ? $"+{LowerDeviation}" : LowerDeviation.ToString();
                    return $"{upper}/{lower}";

                case ToleranceType.Fit:
                    if (Grade.HasValue && !string.IsNullOrEmpty(Deviation))
                    {
                        return $"{Deviation}{(int)Grade.Value}";
                    }
                    break;

                case ToleranceType.Limit:
                    return $"({LowerLimit}~{UpperLimit})";
            }

            return "";
        }

        public ToleranceSpecification Clone()
        {
            return new ToleranceSpecification
            {
                Type = this.Type,
                Value = this.Value,
                UpperDeviation = this.UpperDeviation,
                LowerDeviation = this.LowerDeviation,
                UpperLimit = this.UpperLimit,
                LowerLimit = this.LowerLimit,
                Grade = this.Grade,
                Deviation = this.Deviation
            };
        }
    }

    /// <summary>
    /// 尺寸显示设置
    /// </summary>
    public class DimensionDisplaySettings
    {
        public int DecimalPlaces { get; set; }
        public bool ShowUnit { get; set; }
        public double TextHeight { get; set; }
        public string TextStyle { get; set; }
        public bool ShowLeaderLine { get; set; }
        public double LeaderOffset { get; set; }

        public DimensionDisplaySettings()
        {
            DecimalPlaces = 2;
            ShowUnit = true;
            TextHeight = 2.5;
            TextStyle = "Standard";
            ShowLeaderLine = true;
            LeaderOffset = 10;
        }

        public DimensionDisplaySettings Clone()
        {
            return new DimensionDisplaySettings
            {
                DecimalPlaces = this.DecimalPlaces,
                ShowUnit = this.ShowUnit,
                TextHeight = this.TextHeight,
                TextStyle = this.TextStyle,
                ShowLeaderLine = this.ShowLeaderLine,
                LeaderOffset = this.LeaderOffset
            };
        }
    }

    /// <summary>
    /// 测量要求
    /// </summary>
    public class MeasurementRequirements
    {
        public MeasurementMethod Method { get; set; }
        public string MeasurementPosition { get; set; }
        public string Temperature { get; set; }
        public int MeasurementCount { get; set; }
        public string SpecialRequirements { get; set; }

        public MeasurementRequirements()
        {
            Method = MeasurementMethod.Caliper;
            Temperature = "20±2°C";
            MeasurementCount = 3;
        }

        public MeasurementRequirements Clone()
        {
            return new MeasurementRequirements
            {
                Method = this.Method,
                MeasurementPosition = this.MeasurementPosition,
                Temperature = this.Temperature,
                MeasurementCount = this.MeasurementCount,
                SpecialRequirements = this.SpecialRequirements
            };
        }
    }
}