using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MdModManager.Views;

public partial class IncompatibleModDialog : Window
{
    /// <summary>用户是否点击了"确认下载"（false = 取消）</summary>
    public bool Confirmed { get; private set; } = false;

    /// <summary>用户是否勾选了"以后不再显示该提示"</summary>
    public bool DontShowAgain => DontShowAgainCheckBox.IsChecked == true;

    public IncompatibleModDialog()
    {
        InitializeComponent();
    }

    public async System.Threading.Tasks.Task ShowIndependentDialogAsync(Window? owner)
    {
        this.ShowInTaskbar = true;
        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
        this.Closed += (s, e) => tcs.TrySetResult(true);

        bool originalHitTest = true;
        Avalonia.Controls.Control? contentControl = owner?.Content as Avalonia.Controls.Control;
        if (contentControl != null)
        {
            originalHitTest = contentControl.IsHitTestVisible;
            contentControl.IsHitTestVisible = false;
        }

        this.Show();
        await tcs.Task;

        if (owner != null)
        {
            if (contentControl != null)
            {
                contentControl.IsHitTestVisible = originalHitTest;
            }
            if (owner.WindowState != Avalonia.Controls.WindowState.Minimized)
            {
                owner.Activate();
            }
        }
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        Confirmed = true;
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }
}
