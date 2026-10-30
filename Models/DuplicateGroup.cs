namespace CleanSnap.Models;

/// <summary>
/// A group of files that are byte-for-byte identical (confirmed via SHA-256).
/// One file is designated as the "original" to keep; the rest are duplicates.
/// </summary>
public class DuplicateGroup
{
    /// <summary>The file selected to KEEP (highest score by heuristic).</summary>
    public required FileMetadata Original { get; set; }

    /// <summary>Files confirmed as duplicates (candidates for deletion).</summary>
    public required List<FileMetadata> Duplicates { get; set; }

    /// <summary>The shared SHA-256 hash across all files in this group.</summary>
    public required string Hash { get; set; }

    /// <summary>Total bytes that can be freed by deleting all duplicates.</summary>
    public long ReclaimableBytes => Duplicates.Sum(d => d.SizeBytes);

    /// <summary>Human-readable reclaimable size.</summary>
    public string ReadableReclaimable => ReclaimableBytes switch
    {
        < 1024 => $"{ReclaimableBytes} B",
        < 1024 * 1024 => $"{ReclaimableBytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{ReclaimableBytes / (1024.0 * 1024):F2} MB",
        _ => $"{ReclaimableBytes / (1024.0 * 1024 * 1024):F2} GB"
    };

    /// <summary>Total number of duplicate copies (excluding the original).</summary>
    public int DuplicateCount => Duplicates.Count;

    /// <summary>File extension icon hint.</summary>
    public string IconGlyph => Original.Extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => "🖼️",
        ".mp4" or ".avi" or ".mkv" or ".mov" => "🎬",
        ".mp3" or ".wav" or ".flac" or ".aac" => "🎵",
        ".pdf" => "📕",
        ".doc" or ".docx" => "📄",
        ".ppt" or ".pptx" => "📊",
        ".xls" or ".xlsx" => "📈",
        ".zip" or ".rar" or ".7z" => "📦",
        ".apk" => "📱",
        ".exe" or ".msi" => "💿",
        _ => "📁"
    };
}

