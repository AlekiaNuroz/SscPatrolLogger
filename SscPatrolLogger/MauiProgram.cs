using Microsoft.Extensions.Logging;
using SscPatrolLogger.Services;
using SscPatrolLogger.ViewModels;

namespace SscPatrolLogger;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("FontAwesomeSolid.otf", "FAS");
                fonts.AddFont("FontAwesomeRegular.otf", "FAR");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Repositories
        builder.Services.AddSingleton<IPatrolRepository>(sp =>
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "patrols.db3");
            return new PatrolRepository(dbPath);
        });

        // Services
        builder.Services.AddSingleton<IAlertService, AlertService>();
        builder.Services.AddSingleton(new HttpClient());
        builder.Services.AddSingleton<IReportSender, EmailJsReportSender>();
        builder.Services.AddSingleton<IAppSettings, ApplicationSettings>();

        // ViewModels (state preserved)
        builder.Services.AddSingleton<MainPageViewModel>();
        builder.Services.AddSingleton<HistoryPageViewModel>();
        builder.Services.AddSingleton<ThemeViewModel>();
        builder.Services.AddSingleton<SettingsPageViewModel>();


        // Shell
        builder.Services.AddSingleton<AppShell>();

        // Pages (must be transient with Shell tabs)
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<HistoryPage>();
        builder.Services.AddTransient<SettingsPage>();
        return builder.Build();
    }
}
