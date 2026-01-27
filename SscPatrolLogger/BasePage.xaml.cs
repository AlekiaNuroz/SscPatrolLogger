namespace SscPatrolLogger;

public partial class BasePage : ContentPage
{
    public BasePage()
    {
        InitializeComponent();
    }

    protected static IServiceProvider Services =>
        IPlatformApplication.Current?.Services
        ?? throw new InvalidOperationException("DI services are not available.");

    private async void OnHomeClicked(object sender, EventArgs e)
    {
        var page = Services.GetRequiredService<MainPage>();
        await NavigateToAsync(page);
    }

    private async void OnHistoryClicked(object sender, EventArgs e)
    {
        var page = Services.GetRequiredService<HistoryPage>();
        await NavigateToAsync(page);
    }

    private async Task NavigateToAsync(Page page)
    {
        if (Navigation != null)
        {
            await Navigation.PushAsync(page);
            return;
        }

        throw new InvalidOperationException("No navigation context available.");
    }
}
