using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls;

namespace SscPatrolLogger.ViewModels;

public sealed partial class ThemeViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isDarkMode;

    [ObservableProperty]
    private string themeIcon = "🌞";

    public ThemeViewModel()
    {
        var requested = Application.Current?.RequestedTheme ?? AppTheme.Light;
        IsDarkMode = requested == AppTheme.Dark;
        ThemeIcon = IsDarkMode ? "🌙" : "🌞";
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        if (Application.Current is null) return;

        Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
        ThemeIcon = value ? "🌙" : "🌞";
    }
}
