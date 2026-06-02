using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using LitMath;
using lcdb;
using lcdb.Annotation;

using lcdb.Transaction;
using OtoCAD.OpticEntity;
using Vector2 = LitMath.Vector2;
using Vector3 = LitMath.Vector3;

namespace OtoCAD.OpticEntity
{
    /// <summary>
    /// 增强的光学元件类
    /// 基于现有Element类，添加光学特性支持
    /// </summary>
    public class EnhancedElement : Element
    {
        #region 现有Element属性保持兼容性

        /// <summary>
        /// 第一个表面
        /// </summary>
        [Browsable(false)]
        public new IElementSurface Surface1 { get; set; } = new SurfaceStandard();

        /// <summary>
        /// 第二个表面
        /// </summary>
        [Browsable(false)]
        public new IElementSurface Surface2 { get; set; } = new SurfaceStandard();

        /// <summary>
        /// Zemax文件路径
        /// </summary>
        [Category("Import")]
        [DisplayName("Zemax文件路径")]
        [Description("Zemax透镜数据文件路径")]
        public new string ZemaxFilePath { get; set; }

        /// <summary>
        /// 自动添加镀膜标签
        /// </summary>
        [Category("Annotation")]
        [DisplayName("自动镀膜标签")]
        [Description("是否自动添加镀膜标签")]
        public new bool AutoCoatingLabel { get; set; } = false;

        /// <summary>
        /// 透镜轮廓
        /// </summary>
        [Browsable(false)]
        public new LensOutline lensOutline { get; set; }

        #endregion

        #region 增强的光学属性

        /// <summary>
        /// 元件类型
        /// </summary>
        [Category("Optical Properties")]
        [DisplayName("元件类型")]
        [Description("光学元件的类型")]
        public ElementType ElementType { get; set; } = ElementType.Lens;

        /// <summary>
        /// 中心厚度
        /// </summary>
        [Category("Optical Properties")]
        [DisplayName("中心厚度")]
        [Description("透镜中心厚度 (mm)")]
        public new double CenterThickness
        {
            get => Surface1?.Thickness ?? 0;
            set
            {
                if (Surface1 != null)
                    Surface1.Thickness = value;
            }
        }

        /// <summary>
        /// 第一表面曲率半径
        /// </summary>
        [Category("Optical Properties")]
        [DisplayName("第一表面半径")]
        [Description("第一表面曲率半径 (mm)")]
        public double Surface1Radius
        {
            get => Surface1?.Radius ?? 0;
            set
            {
                if (Surface1 != null)
                    Surface1.Radius = value;
            }
        }

        /// <summary>
        /// 第二表面曲率半径
        /// </summary>
        [Category("Optical Properties")]
        [DisplayName("第二表面半径")]
        [Description("第二表面曲率半径 (mm)")]
        public double Surface2Radius
        {
            get => Surface2?.Radius ?? 0;
            set
            {
                if (Surface2 != null)
                    Surface2.Radius = value;
            }
        }

        /// <summary>
        /// 第一表面半径
        /// </summary>
        [Category("Optical Properties")]
        [DisplayName("第一表面半径")]
        [Description("第一表面半径 (mm)")]
        public double Surface1SemiDiameter
        {
            get => Surface1?.SemiDiameter ?? 0;
            set
            {
                if (Surface1 != null)
                    Surface1.SemiDiameter = value;
            }
        }

        /// <summary>
        /// 第二表面半径
        /// </summary>
        [Category("Optical Properties")]
        [DisplayName("第二表面半径")]
        [Description("第二表面半径 (mm)")]
        public double Surface2SemiDiameter
        {
            get => Surface2?.SemiDiameter ?? 0;
            set
            {
                if (Surface2 != null)
                    Surface2.SemiDiameter = value;
            }
        }

        /// <summary>
        /// 设计波长
        /// </summary>
        [Category("Optical Properties")]
        [DisplayName("设计波长")]
        [Description("设计波长 (nm)")]
        public double DesignWavelength { get; set; } = 587.56;

        /// <summary>
        /// 是否为胶合透镜
        /// </summary>
        [Category("Optical Properties")]
        [DisplayName("胶合透镜")]
        [Description("是否为胶合透镜")]
        public bool IsCemented { get; set; } = false;

        /// <summary>
        /// 胶合材料
        /// </summary>
        [Category("Optical Properties")]
        [DisplayName("胶合材料")]
        [Description("胶合材料类型")]
        public string CementMaterial { get; set; } = "UV胶";

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="database">数据库实例</param>
        public EnhancedElement(Database database) : base(database)
        {
            InitializeElement();
        }

        /// <summary>
        /// 无参构造函数
        /// </summary>
        public EnhancedElement() : base()
        {
            InitializeElement();
        }

        /// <summary>
        /// 初始化元件
        /// </summary>
        private void InitializeElement()
        {
            ElementType = ElementType.Lens;
            lensOutline = new LensOutline(database);
            BlockSelect = true;
            
            // 设置表面数据库
          //  Surface1?.SetDataBase(database);
           // Surface2?.SetDataBase(database);
            
            // 初始化表面集合
            if (Surface1 is IOpticalSurface opticalSurface1)
                AddSurface(opticalSurface1);
            if (Surface2 is IOpticalSurface opticalSurface2)
                AddSurface(opticalSurface2);
        }

        #endregion

        #region 基类重写

        /// <summary>
        /// 设置数据库
        /// </summary>
        /// <param name="database">数据库实例</param>
        public override void SetDataBase(Database database)
        {
            base.SetDataBase(database);
            lensOutline?.SetDataBase(database);
       //     Surface1?.SetDataBase(database);
        //    Surface2?.SetDataBase(database);
        }

        /// <summary>
        /// 获取块名称
        /// </summary>
        public override string BlockName => "ModelSpace";

        /// <summary>
        /// 生成光学几何（事务版本）
        /// </summary>
        /// <param name="transaction">事务对象</param>
        protected override void GenerateEntitiesWithTransaction(IEntityTransaction transaction)
        {
            if (lensOutline == null)
                return;

            try
            {
                // 配置表面参数
                ConfigureSurfaces();

                // 生成表面实体
                Surface1?.GenEntity();
                Surface2?.GenEntity();

                // 计算透镜参数
                lensOutline.CalculateLensParameters();

                // 生成透镜轮廓
                GenerateLensOutlineWithTransaction(transaction);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] EnhancedElement.GenerateOpticalGeometry - 异常: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 生成光学几何（传统版本）
        /// </summary>
        protected override void GenerateEntitiesLegacy()
        {
            if (lensOutline == null)
                return;

            try
            {
                // 配置表面参数
                ConfigureSurfaces();

                // 生成表面实体
                Surface1?.GenEntity();
                Surface2?.GenEntity();

                // 计算透镜参数
                lensOutline.CalculateLensParameters();

                // 生成透镜轮廓
                lensOutline.GenEntity();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] EnhancedElement.GenerateOpticalGeometryLegacy - 异常: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 光学计算增强

        /// <summary>
        /// 获取焦距
        /// </summary>
        /// <returns>焦距值</returns>
        public override double GetFocalLength()
        {
            // 厚透镜焦距计算
            if (Math.Abs(RefractiveIndex - 1.0) < 1e-6)
                return double.PositiveInfinity;

            double r1 = Surface1Radius;
            double r2 = Surface2Radius;
            double t = CenterThickness;
            double n = RefractiveIndex;

            // 厚透镜公式
            double phi1 = (n - 1) / r1;
            double phi2 = (1 - n) / r2;
            double phi = phi1 + phi2 - (t * phi1 * phi2) / n;

            return Math.Abs(phi) > 1e-6 ? 1.0 / phi : double.PositiveInfinity;
        }

        /// <summary>
        /// 获取有效焦距
        /// </summary>
        /// <returns>有效焦距值</returns>
        public override double GetEffectiveFocalLength()
        {
            return GetFocalLength();
        }

        /// <summary>
        /// 获取后焦距
        /// </summary>
        /// <returns>后焦距值</returns>
        public override double GetBackFocalLength()
        {
            double efl = GetEffectiveFocalLength();
            double r2 = Surface2Radius;
            double t = CenterThickness;
            double n = RefractiveIndex;

            if (Math.Abs(efl) < 1e-6)
                return efl;

            // 后焦距计算
            double phi1 = (n - 1) / Surface1Radius;
            double bfl = efl * (1 - t * phi1 / n);

            return bfl;
        }

        /// <summary>
        /// 计算光学性能
        /// </summary>
        /// <returns>光学性能数据</returns>
        public override OpticalPerformanceData CalculatePerformance()
        {
            var performance = base.CalculatePerformance();

            // 计算色差
            if (AbbeNumber > 0)
            {
                double vd = AbbeNumber;
                double primaryChroma = performance.EffectiveFocalLength / vd;
                performance.AberrationCoefficients["ChromaticAberration"] = primaryChroma;
            }

            // 计算球差
            double sphericalAberration = CalculateSphericalAberration();
            performance.AberrationCoefficients["SphericalAberration"] = sphericalAberration;

            // 计算彗差
            double coma = CalculateComa();
            performance.AberrationCoefficients["Coma"] = coma;

            return performance;
        }

        /// <summary>
        /// 验证光学参数
        /// </summary>
        /// <returns>验证结果</returns>
        public override ValidationResult ValidateOpticalParameters()
        {
            var result = base.ValidateOpticalParameters();

            // 验证表面半径
            if (Math.Abs(Surface1Radius) < 1e-6 && Math.Abs(Surface2Radius) < 1e-6)
            {
                result.ErrorMessages.Add("至少有一个表面必须有曲率");
            }

            // 验证中心厚度
            if (CenterThickness <= 0)
            {
                result.ErrorMessages.Add("中心厚度必须大于0");
            }

            // 验证表面半径
            if (Surface1SemiDiameter <= 0 || Surface2SemiDiameter <= 0)
            {
                result.ErrorMessages.Add("表面半径必须大于0");
            }

            // 验证设计波长
            if (DesignWavelength <= 0)
            {
                result.ErrorMessages.Add("设计波长必须大于0");
            }

            return result;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 配置表面参数
        /// </summary>
        private void ConfigureSurfaces()
        {
            // 设置表面基点
            Surface1.BasePoint1 = new LitMath.Vector2(OriginalX, OriginalY);
            Surface2.BasePoint1 = new LitMath.Vector2(OriginalX + CenterThickness, OriginalY);

            // 配置透镜轮廓
            lensOutline.Surface1 = Surface1;
            lensOutline.Surface2 = Surface2;
            lensOutline.BasePoint1 = new LitMath.Vector2(OriginalX, OriginalY);
        }

        /// <summary>
        /// 生成透镜轮廓（事务版本）
        /// </summary>
        /// <param name="transaction">事务对象</param>
        private void GenerateLensOutlineWithTransaction(IEntityTransaction transaction)
        {
            var originalX = lensOutline.BasePoint1.X;
            var originalY = lensOutline.BasePoint1.Y;
            var maxDiameter = Math.Max(lensOutline.RealDiameter, lensOutline.RealDiameter2);
            var minDiameter = Math.Min(lensOutline.RealDiameter, lensOutline.RealDiameter2);
            var vlineX = (int)lensOutline.SingleLensType == 1 ? lensOutline.Surface1.SagPoint.X : lensOutline.Surface2.SagPoint.X;

            // 曲面轮廓
            if (lensOutline.Surface1.ProfileEntity != null)
                AppendEntityWithTransaction(transaction, lensOutline.Surface1.ProfileEntity);
            if (lensOutline.Surface2.ProfileEntity != null)
                AppendEntityWithTransaction(transaction, lensOutline.Surface2.ProfileEntity);

            // 顶部水平线
            var topHLine = new Line();
            topHLine.startPoint = new Vector2(lensOutline.Surface1.SagPoint.X, maxDiameter + originalY);
            topHLine.endPoint = new Vector2(lensOutline.Surface2.SagPoint.X, maxDiameter + originalY);
            AppendEntityWithTransaction(transaction, topHLine);

            // 底部水平线
            var bottomHLine = new Line();
            bottomHLine.startPoint = new Vector2(lensOutline.Surface1.SagPoint.X, -maxDiameter + originalY);
            bottomHLine.endPoint = new Vector2(lensOutline.Surface2.SagPoint.X, -maxDiameter + originalY);
            AppendEntityWithTransaction(transaction, bottomHLine);

            // 垂直线（如果直径不等）
            if ((int)lensOutline.SingleLensType != 0)
            {
                var topVLine = new Line();
                topVLine.startPoint = new Vector2(vlineX, minDiameter + originalY);
                topVLine.endPoint = new Vector2(vlineX, maxDiameter + originalY);
                AppendEntityWithTransaction(transaction, topVLine);

                var bottomVLine = new Line();
                bottomVLine.startPoint = new Vector2(vlineX, -minDiameter + originalY);
                bottomVLine.endPoint = new Vector2(vlineX, -maxDiameter + originalY);
                AppendEntityWithTransaction(transaction, bottomVLine);
            }

            // 中心线
            var centerLine = new Line();
            centerLine.startPoint = new Vector2(lensOutline.Surface1.BasePoint1.X - OpticConstants.Lens.DEFAULT_CENTER_LINE_OFFSET, originalY);
            centerLine.endPoint = new Vector2(lensOutline.Surface2.BasePoint1.X + OpticConstants.Lens.DEFAULT_CENTER_LINE_OFFSET, originalY);
            centerLine.lineType = LineType.DashDot;
            AppendEntityWithTransaction(transaction, centerLine);

            // 设置标注点
            lensOutline.SetDimensionPoints();
        }

        /// <summary>
        /// 计算球差
        /// </summary>
        /// <returns>球差值</returns>
        private double CalculateSphericalAberration()
        {
            // 简化的球差计算
            double r1 = Surface1Radius;
            double r2 = Surface2Radius;
            double n = RefractiveIndex;

            if (Math.Abs(r1) < 1e-6 || Math.Abs(r2) < 1e-6)
                return 0;

            // 球差系数
            double sa = (n - 1) * (1 / Math.Pow(r1, 3) - 1 / Math.Pow(r2, 3)) / (8 * Math.Pow(n, 2));
            return sa;
        }

        /// <summary>
        /// 计算彗差
        /// </summary>
        /// <returns>彗差值</returns>
        private double CalculateComa()
        {
            // 简化的彗差计算
            double r1 = Surface1Radius;
            double r2 = Surface2Radius;
            double n = RefractiveIndex;

            if (Math.Abs(r1) < 1e-6 || Math.Abs(r2) < 1e-6)
                return 0;

            // 彗差系数
            double coma = (n - 1) * (1 / Math.Pow(r1, 2) - 1 / Math.Pow(r2, 2)) / (2 * n);
            return coma;
        }

        #endregion
    }

    /// <summary>
    /// 元件类型枚举
    /// </summary>
    public enum ElementType
    {
        /// <summary>
        /// 透镜
        /// </summary>
        Lens,

        /// <summary>
        /// 反射镜
        /// </summary>
        Mirror,

        /// <summary>
        /// 棱镜
        /// </summary>
        Prism,

        /// <summary>
        /// 滤镜
        /// </summary>
        Filter,

        /// <summary>
        /// 分束镜
        /// </summary>
        BeamSplitter,

        /// <summary>
        /// 窗口
        /// </summary>
        Window
    }
}