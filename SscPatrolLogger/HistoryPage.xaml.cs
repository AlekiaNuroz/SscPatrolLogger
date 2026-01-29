using SscPatrolLogger.ViewModels;

namespace SscPatrolLogger;

public partial class HistoryPage : ContentPage
{
    private readonly HistoryPageViewModel _vm;
    private bool _isInitialized;

    public HistoryPage(HistoryPageViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
        _isInitialized = true;
    }

    private void OnFilterChanged(object sender, EventArgs e) => _vm.ApplyFilters();

    private void OnFromDateSelected(object sender, DateChangedEventArgs e)
    {
        if (!_isInitialized)
            return;

        if (e.NewDate is DateTime newDate)
            _vm.SetFromDate(newDate);
    }

    private void OnToDateSelected(object sender, DateChangedEventArgs e)
    {
        if (!_isInitialized)
            return;

        if (e.NewDate is DateTime newDate)
            _vm.SetToDate(newDate);
    }
}
