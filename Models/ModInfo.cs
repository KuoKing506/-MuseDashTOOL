using System.Text.Json.Serialization;

namespace MdModManager.Models;

public class ModInfo
{
    // 字段名与 Gitee Mods.json 实际返回的小写 JSON key 精确匹配
    // AOT Source Generator 不支持 PropertyNameCaseInsensitive，必须逐字相同
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "";

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = "";

    // Gitee 没有 downloadLink 字段，通过 FileName 拼接，保留字段兼容老格式
    [JsonPropertyName("downloadLink")]
    public string DownloadLink { get; set; } = "";

    [JsonPropertyName("repository")]
    public string HomePage { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("configFile")]
    public string ConfigFile { get; set; } = "";

    // Gitee 用 "gameVersion" (string 而非 array), 为兼容两种格式保留 array 类型
    [JsonPropertyName("gameVersion")]
    public string GameVersion { get; set; } = "*";

    [JsonPropertyName("modDependencies")]
    public string[] DependentMods { get; set; } = [];

    [JsonPropertyName("libDependencies")]
    public string[] DependentLibs { get; set; } = [];

    [JsonPropertyName("incompatibleMods")]
    public string[] IncompatibleMods { get; set; } = [];

    [JsonPropertyName("sha256")]
    public string SHA256 { get; set; } = "";

    /// <summary>来源：Gitee / Euterpe</summary>
    public string Source { get; set; } = "Gitee";
}

public class ModLinks
{
    [JsonPropertyName("Mods")]
    public ModInfo[] Mods { get; set; } = [];

    [JsonPropertyName("Count")]
    public int Count { get; set; }
}
