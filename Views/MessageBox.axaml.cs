using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;

namespace MdModManager.Views;

public partial class MessageBox : Window
{
    private TaskCompletionSource<bool> _tcs = new();

    public MessageBox()
    {
        InitializeComponent();
    }

    public static async Task ShowAsDialogAsync(Window owner, string message)
    {
        var dialog = new MessageBox();
        dialog.FindControl<TextBlock>("MessageText")!.Text = message;
        await dialog.ShowDialog(owner);
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
