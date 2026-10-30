using SQLite;

namespace CleanSnap.Database;

[Table("CleanupHistory")]
public class CleanupHistoryRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime CleanedAt { get; set; } = DateTime.UtcNow;

    public long TotalFreedBytes { get; set; }

    public int DuplicatesRemoved { get; set; }

    public int JunkFilesRemoved { get; set; }

    public string Details { get; set; } = string.Empty;

    [Ignore]
    public string ReadableFreed => TotalFreedBytes switch
    {
        < 1024 => $"{TotalFreedBytes} B",
        < 1024 * 1024 => $"{TotalFreedBytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{TotalFreedBytes / (1024.0 * 1024):F2} MB",
        _ => $"{TotalFreedBytes / (1024.0 * 1024 * 1024):F2} GB"
    };

    [Ignore]
    public string FormattedDate => CleanedAt.ToLocalTime().ToString("g");
}
