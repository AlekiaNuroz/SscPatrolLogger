using SscPatrolLogger.Services;

namespace SscPatrolLogger;

public partial class App : Application
{
    public static PatrolRepository Repository { get; private set; } = null!;

    public App()
    {
        InitializeComponent();

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "patrols.db3");

        Repository = new PatrolRepository(dbPath);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // This replaces MainPage = ...
        return new Window(new NavigationPage(new MainPage(Repository)));
    }
}
