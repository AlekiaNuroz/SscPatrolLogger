using SQLite;
using SscPatrolLogger.Models;

namespace SscPatrolLogger.Services;

public class PatrolRepository : IPatrolRepository
{
    private readonly SQLiteAsyncConnection _db;

    public PatrolRepository(string dbPath)
    {
        _db = new SQLiteAsyncConnection(dbPath);

        _db.CreateTableAsync<CurrentPatrolState>().Wait();
        _db.CreateTableAsync<PatrolRecord>().Wait();
    }

    // ---------------------------
    // CURRENT STATE (Crash Recovery)
    // ---------------------------

    public Task<List<CurrentPatrolState>> GetCurrentStateAsync() =>
        _db.Table<CurrentPatrolState>().ToListAsync();

    public Task SaveCurrentStateAsync(CurrentPatrolState state) =>
        _db.InsertOrReplaceAsync(state);

    public Task ClearCurrentStateAsync() =>
        _db.DeleteAllAsync<CurrentPatrolState>();

    // ---------------------------
    // PERMANENT HISTORY
    // ---------------------------

    public Task SaveRecordAsync(PatrolRecord record) =>
        _db.InsertAsync(record);

    public Task<List<PatrolRecord>> GetHistoryAsync() =>
        _db.Table<PatrolRecord>()
           .OrderByDescending(r => r.Id)
           .ToListAsync();
}
