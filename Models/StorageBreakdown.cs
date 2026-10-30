namespace CleanSnap.Models;

/// <summary>
/// Storage breakdown by file category.
/// </summary>
public class StorageBreakdown
{
    public long TotalBytes { get; set; }
    public long UsedBytes { get; set; }
    public long FreeBytes { get; set; }
    public List<StorageCategory> Categories { get; set; } = [];

    public double UsedPercentage => TotalBytes > 0 ? (double)UsedBytes / TotalBytes * 100 : 0;
    public string ReadableTotal => FormatBytes(TotalBytes);
    public string ReadableUsed => FormatBytes(UsedBytes);
    public string ReadableFree => FormatBytes(FreeBytes);

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F2} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}

/// <summary>
/// A single category within the storage breakdown.
/// </summary>
public class StorageCategory
{
    public required string Name { get; set; }
    public required string IconGlyph { get; set; }
    public long SizeBytes { get; set; }
    public int FileCount { get; set; }
    public string Color { get; set; } = "#2196F3";

    public string ReadableSize => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{SizeBytes / (1024.0 * 1024):F2} MB",
        _ => $"{SizeBytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}

