using SQLite;

namespace SscPatrolLogger.Models;

public class CurrentPatrolState
{
    [PrimaryKey]
    public string Location { get; set; } = "";

    public string Start { get; set; } = "";
    public string End { get; set; } = "";
}
