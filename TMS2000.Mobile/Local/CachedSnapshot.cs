using SQLite;

namespace TMS2000.Mobile.Local;

public class CachedSnapshot
{
    [PrimaryKey]
    public string Email { get; set; } = string.Empty;

    public string SnapshotJson { get; set; } = string.Empty;

    public DateTime SavedAt { get; set; }

    public double ServerClockOffsetSeconds { get; set; }
}
