using Avalonia;
using System;

namespace MdModManager;

sealed class Program
{
    // 初始化代码。在 AppMain 被调用之前，请勿使用任何 Avalonia、第三方 API
    // 或依赖 SynchronizationContext 的代码。
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia 配置，请勿删除；设计器也会使用此方法。
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
