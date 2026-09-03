using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace OtoCAD.Avalonia.Dialogs;

/// <summary>
/// 通用确认对话框 (代码构建, 无 XAML): 一段提示 + 若干按钮, 返回被点按钮的文本 (关窗 = null).
/// 用于"关闭未保存确认""启动恢复提示"等. 第一个按钮为默认 (Enter), 含"取消"则为取消(Esc).
/// </summary>
public sealed class ConfirmDialog : Window
{
    private string? _result;

    private ConfirmDialog(string title, string message, string[] buttons)
    {
        Title = title;
        Width = 400;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var msg = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new global::Avalonia.Thickness(18, 18, 18, 8),
        };

        var bar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new global::Avalonia.Thickness(12, 8, 12, 12),
        };
        foreach (var b in buttons)
        {
            string label = b;
            var btn = new Button { Content = label, MinWidth = 76 };
            btn.Click += (_, _) => { _result = label; Close(); };
            bar.Children.Add(btn);
        }

        Content = new StackPanel { Children = { msg, bar } };
    }

    /// <summary>显示对话框, 返回被点按钮文本; 直接关窗返回 null.</summary>
    public static async Task<string?> ShowAsync(Window owner, string title, string message, params string[] buttons)
    {
        var dlg = new ConfirmDialog(title, message, buttons);
        await dlg.ShowDialog(owner);
        return dlg._result;
    }
}
