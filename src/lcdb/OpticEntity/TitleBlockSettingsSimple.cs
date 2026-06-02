using System;
using System.ComponentModel;

namespace OtoCAD.OpticEntity
{
    /// <summary>
    /// 标题栏设置 - 精简版本，只保留必要属性
    /// </summary>
    public class TitleBlockSettingsSimple
    {
        #region 核心信息（必须）

        /// <summary>
        /// 项目名称
        /// </summary>
        [Category("基本信息")]
        [DisplayName("项目名称")]
        public string ProjectName { get; set; } = "光学零件图纸";

        /// <summary>
        /// 图纸代号
        /// </summary>
        [Category("基本信息")]
        [DisplayName("图纸代号")]
        public string DrawingNumber { get; set; } = "";

        /// <summary>
        /// 页码
        /// </summary>
        [Category("基本信息")]
        [DisplayName("页码")]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// 总页数
        /// </summary>
        [Category("基本信息")]
        [DisplayName("总页数")]
        public int TotalPages { get; set; } = 1;

        #endregion

        #region 签署信息（常用）

        /// <summary>
        /// 设计者
        /// </summary>
        [Category("签署信息")]
        [DisplayName("设计")]
        public string Designer { get; set; } = "";

        /// <summary>
        /// 日期
        /// </summary>
        [Category("签署信息")]
        [DisplayName("日期")]
        public string Date { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");

        /// <summary>
        /// 审核者
        /// </summary>
        [Category("签署信息")]
        [DisplayName("审核")]
        public string Approver { get; set; } = "";

        #endregion

        #region 单位信息（可选）

        /// <summary>
        /// 单位名称
        /// </summary>
        [Category("单位信息")]
        [DisplayName("单位")]
        public string CompanyName { get; set; } = "";

        /// <summary>
        /// 比例
        /// </summary>
        [Category("单位信息")]
        [DisplayName("比例")]
        public string Scale { get; set; } = "1:1";

        #endregion

        #region 方法

        /// <summary>
        /// 克隆
        /// </summary>
        public TitleBlockSettingsSimple Clone()
        {
            return (TitleBlockSettingsSimple)this.MemberwiseClone();
        }

        /// <summary>
        /// 从旧版本迁移
        /// </summary>
        public static TitleBlockSettingsSimple FromOldVersion(TitleBlockSettings old)
        {
            if (old == null)
                return new TitleBlockSettingsSimple();

            return new TitleBlockSettingsSimple
            {
                ProjectName = old.ProjectName,
                DrawingNumber = old.DrawingNumber,
                PageNumber = old.PageNumber,
                TotalPages = old.TotalPages,
                Designer = old.Designer,
                Date = old.DesignDate,
                Approver = old.Approver,
                CompanyName = old.CompanyName,
                Scale = old.Scale
            };
        }

        #endregion
    }
}