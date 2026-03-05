using System;

namespace MdModManager.Services;

public interface INavigationService
{
    event Action<string> OnRequestConfigNavigation;
    void RequestNavigateToConfig(string filePath);
}

public class NavigationService : INavigationService
{
    public event Action<string>? OnRequestConfigNavigation;

    public void RequestNavigateToConfig(string filePath)
    {
        OnRequestConfigNavigation?.Invoke(filePath);
    }
}
