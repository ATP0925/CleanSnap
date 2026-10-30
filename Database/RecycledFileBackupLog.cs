using SQLite;

namespace CleanSnap.Database;

[Table("RecycledFileBackupLog")]
public class RecycledFileBackupLog
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string OriginalFilePath { get; set; } = string.Empty;

    public string DuplicateFilePath { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public string Sha256Hash { get; set; } = string.Empty;

    public DateTime RecycledAt { get; set; } = DateTime.UtcNow;

    public bool IsRestored { get; set; }

    [Ignore]
    public string FileName => System.IO.Path.GetFileName(DuplicateFilePath);

    [Ignore]
    public string ReadableSize => FileSizeBytes switch
    {
        < 1024 => $"{FileSizeBytes} B",
        < 1024 * 1024 => $"{FileSizeBytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{FileSizeBytes / (1024.0 * 1024):F2} MB",
        _ => $"{FileSizeBytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}
