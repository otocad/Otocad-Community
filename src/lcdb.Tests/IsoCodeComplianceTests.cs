using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using lcdb.Annotation;

namespace lcdb.Tests
{
    /// <summary>
    /// ISO 10110 指示代号合规 (权威依据: ISO 10110-10:2004 代号表 + GB/T 13323-2009).
    ///
    /// 防回归: ISO 10110 的指示代号 **≠ 部件号**。历史上多个标记把部件号当代号写错
    /// (波前 14/、激光 17/、对中 6/、面形 5/、不均匀 4/、疵病 7/),出图会被厂方判非标拒收。
    /// 本测试钉死每个带代号标记的正确前缀;详见 dev/mark-verification-contracts 代号审计。
    /// </summary>
    [TestClass]
    public class IsoCodeComplianceTests
    {
        // 标记类型 → ISO 10110 正确指示代号前缀 (ISO 10110-10 代号表)
        //   0/应力(P2) 1/气泡(P3) 2/不均匀条纹(P4) 3/面形(P5) 4/对中(P6) 5/疵病(P7) 6/激光(P17) 13/波前(P14)
        private static readonly (Type type, string code)[] Cases = new[]
        {
            (typeof(ISO10110_2Mark), "0/"),
            (typeof(ISO10110_3Mark), "1/"),
            (typeof(ISO10110_4Mark), "2/"),         // 曾错为 4/
            (typeof(SurfaceFormMark), "3/"),        // 曾错为 5/
            (typeof(FormAccuracyMark), "3/"),
            (typeof(CenteringToleranceMark), "4/"), // 曾错为 6/
            (typeof(SurfaceImperfectionMark), "5/"),// 曾错为 7/
            (typeof(SurfaceQualityMark), "5/"),
            (typeof(LaserDamageThresholdMark), "6/"),// 曾错为 17/
            (typeof(ISO10110_12Mark), "3/"),        // 非球面面形公差走 3/(无专用 12/ 代号), 曾错为 12/
            (typeof(ISO10110_14Mark), "13/"),       // 曾错为 14/
        };

        [TestMethod]
        public void Default_marktext_uses_correct_iso_code()
        {
            foreach (var (type, code) in Cases)
            {
                var m = Activator.CreateInstance(type)!;
                var text = (string)type.GetProperty("MarkText")!.GetValue(m)!;
                StringAssert.StartsWith(text, code,
                    $"{type.Name} 默认 MarkText 应以 ISO 代号 {code} 开头, 实得 {text}");
            }
        }

        [TestMethod]
        public void Generated_marktext_uses_correct_iso_code()
        {
            foreach (var (type, code) in Cases)
            {
                var m = Activator.CreateInstance(type)!;
                var upd = type.GetMethod("UpdateMarkText", BindingFlags.NonPublic | BindingFlags.Instance);
                if (upd == null) continue;   // 无文字生成器的标记跳过 (例: 纯几何)
                upd.Invoke(m, null);
                var text = (string)type.GetProperty("MarkText")!.GetValue(m)!;
                StringAssert.StartsWith(text, code,
                    $"{type.Name}.UpdateMarkText() 应生成以 {code} 开头的代号, 实得 {text}");
            }
        }
    }
}
