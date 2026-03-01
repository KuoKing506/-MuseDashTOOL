using System.Collections.Generic;
using System.Linq;

namespace MdModManager.Models;

/// <summary>表示一个解析后的 .cfg 配置文件</summary>
public class CfgFile
{
    /// <summary>磁盘上的完整路径</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>文件名，例如 "SongInfo.cfg"</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>配置文件中的 Section 名称，例如 "[Bnfour_SongInfo]" -> "Bnfour_SongInfo"</summary>
    public string ModSection { get; set; } = string.Empty;

    /// <summary>所有配置项（键值对 + 注释）</summary>
    public System.Collections.ObjectModel.ObservableCollection<CfgEntry> Entries { get; set; } = new();

    /// <summary>显示在列表中的主名称。优先使用 ModSection，如果是自动生成的顶层文件夹名 fallback 且没有真实 section，则强制使用文件名。</summary>
    public string DisplayName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ModSection))
                return Path.GetFileNameWithoutExtension(FileName);

            var dirSegment = RelativePath.Split(
                new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, 
                StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            if (ModSection == dirSegment)
                return Path.GetFileNameWithoutExtension(FileName);

            return ModSection;
        }
    }

    /// <summary>显示文件来源路径（相对于 UserData 的部分）</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>是否已经经过自动翻译处理（避免重复翻译）</summary>
    public bool HasBeenTranslated { get; set; }

    /// <summary>截断后的文件路径（显示前三个目录和文件名）</summary>
    public string TruncatedFilePath
    {
        get
        {
            if (string.IsNullOrEmpty(FilePath)) return string.Empty;
            var parts = FilePath.Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            if (parts.Length <= 4) return FilePath;

            var firstThree = string.Join(System.IO.Path.DirectorySeparatorChar.ToString(), parts.Take(3));
            return $"{firstThree}{System.IO.Path.DirectorySeparatorChar}...{System.IO.Path.DirectorySeparatorChar}{parts.Last()}";
        }
    }
}
