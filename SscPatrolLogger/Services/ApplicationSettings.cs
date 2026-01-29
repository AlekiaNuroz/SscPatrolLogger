using Microsoft.Maui.Storage;

namespace SscPatrolLogger.Services;

public sealed class ApplicationSettings : IAppSettings
{
    private const string SendToEmailKey = "settings.sendToEmail";

    public string SendToEmail
    {
        get => Preferences.Default.Get(SendToEmailKey, string.Empty);
        set => Preferences.Default.Set(SendToEmailKey, value?.Trim() ?? string.Empty);
    }
}
