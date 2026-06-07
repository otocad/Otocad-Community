using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using LitMath;
using lcdb;
using lcdb.Annotation;
using OtoCAD.OpticEntity;

namespace OtoCAD.OpticEntity.FeatureControl
{
    /// <summary>
    /// 标注功能控制
    /// 控制元素的自动标注功能
    /// </summary>
    public class AnnotationFeature : FeatureControlProperty
    {
        #region 私有字段
        private bool _autoAnnotation = false;
        private int _precision = 2;
        private Color _annotationColor = Color.Black;
        private double _textSize = 2.0;
        private bool _showDimensions = true;
        private bool _showLabels = true;
        private bool _showCoordinates = false;
        private AnnotationPosition _position = AnnotationPosition.Auto;
        private double _offset = 5.0;
        private string _dimensionPrefix = "";
        private string _dimensionSuffix = "mm";
        #endregion

        #region 构造函数
        public AnnotationFeature()
        {
            Name = "标注控制";
            Description = "控制元素的自动标注功能，包括尺寸标注、标签和坐标显示";
        }
        #endregion

        #region 标注属性

        /// <summary>
        /// 自动标注
        /// </summary>
        [Category("General")]
        [DisplayName("自动标注")]
        [Description("是否启用自动标注功能")]
        public bool AutoAnnotation
        {
            get => _autoAnnotation;
            set
            {
                if (_autoAnnotation != value)
                {
                    _autoAnnotation = value;
                    OnPropertyChanged(nameof(AutoAnnotation));
                }
            }
        }

        /// <summary>
        /// 标注精度
        /// </summary>
        [Category("General")]
        [DisplayName("标注精度")]
        [Description("数值标注的小数位数")]
        [Range(0, 6)]
        public int Precision
        {
            get => _precision;
            set
            {
                var clampedValue = Math.Max(0, Math.Min(6, value));
                if (_precision != clampedValue)
                {
                    _precision = clampedValue;
                    OnPropertyChanged(nameof(Precision));
                }
            }
        }

        /// <summary>
        /// 标注颜色
        /// </summary>
        [Category("Appearance")]
        [DisplayName("标注颜色")]
        [Description("标注文字和线条的颜色")]
        public Color AnnotationColor
        {
            get => _annotationColor;
            set
            {
                if (_annotationColor != value)
                {
                    _annotationColor = value;
                    OnPropertyChanged(nameof(AnnotationColor));
                }
            }
        }

        /// <summary>
        /// 文字大小
        /// </summary>
        [Category("Appearance")]
        [DisplayName("文字大小")]
        [Description("标注文字的大小")]
        [Range(0.5, 20.0)]
        public double TextSize
        {
            get => _textSize;
            set
            {
                var clampedValue = Math.Max(0.5, Math.Min(20.0, value));
                if (Math.Abs(_textSize - clampedValue) > 1e-6)
                {
                    _textSize = clampedValue;
                    OnPropertyChanged(nameof(TextSize));
                }
            }
        }

        /// <summary>
        /// 显示尺寸标注
        /// </summary>
        [Category("Content")]
        [DisplayName("显示尺寸")]
        [Description("是否显示尺寸标注")]
        public bool ShowDimensions
        {
            get => _showDimensions;
            set
            {
                if (_showDimensions != value)
                {
                    _showDimensions = value;
                    OnPropertyChanged(nameof(ShowDimensions));
                }
            }
        }

        /// <summary>
        /// 显示标签
        /// </summary>
        [Category("Content")]
        [DisplayName("显示标签")]
        [Description("是否显示元素标签")]
        public bool ShowLabels
        {
            get => _showLabels;
            set
            {
                if (_showLabels != value)
                {
                    _showLabels = value;
                    OnPropertyChanged(nameof(ShowLabels));
                }
            }
        }

        /// <summary>
        /// 显示坐标
        /// </summary>
        [Category("Content")]
        [DisplayName("显示坐标")]
        [Description("是否显示关键点坐标")]
        public bool ShowCoordinates
        {
            get => _showCoordinates;
            set
            {
                if (_showCoordinates != value)
                {
                    _showCoordinates = value;
                    OnPropertyChanged(nameof(ShowCoordinates));
                }
            }
        }

        /// <summary>
        /// 标注位置
        /// </summary>
        [Category("Position")]
        [DisplayName("标注位置")]
        [Description("标注的放置位置")]
        public AnnotationPosition Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    OnPropertyChanged(nameof(Position));
                }
            }
        }

        /// <summary>
        /// 偏移距离
        /// </summary>
        [Category("Position")]
        [DisplayName("偏移距离")]
        [Description("标注与元素的偏移距离")]
        [Range(0.0, 50.0)]
        public double Offset
        {
            get => _offset;
            set
            {
                var clampedValue = Math.Max(0.0, Math.Min(50.0, value));
                if (Math.Abs(_offset - clampedValue) > 1e-6)
                {
                    _offset = clampedValue;
                    OnPropertyChanged(nameof(Offset));
                }
            }
        }

        /// <summary>
        /// 尺寸前缀
        /// </summary>
        [Category("Format")]
        [DisplayName("尺寸前缀")]
        [Description("尺寸标注的前缀文字")]
        public string DimensionPrefix
        {
            get => _dimensionPrefix;
            set
            {
                if (_dimensionPrefix != value)
                {
                    _dimensionPrefix = value ?? "";
                    OnPropertyChanged(nameof(DimensionPrefix));
                }
            }
        }

        /// <summary>
        /// 尺寸后缀
        /// </summary>
        [Category("Format")]
        [DisplayName("尺寸后缀")]
        [Description("尺寸标注的后缀文字（如单位）")]
        public string DimensionSuffix
        {
            get => _dimensionSuffix;
            set
            {
                if (_dimensionSuffix != value)
                {
                    _dimensionSuffix = value ?? "";
                    OnPropertyChanged(nameof(DimensionSuffix));
                }
            }
        }

        #endregion

        #region 覆盖方法

        /// <summary>
        /// 应用标注设置到元素
        /// </summary>
        /// <param name="element">目标元素</param>
        public override void Apply(BaseElementBlock element)
        {
            if (!AutoAnnotation || element == null)
                return;

            // 清除已有的标注
            ClearExistingAnnotations(element);

            // 生成新的标注
            var annotations = GenerateAnnotations(element);
            
            // 添加标注到数据库
            foreach (var annotation in annotations)
            {
                // 简化实现：暂时不执行实际添加操作
                // element.AppendEntity(annotation);
            }
        }

        /// <summary>
        /// 重置标注设置
        /// </summary>
        /// <param name="element">目标元素</param>
        public override void Reset(BaseElementBlock element)
        {
            if (element == null)
                return;

            // 清除所有标注
            ClearExistingAnnotations(element);

            // 重置为默认值
            AutoAnnotation = false;
            Precision = 2;
            AnnotationColor = Color.Black;
            TextSize = 2.0;
            ShowDimensions = true;
            ShowLabels = true;
            ShowCoordinates = false;
            Position = AnnotationPosition.Auto;
            Offset = 5.0;
            DimensionPrefix = "";
            DimensionSuffix = "mm";
        }

        /// <summary>
        /// 验证设置是否有效
        /// </summary>
        /// <returns>验证结果</returns>
        public override bool Validate()
        {
            if (Precision < 0 || Precision > 6)
                return false;

            if (TextSize < 0.5 || TextSize > 20.0)
                return false;

            if (Offset < 0.0 || Offset > 50.0)
                return false;

            // 至少要显示一种标注内容
            if (AutoAnnotation && !ShowDimensions && !ShowLabels && !ShowCoordinates)
                return false;

            return true;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 清除已有的标注
        /// </summary>
        /// <param name="element">目标元素</param>
        private void ClearExistingAnnotations(BaseElementBlock element)
        {
            var annotationsToRemove = new List<ObjectId>();

            foreach (var entityId in element.ChildIdList.ToList())
            {
                var entity = FindEntityInElement(element, entityId);
                if (entity != null && IsAnnotationEntity(entity))
                {
                    annotationsToRemove.Add(entityId);
                }
            }

            // 移除标注实体
            foreach (var id in annotationsToRemove)
            {
                element.ChildIdList.Remove(id);
            }
        }

        /// <summary>
        /// 判断实体是否为标注实体
        /// </summary>
        /// <param name="entity">实体</param>
        /// <returns>是否为标注实体</returns>
        private bool IsAnnotationEntity(Entity entity)
        {
            return entity is Text || 
                   entity is CoatingMark ||
                   entity.GetType().Name.IndexOf("Dimension", StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// 生成标注
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <returns>生成的标注实体</returns>
        private List<Entity> GenerateAnnotations(BaseElementBlock element)
        {
            var annotations = new List<Entity>();

            // 获取元素的几何信息
            var geometryInfo = AnalyzeElementGeometry(element);

            if (ShowDimensions)
            {
                annotations.AddRange(GenerateDimensionAnnotations(geometryInfo));
            }

            if (ShowLabels)
            {
                annotations.AddRange(GenerateLabelAnnotations(element, geometryInfo));
            }

            if (ShowCoordinates)
            {
                annotations.AddRange(GenerateCoordinateAnnotations(geometryInfo));
            }

            return annotations;
        }

        /// <summary>
        /// 分析元素几何信息
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <returns>几何信息</returns>
        private ElementGeometryInfo AnalyzeElementGeometry(BaseElementBlock element)
        {
            var info = new ElementGeometryInfo();
            var entities = new List<Entity>();

            // 收集所有实体
            foreach (var entityId in element.ChildIdList)
            {
                var entity = FindEntityInElement(element, entityId);
                if (entity != null && !IsAnnotationEntity(entity))
                {
                    entities.Add(entity);
                }
            }

            if (entities.Count == 0)
                return info;

            // 计算边界框
            var bounds = entities.First().bounding;
            foreach (var entity in entities.Skip(1))
            {
                bounds = UnionBounding(bounds, entity.bounding);
            }

            info.BoundingBox = bounds;
            info.Center = new Vector2(bounds.center.X, bounds.center.Y);
            info.Width = bounds.width;
            info.Height = bounds.height;
            info.Entities = entities;

            return info;
        }

        /// <summary>
        /// 生成尺寸标注
        /// </summary>
        /// <param name="geometryInfo">几何信息</param>
        /// <returns>尺寸标注实体</returns>
        private List<Entity> GenerateDimensionAnnotations(ElementGeometryInfo geometryInfo)
        {
            var annotations = new List<Entity>();

            // 宽度标注
            if (geometryInfo.Width > 0)
            {
                var widthText = CreateDimensionText(
                    geometryInfo.Width,
                    new Vector2(geometryInfo.Center.X, geometryInfo.BoundingBox.bottom - Offset)
                );
                annotations.Add(widthText);
            }

            // 高度标注
            if (geometryInfo.Height > 0)
            {
                var heightText = CreateDimensionText(
                    geometryInfo.Height,
                    new Vector2(geometryInfo.BoundingBox.right + Offset, geometryInfo.Center.Y)
                );
                annotations.Add(heightText);
            }

            return annotations;
        }

        /// <summary>
        /// 生成标签标注
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <param name="geometryInfo">几何信息</param>
        /// <returns>标签标注实体</returns>
        private List<Entity> GenerateLabelAnnotations(BaseElementBlock element, ElementGeometryInfo geometryInfo)
        {
            var annotations = new List<Entity>();

            // 创建元素名称标签
            var labelText = new Text
            {
                Value = element.GetType().Name,
                Position = new Vector3(geometryInfo.Center.X, geometryInfo.BoundingBox.top + Offset, 0),
                Height = TextSize,
                color = lcdb.Colors.Color.FromColor(AnnotationColor),
                alignment = lcdb.TextAlignment.CenterBottom
            };

            annotations.Add(labelText);

            return annotations;
        }

        /// <summary>
        /// 生成坐标标注
        /// </summary>
        /// <param name="geometryInfo">几何信息</param>
        /// <returns>坐标标注实体</returns>
        private List<Entity> GenerateCoordinateAnnotations(ElementGeometryInfo geometryInfo)
        {
            var annotations = new List<Entity>();

            // 中心点坐标
            var centerText = new Text
            {
                Value = $"({geometryInfo.Center.X.ToString($"F{Precision}")}, {geometryInfo.Center.Y.ToString($"F{Precision}")})",
                Position = new Vector3(geometryInfo.Center.X, geometryInfo.Center.Y - TextSize * 0.5, 0),
                Height = TextSize * 0.8,
                color = lcdb.Colors.Color.FromColor(AnnotationColor),
                alignment = lcdb.TextAlignment.CenterTop
            };

            annotations.Add(centerText);

            return annotations;
        }

        /// <summary>
        /// 创建尺寸文字
        /// </summary>
        /// <param name="dimension">尺寸值</param>
        /// <param name="position">位置</param>
        /// <returns>文字实体</returns>
        private Text CreateDimensionText(double dimension, Vector2 position)
        {
            var dimensionText = $"{DimensionPrefix}{dimension.ToString($"F{Precision}")}{DimensionSuffix}";
            
            return new Text
            {
                Value = dimensionText,
                Position = new Vector3(position.X, position.Y, 0),
                Height = TextSize,
                color = lcdb.Colors.Color.FromColor(AnnotationColor),
                alignment = lcdb.TextAlignment.CenterMiddle
            };
        }

        /// <summary>
        /// 在元素中查找实体
        /// </summary>
        /// <param name="element">元素</param>
        /// <param name="entityId">实体ID</param>
        /// <returns>找到的实体</returns>
        private Entity FindEntityInElement(BaseElementBlock element, ObjectId entityId)
        {
            // 简化实现：直接通过元素访问实体
            // 实际实现需要根据具体的数据库访问模式
            return null; // 暂时返回null，避免编译错误
        }

        /// <summary>
        /// 合并两个边界框
        /// </summary>
        /// <param name="bounds1">边界框1</param>
        /// <param name="bounds2">边界框2</param>
        /// <returns>合并后的边界框</returns>
        private Bounding UnionBounding(Bounding bounds1, Bounding bounds2)
        {
            var minX = Math.Min(bounds1.left, bounds2.left);
            var maxX = Math.Max(bounds1.right, bounds2.right);
            var minY = Math.Min(bounds1.bottom, bounds2.bottom);
            var maxY = Math.Max(bounds1.top, bounds2.top);
            
            return new Bounding(
                new Vector2(minX, minY),
                new Vector2(maxX, maxY)
            );
        }

        #endregion
    }

    /// <summary>
    /// 标注位置枚举
    /// </summary>
    public enum AnnotationPosition
    {
        /// <summary>
        /// 自动选择
        /// </summary>
        [Description("自动选择")]
        Auto = 0,

        /// <summary>
        /// 上方
        /// </summary>
        [Description("上方")]
        Top = 1,

        /// <summary>
        /// 下方
        /// </summary>
        [Description("下方")]
        Bottom = 2,

        /// <summary>
        /// 左侧
        /// </summary>
        [Description("左侧")]
        Left = 3,

        /// <summary>
        /// 右侧
        /// </summary>
        [Description("右侧")]
        Right = 4,

        /// <summary>
        /// 内部
        /// </summary>
        [Description("内部")]
        Inside = 5
    }

    /// <summary>
    /// 元素几何信息
    /// </summary>
    public class ElementGeometryInfo
    {
        /// <summary>
        /// 边界框
        /// </summary>
        public Bounding BoundingBox { get; set; }

        /// <summary>
        /// 中心点
        /// </summary>
        public Vector2 Center { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 相关实体
        /// </summary>
        public List<Entity> Entities { get; set; } = new List<Entity>();
    }
}