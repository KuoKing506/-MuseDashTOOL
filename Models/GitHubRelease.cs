using System.Text.Json.Serialization;

namespace MdModManager.Models;

/// <summary>GitHub Releases API 的单个 Release 对象</summary>
public class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }

    [JsonPropertyName("published_at")]
    public DateTimeOffset PublishedAt { get; set; }

    [JsonPropertyName("assets")]
    public GitHubAsset[] Assets { get; set; } = [];

    // 显示名称：去掉 v 前缀
    public string DisplayName => TagName.TrimStart('v');
}

public class GitHubAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("browser_download_url")]
    public string DownloadUrl { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }
}
