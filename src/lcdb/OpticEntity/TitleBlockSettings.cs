using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace OtoCAD.OpticEntity
{
    /// <summary>
    /// 标题栏设置类
    /// </summary>
    public class TitleBlockSettings
    {
        #region 基本信息

        /// <summary>
        /// 项目名称
        /// </summary>
        [Category("基本信息")]
        [DisplayName("项目名称")]
        [Description("项目或图纸名称")]
        public string ProjectName { get; set; } = "光学零件图纸";

        /// <summary>
        /// 图纸代号
        /// </summary>
        [Category("基本信息")]
        [DisplayName("图纸代号")]
        [Description("图纸唯一标识代号")]
        public string DrawingNumber { get; set; } = "";

        /// <summary>
        /// 版本号
        /// </summary>
        [Category("基本信息")]
        [DisplayName("版本号")]
        [Description("图纸版本号")]
        public string Version { get; set; } = "V1.0";

        /// <summary>
        /// 页码
        /// </summary>
        [Category("基本信息")]
        [DisplayName("页码")]
        [Description("当前页码")]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// 总页数
        /// </summary>
        [Category("基本信息")]
        [DisplayName("总页数")]
        [Description("图纸总页数")]
        public int TotalPages { get; set; } = 1;

        #endregion

        #region 签署信息

        /// <summary>
        /// 设计者
        /// </summary>
        [Category("签署信息")]
        [DisplayName("设计者")]
        [Description("设计人员姓名")]
        public string Designer { get; set; } = "";

        /// <summary>
        /// 设计日期
        /// </summary>
        [Category("签署信息")]
        [DisplayName("设计日期")]
        [Description("设计完成日期")]
        public string DesignDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");

        /// <summary>
        /// 校对者
        /// </summary>
        [Category("签署信息")]
        [DisplayName("校对者")]
        [Description("校对人员姓名")]
        public string Checker { get; set; } = "";

        /// <summary>
        /// 校对日期
        /// </summary>
        [Category("签署信息")]
        [DisplayName("校对日期")]
        [Description("校对完成日期")]
        public string CheckDate { get; set; } = "";

        /// <summary>
        /// 审核者
        /// </summary>
        [Category("签署信息")]
        [DisplayName("审核者")]
        [Description("审核人员姓名")]
        public string Approver { get; set; } = "";

        /// <summary>
        /// 审核日期
        /// </summary>
        [Category("签署信息")]
        [DisplayName("审核日期")]
        [Description("审核完成日期")]
        public string ApproveDate { get; set; } = "";

        /// <summary>
        /// 批准者
        /// </summary>
        [Category("签署信息")]
        [DisplayName("批准者")]
        [Description("批准人员姓名")]
        public string Releaser { get; set; } = "";

        /// <summary>
        /// 批准日期
        /// </summary>
        [Category("签署信息")]
        [DisplayName("批准日期")]
        [Description("批准日期")]
        public string ReleaseDate { get; set; } = "";

        /// <summary>
        /// 标准化
        /// </summary>
        [Category("签署信息")]
        [DisplayName("标准化")]
        [Description("标准化人员姓名")]
        public string Standardizer { get; set; } = "";

        /// <summary>
        /// 标准化日期
        /// </summary>
        [Category("签署信息")]
        [DisplayName("标准化日期")]
        [Description("标准化日期")]
        public string StandardizeDate { get; set; } = "";

        #endregion

        #region 项目信息

        /// <summary>
        /// 单位名称
        /// </summary>
        [Category("项目信息")]
        [DisplayName("单位名称")]
        [Description("公司或单位名称")]
        public string CompanyName { get; set; } = "";

        /// <summary>
        /// 部门名称
        /// </summary>
        [Category("项目信息")]
        [DisplayName("部门名称")]
        [Description("所属部门")]
        public string Department { get; set; } = "";

        /// <summary>
        /// 项目号
        /// </summary>
        [Category("项目信息")]
        [DisplayName("项目号")]
        [Description("项目编号")]
        public string ProjectNumber { get; set; } = "";

        /// <summary>
        /// 客户名称
        /// </summary>
        [Category("项目信息")]
        [DisplayName("客户名称")]
        [Description("客户或委托方名称")]
        public string CustomerName { get; set; } = "";

        /// <summary>
        /// 合同号
        /// </summary>
        [Category("项目信息")]
        [DisplayName("合同号")]
        [Description("合同编号")]
        public string ContractNumber { get; set; } = "";

        /// <summary>
        /// 公司Logo路径
        /// </summary>
        [Category("项目信息")]
        [DisplayName("公司Logo")]
        [Description("公司Logo图片路径")]
        public string CompanyLogo { get; set; } = "";

        /// <summary>
        /// 地址
        /// </summary>
        [Category("项目信息")]
        [DisplayName("地址")]
        [Description("公司地址")]
        public string Address { get; set; } = "";

        /// <summary>
        /// 电话
        /// </summary>
        [Category("项目信息")]
        [DisplayName("电话")]
        [Description("联系电话")]
        public string Phone { get; set; } = "";

        /// <summary>
        /// 网址
        /// </summary>
        [Category("项目信息")]
        [DisplayName("网址")]
        [Description("公司网址")]
        public string Website { get; set; } = "";

        #endregion

        #region 技术信息

        /// <summary>
        /// 材料
        /// </summary>
        [Category("技术信息")]
        [DisplayName("材料")]
        [Description("零件材料")]
        public string Material { get; set; } = "";

        /// <summary>
        /// 重量
        /// </summary>
        [Category("技术信息")]
        [DisplayName("重量")]
        [Description("零件重量(g)")]
        public double Weight { get; set; } = 0;

        /// <summary>
        /// 数量
        /// </summary>
        [Category("技术信息")]
        [DisplayName("数量")]
        [Description("零件数量")]
        public int Quantity { get; set; } = 1;

        /// <summary>
        /// 比例
        /// </summary>
        [Category("技术信息")]
        [DisplayName("比例")]
        [Description("图纸比例")]
        public string Scale { get; set; } = "1:1";

        /// <summary>
        /// 公差等级
        /// </summary>
        [Category("技术信息")]
        [DisplayName("公差等级")]
        [Description("零件公差等级")]
        public string ToleranceGrade { get; set; } = "";

        /// <summary>
        /// 表面处理
        /// </summary>
        [Category("技术信息")]
        [DisplayName("表面处理")]
        [Description("表面处理方式")]
        public string SurfaceTreatment { get; set; } = "";

        /// <summary>
        /// 热处理
        /// </summary>
        [Category("技术信息")]
        [DisplayName("热处理")]
        [Description("热处理方式")]
        public string HeatTreatment { get; set; } = "";

        /// <summary>
        /// 单位
        /// </summary>
        [Category("技术信息")]
        [DisplayName("单位")]
        [Description("尺寸单位")]
        public string Unit { get; set; } = "mm";

        /// <summary>
        /// 图纸状态
        /// </summary>
        [Category("技术信息")]
        [DisplayName("图纸状态")]
        [Description("图纸当前状态")]
        public DrawingStatus Status { get; set; } = DrawingStatus.Draft;

        /// <summary>
        /// 修订号
        /// </summary>
        [Category("技术信息")]
        [DisplayName("修订号")]
        [Description("当前修订号")]
        public string Revision { get; set; } = "A";

        #endregion

        #region 布局设置

        /// <summary>
        /// 标题块宽度
        /// </summary>
        [Category("布局设置")]
        [DisplayName("宽度")]
        [Description("标题块宽度(mm)")]
        public double Width { get; set; } = 180;

        /// <summary>
        /// 标题块高度
        /// </summary>
        [Category("布局设置")]
        [DisplayName("高度")]
        [Description("标题块高度(mm)")]
        public double Height { get; set; } = 56;

        /// <summary>
        /// 日期格式
        /// </summary>
        [Category("布局设置")]
        [DisplayName("日期格式")]
        [Description("日期显示格式")]
        public string DateFormat { get; set; } = "yyyy-MM-dd";

        /// <summary>
        /// 字体大小
        /// </summary>
        [Category("布局设置")]
        [DisplayName("字体大小")]
        [Description("标题块默认字体大小")]
        public double FontSize { get; set; } = 3.5;

        /// <summary>
        /// 字体名称
        /// </summary>
        [Category("布局设置")]
        [DisplayName("字体名称")]
        [Description("标题块字体名称")]
        public string FontName { get; set; } = "宋体";

        /// <summary>
        /// 是否显示边框
        /// </summary>
        [Category("布局设置")]
        [DisplayName("显示边框")]
        [Description("是否显示标题块边框")]
        public bool ShowBorder { get; set; } = true;

        /// <summary>
        /// 边框宽度
        /// </summary>
        [Category("布局设置")]
        [DisplayName("边框宽度")]
        [Description("标题块边框宽度(mm)")]
        public double BorderWidth { get; set; } = 0.5;

        #endregion

        #region 自定义字段

        /// <summary>
        /// 自定义字段字典
        /// </summary>
        [Browsable(false)]
        public Dictionary<string, string> CustomFields { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 修订历史
        /// </summary>
        [Browsable(false)]
        public List<RevisionRecord> RevisionHistory { get; set; } = new List<RevisionRecord>();

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public TitleBlockSettings()
        {
            InitializeDefaults();
        }

        /// <summary>
        /// 初始化默认值
        /// </summary>
        private void InitializeDefaults()
        {
            ProjectName = "光学零件图纸";
            Version = "V1.0";
            DesignDate = DateTime.Now.ToString("yyyy-MM-dd");
            Width = 180;
            Height = 56;
            FontSize = 3.5;
            Quantity = 1;
            Scale = "1:1";
            PageNumber = 1;
            TotalPages = 1;
            DateFormat = "yyyy-MM-dd";
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 验证必填项
        /// </summary>
        /// <returns>验证是否通过</returns>
        public bool ValidateRequired()
        {
            if (string.IsNullOrWhiteSpace(ProjectName))
                return false;

            if (string.IsNullOrWhiteSpace(Designer))
                return false;

            if (string.IsNullOrWhiteSpace(DesignDate))
                return false;

            return true;
        }

        /// <summary>
        /// 获取格式化的页码文本
        /// </summary>
        /// <returns>页码文本</returns>
        public string GetPageText()
        {
            return string.Format("第 {0} 页，共 {1} 页", PageNumber, TotalPages);
        }

        /// <summary>
        /// 克隆标题块设置
        /// </summary>
        /// <returns>克隆的标题块设置</returns>
        public TitleBlockSettings Clone()
        {
            var clone = (TitleBlockSettings)this.MemberwiseClone();
            clone.CustomFields = new Dictionary<string, string>(this.CustomFields);
            clone.RevisionHistory = new List<RevisionRecord>(this.RevisionHistory.Select(r => r.Clone()));
            return clone;
        }

        /// <summary>
        /// 添加自定义字段
        /// </summary>
        public void AddCustomField(string key, string value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                CustomFields[key] = value;
            }
        }

        /// <summary>
        /// 获取自定义字段
        /// </summary>
        public string GetCustomField(string key)
        {
            return CustomFields.ContainsKey(key) ? CustomFields[key] : string.Empty;
        }

        /// <summary>
        /// 添加修订记录
        /// </summary>
        public void AddRevision(string description, string author)
        {
            var revision = new RevisionRecord
            {
                RevisionNo = GenerateNextRevision(),
                Date = DateTime.Now,
                Description = description,
                Author = author
            };
            RevisionHistory.Add(revision);
            Revision = revision.RevisionNo;
        }

        /// <summary>
        /// 生成下一个修订号
        /// </summary>
        private string GenerateNextRevision()
        {
            if (string.IsNullOrEmpty(Revision))
                return "A";

            char lastChar = Revision[Revision.Length - 1];
            if (char.IsLetter(lastChar))
            {
                if (lastChar == 'Z')
                    return Revision + "A";
                else
                    return Revision.Substring(0, Revision.Length - 1) + (char)(lastChar + 1);
            }
            else
            {
                return Revision + "A";
            }
        }

        #endregion
    }

    /// <summary>
    /// 图纸状态枚举
    /// </summary>
    public enum DrawingStatus
    {
        /// <summary>
        /// 草稿
        /// </summary>
        [Description("草稿")]
        Draft,

        /// <summary>
        /// 审核中
        /// </summary>
        [Description("审核中")]
        UnderReview,

        /// <summary>
        /// 已批准
        /// </summary>
        [Description("已批准")]
        Approved,

        /// <summary>
        /// 已发布
        /// </summary>
        [Description("已发布")]
        Released,

        /// <summary>
        /// 已归档
        /// </summary>
        [Description("已归档")]
        Archived,

        /// <summary>
        /// 已作废
        /// </summary>
        [Description("已作废")]
        Obsolete
    }

    /// <summary>
    /// 修订记录
    /// </summary>
    [Serializable]
    public class RevisionRecord
    {
        /// <summary>
        /// 修订号
        /// </summary>
        public string RevisionNo { get; set; }

        /// <summary>
        /// 修订日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 修订描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 修订人
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// 批准人
        /// </summary>
        public string ApprovedBy { get; set; }

        /// <summary>
        /// 克隆修订记录
        /// </summary>
        public RevisionRecord Clone()
        {
            return (RevisionRecord)this.MemberwiseClone();
        }
    }
}