using System;
using System.Collections.Generic;
using System.Linq;

namespace lcdb
{
    /// <summary>
    /// 文本样式管理器
    /// </summary>
    public static class TextStyleManager
    {
        #region Standard Style Templates

        /// <summary>
        /// 获取ISO标准样式模板
        /// </summary>
        /// <returns>ISO标准样式字典</returns>
        public static Dictionary<string, TextStyle> GetISOStandardTemplates()
        {
            return new Dictionary<string, TextStyle>
            {
                // 基础ISO样式
                { "ISO-Standard", CreateISOStandard() },
                { "ISO-Title", CreateISOTitle() },
                { "ISO-Subtitle", CreateISOSubtitle() },
                { "ISO-Dimension", CreateISODimension() },
                { "ISO-Annotation", CreateISOAnnotation() },
                { "ISO-Section", CreateISOSection() },
                { "ISO-Detail", CreateISODetail() },
                
                // ISO技术图纸样式
                { "ISO-Drawing-Number", CreateISODrawingNumber() },
                { "ISO-Revision", CreateISORevision() },
                { "ISO-Notes", CreateISONotes() }
            };
        }

        /// <summary>
        /// 获取GB国标样式模板
        /// </summary>
        /// <returns>GB国标样式字典</returns>
        public static Dictionary<string, TextStyle> GetGBStandardTemplates()
        {
            return new Dictionary<string, TextStyle>
            {
                // 基础GB样式
                { "GB-Standard", CreateGBStandard() },
                { "GB-Title", CreateGBTitle() },
                { "GB-Subtitle", CreateGBSubtitle() },
                { "GB-Dimension", CreateGBDimension() },
                { "GB-Annotation", CreateGBAnnotation() },
                { "GB-Section", CreateGBSection() },
                { "GB-Detail", CreateGBDetail() },
                
                // GB技术图纸样式
                { "GB-Drawing-Number", CreateGBDrawingNumber() },
                { "GB-Revision", CreateGBRevision() },
                { "GB-Notes", CreateGBNotes() }
            };
        }

        #endregion

        #region ISO Style Creators

        public static TextStyle CreateISOStandard()
        {
            return new TextStyle("ISO-Standard", "Arial", FontStyle.Regular)
            {
                Height = 2.5,
                WidthFactor = 1.0,
                ObliqueAngle = 0.0
            };
        }

        public static TextStyle CreateISOTitle()
        {
            return new TextStyle("ISO-Title", "Arial", FontStyle.Bold)
            {
                Height = 7.0,
                WidthFactor = 1.0,
                ObliqueAngle = 0.0
            };
        }

        public static TextStyle CreateISOSubtitle()
        {
            return new TextStyle("ISO-Subtitle", "Arial", FontStyle.Bold)
            {
                Height = 5.0,
                WidthFactor = 1.0,
                ObliqueAngle = 0.0
            };
        }

        public static TextStyle CreateISODimension()
        {
            return new TextStyle("ISO-Dimension", "Arial", FontStyle.Regular)
            {
                Height = 2.0,
                WidthFactor = 1.0,
                ObliqueAngle = 0.0
            };
        }

        public static TextStyle CreateISOAnnotation()
        {
            return new TextStyle("ISO-Annotation", "Arial", FontStyle.Italic)
            {
                Height = 1.8,
                WidthFactor = 1.0,
                ObliqueAngle = 0.0
            };
        }

        public static TextStyle CreateISOSection()
        {
            return new TextStyle("ISO-Section", "Arial", FontStyle.Bold)
            {
                Height = 3.5,
                WidthFactor = 1.0,
                ObliqueAngle = 0.0
            };
        }

        public static TextStyle CreateISODetail()
        {
            return new TextStyle("ISO-Detail", "Arial", FontStyle.Regular)
            {
                Height = 2.5,
                WidthFactor = 1.0,
                ObliqueAngle = 0.0
            };
        }

        public static TextStyle CreateISODrawingNumber()
        {
            return new TextStyle("ISO-Drawing-Number", "Arial", FontStyle.Bold)
            {
                Height = 3.0,
                WidthFactor = 1.0,
                ObliqueAngle = 0.0
            };
        }

        public static TextStyle CreateISORevision()
        {
            return new TextStyle("ISO-Revision", "Arial", FontStyle.Regular)
            {
                Height = 2.0,
                WidthFactor = 1.0,
                ObliqueAngle = 0.0
            };
        }

        public static TextStyle CreateISONotes()
        {
            return new TextStyle("ISO-Notes", "Arial", FontStyle.Regular)
            {
                Height = 1.5,
                WidthFactor = 1.0,
                ObliqueAngle = 0.0
            };
        }

        #endregion

        #region GB Style Creators

        public static TextStyle CreateGBStandard()
        {
            return new TextStyle("GB-Standard", "SimSun", FontStyle.Regular)
            {
                Height = 3.5,
                WidthFactor = 0.8,
                ObliqueAngle = 0.0
            };
        }

        public static TextStyle CreateGBTitle()
        {
            return new TextStyle("GB-Title", "SimHei", FontStyle.Bold)
            {
                Height = 10.0,
                WidthFactor = 0.8,
                ObliqueAngle = 0.0
            };
        }

        public static TextStyle CreateGBSubtitle()
        {
            return new TextStyle("GB-Subtitle", "SimHei", FontStyle.Regular)
            {
                Height = 7.0,
                WidthFactor = 0.8,
                ObliqueAngle = 0.0
            };
        }

        public static TextStyle CreateGBDimension()
        {
            return new TextStyle("GB-Dimension", "SimSun", FontStyle.Regular)
            {
                Height = 2.5,
                WidthFactor = 0.8,
                ObliqueAngle = 0.0
            };
        }

        public static TextStyle CreateGBAnnotation()
        {
            return new TextStyle("GB-Annotation", "KaiTi", FontStyle.Regular)
            {
                Height = 2.0,
                WidthFactor = 0.8,
                ObliqueAngle = 15.0
            };
        }

        public static TextStyle CreateGBSection()
        {
            return new TextStyle("GB-Section", "SimHei", FontStyle.Bold)
            {
                Height = 5.0,
                WidthFactor = 0.8,
                ObliqueAngle = 0.0
            };
        }

        public static TextStyle CreateGBDetail()
        {
            return new TextStyle("GB-Detail", "SimSun", FontStyle.Regular)
            {
                Height = 3.5,
                WidthFactor = 0.8,
                ObliqueAngle = 0.0
            };
        }

        public static TextStyle CreateGBDrawingNumber()
        {
            return new TextStyle("GB-Drawing-Number", "SimHei", FontStyle.Bold)
            {
                Height = 4.0,
                WidthFactor = 0.8,
                ObliqueAngle = 0.0
            };
        }

        public static TextStyle CreateGBRevision()
        {
            return new TextStyle("GB-Revision", "SimSun", FontStyle.Regular)
            {
                Height = 2.5,
                WidthFactor = 0.8,
                ObliqueAngle = 0.0
            };
        }

        public static TextStyle CreateGBNotes()
        {
            return new TextStyle("GB-Notes", "SimSun", FontStyle.Regular)
            {
                Height = 2.0,
                WidthFactor = 0.8,
                ObliqueAngle = 0.0
            };
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// 应用标准样式集到文本样式表
        /// </summary>
        /// <param name="textStyleTable">文本样式表</param>
        /// <param name="standardType">标准类型</param>
        /// <param name="overwriteExisting">是否覆盖已存在的样式</param>
        public static void ApplyStandardStyles(TextStyleTable textStyleTable, StandardType standardType, bool overwriteExisting = false)
        {
            if (textStyleTable == null)
                throw new ArgumentNullException(nameof(textStyleTable));

            Dictionary<string, TextStyle> templates = null;

            switch (standardType)
            {
                case StandardType.ISO:
                    templates = GetISOStandardTemplates();
                    break;
                case StandardType.GB:
                    templates = GetGBStandardTemplates();
                    break;
                case StandardType.All:
                    templates = GetISOStandardTemplates();
                    foreach (var gbTemplate in GetGBStandardTemplates())
                    {
                        templates[gbTemplate.Key] = gbTemplate.Value;
                    }
                    break;
                default:
                    throw new ArgumentException("未知的标准类型", nameof(standardType));
            }

            foreach (var template in templates)
            {
                if (!textStyleTable.Has(template.Key) || overwriteExisting)
                {
                    if (overwriteExisting && textStyleTable.Has(template.Key))
                    {
                        textStyleTable.Remove(template.Key);
                    }
                    textStyleTable.Add((TextStyle)template.Value.Clone());
                }
            }
        }

        /// <summary>
        /// 验证文本样式设置
        /// </summary>
        /// <param name="textStyle">文本样式</param>
        /// <returns>验证结果</returns>
        public static TextStyleValidationResult ValidateTextStyle(TextStyle textStyle)
        {
            var result = new TextStyleValidationResult();

            if (textStyle == null)
            {
                result.IsValid = false;
                result.Errors.Add("文本样式不能为空");
                return result;
            }

            if (string.IsNullOrEmpty(textStyle.name))
            {
                result.IsValid = false;
                result.Errors.Add("文本样式名称不能为空");
            }

            if (string.IsNullOrEmpty(textStyle.FontFile) && string.IsNullOrEmpty(textStyle.FontFamilyName))
            {
                result.IsValid = false;
                result.Errors.Add("必须指定字体文件或字体族名称");
            }

            if (textStyle.Height < 0)
            {
                result.IsValid = false;
                result.Errors.Add("文本高度不能为负数");
            }

            if (textStyle.WidthFactor < 0.01 || textStyle.WidthFactor > 100.0)
            {
                result.IsValid = false;
                result.Errors.Add("宽度因子必须在0.01到100.0之间");
            }

            if (textStyle.ObliqueAngle < -85.0 || textStyle.ObliqueAngle > 85.0)
            {
                result.IsValid = false;
                result.Errors.Add("倾斜角度必须在-85到85度之间");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// 创建样式变体
        /// </summary>
        /// <param name="baseStyle">基础样式</param>
        /// <param name="variantName">变体名称</param>
        /// <param name="modifications">修改操作</param>
        /// <returns>新的样式变体</returns>
        public static TextStyle CreateStyleVariant(TextStyle baseStyle, string variantName, Action<TextStyle> modifications)
        {
            if (baseStyle == null)
                throw new ArgumentNullException(nameof(baseStyle));

            if (string.IsNullOrEmpty(variantName))
                throw new ArgumentNullException(nameof(variantName));

            var variant = (TextStyle)baseStyle.Clone();
            variant.name = variantName;
            modifications?.Invoke(variant);

            return variant;
        }

        #endregion
    }

    /// <summary>
    /// 文本样式验证结果
    /// </summary>
    public class TextStyleValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }
}