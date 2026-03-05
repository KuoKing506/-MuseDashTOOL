using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using MdModManager.ViewModels;

namespace MdModManager.Views;

public partial class AccountView : UserControl
{
    public AccountView()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
    }

    // Called by the ScrollViewer via x:Name reference in XAML
    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer sv) return;
        if (DataContext is not AccountViewModel vm) return;

        // 当滚动到距底部 200px 以内时，触发加载更多
        double remaining = sv.Extent.Height - sv.Offset.Y - sv.Viewport.Height;
        if (remaining < 200)
        {
            vm.LoadMore();
        }
    }
}
