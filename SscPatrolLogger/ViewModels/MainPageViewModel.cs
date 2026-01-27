using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SscPatrolLogger.Models;
using SscPatrolLogger.Services;
using System.Collections.ObjectModel;
using System.Text;

namespace SscPatrolLogger.ViewModels;

public sealed partial class MainPageViewModel : ObservableObject
{
    private readonly PatrolRepository _repo;
    private readonly IAlertService _alerts;
    private readonly IReportSender _sender;

    private readonly List<string> _patrolList =
    [
        "9 Boulevard Montclair",
        "190 Promenade du Portage",
        "200 Promenade du Portage – Suite 0291",
        "200 Promenade du Portage – Suite 1000",
        "200 Promenade du Portage – Suite 5010",
        "105 Rue Hôtel-de-Ville – 2nd Floor",
        "105 Rue Hôtel-de-Ville – 1st Floor",
        "50 Rue Victoria"
    ];

    private readonly Dictionary<string, (string Start, string End)> _patrolTimes = [];

    private bool _started;
    private bool _initialized;

    public ObservableCollection<string> Patrols { get; } = [];
    public ObservableCollection<string> Shifts { get; } =
    [
        "Thursday Morning",
        "Thursday Night",
        "Friday Morning",
        "Friday Night"
    ];

    public ObservableCollection<PatrolStatusItem> StatusItems { get; } = [];

    [ObservableProperty]
    private string selectedPatrol = "";

    [ObservableProperty]
    private string selectedShift = "Thursday Morning";

    [ObservableProperty]
    private string startTimeText = "—";

    [ObservableProperty]
    private string endTimeText = "—";

    [ObservableProperty]
    private string startEndButtonText = "Start Patrol";

    [ObservableProperty]
    private bool isDarkMode;

    [ObservableProperty]
    private string themeIcon = "🌞";

    public MainPageViewModel(PatrolRepository repo, IAlertService alerts, IReportSender sender)
    {
        _repo = repo;
        _alerts = alerts;
        _sender = sender;

        foreach (var p in _patrolList)
        {
            _patrolTimes[p] = ("", "");
            Patrols.Add(p);
        }

        SelectedPatrol = Patrols.FirstOrDefault() ?? "";
        AutoDetectShift();
        InitializeTheme();
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        var saved = await _repo.GetCurrentStateAsync();

        foreach (var p in _patrolList)
        {
            var entry = saved.FirstOrDefault(x => x.Location == p);
            if (entry != null)
                _patrolTimes[p] = (entry.Start, entry.End);
        }

        LoadCurrentPatrolTimes();
        UpdateStatusList();
    }

    private void InitializeTheme()
    {
        var current = Application.Current?.RequestedTheme ?? AppTheme.Light;
        IsDarkMode = current == AppTheme.Dark;
        ThemeIcon = IsDarkMode ? "🌙" : "🌞";
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        if (Application.Current is null) return;

        Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
        ThemeIcon = value ? "🌙" : "🌞";
    }

    partial void OnSelectedPatrolChanged(string value)
    {
        LoadCurrentPatrolTimes();
    }

    [RelayCommand]
    private async Task StartEndAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedPatrol))
            return;

        var now = FormatTimeHHMM(DateTime.Now);

        if (!_started)
        {
            _patrolTimes[SelectedPatrol] = (now, _patrolTimes[SelectedPatrol].End);

            await _repo.SaveCurrentStateAsync(new CurrentPatrolState
            {
                Location = SelectedPatrol,
                Start = now,
                End = _patrolTimes[SelectedPatrol].End
            });

            StartTimeText = $"{now} hrs";
            StartEndButtonText = "End Patrol";
            _started = true;
        }
        else
        {
            _patrolTimes[SelectedPatrol] = (_patrolTimes[SelectedPatrol].Start, now);

            await _repo.SaveCurrentStateAsync(new CurrentPatrolState
            {
                Location = SelectedPatrol,
                Start = _patrolTimes[SelectedPatrol].Start,
                End = now
            });

            await _repo.SaveRecordAsync(new PatrolRecord
            {
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                Shift = SelectedShift ?? "",
                Location = SelectedPatrol,
                Start = _patrolTimes[SelectedPatrol].Start,
                End = now
            });

            EndTimeText = $"{now} hrs";
            StartEndButtonText = "Start Patrol";
            _started = false;

            // Auto-advance
            var idx = Patrols.IndexOf(SelectedPatrol);
            var next = (idx + 1) % Patrols.Count;
            SelectedPatrol = Patrols[next];
        }

        UpdateStatusList();
    }

    [RelayCommand]
    private async Task ResetPatrolAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedPatrol))
            return;

        _patrolTimes[SelectedPatrol] = ("", "");

        await _repo.SaveCurrentStateAsync(new CurrentPatrolState
        {
            Location = SelectedPatrol,
            Start = "",
            End = ""
        });

        LoadCurrentPatrolTimes();
        UpdateStatusList();
    }

    [RelayCommand]
    private async Task ResetAllAsync()
    {
        var confirm = await _alerts.ConfirmAsync("Confirm", "Reset ALL patrols?");
        if (!confirm) return;

        foreach (var p in _patrolList)
            _patrolTimes[p] = ("", "");

        await _repo.ClearCurrentStateAsync();

        LoadCurrentPatrolTimes();
        UpdateStatusList();
    }

    [RelayCommand]
    private async Task SubmitAllAsync()
    {
        var shift = SelectedShift ?? "Unknown";

        var rowsBuilder = new StringBuilder();
        foreach (var p in _patrolList)
        {
            var (start, end) = _patrolTimes[p];
            if (string.IsNullOrEmpty(start) && string.IsNullOrEmpty(end))
                continue;

            rowsBuilder.AppendLine($@"
<tr>
  <td style=""border: 1px solid #ccc; padding: 8px;"">{p}</td>
  <td style=""border: 1px solid #ccc; padding: 8px;"">{(string.IsNullOrEmpty(start) ? "—" : start)}</td>
  <td style=""border: 1px solid #ccc; padding: 8px;"">{(string.IsNullOrEmpty(end) ? "—" : end)}</td>
</tr>");
        }

        if (rowsBuilder.Length == 0)
        {
            await _alerts.ShowAsync("No Data", "No patrols recorded.");
            return;
        }

        try
        {
            await _sender.SendEmailJsAsync(shift, rowsBuilder.ToString());
            await _alerts.ShowAsync("Success", "All patrols sent.");

            foreach (var p in _patrolList)
                _patrolTimes[p] = ("", "");

            await _repo.ClearCurrentStateAsync();

            LoadCurrentPatrolTimes();
            UpdateStatusList();
        }
        catch (Exception ex)
        {
            await _alerts.ShowAsync("Error", $"Error sending email: {ex.Message}");
        }
    }

    private void AutoDetectShift()
    {
        var now = DateTime.Now;
        var day = now.DayOfWeek;
        var hour = now.Hour;

        SelectedShift =
            day == DayOfWeek.Thursday ? (hour < 12 ? "Thursday Morning" : "Thursday Night") :
            day == DayOfWeek.Friday ? (hour < 12 ? "Friday Morning" : "Friday Night") :
            "Thursday Morning";
    }

    private static string FormatTimeHHMM(DateTime dt) => $"{dt:HHmm}";

    private void LoadCurrentPatrolTimes()
    {
        if (string.IsNullOrWhiteSpace(SelectedPatrol))
            return;

        var (start, end) = _patrolTimes[SelectedPatrol];

        StartTimeText = string.IsNullOrEmpty(start) ? "—" : $"{start} hrs";
        EndTimeText = string.IsNullOrEmpty(end) ? "—" : $"{end} hrs";

        _started = !string.IsNullOrEmpty(start) && string.IsNullOrEmpty(end);
        StartEndButtonText = _started ? "End Patrol" : "Start Patrol";
    }

    private void UpdateStatusList()
    {
        StatusItems.Clear();

        foreach (var p in _patrolList)
        {
            var (start, end) = _patrolTimes[p];

            string status =
                (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end)) ? $"✔ Completed – {p}" :
                (!string.IsNullOrEmpty(start)) ? $"● In progress – {p}" :
                $"○ Not started – {p}";

            StatusItems.Add(new PatrolStatusItem(status));
        }
    }
}

public sealed record PatrolStatusItem(string StatusText);
