namespace CleanSnap.Models;

/// <summary>
/// Represents a category of junk/temporary files that can be cleaned.
/// </summary>
public class JunkCategory
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string IconGlyph { get; set; }
    public long SizeBytes { get; set; }
    public bool IsSafeToDelete { get; set; } = true;
    public bool IsSelected { get; set; }
    public List<string> Paths { get; set; } = [];
    public int FileCount { get; set; }

    public string ReadableSize => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{SizeBytes / (1024.0 * 1024):F2} MB",
        _ => $"{SizeBytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}

/// <summary>
/// Result of analyzing all junk categories.
/// </summary>
public class JunkAnalysis
{
    public List<JunkCategory> Categories { get; set; } = [];
    public long TotalJunkBytes => Categories.Sum(c => c.SizeBytes);
    public int TotalFileCount => Categories.Sum(c => c.FileCount);

    public string ReadableTotalJunk => TotalJunkBytes switch
    {
        < 1024 => $"{TotalJunkBytes} B",
        < 1024 * 1024 => $"{TotalJunkBytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{TotalJunkBytes / (1024.0 * 1024):F2} MB",
        _ => $"{TotalJunkBytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}

/// <summary>
/// Report after a cleanup operation completes.
/// </summary>
public class CleanupReport
{
    public int DeletedCount { get; set; }
    public int FailedCount { get; set; }
    public long FreedBytes { get; set; }
    public List<string> Errors { get; set; } = [];
    public DateTime CleanedAt { get; set; } = DateTime.Now;

    public string ReadableFreed => FreedBytes switch
    {
        < 1024 => $"{FreedBytes} B",
        < 1024 * 1024 => $"{FreedBytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{FreedBytes / (1024.0 * 1024):F2} MB",
        _ => $"{FreedBytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}

