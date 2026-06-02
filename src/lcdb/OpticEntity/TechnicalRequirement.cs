using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace OtoCAD.OpticEntity
{
    /// <summary>
    /// 技术要求类
    /// </summary>
    public class TechnicalRequirement
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 序号
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 要求内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 要求类别
        /// </summary>
        public RequirementCategory Category { get; set; }

        /// <summary>
        /// 是否必需
        /// </summary>
        public bool IsMandatory { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public TechnicalRequirement()
        {
            Id = Guid.NewGuid().ToString();
            Index = 1;
            Content = "";
            Category = RequirementCategory.General;
            IsMandatory = false;
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public TechnicalRequirement(int index, string content, RequirementCategory category = RequirementCategory.General)
        {
            Id = Guid.NewGuid().ToString();
            Index = index;
            Content = content;
            Category = category;
            IsMandatory = false;
        }
    }

    /// <summary>
    /// 技术要求类别枚举
    /// </summary>
    public enum RequirementCategory
    {
        /// <summary>
        /// 通用要求
        /// </summary>
        [Description("通用要求")]
        General = 0,

        /// <summary>
        /// 公差要求
        /// </summary>
        [Description("公差要求")]
        Tolerance = 1,

        /// <summary>
        /// 光学要求
        /// </summary>
        [Description("光学要求")]
        Optical = 2,

        /// <summary>
        /// 机械要求
        /// </summary>
        [Description("机械要求")]
        Mechanical = 3,

        /// <summary>
        /// 材料要求
        /// </summary>
        [Description("材料要求")]
        Material = 4,

        /// <summary>
        /// 表面处理
        /// </summary>
        [Description("表面处理")]
        Surface = 5,

        /// <summary>
        /// 检验要求
        /// </summary>
        [Description("检验要求")]
        Inspection = 6,

        /// <summary>
        /// 包装要求
        /// </summary>
        [Description("包装要求")]
        Packaging = 7,

        /// <summary>
        /// 自定义要求
        /// </summary>
        [Description("自定义要求")]
        Custom = 99
    }

    /// <summary>
    /// 技术要求管理器
    /// </summary>
    public class TechnicalRequirementManager
    {
        private List<TechnicalRequirement> requirements;

        /// <summary>
        /// 获取所有技术要求
        /// </summary>
        public List<TechnicalRequirement> Requirements => requirements;

        /// <summary>
        /// 构造函数
        /// </summary>
        public TechnicalRequirementManager()
        {
            requirements = new List<TechnicalRequirement>();
        }

        /// <summary>
        /// 添加技术要求
        /// </summary>
        public void AddRequirement(string content, RequirementCategory category = RequirementCategory.General)
        {
            int nextIndex = requirements.Count > 0 ? requirements.Max(r => r.Index) + 1 : 1;
            var requirement = new TechnicalRequirement(nextIndex, content, category);
            requirements.Add(requirement);
        }

        /// <summary>
        /// 删除技术要求
        /// </summary>
        public bool RemoveRequirement(string id)
        {
            var requirement = requirements.FirstOrDefault(r => r.Id == id);
            if (requirement != null)
            {
                requirements.Remove(requirement);
                ReindexRequirements();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 更新技术要求
        /// </summary>
        public bool UpdateRequirement(string id, string newContent)
        {
            var requirement = requirements.FirstOrDefault(r => r.Id == id);
            if (requirement != null)
            {
                requirement.Content = newContent;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 移动技术要求位置
        /// </summary>
        public void MoveRequirement(string id, int newIndex)
        {
            var requirement = requirements.FirstOrDefault(r => r.Id == id);
            if (requirement != null)
            {
                requirements.Remove(requirement);
                requirements.Insert(Math.Min(newIndex - 1, requirements.Count), requirement);
                ReindexRequirements();
            }
        }

        /// <summary>
        /// 重新编号
        /// </summary>
        private void ReindexRequirements()
        {
            for (int i = 0; i < requirements.Count; i++)
            {
                requirements[i].Index = i + 1;
            }
        }

        /// <summary>
        /// 清空所有要求
        /// </summary>
        public void Clear()
        {
            requirements.Clear();
        }

        /// <summary>
        /// 加载预设模板
        /// </summary>
        public void LoadTemplate(RequirementTemplate template)
        {
            Clear();
            foreach (var content in GetTemplateRequirements(template))
            {
                AddRequirement(content.Item1, content.Item2);
            }
        }

        /// <summary>
        /// 获取模板要求内容
        /// </summary>
        private List<Tuple<string, RequirementCategory>> GetTemplateRequirements(RequirementTemplate template)
        {
            var list = new List<Tuple<string, RequirementCategory>>();

            switch (template)
            {
                case RequirementTemplate.OpticalStandard:
                    list.Add(Tuple.Create("未注公差按GB/T 1804-m", RequirementCategory.Tolerance));
                    list.Add(Tuple.Create("未注倒角0.2×45°", RequirementCategory.Mechanical));
                    list.Add(Tuple.Create("光学表面不得有划痕、麻点、破边", RequirementCategory.Optical));
                    list.Add(Tuple.Create("非工作面毛刺≤0.05mm", RequirementCategory.Surface));
                    list.Add(Tuple.Create("零件加工后需超声波清洗", RequirementCategory.Surface));
                    list.Add(Tuple.Create("包装时光学表面需保护", RequirementCategory.Packaging));
                    break;

                case RequirementTemplate.MechanicalStandard:
                    list.Add(Tuple.Create("未注公差按GB/T 1804-m", RequirementCategory.Tolerance));
                    list.Add(Tuple.Create("未注倒角0.5×45°", RequirementCategory.Mechanical));
                    list.Add(Tuple.Create("去除毛刺和锐边", RequirementCategory.Surface));
                    list.Add(Tuple.Create("表面粗糙度Ra3.2", RequirementCategory.Surface));
                    list.Add(Tuple.Create("清洁后防锈包装", RequirementCategory.Packaging));
                    break;

                case RequirementTemplate.HighPrecision:
                    list.Add(Tuple.Create("未注公差按GB/T 1804-f", RequirementCategory.Tolerance));
                    list.Add(Tuple.Create("未注倒角0.1×45°", RequirementCategory.Mechanical));
                    list.Add(Tuple.Create("光学表面面形PV≤λ/4", RequirementCategory.Optical));
                    list.Add(Tuple.Create("表面粗糙度Ra0.8", RequirementCategory.Surface));
                    list.Add(Tuple.Create("恒温(20±0.5)℃环境下测量", RequirementCategory.Inspection));
                    list.Add(Tuple.Create("使用三坐标测量机检验", RequirementCategory.Inspection));
                    list.Add(Tuple.Create("净化间内包装", RequirementCategory.Packaging));
                    break;

                default:
                    list.Add(Tuple.Create("未注公差按GB/T 1804-m", RequirementCategory.Tolerance));
                    break;
            }

            return list;
        }

        /// <summary>
        /// 获取按类别分组的要求
        /// </summary>
        public Dictionary<RequirementCategory, List<TechnicalRequirement>> GetRequirementsByCategory()
        {
            return requirements.GroupBy(r => r.Category)
                              .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// 导出为文本列表
        /// </summary>
        public List<string> ExportToTextList()
        {
            return requirements.OrderBy(r => r.Index)
                              .Select(r => $"{r.Index}. {r.Content}")
                              .ToList();
        }
    }

    /// <summary>
    /// 技术要求模板枚举
    /// </summary>
    public enum RequirementTemplate
    {
        /// <summary>
        /// 光学零件标准要求
        /// </summary>
        [Description("光学零件标准要求")]
        OpticalStandard = 0,

        /// <summary>
        /// 机械零件标准要求
        /// </summary>
        [Description("机械零件标准要求")]
        MechanicalStandard = 1,

        /// <summary>
        /// 高精度要求
        /// </summary>
        [Description("高精度要求")]
        HighPrecision = 2,

        /// <summary>
        /// 自定义要求
        /// </summary>
        [Description("自定义要求")]
        Custom = 99
    }
}