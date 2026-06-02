using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OtoCAD.Avalonia.Chat;

/// <summary>
/// 简易 AI 客服面板 — 默认接官方 cloud 助手 cadask.optic.chat (/api/v1/chat/send).
///
/// 设计:
/// - 启动时异步 ping 后端, 更新状态指示灯 (绿 = 在线, 灰 = 离线); cloud 在线显示 "在线 (cloud)"
/// - 维护本地对话历史 (List&lt;ChatTurn&gt;), 每次 send 发完整历史
/// - 离线时 input 禁用; cloud 提示检查网络, 本地 (localhost) 提示运行后端
/// - Ctrl+Enter 发送 (单 Enter 换行)
/// </summary>
public partial class ChatPanel : UserControl
{
    private BackendClient? _client;
    private readonly List<ChatTurn> _history = new();
    private bool _online;
    private bool _sending;
    private bool _offlineHintShown;
    private DispatcherTimer? _reconnectTimer;

    // 云版遥测钩子: 实现在 Cloud/Telemetry.Cloud.cs (不进开源版).
    // 开源版无实现时, C# 编译器自动消除对 partial 方法的调用 -> 零开销空操作.
    partial void OnTelemetryInit();
    partial void OnTelemetryOnline();

    public ChatPanel()
    {
        InitializeComponent();

        var sendBtn = this.FindControl<Button>("SendButton")!;
        var input = this.FindControl<TextBox>("InputBox")!;

        sendBtn.Click += async (_, _) => await SendAsync();
        input.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter && (e.KeyModifiers & KeyModifiers.Control) != 0)
            {
                e.Handled = true;
                await SendAsync();
            }
        };

        // 重连按钮: 直接重新 ping 并按结果更新状态.
        // (不要先无条件 UpdateStatus(false), 否则在线时点一下会闪离线+插一条假"后端未启动")
        var reconnect = this.FindControl<Button>("ReconnectButton")!;
        reconnect.Click += async (_, _) => await ConnectAsync();

        // 启动时 ping 后端 (fire-and-forget)
        // 优先从环境变量 OTOCAD_BACKEND_URL 读, 否则连云端
        var url = Environment.GetEnvironmentVariable("OTOCAD_BACKEND_URL")
                  ?? "https://cadask.optic.chat";
        _client = new BackendClient(url);
        _ = ConnectAsync();

        // 离线时自动重连: 每 3s ping 一次, 后端一上线就自动连上, 无需手点 ↻
        _reconnectTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _reconnectTimer.Tick += async (_, _) =>
        {
            if (!_online && !_sending) await ConnectAsync();
        };
        _reconnectTimer.Start();

        OnTelemetryInit();   // 云版遥测心跳 (开源版空操作)
    }

    /// <summary>外部可注入自定义 BaseUrl (默认官方 cloud 助手 cadask.optic.chat).</summary>
    public void Configure(string baseUrl)
    {
        _client?.Dispose();
        _client = new BackendClient(baseUrl);
        _ = ConnectAsync();
    }

    private async Task ConnectAsync()
    {
        _client ??= new BackendClient();
        var ok = await _client.PingAsync();
        Dispatcher.UIThread.Post(() => UpdateStatus(ok));
    }

    private void UpdateStatus(bool online)
    {
        _online = online;
        var dot = this.FindControl<TextBlock>("StatusDot")!;
        var label = this.FindControl<TextBlock>("StatusLabel")!;
        var input = this.FindControl<TextBox>("InputBox")!;
        var send = this.FindControl<Button>("SendButton")!;
        // 本地后端 (localhost) 与云端 (cloud) 显示不同标签
        var isLocal = _client!.BaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                      || _client.BaseUrl.Contains("127.0.0.1");
        if (online)
        {
            dot.Foreground = new SolidColorBrush(Color.FromRgb(0x40, 0xA0, 0x40));
            label.Text = isLocal ? $"在线 ({_client.BaseUrl})" : "在线 (cloud)";
            input.IsEnabled = true;
            send.IsEnabled = true;
            _offlineHintShown = false;  // 重置, 若以后再掉线可再提示一次
            OnTelemetryOnline();        // 云版: 上线即发心跳 (开源版空操作)
        }
        else
        {
            dot.Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0x40, 0x40));
            label.Text = isLocal ? "后端未启动 (自动重连中…)" : "云端连接中… (自动重连中…)";
            input.IsEnabled = false;
            send.IsEnabled = false;
            if (!_offlineHintShown)  // 只提示一次, 避免自动重连刷屏
            {
                _offlineHintShown = true;
                AppendSystem(isLocal
                    ? "后端未启动. 请在另一个终端执行:\n  dotnet run --project src/OtoCAD.Backend\n后端就绪后会自动连接 (无需重启本程序)."
                    : "无法连接云端 AI 服务 (cadask.optic.chat). 请检查网络, 联网后会自动连接.");
            }
        }
    }

    private async Task SendAsync()
    {
        if (_sending || !_online || _client is null) return;
        var input = this.FindControl<TextBox>("InputBox")!;
        var text = input.Text?.Trim() ?? "";
        if (text.Length == 0) return;
        input.Text = "";
        _sending = true;
        try
        {
            AppendUser(text);
            _history.Add(new ChatTurn { Role = "user", Content = text });
            var thinking = AppendAssistant("…");
            var acc = new System.Text.StringBuilder();
            try
            {
                var reply = await _client.ChatStreamAsync(_history, delta =>
                    Dispatcher.UIThread.Post(() =>
                    {
                        acc.Append(delta);
                        thinking.Text = acc.ToString();
                        ScrollToBottom();
                    }));
                _history.Add(new ChatTurn { Role = "assistant", Content = reply.Reply });
                Dispatcher.UIThread.Post(() =>
                {
                    RenderMarkdown(thinking, reply.Reply);  // 完成后以服务端完整回复渲染 Markdown
                    UpdateQuota(reply);
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    thinking.Text = $"[错误] {ex.Message}";
                    thinking.Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0x40, 0x40));
                });
                // 错误不进 history, 避免后续对话被污染
            }
        }
        finally { _sending = false; }
    }

    private void AppendUser(string text)
    {
        var list = this.FindControl<StackPanel>("MessageList")!;
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0xE5, 0xF1, 0xFB)),
            Padding = new Thickness(8, 6),
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Right,
            MaxWidth = 240
        };
        border.Child = new SelectableTextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 12 };
        list.Children.Add(border);
        ScrollToBottom();
    }

    private SelectableTextBlock AppendAssistant(string text)
    {
        var list = this.FindControl<StackPanel>("MessageList")!;
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
            Padding = new Thickness(8, 6),
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Left,
            MaxWidth = 240
        };
        var tb = new SelectableTextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 12 };
        border.Child = tb;
        list.Children.Add(border);
        ScrollToBottom();
        return tb;
    }

    private void AppendSystem(string text)
    {
        var list = this.FindControl<StackPanel>("MessageList")!;
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xFB, 0xE6)),
            Padding = new Thickness(8, 6),
            CornerRadius = new CornerRadius(4),
            MaxWidth = 260
        };
        border.Child = new SelectableTextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0x87, 0x68, 0x00))
        };
        list.Children.Add(border);
        ScrollToBottom();
    }

    // ==================== 轻量 Markdown 渲染 ====================
    // 覆盖客服回复常见语法: 标题(#/##/###)、无序/有序列表、**粗体**、`行内代码`、换行.
    // 渲染进 SelectableTextBlock 的 Inlines, 保留选择+复制能力; 不引第三方依赖.

    private static readonly Regex InlineRx =
        new(@"(\*\*(?<b>.+?)\*\*)|(`(?<c>.+?)`)", RegexOptions.Compiled);

    private static void RenderMarkdown(SelectableTextBlock tb, string md)
    {
        tb.Text = null;
        tb.Inlines!.Clear();
        var lines = md.Replace("\r\n", "\n").Split('\n');
        bool first = true;
        foreach (var raw in lines)
        {
            if (!first) tb.Inlines.Add(new LineBreak());
            first = false;

            var line = raw;
            double size = 12;
            var weight = FontWeight.Normal;

            if (line.StartsWith("### ")) { size = 12; weight = FontWeight.Bold; line = line[4..]; }
            else if (line.StartsWith("## ")) { size = 14; weight = FontWeight.Bold; line = line[3..]; }
            else if (line.StartsWith("# ")) { size = 16; weight = FontWeight.Bold; line = line[2..]; }
            else if (line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("+ ")) { line = "• " + line[2..]; }

            AddInlines(tb.Inlines, line, size, weight);
        }
    }

    private static void AddInlines(InlineCollection target, string text, double size, FontWeight baseWeight)
    {
        int last = 0;
        foreach (Match m in InlineRx.Matches(text))
        {
            if (m.Index > last)
                target.Add(MakeRun(text[last..m.Index], size, baseWeight, false));
            if (m.Groups["b"].Success)
                target.Add(MakeRun(m.Groups["b"].Value, size, FontWeight.Bold, false));
            else if (m.Groups["c"].Success)
                target.Add(MakeRun(m.Groups["c"].Value, size, baseWeight, true));
            last = m.Index + m.Length;
        }
        if (last < text.Length)
            target.Add(MakeRun(text[last..], size, baseWeight, false));
    }

    private static Run MakeRun(string s, double size, FontWeight w, bool code)
    {
        var r = new Run(s) { FontSize = size, FontWeight = w };
        if (code) r.FontFamily = new FontFamily("Consolas, Courier New, monospace");
        return r;
    }

    private void UpdateQuota(ChatReply r)
    {
        var lbl = this.FindControl<TextBlock>("QuotaLabel")!;
        lbl.Text = $"消耗 {r.Usage.TotalTokens} tokens · 今日剩 {r.Quota.DailyRemaining} 次";
    }

    private void ScrollToBottom()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var sv = this.FindControl<ScrollViewer>("MessageScroll");
            sv?.ScrollToEnd();
        }, DispatcherPriority.Background);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
