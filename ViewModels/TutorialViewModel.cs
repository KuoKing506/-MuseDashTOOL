using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System;
using MdModManager.Services;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace MdModManager.ViewModels;

public partial class TutorialViewModel : ObservableObject
{
    private readonly INotificationService? _notificationService;

    public TutorialViewModel()
    {
        _notificationService = Ioc.Default.GetService<INotificationService>();
    }

    [RelayCommand]
    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TutorialViewModel] OpenUrl error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task CopyTextAsync(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var clipboard = desktop.MainWindow?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(text);
                    _notificationService?.ShowSuccess($"复制成功：已将 \"{text}\" 复制到剪贴板");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TutorialViewModel] CopyText error: {ex.Message}");
            _notificationService?.ShowFailure("复制失败", ex.Message);
        }
    }
}
