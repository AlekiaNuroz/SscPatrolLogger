using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SscPatrolLogger.Models;
using SscPatrolLogger.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace SscPatrolLogger.ViewModels;

public sealed partial class HistoryPageViewModel : ObservableObject
{
    private readonly IPatrolRepository _repo;
    private List<PatrolRecord> _allRecords = [];

    public ObservableCollection<HistoryGroup> Groups { get; } = [];

    public IReadOnlyList<string> ShiftOptions { get; } =
    [
        "All Shifts",
        "Thursday Morning",
        "Thursday Night",
        "Friday Morning",
        "Friday Night"
    ];

    [ObservableProperty]
    private string selectedShift = "All Shifts";

    [ObservableProperty]
    private string searchText = string.Empty;

    // Nullable "actual filters" (null = no filter)
    [ObservableProperty]
    private DateTime? fromDate;

    [ObservableProperty]
    private DateTime? toDate;

    // Non-null display values for DatePickers (DatePicker can't be null)
    [ObservableProperty]
    private DateTime fromDateDisplay = DateTime.Today;

    [ObservableProperty]
    private DateTime toDateDisplay = DateTime.Today;

    [ObservableProperty]
    private string dateRangeStatus = "All dates";

    public HistoryPageViewModel(IPatrolRepository repo)
    {
        _repo = repo;

        // Default: all dates (both null), UI shows status "All dates"
        UpdateDateRangeStatus();
    }

    public async Task LoadAsync()
    {
        FromDate = null;
        ToDate = null;
        SearchText = string.Empty;
        SelectedShift = "All Shifts";
        UpdateDateRangeStatus();

        _allRecords = await _repo.GetHistoryAsync();

        ApplyFilters();
    }

    public void SetFromDate(DateTime newDate)
    {
        FromDateDisplay = newDate;
        FromDate = newDate.Date;
        UpdateDateRangeStatus();
        ApplyFilters();
    }

    public void SetToDate(DateTime newDate)
    {
        ToDateDisplay = newDate;
        ToDate = newDate.Date;
        UpdateDateRangeStatus();
        ApplyFilters();
    }

    [RelayCommand]
    private void ClearDateRange()
    {
        FromDate = null;
        ToDate = null;

        // Keep the pickers showing something reasonable
        FromDateDisplay = DateTime.Today;
        ToDateDisplay = DateTime.Today;

        UpdateDateRangeStatus();
        ApplyFilters();
    }

    public void ApplyFilters()
    {
        Groups.Clear();

        var shift = SelectedShift ?? "All Shifts";
        var search = (SearchText ?? string.Empty).Trim();

        DateTime? from = FromDate?.Date;
        DateTime? to = ToDate?.Date;

        // If user picks an inverted range, normalize it
        if (from is not null && to is not null && from > to)
        {
            (from, to) = (to, from);
        }

        var filtered = _allRecords.Where(r =>
        {
            if (shift != "All Shifts" && !string.Equals(r.Shift, shift, StringComparison.Ordinal))
                return false;

            if (!string.IsNullOrEmpty(search))
            {
                var loc = r.Location ?? string.Empty;
                if (!loc.Contains(search, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Your records store Date as "yyyy-MM-dd" string right now
            if (!TryParseRecordDate(r.Date, out var recordDate))
                return false;

            if (from is not null && recordDate < from.Value.Date)
                return false;

            if (to is not null && recordDate > to.Value.Date)
                return false;

            return true;
        }).ToList();

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

    private static bool TryParseRecordDate(string? value, out DateTime dt)
    {
        dt = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        return DateTime.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out dt);
    }

    private void UpdateDateRangeStatus()
    {
        DateTime? from = FromDate?.Date;
        DateTime? to = ToDate?.Date;

        if (from is null && to is null)
        {
            DateRangeStatus = "All dates";
            return;
        }

        if (from is not null && to is null)
        {
            DateRangeStatus = $"From {from:yyyy-MM-dd}";
            return;
        }

        if (from is null && to is not null)
        {
            DateRangeStatus = $"Up to {to:yyyy-MM-dd}";
            return;
        }

        // Both set
        if (from > to) (from, to) = (to, from);
        DateRangeStatus = $"{from:yyyy-MM-dd} to {to:yyyy-MM-dd}";
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
