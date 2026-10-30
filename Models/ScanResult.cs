namespace CleanSnap.Models;

/// <summary>
/// Complete result of a duplicate scan operation.
/// </summary>
public class ScanResult
{
    public int TotalFilesScanned { get; set; }
    public int TotalDuplicateGroups { get; set; }
    public int TotalDuplicateFiles { get; set; }
    public long TotalReclaimableBytes { get; set; }
    public List<DuplicateGroup> Groups { get; set; } = [];
    public TimeSpan ScanDuration { get; set; }

    public string ReadableReclaimable => TotalReclaimableBytes switch
    {
        < 1024 => $"{TotalReclaimableBytes} B",
        < 1024 * 1024 => $"{TotalReclaimableBytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{TotalReclaimableBytes / (1024.0 * 1024):F2} MB",
        _ => $"{TotalReclaimableBytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}

/// <summary>
/// Progress update emitted during scanning.
/// </summary>
public record ScanProgress(
    int Stage,
    string StageDescription,
    int FilesProcessed,
    int TotalFiles,
    int DuplicatesFound,
    long ReclaimableBytes,
    double ProgressPercentage
);

