using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json.Serialization;
using LitMath;
using lcdb;

namespace OtoCAD.OpticEntity
{
    /// <summary>
    /// 单透镜实体类
    /// </summary>
    [Obsolete]
    public class SingleLens : OpticalEntity
    {
        #region 透镜参数

        private double _diameter = 25.4;
        /// <summary>
        /// 透镜直径 (mm)
        /// </summary>
        [Category("几何参数")]
        [DisplayName("直径")]
        [Description("透镜的直径 (mm)")]
        public double Diameter
        {
            get { return _diameter; }
            set 
            { 
                _diameter = value;
                UpdateBounding();
            }
        }

        private double _r1 = 51.5;
        /// <summary>
        /// 第一面曲率半径 (mm)
        /// </summary>
        [Category("几何参数")]
        [DisplayName("R1")]
        [Description("第一面的曲率半径 (mm)，正值表示凸面，负值表示凹面")]
        public double R1
        {
            get { return _r1; }
            set 
            { 
                _r1 = value;
                UpdateBounding();
            }
        }

        private double _r2 = -51.5;
        /// <summary>
        /// 第二面曲率半径 (mm)
        /// </summary>
        [Category("几何参数")]
        [DisplayName("R2")]
        [Description("第二面的曲率半径 (mm)，正值表示凸面，负值表示凹面")]
        public double R2
        {
            get { return _r2; }
            set 
            { 
                _r2 = value;
                UpdateBounding();
            }
        }

        private double _centerThickness = 6.0;
        /// <summary>
        /// 中心厚度 (mm)
        /// </summary>
        [Category("几何参数")]
        [DisplayName("中心厚度")]
        [Description("透镜的中心厚度 (mm)")]
        public double CenterThickness
        {
            get { return _centerThickness; }
            set 
            { 
                _centerThickness = value;
                UpdateBounding();
            }
        }

        // Material属性已在基类OpticalEntity中定义

        /// <summary>
        /// 边缘厚度 (mm) - 计算属性
        /// </summary>
        [Category("计算参数")]
        [DisplayName("边缘厚度")]
        [Description("透镜的边缘厚度 (mm)")]
        [ReadOnly(true)]
        public double EdgeThickness
        {
            get { return CalculateEdgeThickness(); }
        }

        /// <summary>
        /// 公差规格
        /// </summary>
        [Browsable(false)]
        public LensTolerances Tolerances { get; set; }

        /// <summary>
        /// 表面质量规格
        /// </summary>
        [Browsable(false)]
        public new SurfaceQualitySpec SurfaceQuality { get; set; }

        /// <summary>
        /// 镀膜规格
        /// </summary>
        [Browsable(false)]
        public CoatingSpecification Coating { get; set; }

        /// <summary>
        /// 透镜位置
        /// </summary>
        private Vector2 _position = new Vector2(0, 0);
        public Vector2 Position
        {
            get { return _position; }
            set 
            { 
                _position = value;
                UpdateBounding();
            }
        }

        #endregion

        #region 边界框

        private Bounding _bounding = new Bounding();
        public override Bounding bounding
        {
            get { return _bounding; }
        }

        private void UpdateBounding()
        {
            // 计算透镜的边界框
            double halfDiameter = _diameter / 2.0;
            double totalThickness = CenterThickness + Math.Abs(CalculateSag(R1)) + Math.Abs(CalculateSag(R2));
            
            _bounding = new Bounding(
                new Vector2(_position.X - halfDiameter, _position.Y - totalThickness / 2),
                new Vector2(_position.X + halfDiameter, _position.Y + totalThickness / 2)
            );
        }

        #endregion

        #region 方法

        /// <summary>
        /// 计算矢高
        /// </summary>
        private double CalculateSag(double radius)
        {
            if (Math.Abs(radius) < 0.001) return 0;
            
            double h = _diameter / 2.0;
            double sag = radius - Math.Sign(radius) * Math.Sqrt(radius * radius - h * h);
            return sag;
        }

        /// <summary>
        /// 计算边缘厚度
        /// </summary>
        private double CalculateEdgeThickness()
        {
            double sag1 = CalculateSag(R1);
            double sag2 = CalculateSag(R2);
            return CenterThickness + sag2 - sag1;
        }

        public override void Draw(IGraphicsDraw gd)
        {
            // 简化的透镜绘制
            DrawLensOutline(gd);
            DrawOpticalAxis(gd);
        }

        private void DrawLensOutline(IGraphicsDraw gd)
        {
            // 绘制透镜轮廓
            double halfDiameter = _diameter / 2.0;
            
            // 绘制第一面（左侧）
            if (Math.Abs(R1) > 0.001)
            {
                DrawLensSurface(gd, _position.X - CenterThickness / 2, R1, true);
            }
            else
            {
                // 平面
                gd.DrawLine(
                    new Vector2(_position.X - CenterThickness / 2, _position.Y - halfDiameter),
                    new Vector2(_position.X - CenterThickness / 2, _position.Y + halfDiameter)
                );
            }
            
            // 绘制第二面（右侧）
            if (Math.Abs(R2) > 0.001)
            {
                DrawLensSurface(gd, _position.X + CenterThickness / 2, R2, false);
            }
            else
            {
                // 平面
                gd.DrawLine(
                    new Vector2(_position.X + CenterThickness / 2, _position.Y - halfDiameter),
                    new Vector2(_position.X + CenterThickness / 2, _position.Y + halfDiameter)
                );
            }
            
            // 绘制上下边缘
            double x1 = _position.X - CenterThickness / 2 + CalculateSag(R1);
            double x2 = _position.X + CenterThickness / 2 + CalculateSag(R2);
            
            gd.DrawLine(
                new Vector2(x1, _position.Y + halfDiameter),
                new Vector2(x2, _position.Y + halfDiameter)
            );
            gd.DrawLine(
                new Vector2(x1, _position.Y - halfDiameter),
                new Vector2(x2, _position.Y - halfDiameter)
            );
        }

        private void DrawLensSurface(IGraphicsDraw gd, double x, double radius, bool isFirst)
        {
            double halfDiameter = _diameter / 2.0;
            double sag = CalculateSag(radius);
            
            // 计算圆心
            double centerX = x - radius;
            double centerY = _position.Y;
            
            // 计算起始和结束角度
            double angle = Math.Asin(halfDiameter / Math.Abs(radius));
            double startAngle = radius > 0 ? -angle : Math.PI - angle;
            double endAngle = radius > 0 ? angle : Math.PI + angle;
            
            // 转换为度数
            startAngle = startAngle * 180 / Math.PI;
            endAngle = endAngle * 180 / Math.PI;
            
            if (radius < 0)
            {
                double temp = startAngle;
                startAngle = endAngle;
                endAngle = temp;
            }
            
            // 绘制圆弧
            gd.DrawArc(
                new Vector2(centerX, centerY),
                Math.Abs(radius),
                startAngle,
                endAngle - startAngle
            );
        }

        private void DrawOpticalAxis(IGraphicsDraw gd)
        {
            // 绘制光轴
            // 注意：IGraphicsDraw 接口不支持线型设置，所以无法绘制虚线
            
            double extend = 20; // 延伸长度
            gd.DrawLine(
                new Vector2(_position.X - CenterThickness / 2 - extend, _position.Y),
                new Vector2(_position.X + CenterThickness / 2 + extend, _position.Y)
            );
        }

        public override void Translate(Vector2 translation)
        {
            _position += translation;
            UpdateBounding();
        }

        public override object Clone()
        {
            SingleLens newLens = new SingleLens();
            newLens._diameter = this._diameter;
            newLens._r1 = this._r1;
            newLens._r2 = this._r2;
            newLens._centerThickness = this._centerThickness;
            newLens.Material = this.Material;
            newLens.RefractiveIndex = this.RefractiveIndex;
            newLens.AbbeNumber = this.AbbeNumber;
            newLens._position = this._position;
            newLens.color = this.color;
            newLens.layerId = this.layerId;
            
            // Clone new properties
            newLens.Tolerances = this.Tolerances?.Clone();
            newLens.SurfaceQuality = this.SurfaceQuality?.Clone();
            newLens.Coating = this.Coating?.Clone();
            
            newLens.UpdateBounding();
            
            return newLens;
        }

        #endregion

        #region 光学实体基类实现

        /// <summary>
        /// 计算透镜体积
        /// </summary>
        public override double CalculateVolume()
        {
            // 使用圆台体积公式近似计算
            double halfDiameter = _diameter / 2.0;
            double area = Math.PI * halfDiameter * halfDiameter;
            
            // 计算两个球冠的体积
            double vol1 = CalculateSphericalCapVolume(Math.Abs(R1), halfDiameter);
            double vol2 = CalculateSphericalCapVolume(Math.Abs(R2), halfDiameter);
            
            // 计算圆柱体部分的体积
            double cylHeight = EdgeThickness;
            double cylVolume = area * cylHeight;
            
            // 根据曲率方向调整体积
            if (R1 > 0) cylVolume += vol1; // 凸面增加体积
            else cylVolume -= vol1; // 凹面减少体积
            
            if (R2 < 0) cylVolume += vol2; // 凸面增加体积（注意R2的符号约定）
            else cylVolume -= vol2; // 凹面减少体积
            
            return Math.Abs(cylVolume);
        }
        
        /// <summary>
        /// 计算球冠体积
        /// </summary>
        private double CalculateSphericalCapVolume(double radius, double height)
        {
            if (radius < 0.001) return 0;
            
            double sag = Math.Abs(CalculateSag(radius));
            return Math.PI * sag * (3 * height * height + sag * sag) / 6.0;
        }
        
        /// <summary>
        /// 验证光学参数
        /// </summary>
        public override (bool IsValid, List<string> Errors) ValidateOpticalParameters()
        {
            var (isValid, errors) = base.ValidateOpticalParameters();
            
            // 验证透镜特有参数
            if (_diameter <= 0)
            {
                errors.Add("透镜直径必须大于0");
            }
            
            if (_centerThickness <= 0)
            {
                errors.Add("中心厚度必须大于0");
            }
            
            // 验证边缘厚度
            double edgeThickness = EdgeThickness;
            if (edgeThickness < 0.5)
            {
                errors.Add($"边缘厚度 {edgeThickness:F2}mm 太薄，建议至少0.5mm");
            }
            
            // 验证曲率半径与直径的关系
            if (Math.Abs(R1) > 0 && Math.Abs(R1) < _diameter / 2)
            {
                errors.Add($"第一面曲率半径 {R1}mm 小于半径 {_diameter/2}mm，无法制造");
            }
            
            if (Math.Abs(R2) > 0 && Math.Abs(R2) < _diameter / 2)
            {
                errors.Add($"第二面曲率半径 {R2}mm 小于半径 {_diameter/2}mm，无法制造");
            }
            
            return (errors.Count == 0, errors);
        }

        #endregion

        #region 缺失的抽象方法实现

        /// <summary>
        /// 旋转单透镜
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            // 将角度转换为弧度
            double rad = angle * Math.PI / 180.0;
            
            // 计算新位置
            double dx = _position.X - center.X;
            double dy = _position.Y - center.Y;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            
            _position = new Vector2(
                center.X + dx * cos - dy * sin,
                center.Y + dx * sin + dy * cos
            );
            
            UpdateBounding();
        }

        /// <summary>
        /// 通过矩阵变换
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            _position = transform * _position;
            
            // 获取缩放因子
            double scaleX = Math.Sqrt(transform.m11 * transform.m11 + transform.m21 * transform.m21);
            
            // 应用缩放到尺寸参数
            _diameter *= scaleX;
            _r1 *= scaleX;
            _r2 *= scaleX;
            _centerThickness *= scaleX;
            
            UpdateBounding();
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new SingleLens();
        }

        #endregion
    }
}