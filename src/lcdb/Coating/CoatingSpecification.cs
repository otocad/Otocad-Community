using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace lcdb.Coating
{
    /// <summary>
    /// 镀膜规格
    /// </summary>
    public class CoatingSpecification
    {
        #region 基本属性

        /// <summary>
        /// 唯一标识
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 名称
        /// </summary>
        [Description("名称")]
        public string Name { get; set; } = "新镀膜";

        /// <summary>
        /// 镀膜类型
        /// </summary>
        [Description("类型")]
        public CoatingType Type { get; set; } = CoatingType.AR;

        /// <summary>
        /// 配方名称
        /// </summary>
        [Description("配方")]
        public string Recipe { get; set; } = "多层宽带AR";

        /// <summary>
        /// 描述
        /// </summary>
        [Description("描述")]
        public string Description { get; set; }

        /// <summary>
        /// 表面编号
        /// </summary>
        [Description("表面")]
        public string SurfaceId { get; set; }

        /// <summary>
        /// 元件编号
        /// </summary>
        [Description("元件")]
        public string ComponentId { get; set; }

        #endregion

        #region 光谱参数

        /// <summary>
        /// 起始波长（nm）
        /// </summary>
        [Description("起始波长(nm)")]
        public double WavelengthStart { get; set; } = 420;

        /// <summary>
        /// 结束波长（nm）
        /// </summary>
        [Description("结束波长(nm)")]
        public double WavelengthEnd { get; set; } = 680;

        /// <summary>
        /// 中心波长（nm）
        /// </summary>
        [Description("中心波长(nm)")]
        public double CenterWavelength => (WavelengthStart + WavelengthEnd) / 2;

        /// <summary>
        /// 设计波长（nm）
        /// </summary>
        [Description("设计波长(nm)")]
        public double DesignWavelength { get; set; } = 550;

        /// <summary>
        /// 入射角（度）
        /// </summary>
        [Description("入射角(°)")]
        public double IncidentAngle { get; set; } = 0;

        /// <summary>
        /// 偏振态
        /// </summary>
        [Description("偏振态")]
        public PolarizationType Polarization { get; set; } = PolarizationType.Unpolarized;

        /// <summary>
        /// 基底材料
        /// </summary>
        [Description("基底材料")]
        public string SubstrateMaterial { get; set; } = "H-K9L";

        /// <summary>
        /// 基底折射率
        /// </summary>
        [Description("基底折射率")]
        public double SubstrateIndex { get; set; } = 1.5168;

        #endregion

        #region 膜层结构

        /// <summary>
        /// 膜层列表
        /// </summary>
        public List<CoatingLayer> Layers { get; set; } = new List<CoatingLayer>();

        /// <summary>
        /// 总厚度（nm）
        /// </summary>
        [Description("总厚度(nm)")]
        public double TotalThickness => Layers.Sum(l => l.PhysicalThickness);

        /// <summary>
        /// 层数
        /// </summary>
        [Description("层数")]
        public int LayerCount => Layers.Count;

        #endregion

        #region 性能要求

        /// <summary>
        /// 平均反射率要求（%）
        /// </summary>
        [Description("平均反射率(%)")]
        public double AverageReflectanceTarget { get; set; } = 0.5;

        /// <summary>
        /// 最大反射率要求（%）
        /// </summary>
        [Description("最大反射率(%)")]
        public double MaximumReflectanceTarget { get; set; } = 1.0;

        /// <summary>
        /// 平均透过率要求（%）
        /// </summary>
        [Description("平均透过率(%)")]
        public double AverageTransmittanceTarget { get; set; } = 99.0;

        /// <summary>
        /// 最小透过率要求（%）
        /// </summary>
        [Description("最小透过率(%)")]
        public double MinimumTransmittanceTarget { get; set; } = 98.5;

        /// <summary>
        /// 色中性要求
        /// </summary>
        [Description("色中性")]
        public bool ColorNeutral { get; set; } = true;

        /// <summary>
        /// 相位线性要求
        /// </summary>
        [Description("相位线性")]
        public bool PhaseLinear { get; set; } = false;

        #endregion

        #region 工艺参数

        /// <summary>
        /// 镀膜工艺
        /// </summary>
        [Description("镀膜工艺")]
        public CoatingProcessType ProcessType { get; set; } = CoatingProcessType.EBeam;

        /// <summary>
        /// 基板温度（°C）
        /// </summary>
        [Description("基板温度(°C)")]
        public double SubstrateTemperature { get; set; } = 150;

        /// <summary>
        /// 沉积速率（nm/s）
        /// </summary>
        [Description("沉积速率(nm/s)")]
        public double DepositionRate { get; set; } = 0.5;

        /// <summary>
        /// 真空度（Pa）
        /// </summary>
        [Description("真空度(Pa)")]
        public double Pressure { get; set; } = 2e-4;

        /// <summary>
        /// 离子能量（eV）
        /// </summary>
        [Description("离子能量(eV)")]
        public double IonEnergy { get; set; } = 120;

        /// <summary>
        /// 监控方式
        /// </summary>
        [Description("监控方式")]
        public MonitoringType Monitoring { get; set; } = MonitoringType.Optical;

        /// <summary>
        /// 监控波长（nm）
        /// </summary>
        [Description("监控波长(nm)")]
        public double MonitoringWavelength { get; set; } = 550;

        #endregion

        #region 成本估算

        /// <summary>
        /// 材料成本（元）
        /// </summary>
        [Description("材料成本(¥)")]
        public decimal MaterialCost { get; set; }

        /// <summary>
        /// 工艺成本（元）
        /// </summary>
        [Description("工艺成本(¥)")]
        public decimal ProcessCost { get; set; }

        /// <summary>
        /// 其他成本（元）
        /// </summary>
        [Description("其他成本(¥)")]
        public decimal OtherCost { get; set; }

        /// <summary>
        /// 总成本（元）
        /// </summary>
        [Description("总成本(¥)")]
        public decimal TotalCost => MaterialCost + ProcessCost + OtherCost;

        #endregion

        #region 状态和验证

        /// <summary>
        /// 是否已验证
        /// </summary>
        [Description("已验证")]
        public bool IsValidated { get; set; }

        /// <summary>
        /// 验证时间
        /// </summary>
        [Description("验证时间")]
        public DateTime? ValidationTime { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Description("创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 修改时间
        /// </summary>
        [Description("修改时间")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        #endregion

        #region 方法

        /// <summary>
        /// 添加层
        /// </summary>
        public void AddLayer(CoatingLayer layer)
        {
            layer.Index = Layers.Count + 1;
            Layers.Add(layer);
            UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// 删除层
        /// </summary>
        public void RemoveLayer(int index)
        {
            if (index >= 0 && index < Layers.Count)
            {
                Layers.RemoveAt(index);
                // 重新编号
                for (int i = 0; i < Layers.Count; i++)
                {
                    Layers[i].Index = i + 1;
                }
                UpdatedAt = DateTime.Now;
            }
        }

        /// <summary>
        /// 插入层
        /// </summary>
        public void InsertLayer(int index, CoatingLayer layer)
        {
            if (index >= 0 && index <= Layers.Count)
            {
                Layers.Insert(index, layer);
                // 重新编号
                for (int i = 0; i < Layers.Count; i++)
                {
                    Layers[i].Index = i + 1;
                }
                UpdatedAt = DateTime.Now;
            }
        }

        /// <summary>
        /// 移动层
        /// </summary>
        public void MoveLayer(int fromIndex, int toIndex)
        {
            if (fromIndex >= 0 && fromIndex < Layers.Count &&
                toIndex >= 0 && toIndex < Layers.Count &&
                fromIndex != toIndex)
            {
                var layer = Layers[fromIndex];
                Layers.RemoveAt(fromIndex);
                Layers.Insert(toIndex, layer);
                // 重新编号
                for (int i = 0; i < Layers.Count; i++)
                {
                    Layers[i].Index = i + 1;
                }
                UpdatedAt = DateTime.Now;
            }
        }

        /// <summary>
        /// 清空所有层
        /// </summary>
        public void ClearLayers()
        {
            Layers.Clear();
            UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// 从模板加载
        /// </summary>
        public void LoadFromTemplate(CoatingTemplate template)
        {
            if (template != null)
            {
                Type = template.Type;
                Recipe = template.Name;
                Description = template.Description;
                
                // 复制光谱参数
                WavelengthStart = template.WavelengthStart;
                WavelengthEnd = template.WavelengthEnd;
                DesignWavelength = template.DesignWavelength;
                IncidentAngle = template.IncidentAngle;
                Polarization = template.Polarization;
                
                // 复制膜层
                Layers.Clear();
                foreach (var layer in template.Layers)
                {
                    AddLayer(layer.Clone());
                }
                
                // 复制性能要求
                AverageReflectanceTarget = template.AverageReflectanceTarget;
                MaximumReflectanceTarget = template.MaximumReflectanceTarget;
                AverageTransmittanceTarget = template.AverageTransmittanceTarget;
                MinimumTransmittanceTarget = template.MinimumTransmittanceTarget;
                
                // 复制工艺参数
                ProcessType = template.ProcessType;
                SubstrateTemperature = template.SubstrateTemperature;
                DepositionRate = template.DepositionRate;
                Pressure = template.Pressure;
                
                UpdatedAt = DateTime.Now;
            }
        }

        /// <summary>
        /// 验证镀膜参数
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            // 验证波长范围
            if (WavelengthStart <= 0 || WavelengthEnd <= 0)
            {
                errors.Add("波长范围必须大于0");
            }
            if (WavelengthStart >= WavelengthEnd)
            {
                errors.Add("起始波长必须小于结束波长");
            }
            if (WavelengthStart < 200 || WavelengthEnd > 2000)
            {
                errors.Add("波长范围应在200-2000nm之间");
            }

            // 验证入射角
            if (IncidentAngle < 0 || IncidentAngle >= 90)
            {
                errors.Add("入射角应在0-89°之间");
            }

            // 验证膜层
            if (Layers.Count == 0)
            {
                errors.Add("至少需要一层膜层");
            }
            else if (Layers.Count > 99)
            {
                errors.Add("膜层数不能超过99层");
            }

            // 验证每一层
            foreach (var layer in Layers)
            {
                if (!layer.Validate(out string layerError))
                {
                    errors.Add(layerError);
                }
            }

            // 验证总厚度
            if (TotalThickness > 50000)
            {
                errors.Add("总厚度不能超过50μm");
            }

            // 验证性能要求
            if (AverageReflectanceTarget < 0 || AverageReflectanceTarget > 100)
            {
                errors.Add("平均反射率应在0-100%之间");
            }
            if (AverageTransmittanceTarget < 0 || AverageTransmittanceTarget > 100)
            {
                errors.Add("平均透过率应在0-100%之间");
            }

            // 验证工艺参数
            if (SubstrateTemperature < 20 || SubstrateTemperature > 500)
            {
                errors.Add("基板温度应在20-500°C之间");
            }
            if (Pressure < 1e-6 || Pressure > 1e-2)
            {
                errors.Add("真空度应在1e-6到1e-2 Pa之间");
            }

            IsValidated = errors.Count == 0;
            if (IsValidated)
            {
                ValidationTime = DateTime.Now;
            }

            return IsValidated;
        }

        /// <summary>
        /// 计算成本
        /// </summary>
        public void CalculateCost()
        {
            // 材料成本计算
            MaterialCost = 0;
            foreach (var layer in Layers)
            {
                // 根据材料和厚度计算
                decimal costPerNm = GetMaterialCostPerNm(layer.Material);
                MaterialCost += (decimal)layer.PhysicalThickness * costPerNm;
            }

            // 工艺成本计算
            double totalTime = TotalThickness / DepositionRate / 3600; // 小时
            ProcessCost = (decimal)(totalTime * 80); // 80元/小时

            // 其他成本
            OtherCost = 15; // 清洗、检验、包装等

            UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// 获取材料单价
        /// </summary>
        private decimal GetMaterialCostPerNm(string material)
        {
            // 简化的材料价格表
            switch (material.ToUpper())
            {
                case "MGF2": return 0.10m;
                case "TIO2": return 0.15m;
                case "SIO2": return 0.08m;
                case "AL2O3": return 0.12m;
                case "TA2O5": return 0.20m;
                case "HFO2": return 0.25m;
                default: return 0.10m;
            }
        }

        /// <summary>
        /// 克隆
        /// </summary>
        public CoatingSpecification Clone()
        {
            var clone = new CoatingSpecification
            {
                Id = Guid.NewGuid().ToString(),
                Name = this.Name + "_副本",
                Type = this.Type,
                Recipe = this.Recipe,
                Description = this.Description,
                SurfaceId = this.SurfaceId,
                ComponentId = this.ComponentId,
                WavelengthStart = this.WavelengthStart,
                WavelengthEnd = this.WavelengthEnd,
                DesignWavelength = this.DesignWavelength,
                IncidentAngle = this.IncidentAngle,
                Polarization = this.Polarization,
                SubstrateMaterial = this.SubstrateMaterial,
                SubstrateIndex = this.SubstrateIndex,
                AverageReflectanceTarget = this.AverageReflectanceTarget,
                MaximumReflectanceTarget = this.MaximumReflectanceTarget,
                AverageTransmittanceTarget = this.AverageTransmittanceTarget,
                MinimumTransmittanceTarget = this.MinimumTransmittanceTarget,
                ColorNeutral = this.ColorNeutral,
                PhaseLinear = this.PhaseLinear,
                ProcessType = this.ProcessType,
                SubstrateTemperature = this.SubstrateTemperature,
                DepositionRate = this.DepositionRate,
                Pressure = this.Pressure,
                IonEnergy = this.IonEnergy,
                Monitoring = this.Monitoring,
                MonitoringWavelength = this.MonitoringWavelength,
                MaterialCost = this.MaterialCost,
                ProcessCost = this.ProcessCost,
                OtherCost = this.OtherCost,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // 复制膜层
            foreach (var layer in Layers)
            {
                clone.Layers.Add(layer.Clone());
            }

            return clone;
        }

        public override string ToString()
        {
            return $"{Name} ({Type}, {LayerCount}层, {TotalThickness:F1}nm)";
        }

        #endregion
    }
}