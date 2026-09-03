using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OtoCAD.Avalonia.Services;

/// <summary>
/// 极简更新检查 — 走 GitHub Releases API.
///
/// 设计取舍:
/// - 不依赖 AutoUpdater.NET (那是 WPF/WinForms, 嵌不进 Avalonia)
/// - 不自动下载/替换 (v0.1 用户自己点链接去 release 页下载)
/// - 不带任何 telemetry (符合 §1.1.b 后端 P2 降级)
/// </summary>
public static class UpdateChecker
{
    private const string ApiUrl = "https://api.github.com/repos/otocad/Otocad-Community/releases/latest";
    private const string UserAgent = "OtoCAD-UpdateCheck";

    public sealed class UpdateInfo
    {
        public required Version Current { get; init; }
        public required Version Latest { get; init; }
        public required string ReleaseTag { get; init; }
        public required string ReleaseUrl { get; init; }
        public required string ReleaseNotes { get; init; }
        public bool HasUpdate => Latest > Current;
    }

    /// <summary>
    /// 查询 GitHub 最新 Release. 网络/解析失败抛 <see cref="HttpRequestException"/> 或 <see cref="JsonException"/>;
    /// 调用方在 catch 里友好提示即可.
    /// </summary>
    public static async Task<UpdateInfo> CheckAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

        var json = await http.GetStringAsync(ApiUrl);
        var release = JsonSerializer.Deserialize<GitHubRelease>(json)
                      ?? throw new JsonException("GitHub release JSON 为空");

        var current = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0, 0);
        var latest = ParseTag(release.TagName);

        return new UpdateInfo
        {
            Current = current,
            Latest = latest,
            ReleaseTag = release.TagName,
            ReleaseUrl = release.HtmlUrl,
            ReleaseNotes = release.Body ?? string.Empty,
        };
    }

    /// <summary>"v0.1.0" / "0.1.0" / "v0.1.0-rc1" → Version (-rc1 截掉).</summary>
    private static Version ParseTag(string tag)
    {
        var s = tag?.TrimStart('v', 'V') ?? "0.0.0";
        var dash = s.IndexOf('-');
        if (dash > 0) s = s[..dash];
        return Version.TryParse(s, out var v) ? v : new Version(0, 0, 0, 0);
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")] public string TagName { get; set; } = "";
        [JsonPropertyName("html_url")] public string HtmlUrl { get; set; } = "";
        [JsonPropertyName("body")] public string? Body { get; set; }
    }
}
