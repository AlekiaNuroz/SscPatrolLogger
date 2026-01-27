using Microsoft.Extensions.Logging;
using SscPatrolLogger.Services;

namespace SscPatrolLogger
{
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

            builder.Services.AddSingleton<PatrolRepository>(provider => 
                { string dbPath = Path.Combine(FileSystem.AppDataDirectory, "patrols.db3"); return new PatrolRepository(dbPath); });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
