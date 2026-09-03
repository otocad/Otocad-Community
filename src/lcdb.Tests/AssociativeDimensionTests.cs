using LitMath;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using lcdb;
using lcdb.Optic;

namespace lcdb.Tests
{
    /// <summary>
    /// 关联标注 (LinearDimension.RefreshFromAnchors) 数据契约:
    /// 标注端点绑定宿主透镜的捕捉点(角点)索引后, 透镜改尺寸/半径 → 重锚使端点跟随。
    /// 捕捉点序 (OpticalLens.GetSnapPoints): 0中心 / 1前顶 / 2后顶 / 3口径上(+hD) / 4口径下(-hD) /
    /// 5前上角 / 6前下角 / 7后上角 / 8后下角。
    /// </summary>
    [TestClass]
    public class AssociativeDimensionTests
    {
        private static OpticalLens MakeLens(double d, double t, double r1, double r2)
            => new OpticalLens { Position = Vector2.Zero, Diameter = d, Thickness = t, R1 = r1, R2 = r2 };

        private static LinearDimension MakeDim(OpticalLens lens, int anchorA, int anchorB)
        {
            var sps = lens.GetSnapPoints();
            var a = sps[anchorA].position;
            var b = sps[anchorB].position;
            return new LinearDimension(a, b, 10.0, 0.0, new DimensionStyle())
            {
                FirstAnchor = new DimAnchor(lens, anchorA),
                SecondAnchor = new DimAnchor(lens, anchorB),
            };
        }

        [TestMethod]
        public void DiameterDim_FollowsAperture_OnResize()
        {
            var lens = MakeLens(d: 20, t: 5, r1: 50, r2: -50);
            var dim = MakeDim(lens, anchorA: 4, anchorB: 3);   // Ø: 口径下↔口径上
            Assert.AreEqual(-10.0, dim.firstReferencePoint.Y, 1e-6);
            Assert.AreEqual(+10.0, dim.secondReferencePoint.Y, 1e-6);

            lens.Diameter = 40;                                 // 通光直径翻倍
            bool changed = dim.RefreshFromAnchors();

            Assert.IsTrue(changed, "改口径后重锚应返回 true");
            Assert.AreEqual(-20.0, dim.firstReferencePoint.Y, 1e-6, "下端点应跟到 -hD");
            Assert.AreEqual(+20.0, dim.secondReferencePoint.Y, 1e-6, "上端点应跟到 +hD");
        }

        [TestMethod]
        public void ThicknessDim_FollowsApexes_OnResize()
        {
            var lens = MakeLens(d: 20, t: 5, r1: 50, r2: -50);
            var dim = MakeDim(lens, anchorA: 1, anchorB: 2);   // CT: 前顶↔后顶
            Assert.AreEqual(-2.5, dim.firstReferencePoint.X, 1e-6);
            Assert.AreEqual(+2.5, dim.secondReferencePoint.X, 1e-6);

            lens.Thickness = 8;                                 // 中心厚度改变
            bool changed = dim.RefreshFromAnchors();

            Assert.IsTrue(changed);
            Assert.AreEqual(-4.0, dim.firstReferencePoint.X, 1e-6, "前顶点应跟到 -hT");
            Assert.AreEqual(+4.0, dim.secondReferencePoint.X, 1e-6, "后顶点应跟到 +hT");
        }

        [TestMethod]
        public void EdgeCorners_FollowRadius_OnRadiusChange()
        {
            var lens = MakeLens(d: 20, t: 6, r1: 50, r2: -50);
            var dim = MakeDim(lens, anchorA: 5, anchorB: 7);   // 边厚: 前上角↔后上角
            double frontX0 = dim.firstReferencePoint.X;
            double backX0 = dim.secondReferencePoint.X;

            lens.R1 = 25;                                       // 前表面更弯 → 前上角 X 偏移变化
            bool changed = dim.RefreshFromAnchors();

            Assert.IsTrue(changed, "改半径后边角位置变 → 重锚");
            var sps = lens.GetSnapPoints();
            Assert.AreEqual(sps[5].position.X, dim.firstReferencePoint.X, 1e-6, "前上角端点应等于宿主当前角点");
            Assert.AreEqual(sps[7].position.X, dim.secondReferencePoint.X, 1e-6);
            Assert.AreNotEqual(frontX0, dim.firstReferencePoint.X, "前角确实移动了");
        }

        [TestMethod]
        public void Mark_AttachToSurface_RecordsHeightForReanchor()
        {
            // 贴面标记 AttachToSurface 须把 (吸附点Y − 顶点Y) 记入 AttachState.Height,
            // 关联重锚据此在宿主改变后保持标记的贴面高度。
            var coat = new lcdb.Annotation.CoatingMark(Vector2.Zero, lcdb.Annotation.CoatingType.AR, 4.0)
            {
                AttachState = new lcdb.Annotation.SurfaceAttachState(new Vector2(10, 5), 50, false),
            };
            coat.AttachToSurface(new Vector2(12, 13), new Vector2(1, 0));
            Assert.AreEqual(8.0, coat.AttachState!.Height, 1e-9, "13 − 5 = 8");

            var rough = new lcdb.Annotation.SurfaceRoughnessMark(Vector2.Zero, 5.0)
            {
                AttachState = new lcdb.Annotation.SurfaceAttachState(new Vector2(-3, 2), 30, true),
            };
            rough.AttachToSurface(new Vector2(-1, -4), new Vector2(-1, 0));
            Assert.AreEqual(-6.0, rough.AttachState!.Height, 1e-9, "-4 − 2 = -6");
        }

        [TestMethod]
        public void DimStyle_VerticalTextAligned_DefaultTrue_RoundTrips()
        {
            // 竖直标注文字取向跟随标准/用户格式: 默认竖排 (GB/ISO), 可经样式/配置切换为水平。
            var s = new DimensionStyle();
            Assert.IsTrue(s.VerticalTextAligned, "默认竖排 (GB/ISO)");
            s.VerticalTextAligned = false;
            Assert.IsFalse(s.Clone().VerticalTextAligned, "Clone 须保留取向设定");
        }

        [TestMethod]
        public void NoBinding_RefreshIsNoOp()
        {
            var lens = MakeLens(d: 20, t: 5, r1: 50, r2: -50);
            var a = new Vector2(-2.5, 0); var b = new Vector2(2.5, 0);
            var dim = new LinearDimension(a, b, 10.0, 0.0, new DimensionStyle());  // 无锚点绑定

            lens.Thickness = 99;
            bool changed = dim.RefreshFromAnchors();

            Assert.IsFalse(changed, "未绑定锚点的标注重锚应零变化");
            Assert.AreEqual(-2.5, dim.firstReferencePoint.X, 1e-6);
            Assert.AreEqual(+2.5, dim.secondReferencePoint.X, 1e-6);
        }
    }
}
