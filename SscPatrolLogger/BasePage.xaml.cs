using SscPatrolLogger.Services;

namespace SscPatrolLogger;

public partial class BasePage : ContentPage
{
    public BasePage()
    {
        InitializeComponent();
    }

    private async void OnHomeClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MainPage(App.Repository));
    }

    private async void OnHistoryClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new HistoryPage(App.Repository));
    }
}
