using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace lcdb.Coating
{
    /// <summary>
    /// 镀膜模板
    /// </summary>
    public class CoatingTemplate
    {
        /// <summary>
        /// 模板ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 模板名称
        /// </summary>
        [Description("名称")]
        public string Name { get; set; }

        /// <summary>
        /// 模板描述
        /// </summary>
        [Description("描述")]
        public string Description { get; set; }

        /// <summary>
        /// 镀膜类型
        /// </summary>
        [Description("类型")]
        public CoatingType Type { get; set; }

        /// <summary>
        /// 类别
        /// </summary>
        [Description("类别")]
        public string Category { get; set; } = "标准";

        /// <summary>
        /// 标签
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// 起始波长（nm）
        /// </summary>
        [Description("起始波长")]
        public double WavelengthStart { get; set; }

        /// <summary>
        /// 结束波长（nm）
        /// </summary>
        [Description("结束波长")]
        public double WavelengthEnd { get; set; }

        /// <summary>
        /// 设计波长（nm）
        /// </summary>
        [Description("设计波长")]
        public double DesignWavelength { get; set; }

        /// <summary>
        /// 入射角（度）
        /// </summary>
        [Description("入射角")]
        public double IncidentAngle { get; set; }

        /// <summary>
        /// 偏振态
        /// </summary>
        [Description("偏振态")]
        public PolarizationType Polarization { get; set; }

        /// <summary>
        /// 膜层列表
        /// </summary>
        public List<CoatingLayer> Layers { get; set; } = new List<CoatingLayer>();

        /// <summary>
        /// 平均反射率目标（%）
        /// </summary>
        [Description("平均反射率")]
        public double AverageReflectanceTarget { get; set; }

        /// <summary>
        /// 最大反射率目标（%）
        /// </summary>
        [Description("最大反射率")]
        public double MaximumReflectanceTarget { get; set; }

        /// <summary>
        /// 平均透过率目标（%）
        /// </summary>
        [Description("平均透过率")]
        public double AverageTransmittanceTarget { get; set; }

        /// <summary>
        /// 最小透过率目标（%）
        /// </summary>
        [Description("最小透过率")]
        public double MinimumTransmittanceTarget { get; set; }

        /// <summary>
        /// 工艺类型
        /// </summary>
        [Description("工艺")]
        public CoatingProcessType ProcessType { get; set; }

        /// <summary>
        /// 基板温度（°C）
        /// </summary>
        [Description("温度")]
        public double SubstrateTemperature { get; set; }

        /// <summary>
        /// 沉积速率（nm/s）
        /// </summary>
        [Description("速率")]
        public double DepositionRate { get; set; }

        /// <summary>
        /// 真空度（Pa）
        /// </summary>
        [Description("真空度")]
        public double Pressure { get; set; }

        /// <summary>
        /// 适用基底材料
        /// </summary>
        public List<string> ApplicableSubstrates { get; set; } = new List<string>();

        /// <summary>
        /// 作者
        /// </summary>
        [Description("作者")]
        public string Author { get; set; }

        /// <summary>
        /// 版本
        /// </summary>
        [Description("版本")]
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// 是否为系统模板
        /// </summary>
        [Description("系统模板")]
        public bool IsSystemTemplate { get; set; }

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

        /// <summary>
        /// 使用次数
        /// </summary>
        [Description("使用次数")]
        public int UsageCount { get; set; }

        /// <summary>
        /// 评分
        /// </summary>
        [Description("评分")]
        public double Rating { get; set; }

        /// <summary>
        /// 创建标准模板
        /// </summary>
        public static List<CoatingTemplate> CreateStandardTemplates()
        {
            var templates = new List<CoatingTemplate>();

            // 单层MgF2增透膜
            templates.Add(new CoatingTemplate
            {
                Name = "单层MgF2",
                Description = "简单的单层氟化镁增透膜，适用于可见光范围",
                Type = CoatingType.AR,
                Category = "标准",
                Tags = new List<string> { "简单", "低成本", "可见光" },
                WavelengthStart = 400,
                WavelengthEnd = 700,
                DesignWavelength = 550,
                IncidentAngle = 0,
                Polarization = PolarizationType.Unpolarized,
                Layers = new List<CoatingLayer>
                {
                    new CoatingLayer(1, "MgF2", 137.5, 1.38)
                },
                AverageReflectanceTarget = 1.5,
                MaximumReflectanceTarget = 2.5,
                AverageTransmittanceTarget = 98.5,
                MinimumTransmittanceTarget = 97.5,
                ProcessType = CoatingProcessType.EBeam,
                SubstrateTemperature = 150,
                DepositionRate = 0.5,
                Pressure = 2e-4,
                ApplicableSubstrates = new List<string> { "H-K9L", "BK7", "石英" },
                Author = "系统",
                IsSystemTemplate = true
            });

            // 多层宽带AR
            templates.Add(new CoatingTemplate
            {
                Name = "多层宽带AR",
                Description = "7层宽带增透设计，可见光范围平均反射率<0.5%",
                Type = CoatingType.AR,
                Category = "标准",
                Tags = new List<string> { "宽带", "高性能", "可见光" },
                WavelengthStart = 420,
                WavelengthEnd = 680,
                DesignWavelength = 550,
                IncidentAngle = 0,
                Polarization = PolarizationType.Unpolarized,
                Layers = new List<CoatingLayer>
                {
                    new CoatingLayer(1, "MgF2", 138.5, 1.38),
                    new CoatingLayer(2, "TiO2", 25.3, 2.35),
                    new CoatingLayer(3, "SiO2", 45.8, 1.46),
                    new CoatingLayer(4, "TiO2", 15.2, 2.35),
                    new CoatingLayer(5, "SiO2", 32.4, 1.46),
                    new CoatingLayer(6, "TiO2", 18.7, 2.35),
                    new CoatingLayer(7, "MgF2", 9.7, 1.38)
                },
                AverageReflectanceTarget = 0.5,
                MaximumReflectanceTarget = 1.0,
                AverageTransmittanceTarget = 99.5,
                MinimumTransmittanceTarget = 99.0,
                ProcessType = CoatingProcessType.IAD,
                SubstrateTemperature = 180,
                DepositionRate = 0.3,
                Pressure = 1e-4,
                ApplicableSubstrates = new List<string> { "H-K9L", "BK7", "石英" },
                Author = "系统",
                IsSystemTemplate = true
            });

            // V-coat @550nm
            templates.Add(new CoatingTemplate
            {
                Name = "V-coat @550nm",
                Description = "V型增透膜，在550nm处反射率极低",
                Type = CoatingType.AR,
                Category = "标准",
                Tags = new List<string> { "单波长", "高性能", "激光" },
                WavelengthStart = 500,
                WavelengthEnd = 600,
                DesignWavelength = 550,
                IncidentAngle = 0,
                Polarization = PolarizationType.Unpolarized,
                Layers = new List<CoatingLayer>
                {
                    new CoatingLayer(1, "Al2O3", 62.5, 1.62),
                    new CoatingLayer(2, "Ta2O5", 34.8, 2.15),
                    new CoatingLayer(3, "Al2O3", 125.0, 1.62),
                    new CoatingLayer(4, "Ta2O5", 34.8, 2.15),
                    new CoatingLayer(5, "Al2O3", 62.5, 1.62)
                },
                AverageReflectanceTarget = 0.2,
                MaximumReflectanceTarget = 0.5,
                AverageTransmittanceTarget = 99.8,
                MinimumTransmittanceTarget = 99.5,
                ProcessType = CoatingProcessType.IBS,
                SubstrateTemperature = 200,
                DepositionRate = 0.2,
                Pressure = 5e-5,
                ApplicableSubstrates = new List<string> { "H-K9L", "BK7", "石英" },
                Author = "系统",
                IsSystemTemplate = true
            });

            // 激光高反射膜
            templates.Add(new CoatingTemplate
            {
                Name = "激光高反射@532nm",
                Description = "532nm激光高反射膜，反射率>99.5%",
                Type = CoatingType.HR,
                Category = "激光",
                Tags = new List<string> { "激光", "高反射", "532nm" },
                WavelengthStart = 520,
                WavelengthEnd = 544,
                DesignWavelength = 532,
                IncidentAngle = 0,
                Polarization = PolarizationType.Unpolarized,
                Layers = CreateHighReflectorLayers(532, 21),
                AverageReflectanceTarget = 99.5,
                MaximumReflectanceTarget = 99.9,
                AverageTransmittanceTarget = 0.5,
                MinimumTransmittanceTarget = 0.1,
                ProcessType = CoatingProcessType.IBS,
                SubstrateTemperature = 200,
                DepositionRate = 0.15,
                Pressure = 3e-5,
                ApplicableSubstrates = new List<string> { "熔石英", "石英", "BK7" },
                Author = "系统",
                IsSystemTemplate = true
            });

            // 50/50分光膜
            templates.Add(new CoatingTemplate
            {
                Name = "50/50分光膜",
                Description = "可见光范围50/50分光膜",
                Type = CoatingType.BS,
                Category = "标准",
                Tags = new List<string> { "分光", "50/50", "可见光" },
                WavelengthStart = 450,
                WavelengthEnd = 650,
                DesignWavelength = 550,
                IncidentAngle = 45,
                Polarization = PolarizationType.Unpolarized,
                Layers = new List<CoatingLayer>
                {
                    new CoatingLayer(1, "TiO2", 45.0, 2.35),
                    new CoatingLayer(2, "SiO2", 82.5, 1.46),
                    new CoatingLayer(3, "TiO2", 45.0, 2.35),
                    new CoatingLayer(4, "SiO2", 82.5, 1.46),
                    new CoatingLayer(5, "TiO2", 22.5, 2.35)
                },
                AverageReflectanceTarget = 50,
                MaximumReflectanceTarget = 55,
                AverageTransmittanceTarget = 50,
                MinimumTransmittanceTarget = 45,
                ProcessType = CoatingProcessType.EBeam,
                SubstrateTemperature = 180,
                DepositionRate = 0.4,
                Pressure = 1e-4,
                ApplicableSubstrates = new List<string> { "H-K9L", "BK7" },
                Author = "系统",
                IsSystemTemplate = true
            });

            return templates;
        }

        /// <summary>
        /// 创建高反射膜层
        /// </summary>
        private static List<CoatingLayer> CreateHighReflectorLayers(double wavelength, int layerCount)
        {
            var layers = new List<CoatingLayer>();
            double qwot = wavelength / 4.0;

            for (int i = 1; i <= layerCount; i++)
            {
                if (i % 2 == 1)
                {
                    // 高折射率层 (TiO2)
                    layers.Add(new CoatingLayer(i, "TiO2", qwot / 2.35, 2.35));
                }
                else
                {
                    // 低折射率层 (SiO2)
                    layers.Add(new CoatingLayer(i, "SiO2", qwot / 1.46, 1.46));
                }
            }

            return layers;
        }

        /// <summary>
        /// 克隆模板
        /// </summary>
        public CoatingTemplate Clone()
        {
            var clone = new CoatingTemplate
            {
                Id = Guid.NewGuid().ToString(),
                Name = this.Name,
                Description = this.Description,
                Type = this.Type,
                Category = this.Category,
                Tags = new List<string>(this.Tags),
                WavelengthStart = this.WavelengthStart,
                WavelengthEnd = this.WavelengthEnd,
                DesignWavelength = this.DesignWavelength,
                IncidentAngle = this.IncidentAngle,
                Polarization = this.Polarization,
                AverageReflectanceTarget = this.AverageReflectanceTarget,
                MaximumReflectanceTarget = this.MaximumReflectanceTarget,
                AverageTransmittanceTarget = this.AverageTransmittanceTarget,
                MinimumTransmittanceTarget = this.MinimumTransmittanceTarget,
                ProcessType = this.ProcessType,
                SubstrateTemperature = this.SubstrateTemperature,
                DepositionRate = this.DepositionRate,
                Pressure = this.Pressure,
                ApplicableSubstrates = new List<string>(this.ApplicableSubstrates),
                Author = this.Author,
                Version = this.Version,
                IsSystemTemplate = false,
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
            return $"{Name} ({Type}, {Layers.Count}层)";
        }
    }
}