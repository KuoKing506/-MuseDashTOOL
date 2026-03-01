using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MdModManager.Views;

public partial class DownloadManagerView : UserControl
{
    public DownloadManagerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
