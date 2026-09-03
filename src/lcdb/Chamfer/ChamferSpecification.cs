using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace lcdb.Chamfer
{
    /// <summary>
    /// 倒角规格定义
    /// </summary>
    public class ChamferSpecification
    {
        #region 属性

        /// <summary>
        /// 唯一标识
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 倒角类型
        /// </summary>
        public ChamferType Type { get; set; }

        /// <summary>
        /// 倒角位置
        /// </summary>
        public ChamferPosition Position { get; set; }

        /// <summary>
        /// 倒角尺寸（C型为距离，R型为半径，单位：mm）
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
        /// 备注
        /// </summary>
        public string Remarks { get; set; }

        /// <summary>
        /// 关联的光学元件ID
        /// </summary>
        public string ComponentId { get; set; }

        /// <summary>
        /// 关联的边缘ID
        /// </summary>
        public string EdgeId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// 加工成本（元）
        /// </summary>
        public double ProcessingCost { get; set; }

        /// <summary>
        /// 加工时间（分钟）
        /// </summary>
        public double ProcessingTime { get; set; }

        /// <summary>
        /// 是否为标准倒角
        /// </summary>
        public bool IsStandard { get; set; }

        /// <summary>
        /// 关联的模板ID
        /// </summary>
        public Guid? TemplateId { get; set; }

        #region GB/T 13323 4.3.7 边缘处理扩展属性

        /// <summary>
        /// 角度公差（度） - 用于斜面类型
        /// </summary>
        public double AngleTolerance { get; set; }

        /// <summary>
        /// 最小尺寸（mm） - 用于保护性倒角
        /// </summary>
        public double MinSize { get; set; }

        /// <summary>
        /// 最大尺寸（mm） - 用于保护性倒角
        /// </summary>
        public double MaxSize { get; set; }

        /// <summary>
        /// 尺寸公差（mm）
        /// </summary>
        public double SizeTolerance { get; set; }

        /// <summary>
        /// 是否为功能性边缘（尖棱需要保持）
        /// </summary>
        public bool IsFunctionalEdge { get; set; }

        /// <summary>
        /// 中心偏说明（斜面用）
        /// </summary>
        public string CenteringNote { get; set; }

        #endregion

        #endregion

        #region 构造函数

        public ChamferSpecification()
        {
            Id = Guid.NewGuid();
            Name = "新倒角";
            Type = ChamferType.CType;
            Position = ChamferPosition.OuterCircle;
            Size = 0.3;
            Angle = 45;
            Depth = 0.3;
            Finish = SurfaceFinish.FineGrind;
            Difficulty = ProcessingDifficulty.Easy;
            ToleranceGrade = "IT11";
            Roughness = 1.6;
            RequiresProtection = false;
            IsStandard = true;
            CreatedDate = DateTime.Now;
            ModifiedDate = DateTime.Now;
        }

        #endregion

        #region 方法

        /// <summary>
        /// 验证倒角规格
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
            {
                errors.Add("倒角名称不能为空");
            }

            if (Size <= 0)
            {
                errors.Add("倒角尺寸必须大于0");
            }

            if (Type == ChamferType.CType && (Angle <= 0 || Angle >= 90))
            {
                errors.Add("C型倒角角度必须在0到90度之间");
            }

            if (Type == ChamferType.RType && Size > 10)
            {
                errors.Add("R型倒角半径过大（建议不超过10mm）");
            }

            if (Roughness <= 0)
            {
                errors.Add("粗糙度值必须大于0");
            }

            if (Type == ChamferType.Micro && Size > 0.1)
            {
                errors.Add("微倒角尺寸不应超过0.1mm");
            }

            // GB/T 13323 4.3.7 新增类型验证
            if (Type == ChamferType.SharpEdge && Size > 0)
            {
                errors.Add("尖棱类型不应有尺寸值");
            }

            if (Type == ChamferType.Bevel && (Angle <= 0 || Angle >= 90))
            {
                errors.Add("斜面角度必须在0到90度之间");
            }

            if (Type == ChamferType.Protective)
            {
                if (MinSize <= 0 || MaxSize <= 0)
                {
                    errors.Add("保护性倒角必须指定最小和最大宽度");
                }
                if (MinSize >= MaxSize)
                {
                    errors.Add("最小宽度必须小于最大宽度");
                }
            }

            if (Type == ChamferType.InternalTransition && Size <= 0)
            {
                errors.Add("内部边渡必须指定最大宽度");
            }

            // 验证表面处理与粗糙度的匹配
            switch (Finish)
            {
                case SurfaceFinish.RoughGrind:
                    if (Roughness < 3.2)
                    {
                        errors.Add("粗磨表面粗糙度通常不小于Ra3.2");
                    }
                    break;
                case SurfaceFinish.FineGrind:
                    if (Roughness < 0.8 || Roughness > 3.2)
                    {
                        errors.Add("精磨表面粗糙度通常在Ra0.8-3.2之间");
                    }
                    break;
                case SurfaceFinish.Polish:
                    if (Roughness < 0.1 || Roughness > 0.8)
                    {
                        errors.Add("抛光表面粗糙度通常在Ra0.1-0.8之间");
                    }
                    break;
                case SurfaceFinish.SuperPolish:
                    if (Roughness > 0.1)
                    {
                        errors.Add("超光滑抛光表面粗糙度应小于Ra0.1");
                    }
                    break;
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// 计算加工成本
        /// </summary>
        public void CalculateCost()
        {
            double baseCost = 10; // 基础成本

            // 根据倒角类型调整成本
            switch (Type)
            {
                case ChamferType.CType:
                    baseCost *= 1.0;
                    break;
                case ChamferType.RType:
                    baseCost *= 1.2;
                    break;
                case ChamferType.Special:
                    baseCost *= 2.0;
                    break;
                case ChamferType.Double:
                    baseCost *= 1.8;
                    break;
                case ChamferType.Micro:
                    baseCost *= 1.5;
                    break;
            }

            // 根据表面处理调整成本
            switch (Finish)
            {
                case SurfaceFinish.RoughGrind:
                    baseCost *= 1.0;
                    break;
                case SurfaceFinish.FineGrind:
                    baseCost *= 1.5;
                    break;
                case SurfaceFinish.Polish:
                    baseCost *= 2.5;
                    break;
                case SurfaceFinish.SuperPolish:
                    baseCost *= 4.0;
                    break;
            }

            // 根据加工难度调整成本
            baseCost *= (int)Difficulty;

            // 根据尺寸调整成本
            if (Size > 5)
            {
                baseCost *= 1.5;
            }
            else if (Size < 0.5)
            {
                baseCost *= 1.3;
            }

            // 如果需要保护，增加成本
            if (RequiresProtection)
            {
                baseCost *= 1.2;
            }

            ProcessingCost = Math.Round(baseCost, 2);
        }

        /// <summary>
        /// 计算加工时间
        /// </summary>
        public void CalculateProcessingTime()
        {
            double baseTime = 5; // 基础时间（分钟）

            // 根据倒角类型调整时间
            switch (Type)
            {
                case ChamferType.CType:
                    baseTime *= 1.0;
                    break;
                case ChamferType.RType:
                    baseTime *= 1.3;
                    break;
                case ChamferType.Special:
                    baseTime *= 2.5;
                    break;
                case ChamferType.Double:
                    baseTime *= 2.0;
                    break;
                case ChamferType.Micro:
                    baseTime *= 1.8;
                    break;
            }

            // 根据表面处理调整时间
            switch (Finish)
            {
                case SurfaceFinish.RoughGrind:
                    baseTime *= 1.0;
                    break;
                case SurfaceFinish.FineGrind:
                    baseTime *= 1.8;
                    break;
                case SurfaceFinish.Polish:
                    baseTime *= 3.0;
                    break;
                case SurfaceFinish.SuperPolish:
                    baseTime *= 5.0;
                    break;
            }

            // 根据加工难度调整时间
            baseTime *= (int)Difficulty * 0.8;

            ProcessingTime = Math.Round(baseTime, 1);
        }

        /// <summary>
        /// 从模板加载
        /// </summary>
        public void LoadFromTemplate(ChamferTemplate template)
        {
            if (template == null) return;

            Type = template.Type;
            Position = template.Position;
            Size = template.Size;
            Angle = template.Angle;
            Depth = template.Depth;
            Finish = template.Finish;
            Difficulty = template.Difficulty;
            ToleranceGrade = template.ToleranceGrade;
            Roughness = template.Roughness;
            RequiresProtection = template.RequiresProtection;
            ProcessingNotes = template.ProcessingNotes;
            InspectionRequirements = template.InspectionRequirements;
            IsStandard = template.IsStandard;
            TemplateId = template.Id;

            CalculateCost();
            CalculateProcessingTime();
        }

        /// <summary>
        /// 生成标注文本
        /// </summary>
        public string GetAnnotationText()
        {
            string text = "";

            switch (Type)
            {
                case ChamferType.CType:
                    // GB/T 1804: 45度倒角写作 "C0.5"，其他角度写作 "0.5×60°"
                    text = Angle == 45 ? $"C{Size}" : $"{Size}×{Angle}°";
                    break;
                case ChamferType.RType:
                    text = $"R{Size}";
                    break;
                case ChamferType.Special:
                    text = $"特殊倒角 {Size}mm";
                    break;
                case ChamferType.Double:
                    text = $"双倒角 {Size}mm";
                    break;
                case ChamferType.Micro:
                    text = $"微倒角 {Size}mm";
                    break;

                // GB/T 13323 4.3.7 边缘处理新增类型
                case ChamferType.SharpEdge:
                    // 功能性尖棱，标记"0"（图23）
                    text = "0";
                    break;
                case ChamferType.Bevel:
                    // 斜面标注，格式如 "45°±1°"（图24）
                    if (AngleTolerance > 0)
                        text = $"{Angle}°±{AngleTolerance}°";
                    else
                        text = $"{Angle}°";
                    // 添加中心偏说明
                    if (!string.IsNullOrEmpty(CenteringNote))
                        text += $" ({CenteringNote})";
                    break;
                case ChamferType.Protective:
                    // 保护性倒角，标注允许的最大最小宽度（如0.2-0.5）
                    if (MinSize > 0 && MaxSize > 0)
                        text = $"{MinSize}-{MaxSize}";
                    else
                        text = $"保护性倒角 {Size}mm";
                    break;
                case ChamferType.InternalTransition:
                    // 内部边渡形状，标注极限偏差（图27-29）
                    if (SizeTolerance > 0)
                        text = $"{Size}±{SizeTolerance}";
                    else
                        text = $"max {Size}";
                    break;
            }

            // 添加表面处理信息（仅对传统倒角类型）
            if (Type != ChamferType.SharpEdge && Type != ChamferType.Bevel)
            {
                switch (Finish)
                {
                    case SurfaceFinish.Polish:
                        text += " P";
                        break;
                    case SurfaceFinish.SuperPolish:
                        text += " SP";
                        break;
                }

                // 添加粗糙度信息
                if (Roughness < 0.8)
                {
                    text += $" Ra{Roughness}";
                }
            }

            return text;
        }

        /// <summary>
        /// 克隆倒角规格
        /// </summary>
        public ChamferSpecification Clone()
        {
            return new ChamferSpecification
            {
                Id = Guid.NewGuid(),
                Name = this.Name + "_副本",
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
                Remarks = this.Remarks,
                ComponentId = this.ComponentId,
                EdgeId = this.EdgeId,
                ProcessingCost = this.ProcessingCost,
                ProcessingTime = this.ProcessingTime,
                IsStandard = this.IsStandard,
                TemplateId = this.TemplateId,
                // GB/T 13323 4.3.7 扩展属性
                AngleTolerance = this.AngleTolerance,
                MinSize = this.MinSize,
                MaxSize = this.MaxSize,
                SizeTolerance = this.SizeTolerance,
                IsFunctionalEdge = this.IsFunctionalEdge,
                CenteringNote = this.CenteringNote,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };
        }

        #endregion
    }
}