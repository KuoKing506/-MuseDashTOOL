using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.DependencyInjection;
using MdModManager.Services;
using MdModManager.ViewModels;

namespace MdModManager.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                await vm.InitializeAsync();
            }
        };
    }

    private async void OnSelectGamePathClick(object? sender, RoutedEventArgs e)
    {
        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择 Muse Dash 游戏目录",
            AllowMultiple = false
        });

        if (result.Count > 0)
        {
            var path = result[0].Path.LocalPath;
            var pathService = Ioc.Default.GetRequiredService<IGamePathService>();
            var configService = Ioc.Default.GetRequiredService<IConfigService>();
            
            if (pathService.IsValidGamePath(path))
            {
                configService.Config.GamePath = path;
                await configService.SaveAsync();

                if (DataContext is MainWindowViewModel vm)
                {
                    await vm.InitializeAsync();
                }
            }
        }
    }
}