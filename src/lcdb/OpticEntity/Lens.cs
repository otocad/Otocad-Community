using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using LitMath;
using netDxf;
using netDxf.Entities;
using lcdb;
using lcdb.Annotation;
using lcdb.Transaction;
using Vector2 = LitMath.Vector2;

namespace OtoCAD.OpticEntity
{
    /// <summary>
    /// 透镜系统类
    /// 用于管理多个透镜元件组成的光学系统
    /// </summary>
    public class Lens : Element
    {
        #region 透镜系统属性

        /// <summary>
        /// 透镜类型
        /// </summary>
        [Category("Lens System")]
        [DisplayName("透镜类型")]
        [Description("透镜系统的类型")]
        public LensType LensType { get; set; } = LensType.Single;

        /// <summary>
        /// 元件集合
        /// </summary>
        [Category("Lens System")]
        [DisplayName("元件集合")]
        [Description("透镜系统中的元件集合")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public List<EnhancedElement> Elements { get; set; } = new List<EnhancedElement>();

        /// <summary>
        /// 系统有效焦距
        /// </summary>
        [Category("System Properties")]
        [DisplayName("有效焦距")]
        [Description("系统有效焦距 (mm)")]
        public double EffectiveFocalLength { get; set; } = 50.0;

        /// <summary>
        /// 系统后焦距
        /// </summary>
        [Category("System Properties")]
        [DisplayName("后焦距")]
        [Description("系统后焦距 (mm)")]
        public double BackFocalLength { get; set; } = 45.0;

        /// <summary>
        /// 系统总长度
        /// </summary>
        [Category("System Properties")]
        [DisplayName("总长度")]
        [Description("系统总长度 (mm)")]
        public double TotalLength { get; set; } = 25.0;

        /// <summary>
        /// 系统数值孔径
        /// </summary>
        [Category("System Properties")]
        [DisplayName("数值孔径")]
        [Description("系统数值孔径")]
        public double SystemNumericalAperture { get; set; } = 0.25;

        /// <summary>
        /// 系统F数
        /// </summary>
        [Category("System Properties")]
        [DisplayName("F数")]
        [Description("系统F数")]
        public double SystemFNumber { get; set; } = 2.0;

        /// <summary>
        /// 工作距离
        /// </summary>
        [Category("System Properties")]
        [DisplayName("工作距离")]
        [Description("工作距离 (mm)")]
        public double WorkingDistance { get; set; } = 10.0;

        #endregion

        #region 光线追迹属性

        /// <summary>
        /// 光线追迹数据
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public List<RayTrace> RayTraces { get; set; } = new List<RayTrace>();

        /// <summary>
        /// 启用光线追迹
        /// </summary>
        [Category("Ray Tracing")]
        [DisplayName("启用光线追迹")]
        [Description("是否启用光线追迹显示")]
        public bool EnableRayTracing { get; set; } = false;

        /// <summary>
        /// 光线数量
        /// </summary>
        [Category("Ray Tracing")]
        [DisplayName("光线数量")]
        [Description("光线追迹的光线数量")]
        public int RayCount { get; set; } = 5;

        /// <summary>
        /// 追迹波长
        /// </summary>
        [Category("Ray Tracing")]
        [DisplayName("追迹波长")]
        [Description("光线追迹的波长 (nm)")]
        public double TracingWavelength { get; set; } = 587.56;

        #endregion

        #region 优化属性

        /// <summary>
        /// 自动优化
        /// </summary>
        [Category("Optimization")]
        [DisplayName("自动优化")]
        [Description("是否启用自动优化")]
        public bool AutoOptimize { get; set; } = false;

        /// <summary>
        /// 优化目标
        /// </summary>
        [Category("Optimization")]
        [DisplayName("优化目标")]
        [Description("优化的目标参数")]
        public OptimizationTarget OptimizationTarget { get; set; } = OptimizationTarget.SpotSize;

        /// <summary>
        /// 优化权重
        /// </summary>
        [Category("Optimization")]
        [DisplayName("优化权重")]
        [Description("各项优化目标的权重")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public Dictionary<string, double> OptimizationWeights { get; set; } = new Dictionary<string, double>();

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="database">数据库实例</param>
        public Lens(Database database) : base(database)
        {
            InitializeLens();
        }

        /// <summary>
        /// 无参构造函数
        /// </summary>
        public Lens() : base()
        {
            InitializeLens();
        }

        /// <summary>
        /// 初始化透镜系统
        /// </summary>
        private void InitializeLens()
        {
            LensType = LensType.Single;
            Elements = new List<EnhancedElement>();
            RayTraces = new List<RayTrace>();
            OptimizationWeights = new Dictionary<string, double>
            {
                { "SpotSize", 1.0 },
                { "WavefrontError", 0.5 },
                { "Distortion", 0.3 },
                { "ChromaticAberration", 0.8 }
            };
            
            // 设置默认系统参数
            EffectiveFocalLength = 50.0;
            BackFocalLength = 45.0;
            TotalLength = 25.0;
            SystemNumericalAperture = 0.25;
            SystemFNumber = 2.0;
            WorkingDistance = 10.0;
        }

        #endregion

        #region 基类重写

        /// <summary>
        /// 获取块名称
        /// </summary>
        public override string BlockName => "LensSystem";

        /// <summary>
        /// 设置数据库
        /// </summary>
        /// <param name="database">数据库实例</param>
        public override void SetDataBase(Database database)
        {
            base.SetDataBase(database);
            
            // 设置所有元件的数据库
            foreach (var element in Elements)
            {
                element.SetDataBase(database);
            }
        }

        ///// <summary>
        ///// 生成光学几何（事务版本）
        ///// </summary>
        ///// <param name="transaction">事务对象</param>
        //protected override void GenerateOpticalGeometry(IEntityTransaction transaction)
        //{
        //    try
        //    {
        //        // 重新计算系统参数
        //        CalculateSystemParameters();

        //        // 生成所有元件
        //        GenerateElements(transaction);

        //        // 生成光线追迹
        //        if (EnableRayTracing)
        //        {
        //            GenerateRayTracing(transaction);
        //        }

        //        // 生成系统标注
        //        GenerateSystemAnnotations(transaction);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"[ERROR] Lens.GenerateOpticalGeometry - 异常: {ex.Message}");
        //        throw;
        //    }
        //}

        /// <summary>
        /// 生成光学几何（事务版本）
        /// </summary>
        /// <param name="transaction">事务对象</param>
        protected override void GenerateEntitiesWithTransaction(IEntityTransaction transaction)
        {
            try
            {
                // 重新计算系统参数
                CalculateSystemParameters();

                // 生成所有元件
                GenerateElements(transaction);

                // 生成系统标注
                GenerateSystemAnnotations(transaction);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Lens.GenerateOpticalGeometry - 异常: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 生成光学几何（传统版本）
        /// </summary>
        protected override void GenerateEntitiesLegacy()
        {
            try
            {
                // 重新计算系统参数
                CalculateSystemParameters();

                // 生成所有元件
                GenerateElementsLegacy();

                // 生成光线追迹
                if (EnableRayTracing)
                {
                    // GenerateRayTracingLegacy();
                }

                // 生成系统标注
                GenerateSystemAnnotationsLegacy();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Lens.GenerateOpticalGeometryLegacy - 异常: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 元件管理

        /// <summary>
        /// 添加元件
        /// </summary>
        /// <param name="element">要添加的元件</param>
        public void AddElement(EnhancedElement element)
        {
            if (element != null)
            {
                Elements.Add(element);
                element.SetDataBase(database);
                element.OnDataUpdate += () => DataUpdate();
                
                // 重新计算系统参数
                CalculateSystemParameters();
            }
        }

        /// <summary>
        /// 移除元件
        /// </summary>
        /// <param name="element">要移除的元件</param>
        public void RemoveElement(EnhancedElement element)
        {
            if (element != null && Elements.Contains(element))
            {
                Elements.Remove(element);
                element.OnDataUpdate -= () => DataUpdate();
                
                // 重新计算系统参数
                CalculateSystemParameters();
            }
        }

        /// <summary>
        /// 清空所有元件
        /// </summary>
        public void ClearElements()
        {
            foreach (var element in Elements)
            {
                element.OnDataUpdate -= () => DataUpdate();
            }
            Elements.Clear();
            
            // 重新计算系统参数
            CalculateSystemParameters();
        }

        /// <summary>
        /// 获取元件数量
        /// </summary>
        /// <returns>元件数量</returns>
        public int GetElementCount()
        {
            return Elements.Count;
        }

        #endregion

        #region 光学计算

        /// <summary>
        /// 计算系统参数
        /// </summary>
        private void CalculateSystemParameters()
        {
            if (Elements.Count == 0)
            {
                EffectiveFocalLength = 50.0;
                BackFocalLength = 45.0;
                TotalLength = 0.0;
                SystemNumericalAperture = 0.25;
                SystemFNumber = 2.0;
                return;
            }

            // 计算总长度
            TotalLength = CalculateTotalLength();

            // 计算有效焦距
            EffectiveFocalLength = CalculateEffectiveFocalLength();

            // 计算后焦距
            BackFocalLength = CalculateBackFocalLength();

            // 计算系统数值孔径
            SystemNumericalAperture = CalculateSystemNumericalAperture();

            // 计算系统F数
            SystemFNumber = Math.Abs(EffectiveFocalLength) / (2 * SemiDiameter);
        }

        /// <summary>
        /// 计算总长度
        /// </summary>
        /// <returns>总长度</returns>
        private double CalculateTotalLength()
        {
            if (Elements.Count == 0)
                return 0.0;

            double totalLength = 0.0;
            foreach (var element in Elements)
            {
                totalLength += element.CenterThickness;
            }

            return totalLength;
        }

        /// <summary>
        /// 获取有效焦距
        /// </summary>
        /// <returns>有效焦距值</returns>
        public override double GetEffectiveFocalLength()
        {
            return CalculateEffectiveFocalLength();
        }

        /// <summary>
        /// 计算有效焦距
        /// </summary>
        /// <returns>有效焦距值</returns>
        private double CalculateEffectiveFocalLength()
        {
            if (Elements.Count == 0)
                return 50.0;

            if (Elements.Count == 1)
                return Elements[0].GetEffectiveFocalLength();

            // 多元件系统焦距计算
            double totalPower = 0.0;
            // lastThickness预留用于元件间距计算

            for (int i = 0; i < Elements.Count; i++)
            {
                var element = Elements[i];
                double power = 1.0 / element.GetEffectiveFocalLength();
                
                if (i > 0)
                {
                    // 考虑元件间距
                    double spacing = element.OriginalX - Elements[i - 1].OriginalX - Elements[i - 1].CenterThickness;
                    power *= (1 - spacing * totalPower);
                }

                totalPower += power;
            }

            return Math.Abs(totalPower) > 1e-6 ? 1.0 / totalPower : double.PositiveInfinity;
        }

        /// <summary>
        /// 获取后焦距
        /// </summary>
        /// <returns>后焦距值</returns>
        public override double GetBackFocalLength()
        {
            return CalculateBackFocalLength();
        }

        /// <summary>
        /// 计算后焦距
        /// </summary>
        /// <returns>后焦距值</returns>
        private double CalculateBackFocalLength()
        {
            if (Elements.Count == 0)
                return 45.0;

            double efl = GetEffectiveFocalLength();
            
            // 简化的后焦距计算
            return efl * 0.9; // 近似值
        }

        /// <summary>
        /// 计算系统数值孔径
        /// </summary>
        /// <returns>系统数值孔径</returns>
        private double CalculateSystemNumericalAperture()
        {
            if (Elements.Count == 0)
                return 0.25;

            // 找到限制孔径的元件
            double minAperture = double.MaxValue;
            foreach (var element in Elements)
            {
                double aperture = element.SemiDiameter;
                if (aperture < minAperture)
                    minAperture = aperture;
            }

            // 计算数值孔径
            double efl = GetEffectiveFocalLength();
            return Math.Abs(efl) > 1e-6 ? minAperture / (2 * Math.Abs(efl)) : 0.25;
        }

        /// <summary>
        /// 验证光学参数
        /// </summary>
        /// <returns>验证结果</returns>
        public override ValidationResult ValidateOpticalParameters()
        {
            var result = base.ValidateOpticalParameters();

            // 验证元件数量
            if (Elements.Count == 0)
            {
                result.WarningMessages.Add("透镜系统中没有元件");
            }

            // 验证系统参数
            if (EffectiveFocalLength <= 0)
            {
                result.ErrorMessages.Add("有效焦距必须大于0");
            }

            if (SystemNumericalAperture <= 0 || SystemNumericalAperture >= 1)
            {
                result.ErrorMessages.Add("数值孔径必须在0到1之间");
            }

            if (SystemFNumber <= 0)
            {
                result.ErrorMessages.Add("F数必须大于0");
            }

            // 验证每个元件
            foreach (var element in Elements)
            {
                var elementResult = element.ValidateOpticalParameters();
                if (!elementResult.IsValid)
                {
                    result.IsValid = false;
                    result.ErrorMessages.AddRange(elementResult.ErrorMessages);
                }
                result.WarningMessages.AddRange(elementResult.WarningMessages);
            }

            return result;
        }

        /// <summary>
        /// 计算光学性能
        /// </summary>
        /// <returns>光学性能数据</returns>
        public override OpticalPerformanceData CalculatePerformance()
        {
            var performance = new OpticalPerformanceData();

            // 计算基本参数
            performance.EffectiveFocalLength = GetEffectiveFocalLength();
            performance.BackFocalLength = GetBackFocalLength();
            var opticalCenter = GetOpticalCenter();
            performance.PrincipalPoint = new LitMath.Vector3(opticalCenter.X, opticalCenter.Y, 0);
            performance.NodalPoint = new LitMath.Vector3(opticalCenter.X, opticalCenter.Y, 0);
            performance.NumericalAperture = SystemNumericalAperture;
            performance.FNumber = SystemFNumber;

            // 计算系统像差
            performance.AberrationCoefficients = CalculateSystemAberrations();

            return performance;
        }

        /// <summary>
        /// 计算系统像差
        /// </summary>
        /// <returns>像差系数字典</returns>
        private Dictionary<string, double> CalculateSystemAberrations()
        {
            var aberrations = new Dictionary<string, double>();

            if (Elements.Count == 0)
            {
                aberrations["SphericalAberration"] = 0.0;
                aberrations["Coma"] = 0.0;
                aberrations["Astigmatism"] = 0.0;
                aberrations["FieldCurvature"] = 0.0;
                aberrations["Distortion"] = 0.0;
                aberrations["ChromaticAberration"] = 0.0;
                return aberrations;
            }

            // 累积各元件的像差
            double totalSpherical = 0.0;
            double totalComa = 0.0;
            double totalChromatic = 0.0;

            foreach (var element in Elements)
            {
                var elementPerformance = element.CalculatePerformance();
                
                if (elementPerformance.AberrationCoefficients.ContainsKey("SphericalAberration"))
                    totalSpherical += elementPerformance.AberrationCoefficients["SphericalAberration"];
                
                if (elementPerformance.AberrationCoefficients.ContainsKey("Coma"))
                    totalComa += elementPerformance.AberrationCoefficients["Coma"];
                
                if (elementPerformance.AberrationCoefficients.ContainsKey("ChromaticAberration"))
                    totalChromatic += elementPerformance.AberrationCoefficients["ChromaticAberration"];
            }

            aberrations["SphericalAberration"] = totalSpherical;
            aberrations["Coma"] = totalComa;
            aberrations["Astigmatism"] = totalSpherical * 0.5; // 简化
            aberrations["FieldCurvature"] = totalSpherical * 0.3; // 简化
            aberrations["Distortion"] = totalComa * 0.1; // 简化
            aberrations["ChromaticAberration"] = totalChromatic;

            return aberrations;
        }

        #endregion

        #region 生成方法

        /// <summary>
        /// 生成元件（事务版本）
        /// </summary>
        /// <param name="transaction">事务对象</param>
        private void GenerateElements(IEntityTransaction transaction)
        {
            double currentX = OriginalX;
            
            foreach (var element in Elements)
            {
                // 设置元件位置
                element.OriginalX = currentX;
                element.OriginalY = OriginalY;
                
                // 生成元件
                element.GenEntityWithTransaction();
                
                // 更新下一个元件的位置
                currentX += element.CenterThickness + GetElementSpacing();
            }
        }

        /// <summary>
        /// 生成元件（传统版本）
        /// </summary>
        private void GenerateElementsLegacy()
        {
            double currentX = OriginalX;
            
            foreach (var element in Elements)
            {
                // 设置元件位置
                element.OriginalX = currentX;
                element.OriginalY = OriginalY;
                
                // 生成元件
                element.GenEntity();
                
                // 更新下一个元件的位置
                currentX += element.CenterThickness + GetElementSpacing();
            }
        }

        ///// <summary>
        ///// 生成光线追迹（事务版本）
        ///// </summary>
        ///// <param name="transaction">事务对象</param>
        //private void GenerateRayTracing(IEntityTransaction transaction)
        //{
        //    if (!EnableRayTracing || Elements.Count == 0)
        //        return;

        //    // 清空现有光线追迹
        //    RayTraces.Clear();

        //    // 生成光线追迹数据
        //    GenerateRayTraceData();

        //    // 绘制光线追迹
        //    foreach (var rayTrace in RayTraces)
        //    {
        //        DrawRayTrace(transaction, rayTrace);
        //    }
        //}

        ///// <summary>
        ///// 生成光线追迹（传统版本）
        ///// </summary>
        //private void GenerateRayTracingLegacy()
        //{
        //    if (!EnableRayTracing || Elements.Count == 0)
        //        return;

        //    // 清空现有光线追迹
        //    RayTraces.Clear();

        //    // 生成光线追迹数据
        //    GenerateRayTraceData();

        //    // 绘制光线追迹
        //    foreach (var rayTrace in RayTraces)
        //    {
        //        DrawRayTraceLegacy(rayTrace);
        //    }
        //}

        /// <summary>
        /// 生成光线追迹数据
        /// </summary>
        private void GenerateRayTraceData()
        {
            double maxSemiDiameter = Elements.Max(e => e.SemiDiameter);
            
            for (int i = 0; i < RayCount; i++)
            {
                double height = maxSemiDiameter * (2.0 * i / (RayCount - 1) - 1.0);
                
                var startPoint = new LitMath.Vector3(OriginalX - 50, OriginalY + height, 0);
                var direction = new LitMath.Vector3(1, 0, 0);
                var rayTrace = new RayTrace(startPoint, direction, TracingWavelength);

                // 计算光线追迹路径
                CalculateRayPath(rayTrace);
                
                RayTraces.Add(rayTrace);
            }
        }

        /// <summary>
        /// 计算光线路径
        /// </summary>
        /// <param name="rayTrace">光线追迹对象</param>
        private void CalculateRayPath(RayTrace rayTrace)
        {
            var currentPoint = new Vector2((float)rayTrace.Origin.X, (float)rayTrace.Origin.Y);
            var currentDirection = new Vector2((float)rayTrace.Direction.X, (float)rayTrace.Direction.Y);
            
            // Start tracking ray path

            foreach (var element in Elements)
            {
                // 计算光线与元件的交点
                var intersectionPoint = CalculateIntersection(currentPoint, currentDirection, element);
                // Add segment to path
                var intersectionStart3D = new LitMath.Vector3(currentPoint.X, currentPoint.Y, 0);
                var intersectionEnd3D = new LitMath.Vector3(intersectionPoint.X, intersectionPoint.Y, 0);
                rayTrace.AddSegment(intersectionStart3D, intersectionEnd3D, 1.0);

                // 计算折射后的方向
                currentDirection = CalculateRefraction(currentDirection, element);
                currentPoint = intersectionPoint;
            }

            // 添加最终点
            var finalPoint = new Vector2(currentPoint.X + 50, currentPoint.Y + currentDirection.Y * 50);
            var finalStart3D = new LitMath.Vector3(currentPoint.X, currentPoint.Y, 0);
            var finalEnd3D = new LitMath.Vector3(finalPoint.X, finalPoint.Y, 0);
            rayTrace.AddSegment(finalStart3D, finalEnd3D, 1.0);
        }

        /// <summary>
        /// 计算光线与元件的交点
        /// </summary>
        /// <param name="rayPoint">光线点</param>
        /// <param name="rayDirection">光线方向</param>
        /// <param name="element">元件</param>
        /// <returns>交点</returns>
        private Vector2 CalculateIntersection(Vector2 rayPoint, Vector2 rayDirection, EnhancedElement element)
        {
            // 简化的交点计算
            return new Vector2(element.OriginalX, rayPoint.Y);
        }

        /// <summary>
        /// 计算折射方向
        /// </summary>
        /// <param name="incidentDirection">入射方向</param>
        /// <param name="element">元件</param>
        /// <returns>折射方向</returns>
        private Vector2 CalculateRefraction(Vector2 incidentDirection, EnhancedElement element)
        {
            // 简化的折射计算
            double n = element.RefractiveIndex;
            return new Vector2(incidentDirection.X / n, incidentDirection.Y / n);
        }

        ///// <summary>
        ///// 绘制光线追迹（事务版本）
        ///// </summary>
        ///// <param name="transaction">事务对象</param>
        ///// <param name="rayTrace">光线追迹对象</param>
        //private void DrawRayTrace(IEntityTransaction transaction, RayTrace rayTrace)
        //{
        //    for (int i = 0; i < rayTrace.TracePoints.Count - 1; i++)
        //    {
        //        var line = new OLine(
        //            new Line(rayTrace.TracePoints[i], rayTrace.TracePoints[i + 1])
        //        );
        //        line.color = lcdb.Colors.Color.FromColor(System.Drawing.Color.Red);
        //        AppendEntityWithTransaction(transaction, line);
        //    }
        //}

        ///// <summary>
        ///// 绘制光线追迹（传统版本）
        ///// </summary>
        ///// <param name="rayTrace">光线追迹对象</param>
        //private void DrawRayTraceLegacy(RayTrace rayTrace)
        //{
        //    for (int i = 0; i < rayTrace.TracePoints.Count - 1; i++)
        //    {
        //        var line = new OLine(
        //            new Line(rayTrace.TracePoints[i], rayTrace.TracePoints[i + 1])
        //        );
        //        line.color = lcdb.Colors.Color.FromColor(System.Drawing.Color.Red);
        //        AppendEntity(line);
        //    }
        //}

        /// <summary>
        /// 生成系统标注（事务版本）
        /// </summary>
        /// <param name="transaction">事务对象</param>
        private void GenerateSystemAnnotations(IEntityTransaction transaction)
        {
            // 生成系统总长度标注
            var totalLengthLine = new lcdb.Line();
            totalLengthLine.startPoint = new Vector2(OriginalX, OriginalY - SemiDiameter - 10);
            totalLengthLine.endPoint = new Vector2(OriginalX + TotalLength, OriginalY - SemiDiameter - 10);
            AppendEntityWithTransaction(transaction, totalLengthLine);

            // 生成系统标签
            // var systemLabel = new Text
            // {
            //     Value = $"EFL={EffectiveFocalLength:F2}mm, F/{SystemFNumber:F1}",
            //     Position = new Vector3(OriginalX + TotalLength / 2, OriginalY + SemiDiameter + 15, 0),
            //     Height = 3.0,
            //     Alignment = TextAlignment.MiddleCenter
            // };
            // var systemLabelEntity = new lcdb.Text(systemLabel);
            // AppendEntityWithTransaction(transaction, systemLabelEntity);
        }

        /// <summary>
        /// 生成系统标注（传统版本）
        /// </summary>
        private void GenerateSystemAnnotationsLegacy()
        {
            // 生成系统总长度标注
            var totalLengthLine = new lcdb.Line();
            totalLengthLine.startPoint = new Vector2(OriginalX, OriginalY - SemiDiameter - 10);
            totalLengthLine.endPoint = new Vector2(OriginalX + TotalLength, OriginalY - SemiDiameter - 10);
            AppendEntity(totalLengthLine);

            // 生成系统标签
            // var systemLabel = new Text
            // {
            //     Value = $"EFL={EffectiveFocalLength:F2}mm, F/{SystemFNumber:F1}",
            //     Position = new Vector3(OriginalX + TotalLength / 2, OriginalY + SemiDiameter + 15, 0),
            //     Height = 3.0,
            //     Alignment = TextAlignment.MiddleCenter
            // };
            // var systemLabelEntity = new lcdb.Text(systemLabel);
            // AppendEntity(systemLabelEntity);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取元件间距
        /// </summary>
        /// <returns>元件间距</returns>
        private double GetElementSpacing()
        {
            return 2.0; // 默认2mm间距
        }

        /// <summary>
        /// 获取光学中心
        /// </summary>
        /// <returns>光学中心坐标</returns>
        public override LitMath.Vector2 GetOpticalCenter()
        {
            return new LitMath.Vector2(OriginalX + TotalLength / 2, OriginalY);
        }

        /// <summary>
        /// 获取焦距
        /// </summary>
        /// <returns>焦距值</returns>
        public override double GetFocalLength()
        {
            return GetEffectiveFocalLength();
        }

        #endregion

        #region 优化方法

        /// <summary>
        /// 优化系统
        /// </summary>
        public void OptimizeSystem()
        {
            if (!AutoOptimize || Elements.Count == 0)
                return;

            // 简化的优化算法
            switch (OptimizationTarget)
            {
                case OptimizationTarget.SpotSize:
                    OptimizeForSpotSize();
                    break;
                case OptimizationTarget.WavefrontError:
                    OptimizeForWavefrontError();
                    break;
                case OptimizationTarget.Distortion:
                    OptimizeForDistortion();
                    break;
                case OptimizationTarget.ChromaticAberration:
                    OptimizeForChromaticAberration();
                    break;
            }

            // 重新计算系统参数
            CalculateSystemParameters();
        }

        /// <summary>
        /// 优化点列图尺寸
        /// </summary>
        private void OptimizeForSpotSize()
        {
            // 简化的点列图优化
            foreach (var element in Elements)
            {
                element.RefractiveIndex *= 1.001; // 微调
            }
        }

        /// <summary>
        /// 优化波前误差
        /// </summary>
        private void OptimizeForWavefrontError()
        {
            // 简化的波前误差优化
            foreach (var element in Elements)
            {
                element.Surface1Radius *= 1.001; // 微调
                element.Surface2Radius *= 0.999; // 微调
            }
        }

        /// <summary>
        /// 优化畸变
        /// </summary>
        private void OptimizeForDistortion()
        {
            // 简化的畸变优化
            foreach (var element in Elements)
            {
                element.CenterThickness *= 1.001; // 微调
            }
        }

        /// <summary>
        /// 优化色差
        /// </summary>
        private void OptimizeForChromaticAberration()
        {
            // 简化的色差优化
            foreach (var element in Elements)
            {
                element.AbbeNumber *= 1.001; // 微调
            }
        }

        #endregion
    }

    /// <summary>
    /// 优化目标枚举
    /// </summary>
    public enum OptimizationTarget
    {
        /// <summary>
        /// 点列图尺寸
        /// </summary>
        SpotSize,

        /// <summary>
        /// 波前误差
        /// </summary>
        WavefrontError,

        /// <summary>
        /// 畸变
        /// </summary>
        Distortion,

        /// <summary>
        /// 色差
        /// </summary>
        ChromaticAberration
    }
}