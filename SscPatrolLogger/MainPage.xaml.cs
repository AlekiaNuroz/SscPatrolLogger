using SscPatrolLogger.Models;
using SscPatrolLogger.Services;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;

namespace SscPatrolLogger;

public partial class MainPage : BasePage
{
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
    private readonly PatrolRepository _repo;
    private bool _started = false;

    public ObservableCollection<PatrolStatusItem> StatusItems { get; } = [];

    public MainPage(PatrolRepository repo)
    {
        InitializeComponent();
        _repo = repo;

        // PRE-INITIALIZE ALL PATROLS TO AVOID KeyNotFoundException
        foreach (var p in _patrolList)
            _patrolTimes[p] = ("", "");

        // Load saved state asynchronously (will overwrite defaults)
        InitializePatrolTimes();

        ShiftPicker.ItemsSource = new[]
        {
            "Thursday Morning",
            "Thursday Night",
            "Friday Morning",
            "Friday Night"
        };

        PatrolPicker.ItemsSource = _patrolList;
        PatrolPicker.SelectedIndex = 0;

        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        DarkModeSwitch.IsToggled = isDark;
        ThemeIcon.Text = isDark ? "🌙" : "🌞";

        AutoDetectShift();
        LoadCurrentPatrolTimes();
        UpdateStatusList();

        StatusCollectionView.ItemsSource = StatusItems;
    }

    // ---------------------------------------
    // LOAD CURRENT STATE FROM DATABASE
    // ---------------------------------------
    private async void InitializePatrolTimes()
    {
        var saved = await _repo.GetCurrentStateAsync();

        foreach (var p in _patrolList)
        {
            var entry = saved.FirstOrDefault(x => x.Location == p);
            if (entry != null)
                _patrolTimes[p] = (entry.Start, entry.End);
            // else: keep the ("","") default we already set
        }

        // After loading, refresh UI in case data changed
        LoadCurrentPatrolTimes();
        UpdateStatusList();
    }

    private void AutoDetectShift()
    {
        var now = DateTime.Now;
        var day = now.DayOfWeek;
        var hour = now.Hour;

        string shift;

        if (day == DayOfWeek.Thursday)
            shift = hour < 12 ? "Thursday Morning" : "Thursday Night";
        else if (day == DayOfWeek.Friday)
            shift = hour < 12 ? "Friday Morning" : "Friday Night";
        else
            shift = "Thursday Morning";

        ShiftPicker.SelectedItem = shift;
    }

    private static string FormatTimeHHMM(DateTime dt) =>
        $"{dt:HHmm}";

    private void LoadCurrentPatrolTimes()
    {
        if (PatrolPicker.SelectedItem is not string patrol)
            return;

        var (start, end) = _patrolTimes[patrol];

        StartTimeLabel.Text = string.IsNullOrEmpty(start) ? "—" : $"{ start } hrs";
        EndTimeLabel.Text = string.IsNullOrEmpty(end) ? "—" : $"{ end } hrs";

        _started = !string.IsNullOrEmpty(start) && string.IsNullOrEmpty(end);
        StartEndButton.Text = _started ? "End Patrol" : "Start Patrol";
    }

    private void UpdateStatusList()
    {
        StatusItems.Clear();

        foreach (var p in _patrolList)
        {
            var (start, end) = _patrolTimes[p];
            string status;

            if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
                status = $"✔ Completed – {p}";
            else if (!string.IsNullOrEmpty(start))
                status = $"● In progress – {p}";
            else
                status = $"○ Not started – {p}";

            StatusItems.Add(new PatrolStatusItem { StatusText = status });
        }
    }

    private void PatrolPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        LoadCurrentPatrolTimes();
    }

    // ---------------------------------------
    // START / END PATROL
    // ---------------------------------------
    private async void HandleStartEnd(object sender, EventArgs e)
    {
        if (PatrolPicker.SelectedItem is not string patrol)
            return;

        var now = FormatTimeHHMM(DateTime.Now);

        if (!_started)
        {
            // START
            _patrolTimes[patrol] = (now, _patrolTimes[patrol].End);

            await _repo.SaveCurrentStateAsync(new CurrentPatrolState
            {
                Location = patrol,
                Start = now,
                End = _patrolTimes[patrol].End
            });

            StartTimeLabel.Text = $"{ now } hrs";
            StartEndButton.Text = "End Patrol";
            _started = true;
        }
        else
        {
            // END
            _patrolTimes[patrol] = (_patrolTimes[patrol].Start, now);

            await _repo.SaveCurrentStateAsync(new CurrentPatrolState
            {
                Location = patrol,
                Start = _patrolTimes[patrol].Start,
                End = now
            });

            // Save permanent history
            await _repo.SaveRecordAsync(new PatrolRecord
            {
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                Shift = ShiftPicker.SelectedItem?.ToString() ?? "",
                Location = patrol,
                Start = _patrolTimes[patrol].Start,
                End = now
            });

            EndTimeLabel.Text = now;
            StartEndButton.Text = "Start Patrol";
            _started = false;

            // Auto-advance
            var idx = PatrolPicker.SelectedIndex;
            PatrolPicker.SelectedIndex = (idx + 1) % _patrolList.Count;
            LoadCurrentPatrolTimes();
        }

        UpdateStatusList();
    }

    // ---------------------------------------
    // RESET BUTTONS
    // ---------------------------------------
    private async void ResetPatrolButton_Clicked(object sender, EventArgs e)
    {
        if (PatrolPicker.SelectedItem is not string patrol)
            return;

        _patrolTimes[patrol] = ("", "");

        await _repo.SaveCurrentStateAsync(new CurrentPatrolState
        {
            Location = patrol,
            Start = "",
            End = ""
        });

        LoadCurrentPatrolTimes();
        UpdateStatusList();
    }

    private async void ResetAllButton_Clicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync("Confirm", "Reset ALL patrols?", "Yes", "No");
        if (!confirm) return;

        foreach (var p in _patrolList)
            _patrolTimes[p] = ("", "");

        await _repo.ClearCurrentStateAsync();

        LoadCurrentPatrolTimes();
        UpdateStatusList();
    }

    // ---------------------------------------
    // SUBMIT ALL PATROLS
    // ---------------------------------------
    private async void SubmitAllButton_Clicked(object sender, EventArgs e)
    {
        await SubmitAllAsync();
    }

    private async Task SubmitAllAsync()
    {
        var shift = ShiftPicker.SelectedItem?.ToString() ?? "Unknown";

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
            await DisplayAlertAsync("No Data", "No patrols recorded.", "OK");
            return;
        }

        var rowsHtml = rowsBuilder.ToString();

        try
        {
            await SendEmailJsAsync(shift, rowsHtml);

            await DisplayAlertAsync("Success", "All patrols sent.", "OK");

            foreach (var p in _patrolList)
                _patrolTimes[p] = ("", "");

            await _repo.ClearCurrentStateAsync();

            LoadCurrentPatrolTimes();
            UpdateStatusList();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Error sending email: {ex.Message}", "OK");
        }
    }

    // ---------------------------------------
    // EMAILJS
    // ---------------------------------------
    private static async Task SendEmailJsAsync(string shift, string rowsHtml)
    {
        const string serviceId = "service_785wfif";
        const string templateId = "template_70klndz";
        const string publicKey = "PXlRgcuYyDEu_pLHZ";

        var payload = new
        {
            service_id = serviceId,
            template_id = templateId,
            user_id = publicKey,
            template_params = new
            {
                shift,
                rows = rowsHtml
            }
        };

        var json = JsonSerializer.Serialize(payload);

        using var client = new HttpClient();

        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://api.emailjs.com/api/v1.0/email/send")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        request.Headers.Add("origin", "http://localhost");

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"EmailJS error {response.StatusCode}: {body}");
        }
    }

    // ---------------------------------------
    // DARK MODE
    // ---------------------------------------
    private void DarkModeSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        bool isDark = e.Value;
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;
            ThemeIcon.Text = isDark ? "🌙" : "🌞";
        }
    }
}

public class PatrolStatusItem
{
    public string StatusText { get; set; } = "";
}
