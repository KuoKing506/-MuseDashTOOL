using Avalonia.Controls;
using System.Threading.Tasks;

namespace MdModManager.Views;

public partial class EasterEggWindow : Window
{
    public EasterEggWindow()
    {
        InitializeComponent();
    }

    public EasterEggWindow(string message) : this()
    {
        var textBlock = this.FindControl<TextBlock>("MessageText");
        if (textBlock != null)
        {
            textBlock.Text = message;
        }
    }

    public async Task ShowAndAutoCloseAsync()
    {
        Show();
        await Task.Delay(2000);
        Close();
    }
}
