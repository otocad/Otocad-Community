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
    /// 双胶合透镜实体类
    /// </summary>
    [Obsolete]
    public class DoubletLens : OpticalEntity
    {
        #region 双胶合透镜参数

        private double _diameter = 25.4;
        /// <summary>
        /// 透镜直径 (mm)
        /// </summary>
        [Category("几何参数")]
        [DisplayName("直径")]
        [Description("双胶合透镜的直径 (mm)")]
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

        private double _r2 = -38.6;
        /// <summary>
        /// 胶合面曲率半径 (mm)
        /// </summary>
        [Category("几何参数")]
        [DisplayName("R2 (胶合面)")]
        [Description("胶合面的曲率半径 (mm)")]
        public double R2
        {
            get { return _r2; }
            set 
            { 
                _r2 = value;
                UpdateBounding();
            }
        }

        private double _r3 = -128.2;
        /// <summary>
        /// 第三面曲率半径 (mm)
        /// </summary>
        [Category("几何参数")]
        [DisplayName("R3")]
        [Description("第三面的曲率半径 (mm)，正值表示凸面，负值表示凹面")]
        public double R3
        {
            get { return _r3; }
            set 
            { 
                _r3 = value;
                UpdateBounding();
            }
        }

        private double _centerThickness1 = 6.0;
        /// <summary>
        /// 第一片中心厚度 (mm)
        /// </summary>
        [Category("几何参数")]
        [DisplayName("第一片厚度")]
        [Description("第一片透镜的中心厚度 (mm)")]
        public double CenterThickness1
        {
            get { return _centerThickness1; }
            set 
            { 
                _centerThickness1 = value;
                UpdateBounding();
            }
        }

        private double _centerThickness2 = 2.0;
        /// <summary>
        /// 第二片中心厚度 (mm)
        /// </summary>
        [Category("几何参数")]
        [DisplayName("第二片厚度")]
        [Description("第二片透镜的中心厚度 (mm)")]
        public double CenterThickness2
        {
            get { return _centerThickness2; }
            set 
            { 
                _centerThickness2 = value;
                UpdateBounding();
            }
        }

        // 第一片材料使用基类的Material属性
        
        private string _material2 = "SF5";
        /// <summary>
        /// 第二片材料
        /// </summary>
        [Category("材料参数")]
        [DisplayName("第二片材料")]
        [Description("第二片透镜的材料类型")]
        public string Material2
        {
            get { return _material2; }
            set { _material2 = value; }
        }

        private double _refractiveIndex2 = 1.67270;
        /// <summary>
        /// 第二片折射率
        /// </summary>
        [Category("材料参数")]
        [DisplayName("第二片折射率")]
        [Description("第二片材料的d光折射率")]
        public double RefractiveIndex2
        {
            get { return _refractiveIndex2; }
            set { _refractiveIndex2 = value; }
        }

        private double _abbeNumber2 = 32.17;
        /// <summary>
        /// 第二片阿贝数
        /// </summary>
        [Category("材料参数")]
        [DisplayName("第二片阿贝数")]
        [Description("第二片材料的色散特性参数")]
        public double AbbeNumber2
        {
            get { return _abbeNumber2; }
            set { _abbeNumber2 = value; }
        }

        /// <summary>
        /// 总厚度 (mm) - 计算属性
        /// </summary>
        [Category("计算参数")]
        [DisplayName("总厚度")]
        [Description("双胶合透镜的总厚度 (mm)")]
        [ReadOnly(true)]
        public double TotalThickness
        {
            get { return _centerThickness1 + _centerThickness2; }
        }

        /// <summary>
        /// 边缘厚度 (mm) - 计算属性
        /// </summary>
        [Category("计算参数")]
        [DisplayName("边缘厚度")]
        [Description("双胶合透镜的边缘厚度 (mm)")]
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
            double totalThickness = TotalThickness + Math.Abs(CalculateSag(R1, _diameter)) + Math.Abs(CalculateSag(R3, _diameter));
            
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
        private double CalculateSag(double radius, double diameter)
        {
            if (Math.Abs(radius) < 0.001) return 0;
            
            double h = diameter / 2.0;
            if (Math.Abs(radius) < h) return 0; // 无效的曲率半径
            
            double sag = radius - Math.Sign(radius) * Math.Sqrt(radius * radius - h * h);
            return sag;
        }

        /// <summary>
        /// 计算边缘厚度
        /// </summary>
        private double CalculateEdgeThickness()
        {
            double sag1 = CalculateSag(R1, _diameter);
            double sag2 = CalculateSag(R2, _diameter);
            double sag3 = CalculateSag(R3, _diameter);
            
            // 第一片边缘厚度
            double edge1 = _centerThickness1 + sag2 - sag1;
            // 第二片边缘厚度
            double edge2 = _centerThickness2 + sag3 - sag2;
            
            return edge1 + edge2;
        }

        public override void Draw(IGraphicsDraw gd)
        {
            // 绘制双胶合透镜
            DrawDoubletOutline(gd);
            DrawOpticalAxis(gd);
            DrawCementLine(gd);
        }

        private void DrawDoubletOutline(IGraphicsDraw gd)
        {
            double halfDiameter = _diameter / 2.0;
            double totalThickness = TotalThickness;
            
            // 计算各面的x坐标
            double x1 = _position.X - totalThickness / 2;
            double x2 = x1 + _centerThickness1;
            double x3 = x2 + _centerThickness2;
            
            // 绘制第一面
            if (Math.Abs(R1) > 0.001)
            {
                DrawLensSurface(gd, x1, R1, _diameter);
            }
            else
            {
                gd.DrawLine(
                    new Vector2(x1, _position.Y - halfDiameter),
                    new Vector2(x1, _position.Y + halfDiameter)
                );
            }
            
            // 绘制胶合面（通常不绘制，因为是内部面）
            // 但可以用虚线表示
            
            // 绘制第三面
            if (Math.Abs(R3) > 0.001)
            {
                DrawLensSurface(gd, x3, R3, _diameter);
            }
            else
            {
                gd.DrawLine(
                    new Vector2(x3, _position.Y - halfDiameter),
                    new Vector2(x3, _position.Y + halfDiameter)
                );
            }
            
            // 绘制上下边缘
            double xEdge1 = x1 + CalculateSag(R1, _diameter);
            double xEdge3 = x3 + CalculateSag(R3, _diameter);
            
            gd.DrawLine(
                new Vector2(xEdge1, _position.Y + halfDiameter),
                new Vector2(xEdge3, _position.Y + halfDiameter)
            );
            gd.DrawLine(
                new Vector2(xEdge1, _position.Y - halfDiameter),
                new Vector2(xEdge3, _position.Y - halfDiameter)
            );
        }

        private void DrawLensSurface(IGraphicsDraw gd, double x, double radius, double diameter)
        {
            double halfDiameter = diameter / 2.0;
            
            // 计算圆心
            double centerX = x - radius;
            double centerY = _position.Y;
            
            // 计算起始和结束角度
            if (Math.Abs(radius) >= halfDiameter)
            {
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
        }

        private void DrawCementLine(IGraphicsDraw gd)
        {
            // 绘制胶合面指示
            // 注意：IGraphicsDraw 接口不支持线型设置，所以无法绘制虚线
            
            double x2 = _position.X - TotalThickness / 2 + _centerThickness1;
            double halfDiameter = _diameter / 2.0;
            
            // 如果胶合面是曲面，绘制曲线
            if (Math.Abs(R2) > 0.001 && Math.Abs(R2) >= halfDiameter)
            {
                DrawLensSurface(gd, x2, R2, _diameter * 0.8); // 稍微缩小以示区别
            }
        }

        private void DrawOpticalAxis(IGraphicsDraw gd)
        {
            // 绘制光轴
            // 注意：IGraphicsDraw 接口不支持线型设置，所以无法绘制虚线
            
            double extend = 20; // 延伸长度
            double halfThickness = TotalThickness / 2;
            
            gd.DrawLine(
                new Vector2(_position.X - halfThickness - extend, _position.Y),
                new Vector2(_position.X + halfThickness + extend, _position.Y)
            );
        }

        public override void Translate(Vector2 translation)
        {
            _position += translation;
            UpdateBounding();
        }

        public override object Clone()
        {
            DoubletLens newLens = new DoubletLens();
            newLens._diameter = this._diameter;
            newLens._r1 = this._r1;
            newLens._r2 = this._r2;
            newLens._r3 = this._r3;
            newLens._centerThickness1 = this._centerThickness1;
            newLens._centerThickness2 = this._centerThickness2;
            newLens.Material = this.Material;
            newLens.RefractiveIndex = this.RefractiveIndex;
            newLens.AbbeNumber = this.AbbeNumber;
            newLens._material2 = this._material2;
            newLens._refractiveIndex2 = this._refractiveIndex2;
            newLens._abbeNumber2 = this._abbeNumber2;
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
        /// 计算双胶合透镜体积
        /// </summary>
        public override double CalculateVolume()
        {
            double halfDiameter = _diameter / 2.0;
            double area = Math.PI * halfDiameter * halfDiameter;
            
            // 计算第一片体积
            double vol1_front = CalculateSphericalCapVolume(Math.Abs(R1), halfDiameter);
            double vol1_cement = CalculateSphericalCapVolume(Math.Abs(R2), halfDiameter);
            double vol1_cylinder = area * _centerThickness1;
            
            double volume1 = vol1_cylinder;
            if (R1 > 0) volume1 += vol1_front;
            else volume1 -= vol1_front;
            
            if (R2 < 0) volume1 += vol1_cement;
            else volume1 -= vol1_cement;
            
            // 计算第二片体积
            double vol2_cement = vol1_cement; // 胶合面共享
            double vol2_back = CalculateSphericalCapVolume(Math.Abs(R3), halfDiameter);
            double vol2_cylinder = area * _centerThickness2;
            
            double volume2 = vol2_cylinder;
            if (R2 > 0) volume2 += vol2_cement;
            else volume2 -= vol2_cement;
            
            if (R3 < 0) volume2 += vol2_back;
            else volume2 -= vol2_back;
            
            return Math.Abs(volume1) + Math.Abs(volume2);
        }
        
        /// <summary>
        /// 计算球冠体积
        /// </summary>
        private double CalculateSphericalCapVolume(double radius, double height)
        {
            if (radius < 0.001) return 0;
            
            double sag = Math.Abs(CalculateSag(radius, height * 2));
            return Math.PI * sag * (3 * height * height + sag * sag) / 6.0;
        }
        
        /// <summary>
        /// 计算双胶合透镜质量
        /// </summary>
        public override double CalculateMass()
        {
            // 分别计算两片的体积和质量
            double halfDiameter = _diameter / 2.0;
            double area = Math.PI * halfDiameter * halfDiameter;
            
            // 第一片体积（简化计算）
            double volume1 = area * _centerThickness1;
            double density1 = GetMaterialDensityByName(Material);
            double mass1 = (volume1 / 1000.0) * density1;
            
            // 第二片体积（简化计算）
            double volume2 = area * _centerThickness2;
            double density2 = GetMaterialDensityByName(Material2);
            double mass2 = (volume2 / 1000.0) * density2;
            
            return mass1 + mass2;
        }
        
        /// <summary>
        /// 根据材料名称获取密度
        /// </summary>
        private double GetMaterialDensityByName(string materialName)
        {
            switch (materialName?.ToUpper())
            {
                case "BK7":
                case "N-BK7":
                    return 2.51;
                case "SF5":
                case "N-SF5":
                    return 3.07;
                case "SF11":
                case "N-SF11":
                    return 3.98;
                case "FUSED SILICA":
                case "FS":
                    return 2.20;
                default:
                    return 2.51;
            }
        }
        
        /// <summary>
        /// 验证双胶合透镜光学参数
        /// </summary>
        public override (bool IsValid, List<string> Errors) ValidateOpticalParameters()
        {
            var (isValid, errors) = base.ValidateOpticalParameters();
            
            // 验证第二片材料属性
            if (_refractiveIndex2 < 1.0 || _refractiveIndex2 > 3.0)
            {
                errors.Add($"第二片折射率 {_refractiveIndex2} 超出合理范围 (1.0-3.0)");
            }
            
            if (_abbeNumber2 < 10 || _abbeNumber2 > 100)
            {
                errors.Add($"第二片阿贝数 {_abbeNumber2} 超出合理范围 (10-100)");
            }
            
            // 验证几何参数
            if (_diameter <= 0)
            {
                errors.Add("透镜直径必须大于0");
            }
            
            if (_centerThickness1 <= 0)
            {
                errors.Add("第一片厚度必须大于0");
            }
            
            if (_centerThickness2 <= 0)
            {
                errors.Add("第二片厚度必须大于0");
            }
            
            // 验证边缘厚度
            double edgeThickness = EdgeThickness;
            if (edgeThickness < 1.0)
            {
                errors.Add($"边缘厚度 {edgeThickness:F2}mm 太薄，双胶合透镜建议至少1.0mm");
            }
            
            // 验证曲率半径
            if (Math.Abs(R1) > 0 && Math.Abs(R1) < _diameter / 2)
            {
                errors.Add($"第一面曲率半径 {R1}mm 小于半径，无法制造");
            }
            
            if (Math.Abs(R2) > 0 && Math.Abs(R2) < _diameter / 2)
            {
                errors.Add($"胶合面曲率半径 {R2}mm 小于半径，无法制造");
            }
            
            if (Math.Abs(R3) > 0 && Math.Abs(R3) < _diameter / 2)
            {
                errors.Add($"第三面曲率半径 {R3}mm 小于半径，无法制造");
            }
            
            // 验证色散补偿（消色差条件）
            double v1 = AbbeNumber;
            double v2 = _abbeNumber2;
            if (Math.Abs(v1 - v2) < 10)
            {
                errors.Add($"两片材料阿贝数差异过小 ({v1:F1} vs {v2:F1})，难以实现消色差");
            }
            
            return (errors.Count == 0, errors);
        }
        
        /// <summary>
        /// 获取双胶合透镜的制图标注
        /// </summary>
        public override List<string> GetDrawingAnnotations(string standard)
        {
            var annotations = new List<string>();
            
            switch (standard)
            {
                case "ISO10110":
                    annotations.Add($"Doublet: {Material}/{Material2}");
                    annotations.Add($"Ø{_diameter:F1} CT1={_centerThickness1:F1} CT2={_centerThickness2:F1}");
                    annotations.Add($"R1={R1:F1} R2={R2:F1} R3={R3:F1}");
                    annotations.Add($"n1/v1: {RefractiveIndex:F5}/{AbbeNumber:F1}");
                    annotations.Add($"n2/v2: {_refractiveIndex2:F5}/{_abbeNumber2:F1}");
                    break;
                    
                case "GB":
                case "GB/T":
                    annotations.Add($"双胶合透镜：{Material}/{Material2}");
                    annotations.Add($"直径：{_diameter:F1}mm 厚度：{_centerThickness1:F1}+{_centerThickness2:F1}mm");
                    annotations.Add($"曲率半径：R1={R1:F1} R2={R2:F1} R3={R3:F1}");
                    annotations.Add($"第一片：n={RefractiveIndex:F5} v={AbbeNumber:F1}");
                    annotations.Add($"第二片：n={_refractiveIndex2:F5} v={_abbeNumber2:F1}");
                    break;
            }
            
            // 添加表面质量和镀膜信息
            annotations.AddRange(base.GetDrawingAnnotations(standard));
            
            return annotations;
        }

        #endregion

        #region 缺失的抽象方法实现

        /// <summary>
        /// 旋转双胶合透镜
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
            _r3 *= scaleX;
            _centerThickness1 *= scaleX;
            _centerThickness2 *= scaleX;
            
            UpdateBounding();
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new DoubletLens();
        }

        #endregion
    }
}