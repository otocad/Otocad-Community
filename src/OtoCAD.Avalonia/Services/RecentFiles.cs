using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace OtoCAD.Avalonia.Services;

/// <summary>
/// T2: 最近文件列表 — 持久化到 %APPDATA%/OtoCAD/recent.json (Win) / ~/.config/OtoCAD/recent.json (Mac/Linux).
/// 最多保留 MaxItems 个, 按访问时间倒序. 自动跳过已不存在的文件.
/// </summary>
public sealed class RecentFiles
{
    public const int MaxItems = 10;

    private readonly string _storePath;
    private readonly List<string> _items = new();

    public IReadOnlyList<string> Items => _items;

    public RecentFiles()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OtoCAD");
        Directory.CreateDirectory(dir);
        _storePath = Path.Combine(dir, "recent.json");
        Load();
    }

    /// <summary>把 path 推到最前; 已存在则上移; 超出 MaxItems 截尾.</summary>
    public void Push(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        path = Path.GetFullPath(path);
        _items.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
        _items.Insert(0, path);
        while (_items.Count > MaxItems) _items.RemoveAt(_items.Count - 1);
        Save();
    }

    public void Remove(string path)
    {
        _items.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
        Save();
    }

    /// <summary>过滤掉磁盘上已不存在的项 (在打开 UI 时调用).</summary>
    public IReadOnlyList<string> ExistingItems()
        => _items.Where(File.Exists).ToList();

    private void Load()
    {
        if (!File.Exists(_storePath)) return;
        try
        {
            var json = File.ReadAllText(_storePath);
            var arr = JsonSerializer.Deserialize<string[]>(json);
            if (arr is null) return;
            _items.Clear();
            _items.AddRange(arr.Where(s => !string.IsNullOrWhiteSpace(s)));
        }
        catch { /* corrupt — 重置 */ }
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(_storePath, JsonSerializer.Serialize(_items));
        }
        catch { /* 磁盘满或权限不足 — 静默 */ }
    }
}
