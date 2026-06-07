using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LitMath;
using netDxf;
using netDxf.Entities;
using lcdb;
using lcdb.Transaction;
using OtoCAD.OpticEntity;
using Line = lcdb.Line;
using Vector2 = LitMath.Vector2;

namespace OtoCAD.OpticEntity
{

    public enum LensType
    {
        LensType_UnDefined = 0,
        LensType_Concave,
        LensType_LeftMoon,
        LensType_RightMoon,
        Single,
    }
    /// <summary>
    /// 绘制透镜轮廓的类
    /// </summary>
    public class LensOutline : BaseElementBlock
    {
        public LensOutline() : base()
        {
        }

        public LensOutline(Database database) : base(database)
        {
        }

        //public override string BlockName => "LensOutline";

        LensType lensType = LensType.LensType_UnDefined;

        public IElementSurface Surface1 { get; set; } 
        public IElementSurface Surface2 { get; set; }

        [Category("Basic")]
        [DisplayName("Thickness")]
        public double Thickness => Surface1.Thickness;
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public Vector2 BasePoint1 { get; set; }


        public Element element { get; set; } = null;

        public double RealDiameter { get; set; }
        public double RealDiameter2 { get; set; }
      
        public Vector2 LeftMost { get; set; }
        public Vector2 RightMost { get; set; }

 
    
        // 0 相等，1 高，-1 低
        public SingleLensType SingleLensType { get; internal set; } = SingleLensType.EqualDiameter;
        public Vector2 DimCenterThickness1 { get; private set; }
        public Vector2 DimCenterThickness2 { get; private set; }
        public double DimCenterThicknessOffset { get; private set; }
        public Vector2 DimSagFull1 { get; private set; }
        public Vector2 DimSagFull2 { get; private set; }
        public double DimSagFullOffset { get; private set; }
        public Vector2 DimEdgeThicknessTop1 { get; private set; }
        public Vector2 DimEdgeThicknessTop2 { get; private set; }
        public Vector2 DimSagLeft1 { get; private set; }
        public Vector2 DimSagLeft2 { get; private set; }
        public Vector2 DimSagRight1 { get; private set; }
        public Vector2 DimSagRight2 { get; private set; }

        protected override void GenerateEntitiesWithTransaction(IEntityTransaction transaction)
        {
            try
            {
                var savepoint = transaction.CreateSavepoint("BeforeLensOutlineGeneration");
                SafeClearEntityWithTransaction(transaction);
                // 生成曲面实体
                Surface1?.GenEntity();
                Surface2?.GenEntity();

                // 计算参数
                CalculateLensParameters();

                // 生成透镜轮廓实体
                GenerateLensOutlineEntitiesWithTransaction(transaction);

                // 设置标注点
                SetDimensionPoints();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate lens outline entities: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 公共方法供外部调用，用于在事务中生成实体
        /// </summary>
        public void GenerateWithTransaction(IEntityTransaction transaction)
        {
            GenerateEntitiesWithTransaction(transaction);
        }

        protected override void GenerateEntitiesLegacy()
        {
            Surface1?.GenEntity();
            Surface2?.GenEntity();

            // 计算参数
            CalculateLensParameters();

            // 生成透镜轮廓实体
            GenerateLensOutlineEntitiesLegacy();

            // 设置标注点
            SetDimensionPoints();
        }

        public void CalculateLensParameters()
        {
            RealDiameter = Surface1.RealDiameter;
            RealDiameter2 = Surface2.RealDiameter;

            if (RealDiameter < RealDiameter2)
            {
                // left lower right higher
                SingleLensType = SingleLensType.RightLarger;
            }
            else if (RealDiameter > RealDiameter2)
            {
                // left higher right lower
                SingleLensType = SingleLensType.LeftLarger;
            }
            else
            {
                SingleLensType = SingleLensType.EqualDiameter;
            }

            // RIGHT          
            LeftMost = Surface1.Radius < 0.0 ? Surface1.SagPoint : Surface1.BasePoint1;
            RightMost = Surface2.Radius > 0.0 ? Surface2.SagPoint : Surface2.BasePoint1;
        }

        private void GenerateLensOutlineEntitiesWithTransaction(IEntityTransaction transaction)
        {
            var OriginalX = BasePoint1.X;
            var OriginalY = BasePoint1.Y;
            // 使用SemiDiameter而不是RealDiameter，以正确处理超半球情况
            var maxDiameter = Math.Max(Surface1.SemiDiameter, Surface2.SemiDiameter);
            var minDiameter = Math.Min(Surface1.SemiDiameter, Surface2.SemiDiameter);
            var vlineX = SingleLensType == SingleLensType.RightLarger ? Surface1.SagPoint.X : Surface2.SagPoint.X;

            // 曲面轮廓
            AppendEntityWithTransaction(transaction, Surface1.ProfileEntity);
            AppendEntityWithTransaction(transaction, Surface2.ProfileEntity);

            // 顶部水平线
            var topHLine = new Line(
                new Vector2(Surface1.SagPoint.X, maxDiameter + OriginalY),
                new Vector2(Surface2.SagPoint.X, maxDiameter + OriginalY)
            );
            AppendEntityWithTransaction(transaction, topHLine);

            // 底部水平线
            var bottomHLine = new Line(
                new Vector2(Surface1.SagPoint.X, -maxDiameter + OriginalY),
                new Vector2(Surface2.SagPoint.X, -maxDiameter + OriginalY)
            );
            AppendEntityWithTransaction(transaction, bottomHLine);

            // 垂直线（如果直径不等）
            // 注意：超半球垂直线已在ElementOutlineGenerator中处理
            if (SingleLensType != SingleLensType.EqualDiameter)
            {
                var topVLine = new Line(
                    new Vector2(vlineX, minDiameter + OriginalY),
                    new Vector2(vlineX, maxDiameter + OriginalY)
                );
                AppendEntityWithTransaction(transaction, topVLine);

                var bottomVLine = new Line(
                    new Vector2(vlineX, -minDiameter + OriginalY),
                    new Vector2(vlineX, -maxDiameter + OriginalY)
                );
                AppendEntityWithTransaction(transaction, bottomVLine);
            }

            // 中心线
            var centerLine = new Line(
                new Vector2(Surface1.BasePoint1.X - 8.0, OriginalY),
                new Vector2(Surface2.BasePoint1.X + 8.0, OriginalY)
            );
            // TODO: Set line type for centerLine if needed
            AppendEntityWithTransaction(transaction, centerLine);
        }

        private void GenerateLensOutlineEntitiesLegacy()
        {
            var OriginalX = BasePoint1.X;
            var OriginalY = BasePoint1.Y;
            // 使用SemiDiameter而不是RealDiameter，以正确处理超半球情况
            var maxDiameter = Math.Max(Surface1.SemiDiameter, Surface2.SemiDiameter);
            var minDiameter = Math.Min(Surface1.SemiDiameter, Surface2.SemiDiameter);
            var vlineX = SingleLensType == SingleLensType.RightLarger ? Surface1.SagPoint.X : Surface2.SagPoint.X;

            // 曲面轮廓
            AppendEntity(Surface1.ProfileEntity);
            AppendEntity(Surface2.ProfileEntity);

            // 顶部水平线
            AppendEntity(new Line(
                new Vector2(Surface1.SagPoint.X, maxDiameter + OriginalY),
                new Vector2(Surface2.SagPoint.X, maxDiameter + OriginalY)
            ));

            // 底部水平线
            AppendEntity(new Line(
                new Vector2(Surface1.SagPoint.X, -maxDiameter + OriginalY),
                new Vector2(Surface2.SagPoint.X, -maxDiameter + OriginalY)
            ));

            // 垂直线（如果直径不等）
            // 注意：超半球垂直线已在ElementOutlineGenerator中处理
            if (SingleLensType != SingleLensType.EqualDiameter)
            {
                AppendEntity(new Line(
                    new Vector2(vlineX, minDiameter + OriginalY),
                    new Vector2(vlineX, maxDiameter + OriginalY)
                ));
                AppendEntity(new Line(
                    new Vector2(vlineX, -minDiameter + OriginalY),
                    new Vector2(vlineX, -maxDiameter + OriginalY)
                ));
            }

            // 中心线
            AppendEntity(new Line(
                new Vector2(Surface1.BasePoint1.X - 8.0, OriginalY),
                new Vector2(Surface2.BasePoint1.X + 8.0, OriginalY)
            ));
            // TODO: Set line type for centerLine if needed
        }

        public void SetDimensionPoints()
        {
            var OriginalX = BasePoint1.X;
            var OriginalY = BasePoint1.Y;
            // 使用SemiDiameter而不是RealDiameter，以正确处理超半球情况
            var maxDiameter = Math.Max(Surface1.SemiDiameter, Surface2.SemiDiameter);
            var topleftXGlobal = Surface1.SagPoint.X;
            var toprightXInGlobal = Surface2.SagPoint.X;

            // 计算合理的间距，基于最大直径的比例
            var baseSpacing = Math.Max(20.0, maxDiameter * 0.1); // 至少20单位，或直径的10%
            var dimensionSpacing = baseSpacing * 0.8; // 标注之间的间距

            // 标注点设置
            DimCenterThickness1 = new Vector2(OriginalX, OriginalY);
            DimCenterThickness2 = new Vector2(OriginalX + Thickness, OriginalY);
            DimCenterThicknessOffset = maxDiameter + baseSpacing;

            // Max sag for moon lens
            DimSagFull1 = new Vector2(LeftMost.X, -Surface2.RealDiameter + Surface2.BasePoint1.Y);
            DimSagFull2 = new Vector2(RightMost.X, -Surface2.RealDiameter + Surface2.BasePoint1.Y);
            DimSagFullOffset = baseSpacing + dimensionSpacing * 2; // 增加间距避免重叠

            DimEdgeThicknessTop1 = new Vector2(topleftXGlobal, maxDiameter + OriginalY);
            DimEdgeThicknessTop2 = new Vector2(toprightXInGlobal, maxDiameter + OriginalY);

            DimSagLeft1 = new Vector2(topleftXGlobal, RealDiameter + OriginalY);
            DimSagLeft2 = new Vector2(topleftXGlobal, -RealDiameter + OriginalY);

            DimSagRight1 = new Vector2(toprightXInGlobal, RealDiameter2 + OriginalY);
            DimSagRight2 = new Vector2(toprightXInGlobal, -RealDiameter2 + OriginalY);
        }

        public override void GenEntity()
        {
            // 添加调试信息
            System.Diagnostics.Debug.WriteLine($"[LensOutline.GenEntity] Called with database={database?.GetHashCode()}");

            if (database == null)
            {
                System.Diagnostics.Debug.WriteLine("[LensOutline.GenEntity] No database, returning");
                return;
            }

            SafeClearEntity();

            // 调用基类的GenEntity方法，它会根据是否有事务管理器来决定使用哪种方式
            base.GenEntity();
        }


    }
}
