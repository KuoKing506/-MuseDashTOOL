using System.Text.Json.Serialization;

namespace MdModManager.Models;

public class AppConfig
{
    public string GamePath { get; set; } = "";

    public string ModLinksUrl { get; set; } =
        "https://gitee.com/lxymahatma/ModLinks/raw/dev/Mods.json";
        
    public string DownloadSource { get; set; } = "ghproxy.net";

    /// <summary>永久关闭"下载不兼容 mod"的二次确认弹窗</summary>
    public bool SuppressIncompatibleModWarning { get; set; } = false;

    /// <summary>自动翻译 Mod 详情信息为中文</summary>
    public bool AutoTranslateDescriptions { get; set; } = false;

    /// <summary>删除时不再显示确认弹窗</summary>
    public bool SuppressDeleteConfirmation { get; set; } = false;

    /// <summary>谱面名称过长时滚动显示</summary>
    public bool EnableChartNameMarquee { get; set; } = true;

    /// <summary>启动游戏时不再显示确认弹窗</summary>
    public bool SuppressLaunchGameConfirmation { get; set; } = false;

    /// <summary>谱面试听音量</summary>
    public double ChartPreviewVolume { get; set; } = 0.5;

    /// <summary>是否解锁了隐藏的自定义背景功能</summary>
    public bool IsSecretBackgroundUnlocked { get; set; } = false;

    /// <summary>自定义背景图片的路径</summary>
    public string CustomBackgroundImagePath { get; set; } = "";

    /// <summary>自定义背景图片的透明度</summary>
    public double CustomBackgroundOpacity { get; set; } = 0.2;

    /// <summary>隐藏模式下自定义的主题文字颜色 (为空时代表使用默认颜色)</summary>
    public string CustomThemeTextColor { get; set; } = "";
}

[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(ModLinks))]
[JsonSerializable(typeof(ModInfo[]))]
[JsonSerializable(typeof(GitHubRelease[]))]
internal partial class AppJsonContext : JsonSerializerContext { }
