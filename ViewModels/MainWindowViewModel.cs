using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using MdModManager.Services;

namespace MdModManager.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IConfigService? _configService;
    private readonly IGamePathService? _gamePathService;
    private readonly INotificationService? _notificationService;

    [ObservableProperty]
    private object? _currentPage;

    private CancellationTokenSource? _currentPageCts;

    [ObservableProperty]
    private string _gamePathStatus = "Checking...";

    /// <summary>绑定到左下角通知气泡列表</summary>
    public ObservableCollection<DownloadNotification> Notifications =>
        _notificationService?.Notifications ?? new ObservableCollection<DownloadNotification>();

    public string CustomBackgroundImagePath => _configService?.Config?.CustomBackgroundImagePath ?? string.Empty;
    public double CustomBackgroundOpacity => _configService?.Config?.CustomBackgroundOpacity ?? 0.2;

    [ObservableProperty]
    private Avalonia.Media.Imaging.Bitmap? _customBackgroundBitmap;

    [ObservableProperty]
    private bool _isNormalTheme = true;

    [ObservableProperty]
    private Avalonia.Media.IBrush _sidebarBackground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E1E1E"));

    [ObservableProperty]
    private Avalonia.Media.IBrush _contentBackground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E1E1E"));

    [ObservableProperty]
    private Avalonia.Media.IBrush _windowBackground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E1E1E"));

    [ObservableProperty]
    private Avalonia.Media.IBrush _themeTextMainBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E0E0E0"));

    [ObservableProperty]
    private Avalonia.Media.IBrush _themeTextSubBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#A0A0A0"));

    public MainWindowViewModel()
    {
        // For designer
    }

    public MainWindowViewModel(
        IConfigService configService,
        IGamePathService gamePathService,
        INotificationService notificationService)
    {
        _configService = configService;
        _gamePathService = gamePathService;
        _notificationService = notificationService;

        CurrentPage = Ioc.Default.GetRequiredService<ModManagerViewModel>();
    }

    public async Task InitializeAsync()
    {
        if (_configService == null || _gamePathService == null) return;

        await _configService.LoadAsync();

        if (string.IsNullOrEmpty(_configService.Config.GamePath) ||
            !_gamePathService.IsValidGamePath(_configService.Config.GamePath))
        {
            var detectedPath = _gamePathService.DetectGamePath();
            if (!string.IsNullOrEmpty(detectedPath))
            {
                _configService.Config.GamePath = detectedPath;
                await _configService.SaveAsync();
            }
        }

        UpdateGamePathStatus();
        CheckAndShowNotification();
        UpdateBackground();

        _currentPageCts?.Cancel();
        _currentPageCts = new CancellationTokenSource();

        if (CurrentPage is MelonLoaderViewModel mlvm)
            await mlvm.InitializeAsync(_currentPageCts.Token);
        else if (CurrentPage is ModManagerViewModel mmvm)
            await mmvm.InitializeAsync(_currentPageCts.Token);
        else if (CurrentPage is ConfigManagerViewModel cmvm)
            await cmvm.InitializeAsync(_currentPageCts.Token);
        else if (CurrentPage is ChartManagerViewModel chvm)
            await chvm.InitializeAsync(_currentPageCts.Token);
    }

    private void UpdateGamePathStatus()
    {
        if (_configService != null && _gamePathService != null)
        {
            GamePathStatus = _gamePathService.IsValidGamePath(_configService.Config.GamePath)
                ? $"Game Path: {_configService.Config.GamePath}"
                : "Game Path: Not Set or Invalid";
        }
    }

    public void UpdateBackground()
    {
        OnPropertyChanged(nameof(CustomBackgroundOpacity));
        var path = CustomBackgroundImagePath;
        if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
        {
            CustomBackgroundBitmap?.Dispose();
            CustomBackgroundBitmap = null;
            IsNormalTheme = true;
            UpdateThemeColors();
            return;
        }

        try
        {
            // 每次重新加载图片以防原图被修改
            CustomBackgroundBitmap?.Dispose();
            using var stream = System.IO.File.OpenRead(path);
            CustomBackgroundBitmap = new Avalonia.Media.Imaging.Bitmap(stream);
            IsNormalTheme = false;
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"无法加载背景图: {ex.Message}");
            CustomBackgroundBitmap = null;
            IsNormalTheme = true;
        }
        UpdateThemeColors();
    }

    public void UpdateThemeColors()
    {
        if (IsNormalTheme)
        {
            WindowBackground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E1E1E"));
            SidebarBackground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E1E1E"));
            ContentBackground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E1E1E"));
            
            ThemeTextMainBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E0E0E0"));
            ThemeTextSubBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#A0A0A0"));
        }
        else
        {
            WindowBackground = Avalonia.Media.Brushes.Black;
            SidebarBackground = Avalonia.Media.Brushes.Transparent;
            ContentBackground = Avalonia.Media.Brushes.Transparent;
            
            var customColorHex = _configService?.Config?.CustomThemeTextColor;
            if (!string.IsNullOrEmpty(customColorHex) && Avalonia.Media.Color.TryParse(customColorHex, out var parsedColor))
            {
                var customBrush = new Avalonia.Media.SolidColorBrush(parsedColor);
                ThemeTextMainBrush = customBrush;
                ThemeTextSubBrush = customBrush; 
            }
            else
            {
                ThemeTextMainBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E0E0E0"));
                ThemeTextSubBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#A0A0A0"));
            }
        }

        Avalonia.Media.IBrush cardBg, cardHoverBg, modCardBg, controlBg, controlHoverBg, controlPressedBg;
        if (IsNormalTheme)
        {
            cardBg = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#242424"));
            cardHoverBg = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2A2A2A"));
            modCardBg = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2D2D30"));
            controlBg = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2a2a2a"));
            controlHoverBg = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3a3a3a"));
            controlPressedBg = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#4a4a4a"));
        }
        else
        {
            // 半透明黑色背景
            cardBg = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#99000000")); // 60% 黑
            cardHoverBg = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#B3000000")); // 70% 黑
            modCardBg = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#99000000")); // 60% 黑
            controlBg = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#66000000")); // 40% 黑
            controlHoverBg = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#80000000")); // 50% 黑
            controlPressedBg = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#99000000")); // 60% 黑
        }

        if (Avalonia.Application.Current != null)
        {
            Avalonia.Application.Current.Resources["ThemeTextMainBrush"] = ThemeTextMainBrush;
            Avalonia.Application.Current.Resources["ThemeTextSubBrush"] = ThemeTextSubBrush;
            
            Avalonia.Application.Current.Resources["CardBgBrush"] = cardBg;
            Avalonia.Application.Current.Resources["CardHoverBgBrush"] = cardHoverBg;
            Avalonia.Application.Current.Resources["ModCardBgBrush"] = modCardBg;
            Avalonia.Application.Current.Resources["ControlBgBrush"] = controlBg;
            Avalonia.Application.Current.Resources["ControlHoverBgBrush"] = controlHoverBg;
            Avalonia.Application.Current.Resources["ControlPressedBgBrush"] = controlPressedBg;
        }
    }

    private void CheckAndShowNotification()
    {
        if (_notificationService == null || _configService == null || _gamePathService == null) return;
        
        _notificationService.ClearPersistentNotifications();

        var gamePath = _configService.Config.GamePath;
        if (string.IsNullOrEmpty(gamePath) || !_gamePathService.IsValidGamePath(gamePath))
        {
            _notificationService.ShowInfo("未检测到游戏，请手动选择路径", -1);
        }
        else
        {
            try
            {
                var steamNameRaw = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam")?.GetValue("LastGameNameUsed") as string;
                if (!string.IsNullOrEmpty(steamNameRaw))
                {
                    var bytes = System.Text.Encoding.GetEncoding(0).GetBytes(steamNameRaw);
                    var steamNameStr = System.Text.Encoding.UTF8.GetString(bytes);

                    _notificationService.ShowInfo($"欢迎回来\n{steamNameStr}", 1500);
                }
                else
                {
                    _notificationService.ShowInfo("欢迎回来", 1500);
                }
            }
            catch
            {
                // Fallback if registry access fails
                _notificationService.ShowInfo("欢迎回来", 1500);
            }
        }
    }

    [RelayCommand]
    private async Task LaunchGameAsync()
    {
        if (_configService == null) return;
        var gamePath = _configService.Config.GamePath;
        if (string.IsNullOrEmpty(gamePath) || (_gamePathService != null && !_gamePathService.IsValidGamePath(gamePath)))
        {
            return;
        }

        var exePath = System.IO.Path.Combine(gamePath, "MuseDash.exe");
        if (!System.IO.File.Exists(exePath)) return;

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow != null)
            {
                var confirm = await MdModManager.Views.LaunchGameConfirmDialog.ShowDialogAsync(mainWindow, _configService);
                if (!confirm) return;
            }
        }

        try
        {
            var processes = System.Diagnostics.Process.GetProcessesByName("MuseDash");
            bool killedAny = false;
            foreach (var process in processes)
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    killedAny = true;
                }
            }
            if (killedAny)
            {
                await Task.Delay(1500); // 给进程留出关闭的时间
            }
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"Error killing process: {ex.Message}");
        }

        try
        {
            // 如果是在 Steam 目录下，直接通过 Steam 协议启动，可以避免游戏被 Steam 的 DRM 强制关闭并重新拉起
            if (gamePath.Contains("steamapps", System.StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "steam://rungameid/774171",
                    UseShellExecute = true
                });
            }
            else
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = gamePath,
                    UseShellExecute = true
                });
            }
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"Error starting process: {ex.Message}");
        }

        // 按钮冷却保护
        await Task.Delay(3000);
    }

    [RelayCommand]
    private async Task NavigateToSettingsAsync()
    {
        _currentPageCts?.Cancel();
        _currentPageCts = new CancellationTokenSource();

        var vm = Ioc.Default.GetRequiredService<SettingsViewModel>();
        CurrentPage = vm;
        vm.Initialize();
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task NavigateToMelonLoaderAsync()
    {
        _currentPageCts?.Cancel();
        _currentPageCts = new CancellationTokenSource();

        var vm = Ioc.Default.GetRequiredService<MelonLoaderViewModel>();
        CurrentPage = vm;
        await vm.InitializeAsync(_currentPageCts.Token);
    }

    private void CleanupCurrentPage()
    {
        _currentPageCts?.Cancel();
        _currentPageCts = new CancellationTokenSource();
        
        if (CurrentPage is ChartManagerViewModel chartVm)
            chartVm.Dispose();
        else if (CurrentPage is ChartDownloadViewModel chartDownloadVm)
            chartDownloadVm.Dispose(); // 切换时将停止正在播放的试听音频
    }

    [RelayCommand]
    private async Task NavigateToModManagerAsync()
    {
        CleanupCurrentPage();

        var vm = Ioc.Default.GetRequiredService<ModManagerViewModel>();
        CurrentPage = vm;
        await vm.InitializeAsync(_currentPageCts.Token);
    }

    [RelayCommand]
    private async Task NavigateToConfigManagerAsync()
    {
        CleanupCurrentPage();

        System.Console.WriteLine("[DEBUG] Navigating to Config Manager...");
        var vm = Ioc.Default.GetRequiredService<ConfigManagerViewModel>();
        System.Console.WriteLine("[DEBUG] ConfigManagerViewModel resolved.");
        CurrentPage = vm;
        System.Console.WriteLine("[DEBUG] CurrentPage updated.");
        await vm.InitializeAsync(_currentPageCts.Token);
        System.Console.WriteLine("[DEBUG] ConfigManagerViewModel InitializeAsync finished.");
    }

    [RelayCommand]
    private async Task NavigateToChartManagerAsync()
    {
        CleanupCurrentPage();

        var vm = Ioc.Default.GetRequiredService<ChartManagerViewModel>();
        CurrentPage = vm;
        await vm.InitializeAsync(_currentPageCts.Token);
    }

    [RelayCommand]
    private async Task NavigateToChartDownloadAsync()
    {
        CleanupCurrentPage();

        var vm = Ioc.Default.GetRequiredService<ChartDownloadViewModel>();
        CurrentPage = vm;
        await vm.InitializeAsync(_currentPageCts.Token);
    }

    [RelayCommand]
    private async Task NavigateToDownloadManagerAsync()
    {
        CleanupCurrentPage();

        var vm = Ioc.Default.GetRequiredService<DownloadManagerViewModel>();
        CurrentPage = vm;
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task GenerateLogAsync()
    {
        try
        {
            var desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
            var logPath = System.IO.Path.Combine(desktopPath, "MuseDashTOOL_log.txt");

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== MuseDashTOOL Diagnostic Log ===");
            sb.AppendLine($"Timestamp: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"OS Version: {System.Environment.OSVersion}");
            sb.AppendLine($"64-bit OS: {System.Environment.Is64BitOperatingSystem}");
            sb.AppendLine($".NET Version: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            
            if (_configService != null)
            {
                var cfg = _configService.Config;
                sb.AppendLine();
                sb.AppendLine("--- Configuration ---");
                sb.AppendLine($"GamePath: {cfg.GamePath}");
                sb.AppendLine($"DownloadSource: {cfg.DownloadSource}");
                sb.AppendLine($"AutoTranslate: {cfg.AutoTranslateDescriptions}");
                sb.AppendLine($"SuppressIncompatibleWarning: {cfg.SuppressIncompatibleModWarning}");
            }
            
            if (_gamePathService != null && _configService != null)
            {
                sb.AppendLine();
                sb.AppendLine("--- Game State ---");
                sb.AppendLine($"Game Path Valid: {_gamePathService.IsValidGamePath(_configService.Config.GamePath)}");
                
                var mlDir = System.IO.Path.Combine(_configService.Config.GamePath, "MelonLoader");
                var modsDir = System.IO.Path.Combine(_configService.Config.GamePath, "Mods");
                
                sb.AppendLine($"MelonLoader Installed: {System.IO.Directory.Exists(mlDir)}");
                sb.AppendLine($"Mods Directory Exists: {System.IO.Directory.Exists(modsDir)}");
                
                if (System.IO.Directory.Exists(modsDir))
                {
                    var dllFiles = System.IO.Directory.GetFiles(modsDir, "*.dll");
                    sb.AppendLine($"Installed Mods ({dllFiles.Length}):");
                    foreach (var dll in dllFiles)
                    {
                        sb.AppendLine($" - {System.IO.Path.GetFileName(dll)} (v{System.Diagnostics.FileVersionInfo.GetVersionInfo(dll).FileVersion})");
                    }
                }
            }

            await System.IO.File.WriteAllTextAsync(logPath, sb.ToString());
            
            // 可以选择弹个通知告诉用户已生成，利用现有的 NotificationService
            if (_notificationService != null)
            {
                _notificationService.ShowSuccess("诊断日志已保存到桌面！");
            }
        }
        catch (System.Exception ex)
        {
            if (_notificationService != null)
            {
                _notificationService.ShowFailure("生成日志失败", ex.Message);
            }
        }
    }
}
