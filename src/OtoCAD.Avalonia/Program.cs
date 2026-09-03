using Avalonia;
using System;
using System.Linq;

namespace OtoCAD.Avalonia;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // 无界面渲染模式 (供 OCR/视觉测试与 CI): --render-sheet <png> [--width N] [--height N]
        // 初始化 Avalonia 服务(字体等) → 加载标准图纸场景 → 高清 ExportToPng → 退出, 不开窗口.
        int idx = Array.IndexOf(args, "--render-sheet");
        if (idx >= 0 && idx + 1 < args.Length)
        {
            RenderSheetHeadless(args[idx + 1], args);
            return;
        }
        // 无界面渲染单个镀膜标记 (供 OCR 矩阵测试): --render-mark <CoatingType> <png>
        int mi = Array.IndexOf(args, "--render-mark");
        if (mi >= 0 && mi + 2 < args.Length)
        {
            RenderMarkHeadless(args[mi + 1], args[mi + 2], args);
            return;
        }
        // 无界面渲染非球面数据块 (ISO 10110-12, 视觉回归): --render-asph <png>
        int ai = Array.IndexOf(args, "--render-asph");
        if (ai >= 0 && ai + 1 < args.Length)
        {
            RenderAsphericBlockHeadless(args[ai + 1], args);
            return;
        }
        // 无界面渲染直角棱镜标准图纸 (棱镜出图回归): --render-prism <png>
        int pi = Array.IndexOf(args, "--render-prism");
        if (pi >= 0 && pi + 1 < args.Length)
        {
            RenderPrismSheetHeadless(args[pi + 1], args);
            return;
        }
        // 无界面导出 DXF (交付格式回归): --export-dxf <dxf>
        // 无界面验证一键重出不丢数据载体 (回归): --verify-drawing
        if (Array.IndexOf(args, "--verify-drawing") >= 0)
        {
            VerifyDrawingHeadless(args);
            return;
        }
        // 与 --render-sheet 同一场景 (PCX-001), 打印实体计数供 CI 断言"图没被导丢".
        int di = Array.IndexOf(args, "--export-dxf");
        if (di >= 0 && di + 1 < args.Length)
        {
            ExportDxfHeadless(args[di + 1]);
            return;
        }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void RenderAsphericBlockHeadless(string outPath, string[] args)
    {
        BuildAvaloniaApp().SetupWithoutStarting();
        int w = ArgInt(args, "--width", 2400);
        int h = ArgInt(args, "--height", 1600);
        // 对标 docs/需求/图面样例-非球面透镜1.png 的真值.
        var blk = new lcdb.Annotation.AsphericDataBlock
        {
            Position = new LitMath.Vector2(0, 80),
            SurfaceLabel = "1",
            BaseRadius = 56.031,
            ConicConstant = -3,
            EvenCoefficients = new[] { -4.3264e-6, -9.7614e-9, -1.0852e-14, -1.2284e-14 },
            SampleHeights = new[] { 0.0, 5.0, 10.0, 15.0, 19.0 },
            SagTolerances = new[] { 0.0, 0.002, 0.004, 0.006, 0.008 },
            SlopeTolerances = new[] { "0.3'", "0.5'", "0.5'", "0.8'", "" },
            SlopeSampleLength = 1.0,
            SlopeSampleStep = 0.1,
            TextHeight = 3.0,
        };
        var canvas = new CadCanvas();
        canvas.LoadGeneratedSheet(new lcdb.Entity[] { blk });
        var saved = canvas.ExportToPng(outPath, w, h);
        Console.WriteLine($"rendered-asph: {saved} ({w}x{h})");
        Environment.Exit(0);
    }

    /// <summary>--type     private static int ArgInt(string[] args, string name, int fallback)lt;PrismType    private static int ArgInt(string[] args, string name, int fallback)gt; (默认 RightAngle) — 用于验证非直角类型被出图管线拒绝.</summary>
    private static lcdb.Optic.PrismType ArgPrismType(string[] args)
    {
        int i = Array.IndexOf(args, "--type");
        return (i >= 0 && i + 1 < args.Length && Enum.TryParse<lcdb.Optic.PrismType>(args[i + 1], true, out var t))
            ? t : lcdb.Optic.PrismType.RightAngle;
    }

    private static int ArgInt(string[] args, string name, int fallback)
    {
        int i = Array.IndexOf(args, name);
        return (i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out var v)) ? v : fallback;
    }

    private static void RenderSheetHeadless(string outPath, string[] args)
    {
        BuildAvaloniaApp().SetupWithoutStarting();   // 初始化字体/平台服务, 不进消息循环
        int w = ArgInt(args, "--width", 3000);
        int h = ArgInt(args, "--height", 4000);
        var canvas = new CadCanvas();
        canvas.LoadTestScene();                      // PCX-001 ISO 标准单透镜加工图
        var saved = canvas.ExportToPng(outPath, w, h);
        Console.WriteLine($"rendered-sheet: {saved} ({w}x{h})");
        Environment.Exit(0);                         // 强制退出, 避免 Avalonia 后台线程挂住
    }

    private static void RenderPrismSheetHeadless(string outPath, string[] args)
    {
        BuildAvaloniaApp().SetupWithoutStarting();
        int w = ArgInt(args, "--width", 3000);
        int h = ArgInt(args, "--height", 4000);

        // 典型 25.4mm 直角棱镜 (BK7), 带尺寸公差 — 对标常见目录件
        var prism = new lcdb.Optic.Prism
        {
            Width = 25.4,
            Height = 25.4,
            Type = ArgPrismType(args),
            WidthTolerance = new lcdb.Common.ToleranceValue(25.4, 0.1, 0.1),
            HeightTolerance = new lcdb.Common.ToleranceValue(25.4, 0.1, 0.1),
        };
        prism.ApplyMaterial("BK7");

        var sheet = Templating.StandardSheetEngine.Generate(prism, Templating.SheetMeta.Default());
        if (sheet is null)
        {
            Console.WriteLine("prism-sheet: 生成失败 (无适配器接受该零件)");
            Environment.Exit(1);
        }

        var canvas = new CadCanvas();
        canvas.LoadGeneratedSheet(sheet);
        var saved = canvas.ExportToPng(outPath, w, h);
        Console.WriteLine($"rendered-prism: {saved} ({w}x{h}) entities={sheet!.Count}");
        Environment.Exit(0);
    }

    /// <summary>
    /// 无界面验证"一键重出"不吃掉数据载体: --verify-drawing
    ///
    /// 场景 = 透镜 + 自动标注 + 非球面数据块 + 粗糙度标记 (后两者 Owner 也是透镜, 但
    /// AutoDimensionLensCmd.Build 不产出它们)。重出前后计数须一致 — 数据块是 ISO 10110-12
    /// 的 error 级交付要件, 被重出删掉就是静默数据丢失。
    /// </summary>
    private static void VerifyDrawingHeadless(string[] args)
    {
        BuildAvaloniaApp().SetupWithoutStarting();

        var lens = new lcdb.Optic.OpticalLens
        {
            Position = new LitMath.Vector2(0, 0),
            Diameter = 25.4, Thickness = 6,
            FrontSurface = new lcdb.Optic.AsphericSurface { Radius = 56.031, ConicConstant = -3 },
            R2 = -50,
        };
        var blk = lcdb.Annotation.AsphericDataBlock.FromSurface(
            (lcdb.Optic.AsphericSurface)lens.FrontSurface, 12.7, "1", new LitMath.Vector2(-40, 30));
        blk.Owner = lens;
        var rough = new lcdb.Annotation.SurfaceRoughnessMark(new LitMath.Vector2(20, 10), 6) { Owner = lens };

        var canvas = new CadCanvas();
        canvas.LoadGeneratedSheet(new lcdb.Entity[] { lens, blk, rough });

        int Count<T>() => canvas.GetAllEntities().Count(e => e is T);
        int blkBefore = Count<lcdb.Annotation.AsphericDataBlock>();
        int roughBefore = Count<lcdb.Annotation.SurfaceRoughnessMark>();

        canvas.RegenerateAutoDimensionsFor(lens);
        int dimsAfter1 = Count<lcdb.LinearDimension>() + Count<lcdb.Annotation.CoatingMark>();
        canvas.RegenerateAutoDimensionsFor(lens);   // 第二次: 可重建类型不得累积
        int dimsAfter2 = Count<lcdb.LinearDimension>() + Count<lcdb.Annotation.CoatingMark>();

        int blkAfter = Count<lcdb.Annotation.AsphericDataBlock>();
        int roughAfter = Count<lcdb.Annotation.SurfaceRoughnessMark>();

        // (2) GB 路径 (默认惯例) 出非球面透镜图须自带 ISO 10110-12 数据块
        var gbSheet = Commands.GbSheetScene.BuildFromLens(lens);
        int gbBlocks = gbSheet.Count(e => e is lcdb.Annotation.AsphericDataBlock);

        Console.WriteLine($"verify-drawing:");
        Console.WriteLine($"  GB 路径非球面数据块 = {gbBlocks} (须 >= 1, 否则清单 error 恒红不可交付)");
        Console.WriteLine($"  重出: aspheric-block {blkBefore} -> {blkAfter}, roughness {roughBefore} -> {roughAfter}");
        Console.WriteLine($"  可重建类型 (尺寸+镀膜): 重出1次={dimsAfter1}, 重出2次={dimsAfter2} (须相等, 否则累积重复)");
        bool ok = blkAfter == blkBefore && roughAfter == roughBefore && dimsAfter1 == dimsAfter2 && dimsAfter1 > 0 && gbBlocks >= 1;
        // 可选出图供人眼核对数据块落位: --verify-drawing --png <path>
        int pngIdx = Array.IndexOf(args, "--png");
        if (pngIdx >= 0 && pngIdx + 1 < args.Length)
        {
            var c2 = new CadCanvas();
            c2.LoadGeneratedSheet(gbSheet);
            Console.WriteLine($"  已出图: {c2.ExportToPng(args[pngIdx + 1], 2200, 3000)}");
        }

        Console.WriteLine(ok ? "  OK 交付要件齐全且重出不丢" : "  FAIL 见上");
        Environment.Exit(ok ? 0 : 1);
    }

    private static void ExportDxfHeadless(string outPath)
    {
        BuildAvaloniaApp().SetupWithoutStarting();
        var canvas = new CadCanvas();
        canvas.LoadTestScene();                      // 与 --render-sheet 同一张 PCX-001 加工图
        var saved = canvas.ExportToDxf(outPath);

        // 读回统计 — 出图能画出来但导不出去过, 数字是唯一可信证据
        var doc = netDxf.DxfDocument.Load(saved);
        Console.WriteLine($"exported-dxf: {saved}");
        Console.WriteLine($"  entities={doc.Entities.All.Count()} " +
                          $"lines={doc.Entities.Lines.Count()} arcs={doc.Entities.Arcs.Count()} " +
                          $"circles={doc.Entities.Circles.Count()} texts={doc.Entities.Texts.Count()} " +
                          $"hatches={doc.Entities.Hatches.Count()} layers={doc.Layers.Count}");
        Environment.Exit(0);
    }

    private static void RenderMarkHeadless(string coatingType, string outPath, string[] args)
    {
        BuildAvaloniaApp().SetupWithoutStarting();
        int w = ArgInt(args, "--width", 1400);
        int h = ArgInt(args, "--height", 1000);
        var t = (lcdb.Annotation.CoatingType)Enum.Parse(typeof(lcdb.Annotation.CoatingType), coatingType, true);
        var mark = new lcdb.Annotation.CoatingMark
        {
            CoatingType = t,
            CoatingText = t.ToString(),   // 标签=类型名, 供 OCR 断言
            Size = 12,
            ShowText = true,
        };
        var canvas = new CadCanvas();
        canvas.LoadGeneratedSheet(new lcdb.Entity[] { mark });
        var saved = canvas.ExportToPng(outPath, w, h);
        Console.WriteLine($"rendered-mark: {t} -> {saved} ({w}x{h})");
        Environment.Exit(0);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
