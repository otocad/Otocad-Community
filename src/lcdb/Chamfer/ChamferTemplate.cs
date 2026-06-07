using System;
using System.Collections.Generic;
using System.Linq;

namespace lcdb.Chamfer
{
    /// <summary>
    /// 倒角模板
    /// </summary>
    public class ChamferTemplate
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
        /// 倒角类型
        /// </summary>
        public ChamferType Type { get; set; }

        /// <summary>
        /// 倒角位置
        /// </summary>
        public ChamferPosition Position { get; set; }

        /// <summary>
        /// 倒角尺寸（mm）
        /// </summary>
        public double Size { get; set; }

        /// <summary>
        /// 倒角角度（度）
        /// </summary>
        public double Angle { get; set; }

        /// <summary>
        /// 倒角深度（mm）
        /// </summary>
        public double Depth { get; set; }

        /// <summary>
        /// 表面处理
        /// </summary>
        public SurfaceFinish Finish { get; set; }

        /// <summary>
        /// 加工难度
        /// </summary>
        public ProcessingDifficulty Difficulty { get; set; }

        /// <summary>
        /// 公差等级
        /// </summary>
        public string ToleranceGrade { get; set; }

        /// <summary>
        /// 粗糙度要求（Ra值，μm）
        /// </summary>
        public double Roughness { get; set; }

        /// <summary>
        /// 是否需要保护
        /// </summary>
        public bool RequiresProtection { get; set; }

        /// <summary>
        /// 加工工艺说明
        /// </summary>
        public string ProcessingNotes { get; set; }

        /// <summary>
        /// 检验要求
        /// </summary>
        public string InspectionRequirements { get; set; }

        /// <summary>
        /// 适用材料
        /// </summary>
        public List<string> ApplicableMaterials { get; set; }

        /// <summary>
        /// 适用元件类型
        /// </summary>
        public List<string> ApplicableComponents { get; set; }

        /// <summary>
        /// 是否为标准模板
        /// </summary>
        public bool IsStandard { get; set; }

        /// <summary>
        /// 是否为系统模板
        /// </summary>
        public bool IsSystem { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// 使用次数
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// 模板类别
        /// </summary>
        public string Category { get; set; }

        #endregion

        #region 构造函数

        public ChamferTemplate()
        {
            Id = Guid.NewGuid();
            ApplicableMaterials = new List<string>();
            ApplicableComponents = new List<string>();
            CreatedDate = DateTime.Now;
            ModifiedDate = DateTime.Now;
            UsageCount = 0;
        }

        #endregion

        #region 静态方法

        /// <summary>
        /// 创建标准倒角模板
        /// </summary>
        public static List<ChamferTemplate> CreateStandardTemplates()
        {
            var templates = new List<ChamferTemplate>();

            // 1. 标准C型倒角 - 0.3×45°
            templates.Add(new ChamferTemplate
            {
                Name = "标准C型倒角 0.3×45°",
                Description = "最常用的标准倒角，适用于大多数光学元件",
                Type = ChamferType.CType,
                Position = ChamferPosition.OuterCircle,
                Size = 0.3,
                Angle = 45,
                Depth = 0.3,
                Finish = SurfaceFinish.FineGrind,
                Difficulty = ProcessingDifficulty.Easy,
                ToleranceGrade = "IT11",
                Roughness = 1.6,
                RequiresProtection = false,
                ProcessingNotes = "标准磨边工艺，注意防止崩边",
                InspectionRequirements = "目视检查，无崩边、裂纹",
                ApplicableMaterials = new List<string> { "H-K9L", "BK7", "石英", "熔石英" },
                ApplicableComponents = new List<string> { "透镜", "窗口片", "棱镜" },
                IsStandard = true,
                IsSystem = true,
                Category = "常用"
            });

            // 2. 精密C型倒角 - 0.2×45°
            templates.Add(new ChamferTemplate
            {
                Name = "精密C型倒角 0.2×45°",
                Description = "精密光学元件用小倒角",
                Type = ChamferType.CType,
                Position = ChamferPosition.OuterCircle,
                Size = 0.2,
                Angle = 45,
                Depth = 0.2,
                Finish = SurfaceFinish.Polish,
                Difficulty = ProcessingDifficulty.Medium,
                ToleranceGrade = "IT9",
                Roughness = 0.4,
                RequiresProtection = true,
                ProcessingNotes = "精密磨边，需要保护主表面",
                InspectionRequirements = "显微镜检查，无崩边、裂纹",
                ApplicableMaterials = new List<string> { "H-K9L", "BK7", "熔石英" },
                ApplicableComponents = new List<string> { "精密透镜", "激光元件" },
                IsStandard = true,
                IsSystem = true,
                Category = "精密"
            });

            // 3. 标准R型倒角 - R0.5
            templates.Add(new ChamferTemplate
            {
                Name = "标准R型倒角 R0.5",
                Description = "圆弧倒角，减少应力集中",
                Type = ChamferType.RType,
                Position = ChamferPosition.OuterCircle,
                Size = 0.5,
                Angle = 0,
                Depth = 0.5,
                Finish = SurfaceFinish.FineGrind,
                Difficulty = ProcessingDifficulty.Medium,
                ToleranceGrade = "IT11",
                Roughness = 1.6,
                RequiresProtection = false,
                ProcessingNotes = "圆弧倒角，注意圆弧过渡平滑",
                InspectionRequirements = "轮廓仪检查圆弧形状",
                ApplicableMaterials = new List<string> { "H-K9L", "BK7", "石英" },
                ApplicableComponents = new List<string> { "透镜", "棱镜" },
                IsStandard = true,
                IsSystem = true,
                Category = "常用"
            });

            // 4. 微倒角 - 0.05×45°
            templates.Add(new ChamferTemplate
            {
                Name = "微倒角 0.05×45°",
                Description = "超小倒角，用于精密小型光学元件",
                Type = ChamferType.Micro,
                Position = ChamferPosition.OuterCircle,
                Size = 0.05,
                Angle = 45,
                Depth = 0.05,
                Finish = SurfaceFinish.Polish,
                Difficulty = ProcessingDifficulty.Hard,
                ToleranceGrade = "IT7",
                Roughness = 0.2,
                RequiresProtection = true,
                ProcessingNotes = "显微镜下加工，防止崩边",
                InspectionRequirements = "高倍显微镜检查",
                ApplicableMaterials = new List<string> { "熔石英", "蓝宝石" },
                ApplicableComponents = new List<string> { "微透镜", "光纤端面" },
                IsStandard = true,
                IsSystem = true,
                Category = "精密"
            });

            // 5. 保护倒角 - 0.5×30°
            templates.Add(new ChamferTemplate
            {
                Name = "保护倒角 0.5×30°",
                Description = "大倒角，提供更好的边缘保护",
                Type = ChamferType.CType,
                Position = ChamferPosition.OuterCircle,
                Size = 0.5,
                Angle = 30,
                Depth = 0.5,
                Finish = SurfaceFinish.FineGrind,
                Difficulty = ProcessingDifficulty.Easy,
                ToleranceGrade = "IT11",
                Roughness = 3.2,
                RequiresProtection = false,
                ProcessingNotes = "大倒角保护边缘",
                InspectionRequirements = "目视检查",
                ApplicableMaterials = new List<string> { "H-K9L", "BK7", "石英", "CaF2" },
                ApplicableComponents = new List<string> { "大口径透镜", "窗口片" },
                IsStandard = true,
                IsSystem = true,
                Category = "保护"
            });

            // 6. 内孔倒角 - 0.2×45°
            templates.Add(new ChamferTemplate
            {
                Name = "内孔倒角 0.2×45°",
                Description = "内孔边缘倒角",
                Type = ChamferType.CType,
                Position = ChamferPosition.InnerHole,
                Size = 0.2,
                Angle = 45,
                Depth = 0.2,
                Finish = SurfaceFinish.FineGrind,
                Difficulty = ProcessingDifficulty.Medium,
                ToleranceGrade = "IT11",
                Roughness = 1.6,
                RequiresProtection = false,
                ProcessingNotes = "内孔倒角，注意工具选择",
                InspectionRequirements = "内孔镜检查",
                ApplicableMaterials = new List<string> { "H-K9L", "BK7", "石英" },
                ApplicableComponents = new List<string> { "环形透镜", "套筒" },
                IsStandard = true,
                IsSystem = true,
                Category = "特殊"
            });

            // 7. 双倒角 - 0.3+0.2×45°
            templates.Add(new ChamferTemplate
            {
                Name = "双倒角 0.3+0.2×45°",
                Description = "双重倒角，提供更好的保护和装配性能",
                Type = ChamferType.Double,
                Position = ChamferPosition.OuterCircle,
                Size = 0.3,
                Angle = 45,
                Depth = 0.5,
                Finish = SurfaceFinish.FineGrind,
                Difficulty = ProcessingDifficulty.Hard,
                ToleranceGrade = "IT10",
                Roughness = 1.6,
                RequiresProtection = true,
                ProcessingNotes = "两次倒角，第一次0.3mm，第二次0.2mm",
                InspectionRequirements = "检查两个倒角面的过渡",
                ApplicableMaterials = new List<string> { "H-K9L", "BK7" },
                ApplicableComponents = new List<string> { "精密透镜", "装配元件" },
                IsStandard = true,
                IsSystem = true,
                Category = "特殊"
            });

            // 8. 抛光倒角 - 0.3×45°
            templates.Add(new ChamferTemplate
            {
                Name = "抛光倒角 0.3×45°",
                Description = "倒角面抛光处理，减少散射",
                Type = ChamferType.CType,
                Position = ChamferPosition.OuterCircle,
                Size = 0.3,
                Angle = 45,
                Depth = 0.3,
                Finish = SurfaceFinish.Polish,
                Difficulty = ProcessingDifficulty.Medium,
                ToleranceGrade = "IT9",
                Roughness = 0.4,
                RequiresProtection = true,
                ProcessingNotes = "倒角面需要抛光处理",
                InspectionRequirements = "检查倒角面光洁度",
                ApplicableMaterials = new List<string> { "H-K9L", "熔石英" },
                ApplicableComponents = new List<string> { "激光透镜", "高精度元件" },
                IsStandard = true,
                IsSystem = true,
                Category = "精密"
            });

            return templates;
        }

        #endregion

        #region 方法

        /// <summary>
        /// 验证模板
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
            {
                errors.Add("模板名称不能为空");
            }

            if (Size <= 0)
            {
                errors.Add("倒角尺寸必须大于0");
            }

            if (Type == ChamferType.CType && (Angle <= 0 || Angle >= 90))
            {
                errors.Add("C型倒角角度必须在0到90度之间");
            }

            if (Roughness <= 0)
            {
                errors.Add("粗糙度值必须大于0");
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// 创建倒角规格实例
        /// </summary>
        public ChamferSpecification CreateSpecification()
        {
            var spec = new ChamferSpecification
            {
                Name = this.Name,
                Type = this.Type,
                Position = this.Position,
                Size = this.Size,
                Angle = this.Angle,
                Depth = this.Depth,
                Finish = this.Finish,
                Difficulty = this.Difficulty,
                ToleranceGrade = this.ToleranceGrade,
                Roughness = this.Roughness,
                RequiresProtection = this.RequiresProtection,
                ProcessingNotes = this.ProcessingNotes,
                InspectionRequirements = this.InspectionRequirements,
                IsStandard = this.IsStandard,
                TemplateId = this.Id
            };

            spec.CalculateCost();
            spec.CalculateProcessingTime();

            // 增加使用次数
            this.UsageCount++;

            return spec;
        }

        /// <summary>
        /// 克隆模板
        /// </summary>
        public ChamferTemplate Clone()
        {
            return new ChamferTemplate
            {
                Id = Guid.NewGuid(),
                Name = this.Name + "_副本",
                Description = this.Description,
                Type = this.Type,
                Position = this.Position,
                Size = this.Size,
                Angle = this.Angle,
                Depth = this.Depth,
                Finish = this.Finish,
                Difficulty = this.Difficulty,
                ToleranceGrade = this.ToleranceGrade,
                Roughness = this.Roughness,
                RequiresProtection = this.RequiresProtection,
                ProcessingNotes = this.ProcessingNotes,
                InspectionRequirements = this.InspectionRequirements,
                ApplicableMaterials = new List<string>(this.ApplicableMaterials),
                ApplicableComponents = new List<string>(this.ApplicableComponents),
                IsStandard = false,
                IsSystem = false,
                Category = this.Category,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                UsageCount = 0
            };
        }

        #endregion
    }
}