using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.DependencyInjection;
using MdModManager.Models;
using MdModManager.Services;
using MdModManager.ViewModels;

namespace MdModManager.Views;

public partial class ConfigManagerView : UserControl
{
    public ConfigManagerView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
#pragma warning disable CS0618
        var files = e.Data.GetFiles();
#pragma warning restore CS0618
        if (files != null && files.Any())
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private async void Drop(object? sender, DragEventArgs e)
    {
#pragma warning disable CS0618
        var files = e.Data.GetFiles();
#pragma warning restore CS0618
        if (files != null && DataContext is ConfigManagerViewModel cvm)
        {
            foreach (var file in files)
            {
                var filePath = file.Path.LocalPath;
                await cvm.ImportConfigAsync(filePath);
            }
        }
    }

    private async void OnDeleteSectionClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.Tag is not CfgEntry entry) return;
        if (DataContext is not ConfigManagerViewModel vm) return;

        var configService = Ioc.Default.GetRequiredService<IConfigService>();
        if (!configService.Config.SuppressDeleteConfirmation)
        {
            var dialog = new DeleteConfirmDialog();
            var owner = TopLevel.GetTopLevel(this) as Window;
            await dialog.ShowIndependentDialogAsync(owner);

            if (!dialog.Confirmed) return;

            if (dialog.DontShowAgain)
            {
                configService.Config.SuppressDeleteConfirmation = true;
                _ = configService.SaveAsync();
            }
        }

        vm.DeleteSectionCommand.Execute(entry);
    }

    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var result = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择要导入的配置文件",
            AllowMultiple = false
        });

        if (result == null || result.Count == 0) return;

        var file = result[0];
        var filePath = file.Path.LocalPath;

        if (DataContext is ConfigManagerViewModel cvm)
        {
            await cvm.ImportConfigAsync(filePath);
        }
    }

    private async void OnFileNodePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsRightButtonPressed || e.ClickCount != 2) return;

        if (sender is not Button btn) return;
        if (btn.DataContext is not CfgFolderNode node) return;
        if (DataContext is not ConfigManagerViewModel vm) return;
        
        e.Handled = true;

        var configService = Ioc.Default.GetRequiredService<IConfigService>();
        if (!configService.Config.SuppressDeleteConfirmation)
        {
            var dialog = new DeleteConfirmDialog();
            var owner = TopLevel.GetTopLevel(this) as Window;
            await dialog.ShowIndependentDialogAsync(owner);

            if (!dialog.Confirmed) return;

            if (dialog.DontShowAgain)
            {
                configService.Config.SuppressDeleteConfirmation = true;
                _ = configService.SaveAsync();
            }
        }

        await vm.DeleteFileNodeAsync(node);
    }

    private async void OnFolderNodePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsRightButtonPressed || e.ClickCount != 2) return;

        if (sender is not Button btn) return;
        if (btn.DataContext is not CfgFolderNode node) return;
        if (DataContext is not ConfigManagerViewModel vm) return;

        e.Handled = true;

        var configService = Ioc.Default.GetRequiredService<IConfigService>();
        if (!configService.Config.SuppressDeleteConfirmation)
        {
            var dialog = new DeleteConfirmDialog();
            var owner = TopLevel.GetTopLevel(this) as Window;
            await dialog.ShowIndependentDialogAsync(owner);

            if (!dialog.Confirmed) return;

            if (dialog.DontShowAgain)
            {
                configService.Config.SuppressDeleteConfirmation = true;
                _ = configService.SaveAsync();
            }
        }

        await vm.DeleteFolderNodeAsync(node);
    }

    /// <summary>TextBox 增强滚动：Shift + 滚轮实现横向滚动</summary>
    private void OnTextBoxWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (sender is TextBox textBox && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            // 查找 TextBox 内部的 ScrollViewer
            var scrollViewer = textBox.FindDescendantOfType<ScrollViewer>();
            if (scrollViewer != null)
            {
                // 手动调整横向偏移量
                // ScrollViewer 的 Delta 是 Y 轴方向，左右滚动我们需要增减 HorizontalOffset
                double scrollAmount = e.Delta.Y * 30; // 滚动速度倍率
                scrollViewer.Offset = new Avalonia.Vector(
                    Math.Clamp(scrollViewer.Offset.X - scrollAmount, 0, scrollViewer.Extent.Width - scrollViewer.Viewport.Width),
                    scrollViewer.Offset.Y);
                
                e.Handled = true;
            }
        }
    }
}
