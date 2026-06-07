using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace OtoCAD.OpticEntity
{
    /// <summary>
    /// 图框标准枚举
    /// </summary>
    public enum FrameStandardType
    {
        /// <summary>
        /// 中国国家标准
        /// </summary>
        [Description("GB (中国国家标准)")]
        GB = 0,

        /// <summary>
        /// 国际标准
        /// </summary>
        [Description("ISO (国际标准)")]
        ISO = 1,

        /// <summary>
        /// 美国标准
        /// </summary>
        [Description("ANSI (美国标准)")]
        ANSI = 2,

        /// <summary>
        /// 德国标准
        /// </summary>
        [Description("DIN (德国标准)")]
        DIN = 3,

        /// <summary>
        /// 日本标准
        /// </summary>
        [Description("JIS (日本标准)")]
        JIS = 4,

        /// <summary>
        /// 企业定制
        /// </summary>
        [Description("企业定制")]
        Enterprise = 98,

        /// <summary>
        /// 自定义
        /// </summary>
        [Description("自定义")]
        Custom = 99
    }

    /// <summary>
    /// 图纸尺寸枚举
    /// </summary>
    public enum PaperSize
    {
        /// <summary>
        /// A0尺寸
        /// </summary>
        [Description("A0 (841×1189mm)")]
        A0 = 0,

        /// <summary>
        /// A1尺寸
        /// </summary>
        [Description("A1 (594×841mm)")]
        A1 = 1,

        /// <summary>
        /// A2尺寸
        /// </summary>
        [Description("A2 (420×594mm)")]
        A2 = 2,

        /// <summary>
        /// A3尺寸
        /// </summary>
        [Description("A3 (297×420mm)")]
        A3 = 3,

        /// <summary>
        /// A4尺寸
        /// </summary>
        [Description("A4 (210×297mm)")]
        A4 = 4,

        /// <summary>
        /// 自定义尺寸
        /// </summary>
        [Description("自定义")]
        Custom = 99
    }

    /// <summary>
    /// 图纸方向枚举
    /// </summary>
    public enum PaperOrientation
    {
        /// <summary>
        /// 横向
        /// </summary>
        [Description("横向")]
        Landscape = 0,

        /// <summary>
        /// 纵向
        /// </summary>
        [Description("纵向")]
        Portrait = 1
    }

    /// <summary>
    /// 图框标准配置类
    /// </summary>
    public class FrameStandardConfig
    {
        /// <summary>
        /// 标准类型
        /// </summary>
        public FrameStandardType StandardType { get; set; }

        /// <summary>
        /// 标准名称
        /// </summary>
        public string StandardName { get; set; }

        /// <summary>
        /// 标准版本
        /// </summary>
        public string StandardVersion { get; set; }

        /// <summary>
        /// 标准说明
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 适用范围
        /// </summary>
        public string Scope { get; set; }

        /// <summary>
        /// 必填字段列表
        /// </summary>
        public List<string> RequiredFields { get; set; }

        /// <summary>
        /// 字段映射关系
        /// </summary>
        public Dictionary<string, string> FieldMapping { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public FrameStandardConfig()
        {
            RequiredFields = new List<string>();
            FieldMapping = new Dictionary<string, string>();
        }

        /// <summary>
        /// 获取标准配置
        /// </summary>
        public static FrameStandardConfig GetStandardConfig(FrameStandardType type)
        {
            var config = new FrameStandardConfig();
            config.StandardType = type;

            switch (type)
            {
                case FrameStandardType.GB:
                    config.StandardName = "GB/T 14689-2008";
                    config.StandardVersion = "2008";
                    config.Description = "技术制图 图纸幅面和格式";
                    config.Scope = "适用于国内项目，符合国家标准要求";
                    config.RequiredFields = new List<string> 
                    { 
                        "图纸名称", "图纸代号", "设计", "设计日期", 
                        "单位名称", "材料", "比例" 
                    };
                    break;

                case FrameStandardType.ISO:
                    config.StandardName = "ISO 7200:2004";
                    config.StandardVersion = "2004";
                    config.Description = "Technical drawings - Title blocks";
                    config.Scope = "适用于国际项目，符合ISO标准";
                    config.RequiredFields = new List<string> 
                    { 
                        "Title", "Drawing No.", "Drawn", "Date", 
                        "Company", "Material", "Scale" 
                    };
                    config.FieldMapping = new Dictionary<string, string>
                    {
                        { "图纸名称", "Title" },
                        { "图纸代号", "Drawing No." },
                        { "设计", "Drawn" },
                        { "校对", "Checked" },
                        { "审核", "Approved" },
                        { "批准", "Released" }
                    };
                    break;

                case FrameStandardType.ANSI:
                    config.StandardName = "ANSI Y14.1";
                    config.StandardVersion = "2020";
                    config.Description = "Decimal Inch Drawing Sheet Size and Format";
                    config.Scope = "适用于美国标准项目";
                    config.RequiredFields = new List<string> 
                    { 
                        "Title", "Drawing Number", "Drawn By", "Date", 
                        "Company Name", "Material", "Scale" 
                    };
                    break;

                default:
                    config.StandardName = "Custom";
                    config.StandardVersion = "1.0";
                    config.Description = "自定义标准";
                    config.Scope = "用户自定义";
                    break;
            }

            return config;
        }
    }

    /// <summary>
    /// 图纸尺寸信息类
    /// </summary>
    public class PaperSizeInfo
    {
        /// <summary>
        /// 尺寸类型
        /// </summary>
        public PaperSize Size { get; set; }

        /// <summary>
        /// 宽度(mm)
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// 高度(mm)
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 默认装订边(mm)
        /// </summary>
        public double DefaultBindingMargin { get; set; }

        /// <summary>
        /// 获取纸张尺寸信息
        /// </summary>
        public static PaperSizeInfo GetPaperSizeInfo(PaperSize size)
        {
            var info = new PaperSizeInfo { Size = size };

            switch (size)
            {
                case PaperSize.A0:
                    info.Width = 841;
                    info.Height = 1189;
                    info.DefaultBindingMargin = 25;
                    break;

                case PaperSize.A1:
                    info.Width = 594;
                    info.Height = 841;
                    info.DefaultBindingMargin = 25;
                    break;

                case PaperSize.A2:
                    info.Width = 420;
                    info.Height = 594;
                    info.DefaultBindingMargin = 25;
                    break;

                case PaperSize.A3:
                    info.Width = 297;
                    info.Height = 420;
                    info.DefaultBindingMargin = 25;
                    break;

                case PaperSize.A4:
                    info.Width = 210;
                    info.Height = 297;
                    info.DefaultBindingMargin = 20;
                    break;

                default:
                    info.Width = 297;
                    info.Height = 420;
                    info.DefaultBindingMargin = 25;
                    break;
            }

            return info;
        }

        /// <summary>
        /// 根据方向调整尺寸
        /// </summary>
        public void AdjustForOrientation(PaperOrientation orientation)
        {
            if (orientation == PaperOrientation.Landscape && Width < Height)
            {
                // 切换到横向
                double temp = Width;
                Width = Height;
                Height = temp;
            }
            else if (orientation == PaperOrientation.Portrait && Width > Height)
            {
                // 切换到纵向
                double temp = Width;
                Width = Height;
                Height = temp;
            }
        }

        /// <summary>
        /// 计算绘图区域
        /// </summary>
        public void CalculateDrawingArea(double margin, out double drawingWidth, out double drawingHeight)
        {
            drawingWidth = Width - 2 * margin;
            drawingHeight = Height - 2 * margin;
        }
    }

    /// <summary>
    /// 更改记录类
    /// </summary>
    public class ChangeRecord
    {
        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 更改日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 更改者
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// 更改描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 批准人
        /// </summary>
        public string ApprovedBy { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ChangeRecord()
        {
            Date = DateTime.Now;
            Version = "V1.0";
        }

        /// <summary>
        /// 格式化显示
        /// </summary>
        public override string ToString()
        {
            return $"{Version} {Description} {Date:yyyy-MM-dd}";
        }
    }

    /// <summary>
    /// 会签部门信息
    /// </summary>
    public class SignatureDepartment
    {
        /// <summary>
        /// 部门名称
        /// </summary>
        public string DepartmentName { get; set; }

        /// <summary>
        /// 签署人
        /// </summary>
        public string Signer { get; set; }

        /// <summary>
        /// 签署日期
        /// </summary>
        public DateTime? SignDate { get; set; }

        /// <summary>
        /// 签署状态
        /// </summary>
        public SignatureStatus Status { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Comments { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public SignatureDepartment(string departmentName)
        {
            DepartmentName = departmentName;
            Status = SignatureStatus.Pending;
        }
    }

    /// <summary>
    /// 签署状态枚举
    /// </summary>
    public enum SignatureStatus
    {
        /// <summary>
        /// 待签署
        /// </summary>
        [Description("待签署")]
        Pending = 0,

        /// <summary>
        /// 已签署
        /// </summary>
        [Description("已签署")]
        Signed = 1,

        /// <summary>
        /// 已拒绝
        /// </summary>
        [Description("已拒绝")]
        Rejected = 2,

        /// <summary>
        /// 处理中
        /// </summary>
        [Description("处理中")]
        InProgress = 3
    }
}