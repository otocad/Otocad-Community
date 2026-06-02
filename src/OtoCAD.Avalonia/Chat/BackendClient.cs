using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OtoCAD.Avalonia.Chat;

/// <summary>
/// OtoCAD.Backend HTTP 客户端 — 封装 /api/v1/chat/send + /api/v1/health.
/// 默认 BaseUrl 指向官方 cloud 助手 https://cadask.optic.chat —
/// 开源版即用此官方 cloud AI 助手作占位 (无需自建后端);
/// 本地开发可用环境变量 OTOCAD_BACKEND_URL 覆盖为 http://localhost:5000.
///
/// DeviceId: 持久化到 %APPDATA%/OtoCAD/device_id, 首次启动自动生成 GUID.
/// </summary>
public sealed partial class BackendClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _deviceId;

    public string DeviceId => _deviceId;
    public string BaseUrl { get; }

    public BackendClient(string baseUrl = "https://cadask.optic.chat")
    {
        BaseUrl = baseUrl.TrimEnd('/');
        _deviceId = GetOrCreateDeviceId();
        _http = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(60)
        };
        _http.DefaultRequestHeaders.Add("X-Device-ID", _deviceId);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("OtoCAD.Avalonia/0.3.1");
    }

    /// <summary>健康检查 — 用于判定后端是否在线.</summary>
    public async Task<bool> PingAsync()
    {
        try
        {
            var resp = await _http.GetAsync("/api/v1/health");
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    /// <summary>发送聊天 — 失败抛异常 (调用方需 catch + 显示提示).</summary>
    public async Task<ChatReply> ChatAsync(IReadOnlyList<ChatTurn> history)
    {
        var req = new { messages = history };
        var resp = await _http.PostAsJsonAsync("/api/v1/chat/send", req);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"后端 {(int)resp.StatusCode}: {body}");
        var parsed = JsonSerializer.Deserialize<ChatReply>(body, JsonOpts)
                     ?? throw new InvalidOperationException("后端返回空响应");
        return parsed;
    }

    /// <summary>流式聊天 — 逐字回调 onDelta, 返回最终完整响应 (含 quota/usage). 失败抛异常.</summary>
    public async Task<ChatReply> ChatStreamAsync(IReadOnlyList<ChatTurn> history, Action<string> onDelta, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/chat/stream")
        {
            Content = JsonContent.Create(new { messages = history })
        };
        using var resp = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        ChatReply? final = null;
        string? error = null;

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (!line.StartsWith("data:")) continue;
            var data = line["data:".Length..].Trim();
            if (data.Length == 0) continue;
            if (data == "[DONE]") break;

            using var doc = JsonDocument.Parse(data);
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out var e)) { error = e.GetString(); break; }
            if (root.TryGetProperty("done", out var d) && d.GetBoolean())
            {
                final = JsonSerializer.Deserialize<ChatReply>(data, JsonOpts);
                continue;
            }
            if (root.TryGetProperty("delta", out var delta))
                onDelta(delta.GetString() ?? "");
        }

        if (error is not null) throw new InvalidOperationException($"后端: {error}");
        return final ?? throw new InvalidOperationException("后端未返回完整响应");
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static string GetOrCreateDeviceId()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OtoCAD");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "device_id");
        if (File.Exists(path))
        {
            var existing = File.ReadAllText(path).Trim();
            if (existing.Length is >= 8 and <= 64) return existing;
        }
        var id = Guid.NewGuid().ToString("N");
        File.WriteAllText(path, id);
        return id;
    }

    public void Dispose() => _http.Dispose();
}

public sealed class ChatTurn
{
    public string Role { get; set; } = "user";       // user / assistant / system
    public string Content { get; set; } = "";
}

public sealed class ChatReply
{
    public string Reply { get; set; } = "";
    public string MessageId { get; set; } = "";
    public TokenUsageDto Usage { get; set; } = new();
    public QuotaDto Quota { get; set; } = new();
}

public sealed class TokenUsageDto
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

public sealed class QuotaDto
{
    public int HourlyRemaining { get; set; }
    public int DailyRemaining { get; set; }
    public int TokensRemaining { get; set; }
}
