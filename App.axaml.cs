using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using MdModManager.ViewModels;
using MdModManager.Views;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;

namespace MdModManager;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        ConfigureServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = Ioc.Default.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MelonLoaderViewModel>();
        services.AddTransient<ModManagerViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ConfigManagerViewModel>();
        services.AddTransient<ChartManagerViewModel>();
        services.AddTransient<ChartDownloadViewModel>();
        services.AddTransient<DownloadManagerViewModel>();

        // Services (will be added here later)
        services.AddSingleton<MdModManager.Services.IConfigService, MdModManager.Services.ConfigService>();
        services.AddSingleton<MdModManager.Services.IGamePathService, MdModManager.Services.GamePathService>();
        services.AddSingleton<MdModManager.Services.IMelonLoaderService, MdModManager.Services.MelonLoaderService>();
        services.AddSingleton<MdModManager.Services.IModCatalogService, MdModManager.Services.ModCatalogService>();
        services.AddSingleton<MdModManager.Services.ILocalModService, MdModManager.Services.LocalModService>();
        services.AddSingleton<MdModManager.Services.INotificationService, MdModManager.Services.NotificationService>();
        services.AddSingleton<MdModManager.Services.IConfigFileService, MdModManager.Services.ConfigFileService>();
        services.AddSingleton<MdModManager.Services.IChartService, MdModManager.Services.ChartService>();
        services.AddSingleton<MdModManager.Services.IChartDownloadService, MdModManager.Services.ChartDownloadService>();
        services.AddSingleton<MdModManager.Services.IDownloadManagerService, MdModManager.Services.DownloadManagerService>();

        Ioc.Default.ConfigureServices(services.BuildServiceProvider());
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}