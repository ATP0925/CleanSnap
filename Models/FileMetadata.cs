namespace CleanSnap.Models;

/// <summary>
/// Represents metadata about a single file discovered during scanning.
/// </summary>
public record FileMetadata
{
    public required string FullPath { get; init; }
    public required string FileName { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTime ModifiedDate { get; init; }
    public required string Extension { get; init; }

    /// <summary>xxHash64 of first 4KB + last 4KB (Stage 3 quick filter)</summary>
    public string? PartialHash { get; set; }

    /// <summary>SHA-256 of entire file content (Stage 4 confirmation)</summary>
    public string? FullHash { get; set; }

    /// <summary>Human-readable file size (e.g. "3.13 MB")</summary>
    public string ReadableSize => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{SizeBytes / (1024.0 * 1024):F2} MB",
        _ => $"{SizeBytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}

