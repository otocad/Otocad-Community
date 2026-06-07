using Avalonia;
using System;

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
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
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
