using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MdModManager.Services;

namespace MdModManager.ViewModels;

/// <summary>
/// 设置页面的 ViewModel，管理全局配置
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigService _configService;

    // 可用的下载源列表
    [ObservableProperty]
    private string[] _downloadSources = new[] { "ghproxy.net", "kkgithub.com", "github.com" };

    /// <summary>
    /// 当前选定的下载源，与 ConfigService 同步
    /// </summary>
    public string SelectedDownloadSource
    {
        get => _configService.Config.DownloadSource;
        set
        {
            if (_configService.Config.DownloadSource != value)
            {
                _configService.Config.DownloadSource = value;
                OnPropertyChanged();
                
                // 异步保存配置，不阻塞 UI
                _ = _configService.SaveAsync();
            }
        }
    }

    public SettingsViewModel(IConfigService configService)
    {
        _configService = configService;
    }

    /// <summary>
    /// 永久关闭"下载不兼容 mod 确认弹窗"的开关，与 ConfigService 双向同步。
    /// </summary>
    public bool SuppressIncompatibleModWarning
    {
        get => _configService.Config.SuppressIncompatibleModWarning;
        set
        {
            if (_configService.Config.SuppressIncompatibleModWarning != value)
            {
                _configService.Config.SuppressIncompatibleModWarning = value;
                OnPropertyChanged();
                _ = _configService.SaveAsync();
            }
        }
    }

    /// <summary>
    /// 自动翻译 Mod 详情信息的开关，与 ConfigService 双向同步。
    /// </summary>
    public bool AutoTranslateDescriptions
    {
        get => _configService.Config.AutoTranslateDescriptions;
        set
        {
            if (_configService.Config.AutoTranslateDescriptions != value)
            {
                _configService.Config.AutoTranslateDescriptions = value;
                OnPropertyChanged();
                _ = _configService.SaveAsync();
            }
        }
    }

    /// <summary>删除时不再显示确认弹窗</summary>
    public bool SuppressDeleteConfirmation
    {
        get => _configService.Config.SuppressDeleteConfirmation;
        set
        {
            if (_configService.Config.SuppressDeleteConfirmation != value)
            {
                _configService.Config.SuppressDeleteConfirmation = value;
                OnPropertyChanged();
                _ = _configService.SaveAsync();
            }
        }
    }

    /// <summary>谱面名称过长时滚动显示</summary>
    public bool EnableChartNameMarquee
    {
        get => _configService.Config.EnableChartNameMarquee;
        set
        {
            if (_configService.Config.EnableChartNameMarquee != value)
            {
                _configService.Config.EnableChartNameMarquee = value;
                OnPropertyChanged();
                _ = _configService.SaveAsync();
            }
        }
    }

    /// <summary>启动游戏时不再显示确认弹窗</summary>
    public bool SuppressLaunchGameConfirmation
    {
        get => _configService.Config.SuppressLaunchGameConfirmation;
        set
        {
            if (_configService.Config.SuppressLaunchGameConfirmation != value)
            {
                _configService.Config.SuppressLaunchGameConfirmation = value;
                OnPropertyChanged();
                _ = _configService.SaveAsync();
            }
        }
    }

    /// <summary>谱面试听音量</summary>
    public double ChartPreviewVolume
    {
        get => _configService.Config.ChartPreviewVolume;
        set
        {
            if (Math.Abs(_configService.Config.ChartPreviewVolume - value) > 0.001)
            {
                _configService.Config.ChartPreviewVolume = value;
                OnPropertyChanged();
                _ = _configService.SaveAsync();
            }
        }
    }

    /// <summary>隐藏彩蛋是否解锁</summary>
    public bool IsSecretBackgroundUnlocked
    {
        get => _configService.Config.IsSecretBackgroundUnlocked;
        set
        {
            if (_configService.Config.IsSecretBackgroundUnlocked != value)
            {
                _configService.Config.IsSecretBackgroundUnlocked = value;
                OnPropertyChanged();
                _ = _configService.SaveAsync();
            }
        }
    }

    /// <summary>自定义背景图路径</summary>
    public string CustomBackgroundImagePath
    {
        get => _configService.Config.CustomBackgroundImagePath;
        set
        {
            if (_configService.Config.CustomBackgroundImagePath != value)
            {
                _configService.Config.CustomBackgroundImagePath = value;
                OnPropertyChanged();
                _ = _configService.SaveAsync();
                
                // 通知主窗口更新背景
                var desktop = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
                if (desktop?.MainWindow?.DataContext is MainWindowViewModel mwVm)
                {
                    mwVm.UpdateBackground();
                }
            }
        }
    }

    /// <summary>自定义背景图透明度</summary>
    public double CustomBackgroundOpacity
    {
        get => _configService.Config.CustomBackgroundOpacity;
        set
        {
            if (Math.Abs(_configService.Config.CustomBackgroundOpacity - value) > 0.001)
            {
                _configService.Config.CustomBackgroundOpacity = value;
                OnPropertyChanged();
                _ = _configService.SaveAsync();
                
                // 通知主窗口更新背景透明度
                var desktop = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
                if (desktop?.MainWindow?.DataContext is MainWindowViewModel mwVm)
                {
                    mwVm.UpdateBackground();
                }
            }
        }
    }

    /// <summary>隐藏模式下自定义的主题文字颜色</summary>
    public string CustomThemeTextColor
    {
        get => _configService.Config.CustomThemeTextColor;
        set
        {
            if (_configService.Config.CustomThemeTextColor != value)
            {
                _configService.Config.CustomThemeTextColor = value;
                OnPropertyChanged();
                _ = _configService.SaveAsync();

                // 通知主窗口更新主题文字颜色
                var desktop = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
                if (desktop?.MainWindow?.DataContext is MainWindowViewModel mwVm)
                {
                    mwVm.UpdateThemeColors();
                }
            }
        }
    }

    // 彩蛋气泡相关

    [ObservableProperty]
    private bool _isCustomColorInputVisible = false;

    // 会话层级点击计数（关闭应用重新打开后重置）
    private static int _authorClickCount = 0;

    /// <summary>
    /// 初始化设置项显示
    /// </summary>
    public void Initialize()
    {
        OnPropertyChanged(nameof(SelectedDownloadSource));
        OnPropertyChanged(nameof(SuppressIncompatibleModWarning));
        OnPropertyChanged(nameof(IsSecretBackgroundUnlocked));
        OnPropertyChanged(nameof(CustomBackgroundImagePath));
        OnPropertyChanged(nameof(CustomBackgroundOpacity));
    }
    
    [RelayCommand]
    private async System.Threading.Tasks.Task SelectBackgroundImageAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow != null)
            {
                var storageProvider = mainWindow.StorageProvider;
                var files = await storageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "选择自定义背景图片",
                    AllowMultiple = false,
                    FileTypeFilter = new[] 
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("Images")
                        {
                            Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp" }
                        }
                    }
                });

                if (files.Count > 0)
                {
                    CustomBackgroundImagePath = files[0].Path.LocalPath;
                }
            }
        }
    }

    [RelayCommand]
    private void ClearBackgroundImage()
    {
        CustomBackgroundImagePath = string.Empty;
        CustomThemeTextColor = string.Empty; // 当恢复一般模式时，删除掉自定义的字体颜色
        IsCustomColorInputVisible = false;
    }

    [RelayCommand]
    private void ToggleCustomThemeColorInput()
    {
        IsCustomColorInputVisible = !IsCustomColorInputVisible;
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task OpenAuthorHomepageAsync()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://space.bilibili.com/289883561?spm_id_from=333.1007.0.0",
                UseShellExecute = true
            });

            // 检查彩蛋开启状况
            if (!IsSecretBackgroundUnlocked)
            {
                _authorClickCount++;

                if (_authorClickCount == 1)
                {
                    var bubble = new MdModManager.Views.EasterEggWindow("再按一次有惊喜？！");
                    await bubble.ShowAndAutoCloseAsync();
                }
                else if (_authorClickCount == 2)
                {
                    IsSecretBackgroundUnlocked = true; // 永久解锁，同步写入配置
                    var bubble = new MdModManager.Views.EasterEggWindow("设置里好像多了什么东西");
                    await bubble.ShowAndAutoCloseAsync();
                }
            }
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"无法打开作者主页: {ex.Message}");
        }
    }
}
