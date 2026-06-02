using System;
using System.IO;
using System.Linq;
using Avalonia.Threading;

namespace OtoCAD.Avalonia.Services;

/// <summary>
/// T2 AutoSave: 30 秒间隔写一份 .autosave.json 到 %APPDATA%/OtoCAD/autosave/.
/// 仅当 IsDirty=true 才写; 启动时若发现 autosave/ 有文件, 提示用户恢复.
/// 这是奔溃恢复用, 不替代显式 File.Save (用户必须主动保存).
/// </summary>
public sealed class AutoSaveService : IDisposable
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private readonly DispatcherTimer _timer;
    private readonly Func<bool> _isDirty;
    private readonly Func<string?> _currentFilePath;
    private readonly Action<string> _save;
    private readonly Action<string> _onStatusReport;

    public string AutoSaveDir { get; }

    public AutoSaveService(
        Func<bool> isDirty,
        Func<string?> currentFilePath,
        Action<string> save,
        Action<string> onStatusReport)
    {
        _isDirty = isDirty;
        _currentFilePath = currentFilePath;
        _save = save;
        _onStatusReport = onStatusReport;

        AutoSaveDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OtoCAD", "autosave");
        Directory.CreateDirectory(AutoSaveDir);

        _timer = new DispatcherTimer { Interval = Interval };
        _timer.Tick += (_, _) => Tick();
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    /// <summary>autosave 目录 (与实例 <see cref="AutoSaveDir"/> 同一位置), 供启动时扫描恢复.</summary>
    public static string DefaultDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OtoCAD", "autosave");

    /// <summary>
    /// 删除全部自动备份. 干净退出 / 成功保存后调用 —— 没崩溃就无需恢复, 下次启动不再提示.
    /// (只有进程被杀等非正常退出才会留下备份, 从而触发启动恢复提示.)
    /// </summary>
    public static void PurgeBackups()
    {
        try { foreach (var f in FindBackups()) { try { File.Delete(f); } catch { } } }
        catch { }
    }

    /// <summary>列出全部自动备份, 按修改时间 新→旧 排序; 无则空.</summary>
    public static System.Collections.Generic.IReadOnlyList<string> FindBackups()
    {
        try
        {
            if (!Directory.Exists(DefaultDir)) return Array.Empty<string>();
            return Directory.GetFiles(DefaultDir, "*.autosave.json")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToList();
        }
        catch { return Array.Empty<string>(); }
    }

    private void Tick()
    {
        if (!_isDirty()) return;
        try
        {
            var stem = _currentFilePath() is string p
                ? Path.GetFileNameWithoutExtension(p)
                : "untitled";
            var name = $"{stem}.{DateTime.Now:yyyyMMdd-HHmmss}.autosave.json";
            var path = Path.Combine(AutoSaveDir, name);
            _save(path);
            CleanupOldBackups(stem);
            _onStatusReport($"已自动备份: {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            _onStatusReport($"自动备份失败: {ex.Message}");
        }
    }

    /// <summary>每个 stem 只保留最新 3 份, 防止 autosave/ 堆积.</summary>
    private void CleanupOldBackups(string stem)
    {
        try
        {
            var files = Directory.GetFiles(AutoSaveDir, $"{stem}.*.autosave.json");
            if (files.Length <= 3) return;
            Array.Sort(files, (a, b) => string.Compare(b, a, StringComparison.Ordinal));
            for (int i = 3; i < files.Length; i++)
            {
                try { File.Delete(files[i]); } catch { }
            }
        }
        catch { }
    }

    public void Dispose() => _timer.Stop();
}
