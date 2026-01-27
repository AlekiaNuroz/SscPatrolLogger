namespace SscPatrolLogger.Services
{
    public interface IAlertService
    {
        Task ShowAsync(string title, string message, string cancel = "OK");
        Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No");
    }
}
