#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using lcdb;
using lcdb.Host;
using lcdb.Optic;
using lcdb.Optic.Import;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// 光学设计导入 (Optiland/Zemax JSON 或 .zmx) → OpticalLens / CementedLens。
///
/// v0.5.0 起属免费内核: 解析引擎在 lcdb.Optic.Import, 本类只做 UI 流程 (选文件 → 解析 →
/// 元件选择 → 落到光路图/逐元件加工图/当前文档)。此前它是付费模块 OtoCAD.Cloud 经
/// IAppHost.RegisterCommand 反射注入的, 免费版没有这条命令 ——
/// 于是"光学设计 → 加工图"这条主链对开源用户是断的。
///
/// 仍走 <see cref="IAppHost"/> 而非直接依赖 MainWindow: 保持 UI 交互与流程解耦, 也让
/// 这段逻辑与原付费模块实现保持一致 (搬迁而非重写)。
/// </summary>
public static class ImportDesignCmd
{
    public static async Task RunAsync(IAppHost host)
    {
        var path = await host.PickFileAsync("导入光学设计 (Optic JSON / Zemax .zmx)", "*.json", "*.zmx");
        if (path is null) { host.SetStatus("[导入光学设计] 已取消"); return; }

        ZmxSystemData data;
        try
        {
            if (string.Equals(Path.GetExtension(path), ".zmx", StringComparison.OrdinalIgnoreCase))
                data = new ZmxParser().Parse(path);
            else
                data = new OpticJsonDeserializer().Parse(File.ReadAllText(path));
        }
        catch (Exception ex) { host.SetStatus($"[导入光学设计] 读取失败: {ex.Message}"); return; }

        if (!data.IsValid) { host.SetStatus("[导入光学设计] 解析失败: " + string.Join("; ", data.Errors)); return; }

        var result = new PrescriptionToLensBuilder().Build(
            data, new PrescriptionToLensBuilder.Options { SourceLabel = Path.GetFileName(path) });
        if (result.Entities.Count == 0)
        {
            host.SetStatus("[导入光学设计] 未生成元件: " + string.Join("; ", result.Warnings));
            return;
        }

        string fileName = Path.GetFileName(path);
        var sel = await host.ShowImportSelectionAsync(result.Entities, $"{fileName} · {result.Entities.Count} 元件");
        if (sel is null) { host.SetStatus("[导入光学设计] 已取消"); return; }
        var toAdd = sel.Selected;
        if (toAdd.Count == 0) { host.SetStatus("[导入光学设计] 未选择任何元件"); return; }

        if (sel.MakeLayoutTab)
            host.AddLayoutTab($"光路图 - {fileName}", toAdd);

        if (sel.MakePerElementTabs)
        {
            int n = 0;
            foreach (var e in toAdd)
            {
                n++;
                string label = e switch
                {
                    CementedLens cl => $"{cl.Material1}+{cl.Material2}",
                    OpticalLens ol => ol.MaterialName,
                    _ => "元件"
                };
                host.AddElementSheetTab($"元件{n} {label}", e);
            }
        }

        if (!sel.MakeLayoutTab && !sel.MakePerElementTabs)
            host.AddToActiveDocument(toAdd);

        int singles = toAdd.Count(x => x is OpticalLens);
        int cemented = toAdd.Count(x => x is CementedLens);
        var msg = $"[导入光学设计] {fileName}: 导入 {singles} 单 + {cemented} 胶合";
        if (result.SkippedCount > 0) msg += $", 跳过 {result.SkippedCount}";
        if (result.Warnings.Count > 0) msg += $" (告警 {result.Warnings.Count})";
        host.SetStatus(msg);
    }
}
