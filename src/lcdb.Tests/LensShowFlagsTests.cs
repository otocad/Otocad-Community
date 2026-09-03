using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using lcdb;
using lcdb.IO;
using lcdb.Optic;

namespace lcdb.Tests
{
    /// <summary>
    /// 透镜「标注显示开关」(ShowR1/ShowR2/ShowThickness/ShowDiameter/ShowCoating …) 的数据契约:
    /// - 默认全开 = 推荐标准集 (用户不知道标什么时, 一键自动标注给出完整合规集)。
    /// - 取消勾选要能克隆保留 + .otocad 往返保留 (否则保存后丢失用户意图)。
    /// AutoDimensionLensCmd.Build 据这些开关决定生成哪些标注 (该逻辑在 UI 层, 此处只钉数据契约)。
    /// </summary>
    [TestClass]
    public class LensShowFlagsTests
    {
        [TestMethod]
        public void Defaults_AllOn_IsRecommendedSet()
        {
            var single = new OpticalLens();
            Assert.IsTrue(single.ShowR1 && single.ShowR2 && single.ShowThickness
                          && single.ShowDiameter && single.ShowCoating,
                "单透镜默认应全标 (推荐标准集)");

            var doublet = new CementedLens();
            Assert.IsTrue(doublet.ShowR1 && doublet.ShowRc && doublet.ShowR3
                          && doublet.ShowThickness && doublet.ShowDiameter && doublet.ShowCoating,
                "双胶合默认应全标 (推荐标准集)");
        }

        [TestMethod]
        public void Clone_PreservesShowFlags()
        {
            var lens = new OpticalLens { ShowR1 = false, ShowCoating = false };
            var c = (OpticalLens)lens.Clone();
            Assert.IsFalse(c.ShowR1);
            Assert.IsFalse(c.ShowCoating);
            Assert.IsTrue(c.ShowR2, "未改项应保持默认开");
            Assert.IsTrue(c.ShowThickness);
        }

        [TestMethod]
        public void ShowFlags_SurviveOtocadRoundTrip_Single()
        {
            var lens = new OpticalLens { ShowR1 = false, ShowDiameter = false, ShowCoating = false };
            var path = Path.Combine(Path.GetTempPath(), $"lens-show-{Guid.NewGuid():N}.otocad");
            var db = new Database();
            db.AddEntity(lens);
            try
            {
                OtocadFileFormatV4.Save(db, path);
                var (db2, _) = OtocadFileFormatV4.Load(path);
                var back = db2.GetEntitiesByType<OpticalLens>().Single();
                Assert.IsFalse(back.ShowR1, "ShowR1=false 应往返保留");
                Assert.IsFalse(back.ShowDiameter);
                Assert.IsFalse(back.ShowCoating);
                Assert.IsTrue(back.ShowR2, "未改项应保持默认开");
                Assert.IsTrue(back.ShowThickness);
            }
            finally { try { if (File.Exists(path)) File.Delete(path); } catch { } }
        }

        [TestMethod]
        public void ShowFlags_SurviveOtocadRoundTrip_Cemented()
        {
            var lens = new CementedLens { ShowRc = false, ShowR3 = false };
            var path = Path.Combine(Path.GetTempPath(), $"doublet-show-{Guid.NewGuid():N}.otocad");
            var db = new Database();
            db.AddEntity(lens);
            try
            {
                OtocadFileFormatV4.Save(db, path);
                var (db2, _) = OtocadFileFormatV4.Load(path);
                var back = db2.GetEntitiesByType<CementedLens>().Single();
                Assert.IsFalse(back.ShowRc);
                Assert.IsFalse(back.ShowR3);
                Assert.IsTrue(back.ShowR1);
                Assert.IsTrue(back.ShowCoating);
            }
            finally { try { if (File.Exists(path)) File.Delete(path); } catch { } }
        }
    }
}
