using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LitMath;
using lcdb.Annotation;
using lcdb.Rendering;

namespace lcdb.Tests
{
    /// <summary>
    /// 文档资产生成器: 把每个 CoatingType 的**真实** CoatingMark 实体经 SvgGraphicsDraw 渲染成 SVG,
    /// 写入 website-docusaurus 静态目录 —— 文档预览图与程序实际出图共用同一份几何代码 (单一真值),
    /// 杜绝在前端手抄几何导致漂移.
    ///
    /// 几何确定性 → 重复运行产物逐字节一致, 不产生 git 噪声; 同时断言产物有效, 兼作守卫.
    /// 见 memory: ocr-vs-geometry-verification.
    /// </summary>
    [TestClass]
    public class CoatingMarkSvgDocGenerator
    {
        // 与 CoatingMarkGeometryTests 同一张表 (GB/T 13323-2009 表1).
        private static readonly CoatingType[] Types =
        {
            CoatingType.AR, CoatingType.Polarizing, CoatingType.HR,
            CoatingType.OuterReflective, CoatingType.BS, CoatingType.Filter,
            CoatingType.Protective, CoatingType.Conductive, CoatingType.Blackening,
        };

        private static string OutDir([CallerFilePath] string thisFile = "")
        {
            // thisFile = <repo>/src/lcdb.Tests/CoatingMarkSvgDocGenerator.cs
            var repo = Directory.GetParent(thisFile)!.Parent!.Parent!.FullName; // …/src/lcdb.Tests → src → repo
            return Path.Combine(repo, "website-docusaurus", "static", "img", "marks");
        }

        [TestMethod]
        public void Generate_coating_mark_svgs_into_docusaurus()
        {
            var dir = OutDir();
            Directory.CreateDirectory(dir);

            foreach (var type in Types)
            {
                var mark = new CoatingMark
                {
                    CoatingType = type,
                    Size = 10,
                    Center = Vector2.Zero,
                    ShowText = false,   // 文档只看符号几何, 文字单独在表格里给
                };

                var svg = new SvgGraphicsDraw();
                mark.Draw(svg);              // 走真实实体 Draw → IGraphicsDraw 管线
                var xml = svg.BuildSvg(pixelSize: 160);

                Assert.IsTrue(xml.StartsWith("<svg") && xml.EndsWith("</svg>"), $"{type}: 非法 SVG");
                Assert.IsTrue(xml.Contains("<circle") || xml.Contains("<line") || xml.Contains("<polyline"),
                    $"{type}: SVG 无任何图元");

                File.WriteAllText(Path.Combine(dir, $"coating-{type}.svg"), xml);
            }

            Assert.IsTrue(File.Exists(Path.Combine(dir, "coating-AR.svg")), "AR SVG 未生成");
        }
    }
}
