using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using MdModManager.Models;

namespace MdModManager.Services;

public interface ILocalModService
{
    List<LocalMod> GetLocalMods(List<ModInfo> remoteMods, string? stagingPath = null);
    Task<string> ReadGameVersionAsync(string gamePath);
    Task DisableModAsync(LocalMod mod);
    Task EnableModAsync(LocalMod mod);
    Task DeleteModAsync(LocalMod mod);
}

public class LocalModService : ILocalModService
{
    private readonly IConfigService _configService;

    public LocalModService(IConfigService configService)
    {
        _configService = configService;
    }

    public List<LocalMod> GetLocalMods(List<ModInfo> remoteMods, string? stagingPath = null)
    {
        var gamePath = _configService.Config.GamePath;
        if (string.IsNullOrEmpty(gamePath))
            return new List<LocalMod>();

        var modsFolderPath = Path.Combine(gamePath, "Mods");
        if (!Directory.Exists(modsFolderPath))
            return new List<LocalMod>();

        var result = new List<LocalMod>();

        // --- 扫描正式 Mods 文件夹 ---
        foreach (var file in Directory.GetFiles(modsFolderPath))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext != ".dll" && ext != ".disabled") continue;

            var localMod = ParseModFile(file);
            if (localMod != null)
            {
                localMod.RemoteInfo = FindRemoteMatch(localMod.Name, localMod.Author, localMod.FilePath, remoteMods);
                result.Add(localMod);
            }
        }

        // --- 扫描暂存文件夹（Mods_Staging） ---
        if (!string.IsNullOrEmpty(stagingPath) && Directory.Exists(stagingPath))
        {
            // 收集已有文件名，防止重复
            var existingFileNames = new HashSet<string>(
                result.Select(m => Path.GetFileName(m.FilePath)),
                StringComparer.OrdinalIgnoreCase);

            foreach (var file in Directory.GetFiles(stagingPath, "*.dll"))
            {
                if (existingFileNames.Contains(Path.GetFileName(file))) continue;

                var localMod = ParseModFile(file);
                if (localMod != null)
                {
                    localMod.IsStaged = true;
                    // 版本号标注"(暂存)"
                    if (!string.IsNullOrEmpty(localMod.Version))
                        localMod.Version += " (暂存)";
                    else
                        localMod.Version = "暂存";
                    result.Add(localMod);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 读取 Muse Dash 的游戏版本号（bundleVersion），与 Euterpe 采用相同方式：
    /// 解析 MuseDash_Data/globalgamemanagers 文件的 PlayerSettings.bundleVersion 字段。
    /// 失败时返回空字符串，兼容性检测将被跳过（所有 mod 视为兼容）。
    /// </summary>
    public Task<string> ReadGameVersionAsync(string gamePath)
    {
        try
        {
            var bundlePath = Path.Combine(gamePath, "MuseDash_Data", "globalgamemanagers");
            if (!File.Exists(bundlePath))
            {
                Console.WriteLine($"[LocalModService] globalgamemanagers 不存在: {bundlePath}");
                return Task.FromResult(string.Empty);
            }

            // 从嵌入资源加载 classdata.tpk
            var tpkStream = System.Reflection.Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("MdModManager.Assets.classdata.tpk");
            if (tpkStream == null)
            {
                Console.WriteLine("[LocalModService] 找不到嵌入资源 classdata.tpk");
                return Task.FromResult(string.Empty);
            }

            var assetsManager = new AssetsTools.NET.Extra.AssetsManager();
            assetsManager.LoadClassPackage(tpkStream);

            var instance = assetsManager.LoadAssetsFile(bundlePath, true);
            assetsManager.LoadClassDatabaseFromPackage(instance.file.Metadata.UnityVersion);

            var playerSettingsInfo = instance.file.GetAssetsOfType(129)[0]; // 129 = PlayerSettings
            var playerSettings = assetsManager.GetBaseField(instance, playerSettingsInfo);
            var bundleVersion = playerSettings["bundleVersion"].AsString;

            assetsManager.UnloadAll();
            Console.WriteLine($"[LocalModService] 读取游戏版本成功: {bundleVersion}");
            return Task.FromResult(bundleVersion);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalModService] 读取游戏版本失败: {ex.Message}");
            return Task.FromResult(string.Empty);
        }
    }

    /// <summary>
    /// 根据本地 Mod 匹配远端 ModInfo。
    /// 匹配优先级：
    /// 1. 名字完全一致（不区分大小写）
    /// 2. DLL 文件名（不含扩展名）与远端名字或文件名一致（忽略空格与大小写），
    ///    并且作者字段至少有一个词相同（防止不同 Mod 因文件名相似而误匹配）
    /// </summary>
    private static ModInfo? FindRemoteMatch(
        string localName, string localAuthor, string localFilePath, List<ModInfo> remoteMods)
    {
        // 1) 精确名字匹配（不区分大小写）
        var exactMatch = remoteMods.FirstOrDefault(r =>
            r.Name.Equals(localName, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null) return exactMatch;

        // 2) 模糊文件名匹配，附加作者词语重叠校验
        var localFileNameWithoutExt = Path.GetFileNameWithoutExtension(localFilePath);
        var localNameNorm = Normalize(localName);
        var localFileNorm = Normalize(localFileNameWithoutExt);

        foreach (var r in remoteMods)
        {
            var remoteNameNorm = Normalize(r.Name);
            var remoteFileNorm = Normalize(Path.GetFileNameWithoutExtension(r.FileName));

            bool nameMatch = remoteNameNorm == localNameNorm
                          || remoteNameNorm == localFileNorm
                          || remoteFileNorm == localNameNorm
                          || remoteFileNorm == localFileNorm;

            if (!nameMatch) continue;

            // 如果本地有作者信息，要求和远端作者至少一个词重叠；否则跳过作者校验
            if (!string.IsNullOrWhiteSpace(localAuthor) && !AuthorsOverlap(localAuthor, r.Author))
                continue;

            return r;
        }

        return null;
    }

    /// <summary>规范化字符串：去掉空格并转小写，用于模糊比较。</summary>
    private static string Normalize(string s) =>
        s.Replace(" ", "").ToLowerInvariant();

    /// <summary>
    /// 判断两个作者字符串是否有至少一个公共词（以逗号、&、空格等分隔）。
    /// 公共词长度至少 3 个字符，避免 'a'、'by' 等无意义单词误触发。
    /// </summary>
    private static bool AuthorsOverlap(string localAuthor, string remoteAuthor)
    {
        if (string.IsNullOrWhiteSpace(remoteAuthor)) return true;

        var sep = new[] { ',', '&', ';', '/' };
        var localWords  = localAuthor .Split(sep, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(w => w.Trim().ToLowerInvariant())
                                      .Where(w => w.Length >= 3)
                                      .ToHashSet();
        var remoteWords = remoteAuthor.Split(sep, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(w => w.Trim().ToLowerInvariant())
                                      .Where(w => w.Length >= 3);

        return remoteWords.Any(w => localWords.Contains(w));
    }

    private LocalMod? ParseModFile(string filePath)
    {
        try
        {
            var module = ModuleDefinition.FromFile(filePath);
            var melonInfo = module.Assembly?.CustomAttributes
                .FirstOrDefault(c => c.Constructor?.DeclaringType?.Name == "MelonInfoAttribute");

            var mod = new LocalMod
            {
                FilePath = filePath,
                IsDisabled = filePath.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase)
            };

            if (melonInfo != null && melonInfo.Signature != null && melonInfo.Signature.FixedArguments.Count >= 4)
            {
                mod.Name = melonInfo.Signature.FixedArguments[1].Element?.ToString() ?? Path.GetFileNameWithoutExtension(filePath);
                mod.Version = melonInfo.Signature.FixedArguments[2].Element?.ToString() ?? "";
                mod.Author = melonInfo.Signature.FixedArguments[3].Element?.ToString() ?? "";
            }
            else
            {
                mod.Name = Path.GetFileNameWithoutExtension(filePath);
            }

            return mod;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse {filePath}: {ex}");
            return null;
        }
    }

    public async Task DisableModAsync(LocalMod mod)
    {
        if (mod.IsDisabled) return;

        await Task.Run(() =>
        {
            var newPath = mod.FilePath + ".disabled";
            File.Move(mod.FilePath, newPath);
            mod.FilePath = newPath;
            mod.IsDisabled = true;
        });
    }

    public async Task EnableModAsync(LocalMod mod)
    {
        if (!mod.IsDisabled) return;

        await Task.Run(() =>
        {
            var newPath = mod.FilePath.Substring(0, mod.FilePath.Length - ".disabled".Length);
            File.Move(mod.FilePath, newPath);
            mod.FilePath = newPath;
            mod.IsDisabled = false;
        });
    }

    public async Task DeleteModAsync(LocalMod mod)
    {
        await Task.Run(() =>
        {
            if (File.Exists(mod.FilePath))
            {
                File.Delete(mod.FilePath);
            }
        });
    }
}
