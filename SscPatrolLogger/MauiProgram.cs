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

        // Data
        builder.Services.AddSingleton(sp =>
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "patrols.db3");
            return new PatrolRepository(dbPath);
        });

        // Services
        builder.Services.AddSingleton<IAlertService, AlertService>();
        builder.Services.AddSingleton(new HttpClient());
        builder.Services.AddSingleton<IReportSender, EmailJsReportSender>();

        // ViewModels (state preserved)
        builder.Services.AddSingleton<MainPageViewModel>();
        builder.Services.AddSingleton<ThemeViewModel>();


        // Shell
        builder.Services.AddSingleton<AppShell>();

        // Pages (must be transient with Shell tabs)
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<HistoryPage>();
        return builder.Build();
    }
}
