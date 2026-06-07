using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using lcdb;
using OtoCAD.OpticEntity;

namespace OtoCAD.OpticEntity.FeatureControl
{
    /// <summary>
    /// 渲染功能控制
    /// 控制元素的显示效果和渲染属性
    /// </summary>
    public class RenderingFeature : FeatureControlProperty
    {
        #region 私有字段
        private bool _showOutline = true;
        private bool _showFill = false;
        private double _opacity = 1.0;
        private double _lineWidth = 1.0;
        private Color _outlineColor = Color.Black;
        private Color _fillColor = Color.LightGray;
        private bool _antiAlias = true;
        private LineStyle _lineStyle = LineStyle.Solid;
        #endregion

        #region 构造函数
        public RenderingFeature()
        {
            Name = "渲染控制";
            Description = "控制元素的显示效果，包括轮廓、填充、透明度等属性";
        }
        #endregion

        #region 渲染属性

        /// <summary>
        /// 显示轮廓
        /// </summary>
        [Category("Outline")]
        [DisplayName("显示轮廓")]
        [Description("是否显示元素的轮廓线")]
        public bool ShowOutline
        {
            get => _showOutline;
            set
            {
                if (_showOutline != value)
                {
                    _showOutline = value;
                    OnPropertyChanged(nameof(ShowOutline));
                }
            }
        }

        /// <summary>
        /// 轮廓颜色
        /// </summary>
        [Category("Outline")]
        [DisplayName("轮廓颜色")]
        [Description("元素轮廓线的颜色")]
        public Color OutlineColor
        {
            get => _outlineColor;
            set
            {
                if (_outlineColor != value)
                {
                    _outlineColor = value;
                    OnPropertyChanged(nameof(OutlineColor));
                }
            }
        }

        /// <summary>
        /// 线宽
        /// </summary>
        [Category("Outline")]
        [DisplayName("线宽")]
        [Description("轮廓线的宽度")]
        [Range(0.1, 10.0)]
        public double LineWidth
        {
            get => _lineWidth;
            set
            {
                var clampedValue = Math.Max(0.1, Math.Min(10.0, value));
                if (Math.Abs(_lineWidth - clampedValue) > 1e-6)
                {
                    _lineWidth = clampedValue;
                    OnPropertyChanged(nameof(LineWidth));
                }
            }
        }

        /// <summary>
        /// 线型
        /// </summary>
        [Category("Outline")]
        [DisplayName("线型")]
        [Description("轮廓线的样式")]
        public LineStyle LineStyle
        {
            get => _lineStyle;
            set
            {
                if (_lineStyle != value)
                {
                    _lineStyle = value;
                    OnPropertyChanged(nameof(LineStyle));
                }
            }
        }

        /// <summary>
        /// 显示填充
        /// </summary>
        [Category("Fill")]
        [DisplayName("显示填充")]
        [Description("是否显示元素的填充")]
        public bool ShowFill
        {
            get => _showFill;
            set
            {
                if (_showFill != value)
                {
                    _showFill = value;
                    OnPropertyChanged(nameof(ShowFill));
                }
            }
        }

        /// <summary>
        /// 填充颜色
        /// </summary>
        [Category("Fill")]
        [DisplayName("填充颜色")]
        [Description("元素填充的颜色")]
        public Color FillColor
        {
            get => _fillColor;
            set
            {
                if (_fillColor != value)
                {
                    _fillColor = value;
                    OnPropertyChanged(nameof(FillColor));
                }
            }
        }

        /// <summary>
        /// 透明度
        /// </summary>
        [Category("Effect")]
        [DisplayName("透明度")]
        [Description("元素的透明度，0为完全透明，1为完全不透明")]
        [Range(0.0, 1.0)]
        public double Opacity
        {
            get => _opacity;
            set
            {
                var clampedValue = Math.Max(0.0, Math.Min(1.0, value));
                if (Math.Abs(_opacity - clampedValue) > 1e-6)
                {
                    _opacity = clampedValue;
                    OnPropertyChanged(nameof(Opacity));
                }
            }
        }

        /// <summary>
        /// 抗锯齿
        /// </summary>
        [Category("Effect")]
        [DisplayName("抗锯齿")]
        [Description("是否启用抗锯齿渲染")]
        public bool AntiAlias
        {
            get => _antiAlias;
            set
            {
                if (_antiAlias != value)
                {
                    _antiAlias = value;
                    OnPropertyChanged(nameof(AntiAlias));
                }
            }
        }

        #endregion

        #region 覆盖方法

        /// <summary>
        /// 应用渲染设置到元素
        /// </summary>
        /// <param name="element">目标元素</param>
        public override void Apply(BaseElementBlock element)
        {
            if (element == null)
                return;

            // 应用到所有子实体
            foreach (var entityId in element.ChildIdList.ToList())
            {
                ApplyToEntity(element, entityId);
            }
        }

        /// <summary>
        /// 重置渲染设置
        /// </summary>
        /// <param name="element">目标元素</param>
        public override void Reset(BaseElementBlock element)
        {
            if (element == null)
                return;

            // 重置为默认值
            ShowOutline = true;
            ShowFill = false;
            Opacity = 1.0;
            LineWidth = 1.0;
            OutlineColor = Color.Black;
            FillColor = Color.LightGray;
            AntiAlias = true;
            LineStyle = LineStyle.Solid;

            // 重新应用默认设置
            Apply(element);
        }

        /// <summary>
        /// 验证设置是否有效
        /// </summary>
        /// <returns>验证结果</returns>
        public override bool Validate()
        {
            // 检查基本设置
            if (LineWidth < 0.1 || LineWidth > 10.0)
                return false;

            if (Opacity < 0.0 || Opacity > 1.0)
                return false;

            // 至少要显示轮廓或填充中的一种
            if (!ShowOutline && !ShowFill)
                return false;

            return true;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 应用渲染设置到指定实体
        /// </summary>
        /// <param name="element">元素</param>
        /// <param name="entityId">实体ID</param>
        private void ApplyToEntity(BaseElementBlock element, ObjectId entityId)
        {
            var entity = FindEntityInElement(element, entityId);
            if (entity == null)
                return;

            // 应用轮廓设置
            if (ShowOutline)
            {
                entity.color = lcdb.Colors.Color.FromColor(OutlineColor);
                
                // 设置线重
                entity.lineWeight = ConvertToLineWeight(LineWidth);
                
                // 设置线型
                switch (LineStyle)
                {
                    case LineStyle.Solid:
                        entity.lineType = LineType.Solid;
                        break;
                    case LineStyle.Dashed:
                        entity.lineType = LineType.Dash;
                        break;
                    case LineStyle.Dotted:
                        entity.lineType = LineType.Dot;
                        break;
                    case LineStyle.DashDot:
                        entity.lineType = LineType.DashDot;
                        break;
                    case LineStyle.DashDotDot:
                        entity.lineType = LineType.DashDotDot;
                        break;
                }
            }

            // 应用透明度
            if (Opacity < 1.0)
            {
                // 设置透明度 - 注意: OtoCAD的Color不直接支持Alpha通道
                // 这里调整RGB值来模拟透明度效果
                var currentColor = entity.color;
                var alpha = (byte)(255 * Opacity);
                entity.color = lcdb.Colors.Color.FromRGB(
                    (byte)(currentColor.r * Opacity),
                    (byte)(currentColor.g * Opacity),
                    (byte)(currentColor.b * Opacity)
                );
            }

            // 应用填充设置
            if (ShowFill && entity is IFillableEntity fillableEntity)
            {
                fillableEntity.IsFilled = true;
                fillableEntity.FillColor = lcdb.Colors.Color.FromColor(FillColor);
            }
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
        /// 将线宽值转换为LineWeight枚举
        /// </summary>
        /// <param name="lineWidth">线宽值</param>
        /// <returns>LineWeight枚举值</returns>
        private LineWeight ConvertToLineWeight(double lineWidth)
        {
            // 根据线宽值映射到最接近的LineWeight枚举值
            var widthValue = (int)(lineWidth * 100);
            
            if (widthValue <= 0) return LineWeight.LineWeight000;
            if (widthValue <= 5) return LineWeight.LineWeight005;
            if (widthValue <= 9) return LineWeight.LineWeight009;
            if (widthValue <= 13) return LineWeight.LineWeight013;
            if (widthValue <= 15) return LineWeight.LineWeight015;
            if (widthValue <= 18) return LineWeight.LineWeight018;
            if (widthValue <= 20) return LineWeight.LineWeight020;
            if (widthValue <= 25) return LineWeight.LineWeight025;
            if (widthValue <= 30) return LineWeight.LineWeight030;
            if (widthValue <= 35) return LineWeight.LineWeight035;
            if (widthValue <= 40) return LineWeight.LineWeight040;
            if (widthValue <= 50) return LineWeight.LineWeight050;
            if (widthValue <= 53) return LineWeight.LineWeight053;
            if (widthValue <= 60) return LineWeight.LineWeight060;
            if (widthValue <= 70) return LineWeight.LineWeight070;
            if (widthValue <= 80) return LineWeight.LineWeight080;
            if (widthValue <= 90) return LineWeight.LineWeight090;
            if (widthValue <= 100) return LineWeight.LineWeight100;
            if (widthValue <= 106) return LineWeight.LineWeight106;
            if (widthValue <= 120) return LineWeight.LineWeight120;
            if (widthValue <= 140) return LineWeight.LineWeight140;
            if (widthValue <= 158) return LineWeight.LineWeight158;
            if (widthValue <= 200) return LineWeight.LineWeight200;
            
            return LineWeight.LineWeight211; // 最大值
        }

        #endregion
    }

    /// <summary>
    /// 线型枚举
    /// </summary>
    public enum LineStyle
    {
        /// <summary>
        /// 实线
        /// </summary>
        [Description("实线")]
        Solid = 0,

        /// <summary>
        /// 虚线
        /// </summary>
        [Description("虚线")]
        Dashed = 1,

        /// <summary>
        /// 点线
        /// </summary>
        [Description("点线")]
        Dotted = 2,

        /// <summary>
        /// 点划线
        /// </summary>
        [Description("点划线")]
        DashDot = 3,

        /// <summary>
        /// 双点划线
        /// </summary>
        [Description("双点划线")]
        DashDotDot = 4
    }

    /// <summary>
    /// 可填充实体接口
    /// </summary>
    public interface IFillableEntity
    {
        /// <summary>
        /// 是否填充
        /// </summary>
        bool IsFilled { get; set; }

        /// <summary>
        /// 填充颜色
        /// </summary>
        lcdb.Colors.Color FillColor { get; set; }
    }
}