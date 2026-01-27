namespace SscPatrolLogger.Services;

public sealed class AlertService : IAlertService
{
    public Task ShowAsync(string title, string message, string cancel = "OK")
        => GetPage().DisplayAlertAsync(title, message, cancel);

    public Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No")
        => GetPage().DisplayAlertAsync(title, message, accept, cancel);

    private static Page GetPage()
    {
        var page = Shell.Current?.CurrentPage;
        return page is null ? throw new InvalidOperationException("No current page is available to show an alert.") : page;
    }
}
