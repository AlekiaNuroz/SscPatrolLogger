using SscPatrolLogger.Models;
using SscPatrolLogger.Services;
using System.Collections.ObjectModel;

namespace SscPatrolLogger;

public partial class HistoryPage : ContentPage
{
    private readonly PatrolRepository _repo;
    private List<PatrolRecord> _allRecords = [];

    public ObservableCollection<HistoryGroup> Groups { get; } = [];

    public HistoryPage(PatrolRepository repo)
    {
        InitializeComponent();
        _repo = repo;

        HistoryCollectionView.ItemsSource = Groups;

        ShiftFilter.ItemsSource = new[]
        {
            "All Shifts",
            "Thursday Morning",
            "Thursday Night",
            "Friday Morning",
            "Friday Night"
        };
        ShiftFilter.SelectedIndex = 0;

        DateFilter.Date = DateTime.Today;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHistoryAsync();
        ApplyFilters();
    }

    private async Task LoadHistoryAsync()
    {
        _allRecords = await _repo.GetHistoryAsync();
    }

    private void OnFilterChanged(object sender, EventArgs e)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        Groups.Clear();

        string selectedShift = ShiftFilter.SelectedItem?.ToString() ?? "All Shifts";
        string selectedDate = DateFilter.Date.HasValue
            ? DateFilter.Date.Value.ToString("yyyy-MM-dd")
            : DateTime.Today.ToString("yyyy-MM-dd");
        string search = SearchBar.Text?.Trim() ?? "";

        var filtered = _allRecords.Where(r =>
            (selectedShift == "All Shifts" || r.Shift == selectedShift) &&
            (r.Date == selectedDate) &&
            (string.IsNullOrEmpty(search) ||
             (r.Location?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
        ).ToList();

        if (filtered.Count == 0)
        {
            var emptyGroup = new HistoryGroup { Date = "No Results" };
            emptyGroup.Add(new HistoryItem { Line = "No patrols match the selected filters." });
            Groups.Add(emptyGroup);
            return;
        }

        var grouped = filtered
            .GroupBy(r => r.Date)
            .OrderByDescending(g => g.Key);

        foreach (var group in grouped)
        {
            var historyGroup = new HistoryGroup { Date = group.Key };

            foreach (var r in group)
            {
                historyGroup.Add(new HistoryItem
                {
                    Line = $"{r.Location} – {r.Start} hrs to {r.End} hrs"
                });
            }

            Groups.Add(historyGroup);
        }
    }
}

public class HistoryGroup : ObservableCollection<HistoryItem>
{
    public string Date { get; set; } = "";
}

public class HistoryItem
{
    public string Line { get; set; } = "";
}
