using CommunityToolkit.Mvvm.ComponentModel;

namespace MdModManager.Services;

/// <summary>
/// 全局单例服务：管理游戏运行中的 Mod 暂存目录，并暴露 HasPendingFiles 属性供 UI 绑定。
/// </summary>
public partial class ModStagingService : ObservableObject
{
    [ObservableProperty]
    private bool _hasPendingFiles;

    /// <summary>
    /// 根据游戏路径返回暂存文件夹路径 (GamePath/Mods_Staging)
    /// </summary>
    public string GetStagingPath(string gamePath) =>
        System.IO.Path.Combine(gamePath, "Mods_Staging");

    /// <summary>
    /// 确保暂存目录存在，并刷新 HasPendingFiles 状态
    /// </summary>
    public void EnsureAndRefresh(string gamePath)
    {
        if (string.IsNullOrEmpty(gamePath)) return;
        var stagingPath = GetStagingPath(gamePath);
        System.IO.Directory.CreateDirectory(stagingPath);
        Refresh(gamePath);
    }

    /// <summary>
    /// 重新检查暂存目录内是否有文件，并更新 HasPendingFiles
    /// </summary>
    public void Refresh(string gamePath)
    {
        if (string.IsNullOrEmpty(gamePath))
        {
            HasPendingFiles = false;
            return;
        }
        var stagingPath = GetStagingPath(gamePath);
        if (!System.IO.Directory.Exists(stagingPath))
        {
            HasPendingFiles = false;
            return;
        }
        HasPendingFiles = System.IO.Directory.GetFiles(stagingPath, "*.dll").Length > 0;
    }
}
