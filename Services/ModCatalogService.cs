using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MdModManager.Models;

namespace MdModManager.Services;

public interface IModCatalogService
{
    Task<List<ModInfo>> GetModsAsync(CancellationToken cancellationToken = default);
}

public class ModCatalogService : IModCatalogService
{
    private readonly IConfigService _configService;
    private readonly HttpClient _httpClient;

    // 反序列化选项：
    // - 纯反射模式，不使用 AOT Source Generator，避免与其他选项发生冲突
    // - 大小写不敏感，兼容字段名大小写不一致的情况
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ModCatalogService(IConfigService configService)
    {
        _configService = configService;
        _httpClient = new HttpClient();
        // 设置 User-Agent，防止部分服务器拒绝空 UA 请求
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MdModManager/1.0");
    }

    /// <summary>
    /// 从配置的 URL 下载 Mod 列表，兼容两种 JSON 格式：
    /// - 旧格式：{ "Mods": [ ... ] }（来自 GitHub MDMods/MuseDashModLinks）
    /// - 新格式：[ ... ]（来自 Gitee lxymahatma/ModLinks dev 分支）
    /// </summary>
    public async Task<List<ModInfo>> GetModsAsync(CancellationToken cancellationToken = default)
    {
        var url = _configService.Config.ModLinksUrl;
        var response = await _httpClient.GetStringAsync(url, cancellationToken);

        // 先尝试对象格式：{ "Mods": [...] }
        try
        {
            var modLinks = JsonSerializer.Deserialize<ModLinks>(response, _jsonOptions);
            if (modLinks?.Mods != null && modLinks.Mods.Length > 0)
                return new List<ModInfo>(modLinks.Mods);
        }
        catch (JsonException)
        {
            // 不是对象格式（JSON 数组会触发此异常），继续尝试数组格式
        }

        // 再尝试纯数组格式：[...]
        var modArray = JsonSerializer.Deserialize<ModInfo[]>(response, _jsonOptions);
        return new List<ModInfo>(modArray ?? Array.Empty<ModInfo>());
    }
}
