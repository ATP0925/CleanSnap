using CleanSnap.Models;

namespace CleanSnap.Services.Interfaces;

/// <summary>
/// Platform-abstracted file system operations.
/// Implemented separately for Android and Windows.
/// </summary>
public interface IFileSystemService
{
    /// <summary>Returns the default folders to scan on this platform.</summary>
    Task<List<string>> GetDefaultScanFoldersAsync();

    /// <summary>Asynchronously enumerates all files in a directory tree.</summary>
    IAsyncEnumerable<FileMetadata> EnumerateFilesAsync(
        string rootPath,
        CancellationToken cancellationToken = default);

    /// <summary>Safely moves a file to the OS Recycle Bin / Trash.</summary>
    Task<bool> DeleteToTrashAsync(string filePath);

    /// <summary>Gets overall storage information (total, used, free).</summary>
    Task<StorageBreakdown> GetStorageInfoAsync();

    /// <summary>Checks if the app has the required file system permissions.</summary>
    Task<bool> HasFilePermissionsAsync();

    /// <summary>Requests file system permissions from the user.</summary>
    Task<bool> RequestFilePermissionsAsync();

    /// <summary>Reads a portion of a file for partial hashing.</summary>
    Task<byte[]> ReadFileBytesAsync(string filePath, long offset, int count,
        CancellationToken cancellationToken = default);

    /// <summary>Checks if a file exists and is accessible.</summary>
    bool FileExists(string filePath);
}

