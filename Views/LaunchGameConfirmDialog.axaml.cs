using Avalonia.Controls;
using Avalonia.Interactivity;
using MdModManager.Services;
using System.Threading.Tasks;

namespace MdModManager.Views;

public partial class LaunchGameConfirmDialog : Window
{
    private readonly IConfigService? _configService;

    public LaunchGameConfirmDialog()
    {
        InitializeComponent();
    }

    public LaunchGameConfirmDialog(IConfigService configService) : this()
    {
        _configService = configService;
    }

    public bool Confirmed { get; private set; } = false;

    // Static helper method to show the dialog
    public static async Task<bool> ShowDialogAsync(Window owner, IConfigService configService)
    {
        // Check if suppressed
        if (configService.Config.SuppressLaunchGameConfirmation)
        {
            return true;
        }

        var dialog = new LaunchGameConfirmDialog(configService);
        dialog.ShowInTaskbar = true;
        var tcs = new TaskCompletionSource<bool>();
        dialog.Closed += (s, e) => tcs.TrySetResult(true);

        bool originalHitTest = true;
        Control? contentControl = owner?.Content as Control;
        if (contentControl != null)
        {
            originalHitTest = contentControl.IsHitTestVisible;
            contentControl.IsHitTestVisible = false;
        }

        dialog.Show();
        await tcs.Task;

        if (owner != null)
        {
            if (contentControl != null)
            {
                contentControl.IsHitTestVisible = originalHitTest;
            }
            if (owner.WindowState != WindowState.Minimized)
            {
                owner.Activate();
            }
        }
        
        return dialog.Confirmed;
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        if (DontShowAgainCheckBox.IsChecked == true && _configService != null)
        {
            _configService.Config.SuppressLaunchGameConfirmation = true;
            _ = _configService.SaveAsync();
        }

        Confirmed = true;
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }
}
