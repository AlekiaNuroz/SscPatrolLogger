using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SscPatrolLogger.Services;
using System.Net.Mail;

namespace SscPatrolLogger.ViewModels;

public sealed partial class SettingsPageViewModel : ObservableObject
{
    private readonly IAppSettings _settings;
    private readonly IAlertService _alerts;

    [ObservableProperty]
    private string sendToEmail = string.Empty;

    public SettingsPageViewModel(IAppSettings settings, IAlertService alerts)
    {
        _settings = settings;
        _alerts = alerts;

        SendToEmail = _settings.SendToEmail;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var email = (SendToEmail ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            await _alerts.ShowAsync("Missing email", "Please enter a Send-to email address.");
            return;
        }

        if (!IsValidEmail(email))
        {
            await _alerts.ShowAsync("Invalid email", "Please enter a valid email address.");
            return;
        }

        _settings.SendToEmail = email.ToLowerInvariant();
        SendToEmail = email;

        await _alerts.ShowAsync("Saved", "Settings saved.");

        // Optional: auto-return to the previous page
        // await Shell.Current.GoToAsync("..");
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return string.Equals(addr.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
