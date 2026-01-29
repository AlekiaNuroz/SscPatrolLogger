using SscPatrolLogger.Models;

namespace SscPatrolLogger.Services;

public interface IPatrolRepository
{
    // CURRENT STATE (Crash Recovery)
    Task<List<CurrentPatrolState>> GetCurrentStateAsync();
    Task SaveCurrentStateAsync(CurrentPatrolState state);
    Task ClearCurrentStateAsync();

    // PERMANENT HISTORY
    Task SaveRecordAsync(PatrolRecord record);
    Task<List<PatrolRecord>> GetHistoryAsync();
}
