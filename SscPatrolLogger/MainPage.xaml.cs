using SscPatrolLogger.ViewModels;

namespace SscPatrolLogger;

public partial class MainPage : ContentPage
{
    private readonly MainPageViewModel _vm;

    public MainPage(MainPageViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ViewModels.MainPageViewModel vm)
        {
            vm.RefreshSettingsForUi();
        }
        await _vm.InitializeAsync();
    }
}
