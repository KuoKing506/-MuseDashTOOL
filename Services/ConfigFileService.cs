using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MdModManager.Models;

namespace MdModManager.Services;

public interface IConfigFileService
{
    /// <summary>扫描 UserData 目录（含一级子目录）中的所有 .cfg 文件</summary>
    Task<List<CfgFile>> ScanUserDataAsync(string gamePath);

    /// <summary>解析单个 .cfg 文件</summary>
    CfgFile ParseCfgFile(string filePath, string userDataRoot);

    /// <summary>将修改写回磁盘</summary>
    Task SaveCfgFileAsync(CfgFile cfgFile);

    /// <summary>删除指定 Section 的全部内容</summary>
    Task DeleteSectionAsync(CfgFile cfgFile, string sectionName);
}

public class ConfigFileService : IConfigFileService
{
    // 匹配注释中带引号的选项值，例如 "OneLine", "TwoLines"
    private static readonly Regex OptionsRegex = new Regex(
        "\"([^\"]+)\"",
        RegexOptions.Compiled);

    public Task<List<CfgFile>> ScanUserDataAsync(string gamePath)
    {
        return Task.Run(() =>
        {
            var results = new List<CfgFile>();

            if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
                return results;

            var userDataPath = Path.Combine(gamePath, "UserData");
            if (!Directory.Exists(userDataPath))
                return results;

            var allowedExtensions = new[] { ".cfg", ".yml", ".yaml", ".ini", ".txt", ".json" };

            // 扫描 UserData 根目录下的所有支持的配置文件
            foreach (var file in Directory.GetFiles(userDataPath))
            {
                if (allowedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                {
                    results.Add(ParseCfgFile(file, userDataPath));
                }
            }

            // 扫描 UserData 一级子目录下的所有支持的配置文件
            foreach (var subDir in Directory.GetDirectories(userDataPath))
            {
                var folderName = Path.GetFileName(subDir);
                if (string.Equals(folderName, "backups", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    foreach (var file in Directory.GetFiles(subDir, "*.*", SearchOption.AllDirectories))
                    {
                        if (!allowedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                            continue;

                        var cfg = ParseCfgFile(file, userDataPath);
                        // 如果子目录里的配置文件由于没有具体的区块名导致 ModSection 为空，强制拿它的父文件夹名(Mod名)进行分组显示
                        if (string.IsNullOrWhiteSpace(cfg.ModSection))
                        {
                            cfg.ModSection = folderName;
                        }
                        results.Add(cfg);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ConfigFileService] 扫描文件夹 {folderName} 失败: {ex.Message}");
                }
            }

            return results.OrderBy(f => f.DisplayName).ToList();
        });
    }

    public CfgFile ParseCfgFile(string filePath, string userDataRoot)
    {
        var cfgFile = new CfgFile
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            RelativePath = Path.GetRelativePath(userDataRoot, filePath)
        };

        if (!File.Exists(filePath))
            return cfgFile;

        var lines = File.ReadAllLines(filePath, Encoding.UTF8);
        var currentCommentLines = new List<string>();
        string currentSection = string.Empty;
        bool hasAddedFirstSectionHeader = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // 空行：重置注释缓冲区（不跨越空行的注释才算是配置项的注释）
            if (string.IsNullOrEmpty(trimmed))
            {
                if (currentCommentLines.Count > 0)
                {
                    // 如果当前注释后面是空行，保留注释等待下一个键
                }
                continue;
            }

            // Section 标头：[Bnfour_SongInfo]
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                currentSection = trimmed[1..^1];
                
                // 初次解析第一个 Section 作为整个文件的主要依赖名称（后向兼容）
                if (string.IsNullOrEmpty(cfgFile.ModSection))
                {
                    cfgFile.ModSection = currentSection;
                }
                
                // 下次遇到新配置项时，就会触发绘制带有 SectionName 标签的项
                hasAddedFirstSectionHeader = false;
                
                currentCommentLines.Clear();
                continue;
            }

            // 注释行：# ... 或 // ...
            if (trimmed.StartsWith("#") || trimmed.StartsWith("//"))
            {
                // 去掉前缀和空格，保留注释文本
                currentCommentLines.Add(trimmed.TrimStart('#', '/').Trim());
                continue;
            }

            // 键值对：Key = Value 或 Key: Value
            // 找出第一个出现的 = 或 :，作为键值分隔符
            int eqIndex = line.IndexOf('=');
            int colonIndex = line.IndexOf(':');

            int splitIndex = -1;
            if (eqIndex > 0 && colonIndex > 0) splitIndex = Math.Min(eqIndex, colonIndex);
            else if (eqIndex > 0) splitIndex = eqIndex;
            else if (colonIndex > 0) splitIndex = colonIndex;

            if (splitIndex > 0)
            {
                var key = line[..splitIndex].Trim();
                var value = line[(splitIndex + 1)..].Trim();

                var rawComment = string.Join("\n", currentCommentLines);

                // 如果当前有一个新的 Section，我们需要在列表中插入一个虚拟的 UI 分割块（Title）
                if (!string.IsNullOrEmpty(currentSection) && !hasAddedFirstSectionHeader)
                {
                    cfgFile.Entries.Add(new CfgEntry
                    {
                        IsSectionHeader = true,
                        SectionName = currentSection
                    });
                    hasAddedFirstSectionHeader = true;
                }

                // 从注释中提取可选值
                var options = ExtractOptions(rawComment);

                // 如果注释中没有选项，但值本身是 true/false，则智能注入布尔选项
                if (options.Count == 0)
                {
                    var lowerVal = value.ToLower();
                    if (lowerVal == "true" || lowerVal == "false" || lowerVal == "\"true\"" || lowerVal == "\"false\"")
                    {
                        bool hasQuotes = value.Contains("\"");
                        string cleanVal = value.Replace("\"", "");
                        bool isCapital = cleanVal.Length > 0 && char.IsUpper(cleanVal[0]);

                        if (hasQuotes)
                            options = isCapital ? new List<string> { "\"True\"", "\"False\"" } : new List<string> { "\"true\"", "\"false\"" };
                        else
                            options = isCapital ? new List<string> { "True", "False" } : new List<string> { "true", "false" };
                        
                        // 为了确保 ComboBox 默认能选中当前值，把当前精确的值放在第一项（若去重后）
                        if (!options.Contains(value))
                            options.Insert(0, value);
                    }
                }

                var entry = new CfgEntry
                {
                    Key = key,
                    Value = value,
                    RawComment = rawComment,
                    DisplayComment = rawComment,
                    AvailableOptions = options,
                    IsModified = false,
                    SectionName = currentSection
                };

                cfgFile.Entries.Add(entry);
                currentCommentLines.Clear();
            }
        }

        return cfgFile;
    }

    private static List<string> ExtractOptions(string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return new List<string>();

        var matches = OptionsRegex.Matches(comment);
        if (matches.Count < 2)
            return new List<string>(); // 少于两个选项则不当作可选项

        // m.Value 代表整个匹配项（包含首尾的引号），这样才能和带有引号的真实 Value 字符串进行完美相等匹配
        return matches.Select(m => m.Value).Distinct().ToList();
    }

    public async Task SaveCfgFileAsync(CfgFile cfgFile)
    {
        if (!File.Exists(cfgFile.FilePath))
            return;

        var originalLines = await File.ReadAllLinesAsync(cfgFile.FilePath, Encoding.UTF8);
        var newLines = new List<string>();

        // 构建一个快速查找字典：(SectionName, key) -> 新值
        var updatedValues = new Dictionary<(string Section, string Key), string>();
        foreach (var e in cfgFile.Entries.Where(e => !e.IsSectionHeader && e.IsModified))
        {
            updatedValues[(e.SectionName ?? "", e.Key)] = e.Value;
        }

        if (updatedValues.Count == 0)
            return; // 没有修改，无需写入

        string currentSectionInFile = string.Empty;

        foreach (var line in originalLines)
        {
            var trimmed = line.Trim();
            
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                currentSectionInFile = trimmed[1..^1];
                newLines.Add(line);
                continue;
            }

            var eqIndex = trimmed.IndexOf('=');
            if (eqIndex < 0) eqIndex = trimmed.IndexOf(':');

            if (eqIndex > 0 && !trimmed.StartsWith("#") && !trimmed.StartsWith("//"))
            {
                var key = trimmed[..eqIndex].Trim();
                if (updatedValues.TryGetValue((currentSectionInFile, key), out var newValue))
                {
                    // 提取原始行中分隔符（含）之前的部分，以保留原始的缩进格式和具体的分隔符（是=还是:）
                    var prefix = line[..(line.IndexOf(trimmed[eqIndex]) + 1)];
                    newLines.Add($"{prefix} {newValue}");
                    continue;
                }
            }

            newLines.Add(line);
        }

        await File.WriteAllLinesAsync(cfgFile.FilePath, newLines, Encoding.UTF8);

        // 保存成功后，重置 IsModified 标志
        foreach (var entry in cfgFile.Entries)
            entry.IsModified = false;
    }
    public async Task DeleteSectionAsync(CfgFile cfgFile, string sectionName)
    {
        if (!File.Exists(cfgFile.FilePath))
            return;

        var originalLines = await File.ReadAllLinesAsync(cfgFile.FilePath, Encoding.UTF8);
        var newLines = new List<string>();
        
        bool insideTargetSection = false;

        foreach (var line in originalLines)
        {
            var trimmed = line.Trim();
            
            // 检查是不是一个新的 Section 标头
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                var currentSection = trimmed[1..^1];
                if (currentSection == sectionName)
                {
                    // 找到了要删除的目标 Section，开启跳过标记
                    insideTargetSection = true;
                    continue; // 抛弃该行
                }
                else
                {
                    // 遇到其它的 Section，关闭跳过标记
                    insideTargetSection = false;
                }
            }

            // 如果处于目标 Section 中，且不是空行/纯注释等独立于 Section 之外的内容(可选择性保留空行, 这里选择严格跳过)
            if (insideTargetSection)
            {
                continue; // 抛弃该行
            }

            newLines.Add(line);
        }

        // 去除文件末尾由于删除导致的连续多余空行
        while (newLines.Count > 0 && string.IsNullOrWhiteSpace(newLines.Last()))
        {
            newLines.RemoveAt(newLines.Count - 1);
        }

        await File.WriteAllLinesAsync(cfgFile.FilePath, newLines, Encoding.UTF8);
    }
}
