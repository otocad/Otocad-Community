using System;
using System.Collections.Generic;
using System.Linq;

namespace OtoCAD.Dimension
{
    /// <summary>
    /// 尺寸标注模板
    /// </summary>
    public class DimensionTemplate
    {
        #region 属性

        /// <summary>
        /// 模板ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 模板名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 模板描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 适用元件类型
        /// </summary>
        public string ComponentType { get; set; }

        /// <summary>
        /// 尺寸定义列表
        /// </summary>
        public List<DimensionDefinition> Definitions { get; set; }

        /// <summary>
        /// 是否为系统模板
        /// </summary>
        public bool IsSystem { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// 使用次数
        /// </summary>
        public int UsageCount { get; set; }

        #endregion

        #region 构造函数

        public DimensionTemplate()
        {
            Id = Guid.NewGuid();
            Definitions = new List<DimensionDefinition>();
            CreatedDate = DateTime.Now;
            UsageCount = 0;
        }

        #endregion

        #region 静态方法

        /// <summary>
        /// 创建标准尺寸模板
        /// </summary>
        public static List<DimensionTemplate> CreateStandardTemplates()
        {
            var templates = new List<DimensionTemplate>();

            // 1. 精密透镜模板
            templates.Add(new DimensionTemplate
            {
                Name = "精密透镜",
                Description = "精密光学透镜标准尺寸模板",
                ComponentType = "Lens",
                IsSystem = true,
                Definitions = new List<DimensionDefinition>
                {
                    new DimensionDefinition
                    {
                        Name = "直径",
                        Type = DimensionType.Diameter,
                        ToleranceGrade = ToleranceGrade.IT7,
                        Prefix = "Ø",
                        DecimalPlaces = 2
                    },
                    new DimensionDefinition
                    {
                        Name = "中心厚度",
                        Type = DimensionType.Thickness,
                        ToleranceValue = 0.02,
                        DecimalPlaces = 2
                    },
                    new DimensionDefinition
                    {
                        Name = "曲率半径1",
                        Type = DimensionType.Radius,
                        TolerancePercent = 0.5,
                        Prefix = "R",
                        DecimalPlaces = 1
                    },
                    new DimensionDefinition
                    {
                        Name = "曲率半径2",
                        Type = DimensionType.Radius,
                        TolerancePercent = 0.5,
                        Prefix = "R",
                        DecimalPlaces = 1
                    }
                }
            });

            // 2. 标准窗口片模板
            templates.Add(new DimensionTemplate
            {
                Name = "标准窗口片",
                Description = "平面窗口片标准尺寸模板",
                ComponentType = "Window",
                IsSystem = true,
                Definitions = new List<DimensionDefinition>
                {
                    new DimensionDefinition
                    {
                        Name = "直径",
                        Type = DimensionType.Diameter,
                        ToleranceGrade = ToleranceGrade.IT8,
                        Prefix = "Ø",
                        DecimalPlaces = 2
                    },
                    new DimensionDefinition
                    {
                        Name = "厚度",
                        Type = DimensionType.Thickness,
                        ToleranceValue = 0.05,
                        DecimalPlaces = 2
                    },
                    new DimensionDefinition
                    {
                        Name = "平行度",
                        Type = DimensionType.Reference,
                        ToleranceValue = 0.01,
                        DecimalPlaces = 3
                    }
                }
            });

            // 3. 棱镜模板
            templates.Add(new DimensionTemplate
            {
                Name = "直角棱镜",
                Description = "直角棱镜标准尺寸模板",
                ComponentType = "Prism",
                IsSystem = true,
                Definitions = new List<DimensionDefinition>
                {
                    new DimensionDefinition
                    {
                        Name = "边长",
                        Type = DimensionType.Linear,
                        ToleranceValue = 0.1,
                        DecimalPlaces = 1
                    },
                    new DimensionDefinition
                    {
                        Name = "直角",
                        Type = DimensionType.Angle,
                        NominalValue = 90,
                        ToleranceValue = 0.05,
                        Suffix = "°",
                        DecimalPlaces = 2
                    },
                    new DimensionDefinition
                    {
                        Name = "斜边长度",
                        Type = DimensionType.Linear,
                        ToleranceValue = 0.1,
                        DecimalPlaces = 1
                    }
                }
            });

            // 4. 反射镜模板
            templates.Add(new DimensionTemplate
            {
                Name = "平面反射镜",
                Description = "平面反射镜标准尺寸模板",
                ComponentType = "Mirror",
                IsSystem = true,
                Definitions = new List<DimensionDefinition>
                {
                    new DimensionDefinition
                    {
                        Name = "直径",
                        Type = DimensionType.Diameter,
                        ToleranceGrade = ToleranceGrade.IT7,
                        Prefix = "Ø",
                        DecimalPlaces = 2
                    },
                    new DimensionDefinition
                    {
                        Name = "厚度",
                        Type = DimensionType.Thickness,
                        ToleranceValue = 0.1,
                        DecimalPlaces = 1
                    },
                    new DimensionDefinition
                    {
                        Name = "平面度",
                        Type = DimensionType.Reference,
                        NominalValue = 0.1,
                        Prefix = "λ/",
                        DecimalPlaces = 0
                    }
                }
            });

            // 5. 链式标注模板
            templates.Add(new DimensionTemplate
            {
                Name = "台阶透镜链式标注",
                Description = "多台阶透镜链式尺寸模板",
                ComponentType = "StepLens",
                IsSystem = true,
                Definitions = new List<DimensionDefinition>
                {
                    new DimensionDefinition
                    {
                        Name = "总长度",
                        Type = DimensionType.Linear,
                        ToleranceValue = 0.1,
                        DecimalPlaces = 1
                    },
                    new DimensionDefinition
                    {
                        Name = "台阶1",
                        Type = DimensionType.Chain,
                        ToleranceValue = 0.05,
                        DecimalPlaces = 2
                    },
                    new DimensionDefinition
                    {
                        Name = "台阶2",
                        Type = DimensionType.Chain,
                        ToleranceValue = 0.05,
                        DecimalPlaces = 2
                    },
                    new DimensionDefinition
                    {
                        Name = "台阶3",
                        Type = DimensionType.Chain,
                        ToleranceValue = 0.05,
                        DecimalPlaces = 2
                    }
                }
            });

            return templates;
        }

        #endregion

        #region 方法

        /// <summary>
        /// 生成尺寸规格
        /// </summary>
        public List<DimensionSpecification> GenerateDimensions(string componentId)
        {
            var dimensions = new List<DimensionSpecification>();

            foreach (var def in Definitions)
            {
                var spec = new DimensionSpecification
                {
                    Name = def.Name,
                    Type = def.Type,
                    NominalValue = def.NominalValue,
                    ComponentId = componentId,
                    Source = DimensionSource.Auto,
                    Prefix = def.Prefix,
                    Suffix = def.Suffix,
                    DisplaySettings = new DimensionDisplaySettings
                    {
                        DecimalPlaces = def.DecimalPlaces,
                        ShowUnit = true
                    }
                };

                // 设置公差
                if (def.ToleranceGrade.HasValue)
                {
                    spec.Tolerance = new ToleranceSpecification
                    {
                        Type = ToleranceType.Fit,
                        Grade = def.ToleranceGrade.Value
                    };
                }
                else if (def.ToleranceValue > 0)
                {
                    spec.Tolerance = new ToleranceSpecification
                    {
                        Type = ToleranceType.Symmetric,
                        Value = def.ToleranceValue
                    };
                }
                else if (def.TolerancePercent > 0)
                {
                    spec.Tolerance = new ToleranceSpecification
                    {
                        Type = ToleranceType.Symmetric,
                        Value = Math.Abs(def.NominalValue * def.TolerancePercent / 100)
                    };
                }

                dimensions.Add(spec);
            }

            // 增加使用次数
            UsageCount++;

            return dimensions;
        }

        #endregion
    }

    /// <summary>
    /// 尺寸定义
    /// </summary>
    public class DimensionDefinition
    {
        public string Name { get; set; }
        public DimensionType Type { get; set; }
        public double NominalValue { get; set; }
        public double ToleranceValue { get; set; }
        public double TolerancePercent { get; set; }
        public ToleranceGrade? ToleranceGrade { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public int DecimalPlaces { get; set; }

        public DimensionDefinition()
        {
            DecimalPlaces = 2;
        }
    }
}