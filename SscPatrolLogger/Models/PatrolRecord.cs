using SQLite;

namespace SscPatrolLogger.Models;

public class PatrolRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Date { get; set; } = "";
    public string Shift { get; set; } = "";
    public string Location { get; set; } = "";
    public string Start { get; set; } = "";
    public string End { get; set; } = "";
}
