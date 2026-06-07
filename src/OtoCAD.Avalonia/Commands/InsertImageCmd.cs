using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// Phase 1E (实做): 插入图像 — file picker → lcdb.Image 单点放置.
/// 默认显示尺寸 50×50 mm, 用户可在 PropertyPanel 调.
/// </summary>
public sealed class InsertImageCmd : ICadCommand
{
    private readonly Window _ownerWindow;
    private string? _selectedPath;
    private ICadCommandHost? _host;
    public string Name => "插入图像";

    public InsertImageCmd(Window owner) { _ownerWindow = owner; }

    public void Start(ICadCommandHost host)
    {
        _host = host;
        host.SetPrompt("[图像] 选择图像文件...");
        _ = PickFileAsync();
    }

    private async Task PickFileAsync()
    {
        var sp = _ownerWindow.StorageProvider;
        var files = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择图像",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Images") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" } }
            }
        });
        var file = files.FirstOrDefault();
        if (file is null)
        {
            _host?.SetPrompt("[图像] 已取消");
            _host?.FinishCommand();
            return;
        }
        _selectedPath = file.Path.LocalPath;
        _host?.SetPrompt($"[图像] {Path.GetFileName(_selectedPath)} - 单击放置位置 (ESC 取消)");
    }

    public void OnMouseClick(Vector2 p)
    {
        if (_host is null || _selectedPath is null) return;
        try
        {
            var img = new lcdb.Image(_selectedPath, p, new Vector2(50, 50));
            _host.AddEntity(img);
            _host.SetPrompt($"[图像] 已插入 {Path.GetFileName(_selectedPath)} @ ({p.X:F2},{p.Y:F2})");
        }
        catch (Exception ex)
        {
            _host.SetPrompt($"[图像] 插入失败: {ex.Message}");
        }
        _host.FinishCommand();
    }

    public void OnMouseMove(Vector2 p) { }

    public void Cancel()
    {
        _host?.SetPrompt("[图像] 已取消");
        _host?.FinishCommand();
    }
}
