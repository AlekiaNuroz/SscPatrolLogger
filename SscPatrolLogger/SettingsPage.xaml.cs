using SscPatrolLogger.ViewModels;

namespace SscPatrolLogger;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsPageViewModel _vm;

    public SettingsPage(SettingsPageViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }
}
