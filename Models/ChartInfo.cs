using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace MdModManager.Models;

public partial class ChartInfo : ObservableObject
{
    /// <summary>mdm 文件的完整路径</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>歌曲名称（来自 info.json 或文件名）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>歌曲作者/作曲</summary>
    public string? MusicAuthor { get; set; }

    /// <summary>谱面设计者/Mapper</summary>
    public string? ChartAuthor { get; set; }

    /// <summary>难度列表（如 Easy / Hard / Master）</summary>
    public List<string> Difficulties { get; set; } = new();

    /// <summary>BPM信息</summary>
    public string? Bpm { get; set; }

    /// <summary>从 ZIP 内 PNG 加载的封面图</summary>
    public Bitmap? CoverImage { get; set; }

    /// <summary>ZIP 内试听音频的 entry 名称</summary>
    public string? DemoEntryName { get; set; }

    /// <summary>是否正在试听中</summary>
    [ObservableProperty]
    private bool _isPlaying;

    /// <summary>是否在本次进程中新下载的</summary>
    [ObservableProperty]
    private bool _isNewDownload;

    /// <summary>副标题展示（作曲 + 谱师）</summary>
    public string SubInfo
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(MusicAuthor)) parts.Add($"曲：{MusicAuthor}");
            if (!string.IsNullOrEmpty(ChartAuthor)) parts.Add($"谱：{ChartAuthor}");
            return string.Join("  ", parts);
        }
    }

    /// <summary>难度标签文字（逗号连接）</summary>
    public string DifficultyText => Difficulties.Count > 0
        ? string.Join(" / ", Difficulties)
        : string.Empty;
}
